using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// 標記欄位為 StringOptions 的 Context 提供者
    /// 子層欄位使用 [Dropdown("")] 或 [AutoFill("")] 時會往上查找此 Context
    /// </summary>
    /// <example>
    /// public class Config : ScriptableObject
    /// {
    ///     [StringOptionsContext("WeaponTypes")]
    ///     public List&lt;WeaponData&gt; weapons;
    /// }
    /// 
    /// [Serializable]
    /// public class WeaponData
    /// {
    ///     [Dropdown("")]  // 自動繼承 "WeaponTypes"
    ///     public int typeIndex;
    /// }
    /// </example>
    public class StringOptionsContextAttribute : PropertyAttribute
    {
        public string ListKey { get; private set; }

        public StringOptionsContextAttribute(string listKey)
        {
            ListKey = listKey;
        }
    }
}
