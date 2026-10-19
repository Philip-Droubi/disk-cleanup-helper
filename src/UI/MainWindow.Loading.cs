// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Loading views shown while a tab reads its data: a spinner with a short message, then grey placeholders shaped
// like the page, so a tab never looks empty or stuck.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        static bool stillPictures;   // no animations in test snapshots (see Animate)

        // ------------------------------------------------------------ the pages
        // Also fills the SMART details tab, which is read together with Disk health.
        void ShowHealthLoading(string detail)
        {
            int order = 0;
            HealthPanel.Children.Clear();
            tempViews.Clear();
            HealthPanel.Children.Add(LoadingCard(L.T("Checking disks..."), detail));
            HealthPanel.Children.Add(Bone(70, 12, order++, 0));   // "Drives"
            UniformGrid tiles = new UniformGrid();
            tiles.Columns = 3;
            tiles.Margin = new Thickness(0, 4, 0, 6);
            for (int i = 0; i < 3; i++)
            {
                StackPanel sp = new StackPanel();
                sp.Children.Add(Bone(44, 14, order++, 8));
                sp.Children.Add(Bone(0, 8, order++, 8));
                sp.Children.Add(Bone(110, 10, order++, 4));
                Border tile = Card();
                tile.Padding = new Thickness(14, 12, 14, 8);
                tile.Margin = new Thickness(0, 0, 10, 10);
                tile.Child = sp;
                tiles.Children.Add(tile);
            }
            HealthPanel.Children.Add(tiles);
            HealthPanel.Children.Add(Bone(60, 12, order++, 0));   // "Disks"
            for (int i = 0; i < 2; i++)
            {
                Grid stats = new Grid();
                for (int c = 0; c < 4; c++)
                {
                    stats.ColumnDefinitions.Add(Col(Star));
                    StackPanel stat = new StackPanel();
                    stat.Children.Add(Bone(70, 9, order++, 8));
                    stat.Children.Add(Bone(90, 22, order++, 8));
                    Grid.SetColumn(stat, c);
                    stats.Children.Add(stat);
                }
                StackPanel sp = new StackPanel();
                sp.Children.Add(Bone(240, 16, order++, 10));
                sp.Children.Add(Bone(160, 10, order++, 16));
                sp.Children.Add(stats);
                Border card = Card();
                card.Padding = new Thickness(18, 16, 18, 10);
                card.Margin = new Thickness(0, 4, 0, 12);
                card.Child = sp;
                HealthPanel.Children.Add(card);
            }
            ShowSmartLoading(detail);
        }

        void ShowSmartLoading(string detail)
        {
            int order = 0;
            SmartPicker.Children.Clear();
            SmartPanel.Children.Clear();
            Border pick1 = Bone(190, 30, order++, 6), pick2 = Bone(150, 30, order++, 6);
            pick1.Margin = pick2.Margin = new Thickness(0, 0, 8, 6);
            SmartPicker.Children.Add(pick1);
            SmartPicker.Children.Add(pick2);
            SmartPanel.Children.Add(LoadingCard(L.T("Checking disks..."), detail));
            SmartPanel.Children.Add(Bone(260, 15, order++, 10));
            StackPanel rows = new StackPanel();
            double[] widths = { 34, 0, 60, 70 };   // id, name, value, status
            for (int r = 0; r < 9; r++)
            {
                Grid row = new Grid();
                row.ColumnDefinitions.Add(Col(new GridLength(60)));
                row.ColumnDefinitions.Add(Col(Star));
                row.ColumnDefinitions.Add(Col(new GridLength(110)));
                row.ColumnDefinitions.Add(Col(new GridLength(100)));
                for (int c = 0; c < 4; c++)
                {
                    Border cell = Bone(widths[c], r == 0 ? 9 : 11, order++, 0);
                    cell.Margin = new Thickness(0, 0, c == 1 ? 40 : 12, 0);
                    Grid.SetColumn(cell, c);
                    row.Children.Add(cell);
                }
                row.Margin = new Thickness(0, 0, 0, r == 0 ? 16 : 13);
                rows.Children.Add(row);
            }
            Border table = Card();
            table.Padding = new Thickness(16, 14, 16, 6);
            table.Child = rows;
            SmartPanel.Children.Add(table);
        }

        // The "Right now" tiles, both charts and the summary.
        void ShowPerfLoading()
        {
            int order = 0;
            PerfPanel.Children.Clear();
            PerfPanel.Children.Add(LoadingCard(L.T("Getting disk activity ready..."),
                L.T("The first time, Windows can take a few seconds to start its disk activity counters.")));

            Grid tiles = new Grid();
            for (int i = 0; i < 4; i++)
            {
                tiles.ColumnDefinitions.Add(Col(Star));
                StackPanel sp = new StackPanel();
                sp.Children.Add(Bone(60, 9, order++, 10));
                sp.Children.Add(Bone(100, 20, order++, 2));
                Border tile = new Border();
                tile.Background = B("#F8FAFC");
                tile.CornerRadius = new CornerRadius(8);
                tile.Padding = new Thickness(14, 10, 14, 10);
                tile.Margin = new Thickness(0, 0, i == 3 ? 0 : 10, 0);
                tile.Child = sp;
                Grid.SetColumn(tile, i);
                tiles.Children.Add(tile);
            }
            StackPanel now = new StackPanel();
            now.Children.Add(Bone(80, 14, order++, 12));
            now.Children.Add(tiles);
            Border nowCard = Card();
            nowCard.Padding = new Thickness(18, 14, 18, 14);
            nowCard.Child = now;
            PerfPanel.Children.Add(nowCard);

            Grid charts = new Grid();
            charts.ColumnDefinitions.Add(Col(Star));
            charts.ColumnDefinitions.Add(Col(Star));
            for (int c = 0; c < 2; c++)
            {
                StackPanel sp = new StackPanel();
                sp.Children.Add(Bone(c == 0 ? 130 : 220, 11, order++, 16));
                for (int k = 0; k < 5; k++) sp.Children.Add(Bone(0, 2, order++, 26));   // the chart's grid lines
                Grid labels = new Grid();
                Border left = Bone(80, 8, order++, 0), right = Bone(30, 8, order++, 0);
                right.HorizontalAlignment = HorizontalAlignment.Right;
                labels.Children.Add(left);
                labels.Children.Add(right);
                sp.Children.Add(labels);
                Border card = Card();
                card.Padding = new Thickness(16, 12, 16, 10);
                card.Margin = c == 0 ? new Thickness(0, 0, 6, 12) : new Thickness(6, 0, 0, 12);
                card.Child = sp;
                Grid.SetColumn(card, c);
                charts.Children.Add(card);
            }
            PerfPanel.Children.Add(charts);

            StackPanel summary = new StackPanel();
            summary.Children.Add(Bone(90, 14, order++, 14));
            for (int r = 0; r < 4; r++) summary.Children.Add(LabelValueBones(260, r % 2 == 0 ? 200 : 150, 90, ref order));
            Border summaryCard = Card();
            summaryCard.Padding = new Thickness(18, 14, 18, 8);
            summaryCard.Child = summary;
            PerfPanel.Children.Add(summaryCard);
        }

        // The four summary tiles (with their colour bars) and two label/value cards.
        void ShowDeviceLoading()
        {
            int order = 0;
            DevicePanel.Children.Clear();
            DevicePanel.Children.Add(LoadingCard(L.T("Reading this PC's details..."),
                L.T("Collecting details about Windows, the processor, memory, graphics and more. This takes a few seconds.")));
            UniformGrid tiles = new UniformGrid();
            tiles.Columns = 4;
            string[] colors = { "#2563EB", "#7C3AED", "#0891B2", "#16A34A" };   // the tiles' own colours
            for (int i = 0; i < 4; i++)
            {
                Border bar = new Border();
                bar.Background = B(colors[i]);
                bar.Opacity = 0.45;
                bar.CornerRadius = new CornerRadius(9, 0, 0, 9);
                StackPanel sp = new StackPanel();
                sp.Margin = new Thickness(14, 12, 14, 10);
                sp.Children.Add(Bone(64, 9, order++, 8));
                sp.Children.Add(Bone(0, 17, order++, 8));
                sp.Children.Add(Bone(110, 9, order++, 2));
                Grid.SetColumn(sp, 1);
                Grid g = new Grid();
                g.ColumnDefinitions.Add(Col(new GridLength(4)));
                g.ColumnDefinitions.Add(Col(Star));
                g.Children.Add(bar);
                g.Children.Add(sp);
                Border tile = Card();
                tile.Margin = new Thickness(0, 0, i == 3 ? 0 : 10, 12);
                tile.Child = g;
                tiles.Children.Add(tile);
            }
            DevicePanel.Children.Add(tiles);
            for (int k = 0; k < 2; k++)
            {
                Grid columns = new Grid();
                columns.ColumnDefinitions.Add(Col(Star));
                columns.ColumnDefinitions.Add(Col(Star));
                for (int c = 0; c < 2; c++)
                {
                    StackPanel column = new StackPanel();
                    for (int r = 0; r < 4; r++)
                    {
                        Grid row = LabelValueBones(170, 90, r % 2 == 0 ? 130 : 90, ref order);
                        row.Margin = new Thickness(0, 0, 24, 12);
                        column.Children.Add(row);
                    }
                    Grid.SetColumn(column, c);
                    columns.Children.Add(column);
                }
                StackPanel sp = new StackPanel();
                sp.Children.Add(Bone(110, 14, order++, 16));
                sp.Children.Add(columns);
                Border card = Card();
                card.Padding = new Thickness(18, 16, 18, 8);
                card.Child = sp;
                DevicePanel.Children.Add(card);
            }
        }

        // ------------------------------------------------------------ building blocks
        // A label placeholder in a fixed-width column and a value placeholder next to it.
        static Grid LabelValueBones(double labelColumn, double labelWidth, double valueWidth, ref int order)
        {
            Grid row = new Grid();
            row.ColumnDefinitions.Add(Col(new GridLength(labelColumn)));
            row.ColumnDefinitions.Add(Col(Star));
            Border label = Bone(labelWidth, 10, order++, 0), value = Bone(valueWidth, 12, order++, 0);
            Grid.SetColumn(value, 1);
            row.Children.Add(label);
            row.Children.Add(value);
            row.Margin = new Thickness(0, 0, 0, 12);
            return row;
        }

        static Border LoadingCard(string title, string detail)
        {
            FrameworkElement spinner = Spinner(30);
            spinner.VerticalAlignment = VerticalAlignment.Center;
            spinner.Margin = new Thickness(2, 0, 14, 0);
            StackPanel texts = new StackPanel();
            texts.VerticalAlignment = VerticalAlignment.Center;
            texts.Children.Add(Text(title, 14, "#0F172A", true, false));
            TextBlock line = Text(detail, 12.5, "#64748B", false, true);
            line.Margin = new Thickness(0, 2, 0, 0);
            texts.Children.Add(line);
            Grid.SetColumn(texts, 1);
            Grid g = new Grid();
            g.ColumnDefinitions.Add(Col(GridLength.Auto));
            g.ColumnDefinitions.Add(Col(Star));
            g.Children.Add(spinner);
            g.Children.Add(texts);
            Border card = Card();
            card.Padding = new Thickness(16, 14, 16, 14);
            card.Margin = new Thickness(0, 0, 0, 14);
            card.Child = g;
            return card;
        }

        // A grey ring with a blue arc that turns.
        static FrameworkElement Spinner(double size)
        {
            System.Windows.Shapes.Ellipse track = new System.Windows.Shapes.Ellipse();
            track.Stroke = B("#E2E8F0");
            track.StrokeThickness = 4;
            System.Windows.Shapes.Ellipse arc = new System.Windows.Shapes.Ellipse();
            arc.Stroke = B("#2563EB");
            arc.StrokeThickness = 4;
            double ringLength = Math.PI * (size - 4) / 4;   // in stroke widths, the unit of a dash array
            arc.StrokeDashArray = new DoubleCollection(new[] { ringLength * 0.25, ringLength * 0.75 });
            arc.StrokeDashCap = PenLineCap.Round;
            arc.RenderTransformOrigin = new Point(0.5, 0.5);
            RotateTransform turn = new RotateTransform();
            arc.RenderTransform = turn;
            DoubleAnimation spin = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(0.9));
            spin.RepeatBehavior = RepeatBehavior.Forever;
            Animate(arc, turn, RotateTransform.AngleProperty, spin);
            Grid g = new Grid();
            g.Width = size; g.Height = size;
            g.FlowDirection = FlowDirection.LeftToRight;
            g.Children.Add(track);
            g.Children.Add(arc);
            return g;
        }

        // A grey placeholder bar (width 0 = full width). Each bar starts fading a little after the one before
        // ("order"), so the placeholders pulse in a wave.
        static Border Bone(double width, double height, int order, double marginBelow)
        {
            Border bone = new Border();
            bone.Background = B("#E2E8F0");
            bone.CornerRadius = new CornerRadius(Math.Min(6, height / 2));
            bone.Height = height;
            if (width > 0) { bone.Width = width; bone.HorizontalAlignment = HorizontalAlignment.Left; }
            bone.Margin = new Thickness(0, 0, 0, marginBelow);
            DoubleAnimation pulse = new DoubleAnimation(1, 0.35, TimeSpan.FromSeconds(0.8));
            pulse.AutoReverse = true;
            pulse.RepeatBehavior = RepeatBehavior.Forever;
            pulse.BeginTime = TimeSpan.FromMilliseconds((order % 12) * 70);
            SineEase ease = new SineEase();
            ease.EasingMode = EasingMode.EaseInOut;
            pulse.EasingFunction = ease;
            Animate(bone, bone, UIElement.OpacityProperty, pulse);
            return bone;
        }

        // Runs an endless animation only while the element is on screen. Stays still when Windows animations are off
        // and in test snapshots (a snapshot waits for the window to go idle, which never happens while something moves).
        static void Animate(FrameworkElement host, IAnimatable target, DependencyProperty property, AnimationTimeline animation)
        {
            if (!SystemParameters.ClientAreaAnimation || stillPictures) return;
            host.Loaded += delegate { target.BeginAnimation(property, animation); };
            host.Unloaded += delegate { target.BeginAnimation(property, null); };
        }
    }
}
