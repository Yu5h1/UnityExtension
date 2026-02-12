using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI.Effects
{
    /// <summary>
    /// UGUI 漸層效果
    /// 適用於 Image, RawImage, Text 等所有繼承 Graphic 的組件
    /// 對於簡單 quad (Image/RawImage) 會自動切分頂點以支援多色漸層
    /// </summary>
    [AddComponentMenu("UI/Effects/UI Gradient Effect")]
    [RequireComponent(typeof(Graphic))]
    [ExecuteAlways]
    public class UIGradientEffect : BaseMeshEffect
    {
        [SerializeField] protected Gradient _gradient = new Gradient();
        [SerializeField, Range(-180f, 180f)] protected float _angle = 90f;
        [SerializeField, Range(-1f, 1f)] protected float _offset = 0f;
        [SerializeField,Dropdown(nameof(ColorBlend))] protected string _blendMode = "Multiply";
        [SerializeField, Range(0f, 1f)] protected float _blendWeight = 1f;
        [SerializeField, Range(2, 32)] protected int _segments = 16;

        public new Graphic graphic => base.graphic;

        public Gradient Gradient
        {
            get => _gradient;
            set { _gradient = value ?? new Gradient(); SetDirty(); }
        }

        /// <summary>
        /// 漸層角度：0°=左到右, 90°=下到上, -90°=上到下
        /// </summary>
        public float Angle
        {
            get => _angle;
            set { _angle = Mathf.Clamp(value, -180f, 180f); SetDirty(); }
        }

        public float Offset
        {
            get => _offset;
            set { _offset = Mathf.Clamp(value, -1f, 1f); SetDirty(); }
        }

        /// <summary>
        /// 混合模式
        /// </summary>
        public string BlendMode
        {
            get => _blendMode;
            set { _blendMode = value; SetDirty(); }
        }

        /// <summary>
        /// 混合強度 (0~1)
        /// </summary>
        public float BlendWeight
        {
            get => _blendWeight;
            set { _blendWeight = Mathf.Clamp01(value); SetDirty(); }
        }

        /// <summary>
        /// 切分段數（僅對 Image/RawImage 有效）
        /// </summary>
        public int Segments
        {
            get => _segments;
            set { _segments = Mathf.Clamp(value, 2, 32); SetDirty(); }
        }

        protected virtual void SetDirty()
        {
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        // Animation 支援
        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            SetDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0)
                return;

            List<UIVertex> vertices = new List<UIVertex>();
            vh.GetUIVertexStream(vertices);

            if (vertices.Count == 0)
                return;

            // 6 頂點 = 簡單 quad (Image/RawImage)
            bool isSimpleQuad = vertices.Count == 6;

            if (isSimpleQuad)
            {
                ProcessSimpleQuad(vh, vertices);
            }
            else
            {
                ProcessComplexMesh(vh, vertices);
            }
        }

        /// <summary>
        /// 處理簡單 quad（Image/RawImage）- 切分成多段
        /// </summary>
        private void ProcessSimpleQuad(VertexHelper vh, List<UIVertex> vertices)
        {
            UIVertex[] corners = ExtractCorners(vertices);

            if (corners == null)
            {
                ProcessComplexMesh(vh, vertices);
                return;
            }

            vh.Clear();

            float rad = _angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

            // 根據角度決定切分方向
            bool sliceAlongX = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);

            if (sliceAlongX)
            {
                GenerateHorizontalSlices(vh, corners);
            }
            else
            {
                GenerateVerticalSlices(vh, corners);
            }
        }

        private UIVertex[] ExtractCorners(List<UIVertex> vertices)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 pos = vertices[i].position;
                if (pos.x < min.x) min.x = pos.x;
                if (pos.y < min.y) min.y = pos.y;
                if (pos.x > max.x) max.x = pos.x;
                if (pos.y > max.y) max.y = pos.y;
            }

            UIVertex? bottomLeft = null, topLeft = null, topRight = null, bottomRight = null;
            float tolerance = 0.01f;

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 pos = vertices[i].position;

                if (Mathf.Abs(pos.x - min.x) < tolerance && Mathf.Abs(pos.y - min.y) < tolerance)
                    bottomLeft = vertices[i];
                else if (Mathf.Abs(pos.x - min.x) < tolerance && Mathf.Abs(pos.y - max.y) < tolerance)
                    topLeft = vertices[i];
                else if (Mathf.Abs(pos.x - max.x) < tolerance && Mathf.Abs(pos.y - max.y) < tolerance)
                    topRight = vertices[i];
                else if (Mathf.Abs(pos.x - max.x) < tolerance && Mathf.Abs(pos.y - min.y) < tolerance)
                    bottomRight = vertices[i];
            }

            if (!bottomLeft.HasValue || !topLeft.HasValue || !topRight.HasValue || !bottomRight.HasValue)
                return null;

            return new UIVertex[] { bottomLeft.Value, topLeft.Value, topRight.Value, bottomRight.Value };
        }

        private void GenerateHorizontalSlices(VertexHelper vh, UIVertex[] corners)
        {
            UIVertex bl = corners[0], tl = corners[1], tr = corners[2], br = corners[3];
            Vector2 min = bl.position;
            Vector2 max = tr.position;

            for (int i = 0; i < _segments; i++)
            {
                float t0 = (float)i / _segments;
                float t1 = (float)(i + 1) / _segments;

                UIVertex v0 = LerpVertex(bl, br, t0);
                UIVertex v1 = LerpVertex(tl, tr, t0);
                UIVertex v2 = LerpVertex(tl, tr, t1);
                UIVertex v3 = LerpVertex(bl, br, t1);

                v0.color = ModifyColor(v0.color, v0.position, min, max);
                v1.color = ModifyColor(v1.color, v1.position, min, max);
                v2.color = ModifyColor(v2.color, v2.position, min, max);
                v3.color = ModifyColor(v3.color, v3.position, min, max);

                int idx = vh.currentVertCount;
                vh.AddVert(v0);
                vh.AddVert(v1);
                vh.AddVert(v2);
                vh.AddVert(v3);

                vh.AddTriangle(idx, idx + 1, idx + 2);
                vh.AddTriangle(idx, idx + 2, idx + 3);
            }
        }

        private void GenerateVerticalSlices(VertexHelper vh, UIVertex[] corners)
        {
            UIVertex bl = corners[0], tl = corners[1], tr = corners[2], br = corners[3];
            Vector2 min = bl.position;
            Vector2 max = tr.position;

            for (int i = 0; i < _segments; i++)
            {
                float t0 = (float)i / _segments;
                float t1 = (float)(i + 1) / _segments;

                UIVertex v0 = LerpVertex(bl, tl, t0);
                UIVertex v1 = LerpVertex(br, tr, t0);
                UIVertex v2 = LerpVertex(br, tr, t1);
                UIVertex v3 = LerpVertex(bl, tl, t1);

                v0.color = ModifyColor(v0.color, v0.position, min, max);
                v1.color = ModifyColor(v1.color, v1.position, min, max);
                v2.color = ModifyColor(v2.color, v2.position, min, max);
                v3.color = ModifyColor(v3.color, v3.position, min, max);

                int idx = vh.currentVertCount;
                vh.AddVert(v0);
                vh.AddVert(v1);
                vh.AddVert(v2);
                vh.AddVert(v3);

                vh.AddTriangle(idx, idx + 1, idx + 2);
                vh.AddTriangle(idx, idx + 2, idx + 3);
            }
        }

        private UIVertex LerpVertex(UIVertex a, UIVertex b, float t)
        {
            UIVertex v = new UIVertex();
            v.position = Vector3.Lerp(a.position, b.position, t);
            v.normal = Vector3.Lerp(a.normal, b.normal, t).normalized;
            v.tangent = Vector4.Lerp(a.tangent, b.tangent, t);
            v.color = Color32.Lerp(a.color, b.color, t);
            v.uv0 = Vector4.Lerp(a.uv0, b.uv0, t);
            v.uv1 = Vector4.Lerp(a.uv1, b.uv1, t);
            v.uv2 = Vector4.Lerp(a.uv2, b.uv2, t);
            v.uv3 = Vector4.Lerp(a.uv3, b.uv3, t);
            return v;
        }

        /// <summary>
        /// 處理複雜 mesh（Text 等多頂點組件）- 只改顏色不切分
        /// </summary>
        protected void ProcessComplexMesh(VertexHelper vh, List<UIVertex> vertices)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 pos = vertices[i].position;
                if (pos.x < min.x) min.x = pos.x;
                if (pos.y < min.y) min.y = pos.y;
                if (pos.x > max.x) max.x = pos.x;
                if (pos.y > max.y) max.y = pos.y;
            }

            if (Mathf.Approximately(min.x, max.x)) max.x = min.x + 1;
            if (Mathf.Approximately(min.y, max.y)) max.y = min.y + 1;

            for (int i = 0; i < vertices.Count; i++)
            {
                UIVertex vertex = vertices[i];
                vertex.color = ModifyColor(vertex.color, vertex.position, min, max);
                vertices[i] = vertex;
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(vertices);
        }

        #region Gradient Calculation

        /// <summary>
        /// 修改頂點顏色
        /// </summary>
        protected Color ModifyColor(Color originalColor, Vector2 position, Vector2 min, Vector2 max)
        {
            Color gradientColor = EvaluateGradient(position, min, max);
            return ColorBlend.Blend(_blendMode, originalColor, gradientColor, _blendWeight);
        }

        /// <summary>
        /// 計算漸層顏色
        /// </summary>
        protected Color EvaluateGradient(Vector2 position, Vector2 min, Vector2 max)
        {
            if (_gradient == null)
                return Color.white;

            float t = CalculateGradientTime(position, min, max);
            return _gradient.Evaluate(t);
        }

        /// <summary>
        /// 計算漸層時間值 (0~1)
        /// </summary>
        protected float CalculateGradientTime(Vector2 position, Vector2 min, Vector2 max)
        {
            Vector2 size = max - min;
            if (size.x <= 0) size.x = 1;
            if (size.y <= 0) size.y = 1;

            float rad = _angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            Vector2 normalizedPos = new Vector2(
                (position.x - min.x) / size.x,
                (position.y - min.y) / size.y
            );

            Vector2 centeredPos = normalizedPos - new Vector2(0.5f, 0.5f);
            float projection = Vector2.Dot(centeredPos, dir);
            float maxProjection = Mathf.Abs(dir.x) * 0.5f + Mathf.Abs(dir.y) * 0.5f;

            float t = (projection / maxProjection + 1f) * 0.5f;
            t = Mathf.Repeat(t + _offset,1);

            return t;
        }

        #endregion

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirty();
        }
#endif
    }
}
