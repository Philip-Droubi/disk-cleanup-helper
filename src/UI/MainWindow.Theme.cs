// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Light and dark theme button.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        const string MoonIcon = "M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z";
        const string SunIcon = "M12 7A5 5 0 1 1 12 17A5 5 0 1 1 12 7Z M12 1V3 M12 21V23 M4.22 4.22L5.64 5.64 M18.36 18.36L19.78 19.78 " +
                               "M1 12H3 M21 12H23 M4.22 19.78L5.64 18.36 M18.36 5.64L19.78 4.22";

        // The button shows the theme it switches to.
        void UpdateThemeButton()
        {
            ThemeIcon.Data = Geometry.Parse(Theme.Dark ? SunIcon : MoonIcon);
            ThemeLabel.Text = Theme.Dark ? L.T("LIGHT MODE") : L.T("DARK MODE");
            BtnTheme.ToolTip = Theme.Dark ? L.T("Switch to the light theme") : L.T("Switch to the dark theme");
        }

        void ToggleTheme()
        {
            Theme.Dark = !Theme.Dark;
            Theme.Apply(Window);
            UpdateThemeButton();
            Theme.Save();
            Log(Theme.Dark ? L.T("Switched to the dark theme.") : L.T("Switched to the light theme."));
        }
    }
}
