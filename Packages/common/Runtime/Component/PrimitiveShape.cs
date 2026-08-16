using UnityEngine;

namespace Yu5h1Lib
{
    // A primitive volume authored directly on a Transform.
    //
    // The default provider, and the reason a consumer never needs a "no
    // provider" branch: position is the centre, rotation orients it, lossyScale
    // is the full size. There is nothing to author twice, and a negative scale
    // reads as its absolute value rather than as an inverted volume.
    //
    // Sits beside the thing it describes rather than inside it, so a consumer
    // that wants a ParticleSystem's shape instead can swap this out for
    // ParticleSystemAddon without either side learning about the other.
    [DisallowMultipleComponent]
    public class PrimitiveShape : MonoBehaviour, IShapeProvider
    {
        [Tooltip("Box has a flat top and therefore a waterline; a sphere does not.")]
        public ShapeKind kind = ShapeKind.Box;

        public ShapeKind Kind => kind;

        public Vector3 Center => transform.position;

        public Quaternion Rotation => transform.rotation;

        public Vector3 Size
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

        // A cone's narrow radius is zero by design, and that is Size.y, so only
        // the wide radius and the length are required to be positive.
        public bool IsUsable
        {
            get
            {
                Vector3 size = Size;
                if (size.x <= 0f || size.z <= 0f)
                    return false;

                return kind == ShapeKind.Cone || size.y > 0f;
            }
        }

        void OnDrawGizmosSelected()
        {
            ShapeGizmos.Draw(
                this, new Color(0.3f, 0.8f, 1f, 1f));
        }
    }
}
