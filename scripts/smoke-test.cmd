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
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip=[IO.Compression.ZipFile]::OpenRead('%SMILE_ROOT%\artifacts\libraries\Smile.Math.Extras.smilelib'); try { $manifest=([IO.StreamReader]::new($zip.GetEntry('manifest.json').Open())).ReadToEnd() | ConvertFrom-Json; $api=([IO.StreamReader]::new($zip.GetEntry('api/public-symbols.json').Open())).ReadToEnd() | ConvertFrom-Json; if ($manifest.formatVersion -ne 6 -or $api.formatVersion -ne 6 -or $manifest.name -ne 'Smile.Math.Extras' -or $manifest.version -ne '1.0.0' -or $manifest.provider -ne 'Smile.Math.Extras@1.0.0' -or @($manifest.modules).Count -ne 1 -or @($manifest.sources).Count -ne 2 -or @($manifest.dependencies).Count -ne 0 -or $api.library.provider -ne $manifest.provider) { exit 1 } } finally { $zip.Dispose() }"
if errorlevel 1 exit /b 1

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
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip=[IO.Compression.ZipFile]::OpenRead('%SMILE_ROOT%\artifacts\libraries\Smile.Text.Extras.smilelib'); try { $manifest=([IO.StreamReader]::new($zip.GetEntry('manifest.json').Open())).ReadToEnd() | ConvertFrom-Json; $apiText=[IO.StreamReader]::new($zip.GetEntry('api/public-symbols.json').Open()).ReadToEnd(); $api=$apiText | ConvertFrom-Json; if ($manifest.formatVersion -ne 6 -or $api.formatVersion -ne 6 -or $manifest.name -ne 'Smile.Text.Extras' -or $manifest.version -ne '1.0.0' -or $manifest.provider -ne 'Smile.Text.Extras@1.0.0' -or @($manifest.modules).Count -ne 1 -or @($manifest.sources).Count -ne 1 -or @($manifest.dependencies).Count -ne 0 -or $api.library.provider -ne $manifest.provider -or !$apiText.Contains('ByRef') -or !$apiText.Contains('returnType') -or !$apiText.Contains('Text')) { exit 1 } } finally { $zip.Dispose() }"
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
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip=[IO.Compression.ZipFile]::OpenRead('%SMILE_ROOT%\artifacts\libraries\Smile.Data.Models.smilelib'); try { $manifest=([IO.StreamReader]::new($zip.GetEntry('manifest.json').Open())).ReadToEnd() | ConvertFrom-Json; $apiText=[IO.StreamReader]::new($zip.GetEntry('api/public-symbols.json').Open()).ReadToEnd(); $api=$apiText | ConvertFrom-Json; if ($manifest.formatVersion -ne 6 -or $api.formatVersion -ne 6 -or $manifest.name -ne 'Smile.Data.Models' -or $manifest.version -ne '1.0.0' -or $manifest.provider -ne 'Smile.Data.Models@1.0.0' -or @($manifest.modules).Count -ne 1 -or @($manifest.sources).Count -ne 2 -or @($manifest.dependencies).Count -ne 0 -or $api.library.provider -ne $manifest.provider -or !$apiText.Contains('Smile.Data.Models::Actor') -or !$apiText.Contains('\"fields\"') -or $apiText.Contains('InternalTag')) { exit 1 } } finally { $zip.Dispose() }"
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\LightweightOopCalls\LightweightOopLibrary.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.Lightweight.Oop.Proof.smilelib"
if errorlevel 1 exit /b %errorlevel%
copy /y "%SMILE_ROOT%\artifacts\libraries\Smile.Lightweight.Oop.Proof.smilelib" "%SMILE_ROOT%\artifacts\temp\Smile.Lightweight.Oop.Proof.first.smilelib" >nul
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\LightweightOopCalls\LightweightOopLibrary.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.Lightweight.Oop.Proof.smilelib"
if errorlevel 1 exit /b %errorlevel%
fc /b "%SMILE_ROOT%\artifacts\temp\Smile.Lightweight.Oop.Proof.first.smilelib" "%SMILE_ROOT%\artifacts\libraries\Smile.Lightweight.Oop.Proof.smilelib" >nul || exit /b 1
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip=[IO.Compression.ZipFile]::OpenRead('%SMILE_ROOT%\artifacts\libraries\Smile.Lightweight.Oop.Proof.smilelib'); try { $manifest=([IO.StreamReader]::new($zip.GetEntry('manifest.json').Open())).ReadToEnd() | ConvertFrom-Json; $apiText=[IO.StreamReader]::new($zip.GetEntry('api/public-symbols.json').Open()).ReadToEnd(); $api=$apiText | ConvertFrom-Json; $module=$api.modules | Where-Object name -eq 'Smile.Lightweight.Oop.Proof'; $report=$module.members | Where-Object name -eq 'Report'; $counter=$module.members | Where-Object name -eq 'Counter'; $configure=$counter.members | Where-Object name -eq 'Configure'; $difference=$counter.members | Where-Object name -eq 'Difference'; $drawProbe=$counter.members | Where-Object name -eq 'DrawProbe'; $gameProbe=$counter.members | Where-Object name -eq 'GameProbe'; $shifted=$counter.members | Where-Object name -eq 'Shifted'; $caption=$counter.members | Where-Object name -eq 'Caption'; $reference=$module.members | Where-Object name -eq 'ReferenceCounter'; $empty=$module.members | Where-Object name -eq 'EmptyReference'; $gameConstructor=$module.members | Where-Object name -eq 'GameConstructorProbe'; $gameReference=$module.members | Where-Object name -eq 'GameReferenceProbe'; $classGameProbe=$gameReference.members | Where-Object name -eq 'GameProbe'; $classDrawProbe=$gameReference.members | Where-Object name -eq 'DrawProbe'; $p=@($report.parameters); if ($manifest.formatVersion -ne 6 -or $api.formatVersion -ne 6 -or $manifest.version -ne '1.2.0' -or $manifest.provider -ne 'Smile.Lightweight.Oop.Proof@1.2.0' -or @($manifest.sources).Count -ne 1 -or $manifest.sources[0] -cne 'src/Library/Api.smile' -or @($module.members).Count -ne 8 -or (@($module.members.name) -join '|') -cne 'Counter|CounterBox|DisplayMode|EmptyReference|GameConstructorProbe|GameReferenceProbe|ReferenceCounter|Report' -or $p.Count -ne 5 -or $p[0].optional -or $null -ne $p[0].default -or !$p[1].optional -or $p[1].default.kind -cne 'number' -or $p[1].default.value -ne 3 -or $p[2].default.kind -cne 'boolean' -or !$p[2].default.value -or $p[3].default.kind -cne 'text' -or $p[3].default.value -cne '!' -or $p[4].type.kind -cne 'enum' -or $p[4].type.provider -cne $manifest.provider -or $p[4].default.kind -cne 'enum' -or $p[4].default.member -cne 'CompactAlias' -or $p[4].default.value -ne 2 -or $counter.identity -cne 'Smile.Lightweight.Oop.Proof::Counter' -or (@($counter.fields.name) -join '|') -cne 'Label|StoredValue|Enabled|Mode' -or (@($counter.members.name) -join '|') -cne 'Advance|Caption|Configure|Difference|DrawProbe|GameProbe|Shifted|Total' -or (@($configure.parameters.name) -join '|') -cne 'Label|Start|Enabled|Mode' -or $configure.parameters[3].type.provider -cne $manifest.provider -or $difference.parameters[0].type.identity -cne $counter.identity -or $difference.parameters[0].type.provider -cne $manifest.provider -or $shifted.returnType.identity -cne $counter.identity -or $shifted.returnType.provider -cne $manifest.provider -or !$drawProbe.requiresGameWindow -or !$gameProbe.get.requiresGameWindow -or $gameProbe.set.requiresGameWindow -or $null -ne $caption.set -or $gameProbe.get.identity -ceq $gameProbe.set.identity -or $reference.kind -cne 'Class' -or (@($reference.fields.name) -join '|') -cne 'Code|Samples' -or $reference.fields[1].rank -ne 1 -or (@($reference.fields[1].dimensions) -join '|') -cne '2' -or $reference.constructor.identity -cne 'Smile.Lightweight.Oop.Proof::ReferenceCounter::constructor::New' -or !$reference.constructor.declared -or (@($reference.constructor.parameters.name) -join '|') -cne 'Label|Start|Mode' -or (@($reference.members.name) -join '|') -cne 'Advance|Alias|Caption|Same|Snapshot|Total' -or $reference.members[1].returnType.kind -cne 'class' -or $reference.members[1].returnType.provider -cne $manifest.provider -or $empty.constructor.declared -or @($empty.constructor.parameters).Count -ne 0 -or !$gameConstructor.constructor.requiresGameWindow -or $gameReference.constructor.requiresGameWindow -or !$classDrawProbe.requiresGameWindow -or !$classGameProbe.get.requiresGameWindow -or $classGameProbe.set.requiresGameWindow -or $reference.PSObject.Properties.Name -ccontains 'instanceSize' -or $reference.fields.PSObject.Properties.Name -ccontains 'offset' -or $p[4].location.source -cne 'src/Library/Api.smile' -or $apiText.Contains('::member::Hide') -or $apiText.Contains('::property::Secret') -or $apiText.Contains('::receiver') -or $apiText.Contains('::value') -or $apiText.IndexOf('%SMILE_ROOT%', [StringComparison]::OrdinalIgnoreCase) -ge 0) { exit 1 } } finally { $zip.Dispose() }"
if errorlevel 1 exit /b 1

