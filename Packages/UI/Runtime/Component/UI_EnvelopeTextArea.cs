using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib.UI;

namespace Yu5h1Lib.UI
{
    [RequireComponent(typeof(RawImage))]
    public class UI_EnvelopeTextArea : UIControl
    {
        [SerializeField] private TextAdapter adapter;
        [SerializeField] private RawImage rawImage;
        private Material _mat;

        protected override void OnInitializing()
        {
            base.OnInitializing();
            if ("rawImage requied!".printWarningIf(!TryGetComponent(out rawImage)))
                return;
            _mat = Instantiate(rawImage.material); // ���v�T��l shader
            rawImage.material = _mat;
            UpdateShader();
        }
        void Start()
        {
      
        }
        [ContextMenu(nameof(UpdateShader))]
        private void UpdateShader()
        {
            if (adapter == null || rawImage == null) return;

            float fontSize = adapter.fontSize;
            float lineSpacingFactor = adapter.lineSpacing; // 1.2, 1.5 �o��
            float areaHeight = adapter.targetRectTransform.rect.height;

            float lineHeight = fontSize * lineSpacingFactor;
            int lineCount = Mathf.FloorToInt(areaHeight / lineHeight);

            float uvSpacing = 1.0f / lineCount;

            _mat.SetFloat("_LineSpacing", uvSpacing);
            _mat.SetFloat("_LineThickness", uvSpacing * 0.2f); // �Ҧp�e 20% ���׷�u
        }
    } 
}
