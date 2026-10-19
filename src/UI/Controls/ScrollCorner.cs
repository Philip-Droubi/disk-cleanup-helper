// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Makes the square where a list's two scrollbars meet see-through.
// Windows' list template paints it in a fixed light grey, which shows as a white square in the dark theme.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DiskCleanupHelper.UI.Controls
{
    public static class ScrollCorner
    {
        /// <summary>Clears the corner each time the list is loaded (its template is applied on load).</summary>
        public static void Attach(ListView list)
        {
            list.Loaded += delegate { Clear(list); };
        }

        // The corner is the template part in row 1, column 1 of the list's ScrollViewer: a panel (with 1-pixel
        // lines inside) for a list with columns, or a plain rectangle otherwise.
        static void Clear(ListView list)
        {
            foreach (ScrollViewer sv in VisualTree.Descendants<ScrollViewer>(list))
            {
                foreach (FrameworkElement part in VisualTree.Descendants<FrameworkElement>(sv))
                {
                    if (part.TemplatedParent != sv || Grid.GetRow(part) != 1 || Grid.GetColumn(part) != 1) continue;
                    Panel panel = part as Panel;
                    if (panel != null)
                    {
                        panel.Background = Brushes.Transparent;
                        foreach (UIElement line in panel.Children) if (line is Shape) line.Visibility = Visibility.Hidden;
                    }
                    Shape shape = part as Shape;
                    if (shape != null) shape.Fill = Brushes.Transparent;
                }
            }
        }
    }
}
