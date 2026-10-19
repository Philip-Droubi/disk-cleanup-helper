# Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
# Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
# Checks every language file in src\Localization\Languages against the English texts in the code.
#   - errors (the build stops): placeholders that differ from the English, non-Western digits, translated unit words
#   - warnings: texts with no translation yet, translations no longer used
# Writes obj\lang-todo-<code>.lang with every missing text, ready to be filled in.
# Run by build.ps1; can also be run by itself: powershell -File tools\lang-check.ps1
# Keep this file saved as UTF-8 with BOM: Windows PowerShell 5.1 needs it to read the Arabic and Persian patterns below.
param([string]$Root = (Split-Path -Parent (Split-Path -Parent $PSCommandPath)), [switch]$Quiet)
$ErrorActionPreference = 'Stop'
$src = Join-Path $Root 'src'
$obj = Join-Path $Root 'obj'
New-Item -ItemType Directory -Force -Path $obj | Out-Null

function Unescape-CSharp([string]$lit) {
  $s = $lit.Substring(1, $lit.Length - 2)
  $sb = New-Object System.Text.StringBuilder
  for ($i = 0; $i -lt $s.Length; $i++) {
    $c = $s[$i]
    if ($c -ne '\' -or $i -eq $s.Length - 1) { [void]$sb.Append($c); continue }
    $i++; $e = $s[$i]
    switch -CaseSensitive ($e) {
      'n' { [void]$sb.Append("`n") }
      'r' { }
      't' { [void]$sb.Append("`t") }
      '0' { }
      'u' { [void]$sb.Append([char][Convert]::ToInt32($s.Substring($i + 1, 4), 16)); $i += 4 }
      default { [void]$sb.Append($e) }
    }
  }
  return $sb.ToString().Replace("`n", '<br>')
}

function Unescape-Xml([string]$s) {
  return [System.Net.WebUtility]::HtmlDecode($s)
}

# ---- 1. every English text the app shows
$texts = New-Object 'System.Collections.Generic.Dictionary[string,string]'   # text -> where (first place)
$plurals = New-Object 'System.Collections.Generic.HashSet[string]'
$fixedTexts = New-Object 'System.Collections.Generic.HashSet[string]'   # texts used with L.T / L.F
$lit = '"(?:[^"\\\r\n]|\\.)*"'
foreach ($f in Get-ChildItem -LiteralPath $src -Filter *.cs -Recurse) {
  $code = [IO.File]::ReadAllText($f.FullName)
  $code = [regex]::Replace($code, '(?m)^\s*//.*$', '')   # examples in comments are not texts
  foreach ($m in [regex]::Matches($code, "L\.[TF]\(\s*($lit)")) {
    $t = Unescape-CSharp $m.Groups[1].Value
    if (-not $texts.ContainsKey($t)) { $texts[$t] = $f.Name }
    [void]$fixedTexts.Add($t)
  }
  foreach ($m in [regex]::Matches($code, "L\.P\([^`"]*?,\s*($lit)\s*,\s*($lit)\s*\)")) {
    $t = Unescape-CSharp $m.Groups[2].Value
    if (-not $texts.ContainsKey($t)) { $texts[$t] = $f.Name }
    [void]$plurals.Add($t)
  }
}
$xaml = [IO.File]::ReadAllText((Join-Path $src 'UI\MainWindow.xaml'))
foreach ($m in [regex]::Matches($xaml, '\s(?:Text|Content|Header|ToolTip)="([^"{][^"]*)"')) {
  $t = Unescape-Xml $m.Groups[1].Value
  if ($t -match '[A-Za-z]{2}' -and -not $texts.ContainsKey($t)) { $texts[$t] = 'MainWindow.xaml' }
}

