param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$Compiler = Join-Path $RepositoryRoot 'artifacts\compiler\smilec.exe'
$WebRunner = Join-Path $RepositoryRoot 'scripts\run-web-test.js'
$ProofImage = Join-Path $RepositoryRoot 'examples\MenuGallery\Assets\Cursor.png'
$RunRoot = Join-Path $RepositoryRoot ('artifacts\temp\fixed-array-hardening\run-' + [Guid]::NewGuid().ToString('N'))
$SourceRoot = Join-Path $RunRoot 'sources'
$NativeRoot = Join-Path $RunRoot 'native'
$WebRoot = Join-Path $RunRoot 'web'
$Utf8 = [Text.UTF8Encoding]::new($false)

if (-not (Test-Path -LiteralPath $Compiler)) {
    throw "SMILE compiler not found at '$Compiler'. Run scripts\build.cmd first."
}
if (-not (Test-Path -LiteralPath $ProofImage)) {
    throw "Disposable proof image not found at '$ProofImage'."
}

New-Item -ItemType Directory -Force -Path $SourceRoot, $NativeRoot, $WebRoot | Out-Null

function Write-Utf8File {
    param(
        [string]$Path,
        [string]$Content
    )

    [IO.File]::WriteAllText($Path, $Content, $Utf8)
}

function Invoke-Compiler {
    param([string[]]$Arguments)

    & $Compiler @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "smilec failed with exit code ${LASTEXITCODE}: $($Arguments -join ' ')"
    }
}

function Assert-Lines {
    param(
        [string]$Name,
        [string[]]$Expected,
        [string[]]$Actual
    )

    $Difference = Compare-Object -ReferenceObject $Expected -DifferenceObject $Actual -SyncWindow 0
    if ($Difference) {
        throw "$Name output differed.`nEXPECTED:`n$($Expected -join "`n")`nACTUAL:`n$($Actual -join "`n")"
    }
}

