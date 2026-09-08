param([Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot '..\assets\branding\smile-2.0-logo-web.png'
$bytes = [IO.File]::ReadAllBytes($source)
$text = [Text.StringBuilder]::new()
[void]$text.AppendLine('// Generated from the authorized canonical-logo derivative. Do not edit.')
[void]$text.AppendLine('static const unsigned char smile_startup_logo[] = {')
for ($offset = 0; $offset -lt $bytes.Length; $offset += 32) {
    $end = [Math]::Min($offset + 32, $bytes.Length)
    for ($index = $offset; $index -lt $end; $index++) {
        [void]$text.Append($bytes[$index]).Append(',')
    }
    [void]$text.AppendLine()
}
[void]$text.AppendLine('};')
[void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($OutputPath)))
[IO.File]::WriteAllText($OutputPath, $text.ToString(), [Text.UTF8Encoding]::new($false))
