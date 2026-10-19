// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Finds what the cleanup items work on: caches, old update files, installed programs, WSL disks and more.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.Cleanup
{
    public class PatchFile
    {
        public string Product, Name, File;
        public int State, Installed;
        public long Bytes;
    }

    public class ProgramInfo
    {
        public string Name, Drive;
        public double Bytes;
    }

    public class WslDisk
    {
        public string Name, Path;
    }

    public class WorkspaceFolder
    {
        public string Path;
        public long Bytes;
    }

    public static class Finders
    {
        static readonly Regex ProfileName = new Regex(@"^(Default|Profile \d+|Guest Profile|System Profile)$", RegexOptions.IgnoreCase);

        static int ToInt(object v)
        {
            if (v == null) return 0;
            if (v is int) return (int)v;
            int r;
            return int.TryParse(v.ToString(), out r) ? r : 0;
        }

        static RegistryKey LocalMachine64()
        {
            return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Default);
        }

        static string[] SafeDirs(string path)
        {
            try { return Directory.GetDirectories(path); } catch { return new string[0]; }
        }

        // Cache folders of a Chromium-based browser or app (Chrome, Edge, Brave, Vivaldi, new Teams).
        public static List<string> ChromiumCacheDirs(string userData)
        {
            List<string> outList = new List<string>();
            if (!Directory.Exists(userData)) return outList;
            foreach (string pf in SafeDirs(userData))
            {
                if (!ProfileName.IsMatch(Path.GetFileName(pf))) continue;
                foreach (string s in new[] { "Cache", "Code Cache", "GPUCache", @"Service Worker\CacheStorage", "DawnGraphiteCache", "DawnWebGPUCache" })
                {
                    string p = Path.Combine(pf, s);
                    if (Directory.Exists(p)) outList.Add(p);
                }
            }
            foreach (string s in new[] { "GrShaderCache", "ShaderCache", "GraphiteDawnCache", "DawnGraphiteCache", "DawnWebGPUCache" })
            {
                string p = Path.Combine(userData, s);
                if (Directory.Exists(p)) outList.Add(p);
            }
            return outList;
        }

        // Cache folders of an Electron app such as Discord or Slack.
        public static List<string> AppCacheDirs(string root)
        {
            List<string> outList = new List<string>();
            foreach (string s in new[] { "Cache", "Code Cache", "GPUCache", @"Service Worker\CacheStorage", "DawnCache", "DawnGraphiteCache", "DawnWebGPUCache", "Crashpad" })
            {
                string p = Path.Combine(root, s);
                if (Directory.Exists(p)) outList.Add(p);
            }
            return outList;
        }

        public static List<string> FirefoxCacheDirs()
        {
            List<string> outList = new List<string>();
            string root = Path.Combine(AppPaths.LocalAppData, @"Mozilla\Firefox\Profiles");
            if (!Directory.Exists(root)) return outList;
            foreach (string pf in SafeDirs(root))
            {
                string p = Path.Combine(pf, "cache2");
                if (Directory.Exists(p)) outList.Add(p);
            }
            return outList;
        }

        public static List<string> JetBrainsCacheDirs()
        {
            List<string> outList = new List<string>();
            string root = Path.Combine(AppPaths.LocalAppData, "JetBrains");
            if (!Directory.Exists(root)) return outList;
            foreach (string d in SafeDirs(root))
            {
                foreach (string s in new[] { "caches", "log" })
                {
                    string p = Path.Combine(d, s);
                    if (Directory.Exists(p)) outList.Add(p);
                }
            }
            return outList;
        }

        // Old installer update files that Windows marks as "replaced by a newer update".
        // Keeps the newest 'keep' per program part and never touches a file still in use.
        public static List<PatchFile> OldPatches(int keep, CancelFlag cancel)
        {
            List<PatchFile> result = new List<PatchFile>();
            string installerDir = Path.Combine(AppPaths.WindowsDir, "Installer") + "\\";
            using (RegistryKey lm = LocalMachine64())
            using (RegistryKey baseKey = lm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18"))
            {
                if (baseKey == null) return result;
                using (RegistryKey patchesKey = baseKey.OpenSubKey("Patches"))
                using (RegistryKey productsKey = baseKey.OpenSubKey("Products"))
                {
                    if (productsKey == null) return result;
                    Func<string, string> localPackage = delegate(string code)
                    {
                        if (patchesKey == null) return null;
                        using (RegistryKey k = patchesKey.OpenSubKey(code)) { return k == null ? null : k.GetValue("LocalPackage") as string; }
                    };
                    HashSet<string> applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    string[] productNames = productsKey.GetSubKeyNames();
                    foreach (string pn in productNames)
                    {
                        using (RegistryKey pk = productsKey.OpenSubKey(pn + @"\Patches"))
                        {
                            if (pk == null) continue;
                            foreach (string code in pk.GetSubKeyNames())
                            {
                                using (RegistryKey ck = pk.OpenSubKey(code))
                                {
                                    if (ck != null && ToInt(ck.GetValue("State")) == 1)
                                    {
                                        string lp = localPackage(code);
                                        if (!string.IsNullOrEmpty(lp)) applied.Add(lp);
                                    }
                                }
                            }
                        }
                    }
                    foreach (string pn in productNames)
                    {
                        if (cancel != null && cancel.IsSet) throw new OperationCanceledException();
                        using (RegistryKey pk = productsKey.OpenSubKey(pn + @"\Patches"))
                        {
                            if (pk == null) continue;
                            string[] codes = pk.GetSubKeyNames();
                            if (codes.Length <= keep) continue;
                            string pname = null;
                            using (RegistryKey ip = productsKey.OpenSubKey(pn + @"\InstallProperties")) { if (ip != null) pname = ip.GetValue("DisplayName") as string; }
                            if (string.IsNullOrEmpty(pname)) pname = L.T("(unnamed program)");
                            List<PatchFile> list = new List<PatchFile>();
                            foreach (string code in codes)
                            {
                                using (RegistryKey ck = pk.OpenSubKey(code))
                                {
                                    if (ck == null) continue;
                                    string lp = localPackage(code);
                                    if (string.IsNullOrEmpty(lp) || !System.IO.File.Exists(lp)) continue;
                                    PatchFile pf = new PatchFile();
                                    pf.Product = pname;
                                    pf.Name = ck.GetValue("DisplayName") as string;
                                    pf.State = ToInt(ck.GetValue("State"));
                                    pf.Installed = ToInt(ck.GetValue("Installed"));
                                    pf.File = lp;
                                    try { pf.Bytes = new FileInfo(lp).Length; } catch { }
                                    list.Add(pf);
                                }
                            }
                            List<PatchFile> sorted = list.OrderByDescending(x => x.Installed).ThenByDescending(x => x.File, StringComparer.OrdinalIgnoreCase).ToList();
                            foreach (PatchFile pf in sorted.Skip(keep))
                            {
                                if (pf.State == 2 && pf.File.StartsWith(installerDir, StringComparison.OrdinalIgnoreCase)
                                    && pf.File.EndsWith(".msp", StringComparison.OrdinalIgnoreCase) && !applied.Contains(pf.File))
                                    result.Add(pf);
                            }
                        }
                    }
                }
            }
            return result;
        }

        public static void RemovePatches(List<PatchFile> list, OpCounter c)
        {
            try { Directory.CreateDirectory(AppPaths.DataDir); } catch { }
            bool newFile = !System.IO.File.Exists(AppPaths.DeletedCsv);
            StringBuilder sb = new StringBuilder();
            if (newFile) sb.AppendLine("\"Program\",\"Update\",\"Installed\",\"MB\",\"File\"");
            foreach (PatchFile f in list)
            {
                try
                {
                    System.IO.File.SetAttributes(f.File, FileAttributes.Normal);
                    System.IO.File.Delete(f.File);
                    c.Ok++;
                    sb.AppendLine(string.Join(",", new[] { Csv(f.Product), Csv(f.Name), Csv(f.Installed.ToString()), Csv(Math.Round(f.Bytes / 1048576.0).ToString()), Csv(f.File) }));
                }
                catch { c.Failed++; }
            }
            try { System.IO.File.AppendAllText(AppPaths.DeletedCsv, sb.ToString(), Encoding.UTF8); } catch { }
        }

        static string Csv(string s) { return "\"" + (s ?? "").Replace("\"", "\"\"") + "\""; }

        // Project folders VS Code has not touched for 'days' days.
        public static List<WorkspaceFolder> OldWorkspaces(string root, int days, CancelFlag cancel)
        {
            List<WorkspaceFolder> outList = new List<WorkspaceFolder>();
            if (!Directory.Exists(root)) return outList;
            DateTime cut = DateTime.Now.AddDays(-days);
            foreach (string d in SafeDirs(root))
            {
                if (cancel != null && cancel.IsSet) throw new OperationCanceledException();
                DateTime newest = DateTime.MinValue;
                long bytes = 0;
                try
                {
                    DirectoryInfo di = new DirectoryInfo(d);
                    newest = di.LastWriteTime;
                    foreach (FileInfo f in di.GetFiles("*", SearchOption.AllDirectories))
                    {
                        bytes += f.Length;
                        if (f.LastWriteTime > newest) newest = f.LastWriteTime;
                    }
                }
                catch { continue; }
                if (newest < cut)
                {
                    WorkspaceFolder w = new WorkspaceFolder();
                    w.Path = d; w.Bytes = bytes;
                    outList.Add(w);
                }
            }
            return outList;
        }

        public static List<ProgramInfo> InstalledPrograms()
        {
            Dictionary<string, ProgramInfo> seen = new Dictionary<string, ProgramInfo>(StringComparer.OrdinalIgnoreCase);
            List<RegistryKey> sources = new List<RegistryKey>();
            try { sources.Add(LocalMachine64().OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")); } catch { }
            try { sources.Add(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")); } catch { }
            try { sources.Add(Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")); } catch { }
            foreach (RegistryKey root in sources)
            {
                if (root == null) continue;
                foreach (string sub in root.GetSubKeyNames())
                {
                    try
                    {
                        using (RegistryKey k = root.OpenSubKey(sub))
                        {
                            if (k == null) continue;
                            string name = k.GetValue("DisplayName") as string;
                            if (string.IsNullOrWhiteSpace(name)) continue;
                            if (ToInt(k.GetValue("SystemComponent")) == 1) continue;
                            if (k.GetValue("ParentKeyName") != null || k.GetValue("ReleaseType") != null) continue;
                            name = name.Trim();
                            if (seen.ContainsKey(name)) continue;
                            ProgramInfo p = new ProgramInfo();
                            p.Name = name;
                            p.Bytes = (uint)ToInt(k.GetValue("EstimatedSize")) * 1024.0;
                            string loc = ((k.GetValue("InstallLocation") as string) ?? "").Trim().Trim('"');
                            p.Drive = (loc.Length >= 2 && loc[1] == ':' && char.IsLetter(loc[0])) ? char.ToUpperInvariant(loc[0]) + ":" : "?";
                            seen[name] = p;
                        }
                    }
                    catch { }
                }
                root.Close();
            }
            return seen.Values.ToList();
        }

        public static List<WslDisk> WslDisks()
        {
            List<WslDisk> outList = new List<WslDisk>();
            try
            {
                using (RegistryKey lx = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Lxss"))
                {
                    if (lx == null) return outList;
                    foreach (string sub in lx.GetSubKeyNames())
                    {
                        using (RegistryKey k = lx.OpenSubKey(sub))
                        {
                            if (k == null) continue;
                            string bp = k.GetValue("BasePath") as string;
                            string name = k.GetValue("DistributionName") as string;
                            if (string.IsNullOrEmpty(bp) || (name ?? "").StartsWith("docker-desktop", StringComparison.OrdinalIgnoreCase)) continue;
                            if (bp.StartsWith(@"\\?\")) bp = bp.Substring(4);
                            string vhd = Path.Combine(bp, "ext4.vhdx");
                            if (System.IO.File.Exists(vhd)) { WslDisk w = new WslDisk(); w.Name = name; w.Path = vhd; outList.Add(w); }
                        }
                    }
                }
            }
            catch { }
            return outList;
        }

        public static long HiberBytes()
        {
            try
            {
                FileInfo f = new FileInfo(Path.Combine(AppPaths.Root, "hiberfil.sys"));
                return f.Exists ? f.Length : 0;
            }
            catch { return 0; }
        }

        public static long? RestorePointBytes()
        {
            if (!Env.IsAdmin) return null;
            try
            {
                long t = 0;
                using (ManagementObjectSearcher s = new ManagementObjectSearcher("SELECT UsedSpace FROM Win32_ShadowStorage"))
                {
                    foreach (ManagementObject mo in s.Get()) { t += Convert.ToInt64(mo["UsedSpace"]); mo.Dispose(); }
                }
                return t;
            }
            catch { return null; }
        }

        public static string DownloadsPath()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders"))
                {
                    string p = k == null ? null : k.GetValue("{374DE290-123F-4565-9164-39C4925E467B}", null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
                    if (!string.IsNullOrEmpty(p))
                    {
                        p = Regex.Replace(p, "%USERPROFILE%", AppPaths.UserProfile.Replace("$", "$$"), RegexOptions.IgnoreCase);
                        p = Environment.ExpandEnvironmentVariables(p);
                        if (Directory.Exists(p)) return p;
                    }
                }
            }
            catch { }
            return Path.Combine(AppPaths.UserProfile, "Downloads");
        }
    }
}
