// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Reads the device details (like dxdiag). It only reads; nothing is changed.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.Device
{
    public static class DeviceInfoReader
    {
        const string StorageNs = @"root\Microsoft\Windows\Storage";

        public static DeviceReport Read()
        {
            DeviceReport r = new DeviceReport();
            Try(delegate { AddPc(r); });
            Try(delegate { AddWindows(r); });
            Try(delegate { AddProcessor(r); });
            Try(delegate { AddMemory(r); });
            Try(delegate { AddGraphics(r); });
            Try(delegate { AddScreens(r); });
            Try(delegate { AddStorage(r); });
            Try(delegate { AddBattery(r); });
            Try(delegate { AddNetwork(r); });
            Try(delegate { AddSound(r); });
            // the summary tiles come out in the order the sections are read: Windows, processor, memory, graphics
            return r;
        }

        static void Try(Action a) { try { a(); } catch { } }

        // ------------------------------------------------------------ WMI and registry helpers
        // null when the query itself failed (for example access denied), an empty list when nothing was found
        static List<ManagementBaseObject> Query(string ns, string wql)
        {
            try
            {
                List<ManagementBaseObject> list = new List<ManagementBaseObject>();
                EnumerationOptions eo = new EnumerationOptions();
                eo.Timeout = TimeSpan.FromSeconds(15);
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(new ManagementScope(ns), new ObjectQuery(wql), eo))
                {
                    foreach (ManagementBaseObject mo in s.Get()) list.Add(mo);
                }
                return list;
            }
            catch { return null; }
        }

        static List<ManagementBaseObject> All(string wql) { return All(@"root\cimv2", wql); }
        static List<ManagementBaseObject> All(string ns, string wql) { return Query(ns, wql) ?? new List<ManagementBaseObject>(); }
        static ManagementBaseObject First(string wql) { return All(wql).FirstOrDefault(); }

        static object V(ManagementBaseObject mo, string prop)
        {
            if (mo == null) return null;
            try { return mo[prop]; } catch { return null; }
        }

        static string S(ManagementBaseObject mo, string prop)
        {
            object v = V(mo, prop);
            if (v == null) return null;
            string s = Convert.ToString(v, CultureInfo.InvariantCulture).Trim();
            return s.Length == 0 ? null : s;
        }

        static long? Num(ManagementBaseObject mo, string prop)
        {
            object v = V(mo, prop);
            if (v == null) return null;
            try { return Convert.ToInt64(v, CultureInfo.InvariantCulture); } catch { return null; }
        }

        static bool? Bo(ManagementBaseObject mo, string prop)
        {
            object v = V(mo, prop);
            if (v is bool) return (bool)v;
            return null;
        }

        static DateTime? Dt(ManagementBaseObject mo, string prop)
        {
            string s = S(mo, prop);
            if (s == null) return null;
            try { return ManagementDateTimeConverter.ToDateTime(s); } catch { return null; }
        }

        static RegistryKey Hklm()
        {
            return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Default);
        }

        static object Reg(string path, string name)
        {
            try
            {
                using (RegistryKey b = Hklm())
                using (RegistryKey k = b.OpenSubKey(path))
                {
                    return k == null ? null : k.GetValue(name);
                }
            }
            catch { return null; }
        }

        static string RegStr(string path, string name)
        {
            object v = Reg(path, name);
            string s = v == null ? null : Convert.ToString(v, CultureInfo.InvariantCulture).Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        static int RegInt(string path, string name, int fallback)
        {
            object v = Reg(path, name);
            if (v is int) return (int)v;
            return fallback;
        }

        // Makers leave placeholder text in these fields on self-built PCs.
        static string Clean(string s)
        {
            if (s == null) return null;
            s = Regex.Replace(s, @"\s+", " ").Trim();
            string l = s.ToLowerInvariant();
            if (l.Length == 0 || l.Contains("to be filled") || l == "default string" || l == "system manufacturer" || l == "system product name" ||
                l == "system version" || l == "none" || l == "not applicable" || l == "o.e.m." || l == "oem" || l == "undefined" || l == "unknown") return null;
            return s;
        }

        // "Intel(R) Core(TM) i7-8550U CPU @ 1.80GHz" -> "Intel Core i7-8550U"
        static string Tidy(string s)
        {
            if (s == null) return null;
            s = Regex.Replace(s, @"\((R|TM|C)\)|[\u00AE\u2122]", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\s+", " ").Trim();
            return s;
        }

        static string ShortCpu(string s)
        {
            s = Tidy(s);
            if (s == null) return null;
            s = Regex.Replace(s, @"@.*$", "");
            s = Regex.Replace(s, @"\b\d+(st|nd|rd|th) Gen\b", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bwith Radeon.*$", "", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\b(CPU|Processor|\d+-Core)\b", "", RegexOptions.IgnoreCase);
            return Regex.Replace(s, @"\s+", " ").Trim();
        }

        static string Join(string sep, params string[] parts) { return string.Join(sep, parts.Where(p => !string.IsNullOrEmpty(p))); }

        static string Gb(double bytes)
        {
            double g = bytes / 1073741824.0;
            if (g >= 1024) return L.Ltr((g / 1024).ToString("0.#") + " TB");
            if (Math.Abs(g - Math.Round(g)) < 0.05) return L.Ltr(Math.Round(g).ToString("N0") + " GB");
            return L.Ltr(g.ToString("N1") + " GB");
        }

        static string Ghz(long? mhz) { return mhz.HasValue && mhz.Value > 0 ? L.Ltr((mhz.Value / 1000.0).ToString("0.00") + " GHz") : null; }

        static string Cache(long? kb)
        {
            if (!kb.HasValue || kb.Value <= 0) return null;
            return L.Ltr(kb.Value >= 1024 ? (kb.Value / 1024.0).ToString("0.#") + " MB" : kb.Value + " KB");
        }

        static string Ago(TimeSpan t)
        {
            if (t.TotalMinutes < 60) return L.P(Math.Max(0, (int)t.TotalMinutes), "1 minute", "{0} minutes");
            if (t.TotalHours < 48) return L.P((int)t.TotalHours, "1 hour", "{0} hours");
            return L.P((int)t.TotalDays, "1 day", "{0} days");
        }

        static string LinkSpeed(long bps)
        {
            if (bps >= 1000000000) return L.Ltr((bps / 1e9).ToString("0.#") + " Gb/s");
            return L.Ltr((bps / 1e6).ToString("N0") + " Mb/s");
        }

        // ------------------------------------------------------------ This PC
        static void AddPc(DeviceReport rep)
        {
            InfoSection s = new InfoSection(L.T("This PC"));
            ManagementBaseObject cs = First("SELECT * FROM Win32_ComputerSystem");
            ManagementBaseObject prod = First("SELECT * FROM Win32_ComputerSystemProduct");
            ManagementBaseObject board = First("SELECT * FROM Win32_BaseBoard");
            ManagementBaseObject bios = First("SELECT * FROM Win32_BIOS");
            ManagementBaseObject enc = First("SELECT * FROM Win32_SystemEnclosure");

            string maker = Clean(S(cs, "Manufacturer"));
            string model = Clean(S(cs, "Model"));
            string version = Clean(S(prod, "Version"));
            // Lenovo keeps the readable name ("ThinkPad T480") in the product version and a code in the model
            if (maker != null && maker.IndexOf("lenovo", StringComparison.OrdinalIgnoreCase) >= 0 && version != null && model != null && version != model)
                model = version + "  (" + model + ")";
            s.Add(L.T("Maker"), maker);
            s.Add(L.T("Model"), model);
            s.Add(L.T("Type"), PcKind(enc, cs));
            s.Add(L.T("Computer name"), Environment.MachineName);
            s.Add(L.T("Motherboard"), Join(" ", Clean(S(board, "Manufacturer")), Clean(S(board, "Product"))));
            string biosVer = Join(" ", Clean(S(bios, "Manufacturer")), Clean(S(bios, "SMBIOSBIOSVersion")) ?? Clean(S(bios, "Version")));
            DateTime? biosDate = Dt(bios, "ReleaseDate");
            if (biosDate.HasValue) biosVer += "  (" + biosDate.Value.ToString("d") + ")";
            s.Add(L.T("BIOS"), biosVer);
            int fw = 0;
            try { if (!GetFirmwareType(out fw)) fw = 0; } catch { fw = 0; }   // 1 = Legacy BIOS, 2 = UEFI
            s.Add(L.T("Start-up mode"), fw == 2 ? "UEFI" : fw == 1 ? L.T("Legacy BIOS") : null);
            int sb = RegInt(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State", "UEFISecureBootEnabled", -1);
            s.Add(L.T("Secure Boot"), sb == 1 ? L.T("On") : sb == 0 ? L.T("Off") : fw == 1 ? L.T("Not available with Legacy BIOS") : null);
            if (Env.IsAdmin)
            {
                List<ManagementBaseObject> tpms = Query(@"root\cimv2\Security\MicrosoftTpm", "SELECT * FROM Win32_Tpm");
                if (tpms != null)
                {
                    if (tpms.Count == 0) s.Add(L.T("Security chip (TPM)"), L.T("Not found, or turned off in the BIOS"));
                    else
                    {
                        string spec = S(tpms[0], "SpecVersion");
                        spec = spec == null ? "" : spec.Split(',')[0].Trim();
                        bool on = Bo(tpms[0], "IsEnabled_InitialValue") != false && Bo(tpms[0], "IsActivated_InitialValue") != false;
                        s.Add(L.T("Security chip (TPM)"), on ? L.F("TPM {0}, on", spec) : L.F("TPM {0}, turned off", spec));
                    }
                }
            }
            rep.Sections.Add(s);
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetFirmwareType(out int firmwareType);

        static string PcKind(ManagementBaseObject enc, ManagementBaseObject cs)
        {
            ushort[] types = V(enc, "ChassisTypes") as ushort[];
            if (types != null)
            {
                foreach (ushort t in types)
                {
                    switch (t)
                    {
                        case 8: case 9: case 10: case 14: return L.T("Laptop");
                        case 31: return L.T("Laptop that folds into a tablet (2-in-1)");
                        case 32: return L.T("Tablet with a detachable keyboard (2-in-1)");
                        case 30: return L.T("Tablet");
                        case 13: return L.T("All-in-one");
                        case 3: case 4: case 5: case 6: case 7: case 15: case 16: case 35: case 36: return L.T("Desktop");
                        case 17: case 23: case 28: case 29: return L.T("Server");
                    }
                }
            }
            switch ((int)(Num(cs, "PCSystemType") ?? 0))
            {
                case 1: return L.T("Desktop");
                case 2: return L.T("Laptop");
                case 3: return L.T("Workstation");
                case 4: case 5: case 7: return L.T("Server");
                case 8: return L.T("Tablet");
            }
            return null;
        }

        // ------------------------------------------------------------ Windows
        static void AddWindows(DeviceReport rep)
        {
            InfoSection s = new InfoSection(L.T("Windows"));
            ManagementBaseObject os = First("SELECT * FROM Win32_OperatingSystem");
            const string cv = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            // the caption says "Windows 11" correctly; the registry product name still says "Windows 10" there
            string edition = Clean(S(os, "Caption")) ?? RegStr(cv, "ProductName");
            if (edition != null && edition.StartsWith("Microsoft ", StringComparison.OrdinalIgnoreCase)) edition = edition.Substring(10);
            string display = RegStr(cv, "DisplayVersion") ?? RegStr(cv, "ReleaseId");
            string build = RegStr(cv, "CurrentBuild") ?? S(os, "BuildNumber");
            int ubr = RegInt(cv, "UBR", -1);
            if (build != null && ubr >= 0) build += "." + ubr;
            s.Add(L.T("Edition"), edition);
            s.Add(L.T("Version"), display);
            s.Add(L.T("Build"), build);
            string arch = RegStr(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "PROCESSOR_ARCHITECTURE");
            s.Add(L.T("Type"), arch == "ARM64" ? L.T("64-bit, for ARM processors") : Environment.Is64BitOperatingSystem ? L.T("64-bit") : L.T("32-bit"));
            DateTime? inst = Dt(os, "InstallDate");
            if (inst.HasValue) s.Add(L.T("Installed or last big update"), inst.Value.ToString("D"));
            DateTime? lastUpdate = null;
            string lastKb = null;
            foreach (ManagementBaseObject q in All("SELECT HotFixID, InstalledOn FROM Win32_QuickFixEngineering"))
            {
                string on = S(q, "InstalledOn");
                DateTime d;
                if (on == null || !DateTime.TryParseExact(on, new[] { "M/d/yyyy", "MM/dd/yyyy", "yyyyMMdd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) continue;
                if (d > DateTime.Now.AddDays(1)) continue;
                if (!lastUpdate.HasValue || d > lastUpdate.Value) { lastUpdate = d; lastKb = S(q, "HotFixID"); }
            }
            if (lastUpdate.HasValue) s.Add(L.T("Last update installed"), lastKb != null ? L.F("{0}  ({1})", lastUpdate.Value.ToString("D"), lastKb) : lastUpdate.Value.ToString("D"));
            DateTime? boot = Dt(os, "LastBootUpTime");
            if (boot.HasValue) s.Add(L.T("Running since"), L.F("{0}  ({1})", boot.Value.ToString("g"), Ago(DateTime.Now - boot.Value)));
            s.Add(L.T("Display language"), CultureInfo.CurrentUICulture.DisplayName);
            s.Add(L.T("Region format"), CultureInfo.CurrentCulture.DisplayName);
            s.Add(L.T("Time zone"), TimeZoneInfo.Local.DisplayName);
            if (Environment.OSVersion.Version.Major >= 10) s.Add(L.T("DirectX"), "DirectX 12");

            if (lastUpdate.HasValue && (DateTime.Now - lastUpdate.Value).TotalDays > 60)
                s.Warnings.Add(L.F("Windows has not installed an update since {0}. Open Settings > Windows Update and check for updates: they fix security holes and crashes.", lastUpdate.Value.ToString("D")));
            if (boot.HasValue && (DateTime.Now - boot.Value).TotalDays >= 14)
                s.Warnings.Add(L.F("Windows has been running for {0} days without a restart. Shutting down does not fully restart Windows when Fast Startup is on, so use Restart now and then: it finishes updates and frees memory.", (int)(DateTime.Now - boot.Value).TotalDays));

            rep.Sections.Add(s);
            rep.Tiles.Add(new SummaryTile(L.T("WINDOWS"), edition ?? "Windows", Join("  ·  ", display, build != null ? L.F("build {0}", build) : null), "#2563EB"));
        }

        // ------------------------------------------------------------ Processor
        static void AddProcessor(DeviceReport rep)
        {
            List<ManagementBaseObject> cpus = All("SELECT * FROM Win32_Processor");
            if (cpus.Count == 0) return;
            InfoSection s = new InfoSection(L.T("Processor"));
            ManagementBaseObject c = cpus[0];
            ManagementBaseObject cs = First("SELECT * FROM Win32_ComputerSystem");
            string name = Tidy(S(c, "Name"));
            long cores = cpus.Sum(x => Num(x, "NumberOfCores") ?? 0);
            long threads = cpus.Sum(x => Num(x, "NumberOfLogicalProcessors") ?? 0);
            string maker = S(c, "Manufacturer");
            if (maker == "GenuineIntel") maker = "Intel";
            else if (maker == "AuthenticAMD") maker = "AMD";
            s.Add(L.T("Name"), name);
            s.Add(L.T("Made by"), maker);
            if (cores > 0) s.Add(L.T("Cores"), L.F("{0} cores, {1} threads", cores, threads) + (cpus.Count > 1 ? "  " + L.F("({0} processors)", cpus.Count) : ""));
            s.Add(L.T("Base speed"), Ghz(Num(c, "MaxClockSpeed")));
            string cache = Join(", ", Cache(Num(c, "L2CacheSize")) != null ? "L2 " + Cache(Num(c, "L2CacheSize")) : null,
                                       Cache(Num(c, "L3CacheSize")) != null ? "L3 " + Cache(Num(c, "L3CacheSize")) : null);
            s.Add(L.T("Cache"), cache);
            long? load = Num(c, "LoadPercentage");
            if (load.HasValue) s.Add(L.T("Busy right now"), L.Ltr(load.Value + "%"));
            bool? hyper = Bo(cs, "HypervisorPresent");
            bool? vfw = Bo(c, "VirtualizationFirmwareEnabled");
            s.Add(L.T("Virtualization"), hyper == true ? L.T("In use (by Hyper-V, WSL, Docker or Windows security features)")
                                  : vfw == true ? L.T("On in the BIOS") : vfw == false ? L.T("Off in the BIOS") : null);
            s.Add(L.T("Socket"), Clean(S(c, "SocketDesignation")));
            rep.Sections.Add(s);
            rep.Tiles.Add(new SummaryTile(L.T("PROCESSOR"), ShortCpu(name) ?? "-",
                Join("  ·  ", cores > 0 ? L.F("{0} cores, {1} threads", cores, threads) : null, Ghz(Num(c, "MaxClockSpeed"))), "#7C3AED"));
        }

        // ------------------------------------------------------------ Memory
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
            public MEMORYSTATUSEX() { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)); }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        static string MemType(long? t)
        {
            switch ((int)(t ?? 0))
            {
                case 20: return "DDR";
                case 21: return "DDR2";
                case 24: return "DDR3";
                case 26: return "DDR4";
                case 27: return "LPDDR";
                case 28: return "LPDDR2";
                case 29: return "LPDDR3";
                case 30: return "LPDDR4";
                case 34: return "DDR5";
                case 35: return "LPDDR5";
            }
            return null;
        }

        static string MemForm(long? f)
        {
            switch ((int)(f ?? 0))
            {
                case 8: return L.T("DIMM (desktop size)");
                case 12: return L.T("SO-DIMM (laptop size)");
                case 14: case 21: case 22: case 23: return L.T("Soldered on the board");
            }
            return null;
        }

        // Memory makers are sometimes stored as JEDEC codes.
        static string MemMaker(string m)
        {
            m = Clean(m);
            if (m == null) return null;
            string u = m.ToUpperInvariant();
            string[][] codes = {
                new[] { "80CE", "Samsung" }, new[] { "CE00", "Samsung" }, new[] { "802C", "Micron" }, new[] { "2C00", "Micron" },
                new[] { "80AD", "SK hynix" }, new[] { "AD00", "SK hynix" }, new[] { "0198", "Kingston" }, new[] { "9801", "Kingston" },
                new[] { "859B", "Crucial" }, new[] { "9B85", "Crucial" }, new[] { "04CD", "G.Skill" }, new[] { "CD04", "G.Skill" },
                new[] { "029E", "Corsair" }, new[] { "9E02", "Corsair" }, new[] { "04CB", "ADATA" }, new[] { "CB04", "ADATA" }
            };
            if (Regex.IsMatch(u, "^[0-9A-F]{4,}$"))
                foreach (string[] c in codes) if (u.StartsWith(c[0])) return c[1];
            return m;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetSystemFirmwareTable(uint provider, uint tableId, byte[] buffer, uint size);

        // Memory slots from the firmware's own table (one type 17 entry per slot), the way Task Manager counts them.
        // The slot count in Win32_PhysicalMemoryArray is often wrong.
        static void SmbiosSlots(out int slots, out int filled)
        {
            slots = 0; filled = 0;
            try
            {
                const uint RSMB = 0x52534D42;
                uint size = GetSystemFirmwareTable(RSMB, 0, null, 0);
                if (size < 8 || size > (1 << 22)) return;
                byte[] buf = new byte[size];
                if (GetSystemFirmwareTable(RSMB, 0, buf, size) != size) return;
                int end = (int)Math.Min(buf.Length, 8L + BitConverter.ToUInt32(buf, 4));
                int i = 8;   // after the RawSMBIOSData header
                while (i + 4 <= end)
                {
                    byte type = buf[i], len = buf[i + 1];
                    if (len < 4 || type == 127) break;
                    if (type == 17 && len >= 0x0E && i + 0x0E <= end)
                    {
                        slots++;
                        if (BitConverter.ToUInt16(buf, i + 0x0C) != 0) filled++;
                    }
                    int j = i + len;
                    while (j + 1 < end && !(buf[j] == 0 && buf[j + 1] == 0)) j++;
                    i = j + 2;
                }
            }
            catch { slots = 0; filled = 0; }
        }

        static void AddMemory(DeviceReport rep)
        {
            InfoSection s = new InfoSection(L.T("Memory (RAM)"));
            List<ManagementBaseObject> sticks = All("SELECT * FROM Win32_PhysicalMemory");
            MEMORYSTATUSEX m = new MEMORYSTATUSEX();
            bool haveStatus = GlobalMemoryStatusEx(m);
            double installed = sticks.Sum(x => (double)(Num(x, "Capacity") ?? 0));
            if (installed <= 0 && haveStatus) installed = m.ullTotalPhys;
            string type = null, speed = null, form = null;
            bool soldered = false;
            foreach (ManagementBaseObject st in sticks)
            {
                if (type == null) type = MemType(Num(st, "SMBIOSMemoryType"));
                if (speed == null)
                {
                    long? sp = Num(st, "ConfiguredClockSpeed");
                    if (!sp.HasValue || sp.Value <= 0) sp = Num(st, "Speed");
                    if (sp.HasValue && sp.Value > 0) speed = L.Ltr(sp.Value + " MT/s");
                }
                if (form == null) form = MemForm(Num(st, "FormFactor"));
                long ff = Num(st, "FormFactor") ?? 0;
                if (ff == 14 || ff == 21 || ff == 22 || ff == 23) soldered = true;
            }
            if (installed > 0) s.Add(L.T("Installed"), haveStatus ? L.F("{0}  ({1} GB usable by Windows)", Gb(installed), (m.ullTotalPhys / 1073741824.0).ToString("N1")) : Gb(installed));
            if (haveStatus)
            {
                double used = m.ullTotalPhys - m.ullAvailPhys;
                s.Add(L.T("In use now"), L.F("{0} GB of {1} GB  ({2}%)", (used / 1073741824.0).ToString("N1"), (m.ullTotalPhys / 1073741824.0).ToString("N1"), m.dwMemoryLoad));
            }
            s.Add(L.T("Type and speed"), Join(", ", type, speed));
            s.Add(L.T("Module type"), form);

            int slots, filled;
            SmbiosSlots(out slots, out filled);
            if (slots > 0) s.Add(L.T("Memory slots"), L.F("{0} of {1} used", filled, slots));

            // the page file: this is where Windows puts memory when the RAM is full
            List<string> pf = new List<string>();
            foreach (ManagementBaseObject p in All("SELECT * FROM Win32_PageFileUsage"))
            {
                long size = Num(p, "AllocatedBaseSize") ?? 0, cur = Num(p, "CurrentUsage") ?? 0, peak = Num(p, "PeakUsage") ?? 0;
                pf.Add(L.F("{0}, {1}  ({2} in use, highest {3})", S(p, "Name"), Gb(size * 1048576.0), Gb(cur * 1048576.0), Gb(peak * 1048576.0)));
            }
            bool? auto = Bo(First("SELECT AutomaticManagedPagefile FROM Win32_ComputerSystem"), "AutomaticManagedPagefile");
            if (pf.Count > 0) s.Add(L.T("Page file"), string.Join("; ", pf) + (auto == true ? L.T(", size managed by Windows") : auto == false ? L.T(", size set by hand") : ""));
            else if (haveStatus) s.Add(L.T("Page file"), L.T("None. Programs can crash when the memory is full."));
            if (haveStatus)
            {
                double commitUsed = m.ullTotalPageFile - m.ullAvailPageFile;
                s.Add(L.T("Memory promised to programs"), L.F("{0} GB of {1} GB  (memory plus page file)", (commitUsed / 1073741824.0).ToString("N1"), (m.ullTotalPageFile / 1073741824.0).ToString("N1")));
            }

            int n = 0;
            foreach (ManagementBaseObject st in sticks)
            {
                n++;
                string slot = Clean(S(st, "DeviceLocator")) ?? Clean(S(st, "BankLabel")) ?? L.F("Module {0}", n);
                InfoGroup g = new InfoGroup(slot);
                long cap = Num(st, "Capacity") ?? 0;
                if (cap > 0) g.Add(L.T("Size"), Gb(cap));
                long? sp = Num(st, "ConfiguredClockSpeed");
                if (!sp.HasValue || sp.Value <= 0) sp = Num(st, "Speed");
                g.Add(L.T("Type and speed"), Join(", ", MemType(Num(st, "SMBIOSMemoryType")), sp.HasValue && sp.Value > 0 ? sp.Value + " MT/s" : null));
                g.Add(L.T("Made by"), MemMaker(S(st, "Manufacturer")));
                g.Add(L.T("Part number"), Clean(S(st, "PartNumber")));
                s.Groups.Add(g);
            }

            if (haveStatus && m.dwMemoryLoad >= 85)
                s.Warnings.Add(L.F("The memory is almost full right now ({0}% in use). Close programs you do not need. A PC that runs out of memory gets slow and can crash.", m.dwMemoryLoad));
            if (installed > 0 && installed < 7.5 * 1073741824.0)
                s.Warnings.Add(L.F("{0} is little for Windows 10 and 11 today. 8 GB or more makes a big difference.", Gb(installed)));
            else if (slots > filled && filled > 0 && installed <= 16 * 1073741824.0 && !soldered)
                s.Warnings.Add(type != null ? L.F("This PC reports a free memory slot. Adding a memory stick of the same type ({0}) is usually a cheap way to make it faster.", Join(", ", type, speed)) : L.T("This PC reports a free memory slot. Adding a memory stick of the same type is usually a cheap way to make it faster."));
            else if (slots > 0 && slots == filled && installed <= 8.5 * 1073741824.0 && !soldered)
                s.Warnings.Add(slots == 1 ? L.F("The only memory slot is in use. To get more memory, swap the stick for a bigger one ({0}). 16 GB in total is a good size today.", type ?? "-")
                             : slots == 2 ? L.F("Both memory slots are in use. To get more memory, swap the sticks for bigger ones ({0}). 16 GB in total is a good size today.", type ?? "-")
                             : L.F("All {0} memory slots are in use. To get more memory, swap the sticks for bigger ones ({1}). 16 GB in total is a good size today.", slots, type ?? "-"));

            rep.Sections.Add(s);
            if (installed > 0)
                rep.Tiles.Add(new SummaryTile(L.T("MEMORY"), Gb(installed), Join("  ·  ", Join(" ", type, speed),
                    haveStatus ? L.F("{0}% in use now", m.dwMemoryLoad) : null), "#0891B2"));
        }

        // ------------------------------------------------------------ Graphics
        // Video memory from the driver's registry entry: Windows' own list stops counting at 4 GB.
        static Dictionary<string, long> VideoMemory()
        {
            Dictionary<string, long> d = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (RegistryKey b = Hklm())
                using (RegistryKey cls = b.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"))
                {
                    if (cls == null) return d;
                    foreach (string sub in cls.GetSubKeyNames())
                    {
                        if (!Regex.IsMatch(sub, @"^\d{4}$")) continue;
                        try
                        {
                            using (RegistryKey k = cls.OpenSubKey(sub))
                            {
                                if (k == null) continue;
                                string desc = Convert.ToString(k.GetValue("DriverDesc"));
                                long mem = RegBytes(k.GetValue("HardwareInformation.qwMemorySize"));
                                if (mem <= 0) mem = RegBytes(k.GetValue("HardwareInformation.MemorySize"));
                                if (!string.IsNullOrEmpty(desc) && mem > 0 && !d.ContainsKey(desc)) d[desc] = mem;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return d;
        }

        static long RegBytes(object v)
        {
            if (v is long) return (long)v;
            if (v is int) return (uint)(int)v;
            byte[] b = v as byte[];
            if (b != null && b.Length >= 8) return BitConverter.ToInt64(b, 0);
            if (b != null && b.Length >= 4) return BitConverter.ToUInt32(b, 0);
            return 0;
        }

        static void AddGraphics(DeviceReport rep)
        {
            List<ManagementBaseObject> gpus = All("SELECT * FROM Win32_VideoController")
                .Where(x => (S(x, "Name") ?? "").IndexOf("Remote Display", StringComparison.OrdinalIgnoreCase) < 0).ToList();
            if (gpus.Count == 0) return;
            InfoSection s = new InfoSection(L.T("Graphics"));
            Dictionary<string, long> vram = VideoMemory();
            string bestName = null, bestMem = null;
            long bestBytes = -1;
            List<string> names = new List<string>();
            foreach (ManagementBaseObject v in gpus)
            {
                string raw = S(v, "Name") ?? L.T("Graphics");
                string name = Tidy(raw);
                InfoGroup g = new InfoGroup(name);
                g.Add(L.T("Made by"), Tidy(S(v, "AdapterCompatibility")));
                long mem;
                if (!vram.TryGetValue(raw, out mem))
                {
                    mem = Num(v, "AdapterRAM") ?? 0;
                    if (mem < 0) mem += 4294967296L;
                }
                string memText = mem > 0 ? (mem >= 4293918720L && !vram.ContainsKey(raw) ? L.T("4 GB or more") : Gb(mem)) : null;
                g.Add(L.T("Graphics memory"), memText);
                DateTime? dd = Dt(v, "DriverDate");
                g.Add(L.T("Driver"), Join("  ", S(v, "DriverVersion"), dd.HasValue ? "(" + dd.Value.ToString("d") + ")" : null));
                long w = Num(v, "CurrentHorizontalResolution") ?? 0, h = Num(v, "CurrentVerticalResolution") ?? 0, hz = Num(v, "CurrentRefreshRate") ?? 0;
                if (w > 0 && h > 0) g.Add(L.T("Showing now"), hz > 1 ? L.F("{0} × {1} at {2} Hz", w, h, hz) : L.Ltr(w + " × " + h));
                long err = Num(v, "ConfigManagerErrorCode") ?? 0;
                if (raw.IndexOf("Basic Display", StringComparison.OrdinalIgnoreCase) >= 0 || raw.IndexOf("Basic Render", StringComparison.OrdinalIgnoreCase) >= 0)
                    g.Warnings.Add(L.T("No proper graphics driver is installed. Install the driver from the PC maker or the graphics chip maker for a sharper, faster picture."));
                else if (err != 0)
                    g.Warnings.Add(L.F("Windows reports a problem with this device (code {0}). Reinstall its driver from the PC maker's or the chip maker's website.", err));
                else if (dd.HasValue && (DateTime.Now - dd.Value).TotalDays > 730)
                    g.Warnings.Add(L.F("This driver is from {0}. A newer one from the PC maker or the chip maker can fix crashes, blue screens and display problems.", dd.Value.Year));
                s.Groups.Add(g);
                names.Add(name);
                if (mem > bestBytes) { bestBytes = mem; bestName = name; bestMem = memText; }
            }
            rep.Sections.Add(s);
            string others = string.Join(" + ", names.Where(x => x != bestName));
            rep.Tiles.Add(new SummaryTile(L.T("GRAPHICS"), bestName ?? "-", Join("  ·  ", bestMem, others.Length > 0 ? L.F("also {0}", others) : null), "#16A34A"));
        }

        // ------------------------------------------------------------ Screens
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
            public short dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
            public int dmFields;
            public int dmPositionX, dmPositionY, dmDisplayOrientation, dmDisplayFixedOutput;
            public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
            public int dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        class MonitorInfo { public string Name, Connection; public double Inches; public int Year; }

        static string WmiText(object v)
        {
            ushort[] a = v as ushort[];
            if (a == null) return null;
            StringBuilder sb = new StringBuilder();
            foreach (ushort c in a) { if (c == 0) break; sb.Append((char)c); }
            string s = sb.ToString().Trim();
            return s.Length == 0 ? null : s;
        }

        // "DISPLAY\LGD0521\4&..." or "MONITOR\LGD0521\{...}" -> "LGD0521"
        static string MonitorCode(string id)
        {
            if (id == null) return null;
            string[] p = id.Split('\\');
            return p.Length >= 2 ? p[1].ToUpperInvariant() : null;
        }

        static Dictionary<string, MonitorInfo> WmiMonitors()
        {
            Dictionary<string, MonitorInfo> d = new Dictionary<string, MonitorInfo>();
            Func<string, MonitorInfo> get = delegate(string inst)
            {
                string code = MonitorCode(inst);
                if (code == null) return null;
                MonitorInfo mi;
                if (!d.TryGetValue(code, out mi)) { mi = new MonitorInfo(); d[code] = mi; }
                return mi;
            };
            foreach (ManagementBaseObject mo in All(@"root\wmi", "SELECT * FROM WmiMonitorID"))
            {
                MonitorInfo mi = get(S(mo, "InstanceName"));
                if (mi == null) continue;
                mi.Name = WmiText(V(mo, "UserFriendlyName"));
                mi.Year = (int)(Num(mo, "YearOfManufacture") ?? 0);
            }
            foreach (ManagementBaseObject mo in All(@"root\wmi", "SELECT * FROM WmiMonitorBasicDisplayParams"))
            {
                MonitorInfo mi = get(S(mo, "InstanceName"));
                if (mi == null) continue;
                double w = Num(mo, "MaxHorizontalImageSize") ?? 0, h = Num(mo, "MaxVerticalImageSize") ?? 0;
                if (w > 0 && h > 0) mi.Inches = Math.Sqrt(w * w + h * h) / 2.54;
            }
            foreach (ManagementBaseObject mo in All(@"root\wmi", "SELECT * FROM WmiMonitorConnectionParams"))
            {
                MonitorInfo mi = get(S(mo, "InstanceName"));
                if (mi == null) continue;
                long t = Num(mo, "VideoOutputTechnology") ?? -2;
                switch (t)
                {
                    case 0: mi.Connection = "VGA"; break;
                    case 4: mi.Connection = "DVI"; break;
                    case 5: mi.Connection = "HDMI"; break;
                    case 10: mi.Connection = "DisplayPort"; break;
                    case 11: case 13: case 0x80000000L: case -2147483648L: mi.Connection = L.T("Built-in screen"); break;
                    case 15: mi.Connection = L.T("Wireless (Miracast)"); break;
                    case 16: mi.Connection = "USB-C"; break;
                }
            }
            return d;
        }

        static void AddScreens(DeviceReport rep)
        {
            InfoSection s = new InfoSection(L.T("Screens"));
            Dictionary<string, MonitorInfo> mons = WmiMonitors();
            int n = 0;
            for (uint i = 0; i < 16; i++)
            {
                DISPLAY_DEVICE dd = new DISPLAY_DEVICE();
                dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
                if (!EnumDisplayDevices(null, i, ref dd, 0)) break;
                if ((dd.StateFlags & 1) == 0) continue;   // not part of the desktop
                DEVMODE dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                if (!EnumDisplaySettings(dd.DeviceName, -1, ref dm)) continue;
                DISPLAY_DEVICE mon = new DISPLAY_DEVICE();
                mon.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
                bool hasMon = EnumDisplayDevices(dd.DeviceName, 0, ref mon, 0);
                MonitorInfo mi = null;
                string code = hasMon ? MonitorCode(mon.DeviceID) : null;
                if (code != null) mons.TryGetValue(code, out mi);
                n++;
                bool primary = (dd.StateFlags & 4) != 0;
                string title = mi != null && mi.Name != null ? mi.Name
                             : mi != null && mi.Connection == L.T("Built-in screen") ? L.T("Built-in screen")
                             : hasMon && !string.IsNullOrEmpty(mon.DeviceString) ? mon.DeviceString : L.F("Screen {0}", n);
                InfoGroup g = new InfoGroup(primary ? L.F("{0}  (main screen)", title) : title);
                g.Add(L.T("Resolution"), L.Ltr(dm.dmPelsWidth + " × " + dm.dmPelsHeight));
                if (dm.dmDisplayFrequency > 1) g.Add(L.T("Refresh rate"), L.Ltr(dm.dmDisplayFrequency + " Hz"));
                if (mi != null && mi.Inches > 5) g.Add(L.T("Size"), L.F("{0} inches", mi.Inches.ToString("0.#")));
                if (mi != null && mi.Connection != title) g.Add(L.T("Connection"), mi.Connection);
                g.Add(L.T("Graphics"), Tidy(dd.DeviceString));
                if (mi != null && mi.Year > 1990) g.Add(L.T("Made in"), mi.Year.ToString());
                s.Groups.Add(g);
            }
            if (s.Groups.Count > 0) rep.Sections.Add(s);
        }

        // ------------------------------------------------------------ Storage
        static void AddStorage(DeviceReport rep)
        {
            InfoSection s = new InfoSection(L.T("Storage"));
            s.Sub = L.T("Health, temperature and SMART data are in the Disk health and SMART details tabs.");
            Dictionary<int, string> kind = new Dictionary<int, string>();
            foreach (ManagementBaseObject p in All(StorageNs, "SELECT DeviceId, MediaType, BusType FROM MSFT_PhysicalDisk"))
            {
                int idx;
                if (!int.TryParse(S(p, "DeviceId"), out idx)) continue;
                long media = Num(p, "MediaType") ?? 0, bus = Num(p, "BusType") ?? 0;
                string m = media == 3 ? L.T("Hard disk") : media == 4 ? "SSD" : media == 5 ? L.T("Storage class memory") : null;
                string b = bus == 17 ? "NVMe" : bus == 11 ? "SATA" : bus == 7 ? "USB" : bus == 8 ? "RAID" : bus == 10 ? "SAS" : bus == 3 ? "ATA" : bus == 12 ? L.T("SD card") : bus == 13 ? "eMMC" : null;
                kind[idx] = Join(", ", b == "NVMe" && m == null ? "SSD" : m, b);
            }
            Dictionary<int, string> letters = new Dictionary<int, string>();
            foreach (ManagementBaseObject p in All(StorageNs, "SELECT DiskNumber, DriveLetter FROM MSFT_Partition"))
            {
                object dl = V(p, "DriveLetter");
                if (dl == null) continue;
                char c = Convert.ToChar(dl);
                if (!char.IsLetter(c)) continue;
                int dn = (int)(Num(p, "DiskNumber") ?? -1);
                string cur;
                letters.TryGetValue(dn, out cur);
                letters[dn] = Join(" ", cur, char.ToUpperInvariant(c) + ":");
            }
            double total = 0;
            int count = 0;
            foreach (ManagementBaseObject d in All("SELECT Index, Model, Size FROM Win32_DiskDrive").OrderBy(x => Num(x, "Index") ?? 0))
            {
                int idx = (int)(Num(d, "Index") ?? 0);
                long size = Num(d, "Size") ?? 0;
                string k, l;
                kind.TryGetValue(idx, out k);
                letters.TryGetValue(idx, out l);
                string drv = l != null ? L.F("drive {0}", l) : null, sz = size > 0 ? Gb(size) : null;
                // right-to-left: drive letters before the size, so "D: E:" is never the last thing on the line
                s.Add(L.F("Disk {0}", idx), L.Rtl ? Join("  ·  ", S(d, "Model"), k, drv, sz) : Join("  ·  ", S(d, "Model"), k, sz, drv));
                total += size;
                count++;
            }
            if (count > 1) s.Add(L.T("All disks together"), Gb(total));
            if (count > 0) rep.Sections.Add(s);
        }

        // ------------------------------------------------------------ Battery
        static void AddBattery(DeviceReport rep)
        {
            List<ManagementBaseObject> bats = All("SELECT * FROM Win32_Battery");
            if (bats.Count == 0) return;   // desktop PCs have none
            InfoSection s = new InfoSection(L.T("Battery"));
            ManagementBaseObject b = bats[0];
            long charge = Num(b, "EstimatedChargeRemaining") ?? -1;
            long st = Num(b, "BatteryStatus") ?? 0;
            string state = st == 1 || st == 4 || st == 5 ? L.T("on battery") : st >= 6 && st <= 9 ? L.T("plugged in, charging") : st == 3 ? L.T("plugged in, full") : st == 2 || st == 11 ? L.T("plugged in") : null;
            if (charge >= 0) s.Add(L.T("Charge now"), state != null ? L.F("{0}%, {1}", charge, state) : L.Ltr(charge + "%"));
            long run = Num(b, "EstimatedRunTime") ?? 0;
            if ((st == 1 || st == 4 || st == 5) && run > 0 && run < 6000) s.Add(L.T("Time left"), L.F("{0} h {1} min  (Windows' estimate)", run / 60, run % 60));

            List<ManagementBaseObject> stat = Query(@"root\wmi", "SELECT * FROM BatteryStaticData");
            List<ManagementBaseObject> full = Query(@"root\wmi", "SELECT * FROM BatteryFullChargedCapacity");
            double design = stat == null ? 0 : stat.Sum(x => (double)(Num(x, "DesignedCapacity") ?? 0));
            double now = full == null ? 0 : full.Sum(x => (double)(Num(x, "FullChargedCapacity") ?? 0));
            int health = -1;
            if (design > 0 && now > 0)
            {
                health = (int)Math.Round(Math.Min(100, now * 100 / design));
                s.Add(L.T("Battery health"), L.F("{0}%  (holds {1} Wh, it held {2} Wh when new)", health, (now / 1000).ToString("N1"), (design / 1000).ToString("N1")));
            }
            else if (stat == null && !Env.IsAdmin) s.Add(L.T("Battery health"), L.T("Start the tool with admin rights to read it"));
            List<ManagementBaseObject> cyc = Query(@"root\wmi", "SELECT * FROM BatteryCycleCount");
            long cycles = cyc == null ? 0 : cyc.Sum(x => Num(x, "CycleCount") ?? 0);
            if (cycles > 0) s.Add(L.T("Charge cycles"), cycles.ToString("N0"));
            long chem = Num(b, "Chemistry") ?? 0;
            s.Add(L.T("Kind"), chem == 6 ? L.T("Lithium-ion") : chem == 8 ? L.T("Lithium-polymer") : chem == 5 ? L.T("Nickel-metal hydride") : null);
            if (stat != null && stat.Count > 0) s.Add(L.T("Made by"), Clean(S(stat[0], "ManufactureName")));
            if (health >= 0 && health < 60)
                s.Warnings.Add(L.F("The battery holds only {0}% of what it held when new. A new battery gives much longer time away from the charger.", health));
            rep.Sections.Add(s);
        }

        // ------------------------------------------------------------ Network
        static void AddNetwork(DeviceReport rep)
        {
            InfoSection s = new InfoSection(L.T("Network adapters"));
            foreach (ManagementBaseObject a in All("SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter = TRUE"))
            {
                string pnp = (S(a, "PNPDeviceID") ?? "").ToUpperInvariant();
                if (pnp.StartsWith(@"ROOT\") || pnp.StartsWith(@"SWD\") || pnp.StartsWith("BTH") || pnp.StartsWith("{")) continue;
                string label = S(a, "NetConnectionID") ?? L.T("Adapter");
                long status = Num(a, "NetConnectionStatus") ?? -1;
                bool? enabled = Bo(a, "NetEnabled");
                long speed = Num(a, "Speed") ?? 0;
                string state = enabled == false ? L.T("turned off")
                             : status == 2 ? (speed > 0 && speed < 1000000000000L ? L.F("connected at {0}", LinkSpeed(speed)) : L.T("connected"))
                             : status == 7 ? L.T("not connected")
                             : status >= 0 ? L.T("not connected") : null;
                s.Add(label, Join("  ·  ", Tidy(S(a, "Name")), state));
            }
            if (s.Rows.Count > 0) rep.Sections.Add(s);
        }

        // ------------------------------------------------------------ Sound
        static void AddSound(DeviceReport rep)
        {
            InfoSection s = new InfoSection(L.T("Sound"));
            foreach (ManagementBaseObject d in All("SELECT * FROM Win32_SoundDevice"))
            {
                string name = Tidy(S(d, "Name"));
                if (name == null) continue;
                string pnp = (S(d, "PNPDeviceID") ?? "").ToUpperInvariant();
                string l = name.ToLowerInvariant();
                string where = pnp.StartsWith("USB") ? "USB"
                             : pnp.StartsWith("BTH") ? "Bluetooth"
                             : pnp.StartsWith(@"ROOT\") || pnp.StartsWith(@"SWD\") ? L.T("Added by a program")
                             : l.Contains("display audio") || ((l.Contains("nvidia") || l.Contains("amd")) && l.Contains("high definition")) ? L.T("Through the screen (HDMI or DisplayPort)")
                             : L.T("Built in");
                long err = Num(d, "ConfigManagerErrorCode") ?? 0;
                s.Add(where, err != 0 ? L.F("{0}  ·  Windows reports a problem (code {1})", name, err) : name);
            }
            if (s.Rows.Count > 0) rep.Sections.Add(s);
        }
    }
}
