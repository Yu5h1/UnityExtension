using UnityEngine;
using System.CartesianCoordinate;

namespace Yu5h1Lib
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static class TransformEx
    {
        #region Modification
        public static void Reset(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
        public static void Sync(this Transform t,Transform target,bool pos = true,bool rot =false, bool scale = false)
        {
            if (pos)
                t.position = target.position;
            if (rot)
                t.rotation = target.rotation;
            if (scale){
                var s = target.lossyScale;
                var parentScale = t.parent ? t.parent.lossyScale : Vector3.one;
                t.localScale = new Vector3(s.x / parentScale.x, s.y / parentScale.y, s.z / parentScale.z);
            }
            "Why are you use this methods ?".printWarningIf(pos == rot == scale == false);
        }
        #endregion

        #region Find
        public static Transform Find(this Transform t,string text,StringComparisonStyle style,System.StringComparison comparison = System.StringComparison.OrdinalIgnoreCase)
        {
            if (t == null || string.IsNullOrEmpty(text))
                return null;
            foreach (Transform child in t)
            {
                if (child.name.Compare(text, style, comparison))
                    return child;
            }
            return null;
        }
        public static bool TryFind(this Transform t, string name, out Transform result) => result = t.Find(name);
        #endregion

        #region 2D
        public static Vector3 TransformPoint(this Transform t,float x,float y) => t.TransformPoint(new Vector3(x, y));
        #endregion






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
            for (int i = 0; i < transform.transform.childCount; i++) 
                results[i] = transform.transform.GetChild(i);
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
        public static Vector3 GetDirection(this Transform transform,Vector3 point) => (point-transform.position).normalized; 

        #region LookAt
        //public static void LookAt(this Transform transform, Vector3 point, Vector3 upward, Axis axis)
        //    => transform.rotation = Quaternion.LookRotation(transform.GetDirection(point), upward).LookTo(axis);
        //public static void LookAt(this Transform transform, Vector3 point, Vector3 upward, Direction direction)
        //    => transform.rotation = Quaternion.LookRotation(transform.GetDirection(point), upward).LookAt(direction);
        //public static void LookAt(this Transform transform, Vector3 point, Axis axis)
        //    => transform.LookAt(point, transform.up, axis);
        //public static void LookAt(this Transform transform, Transform target, Vector3 upward, Axis axis)
        //    => transform.LookAt(target.position, upward, axis); 
        #endregion

    }
}
