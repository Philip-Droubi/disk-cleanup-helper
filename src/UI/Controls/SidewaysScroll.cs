// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Sideways scrolling with a touchpad or a tilting mouse wheel.
// WPF on .NET 4.8 ignores WM_MOUSEHWHEEL, so wide lists could not be moved sideways with two fingers.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace DiskCleanupHelper.UI.Controls
{
    public static class SidewaysScroll
    {
        const int WM_MOUSEHWHEEL = 0x020E;
        const double PixelsPerUnit = 0.5;   // one wheel notch (120) = 60 pixels

        public static void Attach(Window window)
        {
            window.SourceInitialized += delegate
            {
                HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
                if (source != null)
                    source.AddHook(delegate(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
                    {
                        return msg == WM_MOUSEHWHEEL ? OnWheel(window, wParam, lParam, ref handled) : IntPtr.Zero;
                    });
            };
        }

        // Scrolls the nearest area under the pointer that can move sideways.
        static IntPtr OnWheel(Window window, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            int delta = (short)((wParam.ToInt64() >> 16) & 0xFFFF);   // > 0 = to the right
            for (DependencyObject d = ElementUnderPointer(window, lParam); d != null; d = VisualTree.Parent(d))
            {
                ScrollViewer sv = d as ScrollViewer;
                if (sv == null || sv.ScrollableWidth <= 0) continue;
                if (sv.FlowDirection == FlowDirection.RightToLeft) delta = -delta;   // mirrored content
                sv.ScrollToHorizontalOffset(sv.HorizontalOffset + delta * PixelsPerUnit);
                handled = true;
                return new IntPtr(1);
            }
            return IntPtr.Zero;
        }

        // The message carries the pointer's screen position; that is more reliable than Mouse.DirectlyOver.
        static DependencyObject ElementUnderPointer(Window window, IntPtr lParam)
        {
            long lp = lParam.ToInt64();
            try
            {
                Point at = window.PointFromScreen(new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF)));
                HitTestResult hit = VisualTreeHelper.HitTest(window, at);
                if (hit != null) return hit.VisualHit;
            }
            catch { }
            return Mouse.DirectlyOver as DependencyObject;
        }
    }
}
