// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Facts about this PC and Windows: admin rights, memory, system tools.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.Common
{
    public static class Env
    {
        public static readonly bool IsAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX() { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)); }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        // availGB = free memory, commitGB = room left including the page file
        public static void GetMemory(out double availGB, out double commitGB)
        {
            MEMORYSTATUSEX m = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(m))
            {
                availGB = m.ullAvailPhys / 1073741824.0;
                commitGB = m.ullAvailPageFile / 1073741824.0;
            }
            else { availGB = 99; commitGB = 99; }
        }

        public static bool IsLowMemory()
        {
            double a, c;
            GetMemory(out a, out c);
            return a < 0.5 || c < 1.5;
        }

        public static void EmptyRecycleBin()
        {
            // no confirmation, no progress window, no sound
            SHEmptyRecycleBin(IntPtr.Zero, null, 0x7);
        }

        public static string SysTool(string exe) { return Path.Combine(AppPaths.WindowsDir, "System32", exe); }

        public static int WindowsBuild
        {
            get
            {
                try
                {
                    using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    {
                        int b;
                        if (k != null && int.TryParse(Convert.ToString(k.GetValue("CurrentBuildNumber")), out b)) return b;
                    }
                }
                catch { }
                return Environment.OSVersion.Version.Build;
            }
        }

        public static string QuoteArg(string s)
        {
            if (s == null) return "\"\"";
            int n = 0;
            for (int i = s.Length - 1; i >= 0 && s[i] == '\\'; i--) n++;
            return "\"" + s.Replace("\"", "\\\"") + new string('\\', n) + "\"";
        }

        // Starts this program again with admin rights. Returns false if the user said No.
        public static bool TryRelaunchElevated()
        {
            string args = "/elevated /userlocal " + QuoteArg(AppPaths.LocalAppData) + " /userroaming " + QuoteArg(AppPaths.RoamingAppData) + " /userprofile " + QuoteArg(AppPaths.UserProfile);
            ProcessStartInfo psi = new ProcessStartInfo(AppPaths.ExePath, args);
            psi.UseShellExecute = true;
            psi.Verb = "runas";
            try { Process.Start(psi); return true; }
            catch (Win32Exception) { return false; }
        }

        // Runs a Windows tool without a window and returns what it printed.
        public static string RunHidden(string file, string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(file, args);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (Process p = Process.Start(psi))
                {
                    string o = p.StandardOutput.ReadToEnd();
                    string e = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    return (o + "\n" + e).Trim();
                }
            }
            catch (Exception ex) { return L.F("Could not run {0}: {1}", Path.GetFileName(file), ex.Message); }
        }

        // Runs a long Windows tool in its own visible window and waits until it finishes.
        public static void RunVisibleAndWait(string commandLine)
        {
            ProcessStartInfo psi = new ProcessStartInfo(SysTool("cmd.exe"),
                "/c " + commandLine + " & echo. & echo Finished. This window closes in 15 seconds. & timeout /t 15 /nobreak >nul");
            psi.UseShellExecute = true;
            using (Process p = Process.Start(psi)) { p.WaitForExit(); }
        }

        // Runs a Windows tool in its own window that stays open so the result can be read.
        public static void RunVisibleKeepOpen(string commandLine)
        {
            ProcessStartInfo psi = new ProcessStartInfo(SysTool("cmd.exe"), "/k " + commandLine);
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        public static void OpenUri(string uri)
        {
            try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); } catch { }
        }
    }
}
