@echo off
setlocal

set "SMILE_ROOT=%~dp0.."

call "%SMILE_ROOT%\scripts\build.cmd"
if errorlevel 1 exit /b %errorlevel%

dotnet run --project "%SMILE_ROOT%\src\Smile.Tests\Smile.Tests.csproj" -c Release --no-restore
if errorlevel 1 exit /b %errorlevel%

powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\test-smile-formatter.ps1"
if errorlevel 1 exit /b %errorlevel%

powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\format-smile-style.ps1" -Check -FormatLongIf
if errorlevel 1 exit /b %errorlevel%

"%SMILE_ROOT%\artifacts\tests\Smile.NativeGraphicsTests.exe"
if errorlevel 1 exit /b %errorlevel%

"%SMILE_ROOT%\artifacts\tests\Smile.NativeTextTests.exe"
if errorlevel 1 exit /b %errorlevel%

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Hello.smile" -o "%SMILE_ROOT%\artifacts\games\Hello.exe"
if errorlevel 1 exit /b %errorlevel%

for /f "delims=" %%I in ('"%SMILE_ROOT%\artifacts\games\Hello.exe"') do set "SMILE_HELLO=%%I"
if not "%SMILE_HELLO%"=="Hello World" (
    echo Hello smoke test failed: expected "Hello World", found "%SMILE_HELLO%".
    exit /b 1
)
echo Hello smoke test passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\MultilineExpressionParity\MultilineExpressionParity.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\MultilineExpressionParity.exe" --debug
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\games\MultilineExpressionParity.exe" > "%SMILE_ROOT%\artifacts\temp\MultilineExpressionParity.out"
if errorlevel 1 exit /b %errorlevel%
powershell -NoProfile -Command "$expected=[IO.File]::ReadAllLines('%SMILE_ROOT%\examples\MultilineExpressionParity\MultilineExpressionParity.expected.txt',[Text.Encoding]::UTF8); $actual=[IO.File]::ReadAllLines('%SMILE_ROOT%\artifacts\temp\MultilineExpressionParity.out',[Text.Encoding]::UTF8); if (Compare-Object $expected $actual) { exit 1 }"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\MultilineExpressionParity\MultilineExpressionParity.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\MultilineExpressionParity"
if errorlevel 1 exit /b %errorlevel%
node --check "%SMILE_ROOT%\artifacts\web\MultilineExpressionParity\game.js"
if errorlevel 1 exit /b %errorlevel%
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\MultilineExpressionParity" --expected "%SMILE_ROOT%\examples\MultilineExpressionParity\MultilineExpressionParity.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\MultilineExpressionParity.out" --timeout 10000
if errorlevel 1 exit /b 1
echo Multiline parenthesized expression native and Web parity test passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\libraries\Smile.Math.Extras\Smile.Math.Extras.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.Math.Extras.smilelib"
if errorlevel 1 exit /b %errorlevel%
copy /y "%SMILE_ROOT%\artifacts\libraries\Smile.Math.Extras.smilelib" "%SMILE_ROOT%\artifacts\temp\Smile.Math.Extras.first.smilelib" >nul
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\libraries\Smile.Math.Extras\Smile.Math.Extras.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.Math.Extras.smilelib"
if errorlevel 1 exit /b %errorlevel%
fc /b "%SMILE_ROOT%\artifacts\temp\Smile.Math.Extras.first.smilelib" "%SMILE_ROOT%\artifacts\libraries\Smile.Math.Extras.smilelib" >nul
if errorlevel 1 (
    echo SMILE library deterministic package test failed.
    exit /b 1
)

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\LibraryConsumer\LibraryConsumer.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\LibraryConsumer.exe" --debug
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\games\LibraryConsumer.exe" > "%SMILE_ROOT%\artifacts\temp\LibraryConsumer.out"
if errorlevel 1 exit /b %errorlevel%
for %%V in ("100" "True" "1") do (
    findstr /x /c:%%V "%SMILE_ROOT%\artifacts\temp\LibraryConsumer.out" >nul
    if errorlevel 1 exit /b 1
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\LibraryConsumer\LibraryConsumer.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\LibraryConsumer"
if errorlevel 1 exit /b %errorlevel%
node --check "%SMILE_ROOT%\artifacts\web\LibraryConsumer\game.js"
if errorlevel 1 exit /b %errorlevel%

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\LibraryConsumer\Program.smile" --library "%SMILE_ROOT%\artifacts\libraries\Smile.Math.Extras.smilelib" -o "%SMILE_ROOT%\artifacts\games\LibraryPackageConsumer.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\LocalModuleBasics\LocalModuleBasics.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\LocalModuleBasics.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\games\LocalModuleBasics.exe" | findstr /x /c:"42" >nul
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\LocalModuleBasics\LocalModuleBasics.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\LocalModuleBasics"
if errorlevel 1 exit /b %errorlevel%
node --check "%SMILE_ROOT%\artifacts\web\LocalModuleBasics\game.js"
if errorlevel 1 exit /b %errorlevel%
echo Phase 2 library project, package reference, local module, native, Web, and deterministic package tests passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\libraries\Smile.Text.Extras\Smile.Text.Extras.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.Text.Extras.smilelib"
if errorlevel 1 exit /b %errorlevel%
copy /y "%SMILE_ROOT%\artifacts\libraries\Smile.Text.Extras.smilelib" "%SMILE_ROOT%\artifacts\temp\Smile.Text.Extras.first.smilelib" >nul
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\libraries\Smile.Text.Extras\Smile.Text.Extras.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.Text.Extras.smilelib"
if errorlevel 1 exit /b %errorlevel%
fc /b "%SMILE_ROOT%\artifacts\temp\Smile.Text.Extras.first.smilelib" "%SMILE_ROOT%\artifacts\libraries\Smile.Text.Extras.smilelib" >nul
if errorlevel 1 exit /b 1
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip=[IO.Compression.ZipFile]::OpenRead('%SMILE_ROOT%\artifacts\libraries\Smile.Text.Extras.smilelib'); try { $manifest=[IO.StreamReader]::new($zip.GetEntry('manifest.json').Open()).ReadToEnd(); $api=[IO.StreamReader]::new($zip.GetEntry('api/public-symbols.json').Open()).ReadToEnd(); if (!$manifest.Contains('formatVersion') -or !$manifest.Contains(': 5') -or !$api.Contains('ByRef') -or !$api.Contains('returnType') -or !$api.Contains('Text')) { exit 1 } } finally { $zip.Dispose() }"
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase3ABasics\Phase3ABasics.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\Phase3ABasics.exe" --debug
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\games\Phase3ABasics.exe" > "%SMILE_ROOT%\artifacts\temp\Phase3ABasics.out"
if errorlevel 1 exit /b %errorlevel%
for %%V in ("Changed" "True" "36" "1136" "Module Text" "Match") do (
    findstr /x /c:%%V "%SMILE_ROOT%\artifacts\temp\Phase3ABasics.out" >nul
    if errorlevel 1 exit /b 1
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase3ABasics\Phase3ABasics.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase3ABasics"
if errorlevel 1 exit /b %errorlevel%
node --check "%SMILE_ROOT%\artifacts\web\Phase3ABasics\game.js"
if errorlevel 1 exit /b %errorlevel%
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase3ABasics" --native-output "%SMILE_ROOT%\artifacts\temp\Phase3ABasics.out"
if errorlevel 1 exit /b %errorlevel%

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase3ABasics\Phase3ABasics.Package.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\Phase3ABasicsPackage.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\games\Phase3ABasicsPackage.exe" > "%SMILE_ROOT%\artifacts\temp\Phase3ABasicsPackage.out"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase3ABasics\Phase3ABasics.Package.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase3ABasicsPackage"
if errorlevel 1 exit /b %errorlevel%
node --check "%SMILE_ROOT%\artifacts\web\Phase3ABasicsPackage\game.js"
if errorlevel 1 exit /b %errorlevel%
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase3ABasicsPackage" --native-output "%SMILE_ROOT%\artifacts\temp\Phase3ABasicsPackage.out"
if errorlevel 1 exit /b %errorlevel%

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase3ATextStress.smile" -o "%SMILE_ROOT%\artifacts\games\Phase3ATextStress.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\games\Phase3ATextStress.exe" > "%SMILE_ROOT%\artifacts\temp\Phase3ATextStress.out"
if errorlevel 1 exit /b %errorlevel%
findstr /x /c:"False" "%SMILE_ROOT%\artifacts\temp\Phase3ATextStress.out" >nul || exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase3ATextStress.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\Phase3ATextStress"
if errorlevel 1 exit /b %errorlevel%
node --check "%SMILE_ROOT%\artifacts\web\Phase3ATextStress\game.js"
if errorlevel 1 exit /b %errorlevel%
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase3ATextStress" --native-output "%SMILE_ROOT%\artifacts\temp\Phase3ATextStress.out" --timeout 10000
if errorlevel 1 exit /b %errorlevel%

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase3ATextGame\Phase3ATextGame.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\Phase3ATextGame.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase3ATextGame\Phase3ATextGame.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase3ATextGame"
if errorlevel 1 exit /b %errorlevel%
node --check "%SMILE_ROOT%\artifacts\web\Phase3ATextGame\game.js"
if errorlevel 1 exit /b %errorlevel%
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase3ATextGame" --draw-text-file "%SMILE_ROOT%\examples\Phase3ATextGame\Caption.expected.txt" --frames 2 --timeout 10000
if errorlevel 1 exit /b %errorlevel%

for %%N in (RecursiveFor RecursiveTextSelect ExitCleanup NestedCleanup EndProgramCleanup Unicode WebParity) do (
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase3A1Hardening\%%N.smile" -o "%SMILE_ROOT%\artifacts\games\%%N.exe"
    if errorlevel 1 exit /b 1
    "%SMILE_ROOT%\artifacts\games\%%N.exe" > "%SMILE_ROOT%\artifacts\temp\%%N.out"
    if errorlevel 1 exit /b 1
    set "SMILE_TEXT_LIFETIME_DIAGNOSTICS=1"
    "%SMILE_ROOT%\artifacts\games\%%N.exe" > "%SMILE_ROOT%\artifacts\temp\%%N.lifetime.out"
    if errorlevel 1 exit /b 1
    set "SMILE_TEXT_LIFETIME_DIAGNOSTICS="
    findstr /x /c:"SMILE_TEXT_LIVE=0" "%SMILE_ROOT%\artifacts\temp\%%N.lifetime.out" >nul
    if errorlevel 1 exit /b 1
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase3A1Hardening\%%N.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\%%N"
    if errorlevel 1 exit /b 1
    node --check "%SMILE_ROOT%\artifacts\web\%%N\game.js"
    if errorlevel 1 exit /b 1
    node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\%%N" --expected "%SMILE_ROOT%\examples\Phase3A1Hardening\%%N.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\%%N.out" --timeout 10000
    if errorlevel 1 exit /b 1
)
echo Phase 3A.1 reentrancy, cleanup, Unicode, lifetime, and native/Web execution parity tests passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\libraries\Smile.Data.Models\Smile.Data.Models.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.Data.Models.smilelib"
if errorlevel 1 exit /b %errorlevel%
copy /y "%SMILE_ROOT%\artifacts\libraries\Smile.Data.Models.smilelib" "%SMILE_ROOT%\artifacts\temp\Smile.Data.Models.first.smilelib" >nul
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\libraries\Smile.Data.Models\Smile.Data.Models.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.Data.Models.smilelib"
if errorlevel 1 exit /b %errorlevel%
fc /b "%SMILE_ROOT%\artifacts\temp\Smile.Data.Models.first.smilelib" "%SMILE_ROOT%\artifacts\libraries\Smile.Data.Models.smilelib" >nul || exit /b 1
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip=[IO.Compression.ZipFile]::OpenRead('%SMILE_ROOT%\artifacts\libraries\Smile.Data.Models.smilelib'); try { $manifest=[IO.StreamReader]::new($zip.GetEntry('manifest.json').Open()).ReadToEnd(); $api=[IO.StreamReader]::new($zip.GetEntry('api/public-symbols.json').Open()).ReadToEnd(); if (!$manifest.Contains(': 5') -or !$api.Contains('Smile.Data.Models::Actor') -or !$api.Contains('\"fields\"') -or $api.Contains('InternalTag')) { exit 1 } } finally { $zip.Dispose() }"
if errorlevel 1 exit /b 1

for %%P in (Phase3BRecords.smileproj Phase3BRecords.Package.smileproj) do (
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase3BRecords\%%P" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\%%~nP.exe" --debug
    if errorlevel 1 exit /b 1
    "%SMILE_ROOT%\artifacts\games\%%~nP.exe" > "%SMILE_ROOT%\artifacts\temp\%%~nP.out"
    if errorlevel 1 exit /b 1
    fc "%SMILE_ROOT%\examples\Phase3BRecords\Phase3BRecords.expected.txt" "%SMILE_ROOT%\artifacts\temp\%%~nP.out" >nul || exit /b 1
    set "SMILE_TEXT_LIFETIME_DIAGNOSTICS=1"
    "%SMILE_ROOT%\artifacts\games\%%~nP.exe" > "%SMILE_ROOT%\artifacts\temp\%%~nP.lifetime.out"
    if errorlevel 1 exit /b 1
    set "SMILE_TEXT_LIFETIME_DIAGNOSTICS="
    findstr /x /c:"SMILE_TEXT_LIVE=0" "%SMILE_ROOT%\artifacts\temp\%%~nP.lifetime.out" >nul || exit /b 1
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase3BRecords\%%P" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\%%~nP"
    if errorlevel 1 exit /b 1
    node --check "%SMILE_ROOT%\artifacts\web\%%~nP\game.js" || exit /b 1
    node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\%%~nP" --expected "%SMILE_ROOT%\examples\Phase3BRecords\Phase3BRecords.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\%%~nP.out" --timeout 10000
    if errorlevel 1 exit /b 1
)

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase3BLocalRecords\Phase3BLocalRecords.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\Phase3BLocalRecords.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\Phase3BLocalRecords.exe" > "%SMILE_ROOT%\artifacts\temp\Phase3BLocalRecords.out"
fc "%SMILE_ROOT%\examples\Phase3BLocalRecords\Phase3BLocalRecords.expected.txt" "%SMILE_ROOT%\artifacts\temp\Phase3BLocalRecords.out" >nul || exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase3BLocalRecords\Phase3BLocalRecords.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase3BLocalRecords"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase3BLocalRecords" --expected "%SMILE_ROOT%\examples\Phase3BLocalRecords\Phase3BLocalRecords.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\Phase3BLocalRecords.out" --timeout 10000 || exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase3BRecordMatrix.smile" -o "%SMILE_ROOT%\artifacts\games\Phase3BRecordMatrix.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\Phase3BRecordMatrix.exe" > "%SMILE_ROOT%\artifacts\temp\Phase3BRecordMatrix.out"
fc "%SMILE_ROOT%\examples\Phase3BRecordMatrix.expected.txt" "%SMILE_ROOT%\artifacts\temp\Phase3BRecordMatrix.out" >nul || exit /b 1
set "SMILE_TEXT_LIFETIME_DIAGNOSTICS=1"
"%SMILE_ROOT%\artifacts\games\Phase3BRecordMatrix.exe" > "%SMILE_ROOT%\artifacts\temp\Phase3BRecordMatrix.lifetime.out"
if errorlevel 1 exit /b 1
set "SMILE_TEXT_LIFETIME_DIAGNOSTICS="
findstr /x /c:"SMILE_TEXT_LIVE=0" "%SMILE_ROOT%\artifacts\temp\Phase3BRecordMatrix.lifetime.out" >nul || exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase3BRecordMatrix.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\Phase3BRecordMatrix"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase3BRecordMatrix" --expected "%SMILE_ROOT%\examples\Phase3BRecordMatrix.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\Phase3BRecordMatrix.out" --timeout 10000 || exit /b 1

for %%P in (DuplicateType:SML3400 TypeInsideRoutine:SML3403 EmptyType:SML3402 DuplicateField:SML3402 FieldWithoutAs:SML3402 ArrayField:SML3403 FieldInitializer:SML3403 UnknownFieldType:SML3401 DirectRecursiveType:SML3404 IndirectRecursiveType:SML3404 UnknownField:SML3405 FieldOnNumber:SML3406 RecordComparison:SML3407 PrintRecord:SML3407 WrongRecordAssignment:SML3304 InvalidRecordByRefTemporary:SML3305 RecordConst:SML3403) do (
    for /f "tokens=1,2 delims=:" %%F in ("%%P") do (
        "%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\InvalidPhase3B\%%F.smile" > "%SMILE_ROOT%\artifacts\temp\%%F.log" 2>&1
        if not errorlevel 1 exit /b 1
        if errorlevel 2 exit /b 1
        findstr /c:"%%G" "%SMILE_ROOT%\artifacts\temp\%%F.log" >nul || exit /b 1
        findstr /c:"%%F.smile(" "%SMILE_ROOT%\artifacts\temp\%%F.log" >nul || exit /b 1
    )
)
for %%P in (UnknownImportedType:SML3401 PrivateImportedType:SML3408) do (
    for /f "tokens=1,2 delims=:" %%F in ("%%P") do (
        "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\InvalidPhase3B\%%F\%%F.smileproj" > "%SMILE_ROOT%\artifacts\temp\%%F.log" 2>&1
        if not errorlevel 1 exit /b 1
        if errorlevel 2 exit /b 1
        findstr /c:"%%G" "%SMILE_ROOT%\artifacts\temp\%%F.log" >nul || exit /b 1
    )
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\InvalidPhase3B\PublicApiPrivateType\PublicApiPrivateType.smilelibproj" --target library -o "%SMILE_ROOT%\artifacts\temp\invalid-private.smilelib" > "%SMILE_ROOT%\artifacts\temp\PublicApiPrivateType.log" 2>&1
if not errorlevel 1 exit /b 1
if errorlevel 2 exit /b 1
findstr /c:"SML3409" "%SMILE_ROOT%\artifacts\temp\PublicApiPrivateType.log" >nul || exit /b 1
echo Phase 3B record semantics, native ABI, Web parity, packages, diagnostics, completion, and lifetime tests passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase3B1Hardening\WebFieldKeys.smile" -o "%SMILE_ROOT%\artifacts\games\WebFieldKeys.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\WebFieldKeys.exe" > "%SMILE_ROOT%\artifacts\temp\WebFieldKeys.out"
if errorlevel 1 exit /b 1
fc "%SMILE_ROOT%\examples\Phase3B1Hardening\WebFieldKeys.expected.txt" "%SMILE_ROOT%\artifacts\temp\WebFieldKeys.out" >nul || exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase3B1Hardening\WebFieldKeys.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\WebFieldKeys"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\WebFieldKeys\game.js" || exit /b 1
findstr /c:"__smile_r0_f0" "%SMILE_ROOT%\artifacts\web\WebFieldKeys\game.js" >nul || exit /b 1
findstr /l /c:"[\"__proto__\"]" "%SMILE_ROOT%\artifacts\web\WebFieldKeys\game.js" >nul && exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\WebFieldKeys" --expected "%SMILE_ROOT%\examples\Phase3B1Hardening\WebFieldKeys.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\WebFieldKeys.out" --timeout 10000 || exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\InvalidPhase3B1\ModuleCapture\ModuleCapture.smileproj" -o "%SMILE_ROOT%\artifacts\temp\ModuleCapture.exe" > "%SMILE_ROOT%\artifacts\temp\ModuleCapture.log" 2>&1
if not errorlevel 1 exit /b 1
if errorlevel 2 exit /b 1
findstr /c:"SML3401" "%SMILE_ROOT%\artifacts\temp\ModuleCapture.log" >nul || exit /b 1
findstr /c:"Alias.Type" "%SMILE_ROOT%\artifacts\temp\ModuleCapture.log" >nul || exit /b 1
echo Phase 3B.1 Web field identity, module type boundaries, provider metadata, and completion tests passed.

if not exist "%SMILE_ROOT%\artifacts\games\Phase4VisualSlice-DirectX" mkdir "%SMILE_ROOT%\artifacts\games\Phase4VisualSlice-DirectX"
if not exist "%SMILE_ROOT%\artifacts\games\Phase4VisualSlice-GDI" mkdir "%SMILE_ROOT%\artifacts\games\Phase4VisualSlice-GDI"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase4VisualSlice\Phase4VisualSlice.smileproj" --target windows-x64 --configuration Release --graphics DirectX -o "%SMILE_ROOT%\artifacts\games\Phase4VisualSlice-DirectX\Phase4VisualSlice.exe" --debug
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase4VisualSlice\Phase4VisualSlice.smileproj" --target windows-x64 --configuration Release --graphics GDI -o "%SMILE_ROOT%\artifacts\games\Phase4VisualSlice-GDI\Phase4VisualSlice.exe"
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase4VisualSlice\Phase4VisualSlice.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase4VisualSlice"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\Phase4VisualSlice\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase4VisualSlice" --frames 6 --timeout 10000 --phase4-media
if errorlevel 1 exit /b 1

for %%P in (InvalidImageTarget:SML3500 InvalidDrawImage:SML3501 InvalidImageModifier:SML3503 InvalidClip:SML3504 InvalidTextMeasure:SML3505 InvalidData:SML3506 InvalidChannel:SML3507 InvalidImageOperator:SML3509) do (
    for /f "tokens=1,2 delims=:" %%F in ("%%P") do (
        "%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\InvalidPhase4\%%F.smile" > "%SMILE_ROOT%\artifacts\temp\%%F.log" 2>&1
        if not errorlevel 1 exit /b 1
        if errorlevel 2 exit /b 1
        findstr /c:"%%G" "%SMILE_ROOT%\artifacts\temp\%%F.log" >nul || exit /b 1
    )
)
echo Phase 4 Image, high-resolution drawing, clip, data, SFX, diagnostics, native, and Web tests passed.

if not exist "%SMILE_ROOT%\artifacts\games\Phase4Hardening" mkdir "%SMILE_ROOT%\artifacts\games\Phase4Hardening"
if not exist "%SMILE_ROOT%\artifacts\web\Phase4Hardening" mkdir "%SMILE_ROOT%\artifacts\web\Phase4Hardening"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase4Hardening\DataKeyIdentity.smileproj" --target windows-x64 -o "%SMILE_ROOT%\artifacts\games\Phase4Hardening\DataKeyIdentity.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\Phase4Hardening\DataKeyIdentity.exe" > "%SMILE_ROOT%\artifacts\temp\DataKeyIdentity.out"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase4Hardening\DataLoadCorrupt.smileproj" --target windows-x64 -o "%SMILE_ROOT%\artifacts\games\Phase4Hardening\DataLoadCorrupt.exe"
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\test-phase4-data-envelope.ps1" -LoaderPath "%SMILE_ROOT%\artifacts\games\Phase4Hardening\DataLoadCorrupt.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase4Hardening\DataKeyIdentity.smileproj" --target web --output-dir "%SMILE_ROOT%\artifacts\web\Phase4Hardening\DataKeyIdentity"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase4Hardening\DataKeyIdentity" --expected "%SMILE_ROOT%\examples\Phase4Hardening\DataKeyIdentity.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\DataKeyIdentity.out" --timeout 10000
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase4Hardening\ImageReturnOwnership.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\Phase4Hardening\ImageReturnOwnership"
if errorlevel 1 exit /b 1
xcopy "%SMILE_ROOT%\examples\Phase4VisualSlice\Assets" "%SMILE_ROOT%\artifacts\web\Phase4Hardening\ImageReturnOwnership\Assets" /E /I /Y >nul
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase4Hardening\ImageReturnOwnership" --frames 3 --timeout 10000 --phase4-ownership
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase4Hardening\ClipAcrossFrames.smile" --target windows-x64 --graphics DirectX -o "%SMILE_ROOT%\artifacts\games\Phase4Hardening\ClipAcrossFrames-DirectX.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase4Hardening\ClipAcrossFrames.smile" --target windows-x64 --graphics GDI -o "%SMILE_ROOT%\artifacts\games\Phase4Hardening\ClipAcrossFrames-GDI.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase4Hardening\ClipAcrossFrames.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\Phase4Hardening\ClipAcrossFrames"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase4Hardening\ClipAcrossFrames" --frames 6 --timeout 10000 --phase4-clip
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase4Hardening\AudioGeneration.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\Phase4Hardening\AudioGeneration"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase4Hardening\AudioGeneration" --frames 3 --timeout 10000 --phase4-audio
if errorlevel 1 exit /b 1
echo Phase 4.1 ownership, high-DPI, clip lifetime, Data identity, cache race, and audio generation tests passed.

powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\test-phase4-asset-publication.ps1"
if errorlevel 1 exit /b 1

powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\generate-phase5-ui-assets.ps1"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase5TextPrimitives.smile" --target windows-x64 -o "%SMILE_ROOT%\artifacts\games\Phase5TextPrimitives.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\Phase5TextPrimitives.exe" > "%SMILE_ROOT%\artifacts\temp\Phase5TextPrimitives.out"
if errorlevel 1 exit /b 1
powershell -NoProfile -Command "$expected=[IO.File]::ReadAllLines('%SMILE_ROOT%\examples\Phase5TextPrimitives.expected.txt',[Text.Encoding]::UTF8); $actual=[IO.File]::ReadAllLines('%SMILE_ROOT%\artifacts\temp\Phase5TextPrimitives.out',[Text.Encoding]::UTF8); if (Compare-Object $expected $actual) { exit 1 }"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Phase5TextPrimitives.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\Phase5TextPrimitives"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase5TextPrimitives" --expected "%SMILE_ROOT%\examples\Phase5TextPrimitives.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\Phase5TextPrimitives.out" --timeout 10000
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\libraries\Smile.UI\Smile.UI.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.UI.smilelib"
if errorlevel 1 exit /b 1
copy /y "%SMILE_ROOT%\artifacts\libraries\Smile.UI.smilelib" "%SMILE_ROOT%\artifacts\temp\Smile.UI.first.smilelib" >nul
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\libraries\Smile.UI\Smile.UI.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.UI.smilelib"
if errorlevel 1 exit /b 1
fc /b "%SMILE_ROOT%\artifacts\temp\Smile.UI.first.smilelib" "%SMILE_ROOT%\artifacts\libraries\Smile.UI.smilelib" >nul
if errorlevel 1 exit /b 1
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip=[IO.Compression.ZipFile]::OpenRead('%SMILE_ROOT%\artifacts\libraries\Smile.UI.smilelib'); try { $manifest=([IO.StreamReader]::new($zip.GetEntry('manifest.json').Open())).ReadToEnd() | ConvertFrom-Json; $api=([IO.StreamReader]::new($zip.GetEntry('api/public-symbols.json').Open())).ReadToEnd() | ConvertFrom-Json; $core=$api.modules | Where-Object name -eq 'Smile.UI.Core'; $menu=$api.modules | Where-Object name -eq 'Smile.UI.Menu'; $navigator=$api.modules | Where-Object name -eq 'Smile.UI.MenuNavigator'; $text=$api.modules | Where-Object name -eq 'Smile.UI.Text'; $dialogue=$api.modules | Where-Object name -eq 'Smile.UI.Dialogue'; $insets=$core.members | Where-Object name -eq 'Insets'; $draw=$menu.members | Where-Object name -eq 'Draw'; $drawFocused=$menu.members | Where-Object name -eq 'DrawFocused'; $key=$menu.members | Where-Object name -eq 'HandleKey'; $visibleRows=$menu.members | Where-Object name -eq 'VisibleRows'; $bounds=$menu.members | Where-Object name -eq 'Bounds'; $drawStack=$navigator.members | Where-Object name -eq 'DrawStack'; $drawActive=$navigator.members | Where-Object name -eq 'DrawActive'; $navigatorKey=$navigator.members | Where-Object name -eq 'HandleKey'; $relayout=$navigator.members | Where-Object name -eq 'Relayout'; $textValid=$text.members | Where-Object name -eq 'IsStyleValid'; $dialogueSet=$dialogue.members | Where-Object name -eq 'SetStyle'; if ($manifest.formatVersion -ne 5 -or $manifest.version -ne '1.1.3' -or $insets.fields.name -cnotcontains 'Left' -or $insets.fields.name -cnotcontains 'Right' -or $insets.fields.name -ccontains 'LEFT' -or $insets.fields.name -ccontains 'RIGHT' -or !$draw.requiresGameWindow -or !$drawFocused.requiresGameWindow -or $key.requiresGameWindow -or $visibleRows.requiresGameWindow -or $bounds.requiresGameWindow -or !$drawStack.requiresGameWindow -or !$drawActive.requiresGameWindow -or $navigatorKey.requiresGameWindow -or $relayout.requiresGameWindow -or $textValid.requiresGameWindow -or !$dialogueSet.requiresGameWindow) { exit 1 } } finally { $zip.Dispose() }"
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5UIStateTests\Phase5UIStateTests.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\Phase5UIStateTests.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\Phase5UIStateTests.exe" > "%SMILE_ROOT%\artifacts\temp\Phase5UIStateTests.out"
if errorlevel 1 exit /b 1
powershell -NoProfile -Command "$expected=[IO.File]::ReadAllLines('%SMILE_ROOT%\examples\Phase5UIStateTests\Phase5UIStateTests.expected.txt',[Text.Encoding]::UTF8); $actual=[IO.File]::ReadAllLines('%SMILE_ROOT%\artifacts\temp\Phase5UIStateTests.out',[Text.Encoding]::UTF8); if (Compare-Object $expected $actual) { exit 1 }"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5UIStateTests\Phase5UIStateTests.Package.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\Phase5UIStateTestsPackage.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\Phase5UIStateTestsPackage.exe" > "%SMILE_ROOT%\artifacts\temp\Phase5UIStateTestsPackage.out"
if errorlevel 1 exit /b 1
fc /b "%SMILE_ROOT%\artifacts\temp\Phase5UIStateTests.out" "%SMILE_ROOT%\artifacts\temp\Phase5UIStateTestsPackage.out" >nul
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5SubmenuStateTests\Phase5SubmenuStateTests.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\Phase5SubmenuStateTests.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\Phase5SubmenuStateTests.exe" > "%SMILE_ROOT%\artifacts\temp\Phase5SubmenuStateTests.out"
if errorlevel 1 exit /b 1
powershell -NoProfile -Command "$expected=[IO.File]::ReadAllLines('%SMILE_ROOT%\examples\Phase5SubmenuStateTests\Phase5SubmenuStateTests.expected.txt',[Text.Encoding]::UTF8); $actual=[IO.File]::ReadAllLines('%SMILE_ROOT%\artifacts\temp\Phase5SubmenuStateTests.out',[Text.Encoding]::UTF8); if (Compare-Object $expected $actual) { exit 1 }"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5SubmenuStateTests\Phase5SubmenuStateTests.Package.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\Phase5SubmenuStateTestsPackage.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\Phase5SubmenuStateTestsPackage.exe" > "%SMILE_ROOT%\artifacts\temp\Phase5SubmenuStateTestsPackage.out"
if errorlevel 1 exit /b 1
powershell -NoProfile -Command "$project=[IO.File]::ReadAllLines('%SMILE_ROOT%\artifacts\temp\Phase5SubmenuStateTests.out',[Text.Encoding]::UTF8); $package=[IO.File]::ReadAllLines('%SMILE_ROOT%\artifacts\temp\Phase5SubmenuStateTestsPackage.out',[Text.Encoding]::UTF8); if (Compare-Object $project $package) { exit 1 }"
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5DialogueStateTests\Phase5DialogueStateTests.smileproj" --target windows-x64 --configuration Release --graphics GDI -o "%SMILE_ROOT%\artifacts\games\Phase5DialogueStateTests.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\Phase5DialogueStateTests.exe" > "%SMILE_ROOT%\artifacts\temp\Phase5DialogueStateTests.out"
if errorlevel 1 exit /b 1
powershell -NoProfile -Command "$expected=[IO.File]::ReadAllLines('%SMILE_ROOT%\examples\Phase5DialogueStateTests\Phase5DialogueStateTests.expected.txt',[Text.Encoding]::UTF8); $actual=[IO.File]::ReadAllLines('%SMILE_ROOT%\artifacts\temp\Phase5DialogueStateTests.out',[Text.Encoding]::UTF8); if (Compare-Object $expected $actual) { exit 1 }"
if errorlevel 1 exit /b 1
powershell -NoProfile -Command "if ((Get-Content -Raw -LiteralPath '%LOCALAPPDATA%\SMILE 2.0\Games\Phase5DialogueStateTests\Result.txt').Trim() -ne '0') { exit 1 }"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5DialogueStateTests\Phase5DialogueStateTests.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase5DialogueStateTests"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\Phase5DialogueStateTests\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase5DialogueStateTests" --expected "%SMILE_ROOT%\examples\Phase5DialogueStateTests\Phase5DialogueStateTests.expected.txt" --frames 3 --timeout 10000
if errorlevel 1 exit /b 1

if not exist "%SMILE_ROOT%\artifacts\games\Phase5Hardening-DirectX" mkdir "%SMILE_ROOT%\artifacts\games\Phase5Hardening-DirectX"
if not exist "%SMILE_ROOT%\artifacts\games\Phase5Hardening-GDI" mkdir "%SMILE_ROOT%\artifacts\games\Phase5Hardening-GDI"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5Hardening\Phase5Hardening.smileproj" --target windows-x64 --configuration Release --graphics DirectX -o "%SMILE_ROOT%\artifacts\games\Phase5Hardening-DirectX\Phase5Hardening.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\Phase5Hardening-DirectX\Phase5Hardening.exe" > "%SMILE_ROOT%\artifacts\temp\Phase5Hardening-DirectX.out"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5Hardening\Phase5Hardening.smileproj" --target windows-x64 --configuration Release --graphics GDI -o "%SMILE_ROOT%\artifacts\games\Phase5Hardening-GDI\Phase5Hardening.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\Phase5Hardening-GDI\Phase5Hardening.exe" > "%SMILE_ROOT%\artifacts\temp\Phase5Hardening-GDI.out"
if errorlevel 1 exit /b 1
for %%O in (Phase5Hardening-DirectX.out Phase5Hardening-GDI.out) do (
    powershell -NoProfile -Command "$expected=[IO.File]::ReadAllLines('%SMILE_ROOT%\examples\Phase5Hardening\Phase5Hardening.expected.txt',[Text.Encoding]::UTF8); $actual=[IO.File]::ReadAllLines('%SMILE_ROOT%\artifacts\temp\%%O',[Text.Encoding]::UTF8); if (Compare-Object $expected $actual) { exit 1 }"
    if errorlevel 1 exit /b 1
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5Hardening\Phase5Hardening.Package.smileproj" --target windows-x64 --configuration Release --graphics GDI -o "%SMILE_ROOT%\artifacts\games\Phase5HardeningPackage.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\Phase5HardeningPackage.exe" > "%SMILE_ROOT%\artifacts\temp\Phase5HardeningPackage.out"
if errorlevel 1 exit /b 1
fc /b "%SMILE_ROOT%\artifacts\temp\Phase5Hardening-GDI.out" "%SMILE_ROOT%\artifacts\temp\Phase5HardeningPackage.out" >nul
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5Hardening\Phase5Hardening.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase5Hardening"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase5Hardening" --expected "%SMILE_ROOT%\examples\Phase5Hardening\Phase5Hardening.expected.txt" --phase5-hardening --frames 3 --timeout 10000
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5Hardening\Phase5Hardening.Package.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase5HardeningPackage"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase5HardeningPackage" --expected "%SMILE_ROOT%\examples\Phase5Hardening\Phase5Hardening.expected.txt" --phase5-hardening --frames 3 --timeout 10000
if errorlevel 1 exit /b 1

for %%P in (ConsoleCallsDraw.smileproj ConsoleCallsDraw.Package.smileproj) do (
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\InvalidPhase5\ConsoleCallsDraw\%%P" -o "%SMILE_ROOT%\artifacts\temp\ConsoleCallsDraw.exe" > "%SMILE_ROOT%\artifacts\temp\%%P.log" 2>&1
    if not errorlevel 1 exit /b 1
    if errorlevel 2 exit /b 1
    powershell -NoProfile -Command "if ((Select-String -LiteralPath '%SMILE_ROOT%\artifacts\temp\%%P.log' -SimpleMatch 'SML3704').Count -ne 1) { exit 1 }"
    if errorlevel 1 exit /b 1
)
for %%P in (ConsoleCallsDialogueSetStyle.smileproj ConsoleCallsDialogueSetStyle.Package.smileproj) do (
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\InvalidPhase5\ConsoleCallsDialogueSetStyle\%%P" -o "%SMILE_ROOT%\artifacts\temp\ConsoleCallsDialogueSetStyle.exe" > "%SMILE_ROOT%\artifacts\temp\%%P.log" 2>&1
    if not errorlevel 1 exit /b 1
    if errorlevel 2 exit /b 1
    powershell -NoProfile -Command "if ((Select-String -LiteralPath '%SMILE_ROOT%\artifacts\temp\%%P.log' -SimpleMatch 'SML3704').Count -ne 1) { exit 1 }"
    if errorlevel 1 exit /b 1
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\InvalidPhase5Submenus\ConsoleDrawStack\ConsoleDrawStack.smileproj" -o "%SMILE_ROOT%\artifacts\temp\ConsoleDrawStack.exe" > "%SMILE_ROOT%\artifacts\temp\ConsoleDrawStack.log" 2>&1
if not errorlevel 1 exit /b 1
if errorlevel 2 exit /b 1
powershell -NoProfile -Command "$matches=Select-String -LiteralPath '%SMILE_ROOT%\artifacts\temp\ConsoleDrawStack.log' -SimpleMatch 'SML3704'; if ($matches.Count -ne 1 -or $matches.Line -notmatch 'Program\.smile\(7,20\).*DrawStack.*requires a Game Window') { exit 1 }"
if errorlevel 1 exit /b 1

if not exist "%SMILE_ROOT%\artifacts\games\Phase5SubmenuViewport-DirectX" mkdir "%SMILE_ROOT%\artifacts\games\Phase5SubmenuViewport-DirectX"
if not exist "%SMILE_ROOT%\artifacts\games\Phase5SubmenuViewport-GDI" mkdir "%SMILE_ROOT%\artifacts\games\Phase5SubmenuViewport-GDI"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5SubmenuViewport\Phase5SubmenuViewport.smileproj" --target windows-x64 --configuration Release --graphics DirectX -o "%SMILE_ROOT%\artifacts\games\Phase5SubmenuViewport-DirectX\Phase5SubmenuViewport.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5SubmenuViewport\Phase5SubmenuViewport.smileproj" --target windows-x64 --configuration Release --graphics GDI -o "%SMILE_ROOT%\artifacts\games\Phase5SubmenuViewport-GDI\Phase5SubmenuViewport.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5SubmenuViewport\Phase5SubmenuViewport.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase5SubmenuViewport"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\Phase5SubmenuViewport\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase5SubmenuViewport" --frames 4 --timeout 10000 --phase5-submenu-viewport
if errorlevel 1 exit /b 1

if not exist "%SMILE_ROOT%\artifacts\games\MenuGallery-DirectX" mkdir "%SMILE_ROOT%\artifacts\games\MenuGallery-DirectX"
if not exist "%SMILE_ROOT%\artifacts\games\MenuGallery-GDI" mkdir "%SMILE_ROOT%\artifacts\games\MenuGallery-GDI"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\MenuGallery\MenuGallery.smileproj" --target windows-x64 --configuration Release --graphics DirectX -o "%SMILE_ROOT%\artifacts\games\MenuGallery-DirectX\MenuGallery.exe" --debug
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\MenuGallery\MenuGallery.smileproj" --target windows-x64 --configuration Release --graphics GDI -o "%SMILE_ROOT%\artifacts\games\MenuGallery-GDI\MenuGallery.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\MenuGallery\MenuGallery.Package.smileproj" --target windows-x64 --configuration Release --graphics DirectX -o "%SMILE_ROOT%\artifacts\games\MenuGalleryPackage.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\MenuGallery\MenuGallery.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\MenuGallery"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\MenuGallery\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\MenuGallery" --frames 40 --timeout 10000 --phase5-submenus
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\MenuGallery\MenuGallery.Package.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\MenuGalleryPackage"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\MenuGalleryPackage\game.js"
if errorlevel 1 exit /b 1
for %%A in (Background.png WindowSkin.png Cursor.png Continue.png BitmapFont.png Move.wav Confirm.wav Cancel.wav) do (
    fc /b "%SMILE_ROOT%\examples\MenuGallery\Assets\%%A" "%SMILE_ROOT%\artifacts\web\MenuGallery\Assets\%%A" >nul
    if errorlevel 1 exit /b 1
)
echo Phase 5 Unicode text, routine capabilities, Smile.UI state, submenu navigation, packages, assets, native, and Web tests passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\libraries\Smile.RPG\Smile.RPG.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.RPG.smilelib"
if errorlevel 1 exit /b 1
copy /y "%SMILE_ROOT%\artifacts\libraries\Smile.RPG.smilelib" "%SMILE_ROOT%\artifacts\temp\Smile.RPG.first.smilelib" >nul
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\libraries\Smile.RPG\Smile.RPG.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.RPG.smilelib"
if errorlevel 1 exit /b 1
fc /b "%SMILE_ROOT%\artifacts\temp\Smile.RPG.first.smilelib" "%SMILE_ROOT%\artifacts\libraries\Smile.RPG.smilelib" >nul
if errorlevel 1 exit /b 1
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip=[IO.Compression.ZipFile]::OpenRead('%SMILE_ROOT%\artifacts\libraries\Smile.RPG.smilelib'); try { $manifest=([IO.StreamReader]::new($zip.GetEntry('manifest.json').Open())).ReadToEnd() | ConvertFrom-Json; $api=([IO.StreamReader]::new($zip.GetEntry('api/public-symbols.json').Open())).ReadToEnd() | ConvertFrom-Json; $names=@($api.modules.name); if ($manifest.formatVersion -ne 5 -or $manifest.version -ne '1.0.2' -or $names.Count -ne 8 -or $names -notcontains 'Smile.RPG.Core' -or $names -notcontains 'Smile.RPG.SaveGames' -or @($api.modules.members | Where-Object requiresGameWindow).Count -ne 0 -or $api.modules.members.name -notcontains 'RPG_RESULT_NOT_SELLABLE') { exit 1 } } finally { $zip.Dispose() }"
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase6RpgStateTests\Phase6RpgStateTests.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\tests\Phase6RpgStateTests.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\tests\Phase6RpgStateTests.exe" > "%SMILE_ROOT%\artifacts\temp\Phase6RpgStateTests.out"
if errorlevel 1 exit /b 1
findstr /x /c:"Phase 6 RPG state tests: PASS" "%SMILE_ROOT%\artifacts\temp\Phase6RpgStateTests.out" >nul
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase6RpgStateTests\Phase6RpgStateTests.Package.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\tests\Phase6RpgStateTestsPackage.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\tests\Phase6RpgStateTestsPackage.exe" > "%SMILE_ROOT%\artifacts\temp\Phase6RpgStateTestsPackage.out"
if errorlevel 1 exit /b 1
fc /b "%SMILE_ROOT%\artifacts\temp\Phase6RpgStateTests.out" "%SMILE_ROOT%\artifacts\temp\Phase6RpgStateTestsPackage.out" >nul
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase6RpgStateTests\Phase6RpgStateTests.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase6RpgStateTests"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\Phase6RpgStateTests\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase6RpgStateTests" --native-output "%SMILE_ROOT%\artifacts\temp\Phase6RpgStateTests.out" --timeout 10000
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase6RpgStateTests\Phase6RpgStateTests.Package.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase6RpgStateTestsPackage"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\Phase6RpgStateTestsPackage\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase6RpgStateTestsPackage" --native-output "%SMILE_ROOT%\artifacts\temp\Phase6RpgStateTestsPackage.out" --timeout 10000
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\test-phase6-rpg-rollback.ps1"
if errorlevel 1 exit /b 1

if not exist "%SMILE_ROOT%\artifacts\games\RpgManagementGallery-DirectX" mkdir "%SMILE_ROOT%\artifacts\games\RpgManagementGallery-DirectX"
if not exist "%SMILE_ROOT%\artifacts\games\RpgManagementGallery-GDI" mkdir "%SMILE_ROOT%\artifacts\games\RpgManagementGallery-GDI"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\RpgManagementGallery\RpgManagementGallery.smileproj" --target windows-x64 --configuration Release --graphics DirectX -o "%SMILE_ROOT%\artifacts\games\RpgManagementGallery-DirectX\RpgManagementGallery.exe" --debug
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\RpgManagementGallery\RpgManagementGallery.smileproj" --target windows-x64 --configuration Release --graphics GDI -o "%SMILE_ROOT%\artifacts\games\RpgManagementGallery-GDI\RpgManagementGallery.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\RpgManagementGallery\RpgManagementGallery.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\RpgManagementGallery"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\RpgManagementGallery\game.js"
if errorlevel 1 exit /b 1
echo Phase 6.2 ApplicationId, Smile.RPG package, state, save, query-purity, project/package, DirectX, GDI, and Web tests passed.

for %%P in (OptionExplicitLate:SML3300 OptionExplicitUndeclared:SML3303 ScalarDimWithoutAs:SML3302 UnknownBuiltInType:SML3401 NumberToTextAssignment:SML3304 TextToBooleanAssignment:SML3304 MixedTextAddition:SML3308 TextRelationalComparison:SML3308 InvalidByRefLiteral:SML3305 InvalidByRefConstant:SML3305 WrongArgumentType:SML3304 WrongReturnType:SML3304 InconsistentLegacyReturnTypes:SML3309 DuplicateLocal:SML3306 UseBeforeLocalDeclaration:SML3307) do (
    for /f "tokens=1,2 delims=:" %%F in ("%%P") do (
        "%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\InvalidPhase3A\%%F.smile" > "%SMILE_ROOT%\artifacts\temp\%%F.log" 2>&1
        if not errorlevel 1 exit /b 1
        if errorlevel 2 exit /b 1
        findstr /c:"%%G" "%SMILE_ROOT%\artifacts\temp\%%F.log" >nul || exit /b 1
    )
)
echo Phase 3A typed declarations, Text lifetime, routine ABI, packages, diagnostics, native, and Web tests passed.

for %%F in (MissingModule UnknownMember PrivateMemberAccess DuplicateAlias ModuleImportCycle) do (
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\InvalidModules\%%F\%%F.smileproj" -o "%SMILE_ROOT%\artifacts\temp\%%F.exe" > "%SMILE_ROOT%\artifacts\temp\%%F.log" 2>&1
    if not errorlevel 1 (
        echo Invalid module fixture %%F unexpectedly succeeded.
        exit /b 1
    )
    if errorlevel 2 (
        echo Invalid module fixture %%F returned an infrastructure error.
        exit /b 1
    )
)
findstr /c:"SML3102" "%SMILE_ROOT%\artifacts\temp\MissingModule.log" >nul || exit /b 1
findstr /c:"SML3103" "%SMILE_ROOT%\artifacts\temp\UnknownMember.log" >nul || exit /b 1
findstr /c:"SML3105" "%SMILE_ROOT%\artifacts\temp\PrivateMemberAccess.log" >nul || exit /b 1
findstr /c:"SML3106" "%SMILE_ROOT%\artifacts\temp\DuplicateAlias.log" >nul || exit /b 1
findstr /c:"SML3108" "%SMILE_ROOT%\artifacts\temp\ModuleImportCycle.log" >nul || exit /b 1
echo Phase 2 invalid module diagnostics passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\LanguageBasics.smile" -o "%SMILE_ROOT%\artifacts\games\LanguageBasics.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\games\LanguageBasics.exe"
if errorlevel 1 exit /b %errorlevel%

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\StructuredLanguageBasics.smile" -o "%SMILE_ROOT%\artifacts\games\StructuredLanguageBasics.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\games\StructuredLanguageBasics.exe" > "%SMILE_ROOT%\artifacts\temp\StructuredLanguageBasics.out"
if errorlevel 1 exit /b %errorlevel%
for %%V in ("Even" "12" "40" "1" "2" "200" "5" "3" "2022440" "16744576") do (
    findstr /x /c:%%V "%SMILE_ROOT%\artifacts\temp\StructuredLanguageBasics.out" >nul
    if errorlevel 1 (
        echo Structured language smoke test failed: missing %%V.
        exit /b 1
    )
)
echo Structured language smoke test passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\diagnostics\InvalidStructuredLanguage.smile" > "%SMILE_ROOT%\artifacts\temp\InvalidStructuredLanguage.log" 2>&1
if not errorlevel 1 (
    echo Invalid structured language smoke test failed: compilation unexpectedly succeeded.
    exit /b 1
)
if errorlevel 2 (
    echo Invalid structured language smoke test failed: compiler returned infrastructure error.
    exit /b 1
)
for %%C in (SML3012 SML3006 SML3017 SML3016 SML3018 SML3019 SML3021) do (
    findstr /c:"%%C" "%SMILE_ROOT%\artifacts\temp\InvalidStructuredLanguage.log" >nul
    if errorlevel 1 (
        echo Invalid structured language smoke test failed: missing %%C.
        exit /b 1
    )
)
echo Invalid structured language diagnostics passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\diagnostics\InvalidGameLanguage.smile" > "%SMILE_ROOT%\artifacts\temp\InvalidGameLanguage.log" 2>&1
if not errorlevel 1 (
    echo Invalid game language smoke test failed: compilation unexpectedly succeeded.
    exit /b 1
)
if errorlevel 2 (
    echo Invalid game language smoke test failed: compiler returned infrastructure error.
    exit /b 1
)
for %%C in (SML2001 SML3022 SML3023 SML3024 SML3025 SML3026) do (
    findstr /c:"%%C" "%SMILE_ROOT%\artifacts\temp\InvalidGameLanguage.log" >nul
    if errorlevel 1 (
        echo Invalid game language smoke test failed: missing %%C.
        exit /b 1
    )
)
echo Invalid game language diagnostics passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\RuntimeBasics.smile" -o "%SMILE_ROOT%\artifacts\games\RuntimeBasics.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\games\RuntimeBasics.exe"
if errorlevel 1 exit /b %errorlevel%

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\ConsoleSnake.smile" -o "%SMILE_ROOT%\artifacts\games\ConsoleSnake.exe"
if errorlevel 1 exit /b %errorlevel%
echo Console Snake regression compiled successfully.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\StorageBasics.smile" -o "%SMILE_ROOT%\artifacts\games\StorageBasics.exe"
if errorlevel 1 exit /b %errorlevel%
set "SMILE_STORAGE_DIR=%LOCALAPPDATA%\SMILE 2.0\Games\StorageBasics"
if not exist "%SMILE_STORAGE_DIR%" mkdir "%SMILE_STORAGE_DIR%"
> "%SMILE_STORAGE_DIR%\SmokeValue.txt" echo corrupt-value
"%SMILE_ROOT%\artifacts\games\StorageBasics.exe" > "%SMILE_ROOT%\artifacts\temp\StorageBasics.out"
if errorlevel 1 exit /b %errorlevel%
findstr /x /c:"123" "%SMILE_ROOT%\artifacts\temp\StorageBasics.out" >nul
if errorlevel 1 (
    echo Storage smoke test failed: corrupt value did not use the default.
    exit /b 1
)
findstr /x /c:"456" "%SMILE_ROOT%\artifacts\temp\StorageBasics.out" >nul
if errorlevel 1 (
    echo Storage smoke test failed: saved value did not reload.
    exit /b 1
)
echo Storage default, save, and reload tests passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\TextFileLoadBasics.smile" -o "%SMILE_ROOT%\artifacts\games\TextFileLoadBasics.exe"
if errorlevel 1 exit /b %errorlevel%
powershell -NoProfile -Command "[IO.File]::WriteAllBytes('%SMILE_ROOT%\artifacts\games\TextFileLoadFixture.txt', [byte[]](0xEF,0xBB,0xBF,65,66,67,68,69,70))"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\games\TextFileLoadBasics.exe" > "%SMILE_ROOT%\artifacts\temp\TextFileLoadBasics.out"
if errorlevel 1 exit /b %errorlevel%
findstr /x /c:"5" "%SMILE_ROOT%\artifacts\temp\TextFileLoadBasics.out" >nul
if errorlevel 1 (
    echo Text-file loading smoke test failed: capacity truncation count was not five.
    exit /b 1
)
findstr /x /c:"65" "%SMILE_ROOT%\artifacts\temp\TextFileLoadBasics.out" >nul
if errorlevel 1 (
    echo Text-file loading smoke test failed: UTF-8 BOM was not skipped.
    exit /b 1
)
findstr /x /c:"69" "%SMILE_ROOT%\artifacts\temp\TextFileLoadBasics.out" >nul
if errorlevel 1 (
    echo Text-file loading smoke test failed: bounded bytes were not copied.
    exit /b 1
)
for /f %%Z in ('findstr /x /c:"0" "%SMILE_ROOT%\artifacts\temp\TextFileLoadBasics.out" ^| find /c /v ""') do set "SMILE_ZERO_LINES=%%Z"
if not "%SMILE_ZERO_LINES%"=="3" (
    echo Text-file loading smoke test failed: missing-file count or zero-fill was incorrect.
    exit /b 1
)
echo Text-file BOM, truncation, missing-file, and zero-fill tests passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\GraphicsBasics.smile" -o "%SMILE_ROOT%\artifacts\games\GraphicsBasics.exe"
if errorlevel 1 exit /b %errorlevel%
if not exist "%SMILE_ROOT%\artifacts\games\Assets" mkdir "%SMILE_ROOT%\artifacts\games\Assets"
copy /y "%SMILE_ROOT%\examples\Assets\Graphics.wav" "%SMILE_ROOT%\artifacts\games\Assets\Graphics.wav" >nul
if errorlevel 1 exit /b %errorlevel%
if not exist "%SMILE_ROOT%\artifacts\games\GraphicsBasics.exe" (
    echo GraphicsBasics native executable is missing.
    exit /b 1
)
if not exist "%SMILE_ROOT%\artifacts\games\Assets\Graphics.wav" (
    echo GraphicsBasics sound asset is missing.
    exit /b 1
)
echo GraphicsBasics compiled with its sound asset.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\ArcBasics.smile" -o "%SMILE_ROOT%\artifacts\games\ArcBasics.exe"
if errorlevel 1 exit /b %errorlevel%
if not exist "%SMILE_ROOT%\artifacts\games\ArcBasics.exe" (
    echo ArcBasics native executable is missing.
    exit /b 1
)
echo ArcBasics compiled successfully.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\GraphicsTextSample.smile" -o "%SMILE_ROOT%\artifacts\games\GraphicsTextSample.exe"
if errorlevel 1 exit /b %errorlevel%
if not exist "%SMILE_ROOT%\artifacts\games\GraphicsTextSample.exe" (
    echo GraphicsTextSample native executable is missing.
    exit /b 1
)
echo Required graphics text sample compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\MultiFileBasics" mkdir "%SMILE_ROOT%\artifacts\games\MultiFileBasics"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\MultiFileBasics\Program.smile" --source "%SMILE_ROOT%\examples\MultiFileBasics\GameState.smile" --source "%SMILE_ROOT%\examples\MultiFileBasics\Drawing.smile" -o "%SMILE_ROOT%\artifacts\games\MultiFileBasics\MultiFileBasics.exe" --debug
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\MultiFileBasics\Program.smile" --source "%SMILE_ROOT%\examples\MultiFileBasics\GameState.smile" --source "%SMILE_ROOT%\examples\MultiFileBasics\Drawing.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\MultiFileBasics"
if errorlevel 1 exit /b %errorlevel%
node --check "%SMILE_ROOT%\artifacts\web\MultiFileBasics\game.js"
if errorlevel 1 exit /b %errorlevel%
echo MultiFileBasics native debug and Web versions compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\MultiFileDeclarationHardening" mkdir "%SMILE_ROOT%\artifacts\games\MultiFileDeclarationHardening"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\MultiFileDeclarationHardening\Program.smile" --source "%SMILE_ROOT%\examples\MultiFileDeclarationHardening\Arrays.smile" --source "%SMILE_ROOT%\examples\MultiFileDeclarationHardening\Derived.smile" --source "%SMILE_ROOT%\examples\MultiFileDeclarationHardening\Base.smile" -o "%SMILE_ROOT%\artifacts\games\MultiFileDeclarationHardening\MultiFileDeclarationHardening.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\games\MultiFileDeclarationHardening\MultiFileDeclarationHardening.exe" > "%SMILE_ROOT%\artifacts\temp\MultiFileDeclarationHardening.out"
if errorlevel 1 exit /b %errorlevel%
findstr /x /c:"8" "%SMILE_ROOT%\artifacts\temp\MultiFileDeclarationHardening.out" >nul
if errorlevel 1 (
    echo Multi-file declaration hardening native output is missing 8.
    exit /b 1
)
findstr /x /c:"7" "%SMILE_ROOT%\artifacts\temp\MultiFileDeclarationHardening.out" >nul
if errorlevel 1 (
    echo Multi-file declaration hardening native output is missing 7.
    exit /b 1
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\MultiFileDeclarationHardening\Program.smile" --source "%SMILE_ROOT%\examples\MultiFileDeclarationHardening\Base.smile" --source "%SMILE_ROOT%\examples\MultiFileDeclarationHardening\Derived.smile" --source "%SMILE_ROOT%\examples\MultiFileDeclarationHardening\Arrays.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\MultiFileDeclarationHardening"
if errorlevel 1 exit /b %errorlevel%
node --check "%SMILE_ROOT%\artifacts\web\MultiFileDeclarationHardening\game.js"
if errorlevel 1 exit /b %errorlevel%
echo Multi-file constants and array dimensions compiled in both source orders and targets.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\diagnostics\MultiFileCircularConstants\Program.smile" --source "%SMILE_ROOT%\examples\diagnostics\MultiFileCircularConstants\First.smile" --source "%SMILE_ROOT%\examples\diagnostics\MultiFileCircularConstants\Second.smile" > "%SMILE_ROOT%\artifacts\temp\MultiFileCircularConstants.log" 2>&1
if not errorlevel 1 (
    echo Circular constants smoke test failed: compilation unexpectedly succeeded.
    exit /b 1
)
if errorlevel 2 exit /b 1
findstr /c:"SML3029" /c:"Circular constant dependency" "%SMILE_ROOT%\artifacts\temp\MultiFileCircularConstants.log" >nul
if errorlevel 1 (
    echo Circular constants smoke test failed: missing circular-dependency diagnostic.
    exit /b 1
)

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\diagnostics\MultiFileNameCollision\Program.smile" --source "%SMILE_ROOT%\examples\diagnostics\MultiFileNameCollision\Value.smile" --source "%SMILE_ROOT%\examples\diagnostics\MultiFileNameCollision\Routine.smile" > "%SMILE_ROOT%\artifacts\temp\MultiFileNameCollision.log" 2>&1
if not errorlevel 1 (
    echo Project namespace smoke test failed: compilation unexpectedly succeeded.
    exit /b 1
)
if errorlevel 2 exit /b 1
findstr /c:"SML3005" "%SMILE_ROOT%\artifacts\temp\MultiFileNameCollision.log" >nul
if errorlevel 1 (
    echo Project namespace smoke test failed: missing collision diagnostic.
    exit /b 1
)
echo Circular constants and project-level name collisions failed with the intended diagnostics.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\SourceVisibilityBasics\Program.smile" --source "%SMILE_ROOT%\examples\SourceVisibilityBasics\Helpers.smile" -o "%SMILE_ROOT%\artifacts\games\SourceVisibilityBasics.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\SourceVisibilityBasics\Program.smile" --source "%SMILE_ROOT%\examples\SourceVisibilityBasics\Helpers.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\SourceVisibilityBasics"
if errorlevel 1 exit /b %errorlevel%
node --check "%SMILE_ROOT%\artifacts\web\SourceVisibilityBasics\game.js"
if errorlevel 1 exit /b %errorlevel%
echo Source visibility fixture compiled for native and Web.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\LiveRefreshBasics\Program.smile" --source "%SMILE_ROOT%\examples\LiveRefreshBasics\Helpers.smile" -o "%SMILE_ROOT%\artifacts\games\LiveRefreshBasics.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\LiveRefreshBasics\Program.smile" --source "%SMILE_ROOT%\examples\LiveRefreshBasics\Helpers.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\LiveRefreshBasics"
if errorlevel 1 exit /b %errorlevel%
node --check "%SMILE_ROOT%\artifacts\web\LiveRefreshBasics\game.js"
if errorlevel 1 exit /b %errorlevel%
echo Live refresh fixture compiled for native and Web.

if not exist "%SMILE_ROOT%\artifacts\games\Snake" mkdir "%SMILE_ROOT%\artifacts\games\Snake"
if not exist "%SMILE_ROOT%\games\Snake\Assets\Background.mp3" (
    echo Snake background music source asset is missing.
    exit /b 1
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\games\Snake\Snake.smileproj" -o "%SMILE_ROOT%\artifacts\games\Snake\Snake.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\Snake\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\Snake\Snake-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
if not exist "%SMILE_ROOT%\artifacts\games\Snake\Assets\Background.mp3" (
    echo Snake background music output asset is missing.
    exit /b 1
)
fc /b "%SMILE_ROOT%\games\Snake\Assets\Background.mp3" "%SMILE_ROOT%\artifacts\games\Snake\Assets\Background.mp3" >nul
if errorlevel 1 (
    echo Snake background music output does not match its project asset.
    exit /b 1
)
echo Snake demo and no-demo versions compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\FallingBlocks" mkdir "%SMILE_ROOT%\artifacts\games\FallingBlocks"
if not exist "%SMILE_ROOT%\games\FallingBlocks\Assets\Background.mp3" (
    echo Falling Blocks background music source asset is missing.
    exit /b 1
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\games\FallingBlocks\FallingBlocks.smileproj" -o "%SMILE_ROOT%\artifacts\games\FallingBlocks\FallingBlocks.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\FallingBlocks\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\FallingBlocks\FallingBlocks-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
if not exist "%SMILE_ROOT%\artifacts\games\FallingBlocks\Assets\Background.mp3" (
    echo Falling Blocks background music output asset is missing.
    exit /b 1
)
fc /b "%SMILE_ROOT%\games\FallingBlocks\Assets\Background.mp3" "%SMILE_ROOT%\artifacts\games\FallingBlocks\Assets\Background.mp3" >nul
if errorlevel 1 (
    echo Falling Blocks background music output does not match its project asset.
    exit /b 1
)
echo Falling Blocks demo and no-demo versions compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\PaddleBall" mkdir "%SMILE_ROOT%\artifacts\games\PaddleBall"
if not exist "%SMILE_ROOT%\games\PaddleBall\Assets\Background.mp3" (
    echo Paddle Ball background music source asset is missing.
    exit /b 1
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\games\PaddleBall\PaddleBall.smileproj" -o "%SMILE_ROOT%\artifacts\games\PaddleBall\PaddleBall.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\PaddleBall\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\PaddleBall\PaddleBall-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
if not exist "%SMILE_ROOT%\artifacts\games\PaddleBall\Assets\Background.mp3" (
    echo Paddle Ball background music output asset is missing.
    exit /b 1
)
fc /b "%SMILE_ROOT%\games\PaddleBall\Assets\Background.mp3" "%SMILE_ROOT%\artifacts\games\PaddleBall\Assets\Background.mp3" >nul
if errorlevel 1 (
    echo Paddle Ball background music output does not match its project asset.
    exit /b 1
)
echo Paddle Ball demo and no-demo versions compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\BrickBreaker" mkdir "%SMILE_ROOT%\artifacts\games\BrickBreaker"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\games\BrickBreaker\BrickBreaker.smileproj" -o "%SMILE_ROOT%\artifacts\games\BrickBreaker\BrickBreaker.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\BrickBreaker\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\BrickBreaker\BrickBreaker-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
echo Brick Breaker demo and no-demo versions compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\MazeMuncher" mkdir "%SMILE_ROOT%\artifacts\games\MazeMuncher"
if not exist "%SMILE_ROOT%\games\MazeMuncher\Assets\Background.mp3" (
    echo Maze Muncher background music source asset is missing.
    exit /b 1
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\games\MazeMuncher\MazeMuncher.smileproj" -o "%SMILE_ROOT%\artifacts\games\MazeMuncher\MazeMuncher.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\MazeMuncher\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\MazeMuncher\MazeMuncher-NoDemo.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
if not exist "%SMILE_ROOT%\artifacts\games\MazeMuncher\Assets\Background.mp3" (
    echo Maze Muncher background music output asset is missing.
    exit /b 1
)
fc /b "%SMILE_ROOT%\games\MazeMuncher\Assets\Background.mp3" "%SMILE_ROOT%\artifacts\games\MazeMuncher\Assets\Background.mp3" >nul
if errorlevel 1 (
    echo Maze Muncher background music output does not match its project asset.
    exit /b 1
)
fc /b "%SMILE_ROOT%\games\MazeMuncher\Maps\default.map" "%SMILE_ROOT%\artifacts\games\MazeMuncher\Maps\default.map" >nul
if errorlevel 1 (
    echo Maze Muncher output map does not match its project asset.
    exit /b 1
)
echo Maze Muncher demo and no-demo versions compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\DungeonStarI" mkdir "%SMILE_ROOT%\artifacts\games\DungeonStarI"
if not exist "%SMILE_ROOT%\games\DungeonStarI\Assets\Background.mp3" (
    echo Dungeon Star I background music source asset is missing.
    exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\validate-dungeon-maps.ps1"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\games\DungeonStarI\DungeonStarI.smileproj" -o "%SMILE_ROOT%\artifacts\games\DungeonStarI\DungeonStarI.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\DungeonStarI\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\DungeonStarI\DungeonStarI-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
if not exist "%SMILE_ROOT%\artifacts\games\DungeonStarI\Assets\Background.mp3" (
    echo Dungeon Star I background music output asset is missing.
    exit /b 1
)
fc /b "%SMILE_ROOT%\games\DungeonStarI\Assets\Background.mp3" "%SMILE_ROOT%\artifacts\games\DungeonStarI\Assets\Background.mp3" >nul
if errorlevel 1 (
    echo Dungeon Star I background music output does not match its project asset.
    exit /b 1
)
for %%M in (default.map sample-loops.map sample-switchbacks.map) do (
    fc /b "%SMILE_ROOT%\games\DungeonStarI\Maps\%%M" "%SMILE_ROOT%\artifacts\games\DungeonStarI\Maps\%%M" >nul
    if errorlevel 1 (
        echo Dungeon Star I output map %%M does not match its project asset.
        exit /b 1
    )
)
echo Dungeon Star I demo and no-demo versions compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\DungeonStarII" mkdir "%SMILE_ROOT%\artifacts\games\DungeonStarII"
powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\validate-raycasting-maps.ps1"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\games\DungeonStarII\DungeonStarII.smileproj" -o "%SMILE_ROOT%\artifacts\games\DungeonStarII\DungeonStarII.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\DungeonStarII\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\DungeonStarII\DungeonStarII-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
for %%M in (default.map custom.map) do (
    fc /b "%SMILE_ROOT%\games\DungeonStarII\Maps\%%M" "%SMILE_ROOT%\artifacts\games\DungeonStarII\Maps\%%M" >nul
    if errorlevel 1 (
        echo Dungeon Star II output map %%M does not match its project asset.
        exit /b 1
    )
)
echo Dungeon Star II demo and no-demo versions compiled successfully.

for %%G in (Snake FallingBlocks PaddleBall BrickBreaker DungeonStarI DungeonStarII MazeMuncher) do (
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\games\%%G\%%G.smileproj" --target web --output-dir "%SMILE_ROOT%\artifacts\web\%%G"
    if errorlevel 1 exit /b 1
    node --check "%SMILE_ROOT%\artifacts\web\%%G\game.js"
    if errorlevel 1 exit /b 1
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\%%G\Program-NoDemo.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\%%G-NoDemo"
    if errorlevel 1 exit /b 1
    node --check "%SMILE_ROOT%\artifacts\web\%%G-NoDemo\game.js"
    if errorlevel 1 exit /b 1
)
echo All seven game demo and no-demo Web versions compiled successfully.

powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\verify-artifacts.ps1"
if errorlevel 1 exit /b %errorlevel%

echo Manual gameplay is still required for graphical games.
exit /b 0
