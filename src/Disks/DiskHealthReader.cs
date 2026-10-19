// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Reads disk health and temperature from SMART and the NVMe health log.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.Disks
{
    class SmartData { public byte[] Data, Thresholds; public bool Predict; }
    class SmartAttr { public int Id, Current, Worst, Threshold; public long Raw; }

    public static class DiskHealthReader
    {
        const string StorageNs = @"root\Microsoft\Windows\Storage";
        const uint GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000, FILE_SHARE_READ = 1, FILE_SHARE_WRITE = 2, OPEN_EXISTING = 3;
        const uint IOCTL_STORAGE_QUERY_PROPERTY = 0x002D1400;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool DeviceIoControl(SafeFileHandle device, uint code, byte[] inBuf, int inSize, byte[] outBuf, int outSize, out int returned, IntPtr overlapped);

        static long ToLong(object v) { try { return v == null ? 0 : Convert.ToInt64(v); } catch { return 0; } }
        static string Str(object v) { return v == null ? "" : Convert.ToString(v).Trim(); }

        static string MediaName(object v)
        {
            switch ((int)ToLong(v)) { case 3: return "HDD"; case 4: return "SSD"; case 5: return "SCM"; default: return "Disk"; }
        }

        static string BusName(object v)
        {
            switch ((int)ToLong(v))
            {
                case 1: return "SCSI"; case 3: return "ATA"; case 7: return "USB"; case 8: return "RAID"; case 10: return "SAS";
                case 11: return "SATA"; case 12: return "SD card"; case 13: return "MMC"; case 15: return "Virtual"; case 16: return "Storage Spaces";
                case 17: return "NVMe"; default: return "";
            }
        }

        static string StatusName(object v)
        {
            if (v == null) return "";
            switch ((int)ToLong(v)) { case 0: return "Healthy"; case 1: return "Warning"; case 2: return "Unhealthy"; default: return "Unknown"; }
        }

        public static List<DiskHealth> ReadAll()
        {
            Dictionary<int, DiskHealth> disks = new Dictionary<int, DiskHealth>();
            Dictionary<int, string> pnp = new Dictionary<int, string>();

            // 1. basic disk list (works without admin)
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher("SELECT Index, Model, SerialNumber, PNPDeviceID, Size FROM Win32_DiskDrive"))
                {
                    foreach (ManagementBaseObject mo in s.Get())
                    {
                        DiskHealth d = new DiskHealth();
                        d.Index = (int)ToLong(mo["Index"]);
                        d.Model = Str(mo["Model"]);
                        d.Serial = Str(mo["SerialNumber"]).TrimEnd('.');
                        d.Size = ToLong(mo["Size"]);
                        d.Pnp = Str(mo["PNPDeviceID"]);
                        disks[d.Index] = d;
                        pnp[d.Index] = d.Pnp;
                    }
                }
            }
            catch { }

            // 2. type, bus and Windows' own status; reliability counters when admin
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(StorageNs, "SELECT * FROM MSFT_PhysicalDisk"))
                {
                    foreach (ManagementObject mo in s.Get())
                    {
                        int idx;
                        if (!int.TryParse(Str(mo["DeviceId"]), out idx)) continue;
                        DiskHealth d;
                        if (!disks.TryGetValue(idx, out d)) { d = new DiskHealth(); d.Index = idx; disks[idx] = d; }
                        if (string.IsNullOrEmpty(d.Model)) d.Model = Str(mo["FriendlyName"]);
                        if (string.IsNullOrEmpty(d.Serial)) d.Serial = Str(mo["SerialNumber"]).TrimEnd('.');
                        if (d.Size == 0) d.Size = ToLong(mo["Size"]);
                        d.Media = MediaName(mo["MediaType"]);
                        d.Bus = BusName(mo["BusType"]);
                        d.WindowsStatus = StatusName(mo["HealthStatus"]);
                        if (Env.IsAdmin)
                        {
                            try { foreach (ManagementObject rc in mo.GetRelated("MSFT_StorageReliabilityCounter")) ApplyReliability(d, rc); }
                            catch { }
                        }
                    }
                }
            }
            catch { }

            // 3. drive letters on each disk
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(StorageNs, "SELECT DiskNumber, DriveLetter FROM MSFT_Partition"))
                {
                    foreach (ManagementBaseObject mo in s.Get())
                    {
                        object dl = mo["DriveLetter"];
                        if (dl == null) continue;
                        char c = Convert.ToChar(dl);
                        if (!char.IsLetter(c)) continue;
                        DiskHealth d;
                        if (disks.TryGetValue((int)ToLong(mo["DiskNumber"]), out d)) d.Letters = (d.Letters + " " + char.ToUpperInvariant(c) + ":").Trim();
                    }
                }
            }
            catch { }

            // 4. the disks' own health data
            Dictionary<string, SmartData> smart = Env.IsAdmin ? ReadSmartWmi(true) : new Dictionary<string, SmartData>();
            foreach (DiskHealth d in disks.Values)
            {
                bool done = false;
                if (d.Bus == "NVMe" || d.Media == "SSD")
                {
                    byte[] log = ReadNvmeHealthLog(d.Index);
                    if (log != null) { ApplyNvme(d, log); done = true; }
                }
                if (!done)
                {
                    string p;
                    if (pnp.TryGetValue(d.Index, out p) && !string.IsNullOrEmpty(p))
                    {
                        foreach (KeyValuePair<string, SmartData> kv in smart)
                        {
                            if (kv.Key.StartsWith(p, StringComparison.OrdinalIgnoreCase)) { ApplySmart(d, kv.Value); done = true; break; }
                        }
                    }
                }
                if (!done && d.WearUsedPct.HasValue)
                {
                    d.HealthPct = Math.Max(0, 100 - d.WearUsedPct.Value);
                    d.Source = "Windows";
                }
                if (d.WindowsStatus == "Warning")
                {
                    if (d.HealthPct.HasValue) d.HealthPct = Math.Min(d.HealthPct.Value, 40);
                    d.Problems.Add(L.T("Windows reports a warning for this disk."));
                }
                else if (d.WindowsStatus == "Unhealthy")
                {
                    if (d.HealthPct.HasValue) d.HealthPct = Math.Min(d.HealthPct.Value, 10);
                    d.Problems.Add(L.T("Windows reports this disk as unhealthy."));
                }
            }
            return disks.Values.OrderBy(x => x.Index).ToList();
        }

        static void ApplyReliability(DiskHealth d, ManagementBaseObject rc)
        {
            long t = ToLong(rc["Temperature"]);
            if (t > 0 && t < 120 && !d.TempC.HasValue) { d.TempC = (int)t; d.TempFrom = "Windows"; }
            object w = rc["Wear"];
            if (w != null) { long wear = ToLong(w); if (wear >= 0 && wear <= 100) d.WearUsedPct = (int)wear; }
            long h = ToLong(rc["PowerOnHours"]);
            if (h > 0 && !d.PowerOnHours.HasValue) d.PowerOnHours = h;
        }

        // ---------------------------------------------------------------- NVMe health log
        static byte[] ReadNvmeHealthLog(int index)
        {
            foreach (uint access in new uint[] { GENERIC_READ | GENERIC_WRITE, 0 })
            {
                using (SafeFileHandle h = CreateFile(@"\\.\PhysicalDrive" + index, access, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
                {
                    if (h.IsInvalid) continue;
                    foreach (int prop in new[] { 49, 50 })   // adapter, then device protocol-specific property
                    {
                        byte[] buf = new byte[8 + 40 + 512];
                        BitConverter.GetBytes(prop).CopyTo(buf, 0);   // PropertyId
                        BitConverter.GetBytes(0).CopyTo(buf, 4);      // PropertyStandardQuery
                        BitConverter.GetBytes(3).CopyTo(buf, 8);      // ProtocolTypeNvme
                        BitConverter.GetBytes(2).CopyTo(buf, 12);     // NVMeDataTypeLogPage
                        BitConverter.GetBytes(2).CopyTo(buf, 16);     // health information log page
                        BitConverter.GetBytes(0).CopyTo(buf, 20);
                        BitConverter.GetBytes(40).CopyTo(buf, 24);    // data offset
                        BitConverter.GetBytes(512).CopyTo(buf, 28);   // data length
                        int ret;
                        if (!DeviceIoControl(h, IOCTL_STORAGE_QUERY_PROPERTY, buf, buf.Length, buf, buf.Length, out ret, IntPtr.Zero)) continue;
                        int off = BitConverter.ToInt32(buf, 24);
                        int start = 8 + off;
                        if (off <= 0 || start + 512 > buf.Length) continue;
                        byte[] log = new byte[512];
                        Array.Copy(buf, start, log, 0, 512);
                        int kelvin = log[1] | (log[2] << 8);
                        if (kelvin < 200 || kelvin > 450) continue;   // not a real reading
                        return log;
                    }
                }
            }
            return null;
        }

        static SmartRow NvmeRow(string no, string name, string value, int level, string status)
        {
            SmartRow r = new SmartRow();
            r.No = no; r.Name = name; r.Value = value; r.Level = level; r.Status = status;
            return r;
        }

        // "{0} minutes" is a plural text in the language files, so it must go through L.P, never L.F.
        static string Minutes(ulong count)
        {
            return L.P((long)Math.Min(count, (ulong)long.MaxValue), "1 minute", "{0} minutes");
        }

        static void ApplyNvme(DiskHealth d, byte[] log)
        {
            d.Source = "NVMe";
            d.SmartIsNvme = true;
            {
                int cw = log[0];
                int k = log[1] | (log[2] << 8);
                ulong unitsRead = BitConverter.ToUInt64(log, 32), unitsWritten = BitConverter.ToUInt64(log, 48);
                d.SmartRows.Add(NvmeRow("0", "Critical warning", "0x" + cw.ToString("X2"), cw == 0 ? 0 : 2, cw == 0 ? L.T("OK") : L.T("Warning flags set")));
                d.SmartRows.Add(NvmeRow("1", "Temperature", L.Ltr((k - 273) + " °C"), (k - 273) >= 75 ? 1 : 0, (k - 273) >= 75 ? L.T("Hot") : L.T("OK")));
                d.SmartRows.Add(NvmeRow("3", "Available spare", L.Ltr(log[3] + "%"), log[3] < log[4] ? 2 : 0, log[3] < log[4] ? L.T("Below threshold") : L.T("OK")));
                d.SmartRows.Add(NvmeRow("4", "Available spare threshold", L.Ltr(log[4] + "%"), 0, ""));
                d.SmartRows.Add(NvmeRow("5", "Percentage used (wear)", L.Ltr(log[5] + "%"), log[5] >= 90 ? 1 : 0, log[5] >= 90 ? L.T("Near end of rated life") : L.T("OK")));
                d.SmartRows.Add(NvmeRow("32", "Data units read", unitsRead.ToString("N0") + "  (" + Fmt.LargeSize(unitsRead * 512000.0) + ")", 0, ""));
                d.SmartRows.Add(NvmeRow("48", "Data units written", unitsWritten.ToString("N0") + "  (" + Fmt.LargeSize(unitsWritten * 512000.0) + ")", 0, ""));
                d.SmartRows.Add(NvmeRow("64", "Host read commands", BitConverter.ToUInt64(log, 64).ToString("N0"), 0, ""));
                d.SmartRows.Add(NvmeRow("80", "Host write commands", BitConverter.ToUInt64(log, 80).ToString("N0"), 0, ""));
                d.SmartRows.Add(NvmeRow("96", "Controller busy time", Minutes(BitConverter.ToUInt64(log, 96)), 0, ""));
                d.SmartRows.Add(NvmeRow("112", "Power cycles", BitConverter.ToUInt64(log, 112).ToString("N0"), 0, ""));
                d.SmartRows.Add(NvmeRow("128", "Power on hours", BitConverter.ToUInt64(log, 128).ToString("N0"), 0, ""));
                ulong unsafeCount = BitConverter.ToUInt64(log, 144), media = BitConverter.ToUInt64(log, 160);
                d.SmartRows.Add(NvmeRow("144", "Unsafe shutdowns", unsafeCount.ToString("N0"), 0, ""));
                d.SmartRows.Add(NvmeRow("160", "Media and data integrity errors", media.ToString("N0"), media > 0 ? 1 : 0, media > 0 ? L.T("Watch") : L.T("OK")));
                d.SmartRows.Add(NvmeRow("176", "Error information log entries", BitConverter.ToUInt64(log, 176).ToString("N0"), 0, ""));
                d.SmartRows.Add(NvmeRow("192", "Warning temperature time", Minutes(BitConverter.ToUInt32(log, 192)), 0, ""));
                d.SmartRows.Add(NvmeRow("196", "Critical temperature time", Minutes(BitConverter.ToUInt32(log, 196)), 0, ""));
            }
            int critical = log[0];
            int kelvin = log[1] | (log[2] << 8);
            int spare = log[3], spareThreshold = log[4], used = log[5];
            d.TempC = kelvin - 273;
            d.TempFrom = "NVMe";
            d.SpareLeftPct = spare;
            d.WearUsedPct = used;
            d.ReadBytes = BitConverter.ToUInt64(log, 32) * 512000.0;
            d.WrittenBytes = BitConverter.ToUInt64(log, 48) * 512000.0;
            d.PowerCycles = (long)BitConverter.ToUInt64(log, 112);
            d.PowerOnHours = (long)BitConverter.ToUInt64(log, 128);
            d.UnsafeShutdowns = (long)BitConverter.ToUInt64(log, 144);
            d.MediaErrors = (long)BitConverter.ToUInt64(log, 160);

            double h = 100 - Math.Min(100, used);
            if (spare < 100) h = Math.Min(h, spare);
            if (spare < spareThreshold)
            {
                h = Math.Min(h, 10);
                d.Problems.Add(L.F("Spare space is below the safe level ({0}% left). Back up your files now.", spare));
            }
            if ((critical & 0x04) != 0) { h = Math.Min(h, 10); d.Problems.Add(L.T("The SSD reports that its reliability is degraded. Back up your files now.")); }
            if ((critical & 0x08) != 0) { h = Math.Min(h, 5); d.Problems.Add(L.T("The SSD has switched to read-only mode to protect your data.")); }
            if ((critical & 0x02) != 0) d.Problems.Add(L.T("The SSD reports that it has been too hot."));
            if (d.MediaErrors > 0)
            {
                h -= Math.Min(30, d.MediaErrors.Value);
                d.Problems.Add(L.P(d.MediaErrors.Value, "1 data error was found on the SSD.", "{0} data errors were found on the SSD."));
            }
            d.HealthPct = (int)Math.Max(0, Math.Round(h));
        }

        // ---------------------------------------------------------------- temperature only (live update on Disk health)
        // Re-reads each disk's temperature from the same source the full check used (SATA and Windows' counters
        // need admin rights). Returns disk index -> °C for the disks that answered.
        public static Dictionary<int, int> ReadTemperatures(List<DiskHealth> disks)
        {
            Dictionary<int, int> temps = new Dictionary<int, int>();
            Dictionary<string, SmartData> smart = null;
            foreach (DiskHealth d in disks)
            {
                try
                {
                    int? t = null;
                    if (d.TempFrom == "NVMe")
                    {
                        byte[] log = ReadNvmeHealthLog(d.Index);
                        if (log != null) t = (log[1] | (log[2] << 8)) - 273;
                    }
                    else if (d.TempFrom == "SMART" && Env.IsAdmin && d.Pnp.Length > 0)
                    {
                        if (smart == null) smart = ReadSmartWmi(false);
                        foreach (KeyValuePair<string, SmartData> kv in smart)
                            if (kv.Key.StartsWith(d.Pnp, StringComparison.OrdinalIgnoreCase)) { t = SmartTemp(kv.Value.Data); break; }
                    }
                    else if (d.TempFrom == "Windows" && Env.IsAdmin) t = ReliabilityTemp(d.Index);
                    if (t.HasValue && t.Value > 0 && t.Value < 120) temps[d.Index] = t.Value;
                }
                catch { }
            }
            return temps;
        }

        // SMART attribute 194 (disk temperature), else 190 (airflow temperature): the lowest raw byte is °C.
        static int? SmartTemp(byte[] b)
        {
            if (b == null || b.Length < 362) return null;
            int? airflow = null;
            for (int i = 0; i < 30; i++)
            {
                int o = 2 + i * 12, id = b[o], c = b[o + 5];
                if (c <= 0 || c >= 100) continue;
                if (id == 194) return c;
                if (id == 190 && !airflow.HasValue) airflow = c;
            }
            return airflow;
        }

        static int? ReliabilityTemp(int index)
        {
            using (ManagementObjectSearcher s = new ManagementObjectSearcher(StorageNs, "SELECT * FROM MSFT_PhysicalDisk WHERE DeviceId = '" + index + "'"))
            {
                foreach (ManagementObject mo in s.Get())
                    foreach (ManagementObject rc in mo.GetRelated("MSFT_StorageReliabilityCounter"))
                    {
                        long t = ToLong(rc["Temperature"]);
                        if (t > 0 && t < 120) return (int)t;
                    }
            }
            return null;
        }

        // ---------------------------------------------------------------- SATA / ATA SMART
        // all: also the thresholds and the failure prediction (the temperature needs only the data).
        static Dictionary<string, SmartData> ReadSmartWmi(bool all)
        {
            Dictionary<string, SmartData> map = new Dictionary<string, SmartData>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(@"root\WMI", "SELECT InstanceName, VendorSpecific FROM MSStorageDriver_FailurePredictData"))
                {
                    foreach (ManagementBaseObject mo in s.Get())
                    {
                        SmartData sd = new SmartData();
                        sd.Data = mo["VendorSpecific"] as byte[];
                        map[Str(mo["InstanceName"])] = sd;
                    }
                }
            }
            catch { }
            if (!all) return map;
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(@"root\WMI", "SELECT InstanceName, VendorSpecific FROM MSStorageDriver_FailurePredictThresholds"))
                {
                    foreach (ManagementBaseObject mo in s.Get())
                    {
                        SmartData sd;
                        if (map.TryGetValue(Str(mo["InstanceName"]), out sd)) sd.Thresholds = mo["VendorSpecific"] as byte[];
                    }
                }
            }
            catch { }
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(@"root\WMI", "SELECT InstanceName, PredictFailure FROM MSStorageDriver_FailurePredictStatus"))
                {
                    foreach (ManagementBaseObject mo in s.Get())
                    {
                        SmartData sd;
                        if (map.TryGetValue(Str(mo["InstanceName"]), out sd)) sd.Predict = Convert.ToBoolean(mo["PredictFailure"]);
                    }
                }
            }
            catch { }
            return map;
        }

        static void ApplySmart(DiskHealth d, SmartData sd)
        {
            byte[] b = sd.Data;
            if (b == null || b.Length < 362) return;
            Dictionary<int, SmartAttr> attrs = new Dictionary<int, SmartAttr>();
            for (int i = 0; i < 30; i++)
            {
                int o = 2 + i * 12;
                int id = b[o];
                if (id == 0) continue;
                SmartAttr a = new SmartAttr();
                a.Id = id; a.Current = b[o + 3]; a.Worst = b[o + 4];
                long raw = 0;
                for (int k = 0; k < 6; k++) raw |= (long)b[o + 5 + k] << (8 * k);
                a.Raw = raw;
                attrs[id] = a;
            }
            if (sd.Thresholds != null && sd.Thresholds.Length >= 362)
            {
                for (int i = 0; i < 30; i++)
                {
                    int o = 2 + i * 12;
                    SmartAttr a;
                    if (sd.Thresholds[o] != 0 && attrs.TryGetValue(sd.Thresholds[o], out a)) a.Threshold = sd.Thresholds[o + 1];
                }
            }
            if (attrs.Count == 0) return;
            d.Source = "SMART";

            Func<int, long> count = delegate(int id) { SmartAttr a; return attrs.TryGetValue(id, out a) ? (a.Raw & 0xFFFFFFFF) : 0; };
            int? temp = SmartTemp(b);
            if (temp.HasValue) { d.TempC = temp; d.TempFrom = "SMART"; }
            if (attrs.ContainsKey(9)) d.PowerOnHours = count(9);
            if (attrs.ContainsKey(12)) d.PowerCycles = count(12);
            if (d.Media == "HDD")
            {
                // on hard disks these count 512-byte blocks
                if (attrs.ContainsKey(241) && attrs[241].Raw > 0) d.WrittenBytes = attrs[241].Raw * 512.0;
                if (attrs.ContainsKey(242) && attrs[242].Raw > 0) d.ReadBytes = attrs[242].Raw * 512.0;
            }

            // Health score, tuned to land close to Hard Disk Sentinel's scale:
            // each bad, pending or uncorrectable sector costs about half a percent.
            double h = 100;
            long realloc = count(5), events = count(196), pending = count(197), uncorrectable = count(198);
            long reported = count(187), spinRetry = count(10), endToEnd = count(184), crc = count(199);
            if (realloc > 0) { h -= Math.Min(60, realloc * 0.5); d.Problems.Add(L.P(realloc, "1 bad sector was replaced by a spare sector (reallocated).", "{0} bad sectors were replaced by spare sectors (reallocated).")); }
            else if (events > 0) { h -= Math.Min(30, events * 0.25); d.Problems.Add(L.P(events, "1 sector reallocation event.", "{0} sector reallocation events.")); }
            if (pending > 0) { h -= Math.Min(60, pending * 0.5); d.Problems.Add(L.P(pending, "1 sector is unstable and waiting to be checked (pending). Keep a backup of your files and watch whether this number grows.", "{0} sectors are unstable and waiting to be checked (pending). Keep a backup of your files and watch whether this number grows.")); }
            if (uncorrectable > 0) { h -= Math.Min(60, uncorrectable * 0.5); d.Problems.Add(L.P(uncorrectable, "1 sector could not be read or fixed (uncorrectable).", "{0} sectors could not be read or fixed (uncorrectable).")); }
            if (reported > 0) { h -= Math.Min(30, reported * 1.0); d.Problems.Add(L.P(reported, "1 read error the disk could not correct.", "{0} read errors the disk could not correct.")); }
            if (spinRetry > 0) { h -= Math.Min(20, spinRetry * 2); d.Problems.Add(L.P(spinRetry, "The disk needed 1 retry to spin up.", "The disk needed {0} retries to spin up.")); }
            if (endToEnd > 0) { h -= Math.Min(30, endToEnd * 5); d.Problems.Add(L.P(endToEnd, "1 internal data path error.", "{0} internal data path errors.")); }
            if (crc > 0) d.Notes.Add(L.P(crc, "1 transfer error between the disk and the PC was counted. These usually come from a loose or bad cable and do not harm the disk.", "{0} transfer errors between the disk and the PC were counted. These usually come from a loose or bad cable and do not harm the disk."));

            if (d.Media != "HDD")
            {
                foreach (int id in new[] { 231, 233, 202, 177, 169, 173 })
                {
                    SmartAttr a;
                    if (attrs.TryGetValue(id, out a) && a.Current > 0 && a.Current <= 100)
                    {
                        h = Math.Min(h, a.Current);
                        d.WearUsedPct = 100 - a.Current;
                        break;
                    }
                }
            }
            foreach (SmartAttr a in attrs.Values)
            {
                if (a.Threshold > 0 && a.Current <= a.Threshold)
                {
                    h = Math.Min(h, 5);
                    d.Problems.Add(L.F("SMART attribute {0} is below its safety limit. Back up your files now.", a.Id));
                }
            }
            if (sd.Predict)
            {
                h = Math.Min(h, 5);
                d.PredictFailure = true;
                d.Problems.Add(L.T("The disk itself predicts that it will fail soon. Back up your files now."));
            }
            d.HealthPct = (int)Math.Max(0, Math.Round(h));

            // the SMART table
            foreach (SmartAttr a in attrs.Values.OrderBy(x => x.Id))
            {
                SmartRow r = new SmartRow();
                r.No = a.Id.ToString();
                r.Name = AttrName(a.Id, d.Media);
                r.Threshold = a.Threshold.ToString();
                r.Value = a.Current.ToString();
                r.Worst = a.Worst.ToString();
                r.Data = a.Raw.ToString("X12");
                r.Raw = a.Raw.ToString("N0");
                bool counter = Array.IndexOf(new[] { 5, 10, 184, 187, 196, 197, 198 }, a.Id) >= 0;
                if (a.Threshold > 0 && a.Current <= a.Threshold) { r.Level = 2; r.Status = L.T("Failing"); }
                else if (counter && (a.Raw & 0xFFFFFFFF) > 0) { r.Level = 1; r.Status = L.T("Watch"); }
                else if (a.Threshold == 0) { r.Status = L.T("OK (no limit)"); }
                else r.Status = L.T("OK");
                d.SmartRows.Add(r);
            }
        }

        static readonly Dictionary<int, string> AttrNames = new Dictionary<int, string> {
            { 1, "Raw Read Error Rate" }, { 2, "Throughput Performance" }, { 3, "Spin Up Time" }, { 4, "Start/Stop Count" },
            { 5, "Reallocated Sectors Count" }, { 6, "Read Channel Margin" }, { 7, "Seek Error Rate" }, { 8, "Seek Time Performance" },
            { 9, "Power On Time Count" }, { 10, "Spin Retry Count" }, { 11, "Drive Calibration Retry Count" }, { 12, "Drive Power Cycle Count" },
            { 13, "Soft Read Error Rate" }, { 22, "Current Helium Level" }, { 170, "Available Reserved Space" }, { 171, "Program Fail Count" },
            { 172, "Erase Fail Count" }, { 173, "Wear Leveling Count" }, { 174, "Unexpected Power Loss Count" }, { 175, "Power Loss Protection Failure" },
            { 176, "Erase Fail Count" }, { 177, "Wear Leveling Count" }, { 179, "Used Reserved Block Count" }, { 180, "Unused Reserved Block Count" },
            { 181, "Program Fail Count" }, { 182, "Erase Fail Count" }, { 183, "Runtime Bad Blocks" }, { 184, "End-to-End Error" },
            { 187, "Reported Uncorrectable Errors" }, { 188, "Command Timeout" }, { 189, "High Fly Writes" }, { 190, "Airflow Temperature" },
            { 191, "G-Sense Error Rate" }, { 192, "Power off Retract Cycle Count" }, { 193, "Load/Unload Cycle Count" }, { 194, "Disk Temperature" },
            { 195, "Hardware ECC Recovered" }, { 196, "Reallocation Event Count" }, { 197, "Current Pending Sector Count" },
            { 198, "Off-Line Uncorrectable Sector Count" }, { 199, "Ultra ATA CRC Error Count" }, { 200, "Write Error Rate" },
            { 201, "Soft Read Error Rate" }, { 202, "Data Address Mark Errors" }, { 203, "Run Out Cancel" }, { 204, "Soft ECC Correction" },
            { 205, "Thermal Asperity Rate" }, { 206, "Flying Height" }, { 207, "Spin High Current" }, { 208, "Spin Buzz" },
            { 209, "Offline Seek Performance" }, { 220, "Disk Shift" }, { 221, "G-Sense Error Rate" }, { 222, "Loaded Hours" },
            { 223, "Load/Unload Retry Count" }, { 224, "Load Friction" }, { 225, "Load/Unload Cycle Count" }, { 226, "Load-in Time" },
            { 227, "Torque Amplification Count" }, { 228, "Power-Off Retract Cycle" }, { 230, "GMR Head Amplitude" }, { 231, "Life Left" },
            { 232, "Endurance Remaining" }, { 233, "Media Wearout Indicator" }, { 234, "Average Erase Count" }, { 235, "Good Block Count" },
            { 240, "Head Flying Hours" }, { 241, "Total LBA Written" }, { 242, "Total LBA Read" }, { 243, "Total LBA Written (expanded)" },
            { 244, "Total LBA Read (expanded)" }, { 249, "NAND Writes" }, { 250, "Read Error Retry Rate" }, { 251, "Minimum Spares Remaining" },
            { 252, "Newly Added Bad Flash Block" }, { 254, "Free Fall Event Count" }
        };

        static string AttrName(int id, string media)
        {
            if (media == "HDD" && id == 231) return "Temperature";
            if (media != "HDD" && id == 202) return "Percent Lifetime Remaining";
            string n;
            return AttrNames.TryGetValue(id, out n) ? n : L.T("Vendor specific");
        }

        // For testing: works out health and the SMART table from raw SMART bytes.
        public static DiskHealth FromSmartBytes(byte[] data, byte[] thresholds, string media)
        {
            DiskHealth d = new DiskHealth();
            d.Media = media;
            SmartData sd = new SmartData();
            sd.Data = data; sd.Thresholds = thresholds;
            ApplySmart(d, sd);
            return d;
        }

        // ---------------------------------------------------------------- drives (volumes)
        public static List<VolumeInfo> Volumes()
        {
            Dictionary<string, string> health = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(StorageNs, "SELECT DriveLetter, HealthStatus FROM MSFT_Volume"))
                {
                    foreach (ManagementBaseObject mo in s.Get())
                    {
                        object dl = mo["DriveLetter"];
                        if (dl == null) continue;
                        char c = Convert.ToChar(dl);
                        if (!char.IsLetter(c)) continue;
                        health[c.ToString()] = StatusName(mo["HealthStatus"]);
                    }
                }
            }
            catch { }
            List<VolumeInfo> list = new List<VolumeInfo>();
            foreach (DriveInfo dv in DriveInfo.GetDrives())
            {
                try
                {
                    if (dv.DriveType != DriveType.Fixed || !dv.IsReady) continue;
                    VolumeInfo v = new VolumeInfo();
                    v.Letter = dv.Name.Substring(0, 1).ToUpperInvariant();
                    v.Label = dv.VolumeLabel;
                    v.FileSystem = dv.DriveFormat;
                    v.Size = dv.TotalSize;
                    v.Free = dv.AvailableFreeSpace;
                    string h;
                    v.Health = health.TryGetValue(v.Letter, out h) ? h : "";
                    list.Add(v);
                }
                catch { }
            }
            return list;
        }
    }
}
