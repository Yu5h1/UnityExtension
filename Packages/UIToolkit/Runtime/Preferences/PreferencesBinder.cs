using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Yu5h1Lib;
using Yu5h1Lib.MVVM;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Binds a <see cref="UIDocument"/>'s value elements to a <see cref="IPreferences"/> host (D4).
    /// <c>UIDocument</c> rebuilds <c>rootVisualElement</c> on every <c>OnEnable</c>, so this re-scans
    /// and re-binds every time it is enabled too, rather than once in <c>Start</c> like the uGUI path.
    /// Lifecycle is owned by whatever owns this GameObject; <c>Preferences</c> itself stays unaware
    /// UI Toolkit exists.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PreferencesBinder : MonoBehaviour
    {
        [SerializeField, TypeRestriction(typeof(IPreferences))] private Object _preferences;
        public IPreferences preferences => _preferences as IPreferences;

        private UIDocument uiDocument;
        private readonly List<IValuePort> boundPorts = new List<IValuePort>();

        private void OnEnable()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;
            if (root == null)
            {
                $"PreferencesBinder on {name}: UIDocument has no rootVisualElement.".printWarning();
                return;
            }
            if (preferences == null)
            {
                $"PreferencesBinder on {name}: no Preferences assigned.".printWarning();
                return;
            }

            boundPorts.Clear();
            root.Query().ForEach(element =>
            {
                if (!(element is IBindable bindable) || string.IsNullOrEmpty(bindable.bindingPath))
                    return;
                var port = VisualElementPortFactory.TryCreate(element);
                if (port == null)
                    return;
                boundPorts.Add(port);
                preferences.BindPort(port);
            });
        }

        private void OnDisable()
        {
            foreach (var port in boundPorts)
            {
                if (preferences != null)
                    preferences.UnbindPort(port);
                else
                    port.Unbind();
            }
            boundPorts.Clear();
        }
    }
}
