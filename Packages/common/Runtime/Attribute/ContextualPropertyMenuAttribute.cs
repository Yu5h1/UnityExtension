using System;

namespace Yu5h1Lib
{
    /// <summary>
    /// Marks a parameterless void method on the target object to appear when right-clicking
    /// any of its serialized properties in the Inspector (property-level context menu, not
    /// the component gear icon). Unlike [ContextMenu] which only shows on the component header,
    /// this attribute makes the action accessible from any field's right-click menu.
    /// Only valid on instance methods with: void return, no parameters, no type arguments.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ContextualPropertyMenuAttribute : Attribute
    {
        public string menuItem;
        public ContextualPropertyMenuAttribute(string menuItem) => this.menuItem = menuItem;
    }
}