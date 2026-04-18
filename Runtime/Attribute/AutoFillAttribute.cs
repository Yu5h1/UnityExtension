using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// 為 string 欄位提供自動填入建議
    /// 選項來源從 StringOptionsProvider 取得
    /// </summary>
    /// <example>
    /// [AutoFill("TagNames")]
    /// public string tagName;
    /// </example>
    public class AutoFillAttribute : PropertyAttribute
    {
        public string ListKey { get; private set; }
        /// <summary>
        /// 建立自動填入屬性
        /// </summary>
        /// <param name="listKey">
        /// 在 StringOptionsProvider 中註冊的清單鍵值
        /// 空字串表示從上層 StringOptionsContext 繼承
        /// </param>
        public AutoFillAttribute(string listKey = "")
        {
            ListKey = listKey;
        }
    }
}
