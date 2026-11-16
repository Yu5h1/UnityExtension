// Sleep data of a ParticleWorld (ParticleExtensions EXT-13, ParticleWorld WORLD-10). A sleeping
// entity's particles are not in the active particle list, so no solver kernel updates them; they
// stay in the neighbour structure as immovable contact partners with PARTICLE_FLAG_SLEEPING set
// in ParticleMeta.flags. _SleepPush holds, per sleeping particle and in fixed point of
// PARTICLE_PUSH_SCALE per metre, the largest distance a contact of an awake particle would have
// moved it during the step: the correction the contact asked of it, which it would then have kept
// moving by for the rest of the step, so the correction times the substeps left (this substep
// included). The sleep extension reads and clears it. Include ParticleWorldData.hlsl first.

#ifndef YU5H1LIB_PARTICLE_WORLD_SLEEP
#define YU5H1LIB_PARTICLE_WORLD_SLEEP

#define PARTICLE_FLAG_SLEEPING 2u
#define PARTICLE_PUSH_SCALE 100000.0

RWStructuredBuffer<uint> _SleepPush;

#endif