for %%P in (LightweightOopCalls.smileproj LightweightOopCalls.Package.smileproj) do (
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\LightweightOopCalls\%%P" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\%%~nP.exe" --debug
    if errorlevel 1 exit /b 1
    "%SMILE_ROOT%\artifacts\games\%%~nP.exe" > "%SMILE_ROOT%\artifacts\temp\%%~nP.out"
    if errorlevel 1 exit /b 1
    fc "%SMILE_ROOT%\examples\LightweightOopCalls\LightweightOopCalls.expected.txt" "%SMILE_ROOT%\artifacts\temp\%%~nP.out" >nul || exit /b 1
    set "SMILE_CLASS_LIFETIME_DIAGNOSTICS=1"
    set "SMILE_TEXT_LIFETIME_DIAGNOSTICS=1"
    "%SMILE_ROOT%\artifacts\games\%%~nP.exe" > "%SMILE_ROOT%\artifacts\temp\%%~nP.lifetime.out"
    if errorlevel 1 exit /b 1
    set "SMILE_CLASS_LIFETIME_DIAGNOSTICS="
    set "SMILE_TEXT_LIFETIME_DIAGNOSTICS="
    findstr /x /c:"SMILE_CLASS_LIVE=0" "%SMILE_ROOT%\artifacts\temp\%%~nP.lifetime.out" >nul || exit /b 1
    findstr /x /c:"SMILE_TEXT_LIVE=0" "%SMILE_ROOT%\artifacts\temp\%%~nP.lifetime.out" >nul || exit /b 1
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\LightweightOopCalls\%%P" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\%%~nP"
    if errorlevel 1 exit /b 1
    node --check "%SMILE_ROOT%\artifacts\web\%%~nP\game.js" || exit /b 1
    node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\%%~nP" --expected "%SMILE_ROOT%\examples\LightweightOopCalls\LightweightOopCalls.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\%%~nP.out" --timeout 10000
    if errorlevel 1 exit /b 1
)
echo Optional/default, Type-member, and Class package metadata plus project/package native/Web parity tests passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\ClassRuntime\ClassRuntime.smileproj" --target windows-x64 --configuration Release --debug -o "%SMILE_ROOT%\artifacts\games\ClassRuntime.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\ClassRuntime.exe" > "%SMILE_ROOT%\artifacts\temp\ClassRuntime.out"
if errorlevel 1 exit /b 1
fc "%SMILE_ROOT%\examples\ClassRuntime\ClassRuntime.expected.txt" "%SMILE_ROOT%\artifacts\temp\ClassRuntime.out" >nul || exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS=1"
set "SMILE_TEXT_LIFETIME_DIAGNOSTICS=1"
"%SMILE_ROOT%\artifacts\games\ClassRuntime.exe" > "%SMILE_ROOT%\artifacts\temp\ClassRuntime.lifetime.out"
if errorlevel 1 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS="
set "SMILE_TEXT_LIFETIME_DIAGNOSTICS="
findstr /x /c:"SMILE_CLASS_LIVE=0" "%SMILE_ROOT%\artifacts\temp\ClassRuntime.lifetime.out" >nul || exit /b 1
findstr /x /c:"SMILE_TEXT_LIVE=0" "%SMILE_ROOT%\artifacts\temp\ClassRuntime.lifetime.out" >nul || exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\ClassRuntime\ClassRuntime.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\ClassRuntime"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\ClassRuntime\game.js" || exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\ClassRuntime" --expected "%SMILE_ROOT%\examples\ClassRuntime\ClassRuntime.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\ClassRuntime.out" --timeout 10000
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\ClassRuntime\ClassEndProgramCleanup.smile" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\ClassEndProgramCleanup.exe"
if errorlevel 1 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS=1"
set "SMILE_TEXT_LIFETIME_DIAGNOSTICS=1"
"%SMILE_ROOT%\artifacts\games\ClassEndProgramCleanup.exe" > "%SMILE_ROOT%\artifacts\temp\ClassEndProgramCleanup.out"
if errorlevel 1 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS="
set "SMILE_TEXT_LIFETIME_DIAGNOSTICS="
findstr /x /c:"SMILE_CLASS_LIVE=0" "%SMILE_ROOT%\artifacts\temp\ClassEndProgramCleanup.out" >nul || exit /b 1
findstr /x /c:"SMILE_TEXT_LIVE=0" "%SMILE_ROOT%\artifacts\temp\ClassEndProgramCleanup.out" >nul || exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\ClassRuntime\ClassEndProgramCleanup.smile" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\ClassEndProgramCleanup"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\ClassEndProgramCleanup\game.js" || exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\ClassEndProgramCleanup" --timeout 10000
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\ClassRuntime\ClassWebOwnership.smile" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\ClassWebOwnership"
if errorlevel 1 exit /b 1
if not exist "%SMILE_ROOT%\artifacts\web\ClassWebOwnership\Assets" mkdir "%SMILE_ROOT%\artifacts\web\ClassWebOwnership\Assets"
copy /y "%SMILE_ROOT%\examples\Phase4VisualSlice\Assets\PixelProof.png" "%SMILE_ROOT%\artifacts\web\ClassWebOwnership\Assets\PixelProof.png" >nul
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\ClassWebOwnership\game.js" || exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\ClassWebOwnership" --frames 3 --timeout 10000 --phase4-ownership
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\ClassRuntime\ClassNothingFailure.smile" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\ClassNothingFailure.exe"
if errorlevel 1 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS=1"
"%SMILE_ROOT%\artifacts\games\ClassNothingFailure.exe" > "%SMILE_ROOT%\artifacts\temp\ClassNothingFailure.out" 2>&1
if not errorlevel 2 exit /b 1
if errorlevel 3 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS="
findstr /x /c:"SMILE runtime error: Object reference is Nothing." "%SMILE_ROOT%\artifacts\temp\ClassNothingFailure.out" >nul || exit /b 1
findstr /x /c:"SMILE_CLASS_LIVE=0" "%SMILE_ROOT%\artifacts\temp\ClassNothingFailure.out" >nul || exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\ClassRuntime\ClassNothingFailure.smile" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\ClassNothingFailure"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\ClassNothingFailure\game.js" || exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\ClassNothingFailure" --expected-runtime-error "Object reference is Nothing." --timeout 10000
if errorlevel 1 exit /b 1
echo Class constructor, identity, evaluation-order, ARC, finalization, and null-failure tests passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\TypeMemberRuntime\TypeMemberRuntime.smileproj" --target windows-x64 --configuration Release --debug -o "%SMILE_ROOT%\artifacts\games\TypeMemberRuntime.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\TypeMemberRuntime.exe" > "%SMILE_ROOT%\artifacts\temp\TypeMemberRuntime.out"
if errorlevel 1 exit /b 1
fc "%SMILE_ROOT%\examples\TypeMemberRuntime\TypeMemberRuntime.expected.txt" "%SMILE_ROOT%\artifacts\temp\TypeMemberRuntime.out" >nul || exit /b 1
set "SMILE_TEXT_LIFETIME_DIAGNOSTICS=1"
"%SMILE_ROOT%\artifacts\games\TypeMemberRuntime.exe" > "%SMILE_ROOT%\artifacts\temp\TypeMemberRuntime.lifetime.out"
if errorlevel 1 exit /b 1
set "SMILE_TEXT_LIFETIME_DIAGNOSTICS="
findstr /x /c:"SMILE_TEXT_LIVE=0" "%SMILE_ROOT%\artifacts\temp\TypeMemberRuntime.lifetime.out" >nul || exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\TypeMemberRuntime\TypeMemberRuntime.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\TypeMemberRuntime"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\TypeMemberRuntime\game.js" || exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\TypeMemberRuntime" --expected "%SMILE_ROOT%\examples\TypeMemberRuntime\TypeMemberRuntime.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\TypeMemberRuntime.out" --timeout 10000
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\TypeMemberRuntime\TypeMemberEndProgramCleanup.smile" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\TypeMemberEndProgramCleanup.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\TypeMemberEndProgramCleanup.exe" > "%SMILE_ROOT%\artifacts\temp\TypeMemberEndProgramCleanup.out"
if errorlevel 1 exit /b 1
fc "%SMILE_ROOT%\examples\TypeMemberRuntime\TypeMemberEndProgramCleanup.expected.txt" "%SMILE_ROOT%\artifacts\temp\TypeMemberEndProgramCleanup.out" >nul || exit /b 1
set "SMILE_TEXT_LIFETIME_DIAGNOSTICS=1"
"%SMILE_ROOT%\artifacts\games\TypeMemberEndProgramCleanup.exe" > "%SMILE_ROOT%\artifacts\temp\TypeMemberEndProgramCleanup.lifetime.out"
if errorlevel 1 exit /b 1
set "SMILE_TEXT_LIFETIME_DIAGNOSTICS="
findstr /x /c:"SMILE_TEXT_LIVE=0" "%SMILE_ROOT%\artifacts\temp\TypeMemberEndProgramCleanup.lifetime.out" >nul || exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\TypeMemberRuntime\TypeMemberWebOwnership.smile" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\TypeMemberWebOwnership"
if errorlevel 1 exit /b 1
xcopy "%SMILE_ROOT%\examples\Phase4VisualSlice\Assets" "%SMILE_ROOT%\artifacts\web\TypeMemberWebOwnership\Assets" /E /I /Y >nul
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\TypeMemberWebOwnership\game.js" || exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\TypeMemberWebOwnership" --frames 3 --timeout 10000 --phase4-ownership
if errorlevel 1 exit /b 1

