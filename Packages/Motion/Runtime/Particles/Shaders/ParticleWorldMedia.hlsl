// Fluid regions of a ParticleWorld as the solver and its extensions see them (ParticleSolver
// SOLVER-17, ParticleExtensions EXT-14). The solver compute shader applies them as forces; an
// extension compute shader reads the same list to derive per-body state. The C# side uploads the
// list once per step, volume media first and script media after; GpuParticleBackend.GpuMedium
// mirrors Medium. flow.xyz is the flow velocity, flow.w the density, parameters.x the drag
// coefficient.

#ifndef YU5H1LIB_PARTICLE_WORLD_MEDIA
#define YU5H1LIB_PARTICLE_WORLD_MEDIA

struct Medium
{
    float4 center;
    float4 halfExtents;
    float4 axisX;
    float4 axisY;
    float4 axisZ;
    float4 flow;
    float4 parameters;
};

StructuredBuffer<Medium> _Media;
uint _MediumCount;

bool MediumReaches(Medium medium, float3 position)
{
    float3 relative = position - medium.center.xyz;
    float3 local = float3(dot(relative, medium.axisX.xyz), dot(relative, medium.axisY.xyz), dot(relative, medium.axisZ.xyz));
    return all(abs(local) <= medium.halfExtents.xyz);
}

#endif
