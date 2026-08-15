param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$FormatterSource = Join-Path $PSScriptRoot 'format-smile-style.ps1'
$ReleaseAssembly = Join-Path $RepositoryRoot 'src\Smile.Language\bin\Release\netstandard2.0\Smile.Language.dll'
$DebugAssembly = Join-Path $RepositoryRoot 'src\Smile.Language\bin\Debug\netstandard2.0\Smile.Language.dll'
$LanguageAssembly = if (Test-Path -LiteralPath $ReleaseAssembly) { $ReleaseAssembly } else { $DebugAssembly }
$Utf8WithoutBom = [Text.UTF8Encoding]::new($false)
$TestRoot = Join-Path $env:TEMP ("smile-formatter-tests-" + [Guid]::NewGuid().ToString('N'))
$FormatterPath = Join-Path $TestRoot 'scripts\format-smile-style.ps1'
$Passed = 0

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw $Message
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)

    if ($Expected -cne $Actual) {
        throw "$Message Expected '$Expected', found '$Actual'."
    }
}

function Write-TestSource {
    param([string]$RelativePath, [string]$Text)

    $Path = Join-Path $TestRoot $RelativePath
    [IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
    [IO.File]::WriteAllText($Path, $Text, $Utf8WithoutBom)
    return $Path
}

function Invoke-Formatter {
    param([string[]]$Arguments)

    $PreviousErrorAction = $ErrorActionPreference

    try {
        $ErrorActionPreference = 'Continue'
        $Output = @(& powershell -NoProfile -ExecutionPolicy Bypass -File $FormatterPath @Arguments 2>&1)
        $ExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $PreviousErrorAction
    }

    return [pscustomobject]@{ ExitCode = $ExitCode; Output = ($Output -join "`n") }
}

function Invoke-FormatterCommand {
    param([string]$Command)

    $PreviousErrorAction = $ErrorActionPreference

    try {
        $ErrorActionPreference = 'Continue'
        $Output = @(& powershell -NoProfile -ExecutionPolicy Bypass -Command $Command 2>&1)
        $ExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $PreviousErrorAction
    }

    return [pscustomobject]@{ ExitCode = $ExitCode; Output = ($Output -join "`n") }
}

function Pass {
    param([string]$Name)

    $script:Passed++
    Write-Host "PASS: $Name"
}

if (-not (Test-Path -LiteralPath $LanguageAssembly)) {
    throw 'Smile.Language must be built before the formatter integration tests run.'
}

try {
    [IO.Directory]::CreateDirectory((Join-Path $TestRoot 'scripts')) | Out-Null
    [IO.Directory]::CreateDirectory((Join-Path $TestRoot 'src\Smile.Language\bin\Debug\netstandard2.0')) | Out-Null
    Copy-Item -LiteralPath $FormatterSource -Destination $FormatterPath
    Copy-Item -LiteralPath $LanguageAssembly -Destination (Join-Path $TestRoot 'src\Smile.Language\bin\Debug\netstandard2.0\Smile.Language.dll')

    & git -C $TestRoot init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Unable to initialize the temporary formatter Git fixture.' }

    Write-TestSource 'Tracked.smile' "Option Explicit`n" | Out-Null
    & git -C $TestRoot add -- Tracked.smile
    if ($LASTEXITCODE -ne 0) { throw 'Unable to track the formatter fixture.' }

    $Untracked = Write-TestSource 'Untracked.smile' "Option Explicit`n`nFunction Add() As Number`n`n    Return 1 + 2`n`nEnd Function`n"
    $DefaultCheck = Invoke-Formatter @('-Check', '-FormatLongIf')
    Assert-Equal 0 $DefaultCheck.ExitCode 'Default scope did not ignore an untracked .smile file.'
    $IncludeCheck = Invoke-Formatter @('-Check', '-FormatLongIf', '-IncludeUntracked')
    Assert-True ($IncludeCheck.ExitCode -ne 0) '-IncludeUntracked did not include formatter drift.'
    $ExplicitCheck = Invoke-Formatter @('-Check', '-FormatLongIf', '-Files', 'Untracked.smile')
    Assert-True ($ExplicitCheck.ExitCode -ne 0) '-Files did not explicitly target an untracked file.'
    $ExplicitFormat = Invoke-Formatter @('-FormatLongIf', '-Files', 'Untracked.smile')
    Assert-Equal 0 $ExplicitFormat.ExitCode ("Explicit untracked formatting failed: " + $ExplicitFormat.Output)
    $DuplicateFormat = Invoke-FormatterCommand ("& '" + $FormatterPath + "' -FormatLongIf -Files @('Untracked.smile','Untracked.smile')")
    Assert-Equal 0 $DuplicateFormat.ExitCode ("Duplicate explicit formatting failed: " + $DuplicateFormat.Output)
    Assert-True ($DuplicateFormat.Output.Contains('of 1 SMILE file(s).')) 'Duplicate explicit targets were not deduplicated.'
    Assert-Equal 0 (Invoke-Formatter @('-Check', '-FormatLongIf', '-Files', 'Untracked.smile')).ExitCode 'Explicitly formatted source did not pass Check.'
    Assert-True ((Invoke-Formatter @('-Check', '-Files', 'Missing.smile')).ExitCode -ne 0) 'A missing explicit file did not fail clearly.'
    Pass 'tracked, untracked, explicit, missing, and deduplicated scope behavior'

    $Drift = Write-TestSource 'Drift.smile' "Option Explicit`n`nFunction Multiply() As Number`n`n    Return 2 * 3`n`nEnd Function`n"
    & git -C $TestRoot add -- Drift.smile
    $BeforeBytes = [IO.File]::ReadAllBytes($Drift)
    $BeforeTimestamp = [IO.File]::GetLastWriteTimeUtc($Drift)
    $BeforeFiles = @(Get-ChildItem -LiteralPath $TestRoot -Recurse -File | Select-Object -ExpandProperty FullName)
    $DriftCheck = Invoke-Formatter @('-Check', '-FormatLongIf')
    Assert-True ($DriftCheck.ExitCode -ne 0) 'Check did not report tracked formatter drift.'
    Assert-Equal ([Convert]::ToBase64String($BeforeBytes)) ([Convert]::ToBase64String([IO.File]::ReadAllBytes($Drift))) 'Check changed source bytes.'
    Assert-Equal $BeforeTimestamp ([IO.File]::GetLastWriteTimeUtc($Drift)) 'Check changed the source timestamp.'
    $AfterFiles = @(Get-ChildItem -LiteralPath $TestRoot -Recurse -File | Select-Object -ExpandProperty FullName)
    Assert-Equal ($BeforeFiles -join '|') ($AfterFiles -join '|') 'Check generated a file.'
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Drift.smile')).ExitCode 'Formatting tracked drift failed.'
    Assert-Equal 0 (Invoke-Formatter @('-Check', '-FormatLongIf')).ExitCode 'Repository style gate did not pass after formatting drift.'
    Pass 'read-only Check failure, restoration, bytes, timestamps, and generated-file safety'

    $First = Write-TestSource 'AFirst.smile' "Option Explicit`n`nFunction First() As Number`n`n    Return 4 + 5`n`nEnd Function`n"
    $Bad = Write-TestSource 'ZBad.smile' "Option Explicit`n`nFunction Broken()`n`n    Return Missing()`n`nEnd Function`n"
    $FirstBefore = [IO.File]::ReadAllBytes($First)
    $BadBefore = [IO.File]::ReadAllBytes($Bad)
    $Transaction = Invoke-FormatterCommand ("& '" + $FormatterPath + "' -FormatLongIf -Files @('AFirst.smile','ZBad.smile')")
    Assert-True ($Transaction.ExitCode -ne 0) 'Unsafe transformation did not fail preflight.'
    Assert-Equal ([Convert]::ToBase64String($FirstBefore)) ([Convert]::ToBase64String([IO.File]::ReadAllBytes($First))) 'A preflight failure partially wrote the first target.'
    Assert-Equal ([Convert]::ToBase64String($BadBefore)) ([Convert]::ToBase64String([IO.File]::ReadAllBytes($Bad))) 'A preflight failure changed the failing target.'
    Assert-Equal 0 @(Get-ChildItem -LiteralPath (Join-Path $TestRoot 'artifacts') -Recurse -File -ErrorAction SilentlyContinue).Count 'A preflight failure left staging files.'
    Pass 'all-target preflight prevents partial writes'

    $Concurrent = Write-TestSource 'Concurrent.smile' "Option Explicit`n`nFunction Concurrent() As Number`n`n    Return 6 + 7`n`nEnd Function`n"
    $ConcurrentBefore = [IO.File]::ReadAllText($Concurrent)
    $ConcurrentCommand = "& '" + $FormatterPath +
        "' -FormatLongIf -Files 'Concurrent.smile' -BeforeCommitTestHook { param(`$States) " +
        "[IO.File]::AppendAllText(`$States[0].FullPath, 'External change' + [Environment]::NewLine) }"
    $ConcurrentResult = Invoke-FormatterCommand $ConcurrentCommand
    Assert-True ($ConcurrentResult.ExitCode -ne 0 -and $ConcurrentResult.Output.Contains('changed after preflight')) 'A concurrent target change did not fail the commit hash check.'
    Assert-Equal ($ConcurrentBefore + 'External change' + [Environment]::NewLine) ([IO.File]::ReadAllText($Concurrent)) 'Formatter overwrote a concurrent external change.'
    Pass 'concurrent-change hash mismatch blocks formatter writes'

    $Emoji = [char]::ConvertFromUtf32(0x1F600)
    $World = [string][char]0x4E16 + [string][char]0x754C
    $Quote = [char]34
    $EncodingSource = "Option Explicit`r`n`r`n' Unicode $Emoji stays`r`n" +
        "Dim Message As Text`r`n`r`nMessage = $Quote" + "Hello`r`n$World $Emoji`r`n$Quote" + "`r`n`r`n" +
        "Function Total() As Number`r`n`r`n    Return (`r`n        1 +`r`n`r`n        2`r`n    )`r`n`r`nEnd Function`r`n"
    $Encoding = Write-TestSource 'Encoding.smile' $EncodingSource
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Encoding.smile')).ExitCode 'First encoding/idempotence pass failed.'
    $FirstPass = [IO.File]::ReadAllBytes($Encoding)
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Encoding.smile')).ExitCode 'Second encoding/idempotence pass failed.'
    $SecondPass = [IO.File]::ReadAllBytes($Encoding)
    Assert-Equal 0 (Invoke-Formatter @('-FormatLongIf', '-Files', 'Encoding.smile')).ExitCode 'Third encoding/idempotence pass failed.'
    $ThirdPass = [IO.File]::ReadAllBytes($Encoding)
    Assert-True (([Convert]::ToBase64String($FirstPass) -ceq [Convert]::ToBase64String($SecondPass)) -and
        ([Convert]::ToBase64String($SecondPass) -ceq [Convert]::ToBase64String($ThirdPass))) 'Three formatter passes were not byte-idempotent.'
    Assert-True (-not ($FirstPass.Length -ge 3 -and $FirstPass[0] -eq 0xEF -and $FirstPass[1] -eq 0xBB -and $FirstPass[2] -eq 0xBF)) 'Formatter wrote a UTF-8 BOM.'
    $EncodingText = [Text.Encoding]::UTF8.GetString($FirstPass)
    Assert-True (-not $EncodingText.Contains("`r")) 'Formatter did not normalize to LF.'
    Assert-True ($EncodingText.EndsWith("`n") -and -not $EncodingText.EndsWith("`n`n")) 'Formatter did not write exactly one final newline.'
    Assert-True ($EncodingText.Contains("Hello`n$World $Emoji`n$Quote")) 'Formatter changed a multiline Unicode string value.'
    Assert-Equal 1 ([regex]::Matches($EncodingText, [regex]::Escape("Unicode $Emoji stays")).Count) 'Formatter changed a comment.'
    Pass 'three-pass idempotence, UTF-8 without BOM, LF, final newline, strings, Unicode, and comments'

    Write-Host "$Passed focused SMILE formatter integration tests passed."
}
finally {
    $TempPrefix = ([IO.Path]::GetFullPath($env:TEMP)).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $ResolvedTestRoot = [IO.Path]::GetFullPath($TestRoot)

    if ($ResolvedTestRoot.StartsWith($TempPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Directory]::Exists($ResolvedTestRoot)) {
        Remove-Item -LiteralPath $ResolvedTestRoot -Recurse -Force
    }
}
