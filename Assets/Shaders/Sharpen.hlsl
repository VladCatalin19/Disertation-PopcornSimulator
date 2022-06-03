#ifndef POPCORNGENERATOR_SHADERS_SHARPEN_HLSL
#define POPCORNGENERATOR_SHADERS_SHARPEN_HLSL

inline float unity_noise_randomValue (float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453);
}

inline float unity_noise_interpolate (float a, float b, float t)
{
    return (1.0-t)*a + (t*b);
}

inline float unity_valueNoise (float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);

    uv = abs(frac(uv) - 0.5);
    float2 c0 = i + float2(0.0, 0.0);
    float2 c1 = i + float2(1.0, 0.0);
    float2 c2 = i + float2(0.0, 1.0);
    float2 c3 = i + float2(1.0, 1.0);
    float r0 = unity_noise_randomValue(c0);
    float r1 = unity_noise_randomValue(c1);
    float r2 = unity_noise_randomValue(c2);
    float r3 = unity_noise_randomValue(c3);

    float bottomOfGrid = unity_noise_interpolate(r0, r1, f.x);
    float topOfGrid = unity_noise_interpolate(r2, r3, f.x);
    float t = unity_noise_interpolate(bottomOfGrid, topOfGrid, f.y);
    return t;
}

inline float Unity_SimpleNoise_float(float2 UV, float Scale)
{
    float t = 0.0;

    float freq = pow(2.0, float(0));
    float amp = pow(0.5, float(3-0));
    t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    freq = pow(2.0, float(1));
    amp = pow(0.5, float(3-1));
    t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    freq = pow(2.0, float(2));
    amp = pow(0.5, float(3-2));
    t += unity_valueNoise(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    return t;
}

void SharpenNoise_float(float2 UV, float Noise_Scale, float UV_X_Delta, float UV_Y_Delta, out float Out)
{
    // https://en.wikipedia.org/wiki/Kernel_(image_processing)
    
    float k01 = -Unity_SimpleNoise_float(UV - float2(0.0F, UV_Y_Delta), Noise_Scale);
    float k10 = -Unity_SimpleNoise_float(UV - float2(UV_X_Delta, 0.0F), Noise_Scale);
    float k11 = 5.0F * Unity_SimpleNoise_float(UV, Noise_Scale);
    float k12 = -Unity_SimpleNoise_float(UV + float2(UV_X_Delta, 0.0F), Noise_Scale);
    float k21 = -Unity_SimpleNoise_float(UV + float2(0.0F, UV_Y_Delta), Noise_Scale);
    
    Out = clamp(k01 + k10 + k11 + k12 + k21, 0.0F, 1.0F);
}

#endif // POPCORNGENERATOR_SHADERS_SHARPEN_HLSL
