#include "audio_focus_state.h"

void smile_audio_focus_initialize(SmileAudioFocusState* state)
{
    if (state == 0)
        return;
    state->app_active = 1;
    state->window_active = 1;
    state->minimized = 0;
    state->effective_active = 1;
}

int smile_audio_focus_update(SmileAudioFocusState* state)
{
    int active;
    if (state == 0)
        return 0;
    active = state->app_active && state->window_active && !state->minimized;
    if (active == state->effective_active)
        return 0;
    state->effective_active = active;
    return active ? 1 : -1;
}

long long smile_audio_focus_accepts_sound(const SmileAudioFocusState* state)
{
    return state != 0 && state->effective_active ? 1 : 0;
}

double smile_audio_effective_volume(long long active, long long requested_volume)
{
    if (!active)
        return 0.0;
    if (requested_volume < 0)
        requested_volume = 0;
    if (requested_volume > 100)
        requested_volume = 100;
    return (double)requested_volume / 100.0;
}
