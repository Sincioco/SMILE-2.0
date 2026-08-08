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

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\GraphicsTextSample.smile" -o "%SMILE_ROOT%\artifacts\games\GraphicsTextSample.exe"
if errorlevel 1 exit /b %errorlevel%
if not exist "%SMILE_ROOT%\artifacts\games\GraphicsTextSample.exe" (
    echo GraphicsTextSample native executable is missing.
    exit /b 1
)
echo Required graphics text sample compiled successfully.

if not exist "%SMILE_ROOT%\artifacts\games\Snake" mkdir "%SMILE_ROOT%\artifacts\games\Snake"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\Snake\Program.smile" -o "%SMILE_ROOT%\artifacts\games\Snake\Snake.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\Snake\Assets" "%SMILE_ROOT%\artifacts\games\Snake\Assets" /E /I /Y >nul
if errorlevel 1 exit /b %errorlevel%

echo Snake compiled successfully: %SMILE_ROOT%\artifacts\games\Snake\Snake.exe

if not exist "%SMILE_ROOT%\artifacts\games\FallingBlocks" mkdir "%SMILE_ROOT%\artifacts\games\FallingBlocks"
if not exist "%SMILE_ROOT%\games\FallingBlocks\Assets\Background.mp3" (
    echo Falling Blocks background music source asset is missing.
    exit /b 1
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\FallingBlocks\Program.smile" -o "%SMILE_ROOT%\artifacts\games\FallingBlocks\FallingBlocks.exe" --keep-temp
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
echo Falling Blocks compiled successfully: %SMILE_ROOT%\artifacts\games\FallingBlocks\FallingBlocks.exe

if not exist "%SMILE_ROOT%\artifacts\games\PaddleBall" mkdir "%SMILE_ROOT%\artifacts\games\PaddleBall"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\PaddleBall\Program.smile" -o "%SMILE_ROOT%\artifacts\games\PaddleBall\PaddleBall.exe"
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\PaddleBall\Assets" "%SMILE_ROOT%\artifacts\games\PaddleBall\Assets" /E /I /Y >nul
if errorlevel 1 exit /b %errorlevel%
echo Paddle Ball compiled successfully: %SMILE_ROOT%\artifacts\games\PaddleBall\PaddleBall.exe

if not exist "%SMILE_ROOT%\artifacts\games\BrickBreaker" mkdir "%SMILE_ROOT%\artifacts\games\BrickBreaker"
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\BrickBreaker\Program.smile" -o "%SMILE_ROOT%\artifacts\games\BrickBreaker\BrickBreaker.exe"
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\BrickBreaker\Assets" "%SMILE_ROOT%\artifacts\games\BrickBreaker\Assets" /E /I /Y >nul
if errorlevel 1 exit /b %errorlevel%
echo Brick Breaker compiled successfully: %SMILE_ROOT%\artifacts\games\BrickBreaker\BrickBreaker.exe

if not exist "%SMILE_ROOT%\artifacts\games\DungeonStarI" mkdir "%SMILE_ROOT%\artifacts\games\DungeonStarI"
if not exist "%SMILE_ROOT%\games\DungeonStarI\Assets\Background.mp3" (
    echo Dungeon Star I background music source asset is missing.
    exit /b 1
)
"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\games\DungeonStarI\Program.smile" -o "%SMILE_ROOT%\artifacts\games\DungeonStarI\DungeonStarI.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%
xcopy "%SMILE_ROOT%\games\DungeonStarI\Assets" "%SMILE_ROOT%\artifacts\games\DungeonStarI\Assets" /E /I /Y >nul
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
echo Dungeon Star I compiled successfully: %SMILE_ROOT%\artifacts\games\DungeonStarI\DungeonStarI.exe

powershell -NoProfile -ExecutionPolicy Bypass -File "%SMILE_ROOT%\scripts\verify-artifacts.ps1"
if errorlevel 1 exit /b %errorlevel%

echo Manual gameplay is still required for graphical games.
exit /b 0
