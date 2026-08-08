#pragma once

#ifdef __cplusplus
extern "C" {
#endif

void smile_music_play(const char* path, long long length, long long loop);
void smile_music_pause(void);
void smile_music_resume(void);
void smile_music_stop(void);
void smile_music_set_volume(long long volume_percent);
void smile_music_shutdown(void);

#ifdef __cplusplus
}
#endif
