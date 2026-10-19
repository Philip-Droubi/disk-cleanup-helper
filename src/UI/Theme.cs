// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Light and dark theme. The code writes light colours ("#0F172A"); Theme.Get maps each one to the current theme
// and remembers the light colour behind every brush, so switching themes can recolour the open window.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using DiskCleanupHelper.App;
using DiskCleanupHelper.UI.Controls;

namespace DiskCleanupHelper.UI
{
    public static class Theme
    {
        public static bool Dark;

        public const string AppBgLight = "#F1F5F9", AppBgDark = "#0B1220";

        // light colour -> dark colour. Colours not listed (brand blue, white text on buttons) stay the same.
        static readonly Dictionary<string, string> DarkMap = new Dictionary<string, string>
        {
            // surfaces and lines
            { "#FFFFFF", "#151E2E" }, { "#F8FAFC", "#1A2436" }, { "#F1F5F9", "#212C40" }, { "#EEF2F6", "#263247" },
            { "#E2E8F0", "#2A3549" }, { "#CBD5E1", "#3A4760" },
            // text
            { "#94A3B8", "#7D8CA3" }, { "#64748B", "#94A3B8" }, { "#475569", "#AEBACB" }, { "#334155", "#C7D0DC" }, { "#0F172A", "#E6EBF2" },
            // blue
            { "#2563EB", "#60A5FA" }, { "#1D4ED8", "#93C5FD" }, { "#1E3A8A", "#BFDBFE" }, { "#BFDBFE", "#2B4A80" },
            { "#DBEAFE", "#1B2E52" }, { "#EFF6FF", "#14213A" },
            // green
            { "#16A34A", "#22C55E" }, { "#15803D", "#4ADE80" }, { "#DCFCE7", "#11301F" },
            // amber
            { "#D97706", "#F59E0B" }, { "#B45309", "#FBBF24" }, { "#92400E", "#FCD34D" }, { "#F59E0B", "#FBBF24" },
            { "#FEF3C7", "#33270C" }, { "#FCD34D", "#7A5A12" },
            // red
            { "#DC2626", "#F87171" }, { "#B91C1C", "#F87171" }, { "#991B1B", "#FCA5A5" }, { "#FEE2E2", "#3A1618" },
            { "#FCA5A5", "#7F2A2A" }, { "#FEF2F2", "#3A1618" },
            // purple and cyan
            { "#7C3AED", "#A78BFA" }, { "#6D28D9", "#C4B5FD" }, { "#EDE9FE", "#2A2147" }, { "#0891B2", "#22D3EE" }
        };

        static readonly Dictionary<string, Brush> lightCache = new Dictionary<string, Brush>();
        static readonly Dictionary<string, Brush> darkCache = new Dictionary<string, Brush>();
        static readonly ConditionalWeakTable<Brush, string> tokens = new ConditionalWeakTable<Brush, string>();
        static readonly ConditionalWeakTable<Brush, string> fixedBrushes = new ConditionalWeakTable<Brush, string>();

        static string Norm(string hex) { return hex.Trim().ToUpperInvariant(); }

        static Brush Make(string hex)
        {
            Brush b = (Brush)new BrushConverter().ConvertFromString(hex);
            b.Freeze();
            return b;
        }

        // The brush for a light-theme colour, in the current theme.
        public static Brush Get(string lightHex)
        {
            string key = Norm(lightHex);
            Dictionary<string, Brush> cache = Dark ? darkCache : lightCache;
            Brush b;
            if (cache.TryGetValue(key, out b)) return b;
            string actual;
            if (!Dark || !DarkMap.TryGetValue(key, out actual)) actual = key;
            b = Make(actual);
            cache[key] = b;
            tokens.Add(b, key);
            return b;
        }

        // A colour that stays the same in both themes (for example white text on a coloured badge).
        public static Brush Fixed(string hex)
        {
            Brush b = Make(hex);
            fixedBrushes.Add(b, hex);
            return b;
        }

        // The current-theme version of a brush; null when it should stay as it is.
        public static Brush Remap(Brush b)
        {
            if (b == null) return null;
            string tok;
            if (fixedBrushes.TryGetValue(b, out tok)) return null;
            if (tokens.TryGetValue(b, out tok)) return Get(tok);
            // brushes from the window layout start out in light colours
            SolidColorBrush s = b as SolidColorBrush;
            if (s == null || s.Color.A != 255) return null;
            string hex = string.Format("#{0:X2}{1:X2}{2:X2}", s.Color.R, s.Color.G, s.Color.B);
            return DarkMap.ContainsKey(hex) ? Get(hex) : null;
        }

        public static Brush Current(Brush b)
        {
            Brush r = Remap(b);
            return r ?? b;
        }

        // ------------------------------------------------------------ apply to a window
        static readonly DependencyProperty[] BrushProps = {
            Panel.BackgroundProperty, Border.BackgroundProperty, Border.BorderBrushProperty, Control.BackgroundProperty,
            Control.BorderBrushProperty, TextElement.ForegroundProperty, TextBlock.BackgroundProperty, Shape.FillProperty, Shape.StrokeProperty
        };

        public static void Apply(Window w)
        {
            // the colours used by the window's styles
            foreach (object k in w.Resources.Keys.Cast<object>().ToList())
            {
                string key = k as string;
                if (key != null && key.StartsWith("T_") && key.Length == 8) w.Resources[key] = Get("#" + key.Substring(2));
            }
            w.Resources["AppBg"] = Make(Dark ? AppBgDark : AppBgLight);
            Recolor(w);
            SetDarkTitleBar(w);
        }

        // Swaps every colour set in code or in the layout. Parts tagged "NoTheme" (the dark header) are left alone.
        static void Recolor(DependencyObject d)
        {
            FrameworkElement fe = d as FrameworkElement;
            if (fe != null && "NoTheme".Equals(fe.Tag)) return;
            foreach (DependencyProperty p in BrushProps)
            {
                Brush b = d.ReadLocalValue(p) as Brush;
                if (b == null) continue;
                Brush nb = Remap(b);
                if (nb != null && !ReferenceEquals(nb, b)) d.SetValue(p, nb);
            }
            UIElement ue = d as UIElement;
            if (d is MiniChart) ue.InvalidateVisual();
            foreach (object c in LogicalTreeHelper.GetChildren(d))
            {
                DependencyObject cd = c as DependencyObject;
                if (cd != null) Recolor(cd);
            }
        }

        // ------------------------------------------------------------ title bar
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

        public static void SetDarkTitleBar(Window w)
        {
            try
            {
                IntPtr h = new WindowInteropHelper(w).Handle;
                if (h == IntPtr.Zero) return;
                int on = Dark ? 1 : 0;
                if (DwmSetWindowAttribute(h, 20, ref on, 4) != 0) DwmSetWindowAttribute(h, 19, ref on, 4);   // 19 on older Windows 10
                SetWindowPos(h, IntPtr.Zero, 0, 0, 0, 0, 0x0027);   // redraw the frame: NOSIZE | NOMOVE | NOZORDER | FRAMECHANGED
            }
            catch { }
        }

        // ------------------------------------------------------------ saved choice (light is the default)
        public static bool LoadSaved() { return string.Equals(AppSettings.Read("theme"), "dark", StringComparison.OrdinalIgnoreCase); }

        public static void Save() { AppSettings.Write("theme", Dark ? "dark" : "light"); }
    }
}
