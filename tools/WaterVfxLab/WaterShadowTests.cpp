// Offscreen regression for the actual native water shader: sun occlusion must
// darken water, disabled shadows must preserve it, and SRVs must be released.
#include <d3d11.h>
#include <d3dcompiler.h>
#include <wrl/client.h>
#include <string>
#include <stdio.h>
#include <math.h>
#include <stdlib.h>
#include "../../src/Smile.NativeRuntime/graphics/water_surface3d.h"

using Microsoft::WRL::ComPtr;
static void require(HRESULT result)
{
    if (FAILED(result)) { printf("D3D failure %08x\n", (unsigned)result); exit(2); }
}

static ComPtr<ID3DBlob> compile(const std::string& source, const char* target)
{
    ComPtr<ID3DBlob> code, errors;
    HRESULT result = D3DCompile(source.data(), source.size(), "WaterShadowTests", 0, 0,
        "main", target, D3DCOMPILE_ENABLE_STRICTNESS, 0, &code, &errors);
    if (errors) printf("%s", (char*)errors->GetBufferPointer());
    require(result);
    return code;
}

int main()
{
    ComPtr<ID3D11Device> device;
    ComPtr<ID3D11DeviceContext> context;
    require(D3D11CreateDevice(0, D3D_DRIVER_TYPE_WARP, 0, 0, 0, 0,
        D3D11_SDK_VERSION, &device, 0, &context));
    const char* vertex =
        "struct O{float4 p:SV_POSITION;float2 uv:TEXCOORD;};"
        "O main(uint id:SV_VertexID){O o;o.uv=float2((id<<1)&2,id&2);"
        "o.p=float4(o.uv*float2(2,-2)+float2(-1,1),0,1);return o;}";
    auto vs_code = compile(vertex, "vs_5_0");
    ComPtr<ID3D11VertexShader> vs;
    require(device->CreateVertexShader(vs_code->GetBufferPointer(), vs_code->GetBufferSize(), 0, &vs));
    std::string pixel =
        "cbuffer Test:register(b0){float4 waterShadow;}"
        "static const float4 waterCamera=float4(0,5,-5,0);"
        "static const float4 waterParameters=float4(1,.6,0,0);"
        "static const float4 waterLightDirection=float4(0,1,0,0);"
        "static const float4 waterLightColor=float4(1,1,1,1);"
        "static const float4 waterAmbient=float4(1,1,1,.25);"
        "static const float4 target=float4(64,64,0,0);"
        "static const float4 softDepth=float4(0,0,1,100);"
        "static const float4 atlasOutput=float4(0,0,1,0);"
        "static const float4 waterViewport=float4(0,0,64,64);"
        "static const float4 cameraRight=float4(1,0,0,0),cameraUp=float4(0,1,0,0);"
        "static const row_major float4x4 vp=float4x4(1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1);"
        "static const row_major float4x4 waterShadowMvp="
        "float4x4(1,0,0,0,0,0,0,0,0,1,0,0,0,0,.5,1);"
        "Texture2D sceneDepthTexture:register(t6);SamplerState sceneDepthSampler:register(s6);"
        "float3 ToLinear(float3 c){return lerp(c/12.92,pow((c+.055)/1.055,2.4),step(.04045,c));}"
        "float Linear(float z){return z;}";
    pixel += smile_water_surface_hlsl;
    pixel += "float4 main(float4 p:SV_POSITION,float2 uv:TEXCOORD):SV_TARGET{"
        "return ShadeWater(p,uv,float4(.8,.8,.8,1),float3(uv.x*2-1,0,uv.y*2-1),float3(0,1,0));}";
    auto ps_code = compile(pixel, "ps_5_0");
    ComPtr<ID3D11PixelShader> ps;
    require(device->CreatePixelShader(ps_code->GetBufferPointer(), ps_code->GetBufferSize(), 0, &ps));
    D3D11_TEXTURE2D_DESC desc = {};
    desc.Width = desc.Height = 64;
    desc.MipLevels = desc.ArraySize = desc.SampleDesc.Count = 1;
    desc.Format = DXGI_FORMAT_R32G32B32A32_FLOAT;
    desc.BindFlags = D3D11_BIND_RENDER_TARGET;
    ComPtr<ID3D11Texture2D> target, staging;
    require(device->CreateTexture2D(&desc, 0, &target));
    ComPtr<ID3D11RenderTargetView> rtv;
    require(device->CreateRenderTargetView(target.Get(), 0, &rtv));
    desc.BindFlags = 0;
    desc.Usage = D3D11_USAGE_STAGING;
    desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
    require(device->CreateTexture2D(&desc, 0, &staging));
    float depths[64*64];
    for (int y=0; y<64; ++y) for (int x=0; x<64; ++x) depths[y*64+x] = x<32 ? .25f : 1.f;
    desc.Format = DXGI_FORMAT_R32_FLOAT;
    desc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
    desc.Usage = D3D11_USAGE_DEFAULT;
    desc.CPUAccessFlags = 0;
    D3D11_SUBRESOURCE_DATA data = {depths, 64*sizeof(float), 0};
    ComPtr<ID3D11Texture2D> shadow;
    ComPtr<ID3D11ShaderResourceView> shadow_view;
    require(device->CreateTexture2D(&desc, &data, &shadow));
    require(device->CreateShaderResourceView(shadow.Get(), 0, &shadow_view));
    D3D11_SAMPLER_DESC sampler_desc = {};
    sampler_desc.Filter = D3D11_FILTER_COMPARISON_MIN_MAG_LINEAR_MIP_POINT;
    sampler_desc.AddressU = sampler_desc.AddressV = sampler_desc.AddressW = D3D11_TEXTURE_ADDRESS_CLAMP;
    sampler_desc.ComparisonFunc = D3D11_COMPARISON_LESS_EQUAL;
    sampler_desc.MaxLOD = D3D11_FLOAT32_MAX;
    ComPtr<ID3D11SamplerState> sampler;
    require(device->CreateSamplerState(&sampler_desc, &sampler));
    D3D11_BUFFER_DESC buffer_desc = {};
    buffer_desc.ByteWidth = 16;
    buffer_desc.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
    ComPtr<ID3D11Buffer> constants;
    require(device->CreateBuffer(&buffer_desc, 0, &constants));
    D3D11_VIEWPORT viewport = {0,0,64,64,0,1};
    context->RSSetViewports(1, &viewport);
    context->OMSetRenderTargets(1, rtv.GetAddressOf(), 0);
    context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    context->VSSetShader(vs.Get(), 0, 0);
    context->PSSetShader(ps.Get(), 0, 0);
    context->PSSetConstantBuffers(0, 1, constants.GetAddressOf());
    float readings[2][2] = {};
    bool unbound = true;
    for (int enabled=0; enabled<2; ++enabled)
    {
        float values[] = {(float)enabled, 1.f/64, .001f, 0};
        context->UpdateSubresource(constants.Get(), 0, 0, values, 0, 0);
        {
            SmileWaterTextureBinding3D binding(context.Get(), 0, 0, shadow_view.Get(), sampler.Get());
            context->Draw(3, 0);
        }
        ComPtr<ID3D11ShaderResourceView> bound;
        context->PSGetShaderResources(5, 1, &bound);
        unbound = unbound && !bound;
        context->CopyResource(staging.Get(), target.Get());
        D3D11_MAPPED_SUBRESOURCE mapped = {};
        require(context->Map(staging.Get(), 0, D3D11_MAP_READ, 0, &mapped));
        const float* row = (const float*)((const char*)mapped.pData + 32*mapped.RowPitch);
        readings[enabled][0] = row[16*4] + row[16*4+1] + row[16*4+2];
        readings[enabled][1] = row[48*4] + row[48*4+1] + row[48*4+2];
        context->Unmap(staging.Get(), 0);
    }
    bool passed = unbound && readings[0][0] > .01f &&
        readings[1][0] > .001f && readings[1][0] < readings[0][0]*.7f &&
        fabsf(readings[1][1]-readings[0][1]) < .00001f;
    printf("Water shadow GPU regression: off %.6f/%.6f, on %.6f/%.6f, unbound %d: %s\n",
        readings[0][0], readings[0][1], readings[1][0], readings[1][1], unbound,
        passed ? "PASS" : "FAIL");
    return passed ? 0 : 1;
}
