// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Installs for the current user (Start menu, desktop, Settings > Apps) and uninstalls.
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;
using DiskCleanupHelper.App;
using DiskCleanupHelper.Common;
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.Install
{
    public static class Installer
    {
        const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\DiskCleanupHelper";
        const string LinkName = "Disk Cleanup Helper.lnk";

        public static string InstalledExe { get { return Path.Combine(AppPaths.InstallDir, AppInfo.ExeName); } }
        public static bool IsInstalled { get { return File.Exists(InstalledExe); } }

        public static bool RunningFromInstallDir
        {
            get
            {
                try { return string.Equals(Path.GetFullPath(AppPaths.ExePath), Path.GetFullPath(InstalledExe), StringComparison.OrdinalIgnoreCase); }
                catch { return false; }
            }
        }

        static string StartMenuLink { get { return Path.Combine(AppPaths.RoamingAppData, @"Microsoft\Windows\Start Menu\Programs", LinkName); } }

        static string DesktopLink
        {
            get
            {
                string d = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                return string.IsNullOrEmpty(d) ? null : Path.Combine(d, LinkName);
            }
        }

        static void CreateShortcut(string lnkPath, string target)
        {
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(t);
            try
            {
                object lnk = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
                Type lt = lnk.GetType();
                lt.InvokeMember("TargetPath", BindingFlags.SetProperty, null, lnk, new object[] { target });
                lt.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, lnk, new object[] { Path.GetDirectoryName(target) });
                lt.InvokeMember("IconLocation", BindingFlags.SetProperty, null, lnk, new object[] { target + ",0" });
                lt.InvokeMember("Description", BindingFlags.SetProperty, null, lnk, new object[] { L.T("Free up disk space safely") });
                lt.InvokeMember("Save", BindingFlags.InvokeMethod, null, lnk, null);
                Marshal.FinalReleaseComObject(lnk);
            }
            finally { Marshal.FinalReleaseComObject(shell); }
        }

        // Returns null when it worked, otherwise a message explaining what went wrong.
        public static string Install()
        {
            try
            {
                Directory.CreateDirectory(AppPaths.InstallDir);
                if (!RunningFromInstallDir) File.Copy(AppPaths.ExePath, InstalledExe, true);
                string menuDir = Path.GetDirectoryName(StartMenuLink);
                if (Directory.Exists(menuDir)) CreateShortcut(StartMenuLink, InstalledExe);
                string desk = DesktopLink;
                if (desk != null && Directory.Exists(Path.GetDirectoryName(desk))) CreateShortcut(desk, InstalledExe);
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(UninstallKey))
                {
                    k.SetValue("DisplayName", AppInfo.Title);
                    k.SetValue("DisplayVersion", AppInfo.Version);
                    k.SetValue("Publisher", AppInfo.Publisher);
                    k.SetValue("DisplayIcon", InstalledExe + ",0");
                    k.SetValue("InstallLocation", AppPaths.InstallDir);
                    k.SetValue("UninstallString", "\"" + InstalledExe + "\" /uninstall");
                    k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                    k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    k.SetValue("EstimatedSize", (int)Math.Ceiling(new FileInfo(InstalledExe).Length / 1024.0), RegistryValueKind.DWord);
                }
                return null;
            }
            catch (IOException ex)
            {
                return L.F("Could not copy the program. If the installed Disk Cleanup Helper is open, close it first and try again.\r\n\r\n{0}", ex.Message);
            }
            catch (Exception ex) { return ex.Message; }
        }

        // After a self-update: the version and size shown in Settings > Apps, when this is the installed copy. The entry
        // is only changed when it points at this install folder.
        public static void UpdateRegisteredVersion()
        {
            if (!RunningFromInstallDir) return;
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(UninstallKey, true))
                {
                    if (k == null) return;
                    string location = k.GetValue("InstallLocation") as string;
                    if (string.IsNullOrEmpty(location) ||
                        !string.Equals(Path.GetFullPath(location).TrimEnd('\\'), Path.GetFullPath(AppPaths.InstallDir).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) return;
                    k.SetValue("DisplayVersion", AppInfo.Version);
                    k.SetValue("EstimatedSize", (int)Math.Ceiling(new FileInfo(InstalledExe).Length / 1024.0), RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        public static void InstallWithMessages(Window owner)
        {
            string err = Install();
            if (err == null)
                Show(owner, L.T("Disk Cleanup Helper is installed.\r\n\r\nOpen it from the Start menu or the desktop shortcut called Disk Cleanup Helper.\r\n\r\nTo remove it later: Settings > Apps > Disk Cleanup Helper > Uninstall."), MessageBoxImage.Information);
            else
                Show(owner, L.F("Install failed:\r\n\r\n{0}", err), MessageBoxImage.Error);
        }

        // Returns true when it was removed.
        public static bool UninstallWithMessages(Window owner, bool exitAfter)
        {
            string ask = L.F("Remove Disk Cleanup Helper from this PC?\r\n\r\nYour cleanup history stays in:\r\n{0}", AppPaths.DataDir);
            MessageBoxResult ans = owner != null
                ? MessageBox.Show(owner, ask, AppInfo.Title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No, L.MsgOptions)
                : MessageBox.Show(ask, AppInfo.Title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No, L.MsgOptions);
            if (ans != MessageBoxResult.Yes) return false;
            foreach (string l in new[] { StartMenuLink, DesktopLink })
            {
                try { if (l != null && File.Exists(l)) File.Delete(l); } catch { }
            }
            try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false); } catch { }
            string dir = AppPaths.InstallDir;
            if (Path.IsPathRooted(dir) && dir.EndsWith(@"\Programs\Disk Cleanup Helper", StringComparison.OrdinalIgnoreCase) && Directory.Exists(dir))
            {
                bool removedNow = false;
                if (!RunningFromInstallDir) { try { Directory.Delete(dir, true); removedNow = true; } catch { } }
                if (!removedNow)
                {
                    // the program file is in use: remove the folder a few seconds after this window closes
                    ProcessStartInfo psi = new ProcessStartInfo(Env.SysTool("cmd.exe"), "/c timeout /t 4 /nobreak >nul & rd /s /q \"" + dir + "\"");
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                    try { Process.Start(psi); } catch { }
                }
            }
            Show(owner, L.T("Disk Cleanup Helper was removed from this PC."), MessageBoxImage.Information);
            return true;
        }

        static void Show(Window owner, string text, MessageBoxImage icon)
        {
            if (owner != null) MessageBox.Show(owner, text, AppInfo.Title, MessageBoxButton.OK, icon, MessageBoxResult.OK, L.MsgOptions);
            else MessageBox.Show(text, AppInfo.Title, MessageBoxButton.OK, icon, MessageBoxResult.OK, L.MsgOptions);
        }
    }
}
