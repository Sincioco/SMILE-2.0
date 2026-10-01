// Bounded viewport images use the ordinary checked, atomic application save store.
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>
#include <d2d1_1.h>
#include <stdint.h>
#include "graphics_directx.h"

extern "C" {
void* smile_text_from_utf8(const char*, long long);
long long smile_save_data_checked(const long long*, long long, long long, void*);
long long smile_load_data_checked(void*, long long*, long long, long long*);
}

static const long long limit = 1024 * 1024;
static void put(long long* bytes, int offset, uint32_t value) {
    for (int i = 0; i < 4; ++i) bytes[offset + i] = (value >> (i * 8)) & 255;
}
static uint32_t get(const long long* bytes, int offset) {
    uint32_t value = 0;
    for (int i = 0; i < 4; ++i) value |= (uint32_t)bytes[offset + i] << (i * 8);
    return value;
}

extern "C" int smile_viewport_snapshot_save(void* value, const char* key, long long length)
{
    if (!value || !key || length < 1 || length > 1024) return 0;
    auto bitmap = static_cast<ID2D1Bitmap1*>(value);
    IDXGISurface* surface = nullptr;
    ID3D11Texture2D* source = nullptr;
    ID3D11Texture2D* staging = nullptr;
    if (FAILED(bitmap->GetSurface(&surface))) return 0;
    HRESULT hr = surface->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&source);
    surface->Release();
    if (FAILED(hr)) return 0;
    D3D11_TEXTURE2D_DESC desc;
    source->GetDesc(&desc);
    long long count = 16 + (long long)desc.Width * desc.Height * 4;
    if (desc.Width < 1 || desc.Height < 1 || desc.Width > 2048 || desc.Height > 2048 ||
        count > limit || desc.Format != DXGI_FORMAT_B8G8R8A8_UNORM || desc.SampleDesc.Count != 1) {
        source->Release();
        return 0;
    }
    desc.Usage = D3D11_USAGE_STAGING;
    desc.BindFlags = desc.MiscFlags = 0;
    desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
    auto device = static_cast<ID3D11Device*>(smile_graphics_directx_device());
    auto context = static_cast<ID3D11DeviceContext*>(smile_graphics_directx_context());
    hr = device->CreateTexture2D(&desc, nullptr, &staging);
    if (SUCCEEDED(hr)) context->CopyResource(staging, source);
    source->Release();
    if (FAILED(hr)) return 0;
    auto bytes = static_cast<long long*>(HeapAlloc(GetProcessHeap(), 0, (SIZE_T)count * 8));
    D3D11_MAPPED_SUBRESOURCE mapped = {};
    int success = 0;
    if (bytes && SUCCEEDED(context->Map(staging, 0, D3D11_MAP_READ, 0, &mapped))) {
        put(bytes, 0, 0x31505653); // SVP1: width, height, BGRA8 format, then pixels.
        put(bytes, 4, desc.Width);
        put(bytes, 8, desc.Height);
        put(bytes, 12, (uint32_t)desc.Format);
        for (UINT y = 0; y < desc.Height; ++y) {
            auto row = static_cast<const unsigned char*>(mapped.pData) + y * mapped.RowPitch;
            for (UINT x = 0; x < desc.Width * 4; ++x) bytes[16 + y * desc.Width * 4 + x] = row[x];
        }
        context->Unmap(staging, 0);
        success = smile_save_data_checked(bytes, count, count, smile_text_from_utf8(key, length)) == 0;
    }
    if (bytes) HeapFree(GetProcessHeap(), 0, bytes);
    staging->Release();
    return success;
}

extern "C" void* smile_viewport_snapshot_load(const char* key, long long length)
{
    if (!key || length < 1 || length > 1024 || !smile_graphics_directx_device()) return nullptr;
    auto bytes = static_cast<long long*>(HeapAlloc(GetProcessHeap(), 0, (SIZE_T)limit * 8));
    if (!bytes) return nullptr;
    long long count = 0;
    long long status = smile_load_data_checked(smile_text_from_utf8(key, length), bytes, limit, &count);
    void* result = nullptr;
    if (status == 0 && count >= 16 && get(bytes, 0) == 0x31505653 &&
        get(bytes, 12) == DXGI_FORMAT_B8G8R8A8_UNORM) {
        UINT width = get(bytes, 4), height = get(bytes, 8);
        if (width > 0 && height > 0 && width <= 2048 && height <= 2048 &&
            count == 16 + (long long)width * height * 4) {
            auto pixels = static_cast<unsigned char*>(HeapAlloc(GetProcessHeap(), 0, (SIZE_T)count - 16));
            if (pixels) {
                for (long long i = 16; i < count; ++i) pixels[i - 16] = (unsigned char)bytes[i];
                D3D11_TEXTURE2D_DESC desc = {};
                desc.Width = width; desc.Height = height;
                desc.MipLevels = desc.ArraySize = desc.SampleDesc.Count = 1;
                desc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
                desc.Usage = D3D11_USAGE_DEFAULT;
                desc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
                D3D11_SUBRESOURCE_DATA data = {pixels, width * 4, 0};
                ID3D11Texture2D* texture = nullptr;
                auto device = static_cast<ID3D11Device*>(smile_graphics_directx_device());
                if (SUCCEEDED(device->CreateTexture2D(&desc, &data, &texture))) {
                    result = smile_graphics_directx_snapshot(texture);
                    texture->Release();
                }
                HeapFree(GetProcessHeap(), 0, pixels);
            }
        }
    }
    HeapFree(GetProcessHeap(), 0, bytes);
    return result;
}
