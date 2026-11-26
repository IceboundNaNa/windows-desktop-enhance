using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace WindowsDesktopEnhance.Setup
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            SetupLocalization.InitSystemLanguage();

            bool isUninstall = false;
            if (e.Args != null)
            {
                for (int i = 0; i < e.Args.Length; i++)
                {
                    if (string.Equals(e.Args[i], "--uninstall", StringComparison.OrdinalIgnoreCase))
                    {
                        isUninstall = true;
                        break;
                    }
                }
            }

            if (isUninstall)
            {
                RunUninstall();
                Shutdown();
                return;
            }

            SetupWindow win = new SetupWindow();
            MainWindow = win;
            win.Show();
        }

        private static void RunUninstall()
        {
            MessageBoxResult res = MessageBox.Show(
                SetupLocalization.T("UninstallConfirm"),
                SetupLocalization.T("UninstallTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes) return;

            try
            {
                Process[] procs = Process.GetProcessesByName("Windows Desktop Enhance");
                for (int i = 0; i < procs.Length; i++)
                {
                    try { procs[i].Kill(); } catch { }
                }

                // Remove startup registry entry
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (runKey != null)
                    {
                        if (runKey.GetValue("Windows Desktop Enhance") != null)
                            runKey.DeleteValue("Windows Desktop Enhance", false);
                        if (runKey.GetValue("DesktopRightAlign") != null)
                            runKey.DeleteValue("DesktopRightAlign", false);
                    }
                }

                // Read install location and remove uninstall key
                string installDir = null;
                const string uninstallPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\WindowsDesktopEnhance";
                using (RegistryKey uKey = Registry.CurrentUser.OpenSubKey(uninstallPath, false))
                {
                    if (uKey != null)
                    {
                        object loc = uKey.GetValue("InstallLocation");
                        if (loc != null) installDir = loc.ToString();
                    }
                }
                Registry.CurrentUser.DeleteSubKeyTree(uninstallPath, false);

                // Delete Desktop & Start Menu shortcuts
                string desktopLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Windows Desktop Enhance.lnk");
                if (File.Exists(desktopLnk)) File.Delete(desktopLnk);

                string startMenuLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Windows Desktop Enhance.lnk");
                if (File.Exists(startMenuLnk)) File.Delete(startMenuLnk);

                // Delete installed exe and schedule directory cleanup
                if (!string.IsNullOrEmpty(installDir) && Directory.Exists(installDir))
                {
                    string exePath = Path.Combine(installDir, "Windows Desktop Enhance.exe");
                    if (File.Exists(exePath)) File.Delete(exePath);

                    ProcessStartInfo psi = new ProcessStartInfo("cmd.exe",
                        string.Format("/C timeout /T 2 /NOBREAK > NUL & rmdir /S /Q \"{0}\"", installDir));
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    Process.Start(psi);
                }

                MessageBox.Show(SetupLocalization.T("UninstallDone"), SetupLocalization.T("UninstallTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, SetupLocalization.T("UninstallTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
