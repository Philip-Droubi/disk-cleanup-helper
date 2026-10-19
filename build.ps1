# Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
# Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
# Builds Disk Cleanup Helper into builds\v<version>\DiskCleanupHelper.exe
#
#   build.cmd             builds the next version (1.0 -> 1.1 -> 1.2 ...)
#   build.cmd -Rebuild    builds the current version again into the same folder
#   build.cmd -Major      builds the next major version (1.4 -> 2.0)
#   add -Publish          also publishes the build as a GitHub release (needs release-notes.txt and gh)
#
# version.txt holds major.minor. A .0 version builds into builds\v<major> (1.0 -> builds\v1), others into
# builds\v<major.minor> (builds\v1.1).
# Uses only the C# compiler that is part of Windows (.NET Framework 4.8, %WINDIR%\Microsoft.NET\Framework64\v4.0.30319).
# Nothing to install.
param([switch]$Rebuild, [switch]$Major, [switch]$Publish)
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSCommandPath
$src = Join-Path $root 'src'
$obj = Join-Path $root 'obj'
$verFile = Join-Path $root 'version.txt'
$resources = Join-Path $src 'Resources'
$langDir = Join-Path $src 'Localization\Languages'
$readMeDir = Join-Path $src 'Localization\ReadMe'

function Say([string]$text, [string]$color = 'Gray') { Write-Host $text -ForegroundColor $color }

# ---- version (major.minor; an old plain "1" counts as 1.0)
$lastMajor = 0; $lastMinor = 0
if (Test-Path -LiteralPath $verFile) {
  $parts = ([IO.File]::ReadAllText($verFile)).Trim().Split('.')
  [void][int]::TryParse($parts[0], [ref]$lastMajor)
  if ($parts.Length -gt 1) { [void][int]::TryParse($parts[1], [ref]$lastMinor) }
}
if ($lastMajor -le 0) { $vMajor = 1; $vMinor = 0 }
elseif ($Rebuild) { $vMajor = $lastMajor; $vMinor = $lastMinor }
elseif ($Major) { $vMajor = $lastMajor + 1; $vMinor = 0 }
else { $vMajor = $lastMajor; $vMinor = $lastMinor + 1 }
$ver = '{0}.{1}' -f $vMajor, $vMinor
$folderVer = if ($vMinor -eq 0) { "$vMajor" } else { $ver }   # 1.0 keeps its folder builds\v1

# ---- publishing: everything that can stop it is checked before building
$repo = 'philip-droubi/disk-cleanup-helper'   # the same as Updater.Repo in src\App\Updater.cs; forks change both
$tag = "v$ver"
if ($Publish) {
  # release-notes.txt: first line "Version <x.y>" (so last release's text is never published again), then "What's new"
  $notesFile = Join-Path $root 'release-notes.txt'
  if (-not (Test-Path -LiteralPath $notesFile)) {
    Say ('Not published: write release-notes.txt first. Its first line is "Version {0}", then the "What''s new" list.' -f $ver) Red; exit 1
  }
  $noteLines = @([IO.File]::ReadAllLines($notesFile) | ForEach-Object { $_.TrimEnd() })
  while ($noteLines.Count -gt 0 -and -not $noteLines[0]) { $noteLines = @($noteLines | Select-Object -Skip 1) }
  if ($noteLines.Count -eq 0 -or $noteLines[0] -ne "Version $ver") {
    Say ('Not published: the first line of release-notes.txt must be "Version {0}" (it still holds another version''s text).' -f $ver) Red; exit 1
  }
  $whatsNew = (($noteLines | Select-Object -Skip 1) -join "`r`n").Trim()
  if (-not $whatsNew) { Say 'Not published: release-notes.txt has no "What''s new" text under its first line.' Red; exit 1 }
  if ($whatsNew -match '(?im)^\s*SHA256:') { Say 'Not published: release-notes.txt must not contain a SHA256 line; the build adds it.' Red; exit 1 }
  $gh = Get-Command gh -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1
  if (-not $gh -and (Test-Path -LiteralPath "$env:ProgramFiles\GitHub CLI\gh.exe")) { $gh = "$env:ProgramFiles\GitHub CLI\gh.exe" }
  if (-not $gh) { Say 'Not published: the GitHub CLI (gh) is not installed. Install it with: winget install GitHub.cli' Red; exit 1 }
  $ErrorActionPreference = 'Continue'   # gh writes to stderr; Windows PowerShell would stop on that
  & $gh auth status *> $null
  $loggedIn = $LASTEXITCODE -eq 0
  & $gh release view $tag --repo $repo *> $null
  $exists = $LASTEXITCODE -eq 0
  $ErrorActionPreference = 'Stop'
  if (-not $loggedIn) { Say 'Not published: gh is not logged in. Run: gh auth login' Red; exit 1 }
  if ($exists) { Say ('Not published: release {0} already exists on GitHub. Build the next version (build.cmd -Publish without -Rebuild).' -f $tag) Red; exit 1 }
}

