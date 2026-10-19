// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Where the app runs from, where it keeps its data, and the Windows folders the cleanup items work in.
using System;
using System.IO;
using System.Reflection;

namespace DiskCleanupHelper.App
{
    public static class AppPaths
    {
        // The user's folders. Test switches can point them elsewhere (/userlocal, /userroaming, /userprofile).
        public static string LocalAppData, RoamingAppData, UserProfile;

        // Windows: "C:", "C:\Windows", "C:\ProgramData".
        public static string SysDrive, WindowsDir, ProgramData;

        // The app's own files.
        public static string DataDir, HistoryFile, DeletedCsv, InstallDir, ExePath, ExeDir;

        public static string Root { get { return SysDrive + "\\"; } }

        public static void Init(AppOptions o)
        {
            LocalAppData = !string.IsNullOrEmpty(o.UserLocal) ? o.UserLocal : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            RoamingAppData = !string.IsNullOrEmpty(o.UserRoaming) ? o.UserRoaming : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            UserProfile = !string.IsNullOrEmpty(o.UserProfile) ? o.UserProfile : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            SysDrive = Environment.GetEnvironmentVariable("SystemDrive");
            if (string.IsNullOrEmpty(SysDrive)) SysDrive = "C:";
            WindowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (string.IsNullOrEmpty(WindowsDir)) WindowsDir = Path.Combine(Root, "Windows");
            ProgramData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (string.IsNullOrEmpty(ProgramData)) ProgramData = Path.Combine(Root, "ProgramData");
            DataDir = Path.Combine(LocalAppData, "DiskCleanupHelper");
            HistoryFile = Path.Combine(DataDir, "cleanup-history.log");
            DeletedCsv = Path.Combine(DataDir, "deleted-update-files.csv");
            InstallDir = Path.Combine(LocalAppData, @"Programs\Disk Cleanup Helper");
            ExePath = Assembly.GetExecutingAssembly().Location;
            ExeDir = Path.GetDirectoryName(ExePath);
        }
    }
}
