// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// Command-line options, including the test switches used to check layouts.
using System;
using System.Globalization;

namespace DiskCleanupHelper.App
{
    public class AppOptions
    {
        public bool Install, Uninstall, Elevated, NoElevate, SampleWd, Loading, ManyPoints, Measure, SelectSafe, LangMenu, CheckLayout, Demo, UpdateBar;
        public string Fold;         // testing: these Clean up groups folded for this run (not saved), e.g. "updates,yourcall"
        public double Hover = -1;   // testing: snapshot with each chart's hover bubble at this share of its width
        public string SmartSamplePath, DeviceReportPath, TempsPath;
        public string Theme;   // testing: "light" or "dark" for this run only (not saved)
        public string Lang;    // "/lang ar": this language for this run only (not saved); also used when restarting after a language change
        public string SnapshotPath, TestScanRoot, UserLocal, UserRoaming, UserProfile, SelfTestPath, SpeedTestFolder, SpeedTestReport;
        public string UpdatedFrom;   // "/updated 1.0": this start follows a self-update from that version
        public string UpdateFeed;    // testing: a local file shaped like GitHub's release answer (Updater.FeedFile)
        public int SnapshotTab, SnapshotScroll, SnapshotWidth;
        public int TestMinMB = 1;

        public static AppOptions Parse(string[] args)
        {
            AppOptions o = new AppOptions();
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLowerInvariant();
                string next = (i + 1 < args.Length) ? args[i + 1] : null;
                switch (a)
                {
                    case "/install": o.Install = true; break;
                    case "/uninstall": o.Uninstall = true; break;
                    case "/elevated": o.Elevated = true; break;
                    case "/noelevate": o.NoElevate = true; break;
                    case "/snapshot": o.SnapshotPath = next; i++; break;
                    case "/tab": o.SnapshotTab = ParseInt(next, 0); i++; break;
                    case "/scroll": o.SnapshotScroll = ParseInt(next, 0); i++; break;
                    case "/testscan": o.TestScanRoot = next; i++; break;
                    case "/testmin": o.TestMinMB = ParseInt(next, 1); i++; break;
                    case "/selftest": o.SelfTestPath = next; i++; break;
                    case "/samplewd": o.SampleWd = true; break;
                    case "/loading": o.Loading = true; break;   // testing: snapshot of the Disk health / SMART loading view
                    case "/manypoints": o.ManyPoints = true; break;   // testing: 3000 made-up readings in the history charts
                    case "/measure": o.Measure = true; break;   // testing: snapshot of Clean up after measuring every item
                    case "/selectsafe": o.SelectSafe = true; break;   // testing: snapshot with the safe items selected
                    case "/langmenu": o.LangMenu = true; break;       // testing: snapshot of the opened language menu
                    case "/layoutcheck": o.CheckLayout = true; break; // testing: also list cut-off texts in <snapshot>.txt
                    case "/width": o.SnapshotWidth = ParseInt(next, 0); i++; break;   // testing: snapshot at this window width
                    case "/demo": o.Demo = true; break;               // testing: snapshot with made-up data (README and website pictures)
                    case "/updated": o.UpdatedFrom = next ?? "?"; i++; break;
                    case "/updatefeed": o.UpdateFeed = next; i++; break;
                    case "/updatebar": o.UpdateBar = true; break;     // testing: snapshot with the "ready, restart" bar showing
                    case "/fold": o.Fold = next ?? ""; i++; break;
                    case "/hover":
                        double hv;
                        if (double.TryParse(next, NumberStyles.Float, CultureInfo.InvariantCulture, out hv)) o.Hover = hv;
                        i++;
                        break;
                    case "/smartsample": o.SmartSamplePath = next; i++; break;
                    case "/devicereport": o.DeviceReportPath = next; i++; break;
                    case "/temps": o.TempsPath = next; i++; break;   // testing: full-check vs quick temperature reads
                    case "/theme": o.Theme = (next ?? "").ToLowerInvariant(); i++; break;
                    case "/lang": o.Lang = next; i++; break;
                    case "/speedtest":
                        o.SpeedTestFolder = next;
                        o.SpeedTestReport = (i + 2 < args.Length) ? args[i + 2] : null;
                        i += 2;
                        break;
                    case "/userlocal": o.UserLocal = next; i++; break;
                    case "/userroaming": o.UserRoaming = next; i++; break;
                    case "/userprofile": o.UserProfile = next; i++; break;
                }
            }
            if (o.Demo && o.SnapshotPath != null && string.IsNullOrEmpty(o.UserProfile))
            {
                // demo pictures show a made-up user, never the real user name (paths in the item details)
                o.UserProfile = @"C:\Users\Alex";
                o.UserLocal = o.UserProfile + @"\AppData\Local";
                o.UserRoaming = o.UserProfile + @"\AppData\Roaming";
            }
            return o;
        }

        static int ParseInt(string s, int fallback)
        {
            int v;
            return int.TryParse(s, out v) ? v : fallback;
        }
    }
}
