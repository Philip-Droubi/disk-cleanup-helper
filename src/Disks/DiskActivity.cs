// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Live disk activity from Windows' performance counters.
using System;
using System.Collections.Generic;
using System.Management;

namespace DiskCleanupHelper.Disks
{
    public class DiskRaw
    {
        public string Name;
        public ulong Read, Write, Idle, Ts100, PerfTime, PerfFreq;
    }

    public class DiskRate
    {
        public double ReadBps, WriteBps, Activity;
    }

    public static class DiskPerf
    {
        // Raw counters per physical disk, for example "0 D: E:" or "1 C:". Works without admin rights.
        public static Dictionary<string, DiskRaw> ReadRaw()
        {
            Dictionary<string, DiskRaw> map = new Dictionary<string, DiskRaw>(StringComparer.OrdinalIgnoreCase);
            using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                "SELECT Name, DiskReadBytesPersec, DiskWriteBytesPersec, PercentIdleTime, Timestamp_Sys100NS, Timestamp_PerfTime, Frequency_PerfTime FROM Win32_PerfRawData_PerfDisk_PhysicalDisk"))
            {
                foreach (ManagementBaseObject mo in s.Get())
                {
                    DiskRaw r = new DiskRaw();
                    r.Name = Convert.ToString(mo["Name"]);
                    if (r.Name == "_Total") continue;
                    r.Read = Convert.ToUInt64(mo["DiskReadBytesPersec"]);
                    r.Write = Convert.ToUInt64(mo["DiskWriteBytesPersec"]);
                    r.Idle = Convert.ToUInt64(mo["PercentIdleTime"]);
                    r.Ts100 = Convert.ToUInt64(mo["Timestamp_Sys100NS"]);
                    r.PerfTime = Convert.ToUInt64(mo["Timestamp_PerfTime"]);
                    r.PerfFreq = Convert.ToUInt64(mo["Frequency_PerfTime"]);
                    map[r.Name] = r;
                }
            }
            return map;
        }

        public static DiskRate Rate(DiskRaw a, DiskRaw b)
        {
            DiskRate r = new DiskRate();
            if (a == null || b == null || b.PerfTime <= a.PerfTime || b.PerfFreq == 0) return r;
            double seconds = (double)(b.PerfTime - a.PerfTime) / b.PerfFreq;
            if (seconds <= 0) return r;
            r.ReadBps = b.Read >= a.Read ? (b.Read - a.Read) / seconds : 0;
            r.WriteBps = b.Write >= a.Write ? (b.Write - a.Write) / seconds : 0;
            if (b.Ts100 > a.Ts100)
            {
                double idle = b.Idle >= a.Idle ? (double)(b.Idle - a.Idle) / (b.Ts100 - a.Ts100) : 1;
                r.Activity = Math.Max(0, Math.Min(100, 100 - idle * 100));
            }
            return r;
        }

        // "0 D: E:" -> 0
        public static int DiskIndex(string name)
        {
            int sp = name.IndexOf(' ');
            int idx;
            return int.TryParse(sp > 0 ? name.Substring(0, sp) : name, out idx) ? idx : -1;
        }

        public static string Letters(string name)
        {
            int sp = name.IndexOf(' ');
            return sp > 0 ? name.Substring(sp + 1).Trim() : "";
        }
    }
}
