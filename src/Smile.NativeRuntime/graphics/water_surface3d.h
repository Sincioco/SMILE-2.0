#pragma once

// Native water shading borrows the resolved opaque scene/depth for this draw.
// It owns no render targets and must release the SRV before the next render pass.
struct SmileWaterTextureBinding3D
{
    ID3D11DeviceContext* context;
    SmileWaterTextureBinding3D(ID3D11DeviceContext* value,
        ID3D11ShaderResourceView* texture, ID3D11SamplerState* sampler) : context(value)
    {
        context->PSSetShaderResources(7, 1, &texture);
        context->PSSetSamplers(7, 1, &sampler);
    }
    ~SmileWaterTextureBinding3D()
    {
        ID3D11ShaderResourceView* empty = 0;
        context->PSSetShaderResources(7, 1, &empty);
    }
};

static const char smile_water_surface_hlsl[] = R"water(
Texture2D waterScene : register(t7);
SamplerState waterSampler : register(s7);

float3 WaterEnvironment(float3 reflected)
{
    float horizon = saturate(reflected.y * 0.5 + 0.5);
    return lerp(float3(.018,.028,.035), float3(.32,.43,.50), horizon);
}

float3 WaterReflection(float3 origin, float3 direction, float3 fallback)
{
    if (waterParameters.w < .5 || softDepth.x < .5) return fallback;
    [loop] for (int index = 1; index <= 20; ++index)
    {
        float distance = 4.0 + index * index * .65;
        float4 clip = mul(float4(origin + direction * distance, 1), vp);
        if (clip.w <= .01) break;
        float2 unit = float2(clip.x / clip.w * .5 + .5, .5 - clip.y / clip.w * .5);
        if (any(unit < 0) || any(unit > 1)) break;
        float2 screen = (waterViewport.xy + unit * waterViewport.zw) / target.xy;
        float scene = sceneDepthTexture.SampleLevel(sceneDepthSampler, screen, 0).r;
        float gap = clip.w - scene;
        if (gap > .1 && gap < 4.0 + index * 1.3)
        {
            float edge = saturate(min(min(unit.x, 1-unit.x), min(unit.y, 1-unit.y)) * 12);
            return lerp(fallback, waterScene.SampleLevel(waterSampler, screen, 0).rgb, edge * .85);
        }
    }
    return fallback;
}

float4 ShadeWater(float4 pixel, float2 uv, float4 base, float3 world)
{
    float3 view = normalize(waterCamera.xyz - world);
    float3 normal = normalize(cross(ddx(world), ddy(world)) + float3(0,.000001,0));
    normal *= dot(normal, view) < 0 ? -1 : 1;
    float seconds = waterCamera.w;
    float3 ripples = float3(sin(world.z*.20 + seconds*2.1),
        sin(world.x*.17 - seconds*1.7), cos(world.y*.23 + seconds*1.5));
    normal = normalize(normal + ripples * .11);
    float facing = saturate(dot(normal, view));
    float fresnel = .0204 + .9796 * pow(1-facing, 5);
    float3 reflected = reflect(-view, normal);
    float3 reflection = WaterReflection(world + normal*2, reflected, WaterEnvironment(reflected));

    float3 light = normalize(-waterLightDirection.xyz);
    float3 halfway = normalize(light + view);
    float nl = saturate(dot(normal, light));
    float nh = saturate(dot(normal, halfway));
    float vh = saturate(dot(view, halfway));
    float roughness = max(.08, waterParameters.y);
    float alpha = roughness * roughness;
    float denominator = nh*nh*(alpha*alpha-1)+1;
    float distribution = alpha*alpha / max(3.141593*denominator*denominator, .00001);
    float k = (roughness+1)*(roughness+1)/8;
    float geometry = facing/(facing*(1-k)+k) * nl/(nl*(1-k)+k);
    float specular = distribution*geometry*(.0204+.9796*pow(1-vh,5)) / max(4*facing*nl,.0001);
    float3 highlight = min(specular, 12) * nl * waterLightColor.rgb * waterLightColor.w;

    float2 screen = pixel.xy / target.xy;
    float2 bend = float2(dot(normal,cameraRight.xyz), -dot(normal,cameraUp.xyz)) * .008;
    float3 transmission = float3(.025,.095,.12);
    if (waterParameters.w > .5)
    {
        float2 refracted = clamp(screen + bend, .001, .999);
        float behind = sceneDepthTexture.SampleLevel(sceneDepthSampler, refracted, 0).r;
        if (behind < Linear(pixel.z)) refracted = screen;
        float depth = min(55, max(4, behind-Linear(pixel.z)));
        float3 absorption = exp(-float3(.024,.007,.004)*depth);
        transmission = waterScene.SampleLevel(waterSampler, refracted, 0).rgb * absorption;
        transmission += float3(.012,.07,.09) * (1-absorption);
    }
    float foamNoise = sin(world.x*.62 + sin(world.z*.41)*2 + seconds*2.8) *
        sin(world.y*.83 - world.z*.36 + seconds*1.9);
    float foam = smoothstep(.72,.94,foamNoise) * waterParameters.z;
    float3 result = lerp(transmission, reflection, fresnel) + highlight;
    result = lerp(result, float3(.72,.83,.85), foam);
    float opacity = saturate(base.a * (1.15 + fresnel*.45 + foam*.5));
    if (atlasOutput.z < .5) result = pow(saturate(result), 1.0/2.2);
    return float4(result, opacity);
}
)water";
