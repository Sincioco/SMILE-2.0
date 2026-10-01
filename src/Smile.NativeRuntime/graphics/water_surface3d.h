#pragma once

// Native water shading borrows the water scene owner's opaque color and matching depth.
// Bindings must release the SRV before the next render pass.
struct SmileWaterTextureBinding3D
{
    ID3D11DeviceContext* context;
    SmileWaterTextureBinding3D(ID3D11DeviceContext* value,
        ID3D11ShaderResourceView* texture, ID3D11SamplerState* sampler,
        ID3D11ShaderResourceView* shadow, ID3D11SamplerState* shadow_sampler) : context(value)
    {
        context->PSSetShaderResources(5, 1, &shadow);
        context->PSSetSamplers(5, 1, &shadow_sampler);
        context->PSSetShaderResources(7, 1, &texture);
        context->PSSetSamplers(7, 1, &sampler);
    }
    ~SmileWaterTextureBinding3D()
    {
        ID3D11ShaderResourceView* empty = 0;
        context->PSSetShaderResources(5, 1, &empty);
        context->PSSetShaderResources(7, 1, &empty);
    }
};

static const char smile_water_surface_hlsl[] = R"water(
Texture2D waterScene : register(t7);
SamplerState waterSampler : register(s7);
Texture2D waterShadowMap : register(t5);
SamplerComparisonState waterShadowSampler : register(s5);

// Use the same sun shadow projection and tent filter as opaque receivers.
// Sky reflections remain visible in shade; only direct surface light is occluded.
float WaterShadowVisibility(float3 world, float3 normal, float3 light)
{
    if (waterShadow.x < .5) return 1;
    float4 projected = mul(float4(world, 1), waterShadowMvp);
    if (projected.w <= 0) return 1;
    float3 q = projected.xyz / projected.w;
    float2 uv = float2(q.x*.5+.5, .5-q.y*.5);
    if (any(uv < 0) || any(uv > 1) || q.z < 0 || q.z > 1) return 1;
    float bias = waterShadow.z + waterShadow.w*(1-saturate(dot(normal, light)));
    float sum = 0;
    [unroll] for (int y = -2; y <= 2; ++y)
        [unroll] for (int x = -2; x <= 2; ++x)
        {
            float weight = (3-abs(x))*(3-abs(y));
            sum += weight*waterShadowMap.SampleCmpLevelZero(waterShadowSampler,
                uv+float2(x,y)*(waterShadow.y*1.5), q.z-bias);
        }
    return sum/81;
}

float3 WaterEnvironment(float3 reflected)
{
    float horizon = smoothstep(-.18, .55, reflected.y);
    float cloud = .5 + .5 * sin(reflected.x * 8 + reflected.z * 5);
    return lerp(float3(.025,.032,.038), lerp(float3(.42,.51,.57),
        float3(.82,.87,.90), cloud), horizon);
}

// Continuous world-space detail crosses strip seams and travels with the caller's clock.
float3 WaterFlowPoint(float3 p, float seconds)
{
    if (waterFlow.w > .5)
    {
        p.xz -= waterFlow.xy * (seconds * waterFlow.z);
        p.y = 0;
    }
    return p;
}

