using System;

namespace Yu5h1Lib
{
    /// <summary>
    /// 宣告 Addon/OP 對應的目標 Component 型別。
    /// 用於 1:N 映射情境（例如 TextAdapter 同時支援 Text 和 TMP_Text）。
    /// 若 Addon 已使用 [RequireComponent]，以 RequireComponent 為準。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class AddonForAttribute : Attribute
    {
        public Type TargetType { get; }
        public AddonForAttribute(Type targetType) => TargetType = targetType;
    }
}
