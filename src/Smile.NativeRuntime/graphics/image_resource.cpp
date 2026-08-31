#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <wincodec.h>
#include <d2d1_1.h>
#include <stdint.h>
#include "image_resource.h"

struct SmileImageResource
{
    volatile LONG64 references;
    WCHAR* path;
    UINT width;
    UINT height;
    UINT stride;
    unsigned char* pixels;
    unsigned char* straight_pixels;
    ID2D1Bitmap1* d2d_bitmap;
    ID2D1DeviceContext* d2d_owner;
    SmileImageResource* next;
};

static SRWLOCK smile_image_lock = SRWLOCK_INIT;
static SmileImageResource* smile_image_cache;
static volatile LONG64 smile_image_decodes;
static volatile LONG64 smile_image_cache_hits;
static volatile LONG64 smile_image_live;

template<typename T>
static void smile_image_release_com(T*& value)
{
    if (value != 0) value->Release();
    value = 0;
}

static WCHAR* smile_image_copy_path(const WCHAR* path)
{
    SIZE_T count = (SIZE_T)lstrlenW(path) + 1;
    WCHAR* copy = static_cast<WCHAR*>(HeapAlloc(GetProcessHeap(), 0, count * sizeof(WCHAR)));
    if (copy != 0) CopyMemory(copy, path, count * sizeof(WCHAR));
    return copy;
}

static void smile_image_destroy(SmileImageResource* image)
{
    if (image == 0) return;
    smile_image_release_com(image->d2d_bitmap);
    image->d2d_owner = 0;
    if (image->pixels != 0) HeapFree(GetProcessHeap(), 0, image->pixels);
    if (image->straight_pixels != 0) HeapFree(GetProcessHeap(), 0, image->straight_pixels);
    if (image->path != 0) HeapFree(GetProcessHeap(), 0, image->path);
    HeapFree(GetProcessHeap(), 0, image);
}

static SmileImageResource* smile_image_decode(const WCHAR* path)
{
    IWICBitmapDecoder* decoder = 0;
    IWICBitmapFrameDecode* frame = 0;
    IWICFormatConverter* converter = 0;
    IWICImagingFactory* factory = 0;
    SmileImageResource* image = 0;
    UINT width = 0;
    UINT height = 0;
    UINT stride;
    UINT bytes;
    HRESULT initialize_result = CoInitializeEx(0, COINIT_MULTITHREADED);
    HRESULT result = CoCreateInstance(CLSID_WICImagingFactory, 0, CLSCTX_INPROC_SERVER,
        IID_PPV_ARGS(&factory));
    if (FAILED(result)) goto done;
    result = factory->CreateDecoderFromFilename(path, 0, GENERIC_READ,
        WICDecodeMetadataCacheOnLoad, &decoder);
    if (SUCCEEDED(result)) result = decoder->GetFrame(0, &frame);
    if (SUCCEEDED(result)) result = frame->GetSize(&width, &height);
    if (width == 0 || height == 0 || width > UINT_MAX / 4 || height > UINT_MAX / (width * 4))
        result = E_INVALIDARG;
    stride = width * 4;
    bytes = stride * height;
    if (SUCCEEDED(result)) result = factory->CreateFormatConverter(&converter);
    if (SUCCEEDED(result)) result = converter->Initialize(frame, GUID_WICPixelFormat32bppBGRA,
        WICBitmapDitherTypeNone, 0, 0.0, WICBitmapPaletteTypeCustom);
    if (SUCCEEDED(result))
    {
        image = static_cast<SmileImageResource*>(HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(*image)));
        if (image == 0) result = E_OUTOFMEMORY;
    }
    if (SUCCEEDED(result))
    {
        image->pixels = static_cast<unsigned char*>(HeapAlloc(GetProcessHeap(), 0, bytes));
        image->straight_pixels = static_cast<unsigned char*>(HeapAlloc(GetProcessHeap(), 0, bytes));
        image->path = smile_image_copy_path(path);
        if (image->pixels == 0 || image->straight_pixels == 0 || image->path == 0)
            result = E_OUTOFMEMORY;
    }
    if (SUCCEEDED(result)) result = converter->CopyPixels(0, stride, bytes, image->straight_pixels);
    if (SUCCEEDED(result))
    {
        for (UINT offset = 0; offset < bytes; offset += 4)
        {
            unsigned int alpha = image->straight_pixels[offset + 3];
            image->pixels[offset] = (unsigned char)((image->straight_pixels[offset] * alpha + 127U) / 255U);
            image->pixels[offset + 1] = (unsigned char)((image->straight_pixels[offset + 1] * alpha + 127U) / 255U);
            image->pixels[offset + 2] = (unsigned char)((image->straight_pixels[offset + 2] * alpha + 127U) / 255U);
            image->pixels[offset + 3] = (unsigned char)alpha;
        }
    }
    if (SUCCEEDED(result))
    {
        image->references = 1;
        image->width = width;
        image->height = height;
        image->stride = stride;
    }
    else if (image != 0)
    {
        if (image->pixels != 0) HeapFree(GetProcessHeap(), 0, image->pixels);
        if (image->straight_pixels != 0) HeapFree(GetProcessHeap(), 0, image->straight_pixels);
        if (image->path != 0) HeapFree(GetProcessHeap(), 0, image->path);
        HeapFree(GetProcessHeap(), 0, image);
        image = 0;
    }
done:
    smile_image_release_com(converter);
    smile_image_release_com(frame);
    smile_image_release_com(decoder);
    smile_image_release_com(factory);
    if (SUCCEEDED(initialize_result)) CoUninitialize();
    return image;
}

