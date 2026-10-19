// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// What the app knows about one disk, one SMART table row and one drive (volume).
using System;
using System.Collections.Generic;

namespace DiskCleanupHelper.Disks
{
    public class DiskHealth
    {
        public int Index;
        public string Model = "", Serial = "", Media = "Disk", Bus = "", Letters = "", WindowsStatus = "", Source = "";
        public string Pnp = "", TempFrom = "";   // TempFrom: where the temperature came from ("NVMe", "SMART", "Windows")
        public long Size;
        public int? HealthPct, TempC, WearUsedPct, SpareLeftPct;
        public long? PowerOnHours, PowerCycles, UnsafeShutdowns, MediaErrors;
        public double? WrittenBytes, ReadBytes;
        public bool PredictFailure;
        public List<string> Problems = new List<string>();
        public List<string> Notes = new List<string>();
        public List<SmartRow> SmartRows = new List<SmartRow>();   // the SMART table shown in the app
        public bool SmartIsNvme;
        public string Key { get { return string.IsNullOrEmpty(Serial) ? Model : Serial; } }
    }

    // One line of the SMART table. Level: 0 = OK, 1 = watch, 2 = failing.
    public class SmartRow
    {
        public string No = "", Name = "", Threshold = "", Value = "", Worst = "", Status = "", Data = "", Raw = "";
        public int Level;
    }

    public class VolumeInfo
    {
        public string Letter, Label, FileSystem, Health;
        public long Size, Free;
    }
}
