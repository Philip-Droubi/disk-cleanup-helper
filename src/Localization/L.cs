// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Translations. The code writes English, and the English text is the key:
//     L.T("Measure sizes")   L.F("Scan {0} for errors", drive)   L.P(n, "1 item", "{0} items")
// Each Localization\Languages\<code>.lang file maps English to the translation ("en: Measure sizes" then
// "ar: قياس الأحجام"). A missing translation shows in English. File format, plural forms and the
// right-to-left rules: docs/DEVELOPING.md, section "Translations".
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using DiskCleanupHelper.App;

namespace DiskCleanupHelper.Localization
{
    public class LangInfo
    {
        public string Code, Name;
        public bool Rtl;
    }

    public static class L
    {
        public static string Code = "en";
        public static string Name = "English";
        public static bool Rtl;
        public static string Font;   // font list from the language file ("Segoe UI, Microsoft YaHei UI"), or null

        const string Prefix = "DiskCleanupHelper.lang.";
        static Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);

        // ------------------------------------------------------------ choosing and loading
        public static List<LangInfo> Available()
        {
            List<LangInfo> list = new List<LangInfo>();
            LangInfo en = new LangInfo();
            en.Code = "en"; en.Name = "English";
            list.Add(en);
            foreach (string res in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (!res.StartsWith(Prefix) || !res.EndsWith(".lang")) continue;
                string code = res.Substring(Prefix.Length, res.Length - Prefix.Length - 5);
                Dictionary<string, string> head = new Dictionary<string, string>();
                Parse(ReadHead(res), head, null);
                LangInfo li = new LangInfo();
                li.Code = code;
                li.Name = head.ContainsKey("name") ? head["name"] : code;
                li.Rtl = head.ContainsKey("rtl") && head["rtl"] == "yes";
                list.Add(li);
            }
            return list;
        }