# ---- compiler (ships with Windows)
$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path -LiteralPath (Join-Path $fw 'csc.exe'))) { $fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
$csc = Join-Path $fw 'csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { Say 'The C# compiler that comes with Windows (.NET Framework 4) was not found.' Red; exit 1 }
$wpf = Join-Path $fw 'WPF'

Say ''
Say ('Building Disk Cleanup Helper version {0} ...' -f $ver) Cyan

# ---- version information inside the exe
New-Item -ItemType Directory -Force -Path $obj | Out-Null
$year = (Get-Date).Year
$copyYears = if ([int]$year -gt 2026) { '2026-' + $year } else { '2026' }   # first published 2026
# A changed version puts its own name in AssemblyCompany (and in AssemblyTitle and AssemblyProduct if it is renamed) and
# keeps Philip Droubi in AssemblyCopyright, adding its own name (NOTICE terms 1a and 2).
$assemblyInfo = @"
using System.Reflection;
using System.Runtime.InteropServices;
[assembly: AssemblyTitle("Disk Cleanup Helper")]
[assembly: AssemblyDescription("Shows what fills your disks and frees up space safely.")]
[assembly: AssemblyCompany("Philip Droubi")]
[assembly: AssemblyProduct("Disk Cleanup Helper")]
[assembly: AssemblyCopyright("Copyright (C) $copyYears Philip Droubi - github.com/Philip-Droubi - GNU GPL v3")]
[assembly: AssemblyMetadata("CopyrightYears", "$copyYears")]
[assembly: AssemblyVersion("$ver.0.0")]
[assembly: AssemblyFileVersion("$ver.0.0")]
[assembly: AssemblyInformationalVersion("$ver")]
[assembly: ComVisible(false)]
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8", FrameworkDisplayName = ".NET Framework 4.8")]
"@
[IO.File]::WriteAllText((Join-Path $obj 'AssemblyInfo.g.cs'), $assemblyInfo, (New-Object System.Text.UTF8Encoding($false)))

# ---- output folder
$outDir = Join-Path $root ('builds\v{0}' -f $folderVer)
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$exe = Join-Path $outDir 'DiskCleanupHelper.exe'

# ---- make sure an older copy of this version is not open (or briefly locked by an antivirus scan)
if (Test-Path -LiteralPath $exe) {
  $free = $false
  for ($try = 0; $try -lt 10 -and -not $free; $try++) {
    try { $fs = [IO.File]::Open($exe, 'Open', 'ReadWrite', 'None'); $fs.Close(); $free = $true }
    catch { Start-Sleep -Seconds 2 }
  }
  if (-not $free) {
    Say ('{0} is in use. Close Disk Cleanup Helper and run the build again.' -f $exe) Red
    exit 1
  }
}

# ---- check the translations (rules: docs/DEVELOPING.md, "Translations")
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'tools\lang-check.ps1') -Root $root -Quiet
if ($LASTEXITCODE -ne 0) {
  Say ''
  Say 'Build FAILED: a language file has errors (see the red lines above). Nothing was changed.' Red
  exit 1
}

# ---- compile
$refs =@('System.dll', 'System.Core.dll', 'System.Xml.dll', 'System.Xaml.dll', 'System.Management.dll', 'System.ServiceProcess.dll',
          'System.Drawing.dll', 'Microsoft.VisualBasic.dll', 'System.Web.Extensions.dll') | ForEach-Object { Join-Path $fw $_ }
$refs += @('PresentationFramework.dll', 'PresentationCore.dll', 'WindowsBase.dll') | ForEach-Object { Join-Path $wpf $_ }
$cscArgs = @('/nologo', '/target:winexe', '/optimize+', '/platform:anycpu', '/debug-', '/utf8output', '/codepage:65001', '/warn:3',
             "/out:$exe",
             ('/win32icon:' + (Join-Path $resources 'app.ico')),
             ('/win32manifest:' + (Join-Path $resources 'app.manifest')),
             ('/resource:' + (Join-Path $src 'UI\MainWindow.xaml') + ',DiskCleanupHelper.MainWindow.xaml'))
