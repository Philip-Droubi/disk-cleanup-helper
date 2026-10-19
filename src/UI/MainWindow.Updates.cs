// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Updates: the "Updates" card in About and the bar under the header. Updater (src/App) does the work on a background
// thread; this file shows the results. A downloaded update is installed by "Restart now", or when the app closes.
using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Windows;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.UI
{
    public partial class MainWindow
    {
        static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(24);
        UpdateInfo readyUpdate;      // downloaded and checked, waiting to be installed
        string updatedNotes;         // "What's new" of this version, on the first start after an update
        bool updateBusy;             // a check or download is running
        bool updateInstalled;        // the new version is in place: nothing more to install on close
        readonly CancelFlag updateCancel = new CancelFlag();

        static bool AutoUpdateOn { get { return AppSettings.Read("autoUpdate") != "off"; } }

        void WireUpdateEvents()
        {
            ChkAutoUpdate.IsChecked = AutoUpdateOn;
            ChkAutoUpdate.Click += delegate { AppSettings.Write("autoUpdate", ChkAutoUpdate.IsChecked == true ? "on" : "off"); };
            BtnCheckUpdates.Click += delegate { CheckForUpdates(true); };
            BtnUpdateRestart.Click += delegate { RestartToUpdate(); };
            BtnUpdateLater.Click += delegate { UpdateBar.Visibility = Visibility.Collapsed; };
            BtnUpdateNotes.Click += delegate
            {
                if (readyUpdate != null) ShowWhatsNew(readyUpdate.Version, readyUpdate.Notes);
                else ShowWhatsNew(Updater.Current, updatedNotes);
            };
        }

        // At start: finish an update that just happened, offer one downloaded earlier, or ask GitHub (at most once a day).
        void StartUpdates()
        {
            string notes;
            if (Updater.FinishUpdate(options.UpdatedFrom != null, out notes))
            {
                options.UpdatedFrom = null;   // once: a language switch builds a new window with the same options
                Log(L.F("Updated to version {0}.", Updater.Current));
                ShowUpdatedBar(notes);
            }
            if (Updater.IsDeveloperBuild || options.TestScanRoot != null) return;
            UpdateInfo pending = Updater.Pending();
            if (pending != null) ShowReady(pending);   // and still check: a newer release replaces it, a withdrawn one ends it
            if (!AutoUpdateOn || (Updater.FeedFile == null && !UpdateCheckDue())) return;
            CheckForUpdates(false);
        }

        static bool UpdateCheckDue()
        {
            DateTime last;
            string s = AppSettings.Read("lastUpdateCheck");
            if (s == null || !DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out last)) return true;
            TimeSpan age = DateTime.UtcNow - last.ToUniversalTime();
            return age >= UpdateCheckInterval || age < TimeSpan.Zero;
        }

        // manual: the "Check for updates" button. The automatic check stays quiet when it fails (a log line only).
        void CheckForUpdates(bool manual)
        {
            if (updateBusy) return;
            updateBusy = true;
            BtnCheckUpdates.IsEnabled = false;
            UpdateStatusText.Text = L.T("Checking for updates...");
            bool lowMemory = Env.IsLowMemory();
            ThreadPool.QueueUserWorkItem(delegate
            {
                UpdateInfo info = null;
                string error = null;
                bool offline = false;
                try { info = Updater.Check(); }
                catch (WebException) { offline = true; }
                catch (Exception ex) { error = ex.Message; }
                Ui(delegate { AfterUpdateCheck(info, error, offline, manual, lowMemory); });
            });
        }

        void AfterUpdateCheck(UpdateInfo info, string error, bool offline, bool manual, bool lowMemory)
        {
            updateBusy = false;
            BtnCheckUpdates.IsEnabled = true;
            if (offline)
            {
                Log(L.T("Update check: could not reach GitHub."));
                if (readyUpdate != null) ShowReady(readyUpdate);
                else UpdateStatusText.Text = L.T("Could not reach GitHub. Check the internet connection and try again.");
                return;
            }
            AppSettings.Write("lastUpdateCheck", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            if (error != null)
            {
                Log(L.F("Update check: {0}", error));
                if (readyUpdate != null) ShowReady(readyUpdate);
                else UpdateStatusText.Text = L.F("Could not check for updates: {0}", error);
                return;
            }
            if (info == null)
            {
                Log(L.F("Update check: version {0} is the newest.", Updater.Current));
                if (readyUpdate != null)
                {
                    // the downloaded version is no longer the newest on GitHub: it was withdrawn
                    readyUpdate = null;
                    Updater.DiscardDownloads();
                    UpdateBar.Visibility = Visibility.Collapsed;
                }
                ShowUpdateStatus();
                return;
            }
            Log(L.F("Update check: version {0} is available.", info.Version));
            if (readyUpdate != null && readyUpdate.Version == info.Version && string.Equals(readyUpdate.Sha256, info.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                ShowReady(readyUpdate);   // already downloaded and checked
                return;
            }
            if (Updater.IsDeveloperBuild)
                UpdateStatusText.Text = L.F("Version {0} is available. This is a developer build, so it does not update itself.", info.Version);
            else if (!manual && lowMemory)
                UpdateStatusText.Text = L.F("Version {0} is available. Click \"Check for updates\" to download it.", info.Version);
            else StartUpdateDownload(info);
        }

        void StartUpdateDownload(UpdateInfo info)
        {
            updateBusy = true;
            BtnCheckUpdates.IsEnabled = false;
            UpdateStatusText.Text = L.F("Downloading version {0}...", info.Version);
            Log(L.F("Downloading version {0}...", info.Version));
            ThreadPool.QueueUserWorkItem(delegate
            {
                string error = null;
                try
                {
                    Updater.Download(info, delegate(int pct)
                    {
                        Ui(delegate { UpdateStatusText.Text = L.F("Downloading version {0}... {1}%", info.Version, pct); });
                    }, updateCancel);
                }
                catch (OperationCanceledException) { return; }   // the window closed
                catch (Exception ex) { error = ex.Message; }
                Ui(delegate
                {
                    updateBusy = false;
                    BtnCheckUpdates.IsEnabled = true;
                    if (error != null)
                    {
                        Log(L.F("The update was not downloaded: {0}", error));
                        UpdateStatusText.Text = L.F("The update could not be downloaded: {0}", error);
                        return;
                    }
                    Log(L.F("Version {0} is downloaded and checked (SHA256 and version number).", info.Version));
                    ShowReady(info);
                });
            });
        }

        // The About card's status line when nothing is running.
        void ShowUpdateStatus()
        {
            if (readyUpdate != null) { UpdateStatusText.Text = L.F("Version {0} is ready. Restart to update.", readyUpdate.Version); return; }
            DateTime last;
            string s = AppSettings.Read("lastUpdateCheck");
            string text = s != null && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out last)
                ? L.F("You have the latest version ({0}). Last checked: {1}.", Updater.Current, last.ToLocalTime().ToString("g"))
                : L.F("This is version {0}. It has not checked for updates yet.", Updater.Current);
            if (Updater.IsDeveloperBuild) text += " " + L.T("This is a developer build, so it does not update itself.");
            UpdateStatusText.Text = text;
        }

        void ShowReady(UpdateInfo info)
        {
            readyUpdate = info;
            string text = L.F("Version {0} is ready. Restart to update.", info.Version);
            UpdateStatusText.Text = text;
            UpdateBarText.Text = text;
            BtnUpdateRestart.Visibility = Visibility.Visible;
            BtnUpdateNotes.Visibility = Visibility.Visible;
            BtnUpdateLater.Content = L.T("Later");
            UpdateBar.Visibility = Visibility.Visible;
        }

        // Once, on the first start after an update.
        void ShowUpdatedBar(string notes)
        {
            updatedNotes = notes;
            UpdateBarText.Text = L.F("Updated to version {0}.", Updater.Current);
            BtnUpdateRestart.Visibility = Visibility.Collapsed;
            BtnUpdateNotes.Visibility = string.IsNullOrEmpty(notes) ? Visibility.Collapsed : Visibility.Visible;
            BtnUpdateLater.Content = L.T("OK");
            UpdateBar.Visibility = Visibility.Visible;
        }

        void ShowWhatsNew(Version version, string notes)
        {
            Msg(L.F("What's new in version {0}:", version) + "\r\n\r\n" + (string.IsNullOrEmpty(notes) ? L.T("No details were given for this version.") : notes));
        }

        void RestartToUpdate()
        {
            UpdateInfo info = readyUpdate;
            if (info == null) return;
            if (busy) { Msg(L.T("Please wait until the current task finishes, or click Stop.")); return; }
            if (!InstallUpdate(info, true)) return;
            try { Updater.StartNewVersion(); }
            catch (Exception ex)
            {
                // blocked by antivirus or a company rule, for example: put this version back and keep running
                Log(L.F("The update could not be installed: {0}", ex.Message));
                try { Updater.Rollback(); }
                catch (Exception back)
                {
                    Msg(L.F("Version {0} is installed, but it could not be started: {1}\r\n\r\nStart Disk Cleanup Helper again.", info.Version, ex.Message + " / " + back.Message));
                    Window.Close();
                    return;
                }
                updateInstalled = false;
                readyUpdate = info;
                Msg(L.F("The update could not be installed. This copy keeps working as it is.\r\n\r\n{0}", ex.Message));
                return;
            }
            Window.Close();
        }

        // "Later", or the bar was ignored: the update goes in while the app closes, and the next start is the new
        // version. Not when this window is only being replaced (a language switch keeps the app running).
        void InstallUpdateOnClose()
        {
            if (Application.Current != null && Application.Current.MainWindow != Window) return;
            updateCancel.IsSet = true;
            if (stillPictures || Updater.IsDeveloperBuild) return;   // test pictures (/updatebar) and builds never swap
            if (readyUpdate != null && readyUpdate.LocalPath != null && !updateInstalled) InstallUpdate(readyUpdate, false);
        }

        // Puts the downloaded version in place of this exe. interactive: explain a problem in a message.
        bool InstallUpdate(UpdateInfo info, bool interactive)
        {
            if (info.LocalPath == null) return false;
            if (!Updater.CanReplaceRunningExe())
            {
                Log(L.F("The update cannot be installed here, because this folder cannot be changed: {0}", AppPaths.ExeDir));
                if (interactive)
                {
                    Msg(L.F("This copy is in a folder that cannot be changed:\r\n{0}\r\n\r\nThe release page opens, so you can download version {1} and replace this copy by hand.", AppPaths.ExeDir, info.Version));
                    string page = info.PageUrl ?? Updater.ReleasesPage;
                    if (Updater.FeedFile != null) Log("Test feed: would open " + page);   // tests never open a browser
                    else OpenWebLink(page);
                }
                return false;
            }
            try
            {
                Updater.Install(info);
                updateInstalled = true;
                readyUpdate = null;
                Log(L.F("Version {0} is installed.", info.Version));
                return true;
            }
            catch (Exception ex)
            {
                Log(L.F("The update could not be installed: {0}", ex.Message));
                if (interactive) Msg(L.F("The update could not be installed. This copy keeps working as it is.\r\n\r\n{0}", ex.Message));
                return false;
            }
        }
    }
}