        // code: a language picked for this run (test switch), else the saved choice, else Windows' display language.
        public static void Init(string code)
        {
            List<LangInfo> all = Available();
            if (string.IsNullOrEmpty(code)) code = Saved();
            if (string.IsNullOrEmpty(code)) code = FromWindows(all);
            LangInfo pick = all.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)) ?? all[0];
            Code = pick.Code; Name = pick.Name; Rtl = pick.Rtl; Font = null;
            map.Clear();
            if (Code != "en")
            {
                Dictionary<string, string> head = new Dictionary<string, string>();
                Parse(ReadResource(Prefix + Code + ".lang"), head, map);
                if (head.ContainsKey("font")) Font = head["font"];
            }
        }

        // Windows' display language: the exact culture, then its parents (zh-CN -> zh-Hans), then the same language
        // in another country (pt-PT -> pt-BR). Traditional Chinese is not mapped to Simplified Chinese.
        static string FromWindows(List<LangInfo> all)
        {
            CultureInfo ui = CultureInfo.CurrentUICulture;
            bool traditional = false;
            for (CultureInfo c = ui; c != null && !string.IsNullOrEmpty(c.Name); c = c.Parent)
            {
                if (c.Name == "zh-Hant") traditional = true;
                foreach (LangInfo li in all)
                    if (string.Equals(li.Code, c.Name, StringComparison.OrdinalIgnoreCase)) return li.Code;
            }
            if (traditional) return null;
            foreach (LangInfo li in all)
                if (li.Code.Split('-')[0].Equals(ui.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase)) return li.Code;
            return null;
        }

        // build.ps1 embeds the language files gzip-compressed; a plain file works too.
        static Stream OpenResource(string name)
        {
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (s == null) return null;
            int b1 = s.ReadByte(), b2 = s.ReadByte();
            s.Position = 0;
            if (b1 == 0x1F && b2 == 0x8B) return new GZipStream(s, CompressionMode.Decompress);
            return s;
        }

        static string ReadResource(string name)
        {
            using (Stream s = OpenResource(name))
            {
                if (s == null) return "";
                using (StreamReader r = new StreamReader(s, Encoding.UTF8)) return r.ReadToEnd();
            }
        }

        // Only the lines before the first text (@name, @rtl, @font), for the language list.
        static string ReadHead(string name)
        {
            StringBuilder sb = new StringBuilder();
            using (Stream s = OpenResource(name))
            {
                if (s == null) return "";
                using (StreamReader r = new StreamReader(s, Encoding.UTF8))
                {
                    string line;
                    while ((line = r.ReadLine()) != null && !line.StartsWith("en:")) sb.Append(line).Append('\n');
                }
            }
            return sb.ToString();
        }

        // A number with a unit typed into a right-to-left translation ("16 GB", "45 °C"), which would show as "GB 16".
        // Placeholders ({0} GB, kept by Fill) and text already inside <ltr> are skipped.
        static readonly Regex TypedValue = new Regex(@"(?<![\w\u200E{.,\-\u2013])\d+(?:[.,]\d+)?(?:\s?[-\u2013]\s?\d+(?:[.,]\d+)?)?(?:%| (?:MB/s|KB/s|Gb/s|Mb/s|MT/s|GB|MB|KB|TB|GHz|MHz|Hz|Wh|°C)(?![A-Za-z/]))");

        static void Parse(string text, Dictionary<string, string> head, Dictionary<string, string> into)
        {
            string pending = null;
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                string t = line.Trim();
                if (t.Length == 0 || t.StartsWith("#")) continue;
                if (t.StartsWith("@"))
                {
                    int eq = t.IndexOf('=');
                    if (eq > 1) head[t.Substring(1, eq - 1).Trim().ToLowerInvariant()] = t.Substring(eq + 1).Trim();
                    continue;
                }
                int colon = t.IndexOf(':');
                if (colon < 2 || colon > 8) continue;
                string tag = t.Substring(0, colon);
                string value = t.Substring(colon + 1);
                if (value.StartsWith(" ")) value = value.Substring(1);
                value = value.Replace("<br>", "\n");
                if (tag != "en")
                {
                    value = value.Replace("<ltr>", LtrStart).Replace("</ltr>", LtrEnd);   // a command or path kept left to right
                    if (head.ContainsKey("rtl") && head["rtl"] == "yes")
                        value = TypedValue.Replace(value, delegate(Match m) { return LtrStart + m.Value + LtrEnd; });
                }
                if (tag == "en") pending = value;
                else if (pending != null)
                {
                    if (into != null && value.Length > 0) into[pending] = value;
                    pending = null;
                }
            }
        }

        // ------------------------------------------------------------ saved choice
        public static string Saved() { return AppSettings.Read("lang"); }

        public static void Save(string code) { AppSettings.Write("lang", code); }

        // ------------------------------------------------------------ lookups
        // The translation of an English text, or the English text when there is none.
        public static string T(string en)
        {
            if (en == null || Code == "en" || map.Count == 0) return en;
            bool crlf = en.Contains("\r\n");
            string key = crlf ? en.Replace("\r\n", "\n") : en;
            string tr;
            if (!map.TryGetValue(key, out tr) || tr == "=") return en;
            return crlf ? tr.Replace("\n", "\r\n") : tr;
        }

        // T plus {0} {1} ... In right-to-left languages each inserted value that has no Arabic or Hebrew letters
        // (paths, sizes, dates, names) is kept left-to-right, so its characters stay in order.
        public static string F(string en, params object[] args)
        {
            return Fill(T(en), args);
        }

        // {0}, {0:N1}, and a unit or % written right after it ("{0} GB", "{0}%"), which stays with the value
        static readonly Regex Holder = new Regex(@"\{\{|\}\}|\{(\d+)(?::([^}]*))?\}(%| (?:MB/s|KB/s|Gb/s|Mb/s|MT/s|GB|MB|KB|TB|GHz|MHz|Hz|Wh|°C)(?![A-Za-z/]))?");

        static string Fill(string fmt, object[] args)
        {
            if (args == null || args.Length == 0) return fmt;
            return Holder.Replace(fmt, delegate(Match m)
            {
                if (m.Value == "{{") return "{";
                if (m.Value == "}}") return "}";
                int i = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                if (i >= args.Length) return m.Value;
                object a = args[i];
                string spec = m.Groups[2].Success ? m.Groups[2].Value : null;
                IFormattable f = a as IFormattable;
                string s = f != null ? f.ToString(spec, CultureInfo.CurrentCulture) : Convert.ToString(a, CultureInfo.CurrentCulture);
                return Ltr(s + m.Groups[3].Value);
            });
        }

        // Count-dependent text: one = the English singular, other = the English plural (with {0} for the number).
        // The translation of "other" lists every form the language needs, split by " | ".
        public static string P(long n, string one, string other)
        {
            string fmt = n == 1 ? one : other;
            if (Code != "en")
            {
                string tr = T(other);
                if (!ReferenceEquals(tr, other))
                {
                    string[] forms = tr.Split(new[] { " | " }, StringSplitOptions.None);
                    fmt = forms[Math.Min(forms.Length - 1, PluralIndex(n))];
                }
            }
            return Fill(fmt, new object[] { n.ToString("N0", CultureInfo.CurrentCulture) });
        }

        // Plural form number, following the standard (CLDR) rules.
        static int PluralIndex(long n)
        {
            long a = Math.Abs(n), d10 = a % 10, d100 = a % 100;
            switch (Code)
            {
                case "ar":   // zero | one | two | few | many | other
                    if (a == 0) return 0;
                    if (a == 1) return 1;
                    if (a == 2) return 2;
                    if (d100 >= 3 && d100 <= 10) return 3;
                    if (d100 >= 11 && d100 <= 99) return 4;
                    return 5;
                case "ru": case "uk":   // one | few | many
                    if (d10 == 1 && d100 != 11) return 0;
                    if (d10 >= 2 && d10 <= 4 && (d100 < 12 || d100 > 14)) return 1;
                    return 2;
                case "fr": case "pt-BR": case "fa":   // one (0 and 1) | other
                    return a <= 1 ? 0 : 1;
                case "pl":   // one | few | many
                    if (a == 1) return 0;
                    if (d10 >= 2 && d10 <= 4 && (d100 < 12 || d100 > 14)) return 1;
                    return 2;
                default:     // one | other
                    return a == 1 ? 0 : 1;
            }
        }

        public static bool HasRtlLetters(string s)
        {
            if (s == null) return false;
            foreach (char c in s)
                if ((c >= '\u0590' && c <= '\u08FF') || (c >= '\uFB1D' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF')) return true;
            return false;
        }

        // Keeps a value (path, size, English name) in left-to-right order inside right-to-left text. In WPF only
        // LRE + LRM ... PDF keeps both "C:" and "12.5 GB" right (tested); the LRM before the PDF keeps a trailing
        // colon or bracket in place when right-to-left text follows ("C:  (Windows drive)").
        public const string LtrStart = "\u202A\u200E", LtrEnd = "\u200E\u202C";

        public static string Ltr(string s)
        {
            if (!Rtl || string.IsNullOrEmpty(s) || HasRtlLetters(s)) return s;
            return LtrStart + s + LtrEnd;
        }

        // A right-to-left phrase that starts with a bracket or an English word ("(Windows drive)"), placed after a value:
        // the leading right-to-left mark keeps its bracket from joining the left-to-right value before it.
        public static string RtlPhrase(string s)
        {
            return Rtl && !string.IsNullOrEmpty(s) ? "\u200F" + s : s;
        }

        // "Opens elsewhere" arrow, pointing the reading direction's way.
        public static string Arrow { get { return Rtl ? "↖" : "↗"; } }

        // Message boxes read right to left in right-to-left languages.
        public static MessageBoxOptions MsgOptions { get { return Rtl ? MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : MessageBoxOptions.None; } }

        // ------------------------------------------------------------ the window layout
        // Translates the texts written in MainWindow.xaml, after it is loaded.
        public static void TranslateTree(DependencyObject d)
        {
            if (Code == "en" || d == null) return;
            TextBlock tb = d as TextBlock;
            if (tb != null && tb.Inlines.Count <= 1 && !string.IsNullOrEmpty(tb.Text)) tb.Text = T(tb.Text);
            ContentControl cc = d as ContentControl;
            if (cc != null && cc.Content is string) cc.Content = T((string)cc.Content);
            HeaderedContentControl hc = d as HeaderedContentControl;
            if (hc != null && hc.Header is string) hc.Header = T((string)hc.Header);
            FrameworkElement fe = d as FrameworkElement;
            if (fe != null && fe.ToolTip is string) fe.ToolTip = T((string)fe.ToolTip);
            ListView lv = d as ListView;
            if (lv != null && lv.View is GridView)
                foreach (GridViewColumn col in ((GridView)lv.View).Columns)
                    if (col.Header is string) col.Header = T((string)col.Header);
            foreach (object c in LogicalTreeHelper.GetChildren(d))
            {
                DependencyObject cd = c as DependencyObject;
                if (cd != null) TranslateTree(cd);
            }
        }
    }
}
