#pragma once

#ifdef __cplusplus
extern "C" {
#endif

typedef void (*SmileMusicActivationCallback)(long long active);

void smile_audio_register_music_activation_callback(SmileMusicActivationCallback callback);
long long smile_audio_is_active(void);

#ifdef __cplusplus
}
#endif
