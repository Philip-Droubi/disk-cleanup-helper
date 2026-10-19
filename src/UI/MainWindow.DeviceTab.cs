// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Device details tab (like dxdiag): summary tiles and one card per section.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiskCleanupHelper.Device;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        DeviceReport lastDevice;
        bool deviceLoading;

        void ShowDevice(bool synchronous)
        {
            if (synchronous) { RenderDevice(DeviceInfoReader.Read()); return; }
            if (deviceLoading) return;
            deviceLoading = true;
            BtnDevRefresh.IsEnabled = false;
            if (lastDevice == null) ShowDeviceLoading();
            DevStatus.Text = L.T("Reading...");
            ThreadPool.QueueUserWorkItem(delegate
            {
                DeviceReport r = null;
                try { r = DeviceInfoReader.Read(); }
                catch (Exception ex) { Log(L.F("Could not read the device details: {0}", ex.Message)); }
                Ui(delegate
                {
                    deviceLoading = false;
                    BtnDevRefresh.IsEnabled = true;
                    if (r != null) RenderDevice(r); else DevStatus.Text = "";
                });
            });
        }

        void RenderDevice(DeviceReport r)
        {
            lastDevice = r;
            DevicePanel.Children.Clear();
            DevStatus.Text = L.F("Read at {0}. It stays on this PC unless you copy or save it.", r.Taken.ToString("t"));
            if (r.Tiles.Count > 0)
            {
                System.Windows.Controls.Primitives.UniformGrid tiles = new System.Windows.Controls.Primitives.UniformGrid();
                tiles.Columns = r.Tiles.Count;
                for (int i = 0; i < r.Tiles.Count; i++)
                {
                    Border t = DeviceTile(r.Tiles[i]);
                    if (i == r.Tiles.Count - 1) t.Margin = new Thickness(0, 0, 0, 12);
                    tiles.Children.Add(t);
                }
                DevicePanel.Children.Add(tiles);
            }
            foreach (InfoSection s in r.Sections) DevicePanel.Children.Add(DeviceCard(s));
        }

        Border DeviceTile(SummaryTile t)
        {
            Border b = Card();
            b.Margin = new Thickness(0, 0, 10, 12);
            Grid g = new Grid();
            g.ColumnDefinitions.Add(Col(new GridLength(4)));
            g.ColumnDefinitions.Add(Col(Star));
            Border bar = new Border();
            bar.Background = B(t.Color);
            bar.CornerRadius = new CornerRadius(9, 0, 0, 9);
            g.Children.Add(bar);
            StackPanel sp = new StackPanel();
            sp.Margin = new Thickness(14, 10, 14, 11);
            sp.Children.Add(Text(t.Label, 11, t.Color, true, false));
            TextBlock v = Piece(t.Value, 17, "#0F172A", true, false);
            v.TextTrimming = TextTrimming.CharacterEllipsis;
            v.ToolTip = t.Value;
            v.Margin = new Thickness(0, 2, 0, 1);
            sp.Children.Add(v);
            FrameworkElement sub = ValueText(t.Sub ?? "", 12, "#64748B", false, true);   // wraps in narrow windows and long languages
            if (!string.IsNullOrEmpty(t.Sub)) sub.ToolTip = t.Sub;
            sp.Children.Add(sub);
            Grid.SetColumn(sp, 1);
            g.Children.Add(sp);
            b.Child = g;
            return b;
        }

        // Label and value rows; with two columns the left column fills first.
        Grid LabelValueGrid(List<string[]> rows, bool twoColumns, double labelWidth)
        {
            Grid g = new Grid();
            int cols = twoColumns && rows.Count > 1 ? 2 : 1;
            for (int c = 0; c < cols; c++)
            {
                if (c > 0) g.ColumnDefinitions.Add(Col(new GridLength(28)));
                g.ColumnDefinitions.Add(Col(new GridLength(labelWidth)));
                g.ColumnDefinitions.Add(Col(Star));
            }
            int perCol = (rows.Count + cols - 1) / cols;
            for (int i = 0; i < rows.Count; i++)
            {
                int c = i / perCol, row = i % perCol;
                while (g.RowDefinitions.Count <= row)
                {
                    RowDefinition rd = new RowDefinition();
                    rd.Height = GridLength.Auto;
                    g.RowDefinitions.Add(rd);
                }
                TextBlock l = Text(rows[i][0], 13, "#64748B", false, true);
                l.Margin = new Thickness(0, 3, 10, 3);
                Grid.SetRow(l, row);
                Grid.SetColumn(l, c * 3);
                FrameworkElement v = ValueText(rows[i][1], 13.5, "#0F172A", true, true);
                v.Margin = new Thickness(0, 3, 0, 3);
                Grid.SetRow(v, row);
                Grid.SetColumn(v, c * 3 + 1);
                g.Children.Add(l);
                g.Children.Add(v);
            }
            return g;
        }

        Border Callout(string text)
        {
            Border box = new Border();
            box.Background = B("#FEF3C7");
            box.BorderBrush = B("#FCD34D");
            box.BorderThickness = new Thickness(1);
            box.CornerRadius = new CornerRadius(8);
            box.Padding = new Thickness(12, 7, 12, 7);
            box.Margin = new Thickness(0, 8, 0, 0);
            TextBlock t = Text(text, 13, "#92400E", false, true);
            t.LineHeight = 19;
            box.Child = t;
            return box;
        }

        Border DeviceGroup(InfoGroup grp, bool wide)
        {
            Border b = new Border();
            b.Background = B("#F8FAFC");
            b.BorderBrush = B("#EEF2F6");
            b.BorderThickness = new Thickness(1);
            b.CornerRadius = new CornerRadius(8);
            b.Padding = new Thickness(14, 10, 14, 10);
            StackPanel sp = new StackPanel();
            sp.Children.Add(ValueText(grp.Title, 13.5, "#0F172A", true, true));
            if (grp.Rows.Count > 0) sp.Children.Add(Spaced(LabelValueGrid(grp.Rows, wide, 150), 4, 0));
            foreach (string w in grp.Warnings) sp.Children.Add(Callout(w));
            b.Child = sp;
            return b;
        }

        Border DeviceCard(InfoSection s)
        {
            Border card = Card();
            card.Padding = new Thickness(18, 14, 18, 14);
            StackPanel sp = new StackPanel();
            sp.Children.Add(Text(s.Title, 15, "#0F172A", true, false));
            if (!string.IsNullOrEmpty(s.Sub)) sp.Children.Add(Spaced(Text(s.Sub, 12.5, "#64748B", false, true), 2, 0));
            if (s.Rows.Count > 0) sp.Children.Add(Spaced(LabelValueGrid(s.Rows, s.Rows.Count > 3, 190), 8, 0));
            if (s.Groups.Count > 0)
            {
                // one box: full width; more: two side by side
                Grid groups = new Grid();
                groups.Margin = new Thickness(0, 10, 0, 0);
                int cols = s.Groups.Count == 1 ? 1 : 2;
                for (int c = 0; c < cols; c++) groups.ColumnDefinitions.Add(Col(Star));
                for (int i = 0; i < s.Groups.Count; i++)
                {
                    if (i % cols == 0)
                    {
                        RowDefinition rd = new RowDefinition();
                        rd.Height = GridLength.Auto;
                        groups.RowDefinitions.Add(rd);
                    }
                    Border box = DeviceGroup(s.Groups[i], cols == 1);
                    bool lastRow = i / cols == (s.Groups.Count - 1) / cols;
                    box.Margin = new Thickness(cols == 2 && i % 2 == 1 ? 5 : 0, 0, cols == 2 && i % 2 == 0 ? 5 : 0, lastRow ? 0 : 10);
                    Grid.SetRow(box, i / cols);
                    Grid.SetColumn(box, i % cols);
                    groups.Children.Add(box);
                }
                sp.Children.Add(groups);
            }
            foreach (string w in s.Warnings) sp.Children.Add(Callout(w));
            card.Child = sp;
            return card;
        }

        void CopyDeviceReport()
        {
            if (lastDevice == null) { Msg(L.T("Wait until the details have been read.")); return; }
            try
            {
                Clipboard.SetText(lastDevice.ToText());
                Log(L.T("Copied the device details. Paste them where you need them, for example in a message to someone who helps you."));
            }
            catch (Exception ex) { Msg(L.F("Could not copy: {0}", ex.Message)); }
        }

        void SaveDeviceReport()
        {
            if (lastDevice == null) { Msg(L.T("Wait until the details have been read.")); return; }
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = L.T("Save the device details");
            dlg.FileName = L.T("Device details") + " - " + Environment.MachineName + " - " + lastDevice.Taken.ToString("yyyy-MM-dd") + ".txt";
            dlg.Filter = L.T("Text file") + " (*.txt)|*.txt";
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (dlg.ShowDialog(Window) != true) return;
            try
            {
                File.WriteAllText(dlg.FileName, lastDevice.ToText(), System.Text.Encoding.UTF8);
                Log(L.F("Saved the device details to {0}", dlg.FileName));
            }
            catch (Exception ex) { Msg(L.F("Could not save the file: {0}", ex.Message)); }
        }
    }
}
