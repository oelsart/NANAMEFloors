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
} u;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;
layout(set = 2, binding = 2) uniform sampler2D _MaskTex;
layout(set = 2, binding = 3) uniform sampler2D _BurnTex;
layout(set = 2, binding = 4) uniform sampler2D _AlphaAddTex; // noise; white default -> smooth edges

vec3 linearDodge(vec3 s, vec3 d) { return s + d; }

// Verbatim from TerrainRoughFadeFactor.cginc RoughFade.
float roughFade(vec2 position, float rawAlpha)
{
    const float noiseImportance = 0.6;
    const float exPart = 1.0 - noiseImportance;
    float noise = texture(_AlphaAddTex, position * 2.0).r * 0.333
                + texture(_AlphaAddTex, position * 5.0).g * 0.333
                + texture(_AlphaAddTex, position * 10.0).b * 0.333;
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
    uv.y = -uv.y; 
    vec4 rawCol = texture(_MainTex, uv);

    vec4 burnTex = texture(_BurnTex, uv * u._BurnScale.xy);
    vec3 blendedCol = linearDodge(mix(burnTex.rgb * u._BurnColor.rgb, vec3(0.0), 1.0 - u._BurnColor.a * burnTex.a), rawCol.rgb);

    vec4 finalCol = vec4(blendedCol, 1.0);
    finalCol.a = roughFade(uv, rawCol.a);
    outColor = finalCol * u._Color * u._PollutionTintColor;
    outColor.a *= texture(_MaskTex, RotateUV(fract(vec2(vUv.x, -vUv.y)), vColor.r * 255)).a;
}
