using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>Handles keyboard submission for the multiline uGUI input created by InputFieldAddon.</summary>
    internal sealed class MultilineSubmitInputField : InputField
    {
        private readonly Event keyboardEvent = new();
        private bool previousComposition;

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

            var input = EventSystem.current?.currentInputModule?.input;
            string composition = input is UIInputHandler ? Input.compositionString : input?.compositionString ?? Input.compositionString;
            bool composing = !string.IsNullOrEmpty(composition);
            bool suppressSubmit = composing || previousComposition;
            previousComposition = composing;

            while (Event.PopEvent(keyboardEvent))
            {
                if (keyboardEvent.rawType == EventType.KeyDown)
                    ProcessKeyDown(keyboardEvent, suppressSubmit);
                else if (isFocused && (keyboardEvent.type is EventType.ValidateCommand or EventType.ExecuteCommand) &&
                    keyboardEvent.commandName == "SelectAll")
                {
                    SelectAll();
                    UpdateLabel();
                }
            }

            eventData.Use();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            previousComposition = false;
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
                        Append('\n');
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
