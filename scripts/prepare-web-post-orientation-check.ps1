[CmdletBinding()]
param(
    [string]$WebDirectory = 'tools/Character3DViewer/bin/Release/Web',
    [string]$OutputDirectory = 'artifacts/web/PostOrientationCheck'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$sourcePath = Join-Path $root (Join-Path $WebDirectory 'smile-runtime.js')
$source = Get-Content -LiteralPath $sourcePath -Raw
$start = $source.IndexOf('if ((renderer3DPostRequested || renderer3DRequestedSamples > 1')
if ($start -lt 0) { throw 'Generated post-processing shader owner was not found.' }
$end = $source.IndexOf('renderer3DPostProgram={handle}', $start)
if ($end -lt 0) { throw 'Generated post-processing shader owner is incomplete.' }
$shaders = [regex]::Matches($source.Substring($start, $end - $start),
    'renderer3DCompile\(gl, gl\.(?:VERTEX_SHADER|FRAGMENT_SHADER), `([\s\S]*?)`\);')
if ($shaders.Count -ne 2) { throw 'Expected the actual generated vertex and fragment shaders.' }
$vertex = ConvertTo-Json $shaders[0].Groups[1].Value -Compress
$fragment = ConvertTo-Json $shaders[1].Groups[1].Value -Compress
$hash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
$html = @'
<!doctype html>
<meta charset="utf-8">
<title>SMILE Web post-processing orientation check</title>
<style>body{font:18px system-ui;background:#17202b;color:white;padding:24px}pre{white-space:pre-wrap}canvas{display:none}</style>
<h1>Generated Web shader orientation</h1>
<p>Two-by-two GPU pixels from the generated shader. Scene copies preserve framebuffer orientation; imported backdrops reverse image rows.</p>
<pre id="result">Running</pre><canvas id="gpu" width="2" height="2"></canvas>
<script>
try {
    const gl = document.getElementById('gpu').getContext('webgl2');
    if (!gl) throw Error('WebGL2 unavailable');
    const compile = (type, source) => {
        const shader = gl.createShader(type);
        gl.shaderSource(shader, source); gl.compileShader(shader);
        if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) throw Error(gl.getShaderInfoLog(shader));
        return shader;
    };
    const program = gl.createProgram();
    gl.attachShader(program, compile(gl.VERTEX_SHADER, __VERTEX__));
    gl.attachShader(program, compile(gl.FRAGMENT_SHADER, __FRAGMENT__));
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) throw Error(gl.getProgramInfoLog(program));
    gl.useProgram(program);
    const texture = gl.createTexture(); gl.bindTexture(gl.TEXTURE_2D, texture);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, 2, 2, 0, gl.RGBA, gl.UNSIGNED_BYTE,
        new Uint8Array([255,0,0,255,255,0,0,255,0,0,255,255,0,0,255,255]));
    gl.uniform1i(gl.getUniformLocation(program, 'sceneTexture'), 0);
    gl.uniform1i(gl.getUniformLocation(program, 'bloomTexture'), 0);
    gl.uniform4f(gl.getUniformLocation(program, 'second'), 0, 1, 0, 0);
    const lines = [], pixels = new Uint8Array(16);
    let failures = 0;
    for (const [mode, flipped, label] of [[4,false,'LDR scene copy'],[3,false,'HDR presentation'],
        [7,true,'LDR image backdrop'],[6,true,'HDR image backdrop']]) {
        gl.uniform4f(gl.getUniformLocation(program, 'first'), mode, 0, 0, 0);
        gl.drawArrays(gl.TRIANGLES, 0, 3);
        gl.readPixels(0, 0, 2, 2, gl.RGBA, gl.UNSIGNED_BYTE, pixels);
        const bottomRed = pixels[0] > pixels[2], topRed = pixels[8] > pixels[10];
        const error = gl.getError();
        const passed = bottomRed === !flipped && topRed === flipped && error === gl.NO_ERROR;
        if (!passed) failures++;
        lines.push(`${passed ? 'PASS' : 'FAIL'} ${label}: bottom=${Array.from(pixels.slice(0,4))}, top=${Array.from(pixels.slice(8,12))}, GL=${error}`);
    }
    document.getElementById('result').textContent = `${failures ? 'FAIL' : 'PASS'}: ${4-failures}/4\n${lines.join('\n')}\nGenerated smile-runtime.js SHA-256: __HASH__`;
} catch (error) { document.getElementById('result').textContent = `FAIL: ${error.message}`; }
</script>
'@
$html = $html.Replace('__VERTEX__', $vertex).Replace('__FRAGMENT__', $fragment).Replace('__HASH__', $hash)
$output = Join-Path $root $OutputDirectory
New-Item -ItemType Directory -Path $output -Force | Out-Null
[IO.File]::WriteAllText((Join-Path $output 'index.html'), $html, [Text.UTF8Encoding]::new($false))
Write-Host "Serve and open $output/index.html in installed Chrome. Require PASS: 4/4."
Write-Host "Generated source SHA-256: $hash"
