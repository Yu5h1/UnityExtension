using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>
    /// Draws a non-interactive inset border and reflects native selection and interactability on foreground graphics.
    /// </summary>
    [AddComponentMenu("")]
    internal sealed class UguiControlDecoration : MaskableGraphic
    {
        private const float BorderWidth = 1f;
        private const float FocusWidth = 2f;

        private Selectable control;
        private UguiControlPalette palette;
        private Graphic foreground;
        private Color foregroundColor;
        private Graphic secondary;
        private Color secondaryColor;
        private bool drawBorder;
        private bool drawFocus;
        private bool accent;
        private bool hasState;
        private bool interactable;
        private bool focused;

        internal static bool HasPalette(Selectable control)
        {
            foreach (Transform child in control.transform)
            {
                if (child.TryGetComponent(out UguiControlDecoration decoration)
                    && decoration.control == control && decoration.palette != null)
                    return true;
            }

            return false;
        }

        internal static UguiControlDecoration GetOrCreate(Selectable control)
        {
            foreach (Transform child in control.transform)
            {
                if (child.TryGetComponent(out UguiControlDecoration existing))
                    return existing;
            }

            var decoration = new GameObject("Control appearance", typeof(RectTransform), typeof(CanvasRenderer), typeof(LayoutElement));
            decoration.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = (RectTransform)decoration.transform;
            rect.SetParent(control.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var graphic = decoration.AddComponent<UguiControlDecoration>();
            graphic.raycastTarget = false;
            return graphic;
        }

        internal void Configure(Selectable target, UguiControlPalette theme, Graphic primary, Color primaryColor,
            Graphic detail, Color detailColor, bool border, bool focus, bool onAccent)
        {
            control = target;
            palette = theme;
            foreground = primary;
            foregroundColor = primaryColor;
            secondary = detail;
            secondaryColor = detailColor;
            drawBorder = border;
            drawFocus = focus;
            accent = onAccent;
            hasState = false;
            RefreshState();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            hasState = false;
            RefreshState();
        }

        private void LateUpdate() => RefreshState();

        private void RefreshState()
        {
            if (control == null)
                return;

            bool canInteract = control.IsActive() && control.IsInteractable();
            bool hasFocus = drawFocus && canInteract && EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject == control.gameObject;
            if (hasState && canInteract == interactable && hasFocus == focused)
                return;

            hasState = true;
            interactable = canInteract;
            focused = hasFocus;

            if (foreground != null)
                foreground.color = canInteract ? foregroundColor : palette.Disabled;
            if (secondary != null)
                secondary.color = canInteract ? secondaryColor : palette.Disabled;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (palette == null)
                return;

            Rect rect = GetPixelAdjustedRect();
            if (focused)
            {
                AddStroke(mesh, rect, FocusWidth, palette.Focus);
                if (accent)
                {
                    var inset = Rect.MinMaxRect(rect.xMin + FocusWidth, rect.yMin + FocusWidth,
                        rect.xMax - FocusWidth, rect.yMax - FocusWidth);
                    AddStroke(mesh, inset, BorderWidth, palette.OnAccent);
                }
            }
            else if (drawBorder)
            {
                AddStroke(mesh, rect, BorderWidth, palette.ControlBorder);
            }
        }

        private static void AddStroke(VertexHelper mesh, Rect rect, float width, Color32 color)
        {
            float edge = Mathf.Min(width, Mathf.Min(rect.width, rect.height) / 2f);
            if (edge <= 0f)
                return;

            AddQuad(mesh, rect.xMin, rect.yMin, rect.xMax, rect.yMin + edge, color);
            AddQuad(mesh, rect.xMin, rect.yMax - edge, rect.xMax, rect.yMax, color);
            AddQuad(mesh, rect.xMin, rect.yMin + edge, rect.xMin + edge, rect.yMax - edge, color);
            AddQuad(mesh, rect.xMax - edge, rect.yMin + edge, rect.xMax, rect.yMax - edge, color);
        }

        private static void AddQuad(VertexHelper mesh, float left, float bottom, float right, float top, Color32 color)
        {
            int index = mesh.currentVertCount;
            mesh.AddVert(new Vector3(left, bottom), color, Vector2.zero);
            mesh.AddVert(new Vector3(left, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, bottom), color, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index + 2, index + 3, index);
        }
    }
}
