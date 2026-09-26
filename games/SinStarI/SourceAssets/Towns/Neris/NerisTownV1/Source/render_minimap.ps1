param()
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$package = Split-Path $PSScriptRoot -Parent
$layout = Get-Content (Join-Path $package 'expansion-layout.json') -Raw | ConvertFrom-Json
$bitmap = [Drawing.Bitmap]::new(1140,1080)
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
function MapX([double]$x) { [single](30+($x-$layout.bounds[0])*1080/($layout.bounds[2]-$layout.bounds[0])) }
function MapY([double]$y) { [single](90+($layout.bounds[3]-$y)*940/($layout.bounds[3]-$layout.bounds[1])) }
function Rect($brush,[double]$x,[double]$y,[double]$w,[double]$d) {
    $graphics.FillRectangle($brush,(MapX ($x-$w/2)),(MapY ($y+$d/2)),[single]($w*1080/($layout.bounds[2]-$layout.bounds[0])),[single]($d*940/($layout.bounds[3]-$layout.bounds[1])))
}
function Label([string]$text,[double]$x,[double]$y) {
    $size=$graphics.MeasureString($text,$font)
    $px=(MapX $x)-$size.Width/2; $py=(MapY $y)-$size.Height/2
    $graphics.FillRectangle($navy,[single]($px-5),[single]($py-1),[single]($size.Width+10),[single]($size.Height+2))
    $graphics.DrawString($text,$font,$white,[single]$px,[single]$py)
}
$graphics.Clear([Drawing.Color]::Transparent)
$graphics.FillRectangle($navy,0,0,1140,1080)
$graphics.FillRectangle($water,30,90,1080,940)
$graphics.DrawString('NERIS',$title,$ivory,30,22)
$graphics.DrawString('N ↑',$font,$ivory,1032,27)
function BoundsRect($brush,$r) { Rect $brush (($r[0]+$r[2])/2) (($r[1]+$r[3])/2) ($r[2]-$r[0]) ($r[3]-$r[1]) }
foreach($land in $layout.land) { BoundsRect $green $land }
foreach($band in $layout.waterRectangles) { BoundsRect $water $band }
foreach($road in $layout.paving) { Rect $slate $road[0] $road[1] $road[2] $road[3] }
foreach($residence in $layout.homes) {
    $brush=switch($residence.style) { Large {$estate}; Medium {$family}; Small {$workers} }
    Rect $brush $residence.x $residence.y $residence.depth $residence.width
}
foreach($p in $layout.legacyHomes) {
    Rect $family $p[0] $p[1] 8 8
}
Rect $ivory $layout.comparisonCastle.position[0] $layout.comparisonCastle.position[1] 148 165
Rect $ivory $layout.castle[0] $layout.castle[1] 148 120
Rect $slate $layout.castle[0] $layout.castle[1] 138 110
Rect $estate $layout.castle[0] ($layout.castle[1]+26) 116 42
Rect $ivory $layout.military[0] $layout.military[1] 148 132
Rect $slate $layout.military[0] $layout.military[1] 140 124
Rect $green $layout.military[0] ($layout.military[1]-14) 100 66
Rect $gold $layout.military[0] ($layout.military[1]+42) 140 42
Rect $gold $layout.cityHall[0] $layout.cityHall[1] (24*$layout.cityHallScale) (20*$layout.cityHallScale)
Rect $gold $layout.tower[0] $layout.tower[1] (13*$layout.towerScale) (12*$layout.towerScale)
foreach($y in @(-156,-208,-260)) { Rect $gold 125 $y 13 11 }
foreach($label in $layout.labels) { Label $label[0] $label[1] $label[2] }
$graphics.DrawString('● Leader',$font,$gold,30,1038)
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
