using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// A target is visible when the bounds of any of its renderers enter this Camera's frustum. Put it with a
    /// <see cref="VisibilityTracker"/> (scope Scene) on the camera. A target without renderers is never visible.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraVisibility : MonoBehaviour, IVisibilityTest
    {
        private readonly Plane[] _planes = new Plane[6];
        private Camera _camera;
        private int _frame = -1;

        public bool IsVisible(VisibilityTarget target)
        {
            if (_frame != Time.frameCount)
            {
                if (_camera == null)
                    _camera = GetComponent<Camera>();
                GeometryUtility.CalculateFrustumPlanes(_camera, _planes);
                _frame = Time.frameCount;
            }
            foreach (var renderer in target.renderers)
                if (renderer != null && renderer.enabled && GeometryUtility.TestPlanesAABB(_planes, renderer.bounds))
                    return true;
            return false;
        }
    }
}
