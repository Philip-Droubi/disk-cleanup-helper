// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Connects the buttons and tabs to their handlers, one method per tab.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Cleanup;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Disks;
using DiskCleanupHelper.Install;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        void WireEvents()
        {
            WireUpdateEvents();
            BtnTheme.Click += delegate { ToggleTheme(); };
            BtnLang.Click += delegate { ShowLanguageMenu(); };
            Tabs.SelectionChanged += OnTabChanged;
            // A narrow window hides the tagline, which would otherwise wrap into many lines and push the tabs down,
            // and uses a smaller title so its words still fit next to the stat cards.
            Window.SizeChanged += delegate
            {
                bool narrow = Window.ActualWidth < 1100;
                Tagline.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
                AppTitle.FontSize = narrow ? 18 : 22;
            };
            WireCleanupTab();
            WireAnalyzeTab();
            WireHealthTab();
            WirePerformanceTab();
            WireDeviceTab();
            WireAboutTab();
        }

        // Tabs read their data the first time they are opened. Snapshots fill tabs themselves (MainWindow.TestHooks.cs).
        void OnTabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != Tabs) return;
            int tab = Tabs.SelectedIndex;
            bool snapshot = options.SnapshotPath != null;
            if ((tab == Tab.Health || tab == Tab.Smart) && !healthShown && !snapshot) OpenHealthTab();
            if (tab == Tab.Device && lastDevice == null && !snapshot) ShowDevice(false);
            if (tab == Tab.Performance)
            {
                EnsurePerfTab(snapshot);
                if (perfTimer != null) perfTimer.Start();
            }
            else if (perfTimer != null) perfTimer.Stop();
            UpdateTempTimer();
        }

        // Reads the disks now, or right after the running task (measuring sizes is simply stopped).
        void OpenHealthTab()
        {
            if (!busy) { ShowHealth(false); return; }
            if (healthPending) return;
            healthPending = true;
            if (measuring)
            {
                cancel.IsSet = true;
                ShowHealthLoading(L.T("Reading each disk's health and temperature. This takes a few seconds."));
            }
            else ShowHealthLoading(L.T("Waiting for the current task to finish. The disks are checked right after that."));
        }

        void Stop(TextBlock status)
        {
            cancel.IsSet = true;
            status.Text = L.T("Stopping...");
            Log(L.T("Stopping..."));
        }

        void WireCleanupTab()
        {
            BtnScan.Click += delegate { StartMeasure(false); };
            BtnStop.Click += delegate { Stop(StatusText); };
            BtnClean.Click += delegate { StartClean(); };
            BtnSafe.Click += delegate
            {
                foreach (CleanupItem it in items) if (it.Safe && it.Check != null && it.Check.IsEnabled) it.Check.IsChecked = true;
                UpdateSelection();
            };
            BtnNone.Click += delegate
            {
                foreach (CleanupItem it in items) if (it.Check != null) it.Check.IsChecked = false;
                UpdateSelection();
            };
            BtnOpen.Click += delegate
            {
                CleanupItem it = selected;
                if (it == null) return;
                if (it.Open != null) { it.Open(Log); return; }
                string first = it.Paths == null ? null : it.Paths().FirstOrDefault(FileOps.Exists);
                if (first != null) FileOps.OpenPath(first); else Msg(L.T("This item has no folder to open."));
            };
        }

        void WireAnalyzeTab()
        {
            BtnAnalyze.Click += delegate { AnalyzeChosenDrive(); };
            BtnStop2.Click += delegate { Stop(StatusText2); };
            BtnUp.Click += delegate { ShowParentFolder(); };
            FoldersList.MouseDoubleClick += delegate
            {
                FolderRow row = FoldersList.SelectedItem as FolderRow;
                if (row != null && row.IsDir) ShowFolderView(row.Path);
            };
            BtnOpenFolder.Click += delegate
            {
                FolderRow row = FoldersList.SelectedItem as FolderRow;
                if (row != null) FileOps.OpenPath(row.Path);
                else if (currentFolder != null) FileOps.OpenPath(currentFolder);
                else Msg(L.T("Scan a drive first."));
            };
            BtnFileOpen.Click += delegate
            {
                BigFile file = FilesList.SelectedItem as BigFile;
                if (file != null) FileOps.OpenPath(file.Path); else Msg(L.T("Select a file in the list first."));
            };
            BtnFileRecycle.Click += delegate { RemoveSelectedFiles(false); };
            BtnFileDelete.Click += delegate { RemoveSelectedFiles(true); };
        }

        void WireHealthTab()
        {
            BtnHealth.Click += delegate { ShowHealth(false); };
            BtnChkdsk.Click += delegate
            {
                if (!Env.IsAdmin) { Msg(L.T("This needs admin rights. Start the tool again and click Yes on the Windows prompt.")); return; }
                Env.RunVisibleKeepOpen("chkdsk.exe " + AppPaths.SysDrive + " /scan");
                Log(L.F("Started Windows' own error scan of {0} in its own window. It only reads the drive and takes a few minutes. Read the result in that window.", AppPaths.SysDrive));
            };
            BtnOptimize.Click += delegate { try { Process.Start(Env.SysTool("dfrgui.exe")); } catch { } };
            BtnStorage.Click += delegate { Env.OpenUri("ms-settings:storagesense"); };
            BtnHealthHistory.Click += delegate
            {
                if (File.Exists(HealthHistory.FilePath)) OpenInNotepad(HealthHistory.FilePath);
                else Msg(L.F("No readings have been saved yet.\r\n\r\nThe history will be saved in:\r\n{0}", HealthHistory.FilePath));
            };
        }

        void WirePerformanceTab()
        {
            BtnSpeedTest.Click += delegate { StartSpeedTest(); };
            BtnStop3.Click += delegate { cancel.IsSet = true; StatusText3.Text = L.T("Stopping..."); };
        }

        void WireDeviceTab()
        {
            BtnDevRefresh.Click += delegate { ShowDevice(false); };
            BtnDevCopy.Click += delegate { CopyDeviceReport(); };
            BtnDevSave.Click += delegate { SaveDeviceReport(); };
            BtnDxdiag.Click += delegate { try { Process.Start(Env.SysTool("dxdiag.exe")); } catch (Exception ex) { Msg(L.F("Could not open dxdiag: {0}", ex.Message)); } };
            BtnMsinfo.Click += delegate { try { Process.Start(Env.SysTool("msinfo32.exe")); } catch (Exception ex) { Msg(L.F("Could not open System Information: {0}", ex.Message)); } };
        }

        void WireAboutTab()
        {
            BtnInstall.Click += delegate { Install(); };
            BtnInstallBar.Click += delegate { Install(); };
            BtnUninstall.Click += delegate
            {
                if (!Installer.UninstallWithMessages(Window, false)) return;
                readyUpdate = null;   // nothing is installed into a folder that is being removed
                UpdateInstallState();
                if (Installer.RunningFromInstallDir) Window.Close();
            };
            BtnHist.Click += delegate
            {
                if (File.Exists(AppPaths.HistoryFile)) OpenInNotepad(AppPaths.HistoryFile);
                else Msg(L.F("No cleanups have been run yet.\r\n\r\nThe history will be saved in:\r\n{0}", AppPaths.DataDir));
            };
            BtnHelp.Click += delegate { OpenReadMe(); };
        }

        void Install()
        {
            Installer.InstallWithMessages(Window);
            UpdateInstallState();
            Log(Installer.IsInstalled ? L.F("Install: installed in {0}", AppPaths.InstallDir) : L.T("Install: not installed"));
        }

        static void OpenInNotepad(string path)
        {
            try { Process.Start("notepad.exe", "\"" + path + "\""); } catch { }
        }
    }
}
