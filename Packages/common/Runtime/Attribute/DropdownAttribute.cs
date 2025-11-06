using UnityEngine;

public class DropdownAttribute : PropertyAttribute
{
    public string ListKey { get; private set; }

    /// <summary>
    /// 建立下拉選單屬性
    /// </summary>
    /// <param name="listKey">在 DropdownRegistry 中註冊的清單鍵值</param>
    public DropdownAttribute(string listKey)
    {
        ListKey = listKey;
    }
}
