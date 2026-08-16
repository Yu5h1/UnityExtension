using UnityEngine;

namespace Yu5h1Lib
{
    // Presents a ParticleSystem's shape module as a volume anything can read.
    //
    // The point is not to save writing a box. It is that the region is already
    // authored, already has a gizmo, and already moves with its own Transform --
    // so a consumer that borrows it cannot drift out of step with what the
    // artist sees. Authoring the same box twice is the failure this removes.
    //
    // Only the shapes that enclose a volume are offered. A ParticleSystem's
    // Circle, Edge, Rectangle, Sprite and Mesh have no interior to describe, so
    // IsUsable reports false -- and says so -- rather than inventing a depth.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleSystemAddon :
        ComponentController<ParticleSystem>,
        IShapeProvider
    {
        ParticleSystem _resolved;

        // ComponentController fills its reference from OnInitializing, which
        // has not run in edit mode -- and a shape provider is read by gizmos and
        // by inspectors long before play. So resolve here and let the base
        // field be the serialized convenience it is meant to be.
        ParticleSystem Target
        {
            get
            {
                if (_resolved == null)
                {
                    _resolved = component != null
                        ? component
                        : GetComponent<ParticleSystem>();
                }
                return _resolved;
            }
        }

        ParticleSystem.ShapeModule Shape =>
            Target.shape;

        // Mapped by geometry family, ignoring the emission variant.
        //
        // Shell and Edge say where particles are born, not what space the shape
        // encloses, and a region only cares about the latter -- so Cone,
        // ConeShell, ConeVolume and ConeVolumeShell are all one cone here.
        // Reading only ConeVolume was the first version of this, and it turned
        // the Inspector's default cone into a region that silently did nothing.
        public ShapeKind Kind
        {
            get
            {
                switch (Shape.shapeType)
                {
                    case ParticleSystemShapeType.Sphere:
                    case ParticleSystemShapeType.SphereShell:
                        return ShapeKind.Sphere;

                    case ParticleSystemShapeType.Cone:
                    case ParticleSystemShapeType.ConeShell:
                    case ParticleSystemShapeType.ConeVolume:
                    case ParticleSystemShapeType.ConeVolumeShell:
                        return ShapeKind.Cone;

                    default:
                        return ShapeKind.Box;
                }
            }
        }

        static bool IsSupported(
            ParticleSystemShapeType type)
        {
            switch (type)
            {
                case ParticleSystemShapeType.Box:
                case ParticleSystemShapeType.BoxShell:
                case ParticleSystemShapeType.BoxEdge:
                case ParticleSystemShapeType.Sphere:
                case ParticleSystemShapeType.SphereShell:
                case ParticleSystemShapeType.Cone:
                case ParticleSystemShapeType.ConeShell:
                case ParticleSystemShapeType.ConeVolume:
                case ParticleSystemShapeType.ConeVolumeShell:
                    return true;
                default:
                    return false;
            }
        }

        // The shape module carries its own offset on top of the Transform, and
        // ignoring it puts the volume somewhere the artist never placed it.
        public Vector3 Center
        {
            get
            {
                Vector3 local = Shape.position;
                if (Kind == ShapeKind.Cone)
                {
                    // The module's origin is the narrow end, but a shape's
                    // centre is its middle.
                    local += LocalRotation *
                        new Vector3(0f, 0f, 0.5f * Shape.length);
                }

                return transform.TransformPoint(local);
            }
        }

        // No axis correction: ShapeKind.Cone opens along +Z and so does a
        // ParticleSystem's, so the shape's frame is the system's frame. The
        // two directions this used to reconcile were the source of both axis
        // bugs this component has had.
        public Quaternion Rotation =>
            transform.rotation * LocalRotation;

        Quaternion LocalRotation =>
            Quaternion.Euler(Shape.rotation);

        Vector3 AbsoluteLossyScale
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

        // A box is axis-aligned in its own frame, so each axis can take its own
        // factor. Everything else is a body of revolution, where a per-axis
        // scale would turn the circular cross-section into an ellipse the
        // kernel has no test for.
        float UniformScale => AbsoluteLossyScale.x;

        bool HasUniformScale
        {
            get
            {
                Vector3 s = AbsoluteLossyScale;
                return Mathf.Abs(s.x - s.y) < 1e-3f &&
                       Mathf.Abs(s.x - s.z) < 1e-3f;
            }
        }

        // A ParticleSystem's shape is placed by two transforms, not one: the
        // GameObject's, and the shape module's own position/rotation/scale on
        // top of it. Center and Rotation already compose both; the size has to
        // as well, or a scaled emitter draws a gizmo one size and drives a
        // region another.
        //
        // Only a uniform lossyScale is applied. A non-uniform one on a sphere
        // or a cone would make the cross-section an ellipse, which the kernel's
        // tests do not describe. Refused out loud instead, as the shape
        // module's own Scale is on anything but a box.
        public Vector3 Size
        {
            get
            {
                ParticleSystem.ShapeModule shape = Shape;
                float lossy = UniformScale;

                switch (shape.shapeType)
                {
                    case ParticleSystemShapeType.Sphere:
                    case ParticleSystemShapeType.SphereShell:
                        float diameter =
                            2f * shape.radius * lossy;
                        return new Vector3(
                            diameter, diameter, diameter);

                    case ParticleSystemShapeType.Cone:
                    case ParticleSystemShapeType.ConeShell:
                    case ParticleSystemShapeType.ConeVolume:
                    case ParticleSystemShapeType.ConeVolumeShell:
                        // The module gives the narrow radius and the angle it
                        // opens at; the wide radius is where that angle has
                        // reached by the end of the length.
                        float near = shape.radius;
                        float far = near + shape.length *
                            Mathf.Tan(
                                shape.angle * Mathf.Deg2Rad);
                        return new Vector3(
                            2f * far * lossy,
                            2f * near * lossy,
                            shape.length * lossy);

                    default:
                        return Vector3.Scale(
                            shape.scale,
                            AbsoluteLossyScale);
                }
            }
        }

        bool _reported;

        // Says why once instead of leaving a region that quietly does nothing.
        // A consumer skips an unusable provider without comment -- correctly, it
        // has nothing to say -- so the explanation has to come from here.
        public bool IsUsable
        {
            get
            {
                if (Target == null || !Shape.enabled)
                    return false;

                ParticleSystemShapeType type =
                    Shape.shapeType;

                if (!IsSupported(type))
                {
                    Report(
                        $"shape type {type} encloses no volume this can " +
                        "describe. Use Box, Sphere or Cone.");
                    return false;
                }

                if (Kind != ShapeKind.Box &&
                    !HasUniformScale)
                {
                    Report(
                        $"the Transform's lossy scale is " +
                        $"{AbsoluteLossyScale} on a {Kind}, whose cross " +
                        "section must stay circular. Scale it uniformly, or " +
                        "size the shape with Radius and Length.");
                    return false;
                }

                if (Kind != ShapeKind.Box &&
                    Vector3.Distance(
                        Shape.scale, Vector3.one) > 1e-3f)
                {
                    Report(
                        $"the shape module's Scale is {Shape.scale} on a " +
                        $"{Kind}, where only Radius and Length are read. " +
                        "Set Scale back to one and size the shape with " +
                        "those, or use the Transform.");
                    return false;
                }

                Vector3 size = Size;
                if (size.x <= 0f || size.z <= 0f)
                {
                    Report(
                        Kind == ShapeKind.Cone && Shape.length <= 0f
                            ? "the cone's Length is zero. Set Emit from to " +
                              "Volume so the shape has a length."
                            : $"shape type {type} has no extent yet.");
                    return false;
                }

                _reported = false;
                return true;
            }
        }

        void Report(string reason)
        {
            if (_reported)
                return;

            _reported = true;
            Debug.LogWarning(
                $"ParticleSystemAddon on '{name}' cannot describe a " +
                $"volume: {reason}",
                this);
        }
    }
}
