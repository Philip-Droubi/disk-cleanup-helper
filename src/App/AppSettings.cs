// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// The user's settings (language, theme, folded groups), stored as "key=value" lines in settings.ini.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DiskCleanupHelper.App
{
    public static class AppSettings
    {
        static string FilePath { get { return Path.Combine(AppPaths.DataDir, "settings.ini"); } }

        /// <summary>The saved value, or null when there is none (or the file cannot be read).</summary>
        public static string Read(string key)
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                string prefix = key + "=";
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    string t = line.Trim();
                    if (t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return t.Substring(prefix.Length).Trim();
                }
            }
            catch { }
            return null;
        }

        /// <summary>Saves a value, keeping the other settings. Failures are ignored: settings are a convenience.</summary>
        public static void Write(string key, string value)
        {
            try
            {
                Directory.CreateDirectory(AppPaths.DataDir);
                string prefix = key + "=";
                List<string> lines = new List<string>();
                if (File.Exists(FilePath))
                    lines.AddRange(File.ReadAllLines(FilePath).Where(l => !l.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
                lines.Add(prefix + value);
                File.WriteAllLines(FilePath, lines);
            }
            catch { }
        }
    }
}
