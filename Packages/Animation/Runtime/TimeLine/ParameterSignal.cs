using UnityEngine;

namespace Yu5h1Lib.Timeline
{
    /// <summary>
    /// Timeline marker carrying a <see cref="ParameterObject"/> asset.
    /// The asset's name is the contract: it maps to the target member written by ApplyTo.
    /// Shared by <see cref="ParameterReceiver"/> (re-emit) and <see cref="ParameterApplier"/> (reflect-write).
    /// </summary>
    public class ParameterSignal : SignalMarker<ParameterSignal, ParameterObject> { }
}
