using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    public static class GlbAttachReader
    {
        const uint GlbMagic = 0x46546c67;
        const uint JsonChunk = 0x4e4f534a;

        [Serializable]
        class GltfDoc
        {
            public GltfNode[] nodes;
        }

        [Serializable]
        class GltfNode
        {
            public string name;
            public float[] translation;
            public float[] rotation;
            public float[] scale;
            public int mesh = -1;
        }

        public static bool ReadAttach(string glbPath, out Vector3 unityAttachPos, out Quaternion unityAttachRot, out string errorMessage)
        {
            unityAttachPos = Vector3.zero;
            unityAttachRot = Quaternion.identity;
            errorMessage = null;

            if (!File.Exists(glbPath))
            {
                errorMessage = $"GLB file not found: '{glbPath}'";
                return false;
            }

            byte[] bytes = File.ReadAllBytes(glbPath);
            string json = ExtractJson(bytes);
            if (string.IsNullOrEmpty(json))
            {
                errorMessage = $"Could not extract JSON from GLB '{Path.GetFileName(glbPath)}'";
                return false;
            }

            GltfDoc doc;
            try
            {
                doc = JsonUtility.FromJson<GltfDoc>(json);
            }
            catch (Exception ex)
            {
                errorMessage = $"Damaged GLB JSON in '{Path.GetFileName(glbPath)}': {ex.Message}";
                return false;
            }

            if (doc?.nodes == null)
            {
                errorMessage = $"GLB '{Path.GetFileName(glbPath)}' has no nodes array";
                return false;
            }

            GltfNode attachNode = null;
            int attachCount = 0;
            for (int i = 0; i < doc.nodes.Length; i++)
            {
                var node = doc.nodes[i];
                if (node.name == "attach")
                {
                    attachNode = node;
                    attachCount++;
                }
            }

            if (attachCount == 0)
            {
                errorMessage = $"A part GLB needs exactly one node 'attach', found 0 in '{Path.GetFileName(glbPath)}'";
                return false;
            }
            if (attachCount > 1)
            {
                errorMessage = $"A part GLB needs exactly one node 'attach', found {attachCount} in '{Path.GetFileName(glbPath)}'";
                return false;
            }

            if (attachNode.mesh >= 0)
            {
                errorMessage = $"Node 'attach' in '{Path.GetFileName(glbPath)}' must be a node without mesh";
                return false;
            }

            if (attachNode.scale != null && attachNode.scale.Length == 3)
            {
                if (Mathf.Abs(attachNode.scale[0] - 1f) > 1e-4f ||
                    Mathf.Abs(attachNode.scale[1] - 1f) > 1e-4f ||
                    Mathf.Abs(attachNode.scale[2] - 1f) > 1e-4f)
                {
                    errorMessage = $"Node 'attach' in '{Path.GetFileName(glbPath)}' must not have scale";
                    return false;
                }
            }

            float gx = 0, gy = 0, gz = 0;
            if (attachNode.translation != null && attachNode.translation.Length == 3)
            {
                gx = attachNode.translation[0];
                gy = attachNode.translation[1];
                gz = attachNode.translation[2];
            }

            float qx = 0, qy = 0, qz = 0, qw = 1;
            if (attachNode.rotation != null && attachNode.rotation.Length == 4)
            {
                qx = attachNode.rotation[0];
                qy = attachNode.rotation[1];
                qz = attachNode.rotation[2];
                qw = attachNode.rotation[3];
            }

            // Convert to Unity coordinates: x inverted
            unityAttachPos = new Vector3(-gx, gy, gz);
            unityAttachRot = new Quaternion(-qx, qy, qz, -qw);

            return true;
        }

        static string ExtractJson(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4)
                return null;
            if (BitConverter.ToUInt32(bytes, 0) != GlbMagic)
                return bytes[0] == (byte)'{' ? Encoding.UTF8.GetString(bytes) : null;
            if (bytes.Length < 20 || BitConverter.ToUInt32(bytes, 16) != JsonChunk)
                return null;
            uint jsonLength = BitConverter.ToUInt32(bytes, 12);
            if (20 + jsonLength > bytes.Length)
                return null;
            return Encoding.UTF8.GetString(bytes, 20, (int)jsonLength);
        }
    }
}
