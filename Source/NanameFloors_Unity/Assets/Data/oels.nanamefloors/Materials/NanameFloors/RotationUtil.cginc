#pragma once

float2 RotateUV(float2 uv, float rotateIndex)
{
    float2 centered_uv = uv - 0.5;
    uint rot = (uint)round(rotateIndex) % 4;
    float2 rotated_uv;

    switch (rot)
    {
        case 1:  rotated_uv = float2(-centered_uv.y,  centered_uv.x); break;
        case 2:  rotated_uv = float2(-centered_uv.x, -centered_uv.y); break;
        case 3:  rotated_uv = float2( centered_uv.y, -centered_uv.x); break;
        default: rotated_uv = centered_uv; break;
    }
    
    return rotated_uv + 0.5;
}