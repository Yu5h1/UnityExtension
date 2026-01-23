using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class LayoutGroupAide : MonoBehaviour
    {
        [Range(0f, 0.5f)] public float left;
        [Range(0f, 0.5f)] public float right;
        [Range(0f, 0.5f)] public float top;
        [Range(0f, 0.5f)] public float bottom;

        private RectTransform rect;
        private LayoutGroup layout;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            layout = GetComponent<LayoutGroup>();

            if (layout == null)
            {
                Debug.LogError(
                    $"[LayoutGroupAide] No LayoutGroup found on {name}"
                );
                enabled = false;
                return;
            }

            ApplyPadding();
        }

        void OnEnable()
        {
            ApplyPadding();
        }

        void OnRectTransformDimensionsChange()
        {
            ApplyPadding();
        }

        void ApplyPadding()
        {
            if (layout == null) return;

            var r = rect.rect;

            layout.padding.left = Mathf.RoundToInt(r.width * left);
            layout.padding.right = Mathf.RoundToInt(r.width * right);
            layout.padding.top = Mathf.RoundToInt(r.height * top);
            layout.padding.bottom = Mathf.RoundToInt(r.height * bottom);

            LayoutRebuilder.MarkLayoutForRebuild(rect);
        }
#if UNITY_EDITOR
        void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            Cache();
            ApplyPadding();
        }
#endif
        void Cache()
        {
            if (rect == null)
                rect = GetComponent<RectTransform>();

            if (layout == null)
                layout = GetComponent<LayoutGroup>();
        }
    }
}
