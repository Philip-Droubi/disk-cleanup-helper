// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Made-up data for the /demo test switch, so pictures for the README and the website never show the real PC.
// Only snapshot runs use it (see the demo field in MainWindow.cs): nothing real is read, measured or saved.
using System;
using System.Collections.Generic;
using System.Windows.Media;
using DiskCleanupHelper.Cleanup;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Disks;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        const double DemoGB = 1073741824.0;

        // What a typical home PC shows, by English item name, with a made-up size in MB. Items that are always listed
        // (settings, info) are shown too, with their own size text.
        static readonly Dictionary<string, int> DemoSizesMB = new Dictionary<string, int>
        {
            { "Google Chrome cache", 1240 },
            { "Microsoft Edge cache", 486 },
            { "Firefox cache", 312 },
            { "Microsoft Teams cache", 655 },
            { "Discord cache", 218 },
            { "Graphics caches (DirectX, NVIDIA, AMD, Intel)", 742 },
            { "Your temp files (older than 1 day)", 2180 },
            { "Windows temp files (older than 1 day)", 964 },
            { "Windows Update downloads", 3420 },
            { "Windows error reports", 128 },
            { "Old Windows log files (older than 14 days)", 86 },
            { "Old update files of installed programs (keeps newest 3)", 1860 },
            { "Windows patch backup cache", 540 },
            { "Spotify cache and downloads", 1530 },
            { "Crash dump files", 820 },
            { "Recycle Bin (all drives)", 2640 },
            { "Turn off hibernation", 6380 },
            { "System Restore points", 9800 },
            { "Your Downloads folder", 14200 },
        };

        List<CleanupItem> DemoItems()
        {
            Dictionary<string, int> sizes = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> kv in DemoSizesMB) sizes[L.T(kv.Key)] = kv.Value;
            List<CleanupItem> list = new List<CleanupItem>();
            foreach (CleanupItem it in Catalog.BuildAll())
            {
                int mb;
                bool known = sizes.TryGetValue(it.Name, out mb);
                if (!known && !it.Always) continue;
                if (known && it.SizeText == null) it.Bytes = mb * 1048576L;
                list.Add(it);
            }
            return list;
        }

        // Shows the made-up sizes the way a finished measuring run does.
        void ApplyDemoSizes()
        {
            foreach (CleanupItem it in items) if (it.SizeLabel != null) it.SizeLabel.Text = it.SizeText ?? Fmt.Size(it.Bytes);
            UpdateGroupTotals();
        }

        // The header tiles: the same C: drive as DemoVolumes, and plenty of free memory.
        void ShowDemoStatus()
        {
            VolumeInfo c = DemoVolumes()[0];
            StatFree.Text = L.Ltr((c.Free / DemoGB).ToString("N1") + " GB");
            StatFreeSub.Text = L.F("of {0} GB", (c.Size / DemoGB).ToString("N0"));
            StatMem.Text = L.Ltr((9.3).ToString("N1") + " GB");
            StatMem.Foreground = Brushes.White;
            StatMemSub.Text = L.T("OK");
        }

        static List<VolumeInfo> DemoVolumes()
        {
            List<VolumeInfo> list = new List<VolumeInfo>();
            list.Add(DemoVolume("C", "", 476.3, 186.4));
            list.Add(DemoVolume("D", "Data", 455.2, 212.7));
            list.Add(DemoVolume("E", "Backup", 1863.0, 1402.6));
            return list;
        }

        static VolumeInfo DemoVolume(string letter, string label, double sizeGb, double freeGb)
        {
            VolumeInfo v = new VolumeInfo();
            v.Letter = letter; v.Label = label; v.FileSystem = "NTFS"; v.Health = "Healthy";
            v.Size = (long)(sizeGb * DemoGB); v.Free = (long)(freeGb * DemoGB);
            return v;
        }

        // A healthy NVMe SSD (drives C: and D:) and a healthy hard disk (drive E:).
        static List<DiskHealth> DemoDisks()
        {
            DiskHealth ssd = new DiskHealth();
            ssd.Index = 0; ssd.Model = "Samsung SSD 980 PRO 1TB"; ssd.Serial = "DEMO-SSD"; ssd.Media = "SSD"; ssd.Bus = "NVMe";
            ssd.Letters = "C: D:"; ssd.Size = 1000204886016; ssd.WindowsStatus = "Healthy"; ssd.Source = "NVMe"; ssd.TempFrom = "NVMe";
            ssd.HealthPct = 97; ssd.TempC = 41; ssd.WearUsedPct = 3; ssd.SpareLeftPct = 100; ssd.SmartIsNvme = true;
            ssd.PowerOnHours = 3218; ssd.PowerCycles = 1204; ssd.UnsafeShutdowns = 21; ssd.MediaErrors = 0;
            ssd.WrittenBytes = 28.4e12; ssd.ReadBytes = 41.7e12;

            DiskHealth hdd = new DiskHealth();
            hdd.Index = 1; hdd.Model = "WDC WD20SPZX-22UA7T0"; hdd.Serial = "DEMO-HDD"; hdd.Media = "HDD"; hdd.Bus = "SATA";
            hdd.Letters = "E:"; hdd.Size = 2000398934016; hdd.WindowsStatus = "Healthy"; hdd.Source = "SMART"; hdd.TempFrom = "SMART";
            hdd.HealthPct = 100; hdd.TempC = 34; hdd.PowerOnHours = 5870; hdd.PowerCycles = 2311;
            hdd.WrittenBytes = 9.6e12; hdd.ReadBytes = 14.2e12;

            List<DiskHealth> list = new List<DiskHealth>();
            list.Add(ssd);
            list.Add(hdd);
            return list;
        }

        // Three months of made-up readings, two a day, ending at the disk's current health and temperature.
        static List<HealthPoint> DemoHistory(DiskHealth d)
        {
            const int count = 180;
            List<HealthPoint> list = new List<HealthPoint>();
            Random random = new Random(11 + d.Index);
            DateTime start = DateTime.Now.AddHours(-12.0 * count);
            int health = d.HealthPct ?? 100, temp = d.TempC ?? 38;
            for (int i = 0; i < count; i++)
            {
                HealthPoint p = new HealthPoint();
                p.Time = start.AddHours(12.0 * (i + 1));
                p.Health = Math.Min(100, health + (i < count * 2 / 3 ? 1 : 0));
                p.Temp = (int)Math.Round(temp - 3 + 4 * Math.Sin(i / 9.0) + random.Next(-2, 3));
                list.Add(p);
            }
            if (list.Count > 0) list[list.Count - 1].Temp = temp;
            return list;
        }
    }
}
