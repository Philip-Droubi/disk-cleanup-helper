// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Program start: runs a command-line mode (install, uninstall, test reports) or opens the main window.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using DiskCleanupHelper.Cleanup;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Device;
using DiskCleanupHelper.Disks;
using DiskCleanupHelper.Install;
using DiskCleanupHelper.Localization;
using DiskCleanupHelper.UI;

namespace DiskCleanupHelper.App
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            AppOptions o = AppOptions.Parse(args);
            AppPaths.Init(o);
            if (!string.IsNullOrEmpty(o.UpdateFeed))
                Updater.FeedFile = o.UpdateFeed.Contains("://") ? o.UpdateFeed : System.IO.Path.GetFullPath(o.UpdateFeed);
            L.Init(o.Lang);
            try
            {
                if (o.Install) { Installer.InstallWithMessages(null); return 0; }
                if (o.Uninstall) { Installer.UninstallWithMessages(null, true); return 0; }
                if (o.SelfTestPath != null) return SelfTest(o.SelfTestPath);
                if (o.SmartSamplePath != null)
                {
                    // For testing: health and SMART table worked out from a real disk's values (see SampleWd).
                    DiskHealth d = SampleWd();
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.AppendLine("Health " + d.HealthPct + "%, temperature " + d.TempC + " C, power on " + d.PowerOnHours + " h, power cycles " + d.PowerCycles +
                                  ", written " + Fmt.Size(d.WrittenBytes) + ", read " + Fmt.Size(d.ReadBytes));
                    foreach (string p in d.Problems) sb.AppendLine("Problem: " + p);
                    foreach (string n in d.Notes) sb.AppendLine("Note: " + n);
                    foreach (SmartRow r in d.SmartRows)
                        sb.AppendLine(string.Format("{0,4} {1,-36} thr {2,3} val {3,3} worst {4,3}  {5,-14} {6}  {7}", r.No, r.Name, r.Threshold, r.Value, r.Worst, r.Status, r.Data, r.Raw));
                    System.IO.File.WriteAllText(o.SmartSamplePath, sb.ToString());
                    return 0;
                }
                if (o.TempsPath != null)
                {
                    // For testing: each disk's temperature from the full check, then three quick temperature-only reads.
                    List<DiskHealth> disks = DiskHealthReader.ReadAll();
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    foreach (DiskHealth d in disks) sb.AppendLine("Disk " + d.Index + " " + d.Model + ": full check " + (d.TempC.HasValue ? d.TempC + " C" : "-") + " (from " + (d.TempFrom == "" ? "-" : d.TempFrom) + ")");
                    for (int i = 1; i <= 3; i++)
                    {
                        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                        Dictionary<int, int> t = DiskHealthReader.ReadTemperatures(disks.Where(x => x.TempC.HasValue).ToList());
                        sb.AppendLine("Quick read " + i + " (" + sw.ElapsedMilliseconds + " ms): " + string.Join(", ", t.Select(kv => "disk " + kv.Key + " " + kv.Value + " C")));
                        if (i < 3) System.Threading.Thread.Sleep(2000);
                    }
                    System.IO.File.WriteAllText(o.TempsPath, sb.ToString());
                    return 0;
                }
                if (o.DeviceReportPath != null)
                {
                    // For testing: the Device details tab as text.
                    System.IO.File.WriteAllText(o.DeviceReportPath, DeviceInfoReader.Read().ToText(), System.Text.Encoding.UTF8);
                    return 0;
                }
                if (o.SpeedTestFolder != null)
                {
                    // For testing: a small speed test written to a text file next to the given folder's report path.
                    SpeedResult r = SpeedTest.Run(o.SpeedTestFolder, 256L * 1048576, new CancelFlag(), delegate(string m) { });
                    System.IO.File.WriteAllText(o.SpeedTestReport, string.Format("write {0:N0} MB/s, read {1:N0} MB/s, random 4K {2:N1} MB/s ({3:N0}/s), test file {4} MB",
                        r.WriteMBs, r.ReadMBs, r.RandomMBs, r.RandomIops, r.TestBytes / 1048576));
                    return 0;
                }
                if (Environment.OSVersion.Version.Major < 10)
                {
                    MessageBox.Show(L.T("Disk Cleanup Helper needs Windows 10 or Windows 11."), AppInfo.Title, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, L.MsgOptions);
                    return 1;
                }
                // Ask for admin rights. If the user says No, carry on without them (admin items are greyed out).
                if (!Env.IsAdmin && !o.Elevated && !o.NoElevate && o.SnapshotPath == null)
                {
                    if (Env.TryRelaunchElevated()) return 0;
                }
                Application app = new Application();
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                app.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
                {
                    MessageBox.Show(L.F("Something went wrong:\r\n\r\n{0}", e.Exception.Message), AppInfo.Title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, L.MsgOptions);
                    e.Handled = true;
                };
                MainWindow main = new MainWindow(o);
                return app.Run(main.Window);
            }
            catch (Exception ex)
            {
                MessageBox.Show(L.F("Disk Cleanup Helper could not start:\r\n\r\n{0}", ex), AppInfo.Title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, L.MsgOptions);
                return 1;
            }
        }

        // For testing: the SMART values of a real WD10SPZX laptop disk (taken from Hard Disk Sentinel), as raw SMART bytes.
        // Hard Disk Sentinel rates this disk 98%.
        public static DiskHealth SampleWd()
        {
            long[][] a = {
                new long[] { 1, 51, 200, 194, 0x19 }, new long[] { 3, 21, 195, 184, 0x4C0 }, new long[] { 4, 0, 77, 77, 0x5B42 },
                new long[] { 5, 140, 200, 200, 0 }, new long[] { 7, 0, 200, 200, 0 }, new long[] { 9, 0, 86, 86, 0x288F },
                new long[] { 10, 0, 100, 100, 0 }, new long[] { 11, 0, 100, 100, 0 }, new long[] { 12, 0, 97, 97, 0xE23 },
                new long[] { 191, 0, 1, 1, 0x400F }, new long[] { 192, 0, 200, 200, 0x40 }, new long[] { 193, 0, 152, 152, 0x23C43 },
                new long[] { 194, 0, 108, 96, 0x23 }, new long[] { 196, 0, 200, 200, 0 }, new long[] { 197, 0, 200, 200, 4 },
                new long[] { 198, 0, 100, 253, 0 }, new long[] { 199, 0, 200, 200, 0 }, new long[] { 200, 0, 100, 253, 0 },
                new long[] { 240, 0, 95, 95, 0xED7 }, new long[] { 241, 0, 200, 200, 0x301F92D81 }, new long[] { 242, 0, 200, 200, 0x10C1FF908B },
                new long[] { 254, 0, 200, 200, 0 } };
            byte[] data = new byte[512], thr = new byte[512];
            for (int i = 0; i < a.Length; i++)
            {
                int o = 2 + i * 12;
                data[o] = (byte)a[i][0]; data[o + 3] = (byte)a[i][2]; data[o + 4] = (byte)a[i][3];
                for (int k = 0; k < 6; k++) data[o + 5 + k] = (byte)((a[i][4] >> (8 * k)) & 0xFF);
                thr[o] = (byte)a[i][0]; thr[o + 1] = (byte)a[i][1];
            }
            DiskHealth d = DiskHealthReader.FromSmartBytes(data, thr, "HDD");
            d.Model = "WDC WD10SPZX-75Z10T3 (test sample)"; d.Bus = "SATA"; d.Size = 1000204886016; d.WindowsStatus = "Healthy";
            d.Letters = "D: E:"; d.Serial = "TEST-SAMPLE";
            return d;
        }

        // For testing: measures every cleanup item and writes the results to a text file. Never deletes anything.
        static int SelfTest(string outFile)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            System.Diagnostics.Stopwatch total = System.Diagnostics.Stopwatch.StartNew();
            sb.AppendLine("Disk Cleanup Helper " + AppInfo.Version + " self-test. Admin: " + Env.IsAdmin + ". Windows build " + Env.WindowsBuild);
            System.Collections.Generic.List<CleanupItem> items = Catalog.Build();
            sb.AppendLine(items.Count + " items found on this PC.");
            foreach (CleanupItem it in items)
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                string result;
                try
                {
                    // same rule as the window: items with a fixed text and no custom size are not measured
                    if (it.SizeText != null && it.Size == null) { }
                    else it.Bytes = it.Size != null ? it.Size(it, null) : Catalog.MeasureDefault(it, null);
                    result = it.SizeText ?? Fmt.Size(it.Bytes);
                }
                catch (Exception ex) { result = "ERROR " + ex.GetType().Name + ": " + ex.Message; }
                sb.AppendLine(string.Format("{0,-9} {1,-58} {2,12}  {3,6:N1} s", it.Group, it.Name, result, sw.Elapsed.TotalSeconds));
            }
            sb.AppendLine(string.Format("Total {0:N1} s", total.Elapsed.TotalSeconds));
            sb.AppendLine("--- disk health");
            foreach (DiskHealth d in DiskHealthReader.ReadAll())
            {
                sb.AppendLine(string.Format("Disk {0}: {1} | {2} {3} | letters {4} | Windows {5} | source '{6}' | health {7} | temp {8} | hours {9} | written {10} | problems {11}",
                    d.Index, d.Model, d.Media, d.Bus, d.Letters, d.WindowsStatus, d.Source,
                    d.HealthPct.HasValue ? d.HealthPct + "%" : "-", d.TempC.HasValue ? d.TempC + " C" : "-",
                    d.PowerOnHours.HasValue ? d.PowerOnHours.ToString() : "-", d.WrittenBytes.HasValue ? Fmt.Size(d.WrittenBytes) : "-", d.Problems.Count));
            }
            sb.AppendLine("--- disk activity counters");
            foreach (System.Collections.Generic.KeyValuePair<string, DiskRaw> kv in DiskPerf.ReadRaw())
                sb.AppendLine(string.Format("{0}: read since boot {1}, written since boot {2}", kv.Key, Fmt.Size(kv.Value.Read), Fmt.Size(kv.Value.Write)));
            System.IO.File.WriteAllText(outFile, sb.ToString());
            return 0;
        }
    }
}
