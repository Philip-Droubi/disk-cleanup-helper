// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// The text of the Tips & FAQ tab. Edit, add or remove entries here.
// Tip tags: SPACE, SPEED, HEALTH, SAFETY (each has its own color).
using DiskCleanupHelper.Localization;

namespace DiskCleanupHelper.UI
{
    public class Tip
    {
        public string Tag, Title, Text;
        public Tip(string tag, string title, string text) { Tag = tag; Title = title; Text = text; }
    }

    public class Faq
    {
        public string Question, Answer;
        public Faq(string question, string answer) { Question = question; Answer = answer; }
    }

    public static class HelpContent
    {
        public static Tip[] Tips { get { return new[] {
            new Tip("SPACE", L.T("Turn on Storage Sense"),
                L.T("Windows can delete temporary files and old Recycle Bin items by itself. Settings > System > Storage > Storage Sense.")),
            new Tip("SPACE", L.T("Empty the Recycle Bin"),
                L.T("Files you delete still use space until the Recycle Bin is emptied. Check it from time to time.")),
            new Tip("SPACE", L.T("Uninstall what you no longer use"),
                L.T("Old games and programs are often the biggest space users. Settings > Apps, then sort the list by size.")),
            new Tip("SPACE", L.T("Keep 10 to 15 percent free"),
                L.T("When the Windows drive is almost full, Windows slows down and updates can fail to install.")),
            new Tip("SPACE", L.T("Move big personal files"),
                L.T("Videos, photos and old downloads can live on another drive, an external disk or cloud storage.")),
            new Tip("SPACE", L.T("Let cloud files stay in the cloud"),
                L.T("With OneDrive, right-click a folder and choose \"Free up space\". Files stay visible but only download when you open them.")),
            new Tip("SPACE", L.T("Tidy your Downloads folder"),
                L.T("Installers and old downloads pile up. Sort the folder by size and delete what you no longer need.")),
            new Tip("SPEED", L.T("Close programs you are not using"),
                L.T("When memory runs out, Windows makes its page file bigger, which uses disk space and can make programs crash.")),
            new Tip("SPEED", L.T("Restart your PC now and then"),
                L.T("A restart clears memory, finishes updates, and lets Windows shrink its page file back to normal.")),
            new Tip("SPEED", L.T("Turn off apps you do not need at startup"),
                L.T("Fewer apps starting with Windows means a faster start and more free memory. Task Manager > Startup apps.")),
            new Tip("SPEED", L.T("Do not defragment an SSD"),
                L.T("SSDs do not need it. Windows' \"Optimize drives\" knows the difference, so leave it on its automatic schedule.")),
            new Tip("HEALTH", L.T("Check disk health once a month"),
                L.T("Open the Disk health tab. Each visit saves a reading, so the history charts show if something changes.")),
            new Tip("HEALTH", L.T("Watch for growing bad sectors"),
                L.T("A few reallocated or pending sectors are common. If the number keeps growing, back up and plan to replace the disk.")),
            new Tip("HEALTH", L.T("Keep your PC cool"),
                L.T("Heat shortens a disk's life. Hard disks should stay under about 50 °C and SSDs under about 70 °C. Keep vents clean.")),
            new Tip("SAFETY", L.T("Back up what matters"),
                L.T("Disks can fail without warning. Keep a copy of important files on another drive or in the cloud.")),
            new Tip("SAFETY", L.T("Never delete Windows files by hand"),
                L.T("Folders like Windows, WinSxS and Installer break Windows if cleaned by hand. Use the Clean up tab instead."))
        }; } }

        public static string[] Safety { get { return new[] {
            L.T("Nothing is deleted until you select it and confirm."),
            L.T("Cleanup items only touch caches, temp files and old update files. Your documents, photos, music, videos, desktop and downloads are never cleaned automatically."),
            L.T("Browser and app caches keep your passwords, bookmarks, history and logins."),
            L.T("It asks you to close a browser or app before cleaning its cache."),
            L.T("It warns you if the PC is low on memory before measuring, scanning or cleaning."),
            L.T("Windows and program files are protected in the Analyze disk tab."),
            L.T("It never follows folder links into other folders."),
            L.T("It sends nothing about your PC. It only asks GitHub whether a new version exists, and you can turn that off in the About tab. A history of what was cleaned is saved on this PC only.")
        }; } }

