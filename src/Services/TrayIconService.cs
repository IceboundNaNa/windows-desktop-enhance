using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DesktopRightAlign.Models;

namespace DesktopRightAlign.Services
{
    public class TrayIconService : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ToolStripMenuItem _openItem;
        private readonly ToolStripMenuItem _arrangeItem;
        private readonly ToolStripMenuItem _saveLayoutItem;
        private readonly ToolStripMenuItem _restoreLayoutItem;
        private readonly ToolStripMenuItem _autoArrangeItem;
        private readonly ToolStripMenuItem _layoutGuardItem;
        private readonly ToolStripMenuItem _sortMenu;
        private readonly ToolStripMenuItem _sortNameItem;
        private readonly ToolStripMenuItem _sortSizeItem;
        private readonly ToolStripMenuItem _sortTypeItem;
        private readonly ToolStripMenuItem _sortDateItem;
        private readonly ToolStripMenuItem _alignMenu;
        private readonly ToolStripMenuItem _allLeftItem;
        private readonly ToolStripMenuItem _allRightItem;
        private readonly ToolStripMenuItem _sysLeftItem;
        private readonly ToolStripMenuItem _dirLeftItem;
        private readonly ToolStripMenuItem _fileLeftItem;
        private readonly ToolStripMenuItem _exitItem;
        private readonly Icon _appIcon;

        public event Action ShowWindowRequested;
        public event Action ArrangeNowRequested;
        public event Action SaveLayoutRequested;
        public event Action RestoreLayoutRequested;
        public event Action<bool> AutoArrangeToggled;
        public event Action<bool> LayoutGuardToggled;
        public event Action<DesktopSortMode> SortModeSelected;
        public event Action<DesktopAlignSide> AllAlignSideSelected;
        public event Action<bool, bool, bool> CategoryAlignChanged;
        public event Action ExitRequested;

