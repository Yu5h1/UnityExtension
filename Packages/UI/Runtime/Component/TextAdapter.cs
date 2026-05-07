using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.UI
{
    [DisallowMultipleComponent]
    public class TextAdapter : UI_Adapter<ITextOps>, ITextOps, ITextAttribute
    {
        public string CharactersToTrim;
        public string text { get => adapter.text.Trim(Regex.Unescape(CharactersToTrim).ToCharArray()); set => adapter.text = value; }
        public Color color { get => adapter.color; set => adapter.color = value; }
        public float alpha { get => adapter.alpha; set => adapter.alpha = value; }
        public float fontSize { get => adapter.fontSize; set => adapter.fontSize = value; }
        public float lineSpacing { get => adapter.lineSpacing; set => adapter.lineSpacing = value; }
        public Alignment alignment { get => adapter.alignment; set => adapter.alignment = value; }

        public CanvasRenderer canvasRenderer => adapter.canvasRenderer;

        public void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale) => adapter.CrossFadeAlpha(alpha, duration, ignoreTimeScale);

        #region Ops
        public float preferredWidth => adapter.preferredWidth;
        public float preferredHeight => adapter.preferredHeight;
        public float GetTextWidth(bool forceUpdate) => adapter.GetTextWidth(forceUpdate);
        public float GetLineY(int index,bool local) => adapter.GetLineY(index,local);
        public float GetBaseLineHeight() => adapter.GetBaseLineHeight();
        public int GetLineCount() => adapter.GetLineCount();
        public int GetLineIndexByPosition(int pos) => adapter.GetLineIndexByPosition(pos);
        public void ForceUpdate() => adapter.ForceUpdate();
        public void SetLayoutDirty() => adapter.SetLayoutDirty();
        public void SetWrappingOverflowMode(bool wrap) => adapter.SetWrappingOverflowMode(wrap);
        public Vector3 GetCharacterPosition(int pos, bool local) => adapter.GetCharacterPosition(pos, local);
        #endregion

        public void Append(string content) => text += $"\n{content}";
        public void Show(string content)
        {
            text = content;
            gameObject.SetActive(true);
            
        }
#if UNITY_EDITOR
        [ContextMenu(nameof(Test))]
        public void Test() => $"TextAdapter Test: {text}".print();
#endif
    }
}
