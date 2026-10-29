using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    ///   Import settings for a character or motion GLB, modelled on Unity's ModelImporter animation settings.
    ///   <see cref="HumanoidImporter.Generate"/> reads them and writes the humanoid <see cref="Avatar"/> and the
    ///   humanoid <see cref="AnimationClip"/> files as sub-assets of this asset, so regenerating replaces the
    ///   contents in place and keeps every reference to them.
    /// </summary>
    public class HumanoidImportSettings : ScriptableObject
    {
        /// <summary>
        ///   Root rotation (heading around the vertical axis). Baked into the pose, the character turns without
        ///   turning the GameObject.
        /// </summary>
        [Serializable]
        public class RotationChannel
        {
            public bool bakeIntoPose;
            [Tooltip("Degrees added to the heading.")]
            public float offset;
        }

        /// <summary>
        ///   Root position Y. Baked into the pose, the character rises and falls without moving the GameObject.
        /// </summary>
        [Serializable]
        public class HeightChannel
        {
            public bool bakeIntoPose;
            [Tooltip("Meters added to the height.")]
            public float offset;
        }

        /// <summary>
        ///   Root position XZ. Baked into the pose, the character travels without moving the GameObject.
        /// </summary>
        [Serializable]
        public class PlanarChannel
        {
            public bool bakeIntoPose;
            [Tooltip("Meters added along X and Z.")]
            public Vector2 offset;
        }

        [Tooltip("The character GLB or the motion GLB as glTFast imported it.")]
        public GameObject source;

        [Tooltip("The node whose movement becomes root motion: 'root' for the ground root, or the pelvis for a motion that has no root track.")]
        public string motionNode = "root";

        public RotationChannel rootRotation = new RotationChannel();
        public HeightChannel rootPositionY = new HeightChannel();
        public PlanarChannel rootPositionXZ = new PlanarChannel();

        public bool loopTime;
        [Tooltip("Blend the end of the clip into its start so it loops without a jump.")]
        public bool loopPose;

        public Avatar avatar;
        public List<AnimationClip> clips = new List<AnimationClip>();
    }
}
