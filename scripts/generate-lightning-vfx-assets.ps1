[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$assetRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\TechnicalAssets\Generation3\Lightning'))
Add-Type -AssemblyName System.Drawing

if (-not ('SmileLightningAssets' -as [type])) {
    $references = @('System.Drawing')

    if ($PSVersionTable.PSVersion.Major -ge 7) {
        $references = @(
            'System.Drawing.Common',
            'System.Drawing.Primitives',
            'System.Private.Windows.GdiPlus',
            'System.Private.Windows.Core',
            'System.Runtime'
        )
    }

    Add-Type -ReferencedAssemblies $references -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class SmileLightningAssets
{
    static double Clamp(double value)
    {
        return Math.Max(0.0, Math.Min(1.0, value));
    }

    public static byte[] Thunder()
    {
        const int rate = 22050, samples = rate * 3;
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(new byte[] {82,73,70,70}); writer.Write(36 + samples * 2);
            writer.Write(new byte[] {87,65,86,69,102,109,116,32}); writer.Write(16);
            writer.Write((short)1); writer.Write((short)1); writer.Write(rate);
            writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(new byte[] {100,97,116,97}); writer.Write(samples * 2);
            uint seed = 20260905; double low = 0;
            for (int i = 0; i < samples; i++)
            {
                seed = unchecked(seed * 1664525U + 1013904223U);
                double noise = (seed >> 8) / 8388607.5 - 1;
                double t = (double)i / rate;
                low = low * .965 + noise * .035;
                double attack = Math.Min(1, t * 500);
                double value = attack * (noise * Math.Exp(-t * 25) * .35 +
                    low * 3 * Math.Exp(-t * 1.8) + Math.Sin(t * 220) * .08 * Math.Exp(-t * 4));
                writer.Write((short)(Math.Max(-.85, Math.Min(.85, value)) * 32767));
            }
            return stream.ToArray();
        }
    }

    public static byte[] Generate(int kind)
    {
        int width = kind == 0 ? 128 : 64;
        int height = kind == 0 ? 32 : 64;
        using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double u = (x + 0.5) / width;
                    double v = (y + 0.5) / height;
                    double alpha;

                    if (kind == 0)
                    {
                        double distance = Math.Abs(v - 0.5) * 2.0;
                        double core = Math.Exp(-distance * distance * 4.0);
                        double envelope = Clamp(Math.Min(u * 16.0, (1.0 - u) * 16.0));
                        alpha = core * envelope;
                    }
                    else
                    {
                        double px = (u - 0.5) * 2.0;
                        double py = (v - 0.5) * 2.0;
                        double radius = Math.Sqrt(px * px + py * py);
                        double core = Math.Exp(-radius * radius * 18.0);
                        double cross = Math.Exp(-Math.Min(px * px, py * py) * 90.0) *
                            Clamp((1.0 - radius) * 2.2);
                        alpha = Math.Max(core, cross * 0.72);
                    }

                    int a = (int)Math.Round(255.0 * Clamp(alpha));
                    bitmap.SetPixel(x, y, Color.FromArgb(a, 255, 255, 255));
                }
            }

            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }
}
'@
}

$names = @('lightning-ribbon.png', 'lightning-spark.png', 'thunder.wav')

if (-not $Check) {
    [IO.Directory]::CreateDirectory($assetRoot) | Out-Null
}

for ($index = 0; $index -lt $names.Count; $index++) {
    $path = Join-Path $assetRoot $names[$index]
    $bytes = if ($index -eq 2) { [SmileLightningAssets]::Thunder() } else { [SmileLightningAssets]::Generate($index) }
    $hasher = [Security.Cryptography.SHA256]::Create()

    try {
        $hash = [BitConverter]::ToString($hasher.ComputeHash($bytes)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
    }

    if ($Check) {
        if (-not (Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path).Hash -ne $hash) {
            throw "Deterministic lightning asset mismatch: $path"
        }
    }
    else {
        [IO.File]::WriteAllBytes($path, $bytes)
    }

    '{0} | {1} bytes | {2}' -f $names[$index], $bytes.Length, $hash
}
