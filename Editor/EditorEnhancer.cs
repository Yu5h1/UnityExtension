using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib;

using Type = System.Type;

namespace Yu5h1Lib.EditorExtension
{
    public abstract class EditorEnhancer : Editor
    {
        //	// empty array for invoking methods using reflection
        //	private static readonly object[] EMPTY_ARRAY = new object[0];

        #region Editor Fields

        /// <summary>
        /// Mostly internal types
        /// </summary>
        private Type TargetEditorType;

        /// <summary>
        /// Type object for the object that is edited by this editor.
        /// </summary>
        private Type editedObjectType;

        private Editor editorInstance;
        protected Editor instance
        {
            get
            {
                if (editorInstance == null && !targets.IsEmpty())
                    editorInstance = Editor.CreateEditor(targets, TargetEditorType);

                if (editorInstance == null)
                    "Could not create editor !".printError();
                else
                    OnCreateOriginalEditor();
                return editorInstance;
            }
        }

        #endregion

        private static Dictionary<string, MethodInfo> decoratedMethods = new Dictionary<string, MethodInfo>();


        public EditorEnhancer(string typeName)
        {
            TargetEditorType = FindFromAssembly(typeName);

            Init();
            // Check CustomEditor types.
            var originalEditedType = GetCustomEditorType(TargetEditorType);
            if (originalEditedType != editedObjectType)
            {
                throw new System.ArgumentException(
                    string.Format("Type {0} does not match the editor {1} type {2}",
                              editedObjectType, typeName, originalEditedType));
            }
        }
        protected void OnCreateOriginalEditor()
        {

        }
        private Type GetCustomEditorType(Type type)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var attributes = type.GetCustomAttributes(typeof(CustomEditor), true) as CustomEditor[];
            var field = attributes.Select(editor => editor.GetType().GetField("m_InspectedType", flags)).First();

            return field.GetValue(attributes[0]) as Type;
        }

        private void Init()
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var attributes = this.GetType().GetCustomAttributes(typeof(CustomEditor), true) as CustomEditor[];
            var field = attributes.Select(editor => editor.GetType().GetField("m_InspectedType", flags)).First();

            editedObjectType = field.GetValue(attributes[0]) as System.Type;
        }
        protected virtual void OnEnable()
        {

        }
        protected virtual void OnDisable()
        {
            if (editorInstance != null)
            {
                DestroyImmediate(editorInstance);
            }
        }
        protected void InvokeTargetInspectorMethod(string methodName)
        {
            MethodInfo method = null;
            // Add MethodInfo to cache
            if (!decoratedMethods.ContainsKey(methodName))
            {
                var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

                method = TargetEditorType.GetMethod(methodName, flags);

                if (method != null)
                {
                    decoratedMethods[methodName] = method;
                }
                else
                {
                    $"Could not find method {methodName} ".printWarning();
                    return;
                }
            }
            method = decoratedMethods[methodName];
            if (method == null)
            {
                $"Method Key {methodName} value is null".printWarning();
                return;
            }
            method.Invoke(instance, System.Array.Empty<object>());
        }

        #region override
        protected override void OnHeaderGUI()
        {
            //base.OnHeaderGUI();
            InvokeTargetInspectorMethod("OnHeaderGUI");
        }

        public override void OnInspectorGUI()
        {
            instance.OnInspectorGUI();
        }

        public override void DrawPreview(Rect previewArea)
        {
            //base.DrawPreview(previewArea);
            //if (EditorInstance == null)
            //{
            //	Debug.LogWarning("null EditorInstance");
            //          return;
            //      }
            instance.DrawPreview(previewArea);
        }
        public override string GetInfoString()
        {
            return instance.GetInfoString();
        }

        public override GUIContent GetPreviewTitle()
        {
            return instance.GetPreviewTitle();
        }
        public override bool HasPreviewGUI() => instance.HasPreviewGUI();

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            instance.OnInteractivePreviewGUI(r, background);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            instance.OnPreviewGUI(r, background);
        }

        public override void OnPreviewSettings()
        {
            instance?.OnPreviewSettings();
        }

        public override void ReloadPreviewInstances()
        {
            instance.ReloadPreviewInstances();
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
            => instance.RenderStaticPreview(assetPath, subAssets, width, height);

        public override bool RequiresConstantRepaint() => instance.RequiresConstantRepaint();
        public override bool UseDefaultMargins() => instance.UseDefaultMargins();
        #endregion

        #region Static

        public static Editor FindInstance(System.Predicate<Editor> predicate)
            => Resources.FindObjectsOfTypeAll<Editor>().Find(predicate);
        public static Type FindFromAssembly(string typeName)
             => Assembly.GetAssembly(typeof(Editor)).GetTypes().Find(t => t.Name == typeName);
        #endregion

    }
}
