#pragma once
#include <windows.h>

/* Saved Games is shared by ordinary and packaged-parent desktop launches. */
int smile_storage_root(WCHAR* path, int capacity);
int smile_storage_import_legacy(const WCHAR* path);
