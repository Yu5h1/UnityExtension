using UnityEngine;

public class DropdownContextAttribute : PropertyAttribute
{
    public string ListKey { get; private set; }

    /// <summary>
    /// 建立下拉選單 Context
    /// </summary>
    /// <param name="listKey">清單鍵值，子欄位可以使用 [Dropdown("")]  繼承此值</param>
    public DropdownContextAttribute(string listKey)
    {
        ListKey = listKey;
    }
}