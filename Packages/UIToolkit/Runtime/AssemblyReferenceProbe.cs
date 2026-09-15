// TEMPORARY - step 1 pipeline probe only. Delete once the first real type lands in this package.
// An asmdef with no source file produces no assembly, so its references compile-check nothing.
// This touches one type from each declared reference plus the UI Toolkit engine module.
using System;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>Proves that <c>Yu5h1Lib.Common</c>, <c>Yu5h1Lib.Animation</c> and UI Toolkit all resolve here.</summary>
    internal static class AssemblyReferenceProbe
    {
        internal static Type FromCommon => typeof(Optional<int>);
        internal static Type FromAnimation => typeof(ILocomotor);
        internal static Type FromUIToolkit => typeof(VisualElement);
    }
}
