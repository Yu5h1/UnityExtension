using System;
using UnityEngine;

namespace MotionGen
{
    /// <summary>
    /// 標記 string 欄位以顯示 ObjectField 選擇器
    /// 可透過選擇 Unity 資源來設定路徑，或直接輸入路徑
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class FilePathAttribute : PropertyAttribute
    {
        /// <summary>
        /// 允許的檔案類型（用於 ObjectField 過濾）
        /// 預設為 TextAsset
        /// </summary>
        public Type AssetType { get; }

        /// <summary>
        /// 是否使用相對於 Assets 的路徑
        /// true: Assets/Folder/file.json
        /// false: 完整絕對路徑
        /// </summary>
        public bool UseRelativePath { get; }

        /// <summary>
        /// 是否允許 StreamingAssets 路徑
        /// </summary>
        public bool AllowStreamingAssets { get; }

        public FilePathAttribute(
            Type assetType = null,
            bool useRelativePath = false,
            bool allowStreamingAssets = true)
        {
            AssetType = assetType ?? typeof(TextAsset);
            UseRelativePath = useRelativePath;
            AllowStreamingAssets = allowStreamingAssets;
        }
    }
}
