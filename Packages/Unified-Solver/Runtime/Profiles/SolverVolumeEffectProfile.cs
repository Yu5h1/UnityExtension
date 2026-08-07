using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    // What a volume does to whatever is in it.
    //
    // A `SolverVolume` says only *where*. Everything about *what happens there*
    // lives in one of these, and a volume carries a list of them. So an aquarium
    // that is water inside and recycles anything leaving it is two effects on
    // one box, not two components each carrying their own copy of the same
    // geometry, their own registration list, their own GPU struct and their own
    // inside test.
    //
    // Every effect uploads through the same `SolverVolumeGPU` entry: the volume
    // fills in the geometry, `Write` fills in the payload, and the kernel
    // switches on `EffectType`. That is the whole seam. Adding an effect means
    // an enum value, a subclass and a branch, and touches neither the volume nor
    // the upload path.
    //
    // Mirrors how `SolverParticleProfile.modifiers` already composes behaviour
    // onto an emitter, deliberately: one pattern for "a list of authored
    // behaviours" rather than two that have to be learned separately.
    public abstract class SolverVolumeEffectProfile : ScriptableObject
    {
        [Tooltip("Off silences it without losing its slot or settings.")]
        public bool enabled = true;

        // Which side of the surface this acts on.
        //
        // Belongs to the effect and not to the volume, because the case that
        // motivates volumes carrying several effects is exactly one box that
        // acts on both sides at once: water within, recycle without. A
        // volume-level flag could not express that, and two volumes with
        // opposite flags would have to be kept aligned by hand.
        [Tooltip("On acts everywhere except inside the volume.")]
        public bool actOutside;

        public abstract SolverVolumeEffectType EffectType { get; }

        // Which loop consumes this, and therefore what it is able to ask.
        //
        // Not a performance note. A per-particle effect can answer "how much of
        // this body is in the water", which is what floats a body at a waterline
        // with nothing modelling one. A per-instance effect answers questions
        // about a whole body, which is the only sensible granularity for a
        // decision like recycling. Declaring the wrong one does not make an
        // effect slightly wrong, it makes it answer a different question.
        public abstract SolverVolumeGranularity Granularity { get; }

        // Fills this effect's half of the entry. The volume has already written
        // the geometry, so only the payload slots may be touched.
        //
        // Clamping belongs here rather than in the runner: the effect owns what
        // its own numbers mean, and the upload path must not have to know that
        // one of them cannot be negative.
        //
        // The volume is passed in because an effect may need the volume's own
        // state to decide what its authored numbers mean -- a medium with
        // `flowIsLocal` resolves its flow against the volume's rotation, so
        // aiming the object aims the flow. Handing the volume over here rather
        // than special-casing such an effect in the runner is what keeps the
        // upload path ignorant of every effect, which is the point of the split.
        // Effects that need nothing but their own fields ignore it.
        public abstract void Write(
            SolverVolume volume,
            ref SolverVolumeGPU entry);
    }
}
