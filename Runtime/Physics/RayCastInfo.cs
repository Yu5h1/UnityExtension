using UnityEngine;

namespace Yu5h1Lib.Runtime
{
    [System.Serializable]
    public struct RayCastInfo
    {
        public Ray ray;
        public Vector3 origin { get { return ray.origin; } set { ray.origin = value; } }
        public Vector3 direction { get { return ray.direction; } set { ray.direction = value; } }
        [SerializeField]
        public float Distance;
        public RaycastHit HitInfo;
        public Vector3 hitPoint { get { return HitInfo.point; } }
        public bool IsHited { get { return HitInfo.collider != null; } }
        public void show()
        {
            ray.show(HitInfo, Distance);
        }
        public RayCastInfo(Vector3 origin, Vector3 direction, float distance, RaycastHit hitInfo = default(RaycastHit))
        {
            ray = new Ray(origin, direction);
            Distance = distance;
            HitInfo = hitInfo;
        }
        public bool Cast(LayerMask layerMask)
        {
            return Physics.Raycast(ray, out HitInfo, Distance, layerMask);
        }
        public bool Cast()
        {
            return Physics.Raycast(ray, out HitInfo, Distance);
        }
        public bool SphereCast(LayerMask layerMask,float radius)
        {
            return Physics.SphereCast(ray,radius, out HitInfo, Distance, layerMask);
        }
    }
}
