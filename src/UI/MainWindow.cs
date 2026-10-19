// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// The main window: shared state, start-up, background work and messages.
// The window is split into partial files, one per tab or feature (MainWindow.CleanupTab.cs, ...).
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Cleanup;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Disks;
using DiskCleanupHelper.Install;
using DiskCleanupHelper.Localization;
using DiskCleanupHelper.UI.Controls;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        // Below these the app warns before heavy work: free memory, and room left including the page file (GB).
        const double LowMemoryGb = 0.5, LowCommitGb = 1.5;

        // Tab positions in MainWindow.xaml (also the /tab test switch numbers).
        static class Tab
        {
            public const int Cleanup = 0, Analyze = 1, Health = 2, Smart = 3, Performance = 4, Device = 5, Help = 6, About = 7;
        }

        public Window Window;
        readonly AppOptions options;
        readonly bool demo;    // /demo snapshot: made-up data (MainWindow.Demo.cs)
        readonly bool admin;   // shown as running with admin rights (demo pictures look like a normal admin start)
        List<CleanupItem> items;
        CleanupItem selected;
        List<DiskHealth> lastHealth;
        DriveScanner scan;
        string currentFolder;

        // background work: one task at a time (RunWork); "cancel" belongs to the running task
        volatile bool busy;
        CancelFlag cancel = new CancelFlag();
        bool measuring;      // measuring only reads, so closing the window or opening Disk health just stops it
        bool healthShown;
        bool healthPending;  // Disk health was opened during another task: read the disks when it ends
        int startTab;        // the tab to open on (after a language switch)

        // named elements of MainWindow.xaml (see FindControls)
        TextBlock StatFreeLabel, StatFree, StatFreeSub, StatMem, StatMemSub, StatSel, StatSelSub, StatusText, DetTitle, DetSize,
                  StatusText2, FolderPath, FilesTitle, AboutTitle, InstallState, Tagline, AppTitle;
        Border AdminBanner;
        TabControl Tabs;
        Button BtnScan, BtnStop, BtnSafe, BtnNone, BtnClean, BtnOpen, BtnAnalyze, BtnStop2, BtnUp, BtnOpenFolder, BtnFileOpen,
               BtnFileRecycle, BtnFileDelete, BtnHealth, BtnChkdsk, BtnOptimize, BtnStorage, BtnInstall, BtnUninstall, BtnHist, BtnHelp,
               BtnHealthHistory, BtnSpeedTest, BtnStop3;
        ProgressBar Progress, Progress2;
        StackPanel ListPanel, DetBody, HealthPanel, HelpPanel, PerfPanel, SmartPanel, AuthorPanel, DevicePanel;
        Button BtnDevRefresh, BtnDevCopy, BtnDevSave, BtnDxdiag, BtnMsinfo;
        TextBlock DevStatus;
        Button BtnTheme, BtnLang;
        System.Windows.Shapes.Path ThemeIcon;
        TextBlock ThemeLabel, LangLabel;
        WrapPanel SmartPicker;
        ComboBox CmbDrive, CmbScope, CmbMin, CmbPerfDisk;
        ListView FoldersList, FilesList;
        TextBox LogBox;
        TextBlock StatusText3, InstallBarText;
        Border InstallBar;
        Button BtnInstallBar;
        Border UpdateBar;
        TextBlock UpdateBarText, UpdateStatusText;
        Button BtnUpdateRestart, BtnUpdateNotes, BtnUpdateLater, BtnCheckUpdates;
        CheckBox ChkAutoUpdate;

        public MainWindow(AppOptions appOptions)
        {
            options = appOptions;
            demo = options.Demo && options.SnapshotPath != null;
            admin = Env.IsAdmin || demo;
            stillPictures = options.SnapshotPath != null;
            LoadLayout();
            ApplyLanguage();
            ApplyTheme();
            SizeWindow();
            if (options.SnapshotPath != null) PrepareForSnapshot();

            SmoothScroll.Attach(Window);
            SidewaysScroll.Attach(Window);
            ScrollCorner.Attach(FilesList);
            ScrollCorner.Attach(FoldersList);

            items = demo ? DemoItems() : Catalog.Build();
            BuildList();
            BuildHelpTab();
            BuildAuthorCards();
            WireEvents();
            Window.ContentRendered += OnRendered;
            Window.Closing += OnClosing;
        }

        // ------------------------------------------------------------ start-up
        void LoadLayout()
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("DiskCleanupHelper.MainWindow.xaml"))
            {
                Window = (Window)XamlReader.Load(s);
            }
            FindControls();
            Window.Title = AppInfo.Title + " " + AppInfo.DisplayVersion;
            AboutTitle.Text = AppInfo.Title + "  " + AppInfo.DisplayVersion;
            SetWindowIcon();
            if (!admin) AdminBanner.Visibility = Visibility.Visible;
        }

        void ApplyLanguage()
        {
            L.TranslateTree(Window);   // the texts written in MainWindow.xaml
            if (L.Rtl) Window.FlowDirection = FlowDirection.RightToLeft;
            if (!string.IsNullOrEmpty(L.Font)) Window.FontFamily = new FontFamily(L.Font);
            LangLabel.Text = L.Name;
            BtnLang.ToolTip = L.T("Language");
            StatFreeLabel.Text = L.F("{0} FREE (WINDOWS DRIVE)", AppPaths.SysDrive);
            BtnChkdsk.Content = L.F("Scan {0} for errors", AppPaths.SysDrive);
        }

        void ApplyTheme()
        {
            Theme.Dark = options.Theme != null ? options.Theme == "dark" : Theme.LoadSaved();
            Theme.Apply(Window);
            UpdateThemeButton();
            Window.SourceInitialized += delegate { Theme.SetDarkTitleBar(Window); };
        }

        void SizeWindow()
        {
            Rect area = SystemParameters.WorkArea;
            Window.Width = Math.Min(1200, area.Width - 10);
            Window.Height = Math.Min(740, area.Height - 10);
        }

        // Opens where the previous window was (after a language switch), maximized if it was.
        void TakePlacementFrom(Window previous)
        {
            Rect bounds = previous.WindowState == WindowState.Normal
                ? new Rect(previous.Left, previous.Top, previous.ActualWidth, previous.ActualHeight)
                : previous.RestoreBounds;
            Window.WindowStartupLocation = WindowStartupLocation.Manual;
            Window.Left = bounds.Left;
            Window.Top = bounds.Top;
            Window.Width = bounds.Width;
            Window.Height = bounds.Height;
            if (previous.WindowState == WindowState.Maximized) Window.WindowState = WindowState.Maximized;
        }

        // Test snapshots open the window off screen and close it once the picture is saved.
        void PrepareForSnapshot()
        {
            Window.WindowStartupLocation = WindowStartupLocation.Manual;
            Window.Left = -6000;
            Window.Top = -6000;
            Window.ShowInTaskbar = false;
            if (options.SampleWd) Window.Height = 1700;   // tall enough for a whole SMART table
            if (options.SnapshotWidth > 0) Window.Width = options.SnapshotWidth;
        }

        T Find<T>(string name) where T : class { return Window.FindName(name) as T; }

        void FindControls()
        {
            StatFreeLabel = Find<TextBlock>("StatFreeLabel"); StatFree = Find<TextBlock>("StatFree"); StatFreeSub = Find<TextBlock>("StatFreeSub");
            StatMem = Find<TextBlock>("StatMem"); StatMemSub = Find<TextBlock>("StatMemSub"); StatSel = Find<TextBlock>("StatSel"); StatSelSub = Find<TextBlock>("StatSelSub");
            StatusText = Find<TextBlock>("StatusText"); DetTitle = Find<TextBlock>("DetTitle"); DetSize = Find<TextBlock>("DetSize");
            StatusText2 = Find<TextBlock>("StatusText2"); FolderPath = Find<TextBlock>("FolderPath"); FilesTitle = Find<TextBlock>("FilesTitle");
            AboutTitle = Find<TextBlock>("AboutTitle"); InstallState = Find<TextBlock>("InstallState"); Tagline = Find<TextBlock>("Tagline"); AppTitle = Find<TextBlock>("AppTitle");
            AdminBanner = Find<Border>("AdminBanner"); Tabs = Find<TabControl>("Tabs");
            BtnScan = Find<Button>("BtnScan"); BtnStop = Find<Button>("BtnStop"); BtnSafe = Find<Button>("BtnSafe"); BtnNone = Find<Button>("BtnNone");
            BtnClean = Find<Button>("BtnClean"); BtnOpen = Find<Button>("BtnOpen"); BtnAnalyze = Find<Button>("BtnAnalyze"); BtnStop2 = Find<Button>("BtnStop2");
            BtnUp = Find<Button>("BtnUp"); BtnOpenFolder = Find<Button>("BtnOpenFolder"); BtnFileOpen = Find<Button>("BtnFileOpen");
            BtnFileRecycle = Find<Button>("BtnFileRecycle"); BtnFileDelete = Find<Button>("BtnFileDelete"); BtnHealth = Find<Button>("BtnHealth");
            BtnChkdsk = Find<Button>("BtnChkdsk"); BtnOptimize = Find<Button>("BtnOptimize"); BtnStorage = Find<Button>("BtnStorage");
            BtnInstall = Find<Button>("BtnInstall"); BtnUninstall = Find<Button>("BtnUninstall"); BtnHist = Find<Button>("BtnHist"); BtnHelp = Find<Button>("BtnHelp");
            Progress = Find<ProgressBar>("Progress"); Progress2 = Find<ProgressBar>("Progress2");
            ListPanel = Find<StackPanel>("ListPanel"); DetBody = Find<StackPanel>("DetBody"); HealthPanel = Find<StackPanel>("HealthPanel");
            HelpPanel = Find<StackPanel>("HelpPanel");
            SmartPanel = Find<StackPanel>("SmartPanel"); SmartPicker = Find<WrapPanel>("SmartPicker");
            AuthorPanel = Find<StackPanel>("AuthorPanel");
            CmbDrive = Find<ComboBox>("CmbDrive"); CmbScope = Find<ComboBox>("CmbScope"); CmbMin = Find<ComboBox>("CmbMin");
            FoldersList = Find<ListView>("FoldersList"); FilesList = Find<ListView>("FilesList"); LogBox = Find<TextBox>("LogBox");
            BtnHealthHistory = Find<Button>("BtnHealthHistory"); BtnSpeedTest = Find<Button>("BtnSpeedTest"); BtnStop3 = Find<Button>("BtnStop3");
            PerfPanel = Find<StackPanel>("PerfPanel"); CmbPerfDisk = Find<ComboBox>("CmbPerfDisk"); StatusText3 = Find<TextBlock>("StatusText3");
            InstallBar = Find<Border>("InstallBar"); InstallBarText = Find<TextBlock>("InstallBarText");
            BtnInstallBar = Find<Button>("BtnInstallBar");
            UpdateBar = Find<Border>("UpdateBar"); UpdateBarText = Find<TextBlock>("UpdateBarText"); UpdateStatusText = Find<TextBlock>("UpdateStatusText");
            BtnUpdateRestart = Find<Button>("BtnUpdateRestart"); BtnUpdateNotes = Find<Button>("BtnUpdateNotes"); BtnUpdateLater = Find<Button>("BtnUpdateLater");
            BtnCheckUpdates = Find<Button>("BtnCheckUpdates"); ChkAutoUpdate = Find<CheckBox>("ChkAutoUpdate");
            DevicePanel = Find<StackPanel>("DevicePanel"); DevStatus = Find<TextBlock>("DevStatus");
            BtnDevRefresh = Find<Button>("BtnDevRefresh"); BtnDevCopy = Find<Button>("BtnDevCopy"); BtnDevSave = Find<Button>("BtnDevSave");
            BtnDxdiag = Find<Button>("BtnDxdiag"); BtnMsinfo = Find<Button>("BtnMsinfo");
            BtnTheme = Find<Button>("BtnTheme"); ThemeIcon = Find<System.Windows.Shapes.Path>("ThemeIcon"); ThemeLabel = Find<TextBlock>("ThemeLabel");
            BtnLang = Find<Button>("BtnLang"); LangLabel = Find<TextBlock>("LangLabel");
        }

        void SetWindowIcon()
        {
            try
            {
                using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(AppPaths.ExePath))
                {
                    if (icon != null) Window.Icon = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
            }
            catch { }
        }

        void OnRendered(object sender, EventArgs e)
        {
            FillAnalyzeChoices();
            UpdateStatus();
            UpdateSelection();
            UpdateInstallState();
            Log(admin ? L.T("Running as administrator. All items are available.") : L.T("Not running as administrator. Items marked Admin are disabled."));
            Log(L.T("Only items found on this PC are listed. Click \"Measure sizes\" to see how much each one uses. Nothing is deleted until you confirm."));
            if (!Installer.IsInstalled) Log(L.T("Tip: the About tab can install it, so it appears in the Start menu."));
            if (items.Count > 0) SelectItem(items[0]);
            ShowUpdateStatus();
            if (options.SnapshotPath != null) { TakeSnapshot(); return; }
            if (startTab > 0) Tabs.SelectedIndex = startTab;   // may start reading that tab's data
            StartUpdates();
            RecordHealthInBackground();
            if (options.TestScanRoot != null)
            {
                Tabs.SelectedIndex = Tab.Analyze;   // test switch: open Analyze disk with this folder scanned
                StartAnalyze(options.TestScanRoot, (long)options.TestMinMB * 1048576, false);
            }
            else if (items.Count > 0) StartMeasure(true);
        }

        void OnClosing(object sender, CancelEventArgs e)
        {
            if (busy && measuring) cancel.IsSet = true;
            else if (busy) { e.Cancel = true; Msg(L.T("Please wait until the current task finishes, or click Stop.")); return; }
            InstallUpdateOnClose();
            if (perfTimer != null) perfTimer.Stop();
            if (tempTimer != null) tempTimer.Stop();
        }

        // ------------------------------------------------------------ messages and status
        // Colours are written as their light-theme value; Theme maps them to the current theme.
        static Brush B(string hex) { return Theme.Get(hex); }

        // Runs an action on the window's thread (directly when already there).
        void Ui(Action action)
        {
            if (Window.Dispatcher.CheckAccess()) action();
            else Window.Dispatcher.BeginInvoke(action);
        }

        // Adds a line to the log box at the bottom. Safe to call from any thread.
        void Log(string message)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + "  " + message + "\r\n";
            Ui(delegate { LogBox.AppendText(line); LogBox.ScrollToEnd(); });
        }

        void Msg(string text)
        {
            MessageBox.Show(Window, text, AppInfo.Title, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, L.MsgOptions);
        }

        bool AskYesNo(string text, string title)
        {
            return MessageBox.Show(Window, text, title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, L.MsgOptions) == MessageBoxResult.Yes;
        }

        static bool MemoryIsLow(out double availableGb, out double commitGb)
        {
            Env.GetMemory(out availableGb, out commitGb);
            return availableGb < LowMemoryGb || commitGb < LowCommitGb;
        }

        // Asks before heavy work when memory is low (running out can crash programs or even Windows).
        bool ConfirmMemory(string question)
        {
            double a, c;
            if (!MemoryIsLow(out a, out c)) return true;
            return AskYesNo(L.F("Your PC is low on memory right now:\r\n\r\n  Available memory: {0:N1} GB\r\n  Room left including the page file: {1:N1} GB\r\n\r\nRunning out of memory can make programs crash and, on some PCs, cause a blue screen.\r\nClose programs you do not need (browsers, editors, games), then try again.", a, c) +
                            "\r\n\r\n" + question, L.T("Low memory"));
        }

        // The header tiles: free space on the Windows drive and available memory.
        void UpdateStatus()
        {
            if (demo) { ShowDemoStatus(); return; }
            try
            {
                DriveInfo d = new DriveInfo(AppPaths.SysDrive);
                StatFree.Text = L.Ltr((d.AvailableFreeSpace / 1073741824.0).ToString("N1") + " GB");
                StatFreeSub.Text = L.F("of {0} GB", (d.TotalSize / 1073741824.0).ToString("N0"));
            }
            catch { }
            double a, c;
            bool low = MemoryIsLow(out a, out c);
            StatMem.Text = L.Ltr(a.ToString("N1") + " GB");
            StatMem.Foreground = low ? B("#F87171") : Brushes.White;   // the header is dark in both themes
            StatMemSub.Text = low ? L.T("low: close some programs") : L.T("OK");
        }

        static long FreeBytes() { try { return new DriveInfo(AppPaths.SysDrive).AvailableFreeSpace; } catch { return 0; } }

        // ------------------------------------------------------------ background work
        void SetBusy(bool isBusy)
        {
            busy = isBusy;
            foreach (Control c in new Control[] { BtnScan, BtnSafe, BtnNone, BtnClean, BtnAnalyze, CmbDrive, CmbScope, CmbMin, BtnFileRecycle, BtnFileDelete,
                                                  BtnHealth, BtnChkdsk, BtnInstall, BtnUninstall, BtnSpeedTest, CmbPerfDisk })
                if (c != null) c.IsEnabled = !isBusy;
            BtnStop.IsEnabled = isBusy;
            BtnStop2.IsEnabled = isBusy;
            BtnStop3.IsEnabled = isBusy;
            foreach (CleanupItem it in items) if (it.Check != null) it.Check.IsEnabled = !isBusy && !(it.Admin && !admin);
            Window.Cursor = isBusy ? Cursors.AppStarting : null;
        }

        // Runs work on a background thread, then "after" on the window's thread. One task at a time: the buttons that
        // start work are disabled meanwhile, and Stop sets the task's CancelFlag.
        void RunWork(Action<CancelFlag> work, Action after)
        {
            CancelFlag flag = new CancelFlag();
            cancel = flag;
            SetBusy(true);
            Thread t = new Thread(delegate()
            {
                Exception error = null;
                try { work(flag); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { error = ex; }
                Ui(delegate
                {
                    try
                    {
                        if (error != null) Log(L.F("Problem: {0}", error.Message));
                        if (after != null) after();
                    }
                    finally
                    {
                        SetBusy(false);
                        if (healthPending) ReadHealthWhenFree();
                    }
                });
            });
            t.IsBackground = true;
            t.Start();
        }

        // Disk health was opened while another task ran: read the disks once that task has fully finished.
        void ReadHealthWhenFree()
        {
            healthPending = false;
            Window.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (healthShown) return;
                if (busy) healthPending = true; else ShowHealth(false);
            }));
        }
    }
}
