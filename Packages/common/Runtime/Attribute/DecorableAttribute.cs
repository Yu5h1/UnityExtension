using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Marks a field as decorable — its drawing can be overridden
    /// by an external [Decorator] attribute on the parent field.
    /// When no decorator context is registered, falls back to default PropertyField.
    /// </summary>
    public class DecorableAttribute : PropertyAttribute { }
}
