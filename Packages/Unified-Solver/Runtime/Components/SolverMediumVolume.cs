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
    // Geometry is taken from the Transform, so there is nothing to author twice:
    // position is the centre, lossyScale is the full size, and rotation orients
    // it. A box therefore has a flat top, and that top is the waterline.
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

        [Tooltip("Box has a flat top and therefore a waterline; an ellipsoid does not.")]
        public SolverMediumShape shape =
            SolverMediumShape.Box;

        [Inline]
        public SolverMediumProfile profile;

        public Vector3 Center => transform.position;

        public Vector3 HalfExtents =>
            0.5f * AbsoluteScale;

        public Vector3 AxisX => transform.right;
        public Vector3 AxisY => transform.up;
        public Vector3 AxisZ => transform.forward;

        Vector3 AbsoluteScale
        {
            get
            {
                Vector3 scale = transform.lossyScale;
                return new Vector3(
                    Mathf.Abs(scale.x),
                    Mathf.Abs(scale.y),
                    Mathf.Abs(scale.z));
            }
        }

        public bool IsUsable =>
            profile != null &&
            HalfExtents.x > 0f &&
            HalfExtents.y > 0f &&
            HalfExtents.z > 0f;

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
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                Center,
                transform.rotation,
                AbsoluteScale);
            Gizmos.color = wire;
            if (shape == SolverMediumShape.Box)
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            else
                Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
            Gizmos.matrix = previous;
        }
    }
}
