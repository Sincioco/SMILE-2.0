@echo off
setlocal

for %%I in ("%~dp0..") do set "SMILE_ROOT=%%~fI"
set "SMILE_ROOT_SLASH=%SMILE_ROOT:\=/%/"
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

msbuild "%SMILE_ROOT%\src\Smile.NativeRuntime\Smile.NativeRuntime.vcxproj" /m /nr:false /p:Configuration=Release /p:Platform=x64 /p:SolutionDir="%SMILE_ROOT_SLASH%" /v:minimal
if errorlevel 1 exit /b %errorlevel%

msbuild "%SMILE_ROOT%\src\Smile.NativeGraphicsTests\Smile.NativeGraphicsTests.vcxproj" /m /nr:false /p:Configuration=Release /p:Platform=x64 /p:SolutionDir="%SMILE_ROOT_SLASH%" /v:minimal
if errorlevel 1 exit /b %errorlevel%

msbuild "%SMILE_ROOT%\src\Smile.NativeTextTests\Smile.NativeTextTests.vcxproj" /m /nr:false /p:Configuration=Release /p:Platform=x64 /p:SolutionDir="%SMILE_ROOT_SLASH%" /v:minimal
if errorlevel 1 exit /b %errorlevel%

dotnet publish "%SMILE_ROOT%\src\Smile.Compiler\Smile.Compiler.csproj" -c Release -r win-x64 --self-contained false -o "%SMILE_ROOT%\artifacts\compiler"
if errorlevel 1 exit /b %errorlevel%

copy /y "%SMILE_ROOT%\artifacts\runtime\Smile.NativeRuntime.lib" "%SMILE_ROOT%\artifacts\compiler\Smile.NativeRuntime.lib" >nul

msbuild "%SMILE_ROOT%\SMILE 2.0.sln" /m /nr:false /p:Configuration=Release /p:Platform=x64 /v:minimal
if errorlevel 1 exit /b %errorlevel%

if not exist "%SMILE_ROOT%\artifacts\vsix" mkdir "%SMILE_ROOT%\artifacts\vsix"
copy /y "%SMILE_ROOT%\src\Smile.VisualStudio\bin\Release\net472\Smile.VisualStudio.vsix" "%SMILE_ROOT%\artifacts\vsix\Smile.VisualStudio.vsix" >nul
if errorlevel 1 exit /b %errorlevel%

echo Compiler: %SMILE_ROOT%\artifacts\compiler\smilec.exe
echo VSIX: %SMILE_ROOT%\artifacts\vsix\Smile.VisualStudio.vsix
exit /b 0
