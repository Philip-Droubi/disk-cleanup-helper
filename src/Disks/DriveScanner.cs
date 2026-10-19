// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// One pass over a drive or folder: folder sizes and the biggest files.
using System;
using System.Collections.Generic;
using System.IO;
using DiskCleanupHelper.Common;

namespace DiskCleanupHelper.Disks
{
    public class BigFile
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public string Folder { get; set; }
        public long Bytes { get; set; }
        public string SizeText { get; set; }
        public string ModifiedText { get; set; }
    }

    public class FolderRow
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public double Bytes { get; set; }
        public string SizeText { get; set; }
        public string PctText { get; set; }
        public double BarWidth { get; set; }
        public bool IsDir { get; set; }
    }

    public class DriveScanner
    {
        const long OneMB = 1048576;
        public Dictionary<string, long> Dirs = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, long> FilesHere = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> Children = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public List<BigFile> BigFiles = new List<BigFile>();
        public string Root;
        public long FileCount, DirCount, Skipped;
        public bool Finished;

        long minFile;
        CancelFlag cancel;
        Action<long, long> progress;

        public void Run(string root, long minFileBytes, CancelFlag cancelFlag, Action<long, long> onProgress)
        {
            minFile = minFileBytes;
            cancel = cancelFlag;
            progress = onProgress;
            DirectoryInfo di = new DirectoryInfo(root);
            Root = di.FullName;
            ScanDir(di, true);
            Finished = true;
        }

        long ScanDir(DirectoryInfo di, bool isRoot)
        {
            long here = 0;
            FileInfo[] files = null;
            try { files = di.GetFiles(); } catch { Skipped++; }
            if (files != null)
            {
                foreach (FileInfo f in files)
                {
                    long len;
                    try { len = f.Length; } catch { continue; }
                    here += len;
                    if (len >= minFile)
                    {
                        BigFile b = new BigFile();
                        b.Path = f.FullName; b.Name = f.Name; b.Folder = f.DirectoryName; b.Bytes = len;
                        b.SizeText = Fmt.Size(len);
                        try { b.ModifiedText = f.LastWriteTime.ToString("yyyy-MM-dd"); } catch { b.ModifiedText = ""; }
                        BigFiles.Add(b);
                    }
                }
                FileCount += files.Length;
            }
            long total = here;
            DirectoryInfo[] subs = null;
            try { subs = di.GetDirectories(); } catch { }
            List<string> kids = null;
            if (subs != null)
            {
                foreach (DirectoryInfo s in subs)
                {
                    if ((s.Attributes & FileAttributes.ReparsePoint) != 0) continue;   // never follow folder links
                    long sz = ScanDir(s, false);
                    total += sz;
                    if (sz >= OneMB)
                    {
                        if (kids == null) kids = new List<string>();
                        kids.Add(s.FullName);
                    }
                }
            }
            if (isRoot || total >= OneMB)
            {
                Dirs[di.FullName] = total;
                FilesHere[di.FullName] = here;
                if (kids != null) Children[di.FullName] = kids;
            }
            DirCount++;
            if (DirCount % 200 == 0)
            {
                if (cancel != null && cancel.IsSet) throw new OperationCanceledException();
                if (progress != null) progress(DirCount, FileCount);
            }
            return total;
        }
    }
}
