using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Runtime API for attaching, mirroring and detaching character builder parts on a character hierarchy.
    /// </summary>
    public static class PartAttachment
    {
        /// <summary>
        ///   Finds the target bone in the character hierarchy, instantiates the part prefab,
        ///   parents it under the bone, and sets its local transform to offset * inverse(attach).
        ///   If <paramref name="boneName"/> is null or empty, uses <see cref="PartAsset.targetBone"/>.
        ///   If <paramref name="mirror"/> is true, mirrors the offset across the X plane.
        ///   If the target bone is not found, logs an error containing the bone name and returns null without instantiating.
        /// </summary>
        public static GameObject Attach(GameObject character, PartAsset part, string boneName = null, bool mirror = false)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));
            if (part == null)
                throw new ArgumentNullException(nameof(part));

            string targetName = string.IsNullOrEmpty(boneName) ? part.targetBone : boneName;
            Transform bone = FindBone(character.transform, targetName);
            if (bone == null)
            {
                Debug.LogError($"[PartAttachment] Target bone '{targetName}' not found on character '{character.name}'.");
                return null;
            }

            if (part.prefab == null)
            {
                Debug.LogError($"[PartAttachment] Part '{part.id}' has no prefab assigned.");
                return null;
            }

            GameObject instance = Object.Instantiate(part.prefab);
            instance.name = part.id;
            instance.transform.SetParent(bone, false);

            part.GetLocalTransform(mirror, out Vector3 localPos, out Quaternion localRot);
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = localRot;
            instance.transform.localScale = Vector3.one;

            var attached = instance.AddComponent<AttachedPart>();
            attached.part = part;
            attached.targetBone = targetName;
            attached.isMirrored = mirror;

            return instance;
        }

        /// <summary>
        ///   Detaches and destroys an instantiated part GameObject.
        /// </summary>
        public static bool Detach(GameObject instance)
        {
            if (instance == null)
                return false;
            Object.DestroyImmediate(instance);
            return true;
        }

        /// <summary>
        ///   Finds and detaches any instances of the specified part currently attached to the character.
        /// </summary>
        public static bool Detach(GameObject character, PartAsset part)
        {
            if (character == null || part == null)
                return false;

            var attached = character.GetComponentsInChildren<AttachedPart>(true);
            bool detachedAny = false;
            foreach (var item in attached)
            {
                if (item != null && item.part == part)
                {
                    Object.DestroyImmediate(item.gameObject);
                    detachedAny = true;
                }
            }
            return detachedAny;
        }

        /// <summary>
        ///   Recursively finds a child Transform matching <paramref name="name"/> within <paramref name="root"/>.
        /// </summary>
        public static Transform FindBone(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindBone(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