for %%P in (IllegalPrivateField:SML3440 FieldMethodCollision:SML3440 EmptyProperty:SML3441 MeOutside:SML3442 MissingMember:SML3443 ScalarReceiver:SML3443 TemporaryMethodReceiver:SML3444 TemporaryPropertyReceiver:SML3444 ReadOnlyWrite:SML3445 WriteOnlyRead:SML3445 PrivateMethodAccess:SML3446 PrivatePropertyAccess:SML3446) do (
    for /f "tokens=1,2 delims=:" %%F in ("%%P") do (
        "%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\InvalidTypeMembers\%%F.smile" > "%SMILE_ROOT%\artifacts\temp\InvalidTypeMembers-%%F.log" 2>&1
        if not errorlevel 1 exit /b 1
        if errorlevel 2 exit /b 1
        powershell -NoProfile -Command "$codes=@(); foreach ($match in [regex]::Matches([IO.File]::ReadAllText('%SMILE_ROOT%\artifacts\temp\InvalidTypeMembers-%%F.log'), 'error (SML\d+):')) { $codes += $match.Groups[1].Value }; $actual=$codes -join ','; if ($actual -ne '%%G') { Write-Error ('InvalidTypeMembers %%F expected %%G, found ' + $actual); exit 1 }"
        if errorlevel 1 exit /b 1
        findstr /c:"%%F.smile(" "%SMILE_ROOT%\artifacts\temp\InvalidTypeMembers-%%F.log" >nul || exit /b 1
    )
)

for %%P in (CapabilityMethod.smileproj CapabilityMethod.Package.smileproj CapabilityGetter.smileproj CapabilityGetter.Package.smileproj) do (
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\InvalidTypeMembers\%%P" -o "%SMILE_ROOT%\artifacts\temp\%%~nP.exe" > "%SMILE_ROOT%\artifacts\temp\InvalidTypeMembers-%%P.log" 2>&1
    if not errorlevel 1 exit /b 1
    if errorlevel 2 exit /b 1
    powershell -NoProfile -Command "$codes=@(); foreach ($match in [regex]::Matches([IO.File]::ReadAllText('%SMILE_ROOT%\artifacts\temp\InvalidTypeMembers-%%P.log'), 'error (SML\d+):')) { $codes += $match.Groups[1].Value }; if (($codes -join ',') -ne 'SML3704') { exit 1 }"
    if errorlevel 1 exit /b 1
)

for %%P in (SafeSetter.smileproj SafeSetter.Package.smileproj) do (
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\InvalidTypeMembers\%%P" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\%%~nP.exe"
    if errorlevel 1 exit /b 1
    "%SMILE_ROOT%\artifacts\games\%%~nP.exe" > "%SMILE_ROOT%\artifacts\temp\%%~nP.out"
    if errorlevel 1 exit /b 1
    fc "%SMILE_ROOT%\examples\InvalidTypeMembers\SafeSetter.expected.txt" "%SMILE_ROOT%\artifacts\temp\%%~nP.out" >nul || exit /b 1
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\InvalidTypeMembers\%%P" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\%%~nP"
    if errorlevel 1 exit /b 1
    node --check "%SMILE_ROOT%\artifacts\web\%%~nP\game.js" || exit /b 1
    node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\%%~nP" --expected "%SMILE_ROOT%\examples\InvalidTypeMembers\SafeSetter.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\%%~nP.out" --timeout 10000
    if errorlevel 1 exit /b 1
)
echo Type-member runtime, ownership, exact diagnostics, capabilities, and accessor isolation tests passed.

