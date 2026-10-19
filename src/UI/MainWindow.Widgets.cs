// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Small building blocks used by every tab (texts, cards, pills, grids).
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiskCleanupHelper.Localization;
using DiskCleanupHelper.UI.Controls;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        // Every text in code-built parts of the window goes through here, so fonts and colours stay consistent.
        static TextBlock Text(string text, double size, string color, bool bold, bool wrap)
        {
            TextBlock t = new TextBlock();
            t.Text = text; t.FontSize = size; t.Foreground = B(color);
            if (bold) t.FontWeight = FontWeights.SemiBold;
            if (wrap) t.TextWrapping = TextWrapping.Wrap;
            return t;
        }

        // Right-to-left languages: an English value (model, version, size) is drawn left to right next to its label,
        // and "  ·  "-joined values become separate pieces so each keeps its own direction.
        FrameworkElement ValueText(string text, double size, string color, bool bold, bool wrap)
        {
            if (!L.Rtl || string.IsNullOrEmpty(text)) return Text(text, size, color, bold, wrap);
            string[] parts = text.Split(new[] { "  ·  " }, StringSplitOptions.None);
            if (parts.Length == 1) return Mixed(text, size, color, bold, wrap);
            WrapPanel wp = new WrapPanel();
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) wp.Children.Add(Text("  ·  ", size, "#94A3B8", false, false));
                wp.Children.Add(Mixed(parts[i], size, color, bold, wrap));   // a long piece wraps inside the panel
            }
            return wp;
        }

        // Arabic words ending in an English value ("drive D: E:"): the value becomes its own piece, because WPF flips
        // punctuation at the very end of a right-to-left line.
        FrameworkElement Mixed(string s, double size, string color, bool bold, bool wrap)
        {
            if (L.Rtl && L.HasRtlLetters(s) && s.EndsWith(L.LtrEnd))
            {
                int k = s.LastIndexOf(L.LtrStart, StringComparison.Ordinal);
                if (k > 0 && !L.HasRtlLetters(s.Substring(k)))
                {
                    StackPanel sp = new StackPanel();
                    sp.Orientation = Orientation.Horizontal;
                    sp.Children.Add(Text(s.Substring(0, k).TrimEnd(), size, color, bold, false));
                    Border gap = new Border();
                    gap.Width = size * 0.3;
                    sp.Children.Add(gap);
                    sp.Children.Add(Piece(s.Substring(k), size, color, bold, false));
                    return sp;
                }
            }
            return Piece(s, size, color, bold, wrap);
        }

        // An all-English piece reads left to right even in a right-to-left window.
        TextBlock Piece(string s, double size, string color, bool bold, bool wrap)
        {
            TextBlock t = Text(s, size, color, bold, wrap);
            if (L.Rtl && !L.HasRtlLetters(s))
            {
                t.FlowDirection = FlowDirection.LeftToRight;
                t.TextAlignment = TextAlignment.Right;
            }
            return t;
        }

        // A small rounded label, such as "SAFE" or "Close Chrome first".
        static Border Pill(string text, string fg, string bg)
        {
            Border b = new Border();
            b.Background = B(bg);
            b.CornerRadius = new CornerRadius(9);
            b.Padding = new Thickness(8, 2, 8, 2);
            b.VerticalAlignment = VerticalAlignment.Center;
            b.HorizontalAlignment = HorizontalAlignment.Left;
            b.Child = Text(text, 10.5, fg, true, false);
            return b;
        }

        static Border Card()
        {
            Border c = new Border();
            c.Background = B("#FFFFFF");
            c.CornerRadius = new CornerRadius(10);
            c.BorderBrush = B("#E2E8F0");
            c.BorderThickness = new Thickness(1);
            c.Margin = new Thickness(0, 0, 0, 12);
            return c;
        }

        static ColumnDefinition Col(GridLength width) { ColumnDefinition c = new ColumnDefinition(); c.Width = width; return c; }
        static GridLength Star { get { return new GridLength(1, GridUnitType.Star); } }

        // Background of a row or card under the mouse.
        static Brush HoverBrush { get { return B("#F8FAFC"); } }

        static FrameworkElement Spaced(FrameworkElement e, double top, double bottom)
        {
            e.Margin = new Thickness(0, top, 0, bottom);
            return e;
        }

        // A big number with a small label above and an optional word below (HEALTH 98% Good).
        StackPanel StatBlock(string label, string value, string color, string sub)
        {
            StackPanel sp = new StackPanel();
            sp.Margin = new Thickness(0, 0, 0, 8);
            sp.Children.Add(Text(label, 11, "#64748B", true, false));
            sp.Children.Add(Text(value, 26, color, true, false));
            if (!string.IsNullOrEmpty(sub)) sp.Children.Add(Text(sub, 12, color, true, false));
            return sp;
        }

        StackPanel ChartBox(string title, MiniChart chart, double height, string footer)
        {
            StackPanel sp = new StackPanel();
            sp.Margin = new Thickness(0, 0, 0, 6);
            sp.Children.Add(Text(title, 12, "#475569", true, false));
            chart.Height = height;
            chart.Margin = new Thickness(0, 4, 0, 0);
            sp.Children.Add(chart);
            if (footer != null) sp.Children.Add(Spaced(Text(footer, 11, "#94A3B8", false, true), 2, 0));
            return sp;
        }
    }
}
