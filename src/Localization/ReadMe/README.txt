DISK CLEANUP HELPER
===================

A free, open-source helper for Windows 10 and Windows 11.
It shows what fills your disks and frees up space safely.
It is one small program file. It sends nothing about your PC; it only asks GitHub whether a new version exists.


WHAT IT DOES
------------
Clean up      Safe cleanup of browser and app caches, temp files, old update files,
              Windows leftovers and more. Every item explains what you keep and what you lose.
Analyze disk  Shows the biggest folders (double-click to look inside) and the biggest files
              on any drive, so you can find what takes your space.
Disk health   Shows whether each disk and drive is healthy and how full it is, with a live
              temperature and history charts, and starts Windows' own error scan.
SMART details The full self-monitoring data of each disk.
Disk performance  Live disk activity and a speed test.
Device details    Windows, processor, memory, graphics, screens, storage, battery, network
                  and sound, like Windows' DirectX Diagnostic Tool. Copy it or save it as a text file.
Tips & FAQ    Simple habits that keep your disk from filling up, and answers to common questions.
About         Install it on your PC with a Start menu entry and a desktop shortcut, or uninstall it.
              Check for updates.


HOW TO START
------------
1. Double-click DiskCleanupHelper.exe.
2. Windows asks "Do you want to allow this app to make changes?". Click Yes.
   If you click No, it still opens, but the items that need admin rights are grey.
3. If Windows shows "Windows protected your PC", click "More info", then "Run anyway".
   This can appear for new programs that are not yet known to Microsoft.
4. Language: click the language button (the globe) at the top right and pick a language. The window
   switches to that language right away and remembers your choice.
5. Light or dark: click the "Dark mode" / "Light mode" button at the top right. The app remembers your choice.
6. To keep it: open the About tab and click "Install on this PC".
   It then appears in the Start menu, on the desktop and in Settings > Apps.
   No admin rights are needed to install.


HOW TO USE
----------
Clean up tab:
1. Close programs you do not need, especially browsers, chat apps, code editors and games.
2. Sizes are measured by themselves when the app opens. It takes a minute or two. Click "Stop" to stop,
   or "Measure sizes" to measure again.
3. Each group shows its total size. Click a group's title to fold it away or open it again;
   the app remembers which groups you folded.
4. Click any item to read what it is, where it is, and what you keep or lose.
5. Tick the box of each item you want to clean, or click "Select all safe".
   Each group's title shows how many of its items are selected.
6. Click "Clean selected". It shows the exact list and asks before deleting anything.

Analyze disk tab:
1. Pick a drive (or "My user folder only") and click "Scan". A whole drive can take a few minutes.
2. Left side: the biggest folders. Double-click a folder to look inside, click "Up" to go back.
3. Right side: the biggest files. Select files and choose "Move to Recycle Bin" or "Delete permanently".
   Windows and program files are protected and cannot be deleted here.

Disk health tab:
- Shows each disk (with temperature, wear and error counts when started with admin rights)
  and how full each drive is.
- The temperature updates by itself every few seconds while the tab is open.
- Point at a history chart to see the exact value and when it was saved.
- "Scan for errors" runs Windows' own read-only check of the Windows drive.
- "Optimize drives" opens Windows' own drive optimizer.

Disk performance tab:
- Shows how busy the chosen disk is right now, with charts of the last two minutes.
  Point at a chart to see the exact values.
- "Run speed test" measures how fast the disk can really write and read. The result is saved,
  so you can compare later.


WHAT THE CLEANUP GROUPS MEAN
----------------------------
Safe          Caches and temp files. Programs rebuild them by themselves.
              Browser and app caches keep your passwords, bookmarks, history and logins.
Admin         Old update files of installed programs (keeps the newest 3 of each),
              and Microsoft's own Windows component cleanup.
Your call     You lose something small: old project history, downloaded songs,
              video previews, crash dump files, or what is in the Recycle Bin.
Setting       Windows switches that free space: hibernation, Reserved Storage, CompactOS,
              and Storage Sense.
Info only     Big things the tool only shows and opens, never deletes: previous Windows
              installation, biggest programs, System Restore, Docker, WSL, Downloads.

Only items that exist on your PC are listed.


SAFETY
------
- Nothing is deleted until you select it and confirm.
- Files deleted by the Clean up tab do not go to the Recycle Bin and cannot be restored.
- The Clean up tab never touches your documents, photos, music, videos, desktop or downloads.
- It asks you to close a browser or app before cleaning its cache.
- It warns you if the PC is low on memory before measuring, scanning or cleaning.
- It never follows folder links into other folders.
- Never delete files in the Windows, WinSxS or Installer folders by hand.


HISTORY
-------
What was cleaned is saved on this PC only, in:
%LOCALAPPDATA%\DiskCleanupHelper\cleanup-history.log
Open it from the About tab.


UPDATES
-------
When it starts, the app asks its GitHub page whether a new version exists (at most once a day).
A new version is downloaded in the background and checked (SHA256 and version number) before it is used.
A bar then offers "Restart now". If you choose "Later", the update is installed when you close the app.
To turn the automatic check off, untick it in the About tab. "Check for updates" there still works.
Nothing about your PC is sent.


UNINSTALL
---------
Settings > Apps > Disk Cleanup Helper > Uninstall, or click "Uninstall" in the About tab.
If you never installed it, just delete DiskCleanupHelper.exe.


UNDO A WINDOWS SETTING
----------------------
Open Command Prompt as administrator and run the line you need:
    powercfg /h on                                          (hibernation back on)
    DISM /Online /Set-ReservedStorageState /State:Enabled   (Reserved Storage back on)
    compact.exe /compactos:never                            (stop compressing Windows files)


REQUIREMENTS
------------
Windows 10 or Windows 11. Nothing else to install: it uses the .NET Framework built into Windows.


LICENSE AND AUTHOR
------------------
Disk Cleanup Helper by Philip Droubi - https://github.com/Philip-Droubi
Copyright (C) 2026 Philip Droubi.
Free and open-source software: you may use, change and share it under the GNU General Public License v3
with additional terms (section 7): see LICENSE.txt and NOTICE.txt in the download, or "License and terms" in the About tab.
Source code: https://github.com/Philip-Droubi/disk-cleanup-helper
Contact: https://philip-droubi.github.io/Philip_Droubi_portfolio/ and https://www.linkedin.com/in/philip-droubi/


DISCLAIMER
----------
This program is provided as is, without warranty of any kind. Use it at your own risk.
Make sure important files are backed up before cleaning any computer.
