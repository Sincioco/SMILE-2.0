#pragma once
#include <windows.h>
#ifdef __cplusplus
extern "C" {
#endif
void smile_startup_begin(const char* title, const char* author, const char* build);
void smile_startup_attach(HWND owner);
void smile_startup_resume(HWND owner);
void smile_startup_status(const char* path, long long length);
void smile_startup_ready(void);
#ifdef __cplusplus
}
#endif
