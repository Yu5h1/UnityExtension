using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental
{
    public static class EditorTest
    {
        static void FindScriptableObjects()
        {
            foreach (var item in Resources.FindObjectsOfTypeAll<ScriptableObject>())
            {
                Debug.Log(item.GetType().Name);
            }
        }

        public class TestWindow : EditorWindow
        {
            static TestWindow instance;

            Editor clipEditor;
            PreviewRenderUtility previewUtility;
            object avatarPreview;
            System.Type avatarPreviewType;
            FieldInfo m_PivotPositionOffset;

            Vector3 PivotPositionOffset
            {
                get => (Vector3)m_PivotPositionOffset.GetValue(avatarPreview);
                set => m_PivotPositionOffset.SetValue(avatarPreview, value);
            }
            FieldInfo m_PreviewDir;

            Vector2 PreviewDir
            {
                get => (Vector2)m_PreviewDir.GetValue(avatarPreview);
                set => m_PreviewDir.SetValue(avatarPreview, value);
            }

            FieldInfo m_ZoomFactor;

            private float ZoomFactor
            {
                get => (float)m_ZoomFactor.GetValue(avatarPreview);
                set => m_ZoomFactor.SetValue(avatarPreview, value);
            }

            Vector3 savedOffset;
            Vector2 savedDirection;
            float savedZoomFactor;




            [MenuItem("Test/TestAnimClipEditorWindow")]
            public static void Init()
            {
                instance = ScriptableObject.CreateInstance<TestWindow>();

                instance.Show();
            }
            static void LoadOffset()
            {
                if (instance != null && instance.previewUtility != null)
                {
                    instance.LoadSaved();
                }
            }
            private void OnEnable()
            {

            }
            private void OnGUI()
            {
                if (GUILayout.Button(nameof(GetPreviewRenderUtility)))
                {
                    previewUtility = GetPreviewRenderUtility();
                }
                if (GUILayout.Button(nameof(FindScriptableObjects)))
                {
                    FindScriptableObjects();
                }
                if (previewUtility != null)
                {
                    if (PivotPositionOffset.ChangedCheckField("pos", out Vector3 outPos)) {
                        PivotPositionOffset = outPos;
                        clipEditor.Repaint();
                    }
                    if (PreviewDir.ChangedCheckField("Direction", out Vector2 outDir))
                    {
                        PreviewDir = outDir;
                        clipEditor.Repaint();
                    }
                    if (GUILayout.Button("Save"))
                    {
                        savedOffset = PivotPositionOffset;
                        savedDirection = PreviewDir;
                        savedZoomFactor = ZoomFactor;
                    }
                    if (GUILayout.Button("Load"))
                    {
                        LoadSaved();
                    }
                    EditorGUILayout.Vector3Field("Saved Offset", savedOffset);
                    EditorGUILayout.Vector2Field("Saved Direction", savedDirection);
                    EditorGUILayout.FloatField("Saved ZoomFactor", savedZoomFactor);

                    for (int i = 0; i < previewUtility.lights.Length; i++)
                    {
                        EditorGUILayout.LabelField($"{i}.Light");

                        if (previewUtility.lights[i].transform.position.ChangedCheckField("pos", out Vector3 outLPos))
                        {
                            previewUtility.lights[i].transform.position = outLPos;
                            clipEditor.Repaint();
                        }
                        if (previewUtility.lights[i].transform.eulerAngles.ChangedCheckField("rot", out Vector3 outLRot))
                        {
                            previewUtility.lights[i].transform.eulerAngles = outLRot;
                            clipEditor.Repaint();
                        }
                    }

                }
            }
            public void LoadSaved()
            {
                GetPreviewRenderUtility();
                PivotPositionOffset = savedOffset;
                PreviewDir = savedDirection;
                ZoomFactor = savedZoomFactor;
                clipEditor.Repaint();
            }
            public PreviewRenderUtility GetPreviewRenderUtility()
            {
                clipEditor = null;
                foreach (var editor in Resources.FindObjectsOfTypeAll<Editor>())
                {
                    if (editor.GetType().Name == "AnimationClipEditor")
                    {
                        clipEditor = editor;
                        //Debug.Log($"{editor.GetPreviewTitle()}");
                    }
                }
                if (clipEditor == null)
                {
                    Debug.Log($"clipEditor == null");
                    return null;
                }
                var animClipEditorType = clipEditor.GetType();
                var avaPreviewInfo = animClipEditorType.GetField("m_AvatarPreview", BindingFlags.NonPublic | BindingFlags.Instance);
                avatarPreview = avaPreviewInfo.GetValue(clipEditor);

                avatarPreviewType = avaPreviewInfo.FieldType;
                //Debug.Log($"avatarPreviewType : {avatarPreviewType} ");
                m_PivotPositionOffset = avatarPreviewType.GetField("m_PivotPositionOffset", BindingFlags.NonPublic | BindingFlags.Instance);

                m_PreviewDir = avatarPreviewType.GetField("m_PreviewDir", BindingFlags.NonPublic | BindingFlags.Instance);


                m_ZoomFactor = avatarPreviewType.GetField("m_ZoomFactor", BindingFlags.NonPublic | BindingFlags.Instance);

                //Debug.Log(m_PreviewDir);

                if (avatarPreview == null)
                {
                    Debug.Log("avatarPreview is null");
                    return null;
                }
                //Debug.Log(avatarPreview);

                var previewUtilityValue = avatarPreviewType.GetField("m_PreviewUtility", BindingFlags.NonPublic | BindingFlags.Instance);

                return (PreviewRenderUtility)previewUtilityValue.GetValue(avatarPreview);
            }

        }
    }
}