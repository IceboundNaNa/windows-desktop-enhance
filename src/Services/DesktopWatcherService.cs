using System;
using System.IO;
using System.Threading;
using DesktopRightAlign.Models;
using DesktopRightAlign.Native;
using Microsoft.Win32;

namespace DesktopRightAlign.Services
{
    public class DesktopWatcherService : IDisposable
    {
        private readonly FileSystemWatcher _userDesktopWatcher;
        private readonly FileSystemWatcher _publicDesktopWatcher;
        private readonly Timer _debounceTimer;
        private readonly Timer _guardTimer;
        private readonly object _syncLock = new object();

        private AppSettings _currentSettings = new AppSettings();
        private int _debounceMs = 450;
        private bool _autoArrangeActive;
        private bool _layoutGuardActive;
        private bool _disposed;
        private IntPtr _lastKnownListViewHwnd = IntPtr.Zero;
        private bool _pendingDisplayReset;

        public event Action<string> ArrangeTriggered;
        public event Action<string> LayoutGuardTriggered;

        public DesktopWatcherService()
        {
            _debounceTimer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);
            _guardTimer = new Timer(OnGuardTick, null, 2000, 2000);

            _userDesktopWatcher = CreateWatcher(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            _publicDesktopWatcher = CreateWatcher(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));

            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        public void Configure(AppSettings settings)
        {
            lock (_syncLock)
            {
                _currentSettings = settings;
                _debounceMs = Math.Max(150, settings.DebounceMilliseconds);
                _autoArrangeActive = settings.AutoArrangeEnabled;
                _layoutGuardActive = settings.LayoutGuardEnabled;

                SetWatcherRaising(_userDesktopWatcher, _autoArrangeActive);
                SetWatcherRaising(_publicDesktopWatcher, _autoArrangeActive);
            }
        }

        private FileSystemWatcher CreateWatcher(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return null;
            }

            try
            {
                FileSystemWatcher watcher = new FileSystemWatcher(path);
                watcher.IncludeSubdirectories = false;
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size;
                watcher.Created += OnDesktopFsChanged;
                watcher.Deleted += OnDesktopFsChanged;
                watcher.Renamed += OnDesktopFsRenamed;
                watcher.Changed += OnDesktopFsChanged;
                watcher.EnableRaisingEvents = false;
                return watcher;
            }
            catch
            {
                return null;
            }
        }

        private static void SetWatcherRaising(FileSystemWatcher watcher, bool enable)
        {
            if (watcher == null) return;
            try
            {
                watcher.EnableRaisingEvents = enable;
            }
            catch
            {
            }
        }

        private void OnDesktopFsChanged(object sender, FileSystemEventArgs e)
        {
            if (ShouldIgnoreFile(e.Name)) return;
            ScheduleDebounce(false);
        }

        private void OnDesktopFsRenamed(object sender, RenamedEventArgs e)
        {
            if (ShouldIgnoreFile(e.Name)) return;
            ScheduleDebounce(false);
        }

