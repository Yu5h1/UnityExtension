using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

namespace Yu5h1.UnifiedSolver
{
    // A region where the global environment is replaced by a medium.
    //
    // Outside one of these, a particle falls under the solver's own gravity and
    // meets no resistance. Inside, this takes over: it floats or sinks by
    // density, and it is dragged toward whatever the medium is doing. Leaving
    // hands it straight back. Same relationship a collider has with the world,
    // except it changes what the space is made of rather than blocking it.
    //
    // A sphere, taken from the Transform, so there is nothing to author twice:
    // position is the centre and the largest axis of lossyScale is the diameter.
    //
    // Registration is global rather than per emitter. A medium is a property of
    // the scene, not of whoever happens to be swimming in it, so every emitter
    // reads the same list.
    [DisallowMultipleComponent]
    public sealed class SolverMediumVolume : MonoBehaviour
    {
        static readonly List<SolverMediumVolume> Active =
            new List<SolverMediumVolume>();

        public static IReadOnlyList<SolverMediumVolume>
            Registered => Active;

        [Inline]
        public SolverMediumProfile profile;

        public Vector3 Center => transform.position;

        // The largest axis wins, so a squashed Transform still gives a sphere
        // that covers what it looks like it covers.
        public float Radius
        {
            get
            {
                Vector3 scale = transform.lossyScale;
                return 0.5f * Mathf.Max(
                    Mathf.Abs(scale.x),
                    Mathf.Max(
                        Mathf.Abs(scale.y),
                        Mathf.Abs(scale.z)));
            }
        }

        void OnEnable()
        {
            if (!Active.Contains(this))
                Active.Add(this);
        }

        void OnDisable()
        {
            Active.Remove(this);
        }

        void OnDrawGizmos()
        {
            Draw(new Color(0.2f, 0.6f, 1f, 0.5f));
        }

        void OnDrawGizmosSelected()
        {
            Draw(new Color(0.3f, 0.8f, 1f, 1f));
        }

        void Draw(Color wire)
        {
            Gizmos.color = wire;
            Gizmos.DrawWireSphere(Center, Radius);
        }
    }
}