extern "C" SmileImageResource* smile_image_resource_load(const WCHAR* path)
{
    SmileImageResource* current;
    SmileImageResource* decoded;
    if (path == 0 || path[0] == 0) return 0;
    AcquireSRWLockShared(&smile_image_lock);
    for (current = smile_image_cache; current != 0; current = current->next)
    {
        if (lstrcmpW(current->path, path) == 0)
        {
            InterlockedIncrement64(&current->references);
            InterlockedIncrement64(&smile_image_cache_hits);
            ReleaseSRWLockShared(&smile_image_lock);
            return current;
        }
    }
    ReleaseSRWLockShared(&smile_image_lock);
    decoded = smile_image_decode(path);
    if (decoded == 0) return 0;
    AcquireSRWLockExclusive(&smile_image_lock);
    for (current = smile_image_cache; current != 0; current = current->next)
    {
        if (lstrcmpW(current->path, path) == 0)
        {
            InterlockedIncrement64(&current->references);
            InterlockedIncrement64(&smile_image_cache_hits);
            ReleaseSRWLockExclusive(&smile_image_lock);
            smile_image_destroy(decoded);
            return current;
        }
    }
    InterlockedIncrement64(&smile_image_decodes);
    InterlockedIncrement64(&smile_image_live);
    decoded->next = smile_image_cache;
    smile_image_cache = decoded;
    ReleaseSRWLockExclusive(&smile_image_lock);
    return decoded;
}

extern "C" SmileImageResource* smile_image_resource_retain(SmileImageResource* image)
{
    if (image != 0)
    {
        AcquireSRWLockShared(&smile_image_lock);
        InterlockedIncrement64(&image->references);
        ReleaseSRWLockShared(&smile_image_lock);
    }
    return image;
}

extern "C" void smile_image_resource_release(SmileImageResource* image)
{
    SmileImageResource** link;
    if (image == 0) return;
    AcquireSRWLockExclusive(&smile_image_lock);
    if (InterlockedDecrement64(&image->references) != 0)
    {
        ReleaseSRWLockExclusive(&smile_image_lock);
        return;
    }
    link = &smile_image_cache;
    while (*link != 0 && *link != image) link = &(*link)->next;
    if (*link == image) *link = image->next;
    smile_image_destroy(image);
    InterlockedDecrement64(&smile_image_live);
    ReleaseSRWLockExclusive(&smile_image_lock);
}

extern "C" long long smile_image_resource_width(const SmileImageResource* image) { return image == 0 ? 0 : image->width; }
extern "C" long long smile_image_resource_height(const SmileImageResource* image) { return image == 0 ? 0 : image->height; }
extern "C" const unsigned char* smile_image_resource_pixels(const SmileImageResource* image) { return image == 0 ? 0 : image->pixels; }
extern "C" const unsigned char* smile_image_resource_straight_pixels(const SmileImageResource* image)
{
    return image == 0 ? 0 : image->straight_pixels;
}
extern "C" unsigned int smile_image_resource_stride(const SmileImageResource* image) { return image == 0 ? 0 : image->stride; }

extern "C" void* smile_image_resource_d2d_bitmap(SmileImageResource* image, void* context_value)
{
    ID2D1DeviceContext* context = static_cast<ID2D1DeviceContext*>(context_value);
    D2D1_BITMAP_PROPERTIES1 properties;
    if (image == 0 || context == 0) return 0;
    if (image->d2d_bitmap != 0 && image->d2d_owner == context) return image->d2d_bitmap;
    smile_image_release_com(image->d2d_bitmap);
    image->d2d_owner = 0;
    properties.pixelFormat = D2D1::PixelFormat(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE_PREMULTIPLIED);
    properties.dpiX = 96.0f;
    properties.dpiY = 96.0f;
    properties.bitmapOptions = D2D1_BITMAP_OPTIONS_NONE;
    properties.colorContext = 0;
    if (FAILED(context->CreateBitmap(D2D1::SizeU(image->width, image->height), image->pixels,
        image->stride, &properties, &image->d2d_bitmap))) return 0;
    image->d2d_owner = context;
    return image->d2d_bitmap;
}

extern "C" void smile_image_resource_release_backend_resources(void)
{
    AcquireSRWLockExclusive(&smile_image_lock);
    for (SmileImageResource* image = smile_image_cache; image != 0; image = image->next)
    {
        smile_image_release_com(image->d2d_bitmap);
        image->d2d_owner = 0;
    }
    ReleaseSRWLockExclusive(&smile_image_lock);
}

extern "C" void smile_image_resource_shutdown(void)
{
    smile_image_resource_release_backend_resources();
}

extern "C" long long smile_image_resource_decode_count(void) { return smile_image_decodes; }
extern "C" long long smile_image_resource_cache_hit_count(void) { return smile_image_cache_hits; }
extern "C" long long smile_image_resource_live_count(void) { return smile_image_live; }
extern "C" long long smile_image_resource_cache_count(void)
{
    long long count = 0;
    AcquireSRWLockShared(&smile_image_lock);
    for (SmileImageResource* image = smile_image_cache; image != 0; image = image->next) count++;
    ReleaseSRWLockShared(&smile_image_lock);
    return count;
}
extern "C" long long smile_image_resource_reference_count(void)
{
    long long count = 0;
    AcquireSRWLockShared(&smile_image_lock);
    for (SmileImageResource* image = smile_image_cache; image != 0; image = image->next) count += image->references;
    ReleaseSRWLockShared(&smile_image_lock);
    return count;
}
