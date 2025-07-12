#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

void CubeMapUV_float(float3 dir, out float2 Out)
{
    float absX = abs(dir.x);
    float absY = abs(dir.y);
    float absZ = abs(dir.z);

    bool isXPositive = dir.x > 0;
    bool isYPositive = dir.y > 0;
    bool isZPositive = dir.z > 0;

    float u, v;

    if (absX >= absY && absX >= absZ)
    {
        // Major axis is X
        if (isXPositive)
        {
            // +X face
            u = -dir.z / absX;
            v = -dir.y / absX;
        }
        else
        {
            // -X face
            u = dir.z / absX;
            v = -dir.y / absX;
        }
    }
    else if (absY >= absX && absY >= absZ)
    {
        // Major axis is Y
        if (isYPositive)
        {
            // +Y face
            u = dir.x / absY;
            v = dir.z / absY;
        }
        else
        {
            // -Y face
            u = dir.x / absY;
            v = -dir.z / absY;
        }
    }
    else
    {
        // Major axis is Z
        if (isZPositive)
        {
            // +Z face
            u = dir.x / absZ;
            v = -dir.y / absZ;
        }
        else
        {
            // -Z face
            u = -dir.x / absZ;
            v = -dir.y / absZ;
        }
    }

    Out = float2(u, v) * 0.5f + float2(0.5f, 0.5f);

}
#endif //MYHLSLINCLUDE_INCLUDED
