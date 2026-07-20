using System;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// 將 int 欄位顯示為下拉選單
    /// 選項來源從 StringOptionsProvider 取得
    /// </summary>
    /// <example>
    /// // 明確指定 ListKey
    /// [Dropdown("WeaponTypes")]
    /// public int weaponIndex;
    /// 
    /// // 繼承上層 Context
    /// [Dropdown("")]
    /// public int typeIndex;
    /// </example>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class DropdownAttribute : PropertyAttribute
    {
        public string ListKey { get; private set; }
        public Type EnumType { get; private set; }

        /// <summary>
        /// 建立下拉選單屬性
        /// </summary>
        /// <param name="listKey">
        /// 在 StringOptionsProvider 中註冊的清單鍵值
        /// 空字串表示從上層 StringOptionsContext 繼承
        /// </param>
        public DropdownAttribute(string listKey = "")
        {
            ListKey = listKey;
        }

        /// <summary>
        /// 使用 enum 名稱作為下拉選項。
        /// int 儲存 enum 的實際數值，string 儲存 enum 名稱。
        /// </summary>
        public DropdownAttribute(Type enumType)
        {
            EnumType = enumType;
        }
    }
}
