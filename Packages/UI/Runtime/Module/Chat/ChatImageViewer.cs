
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CloseReason = Yu5h1Lib.UI.ChatPanel.ViewerCloseReason;
using DismissSource = Yu5h1Lib.UI.ChatPanel.ViewerDismissSource;

namespace Yu5h1Lib.UI
{
    /// <summary>Displays a chat image above the host canvas and restores focus to its thumbnail.</summary>
    internal sealed class ChatImageViewer : MonoBehaviour
    {
        private readonly Dictionary<int, Vector2> pointers = new();
        private readonly HashSet<int> suppressClick = new();
        private RectTransform pictureRect;
        private Image picture;
        private Image backdrop;
        private Image closeBackground;
        private UguiControlDecoration pictureFocus;
        private Selectable focusTarget;
        private Button closeButton;
        private Button opener;
        private readonly Vector3[] worldCorners = new Vector3[4];
        private RectTransform safeArea;
        private Func<DismissSource, bool> canDismiss;
        private Action<CloseReason> closed;
        private float previewInset;
        private float threshold;
        private float startDistance;
        private bool pinching;
        private int openId;
        private int lastEscapeFrame = -1;

        internal bool Full { get; private set; }

        internal static ChatImageViewer Create(Transform canvas, Font font)
        {
            var root = ChatPanel.AddRect("Chat Image Viewer", canvas);
            root.gameObject.SetActive(false);
            ChatPanel.Stretch(root);
            var viewer = root.gameObject.AddComponent<ChatImageViewer>();
            var sorting = root.gameObject.AddComponent<Canvas>();
            sorting.overrideSorting = true;
            sorting.sortingOrder = short.MaxValue;
            root.gameObject.AddComponent<GraphicRaycaster>();

            viewer.backdrop = root.gameObject.AddComponent<Image>();
            viewer.AddEvents(root, false);
            viewer.pictureRect = ChatPanel.AddRect("Picture", root);
            viewer.picture = viewer.pictureRect.gameObject.AddComponent<Image>();
            viewer.picture.preserveAspect = true;
            viewer.focusTarget = viewer.pictureRect.gameObject.AddComponent<Selectable>();
            viewer.focusTarget.navigation = new Navigation { mode = Navigation.Mode.None };
            viewer.focusTarget.transition = Selectable.Transition.None;
            viewer.AddEvents(viewer.pictureRect, true);

            var closeRect = ChatPanel.AddRect("Close image", root);
            closeRect.anchorMin = closeRect.anchorMax = Vector2.one;
            closeRect.pivot = Vector2.one;
            closeRect.anchoredPosition = new Vector2(-12f, -12f);
            closeRect.sizeDelta = new Vector2(44f, 44f);
            viewer.closeBackground = closeRect.gameObject.AddComponent<Image>();
            viewer.closeButton = closeRect.gameObject.AddComponent<Button>();
            viewer.closeButton.targetGraphic = viewer.closeBackground;
            viewer.closeButton.navigation = new Navigation { mode = Navigation.Mode.None };
            viewer.closeButton.onClick.AddListener(() => viewer.TryDismiss(DismissSource.CloseButton));
            var label = ChatPanel.AddRect("Label", closeRect).gameObject.AddComponent<Text>();
            ChatPanel.Stretch(label.rectTransform);
            label.text = "×";
            label.font = font;
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;

            return viewer;
        }

        internal void Open(Sprite sprite, Button thumbnail, string description, ChatPanel.Options options,
            Func<DismissSource, bool> allowDismiss, Action<CloseReason> onClosed)
        {
            openId++;
            opener = thumbnail;
            safeArea = options.ViewerSafeArea;
            canDismiss = allowDismiss;
            closed = onClosed;
            previewInset = Mathf.Max(0f, options.ViewerPreviewInset);
            threshold = Mathf.Max(0f, options.ViewerZoomGestureThreshold);
            backdrop.color = new Color(0f, 0f, 0f, Mathf.Clamp01(options.ViewerBackdropOpacity));
            closeBackground.sprite = options.ViewerCloseSprite;
            closeBackground.color = options.ViewerPalette != null
                ? new Color(0f, 0f, 0f, .6f) : new Color(.14f, .16f, .20f, .9f);
            SetPictureFocus(options.ViewerPalette);
            picture.sprite = sprite;
            picture.name = description;
            Full = false;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            ResizePicture();
            focusTarget.Select();
        }

        private void SetPictureFocus(UguiControlPalette palette)
        {
            if (palette == null && pictureFocus == null)
                return;

            pictureFocus ??= UguiControlDecoration.GetOrCreate(focusTarget);
            pictureFocus.Configure(focusTarget, palette, null, default, null, default,
                border: false, focus: palette != null, onAccent: false);
            pictureFocus.rectTransform.offsetMin = -Vector2.one * 4f;
            pictureFocus.rectTransform.offsetMax = Vector2.one * 4f;
        }

        internal void Close(CloseReason reason)
        {
            if (!gameObject.activeSelf)
                return;
            var thumbnail = opener;
            var notify = closed;
            DisposeView();
            if (thumbnail != null && thumbnail.IsActive() && thumbnail.IsInteractable())
                thumbnail.Select();
            notify?.Invoke(reason);
        }

