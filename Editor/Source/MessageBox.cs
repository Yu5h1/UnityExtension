using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    public static class MessageBox
    {
        public static void Show(object message, string title = "Prompt Dialog",string buttonText = "OK") {
            EditorUtility.DisplayDialog(title, message.ToString(), buttonText);
        }
    }
}
