// Procedural instancing of rigid fragments (ParticleRendering REND-06). The object matrix of each
// instance is assembled on the GPU from the world rigid group the instance is bound to
// (RigidGroupMatrix, ParticleExtensions EXT-08), so the CPU never reads a pose back or builds a
// matrix. A mesh drawn with it is authored in the entity local frame, the frame of the layout rest
// positions; _RigidRestCentre is the rest centre of its rigid group (ParticleEntity
// .RigidGroupRestCentroid).
//
// Use from a shader: include UnityCG.cginc, then this file, and add
//     #pragma multi_compile_instancing
//     #pragma instancing_options procedural:ParticleRigidInstancingSetup
// The draw is Graphics.DrawMeshInstancedProcedural with a property block that PhysicsParticleRenderer
// fills: the world bindings (ParticleWorld.TryBind), _RigidInstances (uint2 per instance: x = rigid
// group index, y = body index into _Lifecycle), _RigidRestCentre and _RigidHasLifecycle. A lifecycle
// hidden fraction (_Lifecycle.z) shrinks the instance towards its rest centre.
//
// From a URP Shader Graph, call ParticleRigidObjectToWorld or the Custom Function wrapper
// ParticleRigidPosition_float with the instance index; the include declares PARTICLE_WORLD_READ_ONLY
// itself, so include nothing else of the world data first.

#ifndef YU5H1LIB_PARTICLE_RIGID_INSTANCING
#define YU5H1LIB_PARTICLE_RIGID_INSTANCING

#ifndef PARTICLE_WORLD_READ_ONLY
#define PARTICLE_WORLD_READ_ONLY
#endif
#include "Packages/com.yu5h1.motion/Runtime/Particles/Shaders/ParticleWorldData.hlsl"

StructuredBuffer<uint2> _RigidInstances;
StructuredBuffer<float4> _Lifecycle;
float3 _RigidRestCentre;
uint _RigidHasLifecycle;

float4x4 ParticleRigidObjectToWorld(uint instanceIndex)
{
    uint2 instance = _RigidInstances[instanceIndex];
    float4x4 pose = RigidGroupMatrix(instance.x, _RigidRestCentre);
    float visible = _RigidHasLifecycle != 0u ? saturate(1.0 - _Lifecycle[instance.y].z) : 1.0;
    float3 center = _Groups[instance.x].massCenter.xyz;
    float4x4 result = pose;
    result._m00_m10_m20 = pose._m00_m10_m20 * visible;
    result._m01_m11_m21 = pose._m01_m11_m21 * visible;
    result._m02_m12_m22 = pose._m02_m12_m22 * visible;
    result._m03_m13_m23 = lerp(center, pose._m03_m13_m23, visible);
    return result;
}

float4x4 ParticleRigidWorldToObject(float4x4 objectToWorld)
{
    float3x3 linearPart = (float3x3)objectToWorld;
    float scaleSquared = max(dot(linearPart._m00_m10_m20, linearPart._m00_m10_m20), 1e-8);
    float3x3 inverseLinear = transpose(linearPart) / scaleSquared;
    float3 inverseTranslation = -mul(inverseLinear, objectToWorld._m03_m13_m23);
    return float4x4(
        inverseLinear._m00, inverseLinear._m01, inverseLinear._m02, inverseTranslation.x,
        inverseLinear._m10, inverseLinear._m11, inverseLinear._m12, inverseTranslation.y,
        inverseLinear._m20, inverseLinear._m21, inverseLinear._m22, inverseTranslation.z,
        0.0, 0.0, 0.0, 1.0);
}

void ParticleRigidInstancingSetup()
{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    unity_ObjectToWorld = ParticleRigidObjectToWorld((uint)unity_InstanceID);
    unity_WorldToObject = ParticleRigidWorldToObject(unity_ObjectToWorld);
#endif
}

void ParticleRigidPosition_float(float3 positionOS, float instanceIndex, out float3 positionWS)
{
    positionWS = mul(ParticleRigidObjectToWorld((uint)instanceIndex), float4(positionOS, 1.0)).xyz;
}

void ParticleRigidNormal_float(float3 normalOS, float instanceIndex, out float3 normalWS)
{
    float4x4 objectToWorld = ParticleRigidObjectToWorld((uint)instanceIndex);
    normalWS = normalize(mul((float3x3)objectToWorld, normalOS));
}

#endif
