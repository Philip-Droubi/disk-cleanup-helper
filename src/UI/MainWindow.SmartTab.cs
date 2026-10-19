// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// SMART details tab: the raw self-monitoring table of each disk.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Disks;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        // Filled together with the Disk health tab (see RenderHealth).
        void RenderSmart(List<DiskHealth> disks)
        {
            SmartPicker.Children.Clear();
            SmartPanel.Children.Clear();
            List<DiskHealth> withSmart = disks.Where(x => x.SmartRows.Count > 0).ToList();
            if (withSmart.Count == 0)
            {
                SmartPanel.Children.Add(Text(admin
                    ? L.T("None of the disks shares SMART data with Windows. This is common for USB enclosures and RAID.")
                    : L.T("Start the tool with admin rights to read the disks' SMART data."), 13, "#64748B", false, true));
                return;
            }
            TextBlock title = Text("", 15, "#0F172A", true, true);
            title.Margin = new Thickness(2, 2, 0, 8);
            Border tableCard = Card();
            tableCard.Padding = new Thickness(0);
            tableCard.ClipToBounds = true;
            List<Button> buttons = new List<Button>();
            Action<DiskHealth> select = delegate(DiskHealth sel)
            {
                foreach (Button b in buttons) b.Style = (Style)Window.FindResource(b.Tag == sel ? "Primary" : "Btn");
                title.Text = sel.Model + "   " + (sel.SmartIsNvme ? L.T("NVMe health log") : L.T("SMART attributes")) +
                             (sel.HealthPct.HasValue ? "   " + L.F("(health {0}%)", sel.HealthPct.Value) : "");
                tableCard.Child = SmartTable(sel);
            };
            foreach (DiskHealth disk in withSmart)
            {
                Button b = new Button();
                b.Tag = disk;
                b.Margin = new Thickness(0, 0, 8, 6);
                TextBlock label = new TextBlock();
                label.Text = disk.Model + (string.IsNullOrEmpty(disk.Letters) ? "" : "  (" + disk.Letters + ")");
                if (L.Rtl && !L.HasRtlLetters(label.Text)) label.FlowDirection = FlowDirection.LeftToRight;   // keeps "(C:)" in place
                b.Content = label;
                DiskHealth picked = disk;
                b.Click += delegate { select(picked); };
                buttons.Add(b);
                SmartPicker.Children.Add(b);
            }
            SmartPanel.Children.Add(title);
            SmartPanel.Children.Add(tableCard);
            select(withSmart[0]);
        }

        // One disk's table: NVMe drives have their own health log layout, other disks the classic SMART columns.
        FrameworkElement SmartTable(DiskHealth d)
        {
            Grid t = new Grid();
            string[] heads;
            double[] widths;
            if (d.SmartIsNvme) { heads = new[] { L.T("Offset"), L.T("Item"), L.T("Value"), L.T("Status") }; widths = new double[] { 78, 260, 286, 0 }; }
            else { heads = new[] { L.T("No."), L.T("Attribute"), L.T("Threshold"), L.T("Value"), L.T("Worst"), L.T("Status"), L.T("Data"), L.T("Raw value") }; widths = new double[] { 46, 250, 76, 72, 80, 144, 112, 0 }; }
            foreach (double w in widths) t.ColumnDefinitions.Add(Col(w > 0 ? new GridLength(w) : Star));
            int row = 0;
            AddTableRow(t, row++, heads, null, true);
            foreach (SmartRow r in d.SmartRows)
            {
                string[] cells = d.SmartIsNvme ? new[] { r.No, r.Name, r.Value, r.Status }
                                               : new[] { r.No, r.Name, r.Threshold, r.Value, r.Worst, r.Status, r.Data, r.Raw };
                AddTableRow(t, row++, cells, r, false);
            }
            return t;
        }

        void AddTableRow(Grid t, int row, string[] cells, SmartRow r, bool header)
        {
            RowDefinition rd = new RowDefinition();
            rd.Height = GridLength.Auto;
            t.RowDefinitions.Add(rd);
            Border bg = new Border();
            if (header) bg.Background = B("#F1F5F9");
            else if (r.Level == 2) bg.Background = B("#FEE2E2");
            else if (r.Level == 1) bg.Background = B("#FEF3C7");
            else bg.Background = row % 2 == 0 ? B("#F8FAFC") : B("#FFFFFF");
            Grid.SetRow(bg, row);
            Grid.SetColumnSpan(bg, cells.Length);
            t.Children.Add(bg);
            int statusCol = cells.Length == 8 ? 5 : 3;
            int dataCol = cells.Length == 8 ? 6 : -1;
            for (int c = 0; c < cells.Length; c++)
            {
                bool isStatus = !header && c == statusCol;
                string color = header ? "#475569" : isStatus ? (r.Level == 2 ? "#B91C1C" : r.Level == 1 ? "#B45309" : "#15803D") : "#0F172A";
                TextBlock tb = Piece(cells[c] ?? "", 12, color, header || isStatus, false);   // English names keep their brackets in right-to-left
                tb.Margin = new Thickness(8, 4, 8, 4);
                tb.TextTrimming = TextTrimming.CharacterEllipsis;
                if (!header && c == dataCol) tb.FontFamily = new FontFamily("Consolas");
                Grid.SetRow(tb, row);
                Grid.SetColumn(tb, c);
                t.Children.Add(tb);
            }
        }
    }
}
