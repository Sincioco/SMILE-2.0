@echo off
setlocal

set "SMILE_ROOT=%~dp0.."

call "%SMILE_ROOT%\scripts\build.cmd"
if errorlevel 1 exit /b %errorlevel%

dotnet run --project "%SMILE_ROOT%\src\Smile.Tests\Smile.Tests.csproj" -c Release --no-restore
if errorlevel 1 exit /b %errorlevel%

"%SMILE_ROOT%\artifacts\tests\Smile.NativeGraphicsTests.exe"
if errorlevel 1 exit /b %errorlevel%

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Hello.smile" -o "%SMILE_ROOT%\artifacts\games\Hello.exe"
if errorlevel 1 exit /b %errorlevel%

for /f "delims=" %%I in ('"%SMILE_ROOT%\artifacts\games\Hello.exe"') do set "SMILE_HELLO=%%I"
if not "%SMILE_HELLO%"=="Hello World" (
    echo Hello smoke test failed: expected "Hello World", found "%SMILE_HELLO%".
    exit /b 1
)
echo Hello smoke test passed.

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
for %%V in ("100" "TRUE" "1") do (
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
for %%V in ("EVEN" "12" "40" "1" "2" "200" "5" "3" "2022440" "16744576") do (
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
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\Snake\Program.smile" -o "%SMILE_ROOT%\artifacts\games\Snake\Snake.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\Snake\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\Snake\Snake-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\Snake\Assets" "%SMILE_ROOT%\artifacts\games\Snake\Assets" /E /I /Y >nul
if errorlevel 1 exit /b %errorlevel%

echo Snake demo and no-demo versions compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\FallingBlocks" mkdir "%SMILE_ROOT%\artifacts\games\FallingBlocks"
if not exist "%SMILE_ROOT%\games\FallingBlocks\Assets\Background.mp3" (
    echo Falling Blocks background music source asset is missing.
    exit /b 1
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\FallingBlocks\Program.smile" -o "%SMILE_ROOT%\artifacts\games\FallingBlocks\FallingBlocks.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\FallingBlocks\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\FallingBlocks\FallingBlocks-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\FallingBlocks\Assets" "%SMILE_ROOT%\artifacts\games\FallingBlocks\Assets" /E /I /Y >nul
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
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\PaddleBall\Program.smile" -o "%SMILE_ROOT%\artifacts\games\PaddleBall\PaddleBall.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\PaddleBall\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\PaddleBall\PaddleBall-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\PaddleBall\Assets" "%SMILE_ROOT%\artifacts\games\PaddleBall\Assets" /E /I /Y >nul
if errorlevel 1 exit /b %errorlevel%
echo Paddle Ball demo and no-demo versions compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\BrickBreaker" mkdir "%SMILE_ROOT%\artifacts\games\BrickBreaker"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\BrickBreaker\Program.smile" -o "%SMILE_ROOT%\artifacts\games\BrickBreaker\BrickBreaker.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\BrickBreaker\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\BrickBreaker\BrickBreaker-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\BrickBreaker\Assets" "%SMILE_ROOT%\artifacts\games\BrickBreaker\Assets" /E /I /Y >nul
if errorlevel 1 exit /b %errorlevel%
echo Brick Breaker demo and no-demo versions compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\MazeMuncher" mkdir "%SMILE_ROOT%\artifacts\games\MazeMuncher"
if not exist "%SMILE_ROOT%\games\MazeMuncher\Assets\Background.mp3" (
    echo Maze Muncher background music source asset is missing.
    exit /b 1
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\MazeMuncher\Program.smile" -o "%SMILE_ROOT%\artifacts\games\MazeMuncher\MazeMuncher.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\MazeMuncher\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\MazeMuncher\MazeMuncher-NoDemo.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\MazeMuncher\Assets" "%SMILE_ROOT%\artifacts\games\MazeMuncher\Assets" /E /I /Y >nul
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\MazeMuncher\Maps" "%SMILE_ROOT%\artifacts\games\MazeMuncher\Maps" /E /I /Y >nul
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

if not exist "%SMILE_ROOT%\artifacts\games\StarSquadron" mkdir "%SMILE_ROOT%\artifacts\games\StarSquadron"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\StarSquadron\Program.smile" -o "%SMILE_ROOT%\artifacts\games\StarSquadron\StarSquadron.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\StarSquadron\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\StarSquadron\StarSquadron-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\StarSquadron\Assets" "%SMILE_ROOT%\artifacts\games\StarSquadron\Assets" /E /I /Y >nul
if errorlevel 1 exit /b %errorlevel%
echo Star Squadron demo and no-demo versions compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\DungeonStarI" mkdir "%SMILE_ROOT%\artifacts\games\DungeonStarI"
if not exist "%SMILE_ROOT%\games\DungeonStarI\Assets\Background.mp3" (
    echo Dungeon Star I background music source asset is missing.
    exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\validate-dungeon-maps.ps1"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\DungeonStarI\Program.smile" -o "%SMILE_ROOT%\artifacts\games\DungeonStarI\DungeonStarI.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\DungeonStarI\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\DungeonStarI\DungeonStarI-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\DungeonStarI\Assets" "%SMILE_ROOT%\artifacts\games\DungeonStarI\Assets" /E /I /Y >nul
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\DungeonStarI\Maps" "%SMILE_ROOT%\artifacts\games\DungeonStarI\Maps" /E /I /Y >nul
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
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\DungeonStarII\Program.smile" -o "%SMILE_ROOT%\artifacts\games\DungeonStarII\DungeonStarII.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\DungeonStarII\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\DungeonStarII\DungeonStarII-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\DungeonStarII\Maps" "%SMILE_ROOT%\artifacts\games\DungeonStarII\Maps" /E /I /Y >nul
if errorlevel 1 exit /b %errorlevel%
for %%M in (default.map custom.map) do (
    fc /b "%SMILE_ROOT%\games\DungeonStarII\Maps\%%M" "%SMILE_ROOT%\artifacts\games\DungeonStarII\Maps\%%M" >nul
    if errorlevel 1 (
        echo Dungeon Star II output map %%M does not match its project asset.
        exit /b 1
    )
)
echo Dungeon Star II demo and no-demo versions compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\PlatformQuest" mkdir "%SMILE_ROOT%\artifacts\games\PlatformQuest"
if not exist "%SMILE_ROOT%\games\PlatformQuest\Assets\Background.mp3" (
    echo Platform Quest background music source asset is missing.
    exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\validate-platform-quest-maps.ps1"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\PlatformQuest\Program.smile" -o "%SMILE_ROOT%\artifacts\games\PlatformQuest\PlatformQuest.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\PlatformQuest\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\PlatformQuest\PlatformQuest-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\PlatformQuest\Assets" "%SMILE_ROOT%\artifacts\games\PlatformQuest\Assets" /E /I /Y >nul
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\PlatformQuest\Maps" "%SMILE_ROOT%\artifacts\games\PlatformQuest\Maps" /E /I /Y >nul
if errorlevel 1 exit /b %errorlevel%
for %%A in (Background.mp3 Background.wav Start.wav Jump.wav Coin.wav Block.wav Stomp.wav Hurt.wav Goal.wav GameOver.wav) do (
    fc /b "%SMILE_ROOT%\games\PlatformQuest\Assets\%%A" "%SMILE_ROOT%\artifacts\games\PlatformQuest\Assets\%%A" >nul
    if errorlevel 1 (
        echo Platform Quest output asset %%A does not match its project asset.
        exit /b 1
    )
)
for %%M in (default.map custom.map) do (
    fc /b "%SMILE_ROOT%\games\PlatformQuest\Maps\%%M" "%SMILE_ROOT%\artifacts\games\PlatformQuest\Maps\%%M" >nul
    if errorlevel 1 (
        echo Platform Quest output map %%M does not match its project asset.
        exit /b 1
    )
)
echo Platform Quest demo and no-demo versions compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\SkyHopper" mkdir "%SMILE_ROOT%\artifacts\games\SkyHopper"
if not exist "%SMILE_ROOT%\games\SkyHopper\Assets\Background.mp3" (
    echo Sky Hopper background music source asset is missing.
    exit /b 1
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\SkyHopper\Program.smile" -o "%SMILE_ROOT%\artifacts\games\SkyHopper\SkyHopper.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\SkyHopper\Program-NoDemo.smile" -o "%SMILE_ROOT%\artifacts\games\SkyHopper\SkyHopper-NoDemo.exe"
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\SkyHopper\Assets" "%SMILE_ROOT%\artifacts\games\SkyHopper\Assets" /E /I /Y >nul
if errorlevel 1 exit /b %errorlevel%
for %%A in (Background.mp3 Background.wav Start.wav Flap.wav Score.wav Hit.wav GameOver.wav) do (
    fc /b "%SMILE_ROOT%\games\SkyHopper\Assets\%%A" "%SMILE_ROOT%\artifacts\games\SkyHopper\Assets\%%A" >nul
    if errorlevel 1 (
        echo Sky Hopper output asset %%A does not match its project asset.
        exit /b 1
    )
)
echo Sky Hopper demo and no-demo versions compiled successfully.

for %%G in (Snake FallingBlocks PaddleBall BrickBreaker DungeonStarI DungeonStarII MazeMuncher StarSquadron PlatformQuest SkyHopper) do (
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\%%G\Program.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\%%G"
    if errorlevel 1 exit /b 1
    node --check "%SMILE_ROOT%\artifacts\web\%%G\game.js"
    if errorlevel 1 exit /b 1
    "%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\%%G\Program-NoDemo.smile" --target web --output-dir "%SMILE_ROOT%\artifacts\web\%%G-NoDemo"
    if errorlevel 1 exit /b 1
    node --check "%SMILE_ROOT%\artifacts\web\%%G-NoDemo\game.js"
    if errorlevel 1 exit /b 1
)
echo All ten game demo and no-demo Web versions compiled successfully.

powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\verify-artifacts.ps1"
if errorlevel 1 exit /b %errorlevel%

echo Manual gameplay is still required for graphical games.
exit /b 0