for %%P in (IllegalStatement:SML3450 PrivateConstructor:SML3451 FieldMethodCollision:SML3451 ClassField:SML3452 ClassInType:SML3452 ClassArray:SML3452 NewNonClass:SML3453 NothingNonClass:SML3454 IdentityWithEquality:SML3455 NothingAccess:SML3457 MissingMember:SML3443 ReadOnlyWrite:SML3445 PrivateMethodAccess:SML3446) do (
    for /f "tokens=1,2 delims=:" %%F in ("%%P") do (
        "%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\InvalidClassMembers\%%F.smile" > "%SMILE_ROOT%\artifacts\temp\InvalidClassMembers-%%F.log" 2>&1
        if not errorlevel 1 exit /b 1
        if errorlevel 2 exit /b 1
        powershell -NoProfile -Command "$codes=@(); foreach ($match in [regex]::Matches([IO.File]::ReadAllText('%SMILE_ROOT%\artifacts\temp\InvalidClassMembers-%%F.log'), 'error (SML\d+):')) { $codes += $match.Groups[1].Value }; $actual=$codes -join ','; if ($actual -ne '%%G') { Write-Error ('InvalidClassMembers %%F expected %%G, found ' + $actual); exit 1 }"
        if errorlevel 1 exit /b 1
        findstr /c:"%%F.smile(" "%SMILE_ROOT%\artifacts\temp\InvalidClassMembers-%%F.log" >nul || exit /b 1
    )
)

for %%P in (CapabilityConstructor.smileproj CapabilityConstructor.Package.smileproj CapabilityMethod.smileproj CapabilityMethod.Package.smileproj CapabilityGetter.smileproj CapabilityGetter.Package.smileproj) do (
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\InvalidClassMembers\%%P" -o "%SMILE_ROOT%\artifacts\temp\InvalidClass-%%~nP.exe" > "%SMILE_ROOT%\artifacts\temp\InvalidClassMembers-%%P.log" 2>&1
    if not errorlevel 1 exit /b 1
    if errorlevel 2 exit /b 1
    powershell -NoProfile -Command "$codes=@(); foreach ($match in [regex]::Matches([IO.File]::ReadAllText('%SMILE_ROOT%\artifacts\temp\InvalidClassMembers-%%P.log'), 'error (SML\d+):')) { $codes += $match.Groups[1].Value }; if (($codes -join ',') -ne 'SML3704') { exit 1 }"
    if errorlevel 1 exit /b 1
)

for %%P in (SafeSetter.smileproj SafeSetter.Package.smileproj) do (
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\InvalidClassMembers\%%P" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\InvalidClass-%%~nP.exe"
    if errorlevel 1 exit /b 1
    "%SMILE_ROOT%\artifacts\games\InvalidClass-%%~nP.exe" > "%SMILE_ROOT%\artifacts\temp\InvalidClass-%%~nP.out"
    if errorlevel 1 exit /b 1
    fc "%SMILE_ROOT%\examples\InvalidClassMembers\SafeSetter.expected.txt" "%SMILE_ROOT%\artifacts\temp\InvalidClass-%%~nP.out" >nul || exit /b 1
    set "SMILE_CLASS_LIFETIME_DIAGNOSTICS=1"
    "%SMILE_ROOT%\artifacts\games\InvalidClass-%%~nP.exe" > "%SMILE_ROOT%\artifacts\temp\InvalidClass-%%~nP.lifetime.out"
    if errorlevel 1 exit /b 1
    set "SMILE_CLASS_LIFETIME_DIAGNOSTICS="
    findstr /x /c:"SMILE_CLASS_LIVE=0" "%SMILE_ROOT%\artifacts\temp\InvalidClass-%%~nP.lifetime.out" >nul || exit /b 1
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\InvalidClassMembers\%%P" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\InvalidClass-%%~nP"
    if errorlevel 1 exit /b 1
    node --check "%SMILE_ROOT%\artifacts\web\InvalidClass-%%~nP\game.js" || exit /b 1
    node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\InvalidClass-%%~nP" --expected "%SMILE_ROOT%\examples\InvalidClassMembers\SafeSetter.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\InvalidClass-%%~nP.out" --timeout 10000
    if errorlevel 1 exit /b 1
)
echo Class exact diagnostics, constructor/member/accessor capabilities, and safe-setter isolation tests passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\OptionalNamedStandalone.smile" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\OptionalNamedStandalone.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\OptionalNamedStandalone.exe" > "%SMILE_ROOT%\artifacts\temp\OptionalNamedStandalone.out"
if errorlevel 1 exit /b 1
fc "%SMILE_ROOT%\examples\OptionalNamedStandalone.expected.txt" "%SMILE_ROOT%\artifacts\temp\OptionalNamedStandalone.out" >nul || exit /b 1
set "SMILE_TEXT_LIFETIME_DIAGNOSTICS=1"
"%SMILE_ROOT%\artifacts\games\OptionalNamedStandalone.exe" > "%SMILE_ROOT%\artifacts\temp\OptionalNamedStandalone.lifetime.out"
if errorlevel 1 exit /b 1
set "SMILE_TEXT_LIFETIME_DIAGNOSTICS="
findstr /x /c:"SMILE_TEXT_LIVE=0" "%SMILE_ROOT%\artifacts\temp\OptionalNamedStandalone.lifetime.out" >nul || exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\OptionalNamedStandalone.smile" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\OptionalNamedStandalone"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\OptionalNamedStandalone\game.js" || exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\OptionalNamedStandalone" --expected "%SMILE_ROOT%\examples\OptionalNamedStandalone.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\OptionalNamedStandalone.out" --timeout 10000
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\OptionalNamedEndProgramCleanup.smile" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\OptionalNamedEndProgramCleanup.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\OptionalNamedEndProgramCleanup.exe" > "%SMILE_ROOT%\artifacts\temp\OptionalNamedEndProgramCleanup.out"
if errorlevel 1 exit /b 1
fc "%SMILE_ROOT%\examples\OptionalNamedEndProgramCleanup.expected.txt" "%SMILE_ROOT%\artifacts\temp\OptionalNamedEndProgramCleanup.out" >nul || exit /b 1
set "SMILE_TEXT_LIFETIME_DIAGNOSTICS=1"
"%SMILE_ROOT%\artifacts\games\OptionalNamedEndProgramCleanup.exe" > "%SMILE_ROOT%\artifacts\temp\OptionalNamedEndProgramCleanup.lifetime.out"
if errorlevel 1 exit /b 1
set "SMILE_TEXT_LIFETIME_DIAGNOSTICS="
findstr /x /c:"SMILE_TEXT_LIVE=0" "%SMILE_ROOT%\artifacts\temp\OptionalNamedEndProgramCleanup.lifetime.out" >nul || exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\OptionalNamedWebOwnership.smile" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\OptionalNamedWebOwnership"
if errorlevel 1 exit /b 1
xcopy "%SMILE_ROOT%\examples\Phase4VisualSlice\Assets" "%SMILE_ROOT%\artifacts\web\OptionalNamedWebOwnership\Assets" /E /I /Y >nul
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\OptionalNamedWebOwnership\game.js" || exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\OptionalNamedWebOwnership" --frames 3 --timeout 10000 --phase4-ownership
if errorlevel 1 exit /b 1

