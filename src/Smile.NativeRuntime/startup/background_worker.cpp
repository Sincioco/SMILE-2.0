#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <strsafe.h>

// One opt-in local PowerShell 7 helper per native application process. The helper
// holds this PID-scoped mutex and receives the actual runtime storage directory.
static WCHAR worker_script[2048];
static WCHAR worker_folder[2048];
static WCHAR worker_mutex[96];
static HANDLE worker_process;
static ULONGLONG next_check;

static void report_error(DWORD error)
{
    WCHAR path[2200];
    char message[96];
    DWORD written;
    if (FAILED(StringCchPrintfW(path, 2200, L"%ls\\native-worker.log", worker_folder))) return;
    if (FAILED(StringCchPrintfA(message, 96, "Native worker startup Windows error: %lu\r\n", error))) return;
    HANDLE file = CreateFileW(path, GENERIC_WRITE, FILE_SHARE_READ, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (file == INVALID_HANDLE_VALUE) return;
    WriteFile(file, message, lstrlenA(message), &written, nullptr);
    CloseHandle(file);
}

extern "C" void smile_native_worker_tick(void)
{
    if (!worker_script[0] || GetTickCount64() < next_check) return;
    next_check = GetTickCount64() + 2000;
    if (worker_process)
    {
        if (WaitForSingleObject(worker_process, 0) == WAIT_TIMEOUT) return;
        CloseHandle(worker_process);
        worker_process = nullptr;
    }
    HANDLE existing = OpenMutexW(SYNCHRONIZE, FALSE, worker_mutex);
    if (existing)
    {
        CloseHandle(existing);
        return;
    }
    WCHAR program[2048], command[6400];
    DWORD length = GetEnvironmentVariableW(L"ProgramFiles", program, 2000);
    if (!length || length >= 2000 ||
        FAILED(StringCchCatW(program, 2048, L"\\PowerShell\\7\\pwsh.exe"))) return;
    if (GetFileAttributesW(program) == INVALID_FILE_ATTRIBUTES) { report_error(GetLastError()); return; }
    if (FAILED(StringCchPrintfW(command, 6400,
        L"\"%ls\" -NoProfile -File \"%ls\" -ParentProcessId %lu -DataFolder \"%ls\"",
        program, worker_script, GetCurrentProcessId(), worker_folder))) return;
    STARTUPINFOW startup = {};
    PROCESS_INFORMATION process = {};
    startup.cb = sizeof(startup);
    if (CreateProcessW(program, command, nullptr, nullptr, FALSE, CREATE_NO_WINDOW,
        nullptr, nullptr, &startup, &process))
    {
        worker_process = process.hProcess;
        CloseHandle(process.hThread);
    }
    else report_error(GetLastError());
}

extern "C" void smile_native_worker_start(const char* script, const WCHAR* data_folder)
{
    if (!script || !data_folder || worker_script[0]) return;
    if (!MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, script, -1, worker_script, 2048)) return;
    if (FAILED(StringCchCopyW(worker_folder, 2048, data_folder)) ||
        FAILED(StringCchPrintfW(worker_mutex, 96, L"Local\\SmileNativeWorker-%lu", GetCurrentProcessId())))
    {
        worker_script[0] = 0;
        return;
    }
    smile_native_worker_tick();
}
