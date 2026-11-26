using System;
using System.Runtime.InteropServices;

namespace DesktopRightAlign.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width { get { return Right - Left; } }
        public int Height { get { return Bottom - Top; } }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;

        public PROPERTYKEY(Guid formatId, uint propertyId)
        {
            fmtid = formatId;
            pid = propertyId;
        }

        public bool Matches(PROPERTYKEY other)
        {
            return fmtid == other.fmtid && pid == other.pid;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SORTCOLUMN
    {
        public PROPERTYKEY propkey;
        public int direction;
    }

    [StructLayout(LayoutKind.Explicit, Size = 264)]
    public struct STRRET
    {
        [FieldOffset(0)]
        public uint uType;

        [FieldOffset(4)]
        public IntPtr pOleStr;

        [FieldOffset(4)]
        public uint uOffset;

        [FieldOffset(4)]
        public byte cStr;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SHELLDETAILS
    {
        public int fmt;
        public int cxChar;
        public STRRET str;
    }

    public static class ShellGuids
    {
        public static readonly Guid CLSID_ShellWindows = new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39");
        public static readonly Guid SID_STopLevelBrowser = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837");
        public static readonly Guid IID_IShellBrowser = new Guid("000214E2-0000-0000-C000-000000000046");
        public static readonly Guid IID_IShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
        public static readonly Guid IID_IShellFolder2 = new Guid("93F2F68C-1D1B-11D3-A30E-00C04F79ABD1");

        // Standard Shell Storage Property Set: FMTID_Storage = {B725F130-47EF-101A-A5F1-02608C9EEBAC}
        private static readonly Guid FMTID_Storage = new Guid("B725F130-47EF-101A-A5F1-02608C9EEBAC");

        public static readonly PROPERTYKEY PKEY_ItemNameDisplay = new PROPERTYKEY(FMTID_Storage, 10);
        public static readonly PROPERTYKEY PKEY_Size = new PROPERTYKEY(FMTID_Storage, 12);
        public static readonly PROPERTYKEY PKEY_ItemTypeText = new PROPERTYKEY(FMTID_Storage, 4);
        public static readonly PROPERTYKEY PKEY_DateModified = new PROPERTYKEY(FMTID_Storage, 14);
    }

    public static class ShellConstants
    {
        public const int SWC_DESKTOP = 0x08;
        public const int SWFO_NEEDDISPATCH = 0x01;

        public const uint SVGIO_ALLVIEW = 0x00000002;

        public const uint SVSI_DESELECT = 0x00000000;
        public const uint SVSI_POSITIONITEM = 0x00000080;
        public const uint SVSI_NOSTATECHANGE = 0x80000000;

        public const uint FWF_AUTOARRANGE = 0x00000001;
        public const uint FWF_SNAPTOGRID = 0x00080000;

        public const uint SHGDN_NORMAL = 0x0000;
        public const uint SHGDN_INFOLDER = 0x0001;
        public const uint SHGDN_FORPARSING = 0x8000;

        public const uint SFGAO_FOLDER = 0x20000000;
    }

    [ComImport]
    [Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IShellWindows
    {
        int Count { get; }
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object Item([In, Optional] object index);
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object _NewEnum();
        void Register([In, MarshalAs(UnmanagedType.IDispatch)] object pid, [In] int hwnd, [In] int swClass, out int plCookie);
        void RegisterPending([In] int lThreadId, [In] ref object pvarloc, [In] ref object pvarlocRoot, [In] int swClass, out int plCookie);
        void Revoke([In] int lCookie);
        void OnNavigate([In] int lCookie, [In] ref object pvarloc);
        void OnActivated([In] int lCookie, [In] bool fActive);
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object FindWindowSW(
            [In, MarshalAs(UnmanagedType.Struct)] ref object pvarloc,
            [In, MarshalAs(UnmanagedType.Struct)] ref object pvarlocRoot,
            [In] int swClass,
            out int pHWND,
            [In] int swfwOptions);
    }

    [ComImport]
    [Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IServiceProvider
    {
        [PreserveSig]
        int QueryService(
            [In] ref Guid guidService,
            [In] ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
    }

    [ComImport]
    [Guid("000214E2-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellBrowser
    {
        [PreserveSig]
        int GetWindow(out IntPtr phwnd);
        [PreserveSig]
        int ContextSensitiveHelp(bool fEnterMode);
        [PreserveSig]
        int InsertMenusSB(IntPtr hmenuShared, IntPtr lpMenuWidths);
        [PreserveSig]
        int SetMenuSB(IntPtr hmenuShared, IntPtr holemenuRes, IntPtr hwndActiveObject);
        [PreserveSig]
        int RemoveMenusSB(IntPtr hmenuShared);
        [PreserveSig]
        int SetStatusTextSB([MarshalAs(UnmanagedType.LPWStr)] string pszStatusText);
        [PreserveSig]
        int EnableModelessSB(bool fEnable);
        [PreserveSig]
        int TranslateAcceleratorSB(IntPtr pmsg, ushort wID);
        [PreserveSig]
        int BrowseObject(IntPtr pidl, uint wFlags);
        [PreserveSig]
        int GetViewStateStream(uint grfMode, out IntPtr ppStrm);
        [PreserveSig]
        int GetControlWindow(uint id, out IntPtr lphwnd);
        [PreserveSig]
        int SendControlMsg(uint id, uint uMsg, IntPtr wParam, IntPtr lParam, out IntPtr pret);
        [PreserveSig]
        int QueryActiveShellView([MarshalAs(UnmanagedType.Interface)] out IShellView ppshv);
    }

    [ComImport]
    [Guid("000214E3-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellView
    {
        [PreserveSig]
        int GetWindow(out IntPtr phwnd);
        [PreserveSig]
        int ContextSensitiveHelp(bool fEnterMode);
    }

    [ComImport]
    [Guid("CDE725B0-CCC9-4519-917E-325D72FAB4CE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFolderView
    {
        [PreserveSig]
        int GetCurrentViewMode(out uint pViewMode);
        [PreserveSig]
        int SetCurrentViewMode(uint viewMode);
        [PreserveSig]
        int GetFolder(ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
        [PreserveSig]
        int Item(int iItemIndex, out IntPtr ppidl);
        [PreserveSig]
        int ItemCount(uint uFlags, out int pcItems);
        [PreserveSig]
        int Items(uint uFlags, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
        [PreserveSig]
        int GetSelectionMarkedItem(out int piItem);
        [PreserveSig]
        int GetFocusedItem(out int piItem);
        [PreserveSig]
        int GetItemPosition(IntPtr pidl, out POINT ppt);
        [PreserveSig]
        int GetSpacing(ref POINT ppt);
        [PreserveSig]
        int GetDefaultSpacing(out POINT ppt);
        [PreserveSig]
        int GetAutoArrange();
        [PreserveSig]
        int SelectItem(int iItem, uint dwFlags);
        [PreserveSig]
        int SelectAndPositionItems(
            uint cidl,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] POINT[] apt,
            uint dwFlags);
    }

    [ComImport]
    [Guid("1AF3A467-214F-4298-908E-06B03E0B39F9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFolderView2
    {
        // IFolderView methods
        [PreserveSig]
        int GetCurrentViewMode(out uint pViewMode);
        [PreserveSig]
        int SetCurrentViewMode(uint viewMode);
        [PreserveSig]
        int GetFolder(ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
        [PreserveSig]
        int Item(int iItemIndex, out IntPtr ppidl);
        [PreserveSig]
        int ItemCount(uint uFlags, out int pcItems);
        [PreserveSig]
        int Items(uint uFlags, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
        [PreserveSig]
        int GetSelectionMarkedItem(out int piItem);
        [PreserveSig]
        int GetFocusedItem(out int piItem);
        [PreserveSig]
        int GetItemPosition(IntPtr pidl, out POINT ppt);
        [PreserveSig]
        int GetSpacing(ref POINT ppt);
        [PreserveSig]
        int GetDefaultSpacing(out POINT ppt);
        [PreserveSig]
        int GetAutoArrange();
        [PreserveSig]
        int SelectItem(int iItem, uint dwFlags);
        [PreserveSig]
        int SelectAndPositionItems(
            uint cidl,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] POINT[] apt,
            uint dwFlags);

        // IFolderView2 methods
        [PreserveSig]
        int SetGroupBy(ref PROPERTYKEY key, bool fAscending);
        [PreserveSig]
        int GetGroupBy(out PROPERTYKEY pkey, out bool pfAscending);
        [PreserveSig]
        int SetViewProperty(IntPtr pidl, ref PROPERTYKEY propkey, ref object propvar);
        [PreserveSig]
        int GetViewProperty(IntPtr pidl, ref PROPERTYKEY propkey, out object ppropvar);
        [PreserveSig]
        int SetTileViewProperties(IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszPropList);
        [PreserveSig]
        int SetExtendedTileViewProperties(IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszPropList);
        [PreserveSig]
        int SetText(int iType, [MarshalAs(UnmanagedType.LPWStr)] string pwszText);
        [PreserveSig]
        int SetCurrentFolderFlags(uint dwMask, uint dwFlags);
        [PreserveSig]
        int GetCurrentFolderFlags(out uint pdwFlags);
        [PreserveSig]
        int GetSortColumnCount(out int pcColumns);
        [PreserveSig]
        int SetSortColumns(
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] SORTCOLUMN[] rgSortColumns,
            int cColumns);
        [PreserveSig]
        int GetSortColumns(
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] SORTCOLUMN[] rgSortColumns,
            int cColumns);
    }

    [ComImport]
    [Guid("000214E6-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellFolder
    {
        [PreserveSig]
        int ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, ref uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
        [PreserveSig]
        int EnumObjects(IntPtr hwnd, uint grfFlags, out IntPtr ppenumIDList);
        [PreserveSig]
        int BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
        [PreserveSig]
        int BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
        [PreserveSig]
        int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
        [PreserveSig]
        int CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv);
        [PreserveSig]
        int GetAttributesOf(uint cidl, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, ref uint rgfInOut);
        [PreserveSig]
        int GetUIObjectOf(IntPtr hwndOwner, uint cidl, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] apidl, ref Guid riid, ref uint rgfReserved, out IntPtr ppv);
        [PreserveSig]
        int GetDisplayNameOf(IntPtr pidl, uint uFlags, out STRRET pName);
        [PreserveSig]
        int SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszName, uint uFlags, out IntPtr ppidlOut);
    }

    [ComImport]
    [Guid("93F2F68C-1D1B-11D3-A30E-00C04F79ABD1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellFolder2
    {
        // IShellFolder methods
        [PreserveSig]
        int ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, ref uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
        [PreserveSig]
        int EnumObjects(IntPtr hwnd, uint grfFlags, out IntPtr ppenumIDList);
        [PreserveSig]
        int BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
        [PreserveSig]
        int BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);
        [PreserveSig]
        int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
        [PreserveSig]
        int CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv);
        [PreserveSig]
        int GetAttributesOf(uint cidl, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] apidl, ref uint rgfInOut);
        [PreserveSig]
        int GetUIObjectOf(IntPtr hwndOwner, uint cidl, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] apidl, ref Guid riid, ref uint rgfReserved, out IntPtr ppv);
        [PreserveSig]
        int GetDisplayNameOf(IntPtr pidl, uint uFlags, out STRRET pName);
        [PreserveSig]
        int SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszName, uint uFlags, out IntPtr ppidlOut);

        // IShellFolder2 methods
        [PreserveSig]
        int GetDefaultSearchGUID(out Guid pguid);
        [PreserveSig]
        int EnumSearches(out IntPtr ppenum);
        [PreserveSig]
        int GetDefaultColumn(uint dwRes, out uint pSort, out uint pDisplay);
        [PreserveSig]
        int GetDefaultColumnState(uint iColumn, out uint pcsFlags);
        [PreserveSig]
        int GetDetailsEx(IntPtr pidl, ref PROPERTYKEY pscid, [MarshalAs(UnmanagedType.Struct)] out object pv);
        [PreserveSig]
        int GetDetailsOf(IntPtr pidl, uint iColumn, out SHELLDETAILS psd);
        [PreserveSig]
        int MapColumnToSCID(uint iColumn, out PROPERTYKEY pscid);
    }
}