# ---- 2. each language file
$errors = 0
# A plural text's translation lists every form ("{0} Minute | {0} Minuten"); through L.T / L.F it would show all of them.
foreach ($t in $plurals) {
  if ($fixedTexts.Contains($t)) { Write-Host "ERROR: `"$t`" is a plural text (L.P) but is also used with L.T / L.F" -ForegroundColor Red; $errors++ }
}
$langDir = Join-Path $src 'Localization\Languages'
$files = @()
if (Test-Path -LiteralPath $langDir) { $files = @(Get-ChildItem -LiteralPath $langDir -Filter *.lang) }
foreach ($lf in $files) {
  $code = [IO.Path]::GetFileNameWithoutExtension($lf.Name)
  $map = New-Object 'System.Collections.Generic.Dictionary[string,string]'
  $lineOf = @{}
  $pending = $null; $n = 0
  foreach ($line in [IO.File]::ReadAllLines($lf.FullName, [Text.Encoding]::UTF8)) {
    $n++
    $t = $line.Trim()
    if ($t.Length -eq 0 -or $t.StartsWith('#') -or $t.StartsWith('@')) { continue }
    $ci = $t.IndexOf(':')
    if ($ci -lt 2 -or $ci -gt 8) { continue }
    $tag = $t.Substring(0, $ci); $val = $t.Substring($ci + 1); if ($val.StartsWith(' ')) { $val = $val.Substring(1) }
    if ($tag -eq 'en') { $pending = $val; continue }
    if ($null -ne $pending) {
      if ($map.ContainsKey($pending)) { Write-Host ("  {0}.lang line {1}: this English text is listed twice: {2}" -f $code, $n, $pending) -ForegroundColor Yellow }
      $map[$pending] = $val; $lineOf[$pending] = $n; $pending = $null
    }
  }

  $missing = @($texts.Keys | Where-Object { -not $map.ContainsKey($_) -or $map[$_] -eq '' })
  $unused = @($map.Keys | Where-Object { -not $texts.ContainsKey($_) })
  $bad = New-Object System.Collections.Generic.List[string]
  foreach ($k in $map.Keys) {
    $v = $map[$k]
    if ($v -eq '' -or $v -eq '=') { continue }
    $line = $lineOf[$k]
    if ($v -match '[٠-٩۰-۹]') { $bad.Add("line ${line}: use Western digits 0-9: $v") }
    if ($v -match 'جيجا|ميجا|ميغا|غيغا|كيلوبايت|تيرا|بايت') { $bad.Add("line ${line}: units stay in English (KB, MB, GB, TB): $v") }
    if ($v -match '\u06AF\u06CC\u06AF\u0627|\u0645\u06AF\u0627\u0628\u0627|\u06A9\u06CC\u0644\u0648\u0628\u0627|\u062A\u0631\u0627\u0628\u0627|\u0628\u0627\u06CC\u062A|\u0628\u0627\u0626\u0679|\u30AE\u30AC\u30D0\u30A4\u30C8|\u30E1\u30AC\u30D0\u30A4\u30C8|\uAE30\uAC00\uBC14\uC774\uD2B8|\uBA54\uAC00\uBC14\uC774\uD2B8') { $bad.Add("line ${line}: units stay in English (KB, MB, GB, TB): $v") }
    if ($v -match '[\uFF10-\uFF19]') { $bad.Add("line ${line}: use Western digits 0-9, not full-width digits: $v") }
    if ($v -match '\d\s*(ГБ|МБ|КБ|ТБ|ГГц|МГц|Гц)|\}\s*(ГБ|МБ|КБ|ТБ|ГГц|МГц|Гц)') { $bad.Add("line ${line}: units stay in English (GB, MB, GHz): $v") }
    if ($v -cmatch '\{\d+(?::[^}]*)?\}\s*(Go|Mo|Ko|To)\b') { $bad.Add("line ${line}: units stay in English (GB, MB, KB, TB): $v") }
    $enHolders = @([regex]::Matches($k, '\{\d+(?::[^}]*)?\}') | ForEach-Object { $_.Value } | Sort-Object -Unique)
    if ($plurals.Contains($k)) {
      foreach ($form in $v.Split(@(' | '), [StringSplitOptions]::None)) {
        foreach ($h in [regex]::Matches($form, '\{\d+(?::[^}]*)?\}')) { if ($enHolders -notcontains $h.Value) { $bad.Add("line ${line}: placeholder $($h.Value) is not in the English: $v") } }
      }
    } else {
      $trHolders = @([regex]::Matches($v, '\{\d+(?::[^}]*)?\}') | ForEach-Object { $_.Value } | Sort-Object -Unique)
      if (($enHolders -join ',') -ne ($trHolders -join ',')) { $bad.Add("line ${line}: placeholders differ (English has '$($enHolders -join ' ')', translation has '$($trHolders -join ' ')'): $v") }
    }
  }

  $todo = Join-Path $obj ("lang-todo-$code.lang")
  $sb = New-Object System.Text.StringBuilder
  foreach ($k in ($missing | Sort-Object { $texts[$_] })) { [void]$sb.AppendLine("# " + $texts[$k]); [void]$sb.AppendLine("en: $k"); [void]$sb.AppendLine("${code}: "); [void]$sb.AppendLine() }
  [IO.File]::WriteAllText($todo, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))

  $color = if ($bad.Count -gt 0) { 'Red' } elseif ($missing.Count -gt 0) { 'Yellow' } else { 'Green' }
  Write-Host ("Language {0}: {1} of {2} texts translated, {3} missing, {4} no longer used, {5} errors" -f $code, ($texts.Count - $missing.Count), $texts.Count, $missing.Count, $unused.Count, $bad.Count) -ForegroundColor $color
  foreach ($b in $bad) { Write-Host "  ERROR $code.lang $b" -ForegroundColor Red }
  if (-not $Quiet) {
    foreach ($u in ($unused | Select-Object -First 15)) { Write-Host "  not used any more: $u" -ForegroundColor DarkYellow }
    if ($missing.Count -gt 0) { Write-Host "  Missing texts are listed in $todo" -ForegroundColor DarkYellow }
  }
  $errors += $bad.Count
}
if ($files.Count -eq 0) { Write-Host ("No language files yet. {0} English texts found." -f $texts.Count) }
exit $(if ($errors -gt 0) { 1 } else { 0 })
