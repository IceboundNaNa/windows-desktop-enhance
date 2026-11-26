using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using DesktopRightAlign.Models;

namespace DesktopRightAlign.Native
{
    public class DesktopIconManager
    {
        private class DesktopItemEntry
        {
            public IntPtr Pidl;
            public DesktopIconInfo Info;
        }

        private class DesktopComSession : IDisposable
        {
            public IShellWindows ShellWindows;
            public object DesktopDispatch;
            public IShellBrowser ShellBrowser;
            public IShellView ShellView;
            public IFolderView FolderView;
            public IFolderView2 FolderView2;
            public IShellFolder ShellFolder;
            public IShellFolder2 ShellFolder2;
            public IntPtr DefViewHwnd;
            public IntPtr ListViewHwnd;

            public void Dispose()
            {
                ReleaseCom(ref ShellFolder2);
                ReleaseCom(ref ShellFolder);
                ReleaseCom(ref FolderView2);
                ReleaseCom(ref FolderView);
                ReleaseCom(ref ShellView);
                ReleaseCom(ref ShellBrowser);
                ReleaseCom(ref DesktopDispatch);
                ReleaseCom(ref ShellWindows);
            }

            private static void ReleaseCom<T>(ref T comObj) where T : class
            {
                if (comObj != null && Marshal.IsComObject(comObj))
                {
                    try
                    {
                        Marshal.ReleaseComObject(comObj);
                    }
                    catch
                    {
                    }
                }
                comObj = null;
            }
        }

