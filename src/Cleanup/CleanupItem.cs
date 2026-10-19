// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// One thing that can be cleaned, and the context a cleanup runs in.
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using DiskCleanupHelper.Common;

namespace DiskCleanupHelper.Cleanup
{
    public class WorkContext
    {
        public OpCounter Counter = new OpCounter();
        public Action<string> Log;
        public CancelFlag Cancel;
    }

    public class CleanupItem
    {
        public string Group = "safe";
        public string Name = "", Where = "", Short = "";
        public string SizeText;                          // fixed text instead of a measured size
        public List<string> Details = new List<string>();
        public List<string> BaseDetails = new List<string>();
        public Func<IEnumerable<string>> Paths;          // folders or files this item cleans
        public Func<CleanupItem, CancelFlag, long?> Size; // custom size; default = size of Paths
        public Action<CleanupItem, WorkContext> Clean;    // custom clean; default = empty each path
        public string[] MustClose = new string[0];        // process names that must be closed first
        public bool Admin, Manual, Safe, RunLast, Always;
        public Func<bool> ShowIf;                         // custom test whether the item exists on this PC
        public Action<Action<string>> Open;               // custom "Open location"
        public long? Bytes;

        // window parts, set by MainWindow
        public Border Row;
        public CheckBox Check;
        public TextBlock SizeLabel;
    }
}
