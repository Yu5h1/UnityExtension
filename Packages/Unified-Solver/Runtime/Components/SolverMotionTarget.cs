using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    // Where self-propelled bodies are trying to get to.
    //
    // Supplies either a heading or a place, and nothing else. Interpolation,
    // splines and Timeline all drive this Transform from outside; Unity already
    // animates Transforms better than anything written here would, and feeding
    // bait is simply moving this object.
    //
    // Same split as the medium: the scene component says where, the profile says
    // how. Nothing here knows how fast anything moves.
    //
    // It also has no idea a group exists. A group is not an object, it is
    // whichever bodies happen to be in range of this, which is why there is no
    // component named after one.
    [DisallowMultipleComponent]
    public sealed class SolverMotionTarget : MonoBehaviour
    {
        static readonly List<SolverMotionTarget> Active =
            new List<SolverMotionTarget>();

        public static IReadOnlyList<SolverMotionTarget>
            Registered => Active;

        public SolverMotionTargetMode mode =
            SolverMotionTargetMode.Point;

        // Separates one group from another without anything needing an id: a
        // body follows the nearest target that reaches it. 0 reaches everything,
        // which is what a single shared heading wants.
        [Tooltip("Metres. 0 reaches every body in the scene.")]
        [Min(0f)]
        public float radius;

        public Vector3 Position => transform.position;
        public Vector3 Direction => transform.forward;

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
            Draw(new Color(0.4f, 1f, 0.5f, 0.5f));
        }

        void OnDrawGizmosSelected()
        {
            Draw(new Color(0.5f, 1f, 0.6f, 1f));
        }

        void Draw(Color wire)
        {
            Gizmos.color = wire;
            if (radius > 0f)
                Gizmos.DrawWireSphere(Position, radius);

            if (mode == SolverMotionTargetMode.Point)
            {
                Gizmos.DrawWireSphere(Position, 0.1f);
                return;
            }

            Gizmos.DrawLine(
                Position,
                Position + Direction);
        }
    }
}
