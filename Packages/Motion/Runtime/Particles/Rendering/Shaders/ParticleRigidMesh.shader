// Default shader for rigid fragments drawn at their rigid group pose (ParticleRendering REND-06).
// The object matrix of every instance comes from ParticleRigidInstancing.hlsl, so one instanced
// draw per mesh variant poses all fragments on the GPU. Shading is the fixed light direction
// (0.5, 1, 0.3) with 0.3 + 0.7 N.L, like the other Motion drawing shaders; the colour is the
// texture times the appearance colour (_Color). A URP Shader Graph can replace it by calling the
// same include from a Custom Function.
Shader "Yu5h1Lib/Particles/RigidMesh"
{
    Properties
    {
        [HideInInspector] _ParticleDrawKind ("Particle draw kind", Float) = 4
        _BaseMap ("Base Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        CGINCLUDE
        #include "UnityCG.cginc"
        #include "Packages/com.yu5h1.motion/Runtime/Particles/Rendering/Shaders/ParticleDrawCommon.hlsl"
        #include "Packages/com.yu5h1.motion/Runtime/Particles/Rendering/Shaders/ParticleRigidInstancing.hlsl"

        sampler2D _BaseMap;
        float4 _BaseMap_ST;
        float4 _Color;
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleRigidInstancingSetup

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                PARTICLE_VERTEX_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                PARTICLE_SETUP_INSTANCE(v);
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float diffuse = saturate(dot(normalize(i.normal), normalize(float3(0.5, 1.0, 0.3))));
                float lighting = 0.3 + 0.7 * diffuse;
                float4 baseColor = tex2D(_BaseMap, i.uv) * _Color;
                return float4(baseColor.rgb * lighting, 1.0);
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma target 4.5
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleRigidInstancingSetup

            struct appdataShadow
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                PARTICLE_VERTEX_INSTANCE_ID
            };

            struct v2fShadow
            {
                V2F_SHADOW_CASTER;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2fShadow vertShadow(appdataShadow v)
            {
                PARTICLE_SETUP_INSTANCE(v);
                v2fShadow o;
                UNITY_INITIALIZE_OUTPUT(v2fShadow, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 fragShadow(v2fShadow i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
