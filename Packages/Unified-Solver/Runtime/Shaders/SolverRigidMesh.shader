Shader "Yu5h1/UnifiedSolver/RigidMesh"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        CGINCLUDE
        #include "UnityCG.cginc"

        struct Particle
        {
            float3 position;
            float3 velocity;
            float3 prevPosition;
            float invMass;
            int phase;
            float3 color;
            uint visible;
        };

        struct RigidBody
        {
            int particleOffset;
            int particleCount;
            float4 quaternion;
            float3 xcm;
        };

        struct SolverInstance
        {
            int particleOffset;
            int particleCount;
            int constraintOffset;
            int constraintCount;
            int rigidBodyOffset;
            int rigidBodyCount;
            int topology;
            int profileId;
            float3 scale;
            float padding;
            float4 spawnRotation;
        };

        StructuredBuffer<Particle> _Particles;
        StructuredBuffer<RigidBody> _RigidBodies;
        StructuredBuffer<int> _RigidParticleIndices;
        StructuredBuffer<SolverInstance> _Instances;
        float3 _MeshCenter;
        float3 _BaseVisualScale;
        int _MeshForwardAxis;
        sampler2D _BaseMap;
        float4 _BaseMap_ST;
        float4 _Tint;

        float4 QuatMul(float4 a, float4 b)
        {
            return float4(
                a.w * b.x + a.x * b.w +
                    a.y * b.z - a.z * b.y,
                a.w * b.y - a.x * b.z +
                    a.y * b.w + a.z * b.x,
                a.w * b.z + a.x * b.y -
                    a.y * b.x + a.z * b.w,
                a.w * b.w - a.x * b.x -
                    a.y * b.y - a.z * b.z);
        }

        float3 RotateByQuaternion(
            float3 value,
            float4 rotation)
        {
            float3 t =
                2.0 * cross(rotation.xyz, value);
            return value +
                rotation.w * t +
                cross(rotation.xyz, t);
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

        void DeformVertex(
            float3 meshPosition,
            float3 meshNormal,
            uint instanceID,
            out float3 worldPosition,
            out float3 worldNormal,
            out float3 color)
        {
            SolverInstance instance =
                _Instances[instanceID];
            RigidBody body =
                _RigidBodies[
                    instance.rigidBodyOffset];
            float4 worldRotation = normalize(
                QuatMul(
                    body.quaternion,
                    instance.spawnRotation));
            float3 meshScale =
                CanonicalScaleToMesh(instance.scale);
            float3 localPosition =
                MeshToCanonical(
                    (meshPosition - _MeshCenter) *
                    _BaseVisualScale *
                    meshScale);
            float3 localNormal =
                normalize(
                    MeshToCanonical(
                        meshNormal /
                        max(
                            abs(
                                _BaseVisualScale *
                                meshScale),
                            float3(
                                0.0001,
                                0.0001,
                                0.0001))));

            worldPosition =
                body.xcm +
                RotateByQuaternion(
                    localPosition,
                    worldRotation);
            worldNormal =
                RotateByQuaternion(
                    localNormal,
                    worldRotation);
            int firstParticle =
                _RigidParticleIndices[
                    body.particleOffset];
            color =
                _Particles[firstParticle].color;
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 color : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            v2f vert(
                appdata v,
                uint instanceID : SV_InstanceID)
            {
                v2f o;
                float3 worldPosition;
                float3 worldNormal;
                float3 color;
                DeformVertex(
                    v.vertex.xyz,
                    v.normal,
                    instanceID,
                    worldPosition,
                    worldNormal,
                    color);
                o.pos = UnityWorldToClipPos(
                    float4(worldPosition, 1.0));
                o.normal = worldNormal;
                o.color = color;
                o.uv =
                    TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float diffuse = saturate(
                    dot(
                        normalize(i.normal),
                        normalize(
                            float3(0.5, 1.0, 0.3))));
                float lighting =
                    0.3 + 0.7 * diffuse;
                float4 baseColor =
                    tex2D(_BaseMap, i.uv) *
                    _Tint;
                return baseColor *
                    float4(
                        i.color * lighting,
                        1.0);
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma target 5.0
            #pragma multi_compile_shadowcaster

            struct v2fShadow
            {
                V2F_SHADOW_CASTER;
            };

            v2fShadow vertShadow(
                appdata_base v,
                uint instanceID : SV_InstanceID)
            {
                v2fShadow o;
                float3 worldPosition;
                float3 worldNormal;
                float3 color;
                DeformVertex(
                    v.vertex.xyz,
                    v.normal,
                    instanceID,
                    worldPosition,
                    worldNormal,
                    color);
                v.vertex =
                    float4(worldPosition, 1.0);
                v.normal = worldNormal;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 fragShadow(
                v2fShadow i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
