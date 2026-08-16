using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Yu5h1Lib.ParticlePhysics;

namespace Yu5h1Lib.UnifiedSolver
{
    // Presents a vendored ClothGenerator as something an anchor can bind to.
    //
    // Nothing here is a design choice: ClothGenerator is read-only, so it cannot
    // implement IPhysicsParticleSource itself, and its particle offset is a
    // private field. This wrapper is the smallest thing that closes both gaps.
    // The execution order is repeated rather than inherited: it puts the base
    // class's settings pass after the anchor at -100 and before the solver at 0,
    // and Unity resolves the attribute against the concrete type.
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ClothGenerator))]
    public sealed class SolverClothParticles :
        SolverParticleSource
    {
        ClothGenerator _cloth;

        ClothGenerator Cloth
        {
            get
            {
                if (_cloth == null)
                    _cloth =
                        GetComponent<ClothGenerator>();
                return _cloth;
            }
        }

        public override int ParticleCount
        {
            get
            {
                ClothGenerator cloth = Cloth;
                return cloth == null
                    ? 0
                    : cloth.resolutionX *
                      cloth.resolutionY;
            }
        }

        // Display only. Cloth is a flat sheet, so depth is 1 and the editor
        // draws rows and columns from it.
        public override Vector3Int LayoutSize
        {
            get
            {
                ClothGenerator cloth = Cloth;
                return cloth == null
                    ? Vector3Int.zero
                    : new Vector3Int(
                        cloth.resolutionX,
                        cloth.resolutionY,
                        1);
            }
        }

        // Reproduces the generator's own spawn layout rather than reading any
        // particle, because the editor calls this before anything has spawned.
        //
        // Row-major, matching how ClothGenerator spawns: index = y * width + x.
        public override bool TryGetRestPosition(
            int index,
            out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            ClothGenerator cloth = Cloth;
            if (cloth == null ||
                !IsIndexValid(index, ParticleCount))
            {
                return false;
            }

            int x = index % cloth.resolutionX;
            int y = index / cloth.resolutionX;

            Vector3 centering = new Vector3(
                (cloth.resolutionX - 1) *
                cloth.spacing * 0.5f,
                (cloth.resolutionY - 1) *
                cloth.spacing * 0.5f,
                0f);

            Vector3 local = new Vector3(
                x * cloth.spacing,
                y * cloth.spacing,
                0f) - centering;

            worldPosition =
                cloth.transform.position +
                cloth.transform.rotation * local;
            return true;
        }

        protected override bool TryGetParticleRange(
            SolverManager manager,
            out int particleOffset,
            out int particleCount)
        {
            particleOffset = -1;
            particleCount = 0;
            ClothGenerator cloth = Cloth;
            return cloth != null &&
                SolverManagerAccess
                    .TryGetClothParticleRange(
                        manager,
                        cloth,
                        out particleOffset,
                        out particleCount);
        }

        protected override string DescribeMissingRange()
        {
            return "SolverClothParticles could not identify this cloth in " +
                   "SolverManager. Do not move the ClothGenerator transform " +
                   "after it spawns; move only the anchor Transforms.";
        }

#if UNITY_EDITOR
        // The coordinate the old authoring wrote into each anchor object's name,
        // as in "Cloth Anchor (6, 95)".
        static readonly Regex NodeInName =
            new Regex(@"\(\s*(\d+)\s*,\s*(\d+)\s*\)");

        /// <summary>
        ///   Rebuilds the anchor's bindings from child objects whose names carry
        ///   a grid coordinate, as the pre-binding authoring produced.
        /// </summary>
        /// <remarks>
        ///   For scenes that predate `PhysicsParticleAnchor` and were never
        ///   migrated, where the anchors survive only as one child object per
        ///   pinned vertex with its coordinate in the name. Reads that
        ///   coordinate rather than the nearest particle, because a moved anchor
        ///   would otherwise bind to whatever vertex it now sits over.
        ///
        ///   Offsets are left at zero. The old component wrote the Transform's
        ///   position straight onto the particle, so zero reproduces exactly
        ///   what the scene does today; Recapture Offsets converts that to
        ///   shape-preserving offsets afterwards if that is wanted.
        ///
        ///   Replaces the whole binding list, so anything authored by hand is
        ///   lost. That is the point -- it is a migration, not a merge.
        /// </remarks>
        [ContextMenu("Find Existing Transform Anchors")]
        void FindExistingTransformAnchors()
        {
            var anchor =
                GetComponent<PhysicsParticleAnchor>();
            if (anchor == null)
            {
                Debug.LogWarning(
                    "No PhysicsParticleAnchor on this GameObject.",
                    this);
                return;
            }

            ClothGenerator cloth = Cloth;
            if (cloth == null || cloth.resolutionX <= 0)
            {
                Debug.LogWarning(
                    "ClothGenerator has no usable resolution yet.",
                    this);
                return;
            }

            Transform[] children =
                GetComponentsInChildren<Transform>(true);

            UnityEditor.Undo.RegisterCompleteObjectUndo(
                anchor, "Find Existing Transform Anchors");
            anchor.Bindings.Clear();

            int bound = 0;
            int skipped = 0;
            int count = ParticleCount;

            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == transform)
                    continue;

                Match match = NodeInName.Match(child.name);
                if (!match.Success)
                    continue;

                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);
                int index = y * cloth.resolutionX + x;

                if (x >= cloth.resolutionX ||
                    y >= cloth.resolutionY ||
                    index >= count)
                {
                    Debug.LogWarning(
                        $"'{child.name}' names ({x}, {y}), outside the " +
                        $"{cloth.resolutionX} x {cloth.resolutionY} cloth. " +
                        "Skipped.",
                        child);
                    skipped++;
                    continue;
                }

                anchor.Bindings[child] =
                    new List<PhysicsParticleAnchor.Point>
                    {
                        new PhysicsParticleAnchor.Point
                        {
                            index = index,
                            localOffset = Vector3.zero
                        }
                    };
                bound++;
            }

            anchor.InvalidateBindings();
            UnityEditor.EditorUtility.SetDirty(anchor);

            Debug.Log(
                $"Found {bound} named anchors on a " +
                $"{cloth.resolutionX} x {cloth.resolutionY} cloth" +
                (skipped > 0 ? $", skipped {skipped}" : "") +
                ". Offsets are zero; use Recapture Offsets to keep the " +
                "cloth's shape instead.",
                anchor);
        }
#endif
    }
}
