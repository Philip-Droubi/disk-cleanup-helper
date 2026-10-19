// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Disk performance tab: live activity of the chosen disk (tiles, two charts, a summary) and the speed test.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Disks;
using DiskCleanupHelper.Localization;
using DiskCleanupHelper.UI.Controls;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        const int PerfSeconds = 120;   // the charts show the last two minutes, one sample per second

        // The texts on the tab that change.
        sealed class PerfLabels
        {
            public TextBlock Read, Write, Total, Activity, AverageRate, HighestRate, AverageActivity,
                             ReadSinceBoot, WrittenSinceBoot, WrittenLifetime, SpeedResult, SpeedLast;
        }

        // Samples of the chosen disk since the tab opened (the chart lists keep only the last PerfSeconds).
        sealed class PerfSamples
        {
            public readonly List<double> Activity = new List<double>(), ReadMBs = new List<double>(), WriteMBs = new List<double>();
            public readonly List<DateTime> Times = new List<DateTime>();
            public double SumRate, HighestRate, SumActivity;
            public int Count;

            public void Add(DiskRate r)
            {
                Keep(Activity, r.Activity);
                Keep(ReadMBs, r.ReadBps / 1048576.0);
                Keep(WriteMBs, r.WriteBps / 1048576.0);
                Times.Add(DateTime.Now);
                if (Times.Count > PerfSeconds) Times.RemoveAt(0);
                double total = r.ReadBps + r.WriteBps;
                Count++;
                SumRate += total;
                SumActivity += r.Activity;
                if (total > HighestRate) HighestRate = total;
            }

            public void Clear()
            {
                Activity.Clear(); ReadMBs.Clear(); WriteMBs.Clear(); Times.Clear();
                SumRate = HighestRate = SumActivity = 0;
                Count = 0;
            }

            static void Keep(List<double> list, double value)
            {
                list.Add(value);
                if (list.Count > PerfSeconds) list.RemoveAt(0);
            }
        }

        DispatcherTimer perfTimer;
        bool perfBuilt, perfQuerying;
        string perfDisk;                          // counter instance name of the chosen disk ("1 C:")
        Dictionary<string, DiskRaw> perfLast;     // previous reading; rates are the difference to it
        readonly PerfSamples perfSamples = new PerfSamples();
        PerfLabels perfText;
        MiniChart activityChart, rateChart;

        // ------------------------------------------------------------ opening the tab
        // The first query of Windows' disk counters can take several seconds when Windows has not started them yet,
        // so the tab is filled in the background while a loading view shows. Snapshots fill it synchronously.
        void EnsurePerfTab(bool synchronous)
        {
            if (perfBuilt) return;
            perfBuilt = true;
            List<DiskHealth> health = lastHealth;
            if (synchronous)
            {
                string error;
                Dictionary<string, DiskRaw> first = ReadCounters(out error);
                FinishPerfTab(first, error, first != null ? ModelsByIndex(health) : null);
                return;
            }
            ShowPerfLoading();
            ThreadPool.QueueUserWorkItem(delegate
            {
                string error;
                Dictionary<string, DiskRaw> first = ReadCounters(out error);
                Dictionary<int, string> models = first != null ? ModelsByIndex(health) : null;
                Ui(delegate { FinishPerfTab(first, error, models); });
            });
        }

        static Dictionary<string, DiskRaw> ReadCounters(out string error)
        {
            error = null;
            try { return DiskPerf.ReadRaw(); }
            catch (Exception ex) { error = ex.Message; return null; }
        }

        // Disk model names by disk number: from the last disk check, or asked from Windows.
        static Dictionary<int, string> ModelsByIndex(List<DiskHealth> health)
        {
            Dictionary<int, string> models = new Dictionary<int, string>();
            if (health != null) { foreach (DiskHealth d in health) models[d.Index] = d.Model; return models; }
            try
            {
                using (System.Management.ManagementObjectSearcher s = new System.Management.ManagementObjectSearcher("SELECT Index, Model FROM Win32_DiskDrive"))
                {
                    foreach (System.Management.ManagementBaseObject mo in s.Get()) models[Convert.ToInt32(mo["Index"])] = Convert.ToString(mo["Model"]).Trim();
                }
            }
            catch { }
            return models;
        }

        void FinishPerfTab(Dictionary<string, DiskRaw> first, string error, Dictionary<int, string> models)
        {
            if (first == null)
            {
                PerfPanel.Children.Clear();
                StatusText3.Text = L.F("Windows' disk activity counters are not available on this PC: {0}", error);
                return;
            }
            FillDiskChoices(first, models);
            BuildPerfPanel();   // replaces the loading view
            SelectPerfDisk();
            ApplyPerf(first);   // totals since Windows started now, and the base for the first rates a second later
            CmbPerfDisk.SelectionChanged += delegate { SelectPerfDisk(); };
            perfTimer = new DispatcherTimer();
            perfTimer.Interval = TimeSpan.FromSeconds(1);
            perfTimer.Tick += delegate { PerfTick(); };
            if (Tabs.SelectedIndex == Tab.Performance) perfTimer.Start();
        }

        void FillDiskChoices(Dictionary<string, DiskRaw> counters, Dictionary<int, string> models)
        {
            foreach (string name in counters.Keys.OrderBy(n => DiskPerf.DiskIndex(n)))
            {
                int index = DiskPerf.DiskIndex(name);
                string model;
                if (!models.TryGetValue(index, out model)) model = L.T("Disk");
                string letters = DiskPerf.Letters(name);
                ComboBoxItem item = new ComboBoxItem();
                // right-to-left: drive letters before the model, so the brackets are never last on the line
                if (L.Rtl) item.Content = L.F("Disk {0}: {1}", index, (letters.Length > 0 ? "(" + letters + ")    " : "") + model);
                else item.Content = L.F("Disk {0}: {1}", index, model) + (letters.Length > 0 ? "    (" + letters + ")" : "");
                item.Tag = name;
                CmbPerfDisk.Items.Add(item);
                if (letters.IndexOf(AppPaths.SysDrive, StringComparison.OrdinalIgnoreCase) >= 0) CmbPerfDisk.SelectedItem = item;
            }
            if (CmbPerfDisk.SelectedItem == null && CmbPerfDisk.Items.Count > 0) CmbPerfDisk.SelectedIndex = 0;
        }

        // ------------------------------------------------------------ the page
        void BuildPerfPanel()
        {
            perfText = new PerfLabels();
            PerfPanel.Children.Clear();
            PerfPanel.Children.Add(RightNowCard());
            PerfPanel.Children.Add(PerfCharts());
            PerfPanel.Children.Add(SummaryCard());
            PerfPanel.Children.Add(SpeedTestCard());
        }

        Border RightNowCard()
        {
            Grid tiles = new Grid();
            for (int i = 0; i < 4; i++) tiles.ColumnDefinitions.Add(Col(Star));
            AddTile(tiles, 0, L.T("READ"), "#2563EB", out perfText.Read);
            AddTile(tiles, 1, L.T("WRITE"), "#D97706", out perfText.Write);
            AddTile(tiles, 2, L.T("TOTAL TRANSFER"), "#0F172A", out perfText.Total);
            AddTile(tiles, 3, L.T("DISK ACTIVITY"), "#0891B2", out perfText.Activity);
            StackPanel sp = new StackPanel();
            sp.Children.Add(Spaced(Text(L.T("Right now"), 15, "#0F172A", true, false), 0, 8));
            sp.Children.Add(tiles);
            Border card = Card();
            card.Padding = new Thickness(18, 14, 18, 14);
            card.Child = sp;
            return card;
        }

        void AddTile(Grid tiles, int column, string label, string color, out TextBlock value)
        {
            StackPanel sp = new StackPanel();
            sp.Children.Add(Text(label, 11, "#64748B", true, false));
            value = Text("-", 22, color, true, false);
            sp.Children.Add(value);
            Border tile = new Border();
            tile.Background = B("#F8FAFC");
            tile.CornerRadius = new CornerRadius(8);
            tile.Padding = new Thickness(14, 10, 14, 10);
            tile.Margin = new Thickness(0, 0, column == 3 ? 0 : 10, 0);
            tile.Child = sp;
            Grid.SetColumn(tile, column);
            tiles.Children.Add(tile);
        }

        Grid PerfCharts()
        {
            activityChart = new MiniChart();
            activityChart.MaxY = 100; activityChart.Unit = "%"; activityChart.Capacity = PerfSeconds;
            activityChart.TipFormat = "0"; activityChart.TimeFormat = "T";
            activityChart.LeftLabel = L.T("2 minutes ago"); activityChart.RightLabel = L.T("now");
            activityChart.Colors.Add(B("#0891B2"));
            rateChart = new MiniChart();
            rateChart.MaxY = 1; rateChart.AutoMax = true; rateChart.Capacity = PerfSeconds;
            rateChart.TipUnit = " MB/s"; rateChart.TipFormat = "0.##"; rateChart.TimeFormat = "T";
            rateChart.LeftLabel = L.T("2 minutes ago"); rateChart.RightLabel = L.T("now");
            rateChart.Colors.Add(B("#2563EB"));
            rateChart.Colors.Add(B("#F59E0B"));

            Border left = Card();
            left.Padding = new Thickness(16, 12, 16, 10);
            left.Margin = new Thickness(0, 0, 6, 12);
            left.Child = ChartBox(L.T("Disk activity %"), activityChart, 150, null);
            Border right = Card();
            right.Padding = new Thickness(16, 12, 16, 10);
            right.Margin = new Thickness(6, 0, 0, 12);
            right.Child = ChartBox(L.T("Transfer rate MB/s   (blue = read, orange = write)"), rateChart, 150, null);
            Grid.SetColumn(right, 1);
            Grid charts = new Grid();
            charts.ColumnDefinitions.Add(Col(Star));
            charts.ColumnDefinitions.Add(Col(Star));
            charts.Children.Add(left);
            charts.Children.Add(right);
            return charts;
        }

        Border SummaryCard()
        {
            StackPanel sp = new StackPanel();
            sp.Children.Add(Spaced(Text(L.T("Summary"), 15, "#0F172A", true, false), 0, 8));
            sp.Children.Add(SummaryRow(L.T("Average transfer rate (since this tab opened)"), out perfText.AverageRate));
            sp.Children.Add(SummaryRow(L.T("Highest transfer rate seen"), out perfText.HighestRate));
            sp.Children.Add(SummaryRow(L.T("Average disk activity"), out perfText.AverageActivity));
            sp.Children.Add(SummaryRow(L.T("Read since Windows started"), out perfText.ReadSinceBoot));
            sp.Children.Add(SummaryRow(L.T("Written since Windows started"), out perfText.WrittenSinceBoot));
            sp.Children.Add(SummaryRow(L.T("Written over the disk's whole life"), out perfText.WrittenLifetime));
            Border card = Card();
            card.Padding = new Thickness(18, 14, 18, 10);
            card.Child = sp;
            return card;
        }

        Grid SummaryRow(string label, out TextBlock value)
        {
            Grid g = new Grid();
            g.ColumnDefinitions.Add(Col(new GridLength(260)));
            g.ColumnDefinitions.Add(Col(Star));
            g.Margin = new Thickness(0, 0, 0, 5);
            TextBlock name = Text(label, 13, "#475569", false, true);   // long translations wrap instead of being cut
            name.Margin = new Thickness(0, 0, 8, 0);
            g.Children.Add(name);
            value = Text("-", 13, "#0F172A", true, false);
            Grid.SetColumn(value, 1);
            g.Children.Add(value);
            return g;
        }

        Border SpeedTestCard()
        {
            StackPanel sp = new StackPanel();
            sp.Children.Add(Spaced(Text(L.T("Speed test"), 15, "#0F172A", true, false), 0, 6));
            sp.Children.Add(Spaced(Text(L.T("Measures how fast the disk can really write and read: it writes a temporary test file, reads it back, then reads small random pieces, and deletes the file. Click \"Run speed test\" at the top."), 12.5, "#475569", false, true), 0, 8));
            perfText.SpeedResult = Text(L.T("Not run yet."), 14, "#0F172A", true, true);
            sp.Children.Add(perfText.SpeedResult);
            perfText.SpeedLast = Text("", 12, "#64748B", false, true);
            sp.Children.Add(Spaced(perfText.SpeedLast, 4, 4));
            sp.Children.Add(Text(L.T("For comparison: laptop hard disks reach about 80 to 140 MB/s, SATA SSDs about 500 MB/s, and NVMe SSDs 1,500 to 7,000 MB/s."), 12, "#94A3B8", false, true));
            Border card = Card();
            card.Padding = new Thickness(18, 14, 18, 14);
            card.Child = sp;
            return card;
        }

        // ------------------------------------------------------------ live samples
        void SelectPerfDisk()
        {
            ComboBoxItem item = CmbPerfDisk.SelectedItem as ComboBoxItem;
            perfDisk = item == null ? null : (string)item.Tag;
            perfLast = null;
            perfSamples.Clear();
            perfText.WrittenLifetime.Text = L.T("not reported by this disk");
            if (perfDisk != null && lastHealth != null)
            {
                int index = DiskPerf.DiskIndex(perfDisk);
                foreach (DiskHealth d in lastHealth)
                    if (d.Index == index && d.WrittenBytes.HasValue) perfText.WrittenLifetime.Text = Fmt.LargeSize(d.WrittenBytes.Value);
            }
            SpeedRecord last = SpeedHistory.Last(PerfDiskKey());
            perfText.SpeedResult.Text = L.T("Not run yet.");
            perfText.SpeedLast.Text = last == null ? "" : L.F("Last test on {0}: write {1} MB/s, read {2} MB/s, random 4K read {3} MB/s.", last.Time.ToString("g"), last.WriteMBs.ToString("N0"), last.ReadMBs.ToString("N0"), last.RandomMBs.ToString("N1"));
            RedrawPerf();
        }

        // Speed results are saved under the disk's display name.
        string PerfDiskKey()
        {
            ComboBoxItem item = CmbPerfDisk.SelectedItem as ComboBoxItem;
            return item == null ? "" : Convert.ToString(item.Content);
        }

        void PerfTick()
        {
            if (perfQuerying || perfDisk == null) return;
            perfQuerying = true;
            ThreadPool.QueueUserWorkItem(delegate
            {
                string error;
                Dictionary<string, DiskRaw> reading = ReadCounters(out error);
                Ui(delegate { perfQuerying = false; if (reading != null) ApplyPerf(reading); });
            });
        }

        void ApplyPerf(Dictionary<string, DiskRaw> reading)
        {
            DiskRaw now;
            if (perfDisk == null || !reading.TryGetValue(perfDisk, out now)) return;
            DiskRaw before;
            if (perfLast != null && perfLast.TryGetValue(perfDisk, out before))
            {
                DiskRate r = DiskPerf.Rate(before, now);
                perfSamples.Add(r);
                perfText.Read.Text = FmtRate(r.ReadBps);
                perfText.Write.Text = FmtRate(r.WriteBps);
                perfText.Total.Text = FmtRate(r.ReadBps + r.WriteBps);
                perfText.Activity.Text = L.Ltr(r.Activity.ToString("N0") + "%");
                perfText.AverageRate.Text = FmtRate(perfSamples.SumRate / perfSamples.Count);
                perfText.HighestRate.Text = FmtRate(perfSamples.HighestRate);
                perfText.AverageActivity.Text = L.Ltr((perfSamples.SumActivity / perfSamples.Count).ToString("N1") + "%");
            }
            perfText.ReadSinceBoot.Text = Fmt.LargeSize(now.Read);
            perfText.WrittenSinceBoot.Text = Fmt.LargeSize(now.Write);
            perfLast = reading;
            RedrawPerf();
        }

        void RedrawPerf()
        {
            if (activityChart == null) return;
            DateTime[] times = perfSamples.Times.ToArray();
            activityChart.Series.Clear();
            activityChart.Series.Add(perfSamples.Activity.ToArray());
            activityChart.Times = times;
            activityChart.Refresh();
            rateChart.Series.Clear();
            rateChart.Series.Add(perfSamples.ReadMBs.ToArray());
            rateChart.Series.Add(perfSamples.WriteMBs.ToArray());
            rateChart.Times = times;
            rateChart.Refresh();
        }

        static string FmtRate(double bytesPerSecond)
        {
            if (bytesPerSecond >= 1048576) return L.Ltr((bytesPerSecond / 1048576).ToString("N1") + " MB/s");
            return L.Ltr((bytesPerSecond / 1024).ToString("N0") + " KB/s");
        }

        // ------------------------------------------------------------ speed test
        void StartSpeedTest()
        {
            ComboBoxItem item = CmbPerfDisk.SelectedItem as ComboBoxItem;
            if (item == null) { Msg(L.T("Pick a disk first.")); return; }
            string letter = DiskPerf.Letters((string)item.Tag).Split(' ').FirstOrDefault(x => x.Length == 2 && x[1] == ':');
            if (string.IsNullOrEmpty(letter)) { Msg(L.T("This disk has no drive letter, so the speed test cannot write its test file.")); return; }
            string folder = string.Equals(letter, AppPaths.SysDrive, StringComparison.OrdinalIgnoreCase) ? Path.GetTempPath() : letter + "\\";
            long free = 0;
            try { free = new DriveInfo(letter).AvailableFreeSpace; } catch { }
            if (free < 2L * 1073741824) { Msg(L.F("Drive {0} needs at least 2 GB of free space for the speed test.", letter)); return; }
            long size = free > 8L * 1073741824 ? 1024L * 1048576 : 512L * 1048576;
            if (!AskYesNo(L.F("The speed test writes a {0} MB test file to drive {1}, reads it back, and deletes it.\r\nIt takes about 10 to 60 seconds. Other programs may feel slower meanwhile.\r\n\r\nStart the speed test?", size / 1048576, letter),
                          L.T("Speed test"))) return;
            string key = PerfDiskKey();
            perfText.SpeedResult.Text = L.T("Running...");
            Log(L.F("Speed test started on drive {0}.", letter));
            RunWork(delegate(CancelFlag flag)
            {
                SpeedResult r = SpeedTest.Run(folder, size, flag, delegate(string m) { Ui(delegate { StatusText3.Text = m; }); });
                SpeedHistory.Save(key, r);
                Ui(delegate
                {
                    perfText.SpeedResult.Text = L.F("Write {0:N0} MB/s     Read {1:N0} MB/s     Random 4K read {2:N1} MB/s ({3:N0} per second)", r.WriteMBs, r.ReadMBs, r.RandomMBs, r.RandomIops);
                    perfText.SpeedLast.Text = L.F("Tested with a {0} MB file on {1}. The result is saved, so you can compare later.", r.TestBytes / 1048576, DateTime.Now.ToString("g"));
                    StatusText3.Text = L.T("Speed test finished.");
                    Log(L.F("Speed test: {0}", perfText.SpeedResult.Text));
                });
            }, delegate
            {
                if (cancel.IsSet) { StatusText3.Text = L.T("Speed test stopped. The test file was deleted."); perfText.SpeedResult.Text = L.T("Stopped."); }
                else if (perfText.SpeedResult.Text == L.T("Running...")) perfText.SpeedResult.Text = L.T("The speed test could not finish. See the log below.");
            });
        }
    }
}
