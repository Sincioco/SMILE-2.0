/* Focused window-procedure/queue test; no OS window or injected desktop input.
   Include the real runtime so release-before-poll behavior is tested directly. */
#define UNICODE
#define _UNICODE
#include <stdio.h>
#include "../Smile.NativeRuntime/runtime.c"

#define CHECK(test) do { if (!(test)) { printf("Key event check failed: %s\n", #test); return 1; } } while (0)

int main(void)
{
    smile_closed = 1; /* Select the window queue without creating a window. */
    smile_window_proc(0, WM_KEYDOWN, VK_CONTROL, 0);
    smile_window_proc(0, WM_KEYDOWN, VK_LEFT, 0);
    smile_window_proc(0, WM_KEYUP, VK_LEFT, 0);
    smile_window_proc(0, WM_KEYUP, VK_CONTROL, 0);
    smile_window_proc(0, WM_KEYDOWN, VK_RIGHT, 0);
    smile_window_proc(0, WM_KEYUP, VK_RIGHT, 0);
    CHECK(!smile_key_held(SMILE_KEY_CONTROL));
    CHECK(smile_get_key() == SMILE_KEY_CONTROL); /* Preserve native queue compatibility. */
    CHECK(smile_get_key() == SMILE_KEY_LEFT);
    CHECK(smile_key_event_held(SMILE_KEY_CONTROL));
    CHECK(smile_key_event_held(SMILE_KEY_LEFT));
    CHECK(!smile_key_event_held(SMILE_KEY_OTHER));
    CHECK(!smile_key_event_held(-1));
    CHECK(!smile_key_event_held(1000));
    CHECK(smile_get_key() == SMILE_KEY_RIGHT);
    CHECK(!smile_key_event_held(SMILE_KEY_CONTROL));
    CHECK(smile_key_event_held(SMILE_KEY_RIGHT));
    CHECK(smile_get_key() == SMILE_KEY_NONE);
    CHECK(!smile_key_event_held(SMILE_KEY_RIGHT));
    smile_window_proc(0, WM_KEYDOWN, 'W', 0);
    CHECK(smile_get_key() == SMILE_KEY_W);
    CHECK(smile_key_event_held(SMILE_KEY_W));
    smile_window_proc(0, WM_KEYDOWN, 'A', 0);
    smile_window_proc(0, WM_KILLFOCUS, 0, 0);
    CHECK(!smile_key_event_held(SMILE_KEY_W));
    CHECK(!smile_key_held(SMILE_KEY_W));
    CHECK(smile_get_key() == SMILE_KEY_NONE);
    puts("Native queued key snapshots passed");
    return 0;
}
