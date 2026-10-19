// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// The list of cleanup items. Add, change or remove items here.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Controls;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.Cleanup
{
    public static class Catalog
    {
        public static readonly Dictionary<string, string> AppNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "chrome", "Google Chrome" }, { "msedge", "Microsoft Edge" }, { "brave", "Brave" }, { "vivaldi", "Vivaldi" }, { "opera", "Opera" },
            { "firefox", "Firefox" }, { "Code", "Visual Studio Code" }, { "Cursor", "Cursor" }, { "Teams", "Microsoft Teams (classic)" },
            { "ms-teams", "Microsoft Teams" }, { "Discord", "Discord" }, { "slack", "Slack" }, { "Spotify", "Spotify" }
        };

        public static string AppName(string process)
        {
            string n;
            return AppNames.TryGetValue(process, out n) ? n : process;
        }

        static List<string> Lines(params string[] s) { return new List<string>(s); }

        // Path.Combine; kept short because the item list below uses it on almost every line.
        static string J(string a, string b) { return Path.Combine(a, b); }

        public static long? MeasureDefault(CleanupItem it, CancelFlag cancel)
        {
            if (it.Paths == null) return null;
            return FileOps.SizeOf(it.Paths(), cancel);
        }

        public static bool IsPresent(CleanupItem it)
        {
            if (it.Always) return true;
            if (it.ShowIf != null) { try { return it.ShowIf(); } catch { return false; } }
            if (it.Paths != null) { try { return it.Paths().Any(FileOps.Exists); } catch { return false; } }
            return false;
        }

        static void StopServices(string[] names, WorkContext ctx, List<string> stopped)
        {
            foreach (string name in names)
            {
                try
                {
                    using (ServiceController sc = new ServiceController(name))
                    {
                        if (sc.Status != ServiceControllerStatus.Stopped)
                        {
                            sc.Stop();
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                            stopped.Add(name);
                        }
                    }
                }
                catch (Exception ex) { ctx.Log("  " + L.F("Could not stop {0}: {1}", name, ex.Message)); }
            }
        }

        static void StartServices(List<string> names)
        {
            foreach (string name in names)
            {
                try { using (ServiceController sc = new ServiceController(name)) { sc.Start(); } } catch { }
            }
        }

        static void LogLines(WorkContext ctx, string text)
        {
            foreach (string l in (text ?? "").Split('\n'))
            {
                string t = l.Trim();
                if (t.Length > 0) ctx.Log("  " + t);
            }
        }

        // Every item that applies to this PC, in display order. Items whose folders do not exist are left out.
        public static List<CleanupItem> Build()
        {
            return BuildAll().Where(IsPresent).ToList();
        }

        // Every item, whether or not it exists on this PC (the /demo pictures pick from these).
        public static List<CleanupItem> BuildAll()
        {
            // short names for the base folders used in the item paths below
            string UL = AppPaths.LocalAppData, UR = AppPaths.RoamingAppData, UP = AppPaths.UserProfile, SysRoot = AppPaths.WindowsDir, ProgData = AppPaths.ProgramData;
            string SysDrive = AppPaths.SysDrive, Root = AppPaths.Root;
            string doCache = J(SysRoot, @"ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache");
            string chrome = J(UL, @"Google\Chrome\User Data");
            string edge = J(UL, @"Microsoft\Edge\User Data");
            string brave = J(UL, @"BraveSoftware\Brave-Browser\User Data");
            string vivaldi = J(UL, @"Vivaldi\User Data");
            string newTeams = J(UL, @"Packages\MSTeams_8wekyb3d8bbwe\LocalCache\Microsoft\MSTeams\EBWebView");
            string keeps = L.T("KEEPS: passwords, bookmarks, history, cookies and site logins, autofill, extensions, settings, installed web apps.");
            string after = L.T("Pages load a little slower the first time. Very rarely a site may ask you to log in again, and the browser fills in your saved password.");
            string appKeeps = L.T("KEEPS: your login, messages, settings and files. The app downloads what it needs again.");
            string[] codeCaches = { "Cache", "CachedData", "Code Cache", "GPUCache", "Crashpad" };
            string workspaceRoot = J(UR, @"Code\User\workspaceStorage");
            string downloads = Finders.DownloadsPath();

            List<CleanupItem> items = new List<CleanupItem>();

            // ---------------------------------------------------------------- 1. Safe
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, MustClose = new[] { "chrome" },
                Name = L.T("Google Chrome cache"), Short = L.T("Keeps passwords, bookmarks, logins"),
                Where = chrome + @"\(each profile)\Cache, Code Cache, GPUCache, Service Worker\CacheStorage",
                Details = Lines(L.T("Deletes only Chrome's cache folders in every profile, plus its graphics caches."), keeps, after,
                            L.T("Close every Chrome window first. If a Chrome icon is near the clock, right-click it and choose Exit.")),
                Paths = () => Finders.ChromiumCacheDirs(chrome),
                Open = log => FileOps.OpenPath(chrome)
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, MustClose = new[] { "msedge" },
                Name = L.T("Microsoft Edge cache"), Short = L.T("Keeps passwords, favorites, logins"),
                Where = edge + @"\(each profile)\Cache, Code Cache, GPUCache, Service Worker\CacheStorage",
                Details = Lines(L.T("Deletes only Edge's cache folders in every profile, plus its graphics caches."), keeps, after,
                            L.T("Edge often keeps running in the background. If the tool says it is still running: in Edge, Settings > System and performance > turn off \"Startup boost\" and \"Continue running background extensions and apps\".")),
                Paths = () => Finders.ChromiumCacheDirs(edge),
                Open = log => FileOps.OpenPath(edge)
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, MustClose = new[] { "brave" },
                Name = L.T("Brave cache"), Short = L.T("Keeps passwords, bookmarks, logins"),
                Where = brave + @"\(each profile)\Cache, Code Cache, GPUCache, Service Worker\CacheStorage",
                Details = Lines(L.T("Deletes only Brave's cache folders in every profile."), keeps, after, L.T("Close Brave first.")),
                Paths = () => Finders.ChromiumCacheDirs(brave),
                Open = log => FileOps.OpenPath(brave)
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, MustClose = new[] { "vivaldi" },
                Name = L.T("Vivaldi cache"), Short = L.T("Keeps passwords, bookmarks, logins"),
                Where = vivaldi + @"\(each profile)\Cache, Code Cache, GPUCache",
                Details = Lines(L.T("Deletes only Vivaldi's cache folders in every profile."), keeps, after, L.T("Close Vivaldi first.")),
                Paths = () => Finders.ChromiumCacheDirs(vivaldi),
                Open = log => FileOps.OpenPath(vivaldi)
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, MustClose = new[] { "firefox" },
                Name = L.T("Firefox cache"), Short = L.T("Keeps passwords, bookmarks, logins"),
                Where = J(UL, @"Mozilla\Firefox\Profiles") + @"\(each profile)\cache2",
                Details = Lines(L.T("Deletes only Firefox's cache folder in every profile."), keeps, after, L.T("Close Firefox first.")),
                Paths = () => Finders.FirefoxCacheDirs()
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, MustClose = new[] { "opera" },
                Name = L.T("Opera cache"), Short = L.T("Keeps passwords, bookmarks, logins"),
                Where = L.F("{0} (and Opera GX)", J(UL, @"Opera Software\Opera Stable\Cache")),
                Details = Lines(L.T("Deletes only Opera's cache folder."), keeps, L.T("Close Opera first.")),
                Paths = () => new[] { J(UL, @"Opera Software\Opera Stable\Cache"), J(UL, @"Opera Software\Opera GX Stable\Cache") }
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, MustClose = new[] { "ms-teams", "Teams" },
                Name = L.T("Microsoft Teams cache"), Short = L.T("Keeps login, chats, files"),
                Where = L.T("Teams cache folders (new Teams and classic Teams)"),
                Details = Lines(L.T("Deletes only Teams' cache folders. Microsoft recommends this when Teams is slow or acts strangely."), appKeeps,
                            L.T("Close Teams first, including its icon near the clock.")),
                Paths = () => Finders.ChromiumCacheDirs(newTeams).Concat(Finders.AppCacheDirs(J(UR, @"Microsoft\Teams")))
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, MustClose = new[] { "Discord" },
                Name = L.T("Discord cache"), Short = L.T("Keeps login and messages"),
                Where = J(UR, "discord") + @"\Cache, Code Cache, GPUCache",
                Details = Lines(L.T("Deletes only Discord's cache folders."), appKeeps, L.T("Close Discord first, including its icon near the clock.")),
                Paths = () => Finders.AppCacheDirs(J(UR, "discord"))
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, MustClose = new[] { "slack" },
                Name = L.T("Slack cache"), Short = L.T("Keeps login and messages"),
                Where = J(UR, "Slack") + @"\Cache, Code Cache, GPUCache, Service Worker\CacheStorage",
                Details = Lines(L.T("Deletes only Slack's cache folders."), appKeeps, L.T("Close Slack first, including its icon near the clock.")),
                Paths = () => Finders.AppCacheDirs(J(UR, "Slack"))
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true,
                Name = L.T("Graphics caches (DirectX, NVIDIA, AMD, Intel)"), Short = L.T("Games may stutter once while they rebuild"),
                Where = L.F("{0} and similar", UL + @"\D3DSCache, NVIDIA\DXCache, AMD\DxCache"),
                Details = Lines(L.T("Shader files that Windows and the graphics card driver save so games and apps start faster."),
                            L.T("They are rebuilt automatically. The first launch of a game afterwards may stutter a little.")),
                Paths = () => new[] {
                    J(UL, "D3DSCache"), J(UL, @"NVIDIA\DXCache"), J(UL, @"NVIDIA\GLCache"),
                    J(UL, @"AMD\DxCache"), J(UL, @"AMD\DxcCache"), J(UL, @"AMD\GLCache"), J(UL, @"AMD\VkCache"),
                    J(UP, @"AppData\LocalLow\Intel\ShaderCache") }
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true,
                Name = L.T("Your temp files (older than 1 day)"), Short = L.T("Files in use are skipped"),
                Where = J(UL, "Temp"),
                Details = Lines(L.T("Temporary files programs leave behind. Only files older than 1 day are deleted, so running installs are not disturbed."),
                            L.T("The size shown is the whole folder. The newest files stay, so the amount freed can be a bit lower.")),
                Paths = () => new[] { J(UL, "Temp") },
                Clean = (it, ctx) => FileOps.ClearFolder(J(UL, "Temp"), 1, ctx.Counter)
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, Admin = true,
                Name = L.T("Windows temp files (older than 1 day)"), Short = L.T("Files in use are skipped"),
                Where = J(SysRoot, "Temp"),
                Details = Lines(L.T("Temporary files Windows and installers leave behind. Only files older than 1 day are deleted.")),
                Paths = () => new[] { J(SysRoot, "Temp") },
                Clean = (it, ctx) => FileOps.ClearFolder(J(SysRoot, "Temp"), 1, ctx.Counter)
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, Admin = true,
                Name = L.T("Windows Update downloads"), Short = L.T("Installed updates are not affected"),
                Where = L.F("{0} and the Delivery Optimization cache", J(SysRoot, @"SoftwareDistribution\Download")),
                Details = Lines(L.T("Update files Windows already downloaded. Installed updates are not affected."),
                            L.T("If an update is still waiting to install, Windows downloads it again."),
                            L.T("The Windows Update service is stopped for a moment and started again.")),
                Paths = () => new[] { J(SysRoot, @"SoftwareDistribution\Download"), doCache },
                Clean = (it, ctx) =>
                {
                    List<string> stopped = new List<string>();
                    StopServices(new[] { "wuauserv", "bits", "dosvc" }, ctx, stopped);
                    FileOps.ClearFolder(J(SysRoot, @"SoftwareDistribution\Download"), 0, ctx.Counter);
                    FileOps.ClearFolder(doCache, 0, ctx.Counter);
                    stopped.Reverse();
                    StartServices(stopped);
                }
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, Admin = true,
                Name = L.T("Windows error reports"), Short = L.T("Old problem reports"),
                Where = L.F("{0} and your own WER folder", J(ProgData, @"Microsoft\Windows\WER")),
                Details = Lines(L.T("Reports Windows saved about programs that crashed. They are not needed once sent or once the problem is gone.")),
                Paths = () => new[] {
                    J(ProgData, @"Microsoft\Windows\WER\ReportArchive"), J(ProgData, @"Microsoft\Windows\WER\ReportQueue"),
                    J(UL, @"Microsoft\Windows\WER\ReportArchive"), J(UL, @"Microsoft\Windows\WER\ReportQueue") }
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, Admin = true,
                Name = L.T("Old Windows log files (older than 14 days)"), Short = L.T("Recent logs are kept"),
                Where = L.F("{0} and {1}", J(SysRoot, @"Logs\CBS"), J(SysRoot, @"Logs\DISM")),
                Details = Lines(L.T("Log files Windows writes while installing updates. On some PCs they grow to several GB."),
                            L.T("Only files older than 14 days are deleted. Recent logs stay in case something needs fixing.")),
                Paths = () => new[] { J(SysRoot, @"Logs\CBS"), J(SysRoot, @"Logs\DISM") },
                Clean = (it, ctx) => { FileOps.ClearFolder(J(SysRoot, @"Logs\CBS"), 14, ctx.Counter); FileOps.ClearFolder(J(SysRoot, @"Logs\DISM"), 14, ctx.Counter); }
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, Name = L.T("npm cache"), Short = L.T("Packages download again when needed"), Where = J(UL, "npm-cache"),
                Details = Lines(L.T("Downloaded npm packages (for Node.js developers). npm downloads them again the next time a project needs them.")),
                Paths = () => new[] { J(UL, "npm-cache") }
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, Name = L.T("Yarn cache"), Short = L.T("Packages download again when needed"), Where = J(UL, @"Yarn\Cache"),
                Details = Lines(L.T("Downloaded Yarn packages (for Node.js developers). Yarn downloads them again when needed.")),
                Paths = () => new[] { J(UL, @"Yarn\Cache") }
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, Name = L.T("pip cache"), Short = L.T("Packages download again when needed"), Where = J(UL, @"pip\Cache"),
                Details = Lines(L.T("Downloaded Python packages. pip downloads them again when needed.")),
                Paths = () => new[] { J(UL, @"pip\Cache") }
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, Name = L.T("NuGet download cache"), Short = L.T("Packages download again when needed"), Where = J(UL, @"NuGet\v3-cache"),
                Details = Lines(L.T("Downloaded .NET packages. NuGet downloads them again when needed.")),
                Paths = () => new[] { J(UL, @"NuGet\v3-cache") }
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, Name = L.T("Go build cache"), Short = L.T("Rebuilt on the next build"), Where = J(UL, "go-build"),
                Details = Lines(L.T("Compiled files the Go language keeps to build faster. Go rebuilds them on the next build.")),
                Paths = () => new[] { J(UL, "go-build") }
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, Name = L.T("Composer cache"), Short = L.T("Packages download again when needed"), Where = J(UL, "Composer"),
                Details = Lines(L.T("Downloaded PHP packages. Composer downloads them again when needed.")),
                Paths = () => new[] { J(UL, "Composer") }
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, Name = L.T("Cargo download cache"), Short = L.T("Packages download again when needed"), Where = J(UP, @".cargo\registry\cache"),
                Details = Lines(L.T("Downloaded Rust packages. Cargo downloads them again when needed.")),
                Paths = () => new[] { J(UP, @".cargo\registry\cache") }
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, MustClose = new[] { "Code" },
                Name = L.T("VS Code cache and crash reports"), Short = L.T("Keeps settings, extensions, projects"),
                Where = J(UR, "Code") + @"\Cache, CachedData, Code Cache, GPUCache, Crashpad",
                Details = Lines(L.T("Visual Studio Code's cache folders and crash reports. VS Code rebuilds them."),
                            L.T("KEEPS: settings, extensions, projects, project history and chats."), L.T("Close VS Code first.")),
                Paths = () => codeCaches.Select(s => J(J(UR, "Code"), s))
            });
            items.Add(new CleanupItem {
                Group = "safe", Safe = true, MustClose = new[] { "Cursor" },
                Name = L.T("Cursor cache and crash reports"), Short = L.T("Keeps settings, chats, login"),
                Where = J(UR, "Cursor") + @"\Cache, CachedData, Code Cache, GPUCache, Crashpad",
                Details = Lines(L.T("Cursor's cache folders and crash reports. Cursor rebuilds them."),
                            L.T("KEEPS: settings, chats, login and projects."), L.T("Close Cursor first.")),
                Paths = () => codeCaches.Select(s => J(J(UR, "Cursor"), s))
            });

            // ---------------------------------------------------------------- 2. Old installer files and Windows cleanup
            items.Add(new CleanupItem {
                Group = "updates", Admin = true, Always = true,
                Name = L.T("Old update files of installed programs (keeps newest 3)"), Short = L.T("Programs stay installed. Small risk"),
                Where = L.F("{0} (files picked from Windows' own records)", J(SysRoot, "Installer")),
                Details = Lines(L.T("Some programs keep every update they ever installed. Adobe Acrobat Reader is the usual one, at 0.1 to 0.7 GB per update."),
                            L.T("This deletes only update files that Windows marks as replaced by a newer update. It keeps the newest 3 of each program and never a file still in use."),
                            L.T("Deleted files are listed in deleted-update-files.csv (see the About tab for the folder)."),
                            L.T("Small risk: if a future update or repair of that program complains about a missing file, uninstall and reinstall that program."),
                            L.T("Never delete files in the Windows Installer folder by hand.")),
                Size = (it, cancel) =>
                {
                    List<PatchFile> list = Finders.OldPatches(3, cancel);
                    List<string> lines = new List<string>();
                    foreach (IGrouping<string, PatchFile> g in list.GroupBy(p => p.Product).OrderByDescending(g2 => g2.Sum(p => p.Bytes)))
                        lines.Add(string.Format("  {0,10}   {1}  ", Fmt.Size(g.Sum(p => p.Bytes)), g.Key) + L.P(g.Count(), "(1 file)", "({0} files)"));
                    it.Details = new List<string>(it.BaseDetails);
                    it.Details.Add("");
                    if (lines.Count > 0) { it.Details.Add(L.T("Found on this PC:")); it.Details.AddRange(lines); }
                    else it.Details.Add(L.T("Found on this PC: nothing to remove."));
                    return list.Sum(p => p.Bytes);
                },
                Clean = (it, ctx) => Finders.RemovePatches(Finders.OldPatches(3, null), ctx.Counter),
                Open = log => FileOps.OpenPath(J(SysRoot, "Installer"))
            });
            items.Add(new CleanupItem {
                Group = "updates", Admin = true,
                Name = L.T("Windows patch backup cache"), Short = L.T("Low risk"),
                Where = J(SysRoot, @"Installer\$PatchCache$"),
                Details = Lines(L.T("A backup copy Windows keeps so a single program update can be removed later without the original installer."),
                            L.T("Safe to empty. Windows only needs it to uninstall one update on its own.")),
                Paths = () => new[] { J(SysRoot, @"Installer\$PatchCache$") }
            });
            items.Add(new CleanupItem {
                Group = "updates", Admin = true, RunLast = true, Always = true, SizeText = L.T("when run"),
                Name = L.T("Windows components: normal cleanup"), Short = L.T("Safe, often frees little"),
                Where = L.F("{0} (Microsoft DISM tool)", J(SysRoot, "WinSxS")),
                Details = Lines(L.T("Microsoft's own cleanup of old versions of Windows parts. Safe."),
                            L.T("Runs in its own window and takes 5 to 30 minutes. Do not close that window or turn off the PC. Plug in the charger on a laptop."),
                            L.T("Windows already does this by itself every few weeks, so it often frees very little."),
                            L.T("Never delete anything in WinSxS by hand. It breaks Windows.")),
                Paths = () => new[] { J(SysRoot, "WinSxS") },
                Clean = (it, ctx) => { ctx.Log("  " + L.T("Running in its own window. This can take 5 to 30 minutes. Do not close that window.")); Env.RunVisibleAndWait("dism.exe /Online /Cleanup-Image /StartComponentCleanup"); }
            });
            items.Add(new CleanupItem {
                Group = "updates", Admin = true, RunLast = true, Always = true, SizeText = L.T("when run"),
                Name = L.T("Windows components: deep cleanup"), Short = L.T("Installed updates cannot be uninstalled after"),
                Where = L.F("{0} (Microsoft DISM tool, /ResetBase)", J(SysRoot, "WinSxS")),
                Details = Lines(L.T("Same as the normal cleanup, but also removes every replaced version."),
                            L.T("Afterwards, the Windows updates installed now can no longer be uninstalled. Only use it when Windows works fine."),
                            L.T("If you tick both Windows components items, only this one runs."),
                            L.T("Runs in its own window and takes 5 to 30 minutes. Do not close that window or turn off the PC.")),
                Paths = () => new[] { J(SysRoot, "WinSxS") },
                Clean = (it, ctx) => { ctx.Log("  " + L.T("Running in its own window. This can take 5 to 30 minutes. Do not close that window.")); Env.RunVisibleAndWait("dism.exe /Online /Cleanup-Image /StartComponentCleanup /ResetBase"); }
            });

            // ---------------------------------------------------------------- 3. Your call
            items.Add(new CleanupItem {
                Group = "yourcall", MustClose = new[] { "Code" },
                Name = L.T("VS Code: data of projects not opened in 60 days"), Short = L.T("Lose old projects' tabs, layout, AI chats"),
                Where = L.F("{0} (folders unused for 60+ days)", workspaceRoot),
                Details = Lines(L.T("Each folder holds what VS Code remembers about one project: open tabs, layout, and AI chat history for that project."),
                            L.T("Only folders unchanged for 60 days are deleted. Your project files themselves are never touched."), L.T("Close VS Code first.")),
                Paths = () => new[] { workspaceRoot },
                Size = (it, cancel) => Finders.OldWorkspaces(workspaceRoot, 60, cancel).Sum(w => w.Bytes),
                Clean = (it, ctx) => { foreach (WorkspaceFolder w in Finders.OldWorkspaces(workspaceRoot, 60, null)) FileOps.RemoveFolder(w.Path, ctx.Counter); }
            });
            items.Add(new CleanupItem {
                Group = "yourcall", MustClose = new[] { "Cursor" },
                Name = L.T("Cursor: AI agent snapshots"), Short = L.T("Cannot undo older AI edits"), Where = J(UR, @"Cursor\snapshots"),
                Details = Lines(L.T("Copies of your projects that Cursor's AI agent uses to undo its edits."),
                            L.T("You can no longer roll back older AI changes. Cursor makes new copies when you use the agent again."), L.T("Close Cursor first.")),
                Paths = () => new[] { J(UR, @"Cursor\snapshots") }
            });
            items.Add(new CleanupItem {
                Group = "yourcall", MustClose = new[] { "Cursor" },
                Name = L.T("Cursor: AI agent data"), Short = L.T("May hold old agent chat history"),
                Where = J(UR, @"Cursor\User\globalStorage\anysphere.cursor-agent-worker"),
                Details = Lines(L.T("Working data of Cursor's AI agent. It may include old agent chat history."),
                            L.T("state.vscdb in the same folder holds your chats, login and settings. This tool never touches it."), L.T("Close Cursor first.")),
                Paths = () => new[] { J(UR, @"Cursor\User\globalStorage\anysphere.cursor-agent-worker") }
            });
            items.Add(new CleanupItem {
                Group = "yourcall", MustClose = new[] { "Spotify" },
                Name = L.T("Spotify cache and downloads"), Short = L.T("Downloaded songs must be downloaded again"),
                Where = L.F("{0} and Data", J(UL, @"Spotify\Storage")),
                Details = Lines(L.T("Spotify keeps played and downloaded music here."),
                            L.T("Your account and playlists are safe, but songs you downloaded for offline listening must be downloaded again."),
                            L.T("Tip: in Spotify settings you can also set a smaller cache size or move it to another drive."), L.T("Close Spotify first.")),
                Paths = () => new[] { J(UL, @"Spotify\Storage"), J(UL, @"Spotify\Data") }
            });
            items.Add(new CleanupItem {
                Group = "yourcall",
                Name = L.T("Adobe media cache (Premiere, After Effects)"), Short = L.T("Video previews are rebuilt"),
                Where = J(UR, @"Adobe\Common") + @"\Media Cache Files, Media Cache, Peak Files",
                Details = Lines(L.T("Preview and audio files Adobe video apps create. They can grow to many GB."),
                            L.T("Your projects and videos are not touched. Opening a project the first time afterwards is slower while previews are rebuilt."),
                            L.T("Close Adobe apps first.")),
                Paths = () => new[] { J(UR, @"Adobe\Common\Media Cache Files"), J(UR, @"Adobe\Common\Media Cache"), J(UR, @"Adobe\Common\Peak Files") }
            });
            items.Add(new CleanupItem {
                Group = "yourcall", Name = L.T("Gradle caches"), Short = L.T("Next build downloads everything again"), Where = J(UP, @".gradle\caches"),
                Details = Lines(L.T("Downloaded libraries and build files for Java and Android projects."),
                            L.T("The next build needs internet and takes longer while Gradle downloads them again. Close Android Studio and other Java IDEs first.")),
                Paths = () => new[] { J(UP, @".gradle\caches") }
            });
            items.Add(new CleanupItem {
                Group = "yourcall", Name = L.T("Maven repository"), Short = L.T("Next build downloads everything again"), Where = J(UP, @".m2\repository"),
                Details = Lines(L.T("Downloaded Java libraries."),
                            L.T("The next build needs internet and takes longer while Maven downloads them again. Libraries you installed yourself with \"mvn install\" would need installing again.")),
                Paths = () => new[] { J(UP, @".m2\repository") }
            });
            items.Add(new CleanupItem {
                Group = "yourcall", Name = L.T("JetBrains IDE caches and logs"), Short = L.T("IDE re-indexes your projects"),
                Where = J(UL, "JetBrains") + @"\(each IDE)\caches and log",
                Details = Lines(L.T("Caches of IntelliJ IDEA, PyCharm, WebStorm, Rider and other JetBrains IDEs."),
                            L.T("Your settings and projects stay. The IDE re-indexes projects the next time you open them. Close JetBrains IDEs first.")),
                Paths = () => Finders.JetBrainsCacheDirs()
            });
            items.Add(new CleanupItem {
                Group = "yourcall", Admin = true,
                Name = L.T("Crash dump files"), Short = L.T("Only needed to investigate crashes"),
                Where = J(SysRoot, "MEMORY.DMP") + ", Minidump, LiveKernelReports",
                Details = Lines(L.T("Files Windows writes after a blue screen or a graphics driver problem. MEMORY.DMP alone can be several GB."),
                            L.T("Keep them if someone is investigating why your PC crashes. Otherwise they are safe to delete.")),
                Paths = () => new[] { J(SysRoot, "MEMORY.DMP"), J(SysRoot, "Minidump"), J(SysRoot, "LiveKernelReports") }
            });
            items.Add(new CleanupItem {
                Group = "yourcall", Always = true,
                Name = L.T("Recycle Bin (all drives)"), Short = L.T("Files in it are gone for good"),
                Where = L.T("$Recycle.Bin on every drive"),
                Details = Lines(L.T("Empties the Recycle Bin on every drive. Anything in it can no longer be restored."),
                            L.T("The size shown is the Recycle Bin on the Windows drive.")),
                Paths = () => new[] { J(Root, "$Recycle.Bin") },
                Clean = (it, ctx) => Env.EmptyRecycleBin(),
                Open = log => Env.OpenUri("shell:RecycleBinFolder")
            });

            // ---------------------------------------------------------------- 4. Windows settings
            items.Add(new CleanupItem {
                Group = "settings", Admin = true,
                Name = L.T("Turn off hibernation"), Short = L.T("PC starts a few seconds slower"),
                Where = J(Root, "hiberfil.sys"),
                Details = Lines(L.T("Removes the hibernation file, which is often several GB. Hibernate and Fast Startup stop working, so the PC starts a few seconds slower."),
                            L.T("Sleep still works normally."),
                            L.T("To undo later, run this in an administrator Command Prompt:  powercfg /h on")),
                ShowIf = () => Finders.HiberBytes() > 0,
                Size = (it, cancel) => Finders.HiberBytes(),
                Clean = (it, ctx) => LogLines(ctx, Env.RunHidden(Env.SysTool("powercfg.exe"), "/h off")),
                Open = log => log(L.T("hiberfil.sys is a hidden Windows file. There is nothing to open."))
            });
            items.Add(new CleanupItem {
                Group = "settings", Admin = true, SizeText = L.T("about 7 GB"),
                Name = L.T("Turn off Reserved Storage"), Short = L.T("Updates may fail when the disk is full"),
                Where = L.T("Windows setting (Reserved Storage)"),
                Details = Lines(L.T("Windows keeps about 7 GB aside so updates always have room to install."),
                            L.T("Turning it off gives that space back, but if your disk gets full, Windows updates can fail. Only do this if you keep enough free space."),
                            L.T("To undo later, run this in an administrator Command Prompt:  DISM /Online /Set-ReservedStorageState /State:Enabled")),
                ShowIf = () => Env.IsAdmin && Env.WindowsBuild >= 19041,
                Size = (it, cancel) =>
                {
                    string o = Env.RunHidden(Env.SysTool("dism.exe"), "/Online /Get-ReservedStorageState /English");
                    if (o.IndexOf("is disabled", StringComparison.OrdinalIgnoreCase) >= 0) it.SizeText = L.T("already off");
                    else if (o.IndexOf("is enabled", StringComparison.OrdinalIgnoreCase) >= 0) it.SizeText = L.T("about 7 GB");
                    return null;
                },
                Clean = (it, ctx) => LogLines(ctx, Env.RunHidden(Env.SysTool("dism.exe"), "/Online /Set-ReservedStorageState /State:Disabled /English"))
            });
            items.Add(new CleanupItem {
                Group = "settings", Admin = true, RunLast = true, Always = true, SizeText = L.T("about 2-4 GB"),
                Name = L.T("Compress Windows files (CompactOS)"), Short = L.T("Built into Windows. Slight CPU cost"),
                Where = L.F("Windows system files on {0}", SysDrive),
                Details = Lines(L.T("Microsoft's own feature that stores Windows system files compressed. It usually frees 2 to 4 GB."),
                            L.T("On very slow or old PCs it can make Windows a little slower. On most PCs you will not notice."),
                            L.T("Runs in its own window and can take 10 to 20 minutes. If Windows is already compressed, it finishes quickly."),
                            L.T("To undo later, run this in an administrator Command Prompt:  compact.exe /compactos:never")),
                Clean = (it, ctx) => { ctx.Log("  " + L.T("Running in its own window. This can take 10 to 20 minutes. Do not close that window.")); Env.RunVisibleAndWait("compact.exe /compactos:always"); }
            });
            items.Add(new CleanupItem {
                Group = "settings", Manual = true, Always = true, SizeText = "-",
                Name = L.T("Turn on Storage Sense"), Short = L.T("Cleans temp files automatically"), Where = L.T("Settings > System > Storage"),
                Details = Lines(L.T("Storage Sense is built into Windows. It deletes temporary files and old Recycle Bin items automatically, so space stays free."),
                            L.T("\"Open location\" opens the Storage Sense settings. Turn it on there.")),
                Open = log => Env.OpenUri("ms-settings:storagesense")
            });

            // ---------------------------------------------------------------- 5. Do it yourself
            items.Add(new CleanupItem {
                Group = "manual", Manual = true,
                Name = L.T("Previous Windows installation"), Short = L.T("Remove with Windows' own tool"),
                Where = J(Root, "Windows.old") + ", $Windows.~BT, $Windows.~WS",
                Details = Lines(L.T("Left over from a Windows upgrade. It lets you go back to the previous version for a limited time, and is often 10 to 30 GB."),
                            L.T("Remove it with Windows itself: Settings > System > Storage > Temporary files > tick \"Previous Windows installation(s)\" > Remove files."),
                            L.T("After that you can no longer go back to the previous Windows version."), L.T("\"Open location\" opens the Storage settings.")),
                Paths = () => new[] { J(Root, "Windows.old"), J(Root, "$Windows.~BT"), J(Root, "$Windows.~WS") },
                Open = log => Env.OpenUri("ms-settings:storagesense")
            });
            items.Add(new CleanupItem {
                Group = "manual", Manual = true, Always = true, SizeText = L.T("see details"),
                Name = L.T("Biggest installed programs"), Short = L.T("Uninstall the ones you do not use"), Where = L.T("Settings > Apps > Apps & features"),
                Details = Lines(L.T("Programs only come off by uninstalling them in Settings > Apps. Click \"Measure sizes\" to list the biggest ones here."),
                            L.T("\"Open location\" opens Apps & features.")),
                Size = (it, cancel) =>
                {
                    List<ProgramInfo> progs = Finders.InstalledPrograms();
                    string sys = SysDrive.ToUpperInvariant();
                    List<string> d = Lines(L.T("Programs only come off by uninstalling them in Settings > Apps."), L.T("\"Open location\" opens Apps & features."), "",
                                       L.F("Biggest programs on {0} (Windows' own size estimate; \"?\" = install folder not recorded):", SysDrive));
                    foreach (ProgramInfo p in progs.Where(x => x.Bytes > 0 && (x.Drive == sys || x.Drive == "?")).OrderByDescending(x => x.Bytes).Take(15))
                        d.Add(string.Format("  {0,10}  {1,-3} {2}", Fmt.Size(p.Bytes), p.Drive, p.Name));
                    int elsewhere = progs.Count(x => x.Bytes > 0 && x.Drive != sys && x.Drive != "?");
                    if (elsewhere > 0) { d.Add(""); d.Add(L.F("{0} programs installed on other drives are not listed: removing them does not free space on {1}.", elsewhere, SysDrive)); }
                    int java = progs.Count(x => System.Text.RegularExpressions.Regex.IsMatch(x.Name, @"^Java(\(TM\))? \d|^Java SE Development Kit|JDK|OpenJDK|Temurin"));
                    if (java > 1) { d.Add(""); d.Add(L.F("You have {0} Java versions installed. Most people only need the newest one.", java)); }
                    foreach (ProgramInfo p in progs.Where(x => System.Text.RegularExpressions.Regex.IsMatch(x.Name, "Silverlight|Flash Player|QuickTime")))
                        d.Add(L.F("\"{0}\" is discontinued and no longer needed.", p.Name));
                    it.Details = d;
                    return null;
                },
                Open = log => Env.OpenUri("ms-settings:appsfeatures")
            });
            items.Add(new CleanupItem {
                Group = "manual", Manual = true, Admin = true, Always = true,
                Name = L.T("System Restore points"), Short = L.T("Keep at least the newest one"), Where = L.T("System Properties > System Protection"),
                Details = Lines(L.T("Restore points let you undo system changes. Windows can use several GB for them."),
                            L.T("To make room: \"Open location\" > pick the drive > Configure > lower the \"Max Usage\" slider, or click Delete to remove all of them."),
                            L.T("To keep only the newest one: Disk Cleanup (cleanmgr) > Clean up system files > More Options > System Restore and Shadow Copies > Clean up."),
                            L.T("Keep at least one recent restore point if you can.")),
                Size = (it, cancel) => Finders.RestorePointBytes(),
                Open = log => { try { System.Diagnostics.Process.Start(Env.SysTool("SystemPropertiesProtection.exe")); } catch { } }
            });
            items.Add(new CleanupItem {
                Group = "manual", Manual = true,
                Name = L.T("Docker disk (do not delete)"), Short = L.T("Can be moved to another drive"), Where = J(UL, @"Docker\wsl") + @"\...",
                Details = Lines(L.T("Holds all Docker containers, images and volumes. Never delete this file."),
                            L.T("Be careful with \"docker volume prune\" and \"docker system prune --volumes\": they delete volumes, which may hold databases."),
                            L.T("To free space on this drive, move it: Docker Desktop > Settings (gear) > Resources > Advanced > Disk image location > pick a folder on another drive > Apply & restart."),
                            L.T("On a slower hard disk, Docker runs slower.")),
                Paths = () => new[] { J(UL, @"Docker\wsl\disk\docker_data.vhdx"), J(UL, @"Docker\wsl\data\ext4.vhdx") }
            });
            items.Add(new CleanupItem {
                Group = "manual", Manual = true,
                Name = L.T("Linux (WSL) disks"), Short = L.T("Uninstall the ones you do not use"), Where = L.T("Disk files of your WSL Linux systems"),
                Details = Lines(L.T("Each WSL Linux system (for example Ubuntu) keeps all its files in one disk file."),
                            L.T("If you do not use one: Settings > Apps > find it > Uninstall. That deletes everything inside it."),
                            L.T("To move one to another drive, in a Command Prompt:  wsl --shutdown   then   wsl --manage NAME --move D:\\WSL\\NAME"),
                            L.T("If that command is not recognised, run  wsl --update  first.")),
                ShowIf = () => Finders.WslDisks().Count > 0,
                Size = (it, cancel) =>
                {
                    it.Details = new List<string>(it.BaseDetails);
                    it.Details.Add("");
                    it.Details.Add(L.T("Found on this PC:"));
                    long t = 0;
                    foreach (WslDisk w in Finders.WslDisks())
                    {
                        long s = FileOps.SizeOf(w.Path, cancel);
                        t += s;
                        it.Details.Add(string.Format("  {0,10}   {1}", Fmt.Size(s), w.Name));
                    }
                    return t;
                },
                Open = log => Env.OpenUri("ms-settings:appsfeatures")
            });
            items.Add(new CleanupItem {
                Group = "manual", Manual = true,
                Name = L.T("Your Downloads folder"), Short = L.T("Your own files: you decide"), Where = downloads,
                Details = Lines(L.T("Files you downloaded. This tool never deletes them here."),
                            L.T("Open the folder, sort by size (View > Details, then click the Size column), and delete what you no longer need."),
                            L.T("Tip: the Analyze disk tab lists your biggest files anywhere.")),
                Paths = () => new[] { downloads }
            });
            items.Add(new CleanupItem {
                Group = "manual", Manual = true, Always = true, SizeText = "-",
                Name = L.T("Leave these alone"), Short = L.T("Never delete by hand"),
                Where = L.T("pagefile.sys, swapfile.sys, the Windows Installer and WinSxS folders"),
                Details = Lines(L.T("- pagefile.sys and swapfile.sys: Windows' extra memory on disk. Without enough of it, programs crash when memory runs out."),
                            L.T("- The Windows Installer folder: only remove files with the \"Old update files\" item, never by hand."),
                            L.T("- The WinSxS folder: only clean it with the \"Windows components\" items, never by hand."),
                            L.T("- Folders of apps you use, even if they are big. Deleting them can break the app or lose your data."))
            });

            foreach (CleanupItem it in items) it.BaseDetails = new List<string>(it.Details);
            return items;
        }
    }
}
