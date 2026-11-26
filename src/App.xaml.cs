using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using DesktopRightAlign.Models;
using DesktopRightAlign.Native;
using DesktopRightAlign.Services;

namespace DesktopRightAlign
{
    public partial class App : Application
    {
        private Mutex _singleInstanceMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            _singleInstanceMutex = new Mutex(true, "Local\\DesktopRightAlign_SingleInstance_Mutex", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("Windows Desktop Enhance is already running in the system tray.", "Windows Desktop Enhance", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            bool startMinimizedArg = false;
            if (e.Args != null)
            {
                for (int i = 0; i < e.Args.Length; i++)
                {
                    if (string.Equals(e.Args[i], "--minimized", StringComparison.OrdinalIgnoreCase))
                    {
                        startMinimizedArg = true;
                        break;
                    }
                }
            }

            AppSettings settings = AppSettings.Load();
            LocalizationService.SetLanguage(settings.LanguageCode);
            ApplyTheme(settings);

            MainWindow mainWindow = new MainWindow(settings, startMinimizedArg);
            MainWindow = mainWindow;

            if (!startMinimizedArg && !settings.StartMinimizedToTray)
            {
                mainWindow.Show();
            }
        }

        public void ApplyTheme(AppSettings settings)
        {
            bool useLight = settings.ThemeMode == AppThemeMode.Light ||
                            (settings.ThemeMode == AppThemeMode.System && Win32Interop.IsWindowsLightTheme());

            Color seed = ResolveSeedColor(settings.SeedColorHex);

            if (useLight)
            {
                // Material Design 3 Light Tonal ColorScheme derived from Seed
                Color primary = Blend(seed, Color.FromRgb(0x00, 0x00, 0x00), 0.10);
                Color primaryHover = Blend(primary, Color.FromRgb(0xFF, 0xFF, 0xFF), 0.12);
                Color primaryPressed = Blend(primary, Color.FromRgb(0x00, 0x00, 0x00), 0.14);
                Color onPrimary = Color.FromRgb(0xFF, 0xFF, 0xFF);

                Color secondaryContainer = Blend(Color.FromRgb(0xFF, 0xFF, 0xFF), seed, 0.16);
                Color secondaryContainerHover = Blend(Color.FromRgb(0xFF, 0xFF, 0xFF), seed, 0.23);
                Color onSecondaryContainer = Blend(seed, Color.FromRgb(0x14, 0x12, 0x18), 0.55);

                Color surface = Blend(Color.FromRgb(0xFF, 0xFB, 0xFE), seed, 0.025);
                Color navSurface = Blend(Color.FromRgb(0xF7, 0xF2, 0xFA), seed, 0.055);
                Color cardSurface = Blend(Color.FromRgb(0xF3, 0xED, 0xF7), seed, 0.045);
                Color cardHover = Blend(Color.FromRgb(0xEC, 0xE6, 0xF0), seed, 0.08);
                Color controlBg = Blend(Color.FromRgb(0xE8, 0xE2, 0xEC), seed, 0.07);
                Color controlHover = Blend(Color.FromRgb(0xDE, 0xD8, 0xE4), seed, 0.11);
                Color outlineVariant = Blend(Color.FromRgb(0xCA, 0xC4, 0xD0), seed, 0.10);
                Color outline = Blend(Color.FromRgb(0x79, 0x74, 0x7E), seed, 0.12);

                Resources["AccentBrush"] = new SolidColorBrush(primary);
                Resources["AccentHoverBrush"] = new SolidColorBrush(primaryHover);
                Resources["AccentPressedBrush"] = new SolidColorBrush(primaryPressed);
                Resources["OnAccentBrush"] = new SolidColorBrush(onPrimary);
                Resources["AccentSubtleBrush"] = new SolidColorBrush(secondaryContainer);
                Resources["SecondaryContainerHoverBrush"] = new SolidColorBrush(secondaryContainerHover);
                Resources["OnSecondaryContainerBrush"] = new SolidColorBrush(onSecondaryContainer);

                Resources["WindowBgBrush"] = new SolidColorBrush(surface);
                Resources["NavBgBrush"] = new SolidColorBrush(navSurface);
                Resources["CardBgBrush"] = new SolidColorBrush(cardSurface);
                Resources["CardHoverBrush"] = new SolidColorBrush(cardHover);
                Resources["ControlBgBrush"] = new SolidColorBrush(controlBg);
                Resources["ControlHoverBrush"] = new SolidColorBrush(controlHover);
                Resources["BorderBrushSubtle"] = new SolidColorBrush(outlineVariant);
                Resources["BorderBrushStrong"] = new SolidColorBrush(outline);
                Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x1D, 0x1B, 0x20));
                Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x49, 0x45, 0x4F));
                Resources["PreviewCanvasBgBrush"] = new SolidColorBrush(Blend(Color.FromRgb(0xE6, 0xE0, 0xE9), seed, 0.07));
                Resources["ScrollThumbBrush"] = new SolidColorBrush(outline);
            }
            else
            {
                // Material Design 3 Dark Tonal ColorScheme derived from Seed
                Color primary = Blend(seed, Color.FromRgb(0xFF, 0xFF, 0xFF), 0.42);
                Color primaryHover = Blend(seed, Color.FromRgb(0xFF, 0xFF, 0xFF), 0.52);
                Color primaryPressed = Blend(seed, Color.FromRgb(0xFF, 0xFF, 0xFF), 0.32);
                Color onPrimary = Blend(seed, Color.FromRgb(0x10, 0x0E, 0x14), 0.72);

                Color secondaryContainer = Blend(Color.FromRgb(0x1F, 0x1D, 0x24), seed, 0.34);
                Color secondaryContainerHover = Blend(Color.FromRgb(0x28, 0x25, 0x2E), seed, 0.42);
                Color onSecondaryContainer = Blend(seed, Color.FromRgb(0xFF, 0xFF, 0xFF), 0.76);

                Color surface = settings.PureBlackDark
                    ? Color.FromRgb(0x00, 0x00, 0x00)
                    : Blend(Color.FromRgb(0x14, 0x12, 0x18), seed, 0.045);
                Color navSurface = settings.PureBlackDark
                    ? Color.FromRgb(0x0A, 0x0A, 0x0C)
                    : Blend(Color.FromRgb(0x1B, 0x19, 0x20), seed, 0.06);
                Color cardSurface = settings.PureBlackDark
                    ? Blend(Color.FromRgb(0x12, 0x12, 0x15), seed, 0.05)
                    : Blend(Color.FromRgb(0x1F, 0x1D, 0x24), seed, 0.075);
                Color cardHover = Blend(Color.FromRgb(0x29, 0x27, 0x30), seed, 0.10);
                Color controlBg = Blend(Color.FromRgb(0x2B, 0x29, 0x30), seed, 0.09);
                Color controlHover = Blend(Color.FromRgb(0x36, 0x34, 0x3B), seed, 0.13);
                Color outlineVariant = Blend(Color.FromRgb(0x49, 0x45, 0x4F), seed, 0.12);
                Color outline = Blend(Color.FromRgb(0x93, 0x8F, 0x99), seed, 0.12);

                Resources["AccentBrush"] = new SolidColorBrush(primary);
                Resources["AccentHoverBrush"] = new SolidColorBrush(primaryHover);
                Resources["AccentPressedBrush"] = new SolidColorBrush(primaryPressed);
                Resources["OnAccentBrush"] = new SolidColorBrush(onPrimary);
                Resources["AccentSubtleBrush"] = new SolidColorBrush(secondaryContainer);
                Resources["SecondaryContainerHoverBrush"] = new SolidColorBrush(secondaryContainerHover);
                Resources["OnSecondaryContainerBrush"] = new SolidColorBrush(onSecondaryContainer);

                Resources["WindowBgBrush"] = new SolidColorBrush(surface);
                Resources["NavBgBrush"] = new SolidColorBrush(navSurface);
                Resources["CardBgBrush"] = new SolidColorBrush(cardSurface);
                Resources["CardHoverBrush"] = new SolidColorBrush(cardHover);
                Resources["ControlBgBrush"] = new SolidColorBrush(controlBg);
                Resources["ControlHoverBrush"] = new SolidColorBrush(controlHover);
                Resources["BorderBrushSubtle"] = new SolidColorBrush(outlineVariant);
                Resources["BorderBrushStrong"] = new SolidColorBrush(outline);
                Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xE6, 0xE0, 0xE9));
                Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0xCA, 0xC4, 0xD0));
                Resources["PreviewCanvasBgBrush"] = new SolidColorBrush(Blend(Color.FromRgb(0x0F, 0x0D, 0x13), seed, 0.04));
                Resources["ScrollThumbBrush"] = new SolidColorBrush(outlineVariant);
            }
        }

        private static Color ResolveSeedColor(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Equals("system", StringComparison.OrdinalIgnoreCase))
            {
                return Win32Interop.GetWindowsAccentColor();
            }
            try
            {
                object converted = ColorConverter.ConvertFromString(hex);
                if (converted is Color) return (Color)converted;
            }
            catch
            {
            }
            return Color.FromRgb(0x67, 0x50, 0xA4);
        }

        private static Color Blend(Color baseColor, Color tintColor, double ratio)
        {
            double inv = 1.0 - ratio;
            byte r = (byte)Math.Max(0, Math.Min(255, Math.Round(baseColor.R * inv + tintColor.R * ratio)));
            byte g = (byte)Math.Max(0, Math.Min(255, Math.Round(baseColor.G * inv + tintColor.G * ratio)));
            byte b = (byte)Math.Max(0, Math.Min(255, Math.Round(baseColor.B * inv + tintColor.B * ratio)));
            return Color.FromRgb(r, g, b);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_singleInstanceMutex != null)
            {
                try { _singleInstanceMutex.ReleaseMutex(); } catch { }
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
            }
            base.OnExit(e);
        }
    }
}
