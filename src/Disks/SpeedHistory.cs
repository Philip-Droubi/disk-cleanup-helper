// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Saved speed test results.
using System;
using System.IO;
using DiskCleanupHelper.App;

namespace DiskCleanupHelper.Disks
{
    public class SpeedRecord
    {
        public DateTime Time;
        public double WriteMBs, ReadMBs, RandomMBs;
    }

    // Saves speed test results so they can be compared later.
    public static class SpeedHistory
    {
        static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;
        public static string FilePath { get { return Path.Combine(AppPaths.DataDir, "disk-speed-history.csv"); } }

        public static void Save(string disk, SpeedResult r)
        {
            try
            {
                Directory.CreateDirectory(AppPaths.DataDir);
                if (!File.Exists(FilePath)) File.WriteAllText(FilePath, "Time,Disk,WriteMBs,ReadMBs,Random4kMBs,Random4kPerSecond" + Environment.NewLine, System.Text.Encoding.UTF8);
                File.AppendAllText(FilePath, string.Join(",", new[] {
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm", Inv), "\"" + (disk ?? "").Replace("\"", "\"\"") + "\"",
                    r.WriteMBs.ToString("0", Inv), r.ReadMBs.ToString("0", Inv), r.RandomMBs.ToString("0.0", Inv), r.RandomIops.ToString("0", Inv) }) + Environment.NewLine, System.Text.Encoding.UTF8);
            }
            catch { }
        }

        public static SpeedRecord Last(string disk)
        {
            SpeedRecord last = null;
            try
            {
                if (!File.Exists(FilePath)) return null;
                string quoted = "\"" + (disk ?? "").Replace("\"", "\"\"") + "\"";
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    int a = line.IndexOf(',');
                    if (a < 0 || line.IndexOf(quoted, StringComparison.Ordinal) != a + 1) continue;
                    string[] rest = line.Substring(a + 1 + quoted.Length).TrimStart(',').Split(',');
                    if (rest.Length < 3) continue;
                    SpeedRecord r = new SpeedRecord();
                    if (!DateTime.TryParseExact(line.Substring(0, a), "yyyy-MM-dd HH:mm", Inv, System.Globalization.DateTimeStyles.None, out r.Time)) continue;
                    double.TryParse(rest[0], System.Globalization.NumberStyles.Float, Inv, out r.WriteMBs);
                    double.TryParse(rest[1], System.Globalization.NumberStyles.Float, Inv, out r.ReadMBs);
                    double.TryParse(rest[2], System.Globalization.NumberStyles.Float, Inv, out r.RandomMBs);
                    last = r;
                }
            }
            catch { }
            return last;
        }
    }
}
