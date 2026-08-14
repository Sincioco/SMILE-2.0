#ifndef SMILE_SFX_CHANNELS_H
#define SMILE_SFX_CHANNELS_H

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#ifdef __cplusplus
extern "C" {
#endif

int smile_sfx_play(const WCHAR* path, int channel);
void smile_sfx_stop(int channel);
void smile_sfx_stop_all(void);
void smile_sfx_shutdown(void);
int smile_sfx_active_count(void);
int smile_sfx_preload(const WCHAR* path);
int smile_sfx_cache_count(void);
long long smile_sfx_decode_count(void);
long long smile_sfx_cache_hit_count(void);
long long smile_sfx_completion_count(void);

#ifdef __cplusplus
}
#endif
#endif