# images in src\Resources\Images are embedded as DiskCleanupHelper.<file name>
$imageDir = Join-Path $resources 'Images'
if (Test-Path -LiteralPath $imageDir) {
  $cscArgs += Get-ChildItem -LiteralPath $imageDir -File | ForEach-Object { '/resource:' + $_.FullName + ',DiskCleanupHelper.' + $_.Name }
}
# language files are gzip-compressed into obj\lang to keep the exe small (the app unpacks them)
if (Test-Path -LiteralPath $langDir) {
  $gzDir = Join-Path $obj 'lang'
  New-Item -ItemType Directory -Force -Path $gzDir | Out-Null
  foreach ($lf in Get-ChildItem -LiteralPath $langDir -Filter *.lang) {
    $gz = Join-Path $gzDir $lf.Name
    $bytes = [IO.File]::ReadAllBytes($lf.FullName)
    $fs = [IO.File]::Create($gz)
    $zip = New-Object IO.Compression.GZipStream($fs, [IO.Compression.CompressionLevel]::Optimal)
    $zip.Write($bytes, 0, $bytes.Length); $zip.Dispose(); $fs.Dispose()
    $cscArgs += '/resource:' + $gz + ',DiskCleanupHelper.lang.' + $lf.Name
  }
}
$cscArgs += Get-ChildItem -LiteralPath $readMeDir -Filter 'README*.txt' | ForEach-Object { '/resource:' + $_.FullName + ',DiskCleanupHelper.' + $_.Name }
# LICENSE and NOTICE go inside the exe as well ("License and terms" in the About tab), so a copy that is only the exe
# (installed, self-updated) can still show them
foreach ($legal in 'LICENSE', 'NOTICE') {
  $from = Join-Path $root $legal
  if (-not (Test-Path -LiteralPath $from)) { Say ('{0} is missing next to build.cmd: every copy must include it.' -f $legal) Red; exit 1 }
  $cscArgs += '/resource:' + $from + ',DiskCleanupHelper.' + $legal + '.txt'
}
$cscArgs += $refs | ForEach-Object { '/reference:' + $_ }
$cscArgs += (Get-ChildItem -LiteralPath $src -Filter '*.cs' -Recurse | Sort-Object FullName | ForEach-Object { $_.FullName })
$cscArgs += (Join-Path $obj 'AssemblyInfo.g.cs')

$output = & $csc @cscArgs 2>&1
$code = $LASTEXITCODE
$lines = @($output | ForEach-Object { "$_" } | Where-Object { $_ -and $_ -notmatch 'only supports language versions up to C# 5|https://go.microsoft.com/fwlink/\?LinkID=533240|This compiler is provided as part of the Microsoft' })
foreach ($l in $lines) { if ($l -match ': error ') { Say $l Red } elseif ($l -match ': warning ') { Say $l Yellow } else { Say $l } }
if ($code -ne 0 -or -not (Test-Path -LiteralPath $exe)) {
  Say ''
  Say 'Build FAILED. Nothing was changed. Fix the errors above and run the build again.' Red
  exit 1
}

# ---- optional code signing (see docs/DEVELOPING.md)
$signCfg = Join-Path $root 'signing.txt'
if (Test-Path -LiteralPath $signCfg) {
  $cfg = @{}
  foreach ($line in [IO.File]::ReadAllLines($signCfg)) {
    $t = $line.Trim()
    if (-not $t -or $t.StartsWith('#') -or $t -notmatch '=') { continue }
    $parts = $t.Split([char[]]'=', 2); $cfg[$parts[0].Trim().ToLower()] = $parts[1].Trim()
  }
  if ($cfg.Count -gt 0) {
    $signtool = Get-ChildItem -Path "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter 'signtool.exe' -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -match '\\x64\\' } | Sort-Object FullName -Descending | Select-Object -First 1
    if (-not $signtool) { Say 'signing.txt is filled in, but signtool.exe was not found. Install the Windows SDK to sign builds. The exe is NOT signed.' Yellow }
    else {
      $ts = if ($cfg['timestamp']) { $cfg['timestamp'] } else { 'http://timestamp.digicert.com' }
      $sArgs = @('sign', '/fd', 'SHA256', '/tr', $ts, '/td', 'SHA256', '/d', 'Disk Cleanup Helper')
      if ($cfg['thumbprint']) { $sArgs += @('/sha1', $cfg['thumbprint']) }
      elseif ($cfg['pfx']) { $sArgs += @('/f', $cfg['pfx']); if ($cfg['password']) { $sArgs += @('/p', $cfg['password']) } }
      $sArgs += $exe
      & $signtool.FullName @sArgs
      if ($LASTEXITCODE -eq 0) { Say 'Signed.' Green } else { Say 'Signing failed. The exe is NOT signed.' Yellow }
    }
  }
}

