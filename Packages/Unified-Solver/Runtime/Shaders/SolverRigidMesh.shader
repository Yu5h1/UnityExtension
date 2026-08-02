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

        // Hull mode only. Rest offsets q_i = x_i0 - x_cm0 come straight from the
        // solver's own shape-matching buffer, indexed in parallel with
        // _RigidParticleIndices, so a fragment's corners need no storage of their
        // own. _VariantInstances remaps the batch-local instance id, because one
        // draw covers only the instances of a single 4/6/8 variant.
        StructuredBuffer<float3> _RigidRestOffsets;
        StructuredBuffer<int> _VariantInstances;
        int _VariantOffset;
        float _ParticleRadius;

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

        // Vertex built from the body's own rest particles instead of an authored
        // mesh.
        //
        // The drawn surface is then the convex hull of the very points the
        // solver collides with, so the two cannot drift apart however the
        // fragment was generated, and a randomly shaped fragment needs no mesh
        // asset at all.
        //
        // cornerIndices arrives in UV1 and holds indices, not a direction: x is
        // this vertex's own corner and yz are the other two corners of its face,
        // wound outward. A flat facet has no single vertex normal to
        // interpolate, and those three corners are exactly what is needed to
        // compute the facet's own.
        //
        // The rest offsets already include the spawn rotation and the spawn
        // scale, since they were captured from world positions at spawn. Applying
        // instance.spawnRotation or instance.scale here would apply both twice.
        void DeformHullVertex(
            float3 cornerIndices,
            uint instanceID,
            out float3 worldPosition,
            out float3 worldNormal,
            out float3 color)
        {
            uint index =
                (uint)_VariantInstances[
                    (uint)_VariantOffset + instanceID];
            SolverInstance instance =
                _Instances[index];
            RigidBody body =
                _RigidBodies[
                    instance.rigidBodyOffset];
            float4 rotation =
                normalize(body.quaternion);

            uint slot =
                (uint)body.particleOffset;
            float3 own =
                _RigidRestOffsets[
                    slot + (uint)cornerIndices.x];
            float3 nextCorner =
                _RigidRestOffsets[
                    slot + (uint)cornerIndices.y];
            float3 lastCorner =
                _RigidRestOffsets[
                    slot + (uint)cornerIndices.z];

            // The hull passes through particle centres, but the fragment
            // collides as the union of spheres of _ParticleRadius around them,
            // so drawn as-is it reads a radius smaller than it behaves. Pushing
            // the corners out radially from the centre of mass is the cheap
            // approximation of that union and needs no extra data.
            float ownLength = length(own);
            float3 inflated =
                ownLength > 1e-6
                    ? own *
                      ((ownLength + _ParticleRadius) /
                       ownLength)
                    : own;

            float3 faceNormal = cross(
                nextCorner - own,
                lastCorner - own);
            float faceLength = length(faceNormal);
            float3 localNormal =
                faceLength > 1e-9
                    ? faceNormal / faceLength
                    : normalize(
                        ownLength > 1e-6
                            ? own
                            : float3(0.0, 1.0, 0.0));

            worldPosition =
                body.xcm +
                RotateByQuaternion(
                    inflated,
                    rotation);
            worldNormal = RotateByQuaternion(
                localNormal,
                rotation);
            int firstParticle =
                _RigidParticleIndices[
                    body.particleOffset];
            color =
                _Particles[firstParticle].color;
        }

        void DeformVertex(
            float3 meshPosition,
            float3 meshNormal,
            float3 cornerIndices,
            uint instanceID,
            out float3 worldPosition,
            out float3 worldNormal,
            out float3 color)
        {
        #ifdef SOLVER_HULL_FROM_PARTICLES
            DeformHullVertex(
                cornerIndices,
                instanceID,
                worldPosition,
                worldNormal,
                color);
            return;
        #else
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
        #endif
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #pragma multi_compile_local _ SOLVER_HULL_FROM_PARTICLES

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float3 corners : TEXCOORD1;
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
                    v.corners,
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
            #pragma multi_compile_local _ SOLVER_HULL_FROM_PARTICLES
            #pragma multi_compile_shadowcaster

            struct v2fShadow
            {
                V2F_SHADOW_CASTER;
            };

            // Not appdata_base: hull mode needs the corner indices out of UV1,
            // and appdata_base stops at one texcoord. The shadow caster macros
            // only require vertex and normal to be present by those names.
            struct appdataShadow
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 corners : TEXCOORD1;
            };

            v2fShadow vertShadow(
                appdataShadow v,
                uint instanceID : SV_InstanceID)
            {
                v2fShadow o;
                float3 worldPosition;
                float3 worldNormal;
                float3 color;
                DeformVertex(
                    v.vertex.xyz,
                    v.normal,
                    v.corners,
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
