using System;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    ///   Automatically imports or refreshes <see cref="PartAsset"/> when a .part.json file is imported or modified.
    /// </summary>
    public class PartPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                if (path.EndsWith(".part.json", StringComparison.OrdinalIgnoreCase))
                {
                    PartImporter.Import(path);
                }
            }
        }
    }
}
