#version 450
#include "RotationUtil.glsl"

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv; // object-space XZ

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;              // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _PollutionTintColor; // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _BurnColor;          // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _BurnScale;          // @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec2 _ScrollSpeed;        // @default = vec2(0.0, 0.0)
    float _GameSeconds;       // global, set by TickManager
} u;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;
layout(set = 2, binding = 2) uniform sampler2D _MaskTex;
layout(set = 2, binding = 3) uniform sampler2D _BurnTex;
layout(set = 2, binding = 4) uniform sampler2D _AlphaAddTex; // noise; white default -> smooth edges

vec2 flipV(vec2 uv) { return vec2(uv.x, 1.0 - uv.y); } 

// From PollutionFX.cginc SoftLight.
vec3 softLight(vec3 base, vec3 blend)
{
    vec3 limit = step(vec3(0.5), blend);
    return mix(2.0 * base * blend + base * base * (1.0 - 2.0 * blend),
               sqrt(max(base, vec3(0.0))) * (2.0 * blend - 1.0) + (2.0 * base) * (1.0 - blend),
               limit);
}

// Verbatim from TerrainRoughFadeFactor.cginc RoughFade.
float roughFade(vec2 position, float rawAlpha)
{
    const float noiseImportance = 0.6;
    const float exPart = 1.0 - noiseImportance;
    float noise = texture(_AlphaAddTex, flipV(position * 2.0)).r * 0.333
                + texture(_AlphaAddTex, flipV(position * 5.0)).g * 0.333
                + texture(_AlphaAddTex, flipV(position * 10.0)).b * 0.333;
    float alpha = mix(rawAlpha, noise, noiseImportance);
    alpha -= (1.0 - exPart) * 0.5;
    alpha /= exPart;
    const float clampSpeed = 1.5;
    alpha = clamp(alpha, max(rawAlpha * clampSpeed - clampSpeed + 1.0, 0.0), min(rawAlpha * clampSpeed, 1.0));
    return alpha;
}

void main()
{
    vec2 uv = vUv / 16.0;
    vec4 rawCol = texture(_MainTex, flipV(uv)) * vColor;

    vec4 burnTex = texture(_BurnTex, flipV(uv * u._BurnScale.xy + u._GameSeconds * u._ScrollSpeed));
    burnTex.rgb = mix(burnTex.rgb, vec3(0.5), 1.0 - burnTex.a * u._BurnColor.a);
    vec3 blendedCol = softLight(rawCol.rgb * u._Color.rgb, burnTex.rgb);

    vec4 finalCol = vec4(blendedCol, 1.0);
    finalCol.a = roughFade(uv, rawCol.a);
    outColor = finalCol * u._PollutionTintColor;
    outColor.a *= texture(_MaskTex, RotateUV(fract(vec2(vUv.x, -vUv.y)), vColor.r * 255)).a;
}
