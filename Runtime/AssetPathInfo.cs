using System.IO;

namespace UnityEngine
{
    public class AssetPathInfo : PathInfo
    {
        public static string CreatePath(string subPath) => Combine(EntryDirectory, subPath);
    }
}
