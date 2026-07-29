Shader "Yu5h1/UnifiedSolver/ArticulatedMesh"
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

        struct ControlFrame
        {
            float3 center;
            float3 tangent;
            float3 side;
            float3 normal;
        };

        StructuredBuffer<Particle> _Particles;
        StructuredBuffer<SolverInstance> _Instances;
        float3 _MeshCenter;
        float3 _BaseVisualScale;
        float3 _BaseDimensions;
        int _MeshForwardAxis;
        float _MeshAxisMin;
        float _MeshAxisLength;
        sampler2D _BaseMap;
        float4 _BaseMap_ST;
        float4 _Tint;

        static const int TOPOLOGY_SINGLE = 0;
        static const int TOPOLOGY_CHAIN_3 = 3;
        static const int TOPOLOGY_GUIDE_4 = 4;
        static const int TOPOLOGY_DUAL_RAIL_6 = 6;
        static const int TOPOLOGY_ARTICULATED_12 = 12;

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

        float3 SafePerpendicular(
            float3 candidate,
            float3 tangent,
            float3 fallback)
        {
            float3 projected =
                candidate -
                tangent * dot(candidate, tangent);
            float lengthProjected = length(projected);
            if (lengthProjected > 1e-5)
                return projected / lengthProjected;

            projected =
                fallback -
                tangent * dot(fallback, tangent);
            lengthProjected = length(projected);
            if (lengthProjected > 1e-5)
                return projected / lengthProjected;

            float3 axis =
                abs(tangent.x) < 0.8
                    ? float3(1.0, 0.0, 0.0)
                    : float3(0.0, 0.0, 1.0);
            return normalize(
                axis -
                tangent * dot(axis, tangent));
        }

        float3 ControlCenter(
            SolverInstance instance,
            uint control)
        {
            uint particleBase =
                (uint)instance.particleOffset;
            if (instance.topology ==
                TOPOLOGY_SINGLE)
            {
                return
                    _Particles[
                        particleBase].position;
            }

            if (instance.topology ==
                TOPOLOGY_DUAL_RAIL_6)
            {
                uint pair =
                    particleBase + control * 2u;
                return (
                    _Particles[pair].position +
                    _Particles[pair + 1u].position) *
                    0.5;
            }

            if (instance.topology ==
                TOPOLOGY_ARTICULATED_12)
            {
                uint start =
                    particleBase + control * 4u;
                return (
                    _Particles[start].position +
                    _Particles[start + 1u].position +
                    _Particles[start + 2u].position +
                    _Particles[start + 3u].position) *
                    0.25;
            }

            return _Particles[
                particleBase + control].position;
        }

        float3 SideCandidate(
            SolverInstance instance,
            uint control,
            float3 middle)
        {
            uint particleBase =
                (uint)instance.particleOffset;
            if (instance.topology ==
                TOPOLOGY_GUIDE_4)
            {
                return
                    _Particles[
                        particleBase + 3u].position -
                    middle;
            }

            if (instance.topology ==
                TOPOLOGY_DUAL_RAIL_6)
            {
                uint pair =
                    particleBase + control * 2u;
                return
                    _Particles[pair].position -
                    _Particles[pair + 1u].position;
            }

            if (instance.topology ==
                TOPOLOGY_ARTICULATED_12)
            {
                uint start =
                    particleBase + control * 4u;
                return
                    (_Particles[start].position +
                     _Particles[start + 3u].position) -
                    (_Particles[start + 1u].position +
                     _Particles[start + 2u].position);
            }

            return RotateByQuaternion(
                float3(1.0, 0.0, 0.0),
                instance.spawnRotation);
        }

        ControlFrame GetFrame(
            SolverInstance instance,
            uint control)
        {
            float3 head =
                ControlCenter(instance, 0u);
            float3 middle =
                ControlCenter(instance, 1u);
            float3 tail =
                ControlCenter(instance, 2u);

            ControlFrame frame;
            frame.center =
                control == 0u
                    ? head
                    : control == 1u
                        ? middle
                        : tail;
            float3 tangentCandidate =
                control == 0u
                    ? head - middle
                    : control == 1u
                        ? head - tail
                        : middle - tail;
            frame.tangent =
                instance.topology ==
                TOPOLOGY_SINGLE
                    ? RotateByQuaternion(
                        float3(0.0, 1.0, 0.0),
                        instance.spawnRotation)
                    : normalize(tangentCandidate);

            float3 baseX =
                RotateByQuaternion(
                    float3(1.0, 0.0, 0.0),
                    instance.spawnRotation);
            float3 baseZ =
                RotateByQuaternion(
                    float3(0.0, 0.0, 1.0),
                    instance.spawnRotation);
            frame.side = SafePerpendicular(
                SideCandidate(
                    instance,
                    control,
                    middle),
                frame.tangent,
                baseX);
            frame.normal =
                SafePerpendicular(
                    cross(
                        frame.side,
                        frame.tangent),
                    frame.tangent,
                    baseZ);
            frame.side = normalize(
                cross(
                    frame.tangent,
                    frame.normal));
            return frame;
        }

        float3 FramePosition(
            float3 localPosition,
            float restCenter,
            ControlFrame frame)
        {
            return
                frame.center +
                frame.side * localPosition.x +
                frame.tangent *
                    (localPosition.y - restCenter) +
                frame.normal * localPosition.z;
        }

        float3 FrameNormal(
            float3 localNormal,
            ControlFrame frame)
        {
            return normalize(
                frame.side * localNormal.x +
                frame.tangent * localNormal.y +
                frame.normal * localNormal.z);
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
            ControlFrame head =
                GetFrame(instance, 0u);
            ControlFrame middle =
                GetFrame(instance, 1u);
            ControlFrame tail =
                GetFrame(instance, 2u);

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
            float longitudinal = saturate(
                (AxisCoordinate(meshPosition) -
                 _MeshAxisMin) /
                max(_MeshAxisLength, 0.000001));
            float spacing =
                instance.topology ==
                TOPOLOGY_SINGLE
                    ? 0.0
                    : _BaseDimensions.y *
                      instance.scale.y *
                      (instance.topology ==
                        TOPOLOGY_ARTICULATED_12
                        ? 0.333333333
                        : 0.5);

            if (longitudinal < 0.5)
            {
                float blend = smoothstep(
                    0.0,
                    1.0,
                    longitudinal * 2.0);
                worldPosition = lerp(
                    FramePosition(
                        localPosition,
                        -spacing,
                        tail),
                    FramePosition(
                        localPosition,
                        0.0,
                        middle),
                    blend);
                worldNormal = normalize(
                    lerp(
                        FrameNormal(
                            localNormal,
                            tail),
                        FrameNormal(
                            localNormal,
                            middle),
                        blend));
            }
            else
            {
                float blend = smoothstep(
                    0.0,
                    1.0,
                    (longitudinal - 0.5) * 2.0);
                worldPosition = lerp(
                    FramePosition(
                        localPosition,
                        0.0,
                        middle),
                    FramePosition(
                        localPosition,
                        spacing,
                        head),
                    blend);
                worldNormal = normalize(
                    lerp(
                        FrameNormal(
                            localNormal,
                            middle),
                        FrameNormal(
                            localNormal,
                            head),
                        blend));
            }

            color =
                _Particles[
                    instance.particleOffset].color;
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
