// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Tips & FAQ tab (the texts live in HelpContent.cs).
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        StackPanel faqList;
        TextBlock faqNoMatch;

        static string TagText(string tag)
        {
            switch (tag) { case "SPACE": return L.T("SPACE"); case "SPEED": return L.T("SPEED"); case "HEALTH": return L.T("HEALTH"); default: return L.T("SAFETY"); }
        }

        static string[] TagColors(string tag)
        {
            switch (tag)
            {
                case "SPACE": return new[] { "#1D4ED8", "#DBEAFE" };
                case "SPEED": return new[] { "#6D28D9", "#EDE9FE" };
                case "HEALTH": return new[] { "#15803D", "#DCFCE7" };
                default: return new[] { "#B45309", "#FEF3C7" };
            }
        }

        StackPanel SectionHead(string title, string sub)
        {
            StackPanel sp = new StackPanel();
            sp.Margin = new Thickness(2, 4, 0, 10);
            sp.Children.Add(Text(title, 17, "#0F172A", true, false));
            if (sub != null) sp.Children.Add(Spaced(Text(sub, 12.5, "#64748B", false, true), 2, 0));
            return sp;
        }

        Border TipCard(Tip t)
        {
            Border b = Card();
            b.Padding = new Thickness(16, 12, 16, 14);
            b.Margin = new Thickness(0, 0, 10, 10);
            StackPanel sp = new StackPanel();
            string[] c = TagColors(t.Tag);
            sp.Children.Add(Pill(TagText(t.Tag), c[0], c[1]));
            sp.Children.Add(Spaced(Text(t.Title, 14, "#0F172A", true, true), 8, 4));
            TextBlock body = Text(t.Text, 12.5, "#475569", false, true);
            body.LineHeight = 19;
            sp.Children.Add(body);
            b.Child = sp;
            return b;
        }

        Border FaqItem(Faq f, bool open)
        {
            Border b = Card();
            b.Padding = new Thickness(18, 12, 18, 12);
            b.Margin = new Thickness(0, 0, 10, 8);
            b.Cursor = Cursors.Hand;
            b.Tag = f;
            StackPanel sp = new StackPanel();
            Grid head = new Grid();
            head.ColumnDefinitions.Add(Col(Star));
            head.ColumnDefinitions.Add(Col(GridLength.Auto));
            head.Children.Add(Text(f.Question, 13.5, "#0F172A", true, true));
            TextBlock sign = Text(open ? "−" : "+", 18, "#2563EB", true, false);
            sign.Margin = new Thickness(12, -4, 0, 0);
            Grid.SetColumn(sign, 1);
            head.Children.Add(sign);
            sp.Children.Add(head);
            TextBlock ans = Text(f.Answer, 13, "#475569", false, true);
            ans.LineHeight = 20;
            ans.Margin = new Thickness(0, 8, 24, 2);
            ans.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            sp.Children.Add(ans);
            b.Child = sp;
            b.MouseLeftButtonUp += delegate
            {
                bool show = ans.Visibility != Visibility.Visible;
                ans.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                sign.Text = show ? "−" : "+";
            };
            b.MouseEnter += delegate { b.Background = HoverBrush; };
            b.MouseLeave += delegate { b.Background = B("#FFFFFF"); };
            return b;
        }

        void FilterFaqs(string text)
        {
            string q = (text ?? "").Trim();
            int shown = 0;
            foreach (UIElement el in faqList.Children)
            {
                Border b = el as Border;
                Faq f = b == null ? null : b.Tag as Faq;
                if (f == null) continue;
                bool match = q.Length == 0
                    || f.Question.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                    || f.Answer.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                b.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
                if (match) shown++;
            }
            faqNoMatch.Visibility = shown == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        void BuildHelpTab()
        {
            HelpPanel.Children.Clear();

            HelpPanel.Children.Add(SectionHead(L.T("Tips"), L.T("Simple habits that keep your disk from filling up and your PC running well.")));
            System.Windows.Controls.Primitives.UniformGrid tips = new System.Windows.Controls.Primitives.UniformGrid();
            tips.Columns = 2;
            foreach (Tip t in HelpContent.Tips) tips.Children.Add(TipCard(t));
            HelpPanel.Children.Add(tips);

            HelpPanel.Children.Add(SectionHead(L.T("How Disk Cleanup Helper keeps you safe"), null));
            HelpPanel.Children.Add(SafetyCard());

            HelpPanel.Children.Add(SectionHead(L.T("Frequently asked questions"), L.T("Click a question to see the answer.")));
            HelpPanel.Children.Add(FaqSearchBox());
            faqList = new StackPanel();
            bool first = true;
            foreach (Faq f in HelpContent.Faqs)
            {
                faqList.Children.Add(FaqItem(f, first && options.SnapshotPath != null));   // snapshots show one answer open
                first = false;
            }
            HelpPanel.Children.Add(faqList);
            faqNoMatch = Text(L.T("No questions match your search."), 13, "#64748B", false, false);
            faqNoMatch.Margin = new Thickness(2, 4, 0, 0);
            faqNoMatch.Visibility = Visibility.Collapsed;
            HelpPanel.Children.Add(faqNoMatch);
        }

        Border SafetyCard()
        {
            StackPanel lines = new StackPanel();
            foreach (string s in HelpContent.Safety)
            {
                Grid row = new Grid();
                row.Margin = new Thickness(0, 0, 0, 6);
                row.ColumnDefinitions.Add(Col(new GridLength(24)));
                row.ColumnDefinitions.Add(Col(Star));
                row.Children.Add(Text("✓", 14, "#16A34A", true, false));
                TextBlock line = Text(s, 13, "#334155", false, true);
                Grid.SetColumn(line, 1);
                row.Children.Add(line);
                lines.Children.Add(row);
            }
            Border card = Card();
            card.Padding = new Thickness(18, 14, 18, 10);
            card.Margin = new Thickness(0, 0, 10, 18);
            card.Child = lines;
            return card;
        }

        // A search box with a grey hint text that hides once the user types.
        Grid FaqSearchBox()
        {
            TextBox box = new TextBox();
            box.Height = 34;
            box.FontSize = 13;
            box.Padding = new Thickness(8, 0, 8, 0);
            box.VerticalContentAlignment = VerticalAlignment.Center;
            box.BorderBrush = B("#CBD5E1");
            TextBlock hint = Text(L.T("Search the questions, for example \"temperature\""), 13, "#94A3B8", false, false);
            hint.Margin = new Thickness(11, 0, 0, 0);
            hint.VerticalAlignment = VerticalAlignment.Center;
            hint.IsHitTestVisible = false;
            box.TextChanged += delegate
            {
                hint.Visibility = box.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
                FilterFaqs(box.Text);
            };
            Grid search = new Grid();
            search.Margin = new Thickness(0, 0, 0, 12);
            search.MaxWidth = 440;
            search.HorizontalAlignment = HorizontalAlignment.Left;
            search.Children.Add(box);
            search.Children.Add(hint);
            return search;
        }

        void OpenReadMe()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                string name = "README." + L.Code + ".txt";
                if (L.Code == "en" || asm.GetManifestResourceInfo("DiskCleanupHelper." + name) == null) name = "README.txt";
                string p = Path.Combine(AppPaths.ExeDir, name);
                if (!File.Exists(p))
                {
                    Directory.CreateDirectory(AppPaths.DataDir);
                    p = Path.Combine(AppPaths.DataDir, name);
                    using (Stream s = asm.GetManifestResourceStream("DiskCleanupHelper." + name))
                    using (FileStream f = File.Create(p)) { s.CopyTo(f); }
                }
                Process.Start("notepad.exe", "\"" + p + "\"");
            }
            catch (Exception ex) { Msg(L.F("Could not open the read me: {0}", ex.Message)); }
        }
    }
}
