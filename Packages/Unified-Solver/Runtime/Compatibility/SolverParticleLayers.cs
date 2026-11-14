using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.UnifiedSolver
{
    // A body that owns a run of particles in the solver's buffer and can say
    // which Unity layer they belong to: an emitter, or the glue beside a
    // vendored generator.
    internal interface ISolverParticleLayerSource
    {
        UnityEngine.Object Owner { get; }

        // The GameObject's layer, read every refresh so that changing it at
        // runtime reaches the buffer.
        int Layer { get; }

        // Changes whenever the particles this body owns change, and is -1 while
        // it owns none yet (a cloth that has not started). The buffer is only
        // rewritten for a body whose layer or revision changed.
        int Revision { get; }

        // The runs of the global particle buffer this body owns, as
        // (first index, count).
        void CollectRuns(SolverManager manager, List<Vector2Int> runs);
    }

    // The layer of every particle in the solver, kept beside the solver's own
    // buffer instead of in it.
    //
    // ParticleGPU is 60 bytes with every field spoken for and lives in vendored
    // code, so the layer cannot ride in it. It is a one-bit mask per particle
    // (1 << layer) in a buffer of this package's own, indexed by the same global
    // particle index, and a collider that filters by layer reads it.
    //
    // It is filled from the bodies that register, not read from the particles,
    // so it does not depend on what a particle looks like, and a respawn or park
    // -- which rewrites the particle buffer's slice and nothing else -- cannot
    // touch it: the layer belongs to the GameObject, and the particle indices a
    // body owns never change. A particle no registered body owns (a vendored
    // rigid generator, a cloth with no SolverClothParticles) is in the Default
    // layer, which is where an object nobody has changed is anyway.
    internal static class SolverParticleLayers
    {
        const uint DefaultMask = 1u;

        struct Applied
        {
            public int layer;
            public int revision;
        }

        static readonly List<ISolverParticleLayerSource> Sources =
            new List<ISolverParticleLayerSource>();
        static readonly HashSet<ISolverParticleLayerSource> Known =
            new HashSet<ISolverParticleLayerSource>();
        static readonly Dictionary<ISolverParticleLayerSource, Applied>
            AppliedState =
                new Dictionary<ISolverParticleLayerSource, Applied>();
        static readonly List<Vector2Int> Runs = new List<Vector2Int>();

        static SolverManager _manager;
        static ComputeBuffer _buffer;
        static uint[] _masks;
        static double _refreshedAt = -1.0;

        // With domain reload off the statics outlive play mode, and a buffer
        // from the last run would be handed to a solver that is not that one.
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            ReleaseBuffer();
            Sources.Clear();
            Known.Clear();
            AppliedState.Clear();
            _manager = null;
            _refreshedAt = -1.0;
            Application.quitting -= ReleaseBuffer;
            Application.quitting += ReleaseBuffer;
        }

        // Idempotent. A body that is destroyed is dropped on the next refresh,
        // so nothing has to unregister.
        internal static void Register(ISolverParticleLayerSource source)
        {
            if (source != null && Known.Add(source))
                Sources.Add(source);
        }

        /// <summary>
        ///   The buffer of layer masks, brought up to date, or null when the
        ///   solver has no particle buffer yet.
        /// </summary>
        /// <remarks>
        ///   Refreshes at most once per physics step however many colliders ask.
        /// </remarks>
        internal static ComputeBuffer Resolve(SolverManager manager)
        {
            if (manager == null ||
                manager.ParticleBuffer == null ||
                !manager.ParticleBuffer.IsValid())
            {
                return null;
            }

            int count = manager.ParticleBuffer.count;
            bool rebuilt = false;
            if (_manager != manager ||
                _buffer == null ||
                !_buffer.IsValid() ||
                _buffer.count != count)
            {
                ReleaseBuffer();
                _manager = manager;
                _masks = new uint[count];
                for (int i = 0; i < count; i++)
                    _masks[i] = DefaultMask;
                _buffer = new ComputeBuffer(count, sizeof(uint));
                _buffer.SetData(_masks);
                AppliedState.Clear();
                _refreshedAt = -1.0;
                rebuilt = true;
            }

            double now = Time.fixedTimeAsDouble;
            if (!rebuilt && now == _refreshedAt)
                return _buffer;
            _refreshedAt = now;

            bool dirty = false;
            for (int s = Sources.Count - 1; s >= 0; s--)
            {
                ISolverParticleLayerSource source = Sources[s];
                if (source.Owner == null)
                {
                    Sources.RemoveAt(s);
                    Known.Remove(source);
                    AppliedState.Remove(source);
                    continue;
                }

                int revision = source.Revision;
                if (revision < 0)
                    continue;

                int layer = source.Layer;
                if (AppliedState.TryGetValue(source, out Applied applied) &&
                    applied.layer == layer &&
                    applied.revision == revision)
                {
                    continue;
                }

                uint mask = 1u << Mathf.Clamp(layer, 0, 31);
                Runs.Clear();
                source.CollectRuns(manager, Runs);
                for (int r = 0; r < Runs.Count; r++)
                {
                    int start = Mathf.Max(0, Runs[r].x);
                    int end = Mathf.Min(count, Runs[r].x + Runs[r].y);
                    for (int i = start; i < end; i++)
                        _masks[i] = mask;
                }

                AppliedState[source] =
                    new Applied { layer = layer, revision = revision };
                dirty = true;
            }

            if (dirty)
                _buffer.SetData(_masks);
            return _buffer;
        }

        static void ReleaseBuffer()
        {
            if (_buffer != null)
            {
                _buffer.Release();
                _buffer = null;
            }
            _masks = null;
        }
    }
}
