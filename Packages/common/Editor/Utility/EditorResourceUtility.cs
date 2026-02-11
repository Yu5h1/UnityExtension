using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
	public static class EditorResourceUtility
	{

        [MenuItem("Tools/List Built-in Icons")]
        static void ListBuiltInIcons()
        {
            var icons = Resources.FindObjectsOfTypeAll<Texture2D>()
                .Where(t => t.name.Length > 0)
                .Select(t => t.name)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            icons.Join("\n").print();

            Debug.Log($"Total: {icons.Count} icons");
        }
    } 
}
