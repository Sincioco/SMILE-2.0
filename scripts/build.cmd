@echo off
setlocal

set "SMILE_ROOT=%~dp0.."
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if not exist "%VSWHERE%" (
    echo error SML5005: vswhere.exe was not found.
    exit /b 2
)

for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "SMILE_VS=%%I"

if not defined SMILE_VS (
    echo error SML5006: Visual Studio C++ x64 tools were not found.
    exit /b 2
)

call "%SMILE_VS%\VC\Auxiliary\Build\vcvars64.bat" >nul
if errorlevel 1 exit /b %errorlevel%

msbuild "%SMILE_ROOT%\SMILE.sln" /m /p:Configuration=Release /p:Platform=x64 /v:minimal
if errorlevel 1 exit /b %errorlevel%

dotnet publish "%SMILE_ROOT%\src\Smile.Compiler\Smile.Compiler.csproj" -c Release -r win-x64 --self-contained false -o "%SMILE_ROOT%\artifacts\compiler"
if errorlevel 1 exit /b %errorlevel%

copy /y "%SMILE_ROOT%\artifacts\runtime\Smile.NativeRuntime.lib" "%SMILE_ROOT%\artifacts\compiler\Smile.NativeRuntime.lib" >nul

echo Compiler: %SMILE_ROOT%\artifacts\compiler\smilec.exe
exit /b 0