        public static Faq[] Faqs { get { return new[] {
            new Faq(L.T("Is it safe to clean what this app suggests?"),
                L.T("Items in the \"Safe to clean\" group are caches and temporary files that programs rebuild by themselves. Items in \"Your call\" remove something small, so read them first. Items marked \"Info only\" are never deleted by the app; it only shows them and opens their location.")),
            new Faq(L.T("Will cleaning my browser cache log me out of websites?"),
                L.T("No. The cache cleanup keeps cookies, saved passwords, bookmarks and history. Very rarely a site may ask you to log in again, and your browser fills in the saved password.")),
            new Faq(L.T("Why did I get back less space than the size shown?"),
                L.T("Files that are in use are skipped. Temp cleanup keeps files from the last day. And the Windows component cleanup can only be measured while it runs. The log at the bottom shows exactly what was removed.")),
            new Faq(L.T("Why does my free space go down again after cleaning?"),
                L.T("Caches refill as you use your browser and apps, Windows downloads new updates, and when memory runs low Windows grows its page file. Cleaning again every few weeks, turning on Storage Sense and closing unused programs keeps it under control.")),
            new Faq(L.T("Why does the Windows folder look so big in Analyze disk?"),
                L.T("Windows keeps some files in two places at once, so they are counted twice when sizes are added up. The real size is smaller. Never delete anything in the Windows folder by hand.")),
            new Faq(L.T("What does the health percentage mean?"),
                L.T("It is an estimate from the disk's own self-monitoring data (SMART, or the health log on NVMe SSDs). 100% means no problems or wear were found. It is tuned to land close to tools like Hard Disk Sentinel, but every tool uses its own formula, so numbers can differ slightly.")),
            new Faq(L.T("My SSD shows less than 100% health. Should I worry?"),
                L.T("SSDs wear a little with every write, and the health shows how much of the rated write life is used. Many SSDs keep working well past their rating. Keep backups, and plan to replace the SSD when health gets low or problems appear.")),
            new Faq(L.T("What are pending and reallocated sectors?"),
                L.T("They are small spots the disk could not read reliably. Reallocated sectors have already been replaced by spare ones. Pending sectors are waiting to be checked. A few are common on older disks. If the numbers keep growing, back up your files and consider replacing the disk.")),
            new Faq(L.T("What temperature is normal for a disk?"),
                L.T("Hard disks usually run at 25 to 45 °C. SSDs usually run at 30 to 60 °C, and NVMe SSDs can reach 70 °C under heavy load. Consistently higher temperatures shorten a disk's life.")),
            new Faq(L.T("Why does the app ask for admin rights?"),
                L.T("Admin rights are needed to clean Windows' own temp and update files and to read the disks' health data. If you click No, the app still works, and the items that need admin rights are greyed out.")),
            new Faq(L.T("Why does Windows say \"Windows protected your PC\"?"),
                L.T("Windows shows this for new programs it does not know yet. Click \"More info\" and then \"Run anyway\". The warning goes away for good once the program is signed and known to Microsoft.")),
            new Faq(L.T("Can I undo a cleanup?"),
                L.T("Files deleted by the Clean up tab do not go to the Recycle Bin, so they cannot be restored. Files you move to the Recycle Bin from the Analyze disk tab can be restored until you empty the Recycle Bin. Windows settings can be switched back; see the Read me in the About tab.")),
            new Faq(L.T("Does the speed test harm my disk?"),
                L.T("No. It writes one temporary test file of 512 MB to 1 GB, reads it back, and deletes it. That is less than a normal day of use.")),
            new Faq(L.T("What are hibernation, Reserved Storage and CompactOS?"),
                L.T("Hibernation keeps a file as big as part of your memory so the PC can resume where it stopped. Reserved Storage is about 7 GB Windows keeps aside for updates. CompactOS stores Windows files compressed. Each one can free several GB, and each can be switched back later.")),
            new Faq(L.T("Where can I see my PC's details, like the Windows version, processor, memory and graphics?"),
                L.T("Open the Device details tab. It shows what Windows' DirectX Diagnostic Tool (dxdiag) shows, and also the memory slots, the page file, the screens and the battery health. Click \"Copy all\" or \"Save as text file\" to send the details to someone who helps you.")),
            new Faq(L.T("Where are my history files saved?"),
                L.T("In your user folder: %LOCALAPPDATA%\\DiskCleanupHelper. It holds the cleanup history, the disk health and temperature history, and the speed test results. Open them from the About and Disk health tabs.")),
            new Faq(L.T("Does the app send any of my data anywhere?"),
                L.T("No. It sends nothing about your PC. The only connection it makes is to ask GitHub whether a new version exists, at most once a day. You can turn that off in the About tab.")),
            new Faq(L.T("How do updates work?"),
                L.T("When a new version is out, the app downloads it in the background and checks it: its SHA256 and version number must match the release. A bar then offers \"Restart now\". If you choose \"Later\", the update is installed when you close the app. You can turn off the automatic check in the About tab; the \"Check for updates\" button there still works.")),
            new Faq(L.T("How do I uninstall it?"),
                L.T("Settings > Apps > Disk Cleanup Helper > Uninstall, or the Uninstall button in the About tab. If you never installed it, just delete the program file."))
        }; } }
    }
}
