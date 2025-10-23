using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

public class AnimationController : ComponentController<Animator>
{
    public Animator animator => component;

    public void SetLayerActive(string layerName,bool active)
    {
        if (animator == null || !isActiveAndEnabled)
            return;
        var layerIndex = animator.GetLayerIndex(layerName);
        if (layerIndex < 0)
            return;
        animator.SetLayerWeight(layerIndex, active ? 1f : 0f);
    }

    public void ActivateLayer(string layerName) => SetLayerActive(layerName, true);
    public void DeactivateLayer(string layerName) => SetLayerActive(layerName, false);
}
