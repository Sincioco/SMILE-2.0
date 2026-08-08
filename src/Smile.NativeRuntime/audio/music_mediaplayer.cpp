#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <new>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Media.Core.h>
#include <winrt/Windows.Media.Playback.h>
#include <winrt/Windows.Storage.h>
#include "asset_path.h"
#include "audio_focus.h"
#include "audio_focus_state.h"
#include "music_mediaplayer.h"

using winrt::Windows::Media::Core::MediaSource;
using winrt::Windows::Media::Playback::MediaPlayer;
using winrt::Windows::Storage::StorageFile;

namespace
{
    struct SmileMusicState
    {
        MediaPlayer player{ nullptr };
        MediaSource source{ nullptr };
        bool owns_apartment = false;
    };

    SmileMusicState* music_state;
    long long requested_volume = 100;

    void report_failure(wchar_t const* message) noexcept
    {
        OutputDebugStringW(L"SMILE music: ");
        OutputDebugStringW(message != nullptr ? message : L"unknown Windows MediaPlayer failure");
        OutputDebugStringW(L"\r\n");
    }

    void report_current_exception() noexcept
    {
        try
        {
            throw;
        }
        catch (winrt::hresult_error const& error)
        {
            report_failure(error.message().c_str());
        }
        catch (...)
        {
            report_failure(L"unexpected native exception");
        }
    }

    void apply_volume(long long active) noexcept
    {
        if (music_state == nullptr || music_state->player == nullptr)
            return;
        try
        {
            music_state->player.Volume(smile_audio_effective_volume(active, requested_volume));
        }
        catch (...)
        {
            report_current_exception();
        }
    }

    void on_activation_changed(long long active)
    {
        apply_volume(active);
    }

    void clear_source() noexcept
    {
        if (music_state == nullptr || music_state->player == nullptr)
            return;
        try
        {
            music_state->player.Pause();
        }
        catch (...)
        {
            report_current_exception();
        }
        try
        {
            music_state->player.Source(nullptr);
            music_state->player.IsLoopingEnabled(false);
        }
        catch (...)
        {
            report_current_exception();
        }
        if (music_state->source != nullptr)
        {
            try
            {
                music_state->source.Close();
            }
            catch (...)
            {
                report_current_exception();
            }
            music_state->source = nullptr;
        }
    }

    bool ensure_player() noexcept
    {
        if (music_state != nullptr)
            return true;

        void* state_memory = HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(SmileMusicState));
        SmileMusicState* candidate = state_memory != nullptr
            ? ::new (state_memory) SmileMusicState()
            : nullptr;
        if (candidate == nullptr)
        {
            report_failure(L"could not allocate MediaPlayer state");
            return false;
        }

        try
        {
            try
            {
                winrt::init_apartment(winrt::apartment_type::multi_threaded);
                candidate->owns_apartment = true;
            }
            catch (winrt::hresult_error const& error)
            {
                if (error.code() != RPC_E_CHANGED_MODE)
                    throw;
            }

            candidate->player = MediaPlayer();
            music_state = candidate;
            smile_audio_register_music_activation_callback(on_activation_changed);
            apply_volume(smile_audio_is_active());
            return true;
        }
        catch (...)
        {
            report_current_exception();
            if (candidate->owns_apartment)
                winrt::uninit_apartment();
            candidate->~SmileMusicState();
            HeapFree(GetProcessHeap(), 0, candidate);
            return false;
        }
    }
}

extern "C" void smile_music_play(const char* path, long long length, long long loop)
{
    wchar_t resolved_path[2048];
    if (!smile_resolve_asset_path_utf8(path, length, resolved_path,
        static_cast<int>(sizeof(resolved_path) / sizeof(resolved_path[0]))))
        return;
    if (!ensure_player())
        return;

    try
    {
        clear_source();
        StorageFile file = StorageFile::GetFileFromPathAsync(resolved_path).get();
        MediaSource source = MediaSource::CreateFromStorageFile(file);
        music_state->source = source;
        music_state->player.IsLoopingEnabled(loop != 0);
        apply_volume(smile_audio_is_active());
        music_state->player.Source(source);
        music_state->player.Play();
    }
    catch (...)
    {
        report_current_exception();
        clear_source();
    }
}

extern "C" void smile_music_pause(void)
{
    if (music_state == nullptr || music_state->player == nullptr || music_state->source == nullptr)
        return;
    try
    {
        music_state->player.Pause();
    }
    catch (...)
    {
        report_current_exception();
    }
}

extern "C" void smile_music_resume(void)
{
    if (music_state == nullptr || music_state->player == nullptr || music_state->source == nullptr)
        return;
    try
    {
        music_state->player.Play();
    }
    catch (...)
    {
        report_current_exception();
    }
}

extern "C" void smile_music_stop(void)
{
    clear_source();
}

extern "C" void smile_music_set_volume(long long volume_percent)
{
    requested_volume = volume_percent < 0 ? 0 : (volume_percent > 100 ? 100 : volume_percent);
    apply_volume(smile_audio_is_active());
}

extern "C" void smile_music_shutdown(void)
{
    SmileMusicState* state = music_state;
    if (state == nullptr)
        return;

    smile_audio_register_music_activation_callback(nullptr);
    clear_source();
    if (state->player != nullptr)
    {
        try
        {
            state->player.Close();
        }
        catch (...)
        {
            report_current_exception();
        }
        state->player = nullptr;
    }
    music_state = nullptr;
    bool owns_apartment = state->owns_apartment;
    state->~SmileMusicState();
    HeapFree(GetProcessHeap(), 0, state);
    if (owns_apartment)
        winrt::uninit_apartment();
}
