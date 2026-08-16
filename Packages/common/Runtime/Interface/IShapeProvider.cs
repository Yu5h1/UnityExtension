using UnityEngine;

namespace Yu5h1Lib
{
    // The primitive volumes something can ask a provider for.
    //
    // This set is closed by whoever consumes it -- for a GPU consumer, by the
    // branches its kernel has. That is why it is an enum and not a set of
    // assets: an asset could name a shape no consumer implements, and the
    // failure would be a silent no-op rather than a compile error.
    //
    // Every one of these is describable by an oriented box, which is what makes
    // them cheap: centre, rotation and size are the whole description, and a
    // consumer needs no per-shape parameters.
    public enum ShapeKind
    {
        Box = 0,
        // A non-uniform size makes this an ellipsoid, and an ellipsoid has no
        // flat top -- so anything that depends on a level surface, a waterline
        // most of all, has no surface to find.
        Sphere = 1,
        Cylinder = 2,
        // Narrow at local -Z, widening toward +Z.
        //
        // The axis is +Z, matching Unity's forward and a ParticleSystem's own
        // cone, so a provider never has to turn one frame into another. An
        // earlier version put the axis on +Y to match the box's flat top; a
        // cone has no flat top, so that bought nothing and cost two bugs -- a
        // cone built backwards, and "local" axes that disagreed with the
        // Transform's arrows.
        //
        // A circular cone needs one radius per end, so the two cross-section
        // axes carry them and the axis carries the length: Size.x is the
        // diameter at the wide end, Size.y the diameter at the narrow end --
        // zero for a true cone, non-zero for a frustum -- and Size.z the
        // length. Cylinder reads the same way with both radii equal.
        Cone = 3
    }

    /// <summary>
    ///   A primitive volume placed in the world.
    /// </summary>
    /// <remarks>
    ///   Implement this on a <see cref="MonoBehaviour"/>, never on a
    ///   ScriptableObject. A shape without a Transform can only answer *what*
    ///   and not *where*, which would force every consumer to carry a fallback
    ///   for the missing half. Placement is the implementer's own Transform
    ///   composed with whatever local offset its source carries -- a
    ///   ParticleSystem's shape module has one, for instance.
    ///
    ///   C# cannot express "implementers must be components", so this is a
    ///   contract rather than a constraint. Consumers should expose the
    ///   reference as <c>[TypeRestriction(typeof(IShapeProvider))] Object</c>,
    ///   which keeps the inspector honest even though the compiler cannot.
    /// </remarks>
    public interface IShapeProvider
    {
        ShapeKind Kind { get; }

        /// <summary>World-space centre of the volume.</summary>
        Vector3 Center { get; }

        /// <summary>World-space orientation. Local +Y is the shape's up.</summary>
        Quaternion Rotation { get; }

        /// <summary>Full size along each local axis, not half extents.</summary>
        Vector3 Size { get; }

        /// <summary>
        ///   False when this provider cannot describe a usable volume -- a
        ///   source set to a shape with no 3D extent, or one not yet
        ///   configured. A consumer skips it rather than guessing.
        /// </summary>
        bool IsUsable { get; }
    }
}