function Invoke-NativeFixture {
    param(
        [string]$Name,
        [string]$SourcePath,
        [string[]]$Expected,
        [int]$ExpectedExit
    )

    $Executable = Join-Path $NativeRoot "$Name.exe"
    $StandardOutput = Join-Path $NativeRoot "$Name.stdout.txt"
    $StandardError = Join-Path $NativeRoot "$Name.stderr.txt"
    $AssetDirectory = Join-Path $NativeRoot 'Assets'
    New-Item -ItemType Directory -Force -Path $AssetDirectory | Out-Null
    Copy-Item -LiteralPath $ProofImage -Destination (Join-Path $AssetDirectory 'Proof.png') -Force

    Invoke-Compiler @($SourcePath, '--target', 'windows-x64', '--configuration', $Configuration,
        '--graphics', 'GDI', '-o', $Executable)

    $PreviousClassDiagnostics = [Environment]::GetEnvironmentVariable(
        'SMILE_CLASS_LIFETIME_DIAGNOSTICS', 'Process')
    $PreviousImageDiagnostics = [Environment]::GetEnvironmentVariable(
        'SMILE_IMAGE_LIFETIME_DIAGNOSTICS', 'Process')
    $PreviousTextDiagnostics = [Environment]::GetEnvironmentVariable(
        'SMILE_TEXT_LIFETIME_DIAGNOSTICS', 'Process')
    try {
        $env:SMILE_CLASS_LIFETIME_DIAGNOSTICS = '1'
        $env:SMILE_IMAGE_LIFETIME_DIAGNOSTICS = '1'
        $env:SMILE_TEXT_LIFETIME_DIAGNOSTICS = '1'
        $Process = Start-Process -FilePath $Executable -WorkingDirectory $NativeRoot -Wait -PassThru `
            -NoNewWindow -RedirectStandardOutput $StandardOutput -RedirectStandardError $StandardError
    }
    finally {
        [Environment]::SetEnvironmentVariable(
            'SMILE_CLASS_LIFETIME_DIAGNOSTICS', $PreviousClassDiagnostics, 'Process')
        [Environment]::SetEnvironmentVariable(
            'SMILE_IMAGE_LIFETIME_DIAGNOSTICS', $PreviousImageDiagnostics, 'Process')
        [Environment]::SetEnvironmentVariable(
            'SMILE_TEXT_LIFETIME_DIAGNOSTICS', $PreviousTextDiagnostics, 'Process')
    }

    if ($Process.ExitCode -ne $ExpectedExit) {
        throw "$Name exited with $($Process.ExitCode); expected $ExpectedExit."
    }
    $ErrorLines = [IO.File]::ReadAllLines($StandardError, [Text.Encoding]::UTF8)
    if ($ErrorLines.Count -ne 0) {
        throw "$Name wrote unexpected stderr: $($ErrorLines -join ' | ')"
    }
    $Diagnostics = @('SMILE_CLASS_LIVE=0', 'SMILE_IMAGE_LIVE=0', 'SMILE_TEXT_LIVE=0')
    $Actual = [IO.File]::ReadAllLines($StandardOutput, [Text.Encoding]::UTF8)
    Assert-Lines $Name ($Expected + $Diagnostics) $Actual
}

function Invoke-WebFixture {
    param(
        [string]$Name,
        [string]$SourcePath,
        [string]$ExpectedPath = '',
        [string]$ExpectedRuntimeError = ''
    )

    $OutputDirectory = Join-Path $WebRoot $Name
    Invoke-Compiler @($SourcePath, '--target', 'web', '--configuration', $Configuration,
        '--output-dir', $OutputDirectory)
    $AssetDirectory = Join-Path $OutputDirectory 'Assets'
    New-Item -ItemType Directory -Force -Path $AssetDirectory | Out-Null
    Copy-Item -LiteralPath $ProofImage -Destination (Join-Path $AssetDirectory 'Proof.png') -Force

    & node --check (Join-Path $OutputDirectory 'game.js')
    if ($LASTEXITCODE -ne 0) {
        throw "$Name generated invalid JavaScript."
    }
    $Arguments = @($WebRunner, $OutputDirectory, '--timeout', '10000')
    if ($ExpectedPath.Length -ne 0) {
        $Arguments += @('--expected', $ExpectedPath)
    }
    if ($ExpectedRuntimeError.Length -ne 0) {
        $Arguments += @('--expected-runtime-error', $ExpectedRuntimeError)
    }
    & node @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Name Web execution failed."
    }
}

function Add-Fixture {
    param(
        [string]$Name,
        [string]$Source,
        [string[]]$Expected,
        [int]$ExpectedExit = 0,
        [string]$ExpectedRuntimeError = ''
    )

    $SourcePath = Join-Path $SourceRoot "$Name.smile"
    $ExpectedPath = Join-Path $SourceRoot "$Name.expected.txt"
    Write-Utf8File $SourcePath $Source
    Write-Utf8File $ExpectedPath (($Expected -join "`n") + "`n")
    Invoke-NativeFixture $Name $SourcePath $Expected $ExpectedExit
    if ($ExpectedRuntimeError.Length -eq 0) {
        Invoke-WebFixture $Name $SourcePath $ExpectedPath
    }
    else {
        Invoke-WebFixture $Name $SourcePath -ExpectedRuntimeError $ExpectedRuntimeError
    }
}

$ValidSource = @'
Option Explicit

Type Buffer

    Values[10000] As Number

End Type

Type Bag

    Values[2, 3] As Number

End Type

Dim First As Buffer
Dim Second As Buffer
Dim Shared As Bag
Dim Trace As Number

First.Values[0] = 11
First.Values[9999] = 99
Second = First
Shared.Values[FirstIndex(), SecondIndex()] = 7

Call SetValue(Shared.Values[CaptureIndex(), 2], LaterValue())

Print Second.Values[0]
Print Second.Values[9999]
Print Shared.Values[1, 2]
Print Shared.Values[0, 2]
Print Trace

Function FirstIndex() As Number

    Trace = Trace * 10 + 1

    Return 1

End Function

Function SecondIndex() As Number

    Trace = Trace * 10 + 2

    Return 2

End Function

Function CaptureIndex() As Number

    Trace = Trace * 10 + 3

    Return 0

End Function

Function LaterValue() As Number

    Trace = Trace * 10 + 4

    Return 42

End Function

Sub SetValue(ByRef Value As Number, NewValue As Number)

    Value = NewValue

End Sub
'@

$OwnershipSource = @'
Option Explicit

Game Window "Fixed Array Ownership" Size 320 By 180

Type Payload

    Values[2] As Number
    Images[2] As Image

End Type

Type Envelope

    Items[2] As Payload

End Type

Type Factory

    Public Property Created As Envelope

        Get

            Dim ReturnValue As Envelope

            Load Image ReturnValue.Items[0].Images[1] From "Assets\Proof.png"
            Load Image ReturnValue.Items[1].Images[0] From "Assets\Proof.png"

            Return ReturnValue

        End Get

    End Property

End Type

Dim Builder As Factory
Dim Original As Envelope
Dim SelectedFromFunction As Image
Dim SelectedFromProperty As Image
Dim Borrowed As Image

Load Image Original.Items[0].Images[0] From "Assets\Proof.png"
Original.Items[0].Values[0] = 10
SelectedFromFunction = MakeEnvelope().Items[1].Images[0]
SelectedFromProperty = Builder.Created.Items[1].Images[0]
Borrowed = Original.Items[0].Images[0]

Call ChangeCopy(Original)

Print Original.Items[0].Values[0]

Call ChangeOriginal(Original)

Print Original.Items[0].Values[0]
Print Image_Loaded(SelectedFromFunction)
Print Image_Loaded(SelectedFromProperty)
Print Image_Loaded(Borrowed)
Print Image_Loaded(Original.Items[0].Images[0])

Unload Image Borrowed
Unload Image SelectedFromProperty
Unload Image SelectedFromFunction
Unload Image Original.Items[0].Images[0]

Function MakeEnvelope() As Envelope

    Dim ReturnValue As Envelope

    Load Image ReturnValue.Items[0].Images[1] From "Assets\Proof.png"
    Load Image ReturnValue.Items[1].Images[0] From "Assets\Proof.png"

    Return ReturnValue

End Function

Sub ChangeCopy(Value As Envelope)

    Value.Items[0].Values[0] = 20

End Sub

Sub ChangeOriginal(ByRef Value As Envelope)

    Value.Items[0].Values[0] = 30

End Sub
'@

$FailureTemplate = @'
Option Explicit

Type Payload

    Values[2, 3] As Number
    Images[2] As Image

End Type

Type Envelope

    Items[2] As Payload

End Type

Class Holder

    Public Values[2, 3] As Number

End Class

Dim Shared As Payload
Dim Standalone[2, 3] As Number
Dim Nested As Envelope
Dim Owner As New Holder()

Print "Before"

__STATEMENT__

Print "After"

Sub SetValue(ByRef Value As Number)

    Value = 9

End Sub
'@

$ReturnedFailureSource = @'
Option Explicit

Game Window "Fixed Array Failure Ownership" Size 320 By 180

Type Payload

    Images[2] As Image

End Type

Type Envelope

    Items[2] As Payload

End Type

Dim Selected As Image

Print "Before"

Selected = MakeEnvelope().Items[2].Images[0]

Print "After"

Function MakeEnvelope() As Envelope

    Dim ReturnValue As Envelope

    Load Image ReturnValue.Items[0].Images[0] From "Assets\Proof.png"
    Load Image ReturnValue.Items[1].Images[0] From "Assets\Proof.png"

    Return ReturnValue

End Function
'@

Add-Fixture 'ValidBoundsAndHelpers' $ValidSource @('11', '99', '7', '42', '1234')
Add-Fixture 'ReturnedRecordOwnership' $OwnershipSource @('10', '30', 'True', 'True', 'True', 'True')

$WebArrayError = 'SMILE Web array index'
$FailureCases = [ordered]@{
    'InvalidFieldReadNegative' = @('Print Shared.Values[-1, 0]', -1, 1, 2)
    'InvalidFieldWriteUpper' = @('Shared.Values[0, 3] = 1', 3, 2, 3)
    'InvalidFieldByRefUpper' = @('Call SetValue(Shared.Values[2, 0])', 2, 1, 2)
    'InvalidStandaloneLarge' = @('Print Standalone[9007199254740991, 0]', 9007199254740991, 1, 2)
    'InvalidNestedRowByRef' = @('Call SetValue(Nested.Items[2].Values[0, 0])', 2, 1, 2)
    'InvalidClassColumnByRef' = @('Call SetValue(Owner.Values[0, -1])', -1, 2, 3)
}
foreach ($Case in $FailureCases.GetEnumerator()) {
    $Source = $FailureTemplate.Replace('__STATEMENT__', $Case.Value[0])
    $ArrayError = "SMILE runtime error: Array index $($Case.Value[1]) is outside dimension " +
        "$($Case.Value[2]) (size $($Case.Value[3]))."
    Add-Fixture $Case.Key $Source @('Before', $ArrayError) 4 $WebArrayError
}
$ReturnedArrayError = 'SMILE runtime error: Array index 2 is outside dimension 1 (size 2).'
Add-Fixture 'InvalidReturnedRecordOwnership' $ReturnedFailureSource @('Before', $ReturnedArrayError) 4 `
    $WebArrayError

Write-Output "Fixed-array native/Web hardening passed with disposable artifacts at '$RunRoot'."
