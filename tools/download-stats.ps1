# Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
# Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
# Prints how often each release was downloaded, from GitHub's public API (no login needed).
#   zip = people downloading by hand (roughly new people), exe = the app updating itself.
# A count is a download, not a person: repeat downloads and bots count too. GitHub keeps only running totals, so
# -Csv appends one dated line per release, and running it now and then shows growth over time.
#
#   powershell -File tools\download-stats.ps1
#   powershell -File tools\download-stats.ps1 -Repo cli/cli
#   powershell -File tools\download-stats.ps1 -Csv D:\stats\downloads.csv
param([string]$Repo = 'philip-droubi/disk-cleanup-helper', [string]$Csv = '')
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
$headers = @{ 'User-Agent' = 'DiskCleanupHelper-download-stats'; 'Accept' = 'application/vnd.github+json' }

# ---- every release, 100 per request (GitHub allows 60 requests an hour without a login)
$releases = @()
$page = 1
while ($true) {
  $url = 'https://api.github.com/repos/{0}/releases?per_page=100&page={1}' -f $Repo, $page
  # Windows PowerShell returns a JSON array as one object; piping it lists the releases one by one
  try { $answer = Invoke-RestMethod -Uri $url -Headers $headers -UseBasicParsing; $batch = @($answer | Where-Object { $_ }) }
  catch {
    $status = 0
    if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
    if ($status -eq 404) { Write-Host "Repository not found (create it first, or check -Repo): $Repo" -ForegroundColor Red; exit 1 }
    if ($status -eq 403 -or $status -eq 429) { Write-Host "GitHub's hourly limit for requests without a login is used up; try again in an hour." -ForegroundColor Red; exit 1 }
    Write-Host ("Could not reach GitHub: {0}" -f $_.Exception.Message) -ForegroundColor Red; exit 1
  }
  if ($batch.Count -eq 0) { break }
  $releases += $batch
  $page++
}
if ($releases.Count -eq 0) { Write-Host "$Repo has no releases yet."; exit 0 }

# ---- one line per release, newest first
$rows = foreach ($r in ($releases | Sort-Object { [datetime]$_.published_at } -Descending)) {
  $zip = 0; $exe = 0; $all = 0
  foreach ($a in $r.assets) {
    $n = [int64]$a.download_count
    $all += $n
    if ($a.name -like '*.zip') { $zip += $n } elseif ($a.name -like '*.exe') { $exe += $n }
  }
  $date = if ($r.published_at) { ([datetime]$r.published_at).ToString('yyyy-MM-dd') } else { '(draft)' }
  [pscustomobject]@{ Tag = $r.tag_name; Date = $date; Zip = $zip; Exe = $exe; All = $all }
}
Write-Host ''
Write-Host ("Downloads of github.com/{0}" -f $Repo) -ForegroundColor Cyan
$rows | Format-Table @{ n = 'Release'; e = { $_.Tag } }, Date, @{ n = 'Zip (people)'; e = { $_.Zip.ToString('N0') }; a = 'Right' },
                     @{ n = 'Exe (updates)'; e = { $_.Exe.ToString('N0') }; a = 'Right' }, @{ n = 'All files'; e = { $_.All.ToString('N0') }; a = 'Right' } -AutoSize | Out-Host
$tz = ($rows | Measure-Object Zip -Sum).Sum
$te = ($rows | Measure-Object Exe -Sum).Sum
$ta = ($rows | Measure-Object All -Sum).Sum
Write-Host ('People (zip): {0:N0}   Updates (exe): {1:N0}   All files: {2:N0}' -f $tz, $te, $ta) -ForegroundColor Green

# ---- optional history: one dated line per release
if ($Csv) {
  $today = (Get-Date).ToString('yyyy-MM-dd')
  $lines = @()
  if (-not (Test-Path -LiteralPath $Csv)) { $lines += 'date,tag,zip,exe' }
  $lines += $rows | ForEach-Object { '{0},{1},{2},{3}' -f $today, $_.Tag, $_.Zip, $_.Exe }
  Add-Content -LiteralPath $Csv -Value $lines -Encoding UTF8
  Write-Host ("Saved to {0}" -f $Csv)
}
