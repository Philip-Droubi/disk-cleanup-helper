# Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
# Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
# Draws the app icon into src\Resources\app.ico (and a preview picture into obj\icon-preview.png).
# Only needed to change the icon; replacing src\Resources\app.ico with another .ico file works too.
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent (Split-Path -Parent $PSCommandPath)
$ico = Join-Path $root 'src\Resources\app.ico'
New-Item -ItemType Directory -Force -Path (Join-Path $root 'obj') | Out-Null
$preview = Join-Path $root 'obj\icon-preview.png'

function New-RoundedRect([single]$x, [single]$y, [single]$w, [single]$h, [single]$r) {
  $p = New-Object System.Drawing.Drawing2D.GraphicsPath
  $d = $r * 2
  $p.AddArc($x, $y, $d, $d, 180, 90)
  $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
  $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
  $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
  $p.CloseFigure()
  return $p
}

function New-Star([single]$cx, [single]$cy, [single]$big) {
  $small = $big * 0.30
  $pts = New-Object 'System.Drawing.PointF[]' 8
  for ($i = 0; $i -lt 8; $i++) {
    $a = [Math]::PI / 4 * $i - [Math]::PI / 2
    $r = if ($i % 2 -eq 0) { $big } else { $small }
    $pts[$i] = New-Object System.Drawing.PointF(($cx + $r * [Math]::Cos($a)), ($cy + $r * [Math]::Sin($a)))
  }
  return $pts
}

function Draw-Icon([int]$s) {
  $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
  $g.Clear([System.Drawing.Color]::Transparent)

  # blue rounded square
  $pad = [single]([Math]::Max(0.5, $s * 0.03))
  $side = [single]($s - 2 * $pad)
  $bg = New-RoundedRect $pad $pad $side $side ([single]($s * 0.22))
  $rect = New-Object System.Drawing.RectangleF($pad, $pad, $side, $side)
  $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, [System.Drawing.Color]::FromArgb(255, 59, 130, 246), [System.Drawing.Color]::FromArgb(255, 30, 58, 138), [single]45)
  $g.FillPath($grad, $bg)

  # disk usage ring: white = used, green = free
  $cx = [single]($s * 0.46); $cy = [single]($s * 0.55)
  $thick = [single]($s * $(if ($s -le 24) { 0.15 } else { 0.115 }))
  $rad = [single]($s * 0.30 - $thick / 2)
  $arc = New-Object System.Drawing.RectangleF(($cx - $rad), ($cy - $rad), (2 * $rad), (2 * $rad))
  $penUsed = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 255, 255, 255), $thick)
  $penFree = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 74, 222, 128), $thick)
  $g.DrawArc($penFree, $arc, [single]-90, [single]100)
  $g.DrawArc($penUsed, $arc, [single]14, [single]252)

  # sparkles (clean)
  if ($s -ge 32) {
    $warm = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 254, 243, 199))
    $g.FillPolygon($warm, (New-Star ([single]($s * 0.77)) ([single]($s * 0.24)) ([single]($s * 0.16))))
    if ($s -ge 48) { $g.FillPolygon($warm, (New-Star ([single]($s * 0.86)) ([single]($s * 0.47)) ([single]($s * 0.07)))) }
  }
  $g.Dispose()
  return $bmp
}

function Get-PngBytes($bmp) {
  $ms = New-Object System.IO.MemoryStream
  $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
  return $ms.ToArray()
}

# Classic icon image: 32-bit BGRA rows bottom-up plus an empty AND mask.
function Get-DibBytes($bmp) {
  $w = $bmp.Width; $h = $bmp.Height
  $ms = New-Object System.IO.MemoryStream
  $bw = New-Object System.IO.BinaryWriter($ms)
  $bw.Write([int]40); $bw.Write([int]$w); $bw.Write([int]($h * 2)); $bw.Write([int16]1); $bw.Write([int16]32)
  $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0); $bw.Write([int]0)
  $r = New-Object System.Drawing.Rectangle(0, 0, $w, $h)
  $data = $bmp.LockBits($r, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $buf = New-Object byte[] ($data.Stride * $h)
  [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $buf, 0, $buf.Length)
  $bmp.UnlockBits($data)
  for ($y = $h - 1; $y -ge 0; $y--) { $bw.Write($buf, $y * $data.Stride, $w * 4) }
  $maskRow = [int]([Math]::Ceiling($w / 32.0)) * 4
  $bw.Write((New-Object byte[] ($maskRow * $h)))
  $bw.Flush()
  return $ms.ToArray()
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$images = @()
foreach ($s in $sizes) {
  $bmp = Draw-Icon $s
  if ($s -ge 128) { $bytes = Get-PngBytes $bmp } else { $bytes = Get-DibBytes $bmp }
  if ($s -eq 256) { $bmp.Save($preview, [System.Drawing.Imaging.ImageFormat]::Png) }
  $bmp.Dispose()
  $images += , @($s, $bytes)
}

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)
$bw.Write([int16]0); $bw.Write([int16]1); $bw.Write([int16]$images.Count)
$offset = 6 + 16 * $images.Count
foreach ($im in $images) {
  $s = $im[0]; $len = $im[1].Length
  $dim = if ($s -ge 256) { 0 } else { $s }
  $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
  $bw.Write([int16]1); $bw.Write([int16]32); $bw.Write([int]$len); $bw.Write([int]$offset)
  $offset += $len
}
foreach ($im in $images) { $bw.Write([byte[]]$im[1]) }
$bw.Flush()
[System.IO.File]::WriteAllBytes($ico, $ms.ToArray())
'Saved {0} ({1:N0} KB) and {2}' -f $ico, ((Get-Item $ico).Length / 1KB), $preview
