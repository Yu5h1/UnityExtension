// Net and cloth surface drawn from the particles of a grid entity (ParticleRendering REND-03).
// Ported from Unified-Solver NetRenderer.shader: one vertex per particle, normals from the
// neighbours, two-sided lighting with the fixed light direction (0.5, 1, 0.3), 0.3 + 0.7 N.L
// diffuse plus Blinn-Phong. The mesh is a grid whose vertex number is the particle number inside
// the entity; _EntityStarts maps the instance number to the entity first particle. Appearance
// values (colour, net pattern, thread width, ...) arrive through the property block of
// PhysicsParticleRenderer, so the material only chooses the shader and the net map texture.
Shader "Yu5h1Lib/Particles/GridSurface"
{
    Properties
    {
        [HideInInspector] _ParticleDrawKind ("Particle draw kind", Float) = 1
        [NoScaleOffset] _NetMap ("Net Pattern (RGB + Alpha)", 2D) = "white" {}
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

        CGINCLUDE
        #include "UnityCG.cginc"
        #define PARTICLE_WORLD_READ_ONLY
        #include "Packages/com.yu5h1.motion/Runtime/Particles/Shaders/ParticleWorldData.hlsl"
        #include "Packages/com.yu5h1.motion/Runtime/Particles/Rendering/Shaders/ParticleDrawCommon.hlsl"

        StructuredBuffer<uint> _EntityStarts;
        uint _GridColumns;
        uint _GridRows;
        float4 _Color;
        sampler2D _NetMap;
        float _NetEnabled;
        float _UseNetTexture;
        float _NetPattern;
        float4 _NetTiling;
        float _ThreadWidth;
        float _Cutoff;
        float4 _BorderColor;
        float _BorderWidth;
        float _Metallic;
        float _Smoothness;

        float3 GridPosition(uint start, uint flat)
        {
            return _Position[start + flat].xyz;
        }

        float ProceduralNetMask(float2 uv)
        {
            if (_NetPattern > 0.5)
            {
                float2 centered = uv - 0.5;
                uv = float2(
                    centered.x + centered.y,
                    centered.x - centered.y) * 0.70710678 + 0.5;
            }

            float2 tiling = max(abs(_NetTiling.xy), float2(0.0001, 0.0001));
            float2 tiled = uv * tiling;
            float2 edgeDistance = min(frac(tiled), 1.0 - frac(tiled));
            float2 antialiasWidth = max(fwidth(tiled), float2(0.0001, 0.0001));
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
            float antialiasWidth = max(fwidth(edgeDistance), 0.0001);
            return 1.0 - smoothstep(
                _BorderWidth,
                _BorderWidth + antialiasWidth,
                edgeDistance);
        }

        float NetCoverage(float2 uv, float netAlpha, out float border)
        {
            float netMask = lerp(
                ProceduralNetMask(uv),
                netAlpha,
                saturate(_UseNetTexture));
            border = BorderMask(uv);
            return max(netMask, border);
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile_instancing

            struct appdata
            {
                uint vertexID : SV_VertexID;
                float2 uv : TEXCOORD0;
                PARTICLE_VERTEX_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                PARTICLE_SETUP_INSTANCE(v);
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                uint start = _EntityStarts[PARTICLE_INSTANCE_INDEX(v)];
                uint idx = v.vertexID;
                uint x = idx % _GridColumns;
                uint z = idx / _GridColumns;
                float3 pos = GridPosition(start, idx);

                float3 tangentX;
                if (x > 0u && x < _GridColumns - 1u)
                    tangentX = GridPosition(start, idx + 1u) - GridPosition(start, idx - 1u);
                else if (x > 0u)
                    tangentX = pos - GridPosition(start, idx - 1u);
                else
                    tangentX = GridPosition(start, idx + 1u) - pos;

                float3 tangentZ;
                if (z > 0u && z < _GridRows - 1u)
                    tangentZ = GridPosition(start, idx + _GridColumns) - GridPosition(start, idx - _GridColumns);
                else if (z > 0u)
                    tangentZ = pos - GridPosition(start, idx - _GridColumns);
                else
                    tangentZ = GridPosition(start, idx + _GridColumns) - pos;

                o.pos = UnityWorldToClipPos(float4(pos, 1.0));
                o.normal = normalize(cross(tangentZ, tangentX));
                o.uv = v.uv;
                o.worldPos = pos;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float net = step(0.5, _NetEnabled);
                float2 tiledUv = i.uv * max(abs(_NetTiling.xy), float2(0.0001, 0.0001));
                float4 netTexture = tex2D(_NetMap, tiledUv);
                float border = 0.0;
                float coverage = 1.0;
                if (net > 0.5)
                {
                    coverage = NetCoverage(i.uv, netTexture.a, border);
                    clip(coverage - _Cutoff);
                }

                float3 normal = normalize(i.normal);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                if (dot(normal, viewDir) < 0)
                    normal = -normal;

                float3 lightDir = normalize(float3(0.5, 1.0, 0.3));
                float ndl = saturate(dot(normal, lightDir));

                float3 netAlbedo = _Color.rgb * lerp(
                    float3(1.0, 1.0, 1.0),
                    netTexture.rgb,
                    saturate(_UseNetTexture) * net);
                float3 albedo = lerp(netAlbedo, _BorderColor.rgb, border);
                float3 diffuse = albedo * (0.3 + 0.7 * ndl);

                float3 halfVec = normalize(lightDir + viewDir);
                float ndh = saturate(dot(normal, halfVec));
                float specPow = exp2(10.0 * _Smoothness + 1.0);
                float spec = pow(ndh, specPow) * _Smoothness;

                float3 specColor = lerp(float3(0.04, 0.04, 0.04), albedo, _Metallic);
                diffuse *= (1.0 - _Metallic);

                return float4(diffuse + specColor * spec, coverage);
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
            #pragma target 4.5
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing

            struct appdataShadow
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                uint vertexID : SV_VertexID;
                PARTICLE_VERTEX_INSTANCE_ID
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdataShadow v)
            {
                PARTICLE_SETUP_INSTANCE(v);
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                uint start = _EntityStarts[PARTICLE_INSTANCE_INDEX(v)];
                v.vertex = float4(GridPosition(start, v.vertexID), 1.0);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                if (_NetEnabled > 0.5)
                {
                    float2 tiledUv = i.uv * max(abs(_NetTiling.xy), float2(0.0001, 0.0001));
                    float border;
                    float coverage = NetCoverage(i.uv, tex2D(_NetMap, tiledUv).a, border);
                    clip(coverage - _Cutoff);
                }
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
