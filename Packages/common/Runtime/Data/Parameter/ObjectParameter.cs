using UnityEngine;

namespace Yu5h1Lib.Parameter
{
    /// <summary>
    /// One concrete ParameterBehaviour that holds any UnityEngine.Object reference (Transform, GameObject,
    /// Component, asset, ...). Avoids creating a typed behaviour per Object subclass.
    /// The value is written to the target member matching the value's <b>concrete</b> type, so assign the
    /// correctly-typed object (e.g. a Transform when the target property is Transform).
    /// </summary>
    public class ObjectParameter : ParameterBehaviour<Object> { }
}
