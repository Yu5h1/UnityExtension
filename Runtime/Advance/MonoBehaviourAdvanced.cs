using UnityEngine;

namespace Yu5h1Lib.Runtime
{
    public abstract class MonoBehaviour3D : BaseMonoBehaviour
    {
        public Vector3 up => transform.up;
        public Vector3 down => -up;
        public Vector3 right => transform.right;
        public Vector3 left => -right;
        public Vector3 forward => transform.forward;
        public Vector3 backward => -forward;
        public Vector3 position => transform.position;
        public Quaternion rotation => transform.rotation;

        public void LookAt(Vector3 direction, Vector3 worldUp)
        {
            transform.LookAt(transform.position + direction, worldUp);
        }
        public Vector3 InverseTransformDirection(Vector3 dir)
        {
            return transform.InverseTransformDirection(dir);
        }
    }
}