        public TrayIconService()
        {
            _appIcon = CreateFluentTrayIcon();

            ContextMenuStrip menu = new ContextMenuStrip();

            _openItem = new ToolStripMenuItem();
            _openItem.Font = new Font(_openItem.Font, FontStyle.Bold);
            _openItem.Click += delegate { Raise(ShowWindowRequested); };

            _arrangeItem = new ToolStripMenuItem();
            _arrangeItem.Click += delegate { Raise(ArrangeNowRequested); };

            _saveLayoutItem = new ToolStripMenuItem();
            _saveLayoutItem.Click += delegate { Raise(SaveLayoutRequested); };

            _restoreLayoutItem = new ToolStripMenuItem();
            _restoreLayoutItem.Click += delegate { Raise(RestoreLayoutRequested); };

            _autoArrangeItem = new ToolStripMenuItem();
            _autoArrangeItem.CheckOnClick = true;
            _autoArrangeItem.Click += delegate
            {
                Action<bool> h = AutoArrangeToggled;
                if (h != null) h(_autoArrangeItem.Checked);
            };

            _layoutGuardItem = new ToolStripMenuItem();
            _layoutGuardItem.CheckOnClick = true;
            _layoutGuardItem.Click += delegate
            {
                Action<bool> h = LayoutGuardToggled;
                if (h != null) h(_layoutGuardItem.Checked);
            };

            _sortMenu = new ToolStripMenuItem();
            _sortNameItem = CreateSortItem(DesktopSortMode.Name);
            _sortSizeItem = CreateSortItem(DesktopSortMode.Size);
            _sortTypeItem = CreateSortItem(DesktopSortMode.ItemType);
            _sortDateItem = CreateSortItem(DesktopSortMode.DateModified);
            _sortMenu.DropDownItems.AddRange(new ToolStripItem[] { _sortNameItem, _sortSizeItem, _sortTypeItem, _sortDateItem });

            _alignMenu = new ToolStripMenuItem();
            _allLeftItem = new ToolStripMenuItem();
            _allLeftItem.Click += delegate
            {
                Action<DesktopAlignSide> h = AllAlignSideSelected;
                if (h != null) h(DesktopAlignSide.Left);
            };
            _allRightItem = new ToolStripMenuItem();
            _allRightItem.Click += delegate
            {
                Action<DesktopAlignSide> h = AllAlignSideSelected;
                if (h != null) h(DesktopAlignSide.Right);
            };
            _sysLeftItem = CreateCategoryToggleItem();
            _dirLeftItem = CreateCategoryToggleItem();
            _fileLeftItem = CreateCategoryToggleItem();
            _alignMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                _allLeftItem,
                _allRightItem,
                new ToolStripSeparator(),
                _sysLeftItem,
                _dirLeftItem,
                _fileLeftItem
            });

            _exitItem = new ToolStripMenuItem();
            _exitItem.Click += delegate { Raise(ExitRequested); };

            menu.Items.Add(_openItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_arrangeItem);
            menu.Items.Add(_sortMenu);
            menu.Items.Add(_alignMenu);
            menu.Items.Add(_autoArrangeItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_saveLayoutItem);
            menu.Items.Add(_restoreLayoutItem);
            menu.Items.Add(_layoutGuardItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_exitItem);

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = _appIcon;
            _notifyIcon.Text = "Windows Desktop Enhance";
            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += delegate { Raise(ShowWindowRequested); };

            RefreshLanguage();
        }

        private ToolStripMenuItem CreateCategoryToggleItem()
        {
            ToolStripMenuItem item = new ToolStripMenuItem();
            item.CheckOnClick = true;
            item.Click += delegate
            {
                Action<bool, bool, bool> h = CategoryAlignChanged;
                if (h != null) h(_sysLeftItem.Checked, _dirLeftItem.Checked, _fileLeftItem.Checked);
            };
            return item;
        }

        private ToolStripMenuItem CreateSortItem(DesktopSortMode mode)
        {
            ToolStripMenuItem item = new ToolStripMenuItem();
            item.Click += delegate
            {
                Action<DesktopSortMode> h = SortModeSelected;
                if (h != null) h(mode);
            };
            return item;
        }

        public void RefreshLanguage()
        {
            _openItem.Text = LocalizationService.T("TrayOpen");
            _arrangeItem.Text = LocalizationService.T("TrayArrange");
            _sortMenu.Text = LocalizationService.T("TraySort");
            _sortNameItem.Text = LocalizationService.T("SortName");
            _sortSizeItem.Text = LocalizationService.T("SortSize");
            _sortTypeItem.Text = LocalizationService.T("SortType");
            _sortDateItem.Text = LocalizationService.T("SortDate");
            _alignMenu.Text = LocalizationService.T("TrayAlignMenu");
            _allLeftItem.Text = LocalizationService.T("AlignAllLeft");
            _allRightItem.Text = LocalizationService.T("AlignAllRight");
            _sysLeftItem.Text = LocalizationService.T("TraySysLeft");
            _dirLeftItem.Text = LocalizationService.T("TrayDirLeft");
            _fileLeftItem.Text = LocalizationService.T("TrayFileLeft");
            _autoArrangeItem.Text = LocalizationService.T("TrayAuto");
            _saveLayoutItem.Text = LocalizationService.T("TraySave");
            _restoreLayoutItem.Text = LocalizationService.T("TrayRestore");
            _layoutGuardItem.Text = LocalizationService.T("TrayGuard");
            _exitItem.Text = LocalizationService.T("TrayExit");
        }

        public void SyncState(AppSettings settings)
        {
            _autoArrangeItem.Checked = settings.AutoArrangeEnabled;
            _layoutGuardItem.Checked = settings.LayoutGuardEnabled;
            _sortNameItem.Checked = settings.SortMode == DesktopSortMode.Name;
            _sortSizeItem.Checked = settings.SortMode == DesktopSortMode.Size;
            _sortTypeItem.Checked = settings.SortMode == DesktopSortMode.ItemType;
            _sortDateItem.Checked = settings.SortMode == DesktopSortMode.DateModified;

            _sysLeftItem.Checked = settings.SystemIconsAlign == DesktopAlignSide.Left;
            _dirLeftItem.Checked = settings.FoldersAlign == DesktopAlignSide.Left;
            _fileLeftItem.Checked = settings.FilesAlign == DesktopAlignSide.Left;
            _allLeftItem.Checked = _sysLeftItem.Checked && _dirLeftItem.Checked && _fileLeftItem.Checked;
            _allRightItem.Checked = !_sysLeftItem.Checked && !_dirLeftItem.Checked && !_fileLeftItem.Checked;
        }

        private static void Raise(Action action)
        {
            if (action != null) action();
        }

        private static Icon CreateFluentTrayIcon()
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (SolidBrush bg = new SolidBrush(Color.FromArgb(0x00, 0x78, 0xD7)))
                {
                    g.FillRectangle(bg, 2, 4, 28, 24);
                }

                using (Pen border = new Pen(Color.FromArgb(140, 255, 255, 255), 1.5f))
                {
                    g.DrawRectangle(border, 2.5f, 4.5f, 27, 23);
                }

                using (SolidBrush white = new SolidBrush(Color.White))
                {
                    g.FillRectangle(white, 21, 8, 5, 4);
                    g.FillRectangle(white, 21, 14, 5, 4);
                    g.FillRectangle(white, 21, 20, 5, 4);
                    g.FillRectangle(white, 14, 8, 5, 4);
                    g.FillRectangle(white, 14, 14, 5, 4);
                }

                IntPtr hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            if (_appIcon != null)
            {
                _appIcon.Dispose();
            }
        }
    }
}
