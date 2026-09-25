#pragma once

// Native water shading borrows this view's opaque scene and optional matching depth.
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
    float horizon = smoothstep(-.18, .55, reflected.y);
    float cloud = .5 + .5 * sin(reflected.x * 8 + reflected.z * 5);
    return lerp(float3(.025,.032,.038), lerp(float3(.42,.51,.57),
        float3(.82,.87,.90), cloud), horizon);
}

// Continuous world-space detail crosses strip seams and travels with the caller's clock.
float WaterHeight(float3 p, float seconds, float footprint)
{
    p += float3(seconds * -1.8, seconds * 3.2, seconds * .9);
    float warp = sin(p.x*.12 + p.y*.16) + cos(p.z*.18 - p.y*.09);
    float broad = sin(p.x*.24 + warp) * cos(p.y*.21 - p.z*.19 + warp);
    float folds = sin(p.x*.71 - p.z*.53 + broad*2) * sin(p.y*.63 + warp);
    float fine = sin(p.x*1.83 + p.y*1.21 + folds) * cos(p.z*1.67 - p.y*.97);
    // Fade each octave before it becomes subpixel; MSAA cannot filter shader noise.
    float broadWeight = 1-smoothstep(2.0, 8.0, footprint);
    float foldWeight = 1-smoothstep(.8, 3.0, footprint);
    float fineWeight = 1-smoothstep(.3, 1.3, footprint);
    return broad * 1.15 * broadWeight + folds * .32 * foldWeight + fine * .075 * fineWeight;
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
            float3 sceneColor = waterScene.SampleLevel(waterSampler, screen, 0).rgb;
            if (atlasOutput.z < .5) sceneColor = ToLinear(saturate(sceneColor));
            return lerp(fallback, sceneColor, edge * .85);
        }
    }
    return fallback;
}

float4 ShadeWater(float4 pixel, float2 uv, float4 base, float3 world, float3 surfaceNormal)
{
    float3 view = normalize(waterCamera.xyz - world);
    float3 normal = normalize(dot(surfaceNormal,surfaceNormal) > .0001 ? surfaceNormal :
        cross(ddx(world), ddy(world)) + float3(0,.000001,0));
    normal *= dot(normal, view) < 0 ? -1 : 1;
    float seconds = waterCamera.w;
    float3 dx = ddx(world), dy = ddy(world);
    float footprint = max(length(dx), length(dy));
    float height = WaterHeight(world, seconds, footprint) * waterLightDirection.w;
    float3 acrossX = cross(dy, normal), acrossY = cross(normal, dx);
    float determinant = dot(dx, acrossX);
    float3 gradient = ddx(height)*acrossX + ddy(height)*acrossY;
    normal = normalize(normal - gradient * sign(determinant) / max(abs(determinant), .00001));
    float facing = saturate(dot(normal, view));
    // Broaden the reflected rim so narrow authored streams read at gameplay distance.
    float fresnel = .06 + .94 * pow(1-facing, 3);
    float3 reflected = reflect(-view, normal);
    float3 reflection = WaterReflection(world + normal*2, reflected, WaterEnvironment(reflected));

    float3 light = normalize(-waterLightDirection.xyz);
    float3 halfway = normalize(light + view);
    float nl = saturate(dot(normal, light));
    float nh = saturate(dot(normal, halfway));
    float vh = saturate(dot(view, halfway));
    float roughness = max(.08, waterParameters.y);
    float3 normalDx = ddx(normal), normalDy = ddy(normal);
    float variance = min(.18, .25*(dot(normalDx,normalDx)+dot(normalDy,normalDy)));
    roughness = min(1, sqrt(sqrt(pow(roughness,4)+variance)));
    float alpha = roughness * roughness;
    float denominator = nh*nh*(alpha*alpha-1)+1;
    float distribution = alpha*alpha / max(3.141593*denominator*denominator, .00001);
    float k = (roughness+1)*(roughness+1)/8;
    float geometry = facing/(facing*(1-k)+k) * nl/(nl*(1-k)+k);
    float specular = distribution*geometry*(.0204+.9796*pow(1-vh,5)) / max(4*facing*nl,.0001);
    float3 highlight = min(specular, 12) * nl * waterLightColor.rgb * waterLightColor.w;

    float2 screen = pixel.xy / target.xy;
    float2 bend = float2(dot(normal,cameraRight.xyz), -dot(normal,cameraUp.xyz)) * .012;
    float3 transmission = float3(.055,.085,.095);
    if (waterParameters.w > .5)
    {
        float2 refracted = clamp(screen + bend, .001, .999);
        // Reflected color is valid without borrowing the unrelated main-camera depth.
        float depth = 2;
        if (softDepth.x > .5)
        {
            float behind = sceneDepthTexture.SampleLevel(sceneDepthSampler, refracted, 0).r;
            if (behind < Linear(pixel.z)) refracted = screen;
            // Screen depth is a bounded thickness proxy, never a far-backdrop distance.
            depth = min(24, max(2, behind-Linear(pixel.z)));
        }
        float3 absorption = exp(-float3(.009,.004,.0028)*depth);
        float3 sceneColor = waterScene.SampleLevel(waterSampler, refracted, 0).rgb;
        if (atlasOutput.z < .5) sceneColor = ToLinear(saturate(sceneColor));
        transmission = sceneColor * absorption;
        transmission += float3(.035,.075,.085) * (1-absorption);
    }
    float foamNoise = sin(world.x*.62 + sin(world.z*.41)*2 + seconds*2.8) *
        sin(world.y*.83 - world.z*.36 + seconds*1.9);
    float foam = smoothstep(.72,.94,foamNoise) * waterParameters.z * (1-smoothstep(.8,3,footprint));
    // Honor the artist's material/vertex tint across the water body, including
    // its reflected environment. Otherwise grazing views erase deep-water color.
    // White preserves existing water; direct sunlight still produces white glints.
    float3 result = lerp(transmission, reflection, fresnel) * ToLinear(saturate(base.rgb)) + highlight;
    result = lerp(result, float3(.72,.83,.85), foam);
    float opacity = saturate(base.a * (1.15 + fresnel*.45 + foam*.5));
    if (atlasOutput.z < .5) result = pow(saturate(result), 1.0/2.2);
    return float4(result, opacity);
}
)water";
