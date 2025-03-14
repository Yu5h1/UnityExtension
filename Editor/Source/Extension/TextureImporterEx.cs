using System.ComponentModel;
using System.IO;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class TextureImporterEx
    {
        public static bool IsFileType(this TextureImporter textureImporter,string extension)
            => PathInfo.IsFileType(textureImporter.assetPath, extension);
    }
}
