using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class LayoutGroupAddon : MonoBehaviour
    {
        [Range(0f, 0.5f)] public float left;
        [Range(0f, 0.5f)] public float right;
        [Range(0f, 0.5f)] public float top;
        [Range(0f, 0.5f)] public float bottom;

        // Ratio of width (horizontal), height (vertical) or both (grid); 0 keeps the LayoutGroup's own spacing.
        [Range(0f, 1f)] public float spacing;

        private RectTransform rect;
        private LayoutGroup layout;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            layout = GetComponent<LayoutGroup>();

            if (layout == null)
            {
                Debug.LogError(
                    $"[{nameof(LayoutGroupAddon)}] No LayoutGroup found on {name}"
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

            ApplySpacing(r);

            LayoutRebuilder.MarkLayoutForRebuild(rect);
        }

        void ApplySpacing(Rect r)
        {
            if (spacing <= 0f)
                return;

            switch (layout)
            {
                case HorizontalLayoutGroup horizontal:
                    horizontal.spacing = r.width * spacing;
                    break;
                case VerticalLayoutGroup vertical:
                    vertical.spacing = r.height * spacing;
                    break;
                case GridLayoutGroup grid:
                    grid.spacing = new Vector2(r.width * spacing, r.height * spacing);
                    break;
            }
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
