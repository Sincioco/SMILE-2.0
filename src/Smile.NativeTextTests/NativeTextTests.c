#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>

typedef struct StaticText
{
    volatile LONG64 references;
    long long length;
    unsigned char bytes[16];
} StaticText;

void* smile_text_retain(void* value);
void smile_text_release(void* value);
void smile_text_move_assign(void** target, void* owned_value);
void smile_text_clear(void** target);
void* smile_text_concat(void* owned_left, void* owned_right);
long long smile_text_equal(void* owned_left, void* owned_right);
long long smile_text_allocation_count(void);
long long smile_text_free_count(void);
long long smile_text_live_count(void);

static StaticText x_text = { -1, 1, { 'X', 0 } };
static StaticText emoji_text = { -1, 4, { 0xf0, 0x9f, 0x98, 0x80, 0 } };
static StaticText x_emoji_text = { -1, 5, { 'X', 0xf0, 0x9f, 0x98, 0x80, 0 } };
static int failures;
static int checks;

static void check(int condition, const char* message)
{
    checks++;
    if (!condition)
    {
        failures++;
        fprintf(stderr, "FAIL: %s\n", message);
    }
}

int main(void)
{
    long long initial_allocations = smile_text_allocation_count();
    long long initial_frees = smile_text_free_count();
    void* value = 0;
    void* replacement;
    void* owned;

    check(smile_text_live_count() == 0, "TEXT runtime starts with zero dynamic objects");
    value = smile_text_concat(smile_text_retain(&x_text), smile_text_retain(&emoji_text));
    check(value != 0, "concat allocates a dynamic TEXT value");
    check(smile_text_allocation_count() == initial_allocations + 1, "allocation counter increments");
    check(smile_text_live_count() == 1, "concat produces one live object");
    check(smile_text_equal(smile_text_retain(value), smile_text_retain(&x_emoji_text)) != 0,
        "Unicode UTF-8 bytes compare exactly");

    owned = smile_text_retain(value);
    smile_text_move_assign(&value, owned);
    check(smile_text_live_count() == 1, "self-assignment retain/move pattern preserves one owner");
    check(smile_text_concat(0, 0) == 0, "empty concat remains the zero TEXT handle");
    smile_text_clear(&value);
    check(value == 0 && smile_text_live_count() == 0, "clear releases a dynamic value and zeros its slot");
    smile_text_clear(&value);
    check(smile_text_live_count() == 0, "clear of zero is idempotent");

    value = smile_text_concat(smile_text_retain(&x_text), smile_text_retain(&x_text));
    replacement = smile_text_concat(smile_text_retain(&emoji_text), smile_text_retain(&x_text));
    check(smile_text_live_count() == 2, "two concatenations produce two live objects");
    smile_text_move_assign(&value, replacement);
    check(smile_text_live_count() == 1, "move assignment releases the replaced owner exactly once");
    smile_text_clear(&value);
    check(smile_text_live_count() == 0, "final clear returns the live count to zero");
    check(smile_text_allocation_count() - initial_allocations ==
        smile_text_free_count() - initial_frees, "dynamic allocation and free counters balance");

    if (failures != 0)
    {
        fprintf(stderr, "%d native TEXT runtime check(s) failed.\n", failures);
        return 1;
    }
    printf("%d native TEXT runtime checks passed.\n", checks);
    return 0;
}
