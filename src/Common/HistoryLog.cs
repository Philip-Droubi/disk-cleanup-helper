// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// The cleanup history file.
using System;
using System.IO;
using System.Text;
using DiskCleanupHelper.App;

namespace DiskCleanupHelper.Common
{
    public static class HistoryLog
    {
        public static void Write(string m)
        {
            try
            {
                Directory.CreateDirectory(AppPaths.DataDir);
                File.AppendAllText(AppPaths.HistoryFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "  " + m + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }
    }
}
