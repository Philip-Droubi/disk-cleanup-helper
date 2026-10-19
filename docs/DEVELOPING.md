# Disk Cleanup Helper — developer guide

A Windows 10/11 desktop app (C# WPF, .NET Framework 4.8) that cleans disks safely, analyzes disk space and shows disk
health, SMART data, disk performance and device details. 18 languages (Arabic, Persian and Urdu right-to-left),
light and dark theme.

`README.md` in the project root is the page visitors see on GitHub. The user guide shipped with the app is
`src/Localization/ReadMe/README.txt` (one per language). Using an AI assistant on this code? Point it at this file.

## License

Disk Cleanup Helper is free software under the GNU General Public License v3 only (`LICENSE`), with the additional
terms in `NOTICE` that section 7 of the license allows. The app shows the author attribution
**"Disk Cleanup Helper by Philip Droubi - https://github.com/Philip-Droubi"** in its legal notices (About tab) and in its
read-me files, and NOTICE asks that:

- every copy and every changed version keeps `NOTICE` and the copyright header of each source file;
- an unchanged copy keeps that attribution;
- a changed version shows **"Based on Disk Cleanup Helper by Philip Droubi - https://github.com/Philip-Droubi"** in its
  place, is marked as changed, and does not present itself as the original.

The About tab's "License and terms" link shows `NOTICE` and `LICENSE` from inside the exe, so every copy can show them.

New source files start with the same two-line header as the others. If you wrote the file, you may put your own name
and year in the first line; keep the second line.

```
// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
```

The `.lang` files carry the same notice as a `#` comment line.

## Build

You need Windows 10 or 11. Nothing else to install: the build uses Windows PowerShell 5.1 and the C# compiler and WPF
assemblies that ship with the .NET Framework 4.8 (`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`). No Visual
Studio, SDK or NuGet.

| Command | Result |
|---|---|
| `build.cmd -Rebuild` | builds the current version (from `version.txt`) into `builds\v<version>\`: the exe, the read-me files, `LICENSE.txt`, `NOTICE.txt` and `DiskCleanupHelper-v<version>.zip`. **Use this one for your own builds.** |
| `build.cmd` | builds the next version (1.0, 1.1, 1.2 ...) and writes it to `version.txt` |
| `build.cmd -Major` | builds the next major version (1.4 to 2.0) |
| `build.cmd -Publish` | maintainer only: publishes the build as a GitHub release of the repository set in `build.ps1` (`$repo`). Needs a logged-in `gh` (GitHub CLI) and `release-notes.txt` (first line `Version <x.y>`, then the "What's new" text) |

A `.0` version uses the short folder name (1.0 builds into `builds\v1\`). The build first runs the language check
(`tools\lang-check.ps1`) and stops on translation errors. If compiling fails, the errors are shown in red and nothing
is changed. Close the app before building: an open exe cannot be replaced.

**C# 5 only.** The Windows compiler does not support `$"..."`, `?.`, `nameof`, `=>` members, `out var`, tuples or other
C# 6+ features.

## Folders

```
build.cmd, build.ps1     build (version number in version.txt)
LICENSE, NOTICE          GNU GPL v3, and the copyright, the additional terms and third-party notices (both are
                         also built into the exe)
README.md                the GitHub page; its pictures are in docs/images/
docs/DEVELOPING.md       this guide
tools/                   lang-check.ps1 (run by the build), make-icon.ps1, download-stats.ps1
builds/, obj/            build output and temporary files (not in git; safe to delete)
src/
  App/                   start-up (Program), command-line options, paths, saved settings, the updater
  Common/                small shared helpers: formatting, safe file operations, facts about this PC
  Cleanup/               the cleanup items (Catalog.cs) and what finds their files (Finders.cs)
  Disks/                 disk health and SMART, health history, live activity, speed test, drive scanner
  Device/                the Device details report
  Install/               install / uninstall for the current user
  Localization/          L (translations), Languages/*.lang, ReadMe/README*.txt
  UI/                    the main window, split per tab (MainWindow.CleanupTab.cs, ...), theme, help texts
  UI/Controls/           reusable controls: chart, smooth and sideways scrolling, scrollbar corner fix
  Resources/             icon, manifest, embedded images
```

The website (https://philip-droubi.github.io/disk-cleanup-helper/) is built separately; only its built pages are in
this repository, on the `gh-pages` branch.

## Where to change things

| To change | Edit |
|---|---|
| a cleanup item (add, change, remove) | `src/Cleanup/Catalog.cs` |
| the Tips & FAQ texts | `src/UI/HelpContent.cs` |
| how the window looks | `src/UI/MainWindow.xaml` and the tab's `MainWindow.*.cs` file |
| a translation | `src/Localization/Languages/<code>.lang` (see Translation rules below) |
| colours | write the light colour (`B("#RRGGBB")` in code, `T_RRGGBB` in XAML); `Theme.cs` maps it for dark |
| the icon | replace `src/Resources/app.ico`, or redraw it with `tools/make-icon.ps1` |

Every text the user sees goes through `L.T` (a text), `L.F` (a text with `{0}` placeholders) or `L.P` (a plural) in
`src/Localization/L.cs`, so it can be translated. The English in the code is the key.

## Translations

Each language has a file `src/Localization/Languages/<code>.lang` with pairs of lines: `en:` holds the English exactly
as in the code, and the next line (`de:`, `ar:` ...) its translation. `=` as the whole translation keeps the English on
purpose. Plurals list every form the language needs, split by ` | ` (Arabic: zero | one | two | few | many | other;
Russian, Ukrainian, Polish: one | few | many; most others: one | other). `@rtl = yes` marks a right-to-left language.
`tools\lang-check.ps1` (run by every build) lists missing and unused texts in `obj\lang-todo-<code>.lang` and stops the
build on errors. Keep `tools\lang-check.ps1` saved as UTF-8 with BOM: Windows PowerShell 5.1 needs the BOM to read its
Arabic patterns.

### Translation rules

These apply to every `.lang` file and every `README.<code>.txt` in `src/Localization/ReadMe/`.

1. **Never edit `en:` lines by hand.** They must match the code exactly; the check lists what is missing or unused.
2. **Placeholders** `{0}`, `{1}`, `{0:N1}` ... all stay, exactly as written; their order may change.
3. **Digits:** always Western `0123456789`, in every language, Arabic included.
4. **Units stay English:** KB, MB, GB, TB, MB/s, GHz, Hz, Wh, °C, %. Write them right after the placeholder (`{0} GB`).
5. **The product name** "Disk Cleanup Helper" stays in English.
6. **Technical text stays as written:** paths, registry paths, command lines, file names, web addresses, versions,
   drive letters, and program and brand names. In right-to-left languages wrap a command or path inside a sentence in
   `<ltr>...</ltr>`.
7. **SMART and NVMe field names** stay in English; translate only headers and status words.
8. **`<br>`** line breaks stay where they are. No HTML or Markdown.
9. **Never soften a warning** ("permanently", "cannot be restored", "needs admin rights", "close it first").
10. **"Keeps" and "Loses"** lines keep their exact meaning.
11. **Windows names** as Windows shows them in that language (Settings, Storage Sense, Recycle Bin, Task Manager ...).
12. **The app's own names match everywhere:** a text that mentions a tab, group or button uses that exact translation.
13. **Do not add, drop or "improve" information.** If the English is wrong, fix it in the code first.
14. **Plain words and short sentences**, about as long as the English; buttons and tab names stay short.
15. **Right-to-left:** simple Modern Standard Arabic as in Windows (verbal nouns on buttons); Persian polite "شما",
    Urdu polite "آپ". Never end a line with an English value that ends in punctuation; prefer `  ·  ` to brackets
    around an English value; never glue an Arabic-script letter to Latin letters or digits.
16. **Text from Windows or the hardware** (disk models, file names, adapter names) is never translated.

After translating: build, fix every error and unintended warning, and take snapshots of all tabs in light and dark with
`/lang <code> /layoutcheck` at the normal width and with `/width 960` (see below).

## Checking a change

A normal start asks Windows for admin rights. The exe has test switches that save a picture of a tab and exit, so
layouts can be checked without clicking through the app:

```
DiskCleanupHelper.exe /noelevate /tab 2 /theme dark /lang ar /snapshot %TEMP%\health.png
```

| Switch | What it does |
|---|---|
| `/noelevate` | starts without asking for admin rights (items that need them are grey) |
| `/snapshot <file.png>` | saves a picture of the window and exits (the folder must exist) |
| `/tab <0-7>` | 0 Clean up, 1 Analyze disk, 2 Disk health, 3 SMART details, 4 Disk performance, 5 Device details, 6 Tips & FAQ, 7 About |
| `/theme light\|dark\|switch`, `/lang <code>` | theme and language for this run |
| `/scroll <px>`, `/width <px>` | scroll position; window width (`/width 960` is the narrowest window) |
| `/layoutcheck` | also writes `<file.png>.txt`: every text that is cut off (empty after the first line when all fits) |
| `/loading` | with `/tab 2` to `/tab 5`: pictures the loading view |
| `/measure`, `/selectsafe`, `/fold <groups>` | with `/tab 0`: measures every item, ticks the safe ones, folds groups |
| `/langmenu` | pictures the opened language menu |
| `/demo` | made-up drives, disks and sizes for public pictures (Clean up and Disk health tabs) |
| `/samplewd` | adds a sample WD disk to Disk health and SMART details |
| `/hover 0.7`, `/manypoints` | chart hover bubbles at 70% width; 3000 made-up history readings |
| `/testscan <folder> /testmin <MB>` | opens Analyze disk with that folder scanned |
| `/devicereport <file.txt>`, `/selftest <file.txt>`, `/smartsample <file.txt>`, `/temps <file.txt>` | text reports, no window |
| `/speedtest <folder> <report.txt>` | runs a small speed test (256 MB) in that folder and writes the result, no window |
| `/userprofile`, `/userlocal`, `/userroaming <folder>` | use another folder as the user profile, Local or Roaming AppData (to test cleanup items safely) |
| `/updatefeed <file or address>`, `/updatebar` | updater tests: read a local feed instead of GitHub; show the "restart to update" bar |

`src/App/AppOptions.cs` parses them all.

## Updates

The app asks GitHub for the newest release of `Updater.Repo` (`src/App/Updater.cs`) at most once a day. A release
counts only when its tag is a higher version, it has the exe on github.com, and its text has a `SHA256: <hash>` line
(`build.cmd -Publish` writes it). The download is checked against that hash and the exe's own version before it
replaces the running copy (the old one is kept as `.old.exe` until the new one starts). Copies inside `builds\` never
update themselves. The `/updatefeed` test switch reads a local feed instead and is meant for testing only: whoever can
start the app with it could already run any program as that user.

## Forks and changed versions

If you publish your own changed version:

- change `Updater.Repo` in `src/App/Updater.cs`, `$repo` in `build.ps1` and the default repository in
  `tools/download-stats.ps1` to your repository (or turn the update check off), otherwise your build updates itself
  from the original releases;
- give it another name or mark it clearly as a changed version (NOTICE, term 2), and put your own name in
  `AppInfo.Publisher` (`src/App/AppInfo.cs`) and in `AssemblyCompany` in `build.ps1` (also `AssemblyTitle` and
  `AssemblyProduct` if you rename it). In `AssemblyCopyright`, keep Philip Droubi and add your own name;
- change `UninstallKey` and `LinkName` in `src/Install/Installer.cs` (and the `\Programs\Disk Cleanup Helper` check in
  its uninstall code), and `DataDir` and `InstallDir` in `src/App/AppPaths.cs`, so your version installs, keeps its
  settings and downloads its updates apart from the original instead of replacing it;
- point the source links that do not follow `Updater.Repo` to your repository: the "Source code:" line in each
  `src/Localization/ReadMe/README*.txt` and the address shown on the "Source code on GitHub" card in the About tab;
- in the About tab (`src/UI/MainWindow.AboutTab.cs`), replace the "Made by Philip Droubi" heading with your own name.
  You may keep the portfolio and LinkedIn cards as a way to reach the original author, under a heading such as
  "Original author";
- replace the attribution "Disk Cleanup Helper by Philip Droubi - https://github.com/Philip-Droubi" (`Credit` in the
  About tab, and the LICENSE AND AUTHOR section of each read-me) with "Based on Disk Cleanup Helper by Philip Droubi -
  https://github.com/Philip-Droubi", as a clickable link where links work (NOTICE, term 1);
- keep `NOTICE` and the source file headers.

## Windows SmartScreen and Defender

The exe is a plain .NET program (no packer, no scripts inside) with proper version information, which keeps antivirus
false alarms unlikely. Windows SmartScreen may still show "Windows protected your PC" for any new unsigned program until
it is known. If you distribute builds:

1. Sign the exe with a code signing certificate from a certificate authority, installed in the Windows certificate
   store or as a .pfx file: create `signing.txt` next to `build.cmd` (see `signing.example.txt`) and every build is
   signed with `signtool.exe` from the Windows SDK. `signing.txt` is in `.gitignore`; never commit it. (Microsoft
   Trusted Signing needs signtool's `/dlib` and `/dmdf` options, which `build.ps1` does not pass.)
2. Submit each new version at https://www.microsoft.com/en-us/wdsi/filesubmission ("Software developer").
3. Share the zip through a website or cloud link; email services often block .exe files, even inside a zip.

## Contributing

Bugs and ideas: https://github.com/Philip-Droubi/disk-cleanup-helper/issues. Pull requests are welcome; see
`CONTRIBUTING.md`.
