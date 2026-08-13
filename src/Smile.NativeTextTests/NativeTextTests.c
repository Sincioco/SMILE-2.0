#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
#include "image_resource.h"
#include "sfx_channels.h"

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
    WCHAR character_path[MAX_PATH];
    WCHAR background_path[MAX_PATH];
    WCHAR pixel_path[MAX_PATH];
    WCHAR tone_one_path[MAX_PATH];
    WCHAR tone_two_path[MAX_PATH];
    SmileImageResource* first_image;
    SmileImageResource* second_image;
    SmileImageResource* background_image;
    SmileImageResource* pixel_image;
    long long initial_image_decodes = smile_image_resource_decode_count();
    long long initial_image_hits = smile_image_resource_cache_hit_count();
    long long initial_image_live = smile_image_resource_live_count();
    long long initial_sfx_decodes = smile_sfx_decode_count();
    long long initial_sfx_hits = smile_sfx_cache_hit_count();

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

    GetFullPathNameW(L"examples\\Phase4VisualSlice\\Assets\\CharacterSheet.png", MAX_PATH, character_path, 0);
    GetFullPathNameW(L"examples\\Phase4VisualSlice\\Assets\\Background.png", MAX_PATH, background_path, 0);
    GetFullPathNameW(L"examples\\Phase4VisualSlice\\Assets\\PixelProof.png", MAX_PATH, pixel_path, 0);
    first_image = smile_image_resource_load(character_path);
    second_image = smile_image_resource_load(character_path);
    check(first_image != 0 && second_image == first_image, "duplicate IMAGE paths share one cached resource");
    check(smile_image_resource_width(first_image) == 2048 && smile_image_resource_height(first_image) == 1024,
        "WIC preserves high-resolution sprite-sheet dimensions");
    check(smile_image_resource_pixels(first_image) != 0 && smile_image_resource_pixels(first_image)[3] == 0,
        "WIC preserves fully transparent PNG pixels");
    check(smile_image_resource_decode_count() == initial_image_decodes + 1 &&
        smile_image_resource_cache_hit_count() == initial_image_hits + 1,
        "IMAGE cache records one decode and one hit");
    smile_image_resource_release(first_image);
    check(smile_image_resource_width(second_image) == 2048 &&
        smile_image_resource_live_count() == initial_image_live + 1,
        "releasing one shared IMAGE handle preserves the other owner");
    smile_image_resource_release(second_image);
    check(smile_image_resource_live_count() == initial_image_live,
        "final IMAGE release evicts the cached resource");

    background_image = smile_image_resource_load(background_path);
    pixel_image = smile_image_resource_load(pixel_path);
    check(background_image != 0 && smile_image_resource_width(background_image) == 2304 &&
        smile_image_resource_height(background_image) == 1296,
        "WIC loads a non-power-of-two image larger than the logical window");
    check(pixel_image != 0 && smile_image_resource_width(pixel_image) == 37 &&
        smile_image_resource_height(pixel_image) == 53,
        "WIC loads arbitrary non-square image dimensions");
    smile_image_resource_release(background_image);
    smile_image_resource_release(pixel_image);
    check(smile_image_resource_live_count() == initial_image_live,
        "all focused IMAGE resources return to zero live owners");
    smile_image_resource_shutdown();

    GetFullPathNameW(L"examples\\Phase4VisualSlice\\Assets\\ToneOne.wav", MAX_PATH, tone_one_path, 0);
    GetFullPathNameW(L"examples\\Phase4VisualSlice\\Assets\\ToneTwo.wav", MAX_PATH, tone_two_path, 0);
    check(smile_sfx_preload(tone_one_path) && smile_sfx_preload(tone_one_path) && smile_sfx_preload(tone_two_path),
        "WAV cache accepts two original PCM fixtures");
    check(smile_sfx_decode_count() == initial_sfx_decodes + 2 &&
        smile_sfx_cache_hit_count() == initial_sfx_hits + 1 && smile_sfx_cache_count() == 2,
        "WAV cache records two decodes and one cache hit");
    check(smile_sfx_active_count() == 0, "preloading WAV data does not occupy any of the 16 playback channels");
    smile_sfx_stop(-1);
    smile_sfx_stop(16);
    check(smile_sfx_active_count() == 0, "out-of-range channel stop requests are harmless");
    smile_sfx_shutdown();

    if (failures != 0)
    {
        fprintf(stderr, "%d native TEXT runtime check(s) failed.\n", failures);
        return 1;
    }
    printf("%d native TEXT runtime checks passed.\n", checks);
    return 0;
}
