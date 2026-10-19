// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// The device details report: summary tiles and sections of label/value rows.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.Device
{
    public class InfoGroup
    {
        public string Title;
        public List<string[]> Rows = new List<string[]>();
        public List<string> Warnings = new List<string>();

        public InfoGroup() { }
        public InfoGroup(string title) { Title = title; }

        public void Add(string label, string value)
        {
            if (value == null) return;
            value = value.Trim();
            if (value.Length > 0) Rows.Add(new[] { label, value });
        }
    }

    public class InfoSection : InfoGroup
    {
        public string Sub;
        public List<InfoGroup> Groups = new List<InfoGroup>();
        public InfoSection(string title) { Title = title; }
    }

    public class SummaryTile
    {
        public string Label, Value, Sub, Color;
        public SummaryTile(string label, string value, string sub, string color) { Label = label; Value = value; Sub = sub; Color = color; }
    }

    public class DeviceReport
    {
        public DateTime Taken = DateTime.Now;
        public List<SummaryTile> Tiles = new List<SummaryTile>();
        public List<InfoSection> Sections = new List<InfoSection>();

        // Plain text for "Copy all" and "Save as text file".
        public string ToText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(AppInfo.Title + " " + AppInfo.DisplayVersion + "  -  " + L.T("Device details"));
            sb.AppendLine(L.F("Read on {0}", Taken.ToString("f")));
            foreach (InfoSection s in Sections)
            {
                sb.AppendLine();
                sb.AppendLine(s.Title.ToUpperInvariant());
                sb.AppendLine(new string('-', s.Title.Length));
                AppendRows(sb, s, "  ");
                foreach (InfoGroup g in s.Groups)
                {
                    sb.AppendLine("  " + g.Title);
                    AppendRows(sb, g, "    ");
                }
            }
            return sb.ToString();
        }

        static void AppendRows(StringBuilder sb, InfoGroup g, string indent)
        {
            int w = g.Rows.Count == 0 ? 0 : g.Rows.Max(r => r[0].Length);
            foreach (string[] r in g.Rows) sb.AppendLine(indent + (r[0] + ":").PadRight(w + 2) + r[1]);
            foreach (string n in g.Warnings) sb.AppendLine(indent + "! " + n);
        }
    }
}
