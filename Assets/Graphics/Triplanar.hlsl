#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED
// gets the coordinate returns the uv position
void TriplanarUV_float(float3 vert, float3 norm, out float2 uv)
{
    float3 w = abs(norm);
    w /= (w.x + w.y + w.z);

    float2 uvX = vert.yz;
    float2 uvY = vert.xz;
    float2 uvZ = vert.xy;

    // Orientation correction
    uvX.x *= sign(norm.x);
    uvY.x *= sign(norm.y);
    uvZ.x *= sign(norm.z);

    uv = uvX * w.x +
         uvY * w.y +
         uvZ * w.z;
}

#endif //MYHLSLINCLUDE_INCLUDED