        private DesktopComSession OpenSession()
        {
            DesktopComSession session = new DesktopComSession();
            try
            {
                Type shellWindowsType = Type.GetTypeFromCLSID(ShellGuids.CLSID_ShellWindows);
                if (shellWindowsType == null)
                {
                    throw new InvalidOperationException("无法获取 ShellWindows COM 类。");
                }

                session.ShellWindows = (IShellWindows)Activator.CreateInstance(shellWindowsType);
                object empty = Type.Missing;
                int desktopHwnd;
                session.DesktopDispatch = session.ShellWindows.FindWindowSW(
                    ref empty,
                    ref empty,
                    ShellConstants.SWC_DESKTOP,
                    out desktopHwnd,
                    ShellConstants.SWFO_NEEDDISPATCH);

                if (session.DesktopDispatch == null)
                {
                    throw new InvalidOperationException("无法连接到 Windows 桌面 Shell 视图（explorer.exe 可能未运行）。");
                }

                IServiceProvider serviceProvider = (IServiceProvider)session.DesktopDispatch;
                Guid sid = ShellGuids.SID_STopLevelBrowser;
                Guid iidBrowser = ShellGuids.IID_IShellBrowser;
                object browserObj;
                int hr = serviceProvider.QueryService(ref sid, ref iidBrowser, out browserObj);
                if (hr < 0 || browserObj == null)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                session.ShellBrowser = (IShellBrowser)browserObj;
                hr = session.ShellBrowser.QueryActiveShellView(out session.ShellView);
                if (hr < 0 || session.ShellView == null)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                session.ShellView.GetWindow(out session.DefViewHwnd);
                if (session.DefViewHwnd != IntPtr.Zero)
                {
                    session.ListViewHwnd = Win32Interop.FindWindowEx(session.DefViewHwnd, IntPtr.Zero, "SysListView32", null);
                    if (session.ListViewHwnd == IntPtr.Zero)
                    {
                        session.ListViewHwnd = session.DefViewHwnd;
                    }
                }

                session.FolderView = (IFolderView)session.ShellView;
                session.FolderView2 = session.ShellView as IFolderView2;

                Guid iidFolder = ShellGuids.IID_IShellFolder;
                object folderObj;
                hr = session.FolderView.GetFolder(ref iidFolder, out folderObj);
                if (hr < 0 || folderObj == null)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                session.ShellFolder = (IShellFolder)folderObj;
                session.ShellFolder2 = folderObj as IShellFolder2;
                return session;
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        public DesktopEnvironmentInfo GetEnvironmentInfo()
        {
            using (DesktopComSession session = OpenSession())
            {
                return ReadEnvironmentInfo(session);
            }
        }

        private DesktopEnvironmentInfo ReadEnvironmentInfo(DesktopComSession session)
        {
            RECT workArea = Win32Interop.GetPrimaryDesktopWorkArea(session.ListViewHwnd);

            POINT spacing = new POINT(0, 0);
            int hr = session.FolderView.GetSpacing(ref spacing);
            if (hr < 0 || spacing.X <= 0 || spacing.Y <= 0)
            {
                session.FolderView.GetDefaultSpacing(out spacing);
            }
            if (spacing.X <= 0) spacing.X = 96;
            if (spacing.Y <= 0) spacing.Y = 96;

            int count = 0;
            session.FolderView.ItemCount(ShellConstants.SVGIO_ALLVIEW, out count);

            bool autoArrange = false;
            bool snapToGrid = false;
            if (session.FolderView2 != null)
            {
                uint flags;
                if (session.FolderView2.GetCurrentFolderFlags(out flags) == 0)
                {
                    autoArrange = (flags & ShellConstants.FWF_AUTOARRANGE) != 0;
                    snapToGrid = (flags & ShellConstants.FWF_SNAPTOGRID) != 0;
                }
            }
            else
            {
                autoArrange = session.FolderView.GetAutoArrange() == 0;
            }

            return new DesktopEnvironmentInfo
            {
                WorkAreaLeft = workArea.Left,
                WorkAreaTop = workArea.Top,
                WorkAreaWidth = workArea.Width,
                WorkAreaHeight = workArea.Height,
                SpacingX = spacing.X,
                SpacingY = spacing.Y,
                NativeAutoArrange = autoArrange,
                NativeSnapToGrid = snapToGrid,
                IconCount = count,
                DefViewHwnd = session.DefViewHwnd,
                ListViewHwnd = session.ListViewHwnd
            };
        }

        public List<DesktopIconInfo> GetDesktopIconsWithPreview(AppSettings settings, out DesktopEnvironmentInfo envInfo)
        {
            using (DesktopComSession session = OpenSession())
            {
                envInfo = ReadEnvironmentInfo(session);
                List<DesktopItemEntry> entries = EnumerateItems(session);
                try
                {
                    SortEntriesNative(session, entries, settings.SortMode, settings.SortAscending);
                    ComputeRightAlignedTargets(entries, envInfo, settings);

                    List<DesktopIconInfo> result = new List<DesktopIconInfo>(entries.Count);
                    for (int i = 0; i < entries.Count; i++)
                    {
                        result.Add(entries[i].Info);
                    }
                    return result;
                }
                finally
                {
                    FreePidls(entries);
                }
            }
        }

        public int ArrangeRight(AppSettings settings, out DesktopEnvironmentInfo envInfo)
        {
            using (DesktopComSession session = OpenSession())
            {
                envInfo = ReadEnvironmentInfo(session);

                // Always take over Windows' built-in left-aligned Auto Arrange when right-aligning or when configured.
                if ((settings.AutoArrangeEnabled || settings.AutoDisableNativeAutoArrange) && envInfo.NativeAutoArrange && session.FolderView2 != null)
                {
                    session.FolderView2.SetCurrentFolderFlags(ShellConstants.FWF_AUTOARRANGE, 0);
                    envInfo.NativeAutoArrange = false;
                }

                List<DesktopItemEntry> entries = EnumerateItems(session);
                try
                {
                    if (entries.Count == 0)
                    {
                        return 0;
                    }

                    // Auto-create an initial backup once if none exists yet.
                    if (!File.Exists(AppSettings.BackupFilePath))
                    {
                        SaveBackupInternal(entries, envInfo);
                    }

                    SortEntriesNative(session, entries, settings.SortMode, settings.SortAscending);
                    ComputeRightAlignedTargets(entries, envInfo, settings);

                    IntPtr[] pidls = new IntPtr[entries.Count];
                    POINT[] points = new POINT[entries.Count];
                    for (int i = 0; i < entries.Count; i++)
                    {
                        pidls[i] = entries[i].Pidl;
                        points[i] = new POINT(entries[i].Info.TargetX, entries[i].Info.TargetY);
                    }

                    ApplyPositionsFlickerFree(session, pidls, points);
                    return entries.Count;
                }
                finally
                {
                    FreePidls(entries);
                }
            }
        }

        public int BackupCurrentLayout()
        {
            using (DesktopComSession session = OpenSession())
            {
                DesktopEnvironmentInfo env = ReadEnvironmentInfo(session);
                List<DesktopItemEntry> entries = EnumerateItems(session);
                try
                {
                    SaveBackupInternal(entries, env);
                    return entries.Count;
                }
                finally
                {
                    FreePidls(entries);
                }
            }
        }

        public DesktopLayoutBackup GetSavedLayoutMetadata()
        {
            if (!File.Exists(AppSettings.BackupFilePath))
            {
                return null;
            }
            try
            {
                using (FileStream fs = new FileStream(AppSettings.BackupFilePath, FileMode.Open, FileAccess.Read))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(DesktopLayoutBackup));
                    return serializer.ReadObject(fs) as DesktopLayoutBackup;
                }
            }
            catch
            {
                return null;
            }
        }