float WaterHeight(float3 p, float seconds, float footprint)
{
    if (waterFlow.w > .5) p = WaterFlowPoint(p, seconds);
    else p += float3(seconds * -1.8, seconds * 3.2, seconds * .9);
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
    // Scale the search to the visible scene instead of assuming metre-sized geometry.
    float stride = max(.15, length(waterCamera.xyz-origin) * .002);
    float previousDistance = stride;
    float previousGap = -stride;
    [loop] for (int index = 1; index <= 28; ++index)
    {
        float distance = stride * (1.0 + index * index * .5);
        float4 clip = mul(float4(origin + direction * distance, 1), vp);
        if (clip.w <= .01) break;
        float2 unit = float2(clip.x / clip.w * .5 + .5, .5 - clip.y / clip.w * .5);
        if (any(unit < 0) || any(unit > 1)) break;
        float2 screen = (waterViewport.xy + unit * waterViewport.zw) / target.xy;
        float scene = sceneDepthTexture.SampleLevel(sceneDepthSampler, screen, 0).r;
        float gap = clip.w - scene;
        float thickness = max(stride*2, scene*.004);
        if (gap >= 0 && previousGap < 0 && gap < thickness + distance-previousDistance)
        {
            float low = previousDistance, high = distance;
            [unroll] for (int refine = 0; refine < 4; ++refine)
            {
                float middle = (low+high)*.5;
                float4 sampleClip = mul(float4(origin+direction*middle,1),vp);
                float2 sampleUnit = float2(sampleClip.x/sampleClip.w*.5+.5,.5-sampleClip.y/sampleClip.w*.5);
                float2 sampleScreen = (waterViewport.xy+sampleUnit*waterViewport.zw)/target.xy;
                float sampleDepth = sceneDepthTexture.SampleLevel(sceneDepthSampler,sampleScreen,0).r;
                if (sampleClip.w >= sampleDepth) high=middle; else low=middle;
                unit=sampleUnit; screen=sampleScreen;
            }
            float edge = saturate(min(min(unit.x, 1-unit.x), min(unit.y, 1-unit.y)) * 12);
            float3 sceneColor = waterScene.SampleLevel(waterSampler, screen, 0).rgb;
            if (atlasOutput.z < .5) sceneColor = ToLinear(saturate(sceneColor));
            return lerp(fallback, sceneColor, edge * .85);
        }
        previousDistance=distance;
        previousGap=gap;
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
    // Water reflects little head-on and more strongly at grazing angles.
    float fresnel = .0204 + .9796 * pow(1-facing, 5);
    float3 reflected = reflect(-view, normal);
    float3 reflection = WaterReflection(world + normal*2, reflected, WaterEnvironment(reflected));

    float3 light = normalize(waterLightDirection.xyz);
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
    float visibility = WaterShadowVisibility(world, normal, light);
    float ambientShare = waterAmbient.w /
        max(waterAmbient.w + waterLightColor.w * max(nl, .15), .0001);
    float physicalVisibility = waterShadowStyle.x < 0 ? visibility : 1;
    float illumination = lerp(ambientShare, 1, physicalVisibility);
    float3 highlight = min(specular, 12) * nl * waterLightColor.rgb * waterLightColor.w * physicalVisibility;

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
    if (waterFlow.w > .5)
    {
        float3 flowPoint = WaterFlowPoint(world, seconds);
        float along = dot(flowPoint.xz, waterFlow.xy);
        float across = dot(flowPoint.xz, float2(-waterFlow.y, waterFlow.x));
        // Broad irregular crests remain trackable at normal editor distances.
        foamNoise = sin(along*.25 + sin(across*.4)*.6);
    }
    float foam = smoothstep(.72,.94,foamNoise) * waterParameters.z * (1-smoothstep(.8,3,footprint));
    if (waterFlow.w > .5)
        foam = smoothstep(.55,.85,foamNoise) * waterParameters.z * (1-smoothstep(3,12,footprint));
    // Water absorbs its transmitted color; reflected buildings retain their own color.
    // Rough surfaces soften the reflection instead of becoming a mirror-like floor.
    // Retain the material's blue identity at grazing angles; preserve a bounded sheen.
    float3 waterTint = ToLinear(saturate(base.rgb));
    float tintPeak = max(max(waterTint.r, waterTint.g), max(waterTint.b, .001));
    float3 reflectionTint = lerp(float3(1,1,1), waterTint/tintPeak, .72);
    reflection *= reflectionTint;
    highlight *= lerp(float3(1,1,1), reflectionTint, .55);
    float reflectedAmount = min(.34, fresnel * (1 - roughness * .65));
    // Material opacity also bounds optical transmission. At 100% the submerged
    // bed cannot tint the surface black or expose underwater geometry. Reflection
    // still samples nearby opaque scenery independently of this bulk water color.
    float3 bulkColor = lerp(transmission * waterTint, waterTint * .45, saturate(base.a));
    float3 result = lerp(bulkColor * illumination, reflection, reflectedAmount) + highlight;
    result = lerp(result, float3(.38,.61,.80) * illumination, foam);
    if (waterShadowStyle.x >= 0) result *= lerp(1, visibility, waterShadowStyle.x);
    float opacity = saturate(base.a * (1.15 + fresnel*.45 + foam*.5));
    if (atlasOutput.z < .5) result = pow(saturate(result), 1.0/2.2);
    return float4(result, opacity);
}
)water";
