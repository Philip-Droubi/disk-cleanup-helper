// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Calmer mouse-wheel scrolling: shorter steps that glide (Windows jumps about 48 pixels per notch).
// When the area under the mouse is already at its end, the scroll passes on to the area around it.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DiskCleanupHelper.UI.Controls
{
    public static class SmoothScroll
    {
        // pixels per wheel notch = 120 * Speed  (Windows' default is about 48)
        const double Speed = 0.28;
        // share of the remaining distance covered each frame (higher = snappier)
        const double Ease = 0.22;

        static readonly Dictionary<ScrollViewer, double> targets = new Dictionary<ScrollViewer, double>();
        static bool hooked;

        public static void Attach(Window w)
        {
            w.AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnWheel), true);
        }

        static void OnWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled || Keyboard.Modifiers != ModifierKeys.None) return;
            DependencyObject d = e.OriginalSource as DependencyObject;
            Visual root = sender as Visual;
            // leave drop-down lists (they live in their own popup window) to Windows
            Visual v = d as Visual;
            if (v != null && root != null && PresentationSource.FromVisual(v) != PresentationSource.FromVisual(root)) return;
            while (d != null)
            {
                ScrollViewer sv = d as ScrollViewer;
                if (sv != null && sv.ScrollableHeight > 0 && !sv.CanContentScroll)
                {
                    double cur;
                    if (!targets.TryGetValue(sv, out cur)) cur = sv.VerticalOffset;
                    bool canMove = e.Delta > 0 ? cur > 0.5 : cur < sv.ScrollableHeight - 0.5;
                    if (canMove)
                    {
                        targets[sv] = Math.Max(0, Math.Min(sv.ScrollableHeight, cur - e.Delta * Speed));
                        if (!hooked) { CompositionTarget.Rendering += Tick; hooked = true; }
                        e.Handled = true;
                        return;
                    }
                }
                d = VisualTree.Parent(d);
            }
        }

        static void Tick(object sender, EventArgs e)
        {
            foreach (KeyValuePair<ScrollViewer, double> kv in targets.ToList())
            {
                ScrollViewer sv = kv.Key;
                double target = Math.Min(kv.Value, sv.ScrollableHeight);
                double cur = sv.VerticalOffset;
                double next = cur + (target - cur) * Ease;
                if (Math.Abs(target - next) < 0.5 || !sv.IsVisible)
                {
                    sv.ScrollToVerticalOffset(target);
                    targets.Remove(sv);
                }
                else sv.ScrollToVerticalOffset(next);
            }
            if (targets.Count == 0)
            {
                CompositionTarget.Rendering -= Tick;
                hooked = false;
            }
        }
    }
}
