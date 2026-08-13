@echo off
setlocal

for %%I in ("%~dp0..") do set "SMILE_ROOT=%%~fI"
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
set "SMILE_EXTENSION_ID=Smile.VisualStudio.2.0"

call "%~dp0build.cmd"
if errorlevel 1 exit /b %errorlevel%

if not exist "%VSWHERE%" (
    echo error: vswhere.exe was not found.
    exit /b 2
)

for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products Microsoft.VisualStudio.Product.Enterprise -requires Microsoft.VisualStudio.Component.CoreEditor -property installationPath`) do set "SMILE_VS=%%I"
for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products Microsoft.VisualStudio.Product.Enterprise -requires Microsoft.VisualStudio.Component.CoreEditor -property instanceId`) do set "SMILE_VS_INSTANCE=%%I"

if not defined SMILE_VS (
    echo error: Visual Studio Enterprise was not found.
    exit /b 2
)

if not defined SMILE_VS_INSTANCE (
    echo error: Visual Studio Enterprise was not found.
    exit /b 2
)

set "VSIX_INSTALLER=%SMILE_VS%\Common7\IDE\VSIXInstaller.exe"
set "SMILE_VSIX=%SMILE_ROOT%\artifacts\vsix\Smile.VisualStudio.vsix"
set "SMILE_VSIX_DLL=%SMILE_ROOT%\src\Smile.VisualStudio\bin\Release\net472\Smile.VisualStudio.dll"
set "SMILE_VSIX_MANIFEST=%SMILE_ROOT%\src\Smile.VisualStudio\source.extension.vsixmanifest"

if not exist "%VSIX_INSTALLER%" (
    echo error: VSIXInstaller.exe was not found.
    exit /b 2
)

if not exist "%SMILE_VSIX%" (
    echo error: The newly built SMILE VSIX was not found.
    exit /b 2
)

if not exist "%SMILE_ROOT%\artifacts\temp" mkdir "%SMILE_ROOT%\artifacts\temp"

echo Refreshing %SMILE_EXTENSION_ID% in Visual Studio instance %SMILE_VS_INSTANCE%.
echo Visual Studio may close automatically. Save open work before running this script.
echo.
echo [1/3] Removing the installed SMILE extension.
echo Visual Studio's installer will show its own progress window.
"%VSIX_INSTALLER%" /quiet /shutdownprocesses /instanceIds:%SMILE_VS_INSTANCE% /uninstall:%SMILE_EXTENSION_ID% /logFile:"%SMILE_ROOT%\artifacts\temp\vsix-uninstall.log"
if errorlevel 1 echo Existing SMILE extension was not installed or could not be removed; continuing with forced installation.

echo.
echo [2/3] Removing proven orphaned SMILE extension directories.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0verify-vsix-install.ps1" -InstanceId "%SMILE_VS_INSTANCE%" -RemoveOrphans
if errorlevel 1 exit /b %errorlevel%

echo.
echo [3/3] Installing the newly built SMILE extension.
echo Visual Studio's installer will show its own progress window.
"%VSIX_INSTALLER%" /quiet /shutdownprocesses /force /instanceIds:%SMILE_VS_INSTANCE% /logFile:"%SMILE_ROOT%\artifacts\temp\vsix-install.log" "%SMILE_VSIX%"
if errorlevel 1 (
    echo error: The new SMILE extension could not be installed.
    echo See "%SMILE_ROOT%\artifacts\temp\vsix-install.log" for details.
    exit /b 2
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0verify-vsix-install.ps1" -InstanceId "%SMILE_VS_INSTANCE%" -BuiltDllPath "%SMILE_VSIX_DLL%" -ManifestPath "%SMILE_VSIX_MANIFEST%"
if errorlevel 1 exit /b %errorlevel%

echo Installed the newly built SMILE extension automatically:
echo %SMILE_VSIX%
echo Restart Visual Studio to load the refreshed extension.
exit /b 0
