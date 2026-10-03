// Windows PNG encoding runs on the existing data-file worker, never the UI thread.
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <wincodec.h>
#include <stdint.h>
#include <string.h>

DWORD smile_viewport_write_png(const WCHAR* path, const unsigned char* bytes, DWORD count)
{
    uint32_t width = 0, height = 0, format = 0;
    if (count < 16 || memcmp(bytes, "SVP1", 4)) return ERROR_INVALID_DATA;
    memcpy(&width, bytes + 4, 4);
    memcpy(&height, bytes + 8, 4);
    memcpy(&format, bytes + 12, 4);
    if (!width || !height || width > 2048 || height > 2048 || format != 87 ||
        16ull + (uint64_t)width * height * 4 != count) return ERROR_INVALID_DATA;
    HRESULT initialized = CoInitializeEx(nullptr, COINIT_MULTITHREADED);
    if (FAILED(initialized)) return ERROR_NOT_SUPPORTED;
    IWICImagingFactory* factory = nullptr;
    IWICStream* stream = nullptr;
    IWICBitmapEncoder* encoder = nullptr;
    IWICBitmapFrameEncode* frame = nullptr;
    HRESULT hr = CoCreateInstance(CLSID_WICImagingFactory, nullptr, CLSCTX_INPROC_SERVER,
        IID_PPV_ARGS(&factory));
    if (SUCCEEDED(hr)) hr = factory->CreateStream(&stream);
    if (SUCCEEDED(hr)) hr = stream->InitializeFromFilename(path, GENERIC_WRITE);
    if (SUCCEEDED(hr)) hr = factory->CreateEncoder(GUID_ContainerFormatPng, nullptr, &encoder);
    if (SUCCEEDED(hr)) hr = encoder->Initialize(stream, WICBitmapEncoderNoCache);
    if (SUCCEEDED(hr)) hr = encoder->CreateNewFrame(&frame, nullptr);
    if (SUCCEEDED(hr)) hr = frame->Initialize(nullptr);
    if (SUCCEEDED(hr)) hr = frame->SetSize(width, height);
    WICPixelFormatGUID pixels = GUID_WICPixelFormat32bppBGRA;
    if (SUCCEEDED(hr)) hr = frame->SetPixelFormat(&pixels);
    if (SUCCEEDED(hr) && !IsEqualGUID(pixels, GUID_WICPixelFormat32bppBGRA)) hr = E_FAIL;
    if (SUCCEEDED(hr)) hr = frame->WritePixels(height, width * 4, count - 16,
        const_cast<BYTE*>(bytes + 16));
    if (SUCCEEDED(hr)) hr = frame->Commit();
    if (SUCCEEDED(hr)) hr = encoder->Commit();
    if (frame) frame->Release();
    if (encoder) encoder->Release();
    if (stream) stream->Release();

    // Reopen the completed file before the transaction replaces the destination.
    IWICBitmapDecoder* decoder = nullptr;
    IWICBitmapFrameDecode* decoded = nullptr;
    UINT checkedWidth = 0, checkedHeight = 0;
    if (SUCCEEDED(hr)) hr = factory->CreateDecoderFromFilename(path, nullptr, GENERIC_READ,
        WICDecodeMetadataCacheOnLoad, &decoder);
    if (SUCCEEDED(hr)) hr = decoder->GetFrame(0, &decoded);
    if (SUCCEEDED(hr)) hr = decoded->GetSize(&checkedWidth, &checkedHeight);
    if (SUCCEEDED(hr) && (checkedWidth != width || checkedHeight != height)) hr = E_FAIL;
    if (decoded) decoded->Release();
    if (decoder) decoder->Release();
    if (factory) factory->Release();
    CoUninitialize();
    return SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INVALID_DATA;
}
