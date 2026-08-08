@echo off
setlocal

for %%I in ("%~dp0..") do set "SMILE_ROOT=%%~fI"
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

call "%~dp0build.cmd"
if errorlevel 1 exit /b %errorlevel%

if not exist "%VSWHERE%" (
    echo error: vswhere.exe was not found.
    exit /b 2
)

for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products * -property installationPath`) do set "SMILE_VS=%%I"

if not defined SMILE_VS (
    echo error: Visual Studio was not found.
    exit /b 2
)

set "VSIX_INSTALLER=%SMILE_VS%\Common7\IDE\VSIXInstaller.exe"
set "SMILE_VSIX=%SMILE_ROOT%\artifacts\vsix\Smile.VisualStudio.vsix"

if not exist "%VSIX_INSTALLER%" (
    echo error: VSIXInstaller.exe was not found.
    exit /b 2
)

start "" "%VSIX_INSTALLER%" "%SMILE_VSIX%"
echo Launched the Visual Studio extension installer for:
echo %SMILE_VSIX%
exit /b 0
