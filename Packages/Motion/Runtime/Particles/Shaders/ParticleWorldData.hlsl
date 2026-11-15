// Particle data of a ParticleWorld as the solver and its extensions see it
// (ParticleExtensions EXT-03). The solver compute shader and every extension compute shader
// include this file, so the layout has one declaration. The C# side binds these names through
// ParticleWorld.TryBind and ParticleExtensionContext.Bind; GpuParticleBackend.GpuMeta mirrors
// ParticleMeta and GpuGroup mirrors ParticleRigidGroup. Define PARTICLE_WORLD_READ_ONLY before
// including to get read-only buffers, for shaders that only draw; it also declares the rigid
// groups, which a compute extension gets read-only with PARTICLE_WORLD_RIGID_GROUPS.

#ifndef YU5H1LIB_PARTICLE_WORLD_DATA
#define YU5H1LIB_PARTICLE_WORLD_DATA

struct ParticleMeta
{
    uint group;
    uint category;
    uint flags;
    float radius;
};

#ifdef PARTICLE_WORLD_READ_ONLY
StructuredBuffer<float4> _Position;
StructuredBuffer<float4> _Previous;
StructuredBuffer<float4> _Velocity;
#else
RWStructuredBuffer<float4> _Position;
RWStructuredBuffer<float4> _Previous;
RWStructuredBuffer<float4> _Velocity;
#endif

struct ParticleRigidGroup
{
    uint memberStart;
    uint memberCount;
    uint padding0;
    uint padding1;
    float4 rotation;
    float4 massCenter;
};

StructuredBuffer<ParticleMeta> _Meta;
StructuredBuffer<uint> _ActiveParticles;

uint _ActiveParticleCount;
uint _SubstepIndex;
uint _SubstepCount;
float _Substep;
float _StepTime;
float3 _Gravity;

float4 QuaternionMultiply(float4 a, float4 b)
{
    return float4(
        a.w * b.xyz + b.w * a.xyz + cross(a.xyz, b.xyz),
        a.w * b.w - dot(a.xyz, b.xyz));
}

float3 Rotate(float4 q, float3 v)
{
    float3 t = 2.0 * cross(q.xyz, v);
    return v + q.w * t + cross(q.xyz, t);
}

#if defined(PARTICLE_WORLD_READ_ONLY) || defined(PARTICLE_WORLD_RIGID_GROUPS)
StructuredBuffer<ParticleRigidGroup> _Groups;

// World position of a point given relative to rigid group g's rest centre in the entity's local
// frame (ParticleExtensions EXT-08): the simulated mass centre plus the point turned by the
// group's rotation. ParticleEntity.RigidGroupRestCentroid gives the rest centre.
float3 RigidGroupPoint(uint g, float3 offsetFromRestCentre)
{
    ParticleRigidGroup group = _Groups[g];
    return group.massCenter.xyz + Rotate(group.rotation, offsetFromRestCentre);
}

// localToWorld of a mesh authored in the entity's local frame and drawn by rigid group g.
float4x4 RigidGroupMatrix(uint g, float3 restCentre)
{
    ParticleRigidGroup group = _Groups[g];
    float4 q = group.rotation;
    float3 x = Rotate(q, float3(1.0, 0.0, 0.0));
    float3 y = Rotate(q, float3(0.0, 1.0, 0.0));
    float3 z = Rotate(q, float3(0.0, 0.0, 1.0));
    float3 t = group.massCenter.xyz - Rotate(q, restCentre);
    return float4x4(
        x.x, y.x, z.x, t.x,
        x.y, y.y, z.y, t.y,
        x.z, y.z, z.z, t.z,
        0.0, 0.0, 0.0, 1.0);
}
#endif

float SubstepFractionStart()
{
    return _SubstepCount > 0u ? (float)_SubstepIndex / (float)_SubstepCount : 0.0;
}

float SubstepFractionEnd()
{
    return _SubstepCount > 0u ? (float)(_SubstepIndex + 1u) / (float)_SubstepCount : 1.0;
}

#endif
