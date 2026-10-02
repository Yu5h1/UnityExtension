using UnityEditor;
using UnityEngine;
using Yu5h1Lib.MVVM;
using Type = System.Type;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// <c>[TypeRestriction(..., filter = typeof(ValuePortResolver))]</c>: accepts only a control that is an
    /// <see cref="IValuePort"/> or has a registered adapter, and flags a field name already used by a sibling entry.
    /// </summary>
    public class PreferencesBindingFilter : TypeRestrictionDrawer.Filter
    {
        public override Type key => typeof(ValuePortResolver);

        public override Object Validate(Object obj, SerializedProperty property)
            => PreferencesBindingUtility.ResolveBindableComponent(obj);

        public override string RejectMessage(Object obj)
            => $"{obj.name} is not an IValuePort and has no registered Adapter; rejected.";

        public override void OnAssigned(Object obj, SerializedProperty property)
        {
            if (obj is Component resolved && PreferencesBindingUtility.IsDuplicateName(property, resolved, out var duplicateIndex))
                $"Field name '{resolved.gameObject.name}' is already bound at index {duplicateIndex}.".printWarning();
        }

        /// <summary>Flags a control whose value type does not match the host's source member of the same name.</summary>
        public override string GetIssue(Object obj, SerializedProperty property)
        {
            if (!(property.serializedObject.targetObject is IPreferences host) || host.SourceType == null)
                return null;
            var port = ValuePortResolver.Resolve(obj);
            if (port == null)
                return null;
            var key = port.GetFieldName();
            if (!host.TryGetSourceMemberType(key, out var memberType))
                return null;
            var portType = Preferences.GetPortValueType(port);
            return Preferences.IsCompatible(portType, memberType) ? null : Preferences.DescribeMismatch(key, portType, memberType);
        }
    }
}
