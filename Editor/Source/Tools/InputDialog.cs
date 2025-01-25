using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    public class InputDialog : EditorWindow
    {
        public static void Popup()
        {
            InputDialog window = ScriptableObject.CreateInstance(typeof(InputDialog)) as InputDialog;
            window.ShowPopup();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 150);
            
            window.Focus();
        }
        void OnGUI()
        {
            EditorGUILayout.TextField("test");
        }
        void OnLostFocus()
        {
            Close();
        }
    }
}
