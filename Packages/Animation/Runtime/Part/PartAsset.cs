using System;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   A character builder equipment part asset (format: yu5h1.part.draft).
    ///   Holds metadata, target bone, offset transform, attach node transform, and the model prefab.
    /// </summary>
    public class PartAsset : ScriptableObject
    {
        [Header("Metadata")]
        public string id;
        public string version;
        public string kind;
        public string format = "yu5h1.part.draft";
        public string formatVersion = "0.1.0";

        [Header("Attachment")]
        public string targetBone;
        public Vector3 offsetPosition;
        public Vector3 offsetRotation;

        [Header("Attach Node (GLB)")]
        public Vector3 attachPosition;
        public Quaternion attachRotation = Quaternion.identity;

        [Header("Prefab")]
        public GameObject prefab;

        /// <summary>
        ///   Computes the local transform of the part relative to its target bone: offset * inverse(attach).
        ///   When <paramref name="mirror"/> is true, mirrors across the X plane (x position negated, Y and Z Euler angles negated).
        /// </summary>
        public void GetLocalTransform(bool mirror, out Vector3 localPosition, out Quaternion localRotation)
        {
            Vector3 offsetPos;
            Vector3 offsetEuler;
            if (mirror)
            {
                offsetPos = new Vector3(-offsetPosition.x, offsetPosition.y, offsetPosition.z);
                offsetEuler = new Vector3(offsetRotation.x, -offsetRotation.y, -offsetRotation.z);
            }
            else
            {
                offsetPos = offsetPosition;
                offsetEuler = offsetRotation;
            }

            var rotOffset = Quaternion.Euler(offsetEuler);
            localRotation = rotOffset * Quaternion.Inverse(attachRotation);
            localPosition = offsetPos - localRotation * attachPosition;
        }

        /// <summary>
        ///   Attaches this part to the character hierarchy.
        /// </summary>
        public GameObject AttachTo(GameObject character, string boneName = null, bool mirror = false)
            => PartAttachment.Attach(character, this, boneName, mirror);

        /// <summary>
        ///   Detaches this part from the character hierarchy.
        /// </summary>
        public bool DetachFrom(GameObject character)
            => PartAttachment.Detach(character, this);
    }
}
