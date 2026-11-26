using System;
using System.Reflection;
using Microsoft.Win32;

namespace DesktopRightAlign.Services
{
    public static class StartupService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppValueName = "Windows Desktop Enhance";
        private const string LegacyValueName = "DesktopRightAlign";

        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null) return false;
                    object val = key.GetValue(AppValueName);
                    return val != null && !string.IsNullOrEmpty(val.ToString());
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key == null) return;
                    if (key.GetValue(LegacyValueName) != null)
                    {
                        key.DeleteValue(LegacyValueName, false);
                    }
                    if (enabled)
                    {
                        string exePath = Assembly.GetExecutingAssembly().Location;
                        key.SetValue(AppValueName, string.Format("\"{0}\" --minimized", exePath));
                    }
                    else
                    {
                        if (key.GetValue(AppValueName) != null)
                        {
                            key.DeleteValue(AppValueName, false);
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}