        internal void DisposeView()
        {
            if (!gameObject.activeSelf)
                return;
            opener = null;
            safeArea = null;
            canDismiss = null;
            closed = null;
            picture.sprite = null;
            pointers.Clear();
            suppressClick.Clear();
            pinching = false;
            gameObject.SetActive(false);
        }

        private void AddEvents(RectTransform target, bool isPicture)
        {
            var trigger = target.gameObject.AddComponent<EventTrigger>();
            AddEvent(trigger, EventTriggerType.PointerDown, value => PointerDown((PointerEventData)value));
            AddEvent(trigger, EventTriggerType.Drag, value => MovePointer((PointerEventData)value));
            AddEvent(trigger, EventTriggerType.PointerUp, value => PointerUp((PointerEventData)value));
            AddEvent(trigger, EventTriggerType.PointerClick, value => Click((PointerEventData)value, isPicture));
            AddEvent(trigger, EventTriggerType.Scroll, value => Zoom(((PointerEventData)value).scrollDelta.y));
            AddEvent(trigger, EventTriggerType.Submit, _ => { if (isPicture) Toggle(); });
            AddEvent(trigger, EventTriggerType.Cancel, _ => TryDismiss(DismissSource.Escape));
        }

        private static void AddEvent(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(value => callback(value));
            trigger.triggers.Add(entry);
        }

        private void PointerDown(PointerEventData value)
        {
            if (value.button != PointerEventData.InputButton.Left)
                return;
            focusTarget.Select();
            suppressClick.Remove(value.pointerId);
            if (pointers.Count >= 2 && !pointers.ContainsKey(value.pointerId))
            {
                suppressClick.Add(value.pointerId);
                return;
            }
            pointers[value.pointerId] = value.position;
            if (pointers.Count == 2)
            {
                pinching = true;
                startDistance = PointerDistance();
            }
            if (pinching)
                foreach (int id in pointers.Keys)
                    suppressClick.Add(id);
        }

        private void MovePointer(PointerEventData value)
        {
            if (pointers.ContainsKey(value.pointerId))
                pointers[value.pointerId] = value.position;
        }

        private void PointerUp(PointerEventData value)
        {
            if (!pointers.ContainsKey(value.pointerId))
                return;
            MovePointer(value);
            float scale = pinching && pointers.Count == 2 && startDistance > 0f
                ? PointerDistance() / startDistance : 1f;
            pointers.Remove(value.pointerId);
            if (pointers.Count < 2)
                pinching = false;
            if (scale > 1f + threshold)
                Zoom(1f);
            else if (scale < 1f - threshold)
                Zoom(-1f);
        }

        private float PointerDistance()
        {
            using var values = pointers.Values.GetEnumerator();
            values.MoveNext();
            Vector2 first = values.Current;
            values.MoveNext();
            return Vector2.Distance(first, values.Current);
        }

        private void Click(PointerEventData value, bool isPicture)
        {
            if (value.button != PointerEventData.InputButton.Left || suppressClick.Remove(value.pointerId))
                return;
            if (isPicture || Full)
                Toggle();
            else
                TryDismiss(DismissSource.Backdrop);
        }

        private void Zoom(float direction)
        {
            if (direction > 0f && !Full || direction < 0f && Full)
                Toggle();
            else if (direction < 0f)
                TryDismiss(DismissSource.ZoomOut);
        }

        private void Toggle()
        {
            Full = !Full;
            ResizePicture();
            focusTarget.Select();
        }

        private void TryDismiss(DismissSource source)
        {
            if (!gameObject.activeSelf)
                return;
            if (source == DismissSource.Escape)
            {
                if (lastEscapeFrame == Time.frameCount)
                    return;
                lastEscapeFrame = Time.frameCount;
            }
            int activeOpen = openId;
            if (canDismiss?.Invoke(source) != false && openId == activeOpen)
                Close(CloseReason.Cancel);
        }

        private void Update()
        {
            if (InputHandler.GetKeyDown(KeyCode.Escape))
                TryDismiss(DismissSource.Escape);
            else if (InputHandler.GetKeyDown(KeyCode.Tab))
            {
                if (EventSystem.current?.currentSelectedGameObject == closeButton.gameObject)
                    focusTarget.Select();
                else
                    closeButton.Select();
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                Close(CloseReason.Programmatic);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                Close(CloseReason.Programmatic);
        }

        private void OnRectTransformDimensionsChange() => ResizePicture();

        private void LateUpdate()
        {
            if (safeArea != null)
                ResizePicture();
        }

        private Rect AvailableRect(RectTransform root)
        {
            if (safeArea == null)
                return root.rect;
            safeArea.GetWorldCorners(worldCorners);
            Vector2 min = root.InverseTransformPoint(worldCorners[0]);
            Vector2 max = root.InverseTransformPoint(worldCorners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void ResizePicture()
        {
            if (picture == null || picture.sprite == null)
                return;
            float inset = Full ? 0f : previewInset;
            var root = (RectTransform)transform;
            Rect area = AvailableRect(root);
            Vector2 available = area.size - Vector2.one * (2f * inset);
            Vector2 source = picture.sprite.rect.size;
            float scale = Mathf.Max(0f, Mathf.Min(available.x / source.x, available.y / source.y));
            pictureRect.anchoredPosition = area.center - root.rect.center;
            pictureRect.sizeDelta = source * scale;
            ((RectTransform)closeButton.transform).anchoredPosition =
                area.max - root.rect.max - Vector2.one * 12f;
        }
    }
}
