// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Language menu and switching language.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        void ShowLanguageMenu()
        {
            ContextMenu menu = new ContextMenu();
            menu.Template = MenuTemplate();
            menu.Foreground = B("#0F172A");
            menu.FontFamily = Window.FontFamily;
            menu.FontSize = 13;
            menu.MinWidth = 210;
            menu.FlowDirection = Window.FlowDirection;
            ControlTemplate itemTemplate = MenuItemTemplate();
            foreach (LangInfo li in L.Available())
            {
                MenuItem mi = new MenuItem();
                mi.Template = itemTemplate;
                // right-to-left menu: a mark after a Latin name keeps its closing bracket in place ("Português (Brasil)")
                mi.Header = L.Rtl && !L.HasRtlLetters(li.Name) ? li.Name + "\u200E" : li.Name;
                mi.IsChecked = li.Code == L.Code;
                mi.Foreground = B("#0F172A");
                string code = li.Code;
                mi.Click += delegate { SwitchLanguage(code); };
                menu.Items.Add(mi);
            }
            menu.PlacementTarget = BtnLang;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            BtnLang.ContextMenu = menu;
            menu.IsOpen = true;
        }

        // Windows' own menu template has a light icon strip the dark theme cannot reach, so the menu gets its own
        // templates, built with the current theme's colours each time it opens.
        static string Hex(string light) { return ((SolidColorBrush)B(light)).Color.ToString(); }

        static ControlTemplate MenuTemplate()
        {
            return (ControlTemplate)XamlReader.Parse(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ContextMenu'>" +
                "<Border Background='" + Hex("#FFFFFF") + "' BorderBrush='" + Hex("#CBD5E1") + "' BorderThickness='1' CornerRadius='8' Padding='4'>" +
                "<ItemsPresenter KeyboardNavigation.DirectionalNavigation='Cycle'/>" +
                "</Border></ControlTemplate>");
        }

        static ControlTemplate MenuItemTemplate()
        {
            return (ControlTemplate)XamlReader.Parse(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='MenuItem'>" +
                "<Border x:Name='Bd' Background='Transparent' CornerRadius='5' Padding='8,5,14,5'>" +
                "<Grid><Grid.ColumnDefinitions><ColumnDefinition Width='24'/><ColumnDefinition Width='*'/></Grid.ColumnDefinitions>" +
                "<Path x:Name='Tick' Data='M 1,5 L 4.5,8.5 L 11,1.5' Stroke='" + Hex("#2563EB") + "' StrokeThickness='2' StrokeStartLineCap='Round' " +
                "StrokeEndLineCap='Round' StrokeLineJoin='Round' VerticalAlignment='Center' Visibility='Hidden'/>" +
                "<ContentPresenter Grid.Column='1' ContentSource='Header' RecognizesAccessKey='False' VerticalAlignment='Center'/></Grid></Border>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property='IsChecked' Value='True'><Setter TargetName='Tick' Property='Visibility' Value='Visible'/>" +
                "<Setter TargetName='Bd' Property='Background' Value='" + Hex("#EFF6FF") + "'/></Trigger>" +
                "<Trigger Property='IsHighlighted' Value='True'><Setter TargetName='Bd' Property='Background' Value='" + Hex("#F1F5F9") + "'/></Trigger>" +
                "</ControlTemplate.Triggers></ControlTemplate>");
        }

        // Builds the window again in the new language inside the running app: same place, size and tab, same rights.
        // Every text is set when its part of the window is built, so a fresh window is simpler and safer than
        // re-translating the open one. Measuring sizes is simply stopped (it starts again in the new window).
        void SwitchLanguage(string code)
        {
            if (code == L.Code) return;
            if (busy && !measuring) { Msg(L.T("Please wait until the current task finishes, or click Stop.")); return; }
            L.Save(code);
            try
            {
                L.Init(code);
                MainWindow next = new MainWindow(options);
                next.TakePlacementFrom(Window);
                next.startTab = Tabs.SelectedIndex;
                Application.Current.MainWindow = next.Window;   // so closing this window does not end the app
                next.Window.Show();
                Window.Close();
            }
            catch (Exception ex)
            {
                Log(L.F("Problem: {0}", ex.Message));
                RestartApp();
            }
        }

        // Fallback: starts the app again with the same rights (no new Windows prompt) and closes this copy.
        void RestartApp()
        {
            try
            {
                List<string> args = new List<string>();
                string[] cur = Environment.GetCommandLineArgs();
                for (int i = 1; i < cur.Length; i++)
                {
                    string a = cur[i].ToLowerInvariant();
                    if (a == "/lang" || a == "/theme" || a == "/snapshot" || a == "/tab" || a == "/scroll" || a == "/fold" || a == "/hover" || a == "/updated") { i++; continue; }
                    args.Add(Env.QuoteArg(cur[i]));
                }
                if (!Env.IsAdmin && !cur.Any(x => x.ToLowerInvariant() == "/noelevate")) args.Add("/noelevate");
                ProcessStartInfo psi = new ProcessStartInfo(AppPaths.ExePath, string.Join(" ", args));
                psi.UseShellExecute = false;
                Process.Start(psi);
                Window.Close();
            }
            catch (Exception ex) { Msg(L.F("Could not restart the app: {0}", ex.Message)); }
        }
    }
}
