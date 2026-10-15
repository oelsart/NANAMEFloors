#version 450
#include "RotationUtil.glsl"

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv; // object-space XZ

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color; // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;
layout(set = 2, binding = 2) uniform sampler2D _MaskTex;
layout(set = 2, binding = 3) uniform sampler2D _AlphaAddTex; // noise; white default -> smooth edges

float roughFade(vec2 position, float rawAlpha)
{
    const float noiseImportance = 0.6;
    const float exPart = 1.0 - noiseImportance;

    float noise = texture(_AlphaAddTex, position * 2.0).r * 0.333
                + texture(_AlphaAddTex, position * 5.0).g * 0.333
                + texture(_AlphaAddTex, position * 10.0).b * 0.333;

    float alpha = mix(rawAlpha, noise, noiseImportance);

    // Expand the middle of the alpha to make a sharper line.
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
    vec4 finalCol = rawCol;
    finalCol.a = roughFade(uv, rawCol.a);
    outColor = finalCol * uMaterial._Color;
    outColor.a *= texture(_MaskTex, RotateUV(fract(vec2(vUv.x, -vUv.y)), vColor.r * 255)).a;
}
