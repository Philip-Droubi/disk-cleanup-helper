// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Product name and version.
using System;
using System.Reflection;

namespace DiskCleanupHelper.App
{
    public static class AppInfo
    {
        public const string Title = "Disk Cleanup Helper";
        public const string ExeName = "DiskCleanupHelper.exe";
        public const string Publisher = "Philip Droubi";   // Settings > Apps; a changed version puts its own name here

        public static string Version
        {
            get
            {
                Assembly a = Assembly.GetExecutingAssembly();
                object[] attrs = a.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                if (attrs.Length > 0) return ((AssemblyInformationalVersionAttribute)attrs[0]).InformationalVersion;
                Version v = a.GetName().Version;
                return v.Major + "." + v.Minor;
            }
        }

        // Shown in the window title, for example "v1.0".
        public static string DisplayVersion { get { return "v" + Version; } }

        // "2026", or "2026-2028" from 2027 on: build.ps1 writes it next to the exe's copyright, so the About tab matches.
        public static string CopyrightYears
        {
            get
            {
                object[] attrs = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);
                foreach (AssemblyMetadataAttribute m in attrs)
                    if (m.Key == "CopyrightYears" && !string.IsNullOrEmpty(m.Value)) return m.Value;
                return "2026";
            }
        }
    }
}
