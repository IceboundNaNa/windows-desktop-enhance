using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DesktopRightAlign.Models
{
    public class DesktopIconInfo
    {
        public string DisplayName { get; set; }
        public string ParsingName { get; set; }
        public int CurrentX { get; set; }
        public int CurrentY { get; set; }
        public int TargetX { get; set; }
        public int TargetY { get; set; }
        public string ItemType { get; set; }
        public long SizeBytes { get; set; }
        public DateTime? ModifiedTime { get; set; }
        public bool IsFolder { get; set; }
    }

    [DataContract]
    public class SavedIconPosition
    {
        [DataMember]
        public string ParsingName { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public int X { get; set; }

        [DataMember]
        public int Y { get; set; }
    }

    [DataContract]
    public class DesktopLayoutBackup
    {
        [DataMember]
        public string SavedAtUtc { get; set; }

        [DataMember]
        public int WorkAreaWidth { get; set; }

        [DataMember]
        public int WorkAreaHeight { get; set; }

        [DataMember]
        public List<SavedIconPosition> Icons { get; set; }

        public DesktopLayoutBackup()
        {
            Icons = new List<SavedIconPosition>();
        }
    }

    public class DesktopEnvironmentInfo
    {
        public int WorkAreaLeft { get; set; }
        public int WorkAreaTop { get; set; }
        public int WorkAreaWidth { get; set; }
        public int WorkAreaHeight { get; set; }
        public int SpacingX { get; set; }
        public int SpacingY { get; set; }
        public bool NativeAutoArrange { get; set; }
        public bool NativeSnapToGrid { get; set; }
        public int IconCount { get; set; }
        public IntPtr DefViewHwnd { get; set; }
        public IntPtr ListViewHwnd { get; set; }
    }
}
