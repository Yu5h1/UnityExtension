// A mesh bent along the head, middle and tail frames of a body, one instance per body
// (ParticleRendering REND-05). Ported from Unified-Solver SolverArticulatedMesh.shader: the frames
// come from ParticleBodyFrame.hlsl, the same code the behaviour kernels use, each vertex blends
// the tail and middle frames or the middle and head frames by smoothstep of its position along the
// forward axis, and a lifecycle hidden fraction collapses the mesh onto the middle frame centre.
// _BodyIndices maps the instance number to the body number in _Bodies and _Lifecycle. Colour is
// texture * appearance colour (_Color) * body colour. Differences from the original: the base
// dimensions come from each body (dimensions.xyz) instead of one uniform, and the fit and visual
// scale are applied here from the mesh size.
Shader "Yu5h1Lib/Particles/ArticulatedMesh"
{
    Properties
    {
        [HideInInspector] _ParticleDrawKind ("Particle draw kind", Float) = 3
        _BaseMap ("Base Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        CGINCLUDE
        #include "UnityCG.cginc"
        #define PARTICLE_WORLD_READ_ONLY
        #include "Packages/com.yu5h1.motion/Runtime/Particles/Shaders/ParticleWorldData.hlsl"
        #include "Packages/com.yu5h1.motion/Runtime/Particles/Shaders/ParticleBodyFrame.hlsl"
        #include "Packages/com.yu5h1.motion/Runtime/Particles/Rendering/Shaders/ParticleDrawCommon.hlsl"

        StructuredBuffer<ParticleBody> _Bodies;
        StructuredBuffer<uint> _BodyIndices;
        StructuredBuffer<float4> _Lifecycle;
        uint _HasLifecycle;

        float3 _MeshCenter;
        float3 _MeshSize;
        float3 _VisualScale;
        float _FitToDimensions;
        int _MeshForwardAxis;
        float _MeshAxisMin;
        float _MeshAxisLength;
        float _MeshFlipForward;
        sampler2D _BaseMap;
        float4 _BaseMap_ST;
        float4 _Color;

        float AxisCoordinate(float3 value)
        {
            if (_MeshForwardAxis == 0)
                return value.x;
            if (_MeshForwardAxis == 1)
                return value.y;
            return value.z;
        }

        float3 MeshToCanonical(float3 value)
        {
            if (_MeshForwardAxis == 0)
                return float3(value.y, value.x, value.z);
            if (_MeshForwardAxis == 1)
                return value;
            return float3(value.x, value.z, value.y);
        }

        float3 CanonicalScaleToMesh(float3 value)
        {
            if (_MeshForwardAxis == 0)
                return float3(value.y, value.x, value.z);
            if (_MeshForwardAxis == 1)
                return value;
            return float3(value.x, value.z, value.y);
        }

        float3 BaseVisualScale(float3 dimensions)
        {
            if (_FitToDimensions < 0.5)
                return _VisualScale;
            float3 target = CanonicalScaleToMesh(dimensions);
            return target / max(_MeshSize, float3(0.000001, 0.000001, 0.000001)) * _VisualScale;
        }

        float3 FramePosition(float3 localPosition, float restCenter, BodyFrame frame)
        {
            return frame.center +
                frame.side * localPosition.x +
                frame.tangent * (localPosition.y - restCenter) +
                frame.normal * localPosition.z;
        }

        float3 FrameNormal(float3 localNormal, BodyFrame frame)
        {
            return normalize(
                frame.side * localNormal.x +
                frame.tangent * localNormal.y +
                frame.normal * localNormal.z);
        }

        void DeformVertex(
            float3 meshPosition,
            float3 meshNormal,
            uint instanceIndex,
            out float3 worldPosition,
            out float3 worldNormal,
            out float4 color)
        {
            uint bodyIndex = _BodyIndices[instanceIndex];
            ParticleBody body = _Bodies[bodyIndex];
            BodyFrame head = GetBodyFrame(body, 0u);
            BodyFrame middle = GetBodyFrame(body, 1u);
            BodyFrame tail = GetBodyFrame(body, 2u);

            float3 meshScale = CanonicalScaleToMesh(body.scale.xyz);
            float3 baseVisualScale = BaseVisualScale(body.dimensions.xyz);
            float3 localPosition = MeshToCanonical(
                (meshPosition - _MeshCenter) * baseVisualScale * meshScale);
            float3 localNormal = normalize(MeshToCanonical(
                meshNormal / max(abs(baseVisualScale * meshScale), float3(0.0001, 0.0001, 0.0001))));
            float longitudinal = saturate(
                (AxisCoordinate(meshPosition) - _MeshAxisMin) / max(_MeshAxisLength, 0.000001));
            if (_MeshFlipForward > 0.5)
            {
                longitudinal = 1.0 - longitudinal;
                localPosition.xy = -localPosition.xy;
                localNormal.xy = -localNormal.xy;
            }
            float spacing = body.topology == BODY_SINGLE
                ? 0.0
                : body.dimensions.y * body.scale.y * (body.topology == BODY_ARTICULATED_12 ? 0.333333333 : 0.5);

            if (longitudinal < 0.5)
            {
                float blend = smoothstep(0.0, 1.0, longitudinal * 2.0);
                worldPosition = lerp(
                    FramePosition(localPosition, -spacing, tail),
                    FramePosition(localPosition, 0.0, middle),
                    blend);
                worldNormal = normalize(lerp(
                    FrameNormal(localNormal, tail),
                    FrameNormal(localNormal, middle),
                    blend));
            }
            else
            {
                float blend = smoothstep(0.0, 1.0, (longitudinal - 0.5) * 2.0);
                worldPosition = lerp(
                    FramePosition(localPosition, 0.0, middle),
                    FramePosition(localPosition, spacing, head),
                    blend);
                worldNormal = normalize(lerp(
                    FrameNormal(localNormal, middle),
                    FrameNormal(localNormal, head),
                    blend));
            }

            float visible = _HasLifecycle != 0u ? saturate(1.0 - _Lifecycle[bodyIndex].z) : 1.0;
            worldPosition = lerp(middle.center, worldPosition, visible);
            color = BodyColor(body);
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
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                PARTICLE_VERTEX_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float4 color : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                PARTICLE_SETUP_INSTANCE(v);
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 worldPosition;
                float3 worldNormal;
                float4 color;
                DeformVertex(v.vertex.xyz, v.normal, PARTICLE_INSTANCE_INDEX(v), worldPosition, worldNormal, color);
                o.pos = UnityWorldToClipPos(float4(worldPosition, 1.0));
                o.normal = worldNormal;
                o.color = color;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float diffuse = saturate(dot(normalize(i.normal), normalize(float3(0.5, 1.0, 0.3))));
                float lighting = 0.3 + 0.7 * diffuse;
                float4 baseColor = tex2D(_BaseMap, i.uv) * _Color;
                return baseColor * float4(i.color.rgb * lighting, 1.0);
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

                float3 worldPosition;
                float3 worldNormal;
                float4 color;
                DeformVertex(v.vertex.xyz, v.normal, PARTICLE_INSTANCE_INDEX(v), worldPosition, worldNormal, color);
                v.vertex = float4(worldPosition, 1.0);
                v.normal = worldNormal;
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
