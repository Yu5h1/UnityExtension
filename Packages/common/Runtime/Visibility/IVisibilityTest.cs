namespace Yu5h1Lib
{
    /// <summary>
    /// How a <see cref="VisibilityTracker"/> decides that a target is on screen: put one on the tracker's GameObject
    /// (<see cref="ViewportVisibility"/> for a scroll view's viewport). A camera test is planned on Unity's CullingGroup,
    /// which needs no renderer on content that is not loaded yet (UnityExtension 非同步工作).
    /// </summary>
    public interface IVisibilityTest
    {
        bool IsVisible(VisibilityTarget target);
    }
}
