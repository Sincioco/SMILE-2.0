param([switch]$SkipSuccess)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$Compiler = Join-Path $Root 'artifacts/compiler/smilec.exe'
$Run = Join-Path $Root ('artifacts/temp/double/run-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $Run -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $Run 'Assets') -Force | Out-Null
Copy-Item (Join-Path $Root 'examples/MenuGallery/Assets/Cursor.png') (Join-Path $Run 'Assets/Proof.png')
$Utf8 = [Text.UTF8Encoding]::new($false)

function Compile([string[]]$Arguments) {
    if ($Arguments[0].EndsWith('.smileproj')) { $Arguments = @('--project') + $Arguments }
    & $Compiler @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Double compilation failed: $Arguments" }
}

function Native([string]$Source, [string]$Name, [string]$Expected, [string]$Failure = '',
    [switch]$Debug, [string]$FailureExpression = '') {
    $Exe = Join-Path $Run "$Name.exe"
    $CompilerArguments = @($Source, '--graphics', 'GDI', '-o', $Exe)
    if ($Debug) { $CompilerArguments += @('--debug', '--configuration', 'Debug', '--keep-temp') }
    Compile $CompilerArguments
    $Variables = @('SMILE_CLASS_LIFETIME_DIAGNOSTICS', 'SMILE_IMAGE_LIFETIME_DIAGNOSTICS',
        'SMILE_TEXT_LIFETIME_DIAGNOSTICS')
    $Previous = @{}
    try {
        foreach ($Variable in $Variables) {
            $Previous[$Variable] = [Environment]::GetEnvironmentVariable($Variable, 'Process')
            [Environment]::SetEnvironmentVariable($Variable, '1', 'Process')
        }
        Push-Location $Run
        try {
            $Output = @(& $Exe 2>&1)
            $Code = $LASTEXITCODE
        }
        finally { Pop-Location }
    }
    finally {
        foreach ($Variable in $Variables) {
            [Environment]::SetEnvironmentVariable($Variable, $Previous[$Variable], 'Process')
        }
    }
    [IO.File]::WriteAllLines((Join-Path $Run "$Name.native.txt"), [string[]]$Output, $Utf8)
    $ExpectedCode = if ($Failure) { 5 } else { 0 }
    if ($Code -ne $ExpectedCode) { throw "$Name exit $Code, expected $ExpectedCode. $Output" }
    $Text = ($Output -join "`n") + "`n"
    $Diagnostics = "SMILE_CLASS_LIVE=0`nSMILE_IMAGE_LIVE=0`nSMILE_TEXT_LIVE=0`n"
    if ($Failure) {
        $Location = [regex]::Escape([IO.Path]::GetFullPath($Source)) + '\(\d+,\d+\)'
        if ($FailureExpression) {
            $SourceText = [IO.File]::ReadAllText($Source)
            $Start = $SourceText.IndexOf($FailureExpression, [StringComparison]::Ordinal)
            if ($Start -lt 0) { throw "$Name has no expected failing expression." }
            $Prefix = $SourceText.Substring(0, $Start)
            $Line = ($Prefix -split "`n").Count
            $Column = $Start - $Prefix.LastIndexOf("`n", [StringComparison]::Ordinal)
            $Location = [regex]::Escape("$([IO.Path]::GetFullPath($Source))($Line,$Column)")
        }
        $Pattern = '^' + [regex]::Escape($Expected) + $Location + ': error SML3902: ' +
            [regex]::Escape($Failure) + '\n' + [regex]::Escape($Diagnostics) + '$'
        if ($Text -notmatch $Pattern) { throw "$Name failure output/cleanup differed: $Text" }
    }
    elseif ($Text -cne ($Expected + $Diagnostics)) { throw "$Name native output differed: $Text" }
}

function Web([string]$Source, [string]$Name, [string]$Expected, [string]$Failure = '') {
    $Directory = Join-Path $Run "$Name-web"
    Compile @($Source, '--target', 'web', '--output-dir', $Directory)
    New-Item -ItemType Directory -Path (Join-Path $Directory 'Assets') -Force | Out-Null
    Copy-Item (Join-Path $Run 'Assets/Proof.png') (Join-Path $Directory 'Assets/Proof.png')
    $ExpectedFile = Join-Path $Run "$Name.expected.txt"
    [IO.File]::WriteAllText($ExpectedFile, $Expected, $Utf8)
    $Arguments = @((Join-Path $Root 'scripts/run-web-test.js'), $Directory, '--expected', $ExpectedFile)
    if ($Failure) { $Arguments += @('--expected-runtime-error', $Failure) }
    & node @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Name Web execution failed." }
}

if (-not $SkipSuccess) {
    $Unicode = Join-Path $Root 'examples/DoubleTests/UnicodeDebug.smile'
    Native $Unicode 'UnicodeRelease' "0.125`n0.25`n0.5`n"
    Native $Unicode 'UnicodeDebug' "0.125`n0.25`n0.5`n" -Debug
    $Fixture = Join-Path $Root 'examples/DoubleTests/Program.smile'
    Native $Fixture 'Success' "0.002`nDouble tests passed`n"
    Web $Fixture 'Success' "0.002`nDouble tests passed`n"
    $PackageProject = Join-Path $Root 'examples/DoubleTests/DoubleProof.smilelibproj'
    $Package = Join-Path $Root 'examples/DoubleTests/bin/Debug/Smile.Double.Proof.smilelib'
    Compile @('--project', $PackageProject, '--target', 'library', '-o', $Package)
    $Hash = (Get-FileHash $Package).Hash
    Compile @('--project', $PackageProject, '--target', 'library', '-o', $Package)
    if ((Get-FileHash $Package).Hash -cne $Hash) { throw 'Double package is not deterministic.' }
    foreach ($Project in @('PackageConsumer.smileproj', 'PackageConsumer.Package.smileproj')) {
        $Source = Join-Path $Root "examples/DoubleTests/$Project"
        Native $Source $Project "True`nTrue`nTrue`nTrue`nTrue`n"
        Web $Source $Project "True`nTrue`nTrue`nTrue`nTrue`n"
    }
    Write-Host "Double package SHA-256: $Hash"
}

# Each row exercises an actual checked operation in generated code. The shared
# fixtures cover successful subnormal, signed-zero, call and storage behavior.
$Cases = @(
    @('DividePositiveZero', '0.0', '1.0 / Value', 'Double division by zero.'),
    @('DivideNegativeZero', '-0.0', '1.0 / Value', 'Double division by zero.'),
    @('Overflow', '1e308', 'Value * 2.0', 'Nonfinite Double result.'),
    @('SquareRootDomain', '-0.25', 'Sqrt(Value)', 'Double domain error.'),
    @('ClampDomain', '0.25', 'Clamp(Value, 2.0, 1.0)', 'Double domain error.'),
    @('NativeUpperBound', '9223372036854775808.0', 'ToDouble(ToNumber(Value))', 'Double conversion out of range.'),
    @('NativeBelowLower', '-9223372036854777856.0', 'ToDouble(ToNumber(Value))', 'Double conversion out of range.')
)
foreach ($Invalid in @('', '1tail', '0x10', 'NaN', 'Infinity', '1,5', '1e999', '1.')) {
    $Cases += ,@(('Text' + $Cases.Count), '0.0', ('Text_To_Double("' + $Invalid + '")'), 'Invalid Double text.')
}
foreach ($Case in $Cases) {
    $Source = Join-Path $Run ($Case[0] + '.smile')
    $Program = @"
Option Explicit

Type OwnedState

    Values[2] As Double
    Picture As Image

End Type

Class Holder

    Public Value As Double
    Public Label As Text
    Public State As OwnedState

End Class

Dim Item As New Holder()
Dim Value As Double

Item.Value = 7.25
Item.Label = "Owned value"
Load Image Item.State.Picture From "Assets\Proof.png"

If Not Image_Loaded(Item.State.Picture) Then
    Print "Image load failed"
End If

Value = $($Case[1])

Call Update(Item.Value)

Print "After failure"

Sub Update(ByRef Destination As Double)

    Dim Owned As Text

    Owned = "Local value"
    Print "Before failure"
    Destination = $($Case[2])
    Print "After write"

End Sub
"@
    [IO.File]::WriteAllText($Source, $Program, $Utf8)
    $FailureExpression = if ($Case[0] -like 'Native*') { 'ToNumber(Value)' } else { $Case[2] }
    Native $Source $Case[0] "Before failure`n" $Case[3] -FailureExpression $FailureExpression
    Web $Source $Case[0] "Before failure`n" $Case[3]
}

$NativeBounds = Join-Path $Run 'NativeBounds.smile'
[IO.File]::WriteAllText($NativeBounds, @'
Dim Value As Double

Value = -9223372036854775808.0
Print ToNumber(Value)
Print ToDouble(9007199254740993) = 9007199254740992.0
'@, $Utf8)
Native $NativeBounds 'NativeBounds' "-9223372036854775808`nTrue`n"
$WebBounds = Join-Path $Run 'WebBounds.smile'
[IO.File]::WriteAllText($WebBounds, @'
Dim Value As Double

Value = 9007199254740991.0
Print ToNumber(Value) = 9007199254740991
Value = -9007199254740991.0
Print ToNumber(Value) = -9007199254740991
Value = 9007199254740992.0
Print ToNumber(Value)
'@, $Utf8)
Web $WebBounds 'WebBounds' "True`nTrue`n" 'Double conversion out of range.'
Write-Host "Double native/Web focused gate passed. Evidence: $Run"
