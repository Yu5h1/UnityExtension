using UnityEngine;
using TMPro;

namespace Yu5h1Lib.UI.Effects
{
    /// <summary>
    /// TextMeshPro 漸層效果
    /// 繼承 UIGradientEffect，共用所有欄位和漸層計算邏輯
    /// ModifyMesh 不執行（TMP 不走這條路），改用 TMP 事件系統
    /// </summary>
    [AddComponentMenu("UI/Effects/TMP Gradient Effect")]
    [RequireComponent(typeof(TMP_Text))]
    [ExecuteAlways]
    public class TMPGradientEffect : UIGradientEffect
    {
        [SerializeField]
        [Tooltip("套用範圍：整體文字或每個字元")]
        private GradientScope _scope = GradientScope.PerCharacter;

        public enum GradientScope
        {
            /// <summary>以整個文字框為範圍</summary>
            WholeText,
            /// <summary>每個字元獨立套用漸層</summary>
            PerCharacter,
            /// <summary>每行獨立套用漸層</summary>
            PerLine
        }

        private TMP_Text _textComponent;
        private bool _isDirty;

        public GradientScope Scope
        {
            get => _scope;
            set { _scope = value; SetDirty(); }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _textComponent = GetComponent<TMP_Text>();

            if (_textComponent != null)
            {
                TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
                SetDirty();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);

            if (_textComponent != null)
                _textComponent.ForceMeshUpdate();
        }

        private void LateUpdate()
        {
            if (_isDirty && _textComponent != null)
            {
                ApplyGradient();
                _isDirty = false;
            }
        }

        private void OnTextChanged(Object obj)
        {
            if (obj == _textComponent)
                SetDirty();
        }

        protected override void SetDirty()
        {
            _isDirty = true;
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetDirty();
        }

        // TMP 不走 ModifyMesh，直接 return
        public override void ModifyMesh(UnityEngine.UI.VertexHelper vh)
        {
            // 不執行任何操作
        }

        private void ApplyGradient()
        {
            if (_textComponent == null || !isActiveAndEnabled)
                return;

            var textInfo = _textComponent.textInfo;
            if (textInfo == null || textInfo.characterCount == 0)
                return;

            _textComponent.ForceMeshUpdate();

            // 計算整體邊界
            Vector2 textMin, textMax;
            CalculateTextBounds(textInfo, out textMin, out textMax);

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible)
                    continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                var meshInfo = textInfo.meshInfo[materialIndex];
                var colors = meshInfo.colors32;
                var vertices = meshInfo.vertices;

                // 根據 Scope 取得邊界
                Vector2 min, max;
                GetGradientBounds(textInfo, charInfo, vertices, vertexIndex, textMin, textMax, out min, out max);

                // TMP 頂點順序: 0=左下, 1=左上, 2=右上, 3=右下
                for (int j = 0; j < 4; j++)
                {
                    Vector2 pos = vertices[vertexIndex + j];
                    Color32 originalColor = colors[vertexIndex + j];
                    Color gradientColor = ModifyColor(originalColor, pos, min, max);
                    colors[vertexIndex + j] = gradientColor;
                }
            }

            // 更新 mesh
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                var meshInfo = textInfo.meshInfo[i];
                meshInfo.mesh.colors32 = meshInfo.colors32;
                _textComponent.UpdateGeometry(meshInfo.mesh, i);
            }
        }

        private void GetGradientBounds(
            TMP_TextInfo textInfo,
            TMP_CharacterInfo charInfo,
            Vector3[] vertices,
            int vertexIndex,
            Vector2 textMin,
            Vector2 textMax,
            out Vector2 min,
            out Vector2 max)
        {
            switch (_scope)
            {
                case GradientScope.WholeText:
                    min = textMin;
                    max = textMax;
                    break;

                case GradientScope.PerLine:
                    CalculateLineBounds(textInfo, charInfo.lineNumber, out min, out max);
                    break;

                case GradientScope.PerCharacter:
                default:
                    min = new Vector2(vertices[vertexIndex].x, vertices[vertexIndex].y);
                    max = new Vector2(vertices[vertexIndex + 2].x, vertices[vertexIndex + 2].y);
                    break;
            }
        }

        private void CalculateTextBounds(TMP_TextInfo textInfo, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                if (charInfo.bottomLeft.x < min.x) min.x = charInfo.bottomLeft.x;
                if (charInfo.bottomLeft.y < min.y) min.y = charInfo.bottomLeft.y;
                if (charInfo.topRight.x > max.x) max.x = charInfo.topRight.x;
                if (charInfo.topRight.y > max.y) max.y = charInfo.topRight.y;
            }

            if (Mathf.Approximately(min.x, max.x)) max.x = min.x + 1;
            if (Mathf.Approximately(min.y, max.y)) max.y = min.y + 1;
        }

        private void CalculateLineBounds(TMP_TextInfo textInfo, int lineNumber, out Vector2 min, out Vector2 max)
        {
            if (lineNumber >= textInfo.lineCount)
            {
                min = Vector2.zero;
                max = Vector2.one;
                return;
            }

            var lineInfo = textInfo.lineInfo[lineNumber];
            min = new Vector2(lineInfo.lineExtents.min.x, lineInfo.descender);
            max = new Vector2(lineInfo.lineExtents.max.x, lineInfo.ascender);

            if (Mathf.Approximately(min.x, max.x)) max.x = min.x + 1;
            if (Mathf.Approximately(min.y, max.y)) max.y = min.y + 1;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirty();
        }
#endif
    }
}
