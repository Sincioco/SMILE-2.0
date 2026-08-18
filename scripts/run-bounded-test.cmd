@echo off
setlocal

if "%~2"=="" (
    echo Usage: run-bounded-test.cmd TIMEOUT_SECONDS PROGRAM_PATH 1>&2
    exit /b 2
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Invoke-BoundedProcess.ps1" -TimeoutSeconds %~1 -ProgramPath "%~2"
exit /b %errorlevel%