for %%P in (DefaultTypeMismatch:SML3431 DuplicateArgument:SML3434 MissingRequired:SML3435 NamedBuiltIn:SML3433 OptionalByRef:SML3430 PositionalAfterNamed:SML3432 RequiredAfterOptional:SML3430 UnknownName:SML3433) do (
    for /f "tokens=1,2 delims=:" %%F in ("%%P") do (
        "%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\InvalidOptionalNamed\%%F.smile" > "%SMILE_ROOT%\artifacts\temp\InvalidOptionalNamed-%%F.log" 2>&1
        if not errorlevel 1 exit /b 1
        if errorlevel 2 exit /b 1
        powershell -NoProfile -Command "$codes=@(); foreach ($match in [regex]::Matches([IO.File]::ReadAllText('%SMILE_ROOT%\artifacts\temp\InvalidOptionalNamed-%%F.log'), 'error (SML\d+):')) { $codes += $match.Groups[1].Value }; $actual=$codes -join ','; if ($actual -ne '%%G') { Write-Error ('InvalidOptionalNamed %%F expected %%G, found ' + $actual); exit 1 }"
        if errorlevel 1 exit /b 1
        findstr /c:"%%F.smile(" "%SMILE_ROOT%\artifacts\temp\InvalidOptionalNamed-%%F.log" >nul || exit /b 1
    )
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\InvalidOptionalNamed\UnsafeWebDefault.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\InvalidOptionalNamed-UnsafeWebDefault" > "%SMILE_ROOT%\artifacts\temp\InvalidOptionalNamed-UnsafeWebDefault.log" 2>&1
if not errorlevel 1 exit /b 1
if errorlevel 2 exit /b 1
powershell -NoProfile -Command "$codes=@(); foreach ($match in [regex]::Matches([IO.File]::ReadAllText('%SMILE_ROOT%\artifacts\temp\InvalidOptionalNamed-UnsafeWebDefault.log'), 'error (SML\d+):')) { $codes += $match.Groups[1].Value }; $actual=$codes -join ','; if ($actual -ne 'SML5102') { Write-Error ('InvalidOptionalNamed UnsafeWebDefault expected SML5102, found ' + $actual); exit 1 }"
if errorlevel 1 exit /b 1
findstr /c:"UnsafeWebDefault.smile(" "%SMILE_ROOT%\artifacts\temp\InvalidOptionalNamed-UnsafeWebDefault.log" >nul || exit /b 1
echo Optional/named source-order capture, ByRef location, record ownership, End Program cleanup, and exact diagnostic tests passed.

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

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\EnumCore\EnumCore.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\EnumCore.exe" --debug
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\EnumCore.exe" > "%SMILE_ROOT%\artifacts\temp\EnumCore.out"
if errorlevel 1 exit /b 1
fc "%SMILE_ROOT%\examples\EnumCore\EnumCore.expected.txt" "%SMILE_ROOT%\artifacts\temp\EnumCore.out" >nul || exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\EnumCore\EnumCore.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\EnumCore"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\EnumCore\game.js" || exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\EnumCore" --expected "%SMILE_ROOT%\examples\EnumCore\EnumCore.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\EnumCore.out" --timeout 10000 || exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\EnumCoreStandalone.smile" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\EnumCoreStandalone.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\games\EnumCoreStandalone.exe" > "%SMILE_ROOT%\artifacts\temp\EnumCoreStandalone.out"
if errorlevel 1 exit /b 1
fc "%SMILE_ROOT%\examples\EnumCoreStandalone.expected.txt" "%SMILE_ROOT%\artifacts\temp\EnumCoreStandalone.out" >nul || exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\EnumCoreStandalone.smile" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\EnumCoreStandalone"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\EnumCoreStandalone\game.js" || exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\EnumCoreStandalone" --expected "%SMILE_ROOT%\examples\EnumCoreStandalone.expected.txt" --native-output "%SMILE_ROOT%\artifacts\temp\EnumCoreStandalone.out" --timeout 10000 || exit /b 1

for %%P in (Arithmetic:SML3424 CheckedConstantOverflow:SML3422 DuplicateMember:SML3421 DuplicateSelectAlias:SML3019 EnumInitializerArithmetic:SML3422 IdentityMismatch:SML3424+SML3304 ImplicitOverflow:SML3422 MissingMember:SML3423 NumberConversion:SML3304+SML3304) do (
    for /f "tokens=1,2 delims=:" %%F in ("%%P") do (
        "%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\InvalidEnum\%%F.smile" > "%SMILE_ROOT%\artifacts\temp\InvalidEnum-%%F.log" 2>&1
        if not errorlevel 1 exit /b 1
        if errorlevel 2 exit /b 1
        powershell -NoProfile -Command "$codes=@(); foreach ($match in [regex]::Matches([IO.File]::ReadAllText('%SMILE_ROOT%\artifacts\temp\InvalidEnum-%%F.log'), 'error (SML\d+):')) { $codes += $match.Groups[1].Value }; $actual=$codes -join ','; $expected='%%G'.Replace('+', ','); if ($actual -ne $expected) { Write-Error ('InvalidEnum %%F expected ' + $expected + ', found ' + $actual); exit 1 }"
        if errorlevel 1 exit /b 1
        findstr /c:"%%F.smile(" "%SMILE_ROOT%\artifacts\temp\InvalidEnum-%%F.log" >nul || exit /b 1
    )
)
echo Enum nominal typing, checked values, native qword, Web BigInt, editor, parser recovery, and exact diagnostic tests passed.

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
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip=[IO.Compression.ZipFile]::OpenRead('%SMILE_ROOT%\artifacts\libraries\Smile.UI.smilelib'); try { $manifest=([IO.StreamReader]::new($zip.GetEntry('manifest.json').Open())).ReadToEnd() | ConvertFrom-Json; $apiText=([IO.StreamReader]::new($zip.GetEntry('api/public-symbols.json').Open())).ReadToEnd(); $api=$apiText | ConvertFrom-Json; $core=$api.modules | Where-Object name -eq 'Smile.UI.Core'; $menuModule=$api.modules | Where-Object name -eq 'Smile.UI.Menu'; $dialogueModule=$api.modules | Where-Object name -eq 'Smile.UI.Dialogue'; $legacyNavigator=$api.modules | Where-Object name -eq 'Smile.UI.MenuNavigator'; $insets=$core.members | Where-Object name -eq 'Insets'; $menu=$menuModule.members | Where-Object name -eq 'Menu'; $navigator=$menuModule.members | Where-Object name -eq 'MenuNavigator'; $dialogue=$dialogueModule.members | Where-Object name -eq 'Dialogue'; $menuDraw=$menu.members | Where-Object name -eq 'Draw'; $menuUpdate=$menu.members | Where-Object name -eq 'Update'; $navDraw=$navigator.members | Where-Object name -eq 'Draw'; $navUpdate=$navigator.members | Where-Object name -eq 'Update'; $dialogueDraw=$dialogue.members | Where-Object name -eq 'Draw'; $dialogueSet=$dialogue.members | Where-Object name -eq 'SetStyle'; $addItem=$menu.members | Where-Object name -eq 'AddItem'; $bind=$navigator.members | Where-Object name -eq 'BindSubmenu'; if ($manifest.formatVersion -ne 6 -or $api.formatVersion -ne 6 -or $api.library.provider -ne 'Smile.UI@2.0.0' -or $manifest.version -ne '2.0.0' -or @($api.modules).Count -ne 6 -or $null -ne $legacyNavigator -or $menu.kind -ne 'Class' -or $navigator.kind -ne 'Class' -or $dialogue.kind -ne 'Class' -or $insets.fields.name -cnotcontains 'Left' -or $insets.fields.name -cnotcontains 'Right' -or !$menuDraw.requiresGameWindow -or $menuUpdate.requiresGameWindow -or !$navDraw.requiresGameWindow -or $navUpdate.requiresGameWindow -or !$dialogueDraw.requiresGameWindow -or !$dialogueSet.requiresGameWindow -or !$addItem.parameters[2].optional -or !$addItem.parameters[2].default.value -or !$bind.parameters[3].optional -or !$bind.parameters[3].default.value -or $apiText.Contains('MenuHandleCreate') -or $apiText.Contains('NavigatorHandleCreate') -or $apiText.Contains('DialogueHandleCreate') -or $apiText.Contains('InternalNavigationHandle')) { exit 1 } } finally { $zip.Dispose() }"
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5UIStateTests\Phase5UIStateTests.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\Phase5UIStateTests.exe"
if errorlevel 1 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS=1"
"%SMILE_ROOT%\artifacts\games\Phase5UIStateTests.exe" > "%SMILE_ROOT%\artifacts\temp\Phase5UIStateTests.out"
if errorlevel 1 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS="
powershell -NoProfile -Command "$expected=[IO.File]::ReadAllLines('%SMILE_ROOT%\examples\Phase5UIStateTests\Phase5UIStateTests.expected.txt',[Text.Encoding]::UTF8); $raw=[IO.File]::ReadAllLines('%SMILE_ROOT%\artifacts\temp\Phase5UIStateTests.out',[Text.Encoding]::UTF8); $actual=@($raw | Where-Object { $_ -notlike 'SMILE_CLASS_LIVE=*' }); $lifetime=@($raw | Where-Object { $_ -like 'SMILE_CLASS_LIVE=*' }); if ((Compare-Object $expected $actual) -or $lifetime.Count -ne 1 -or $lifetime[0] -cne 'SMILE_CLASS_LIVE=0') { exit 1 }"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5UIStateTests\Phase5UIStateTests.Package.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\Phase5UIStateTestsPackage.exe"
if errorlevel 1 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS=1"
"%SMILE_ROOT%\artifacts\games\Phase5UIStateTestsPackage.exe" > "%SMILE_ROOT%\artifacts\temp\Phase5UIStateTestsPackage.out"
if errorlevel 1 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS="
fc /b "%SMILE_ROOT%\artifacts\temp\Phase5UIStateTests.out" "%SMILE_ROOT%\artifacts\temp\Phase5UIStateTestsPackage.out" >nul
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5SubmenuStateTests\Phase5SubmenuStateTests.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\Phase5SubmenuStateTests.exe"
if errorlevel 1 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS=1"
"%SMILE_ROOT%\artifacts\games\Phase5SubmenuStateTests.exe" > "%SMILE_ROOT%\artifacts\temp\Phase5SubmenuStateTests.out"
if errorlevel 1 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS="
powershell -NoProfile -Command "$expected=[IO.File]::ReadAllLines('%SMILE_ROOT%\examples\Phase5SubmenuStateTests\Phase5SubmenuStateTests.expected.txt',[Text.Encoding]::UTF8); $raw=[IO.File]::ReadAllLines('%SMILE_ROOT%\artifacts\temp\Phase5SubmenuStateTests.out',[Text.Encoding]::UTF8); $actual=@($raw | Where-Object { $_ -notlike 'SMILE_CLASS_LIVE=*' }); $lifetime=@($raw | Where-Object { $_ -like 'SMILE_CLASS_LIVE=*' }); if ((Compare-Object $expected $actual) -or $lifetime.Count -ne 1 -or $lifetime[0] -cne 'SMILE_CLASS_LIVE=0') { exit 1 }"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5SubmenuStateTests\Phase5SubmenuStateTests.Package.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\games\Phase5SubmenuStateTestsPackage.exe"
if errorlevel 1 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS=1"
"%SMILE_ROOT%\artifacts\games\Phase5SubmenuStateTestsPackage.exe" > "%SMILE_ROOT%\artifacts\temp\Phase5SubmenuStateTestsPackage.out"
if errorlevel 1 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS="
powershell -NoProfile -Command "$project=[IO.File]::ReadAllLines('%SMILE_ROOT%\artifacts\temp\Phase5SubmenuStateTests.out',[Text.Encoding]::UTF8); $package=[IO.File]::ReadAllLines('%SMILE_ROOT%\artifacts\temp\Phase5SubmenuStateTestsPackage.out',[Text.Encoding]::UTF8); if (Compare-Object $project $package) { exit 1 }"
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase5DialogueStateTests\Phase5DialogueStateTests.smileproj" --target windows-x64 --configuration Release --graphics GDI -o "%SMILE_ROOT%\artifacts\games\Phase5DialogueStateTests.exe"
if errorlevel 1 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS=1"
"%SMILE_ROOT%\artifacts\games\Phase5DialogueStateTests.exe" > "%SMILE_ROOT%\artifacts\temp\Phase5DialogueStateTests.out"
if errorlevel 1 exit /b 1
set "SMILE_CLASS_LIFETIME_DIAGNOSTICS="
powershell -NoProfile -Command "$expected=[IO.File]::ReadAllLines('%SMILE_ROOT%\examples\Phase5DialogueStateTests\Phase5DialogueStateTests.expected.txt',[Text.Encoding]::UTF8); $raw=[IO.File]::ReadAllLines('%SMILE_ROOT%\artifacts\temp\Phase5DialogueStateTests.out',[Text.Encoding]::UTF8); $actual=@($raw | Where-Object { $_ -notlike 'SMILE_CLASS_LIVE=*' }); $lifetime=@($raw | Where-Object { $_ -like 'SMILE_CLASS_LIVE=*' }); if ((Compare-Object $expected $actual) -or $lifetime.Count -ne 1 -or $lifetime[0] -cne 'SMILE_CLASS_LIVE=0') { exit 1 }"
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
powershell -NoProfile -Command "$matches=Select-String -LiteralPath '%SMILE_ROOT%\artifacts\temp\ConsoleDrawStack.log' -SimpleMatch 'SML3704'; if ($matches.Count -ne 1 -or $matches.Line -notmatch 'Program\.smile\(7,16\).*Draw.*requires a Game Window') { exit 1 }"
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

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\libraries\Smile.Game\Smile.Game.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.Game.smilelib"
if errorlevel 1 exit /b 1
copy /y "%SMILE_ROOT%\artifacts\libraries\Smile.Game.smilelib" "%SMILE_ROOT%\artifacts\temp\Smile.Game.first.smilelib" >nul
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\libraries\Smile.Game\Smile.Game.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.Game.smilelib"
if errorlevel 1 exit /b 1
fc /b "%SMILE_ROOT%\artifacts\temp\Smile.Game.first.smilelib" "%SMILE_ROOT%\artifacts\libraries\Smile.Game.smilelib" >nul
if errorlevel 1 exit /b 1
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip=[IO.Compression.ZipFile]::OpenRead('%SMILE_ROOT%\artifacts\libraries\Smile.Game.smilelib'); try { $manifest=([IO.StreamReader]::new($zip.GetEntry('manifest.json').Open())).ReadToEnd() | ConvertFrom-Json; $api=([IO.StreamReader]::new($zip.GetEntry('api/public-symbols.json').Open())).ReadToEnd() | ConvertFrom-Json; $names=@($api.modules.name); if ($manifest.formatVersion -ne 6 -or $api.formatVersion -ne 6 -or $api.library.provider -ne $manifest.provider -or $manifest.version -ne '1.0.0' -or $names.Count -ne 5 -or $names -notcontains 'Smile.Game.Core' -or $names -notcontains 'Smile.Game.TileMap' -or @($api.modules.members | Where-Object requiresGameWindow).Count -ne 2) { exit 1 } } finally { $zip.Dispose() }"
if errorlevel 1 exit /b 1

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\libraries\Smile.RPG\Smile.RPG.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.RPG.smilelib"
if errorlevel 1 exit /b 1
copy /y "%SMILE_ROOT%\artifacts\libraries\Smile.RPG.smilelib" "%SMILE_ROOT%\artifacts\temp\Smile.RPG.first.smilelib" >nul
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\libraries\Smile.RPG\Smile.RPG.smilelibproj" --target library --configuration Release -o "%SMILE_ROOT%\artifacts\libraries\Smile.RPG.smilelib"
if errorlevel 1 exit /b 1
fc /b "%SMILE_ROOT%\artifacts\temp\Smile.RPG.first.smilelib" "%SMILE_ROOT%\artifacts\libraries\Smile.RPG.smilelib" >nul
if errorlevel 1 exit /b 1
powershell -NoProfile -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; $zip=[IO.Compression.ZipFile]::OpenRead('%SMILE_ROOT%\artifacts\libraries\Smile.RPG.smilelib'); try { $manifest=([IO.StreamReader]::new($zip.GetEntry('manifest.json').Open())).ReadToEnd() | ConvertFrom-Json; $api=([IO.StreamReader]::new($zip.GetEntry('api/public-symbols.json').Open())).ReadToEnd() | ConvertFrom-Json; $names=@($api.modules.name); if ($manifest.formatVersion -ne 6 -or $api.formatVersion -ne 6 -or $api.library.provider -ne $manifest.provider -or $manifest.version -ne '1.2.0' -or $names.Count -ne 15 -or $names -notcontains 'Smile.RPG.Core' -or $names -notcontains 'Smile.RPG.World' -or $names -notcontains 'Smile.RPG.Story' -or $names -notcontains 'Smile.RPG.Encounters' -or $names -notcontains 'Smile.RPG.BattleEffects' -or $names -notcontains 'Smile.RPG.BattleCore' -or $names -notcontains 'Smile.RPG.BattleStrategy' -or $names -notcontains 'Smile.RPG.BattleView' -or $names -notcontains 'Smile.RPG.SaveGames' -or @($api.modules.members | Where-Object requiresGameWindow).Count -ne 0 -or $api.modules.members.name -notcontains 'RPG_RESULT_NOT_SELLABLE' -or $api.modules.members.name -notcontains 'RPG_RESULT_BATTLE_ACTIVE') { exit 1 } } finally { $zip.Dispose() }"
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

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase7WorldStateTests\Phase7WorldStateTests.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\tests\Phase7WorldStateTests.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\tests\Phase7WorldStateTests.exe" > "%SMILE_ROOT%\artifacts\temp\Phase7WorldStateTests.out"
if errorlevel 1 exit /b 1
findstr /x /c:"Phase 7 world state tests: PASS" "%SMILE_ROOT%\artifacts\temp\Phase7WorldStateTests.out" >nul
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase7WorldStateTests\Phase7WorldStateTests.Package.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\tests\Phase7WorldStateTestsPackage.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\tests\Phase7WorldStateTestsPackage.exe" > "%SMILE_ROOT%\artifacts\temp\Phase7WorldStateTestsPackage.out"
if errorlevel 1 exit /b 1
fc /b "%SMILE_ROOT%\artifacts\temp\Phase7WorldStateTests.out" "%SMILE_ROOT%\artifacts\temp\Phase7WorldStateTestsPackage.out" >nul
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase7WorldStateTests\Phase7WorldStateTests.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase7WorldStateTests"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\Phase7WorldStateTests\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase7WorldStateTests" --native-output "%SMILE_ROOT%\artifacts\temp\Phase7WorldStateTests.out" --timeout 10000
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase7WorldStateTests\Phase7WorldStateTests.Package.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase7WorldStateTestsPackage"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\Phase7WorldStateTestsPackage\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase7WorldStateTestsPackage" --native-output "%SMILE_ROOT%\artifacts\temp\Phase7WorldStateTestsPackage.out" --timeout 10000
if errorlevel 1 exit /b 1

if not exist "%SMILE_ROOT%\artifacts\games\RpgWorldGallery-DirectX" mkdir "%SMILE_ROOT%\artifacts\games\RpgWorldGallery-DirectX"
if not exist "%SMILE_ROOT%\artifacts\games\RpgWorldGallery-GDI" mkdir "%SMILE_ROOT%\artifacts\games\RpgWorldGallery-GDI"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\RpgWorldGallery\RpgWorldGallery.smileproj" --target windows-x64 --configuration Release --graphics DirectX -o "%SMILE_ROOT%\artifacts\games\RpgWorldGallery-DirectX\RpgWorldGallery.exe" --debug
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\RpgWorldGallery\RpgWorldGallery.smileproj" --target windows-x64 --configuration Release --graphics GDI -o "%SMILE_ROOT%\artifacts\games\RpgWorldGallery-GDI\RpgWorldGallery.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\RpgWorldGallery\RpgWorldGallery.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\RpgWorldGallery"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\RpgWorldGallery\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\RpgWorldGallery" --frames 40 --timeout 10000
if errorlevel 1 exit /b 1
for %%A in (Companion.png EncounterBackground.png Hero.png MireWarden.png Npc.png PanelOverlay.png TitleBackground.png WorldTiles.png LumenTheme.wav) do (
    fc /b "%SMILE_ROOT%\examples\RpgWorldGallery\Assets\%%A" "%SMILE_ROOT%\artifacts\web\RpgWorldGallery\Assets\%%A" >nul
    if errorlevel 1 exit /b 1
)
for %%A in (Town.smilemap Shop.smilemap Overworld.smilemap) do (
    fc /b "%SMILE_ROOT%\examples\RpgWorldGallery\Maps\%%A" "%SMILE_ROOT%\artifacts\web\RpgWorldGallery\Maps\%%A" >nul
    if errorlevel 1 exit /b 1
)
echo Phase 7 Smile.Game, Smile.RPG world state, format compatibility, package, gallery, DirectX, GDI, and Web tests passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase8DungeonStateTests\Phase8DungeonStateTests.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\tests\Phase8DungeonStateTests.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\tests\Phase8DungeonStateTests.exe" > "%SMILE_ROOT%\artifacts\temp\Phase8DungeonStateTests.out"
if errorlevel 1 exit /b 1
findstr /x /c:"Phase 8 dungeon state tests: PASS" "%SMILE_ROOT%\artifacts\temp\Phase8DungeonStateTests.out" >nul
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase8DungeonStateTests\Phase8DungeonStateTests.Package.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\tests\Phase8DungeonStateTestsPackage.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\tests\Phase8DungeonStateTestsPackage.exe" > "%SMILE_ROOT%\artifacts\temp\Phase8DungeonStateTestsPackage.out"
if errorlevel 1 exit /b 1
fc /b "%SMILE_ROOT%\artifacts\temp\Phase8DungeonStateTests.out" "%SMILE_ROOT%\artifacts\temp\Phase8DungeonStateTestsPackage.out" >nul
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase8DungeonStateTests\Phase8DungeonStateTests.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase8DungeonStateTests"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\Phase8DungeonStateTests\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase8DungeonStateTests" --native-output "%SMILE_ROOT%\artifacts\temp\Phase8DungeonStateTests.out" --timeout 10000
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase8DungeonStateTests\Phase8DungeonStateTests.Package.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase8DungeonStateTestsPackage"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\Phase8DungeonStateTestsPackage\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase8DungeonStateTestsPackage" --native-output "%SMILE_ROOT%\artifacts\temp\Phase8DungeonStateTestsPackage.out" --timeout 10000
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\test-phase8-dungeon-workflow-rollback.ps1"
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\test-phase8-dungeon-maps.ps1"
if errorlevel 1 exit /b 1

if not exist "%SMILE_ROOT%\artifacts\games\RpgDungeonGallery-DirectX" mkdir "%SMILE_ROOT%\artifacts\games\RpgDungeonGallery-DirectX"
if not exist "%SMILE_ROOT%\artifacts\games\RpgDungeonGallery-GDI" mkdir "%SMILE_ROOT%\artifacts\games\RpgDungeonGallery-GDI"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\RpgDungeonGallery\RpgDungeonGallery.smileproj" --target windows-x64 --configuration Release --graphics DirectX -o "%SMILE_ROOT%\artifacts\games\RpgDungeonGallery-DirectX\RpgDungeonGallery.exe" --debug
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\RpgDungeonGallery\RpgDungeonGallery.smileproj" --target windows-x64 --configuration Release --graphics GDI -o "%SMILE_ROOT%\artifacts\games\RpgDungeonGallery-GDI\RpgDungeonGallery.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\RpgDungeonGallery\RpgDungeonGallery.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\RpgDungeonGallery"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\RpgDungeonGallery\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\RpgDungeonGallery" --frames 40 --timeout 10000
if errorlevel 1 exit /b 1
for %%A in (Companion.png Hero.png MireWarden.png Npc.png WorldTiles.png) do (
    fc /b "%SMILE_ROOT%\examples\RpgDungeonGallery\Assets\%%A" "%SMILE_ROOT%\artifacts\web\RpgDungeonGallery\Assets\%%A" >nul
    if errorlevel 1 exit /b 1
)
for %%A in (Archive1.smilemap Archive2.smilemap Archive3.smilemap Archive4.smilemap) do (
    fc /b "%SMILE_ROOT%\examples\RpgDungeonGallery\Maps\%%A" "%SMILE_ROOT%\artifacts\web\RpgDungeonGallery\Maps\%%A" >nul
    if errorlevel 1 exit /b 1
)
echo Phase 8 dungeon composition, SRPG 2 state, package, DirectX, GDI, and DPR-2 Web tests passed.

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase9BattleStateTests\Phase9BattleStateTests.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\tests\Phase9BattleStateTests.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\tests\Phase9BattleStateTests.exe" > "%SMILE_ROOT%\artifacts\temp\Phase9BattleStateTests.out"
if errorlevel 1 exit /b 1
findstr /x /c:"Phase 9 battle state tests: PASS" "%SMILE_ROOT%\artifacts\temp\Phase9BattleStateTests.out" >nul
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase9BattleStateTests\Phase9BattleStateTests.Package.smileproj" --target windows-x64 --configuration Release -o "%SMILE_ROOT%\artifacts\tests\Phase9BattleStateTestsPackage.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\tests\Phase9BattleStateTestsPackage.exe" > "%SMILE_ROOT%\artifacts\temp\Phase9BattleStateTestsPackage.out"
if errorlevel 1 exit /b 1
fc /b "%SMILE_ROOT%\artifacts\temp\Phase9BattleStateTests.out" "%SMILE_ROOT%\artifacts\temp\Phase9BattleStateTestsPackage.out" >nul
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase9BattleStateTests\Phase9BattleStateTests.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase9BattleStateTests"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\Phase9BattleStateTests\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase9BattleStateTests" --native-output "%SMILE_ROOT%\artifacts\temp\Phase9BattleStateTests.out" --timeout 10000
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\Phase9BattleStateTests\Phase9BattleStateTests.Package.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\Phase9BattleStateTestsPackage"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\Phase9BattleStateTestsPackage\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\Phase9BattleStateTestsPackage" --native-output "%SMILE_ROOT%\artifacts\temp\Phase9BattleStateTestsPackage.out" --timeout 10000
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\test-phase9-battle-rollback.ps1"
if errorlevel 1 exit /b 1

if not exist "%SMILE_ROOT%\artifacts\games\RpgBattleGallery-DirectX" mkdir "%SMILE_ROOT%\artifacts\games\RpgBattleGallery-DirectX"
if not exist "%SMILE_ROOT%\artifacts\games\RpgBattleGallery-GDI" mkdir "%SMILE_ROOT%\artifacts\games\RpgBattleGallery-GDI"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\RpgBattleGallery\RpgBattleGallery.smileproj" --target windows-x64 --configuration Release --graphics DirectX -o "%SMILE_ROOT%\artifacts\games\RpgBattleGallery-DirectX\RpgBattleGallery.exe" --debug
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\RpgBattleGallery\RpgBattleGallery.smileproj" --target windows-x64 --configuration Release --graphics GDI -o "%SMILE_ROOT%\artifacts\games\RpgBattleGallery-GDI\RpgBattleGallery.exe"
if errorlevel 1 exit /b 1
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" --project "%SMILE_ROOT%\examples\RpgBattleGallery\RpgBattleGallery.smileproj" --target web --configuration Release --output-dir "%SMILE_ROOT%\artifacts\web\RpgBattleGallery"
if errorlevel 1 exit /b 1
node --check "%SMILE_ROOT%\artifacts\web\RpgBattleGallery\game.js"
if errorlevel 1 exit /b 1
node "%SMILE_ROOT%\scripts\run-web-test.js" "%SMILE_ROOT%\artifacts\web\RpgBattleGallery" --frames 40 --timeout 10000
if errorlevel 1 exit /b 1
for %%A in (Ability.wav DungeonTheme.wav EnemyLineup.png LumenPlaza.png OverworldTheme.wav PartyLineup.png PrismVault.png StarfallPlateau.png Strike.wav TownTheme.wav Victory.wav) do (
    fc /b "%SMILE_ROOT%\examples\RpgBattleGallery\Assets\%%A" "%SMILE_ROOT%\artifacts\web\RpgBattleGallery\Assets\%%A" >nul
    if errorlevel 1 exit /b 1
)
echo Phase 9 battle modules, SRPG-2 active-session boundary, project/package parity, six rollback checkpoints, gallery, DirectX, GDI, and DPR-2 Web tests passed.

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
for %%C in (SML3012 SML3006 SML3017 SML3435 SML3018 SML3019 SML3021) do (
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
