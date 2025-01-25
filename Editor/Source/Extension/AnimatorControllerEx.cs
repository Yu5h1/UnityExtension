using System.ComponentModel;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class AnimatorControllerEx
    {
        [MenuItem("CONTEXT/AnimatorController/Get Parameters")]
        public static void GenerateReadonlyParamaters(MenuCommand command)
        {
            var a = (AnimatorController)command.context;
            string parameterscode = "";
            foreach (var item in a.parameters)
            {
                string pID = item.name + "_id";
                parameterscode += "readonly int " + pID + " = Animator.StringToHash(\"" + item.name + "\");\n";
                string ptype = item.type.ToString();
                parameterscode += ptype.ToLower() + " " + item.name + " {\n" +
                "get{ return animator.Get" + ptype + "(" + pID + "); }\n" +
                "set{ animator.Set" + ptype + "(" + pID + ", value); }\n}\n";
            }
            EditorGUIUtility.systemCopyBuffer = parameterscode;
            parameterscode.print();
        }
    }
}
