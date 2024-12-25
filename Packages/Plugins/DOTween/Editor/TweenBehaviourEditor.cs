using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Yu5h1Lib.EditorExtension;
using Yu5h1Lib;
using DG.Tweening;
using System.Reflection;
using UnityEngine.UIElements;
using UnityEngine.UI;

public abstract class TweenBehaviourEditor<T> : Editor<T> where T : TweenBehaviour
{
	public float normalized;
    Tweener tweener => targetObject.tweener;
    Component component;
    protected void OnEnable()
    {
        if (EditorApplication.isPlaying)
            return;
        targetObject.Kill();
        targetObject.Init();
        //$"Initinalized:{targetObject.IsInitinalized}".print();
        var componentProp = targetObject.GetType().GetProperty("component");
        if (componentProp != null)
            component = (Component)componentProp.GetValue(targetObject, new object[0]);
    }
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (this.TrySlider("Simulation:", normalized, 0, 1, out float result))
        {
            normalized = result;
            tweener.Goto(normalized * targetObject.Duration, true);
            switch (component)
            {
                case MaskableGraphic graphic:
                    graphic.Rebuild(CanvasUpdate.PreRender);
                    break;
            }
            SceneView.RepaintAll();
        }
    }
    protected void OnDisable()
    {
        targetObject.Kill();
    }
};