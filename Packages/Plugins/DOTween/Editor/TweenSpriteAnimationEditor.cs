using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Yu5h1Lib.EditorExtension;
using Yu5h1Lib;
using DG.Tweening;

[CustomEditor(typeof(DOSpriteAnimation))]
public class TweenSpriteAnimationEditor : TweenBehaviourEditor<DOSpriteAnimation>
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
};