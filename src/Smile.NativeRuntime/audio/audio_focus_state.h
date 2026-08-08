#pragma once

#ifdef __cplusplus
extern "C" {
#endif

typedef struct SmileAudioFocusState
{
    int app_active;
    int window_active;
    int minimized;
    int effective_active;
} SmileAudioFocusState;

void smile_audio_focus_initialize(SmileAudioFocusState* state);
int smile_audio_focus_update(SmileAudioFocusState* state);
long long smile_audio_focus_accepts_sound(const SmileAudioFocusState* state);
double smile_audio_effective_volume(long long active, long long requested_volume);

#ifdef __cplusplus
}
#endif
