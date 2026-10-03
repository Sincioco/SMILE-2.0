#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <strsafe.h>
#include <stdint.h>
#include <string.h>
#include <string>

DWORD smile_viewport_write_png(const WCHAR*, const unsigned char*, DWORD);

DWORD smile_data_bundle_transfer(bool, const char*, const char*, const WCHAR*, long long, volatile LONG*);

extern "C" {
const char* smile_text_utf8(void*);
long long smile_text_byte_length(void*);
void smile_text_release(void*);
void* smile_text_from_utf8(const char*, long long);
int smile_storage_data_path(const char*, long long, WCHAR*, int);
void smile_sha_bytes(const unsigned char*, SIZE_T, unsigned char*);
}

// One bounded transfer owned by this application. No other process, mailbox,
// heartbeat, current directory or launcher participates in data-file persistence.
static struct Transfer {
    WCHAR source[4096], destination[4096];
    char message[512];
    long long id;
    volatile LONG progress;
    volatile LONG status; // 0 pending, 1 successful, 2 failed; -1 unknown job.
    HANDLE thread;
    bool bundle, saving, png;
    char key[1025], records[264193];
} job;

static bool wide(void* text, WCHAR* output, int capacity)
{
    long long length = smile_text_byte_length(text);
    if (length <= 0 || length > 16380) return false;
    int count = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS,
        smile_text_utf8(text), static_cast<int>(length), output, capacity - 1);
    if (!count) return false;
    // Embedded NULs must not silently redirect a chosen path.
    for (int i = 0; i < count; ++i) if (!output[i]) return false;
    output[count] = 0;
    return true;
}

static void fail(DWORD error)
{
    WCHAR detail[256] = {};
    char utf8[384] = {};
    FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
        nullptr, error, 0, detail, 256, nullptr);
    WideCharToMultiByte(CP_UTF8, 0, detail, -1, utf8, 384, nullptr, nullptr);
    for (int i = 0; utf8[i]; ++i) if (utf8[i] == '\r' || utf8[i] == '\n') utf8[i] = ' ';
    StringCchPrintfA(job.message, 512, "File not saved/opened (Windows %lu): %s", error, utf8);
}

static bool envelope(const unsigned char* bytes, DWORD count)
{
    if (count < 44 || count > 1048620 || memcmp(bytes, "SMD4", 4) != 0) return false;
    uint32_t version, length;
    memcpy(&version, bytes + 4, 4);
    memcpy(&length, bytes + 8, 4);
    if (version != 1 || length != count - 44) return false;
    unsigned char digest[32];
    smile_sha_bytes(bytes + 44, length, digest);
    return memcmp(digest, bytes + 12, 32) == 0;
}

static DWORD WINAPI transfer(void*)
{
    HANDLE input = INVALID_HANDLE_VALUE, output = INVALID_HANDLE_VALUE;
    unsigned char* bytes = nullptr;
    WCHAR temporary[4200] = {}, backup[4200] = {};
    DWORD error = ERROR_SUCCESS, count = 0, written = 0;
    LARGE_INTEGER size = {};
    bool ownsTemporary = false;
    if (job.bundle) {
        error = smile_data_bundle_transfer(job.saving, job.key, job.records,
            job.saving ? job.destination : job.source, job.id, &job.progress);
        goto done;
    }
    input = CreateFileW(job.source, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE,
        nullptr, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, nullptr);
    if (input == INVALID_HANDLE_VALUE) { error = GetLastError(); goto done; }
    if (!GetFileSizeEx(input, &size)) { error = GetLastError(); goto done; }
    if (size.QuadPart < 44 || size.QuadPart > 1048620) { error = ERROR_INVALID_DATA; goto done; }
    bytes = static_cast<unsigned char*>(HeapAlloc(GetProcessHeap(), 0, static_cast<SIZE_T>(size.QuadPart)));
    if (!bytes) { error = ERROR_NOT_ENOUGH_MEMORY; goto done; }
    if (!ReadFile(input, bytes, static_cast<DWORD>(size.QuadPart), &count, nullptr))
        { error = GetLastError(); goto done; }
    if (count != size.QuadPart || !envelope(bytes, count)) { error = ERROR_CRC; goto done; }
    InterlockedExchange(&job.progress, 25); // Source checksum verified.
    CloseHandle(input); input = INVALID_HANDLE_VALUE;
    StringCchPrintfW(temporary, 4200, L"%ls.pending.%lu.%lld", job.destination, GetCurrentProcessId(), job.id);
    StringCchPrintfW(backup, 4200, L"%ls.bak", job.destination);
    output = CreateFileW(temporary, GENERIC_READ | GENERIC_WRITE, 0, nullptr, CREATE_NEW,
        FILE_ATTRIBUTE_NORMAL, nullptr);
    if (output == INVALID_HANDLE_VALUE) { error = GetLastError(); goto done; }
    ownsTemporary = true;
    if (job.png) {
        CloseHandle(output); output = INVALID_HANDLE_VALUE;
        error = smile_viewport_write_png(temporary, bytes + 44, count - 44);
        if (error) goto done;
        output = CreateFileW(temporary, GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, 0, nullptr);
        if (output == INVALID_HANDLE_VALUE || !FlushFileBuffers(output)) {
            error = GetLastError(); goto done;
        }
        CloseHandle(output); output = INVALID_HANDLE_VALUE;
        goto publish;
    }
    if (!WriteFile(output, bytes, count, &written, nullptr) || !FlushFileBuffers(output))
        { error = GetLastError(); goto done; }
    if (written != count) { error = ERROR_WRITE_FAULT; goto done; }
    InterlockedExchange(&job.progress, 70); // All bytes flushed.
    // Verify the durable temporary before replacing the chosen destination.
    SetFilePointer(output, 0, nullptr, FILE_BEGIN);
    if (!ReadFile(output, bytes, count, &written, nullptr)) { error = GetLastError(); goto done; }
    if (written != count || !envelope(bytes, written)) { error = ERROR_CRC; goto done; }
    InterlockedExchange(&job.progress, 95); // Written envelope verified.
    CloseHandle(output); output = INVALID_HANDLE_VALUE;
publish:
    if (GetFileAttributesW(job.destination) != INVALID_FILE_ATTRIBUTES)
    {
        if (!ReplaceFileW(job.destination, temporary, backup, 0, nullptr, nullptr))
            { error = GetLastError(); goto done; }
    }
    else if (!MoveFileExW(temporary, job.destination, MOVEFILE_WRITE_THROUGH))
        { error = GetLastError(); goto done; }
    ownsTemporary = false;
done:
    if (input != INVALID_HANDLE_VALUE) CloseHandle(input);
    if (output != INVALID_HANDLE_VALUE) CloseHandle(output);
    if (ownsTemporary) DeleteFileW(temporary);
    if (bytes) HeapFree(GetProcessHeap(), 0, bytes);
    if (job.png) DeleteFileW(job.source);
    if (error) fail(error);
    else { StringCchCopyA(job.message, 512, "File transfer complete."); InterlockedExchange(&job.progress, 100); }
    InterlockedExchange(&job.status, error ? 2 : 1);
    return 0;
}

