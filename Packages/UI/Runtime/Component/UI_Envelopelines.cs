using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class EnvelopeLines : MaskableGraphic
    {
        [SerializeField] private string defaultShader = "UI/EnvelopeLinesAA";

        [Header("Line Settings")]
        public float lineThickness = 1f;

        [Header("Text Integration")]
        public TextAdapter adapter;
        public bool followTextSettings = true;
        public float lineSpacingMultiplier = 1f;

        [Header("Manual Settings (when not following text)")]
        public float manualLineSpacing = 20f;
        public float startOffset = 0f;

        private Material lineMaterial;
        private float currentLineSpacing;
        private float currentStartOffset;

        protected override void Start()
        {
            base.Start();

            if (lineMaterial == null)
            {
                Shader shader = Shader.Find(defaultShader);
                if (shader != null)
                {
                    lineMaterial = new Material(shader);
                    material = lineMaterial;
                }
                else
                {
                    Debug.LogError("找不到 UI/EnvelopeLinesAA shader");
                }
            }
            RefreshFromTextAfterFrame();
        }
        public void RefreshFromTextAfterFrame()
        {
            if (!isActiveAndEnabled || adapter == null)
                return;
            StartCoroutine(WaitInvoke(RefreshFromText));
        }
        private IEnumerator WaitInvoke(UnityAction action,YieldInstruction instruction = null)
        {
            yield return instruction;
            yield return null;
            action();
        }
        private void CalculateLineSettings()
        {
            adapter.ForceUpdate();
            if (followTextSettings && adapter != null)
            {
                currentLineSpacing = adapter.GetWrapDistance();
                currentStartOffset = adapter.GetFirstLineOffsetY();
            }
            else
            {
                currentLineSpacing = manualLineSpacing;
                currentStartOffset = startOffset;
            }
        }
        


        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            var modified = base.GetModifiedMaterial(baseMaterial);
            ApplyShaderParameters(modified);
            return modified;
        }

        private void ApplyShaderParameters(Material mat)
        {
            if (mat == null) return;

            float rectHeight = rectTransform.rect.height;

            float pixelLineSpacing = Mathf.Round(currentLineSpacing);
            float pixelThickness = Mathf.Max(1f, Mathf.Round(lineThickness));
            float pixelOffset = Mathf.Round(currentStartOffset);

            float uvSpacing = pixelLineSpacing / rectHeight;
            float uvThickness = pixelThickness / rectHeight;
            float uvOffset = pixelOffset / rectHeight;

            mat.SetFloat("_LineSpacing", uvSpacing);
            mat.SetFloat("_LineThickness", uvThickness);
            mat.SetFloat("_StartOffset", uvOffset);

            mat.SetFloat("_RectHeight", rectHeight);
            mat.SetFloat("_PixelLineSpacing", pixelLineSpacing);
            mat.SetFloat("_PixelThickness", pixelThickness);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = new Vector3(rect.xMin, rect.yMin);
            vertex.uv0 = new Vector2(0, 0);
            vh.AddVert(vertex);

            vertex.position = new Vector3(rect.xMax, rect.yMin);
            vertex.uv0 = new Vector2(1, 0);
            vh.AddVert(vertex);

            vertex.position = new Vector3(rect.xMax, rect.yMax);
            vertex.uv0 = new Vector2(1, 1);
            vh.AddVert(vertex);

            vertex.position = new Vector3(rect.xMin, rect.yMax);
            vertex.uv0 = new Vector2(0, 1);
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            RefreshFromText();
        }
#endif

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (!isActiveAndEnabled)
                return;
            RefreshFromText();
        }

        public void RefreshFromText()
        {
            if (followTextSettings && adapter != null)
            {
                CalculateLineSettings();
                ApplyShaderParameters(materialForRendering);
                SetVerticesDirty(); 
            }
        }
    }
}
