#pragma once

#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

int smile_resolve_asset_path_utf8(const char* path, long long length,
    wchar_t* resolved_path, int capacity);

#ifdef __cplusplus
}
#endif
