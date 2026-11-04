namespace Yu5h1Lib.CameraControl
{
    /// <summary>
    /// Turns a consumer's devices into control intents. It reads its own buttons, drags, touches and UI focus and calls
    /// <see cref="CameraController"/> methods; the controller never reads a device. Put the implementing component on the
    /// controller's GameObject, or hand any object to <see cref="CameraController.AddInputSource"/>.
    /// </summary>
    /// <remarks>
    /// Every amount passed to the controller is for this call, already scaled by the source: degrees for
    /// <see cref="CameraController.Orbit"/>, world units for <see cref="CameraController.Pan"/> and
    /// <see cref="CameraController.Dolly"/>, the projection's own unit for the other zooms. A pointer delta or a wheel
    /// notch is passed as it is; a held stick or key is a rate, so multiply it by <paramref name="deltaTime"/> first.
    /// </remarks>
    public interface ICameraInputSource
    {
        void Feed(CameraController controller, float deltaTime);
    }
}
