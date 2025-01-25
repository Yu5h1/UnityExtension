using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class CameraEx
    {
        public static void GetOrthographicSize(this Camera camera, out float width, out float height)
        {
            height = camera.orthographicSize * 2;
            width = height * camera.aspect;
        }
        public static void GetPerspectiveSize(this Camera camera, float depth, out float width, out float height)
        {
            width = height = 0;
            if ("You cannot get PerspectiveSize in orthographic mode.".printWarningIf(camera.orthographic))
                return;
            float verticalFOVRad = camera.fieldOfView * Mathf.Deg2Rad;
            height = 2f * depth * Mathf.Tan(verticalFOVRad / 2f);
            width = height * camera.aspect;
        }
        public static Vector2 GetNormalizedCoordinates(this Camera cam, Vector2 screenPoint)
        {
            var result = cam.ScreenToViewportPoint(screenPoint);
            return new Vector3(2 * result.x - 1, 2 * result.y - 1, result.z);
        }
    }
}
