#ifndef POPCORNGENERATOR_SHADERS_SPHEREMASK_HLSL
#define POPCORNGENERATOR_SHADERS_SPHEREMASK_HLSL

void SphereMask_float(const float2 Center, const float Radius, const float2 Point, out float GrayLevel)
{
    if (length(Point - Center) <= (Radius * 0.5F))
    {
        float distancePercent = clamp(length(Point - Center) / Radius, 0.0F, 1.0F);
        GrayLevel = 0.5F - atan(5.0F * distancePercent - 2.5F) / 3.1415926F;
    }
    else if (length(Point - Center) <= Radius)
    {
        float distancePercent = clamp(length(Point - Center) / Radius, 0.0F, 1.0F);
        GrayLevel = 0.5F - atan(50.0F * distancePercent - 25.0F) / 3.1415926F;
    }
    else
    {
        GrayLevel = 0.0F;
    }    
}

#endif // POPCORNGENERATOR_SHADERS_SPHEREMASK_HLSL
