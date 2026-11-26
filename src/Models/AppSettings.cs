using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DesktopRightAlign.Models
{
    [DataContract]
    public class ExclusionZone
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int Left { get; set; }

        [DataMember]
        public int Top { get; set; }

        [DataMember]
        public int Width { get; set; }

        [DataMember]
        public int Height { get; set; }

        [DataMember]
        public bool Enabled { get; set; }

        public int Right { get { return Left + Width; } }
        public int Bottom { get { return Top + Height; } }

        public ExclusionZone()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "避让区域";
            Enabled = true;
        }

        public bool Intersects(int rectLeft, int rectTop, int rectRight, int rectBottom)
        {
            if (!Enabled || Width <= 0 || Height <= 0) return false;
            return rectLeft < Right && rectRight > Left && rectTop < Bottom && rectBottom > Top;
        }
    }
    public enum DesktopSortMode
    {
        Name = 0,
        Size = 1,
        ItemType = 2,
        DateModified = 3
    }

    public enum VerticalFlowDirection
    {
        TopToBottom = 0,
        BottomToTop = 1
    }

    public enum AppThemeMode
    {
        System = 0,
        Dark = 1,
        Light = 2
    }

    [DataContract]
    public class AppSettings
    {
        [DataMember]
        public bool AutoArrangeEnabled { get; set; }

        [DataMember]
        public bool LayoutGuardEnabled { get; set; }

        [DataMember]
        public bool RestoreLayoutOnStartup { get; set; }

        [DataMember]
        public DesktopSortMode SortMode { get; set; }

        [DataMember]
        public bool SortAscending { get; set; }

        [DataMember]
        public VerticalFlowDirection FlowDirection { get; set; }

        [DataMember]
        public int RightMargin { get; set; }

        [DataMember]
        public int TopMargin { get; set; }

        [DataMember]
        public int BottomMargin { get; set; }

        [DataMember]
        public bool UseNativeGridSpacing { get; set; }

        [DataMember]
        public int CustomHorizontalSpacing { get; set; }

        [DataMember]
        public int CustomVerticalSpacing { get; set; }

        [DataMember]
        public bool AutoDisableNativeAutoArrange { get; set; }

        [DataMember]
        public int DebounceMilliseconds { get; set; }

        [DataMember]
        public bool StartWithWindows { get; set; }

        [DataMember]
        public bool StartMinimizedToTray { get; set; }

        [DataMember]
        public bool MinimizeToTrayOnClose { get; set; }

        [DataMember]
        public AppThemeMode ThemeMode { get; set; }

        [DataMember]
        public string SeedColorHex { get; set; }

        [DataMember]
        public bool PureBlackDark { get; set; }

        [DataMember]
        public string LanguageCode { get; set; }

        [DataMember]
        public bool ExclusionZonesEnabled { get; set; }

        [DataMember]
        public List<ExclusionZone> ExclusionZones { get; set; }

        public AppSettings()
        {
            AutoArrangeEnabled = false;
            LayoutGuardEnabled = true;
            RestoreLayoutOnStartup = true;
            SortMode = DesktopSortMode.Name;
            SortAscending = true;
            FlowDirection = VerticalFlowDirection.TopToBottom;
            RightMargin = 12;
            TopMargin = 12;
            BottomMargin = 12;
            UseNativeGridSpacing = true;
            CustomHorizontalSpacing = 96;
            CustomVerticalSpacing = 96;
            AutoDisableNativeAutoArrange = true;
            DebounceMilliseconds = 450;
            StartWithWindows = false;
            StartMinimizedToTray = false;
            MinimizeToTrayOnClose = true;
            ThemeMode = AppThemeMode.System;
            SeedColorHex = "#6750A4";
            PureBlackDark = false;
            LanguageCode = "auto";
            ExclusionZonesEnabled = true;
            ExclusionZones = new List<ExclusionZone>();
        }

        public static string SettingsDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DesktopRightAlign");
            }
        }

        public static string SettingsFilePath
        {
            get { return Path.Combine(SettingsDirectory, "settings.json"); }
        }

        public static string BackupFilePath
        {
            get { return Path.Combine(SettingsDirectory, "layout_backup.json"); }
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    using (var fs = new FileStream(SettingsFilePath, FileMode.Open, FileAccess.Read))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(AppSettings));
                        var loaded = serializer.ReadObject(fs) as AppSettings;
                        if (loaded != null)
                        {
                            if (loaded.DebounceMilliseconds < 150) loaded.DebounceMilliseconds = 150;
                            if (loaded.CustomHorizontalSpacing < 48) loaded.CustomHorizontalSpacing = 48;
                            if (loaded.CustomVerticalSpacing < 48) loaded.CustomVerticalSpacing = 48;
                            if (loaded.ExclusionZones == null) loaded.ExclusionZones = new List<ExclusionZone>();
                            if (string.IsNullOrEmpty(loaded.LanguageCode)) loaded.LanguageCode = "auto";
                            if (string.IsNullOrEmpty(loaded.SeedColorHex)) loaded.SeedColorHex = "#6750A4";
                            return loaded;
                        }
                    }
                }
            }
            catch
            {
                // Fall back to defaults on corrupted file.
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                using (var fs = new FileStream(SettingsFilePath, FileMode.Create, FileAccess.Write))
                {
                    var serializer = new DataContractJsonSerializer(typeof(AppSettings));
                    serializer.WriteObject(fs, this);
                }
            }
            catch
            {
                // Ignore transient I/O errors.
            }
        }
    }
}