        private static bool ShouldIgnoreFile(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            if (name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.StartsWith("~$", StringComparison.Ordinal)) return true;
            return false;
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            ScheduleDebounce(true);
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionUnlock || e.Reason == SessionSwitchReason.RemoteConnect || e.Reason == SessionSwitchReason.ConsoleConnect)
            {
                ScheduleDebounce(true);
            }
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                ScheduleDebounce(true);
            }
        }

        private void ScheduleDebounce(bool isSystemResetEvent)
        {
            lock (_syncLock)
            {
                if (_disposed) return;
                if (!_autoArrangeActive && !_layoutGuardActive) return;
                if (isSystemResetEvent)
                {
                    _pendingDisplayReset = true;
                }
                int delay = isSystemResetEvent ? Math.Max(900, _debounceMs) : _debounceMs;
                _debounceTimer.Change(delay, Timeout.Infinite);
            }
        }

        private void OnDebounceElapsed(object state)
        {
            bool autoArrange;
            bool layoutGuard;
            bool wasSystemReset;

            lock (_syncLock)
            {
                if (_disposed) return;
                autoArrange = _autoArrangeActive;
                layoutGuard = _layoutGuardActive;
                wasSystemReset = _pendingDisplayReset;
                _pendingDisplayReset = false;
            }

            // Never fight the user while dragging icons with the mouse
            if (Win32Interop.IsMouseButtonDown())
            {
                ScheduleDebounce(wasSystemReset);
                return;
            }

            // When Auto Right-Align is enabled, it takes over system auto-arrange completely to prevent conflict
            if (autoArrange)
            {
                Action<string> arrangeHandler = ArrangeTriggered;
                if (arrangeHandler != null)
                {
                    arrangeHandler(wasSystemReset ? "显示/会话已变更，接管并自动靠右排列" : "桌面文件变化，自动靠右排列");
                }
                return;
            }

            if (wasSystemReset && layoutGuard)
            {
                Action<string> guardHandler = LayoutGuardTriggered;
                if (guardHandler != null)
                {
                    guardHandler("检测到系统显示/会话切换，正在检查并还原已保存的桌面布局");
                }
            }
        }

        private void OnGuardTick(object state)
        {
            bool autoArrange;
            bool guardEnabled;
            AppSettings settings;
            lock (_syncLock)
            {
                if (_disposed) return;
                autoArrange = _autoArrangeActive;
                guardEnabled = _layoutGuardActive;
                settings = _currentSettings;
            }

            if ((!autoArrange && !guardEnabled) || Win32Interop.IsMouseButtonDown())
            {
                return;
            }

            try
            {
                DesktopIconManager manager = new DesktopIconManager();
                DesktopEnvironmentInfo env = manager.GetEnvironmentInfo();
                bool explorerRestarted = _lastKnownListViewHwnd != IntPtr.Zero && env.ListViewHwnd != IntPtr.Zero && env.ListViewHwnd != _lastKnownListViewHwnd;
                if (env.ListViewHwnd != IntPtr.Zero)
                {
                    _lastKnownListViewHwnd = env.ListViewHwnd;
                }

                // Mode 1: Auto Right-Align Takeover Mode (takes over FWF_AUTOARRANGE and prevents system conflicts)
                if (autoArrange)
                {
                    bool nativeAutoOn = false;
                    if (explorerRestarted || manager.NeedsRightAlignTakeover(settings, 40, out nativeAutoOn))
                    {
                        Action<string> handler = ArrangeTriggered;
                        if (handler != null)
                        {
                            string reason = explorerRestarted
                                ? "检测到资源管理器重启，重新接管并靠右排列"
                                : (nativeAutoOn
                                    ? "检测到系统开启原生自动排列，已自动接管并靠右排列"
                                    : "检测到桌面图标偏移，已自动靠右对齐");
                            handler(reason);
                        }
                    }
                    return;
                }

                // Mode 2: Saved Layout Guard Mode (only when Auto Right-Align is off)
                if (guardEnabled && manager.HasBackup())
                {
                    if (explorerRestarted || env.NativeAutoArrange || manager.HasDriftedFromSavedLayout(48))
                    {
                        Action<string> handler = LayoutGuardTriggered;
                        if (handler != null)
                        {
                            handler(explorerRestarted
                                ? "检测到资源管理器重启，自动还原已保存的桌面布局"
                                : "检测到系统重置桌面图标位置，自动还原已保存的布局");
                        }
                    }
                }
            }
            catch
            {
                // Explorer may be restarting transiently; retry on next tick.
            }
        }

        public void Dispose()
        {
            lock (_syncLock)
            {
                if (_disposed) return;
                _disposed = true;
            }

            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;

            _debounceTimer.Dispose();
            _guardTimer.Dispose();

            if (_userDesktopWatcher != null)
            {
                _userDesktopWatcher.EnableRaisingEvents = false;
                _userDesktopWatcher.Dispose();
            }
            if (_publicDesktopWatcher != null)
            {
                _publicDesktopWatcher.EnableRaisingEvents = false;
                _publicDesktopWatcher.Dispose();
            }
        }
    }
}
