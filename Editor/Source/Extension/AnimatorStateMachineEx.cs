using System.ComponentModel;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class AnimatorStateMachineEx
    {
        [MenuItem("CONTEXT/AnimatorStateMachine/Sort States")]
        public static void SortStates(MenuCommand command)
        {
            AnimatorStateMachine statemachine = (AnimatorStateMachine)command.context;
            int sqrt = (int)Mathf.Sqrt(statemachine.states.Length);

            var states = statemachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                states[i].position = new Vector3(i % sqrt * 210, Mathf.CeilToInt(i / sqrt) * 55);
            }
            statemachine.parentStateMachinePosition = new Vector3(-200, 0);
            statemachine.anyStatePosition = new Vector3(-175, 50);
            statemachine.entryPosition = new Vector3(-175, 80);
            statemachine.exitPosition = new Vector3(-175, 110);
            statemachine.states = states;
            EditorUtility.SetDirty(statemachine);
            AssetDatabase.SaveAssets();
            var AnimatorControllerToolWindow = EditorWindowUtil.GetExistsWindow("AnimatorControllerTool");
            object[] args = null;
            if (Application.unityVersion == "2019.4.0f1") args = new object[] { true };

            AnimatorControllerToolWindow.GetType().GetMethod("RebuildGraph").Invoke(AnimatorControllerToolWindow, args);
        }
    }
}
