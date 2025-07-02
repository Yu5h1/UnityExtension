using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class EnvelopeLines : MaskableGraphic
    {
        [Header("Line Settings")]
        public float lineThickness = 1f;

        [Header("Text Integration")]
        public TextAdapter adapter;
        public bool followTextSettings = true;
        public float lineSpacingMultiplier = 1f; // 可以調整行距倍數    

        [Header("Manual Settings (when not following text)")]
        public float manualLineSpacing = 20f;
        public float startOffset = 0f;

        private Material lineMaterial;
        private float currentLineSpacing;
        private float currentStartOffset;

        [SerializeField] private string defaultShader = "UI/EnvelopeLinesAA";

        protected override void Start()
        {
            base.Start();
            // 創建材質實例
            if (lineMaterial == null)
            {
                Shader shader = Shader.Find("UI/EnvelopeLinesAA");
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
            UpdateLineSettings();

            // 如果有指定文字組件，監聽其變化
            if (followTextSettings && adapter != null)
            {
                CalculateLineSettings();
            }

            // 確保這個組件參與遮罩系統
            maskable = true;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            CalculateLineSettings();
            UpdateLineSettings();
            SetVerticesDirty();
        }

        private void CalculateLineSettings()
        {
            if (followTextSettings && adapter != null)
            {
                // 使用擴充方法獲取實際字體大小
                float actualFontSize = adapter.GetActualFontSize();

                // 根據實際字體大小計算行距
                currentLineSpacing = adapter.GetWrapDistance();

                currentStartOffset = adapter.GetFirstLineOffsetY();
            }
            else
            {
                currentLineSpacing = manualLineSpacing;
                currentStartOffset = startOffset;
            }
        }

        private void UpdateLineSettings()
        {
            if (lineMaterial != null)
            {
                RectTransform rectTransform = GetComponent<RectTransform>();
                float rectHeight = rectTransform.rect.height;

                // 確保行距是整數像素，避免精度問題
                float pixelLineSpacing = Mathf.Round(currentLineSpacing);
                float pixelThickness = Mathf.Max(1f, Mathf.Round(lineThickness));
                float pixelOffset = Mathf.Round(currentStartOffset);

                // 將像素間距和偏移轉換為 UV 座標
                float uvSpacing = pixelLineSpacing / rectHeight;
                float uvThickness = pixelThickness / rectHeight;
                float uvOffset = pixelOffset / rectHeight;

                lineMaterial.SetFloat("_LineSpacing", uvSpacing);
                lineMaterial.SetFloat("_LineThickness", uvThickness);
                lineMaterial.SetFloat("_StartOffset", uvOffset);

                // 傳遞實際像素尺寸給 shader 做更精確的計算
                lineMaterial.SetFloat("_RectHeight", rectHeight);
                lineMaterial.SetFloat("_PixelLineSpacing", pixelLineSpacing);
                lineMaterial.SetFloat("_PixelThickness", pixelThickness);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            RectTransform rectTransform = GetComponent<RectTransform>();
            Rect rect = rectTransform.rect;

            // 創建四個頂點來覆蓋整個矩形
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            // 左下角
            vertex.position = new Vector3(rect.xMin, rect.yMin);
            vertex.uv0 = new Vector2(0, 0);
            vh.AddVert(vertex);

            // 右下角
            vertex.position = new Vector3(rect.xMax, rect.yMin);
            vertex.uv0 = new Vector2(1, 0);
            vh.AddVert(vertex);

            // 右上角
            vertex.position = new Vector3(rect.xMax, rect.yMax);
            vertex.uv0 = new Vector2(1, 1);
            vh.AddVert(vertex);

            // 左上角
            vertex.position = new Vector3(rect.xMin, rect.yMax);
            vertex.uv0 = new Vector2(0, 1);
            vh.AddVert(vertex);

            // 添加三角形
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            CalculateLineSettings();
            UpdateLineSettings();
            SetVerticesDirty();
        }

        // 公開方法，讓外部可以強制更新
        public void RefreshFromText()
        {
            if (followTextSettings && adapter != null)
            {
                CalculateLineSettings();
                UpdateLineSettings();
                SetVerticesDirty();
            }
        }
    } 
}