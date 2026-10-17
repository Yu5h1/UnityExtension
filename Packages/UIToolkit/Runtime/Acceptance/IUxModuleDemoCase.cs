using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit.Acceptance
{
    /// <summary>
    /// Contract for an acceptance demo case of a referenced UX module (e.g. Button, OptionPicker).
    /// Conforms to referenced-module-acceptance shared acceptance application specification.
    /// </summary>
    public interface IUxModuleDemoCase
    {
        /// <summary>Stable unique identifier, e.g. "button", "optionpicker".</summary>
        string Id { get; }

        /// <summary>Human-readable display name, e.g. "UX Button (2.0.0)".</summary>
        string DisplayName { get; }

        /// <summary>Authoritative specification route, e.g. "Yu5h1Lib.UX/Button".</summary>
        string ModuleSpec { get; }

        /// <summary>Mounts the module demo into the provided UI container.</summary>
        void Mount(VisualElement container);

        /// <summary>Unmounts the demo and releases any resources or callbacks.</summary>
        void Unmount();
    }
}
