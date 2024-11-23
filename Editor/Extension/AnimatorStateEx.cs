using UnityEditor.Animations;
using UnityEditor;
using System.ComponentModel;

namespace Yu5h1Lib.EditorExtension
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class AnimatorStateEx
    {
        [MenuItem("CONTEXT/AnimatorState/Add Exit Transition")]
        public static void AddExitTransition(MenuCommand command)
        {
            AnimatorState state = (AnimatorState)command.context;
            var t = state.AddExitTransition();
            t.exitTime = 1;
            t.duration = 0;
            t.hasExitTime = true;
        }
        [MenuItem("CONTEXT/AnimatorStateTransition/ZeroDuration")]
        public static void ZeroDuration(MenuCommand command)
        {
            var t = (AnimatorStateTransition)command.context;
            t.duration = 0;
        }
        [MenuItem("CONTEXT/AnimatorStateTransition/Set Full Exit Time")]
        public static void SetTransitionTimeToFull(MenuCommand command)
        {
            AnimatorStateTransition t = (AnimatorStateTransition)command.context;
            t.exitTime = 1;
            t.duration = 0;
        }
    } 
}
