#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <xaudio2.h>
#include <stdint.h>
#include "sfx_channels.h"

struct SmileWavCacheEntry
{
    WCHAR* path;
    WAVEFORMATEX* format;
    unsigned char* audio;
    DWORD audio_bytes;
    SmileWavCacheEntry* next;
};

static SRWLOCK smile_sfx_lock = SRWLOCK_INIT;
static IXAudio2* smile_sfx_engine;
static IXAudio2MasteringVoice* smile_sfx_master;
static IXAudio2SourceVoice* smile_sfx_voices[16];
static SmileWavCacheEntry* smile_sfx_cache;
static volatile LONG64 smile_sfx_decodes;
static volatile LONG64 smile_sfx_cache_hits;

static DWORD smile_u32(const unsigned char* value)
{
    return (DWORD)value[0] | ((DWORD)value[1] << 8) | ((DWORD)value[2] << 16) | ((DWORD)value[3] << 24);
}

static int smile_fourcc(const unsigned char* value, const char* text)
{
    return value[0] == (unsigned char)text[0] && value[1] == (unsigned char)text[1] &&
        value[2] == (unsigned char)text[2] && value[3] == (unsigned char)text[3];
}

static WCHAR* smile_sfx_copy_path(const WCHAR* path)
{
    SIZE_T bytes = ((SIZE_T)lstrlenW(path) + 1) * sizeof(WCHAR);
    WCHAR* copy = static_cast<WCHAR*>(HeapAlloc(GetProcessHeap(), 0, bytes));
    if (copy != 0) CopyMemory(copy, path, bytes);
    return copy;
}

static SmileWavCacheEntry* smile_sfx_decode(const WCHAR* path)
{
    HANDLE file = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
    LARGE_INTEGER length;
    unsigned char* bytes = 0;
    DWORD read = 0;
    DWORD offset;
    const unsigned char* format_bytes = 0;
    DWORD format_length = 0;
    const unsigned char* audio_bytes = 0;
    DWORD audio_length = 0;
    SmileWavCacheEntry* entry = 0;
    if (file == INVALID_HANDLE_VALUE || !GetFileSizeEx(file, &length) || length.QuadPart < 12 ||
        length.QuadPart > 64 * 1024 * 1024) goto done;
    bytes = static_cast<unsigned char*>(HeapAlloc(GetProcessHeap(), 0, (SIZE_T)length.QuadPart));
    if (bytes == 0 || !ReadFile(file, bytes, (DWORD)length.QuadPart, &read, 0) || read != (DWORD)length.QuadPart) goto done;
    if (!smile_fourcc(bytes, "RIFF") || !smile_fourcc(bytes + 8, "WAVE")) goto done;
    offset = 12;
    while (offset <= read - 8)
    {
        DWORD chunk = smile_u32(bytes + offset + 4);
        DWORD data = offset + 8;
        if (chunk > read - data) goto done;
        if (smile_fourcc(bytes + offset, "fmt ")) { format_bytes = bytes + data; format_length = chunk; }
        else if (smile_fourcc(bytes + offset, "data")) { audio_bytes = bytes + data; audio_length = chunk; }
        offset = data + chunk + (chunk & 1);
    }
    if (format_bytes == 0 || format_length < 16 || audio_bytes == 0 || audio_length == 0) goto done;
    entry = static_cast<SmileWavCacheEntry*>(HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(*entry)));
    if (entry == 0) goto done;
    entry->path = smile_sfx_copy_path(path);
    entry->format = static_cast<WAVEFORMATEX*>(HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY,
        format_length < sizeof(WAVEFORMATEX) ? sizeof(WAVEFORMATEX) : format_length));
    entry->audio = static_cast<unsigned char*>(HeapAlloc(GetProcessHeap(), 0, audio_length));
    if (entry->path == 0 || entry->format == 0 || entry->audio == 0) goto failed;
    CopyMemory(entry->format, format_bytes, format_length);
    if (format_length == 16) entry->format->cbSize = 0;
    CopyMemory(entry->audio, audio_bytes, audio_length);
    entry->audio_bytes = audio_length;
    goto done;

failed:
    if (entry->path != 0) HeapFree(GetProcessHeap(), 0, entry->path);
    if (entry->format != 0) HeapFree(GetProcessHeap(), 0, entry->format);
    if (entry->audio != 0) HeapFree(GetProcessHeap(), 0, entry->audio);
    HeapFree(GetProcessHeap(), 0, entry);
    entry = 0;
done:
    if (bytes != 0) HeapFree(GetProcessHeap(), 0, bytes);
    if (file != INVALID_HANDLE_VALUE) CloseHandle(file);
    return entry;
}

static int smile_sfx_initialize(void)
{
    if (smile_sfx_engine != 0) return 1;
    if (FAILED(XAudio2Create(&smile_sfx_engine, 0, XAUDIO2_DEFAULT_PROCESSOR))) return 0;
    if (FAILED(smile_sfx_engine->CreateMasteringVoice(&smile_sfx_master)))
    {
        smile_sfx_engine->Release();
        smile_sfx_engine = 0;
        return 0;
    }
    return 1;
}

