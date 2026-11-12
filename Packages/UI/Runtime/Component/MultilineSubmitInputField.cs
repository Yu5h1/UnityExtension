using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>Handles keyboard submission and optional line breaks for the uGUI input created by InputFieldAddon.</summary>
    internal sealed class MultilineSubmitInputField : InputField
    {
        private readonly Event keyboardEvent = new();
        private bool previousComposition;
        private int heldEnterKeys;

        internal event Action SubmitRequested;

        public override void OnUpdateSelected(BaseEventData eventData)
        {
            if (!isFocused)
                return;

            if (touchScreenKeyboard != null)
            {
                base.OnUpdateSelected(eventData);
                return;
            }

            bool composing = HasComposition();
            bool suppressSubmit = composing || previousComposition;
            previousComposition = composing;

            if (InputHandler.CurrentBackend != null)
            {
                if (!InputHandler.GetKey(KeyCode.Return))
                    heldEnterKeys &= ~1;
                if (!InputHandler.GetKey(KeyCode.KeypadEnter))
                    heldEnterKeys &= ~2;
            }

            while (Event.PopEvent(keyboardEvent))
                ProcessKeyboardEvent(keyboardEvent, suppressSubmit);

            eventData.Use();
        }

        internal void ProcessKeyboardEvent(Event key, bool composing)
        {
            int enterMask = key.keyCode switch
            {
                KeyCode.Return => 1,
                KeyCode.KeypadEnter => 2,
                _ => 0
            };
            if (key.rawType == EventType.KeyUp)
                heldEnterKeys &= ~enterMask;
            else if (key.rawType == EventType.KeyDown)
            {
                if ((heldEnterKeys & enterMask) != 0)
                    return;
                heldEnterKeys |= enterMask;
                ProcessKeyDown(key, composing);
            }
            else if (isFocused && (key.type is EventType.ValidateCommand or EventType.ExecuteCommand) &&
                key.commandName == "SelectAll")
            {
                SelectAll();
                UpdateLabel();
            }
        }

        internal static bool HasComposition()
        {
            var input = EventSystem.current?.currentInputModule?.input;
            string composition = input is UIInputHandler ? Input.compositionString : input?.compositionString ?? Input.compositionString;
            return !string.IsNullOrEmpty(composition);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            previousComposition = false;
            heldEnterKeys = 0;
            base.OnDeselect(eventData);
        }

        internal void ProcessKeyDown(Event key, bool composing)
        {
            if (!isFocused)
                return;

            if (composing && key.keyCode == KeyCode.Escape)
                return;

            if (key.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
            {
                const EventModifiers otherModifiers = EventModifiers.Control | EventModifiers.Alt | EventModifiers.Command;
                if (!composing && (key.modifiers & otherModifiers) == 0 && !readOnly && IsInteractable())
                {
                    if (key.shift)
                    {
                        if (multiLine)
                            Append('\n');
                    }
                    else
                    {
                        string draft = text;
                        SubmitRequested?.Invoke();
                        if (this == null || !IsActive())
                            return;
                        if (draft.Length > 0 && text.Length == 0 && isFocused &&
                            EventSystem.current?.currentSelectedGameObject == gameObject)
                        {
                            DeactivateInputField();
                            if (this != null && IsActive() && IsInteractable() &&
                                EventSystem.current?.currentSelectedGameObject == gameObject)
                                ActivateInputField();
                            return;
                        }
                    }
                }
            }
            else if (key.keyCode == KeyCode.None && (key.character is '\r' or '\n' or '\u0003'))
            {
                return;
            }
            else if (KeyPressed(key) == EditState.Finish)
            {
                if (!wasCanceled)
                    SendOnSubmit();
                DeactivateInputField();
                return;
            }

            UpdateLabel();
        }
    }
}
