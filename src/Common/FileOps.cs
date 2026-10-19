// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Safe file and folder operations used by the cleanup items.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DiskCleanupHelper.App;

namespace DiskCleanupHelper.Common
{
    public static class FileOps
    {
        public static bool Exists(string p)
        {
            try { return !string.IsNullOrEmpty(p) && (File.Exists(p) || Directory.Exists(p)); }
            catch { return false; }
        }

        // Size of a file or folder. Never follows folder links.
        public static long SizeOf(string path, CancelFlag cancel)
        {
            if (string.IsNullOrEmpty(path)) return 0;
            try
            {
                if (File.Exists(path)) return new FileInfo(path).Length;
                if (!Directory.Exists(path)) return 0;
            }
            catch { return 0; }
            long total = 0;
            int n = 0;
            Stack<DirectoryInfo> stack = new Stack<DirectoryInfo>();
            stack.Push(new DirectoryInfo(path));
            while (stack.Count > 0)
            {
                if (cancel != null && cancel.IsSet && (++n % 50) == 0) throw new OperationCanceledException();
                DirectoryInfo d = stack.Pop();
                FileInfo[] files = null;
                try { files = d.GetFiles(); } catch { }
                if (files != null) foreach (FileInfo f in files) { try { total += f.Length; } catch { } }
                DirectoryInfo[] subs = null;
                try { subs = d.GetDirectories(); } catch { }
                if (subs != null) foreach (DirectoryInfo s in subs) { if ((s.Attributes & FileAttributes.ReparsePoint) == 0) stack.Push(s); }
            }
            return total;
        }

        public static long SizeOf(IEnumerable<string> paths, CancelFlag cancel)
        {
            long t = 0;
            if (paths == null) return 0;
            foreach (string p in paths) t += SizeOf(p, cancel);
            return t;
        }

        // Empties a folder but keeps the folder itself. olderThanDays > 0 keeps newer files.
        // If the path is a single file, deletes that file. Never clears a folder that is itself a link.
        public static void ClearFolder(string path, int olderThanDays, OpCounter c)
        {
            if (!Exists(path)) return;
            DateTime cut = olderThanDays > 0 ? DateTime.Now.AddDays(-olderThanDays) : DateTime.MaxValue;
            try
            {
                if (File.Exists(path)) { RemoveTree(new FileInfo(path), cut, c); return; }
                DirectoryInfo di = new DirectoryInfo(path);
                if ((di.Attributes & FileAttributes.ReparsePoint) != 0) return;
                FileSystemInfo[] kids = di.GetFileSystemInfos();
                foreach (FileSystemInfo k in kids) RemoveTree(k, cut, c);
            }
            catch { }
        }

        // Deletes a folder (or file) completely.
        public static void RemoveFolder(string path, OpCounter c)
        {
            if (!Exists(path)) return;
            FileSystemInfo fi = Directory.Exists(path) ? (FileSystemInfo)new DirectoryInfo(path) : new FileInfo(path);
            RemoveTree(fi, DateTime.MaxValue, c);
        }

        // A folder link (junction) is removed as a link only, never what it points to.
        static void RemoveTree(FileSystemInfo item, DateTime cut, OpCounter c)
        {
            DirectoryInfo di = item as DirectoryInfo;
            bool isLink = (item.Attributes & FileAttributes.ReparsePoint) != 0;
            if (di != null)
            {
                if (isLink)
                {
                    try { Directory.Delete(di.FullName, false); c.Ok++; } catch { c.Failed++; }
                    return;
                }
                FileSystemInfo[] kids = null;
                try { kids = di.GetFileSystemInfos(); } catch { }
                if (kids != null) foreach (FileSystemInfo k in kids) RemoveTree(k, cut, c);
                try { Directory.Delete(di.FullName, false); } catch { }   // only works once the folder is empty
            }
            else
            {
                if (item.LastWriteTime >= cut) return;
                try
                {
                    if ((item.Attributes & FileAttributes.ReadOnly) != 0) File.SetAttributes(item.FullName, FileAttributes.Normal);
                    File.Delete(item.FullName);
                    c.Ok++;
                }
                catch { c.Failed++; }
            }
        }

        public static void OpenPath(string p)
        {
            if (string.IsNullOrEmpty(p)) return;
            try
            {
                if (File.Exists(p)) { Process.Start("explorer.exe", "/select,\"" + p + "\""); return; }
                string q = p;
                while (!string.IsNullOrEmpty(q) && !Directory.Exists(q)) q = Path.GetDirectoryName(q);
                if (!string.IsNullOrEmpty(q)) Process.Start("explorer.exe", "\"" + q + "\"");
            }
            catch { }
        }

        // Windows, program and system files that the Analyze tab never deletes.
        public static bool IsProtected(string p)
        {
            if (string.IsNullOrEmpty(p)) return true;
            List<string> roots = new List<string> {
                AppPaths.WindowsDir, Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
                Environment.GetEnvironmentVariable("ProgramW6432"), AppPaths.ProgramData, Path.Combine(AppPaths.Root, "Recovery")
            };
            foreach (string r in roots)
            {
                if (string.IsNullOrEmpty(r)) continue;
                if (p.StartsWith(r.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) return true;
            }
            if (Regex.IsMatch(p, @"\\(System Volume Information|\$Recycle\.Bin|\$WinREAgent|\$SysReset)\\", RegexOptions.IgnoreCase)) return true;
            string leaf = Path.GetFileName(p);
            string[] sys = { "pagefile.sys", "hiberfil.sys", "swapfile.sys", "DumpStack.log.tmp", "bootmgr", "BOOTNXT" };
            return sys.Any(s => string.Equals(s, leaf, StringComparison.OrdinalIgnoreCase));
        }
    }
}
