// Camera-facing strip drawn along the particles of a chain entity (ParticleRendering REND-04).
// Ported from the RopeRenderer.shader of the third-party solver: two vertices per particle, the
// tangent is the central difference of the neighbours (one-sided at the ends), the strip opens
// along cross(tangent, view) by _StripWidth on each side of the centre line, and the fragment is a
// single colour darkened towards the edges (1 - edge^2 * 0.4), unlit and two-sided. The shadow
// pass is an addition; PhysicsParticleRenderer only draws it when the appearance casts shadows.
// The camera position is the one of the current eye, so Single Pass Instanced draws face each eye.
Shader "Yu5h1Lib/Particles/ChainStrip"
{
    Properties
    {
        [HideInInspector] _ParticleDrawKind ("Particle draw kind", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Cull Off

        CGINCLUDE
        #include "UnityCG.cginc"
        #define PARTICLE_WORLD_READ_ONLY
        #include "Packages/com.yu5h1.motion/Runtime/Particles/Shaders/ParticleWorldData.hlsl"
        #include "Packages/com.yu5h1.motion/Runtime/Particles/Rendering/Shaders/ParticleDrawCommon.hlsl"

        StructuredBuffer<uint> _EntityStarts;
        uint _ChainCount;
        float _StripWidth;
        float4 _Color;

        float3 ChainPosition(uint start, uint index)
        {
            return _Position[start + index].xyz;
        }

        float3 StripVertex(uint start, uint vertexID)
        {
            uint count = max(_ChainCount, 1u);
            uint seg = min(vertexID / 2u, count - 1u);
            float side = (vertexID % 2u) == 0u ? -1.0 : 1.0;
            float3 pos = ChainPosition(start, seg);

            float3 tangent;
            if (count < 2u)
                tangent = float3(0, 1, 0);
            else if (seg == 0u)
                tangent = ChainPosition(start, 1u) - pos;
            else if (seg == count - 1u)
                tangent = pos - ChainPosition(start, seg - 1u);
            else
                tangent = ChainPosition(start, seg + 1u) - ChainPosition(start, seg - 1u);

            float tangentLength = length(tangent);
            tangent = tangentLength > 1e-6 ? tangent / tangentLength : float3(0, 1, 0);

            float3 viewDir = normalize(_WorldSpaceCameraPos - pos);
            float3 offset = normalize(cross(tangent, viewDir)) * _StripWidth;
            return pos + offset * side;
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
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                PARTICLE_SETUP_INSTANCE(v);
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                uint start = _EntityStarts[PARTICLE_INSTANCE_INDEX(v)];
                o.pos = UnityWorldToClipPos(float4(StripVertex(start, v.vertexID), 1.0));
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float edge = abs(i.uv.x - 0.5) * 2.0;
                float shade = 1.0 - edge * edge * 0.4;
                return float4(_Color.rgb * shade, 1.0);
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
                uint vertexID : SV_VertexID;
                PARTICLE_VERTEX_INSTANCE_ID
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdataShadow v)
            {
                PARTICLE_SETUP_INSTANCE(v);
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                uint start = _EntityStarts[PARTICLE_INSTANCE_INDEX(v)];
                v.vertex = float4(StripVertex(start, v.vertexID), 1.0);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
