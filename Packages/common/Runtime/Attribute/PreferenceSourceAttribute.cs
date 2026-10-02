using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Marks a host's source field: the Inspector shows it only when the host declares
    /// <see cref="IPreferences.SourceType"/>, accepts only that type, and lists the members it contributes.
    /// </summary>
    public class PreferenceSourceAttribute : PropertyAttribute { }
}