static long long start(long long saving, void* key, void* path, void* records, bool bundle, bool png = false)
{
    WCHAR stored[2048], selected[4096];
    std::string keyCopy(smile_text_utf8(key), static_cast<size_t>(smile_text_byte_length(key)));
    std::string recordCopy;
    if (records) recordCopy.assign(smile_text_utf8(records), static_cast<size_t>(smile_text_byte_length(records)));
    bool valid = wide(path, selected, 4096) &&
        smile_storage_data_path(smile_text_utf8(key), smile_text_byte_length(key), stored, 2048);
    valid = valid && keyCopy.size() <= 1024 && recordCopy.size() <= 264192 &&
        keyCopy.find('\0') == std::string::npos && recordCopy.find('\0') == std::string::npos;
    smile_text_release(key);
    smile_text_release(path);
    if (records) smile_text_release(records);
    // A caller cannot replace a pending transaction, even when a network is slow.
    if (job.thread && WaitForSingleObject(job.thread, 0) == WAIT_TIMEOUT) return 0;
    if (job.thread) { CloseHandle(job.thread); job.thread = nullptr; }
    ++job.id;
    job.bundle = bundle;
    job.png = png;
    job.saving = saving != 0;
    InterlockedExchange(&job.progress, 0);
    if (!valid || !((selected[0] && selected[1] == L':' && selected[2] == L'\\') ||
        (selected[0] == L'\\' && selected[1] == L'\\')))
    {
        fail(ERROR_BAD_PATHNAME);
        InterlockedExchange(&job.status, 2);
        return job.id;
    }
    StringCchCopyA(job.key, 1025, keyCopy.c_str());
    StringCchCopyA(job.records, 264193, recordCopy.c_str());
    StringCchCopyW(job.source, 4096, saving ? stored : selected);
    StringCchCopyW(job.destination, 4096, saving ? selected : stored);
    StringCchCopyA(job.message, 512, "Saving/opening file...");
    InterlockedExchange(&job.status, 0);
    job.thread = CreateThread(nullptr, 0, transfer, nullptr, 0, nullptr);
    if (!job.thread) { fail(GetLastError()); InterlockedExchange(&job.status, 2); }
    return job.id;
}

extern "C" long long smile_data_png_start(const char* key, const char* path, long long length)
{
    return start(1, smile_text_from_utf8(key, lstrlenA(key)),
        smile_text_from_utf8(path, length), nullptr, false, true);
}

extern "C" int smile_data_file_busy()
{
    return job.thread && WaitForSingleObject(job.thread, 0) == WAIT_TIMEOUT;
}

extern "C" long long smile_data_file_start(long long saving, void* key, void* path)
{
    return start(saving, key, path, nullptr, false);
}

extern "C" long long smile_data_bundle_start(long long saving, void* key, void* path, void* records)
{
    return start(saving, key, path, records, true);
}

extern "C" long long smile_data_file_status(long long id)
{
    return id > 0 && id == job.id ? InterlockedCompareExchange(&job.status, 0, 0) : -1;
}

extern "C" long long smile_data_file_progress(long long id)
{
    return id == job.id ? InterlockedCompareExchange(&job.progress, 0, 0) : 0;
}

extern "C" void* smile_data_file_message(long long id)
{
    long long status = smile_data_file_status(id);
    const char* text = status < 0 ? "Unknown file operation." : status == 0 ? "Saving/opening file..." : job.message;
    return smile_text_from_utf8(text, lstrlenA(text));
}

extern "C" void* smile_local_timestamp(void)
{
    SYSTEMTIME now;
    char text[32];
    GetLocalTime(&now);
    StringCchPrintfA(text, 32, "%04u-%02u-%02u %02u%02u", now.wYear, now.wMonth,
        now.wDay, now.wHour, now.wMinute);
    return smile_text_from_utf8(text, lstrlenA(text));
}
