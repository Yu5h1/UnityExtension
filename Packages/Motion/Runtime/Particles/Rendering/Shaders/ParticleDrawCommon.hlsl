// Instance and stereo plumbing shared by every Motion particle drawing shader (ParticleRendering
// REND-02). Include UnityCG.cginc first. The draws are DrawMeshInstancedProcedural calls whose
// instance number picks a body, an entity or a rigid group, so the number has to survive Single
// Pass Instanced rendering, where Unity doubles the instance count and the low bit selects the eye:
// PARTICLE_INSTANCE_INDEX returns the real instance number in every stereo mode, so a shader never
// reads SV_InstanceID directly.
//
// Vertex stage order: declare PARTICLE_VERTEX_INSTANCE_ID in the vertex input and
// UNITY_VERTEX_OUTPUT_STEREO in the vertex output; call PARTICLE_SETUP_INSTANCE(v) first, then
// UNITY_INITIALIZE_OUTPUT(v2f, o) and UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o). Fragment stage:
// UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i) first, so per-eye values such as
// _WorldSpaceCameraPos are right.

#ifndef YU5H1LIB_PARTICLE_DRAW_COMMON
#define YU5H1LIB_PARTICLE_DRAW_COMMON

#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED)
    #define PARTICLE_DRAW_UNITY_INSTANCING 1
#endif

#ifdef PARTICLE_DRAW_UNITY_INSTANCING
    #define PARTICLE_VERTEX_INSTANCE_ID UNITY_VERTEX_INPUT_INSTANCE_ID
    #define PARTICLE_INSTANCE_INDEX(v) ((uint)unity_InstanceID)
    #define PARTICLE_SETUP_INSTANCE(v) UNITY_SETUP_INSTANCE_ID(v)
#else
    #define PARTICLE_VERTEX_INSTANCE_ID uint instanceID : SV_InstanceID;
    #define PARTICLE_INSTANCE_INDEX(v) ((uint)(v).instanceID)
    #define PARTICLE_SETUP_INSTANCE(v)
#endif

#endif
