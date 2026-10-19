// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Disk health tab: drive tiles, one card per disk (health, temperature, facts, history charts) and live temperature.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
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
        static readonly TimeSpan HistoryInterval = TimeSpan.FromMinutes(30);   // at most one saved reading per disk per half hour
        const int TempRefreshSeconds = 7;

        // The temperature on a disk card, updated in place by the live refresh.
        sealed class TempView
        {
            public TextBlock Value, Word;
        }

        readonly Dictionary<int, TempView> tempViews = new Dictionary<int, TempView>();   // by disk index
        DispatcherTimer tempTimer;
        bool tempReading;

        // ------------------------------------------------------------ reading the disks
        void ShowHealth(bool synchronous)
        {
            List<DiskHealth> disks = null;
            List<VolumeInfo> volumes = null;
            Action load = delegate
            {
                if (demo) { disks = DemoDisks(); volumes = DemoVolumes(); return; }   // nothing read, nothing saved to the history
                try { disks = DiskHealthReader.ReadAll(); } catch (Exception ex) { Log(L.F("Could not read disks: {0}", ex.Message)); disks = new List<DiskHealth>(); }
                try { volumes = DiskHealthReader.Volumes(); } catch { volumes = new List<VolumeInfo>(); }
                int added = HealthHistory.Record(disks, HistoryInterval);
                if (added > 0) Log(L.P(added, "Saved a health and temperature reading for 1 disk to the history.", "Saved a health and temperature reading for {0} disks to the history."));
            };
            Action show = delegate
            {
                lastHealth = disks;
                List<DiskHealth> shown = disks;
                if (options.SampleWd) { shown = new List<DiskHealth>(disks); shown.Insert(0, Program.SampleWd()); }   // test switch
                RenderHealth(shown, volumes);
            };
            if (synchronous) { load(); show(); return; }
            ShowHealthLoading(L.T("Reading each disk's health and temperature. This takes a few seconds."));
            RunWork(delegate(CancelFlag flag) { load(); }, show);
        }

        // Saves a reading when the app starts, so health and temperature are tracked even if the tab is never opened.
        void RecordHealthInBackground()
        {
            if (!Env.IsAdmin) return;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<DiskHealth> disks = DiskHealthReader.ReadAll();
                    int added = HealthHistory.Record(disks, HistoryInterval);
                    Ui(delegate
                    {
                        if (lastHealth == null) lastHealth = disks;
                        if (added > 0) Log(L.T("Saved today's disk health and temperature reading to the history."));
                    });
                }
                catch { }
            });
        }

        void RenderHealth(List<DiskHealth> disks, List<VolumeInfo> volumes)
        {
            HealthPanel.Children.Clear();
            tempViews.Clear();

            AddHealthHeading(L.T("Drives"));
            UniformGrid tiles = new UniformGrid();
            tiles.Columns = Math.Max(1, Math.Min(3, volumes.Count));
            tiles.Margin = new Thickness(0, 0, 0, 6);
            foreach (VolumeInfo v in volumes) tiles.Children.Add(DriveTile(v));
            HealthPanel.Children.Add(tiles);

            AddHealthHeading(L.T("Disks"));
            if (disks.Count == 0) HealthPanel.Children.Add(Text(L.T("Windows did not report any disks."), 13, "#64748B", false, false));
            foreach (DiskHealth d in disks) HealthPanel.Children.Add(DiskCard(d));
            TextBlock note = Text(L.T("Health % is this app's estimate from each disk's own data. Other tools use their own formulas, so their numbers can differ a little. A reading is saved each time you open this tab or start the app (at most every 30 minutes), so you can follow health and temperature over time."),
                                  12.5, "#64748B", false, true);
            note.Margin = new Thickness(2, 0, 0, 14);
            HealthPanel.Children.Add(note);

            RenderSmart(disks);   // the SMART tables live in their own tab

            healthShown = true;
            UpdateTempTimer();
            bool anyBad = disks.Any(x => (x.HealthPct.HasValue && x.HealthPct.Value < 50) || x.WindowsStatus == "Unhealthy" || x.PredictFailure);
            Log(anyBad ? L.T("A disk reports a problem. Back up your important files now.") : L.T("Disk check finished."));
        }

        void AddHealthHeading(string text)
        {
            TextBlock t = Text(text, 15, "#0F172A", true, false);
            t.Margin = new Thickness(2, 6, 0, 8);
            HealthPanel.Children.Add(t);
        }

        // ------------------------------------------------------------ drive tiles
        Border DriveTile(VolumeInfo v)
        {
            double usedPct = v.Size > 0 ? 100.0 * (v.Size - v.Free) / v.Size : 0;
            string color = usedPct >= 90 ? "#DC2626" : usedPct >= 75 ? "#D97706" : "#2563EB";

            Grid top = new Grid();
            top.ColumnDefinitions.Add(Col(Star));
            top.ColumnDefinitions.Add(Col(GridLength.Auto));
            string title = L.Ltr(v.Letter + ":");
            if (!string.IsNullOrEmpty(v.Label)) title += "  " + v.Label;
            if (string.Equals(v.Letter + ":", AppPaths.SysDrive, StringComparison.OrdinalIgnoreCase)) title += "  " + L.RtlPhrase(L.T("(Windows)"));
            TextBlock name = Text(title, 13.5, "#0F172A", true, false);
            name.TextTrimming = TextTrimming.CharacterEllipsis;
            top.Children.Add(name);
            TextBlock used = Text(L.F("{0}% used", usedPct.ToString("N0")), 12, color, true, false);
            used.Margin = new Thickness(8, 1, 0, 0);
            Grid.SetColumn(used, 1);
            top.Children.Add(used);

            StackPanel sp = new StackPanel();
            sp.Children.Add(top);
            sp.Children.Add(Bar(usedPct, color, new Thickness(0, 7, 0, 6)));
            string line = L.F("{0} free of {1}", Fmt.Size(v.Free), Fmt.Size(v.Size));
            if (!string.IsNullOrEmpty(v.FileSystem)) line += "   ·   " + v.FileSystem;
            sp.Children.Add(Text(line, 12.5, "#475569", false, false));
            if (usedPct >= 90) sp.Children.Add(Spaced(Text(L.T("Almost full: Windows and apps slow down when a drive is this full."), 12, "#B91C1C", true, true), 4, 0));
            if (!string.IsNullOrEmpty(v.Health) && v.Health != "Healthy")
            {
                Border pill = VolumePill(v.Health);
                pill.Margin = new Thickness(0, 6, 0, 0);
                sp.Children.Add(pill);
            }
            Border tile = Card();
            tile.Padding = new Thickness(14, 10, 14, 10);
            tile.Margin = new Thickness(0, 0, 10, 10);
            tile.Child = sp;
            return tile;
        }

        static ProgressBar Bar(double percent, string color, Thickness margin)
        {
            ProgressBar bar = new ProgressBar();
            bar.Height = 6; bar.Minimum = 0; bar.Maximum = 100; bar.Value = percent;
            bar.BorderThickness = new Thickness(0);
            bar.Background = B("#E2E8F0");
            bar.Foreground = B(color);
            bar.Margin = margin;
            return bar;
        }

        // ------------------------------------------------------------ disk cards
        Border DiskCard(DiskHealth d)
        {
            Grid g = new Grid();
            g.ColumnDefinitions.Add(Col(new GridLength(160)));
            g.ColumnDefinitions.Add(Col(Star));
            g.Children.Add(HealthAndTemperature(d));
            FrameworkElement facts = DiskFacts(d);
            Grid.SetColumn(facts, 1);
            g.Children.Add(facts);

            Border line = new Border();
            line.Height = 1;
            line.Background = B("#E2E8F0");
            line.Margin = new Thickness(0, 14, 0, 10);

            StackPanel outer = new StackPanel();
            outer.Children.Add(g);
            outer.Children.Add(line);
            AddHistoryCharts(outer, d);
            Border card = Card();
            card.Padding = new Thickness(18, 14, 18, 14);
            card.Child = outer;
            return card;
        }

        StackPanel HealthAndTemperature(DiskHealth d)
        {
            string unknown = admin ? L.T("not reported") : L.T("needs admin");
            StackPanel left = new StackPanel();
            if (d.HealthPct.HasValue)
            {
                string color = HealthColor(d.HealthPct.Value);
                left.Children.Add(StatBlock(L.T("HEALTH"), L.Ltr(d.HealthPct.Value + "%"), color, HealthWord(d.HealthPct.Value)));
                left.Children.Add(Bar(d.HealthPct.Value, color, new Thickness(0, 0, 24, 14)));
            }
            else left.Children.Add(StatBlock(L.T("HEALTH"), "-", "#94A3B8", unknown));
            if (d.TempC.HasValue)
            {
                StackPanel temp = StatBlock(L.T("TEMPERATURE"), L.Ltr(d.TempC.Value + " °C"), TempColor(d), TempWord(d));
                TempView view = new TempView();
                view.Value = (TextBlock)temp.Children[1];
                view.Word = (TextBlock)temp.Children[2];
                tempViews[d.Index] = view;
                left.Children.Add(temp);
            }
            else left.Children.Add(StatBlock(L.T("TEMPERATURE"), "-", "#94A3B8", unknown));
            return left;
        }

        // Name, type, label/value facts, problems and where the data came from.
        StackPanel DiskFacts(DiskHealth d)
        {
            StackPanel mid = new StackPanel();
            mid.Margin = new Thickness(8, 0, 0, 0);
            Grid top = new Grid();
            top.ColumnDefinitions.Add(Col(Star));
            top.ColumnDefinitions.Add(Col(GridLength.Auto));
            top.Children.Add(Piece(d.Model, 16, "#0F172A", true, true));
            if (!string.IsNullOrEmpty(d.WindowsStatus))
            {
                Border pill = HealthPill(d.WindowsStatus);
                Grid.SetColumn(pill, 1);
                top.Children.Add(pill);
            }
            mid.Children.Add(top);
            string kind = MediaText(d.Media) + (string.IsNullOrEmpty(d.Bus) ? "" : ", " + BusText(d.Bus));
            string drives = string.IsNullOrEmpty(d.Letters) ? "" : ", " + L.F("drive {0}", d.Letters);
            // right-to-left: drive letters before the size, so "D: E:" is never last on the line (it would flip)
            string info = L.Rtl ? kind + drives + ", " + Fmt.Size(d.Size) : kind + ", " + Fmt.Size(d.Size) + drives;
            mid.Children.Add(Spaced(Text(info, 13, "#475569", false, true), 2, 10));

            Grid facts = new Grid();
            facts.ColumnDefinitions.Add(Col(new GridLength(190)));
            facts.ColumnDefinitions.Add(Col(Star));
            if (d.PowerOnHours.HasValue) AddFact(facts, L.T("Powered on"), L.F("{0} hours  (about {1} days)", d.PowerOnHours.Value.ToString("N0"), (d.PowerOnHours.Value / 24).ToString("N0")));
            if (d.PowerCycles.HasValue) AddFact(facts, L.T("Turned on"), L.F("{0} times", d.PowerCycles.Value.ToString("N0")));
            if (d.WrittenBytes.HasValue) AddFact(facts, L.T("Data written over its life"), Fmt.LargeSize(d.WrittenBytes.Value));
            if (d.ReadBytes.HasValue) AddFact(facts, L.T("Data read over its life"), Fmt.LargeSize(d.ReadBytes.Value));
            if (d.WearUsedPct.HasValue && d.Media != "HDD") AddFact(facts, L.T("Rated write life used"), L.Ltr(d.WearUsedPct.Value + "%"));
            if (d.SpareLeftPct.HasValue) AddFact(facts, L.T("Spare space left"), L.Ltr(d.SpareLeftPct.Value + "%"));
            if (d.UnsafeShutdowns.HasValue && d.UnsafeShutdowns.Value > 0) AddFact(facts, L.T("Unexpected power losses"), d.UnsafeShutdowns.Value.ToString("N0"));
            if (facts.RowDefinitions.Count > 0) mid.Children.Add(facts);

            if (d.Problems.Count > 0) mid.Children.Add(ProblemsBox(d));
            else if (d.Source != "") mid.Children.Add(Spaced(Text(L.T("✓ No problems found."), 13, "#15803D", true, false), 8, 0));
            foreach (string n in d.Notes) mid.Children.Add(Spaced(Text(n, 12.5, "#475569", false, true), 4, 0));
            string source = d.Source == "SMART" ? L.T("Read from the disk's own self-monitoring data (SMART). The full list is in the SMART details tab.")
                          : d.Source == "NVMe" ? L.T("Read from the SSD's own health log (NVMe). The full list is in the SMART details tab.")
                          : d.Source == "Windows" ? L.T("From Windows' storage information.")
                          : admin ? L.T("This disk does not share detailed health data with Windows (common for USB enclosures and RAID).")
                          : L.T("Start the tool with admin rights to read health and temperature.");
            mid.Children.Add(Spaced(Text(source, 12, "#64748B", false, true), 8, 0));
            return mid;
        }

        void AddFact(Grid facts, string label, string value)
        {
            int row = facts.RowDefinitions.Count;
            RowDefinition rd = new RowDefinition();
            rd.Height = GridLength.Auto;
            facts.RowDefinitions.Add(rd);
            TextBlock name = Text(label, 13, "#64748B", false, true);   // long translations wrap instead of being cut
            name.Margin = new Thickness(0, 3, 8, 3);
            Grid.SetRow(name, row);
            FrameworkElement shown = ValueText(value, 13.5, "#0F172A", true, true);
            shown.Margin = new Thickness(0, 3, 0, 3);
            Grid.SetRow(shown, row);
            Grid.SetColumn(shown, 1);
            facts.Children.Add(name);
            facts.Children.Add(shown);
        }

        // Red when the disk is at risk, amber for things to watch.
        static Border ProblemsBox(DiskHealth d)
        {
            bool severe = !(d.HealthPct.HasValue && d.HealthPct.Value >= 80 && !d.PredictFailure);
            StackPanel lines = new StackPanel();
            foreach (string p in d.Problems)
            {
                TextBlock t = Text("• " + p, 13, severe ? "#991B1B" : "#92400E", false, true);
                t.LineHeight = 19;
                lines.Children.Add(t);
            }
            Border box = new Border();
            box.Background = B(severe ? "#FEE2E2" : "#FEF3C7");
            box.BorderBrush = B(severe ? "#FCA5A5" : "#FCD34D");
            box.BorderThickness = new Thickness(1);
            box.CornerRadius = new CornerRadius(8);
            box.Padding = new Thickness(12, 8, 12, 8);
            box.Margin = new Thickness(0, 10, 0, 2);
            box.Child = lines;
            return box;
        }

        // Health and temperature history side by side, from the saved readings.
        void AddHistoryCharts(StackPanel outer, DiskHealth d)
        {
            List<HealthPoint> history = demo ? DemoHistory(d) : options.ManyPoints ? SampleHistory(3000) : HealthHistory.Load(d.Key);
            MiniChart healthChart = new MiniChart();
            healthChart.MaxY = 100; healthChart.Unit = "%"; healthChart.TipFormat = "0";
            healthChart.Series.Add(history.Where(p => p.Health.HasValue).Select(p => (double)p.Health.Value).ToArray());
            healthChart.Times = history.Where(p => p.Health.HasValue).Select(p => p.Time).ToArray();
            healthChart.Colors.Add(B(d.HealthPct.HasValue ? HealthColor(d.HealthPct.Value) : "#2563EB"));
            MiniChart tempChart = new MiniChart();
            tempChart.MaxY = 60; tempChart.AutoMax = true; tempChart.Unit = "°"; tempChart.TipUnit = " °C"; tempChart.TipFormat = "0";
            tempChart.Series.Add(history.Where(p => p.Temp.HasValue).Select(p => (double)p.Temp.Value).ToArray());
            tempChart.Times = history.Where(p => p.Temp.HasValue).Select(p => p.Time).ToArray();
            tempChart.Colors.Add(B("#F59E0B"));
            if (history.Count > 1)
            {
                healthChart.LeftLabel = tempChart.LeftLabel = history[0].Time.ToString("d");
                healthChart.RightLabel = tempChart.RightLabel = history[history.Count - 1].Time.ToString("d");
            }
            Grid charts = new Grid();
            charts.ColumnDefinitions.Add(Col(Star));
            charts.ColumnDefinitions.Add(Col(Star));
            StackPanel healthBox = ChartBox(L.T("Health history"), healthChart, 90, null);
            healthBox.Margin = new Thickness(0, 0, 14, 0);
            charts.Children.Add(healthBox);
            StackPanel tempBox = ChartBox(L.T("Temperature history"), tempChart, 90, null);
            Grid.SetColumn(tempBox, 1);
            charts.Children.Add(tempBox);
            outer.Children.Add(charts);
            string since = history.Count == 0 ? L.T("No readings saved yet.")
                         : history.Count == 1 ? L.F("1 reading so far, on {0}. The charts grow as more readings are saved.", history[0].Time.ToString("g"))
                         : L.F("{0} readings since {1}.", history.Count, history[0].Time.ToString("d"));
            outer.Children.Add(Spaced(Text(since, 12, "#64748B", false, true), 2, 0));
        }

        // ------------------------------------------------------------ live temperature
        // Every few seconds while the tab is open, only the temperatures are read again and updated in place.
        void UpdateTempTimer()
        {
            bool on = Tabs.SelectedIndex == Tab.Health && healthShown && tempViews.Count > 0 && options.SnapshotPath == null;
            if (on && tempTimer == null)
            {
                tempTimer = new DispatcherTimer();
                tempTimer.Interval = TimeSpan.FromSeconds(TempRefreshSeconds);
                tempTimer.Tick += delegate { RefreshTemps(); };
            }
            if (tempTimer != null) { if (on) tempTimer.Start(); else tempTimer.Stop(); }
        }

        void RefreshTemps()
        {
            if (tempReading || lastHealth == null) return;
            List<DiskHealth> disks = lastHealth.Where(x => x.TempC.HasValue && tempViews.ContainsKey(x.Index)).ToList();
            if (disks.Count == 0) return;
            tempReading = true;
            ThreadPool.QueueUserWorkItem(delegate
            {
                Dictionary<int, int> temps = null;
                try { temps = DiskHealthReader.ReadTemperatures(disks); } catch { }
                Ui(delegate
                {
                    tempReading = false;
                    if (temps == null) return;
                    foreach (DiskHealth d in disks)
                    {
                        int celsius;
                        TempView view;
                        if (!temps.TryGetValue(d.Index, out celsius) || d.TempC == celsius || !tempViews.TryGetValue(d.Index, out view)) continue;
                        d.TempC = celsius;
                        Brush color = B(TempColor(d));
                        view.Value.Text = L.Ltr(celsius + " °C");
                        view.Value.Foreground = color;
                        view.Word.Text = TempWord(d);
                        view.Word.Foreground = color;
                    }
                });
            });
        }

        // ------------------------------------------------------------ words and colours
        static string HealthColor(int pct) { return pct >= 80 ? "#16A34A" : pct >= 50 ? "#D97706" : "#DC2626"; }
        static string HealthWord(int pct) { return pct >= 80 ? L.T("Good") : pct >= 50 ? L.T("Fair") : L.T("Poor"); }

        // 0 = normal, 1 = warm, 2 = too hot. Hard disks run cooler than SSDs.
        static int TempLevel(DiskHealth d)
        {
            int t = d.TempC.Value;
            bool hdd = d.Media == "HDD";
            if (t >= (hdd ? 55 : 75)) return 2;
            if (t >= (hdd ? 45 : 60)) return 1;
            return 0;
        }

        static string TempColor(DiskHealth d) { return new[] { "#16A34A", "#D97706", "#DC2626" }[TempLevel(d)]; }
        static string TempWord(DiskHealth d) { return new[] { L.T("Normal"), L.T("Warm"), L.T("Too hot") }[TempLevel(d)]; }

        // Disk type and connection in the user's language (the code itself compares the English values).
        static string MediaText(string media) { return media == "Disk" ? L.T("Disk") : media; }

        static string BusText(string bus)
        {
            switch (bus)
            {
                case "SD card": return L.T("SD card");
                case "Virtual": return L.T("Virtual");
                case "Storage Spaces": return L.T("Storage Spaces");
                default: return bus;
            }
        }

        static Border HealthPill(string status)
        {
            switch (status)
            {
                case "Healthy": return Pill(L.T("Windows: OK"), "#15803D", "#DCFCE7");
                case "Warning": return Pill(L.T("Windows: warning"), "#B45309", "#FEF3C7");
                case "Unhealthy": return Pill(L.T("Windows: problem"), "#B91C1C", "#FEE2E2");
                default: return Pill(string.IsNullOrEmpty(status) ? L.T("Unknown") : status, "#475569", "#E2E8F0");
            }
        }

        static Border VolumePill(string status)
        {
            switch (status)
            {
                case "Healthy": return Pill(L.T("Healthy"), "#15803D", "#DCFCE7");
                case "Warning": return Pill(L.T("Warning"), "#B45309", "#FEF3C7");
                case "Unhealthy": return Pill(L.T("Problem"), "#B91C1C", "#FEE2E2");
                default: return Pill(string.IsNullOrEmpty(status) ? L.T("Unknown") : status, "#475569", "#E2E8F0");
            }
        }
    }
}
