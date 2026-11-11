// Smoke flow for GlyphSmoke.shadergraph (used by the "SmokeFlow" Custom Function node).
// Noise flows outward from the centre of the glyph, and the smoke samples are pushed outward with it,
// so ink appears to bleed away from the seal's strokes in every direction.

#ifndef GLYPH_SMOKE_FLOW_INCLUDED
#define GLYPH_SMOKE_FLOW_INCLUDED

// Same maths as Shader Graph's Gradient Noise node, renamed so it can't clash with one in the graph.
float2 GSF_GradientDir(float2 p)
{
    p = p % 289;
    float x = (34 * p.x + 1) * p.x % 289 + p.y;
    x = (34 * x + 1) * x % 289;
    x = frac(x / 41) * 2 - 1;
    return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

// Returns roughly 0..1, like the Gradient Noise node.
float GSF_GradientNoise(float2 p)
{
    float2 ip = floor(p);
    float2 fp = frac(p);
    float d00 = dot(GSF_GradientDir(ip), fp);
    float d01 = dot(GSF_GradientDir(ip + float2(0, 1)), fp - float2(0, 1));
    float d10 = dot(GSF_GradientDir(ip + float2(1, 0)), fp - float2(1, 0));
    float d11 = dot(GSF_GradientDir(ip + float2(1, 1)), fp - float2(1, 1));
    fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
    return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
}

// Noise whose features drift outward from uv (0.5, 0.5). The zoom restarts every cycle, so two copies run
// half a cycle apart and are crossfaded: each copy is fully faded out at the moment it restarts.
float GSF_RadialNoise(float2 uv, float time, float speed, float scale, float2 seed)
{
    float2 p = uv - 0.5;
    float phaseA = frac(time * speed);
    float phaseB = frac(time * speed + 0.5);

    // Sampling closer to the centre as the phase grows makes the pattern move outward on screen.
    float nA = GSF_GradientNoise((p * (1 - phaseA * 0.5) + seed) * scale);
    float nB = GSF_GradientNoise((p * (1 - phaseB * 0.5) + seed + float2(17.3, 9.1)) * scale);

    float weightA = 1 - abs(phaseA * 2 - 1); // 0 when A restarts, 1 when B restarts
    return lerp(nB, nA, weightA);
}

// UV: the glyph UV (gUV). Outputs the noise for the wisps and the UVs for the two smoke samples.
void SmokeFlow_float(float2 UV, float Time, float Speed, float Scale1, float Scale2, float Distortion,
                     out float Noise, out float2 UV1, out float2 UV2)
{
    float2 fromCentre = UV - 0.5;
    float2 dir = fromCentre / max(length(fromCentre), 1e-4); // outward; guarded against the exact centre

    float n1 = GSF_RadialNoise(UV, Time, Speed, Scale1, float2(0, 0));
    float n2 = GSF_RadialNoise(UV, Time, Speed * 1.6, Scale2, float2(5.7, 31.2));

    // Outward push varying with n1, plus a sideways wobble from n2 for wispy edges.
    float2 offset = dir * n1 * Distortion
                  + float2(-dir.y, dir.x) * (n2 - 0.5) * Distortion;

    // Sampling the glyph closer to the centre makes the smoke appear further out.
    Noise = n1;
    UV1 = UV - offset;
    UV2 = UV - offset * 2 - dir * 0.04;
}

void SmokeFlow_half(half2 UV, half Time, half Speed, half Scale1, half Scale2, half Distortion,
                    out half Noise, out half2 UV1, out half2 UV2)
{
    float noise;
    float2 uv1, uv2;
    SmokeFlow_float(UV, Time, Speed, Scale1, Scale2, Distortion, noise, uv1, uv2);
    Noise = noise;
    UV1 = uv1;
    UV2 = uv2;
}

#endif
