using System;

namespace Yu5h1Lib
{
    /// <summary>
    /// Specifies which drawer type should be used for inner [Decorable] fields.
    /// This is a System.Attribute (not PropertyAttribute) so it doesn't occupy
    /// a PropertyDrawer slot and can coexist with [Inline] on the same field.
    /// </summary>
    /// <example>
    /// [Inline(true), Decorator(typeof(DropdownAttribute)), StringOptionsContext("ComponentProperties")]
    /// public List&lt;ParameterObject&gt; properties;
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public class DecoratorAttribute : Attribute
    {
        public Type DrawerType { get; }

        public DecoratorAttribute(Type drawerType)
        {
            DrawerType = drawerType;
        }
    }
}
