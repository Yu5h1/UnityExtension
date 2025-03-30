using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Yu5h1Lib;
using Yu5h1Lib.Runtime;

public class urpCameraController : BaseMonoBehaviour
{
    [SerializeField, ReadOnly]
    private Camera _camera;
#pragma warning disable 0109
    public new Camera camera => _camera;
#pragma warning restore 0109


    protected override void OnInitializing()
    {
        this.GetComponent(ref _camera);
    }

    void Start() {}
    //public void AddSceneOverlaysAsStacks(Camera owner)
    //{
    //    var ownerData = owner.GetUniversalAdditionalCameraData();
    //    if (ownerData.renderType == CameraRenderType.Overlay)
    //        return;
    //    foreach (var camer in GameObject.FindObjectsOfType<Camera>())
    //    {
    //        if (camer == owner)
    //            continue;
    //        var data = camer.GetUniversalAdditionalCameraData();
    //        if (data.renderType == CameraRenderType.Overlay)
    //            ownerData.cameraStack.Add(camer);

    //    }
    //    owner.Render();
    //}
    public void AddSceneOverlaysAsStacks(Camera owner)
    {
        var ownerData = owner.GetUniversalAdditionalCameraData();

        if (ownerData.renderType == CameraRenderType.Overlay)
            return;

        ownerData.cameraStack.Clear();

        foreach (var camer in GameObject.FindObjectsOfType<Camera>())
        {
            if (camer == owner) continue;

            var data = camer.GetUniversalAdditionalCameraData();

            if (data.renderType == CameraRenderType.Overlay)
            {
                if (!ownerData.cameraStack.Contains(camer))
                {
                    ownerData.cameraStack.Add(camer);
                }
            }
        }

        ForceRender(owner);
    }

    private void ForceRender(Camera cam)
    {
        cam.Render();
        Graphics.ExecuteCommandBuffer(new UnityEngine.Rendering.CommandBuffer());
    }
}
