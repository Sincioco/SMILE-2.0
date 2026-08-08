#define WIN32_LEAN_AND_MEAN
#include <windows.h>

static HANDLE smile_output(void)
{
    return GetStdHandle(STD_OUTPUT_HANDLE);
}

void smile_print_text(const char* text, long long length)
{
    DWORD written;
    if (text == 0 || length <= 0)
        return;
    WriteFile(smile_output(), text, (DWORD)length, &written, 0);
}

void smile_print_number(long long value)
{
    char buffer[32];
    char* current = buffer + sizeof(buffer);
    unsigned long long magnitude;

    if (value < 0)
        magnitude = (unsigned long long)(-(value + 1)) + 1;
    else
        magnitude = (unsigned long long)value;

    do
    {
        *--current = (char)('0' + (magnitude % 10));
        magnitude /= 10;
    }
    while (magnitude != 0);

    if (value < 0)
        *--current = '-';

    smile_print_text(current, (long long)((buffer + sizeof(buffer)) - current));
}

void smile_print_boolean(long long value)
{
    static const char true_text[] = "TRUE";
    static const char false_text[] = "FALSE";
    if (value != 0)
        smile_print_text(true_text, 4);
    else
        smile_print_text(false_text, 5);
}

void smile_print_newline(void)
{
    static const char newline[] = "\r\n";
    smile_print_text(newline, 2);
}

static long long smile_map_key(WCHAR character, WORD virtual_key)
{
    if (character == L'w' || character == L'W' || virtual_key == 'W')
        return 1;
    if (character == L'a' || character == L'A' || virtual_key == 'A')
        return 2;
    if (character == L's' || character == L'S' || virtual_key == 'S')
        return 3;
    if (character == L'd' || character == L'D' || virtual_key == 'D')
        return 4;
    return 0;
}

long long smile_get_key(void)
{
    HANDLE input = GetStdHandle(STD_INPUT_HANDLE);
    DWORD file_type = GetFileType(input);

    if (file_type == FILE_TYPE_CHAR)
    {
        INPUT_RECORD record;
        DWORD available;
        DWORD read;
        while (PeekConsoleInputW(input, &record, 1, &available) && available != 0)
        {
            if (!ReadConsoleInputW(input, &record, 1, &read) || read == 0)
                break;
            if (record.EventType == KEY_EVENT && record.Event.KeyEvent.bKeyDown)
            {
                long long key = smile_map_key(
                    record.Event.KeyEvent.uChar.UnicodeChar,
                    record.Event.KeyEvent.wVirtualKeyCode);
                if (key != 0)
                    return key;
            }
        }
    }
    else if (file_type == FILE_TYPE_PIPE)
    {
        DWORD available;
        if (PeekNamedPipe(input, 0, 0, 0, &available, 0) && available != 0)
        {
            char character;
            DWORD read;
            if (ReadFile(input, &character, 1, &read, 0) && read == 1)
                return smile_map_key((WCHAR)(unsigned char)character, 0);
        }
    }

    if ((GetAsyncKeyState('W') & 0x8000) != 0)
        return 1;
    if ((GetAsyncKeyState('A') & 0x8000) != 0)
        return 2;
    if ((GetAsyncKeyState('S') & 0x8000) != 0)
        return 3;
    if ((GetAsyncKeyState('D') & 0x8000) != 0)
        return 4;
    return 0;
}

void smile_clear_screen(void)
{
    COORD origin;
    origin.X = 0;
    origin.Y = 0;
    SetConsoleCursorPosition(smile_output(), origin);
}

void smile_wait(long long milliseconds)
{
    if (milliseconds < 0)
        milliseconds = 0;
    if (milliseconds > 0xFFFFFFFFLL)
        milliseconds = 0xFFFFFFFFLL;
    Sleep((DWORD)milliseconds);
}

long long smile_random(long long minimum, long long maximum)
{
    static unsigned long long state;
    unsigned long long range;

    if (minimum > maximum)
        return minimum;

    if (state == 0)
        state = GetTickCount64() ^ (unsigned long long)(ULONG_PTR)&state;

    state = state * 6364136223846793005ULL + 1442695040888963407ULL;
    range = (unsigned long long)(maximum - minimum) + 1ULL;
    return minimum + (long long)(state % range);
}
