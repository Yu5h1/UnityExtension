// Particle data of a ParticleWorld as the solver and its extensions see it
// (ParticleExtensions EXT-03). The solver compute shader and every extension compute shader
// include this file, so the layout has one declaration. The C# side binds these names through
// ParticleWorld.TryBind and ParticleExtensionContext.Bind; GpuParticleBackend.GpuMeta mirrors
// ParticleMeta. Define PARTICLE_WORLD_READ_ONLY before including to get read-only buffers, for
// shaders that only draw.

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

float SubstepFractionStart()
{
    return _SubstepCount > 0u ? (float)_SubstepIndex / (float)_SubstepCount : 0.0;
}

float SubstepFractionEnd()
{
    return _SubstepCount > 0u ? (float)(_SubstepIndex + 1u) / (float)_SubstepCount : 1.0;
}

#endif
