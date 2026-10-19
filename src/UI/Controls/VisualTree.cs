// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Walking the WPF element tree.
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DiskCleanupHelper.UI.Controls
{
    public static class VisualTree
    {
        /// <summary>The parent element; also works for text pieces such as a Run, which are not visuals.</summary>
        public static DependencyObject Parent(DependencyObject d)
        {
            if (d is Visual || d is Visual3D) return VisualTreeHelper.GetParent(d);
            return LogicalTreeHelper.GetParent(d);
        }

        /// <summary>Every element of type T inside root, depth first.</summary>
        public static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                T match = child as T;
                if (match != null) yield return match;
                foreach (T d in Descendants<T>(child)) yield return d;
            }
        }
    }
}
