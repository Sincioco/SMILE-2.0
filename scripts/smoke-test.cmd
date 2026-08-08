@echo off
setlocal

set "SMILE_ROOT=%~dp0.."

call "%SMILE_ROOT%\scripts\build.cmd"
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

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\RuntimeBasics.smile" -o "%SMILE_ROOT%\artifacts\games\RuntimeBasics.exe"
if errorlevel 1 exit /b %errorlevel%
"%SMILE_ROOT%\artifacts\games\RuntimeBasics.exe"
if errorlevel 1 exit /b %errorlevel%

"%SMILE_ROOT%\artifacts\compiler\smilec.exe" "%SMILE_ROOT%\examples\Snake.smile" -o "%SMILE_ROOT%\artifacts\games\Snake.exe" --keep-temp
if errorlevel 1 exit /b %errorlevel%

echo Snake compiled successfully: %SMILE_ROOT%\artifacts\games\Snake.exe
echo Manual gameplay is still required.
exit /b 0
