using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public static class CameraEx
    {
        public static void AddSceneOverlaysAsStacks(this Camera camera)
           => AddCamerasAsStacks(camera, GameObject.FindObjectsOfType<Camera>(true));

        public static void ClearStacks(this Camera cam)
        {
            var mainCamData = cam.GetUniversalAdditionalCameraData();
            mainCamData.cameraStack.Clear();
        }

        public static void AddCamerasAsStacks(this Camera main, params Camera[] overlays)
        {
            var mainCamData = main.GetUniversalAdditionalCameraData();

            if (mainCamData.renderType == CameraRenderType.Overlay)
                return;

            mainCamData.cameraStack.Clear();

            foreach (var cam in overlays)
            {
                if (main == cam) continue;

                var data = cam.GetUniversalAdditionalCameraData();
                if (data.renderType == CameraRenderType.Overlay)
                {
                    if (!mainCamData.cameraStack.Contains(cam))
                        mainCamData.cameraStack.Add(cam);
                }
            }
            main.Render();
            Graphics.ExecuteCommandBuffer(new UnityEngine.Rendering.CommandBuffer());
        }
        public static void SetRenderType(this Camera cam, CameraRenderType renderType)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderType = renderType;
        }


    }

}