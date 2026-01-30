using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib
{
    [InitializeOnLoad]
    public static class UndoAdvanced
    {
        static System.Action pending;

        static UndoAdvanced()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public static void Delay(System.Action action)
        {
            pending = action;
        }

        static void OnUndoRedo()
        {
            if (pending == null)
                return;

            EditorApplication.delayCall += () =>
            {
                pending?.Invoke();
                pending = null;
            };
        }
    }
}
