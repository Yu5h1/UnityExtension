Shader "UnifiedSolver/NetRenderer"
{
    Properties
    {
        _Color ("Net Color", Color) = (0.12, 0.12, 0.12, 1.0)
        [NoScaleOffset] _NetMap ("Net Pattern (RGB + Alpha)", 2D) = "white" {}
        [Toggle] _UseNetTexture ("Use Net Texture", Float) = 0
        [Enum(Square, 0, Diamond, 1)] _NetPattern ("Net Pattern", Float) = 1
        _NetTiling ("Net Tiling (X, Y)", Vector) = (20, 20, 0, 0)
        _ThreadWidth ("Procedural Thread Width", Range(0.01, 0.45)) = 0.08
        _Cutoff ("Alpha Cutoff", Range(0.01, 0.99)) = 0.5
        _BorderColor ("Border Color", Color) = (0.01, 0.01, 0.01, 1.0)
        _BorderWidth ("Border Width (UV)", Range(0.001, 0.25)) = 0.025
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
        }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #include "UnityCG.cginc"

            // Must match ParticleGPU / Particle struct byte-for-byte (60 bytes).
            struct Particle
            {
                float3 position;
                float3 velocity;
                float3 prevPosition;
                float  invMass;
                int    phase;
                float3 color;
                uint   visible;
            };

            StructuredBuffer<Particle> _Particles;
            int _ParticleOffset;
            int _ResolutionX;
            int _ResolutionY;
            float4 _Color;
            sampler2D _NetMap;
            float _UseNetTexture;
            float _NetPattern;
            float4 _NetTiling;
            float _ThreadWidth;
            float _Cutoff;
            float4 _BorderColor;
            float _BorderWidth;
            float _Metallic;
            float _Smoothness;

            struct appdata
            {
                uint vertexID : SV_VertexID;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 normal   : TEXCOORD0;
                float2 uv       : TEXCOORD1;
                float3 color    : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
            };

            // Fetch world position of a grid particle by its flat index.
            float3 GetPos(int flatIndex)
            {
                return _Particles[_ParticleOffset + flatIndex].position;
            }

            float ProceduralNetMask(float2 uv)
            {
                // Rotate the repeating coordinate system by 45 degrees for
                // a diamond fishing-net pattern. The square mode preserves
                // the original horizontal/vertical weave.
                if (_NetPattern > 0.5)
                {
                    float2 centered = uv - 0.5;
                    uv = float2(
                        centered.x + centered.y,
                        centered.x - centered.y) * 0.70710678 + 0.5;
                }

                float2 tiling = max(
                    abs(_NetTiling.xy),
                    float2(0.0001, 0.0001));
                float2 tiled = uv * tiling;
                float2 edgeDistance = min(
                    frac(tiled),
                    1.0 - frac(tiled));
                float2 antialiasWidth = max(
                    fwidth(tiled),
                    float2(0.0001, 0.0001));
                float2 thread = 1.0 - smoothstep(
                    _ThreadWidth,
                    _ThreadWidth + antialiasWidth,
                    edgeDistance);
                return max(thread.x, thread.y);
            }

            float BorderMask(float2 uv)
            {
                float edgeDistance = min(
                    min(uv.x, 1.0 - uv.x),
                    min(uv.y, 1.0 - uv.y));
                float antialiasWidth = max(
                    fwidth(edgeDistance),
                    0.0001);
                return 1.0 - smoothstep(
                    _BorderWidth,
                    _BorderWidth + antialiasWidth,
                    edgeDistance);
            }

            v2f vert(appdata v)
            {
                v2f o;

                int idx = (int)v.vertexID;
                float3 pos = GetPos(idx);

                // Compute normal from grid neighbors via finite differences.
                int x = idx % _ResolutionX;
                int z = idx / _ResolutionX;

                // Horizontal tangent
                float3 ddx;
                if (x > 0 && x < _ResolutionX - 1)
                    ddx = GetPos(idx + 1) - GetPos(idx - 1);
                else if (x > 0)
                    ddx = pos - GetPos(idx - 1);
                else
                    ddx = GetPos(idx + 1) - pos;

                // Vertical tangent
                float3 ddz;
                if (z > 0 && z < _ResolutionY - 1)
                    ddz = GetPos(idx + _ResolutionX) - GetPos(idx - _ResolutionX);
                else if (z > 0)
                    ddz = pos - GetPos(idx - _ResolutionX);
                else
                    ddz = GetPos(idx + _ResolutionX) - pos;

                float3 normal = normalize(cross(ddz, ddx));

                o.pos      = UnityWorldToClipPos(float4(pos, 1.0));
                o.normal   = normal;
                o.uv       = v.uv;
                o.color    = _Particles[_ParticleOffset + idx].color;
                o.worldPos = pos;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 tiledUv =
                    i.uv * max(
                        abs(_NetTiling.xy),
                        float2(0.0001, 0.0001));
                float4 netTexture = tex2D(_NetMap, tiledUv);
                float netMask = lerp(
                    ProceduralNetMask(i.uv),
                    netTexture.a,
                    saturate(_UseNetTexture));
                float border = BorderMask(i.uv);
                float coverage = max(netMask, border);
                clip(coverage - _Cutoff);

                // Two-sided lighting: flip normal for backfaces.
                float3 normal  = normalize(i.normal);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                if (dot(normal, viewDir) < 0) normal = -normal;

                float3 lightDir = normalize(float3(0.5, 1.0, 0.3));
                float  ndl      = saturate(dot(normal, lightDir));

                // Diffuse
                float3 netAlbedo =
                    _Color.rgb *
                    lerp(
                        float3(1.0, 1.0, 1.0),
                        netTexture.rgb,
                        saturate(_UseNetTexture));
                float3 albedo = lerp(
                    netAlbedo,
                    _BorderColor.rgb,
                    border);
                float3 diffuse  = albedo * (0.3 + 0.7 * ndl);

                // Specular (Blinn-Phong approximation for metallic/smoothness)
                float3 halfVec  = normalize(lightDir + viewDir);
                float  ndh      = saturate(dot(normal, halfVec));
                float  specPow  = exp2(10.0 * _Smoothness + 1.0);
                float  spec     = pow(ndh, specPow) * _Smoothness;

                // Metallic surfaces tint specular with albedo, non-metallic use white.
                float3 specColor = lerp(float3(0.04, 0.04, 0.04), albedo, _Metallic);
                // Metallic surfaces darken diffuse.
                diffuse *= (1.0 - _Metallic);

                float3 col = diffuse + specColor * spec;
                return float4(col, coverage);
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct Particle
            {
                float3 position;
                float3 velocity;
                float3 prevPosition;
                float  invMass;
                int    phase;
                float3 color;
                uint   visible;
            };

            StructuredBuffer<Particle> _Particles;
            int _ParticleOffset;
            sampler2D _NetMap;
            float _UseNetTexture;
            float _NetPattern;
            float4 _NetTiling;
            float _ThreadWidth;
            float _Cutoff;
            float _BorderWidth;

            struct v2f
            {
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
            };

            float ProceduralNetMask(float2 uv)
            {
                if (_NetPattern > 0.5)
                {
                    float2 centered = uv - 0.5;
                    uv = float2(
                        centered.x + centered.y,
                        centered.x - centered.y) * 0.70710678 + 0.5;
                }

                float2 tiling = max(
                    abs(_NetTiling.xy),
                    float2(0.0001, 0.0001));
                float2 tiled = uv * tiling;
                float2 edgeDistance = min(
                    frac(tiled),
                    1.0 - frac(tiled));
                float2 antialiasWidth = max(
                    fwidth(tiled),
                    float2(0.0001, 0.0001));
                float2 thread = 1.0 - smoothstep(
                    _ThreadWidth,
                    _ThreadWidth + antialiasWidth,
                    edgeDistance);
                return max(thread.x, thread.y);
            }

            float BorderMask(float2 uv)
            {
                float edgeDistance = min(
                    min(uv.x, 1.0 - uv.x),
                    min(uv.y, 1.0 - uv.y));
                float antialiasWidth = max(
                    fwidth(edgeDistance),
                    0.0001);
                return 1.0 - smoothstep(
                    _BorderWidth,
                    _BorderWidth + antialiasWidth,
                    edgeDistance);
            }

            v2f vert(appdata_base v, uint vertexID : SV_VertexID)
            {
                float3 worldPos = _Particles[_ParticleOffset + (int)vertexID].position;
                v.vertex = float4(worldPos, 1.0);

                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = v.texcoord.xy;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 tiledUv =
                    i.uv * max(
                        abs(_NetTiling.xy),
                        float2(0.0001, 0.0001));
                float textureMask = tex2D(_NetMap, tiledUv).a;
                float netMask = lerp(
                    ProceduralNetMask(i.uv),
                    textureMask,
                    saturate(_UseNetTexture));
                float coverage = max(
                    netMask,
                    BorderMask(i.uv));
                clip(coverage - _Cutoff);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