static SmileWavCacheEntry* smile_sfx_find_or_decode(const WCHAR* path)
{
    SmileWavCacheEntry* entry;
    for (entry = smile_sfx_cache; entry != 0; entry = entry->next)
    {
        if (lstrcmpiW(entry->path, path) != 0) continue;
        InterlockedIncrement64(&smile_sfx_cache_hits);
        return entry;
    }
    entry = smile_sfx_decode(path);
    if (entry != 0)
    {
        entry->next = smile_sfx_cache;
        smile_sfx_cache = entry;
        InterlockedIncrement64(&smile_sfx_decodes);
    }
    return entry;
}

extern "C" int smile_sfx_preload(const WCHAR* path)
{
    int loaded;
    if (path == 0 || path[0] == 0) return 0;
    AcquireSRWLockExclusive(&smile_sfx_lock);
    loaded = smile_sfx_find_or_decode(path) != 0;
    ReleaseSRWLockExclusive(&smile_sfx_lock);
    return loaded;
}

extern "C" int smile_sfx_play(const WCHAR* path, int channel)
{
    SmileWavCacheEntry* entry;
    XAUDIO2_BUFFER buffer;
    HRESULT result;
    if (path == 0 || channel < 0 || channel >= 16) return 0;
    AcquireSRWLockExclusive(&smile_sfx_lock);
    if (!smile_sfx_initialize()) { ReleaseSRWLockExclusive(&smile_sfx_lock); return 0; }
    entry = smile_sfx_find_or_decode(path);
    if (entry == 0) { ReleaseSRWLockExclusive(&smile_sfx_lock); return 0; }
    if (smile_sfx_voices[channel] != 0)
    {
        smile_sfx_voices[channel]->Stop(0);
        smile_sfx_voices[channel]->DestroyVoice();
        smile_sfx_voices[channel] = 0;
    }
    result = smile_sfx_engine->CreateSourceVoice(&smile_sfx_voices[channel], entry->format);
    ZeroMemory(&buffer, sizeof(buffer));
    buffer.AudioBytes = entry->audio_bytes;
    buffer.pAudioData = entry->audio;
    buffer.Flags = XAUDIO2_END_OF_STREAM;
    if (SUCCEEDED(result)) result = smile_sfx_voices[channel]->SubmitSourceBuffer(&buffer);
    if (SUCCEEDED(result)) result = smile_sfx_voices[channel]->Start(0);
    if (FAILED(result) && smile_sfx_voices[channel] != 0)
    {
        smile_sfx_voices[channel]->DestroyVoice();
        smile_sfx_voices[channel] = 0;
    }
    ReleaseSRWLockExclusive(&smile_sfx_lock);
    return SUCCEEDED(result);
}

extern "C" void smile_sfx_stop(int channel)
{
    if (channel < 0 || channel >= 16) return;
    AcquireSRWLockExclusive(&smile_sfx_lock);
    if (smile_sfx_voices[channel] != 0)
    {
        smile_sfx_voices[channel]->Stop(0);
        smile_sfx_voices[channel]->DestroyVoice();
        smile_sfx_voices[channel] = 0;
    }
    ReleaseSRWLockExclusive(&smile_sfx_lock);
}

extern "C" void smile_sfx_stop_all(void)
{
    int channel;
    AcquireSRWLockExclusive(&smile_sfx_lock);
    for (channel = 0; channel < 16; ++channel)
    {
        if (smile_sfx_voices[channel] == 0) continue;
        smile_sfx_voices[channel]->Stop(0);
        smile_sfx_voices[channel]->DestroyVoice();
        smile_sfx_voices[channel] = 0;
    }
    ReleaseSRWLockExclusive(&smile_sfx_lock);
}

extern "C" int smile_sfx_active_count(void)
{
    int channel;
    int count = 0;
    AcquireSRWLockShared(&smile_sfx_lock);
    for (channel = 0; channel < 16; ++channel) if (smile_sfx_voices[channel] != 0) ++count;
    ReleaseSRWLockShared(&smile_sfx_lock);
    return count;
}

extern "C" int smile_sfx_cache_count(void)
{
    int count = 0;
    AcquireSRWLockShared(&smile_sfx_lock);
    for (SmileWavCacheEntry* entry = smile_sfx_cache; entry != 0; entry = entry->next) ++count;
    ReleaseSRWLockShared(&smile_sfx_lock);
    return count;
}

extern "C" long long smile_sfx_decode_count(void) { return smile_sfx_decodes; }
extern "C" long long smile_sfx_cache_hit_count(void) { return smile_sfx_cache_hits; }

extern "C" void smile_sfx_shutdown(void)
{
    SmileWavCacheEntry* entry;
    SmileWavCacheEntry* next;
    smile_sfx_stop_all();
    AcquireSRWLockExclusive(&smile_sfx_lock);
    if (smile_sfx_master != 0) smile_sfx_master->DestroyVoice();
    smile_sfx_master = 0;
    if (smile_sfx_engine != 0) smile_sfx_engine->Release();
    smile_sfx_engine = 0;
    for (entry = smile_sfx_cache; entry != 0; entry = next)
    {
        next = entry->next;
        HeapFree(GetProcessHeap(), 0, entry->path);
        HeapFree(GetProcessHeap(), 0, entry->format);
        HeapFree(GetProcessHeap(), 0, entry->audio);
        HeapFree(GetProcessHeap(), 0, entry);
    }
    smile_sfx_cache = 0;
    ReleaseSRWLockExclusive(&smile_sfx_lock);
}