# ---- package: the exe, the read-me files, and LICENSE and NOTICE (the GNU GPL v3 goes with every copy, and NOTICE
# term 1 asks every copy to keep NOTICE)
$packed = @($exe)
foreach ($rm in Get-ChildItem -LiteralPath $readMeDir -Filter 'README*.txt') {
  Copy-Item -LiteralPath $rm.FullName -Destination (Join-Path $outDir $rm.Name) -Force
  $packed += (Join-Path $outDir $rm.Name)
}
foreach ($legal in 'LICENSE', 'NOTICE') {
  $to = Join-Path $outDir ($legal + '.txt')   # .txt, so Windows opens it with a double-click
  Copy-Item -LiteralPath (Join-Path $root $legal) -Destination $to -Force
  $packed += $to
}
$zip = Join-Path $outDir ('DiskCleanupHelper-v{0}.zip' -f $folderVer)
Compress-Archive -LiteralPath $packed -DestinationPath $zip -Force
[IO.File]::WriteAllText($verFile, $ver)

$size = (Get-Item -LiteralPath $exe).Length
$hash = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
Say ''
Say ('Done. Version {0}' -f $ver) Green
Say ('  Program : {0}  ({1:N0} KB)' -f $exe, ($size / 1KB))
Say ('  Zip     : {0}' -f $zip)
Say ('  SHA256  : {0}' -f $hash)
Say ''
Say ('Next build will be version {0}.{1}. To rebuild this same version, run: build.cmd -Rebuild' -f $vMajor, ($vMinor + 1))

# ---- publish: the same file names in every release, so the website can always link to .../releases/latest/download/...
if ($Publish) {
  $relDir = Join-Path $obj 'release'
  if (Test-Path -LiteralPath $relDir) { Remove-Item -LiteralPath $relDir -Recurse -Force }
  New-Item -ItemType Directory -Force -Path $relDir | Out-Null
  $relExe = Join-Path $relDir 'DiskCleanupHelper.exe'
  $relZip = Join-Path $relDir 'DiskCleanupHelper.zip'
  Copy-Item -LiteralPath $exe -Destination $relExe
  Copy-Item -LiteralPath $zip -Destination $relZip
  # the app reads "What's new" from this text and refuses a download whose SHA256 differs from this line
  $relNotes = Join-Path $obj 'release-notes.md'
  [IO.File]::WriteAllText($relNotes, ($whatsNew + "`r`n`r`nSHA256: " + $hash + "`r`n"), (New-Object System.Text.UTF8Encoding($false)))
  Say ''
  Say ('Publishing {0} to github.com/{1} ...' -f $tag, $repo) Cyan
  $ErrorActionPreference = 'Continue'
  # release-notes.md is also uploaded: the app reads it when GitHub's API limit for its internet address is used up
  & $gh release create $tag $relExe $relZip $relNotes --repo $repo --title "Disk Cleanup Helper $ver" --notes-file $relNotes
  $ErrorActionPreference = 'Stop'
  if ($LASTEXITCODE -ne 0) { Say 'Publishing FAILED (see the message above). The build is kept; fix the problem and run build.cmd -Rebuild -Publish.' Red; exit 1 }
  Say ('Published: https://github.com/{0}/releases/tag/{1}' -f $repo, $tag) Green
  # the website shows the latest version, size and SHA256: rebuild it and publish the built pages (gh-pages branch);
  # a failure here never undoes the release
  # the website's code is not in this repository (a git-ignored website\ folder on the maintainer's PC); the step runs
  # only where that folder exists
  $site = Join-Path $root 'website'
  if (-not (Test-Path -LiteralPath (Join-Path $site 'package.json'))) {
    # nothing to do: no website folder here
  } elseif (Test-Path -LiteralPath (Join-Path $site 'node_modules')) {
    Say ''
    Say 'Updating the website ...' Cyan
    $siteDone = $false
    $env:EXPECT_VERSION = $ver
    Push-Location -LiteralPath $site
    try {
      $ErrorActionPreference = 'Continue'
      # gh's login lifts GitHub's limit for anonymous calls (shared per internet address) while the release is read
      $env:GITHUB_TOKEN = (& $gh auth token 2> $null)
      $global:LASTEXITCODE = 1   # $global: on purpose: a plain assignment makes a script copy that hides npm's result
      & npm run deploy
      $siteDone = ($LASTEXITCODE -eq 0)
    } catch {
      Say ('Website step failed: {0}' -f $_.Exception.Message) Yellow
    } finally {
      $ErrorActionPreference = 'Stop'
      Pop-Location
      Remove-Item Env:EXPECT_VERSION, Env:GITHUB_TOKEN -ErrorAction SilentlyContinue
    }
    if ($siteDone) { Say 'Website updated.' Green }
    else { Say 'Website not updated (the reason is above): run "npm run deploy" in the website folder again later.' Yellow }
  } else {
    Say 'Website not updated: in the website folder run "npm install" (once), then "npm run deploy".' Yellow
  }
  Say 'Next: upload DiskCleanupHelper.exe to virustotal.com and add the report link to the release text.'
}
