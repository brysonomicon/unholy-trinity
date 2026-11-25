// Procedural stylized night sky for the title screen.
// Gradient with horizon glow, two layers of procedural stars, a noise nebula band,
// and slow built-in rotation. Use on a material assigned as the scene's Skybox Material.
Shader "UnholyTrinity/StylizedNightSky"
{
    Properties
    {
        [Header(Sky Gradient)]
        [HDR] _ZenithColor ("Zenith Color", Color) = (0.004, 0.008, 0.006, 1)
        [HDR] _HorizonColor ("Horizon Glow Color", Color) = (0.06, 0.22, 0.1, 1)
        [HDR] _GroundColor ("Below Horizon Color", Color) = (0, 0, 0, 1)
        _HorizonOffset ("Horizon Height", Range(-0.5, 0.5)) = 0
        _GlowHeight ("Glow Height", Range(0.01, 1)) = 0.35
        _GlowFalloff ("Glow Falloff", Range(0.1, 8)) = 1.5
        _GroundBlend ("Ground Blend", Range(0.001, 0.5)) = 0.05

        [Header(Stars)]
        [HDR] _StarColor ("Star Color", Color) = (1.5, 1.6, 1.4, 1)
        [HDR] _StarColorAlt ("Star Color Alt", Color) = (0.8, 1.6, 0.9, 1)
        _StarDensity ("Small Star Density", Range(10, 400)) = 120
        _StarChance ("Small Star Chance", Range(0, 1)) = 0.15
        _StarSize ("Small Star Size", Range(0, 0.5)) = 0.06
        _BigStarDensity ("Big Star Density", Range(2, 100)) = 25
        _BigStarChance ("Big Star Chance", Range(0, 1)) = 0.08
        _BigStarSize ("Big Star Size", Range(0, 0.5)) = 0.12
        _BigStarBrightness ("Big Star Brightness", Range(0, 10)) = 3
        _MinStarPixels ("Min Star Size (pixels)", Range(0, 4)) = 1.5
        _StarSoftness ("Star Softness", Range(0, 1)) = 0.3
        _StarHorizonFade ("Star Horizon Fade", Range(0.001, 0.5)) = 0.15
        _TwinkleSpeed ("Twinkle Speed", Range(0, 10)) = 1.5
        _TwinkleAmount ("Twinkle Amount", Range(0, 1)) = 0.3

        [Header(Nebula)]
        [HDR] _NebulaColor ("Nebula Color", Color) = (0.05, 0.2, 0.08, 1)
        _NebulaIntensity ("Nebula Intensity", Range(0, 4)) = 1
        _NebulaScale ("Nebula Scale", Range(0.5, 10)) = 2.5
        _NebulaWidth ("Nebula Band Width", Range(0.05, 1)) = 0.35
        _NebulaTilt ("Nebula Band Tilt (deg)", Range(-90, 90)) = 60
        _NebulaContrast ("Nebula Contrast", Range(0.5, 4)) = 1.5

        [Header(Rotation)]
        _RotationSpeed ("Rotation Speed (deg per sec)", Float) = 0.5
        _RotationOffset ("Rotation Offset (deg)", Range(0, 360)) = 0
        _AxisTilt ("Rotation Axis Tilt (deg)", Range(0, 90)) = 20

        [Header(Output)]
        _Exposure ("Exposure", Range(0, 8)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Skybox"
        }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Stars live in a 3D grid of cells; each cell may hold one star.
            // Only stars within this radial distance of the sky sphere are visible.
            #define SLAB_HALF_THICKNESS 0.5
            // Stars are searched in neighbouring cells, so they must stay under one cell wide.
            #define MAX_STAR_RADIUS 0.9
            #define MIN_STAR_SIZE_SCALE 0.5
            #define MIN_STAR_BRIGHTNESS 0.3
            #define SMALL_STAR_SEED 0.0
            #define BIG_STAR_SEED 73.1
            #define STAR_POSITION_SEED_OFFSET 19.7

            #define NEBULA_OCTAVES 4
            #define FBM_START_AMPLITUDE 0.5
            #define FBM_LACUNARITY 2.0
            #define FBM_GAIN 0.5

            CBUFFER_START(UnityPerMaterial)
                float4 _ZenithColor;
                float4 _HorizonColor;
                float4 _GroundColor;
                float _HorizonOffset;
                float _GlowHeight;
                float _GlowFalloff;
                float _GroundBlend;

                float4 _StarColor;
                float4 _StarColorAlt;
                float _StarDensity;
                float _StarChance;
                float _StarSize;
                float _BigStarDensity;
                float _BigStarChance;
                float _BigStarSize;
                float _BigStarBrightness;
                float _MinStarPixels;
                float _StarSoftness;
                float _StarHorizonFade;
                float _TwinkleSpeed;
                float _TwinkleAmount;

                float4 _NebulaColor;
                float _NebulaIntensity;
                float _NebulaScale;
                float _NebulaWidth;
                float _NebulaTilt;
                float _NebulaContrast;

                float _RotationSpeed;
                float _RotationOffset;
                float _AxisTilt;

                float _Exposure;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 directionOS : TEXCOORD0;
            };

            // Hash functions (Dave Hoskins, "Hash without Sine").
            float3 Hash33(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);

                float n000 = Hash13(i + float3(0, 0, 0));
                float n100 = Hash13(i + float3(1, 0, 0));
                float n010 = Hash13(i + float3(0, 1, 0));
                float n110 = Hash13(i + float3(1, 1, 0));
                float n001 = Hash13(i + float3(0, 0, 1));
                float n101 = Hash13(i + float3(1, 0, 1));
                float n011 = Hash13(i + float3(0, 1, 1));
                float n111 = Hash13(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, u.x);
                float nx10 = lerp(n010, n110, u.x);
                float nx01 = lerp(n001, n101, u.x);
                float nx11 = lerp(n011, n111, u.x);
                float nxy0 = lerp(nx00, nx10, u.y);
                float nxy1 = lerp(nx01, nx11, u.y);
                return lerp(nxy0, nxy1, u.z);
            }

            float Fbm(float3 p)
            {
                float sum = 0.0;
                float amplitude = FBM_START_AMPLITUDE;
                [unroll]
                for (int i = 0; i < NEBULA_OCTAVES; i++)
                {
                    sum += amplitude * ValueNoise(p);
                    p *= FBM_LACUNARITY;
                    amplitude *= FBM_GAIN;
                }
                return sum;
            }

            // Rodrigues' rotation of v around unit axis k by angle (radians).
            float3 RotateAroundAxis(float3 v, float3 k, float angle)
            {
                float s;
                float c;
                sincos(angle, s, c);
                return v * c + cross(k, v) * s + k * dot(k, v) * (1.0 - c);
            }

            float3 StarLayer(float3 dir, float density, float chance, float size, float brightness, float seed)
            {
                float3 p = dir * density;
                float3 cell = floor(p);

                // Cell units covered by one screen pixel, so stars never shrink below a set pixel size.
                float pixelSize = length(fwidth(p));
                float minRadius = pixelSize * _MinStarPixels * 0.5;

                float3 result = 0;
                [unroll]
                for (int x = -1; x <= 1; x++)
                {
                    [unroll]
                    for (int y = -1; y <= 1; y++)
                    {
                        [unroll]
                        for (int z = -1; z <= 1; z++)
                        {
                            float3 c = cell + float3(x, y, z);
                            float3 h = Hash33(c + seed);
                            if (h.x > chance)
                            {
                                continue;
                            }

                            float3 starPos = c + Hash33(c + seed + STAR_POSITION_SEED_OFFSET);
                            float3 delta = starPos - p;
                            float radial = dot(delta, dir);
                            if (abs(radial) > SLAB_HALF_THICKNESS)
                            {
                                continue;
                            }

                            float tangential = length(delta - radial * dir);
                            float radius = min(max(size * lerp(MIN_STAR_SIZE_SCALE, 1.0, h.y), minRadius), MAX_STAR_RADIUS);
                            float softness = max(pixelSize, radius * _StarSoftness);
                            float shape = 1.0 - smoothstep(radius - softness, radius, tangential);

                            float twinkleWave = 0.5 + 0.5 * sin(_Time.y * _TwinkleSpeed + h.z * TWO_PI);
                            float twinkle = 1.0 - _TwinkleAmount * twinkleWave;
                            float3 color = lerp(_StarColor.rgb, _StarColorAlt.rgb, h.z);

                            result += color * shape * twinkle * brightness * lerp(MIN_STAR_BRIGHTNESS, 1.0, h.y);
                        }
                    }
                }
                return result;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.directionOS = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.directionOS);

                // Gradient stays fixed to the horizon; only stars and nebula rotate.
                float height = dir.y - _HorizonOffset;
                float glowT = saturate(height / _GlowHeight);
                float3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(glowT, _GlowFalloff));
                float groundT = saturate(-height / _GroundBlend);
                sky = lerp(sky, _GroundColor.rgb, groundT);

                float aboveHorizon = smoothstep(0.0, _StarHorizonFade, height);

                float tilt = radians(_AxisTilt);
                float3 axis = float3(sin(tilt), cos(tilt), 0.0);
                float angle = radians(_RotationOffset + _Time.y * _RotationSpeed);
                float3 skyDir = RotateAroundAxis(dir, axis, angle);

                float nebulaTilt = radians(_NebulaTilt);
                float3 bandNormal = float3(0.0, cos(nebulaTilt), sin(nebulaTilt));
                float bandDistance = dot(skyDir, bandNormal) / _NebulaWidth;
                float band = exp(-bandDistance * bandDistance);
                float nebula = pow(saturate(Fbm(skyDir * _NebulaScale)), _NebulaContrast) * band;
                sky += _NebulaColor.rgb * nebula * _NebulaIntensity * aboveHorizon;

                float3 stars = StarLayer(skyDir, _StarDensity, _StarChance, _StarSize, 1.0, SMALL_STAR_SEED)
                             + StarLayer(skyDir, _BigStarDensity, _BigStarChance, _BigStarSize, _BigStarBrightness, BIG_STAR_SEED);
                sky += stars * aboveHorizon;

                return half4(sky * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
