// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Finds text that does not fit: wider or taller than its own box, or cut off by a parent that clips it.
// Used by the /layoutcheck test switch to check every tab in every language; never runs in normal use.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace DiskCleanupHelper.UI.Controls
{
    public static class LayoutCheck
    {
        const double Slack = 1.5;   // layout rounding

        /// <summary>One line per text that is cut off, empty when everything fits.</summary>
        public static List<string> Find(FrameworkElement root)
        {
            List<string> problems = new List<string>();
            foreach (TextBlock tb in VisualTree.Descendants<TextBlock>(root))
            {
                string text = tb.Text;
                if (!tb.IsVisible || tb.ActualWidth <= 0 || string.IsNullOrWhiteSpace(text)) continue;
                if (InListCell(tb)) continue;   // file names and paths are cut by the column on purpose; columns can be widened
                string why = TooBig(tb, text) ?? CutByParent(tb, root);
                if (why != null) problems.Add(why + "  \"" + OneLine(text) + "\"");
            }
            return problems;
        }

        // Compares the text's natural size with the box it was given.
        static string TooBig(TextBlock tb, string text)
        {
            Typeface face = new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch);
            FormattedText ft = new FormattedText(text, CultureInfo.CurrentUICulture, tb.FlowDirection, face, tb.FontSize,
                Brushes.Black, VisualTreeHelper.GetDpi(tb).PixelsPerDip);
            if (!double.IsNaN(tb.LineHeight)) ft.LineHeight = tb.LineHeight;
            double width = tb.ActualWidth - tb.Padding.Left - tb.Padding.Right;
            double height = tb.ActualHeight - tb.Padding.Top - tb.Padding.Bottom;
            string trimmed = tb.TextTrimming != TextTrimming.None ? " (shortened with ...)" : "";
            if (tb.TextWrapping == TextWrapping.NoWrap)
            {
                if (ft.WidthIncludingTrailingWhitespace > width + Slack)
                    return string.Format(CultureInfo.InvariantCulture, "too wide{0}: needs {1:0}, has {2:0}", trimmed, ft.WidthIncludingTrailingWhitespace, width);
                return null;
            }
            ft.MaxTextWidth = Math.Max(1, width + Slack);   // a box sized to its text must not wrap here by a rounding error
            if (ft.Height > height + Slack)
                return string.Format(CultureInfo.InvariantCulture, "too tall{0}: needs {1:0}, has {2:0}", trimmed, ft.Height, height);
            return null;
        }

        // Walks up from the text and checks it against every element that clips what is inside it. Scrolling areas only
        // count in the directions they cannot scroll.
        static string CutByParent(TextBlock tb, FrameworkElement root)
        {
            Rect box = tb.TransformToAncestor(root).TransformBounds(new Rect(tb.RenderSize));
            for (DependencyObject d = tb; d != null && d != root; d = VisualTreeHelper.GetParent(d))
            {
                FrameworkElement fe = d as FrameworkElement;
                if (fe == null) continue;
                bool sides = true, ends = true;
                Rect area;
                ScrollContentPresenter viewport = fe as ScrollContentPresenter;
                if (viewport != null)
                {
                    ScrollViewer sv = viewport.TemplatedParent as ScrollViewer;
                    if (sv != null)
                    {
                        sides = sv.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled;
                        ends = sv.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled;
                    }
                    area = new Rect(fe.RenderSize);
                }
                else
                {
                    Geometry clip = LayoutInformation.GetLayoutClip(fe);
                    if (clip == null || clip.IsEmpty()) continue;
                    area = clip.Bounds;
                }
                Rect cut = fe.TransformToAncestor(root).TransformBounds(area);
                if (sides && (box.Left < cut.Left - Slack || box.Right > cut.Right + Slack))
                    return string.Format(CultureInfo.InvariantCulture, "cut at the side by {0}: {1:0} px hidden", Name(fe),
                        Math.Max(cut.Left - box.Left, box.Right - cut.Right));
                if (ends && (box.Top < cut.Top - Slack || box.Bottom > cut.Bottom + Slack))
                    return string.Format(CultureInfo.InvariantCulture, "cut at the top or bottom by {0}: {1:0} px hidden", Name(fe),
                        Math.Max(cut.Top - box.Top, box.Bottom - cut.Bottom));
                // Scrolled out of view is fine; from here up, only the visible part of the scrolling area counts.
                if (!sides) box = new Rect(cut.Left, box.Top, cut.Width, box.Height);
                if (!ends) box = new Rect(box.Left, cut.Top, box.Width, cut.Height);
            }
            if (box.Left < -Slack || box.Right > root.ActualWidth + Slack) return "outside the window";
            return null;
        }

        static bool InListCell(DependencyObject d)
        {
            for (; d != null; d = VisualTreeHelper.GetParent(d)) if (d is GridViewRowPresenter) return true;
            return false;
        }

        static string Name(FrameworkElement fe)
        {
            return string.IsNullOrEmpty(fe.Name) ? fe.GetType().Name : fe.GetType().Name + " " + fe.Name;
        }

        static string OneLine(string text)
        {
            string s = text.Replace("\r", " ").Replace("\n", " ");
            return s.Length > 90 ? s.Substring(0, 90) + "..." : s;
        }
    }
}
