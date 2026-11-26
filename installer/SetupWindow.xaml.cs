using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace WindowsDesktopEnhance.Setup
{
    public partial class SetupWindow : Window
    {
        private bool _isInitializing = true;
        private bool _installed;
        private string _installedExePath;

        public SetupWindow()
        {
            InitializeComponent();

            ApplySystemLightOrDarkTheme();

            string defaultDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "Windows Desktop Enhance");
            TxtInstallDir.Text = defaultDir;

            SelectCurrentLanguageRadio();
            ApplyLocalization();
            _isInitializing = false;
        }

        private void ApplySystemLightOrDarkTheme()
        {
            bool useLight = false;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object v = key.GetValue("AppsUseLightTheme");
                        if (v is int && ((int)v) != 0) useLight = true;
                    }
                }
            }
            catch
            {
            }

            if (useLight)
            {
                Resources["BgBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFB, 0xFE));
                Resources["CardBrush"] = new SolidColorBrush(Color.FromRgb(0xF3, 0xED, 0xF7));
                Resources["ControlBrush"] = new SolidColorBrush(Color.FromRgb(0xE8, 0xE2, 0xEC));
                Resources["BorderBrushSubtle"] = new SolidColorBrush(Color.FromRgb(0xCA, 0xC4, 0xD0));
                Resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(0x1D, 0x1B, 0x20));
                Resources["TextSecondary"] = new SolidColorBrush(Color.FromRgb(0x49, 0x45, 0x4F));
                Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0x67, 0x50, 0xA4));
                Resources["OnAccentBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                Resources["SecondaryContainer"] = new SolidColorBrush(Color.FromRgb(0xE8, 0xDE, 0xF8));
                Resources["OnSecondaryContainer"] = new SolidColorBrush(Color.FromRgb(0x1D, 0x19, 0x2B));
            }
        }

        private void SelectCurrentLanguageRadio()
        {
            string lang = SetupLocalization.CurrentLanguage;
            LangZhCn.IsChecked = lang == "zh-CN";
            LangZhTw.IsChecked = lang == "zh-TW";
            LangEn.IsChecked = lang == "en-US";
            LangJa.IsChecked = lang == "ja-JP";
            LangKo.IsChecked = lang == "ko-KR";
        }

        private void ApplyLocalization()
        {
            Title = SetupLocalization.T("Title");
            LblTitle.Text = SetupLocalization.T("Title");
            LblSubtitle.Text = SetupLocalization.T("Subtitle");
            LblPathLabel.Text = SetupLocalization.T("PathLabel");
            BtnBrowse.Content = SetupLocalization.T("Browse");
            LblOptionsLabel.Text = SetupLocalization.T("OptionsLabel");
            ChkDesktopShortcut.Content = SetupLocalization.T("OptDesktop");
            ChkAutoStart.Content = SetupLocalization.T("OptStartup");
            ChkLaunchAfter.Content = SetupLocalization.T("OptLaunch");
            BtnCancel.Content = SetupLocalization.T("BtnCancel");
            BtnInstall.Content = _installed ? SetupLocalization.T("BtnFinish") : SetupLocalization.T("BtnInstall");
            LblStatus.Text = _installed ? SetupLocalization.T("StatusDone") : SetupLocalization.T("StatusReady");
        }

        private void RootClipGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RootClipGrid.ActualWidth > 0 && RootClipGrid.ActualHeight > 0)
            {
                RootClipGrid.Clip = new RectangleGeometry(
                    new Rect(0, 0, RootClipGrid.ActualWidth, RootClipGrid.ActualHeight),
                    15,
                    15);
            }
        }

        private void Lang_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            RadioButton rb = sender as RadioButton;
            if (rb == null || rb.Tag == null) return;
            SetupLocalization.SetLanguage(rb.Tag.ToString());
            ApplyLocalization();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                dlg.SelectedPath = TxtInstallDir.Text;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedPath))
                {
                    TxtInstallDir.Text = dlg.SelectedPath;
                }
            }
        }

        private void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (_installed)
            {
                if (ChkLaunchAfter.IsChecked == true && !string.IsNullOrEmpty(_installedExePath) && File.Exists(_installedExePath))
                {
                    try { Process.Start(_installedExePath); } catch { }
                }
                Close();
                return;
            }

            string installDir = (TxtInstallDir.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(installDir)) return;

            BtnInstall.IsEnabled = false;
            BtnInstall.Content = SetupLocalization.T("BtnInstalling");

            try
            {
                // Stop any running instance first
                Process[] running = Process.GetProcessesByName("Windows Desktop Enhance");
                for (int i = 0; i < running.Length; i++)
                {
                    try { running[i].Kill(); running[i].WaitForExit(1500); } catch { }
                }

                Directory.CreateDirectory(installDir);
                _installedExePath = Path.Combine(installDir, "Windows Desktop Enhance.exe");

                // Extract embedded application executable
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream payload = asm.GetManifestResourceStream("Payload.AppExe"))
                {
                    if (payload == null)
                    {
                        throw new InvalidOperationException("Embedded application payload missing.");
                    }
                    using (FileStream fs = new FileStream(_installedExePath, FileMode.Create, FileAccess.Write))
                    {
                        payload.CopyTo(fs);
                    }
                }

                // Copy self as uninstaller inside installDir
                string uninstallerPath = Path.Combine(installDir, "Uninstall.exe");
                try
                {
                    File.Copy(asm.Location, uninstallerPath, true);
                }
                catch
                {
                }

                // Start Menu Shortcut
                string startMenuLnk = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                    "Windows Desktop Enhance.lnk");
                CreateShortcut(startMenuLnk, _installedExePath, installDir);

                // Optional Desktop Shortcut
                if (ChkDesktopShortcut.IsChecked == true)
                {
                    string desktopLnk = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                        "Windows Desktop Enhance.lnk");
                    CreateShortcut(desktopLnk, _installedExePath, installDir);
                }

                // Sync Start with Windows registry entry (single source of truth shared with app)
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (runKey != null)
                    {
                        if (runKey.GetValue("DesktopRightAlign") != null)
                        {
                            runKey.DeleteValue("DesktopRightAlign", false);
                        }

                        if (ChkAutoStart.IsChecked == true)
                        {
                            runKey.SetValue("Windows Desktop Enhance", string.Format("\"{0}\" --minimized", _installedExePath));
                        }
                        else if (runKey.GetValue("Windows Desktop Enhance") != null)
                        {
                            runKey.DeleteValue("Windows Desktop Enhance", false);
                        }
                    }
                }

                // Register in Windows Installed Apps (Uninstall)
                using (RegistryKey uKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\WindowsDesktopEnhance"))
                {
                    if (uKey != null)
                    {
                        Version asmVer = asm.GetName().Version;
                        string displayVer = asmVer != null ? asmVer.ToString(3) : "1.1.0";
                        uKey.SetValue("DisplayName", "Windows Desktop Enhance");
                        uKey.SetValue("DisplayVersion", displayVer);
                        uKey.SetValue("Publisher", "Windows Desktop Enhance");
                        uKey.SetValue("InstallLocation", installDir);
                        uKey.SetValue("DisplayIcon", _installedExePath);
                        uKey.SetValue("UninstallString", string.Format("\"{0}\" --uninstall", uninstallerPath));
                        uKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        uKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    }
                }

                _installed = true;
                TxtInstallDir.IsEnabled = false;
                BtnBrowse.IsEnabled = false;
                ChkDesktopShortcut.IsEnabled = false;
                ChkAutoStart.IsEnabled = false;
                BtnCancel.Visibility = Visibility.Collapsed;
                BtnInstall.Content = SetupLocalization.T("BtnFinish");
                BtnInstall.IsEnabled = true;
                LblStatus.Text = SetupLocalization.T("StatusDone");
            }
            catch (Exception ex)
            {
                BtnInstall.IsEnabled = true;
                BtnInstall.Content = SetupLocalization.T("BtnInstall");
                LblStatus.Text = SetupLocalization.T("StatusErr") + ex.Message;
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetExePath, string workingDir)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;
            object shell = Activator.CreateInstance(shellType);
            object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
            Type scType = shortcut.GetType();
            scType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetExePath });
            scType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDir });
            scType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { "Windows Desktop Enhance" });
            scType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
