using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Pattern of the threads a <see cref="GridSurfaceAppearance"/> draws when it uses the
    ///   procedural net.
    /// </summary>
    public enum GridNetStyle
    {
        Square = 0,
        Diamond = 1
    }

    /// <summary>
    ///   A net or cloth surface over a grid entity: each particle is a vertex, the four particles of
    ///   a cell make two triangles (four when double sided) and normals come from the neighbours
    ///   (ParticleRendering REND-03). Fields follow NetRenderer.shader of Unified-Solver; with
    ///   <see cref="UseNet"/> off the surface is plain opaque cloth in the profile colour. The
    ///   material must use the Yu5h1Lib/Particles/GridSurface shader or one that reads the same
    ///   particle data.
    /// </summary>
    public sealed class GridSurfaceAppearance : PhysicsParticleAppearance
    {
        [Tooltip("Off draws plain opaque cloth instead of a net with holes.")]
        [SerializeField]
        bool _useNet = true;
        [SerializeField]
        Texture2D _netMap;
        [Tooltip("Take the net shape and colour from the net map instead of the procedural threads.")]
        [SerializeField]
        bool _useNetTexture;
        [SerializeField]
        GridNetStyle _netStyle = GridNetStyle.Diamond;
        [Tooltip("Pattern repeats across the surface.")]
        [SerializeField]
        Vector2 _netTiling = new Vector2(20f, 20f);
        [Range(0.01f, 0.45f)]
        [SerializeField]
        float _threadWidth = 0.08f;
        [Tooltip("Coverage below this is clipped, which makes the holes.")]
        [Range(0.01f, 0.99f)]
        [SerializeField]
        float _cutoff = 0.5f;
        [SerializeField]
        Color _borderColor = new Color(0.01f, 0.01f, 0.01f, 1f);
        [Tooltip("Border width in UV units.")]
        [Range(0.001f, 0.25f)]
        [SerializeField]
        float _borderWidth = 0.025f;
        [Range(0f, 1f)]
        [SerializeField]
        float _metallic;
        [Range(0f, 1f)]
        [SerializeField]
        float _smoothness = 0.3f;
        [Tooltip("Build four triangles per cell so both faces exist; the shader draws two-sided either way.")]
        [SerializeField]
        bool _doubleSided = true;

        public GridSurfaceAppearance()
        {
            Color = new Color(0.12f, 0.12f, 0.12f, 1f);
        }

        public bool UseNet
        {
            get => _useNet;
            set => _useNet = value;
        }

        public Texture2D NetMap
        {
            get => _netMap;
            set => _netMap = value;
        }

        public bool UseNetTexture
        {
            get => _useNetTexture;
            set => _useNetTexture = value;
        }

        public GridNetStyle NetStyle
        {
            get => _netStyle;
            set => _netStyle = value;
        }

        public Vector2 NetTiling
        {
            get => _netTiling;
            set => _netTiling = value;
        }

        public float ThreadWidth
        {
            get => _threadWidth;
            set => _threadWidth = Mathf.Clamp(value, 0.01f, 0.45f);
        }

        public float Cutoff
        {
            get => _cutoff;
            set => _cutoff = Mathf.Clamp(value, 0.01f, 0.99f);
        }

        public Color BorderColor
        {
            get => _borderColor;
            set => _borderColor = value;
        }

        public float BorderWidth
        {
            get => _borderWidth;
            set => _borderWidth = Mathf.Clamp(value, 0.001f, 0.25f);
        }

        public float Metallic
        {
            get => _metallic;
            set => _metallic = Mathf.Clamp01(value);
        }

        public float Smoothness
        {
            get => _smoothness;
            set => _smoothness = Mathf.Clamp01(value);
        }

        public bool DoubleSided
        {
            get => _doubleSided;
            set => _doubleSided = value;
        }

        public override void Validate(
            PhysicsParticleLayout layout,
            PhysicsParticleInteraction interaction,
            List<PhysicsParticleMessage> messages)
        {
            base.Validate(layout, interaction, messages);
            if (layout == null)
                return;
            if (layout.Kind != ParticleLayoutKind.Grid)
            {
                messages.Add(PhysicsParticleMessage.Error($"{name}: a grid surface needs a grid layout, not {layout.Kind}."));
                return;
            }
            if (layout is GridParticleLayout grid && (grid.Columns < 2 || grid.Rows < 2))
                messages.Add(PhysicsParticleMessage.Error($"{name}: a grid surface needs at least 2 columns and 2 rows; the layout has {grid.Columns} x {grid.Rows}."));
        }
    }
}
