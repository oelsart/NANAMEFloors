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

void main()
{
    outColor = texture(_MainTex, vec2(vUv.x, -vUv.y) / 16.0) * uMaterial._Color;
    outColor.a *= texture(_MaskTex, RotateUV(fract(vec2(vUv.x, -vUv.y)), vColor.r * 255)).a;
}
