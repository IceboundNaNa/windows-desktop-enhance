using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DesktopRightAlign.Models;
using DesktopRightAlign.Native;
using DesktopRightAlign.Services;

namespace DesktopRightAlign
{
    public partial class MainWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly DesktopIconManager _iconManager;
        private readonly DesktopWatcherService _watcherService;
        private readonly TrayIconService _trayService;

        private bool _isInitializing = true;
        private bool _isExiting;
        private List<DesktopIconInfo> _cachedIcons = new List<DesktopIconInfo>();
        private DesktopEnvironmentInfo _cachedEnv;

        public MainWindow(AppSettings settings, bool launchedMinimized)
        {
            InitializeComponent();

            _settings = settings;
            _iconManager = new DesktopIconManager();
            _watcherService = new DesktopWatcherService();
            _trayService = new TrayIconService();

            _watcherService.ArrangeTriggered += OnWatcherArrangeTriggered;
            _watcherService.LayoutGuardTriggered += OnWatcherLayoutGuardTriggered;

            _trayService.ShowWindowRequested += ShowAndActivate;
            _trayService.ArrangeNowRequested += delegate { Dispatcher.BeginInvoke(new Action(ExecuteArrangeNow)); };
            _trayService.SaveLayoutRequested += delegate { Dispatcher.BeginInvoke(new Action(ExecuteSaveLayout)); };
            _trayService.RestoreLayoutRequested += delegate { Dispatcher.BeginInvoke(new Action(ExecuteRestoreLayout)); };
            _trayService.AutoArrangeToggled += OnTrayAutoArrangeToggled;
            _trayService.LayoutGuardToggled += OnTrayLayoutGuardToggled;
            _trayService.SortModeSelected += OnTraySortModeSelected;
            _trayService.ExitRequested += ExecuteExit;

            SourceInitialized += OnSourceInitialized;
            Closing += OnWindowClosing;

            // Registry Run key is the single source of truth shared with the installer
            bool registryStartupEnabled = StartupService.IsEnabled();
            if (_settings.StartWithWindows != registryStartupEnabled)
            {
                _settings.StartWithWindows = registryStartupEnabled;
                _settings.Save();
            }

            LoadSettingsIntoUi();
            ApplyLocalization();
            _isInitializing = false;

            _watcherService.Configure(_settings);
            _trayService.SyncState(_settings);
            RefreshDesktopPreviewAndDiagnostics();
            UpdateSavedLayoutStatus();
            SetStatus(LocalizationService.T("Ready"));

            if (launchedMinimized || _settings.RestoreLayoutOnStartup || _settings.AutoArrangeEnabled)
            {
                DispatcherTimer startupTimer = new DispatcherTimer();
                startupTimer.Interval = TimeSpan.FromMilliseconds(launchedMinimized ? 2500 : 800);
                startupTimer.Tick += delegate
                {
                    startupTimer.Stop();
                    RunStartupPolicy();
                };
                startupTimer.Start();
            }
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            ApplyWindowDarkChrome();
        }

