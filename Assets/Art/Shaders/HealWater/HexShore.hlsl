#ifndef HEX_SHORE_INCLUDED
#define HEX_SHORE_INCLUDED

// p: object-space xz of a pointy-top hex of size 1.
// waterMask: bit i set = edge i borders more water (no shore there).
// Edge i's outward normal is at 60*i degrees from +X; must match HealPoolVisual.LocalEdgeIndex.
void HexShoreDistance_float(float2 p, float waterMask, out float dist)
{
    const float apothem = 0.8660254; // sqrt(3)/2
    uint mask = (uint)round(waterMask);
    dist = 1.0; // "far from any shore"

    [unroll]
    for (uint i = 0; i < 6; i++)
    {
        if ((mask >> i) & 1u)
            continue;
        float a = radians(60.0 * i);
        dist = min(dist, apothem - dot(p, float2(cos(a), sin(a))));
    }
}
#endif