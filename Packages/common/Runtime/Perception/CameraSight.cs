using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Sees a target when the bounds of its renderers enter the frustum of the Camera on the perceiver's GameObject.
    /// A target without renderers is never seen.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraSight : MonoBehaviour, ISightStrategy
    {
        private readonly Plane[] _planes = new Plane[6];
        private Camera _camera;
        private int _frame = -1;

        private void Awake() => _camera = GetComponent<Camera>();

        public bool Check(IPerceiver perceiver, IPerceptible target)
        {
            if (!(target is Component seen))
                return false;
            if (_frame != Time.frameCount)
            {
                GeometryUtility.CalculateFrustumPlanes(_camera, _planes);
                _frame = Time.frameCount;
            }
            foreach (var renderer in seen.GetComponentsInChildren<Renderer>())
                if (GeometryUtility.TestPlanesAABB(_planes, renderer.bounds))
                    return true;
            return false;
        }
    }
}
