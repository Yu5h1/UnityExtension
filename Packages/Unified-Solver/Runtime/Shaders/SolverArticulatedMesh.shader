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
        #include "SolverBodyFrameTypes.hlsl"

        StructuredBuffer<Particle> _Particles;
        StructuredBuffer<SolverInstance> _Instances;

        #include "SolverBodyFrame.hlsl"

        float3 _MeshCenter;
        float3 _BaseVisualScale;
        float3 _BaseDimensions;
        int _MeshForwardAxis;
        float _MeshAxisMin;
        float _MeshAxisLength;
        sampler2D _BaseMap;
        float4 _BaseMap_ST;
        float4 _Tint;

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
