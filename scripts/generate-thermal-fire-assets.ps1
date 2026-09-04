[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$taskAssetRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\TechnicalAssets\Generation3\Fire'))
Add-Type -AssemblyName System.Drawing
if (-not ('SmileThermalAssets' -as [type])) {
    $taskReferences = @('System.Drawing')
    if ($PSVersionTable.PSVersion.Major -ge 7) {
        $taskReferences = @('System.Drawing.Common','System.Drawing.Primitives',
            'System.Private.Windows.GdiPlus','System.Private.Windows.Core','System.Runtime')
    }
    Add-Type -ReferencedAssemblies $taskReferences -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
public static class SmileThermalAssets {
    static double Clamp(double v) { return Math.Max(0, Math.Min(1,v)); }
    static double Hash(int x,int y,int seed) {
        unchecked { uint h=(uint)x*374761393u+(uint)y*668265263u+(uint)seed*1013904223u;
            h=(h^(h>>13))*1274126177u; return (h^(h>>16))/(double)uint.MaxValue; }
    }
    static double Noise(double x,double y,int seed) {
        int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y); double u=x-ix,v=y-iy;
        u=u*u*(3-2*u); v=v*v*(3-2*v);
        return (Hash(ix,iy,seed)*(1-u)+Hash(ix+1,iy,seed)*u)*(1-v)+
            (Hash(ix,iy+1,seed)*(1-u)+Hash(ix+1,iy+1,seed)*u)*v;
    }
    public static byte[] Generate(int kind) {
        int width=kind==2?64:(kind==3?256:1024), height=kind==3?4:width;
        using(var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb)) {
            for(int y=0;y<height;y++) for(int x=0;x<width;x++) {
                double alpha=1; int r=255,g=255,b=255;
                if(kind==3) {
                    double t=x/255.0;
                    double[] stops={0,.25,.55,.8,1};
                    double[,] colors={{.16,.005,0},{.95,.1,.005},{1,.55,.03},{1,.92,.3},{1,1,.96}};
                    int s=0; while(s<3&&t>stops[s+1]) s++;
                    double q=(t-stops[s])/(stops[s+1]-stops[s]);
                    r=(int)Math.Round(255*(colors[s,0]+(colors[s+1,0]-colors[s,0])*q));
                    g=(int)Math.Round(255*(colors[s,1]+(colors[s+1,1]-colors[s,1])*q));
                    b=(int)Math.Round(255*(colors[s,2]+(colors[s+1,2]-colors[s,2])*q));
                } else {
                    int cell=kind==2?64:256, frame=(y/cell)*4+x/cell;
                    double u=((x%cell)+.5)/cell, v=((y%cell)+.5)/cell;
                    double px=(u-.5)*2, py=(v-.5)*2;
                    if(kind==0) {
                        double center=.11*Math.Sin(v*8+frame*1.7)+.06*Math.Sin(v*19+frame);
                        double radius=.16+.24*v;
                        double tongue=Math.Exp(-Math.Pow((px-center)/radius,2)*2);
                        double lobes=.68+.19*Math.Sin(px*16+v*11+frame)+.13*Math.Sin(v*27-frame*2);
                        double wisps=Noise(px*13+Noise(px*5,v*8,frame)*2,v*19,frame);
                        double detail=Noise(px*31,v*43,frame+77);
                        alpha=tongue*Clamp((v-.06)*10)*Clamp((.92-v)*12)*Clamp(lobes)*
                            Clamp((wisps*.75+detail*.25-.18)*2.1);
                    } else if(kind==1) {
                        double radial=Math.Exp(-(px*px+py*py)*5);
                        double lobes=.66+.17*Math.Sin(px*9+frame)+.17*Math.Cos(py*11+px*4+frame*1.3);
                        double billows=Noise(px*5+Noise(px*3,py*3,frame),py*7,frame);
                        alpha=radial*Clamp(lobes)*Clamp((.92-Math.Sqrt(px*px+py*py))*5)*(.35+billows*.8);
                    } else alpha=Math.Exp(-(px*px+py*py)*12);
                    // Three completely transparent texels per atlas edge; straight white RGB.
                    if(x%cell<3||y%cell<3||x%cell>=cell-3||y%cell>=cell-3) alpha=0;
                }
                bitmap.SetPixel(x,y,Color.FromArgb((int)Math.Round(255*Clamp(alpha)),r,g,b));
            }
            using(var stream=new MemoryStream()) { bitmap.Save(stream,ImageFormat.Png); return stream.ToArray(); }
        }
    }
}
'@
}
$taskNames = @('fire-shape-atlas.png','smoke-shape-atlas.png','ember-shape.png','thermal-gradient-lut.png')
if (-not $Check) { [IO.Directory]::CreateDirectory($taskAssetRoot) | Out-Null }
for ($taskIndex=0; $taskIndex -lt $taskNames.Count; $taskIndex++) {
    $taskPath=Join-Path $taskAssetRoot $taskNames[$taskIndex]
    $taskBytes=[SmileThermalAssets]::Generate($taskIndex)
    $taskHasher = [Security.Cryptography.SHA256]::Create()
    try { $taskHash = [BitConverter]::ToString($taskHasher.ComputeHash($taskBytes)).Replace('-','').ToLowerInvariant() }
    finally { $taskHasher.Dispose() }
    if ($Check) {
        if (-not (Test-Path -LiteralPath $taskPath) -or (Get-FileHash -LiteralPath $taskPath).Hash -ne $taskHash) {
            throw "Deterministic fire asset mismatch: $taskPath"
        }
    } else { [IO.File]::WriteAllBytes($taskPath,$taskBytes) }
    '{0} | {1} bytes | {2}' -f $taskNames[$taskIndex],$taskBytes.Length,$taskHash
}
