#ifndef ROTATION_UTIL_GLSL
#define ROTATION_UTIL_GLSL

vec2 RotateUV(vec2 uv, float rotateIndex)
{
    vec2 centered_uv = uv - 0.5;
    uint rot = uint(round(rotateIndex)) % 4u;
    vec2 rotated_uv;

    switch (rot)
    {
        case 1u:  rotated_uv = vec2( centered_uv.y, -centered_uv.x); break;
        case 2u:  rotated_uv = vec2(-centered_uv.x, -centered_uv.y); break;
        case 3u:  rotated_uv = vec2(-centered_uv.y,  centered_uv.x); break;
        default:  rotated_uv = centered_uv; break;
    }

    return rotated_uv + 0.5;
}

#endif