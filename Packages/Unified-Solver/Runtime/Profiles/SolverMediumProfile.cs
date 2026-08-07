using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    // What a medium is made of: how dense it is, which way it is moving, and how
    // strongly it drags what is inside along with it.
    //
    // Water, air, sand and a current are all the same three numbers. The volume
    // that references this says only where the medium is; everything about how
    // it behaves lives here, so one profile can serve several volumes.
    //
    // Per particle, which is what floats a body at the waterline for nothing:
    // the half of it above the surface is simply not inside, and it settles at
    // partial submersion with no surface being modelled anywhere.
    [CreateAssetMenu(
        fileName = "MediumEffect",
        menuName = "Yu5h1/Unified Solver/Volume Effects/Medium")]
    public sealed class SolverMediumProfile :
        SolverVolumeEffectProfile
    {
        // Buoyancy is a ratio, not a force: a = g * (1 - medium / body). So a
        // heavy fragment sinks and a light one floats in the same medium, which
        // is what separates this from simply overriding gravity inside the
        // volume. Body density comes from the particle's own mass and the
        // solver's global particle radius.
        //
        // The units follow the profile masses and particle radius, not kg/m3, so
        // neutral usually lands in the hundreds and water is not 1000. The
        // runner logs the exact number per profile the first time a medium
        // touches it; do not guess it.
        [Tooltip("Equals the body's own density for neutral buoyancy. See the console for the number.")]
        [Min(0f)]
        public float density;

        [Space]
        [Tooltip("Metres per second the medium itself moves at. Zero is still water.")]
        public Vector3 flow;
        // Whether `flow` is read in the volume's own axes rather than the
        // world's.
        //
        // A profile is a shared asset, so a world-space flow is the same vector
        // in every volume that references it: rotating a nozzle would not change
        // where it sprays, and two nozzles pointing different ways could not
        // share a profile. That is fine for an ocean current, which is a
        // property of the world and has no orientation of its own, and it is the
        // wrong answer for anything aimed -- which is why this is off by default
        // and existing assets are untouched.
        //
        // Resolved here rather than in the kernel because the choice is about
        // what the authored numbers *mean*, which is the effect's business; the
        // kernel receives a world-space flow either way and never learns there
        // was a decision.
        [Tooltip("On reads Flow in the volume's own axes, so aiming the volume aims the flow.")]
        public bool flowIsLocal;
        // A rate, not a force: things converge on `flow` rather than
        // accelerating without limit. That is the difference between authoring a
        // current of 1 m/s and authoring an acceleration whose final speed turns
        // out to be set by the solver's global damping.
        [Tooltip("How fast things are dragged to match Flow. 0 lets them pass through freely.")]
        [Min(0f)]
        public float viscosity = 1f;

        public override SolverVolumeEffectType EffectType =>
            SolverVolumeEffectType.Medium;

        public override SolverVolumeGranularity Granularity =>
            SolverVolumeGranularity.Particle;

        // A medium describes the space rather than the volume that bounds it, so
        // the only thing read from the volume here is its orientation, and only
        // when the flow was authored in the volume's own axes. Water in a moving
        // tank is still water; a jet aimed by its nozzle is not.
        public override void Write(
            SolverVolume volume,
            ref SolverVolumeGPU entry)
        {
            entry.payloadX = Mathf.Max(0f, density);
            entry.payloadY = Mathf.Max(0f, viscosity);
            entry.payloadVector = flowIsLocal
                ? volume.transform.rotation * flow
                : flow;
        }
    }
}
