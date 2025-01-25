using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    public static class CreationUtil
    {
        public static T UndoGetOrAddComponet<T>(this Component target) where T : Component
        {
            var result = target.gameObject.GetComponent<T>();
            return result ? result : Undo.AddComponent<T>(target.gameObject);
        }
    }
}
