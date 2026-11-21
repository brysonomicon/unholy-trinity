// Boost tile chevrons for BoostChevrons.shadergraph (used by the "BoostChevrons" Custom Function node).
// Three concentric hex rings, split into six chevrons that point at the tile's faces.
// Unidirectional tiles keep only the wedge facing +V; the quad is rotated in code to face the boost direction.

#ifndef BOOST_CHEVRONS_INCLUDED
#define BOOST_CHEVRONS_INCLUDED

// UV: 0..1 across a 2x2-unit quad, so p = ±1 is the hex corner radius (faces are at 0.866).
// Inner / Spacing are in that same unit. Thickness is a fraction of Spacing (0..1).
void BoostChevrons_float(float2 UV, float Strength, float Unidirectional, float Time, float Speed,
                         float Inner, float Spacing, float Thickness, float Gap, out float Mask)
{
    float2 p = UV * 2 - 1;
    float2 a = abs(p);

    // Hex distance (rings are pointy-top so a corner points +V, at the tile face)
    float d = max(a.x, a.x * 0.5 + a.y * 0.866);
    float k = (d - Inner) / Spacing;
    float ring = floor(k);
    float band = frac(k);
    float aa = fwidth(k);
    float stroke = smoothstep(0, aa, band) * (1 - smoothstep(Thickness - aa, Thickness, band));

    // Show 1..3 rings depending on strength
    float shown = clamp(Strength, 0, 3);
    stroke *= step(0, ring) * step(ring, shown - 1);

    // Constant-width gaps along the three lines through the centre at 0°, 60° and 120°
    float lineDist = min(a.y, min(abs(0.866 * p.x - 0.5 * p.y), abs(0.866 * p.x + 0.5 * p.y)));
    stroke *= smoothstep(Gap * 0.5, Gap * 0.5 + fwidth(lineDist), lineDist);

    // Unidirectional mode: keep only the wedge within ±30° of +V
    float wedge = step(0, p.y) * step(a.x, p.y * 0.577);
    stroke *= lerp(1, wedge, Unidirectional);

    // Brightness pulse travelling inner -> outer across the rings that are shown
    float chase = saturate(1 - frac(ring / max(shown, 1) - Time * Speed) * 2);
    Mask = stroke * lerp(0.35, 1, chase);
}

#endif
