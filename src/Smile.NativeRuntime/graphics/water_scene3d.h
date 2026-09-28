#pragma once

// One opaque-color snapshot shared by all water surfaces in the current view.
// It is independent of heat distortion and reuses its allocation until resize.
struct SmileWaterScene3D
{
    ID3D11Texture2D* texture = 0;
    ID3D11ShaderResourceView* view = 0;
    UINT width = 0, height = 0;
    DXGI_FORMAT format = DXGI_FORMAT_UNKNOWN;
    bool valid = false;

    void reset()
    {
        if (view) view->Release();
        if (texture) texture->Release();
        view = 0; texture = 0; width = height = 0;
        format = DXGI_FORMAT_UNKNOWN; valid = false;
    }

    bool capture(ID3D11Device* device, ID3D11DeviceContext* context,
        ID3D11RenderTargetView* source_view)
    {
        valid = false;
        if (!device || !context || !source_view) return false;
        ID3D11Resource* resource = 0;
        ID3D11Texture2D* source = 0;
        source_view->GetResource(&resource);
        if (!resource) return false;
        const HRESULT result = resource->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&source);
        resource->Release();
        if (FAILED(result)) return false;
        D3D11_TEXTURE2D_DESC description = {};
        source->GetDesc(&description);
        const UINT samples = description.SampleDesc.Count;
        if (!texture || width != description.Width || height != description.Height || format != description.Format)
        {
            reset();
            width = description.Width; height = description.Height; format = description.Format;
            description.MipLevels = description.ArraySize = 1;
            description.SampleDesc.Count = 1; description.SampleDesc.Quality = 0;
            description.Usage = D3D11_USAGE_DEFAULT;
            description.BindFlags = D3D11_BIND_SHADER_RESOURCE;
            description.CPUAccessFlags = description.MiscFlags = 0;
            if (FAILED(device->CreateTexture2D(&description, 0, &texture)) ||
                FAILED(device->CreateShaderResourceView(texture, 0, &view)))
            { source->Release(); reset(); return false; }
        }
        context->OMSetRenderTargets(0, 0, 0);
        if (samples > 1) context->ResolveSubresource(texture, 0, source, 0, format);
        else context->CopyResource(texture, source);
        source->Release();
        valid = true;
        return true;
    }
};
