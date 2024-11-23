using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Yu5h1Lib.Runtime
{
    public abstract class CameraController : MonoBehaviour
    {
        [SerializeField]
        protected Camera _camera;
        public new Camera camera => _camera;
        public void LookAt3rdPerson(Vector2 delta, Transform viewer, Vector3 pivot, ref Vector2 angle, MinMax AngleRangeX,MinMax AngleRangeY,float distance)
        {
            angle.x += delta.x;
            angle.y -= delta.y;
            angle.x = Mathf.Clamp(angle.x, AngleRangeX.Min, AngleRangeX.Max);
            angle.y = Mathf.Clamp(angle.y, AngleRangeY.Min, AngleRangeY.Max);
            var direction = new Vector3(0, 0, -distance);
            var rotation = Quaternion.Euler(angle.y, angle.x, 0);
            viewer.position = pivot + rotation * direction;
            camera.transform.LookAt(pivot);
        }
    }
}
