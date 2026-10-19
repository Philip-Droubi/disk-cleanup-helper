// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Test switches (/snapshot and friends): saves a picture of a tab and closes, so layouts can be checked without
// clicking through the app. The switches are listed in docs/DEVELOPING.md ("Checking a change") and parsed in
// App/AppOptions.cs.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Cleanup;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Disks;
using DiskCleanupHelper.Localization;
using DiskCleanupHelper.UI.Controls;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        void TakeSnapshot()
        {
            if (options.TestScanRoot != null) SnapshotAnalyze();
            else SnapshotTab(options.SnapshotTab);
            if (options.Theme == "switch") { Theme.Dark = true; Theme.Apply(Window); UpdateThemeButton(); }   // switch live, after everything is built
            if (options.UpdateBar)
            {
                UpdateInfo next = new UpdateInfo();
                next.Version = new Version(Updater.Current.Major, Updater.Current.Minor + 1);
                ShowReady(next);
            }
            Window.UpdateLayout();
            if (options.SnapshotScroll > 0) ScrollOpenTab(options.SnapshotScroll);
            if (options.Hover >= 0)
            {
                foreach (MiniChart chart in VisualTree.Descendants<MiniChart>(Window)) chart.HoverAt(options.Hover);
                Window.UpdateLayout();
            }
            WaitUntilIdle();
            FrameworkElement content = (FrameworkElement)Window.Content;
            if (options.LangMenu)
            {
                ShowLanguageMenu();
                WaitUntilIdle();
                content = BtnLang.ContextMenu;
            }
            SavePicture(content, options.SnapshotPath);
            if (options.CheckLayout) SaveLayoutReport(content, options.SnapshotPath + ".txt");
            Window.Close();
        }

        // Lists every text that is cut off, so a sweep over all tabs and languages does not rely on eyes alone.
        void SaveLayoutReport(FrameworkElement content, string path)
        {
            List<string> lines = new List<string>();
            lines.Add(string.Format("{0}  tab {1}  {2:0}x{3:0}", L.Code, options.SnapshotTab, Window.ActualWidth, Window.ActualHeight));
            lines.AddRange(LayoutCheck.Find(content));
            File.WriteAllLines(path, lines);
        }

        void SnapshotAnalyze()
        {
            Tabs.SelectedIndex = Tab.Analyze;
            DriveScanner scanner = new DriveScanner();
            Stopwatch sw = Stopwatch.StartNew();
            try { scanner.Run(options.TestScanRoot, (long)options.TestMinMB * 1048576, new CancelFlag(), null); } catch { }
            FinishAnalyze(scanner, options.TestScanRoot, sw);
        }

        // Fills the tab synchronously (in normal use tabs load in the background).
        void SnapshotTab(int tab)
        {
            Tabs.SelectedIndex = tab;
            switch (tab)
            {
                case Tab.Cleanup:
                    if (demo) ApplyDemoSizes();
                    else if (options.Measure) MeasureAllNow();
                    if (demo || options.Measure)
                    {
                        // what a finished measuring run leaves on screen
                        Progress.Value = 100;
                        StatusText.Text = L.T("Done. Select what you want to clean.");
                        Log(L.T("Done. Click an item to read about it, select what you want, then click \"Clean selected\"."));
                        ShowDetails();
                    }
                    if (options.SelectSafe)
                    {
                        foreach (CleanupItem it in items) if (it.Safe && it.Check != null && it.Check.IsEnabled) it.Check.IsChecked = true;
                        UpdateSelection();
                    }
                    break;
                case Tab.Health:
                case Tab.Smart:
                    if (options.Loading) ShowHealthLoading(L.T("Reading each disk's health and temperature. This takes a few seconds."));
                    else ShowHealth(true);
                    break;
                case Tab.Performance:
                    if (perfTimer != null) perfTimer.Stop();
                    if (options.Loading) ShowPerfLoading();
                    else SamplePerformance();
                    break;
                case Tab.Device:
                    if (options.Loading) ShowDeviceLoading();
                    else ShowDevice(true);
                    break;
            }
        }

        void MeasureAllNow()
        {
            foreach (CleanupItem it in items)
            {
                if (it.SizeText != null && it.Size == null) continue;
                MeasureItem(it, new CancelFlag());
                if (it.SizeLabel != null) it.SizeLabel.Text = it.SizeText ?? Fmt.Size(it.Bytes);
            }
            UpdateGroupTotals();
        }

        // A few seconds of samples, so the live numbers and charts have data.
        void SamplePerformance()
        {
            for (int i = 0; i < 4; i++)
            {
                try { ApplyPerf(DiskPerf.ReadRaw()); } catch { }
                Thread.Sleep(1000);
            }
            try { ApplyPerf(DiskPerf.ReadRaw()); } catch { }
        }

        void ScrollOpenTab(double offset)
        {
            StackPanel[] panels = { ListPanel, null, HealthPanel, SmartPanel, PerfPanel, DevicePanel, HelpPanel, AuthorPanel };
            int tab = options.SnapshotTab;
            DependencyObject d = tab < panels.Length ? panels[tab] : null;
            while (d != null && !(d is ScrollViewer)) d = VisualTreeHelper.GetParent(d);
            ScrollViewer sv = d as ScrollViewer;
            if (sv != null) { sv.ScrollToVerticalOffset(offset); Window.UpdateLayout(); }
        }

        // Lets pending layout and Loaded events run. Endless animations would keep the window from ever going idle,
        // which is why they stay still in snapshot runs (see Animate in MainWindow.Loading.cs).
        void WaitUntilIdle()
        {
            Window.Dispatcher.Invoke(new Action(delegate { }), DispatcherPriority.ApplicationIdle);
        }

        void SavePicture(FrameworkElement content, string path)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)content.ActualWidth, (int)content.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(content);
            // A right-to-left window is mirrored on screen but not in a rendered bitmap; the menu mirrors itself.
            bool mirror = Window.FlowDirection == FlowDirection.RightToLeft && content == Window.Content;
            BitmapSource picture = mirror ? (BitmapSource)new TransformedBitmap(bitmap, new ScaleTransform(-1, 1)) : bitmap;
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(picture));
            using (FileStream fs = File.Create(path)) { encoder.Save(fs); }
        }

        // A long made-up history (one reading every 30 minutes) to check the charts with many points (/manypoints).
        static List<HealthPoint> SampleHistory(int count)
        {
            List<HealthPoint> list = new List<HealthPoint>();
            Random random = new Random(7);
            DateTime start = DateTime.Now.AddMinutes(-30.0 * count);
            for (int i = 0; i < count; i++)
            {
                HealthPoint p = new HealthPoint();
                p.Time = start.AddMinutes(30.0 * i);
                p.Health = 100 - i * 4 / count;
                p.Temp = (int)Math.Round(38 + 7 * Math.Sin(i / 24.0) + random.Next(-3, 4));
                list.Add(p);
            }
            return list;
        }
    }
}