        private void ApplyWindowDarkChrome()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;
            bool isDark = _settings.ThemeMode == AppThemeMode.Dark ||
                          (_settings.ThemeMode == AppThemeMode.System && !Win32Interop.IsWindowsLightTheme());
            Win32Interop.ApplyImmersiveDarkMode(hwnd, isDark);
        }

        private void ApplyLocalization()
        {
            Title = LocalizationService.T("AppTitle");
            TitleBarAppName.Text = LocalizationService.T("AppTitle");
            BtnMinimize.ToolTip = LocalizationService.T("TipMinimize");
            BtnClose.ToolTip = LocalizationService.T("TipClose");

            NavOverview.Content = LocalizationService.T("NavOverview");
            NavLayout.Content = LocalizationService.T("NavLayout");
            NavSystem.Content = LocalizationService.T("NavSystem");
            LblSidebarStatusTitle.Text = LocalizationService.T("SidebarStatusLabel");

            // Page 1: Overview
            LblOverviewTitle.Text = LocalizationService.T("OverviewTitle");
            LblOverviewSub.Text = LocalizationService.T("OverviewSub");
            LblAutoArrangeTitle.Text = LocalizationService.T("AutoArrangeTitle");
            LblAutoArrangeSub.Text = LocalizationService.T("AutoArrangeSub");
            LblBtnArrangeNow.Text = LocalizationService.T("BtnArrangeNow");
            LblBtnSaveLayout.Text = LocalizationService.T("BtnSaveLayout");
            LblBtnRestoreLayout.Text = LocalizationService.T("BtnRestoreLayout");
            LblBtnDrawZone.Text = LocalizationService.T("BtnDrawZone");

            LblSortTitle.Text = LocalizationService.T("SortTitle");
            LblSortSub.Text = LocalizationService.T("SortSub");
            RadSortAsc.Content = LocalizationService.T("SortAsc");
            RadSortDesc.Content = LocalizationService.T("SortDesc");
            RadSortName.Content = LocalizationService.T("SortName");
            RadSortSize.Content = LocalizationService.T("SortSize");
            RadSortType.Content = LocalizationService.T("SortType");
            RadSortDate.Content = LocalizationService.T("SortDate");

            LblPreviewTitle.Text = LocalizationService.T("PreviewTitle");
            RadPreviewTarget.Content = LocalizationService.T("PreviewTarget");
            RadPreviewCurrent.Content = LocalizationService.T("PreviewCurrent");
            BtnRefreshPreview.Content = LocalizationService.T("BtnRefresh");

            // Page 2: Layout & Guard
            LblLayoutPageTitle.Text = LocalizationService.T("LayoutPageTitle");
            LblLayoutPageSub.Text = LocalizationService.T("LayoutPageSub");
            LblSnapshotTitle.Text = LocalizationService.T("SnapshotTitle");
            BtnSaveCurrent.Content = LocalizationService.T("BtnSaveCurrent");
            BtnRestoreSaved.Content = LocalizationService.T("BtnRestoreSaved");
            LblGuardTitle.Text = LocalizationService.T("GuardTitle");
            LblGuardSub.Text = LocalizationService.T("GuardSub");
            LblStartupRestoreTitle.Text = LocalizationService.T("StartupRestoreTitle");
            LblStartupRestoreSub.Text = LocalizationService.T("StartupRestoreSub");

            LblZoneTitle.Text = LocalizationService.T("ZoneTitle");
            LblZoneSub.Text = LocalizationService.T("ZoneSub");
            LblBtnDrawZoneFull.Text = LocalizationService.T("BtnDrawZone");
            BtnClearZones.Content = LocalizationService.T("BtnClearZones");

            LblGridTitle.Text = LocalizationService.T("GridTitle");
            LblFlowTitle.Text = LocalizationService.T("FlowTitle");
            LblFlowSub.Text = LocalizationService.T("FlowSub");
            RadFlowTop.Content = LocalizationService.T("FlowTop");
            RadFlowBottom.Content = LocalizationService.T("FlowBottom");
            LblMarginRightTitle.Text = LocalizationService.T("MarginRight");
            LblMarginTopTitle.Text = LocalizationService.T("MarginTop");
            LblMarginBottomTitle.Text = LocalizationService.T("MarginBottom");
            LblNativeSpacingTitle.Text = LocalizationService.T("NativeSpacingTitle");
            LblNativeSpacingSub.Text = LocalizationService.T("NativeSpacingSub");
            LblCustomSpaceXTitle.Text = LocalizationService.T("CustomSpaceX");
            LblCustomSpaceYTitle.Text = LocalizationService.T("CustomSpaceY");
            LblAutoDisableTitle.Text = LocalizationService.T("AutoDisableTitle");
            LblAutoDisableSub.Text = LocalizationService.T("AutoDisableSub");
            LblDebounceTitle.Text = LocalizationService.T("DebounceLabel");

            // Page 3: Settings
            LblSystemPageTitle.Text = LocalizationService.T("SystemPageTitle");
            LblSystemPageSub.Text = LocalizationService.T("SystemPageSub");
            LblAppearanceTitle.Text = LocalizationService.T("AppearanceTitle");
            LblThemeLabel.Text = LocalizationService.T("ThemeLabel");
            RadThemeSystem.Content = LocalizationService.T("ThemeSystem");
            RadThemeDark.Content = LocalizationService.T("ThemeDark");
            RadThemeLight.Content = LocalizationService.T("ThemeLight");
            LblSeedColorLabel.Text = LocalizationService.T("SeedColorLabel");
            LblPureBlackLabel.Text = LocalizationService.T("PureBlackLabel");
            LblLangLabel.Text = LocalizationService.T("LangLabel");
            RadLangAuto.Content = LocalizationService.T("LangAuto");

            LblStartupCardTitle.Text = LocalizationService.T("StartupCardTitle");
            LblStartWithWinTitle.Text = LocalizationService.T("StartWithWinTitle");
            LblStartWithWinSub.Text = LocalizationService.T("StartWithWinSub");
            LblStartMinTitle.Text = LocalizationService.T("StartMinTitle");
            LblStartMinSub.Text = LocalizationService.T("StartMinSub");
            LblCloseToTrayTitle.Text = LocalizationService.T("CloseToTrayTitle");
            LblCloseToTraySub.Text = LocalizationService.T("CloseToTraySub");
            LblDiagTitle.Text = LocalizationService.T("DiagTitle");
            BtnRefreshDiag.Content = LocalizationService.T("BtnRefresh");

            UpdateSidebarStatus();
            UpdateSavedLayoutStatus();
            RefreshExclusionZonesList();
            _trayService.RefreshLanguage();
        }

        private void RunStartupPolicy()
        {
            try
            {
                if (_settings.AutoArrangeEnabled)
                {
                    _iconManager.DisableNativeAutoArrange();
                    ExecuteArrangeNow();
                }
                else if (_settings.RestoreLayoutOnStartup && _iconManager.HasBackup())
                {
                    if (_iconManager.HasDriftedFromSavedLayout(32))
                    {
                        int restored = _iconManager.RestoreBackupLayout();
                        SetStatus(LocalizationService.F("MsgRestored", restored));
                        RefreshDesktopPreviewAndDiagnostics();
                    }
                }
            }
            catch
            {
            }
        }

        private void LoadSettingsIntoUi()
        {
            ChkAutoArrange.IsChecked = _settings.AutoArrangeEnabled;
            ChkLayoutGuard.IsChecked = _settings.LayoutGuardEnabled;
            ChkRestoreOnStartup.IsChecked = _settings.RestoreLayoutOnStartup;

            RadSortName.IsChecked = _settings.SortMode == DesktopSortMode.Name;
            RadSortSize.IsChecked = _settings.SortMode == DesktopSortMode.Size;
            RadSortType.IsChecked = _settings.SortMode == DesktopSortMode.ItemType;
            RadSortDate.IsChecked = _settings.SortMode == DesktopSortMode.DateModified;

            RadSortAsc.IsChecked = _settings.SortAscending;
            RadSortDesc.IsChecked = !_settings.SortAscending;

            RadFlowTop.IsChecked = _settings.FlowDirection == VerticalFlowDirection.TopToBottom;
            RadFlowBottom.IsChecked = _settings.FlowDirection == VerticalFlowDirection.BottomToTop;

            SldRightMargin.Value = _settings.RightMargin;
            SldTopMargin.Value = _settings.TopMargin;
            SldBottomMargin.Value = _settings.BottomMargin;
            ChkNativeSpacing.IsChecked = _settings.UseNativeGridSpacing;
            SldSpaceX.Value = _settings.CustomHorizontalSpacing;
            SldSpaceY.Value = _settings.CustomVerticalSpacing;
            ChkAutoDisableNative.IsChecked = _settings.AutoDisableNativeAutoArrange;
            SldDebounce.Value = _settings.DebounceMilliseconds;
            ChkExclusionEnabled.IsChecked = _settings.ExclusionZonesEnabled;

            ChkStartWithWindows.IsChecked = _settings.StartWithWindows;
            ChkStartMinimized.IsChecked = _settings.StartMinimizedToTray;
            ChkMinimizeOnClose.IsChecked = _settings.MinimizeToTrayOnClose;

            RadThemeSystem.IsChecked = _settings.ThemeMode == AppThemeMode.System;
            RadThemeDark.IsChecked = _settings.ThemeMode == AppThemeMode.Dark;
            RadThemeLight.IsChecked = _settings.ThemeMode == AppThemeMode.Light;
            ChkPureBlack.IsChecked = _settings.PureBlackDark;

            string seed = _settings.SeedColorHex ?? "#6750A4";
            SwatchViolet.IsChecked = seed.Equals("#6750A4", StringComparison.OrdinalIgnoreCase);
            SwatchBlue.IsChecked = seed.Equals("#0061A4", StringComparison.OrdinalIgnoreCase);
            SwatchTeal.IsChecked = seed.Equals("#006A6A", StringComparison.OrdinalIgnoreCase);
            SwatchGreen.IsChecked = seed.Equals("#386A20", StringComparison.OrdinalIgnoreCase);
            SwatchAmber.IsChecked = seed.Equals("#8B5000", StringComparison.OrdinalIgnoreCase);
            SwatchRose.IsChecked = seed.Equals("#984061", StringComparison.OrdinalIgnoreCase);
            if (SwatchViolet.IsChecked != true && SwatchBlue.IsChecked != true && SwatchTeal.IsChecked != true &&
                SwatchGreen.IsChecked != true && SwatchAmber.IsChecked != true && SwatchRose.IsChecked != true)
            {
                SwatchViolet.IsChecked = true;
            }

            string lang = _settings.LanguageCode ?? "auto";
            RadLangAuto.IsChecked = lang.Equals("auto", StringComparison.OrdinalIgnoreCase);
            RadLangZhCn.IsChecked = lang.Equals("zh-CN", StringComparison.OrdinalIgnoreCase);
            RadLangZhTw.IsChecked = lang.Equals("zh-TW", StringComparison.OrdinalIgnoreCase);
            RadLangEn.IsChecked = lang.Equals("en-US", StringComparison.OrdinalIgnoreCase);
            RadLangJa.IsChecked = lang.Equals("ja-JP", StringComparison.OrdinalIgnoreCase);
            RadLangKo.IsChecked = lang.Equals("ko-KR", StringComparison.OrdinalIgnoreCase);

            UpdateSliderLabels();
        }

        private void UpdateSliderLabels()
        {
            LblRightMargin.Text = string.Format("{0} px", (int)SldRightMargin.Value);
            LblTopMargin.Text = string.Format("{0} px", (int)SldTopMargin.Value);
            LblBottomMargin.Text = string.Format("{0} px", (int)SldBottomMargin.Value);
            LblSpaceX.Text = string.Format("{0} px", (int)SldSpaceX.Value);
            LblSpaceY.Text = string.Format("{0} px", (int)SldSpaceY.Value);
            LblDebounce.Text = string.Format("{0} ms", (int)SldDebounce.Value);

            CustomSpacingPanel.IsEnabled = ChkNativeSpacing.IsChecked != true;
            CustomSpacingPanel.Opacity = CustomSpacingPanel.IsEnabled ? 1.0 : 0.45;
        }

        private void UpdateSidebarStatus()
        {
            if (_settings.AutoArrangeEnabled)
            {
                SidebarStatusText.Text = LocalizationService.T("StatusAutoTakeover");
            }
            else if (_settings.LayoutGuardEnabled)
            {
                SidebarStatusText.Text = LocalizationService.T("StatusGuardOnly");
            }
            else
            {
                SidebarStatusText.Text = LocalizationService.T("StatusManual");
            }
        }

        private void UpdateSavedLayoutStatus()
        {
            DesktopLayoutBackup meta = _iconManager.GetSavedLayoutMetadata();
            if (meta == null || meta.Icons == null || meta.Icons.Count == 0)
            {
                SidebarBackupText.Text = LocalizationService.T("SidebarNoBackup");
                SavedLayoutDetailText.Text = LocalizationService.T("SnapshotNone");
                BtnRestoreLayout.IsEnabled = false;
                BtnRestoreSaved.IsEnabled = false;
                return;
            }

            string timeStr = meta.SavedAtUtc;
            DateTime dt;
            if (DateTime.TryParse(meta.SavedAtUtc, out dt))
            {
                timeStr = dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            }

            string resStr = (meta.WorkAreaWidth > 0 && meta.WorkAreaHeight > 0)
                ? string.Format(" · {0}×{1}", meta.WorkAreaWidth, meta.WorkAreaHeight)
                : string.Empty;

            SidebarBackupText.Text = LocalizationService.F("SidebarBackupCount", meta.Icons.Count);
            SavedLayoutDetailText.Text = LocalizationService.F("SnapshotInfo", meta.Icons.Count, timeStr, resStr);
            BtnRestoreLayout.IsEnabled = true;
            BtnRestoreSaved.IsEnabled = true;
        }

        private void RefreshDesktopPreviewAndDiagnostics()
        {
            try
            {
                DesktopEnvironmentInfo env;
                _cachedIcons = _iconManager.GetDesktopIconsWithPreview(_settings, out env);
                _cachedEnv = env;

                PreviewMetaText.Text = LocalizationService.F(
                    "PreviewMeta",
                    env.IconCount,
                    env.WorkAreaWidth,
                    env.WorkAreaHeight,
                    GetSortModeLabel(_settings.SortMode),
                    _settings.SortAscending ? LocalizationService.T("SortAsc") : LocalizationService.T("SortDesc"));

                DiagnosticsTextBox.Text = string.Format(
                    "SHELLDLL_DefView HWND : 0x{0:X}\r\n" +
                    "SysListView32 HWND    : 0x{1:X}\r\n" +
                    "Primary WorkArea      : ({2}, {3}) - {4} x {5}\r\n" +
                    "IFolderView Spacing   : {6} x {7} px\r\n" +
                    "FWF_AUTOARRANGE       : {8}\r\n" +
                    "FWF_SNAPTOGRID        : {9}\r\n" +
                    "Desktop Icon Count    : {10}\r\n" +
                    "Settings Path         : {11}",
                    env.DefViewHwnd.ToInt64(),
                    env.ListViewHwnd.ToInt64(),
                    env.WorkAreaLeft,
                    env.WorkAreaTop,
                    env.WorkAreaWidth,
                    env.WorkAreaHeight,
                    env.SpacingX,
                    env.SpacingY,
                    env.NativeAutoArrange ? "Enabled" : "Disabled",
                    env.NativeSnapToGrid ? "Enabled" : "Disabled",
                    env.IconCount,
                    AppSettings.SettingsFilePath);

                RenderPreviewCanvas();
            }
            catch (Exception ex)
            {
                PreviewMetaText.Text = ex.Message;
                DiagnosticsTextBox.Text = ex.ToString();
            }
        }

        private void RenderPreviewCanvas()
        {
            DesktopPreviewCanvas.Children.Clear();
            if (_cachedEnv == null || _cachedIcons == null || _cachedEnv.WorkAreaWidth <= 0 || _cachedEnv.WorkAreaHeight <= 0)
            {
                return;
            }

            double canvasW = DesktopPreviewCanvas.ActualWidth;
            double canvasH = DesktopPreviewCanvas.ActualHeight;
            if (canvasW < 40 || canvasH < 40) return;

            double pad = 10;
            double availW = canvasW - pad * 2;
            double availH = canvasH - pad * 2;
            double scale = Math.Min(availW / _cachedEnv.WorkAreaWidth, availH / _cachedEnv.WorkAreaHeight);

            double screenW = _cachedEnv.WorkAreaWidth * scale;
            double screenH = _cachedEnv.WorkAreaHeight * scale;
            double offsetX = (canvasW - screenW) / 2.0;
            double offsetY = (canvasH - screenH) / 2.0;

            Border screenFrame = new Border
            {
                Width = screenW,
                Height = screenH,
                CornerRadius = new CornerRadius(3),
                BorderBrush = (Brush)FindResource("BorderBrushStrong"),
                BorderThickness = new Thickness(1),
                Background = (Brush)FindResource("WindowBgBrush")
            };
            Canvas.SetLeft(screenFrame, offsetX);
            Canvas.SetTop(screenFrame, offsetY);
            DesktopPreviewCanvas.Children.Add(screenFrame);

            // Render Exclusion Zones
            if (_settings.ExclusionZonesEnabled && _settings.ExclusionZones != null)
            {
                for (int z = 0; z < _settings.ExclusionZones.Count; z++)
                {
                    ExclusionZone zone = _settings.ExclusionZones[z];
                    if (zone == null || !zone.Enabled || zone.Width <= 0 || zone.Height <= 0) continue;

                    double zx = offsetX + (zone.Left - _cachedEnv.WorkAreaLeft) * scale;
                    double zy = offsetY + (zone.Top - _cachedEnv.WorkAreaTop) * scale;
                    double zw = Math.Max(6, zone.Width * scale);
                    double zh = Math.Max(6, zone.Height * scale);

                    Border zoneBox = new Border
                    {
                        Width = zw,
                        Height = zh,
                        Background = new SolidColorBrush(Color.FromArgb(55, 232, 17, 35)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(190, 255, 95, 95)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(2),
                        ToolTip = string.Format("{0}\n({1}, {2}) · {3}×{4} px", zone.Name, zone.Left, zone.Top, zone.Width, zone.Height),
                        Child = zw >= 44 && zh >= 18
                            ? new TextBlock
                            {
                                Text = zone.Name,
                                FontSize = 9.5,
                                Foreground = Brushes.White,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                            : null
                    };
                    Canvas.SetLeft(zoneBox, zx);
                    Canvas.SetTop(zoneBox, zy);
                    DesktopPreviewCanvas.Children.Add(zoneBox);
                }
            }

            bool showTarget = RadPreviewTarget.IsChecked == true;
            SolidColorBrush accentSolid = (SolidColorBrush)FindResource("AccentBrush");
            Color accentColor = accentSolid.Color;
            Brush folderTileBrush = new SolidColorBrush(Color.FromArgb(195, accentColor.R, accentColor.G, accentColor.B));
            Brush fileTileBrush = new SolidColorBrush(Color.FromArgb(85, accentColor.R, accentColor.G, accentColor.B));
            Brush tileBorderBrush = new SolidColorBrush(Color.FromArgb(110, accentColor.R, accentColor.G, accentColor.B));

            double stepX = (_settings.UseNativeGridSpacing ? _cachedEnv.SpacingX : _settings.CustomHorizontalSpacing) * scale;
            double stepY = (_settings.UseNativeGridSpacing ? _cachedEnv.SpacingY : _settings.CustomVerticalSpacing) * scale;
            double tileW = Math.Max(6, stepX * 0.72);
            double tileH = Math.Max(6, stepY * 0.72);

            for (int i = 0; i < _cachedIcons.Count; i++)
            {
                DesktopIconInfo icon = _cachedIcons[i];
                int rawX = showTarget ? icon.TargetX : icon.CurrentX;
                int rawY = showTarget ? icon.TargetY : icon.CurrentY;

                double px = offsetX + (rawX - _cachedEnv.WorkAreaLeft) * scale;
                double py = offsetY + (rawY - _cachedEnv.WorkAreaTop) * scale;

                px = Math.Max(offsetX + 2, Math.Min(offsetX + screenW - tileW - 2, px));
                py = Math.Max(offsetY + 2, Math.Min(offsetY + screenH - tileH - 2, py));

                Border tile = new Border
                {
                    Width = tileW,
                    Height = tileH,
                    CornerRadius = new CornerRadius(1.5),
                    Background = icon.IsFolder ? folderTileBrush : fileTileBrush,
                    BorderBrush = tileBorderBrush,
                    BorderThickness = new Thickness(0.6),
                    ToolTip = string.Format("{0}\n({1}, {2}) → ({3}, {4})",
                        icon.DisplayName, icon.CurrentX, icon.CurrentY, icon.TargetX, icon.TargetY)
                };

                Canvas.SetLeft(tile, px);
                Canvas.SetTop(tile, py);
                DesktopPreviewCanvas.Children.Add(tile);
            }
        }

        private void ExecuteArrangeNow()
        {
            try
            {
                DesktopEnvironmentInfo env;
                int count = _iconManager.ArrangeRight(_settings, out env);
                SetStatus(LocalizationService.F(
                    "MsgArranged",
                    GetSortModeLabel(_settings.SortMode),
                    _settings.SortAscending ? LocalizationService.T("SortAsc") : LocalizationService.T("SortDesc"),
                    count));
                RefreshDesktopPreviewAndDiagnostics();
                UpdateSavedLayoutStatus();
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message);
            }
        }

        private void ExecuteSaveLayout()
        {
            try
            {
                int count = _iconManager.BackupCurrentLayout();
                UpdateSavedLayoutStatus();
                SetStatus(LocalizationService.F("MsgSaved", count));
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message);
            }
        }

        private void ExecuteRestoreLayout()
        {
            try
            {
                int count = _iconManager.RestoreBackupLayout();
                if (count > 0)
                {
                    SetStatus(LocalizationService.F("MsgRestored", count));
                    RefreshDesktopPreviewAndDiagnostics();
                }
                else
                {
                    SetStatus(LocalizationService.T("MsgNoBackup"));
                }
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message);
            }
        }

        private void OnWatcherArrangeTriggered(string reason)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                try
                {
                    DesktopEnvironmentInfo env;
                    int count = _iconManager.ArrangeRight(_settings, out env);
                    SetStatus(LocalizationService.F(
                        "MsgArranged",
                        GetSortModeLabel(_settings.SortMode),
                        _settings.SortAscending ? LocalizationService.T("SortAsc") : LocalizationService.T("SortDesc"),
                        count));
                    if (IsVisible) RefreshDesktopPreviewAndDiagnostics();
                }
                catch
                {
                }
            }));
        }

        private void OnWatcherLayoutGuardTriggered(string reason)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                try
                {
                    int restored = _iconManager.RestoreBackupLayout();
                    if (restored > 0)
                    {
                        SetStatus(LocalizationService.F("MsgRestored", restored));
                        if (IsVisible) RefreshDesktopPreviewAndDiagnostics();
                    }
                }
                catch
                {
                }
            }));
        }

        private void OnTrayAutoArrangeToggled(bool enabled)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                _settings.AutoArrangeEnabled = enabled;
                _settings.Save();
                _isInitializing = true;
                ChkAutoArrange.IsChecked = enabled;
                _isInitializing = false;
                _watcherService.Configure(_settings);
                UpdateSidebarStatus();
                if (enabled) ExecuteArrangeNow();
            }));
        }

        private void OnTrayLayoutGuardToggled(bool enabled)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                _settings.LayoutGuardEnabled = enabled;
                _settings.Save();
                _isInitializing = true;
                ChkLayoutGuard.IsChecked = enabled;
                _isInitializing = false;
                _watcherService.Configure(_settings);
                UpdateSidebarStatus();
            }));
        }

        private void OnTraySortModeSelected(DesktopSortMode mode)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                _settings.SortMode = mode;
                _settings.Save();
                _isInitializing = true;
                RadSortName.IsChecked = mode == DesktopSortMode.Name;
                RadSortSize.IsChecked = mode == DesktopSortMode.Size;
                RadSortType.IsChecked = mode == DesktopSortMode.ItemType;
                RadSortDate.IsChecked = mode == DesktopSortMode.DateModified;
                _isInitializing = false;
                _trayService.SyncState(_settings);
                ExecuteArrangeNow();
            }));
        }

        private void ShowAndActivate()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                Show();
                if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
                Activate();
                RefreshDesktopPreviewAndDiagnostics();
                UpdateSavedLayoutStatus();
            }));
        }

        private void ExecuteExit()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                _isExiting = true;
                _watcherService.Dispose();
                _trayService.Dispose();
                Application.Current.Shutdown();
            }));
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExiting && _settings.MinimizeToTrayOnClose)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            _watcherService.Dispose();
            _trayService.Dispose();
            Application.Current.Shutdown();
        }

        private void SetStatus(string text)
        {
            StatusBarText.Text = text;
            StatusTimestampText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private static string GetSortModeLabel(DesktopSortMode mode)
        {
            switch (mode)
            {
                case DesktopSortMode.Size: return LocalizationService.T("SortSize");
                case DesktopSortMode.ItemType: return LocalizationService.T("SortType");
                case DesktopSortMode.DateModified: return LocalizationService.T("SortDate");
                case DesktopSortMode.Name:
                default: return LocalizationService.T("SortName");
            }
        }

        // UI Event Handlers
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }
        private void BtnClose_Click(object sender, RoutedEventArgs e) { Close(); }

        private void WindowContentGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WindowContentGrid.ActualWidth > 0 && WindowContentGrid.ActualHeight > 0)
            {
                WindowContentGrid.Clip = new RectangleGeometry(
                    new Rect(0, 0, WindowContentGrid.ActualWidth, WindowContentGrid.ActualHeight),
                    15,
                    15);
            }
        }

        private void NavItem_Checked(object sender, RoutedEventArgs e)
        {
            if (PageOverview == null || PageLayout == null || PageSystem == null) return;
            PageOverview.Visibility = NavOverview.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            PageLayout.Visibility = NavLayout.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            PageSystem.Visibility = NavSystem.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            if (NavSystem.IsChecked == true)
            {
                bool regEnabled = StartupService.IsEnabled();
                if (_settings.StartWithWindows != regEnabled || ChkStartWithWindows.IsChecked != regEnabled)
                {
                    _settings.StartWithWindows = regEnabled;
                    _isInitializing = true;
                    ChkStartWithWindows.IsChecked = regEnabled;
                    _isInitializing = false;
                }
            }
        }

        private void ChkAutoArrange_Click(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settings.AutoArrangeEnabled = ChkAutoArrange.IsChecked == true;
            _settings.Save();
            _watcherService.Configure(_settings);
            _trayService.SyncState(_settings);
            UpdateSidebarStatus();

            if (_settings.AutoArrangeEnabled)
            {
                bool tookOver = _iconManager.DisableNativeAutoArrange();
                ExecuteArrangeNow();
                if (tookOver)
                {
                    SetStatus(LocalizationService.F("MsgTakeover", GetSortModeLabel(_settings.SortMode)));
                }
            }
            else
            {
                SetStatus(LocalizationService.T("MsgPaused"));
            }
        }

        private void BtnArrangeNow_Click(object sender, RoutedEventArgs e) { ExecuteArrangeNow(); }
        private void BtnSaveLayout_Click(object sender, RoutedEventArgs e) { ExecuteSaveLayout(); }
        private void BtnRestoreLayout_Click(object sender, RoutedEventArgs e) { ExecuteRestoreLayout(); }

        private void SortSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            if (RadSortSize.IsChecked == true) _settings.SortMode = DesktopSortMode.Size;
            else if (RadSortType.IsChecked == true) _settings.SortMode = DesktopSortMode.ItemType;
            else if (RadSortDate.IsChecked == true) _settings.SortMode = DesktopSortMode.DateModified;
            else _settings.SortMode = DesktopSortMode.Name;

            _settings.SortAscending = RadSortAsc.IsChecked == true;
            _settings.Save();
            _trayService.SyncState(_settings);

            if (_settings.AutoArrangeEnabled) ExecuteArrangeNow();
            else RefreshDesktopPreviewAndDiagnostics();
        }

        private void PreviewMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            RenderPreviewCanvas();
        }

        private void BtnRefreshPreview_Click(object sender, RoutedEventArgs e)
        {
            RefreshDesktopPreviewAndDiagnostics();
            UpdateSavedLayoutStatus();
            SetStatus(LocalizationService.T("MsgRefreshed"));
        }

        private void DesktopPreviewCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RenderPreviewCanvas();
        }

        private void LayoutSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            _settings.LayoutGuardEnabled = ChkLayoutGuard.IsChecked == true;
            _settings.RestoreLayoutOnStartup = ChkRestoreOnStartup.IsChecked == true;
            _settings.FlowDirection = RadFlowBottom.IsChecked == true
                ? VerticalFlowDirection.BottomToTop
                : VerticalFlowDirection.TopToBottom;
            _settings.UseNativeGridSpacing = ChkNativeSpacing.IsChecked == true;
            _settings.AutoDisableNativeAutoArrange = ChkAutoDisableNative.IsChecked == true;
            _settings.ExclusionZonesEnabled = ChkExclusionEnabled.IsChecked == true;

            _settings.Save();
            _watcherService.Configure(_settings);
            _trayService.SyncState(_settings);
            UpdateSliderLabels();
            UpdateSidebarStatus();
            RefreshExclusionZonesList();
            if (_settings.AutoArrangeEnabled) ExecuteArrangeNow();
            else RefreshDesktopPreviewAndDiagnostics();
        }

        private void RefreshExclusionZonesList()
        {
            if (ExclusionZonesListPanel == null) return;
            ExclusionZonesListPanel.Children.Clear();

            List<ExclusionZone> zones = _settings.ExclusionZones;
            BtnClearZones.IsEnabled = zones != null && zones.Count > 0;

            if (zones == null || zones.Count == 0)
            {
                ExclusionZonesListPanel.Children.Add(new TextBlock
                {
                    Text = LocalizationService.T("ZoneEmpty"),
                    FontSize = 12,
                    Foreground = (Brush)FindResource("TextSecondaryBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 2)
                });
                return;
            }

            for (int i = 0; i < zones.Count; i++)
            {
                ExclusionZone zone = zones[i];
                Border row = new Border
                {
                    Background = (Brush)FindResource("ControlBgBrush"),
                    BorderBrush = (Brush)FindResource("BorderBrushSubtle"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                StackPanel info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
                info.Children.Add(new TextBlock { Text = zone.Name, FontSize = 13, FontWeight = FontWeights.SemiBold });
                info.Children.Add(new TextBlock
                {
                    Text = LocalizationService.F("ZoneCoords", zone.Left, zone.Top, zone.Width, zone.Height),
                    FontSize = 11.5,
                    Foreground = (Brush)FindResource("TextSecondaryBrush"),
                    Margin = new Thickness(0, 2, 0, 0)
                });
                Grid.SetColumn(info, 0);

                CheckBox toggle = new CheckBox
                {
                    Style = (Style)FindResource("Win10ToggleSwitchStyle"),
                    IsChecked = zone.Enabled,
                    VerticalAlignment = VerticalAlignment.Center
                };
                toggle.Click += delegate
                {
                    zone.Enabled = toggle.IsChecked == true;
                    _settings.Save();
                    if (_settings.AutoArrangeEnabled) ExecuteArrangeNow();
                    else RefreshDesktopPreviewAndDiagnostics();
                };
                Grid.SetColumn(toggle, 1);

                string zoneId = zone.Id;

                Button editBtn = new Button
                {
                    Content = LocalizationService.T("BtnEdit"),
                    Style = (Style)FindResource("FluentButtonStyle"),
                    Padding = new Thickness(10, 4, 10, 4),
                    FontSize = 12,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                editBtn.Click += delegate { EditExclusionZone(zoneId); };
                Grid.SetColumn(editBtn, 2);

                Button delBtn = new Button
                {
                    Content = LocalizationService.T("BtnDelete"),
                    Style = (Style)FindResource("FluentButtonStyle"),
                    Padding = new Thickness(10, 4, 10, 4),
                    FontSize = 12,
                    Margin = new Thickness(6, 0, 0, 0)
                };
                delBtn.Click += delegate { RemoveExclusionZone(zoneId); };
                Grid.SetColumn(delBtn, 3);

                grid.Children.Add(info);
                grid.Children.Add(toggle);
                grid.Children.Add(editBtn);
                grid.Children.Add(delBtn);
                row.Child = grid;
                ExclusionZonesListPanel.Children.Add(row);
            }
        }

        private void RemoveExclusionZone(string zoneId)
        {
            if (_settings.ExclusionZones == null) return;
            for (int i = _settings.ExclusionZones.Count - 1; i >= 0; i--)
            {
                if (_settings.ExclusionZones[i].Id == zoneId) _settings.ExclusionZones.RemoveAt(i);
            }
            _settings.Save();
            RefreshExclusionZonesList();
            if (_settings.AutoArrangeEnabled) ExecuteArrangeNow();
            else RefreshDesktopPreviewAndDiagnostics();
            SetStatus(LocalizationService.T("MsgZoneDeleted"));
        }

        private void EditExclusionZone(string zoneId)
        {
            if (_settings.ExclusionZones == null) return;
            ExclusionZone target = null;
            for (int i = 0; i < _settings.ExclusionZones.Count; i++)
            {
                if (_settings.ExclusionZones[i].Id == zoneId)
                {
                    target = _settings.ExclusionZones[i];
                    break;
                }
            }
            if (target == null) return;

            DesktopEnvironmentInfo env = _cachedEnv;
            if (env == null)
            {
                try { env = _iconManager.GetEnvironmentInfo(); } catch { return; }
            }

            Hide();
            try
            {
                ZoneSelectorWindow selector = new ZoneSelectorWindow(env, _settings.ExclusionZones, target);
                bool? ok = selector.ShowDialog();
                if (ok == true && selector.SelectedZone != null)
                {
                    target.Left = selector.SelectedZone.Left;
                    target.Top = selector.SelectedZone.Top;
                    target.Width = selector.SelectedZone.Width;
                    target.Height = selector.SelectedZone.Height;
                    _settings.Save();

                    RefreshExclusionZonesList();
                    if (_settings.AutoArrangeEnabled) ExecuteArrangeNow();
                    else RefreshDesktopPreviewAndDiagnostics();

                    SetStatus(LocalizationService.F("MsgZoneUpdated", target.Name, target.Width, target.Height));
                }
            }
            finally
            {
                Show();
                Activate();
            }
        }

        private void BtnDrawExclusionZone_Click(object sender, RoutedEventArgs e)
        {
            DesktopEnvironmentInfo env = _cachedEnv;
            if (env == null)
            {
                try { env = _iconManager.GetEnvironmentInfo(); } catch { return; }
            }

            Hide();
            try
            {
                ZoneSelectorWindow selector = new ZoneSelectorWindow(env, _settings.ExclusionZones, null);
                bool? ok = selector.ShowDialog();
                if (ok == true && selector.SelectedZone != null)
                {
                    if (_settings.ExclusionZones == null) _settings.ExclusionZones = new List<ExclusionZone>();
                    _settings.ExclusionZones.Add(selector.SelectedZone);
                    _settings.ExclusionZonesEnabled = true;
                    _settings.Save();

                    _isInitializing = true;
                    ChkExclusionEnabled.IsChecked = true;
                    _isInitializing = false;

                    RefreshExclusionZonesList();
                    if (_settings.AutoArrangeEnabled) ExecuteArrangeNow();
                    else RefreshDesktopPreviewAndDiagnostics();

                    SetStatus(LocalizationService.F("MsgZoneAdded", selector.SelectedZone.Name, selector.SelectedZone.Width, selector.SelectedZone.Height));
                }
            }
            finally
            {
                Show();
                Activate();
            }
        }

        private void BtnClearZones_Click(object sender, RoutedEventArgs e)
        {
            if (_settings.ExclusionZones != null) _settings.ExclusionZones.Clear();
            _settings.Save();
            RefreshExclusionZonesList();
            if (_settings.AutoArrangeEnabled) ExecuteArrangeNow();
            else RefreshDesktopPreviewAndDiagnostics();
            SetStatus(LocalizationService.T("MsgZonesCleared"));
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;

            _settings.RightMargin = (int)SldRightMargin.Value;
            _settings.TopMargin = (int)SldTopMargin.Value;
            _settings.BottomMargin = (int)SldBottomMargin.Value;
            _settings.CustomHorizontalSpacing = (int)SldSpaceX.Value;
            _settings.CustomVerticalSpacing = (int)SldSpaceY.Value;
            _settings.DebounceMilliseconds = (int)SldDebounce.Value;

            _settings.Save();
            _watcherService.Configure(_settings);
            UpdateSliderLabels();
            RefreshDesktopPreviewAndDiagnostics();
        }

        private void SystemSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            _settings.StartWithWindows = ChkStartWithWindows.IsChecked == true;
            _settings.StartMinimizedToTray = ChkStartMinimized.IsChecked == true;
            _settings.MinimizeToTrayOnClose = ChkMinimizeOnClose.IsChecked == true;
            _settings.Save();

            StartupService.SetEnabled(_settings.StartWithWindows);
            SetStatus(_settings.StartWithWindows ? LocalizationService.T("MsgStartupOn") : LocalizationService.T("MsgSettingsSaved"));
        }

        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            if (RadThemeDark.IsChecked == true) _settings.ThemeMode = AppThemeMode.Dark;
            else if (RadThemeLight.IsChecked == true) _settings.ThemeMode = AppThemeMode.Light;
            else _settings.ThemeMode = AppThemeMode.System;

            _settings.Save();
            App app = Application.Current as App;
            if (app != null) app.ApplyTheme(_settings);
            ApplyWindowDarkChrome();
            RenderPreviewCanvas();
        }

        private void SeedColor_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            RadioButton rb = sender as RadioButton;
            if (rb == null || rb.Tag == null) return;

            _settings.SeedColorHex = rb.Tag.ToString();
            _settings.Save();
            App app = Application.Current as App;
            if (app != null) app.ApplyTheme(_settings);
            RenderPreviewCanvas();
        }

        private void PureBlack_Click(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settings.PureBlackDark = ChkPureBlack.IsChecked == true;
            _settings.Save();
            App app = Application.Current as App;
            if (app != null) app.ApplyTheme(_settings);
            ApplyWindowDarkChrome();
            RenderPreviewCanvas();
        }

        private void LanguageRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            RadioButton rb = sender as RadioButton;
            if (rb == null || rb.Tag == null) return;

            _settings.LanguageCode = rb.Tag.ToString();
            _settings.Save();
            LocalizationService.SetLanguage(_settings.LanguageCode);
            ApplyLocalization();
            RefreshDesktopPreviewAndDiagnostics();
            SetStatus(LocalizationService.T("MsgSettingsSaved"));
        }
    }
}
