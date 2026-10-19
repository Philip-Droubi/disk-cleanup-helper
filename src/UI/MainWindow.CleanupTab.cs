// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Clean up tab: the grouped item list, measuring sizes and cleaning.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Cleanup;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        // How a group of items is shown. Id matches CleanupItem.Group.
        sealed class CleanupGroup
        {
            public readonly string Id, Title, Description, Badge, Color, Tint;
            public CleanupGroup(string id, string title, string description, string badge, string color, string tint)
            {
                Id = id; Title = title; Description = description; Badge = badge; Color = color; Tint = tint;
            }
        }

        // The parts of a group header that change: the selected-count chip and the total badge.
        sealed class GroupHeader
        {
            public Border SelectedChip, TotalBadge;
            public TextBlock SelectedText, TotalText;
        }

        static CleanupGroup[] Groups
        {
            get
            {
                return new[] {
                    new CleanupGroup("safe", L.T("Safe to clean"), L.T("Caches and temp files. Programs rebuild them by themselves."), L.T("SAFE"), "#15803D", "#DCFCE7"),
                    new CleanupGroup("updates", L.T("Old installer files and Windows cleanup"), L.T("Needs admin rights. Read each one before you select it."), L.T("ADMIN"), "#1D4ED8", "#DBEAFE"),
                    new CleanupGroup("yourcall", L.T("Your call"), L.T("You lose something small. Read each one before you select it."), L.T("YOUR CALL"), "#B45309", "#FEF3C7"),
                    new CleanupGroup("settings", L.T("Windows settings"), L.T("Switches that free space or keep it free."), L.T("SETTING"), "#6D28D9", "#EDE9FE"),
                    new CleanupGroup("manual", L.T("Do it yourself"), L.T("The tool only shows and opens these. It never deletes them."), L.T("INFO ONLY"), "#475569", "#E2E8F0")
                };
            }
        }

        readonly Dictionary<string, GroupHeader> groupHeaders = new Dictionary<string, GroupHeader>();
        readonly HashSet<string> folded = new HashSet<string>();   // remembered in settings.ini as "folded=updates,manual"

        static Brush RowSelectedBrush { get { return B("#EFF6FF"); } }
        static Brush RowSelectedBorder { get { return B("#BFDBFE"); } }

        // ------------------------------------------------------------ the list
        void BuildList()
        {
            folded.Clear();
            string saved = options.Fold ?? AppSettings.Read("folded");   // /fold folds groups for one test run only
            if (!string.IsNullOrEmpty(saved))
                foreach (string id in saved.Split(',')) if (id.Trim().Length > 0) folded.Add(id.Trim());
            foreach (CleanupGroup group in Groups)
            {
                List<CleanupItem> members = items.Where(i => i.Group == group.Id).ToList();
                if (members.Count > 0) ListPanel.Children.Add(GroupCard(group, members));
            }
        }

        // A card with a clickable header (chevron, badge, title, selected count, total) and a body that folds away.
        Border GroupCard(CleanupGroup group, List<CleanupItem> members)
        {
            GroupHeader parts = new GroupHeader();
            groupHeaders[group.Id] = parts;

            Grid head = new Grid();
            head.ColumnDefinitions.Add(Col(GridLength.Auto));
            head.ColumnDefinitions.Add(Col(GridLength.Auto));
            head.ColumnDefinitions.Add(Col(Star));
            head.ColumnDefinitions.Add(Col(GridLength.Auto));
            head.ColumnDefinitions.Add(Col(GridLength.Auto));
            head.Margin = new Thickness(4, 0, 10, 2);
            head.Background = Brushes.Transparent;   // so the whole header answers clicks
            head.Cursor = Cursors.Hand;
            RotateTransform turn;
            head.Children.Add(Chevron(out turn));
            Border badge = Pill(group.Badge, group.Color, group.Tint);
            badge.Margin = new Thickness(0, 0, 10, 0);
            Grid.SetColumn(badge, 1);
            head.Children.Add(badge);
            TextBlock title = Text(group.Title, 15, "#0F172A", true, true);
            title.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(title, 2);
            head.Children.Add(title);
            Border chip = SelectedChip(parts);
            Grid.SetColumn(chip, 3);
            head.Children.Add(chip);
            Border total = TotalBadge(parts, group.Color, group.Tint);
            Grid.SetColumn(total, 4);
            head.Children.Add(total);

            StackPanel body = new StackPanel();
            TextBlock description = Text(group.Description, 12, "#64748B", false, true);
            description.Margin = new Thickness(10, 2, 10, 6);
            body.Children.Add(description);
            foreach (CleanupItem it in members) body.Children.Add(ItemRow(it));

            Action<bool, bool> fold = delegate(bool isFolded, bool animate)
            {
                body.Visibility = isFolded ? Visibility.Collapsed : Visibility.Visible;
                TurnChevron(turn, isFolded ? -90 : 0, animate);
            };
            fold(folded.Contains(group.Id), false);
            head.MouseLeftButtonUp += delegate
            {
                bool nowFolded = !folded.Contains(group.Id);
                if (nowFolded) folded.Add(group.Id); else folded.Remove(group.Id);
                fold(nowFolded, true);
                if (options.SnapshotPath == null) AppSettings.Write("folded", string.Join(",", folded));
            };

            StackPanel stack = new StackPanel();
            stack.Children.Add(head);
            stack.Children.Add(body);
            Border card = Card();
            card.Padding = new Thickness(6, 12, 6, 6);
            card.Child = stack;
            return card;
        }

        static FrameworkElement Chevron(out RotateTransform turn)
        {
            System.Windows.Shapes.Path arrow = new System.Windows.Shapes.Path();
            arrow.Data = Geometry.Parse("M 1,1 L 5,5 L 9,1");
            arrow.Stroke = B("#64748B");
            arrow.StrokeThickness = 1.8;
            arrow.StrokeStartLineCap = arrow.StrokeEndLineCap = PenLineCap.Round;
            arrow.StrokeLineJoin = PenLineJoin.Round;
            arrow.Width = 10; arrow.Height = 6;
            arrow.HorizontalAlignment = HorizontalAlignment.Center;
            arrow.VerticalAlignment = VerticalAlignment.Center;
            Grid box = new Grid();
            box.Width = 22; box.Height = 22;
            box.Margin = new Thickness(0, 0, 4, 0);
            box.VerticalAlignment = VerticalAlignment.Center;
            box.RenderTransformOrigin = new Point(0.5, 0.5);
            turn = new RotateTransform();
            box.RenderTransform = turn;
            box.Children.Add(arrow);
            return box;
        }

        // 0 = pointing down (open), -90 = pointing sideways (folded).
        static void TurnChevron(RotateTransform turn, double angle, bool animate)
        {
            if (animate && SystemParameters.ClientAreaAnimation && !stillPictures)
            {
                DoubleAnimation a = new DoubleAnimation(angle, TimeSpan.FromMilliseconds(160));
                CubicEase ease = new CubicEase();
                ease.EasingMode = EasingMode.EaseOut;
                a.EasingFunction = ease;
                turn.BeginAnimation(RotateTransform.AngleProperty, a);
            }
            else
            {
                turn.BeginAnimation(RotateTransform.AngleProperty, null);
                turn.Angle = angle;
            }
        }

        static Border SelectedChip(GroupHeader parts)
        {
            Border chip = new Border();
            chip.Background = B("#DBEAFE");
            chip.CornerRadius = new CornerRadius(8);
            chip.Padding = new Thickness(9, 3, 9, 4);
            chip.Margin = new Thickness(10, 0, 0, 0);
            chip.VerticalAlignment = VerticalAlignment.Center;
            chip.Visibility = Visibility.Collapsed;
            parts.SelectedText = Text("", 12, "#1D4ED8", true, false);
            chip.Child = parts.SelectedText;
            parts.SelectedChip = chip;
            return chip;
        }

        static Border TotalBadge(GroupHeader parts, string color, string tint)
        {
            Border badge = new Border();
            badge.Background = B(tint);
            badge.CornerRadius = new CornerRadius(8);
            badge.Padding = new Thickness(10, 3, 10, 4);
            badge.Margin = new Thickness(12, 0, 0, 0);
            badge.VerticalAlignment = VerticalAlignment.Center;
            badge.Visibility = Visibility.Collapsed;   // shown once something in the group is measured
            TextBlock label = Text(L.T("TOTAL"), 10.5, color, true, false);
            label.VerticalAlignment = VerticalAlignment.Center;
            label.Opacity = 0.8;
            label.Margin = new Thickness(0, 1, 8, 0);
            parts.TotalText = Text("", 14, color, true, false);
            parts.TotalText.VerticalAlignment = VerticalAlignment.Center;
            StackPanel row = new StackPanel();
            row.Orientation = Orientation.Horizontal;
            row.Children.Add(label);
            row.Children.Add(parts.TotalText);
            badge.Child = row;
            parts.TotalBadge = badge;
            return badge;
        }

        Border ItemRow(CleanupItem it)
        {
            Grid g = new Grid();
            g.ColumnDefinitions.Add(Col(new GridLength(30)));   // check box
            g.ColumnDefinitions.Add(Col(Star));                 // name and short note
            g.ColumnDefinitions.Add(Col(GridLength.Auto));      // "Close X first" / "Admin"
            g.ColumnDefinitions.Add(Col(new GridLength(100)));  // size

            CheckBox check = new CheckBox();
            check.VerticalAlignment = VerticalAlignment.Center;
            check.Cursor = Cursors.Hand;
            if (it.Manual) check.Visibility = Visibility.Hidden;
            check.Click += delegate { UpdateSelection(); };
            g.Children.Add(check);

            StackPanel names = new StackPanel();
            names.VerticalAlignment = VerticalAlignment.Center;
            names.Children.Add(Text(it.Name, 13.5, "#0F172A", true, true));
            TextBlock note = Text(it.Short, 12, "#64748B", false, true);
            note.Margin = new Thickness(0, 1, 0, 0);
            names.Children.Add(note);
            Grid.SetColumn(names, 1);
            g.Children.Add(names);

            Border pill = null;
            if (!it.Manual && it.MustClose.Length > 0) pill = Pill(L.F("Close {0} first", Catalog.AppName(it.MustClose[0])), "#475569", "#F1F5F9");
            else if (!it.Manual && it.Admin) pill = Pill(L.T("Admin"), "#1D4ED8", "#DBEAFE");
            if (pill != null)
            {
                pill.Margin = new Thickness(10, 0, 12, 0);
                Grid.SetColumn(pill, 2);
                g.Children.Add(pill);
            }

            TextBlock size = Text(it.SizeText ?? "-", 14, "#0F172A", true, false);
            size.HorizontalAlignment = HorizontalAlignment.Right;
            size.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(size, 3);
            g.Children.Add(size);

            Border row = new Border();
            row.CornerRadius = new CornerRadius(7);
            row.Padding = new Thickness(10, 7, 12, 7);
            row.Margin = new Thickness(0, 1, 0, 1);
            row.Background = Brushes.Transparent;
            row.BorderThickness = new Thickness(1);
            row.BorderBrush = Brushes.Transparent;
            row.Cursor = Cursors.Hand;
            row.Tag = it;
            row.Child = g;
            if (it.Admin && !admin) { row.Opacity = 0.5; check.IsEnabled = false; }
            row.MouseLeftButtonUp += delegate { SelectItem(it); };
            row.MouseEnter += delegate { if (selected != it) row.Background = HoverBrush; };
            row.MouseLeave += delegate { if (selected != it) row.Background = Brushes.Transparent; };
            it.Row = row; it.Check = check; it.SizeLabel = size;
            return row;
        }

        // ------------------------------------------------------------ selection and totals
        void UpdateSelection()
        {
            long bytes = 0;
            int count = 0;
            foreach (CleanupItem it in items)
                if (IsChecked(it)) { count++; if (it.Bytes.HasValue) bytes += it.Bytes.Value; }
            foreach (KeyValuePair<string, GroupHeader> kv in groupHeaders)
            {
                int n = items.Count(i => i.Group == kv.Key && IsChecked(i));
                kv.Value.SelectedText.Text = "✓  " + L.P(n, "1 selected", "{0} selected");
                kv.Value.SelectedChip.Visibility = n > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            StatSel.Text = L.P(count, "1 item", "{0} items");
            if (count == 0) { StatSelSub.Text = L.T("nothing selected"); BtnClean.Content = L.T("Clean selected"); }
            else { StatSelSub.Text = L.F("up to {0}", Fmt.Size(bytes)); BtnClean.Content = L.F("Clean selected ({0})", count); }
        }

        static bool IsChecked(CleanupItem it) { return it.Check != null && it.Check.IsChecked == true; }

        // Sizes shown as text ("when run", "about 7 GB") are not added; a group with no measured number shows no total.
        void UpdateGroupTotals()
        {
            foreach (KeyValuePair<string, GroupHeader> kv in groupHeaders)
            {
                long sum = 0;
                bool any = false;
                foreach (CleanupItem it in items)
                    if (it.Group == kv.Key && it.SizeText == null && it.Bytes.HasValue) { sum += it.Bytes.Value; any = true; }
                kv.Value.TotalText.Text = Fmt.Size(sum);
                kv.Value.TotalBadge.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // ------------------------------------------------------------ details panel
        void SelectItem(CleanupItem it)
        {
            if (selected != null && selected.Row != null)
            {
                selected.Row.Background = Brushes.Transparent;
                selected.Row.BorderBrush = Brushes.Transparent;
            }
            selected = it;
            it.Row.Background = RowSelectedBrush;
            it.Row.BorderBrush = RowSelectedBorder;
            ShowDetails();
        }

        void ShowDetails()
        {
            CleanupItem it = selected;
            if (it == null) return;
            DetTitle.Text = it.Name;
            if (it.SizeText != null) DetSize.Text = L.F("Size: {0}", it.SizeText);
            else if (it.Bytes.HasValue) DetSize.Text = L.F("Size: {0}", Fmt.Size(it.Bytes));
            else DetSize.Text = L.T("Size: not measured yet");
            DetBody.Children.Clear();
            foreach (string line in it.Details.ToList()) AddDetailLine(line, "#334155", false);
            AddDetailLine("", null, false);
            AddDetailLine(L.T("Where"), "#0F172A", true);
            TextBox where = new TextBox();
            where.Text = it.Where; where.IsReadOnly = true; where.BorderThickness = new Thickness(0); where.Background = Brushes.Transparent;
            where.TextWrapping = TextWrapping.Wrap; where.Foreground = B("#475569"); where.FontSize = 12; where.Margin = new Thickness(0, 0, 0, 8);
            if (L.Rtl && !L.HasRtlLetters(it.Where)) where.FlowDirection = FlowDirection.LeftToRight;   // a path reads left to right
            DetBody.Children.Add(where);
            if (it.MustClose.Length > 0) AddDetailLine(L.F("Close first: {0}", string.Join(", ", it.MustClose.Select(Catalog.AppName).Distinct())), "#B45309", true);
            if (it.Admin && !admin) AddDetailLine(L.T("Needs admin rights. Start the tool again and click Yes on the Windows prompt to use this item."), "#B45309", false);
            if (it.Manual) AddDetailLine(L.T("This tool never deletes this item. Use \"Open location\" and follow the steps above."), "#475569", true);
            BtnOpen.IsEnabled = true;
        }

        // An empty text adds a small gap; a text starting with two spaces is a command or path (monospace).
        void AddDetailLine(string text, string color, bool bold)
        {
            if (text == "")
            {
                Border gap = new Border();
                gap.Height = 6;
                DetBody.Children.Add(gap);
                return;
            }
            TextBlock t = new TextBlock();
            t.Text = text;
            t.Foreground = B(color);
            if (text.StartsWith("  "))
            {
                t.FontFamily = new FontFamily("Consolas"); t.FontSize = 12; t.Margin = new Thickness(0, 0, 0, 1);
            }
            else
            {
                t.TextWrapping = TextWrapping.Wrap; t.Margin = new Thickness(0, 0, 0, 7); t.LineHeight = 19;
            }
            if (bold) t.FontWeight = FontWeights.SemiBold;
            DetBody.Children.Add(t);
        }

        // ------------------------------------------------------------ measuring
        void MeasureItem(CleanupItem it, CancelFlag flag)
        {
            try { it.Bytes = it.Size != null ? it.Size(it, flag) : Catalog.MeasureDefault(it, flag); }
            catch (OperationCanceledException) { throw; }
            catch { it.Bytes = null; }
        }

        // auto = started by itself when the window opens; then low memory skips it quietly instead of asking.
        void StartMeasure(bool auto)
        {
            double a, c;
            if (auto) { if (busy || MemoryIsLow(out a, out c)) return; }   // another task (an opened tab) goes first
            else if (!ConfirmMemory(L.T("Measure anyway?"))) return;
            measuring = true;
            Progress.Value = 0;
            Log(L.T("Measuring sizes. This can take a minute or two. Click Stop to stop."));
            List<CleanupItem> list = items.ToList();
            RunWork(delegate(CancelFlag flag)
            {
                for (int i = 0; i < list.Count && !flag.IsSet; i++)
                {
                    CleanupItem item = list[i];
                    if (item.SizeText != null && item.Size == null) continue;   // nothing to measure
                    int number = i + 1;
                    Ui(delegate
                    {
                        if (item.SizeText == null) item.SizeLabel.Text = "...";
                        StatusText.Text = L.F("Measuring {0} of {1}: {2}", number, list.Count, item.Name);
                    });
                    MeasureItem(item, flag);
                    Ui(delegate
                    {
                        item.SizeLabel.Text = item.SizeText ?? Fmt.Size(item.Bytes);
                        Progress.Value = 100.0 * number / list.Count;
                        UpdateGroupTotals();
                    });
                }
            }, delegate
            {
                measuring = false;
                foreach (CleanupItem it in items) if (it.SizeLabel != null && it.SizeLabel.Text == "...") it.SizeLabel.Text = "-";
                UpdateGroupTotals();
                UpdateStatus();
                UpdateSelection();
                if (cancel.IsSet) { StatusText.Text = L.T("Measuring stopped."); Log(L.T("Measuring stopped.")); }
                else
                {
                    Progress.Value = 100;
                    StatusText.Text = L.T("Done. Select what you want to clean.");
                    Log(L.T("Done. Click an item to read about it, select what you want, then click \"Clean selected\"."));
                }
                ShowDetails();
            });
        }

        // ------------------------------------------------------------ cleaning
        void StartClean()
        {
            List<CleanupItem> chosen = items.Where(IsChecked).ToList();
            if (chosen.Count == 0) { Msg(L.T("Select at least one item first.")); return; }
            chosen = SkipItemsWithOpenPrograms(chosen);
            if (chosen == null) return;
            if (!ConfirmMemory(L.T("Clean anyway?"))) return;
            string lines = string.Join("\r\n", chosen.Select(i => "  - " + i.Name + "   (" + (i.SizeText ?? Fmt.Size(i.Bytes)) + ")"));
            if (!AskYesNo(L.F("These will be cleaned. Deleted files do not go to the Recycle Bin and cannot be restored:\r\n\r\n{0}\r\n\r\nContinue?", lines), L.T("Confirm cleanup"))) return;

            List<CleanupItem> order = chosen.Where(i => !i.RunLast).Concat(chosen.Where(i => i.RunLast)).ToList();
            if (order.Any(i => i.Name == L.T("Windows components: deep cleanup")))   // the deep cleanup includes the normal one
                order = order.Where(i => i.Name != L.T("Windows components: normal cleanup")).ToList();
            long startFree = FreeBytes();
            HistoryLog.Write("---- " + L.F("Cleanup started. Free space on {0}: {1}", AppPaths.SysDrive, Fmt.Size(startFree)));
            RunWork(delegate(CancelFlag flag)
            {
                for (int i = 0; i < order.Count; i++)
                {
                    if (flag.IsSet) { Log(L.F("Stopped. Not cleaned: {0}", order[i].Name)); continue; }
                    CleanItem(order[i], flag);
                    int done = i + 1;
                    Ui(delegate { Progress.Value = 100.0 * done / order.Count; UpdateStatus(); });
                }
            }, delegate
            {
                foreach (CleanupItem it in chosen) it.Check.IsChecked = false;
                UpdateSelection();
                UpdateStatus();
                long gain = Math.Max(0, FreeBytes() - startFree);
                string summary = L.F("All done. Free space on {0} grew by {1}. Now {2} free.", AppPaths.SysDrive, Fmt.Size(gain), Fmt.Size(FreeBytes()));
                StatusText.Text = summary;
                Log(summary);
                HistoryLog.Write("---- " + summary);
                ShowDetails();
                Msg(summary);
            });
        }

        // Items whose program is still open would only half clean. Asks to close them; returns null to cancel.
        List<CleanupItem> SkipItemsWithOpenPrograms(List<CleanupItem> chosen)
        {
            List<string> running = chosen.SelectMany(i => i.MustClose).Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(p => { try { return Process.GetProcessesByName(p).Length > 0; } catch { return false; } }).ToList();
            if (running.Count == 0) return chosen;
            string names = string.Join("\r\n", running.Select(p => "  " + Catalog.AppName(p)).Distinct());
            string tip = L.T("Tip: some apps keep running near the clock. Right-click their icon there and choose Exit or Quit.");
            if (running.Contains("msedge", StringComparer.OrdinalIgnoreCase))
                tip += "\r\n" + L.T("Edge often runs in the background: in Edge, Settings > System and performance > turn off Startup boost and background apps.");
            if (!AskYesNo(L.F("These programs are still open:\r\n\r\n{0}\r\n\r\n{1}\r\n\r\nClick No, close them, then click Clean again.\r\nOr click Yes to skip the items that need them and clean the rest.", names, tip),
                          L.T("Close programs first")))
                return null;
            List<CleanupItem> rest = chosen.Where(i => !i.MustClose.Any(p => running.Contains(p, StringComparer.OrdinalIgnoreCase))).ToList();
            if (rest.Count == 0) { Msg(L.T("Nothing left to clean. Close the programs and try again.")); return null; }
            return rest;
        }

        // Runs on the background thread.
        void CleanItem(CleanupItem it, CancelFlag flag)
        {
            Log(L.F("Cleaning: {0}", it.Name));
            Ui(delegate { StatusText.Text = L.F("Cleaning: {0}", it.Name); });
            WorkContext context = new WorkContext();
            context.Log = Log;
            context.Cancel = flag;
            long freeBefore = FreeBytes();
            long? sizeBefore = it.Bytes;
            try
            {
                if (it.Clean != null) it.Clean(it, context);
                else if (it.Paths != null) foreach (string p in it.Paths().ToList()) FileOps.ClearFolder(p, 0, context.Counter);
            }
            catch (Exception ex) { Log("  " + L.F("Error: {0}", ex.Message)); }

            string result;
            if (sizeBefore.HasValue && it.SizeText == null)
            {
                MeasureItem(it, null);
                long removed = Math.Max(0, sizeBefore.Value - (it.Bytes ?? 0));
                result = "  " + L.F("Removed {0}. Left: {1}.", Fmt.Size(removed), Fmt.Size(it.Bytes));
                long? left = it.Bytes;
                Ui(delegate { it.SizeLabel.Text = Fmt.Size(left); UpdateGroupTotals(); });
            }
            else
            {
                long freed = Math.Max(0, FreeBytes() - freeBefore);
                result = "  " + L.F("Free space on {0} grew by {1}.", AppPaths.SysDrive, Fmt.Size(freed));
            }
            if (context.Counter.Failed > 0) result += " " + L.P(context.Counter.Failed, "1 file was in use and was skipped.", "{0} files were in use and were skipped.");
            Log(result);
            HistoryLog.Write(it.Name + " --" + result);
        }
    }
}
