using UnityEngine;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>
    /// Contract for an acceptance demo case of a referenced UX module in Yu5h1Lib.UI (UGUI).
    /// Conforms to referenced-module-acceptance shared acceptance application specification.
    /// </summary>
    public interface IUxUiModuleDemoCase
    {
        /// <summary>Stable unique identifier, e.g. "button".</summary>
        string Id { get; }

        /// <summary>Human-readable display name, e.g. "Button (2.0.0)".</summary>
        string DisplayName { get; }

        /// <summary>Authoritative specification route, e.g. "Yu5h1Lib.UX/Button".</summary>
        string ModuleSpec { get; }

        /// <summary>Mounts the module demo into the provided UGUI container.</summary>
        void Mount(RectTransform container);

        /// <summary>Unmounts the demo and releases any created GameObjects or resources.</summary>
        void Unmount();
    }
}
