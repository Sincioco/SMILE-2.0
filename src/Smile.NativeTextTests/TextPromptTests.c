#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <string.h>
#include <stdio.h>

void* smile_text_from_code(long long);
long long smile_text_code_at(void*, long long);
void* smile_text_from_utf8(const char*, long long);
const char* smile_text_utf8(void*);
long long smile_text_byte_length(void*);
long long smile_text_live_count(void);
void smile_text_release(void*);
void* smile_text_prompt_window(HWND, void*, void*, void*);

static HWND prompt_owner;
static int prompt_accept;
static int prompt_completed;

// Exercise only fixture-owned windows. Prevent this fixture thread from taking
// activation/focus away from the user's applications; no mouse/key synthesis.
static LRESULT CALLBACK prevent_activation(int code, WPARAM wp, LPARAM lp)
{
    if (code == HCBT_ACTIVATE || code == HCBT_SETFOCUS) return 1;
    return CallNextHookEx(0, code, wp, lp);
}

static void CALLBACK complete_prompt(HWND window, UINT message, UINT_PTR timer, DWORD time)
{
    HWND dialog = FindWindowW(L"SMILE20TextPrompt", L"Fixture");
    (void)window; (void)message; (void)time;
    if (!dialog || GetWindow(dialog, GW_OWNER) != prompt_owner) return;
    SetWindowTextW(GetDlgItem(dialog, 101), L"Neris \x57CE\x93AE \xD83C\xDF33");
    prompt_completed = 1;
    KillTimer(0, timer);
    SendMessageW(dialog, WM_COMMAND, prompt_accept ? IDOK : IDCANCEL, 0);
}

int test_text_prompt(void)
{
    int failures = 0;
    long long before = smile_text_live_count();
    void* result = smile_text_prompt_window(0, smile_text_from_utf8("Fixture", 7),
        smile_text_from_utf8("Name", 4), smile_text_from_utf8("Neris", 5));
    if (result || smile_text_live_count() != before) failures++;
    const long long scalars[] = {0, 127, 128, 2047, 2048, 0xd7ff, 0xe000, 65535, 65536, 0x1f333, 0x10ffff};
    for (int index = 0; index < (int)(sizeof(scalars) / sizeof(scalars[0])); index++)
        if (smile_text_code_at(smile_text_from_code(scalars[index]), 0) != scalars[index]) failures++;
    if (smile_text_from_code(-1) || smile_text_from_code(0xd800) ||
        smile_text_from_code(0xdfff) || smile_text_from_code(0x110000)) failures++;
    prompt_owner = CreateWindowExW(0, L"STATIC", L"SMILE Text Prompt Fixture",
        WS_OVERLAPPED, 0, 0, 600, 300, 0, 0, GetModuleHandleW(0), 0);
    HHOOK hook = SetWindowsHookExW(WH_CBT, prevent_activation, 0, GetCurrentThreadId());
    if (!prompt_owner || !hook) {
        if (prompt_owner) DestroyWindow(prompt_owner);
        if (hook) UnhookWindowsHookEx(hook);
        return 1;
    }
    for (prompt_accept = 0; prompt_accept <= 1; prompt_accept++) {
        const char* expected = "Neris \xE5\x9F\x8E\xE9\x8E\xAE \xF0\x9F\x8C\xB3";
        prompt_completed = 0;
        UINT_PTR timer = SetTimer(0, 0, 10, complete_prompt);
        if (!timer) { failures++; break; }
        result = smile_text_prompt_window(prompt_owner, smile_text_from_utf8("Fixture", 7),
            smile_text_from_utf8("Name", 4), smile_text_from_utf8("Neris", 5));
        KillTimer(0, timer);
        if (!prompt_completed || !IsWindowEnabled(prompt_owner)) failures++;
        if (prompt_accept) {
            if (smile_text_byte_length(result) != (long long)strlen(expected) ||
                memcmp(smile_text_utf8(result), expected, strlen(expected)) != 0) failures++;
        } else if (smile_text_byte_length(result) != 0) failures++;
        smile_text_release(result);
    }
    DestroyWindow(prompt_owner);
    UnhookWindowsHookEx(hook);
    if (smile_text_live_count() != before) failures++;
    if (failures) fprintf(stderr, "FAIL: %d native text prompt checks.\n", failures);
    else printf("PASS Native text prompt accept, cancel, Unicode, ownership and resource cleanup.\n");
    return failures;
}
