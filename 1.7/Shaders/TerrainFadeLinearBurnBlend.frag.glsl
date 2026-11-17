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

vec2 flipV(vec2 uv) { return vec2(uv.x, 1.0 - uv.y); } 

const float LOCAL_PI = 3.14159265;
const float LOCAL_PI_2 = 1.57079632;

vec3 linearBurn(vec3 s, vec3 d) { return s + d - 1.0; }

vec2 uvRotate(vec2 v, float angle)
{
    return vec2(
        cos(angle) * (v.x - 0.5) + sin(angle) * (v.y - 0.5) + 0.5,
        cos(angle) * (v.y - 0.5) - sin(angle) * (v.x - 0.5) + 0.5);
}

vec2 randomizeAngleWithUV(vec2 uv)
{
    int tX = int(trunc(uv.x));
    int tY = int(trunc(uv.y));
    vec2 uvMod = fract(uv);
    if (tX % 2 == 0) uvMod = uvRotate(uvMod, LOCAL_PI);
    if (tY % 3 == 0) uvMod = uvRotate(uvMod, -LOCAL_PI_2);
    return uvMod;
}

void main()
{
    vec2 uv = vUv / 16.0;
    vec2 burnUv = randomizeAngleWithUV(uv * u._BurnScale.xy);
    vec4 burnTex = texture(_BurnTex, flipV(burnUv));
    vec3 sampleCol = linearBurn(mix(burnTex.rgb * u._BurnColor.rgb, vec3(1.0), 1.0 - u._BurnColor.a * burnTex.a), texture(_MainTex, flipV(uv)).rgb);
    outColor = vec4(sampleCol, 1.0) * u._Color * u._PollutionTintColor;
    outColor.a *= texture(_MaskTex, RotateUV(fract(vec2(vUv.x, -vUv.y)), vColor.r * 255)).a;
}
