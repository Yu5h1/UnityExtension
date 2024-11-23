using UnityEngine;
using System.CartesianCoordinate;

namespace Yu5h1Lib
{
    public static class TransformEx
    {
			public static bool TryFind(this Transform t,string name, out Transform result)
        => result = t.Find(name);
    #region 2D
    public static Vector3 TransformPoint(this Transform t,float x,float y) => t.TransformPoint(new Vector3(x, y));
    #endregion
		
		
        public static Vector3 back(this Transform transform) { return -transform.forward; }
        public static Vector3 down(this Transform transform) { return -transform.up; }
        public static Vector3 left(this Transform transform) { return -transform.right; }

        /// <summary>
        /// reset local postion and rotation
        /// </summary>
        /// <param name="transform"></param>
        public static void Reset(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;
            transform.localScale = Vector3.one;
        }

        public static Vector3 GetDirection(this Transform transform, Direction directdironType)
        {
            return transform.TransformDirection(directdironType.ToVector3());
        }
        public static void fromMatrix(this Transform transform, Matrix4x4 matrix)
        {
            transform.position = matrix.GetPosition();
            transform.rotation = matrix.GetRotation();
        }
        public static Vector3 Center(this Transform[] transforms)
        {
            return Vector3.zero;
        }
        public static Transform[] GetChildren(this Transform transform)
        {
            Transform[] results = new Transform[transform.childCount];
            for (int i = 0; i < transform.transform.childCount; i++) results[i] = transform.transform.GetChild(i);
            return results;
        }
        public static GameObject[] ToGameObjects(this Transform[] transforms)
        {
            GameObject[] results = new GameObject[transforms.Length];
            for (int i = 0; i < results.Length; i++)
            {
                if (transforms[i] != null)
                    results[i] = transforms[i].gameObject;
            }
            return results;
        }
        public static T Find<T>(this Transform transform,string name) where T : Component
        {
            var child = transform.Find(name);
            if (child != null) return transform.GetComponent<T>();
            return null;
        }
        public static void SetPosition(this Transform transform,float x, float? y = null, float? z = null, Space space = Space.World) {
            Vector3 pos = space == Space.World ? transform.position : transform.localPosition ;
            pos.x = x;
            if (y != null)
                pos.y = y.Value;
            if (z != null)
                pos.z = z.Value;
            if (space == Space.World) transform.position = pos;
            else transform.localPosition = pos;
        }
        public static void SetPosition(this Transform transform,Vector2 position, Space space = Space.World)
        {
            Vector3 pos = space == Space.World ? transform.position : transform.localPosition;
            pos.x = position.x;
            pos.y = position.y;
            if (space == Space.World) transform.position = pos;
            else transform.localPosition = pos;
        }
        public static Vector3 GetDirection(this Transform transform,Vector3 point)
        { return (point-transform.position).normalized; }

        public static void LookAt(this Transform transform,Vector3 point, Vector3 upward, Axis axis)
            => transform.rotation = Quaternion.LookRotation(transform.GetDirection(point), upward).LookTo(axis);

        public static void LookAt(this Transform transform, Vector3 point, Vector3 upward, Direction direction)
            => transform.rotation = Quaternion.LookRotation(transform.GetDirection(point), upward).LookAt(direction);

        //public static void LookAt(this Transform transform, Vector3 point, Direction direction)
        //    => transform.rotation = Quaternion.LookRotation(transform.GetDirection(point), transform.up).LookAt(direction);

        public static void LookAt(this Transform transform, Vector3 point, Axis axis)
            => transform.LookAt(point, transform.up, axis);

        //public static void LookAt(this Transform transform, Transform target, Axis axis)
        //    => transform.LookAt(target.position, axis); 

        public static void LookAt(this Transform transform, Transform target, Vector3 upward, Axis axis)
            => transform.LookAt(target.position, upward, axis);

    }
}
