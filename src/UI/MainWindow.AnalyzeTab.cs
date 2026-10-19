// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Analyze disk tab: one scan of a drive or folder, then its biggest folders and biggest files.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Disks;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        const long DefaultMinFileBytes = 100L * 1048576;

        // The drive, scope ("whole drive" / "my user folder") and minimum file size boxes.
        void FillAnalyzeChoices()
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;
                    string label = L.Ltr(drive.Name.TrimEnd('\\'));
                    if (!string.IsNullOrEmpty(drive.VolumeLabel)) label += "  " + drive.VolumeLabel;
                    ComboBoxItem item = Choice(label + "    " + L.F("{0} free of {1}", Fmt.Size(drive.AvailableFreeSpace), Fmt.Size(drive.TotalSize)), drive.Name);
                    CmbDrive.Items.Add(item);
                    if (drive.Name.StartsWith(AppPaths.SysDrive, StringComparison.OrdinalIgnoreCase)) CmbDrive.SelectedItem = item;
                }
                catch { }
            }
            if (CmbDrive.SelectedItem == null && CmbDrive.Items.Count > 0) CmbDrive.SelectedIndex = 0;

            CmbScope.Items.Add(Choice(L.T("Whole drive"), "drive"));
            CmbScope.Items.Add(Choice(L.T("My user folder only"), "user"));
            CmbScope.SelectedIndex = 0;

            CmbMin.Items.Add(Choice(L.Ltr("50 MB"), 50L * 1048576));
            CmbMin.Items.Add(Choice(L.Ltr("100 MB"), DefaultMinFileBytes));
            CmbMin.Items.Add(Choice(L.Ltr("500 MB"), 500L * 1048576));
            CmbMin.Items.Add(Choice(L.Ltr("1 GB"), 1024L * 1048576));
            CmbMin.SelectedIndex = 1;
        }

        static ComboBoxItem Choice(string text, object value)
        {
            ComboBoxItem item = new ComboBoxItem();
            item.Content = text;
            item.Tag = value;
            return item;
        }

        void AnalyzeChosenDrive()
        {
            ComboBoxItem scope = CmbScope.SelectedItem as ComboBoxItem;
            ComboBoxItem drive = CmbDrive.SelectedItem as ComboBoxItem;
            ComboBoxItem min = CmbMin.SelectedItem as ComboBoxItem;
            string root;
            if (scope != null && (string)scope.Tag == "user") root = AppPaths.UserProfile;
            else if (drive != null) root = (string)drive.Tag;
            else { Msg(L.T("Pick a drive first.")); return; }
            StartAnalyze(root, min != null ? (long)min.Tag : DefaultMinFileBytes, true);
        }

        void StartAnalyze(string root, long minFileBytes, bool confirm)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) { Msg(L.T("That drive or folder is not available.")); return; }
            if (confirm && !ConfirmMemory(L.T("Scan anyway?"))) return;
            DriveScanner scanner = new DriveScanner();
            Progress2.IsIndeterminate = true;
            StatusText2.Text = L.F("Scanning {0} ...", root);
            Log(L.F("Scanning {0}. Click Stop to stop.", root));
            Stopwatch sw = Stopwatch.StartNew();
            RunWork(delegate(CancelFlag flag)
            {
                scanner.Run(root, minFileBytes, flag, delegate(long dirs, long files)
                {
                    Ui(delegate { StatusText2.Text = L.F("Scanning... {0:N0} folders and {1:N0} files so far", dirs, files); });
                });
            }, delegate { FinishAnalyze(scanner, root, sw); });
        }

        void FinishAnalyze(DriveScanner scanner, string root, Stopwatch sw)
        {
            scan = scanner;
            Progress2.IsIndeterminate = false;
            ShowFolderView(scanner.Finished ? scanner.Root : null);
            ShowBigFiles();
            if (!scanner.Finished)
            {
                Progress2.Value = 0;
                StatusText2.Text = L.T("Stopped. Results are incomplete.");
                Log(L.T("Scan stopped. The biggest files found so far are listed. Folder sizes are only shown for a finished scan."));
                return;
            }
            Progress2.Value = 100;
            string summary = L.F("Done in {0:N0} s: {1:N0} folders, {2:N0} files.", sw.Elapsed.TotalSeconds, scanner.DirCount, scanner.FileCount);
            if (scanner.Skipped > 0) summary += " " + L.F("{0:N0} folders could not be read (Windows protects them).", scanner.Skipped);
            StatusText2.Text = summary;
            Log(summary);
            if (root.StartsWith(AppPaths.SysDrive, StringComparison.OrdinalIgnoreCase))
                Log(L.T("Note: the Windows folder can look bigger than it really is, because Windows keeps some files in two places at once."));
        }

        // ------------------------------------------------------------ biggest folders
        void ShowFolderView(string path)
        {
            if (scan == null || path == null || !scan.Dirs.ContainsKey(path))
            {
                FoldersList.ItemsSource = null;
                FolderPath.Text = L.T("Scan a drive to see its biggest folders.");
                BtnUp.IsEnabled = false;
                return;
            }
            currentFolder = path;
            double total = scan.Dirs[path];
            List<FolderRow> rows = new List<FolderRow>();
            List<string> children;
            if (scan.Children.TryGetValue(path, out children))
                foreach (string c in children) if (scan.Dirs.ContainsKey(c)) rows.Add(NewFolderRow(Path.GetFileName(c), c, scan.Dirs[c], total, true));
            long filesHere;
            if (scan.FilesHere.TryGetValue(path, out filesHere) && filesHere > 0) rows.Add(NewFolderRow(L.T("(files directly in this folder)"), path, filesHere, total, false));
            FoldersList.ItemsSource = rows.OrderByDescending(r => r.Bytes).ToList();
            FolderPath.Text = L.Ltr(path) + "      " + L.Ltr(Fmt.Size(total));
            BtnUp.IsEnabled = !string.Equals(path.TrimEnd('\\'), scan.Root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        }

        void ShowParentFolder()
        {
            if (currentFolder == null) return;
            string parent = Path.GetDirectoryName(currentFolder.TrimEnd('\\'));
            if (string.IsNullOrEmpty(parent)) return;
            if (!scan.Dirs.ContainsKey(parent) && scan.Dirs.ContainsKey(parent + "\\")) parent += "\\";   // drive roots end in "\"
            ShowFolderView(parent);
        }

        static FolderRow NewFolderRow(string name, string path, double bytes, double total, bool isDir)
        {
            double pct = total > 0 ? 100.0 * bytes / total : 0;
            FolderRow row = new FolderRow();
            row.Name = name; row.Path = path; row.Bytes = bytes; row.SizeText = Fmt.Size(bytes);
            row.PctText = L.Ltr(pct.ToString("N0") + "%"); row.BarWidth = 110.0 * pct / 100.0; row.IsDir = isDir;
            return row;
        }

        // ------------------------------------------------------------ biggest files
        void ShowBigFiles()
        {
            if (scan == null) { FilesList.ItemsSource = null; FilesTitle.Text = L.T("Biggest files"); return; }
            FilesList.ItemsSource = scan.BigFiles.OrderByDescending(b => b.Bytes).Take(300).ToList();
            FilesTitle.Text = L.F("Biggest files ({0} found)", scan.BigFiles.Count.ToString("N0"));
        }

        // Moves the selected files to the Recycle Bin, or deletes them. Windows and program files are refused.
        void RemoveSelectedFiles(bool permanent)
        {
            List<BigFile> files = FilesList.SelectedItems.Cast<BigFile>().ToList();
            if (files.Count == 0) { Msg(L.T("Select one or more files in the list first. Hold Ctrl to select several.")); return; }
            List<BigFile> blocked = files.Where(f => FileOps.IsProtected(f.Path)).ToList();
            if (blocked.Count > 0)
            {
                Msg(L.F("These are Windows, program or system files and are protected:\r\n\r\n{0}\r\n\r\nUninstall the program instead, or use the Clean up tab.",
                    string.Join("\r\n", blocked.Take(10).Select(f => "  " + L.Ltr(f.Path)))));
                return;
            }
            if (!ConfirmRemoval(files, permanent)) return;

            int done = 0, failed = 0;
            foreach (BigFile f in files)
            {
                try
                {
                    if (permanent)
                    {
                        File.SetAttributes(f.Path, FileAttributes.Normal);
                        File.Delete(f.Path);
                    }
                    else
                    {
                        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(f.Path, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                                                                           Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                    }
                    done++;
                    scan.BigFiles.Remove(f);
                    HistoryLog.Write(permanent ? L.F("Deleted file: {0} ({1})", f.Path, f.SizeText) : L.F("Moved to Recycle Bin: {0} ({1})", f.Path, f.SizeText));
                }
                catch (Exception ex)
                {
                    failed++;
                    Log("  " + L.F("Could not remove {0}: {1}", f.Path, ex.Message));
                }
            }
            ShowBigFiles();
            UpdateStatus();
            string result = permanent ? L.F("Files deleted: {0}.", done) : L.F("Files moved to the Recycle Bin: {0}.", done);
            if (failed > 0) result += " " + L.F("{0} could not be removed (in use or protected).", failed);
            Log(result);
            Msg(result);
        }

        bool ConfirmRemoval(List<BigFile> files, bool permanent)
        {
            double total = files.Sum(f => (double)f.Bytes);
            List<string> names = files.Take(12).Select(f => "  - " + f.Name + "   (" + f.SizeText + ")").ToList();
            if (files.Count > 12) names.Add("  " + L.F("... and {0} more", files.Count - 12));
            string what = permanent ? L.T("They will be deleted permanently and cannot be restored.")
                                    : L.T("They will go to the Recycle Bin. The space is only freed after you empty the Recycle Bin.");
            string text = L.F("Files: {0}, {1} in total.", files.Count, Fmt.Size(total)) + "\r\n" + what + "\r\n\r\n" +
                          string.Join("\r\n", names) + "\r\n\r\n" + L.T("Make sure you do not need them. Continue?");
            return AskYesNo(text, permanent ? L.T("Delete permanently") : L.T("Move to Recycle Bin"));
        }
    }
}
