// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Self-update: asks GitHub for the newest release, downloads and checks its program file, and swaps it in for the
// running exe (a running exe cannot be overwritten, but it can be renamed). No UI here; the window calls these on a
// background thread and shows the results. How it works: docs/DEVELOPING.md, section "Updates".
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Install;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.App
{
    public class UpdateInfo
    {
        public Version Version;        // major.minor
        public string Notes = "";      // the release text without its SHA256 line ("What's new")
        public string ExeUrl, Sha256, PageUrl;
        public string LocalPath;       // the downloaded and checked program file
    }

    public static class Updater
    {
        public const string Repo = "philip-droubi/disk-cleanup-helper";
        public const string ReleasesPage = "https://github.com/" + Repo + "/releases/latest";
        const string ApiUrl = "https://api.github.com/repos/" + Repo + "/releases/latest";
        const string ExeAsset = "DiskCleanupHelper.exe";
        const string NotesAsset = "release-notes.md";   // the release text as a file, for CheckWithoutApi
        const long MinBytes = 100 * 1024, MaxBytes = 20L * 1024 * 1024;
        static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(5);
        static readonly Regex ShaLine = new Regex(@"^[ \t]*SHA256:[ \t]*([0-9A-Fa-f]{64})[ \t]*$", RegexOptions.Multiline);

        // Test switch /updatefeed: a local file (or web address) shaped like GitHub's answer, whose download addresses
        // may be file://.
        public static string FeedFile;
        static bool FeedIsWeb
        {
            get { return FeedFile != null && (FeedFile.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || FeedFile.StartsWith("https://", StringComparison.OrdinalIgnoreCase)); }
        }

        static Updater()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        // This copy's version as major.minor (1.0, 1.1 ...).
        public static Version Current
        {
            get { Version v = Assembly.GetExecutingAssembly().GetName().Version; return new Version(v.Major, v.Minor); }
        }

        public static string Folder { get { return Path.Combine(AppPaths.DataDir, "update"); } }
        // What was downloaded (version, SHA256, "What's new"), so a later start can check the file again before using it.
        static string StateFile { get { return Path.Combine(Folder, "update.txt"); } }
        static string UserAgent { get { return "DiskCleanupHelper/" + Current; } }

        // The running exe while an update is being installed: kept until the new version has started.
        public static string OldPath { get { return SiblingOfExe(".old.exe"); } }
        // The new version copied next to the running exe and checked there, before the two are swapped.
        static string NewPath { get { return SiblingOfExe(".new.exe"); } }
        static string SiblingOfExe(string suffix)
        {
            return Path.Combine(AppPaths.ExeDir, Path.GetFileNameWithoutExtension(AppPaths.ExePath) + suffix);
        }

        // The developer's own build (builds\v1 or builds\v1.2 in the project, next to build.ps1 and src) never updates
        // itself, so a build output is never overwritten by a download.
        public static bool IsDeveloperBuild
        {
            get
            {
                try
                {
                    DirectoryInfo version = new DirectoryInfo(AppPaths.ExeDir), builds = version.Parent;
                    if (builds == null || builds.Parent == null) return false;
                    string root = builds.Parent.FullName;
                    return Regex.IsMatch(version.Name, @"^v\d+(\.\d+)?$") && string.Equals(builds.Name, "builds", StringComparison.OrdinalIgnoreCase)
                        && File.Exists(Path.Combine(root, "build.ps1")) && Directory.Exists(Path.Combine(root, "src"));
                }
                catch { return false; }
            }
        }

        // ------------------------------------------------------------ check
        // The newest release when it is newer than this copy, else null. Throws WebException when GitHub cannot be
        // reached, and InvalidDataException (with the reason) when the release cannot be used.
        public static UpdateInfo Check()
        {
            string json;
            if (FeedIsWeb) json = Get(FeedFile, false);   // testing, e.g. an unreachable address for "no internet"
            else if (FeedFile != null) json = File.ReadAllText(FeedFile);
            else
            {
                try { json = Get(ApiUrl, true); }
                catch (WebException ex)
                {
                    HttpWebResponse r = ex.Response as HttpWebResponse;
                    int status = r != null ? (int)r.StatusCode : 0;
                    if (ex.Response != null) ex.Response.Close();   // frees the connection (only 2 per host by default)
                    if (status == 404) return null;   // nothing published yet
                    if (status == 403 || status == 429) return CheckWithoutApi();
                    throw;
                }
            }
            return Parse(json, Current, FeedFile != null);
        }

        // Without a login, GitHub's API answers 60 requests an hour per internet address, which people sharing an
        // address (a VPN, a mobile network) can use up. The release pages have no such limit: "releases/latest"
        // redirects to the newest tag, and the release text is also published as a file (NotesAsset).
        static UpdateInfo CheckWithoutApi()
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(ReleasesPage);
            req.UserAgent = UserAgent;
            req.Timeout = 15000;
            req.ReadWriteTimeout = 15000;
            req.AllowAutoRedirect = false;
            string location;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse()) location = resp.Headers["Location"];
            const string marker = "/releases/tag/";
            int at = location == null ? -1 : location.IndexOf(marker, StringComparison.Ordinal);
            if (at < 0) return null;   // no release yet: GitHub redirects to the list of releases
            string tag = Uri.UnescapeDataString(location.Substring(at + marker.Length).TrimEnd('/'));
            Version v = TagVersion(tag);
            if (v != null && v <= Current) return null;
            string download = "https://github.com/" + Repo + "/releases/download/" + Uri.EscapeDataString(tag) + "/";
            string body = Get(download + NotesAsset, false);
            return Build(tag, body, download + ExeAsset, location, Current, false);
        }

        static string Get(string url, bool api)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = UserAgent;
            if (api) req.Accept = "application/vnd.github+json";
            req.Timeout = 15000;
            req.ReadWriteTimeout = 15000;
            using (WebResponse resp = req.GetResponse())
            using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }

        // Rules: the tag is a higher version, there is an exe file on github.com, and the text has a SHA256 line.
        public static UpdateInfo Parse(string json, Version current, bool localFeed)
        {
            Dictionary<string, object> rel = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
            if (rel == null) throw new InvalidDataException(L.T("GitHub's answer could not be read."));
            string exeUrl = null;
            object[] assets = rel.ContainsKey("assets") ? rel["assets"] as object[] : null;
            if (assets != null)
            {
                foreach (object o in assets)
                {
                    Dictionary<string, object> a = o as Dictionary<string, object>;
                    if (a != null && string.Equals(Str(a, "name"), ExeAsset, StringComparison.OrdinalIgnoreCase)) exeUrl = Str(a, "browser_download_url");
                }
            }
            return Build(Str(rel, "tag_name"), Str(rel, "body"), exeUrl, Str(rel, "html_url"), current, localFeed);
        }

        static UpdateInfo Build(string tag, string body, string exeUrl, string page, Version current, bool localFeed)
        {
            Version v = TagVersion(tag);
            if (v == null) throw new InvalidDataException(L.F("The newest release has an unreadable version number: {0}", tag ?? "-"));
            if (v <= current) return null;

            UpdateInfo info = new UpdateInfo();
            info.Version = v;
            body = (body ?? "").Replace("\r\n", "\n");
            Match m = ShaLine.Match(body);
            if (!m.Success) throw new InvalidDataException(L.F("Version {0} has no SHA256 line in its release text.", v));
            info.Sha256 = m.Groups[1].Value.ToUpperInvariant();
            info.Notes = ShaLine.Replace(body, "").Trim();
            info.ExeUrl = exeUrl;
            if (string.IsNullOrEmpty(info.ExeUrl)) throw new InvalidDataException(L.F("Version {0} has no {1} file.", v, ExeAsset));
            if (!AllowedDownload(info.ExeUrl, localFeed)) throw new InvalidDataException(L.F("Version {0} has a download address that is not on github.com.", v));
            Uri pu;
            info.PageUrl = page != null && Uri.TryCreate(page, UriKind.Absolute, out pu) && pu.Scheme == Uri.UriSchemeHttps && IsHost(pu, "github.com")
                ? page : ReleasesPage;
            return info;
        }

        static string Str(Dictionary<string, object> d, string key)
        {
            object o;
            return d.TryGetValue(key, out o) && o != null ? o.ToString() : null;
        }

        // "v1.2" -> 1.2; null when it is not a version.
        static Version TagVersion(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;
            string t = tag.Trim().TrimStart('v', 'V');
            if (t.IndexOf('.') < 0) t += ".0";
            Version v;
            return Version.TryParse(t, out v) ? new Version(v.Major, v.Minor) : null;
        }

        static bool IsHost(Uri u, string host) { return string.Equals(u.Host, host, StringComparison.OrdinalIgnoreCase); }

        // The download address: https on github.com (a local file only with the /updatefeed test switch).
        static bool AllowedDownload(string url, bool localFeed)
        {
            Uri u;
            if (!Uri.TryCreate(url, UriKind.Absolute, out u)) return false;
            if (u.IsFile) return localFeed;
            return u.Scheme == Uri.UriSchemeHttps && IsHost(u, "github.com");
        }

        // GitHub redirects downloads to its file host, which has changed before (objects.githubusercontent.com, now
        // release-assets.githubusercontent.com). The SHA256 check is what actually protects the download.
        static bool AllowedFileHost(Uri u)
        {
            if (u.Scheme != Uri.UriSchemeHttps) return false;
            string h = u.Host.ToLowerInvariant();
            return h == "github.com" || h.EndsWith(".githubusercontent.com", StringComparison.Ordinal);
        }

        // ------------------------------------------------------------ download
        // Downloads and checks the new program file into the update folder and sets info.LocalPath. Throws with the
        // reason when a check fails; the partial file is then deleted.
        public static void Download(UpdateInfo info, Action<int> progress, CancelFlag cancel)
        {
            Directory.CreateDirectory(Folder);
            string final = Path.Combine(Folder, "DiskCleanupHelper-" + info.Version + ".exe");
            if (!File.Exists(final) || Verify(final, info) != null)
            {
                string part = final + ".part";
                try
                {
                    Uri u = new Uri(info.ExeUrl);
                    if (u.IsFile)
                    {
                        if (FeedFile == null) throw new InvalidDataException(L.F("Version {0} has a download address that is not on github.com.", info.Version));
                        File.Copy(u.LocalPath, part, true);
                    }
                    else Fetch(u, part, progress, cancel);
                    string problem = Verify(part, info);
                    if (problem != null) throw new InvalidDataException(problem);
                    if (File.Exists(final)) File.Delete(final);
                    File.Move(part, final);
                }
                catch
                {
                    TryDelete(part);
                    throw;
                }
            }
            try { File.WriteAllText(StateFile, info.Version + "\n" + info.Sha256 + "\n" + info.Notes, Encoding.UTF8); } catch { }
            info.LocalPath = final;
        }

        // The saved release of version v (see StateFile), or null.
        static UpdateInfo SavedRelease(Version v)
        {
            try
            {
                if (!File.Exists(StateFile)) return null;
                string[] parts = File.ReadAllText(StateFile, Encoding.UTF8).Split(new[] { '\n' }, 3);
                if (parts.Length < 2 || parts[0].Trim() != v.ToString()) return null;
                UpdateInfo saved = new UpdateInfo();
                saved.Version = v;
                saved.Sha256 = parts[1].Trim();
                saved.Notes = parts.Length > 2 ? parts[2].Trim() : "";
                saved.PageUrl = ReleasesPage;
                return saved;
            }
            catch { return null; }
        }

        static void Fetch(Uri url, string path, Action<int> progress, CancelFlag cancel)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = UserAgent;
            req.Timeout = 30000;
            req.ReadWriteTimeout = 60000;
            req.AllowAutoRedirect = true;
            Stopwatch clock = Stopwatch.StartNew();
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                if (!AllowedFileHost(resp.ResponseUri)) throw new InvalidDataException(L.F("The download came from an unexpected address: {0}", resp.ResponseUri.Host));
                long length = resp.ContentLength;
                if (length > MaxBytes) throw new InvalidDataException(L.F("The downloaded file has an unexpected size ({0}).", Fmt.Size(length)));
                using (Stream s = resp.GetResponseStream())
                using (FileStream fs = File.Create(path))
                {
                    byte[] buffer = new byte[81920];
                    long got = 0;
                    int n, lastPct = -1;
                    while ((n = s.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (cancel != null && cancel.IsSet) throw new OperationCanceledException();
                        if (clock.Elapsed > DownloadTimeout) throw new TimeoutException(L.T("The download took too long."));
                        got += n;
                        if (got > MaxBytes) throw new InvalidDataException(L.F("The downloaded file has an unexpected size ({0}).", Fmt.Size(got)));
                        fs.Write(buffer, 0, n);
                        if (length > 0 && progress != null)
                        {
                            int pct = (int)(100 * got / length);
                            if (pct != lastPct) { lastPct = pct; progress(pct); }
                        }
                    }
                }
            }
        }

        // Null when the file is this release's program file, otherwise why not.
        static string Verify(string path, UpdateInfo info)
        {
            long size = new FileInfo(path).Length;
            if (size < MinBytes || size > MaxBytes) return L.F("The downloaded file has an unexpected size ({0}).", Fmt.Size(size));
            if (!string.Equals(Sha256Of(path), info.Sha256, StringComparison.OrdinalIgnoreCase))
                return L.T("The downloaded file does not match the SHA256 in the release text.");
            Version v = FileVersion(path);
            if (v == null) return L.T("The downloaded file is not a Disk Cleanup Helper program.");
            if (v != info.Version) return L.F("The downloaded file is version {0}, not {1}.", v, info.Version);
            return null;
        }

        static string Sha256Of(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream fs = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
            }
        }

        static Version FileVersion(string path)
        {
            try { Version a = AssemblyName.GetAssemblyName(path).Version; return new Version(a.Major, a.Minor); }
            catch { return null; }
        }

        // A program file downloaded earlier but not installed yet (the app was closed first), or null. It is checked again
        // against the saved release (SHA256 and version); any other file in the update folder is deleted.
        public static UpdateInfo Pending()
        {
            try
            {
                if (!Directory.Exists(Folder)) return null;
                UpdateInfo best = null;
                foreach (string f in Directory.GetFiles(Folder, "DiskCleanupHelper-*.exe"))
                {
                    Version v = FileVersion(f);
                    UpdateInfo saved = v != null && v > Current ? SavedRelease(v) : null;
                    if (saved == null || Verify(f, saved) != null) { TryDelete(f); continue; }
                    if (best != null && v <= best.Version) continue;
                    saved.LocalPath = f;
                    best = saved;
                }
                return best;
            }
            catch { return null; }
        }

        // Forgets a downloaded update, for example when the release it came from was withdrawn on GitHub.
        public static void DiscardDownloads()
        {
            try
            {
                if (Directory.Exists(Folder)) foreach (string f in Directory.GetFiles(Folder)) TryDelete(f);
            }
            catch { }
        }

        // ------------------------------------------------------------ install (the swap)
        // Whether the running exe's folder can be changed (it cannot on a read-only USB stick, for example).
        public static bool CanReplaceRunningExe()
        {
            try
            {
                string probe = Path.Combine(AppPaths.ExeDir, "update-test-" + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        // Puts the downloaded version in place of the running exe, which is renamed to .old.exe until the next start.
        // The new file is first copied next to the exe and checked there, so the bytes that run are the bytes that were
        // checked, and the swap itself is two renames. Throws when it fails, with the running exe back in place.
        public static void Install(UpdateInfo info)
        {
            string target = AppPaths.ExePath, old = OldPath, fresh = NewPath;
            DeleteFile(fresh);
            File.Copy(info.LocalPath, fresh);
            string problem = Verify(fresh, info);
            if (problem != null) { TryDelete(fresh); throw new InvalidDataException(problem); }
            DeleteFile(old);
            File.Move(target, old);
            try { File.Move(fresh, target); }
            catch
            {
                try { File.Move(old, target); } catch { }
                TryDelete(fresh);
                throw;
            }
        }

        // Puts the running version back when the new one could not be started.
        public static void Rollback()
        {
            string target = AppPaths.ExePath, old = OldPath, fresh = NewPath;
            if (!File.Exists(old)) return;
            DeleteFile(fresh);
            File.Move(target, fresh);
            File.Move(old, target);
            TryDelete(fresh);
        }

        // Also when the file is read-only (copied from a CD or a read-only share, for example).
        static void DeleteFile(string path)
        {
            if (!File.Exists(path)) return;
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }

        // Starts the new version with the same rights as this one, so there is no new Windows prompt, and with the
        // same switches (none in normal use).
        public static void StartNewVersion()
        {
            List<string> args = new List<string>();
            string[] current = Environment.GetCommandLineArgs();
            for (int i = 1; i < current.Length; i++)
            {
                string a = current[i].ToLowerInvariant();
                if (a == "/updated") { i++; continue; }
                if (a == "/noelevate") continue;   // added again below when needed
                args.Add(Env.QuoteArg(current[i]));
            }
            args.Add("/updated " + Current);
            if (!Env.IsAdmin) args.Add("/noelevate");
            ProcessStartInfo psi = new ProcessStartInfo(AppPaths.ExePath, string.Join(" ", args));
            psi.UseShellExecute = false;
            psi.WorkingDirectory = AppPaths.ExeDir;
            Process.Start(psi);
        }

        // At every start: removes a leftover old program file quietly. Returns true on the first start after an update:
        // the /updated switch (Restart now), or the saved release is this version (installed when the app closed). Then
        // it empties the update folder, updates the version shown in Settings > Apps, and notes is this version's
        // "What's new" text (may be empty).
        public static bool FinishUpdate(bool updatedSwitch, out string notes)
        {
            notes = null;
            string old = OldPath, fresh = NewPath;
            if (File.Exists(old) || File.Exists(fresh))
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    // the old app may still be closing (slowly on a busy PC), so keep trying for half a minute
                    for (int i = 0; i < 60 && (File.Exists(old) || File.Exists(fresh)); i++)
                    {
                        try { DeleteFile(old); DeleteFile(fresh); } catch { Thread.Sleep(500); }
                    }
                });
            }
            UpdateInfo saved = SavedRelease(Current);
            if (!updatedSwitch && saved == null) return false;
            notes = saved != null ? saved.Notes : "";
            DiscardDownloads();
            Installer.UpdateRegisteredVersion();
            return true;
        }

        static void TryDelete(string path)
        {
            try { DeleteFile(path); } catch { }
        }
    }
}
