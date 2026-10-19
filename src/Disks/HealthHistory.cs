// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Saved health and temperature readings, for the history charts.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DiskCleanupHelper.App;

namespace DiskCleanupHelper.Disks
{
    // ---------------------------------------------------------------- history of health and temperature
    public class HealthPoint
    {
        public DateTime Time;
        public int? Health, Temp;
    }

    public static class HealthHistory
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        public static string FilePath { get { return Path.Combine(AppPaths.DataDir, "disk-health-history.csv"); } }

        static string Q(string s) { return "\"" + (s ?? "").Replace("\"", "\"\"") + "\""; }
        static string N(long? v) { return v.HasValue ? v.Value.ToString(Inv) : ""; }

        static List<string> SplitCsv(string line)
        {
            List<string> f = new List<string>();
            StringBuilder cur = new StringBuilder();
            bool q = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (q)
                {
                    if (c == '"') { if (i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; } else q = false; }
                    else cur.Append(c);
                }
                else if (c == '"') q = true;
                else if (c == ',') { f.Add(cur.ToString()); cur.Length = 0; }
                else cur.Append(c);
            }
            f.Add(cur.ToString());
            return f;
        }

        static List<List<string>> Rows()
        {
            List<List<string>> rows = new List<List<string>>();
            try
            {
                if (!File.Exists(FilePath)) return rows;
                foreach (string line in File.ReadAllLines(FilePath).Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    List<string> f = SplitCsv(line);
                    if (f.Count >= 5) rows.Add(f);
                }
            }
            catch { }
            return rows;
        }

        // Saves one reading per disk, at most once every 'minGap'.
        public static int Record(List<DiskHealth> disks, TimeSpan minGap)
        {
            Dictionary<string, DateTime> last = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            foreach (List<string> r in Rows())
            {
                DateTime t;
                if (DateTime.TryParseExact(r[0], "yyyy-MM-dd HH:mm", Inv, DateTimeStyles.None, out t)) last[r[1]] = t;
            }
            DateTime now = DateTime.Now;
            StringBuilder sb = new StringBuilder();
            int added = 0;
            foreach (DiskHealth d in disks)
            {
                if (!d.HealthPct.HasValue && !d.TempC.HasValue) continue;
                DateTime t;
                if (last.TryGetValue(d.Key, out t) && now - t < minGap) continue;
                sb.AppendLine(string.Join(",", new[] { now.ToString("yyyy-MM-dd HH:mm", Inv), Q(d.Key), Q(d.Model), N(d.HealthPct), N(d.TempC), N(d.PowerOnHours) }));
                added++;
            }
            if (added == 0) return 0;
            try
            {
                Directory.CreateDirectory(AppPaths.DataDir);
                if (!File.Exists(FilePath)) File.WriteAllText(FilePath, "Time,Disk,Model,HealthPercent,TemperatureC,PowerOnHours" + Environment.NewLine, Encoding.UTF8);
                File.AppendAllText(FilePath, sb.ToString(), Encoding.UTF8);
            }
            catch { return 0; }
            return added;
        }

        public static List<HealthPoint> Load(string key)
        {
            List<HealthPoint> pts = new List<HealthPoint>();
            foreach (List<string> r in Rows())
            {
                if (!string.Equals(r[1], key, StringComparison.OrdinalIgnoreCase)) continue;
                HealthPoint p = new HealthPoint();
                if (!DateTime.TryParseExact(r[0], "yyyy-MM-dd HH:mm", Inv, DateTimeStyles.None, out p.Time)) continue;
                int v;
                if (int.TryParse(r[3], NumberStyles.Integer, Inv, out v)) p.Health = v;
                if (int.TryParse(r[4], NumberStyles.Integer, Inv, out v)) p.Temp = v;
                pts.Add(p);
            }
            return pts.OrderBy(p => p.Time).ToList();
        }
    }
}
