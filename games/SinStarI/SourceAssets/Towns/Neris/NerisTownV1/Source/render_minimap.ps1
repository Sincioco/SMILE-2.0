param()
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$package = Split-Path $PSScriptRoot -Parent
$layout = Get-Content (Join-Path $package 'expansion-layout.json') -Raw | ConvertFrom-Json
$bitmap = [Drawing.Bitmap]::new(900,1080)
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
function Ink($r,$g,$b) { [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255,$r,$g,$b)) }
$navy=Ink 12 24 34; $green=Ink 43 91 54; $slate=Ink 76 95 109
$ivory=Ink 232 220 187; $water=Ink 35 137 168; $gold=Ink 228 188 109
$estate=Ink 218 184 117; $family=Ink 183 203 207; $workers=Ink 179 169 157
$white=Ink 235 243 246
$font=[Drawing.Font]::new('Segoe UI',30,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
$title=[Drawing.Font]::new('Segoe UI',38,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
function MapX([double]$x) { [single](30+($x+135)*840/270) }
function MapY([double]$y) { [single](90+(182-$y)*940/302) }
function Rect($brush,[double]$x,[double]$y,[double]$w,[double]$d) {
    $graphics.FillRectangle($brush,(MapX ($x-$w/2)),(MapY ($y+$d/2)),[single]($w*840/270),[single]($d*940/302))
}
function Label([string]$text,[double]$x,[double]$y) {
    $size=$graphics.MeasureString($text,$font)
    $px=(MapX $x)-$size.Width/2; $py=(MapY $y)-$size.Height/2
    $graphics.FillRectangle($navy,[single]($px-5),[single]($py-1),[single]($size.Width+10),[single]($size.Height+2))
    $graphics.DrawString($text,$font,$white,[single]$px,[single]$py)
}
$graphics.Clear([Drawing.Color]::Transparent)
$graphics.FillRectangle($navy,0,0,900,1080)
$graphics.FillRectangle($green,30,90,840,940)
$graphics.DrawString('NERIS',$title,$ivory,30,22)
$graphics.DrawString('N ↑',$font,$ivory,792,27)
foreach($road in $layout.roads) { Rect $slate $road[0] $road[1] $road[2] $road[3] }
Rect $slate 0 -3 17 79
Rect $slate 0 25 27 29
foreach($y in @(-32,-10,14,39)) { Rect $slate 0 $y 90 4.8 }
foreach($x in @(-21,21)) { Rect $slate $x 0 5 81 }
foreach($x in @(-12.5,12.5)) { Rect $water $x -12 2.8 53 }
foreach($residence in $layout.homes) {
    $brush=switch($residence.style) { Large {$estate}; Medium {$family}; Small {$workers} }
    Rect $brush $residence.x $residence.y $residence.width $residence.depth
}
foreach($p in @(@(-34,25),@(-34,6),@(-34,-14),@(-34,-33),@(-36,39),@(-24,-54),@(32,37),@(31,-36))) {
    Rect $family $p[0] $p[1] 8 8
}
foreach($band in $layout.water) { Rect $water $band[0] $band[1] $band[2] $band[3] }
Rect $ivory -48 127 74 60
Rect $slate -48 127 69 55
Rect $estate -48 140 58 21
Rect $slate -48 89 9 22
Rect $ivory 66 125 74 66
Rect $slate 66 125 70 62
Rect $green 66 118 50 33
Rect $gold 66 146 70 21
Rect $gold 0 26 22 17
Rect $gold 15 75 13 12
foreach($y in @(-22,0,22)) { Rect $gold 32 $y 11 13 }
Label 'Castle' -48 154
Label 'Military HQ' 66 166
Label 'Parade' 66 113
Label 'Relay' 15 86
Label 'City Hall' 0 43
Label 'Weapon' 31 23
Label 'Item' 31 1
Label 'Armor' 31 -22
Label 'Estates' -96 45
Label 'Homes' 96 44
Label 'Workers' 0 -116
$graphics.DrawString('● Arin',$font,$gold,30,1038)
$bitmap.Save((Join-Path $package 'Textures/Neris-Minimap.png'),[Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose(); $bitmap.Dispose()
$marker=[Drawing.Bitmap]::new(32,32)
$paint=[Drawing.Graphics]::FromImage($marker)
$paint.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
$paint.Clear([Drawing.Color]::Transparent)
$paint.FillEllipse($navy,1,1,30,30)
$paint.FillEllipse($gold,6,6,20,20)
$marker.Save((Join-Path $package 'Textures/Neris-Minimap-Arin.png'),[Drawing.Imaging.ImageFormat]::Png)
$paint.Dispose(); $marker.Dispose()
# A small sprite sheet uses the existing source-rectangle image API. North first,
# then clockwise in five-degree steps; the player marker covers the cone origin.
$heading=[Drawing.Bitmap]::new(1152,576)
$paint=[Drawing.Graphics]::FromImage($heading)
$paint.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
$paint.Clear([Drawing.Color]::Transparent)
$fan=[Drawing.Drawing2D.GraphicsPath]::new()
$fan.AddPie(-43,-43,86,86,-130,80)
$glow=[Drawing.Drawing2D.PathGradientBrush]::new($fan)
$glow.CenterPoint=[Drawing.PointF]::new(0,0)
$glow.CenterColor=[Drawing.Color]::FromArgb(230,255,230,144)
$glow.SurroundColors=@([Drawing.Color]::FromArgb(0,255,230,144))
for($frame=0;$frame -lt 72;$frame++) {
    $paint.ResetTransform()
    $paint.TranslateTransform(($frame%12)*96+48,[Math]::Floor($frame/12)*96+48)
    $paint.RotateTransform($frame*5)
    $paint.FillPath($glow,$fan)
}
$heading.Save((Join-Path $package 'Textures/Neris-Minimap-Heading.png'),[Drawing.Imaging.ImageFormat]::Png)
$glow.Dispose(); $fan.Dispose(); $paint.Dispose(); $heading.Dispose()
foreach($item in @($navy,$green,$slate,$ivory,$water,$gold,$estate,$family,$workers,$white,$font,$title)) { $item.Dispose() }
Write-Output 'Rendered expanded minimap and alpha marker using Windows System.Drawing.'
