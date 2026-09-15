using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

public class AnimationController : ComponentController<Animator>
{
    public Animator animator => component;
    private HashSet<StateBehaviour> stateBehaviours = new HashSet<StateBehaviour>();
    public StateBehaviour currentState { get; private set; }

    public bool IsCurrentStateName(string n)
    {
        if (currentState == null)
            return false;
        return currentState.name == n;
    }
    public void Join(StateBehaviour state)
    {
        stateBehaviours.Add(state);
        state.entered += StateEntered;
        state.exited += StateExited; 
    }
    private void StateEntered(StateBehaviour s)
    {
        currentState = s;
    }
    private void StateExited(StateBehaviour s)
    {
        if (currentState == s)
            currentState = null;
    }



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
