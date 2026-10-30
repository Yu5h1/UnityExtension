namespace Yu5h1Lib
{
    /// <summary>
    /// How a <see cref="VisibilityTracker"/> decides that a target is on screen: put one on the tracker's GameObject
    /// (<see cref="ViewportVisibility"/> for a scroll view's viewport, <see cref="CameraVisibility"/> for a camera).
    /// </summary>
    public interface IVisibilityTest
    {
        bool IsVisible(VisibilityTarget target);
    }
}