        public bool DisableNativeAutoArrange()
        {
            using (DesktopComSession session = OpenSession())
            {
                if (session.FolderView2 == null) return false;
                uint flags;
                if (session.FolderView2.GetCurrentFolderFlags(out flags) == 0 && (flags & ShellConstants.FWF_AUTOARRANGE) != 0)
                {
                    session.FolderView2.SetCurrentFolderFlags(ShellConstants.FWF_AUTOARRANGE, 0);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Checks whether Windows native auto-arrange was re-enabled or icons drifted away from their right-aligned targets.
        /// </summary>
        public bool NeedsRightAlignTakeover(AppSettings settings, int tolerancePx, out bool nativeAutoArrangeWasOn)
        {
            nativeAutoArrangeWasOn = false;
            using (DesktopComSession session = OpenSession())
            {
                DesktopEnvironmentInfo env = ReadEnvironmentInfo(session);
                if (env.NativeAutoArrange)
                {
                    nativeAutoArrangeWasOn = true;
                    return true;
                }

                List<DesktopItemEntry> entries = EnumerateItems(session);
                try
                {
                    if (entries.Count == 0) return false;
                    SortEntriesNative(session, entries, settings.SortMode, settings.SortAscending);
                    ComputeRightAlignedTargets(entries, env, settings);

                    for (int i = 0; i < entries.Count; i++)
                    {
                        DesktopIconInfo info = entries[i].Info;
                        if (Math.Abs(info.CurrentX - info.TargetX) > tolerancePx ||
                            Math.Abs(info.CurrentY - info.TargetY) > tolerancePx)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                finally
                {
                    FreePidls(entries);
                }
            }
        }

        public bool HasBackup()
        {
            return File.Exists(AppSettings.BackupFilePath);
        }

        /// <summary>
        /// Checks whether Windows has reset/shifted desktop icons away from the saved layout.
        /// Returns true if a significant portion of saved icons drifted by more than tolerancePx.
        /// </summary>
        public bool HasDriftedFromSavedLayout(int tolerancePx)
        {
            DesktopLayoutBackup backup = GetSavedLayoutMetadata();
            if (backup == null || backup.Icons == null || backup.Icons.Count == 0)
            {
                return false;
            }

            Dictionary<string, SavedIconPosition> byParsing = new Dictionary<string, SavedIconPosition>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, SavedIconPosition> byDisplay = new Dictionary<string, SavedIconPosition>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < backup.Icons.Count; i++)
            {
                SavedIconPosition pos = backup.Icons[i];
                if (!string.IsNullOrEmpty(pos.ParsingName) && !byParsing.ContainsKey(pos.ParsingName))
                {
                    byParsing[pos.ParsingName] = pos;
                }
                if (!string.IsNullOrEmpty(pos.DisplayName) && !byDisplay.ContainsKey(pos.DisplayName))
                {
                    byDisplay[pos.DisplayName] = pos;
                }
            }

            using (DesktopComSession session = OpenSession())
            {
                List<DesktopItemEntry> entries = EnumerateItems(session);
                try
                {
                    int matched = 0;
                    int drifted = 0;
                    for (int i = 0; i < entries.Count; i++)
                    {
                        DesktopItemEntry entry = entries[i];
                        SavedIconPosition saved = null;
                        if (!string.IsNullOrEmpty(entry.Info.ParsingName))
                        {
                            byParsing.TryGetValue(entry.Info.ParsingName, out saved);
                        }
                        if (saved == null && !string.IsNullOrEmpty(entry.Info.DisplayName))
                        {
                            byDisplay.TryGetValue(entry.Info.DisplayName, out saved);
                        }

                        if (saved != null)
                        {
                            matched++;
                            int dx = Math.Abs(entry.Info.CurrentX - saved.X);
                            int dy = Math.Abs(entry.Info.CurrentY - saved.Y);
                            if (dx > tolerancePx || dy > tolerancePx)
                            {
                                drifted++;
                            }
                        }
                    }

                    if (matched == 0)
                    {
                        return false;
                    }

                    // If >= 2 icons (or >= 25% of icons) suddenly drifted, Windows likely reset the desktop layout
                    return drifted >= 2 || (matched == 1 && drifted == 1) || ((double)drifted / matched >= 0.25);
                }
                finally
                {
                    FreePidls(entries);
                }
            }
        }

        public int RestoreBackupLayout()
        {
            if (!File.Exists(AppSettings.BackupFilePath))
            {
                return 0;
            }

            DesktopLayoutBackup backup;
            using (FileStream fs = new FileStream(AppSettings.BackupFilePath, FileMode.Open, FileAccess.Read))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(DesktopLayoutBackup));
                backup = serializer.ReadObject(fs) as DesktopLayoutBackup;
            }

            if (backup == null || backup.Icons == null || backup.Icons.Count == 0)
            {
                return 0;
            }

            Dictionary<string, SavedIconPosition> byParsing = new Dictionary<string, SavedIconPosition>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, SavedIconPosition> byDisplay = new Dictionary<string, SavedIconPosition>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < backup.Icons.Count; i++)
            {
                SavedIconPosition pos = backup.Icons[i];
                if (!string.IsNullOrEmpty(pos.ParsingName) && !byParsing.ContainsKey(pos.ParsingName))
                {
                    byParsing[pos.ParsingName] = pos;
                }
                if (!string.IsNullOrEmpty(pos.DisplayName) && !byDisplay.ContainsKey(pos.DisplayName))
                {
                    byDisplay[pos.DisplayName] = pos;
                }
            }

            using (DesktopComSession session = OpenSession())
            {
                if (session.FolderView2 != null)
                {
                    session.FolderView2.SetCurrentFolderFlags(ShellConstants.FWF_AUTOARRANGE, 0);
                }

                List<DesktopItemEntry> entries = EnumerateItems(session);
                try
                {
                    List<IntPtr> pidls = new List<IntPtr>();
                    List<POINT> points = new List<POINT>();

                    for (int i = 0; i < entries.Count; i++)
                    {
                        DesktopItemEntry entry = entries[i];
                        SavedIconPosition saved = null;
                        if (!string.IsNullOrEmpty(entry.Info.ParsingName))
                        {
                            byParsing.TryGetValue(entry.Info.ParsingName, out saved);
                        }
                        if (saved == null && !string.IsNullOrEmpty(entry.Info.DisplayName))
                        {
                            byDisplay.TryGetValue(entry.Info.DisplayName, out saved);
                        }

                        if (saved != null)
                        {
                            pidls.Add(entry.Pidl);
                            points.Add(new POINT(saved.X, saved.Y));
                        }
                    }

                    if (pidls.Count > 0)
                    {
                        ApplyPositionsFlickerFree(session, pidls.ToArray(), points.ToArray());
                    }
                    return pidls.Count;
                }
                finally
                {
                    FreePidls(entries);
                }
            }
        }

        private void ApplyPositionsFlickerFree(DesktopComSession session, IntPtr[] pidls, POINT[] points)
        {
            IntPtr lv = session.ListViewHwnd;
            if (lv != IntPtr.Zero)
            {
                Win32Interop.SendMessage(lv, Win32Interop.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            }

            try
            {
                int hr = session.FolderView.SelectAndPositionItems(
                    (uint)pidls.Length,
                    pidls,
                    points,
                    ShellConstants.SVSI_POSITIONITEM | ShellConstants.SVSI_NOSTATECHANGE);

                if (hr < 0)
                {
                    // Fallback: position without SVSI_NOSTATECHANGE in case shell rejects combined flag
                    hr = session.FolderView.SelectAndPositionItems(
                        (uint)pidls.Length,
                        pidls,
                        points,
                        ShellConstants.SVSI_POSITIONITEM);
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }
                }
            }
            finally
            {
                if (lv != IntPtr.Zero)
                {
                    Win32Interop.SendMessage(lv, Win32Interop.WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
                    Win32Interop.RedrawWindow(
                        lv,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        Win32Interop.RDW_INVALIDATE | Win32Interop.RDW_ERASE | Win32Interop.RDW_ALLCHILDREN | Win32Interop.RDW_UPDATENOW);
                }
            }
        }

        private List<DesktopItemEntry> EnumerateItems(DesktopComSession session)
        {
            int count = 0;
            int hr = session.FolderView.ItemCount(ShellConstants.SVGIO_ALLVIEW, out count);
            if (hr < 0 || count <= 0)
            {
                return new List<DesktopItemEntry>();
            }

            string userDesktopDir = EnsureTrailingSlash(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            string commonDesktopDir = EnsureTrailingSlash(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));

            List<DesktopItemEntry> entries = new List<DesktopItemEntry>(count);
            for (int i = 0; i < count; i++)
            {
                IntPtr pidl;
                if (session.FolderView.Item(i, out pidl) != 0 || pidl == IntPtr.Zero)
                {
                    continue;
                }

                POINT pt;
                if (session.FolderView.GetItemPosition(pidl, out pt) < 0)
                {
                    pt = new POINT(0, 0);
                }

                string displayName = GetItemDisplayName(session.ShellFolder, pidl, ShellConstants.SHGDN_NORMAL);
                string parsingName = GetItemDisplayName(session.ShellFolder, pidl, ShellConstants.SHGDN_FORPARSING);

                uint attrs = ShellConstants.SFGAO_FOLDER | ShellConstants.SFGAO_STREAM | ShellConstants.SFGAO_FILESYSTEM;
                IntPtr[] singlePidl = new IntPtr[] { pidl };
                session.ShellFolder.GetAttributesOf(1, singlePidl, ref attrs);

                bool isFileSystem = (attrs & ShellConstants.SFGAO_FILESYSTEM) != 0;
                bool isInDesktopDirs = !string.IsNullOrEmpty(parsingName) &&
                    (parsingName.StartsWith(userDesktopDir, StringComparison.OrdinalIgnoreCase) ||
                     parsingName.StartsWith(commonDesktopDir, StringComparison.OrdinalIgnoreCase));

                bool isSystemIcon = !isFileSystem ||
                                    (!string.IsNullOrEmpty(parsingName) && parsingName.StartsWith("::", StringComparison.Ordinal)) ||
                                    !isInDesktopDirs;

                bool isFolder = !isSystemIcon &&
                                (attrs & ShellConstants.SFGAO_FOLDER) != 0 &&
                                (attrs & ShellConstants.SFGAO_STREAM) == 0;

                string itemType = string.Empty;
                long sizeBytes = 0;
                DateTime? modified = null;

                if (session.ShellFolder2 != null)
                {
                    object val;
                    PROPERTYKEY pkType = ShellGuids.PKEY_ItemTypeText;
                    if (session.ShellFolder2.GetDetailsEx(pidl, ref pkType, out val) == 0 && val != null)
                    {
                        itemType = val.ToString();
                    }

                    PROPERTYKEY pkSize = ShellGuids.PKEY_Size;
                    if (session.ShellFolder2.GetDetailsEx(pidl, ref pkSize, out val) == 0 && val != null)
                    {
                        try { sizeBytes = Convert.ToInt64(val); } catch { }
                    }

                    PROPERTYKEY pkDate = ShellGuids.PKEY_DateModified;
                    if (session.ShellFolder2.GetDetailsEx(pidl, ref pkDate, out val) == 0 && val is DateTime)
                    {
                        modified = (DateTime)val;
                    }
                }

                DesktopIconInfo info = new DesktopIconInfo
                {
                    DisplayName = displayName,
                    ParsingName = parsingName,
                    CurrentX = pt.X,
                    CurrentY = pt.Y,
                    TargetX = pt.X,
                    TargetY = pt.Y,
                    ItemType = itemType,
                    SizeBytes = sizeBytes,
                    ModifiedTime = modified,
                    IsSystemIcon = isSystemIcon,
                    IsFolder = isFolder
                };

                entries.Add(new DesktopItemEntry { Pidl = pidl, Info = info });
            }

            return entries;
        }

        private string GetItemDisplayName(IShellFolder folder, IntPtr pidl, uint flags)
        {
            STRRET strret;
            if (folder.GetDisplayNameOf(pidl, flags, out strret) == 0)
            {
                return Win32Interop.StrRetToString(ref strret, pidl);
            }
            return string.Empty;
        }

        private int ResolveNativeColumnIndex(IShellFolder2 folder2, DesktopSortMode sortMode)
        {
            if (folder2 == null)
            {
                return (int)sortMode;
            }

            PROPERTYKEY targetKey;
            switch (sortMode)
            {
                case DesktopSortMode.Size:
                    targetKey = ShellGuids.PKEY_Size;
                    break;
                case DesktopSortMode.ItemType:
                    targetKey = ShellGuids.PKEY_ItemTypeText;
                    break;
                case DesktopSortMode.DateModified:
                    targetKey = ShellGuids.PKEY_DateModified;
                    break;
                case DesktopSortMode.Name:
                default:
                    targetKey = ShellGuids.PKEY_ItemNameDisplay;
                    break;
            }

            for (uint col = 0; col < 32; col++)
            {
                PROPERTYKEY scid;
                int hr = folder2.MapColumnToSCID(col, out scid);
                if (hr < 0)
                {
                    break;
                }
                if (scid.Matches(targetKey))
                {
                    return (int)col;
                }
            }

            // Standard Desktop fallback column indices: 0=Name, 1=Size, 2=ItemType, 3=DateModified
            return (int)sortMode;
        }

        private static string EnsureTrailingSlash(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            return path.EndsWith("\\", StringComparison.Ordinal) ? path : path + "\\";
        }

        private static int GetSortGroupRank(DesktopIconInfo info)
        {
            if (info.IsSystemIcon) return 0; // System icons first (User, This PC, Network, Recycle Bin, Control Panel)
            if (info.IsFolder) return 1;     // Regular desktop folders immediately after system icons
            return 2;                        // Regular files and shortcuts
        }

        private void SortEntriesNative(DesktopComSession session, List<DesktopItemEntry> entries, DesktopSortMode sortMode, bool ascending)
        {
            int nativeColumn = ResolveNativeColumnIndex(session.ShellFolder2, sortMode);
            IntPtr lParam = new IntPtr(nativeColumn);

            entries.Sort(delegate(DesktopItemEntry a, DesktopItemEntry b)
            {
                int groupA = GetSortGroupRank(a.Info);
                int groupB = GetSortGroupRank(b.Info);
                if (groupA != groupB)
                {
                    return groupA.CompareTo(groupB);
                }

                int cmp = 0;
                int hr = session.ShellFolder.CompareIDs(lParam, a.Pidl, b.Pidl);
                if (hr >= 0)
                {
                    cmp = unchecked((short)(hr & 0xFFFF));
                }
                else
                {
                    // Fallback to native StrCmpLogicalW if CompareIDs fails for any exotic item
                    cmp = Win32Interop.StrCmpLogicalW(a.Info.DisplayName ?? string.Empty, b.Info.DisplayName ?? string.Empty);
                }

                if (cmp == 0 && sortMode != DesktopSortMode.Name)
                {
                    int hrTie = session.ShellFolder.CompareIDs(IntPtr.Zero, a.Pidl, b.Pidl);
                    if (hrTie >= 0)
                    {
                        cmp = unchecked((short)(hrTie & 0xFFFF));
                    }
                }

                // Keep system icons in native Windows order; apply asc/desc within folders & files
                if (groupA == 0)
                {
                    return cmp;
                }
                return ascending ? cmp : -cmp;
            });
        }

        private void ComputeRightAlignedTargets(List<DesktopItemEntry> entries, DesktopEnvironmentInfo envInfo, AppSettings settings)
        {
            int stepX = settings.UseNativeGridSpacing ? envInfo.SpacingX : settings.CustomHorizontalSpacing;
            int stepY = settings.UseNativeGridSpacing ? envInfo.SpacingY : settings.CustomVerticalSpacing;
            if (stepX < 48) stepX = 48;
            if (stepY < 48) stepY = 48;

            int workLeft = envInfo.WorkAreaLeft;
            int workTop = envInfo.WorkAreaTop;
            int workRight = envInfo.WorkAreaLeft + envInfo.WorkAreaWidth;
            int workBottom = envInfo.WorkAreaTop + envInfo.WorkAreaHeight;

            int usableHeight = (workBottom - settings.BottomMargin) - (workTop + settings.TopMargin);
            int rowsPerColumn = Math.Max(1, usableHeight / stepY);

            int firstColX = workRight - settings.RightMargin - stepX;
            int topStartY = workTop + settings.TopMargin;
            int bottomStartY = workBottom - settings.BottomMargin - stepY;

            int maxColumns = Math.Max(1, ((firstColX - workLeft) / stepX) + 1);

            List<ExclusionZone> activeZones = new List<ExclusionZone>();
            if (settings.ExclusionZonesEnabled && settings.ExclusionZones != null)
            {
                for (int z = 0; z < settings.ExclusionZones.Count; z++)
                {
                    ExclusionZone zone = settings.ExclusionZones[z];
                    if (zone != null && zone.Enabled && zone.Width > 0 && zone.Height > 0)
                    {
                        activeZones.Add(zone);
                    }
                }
            }

            // Build ordered list of valid right-to-left grid slots, skipping any slot overlapping an exclusion zone.
            List<POINT> availableSlots = new List<POINT>(entries.Count);
            List<POINT> fallbackSlots = new List<POINT>();
            const int inset = 4;

            for (int col = 0; col < maxColumns; col++)
            {
                int x = firstColX - (col * stepX);
                if (x < workLeft)
                {
                    break;
                }

                for (int row = 0; row < rowsPerColumn; row++)
                {
                    int y;
                    if (settings.FlowDirection == VerticalFlowDirection.BottomToTop)
                    {
                        y = bottomStartY - (row * stepY);
                        if (y < workTop) y = workTop;
                    }
                    else
                    {
                        y = topStartY + (row * stepY);
                    }

                    bool blocked = false;
                    if (activeZones.Count > 0)
                    {
                        int cellLeft = x + inset;
                        int cellTop = y + inset;
                        int cellRight = x + stepX - inset;
                        int cellBottom = y + stepY - inset;

                        for (int z = 0; z < activeZones.Count; z++)
                        {
                            if (activeZones[z].Intersects(cellLeft, cellTop, cellRight, cellBottom))
                            {
                                blocked = true;
                                break;
                            }
                        }
                    }

                    if (!blocked)
                    {
                        availableSlots.Add(new POINT(x, y));
                        if (availableSlots.Count >= entries.Count)
                        {
                            break;
                        }
                    }
                    else
                    {
                        fallbackSlots.Add(new POINT(x, y));
                    }
                }

                if (availableSlots.Count >= entries.Count)
                {
                    break;
                }
            }

            // Assign positions: prefer non-excluded slots; if screen is overfilled, gracefully use fallback slots.
            for (int i = 0; i < entries.Count; i++)
            {
                POINT pt;
                if (i < availableSlots.Count)
                {
                    pt = availableSlots[i];
                }
                else
                {
                    int fbIdx = i - availableSlots.Count;
                    if (fbIdx < fallbackSlots.Count)
                    {
                        pt = fallbackSlots[fbIdx];
                    }
                    else
                    {
                        int rowIndex = i % rowsPerColumn;
                        int y = settings.FlowDirection == VerticalFlowDirection.BottomToTop
                            ? Math.Max(workTop, bottomStartY - (rowIndex * stepY))
                            : topStartY + (rowIndex * stepY);
                        pt = new POINT(workLeft, y);
                    }
                }

                entries[i].Info.TargetX = pt.X;
                entries[i].Info.TargetY = pt.Y;
            }
        }

        private void SaveBackupInternal(List<DesktopItemEntry> entries, DesktopEnvironmentInfo envInfo)
        {
            DesktopLayoutBackup backup = new DesktopLayoutBackup
            {
                SavedAtUtc = DateTime.UtcNow.ToString("o"),
                WorkAreaWidth = envInfo != null ? envInfo.WorkAreaWidth : 0,
                WorkAreaHeight = envInfo != null ? envInfo.WorkAreaHeight : 0
            };

            for (int i = 0; i < entries.Count; i++)
            {
                DesktopIconInfo info = entries[i].Info;
                backup.Icons.Add(new SavedIconPosition
                {
                    ParsingName = info.ParsingName,
                    DisplayName = info.DisplayName,
                    X = info.CurrentX,
                    Y = info.CurrentY
                });
            }

            Directory.CreateDirectory(AppSettings.SettingsDirectory);
            using (FileStream fs = new FileStream(AppSettings.BackupFilePath, FileMode.Create, FileAccess.Write))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(DesktopLayoutBackup));
                serializer.WriteObject(fs, backup);
            }
        }

        private static void FreePidls(List<DesktopItemEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Pidl != IntPtr.Zero)
                {
                    Win32Interop.CoTaskMemFree(entries[i].Pidl);
                    entries[i].Pidl = IntPtr.Zero;
                }
            }
        }
    }
}
