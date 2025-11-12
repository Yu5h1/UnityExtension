using System.IO;

namespace UnityEngine
{
    public class AssetPathInfo : PathInfo
    {
        public AssetPathInfo(params string[] args) : base(args) { }
        public static string CreatePath(string subPath) => Combine(EntryDirectory, subPath);
    }
}
