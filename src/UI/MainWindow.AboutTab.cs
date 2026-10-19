// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// About tab: install state, the author link cards, the source code card and the legal notices.
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Install;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        void UpdateInstallState()
        {
            if (Installer.IsInstalled)
            {
                InstallState.Text = L.F("Installed in {0}. Open it any time from the Start menu or the desktop shortcut. To remove it: click Uninstall, or Settings > Apps > Disk Cleanup Helper.", AppPaths.InstallDir);
                BtnInstall.Content = Installer.RunningFromInstallDir ? L.T("Repair shortcuts") : L.T("Update installed copy");
                BtnUninstall.Visibility = Visibility.Visible;
            }
            else
            {
                InstallState.Text = L.F("Not installed. You are running it from {0}. Install adds a Start menu entry, a desktop shortcut and an entry in Settings > Apps so it can be removed like any program. No admin rights are needed.", AppPaths.ExeDir);
                BtnInstall.Content = L.T("Install on this PC");
                BtnUninstall.Visibility = Visibility.Collapsed;
            }
            UpdateInstallBar();
        }

        // The slim bar under the header, visible on every tab until the app is installed.
        void UpdateInstallBar()
        {
            bool show;
            if (!Installer.IsInstalled)
            {
                show = true;
                InstallBarText.Text = L.T("Keep Disk Cleanup Helper on this PC: install it to open it from the Start menu and the desktop. No admin rights needed.");
                BtnInstallBar.Content = L.T("Install on this PC");
            }
            else if (Installer.RunningFromInstallDir || SameAsInstalled())
            {
                show = false;
            }
            else
            {
                show = true;
                InstallBarText.Text = L.F("This copy ({0}) is not the one installed on this PC. Update the installed copy so the Start menu opens this version.", AppInfo.DisplayVersion);
                BtnInstallBar.Content = L.T("Update installed copy");
            }
            InstallBar.Visibility = show && !demo ? Visibility.Visible : Visibility.Collapsed;   // demo pictures: the tab, not the bar
        }

        static bool SameAsInstalled()
        {
            try
            {
                FileInfo a = new FileInfo(AppPaths.ExePath), b = new FileInfo(Installer.InstalledExe);
                return a.Length == b.Length && a.LastWriteTimeUtc == b.LastWriteTimeUtc;
            }
            catch { return false; }
        }

        // ------------------------------------------------------------ "Made by" link cards
        const string PortfolioUrl = "https://philip-droubi.github.io/Philip_Droubi_portfolio/";
        const string LinkedInUrl = "https://www.linkedin.com/in/philip-droubi/";
        const string SourceUrl = "https://github.com/" + Updater.Repo;
        const string LicenseUrl = SourceUrl + "#license";   // the README's License section links LICENSE and NOTICE

        // The author attribution that NOTICE term 1 protects. It stays English in every language. A changed version shows
        // "Based on Disk Cleanup Helper by Philip Droubi - https://github.com/Philip-Droubi" here instead (term 1c).
        const string Credit = "Disk Cleanup Helper by Philip Droubi - ";
        const string CreditUrl = "https://github.com/Philip-Droubi";

        // Opens a web link through File Explorer, so the browser starts as a normal app and not as administrator.
        static void OpenWebLink(string url)
        {
            try { Process.Start("explorer.exe", "\"" + url + "\""); }
            catch { Env.OpenUri(url); }
        }

        static ImageSource ResourceImage(string name)
        {
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("DiskCleanupHelper." + name))
                {
                    if (s == null) return null;
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = s;
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
            }
            catch { return null; }
        }

        Border LinkCard(string url, FrameworkElement picture, string title, string description, string domain)
        {
            Border card = Card();
            card.Padding = new Thickness(0);
            card.Margin = new Thickness(0, 0, 0, 10);
            card.Cursor = Cursors.Hand;
            card.ToolTip = url;
            Grid g = new Grid();
            g.ColumnDefinitions.Add(Col(new GridLength(220)));
            g.ColumnDefinitions.Add(Col(Star));
            g.Children.Add(picture);
            StackPanel sp = new StackPanel();
            sp.Margin = new Thickness(18, 14, 18, 14);
            sp.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(Text(title, 15, "#0F172A", true, true));
            TextBlock desc = Text(description, 13, "#475569", false, true);
            desc.LineHeight = 19;
            desc.Margin = new Thickness(0, 4, 0, 6);
            sp.Children.Add(desc);
            sp.Children.Add(Text(domain + "   " + L.Arrow, 12.5, "#2563EB", true, false));
            Grid.SetColumn(sp, 1);
            g.Children.Add(sp);
            card.Child = g;
            card.MouseEnter += delegate { card.Background = HoverBrush; card.BorderBrush = B("#BFDBFE"); };
            card.MouseLeave += delegate { card.Background = B("#FFFFFF"); card.BorderBrush = B("#E2E8F0"); };
            card.MouseLeftButtonUp += delegate { OpenWebLink(url); };
            return card;
        }

        void BuildAuthorCards()
        {
            AuthorPanel.Children.Clear();
            StackPanel head = new StackPanel();
            head.Margin = new Thickness(2, 6, 0, 10);
            head.Children.Add(Text(L.T("Made by Philip Droubi"), 17, "#0F172A", true, false));
            head.Children.Add(Spaced(Text(L.T("Software Engineer. Questions, ideas or feedback about Disk Cleanup Helper are welcome."), 12.5, "#64748B", false, true), 2, 0));
            AuthorPanel.Children.Add(head);
            AuthorPanel.Children.Add(LinkCard(PortfolioUrl, PortfolioPicture(),
                L.T("Philip Droubi | Software Engineer"),
                L.T("Philip Droubi is a Software Engineer."),
                "philip-droubi.github.io"));
            AuthorPanel.Children.Add(LinkCard(LinkedInUrl, LinkedInBadge(),
                "Philip Droubi | LinkedIn",
                L.T("Software Engineer. Connect with Philip Droubi on LinkedIn."),
                "linkedin.com/in/philip-droubi"));
            AuthorPanel.Children.Add(LinkCard(SourceUrl, GitHubBadge(),
                L.T("Source code on GitHub"),
                L.T("Free and open source under the GNU GPL v3: use it, change it and share it. Copies keep the credit to Philip Droubi."),
                "github.com/Philip-Droubi/disk-cleanup-helper"));
            AuthorPanel.Children.Add(LegalNotice());
        }

        // The notices the GNU GPL asks a program with a window to show (its "Appropriate Legal Notices"): the author
        // attribution, the copyright, the permission to use, change and share it, no warranty, and the license itself.
        StackPanel LegalNotice()
        {
            StackPanel notice = new StackPanel();
            notice.Margin = new Thickness(2, 2, 0, 0);
            TextBlock credit = Text(Credit, 12.5, "#334155", true, true);
            credit.Inlines.Add(Link(CreditUrl, delegate { OpenWebLink(CreditUrl); }));
            if (L.Rtl) { credit.FlowDirection = FlowDirection.LeftToRight; credit.TextAlignment = TextAlignment.Right; }
            notice.Children.Add(credit);
            TextBlock legal = Text(L.F("© {0} Philip Droubi. Free software: you may use, change and share it under the GNU General Public License v3 with the additional terms in NOTICE. It comes with no warranty.", AppInfo.CopyrightYears) + " ", 12, "#64748B", false, true);
            legal.LineHeight = 18;
            legal.Margin = new Thickness(0, 4, 0, 0);
            legal.Inlines.Add(Link(L.T("License and terms"), delegate { OpenLicense(); }));
            notice.Children.Add(legal);
            return notice;
        }

        Hyperlink Link(string text, RoutedEventHandler click)
        {
            Hyperlink link = new Hyperlink(new Run(text));
            link.Foreground = B("#2563EB");
            link.Click += click;
            return link;
        }

        // NOTICE and the GNU GPL, read from inside the exe and shown as one file in Notepad, so every copy can show them,
        // also offline and when it is only the exe (installed, self-updated). The GitHub page is the fallback.
        void OpenLicense()
        {
            try
            {
                string text = ResourceText("NOTICE.txt") + "\n\n" + new string('=', 117) + "\n\n" + ResourceText("LICENSE.txt");
                Directory.CreateDirectory(AppPaths.DataDir);
                string p = Path.Combine(AppPaths.DataDir, "LICENSE and NOTICE.txt");
                File.WriteAllText(p, text.Replace("\r\n", "\n").Replace("\n", "\r\n"), new UTF8Encoding(false));
                Process.Start("notepad.exe", "\"" + p + "\"");
            }
            catch { OpenWebLink(LicenseUrl); }
        }

        static string ResourceText(string name)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("DiskCleanupHelper." + name))
            using (StreamReader r = new StreamReader(s, Encoding.UTF8)) return r.ReadToEnd();
        }

        // The portfolio site's own preview picture.
        static Border PortfolioPicture()
        {
            Border picture = new Border();
            picture.Height = 116;
            picture.CornerRadius = new CornerRadius(10, 0, 0, 10);
            ImageSource image = ResourceImage("portfolio-og.jpg");
            if (image != null) { ImageBrush brush = new ImageBrush(image); brush.Stretch = Stretch.UniformToFill; picture.Background = brush; }
            else picture.Background = B("#0F172A");
            if (L.Rtl)
            {
                // a picture is never mirrored; it sits on the right, so round its right-hand corners
                picture.FlowDirection = FlowDirection.LeftToRight;
                picture.CornerRadius = new CornerRadius(0, 10, 10, 0);
            }
            return picture;
        }

        // The GitHub mark, drawn (no picture file), white on GitHub's own dark grey in both themes. The path is Octicons'
        // "mark-github" (MIT License, GitHub Inc.; see NOTICE, Third-party material).
        const string GitHubMark = "M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.013 8.013 0 0016 8c0-4.42-3.58-8-8-8z";

        static Border GitHubBadge()
        {
            System.Windows.Shapes.Path mark = new System.Windows.Shapes.Path();
            mark.Data = Geometry.Parse(GitHubMark);
            mark.Fill = Theme.Fixed("#FFFFFF");
            mark.Stretch = Stretch.Uniform;
            mark.Width = 54; mark.Height = 54;
            mark.HorizontalAlignment = HorizontalAlignment.Center;
            mark.VerticalAlignment = VerticalAlignment.Center;
            mark.FlowDirection = FlowDirection.LeftToRight;   // a logo is never mirrored in right-to-left languages
            Border badge = new Border();
            badge.Height = 116;
            badge.CornerRadius = new CornerRadius(10, 0, 0, 10);
            badge.Background = Theme.Fixed("#24292F");
            badge.Child = mark;
            return badge;
        }

        // A simple drawn badge (LinkedIn does not share preview pictures).
        static Border LinkedInBadge()
        {
            TextBlock letters = Text("in", 34, "#0A66C2", true, false);
            letters.FontWeight = FontWeights.Bold;
            letters.HorizontalAlignment = HorizontalAlignment.Center;
            letters.VerticalAlignment = VerticalAlignment.Center;
            letters.Margin = new Thickness(0, -6, 0, 0);
            Border mark = new Border();
            mark.Width = 58; mark.Height = 58;
            mark.CornerRadius = new CornerRadius(10);
            mark.Background = Theme.Fixed("#FFFFFF");   // stays white in both themes
            mark.HorizontalAlignment = HorizontalAlignment.Center;
            mark.VerticalAlignment = VerticalAlignment.Center;
            mark.Child = letters;
            Border badge = new Border();
            badge.Height = 116;
            badge.CornerRadius = new CornerRadius(10, 0, 0, 10);
            badge.Background = B("#0A66C2");
            badge.Child = mark;
            return badge;
        }
    }
}
