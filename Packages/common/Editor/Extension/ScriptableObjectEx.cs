using System.ComponentModel;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.Common
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class ScriptableObjectEx
    {
        public const string MENU_PATH = "CONTEXT/ScriptableObject/";
        public const string Ping_PATH = MENU_PATH + "Ping";

        [MenuItem(Ping_PATH)]
        private static void Ping(MenuCommand command)
        {
            ScriptableObject obj = command.context as ScriptableObject;
            EditorGUIUtility.PingObject(obj);

        }
    }
}
