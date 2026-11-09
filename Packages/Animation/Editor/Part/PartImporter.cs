using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    ///   Imports a character builder part (.part.json and its companion GLB) into a <see cref="PartAsset"/>.
    /// </summary>
    public static class PartImporter
    {
        public static string GetAssetPath(string partJsonPath)
        {
            if (string.IsNullOrEmpty(partJsonPath))
                return null;
            string dir = Path.GetDirectoryName(partJsonPath);
            string filename = Path.GetFileName(partJsonPath);
            if (filename.EndsWith(".part.json", StringComparison.OrdinalIgnoreCase))
            {
                string stem = filename.Substring(0, filename.Length - ".part.json".Length);
                return (string.IsNullOrEmpty(dir) ? stem + ".asset" : Path.Combine(dir, stem + ".asset")).Replace('\\', '/');
            }
            return Path.ChangeExtension(partJsonPath, ".asset").Replace('\\', '/');
        }

        public static PartAsset Import(string partJsonPath)
        {
            if (string.IsNullOrEmpty(partJsonPath))
            {
                Debug.LogError("[PartImporter] Path is null or empty.");
                return null;
            }

            string fullJsonPath = Path.GetFullPath(partJsonPath);
            if (!File.Exists(fullJsonPath))
            {
                Debug.LogError($"[PartImporter] Part file not found: '{partJsonPath}'");
                return null;
            }

            string jsonText = File.ReadAllText(fullJsonPath);
            if (!PartValidation.Validate(jsonText, out var desc, out var validationError))
            {
                Debug.LogError($"[PartImporter] Failed to import '{partJsonPath}': {validationError}");
                return null;
            }

            string dir = Path.GetDirectoryName(fullJsonPath);
            string glbFullPath = Path.Combine(dir, desc.glb);
            if (!GlbAttachReader.ReadAttach(glbFullPath, out var attachPos, out var attachRot, out var glbError))
            {
                Debug.LogError($"[PartImporter] Failed to import '{partJsonPath}': {glbError}");
                return null;
            }

            string assetPath = NormalizeToUnityAssetPath(GetAssetPath(partJsonPath));
            string dirRel = Path.GetDirectoryName(partJsonPath);
            string glbRelPath = string.IsNullOrEmpty(dirRel) ? desc.glb : Path.Combine(dirRel, desc.glb);
            string glbAssetPath = NormalizeToUnityAssetPath(glbRelPath);

            GameObject prefab = null;
            if (!string.IsNullOrEmpty(glbAssetPath))
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(glbAssetPath);
                if (prefab == null)
                {
                    AssetDatabase.ImportAsset(glbAssetPath, ImportAssetOptions.ForceSynchronousImport);
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(glbAssetPath);
                }
            }

            PartAsset asset = AssetDatabase.LoadAssetAtPath<PartAsset>(assetPath);
            bool isNew = (asset == null);
            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<PartAsset>();
            }

            asset.id = desc.id;
            asset.version = desc.version;
            asset.kind = desc.kind;
            asset.format = desc.format;
            asset.formatVersion = desc.formatVersion;
            asset.targetBone = desc.targetBone;
            asset.offsetPosition = desc.offsetPosition;
            asset.offsetRotation = desc.offsetRotation;
            asset.attachPosition = attachPos;
            asset.attachRotation = attachRot;
            asset.prefab = prefab;

            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            return asset;
        }

        public static string NormalizeToUnityAssetPath(string fullOrRelPath)
        {
            if (string.IsNullOrEmpty(fullOrRelPath))
                return fullOrRelPath;

            string normalized = fullOrRelPath.Replace('\\', '/');
            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            int assetsIdx = normalized.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
            if (assetsIdx >= 0)
            {
                return normalized.Substring(assetsIdx + 1);
            }

            int animIdx = normalized.IndexOf("/Packages/Animation/", StringComparison.OrdinalIgnoreCase);
            if (animIdx >= 0)
            {
                return "Packages/com.yu5h1.animation/" + normalized.Substring(animIdx + "/Packages/Animation/".Length);
            }

            int pkgIdx = normalized.IndexOf("/Packages/", StringComparison.OrdinalIgnoreCase);
            if (pkgIdx >= 0)
            {
                return normalized.Substring(pkgIdx + 1);
            }

            return normalized;
        }
    }
}
