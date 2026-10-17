using System;
using System.Collections.Generic;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Pure state machine and logic controller for OptionPicker (OPT-VALUE, OPT-OPTIONS, OPT-OPEN, OPT-CYCLE).
    /// Decoupled from rendering so all neutral test cases run synchronously without UI tree dependencies.
    /// </summary>
    public class OptionPickerModel
    {
        public const float DefaultHoldDuration = 0.4f; // 400ms from Input design 3.0.0

        private readonly List<OptionItem> options = new List<OptionItem>();
        private readonly HashSet<string> optionIds = new HashSet<string>();

        public OptionPickerInteraction interaction = OptionPickerInteraction.Picker;
        public OptionPickerOrientation orientation = OptionPickerOrientation.Horizontal;
        public OptionPickerPlacement placement = OptionPickerPlacement.Aligned;

        public string currentValue;
        public bool isOpen;
        public string focusedId; // "trigger" or option ID
        public bool hasValidOptions;

        public event Action<string> OnChosen;
        public event Action OnOpened;
        public event Action OnClosed;

        public IReadOnlyList<OptionItem> Options => options;

        public bool SetOptions(IEnumerable<OptionItem> newOptions)
        {
            options.Clear();
            optionIds.Clear();
            hasValidOptions = false;

            if (newOptions == null) return false;

            foreach (var opt in newOptions)
            {
                if (opt == null || string.IsNullOrWhiteSpace(opt.id))
                {
                    // OPT-OPTIONS: ID 重複或空白時不開啟面板、不循環
                    options.Clear();
                    optionIds.Clear();
                    return false;
                }
                if (!optionIds.Add(opt.id))
                {
                    // 重複 ID
                    options.Clear();
                    optionIds.Clear();
                    return false;
                }
                options.Add(opt);
            }

            hasValidOptions = options.Count > 0 && HasAnyAvailableOption();
            return hasValidOptions;
        }

        public bool HasAnyAvailableOption()
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (!options[i].disabled) return true;
            }
            return false;
        }

        public List<OptionItem> GetAvailableOptions()
        {
            var list = new List<OptionItem>();
            for (int i = 0; i < options.Count; i++)
            {
                if (!options[i].disabled) list.Add(options[i]);
            }
            return list;
        }

        public OptionItem GetResolvedCurrentOption()
        {
            var available = GetAvailableOptions();
            if (available.Count == 0) return null;

            if (!string.IsNullOrEmpty(currentValue))
            {
                for (int i = 0; i < available.Count; i++)
                {
                    if (available[i].id == currentValue) return available[i];
                }
            }

            // OPT-OPTIONS: 目前的值不可用或沒有時，焦點與對齊改用第一個可用選項
            return available[0];
        }

        public bool TryCycleNext(out string chosenId)
        {
            chosenId = null;
            if (!hasValidOptions) return false;

            var available = GetAvailableOptions();
            if (available.Count == 0) return false;

            int currentIndex = -1;
            if (!string.IsNullOrEmpty(currentValue))
            {
                for (int i = 0; i < available.Count; i++)
                {
                    if (available[i].id == currentValue)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            int nextIndex;
            if (currentIndex < 0)
            {
                // 目前的值不可用或沒有時取第一個可用選項
                nextIndex = 0;
            }
            else
            {
                // OPT-CYCLE: 依清單順序在目前值之後的下一個可用選項，最後一個之後回到第一個
                nextIndex = (currentIndex + 1) % available.Count;
            }

            chosenId = available[nextIndex].id;
            focusedId = "trigger";
            OnChosen?.Invoke(chosenId);
            return true;
        }

        public bool TryOpen()
        {
            if (isOpen) return false;
            if (!hasValidOptions) return false;

            var resolved = GetResolvedCurrentOption();
            if (resolved == null) return false;

            isOpen = true;
            focusedId = resolved.id;
            OnOpened?.Invoke();
            return true;
        }

        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;
            focusedId = "trigger";
            OnClosed?.Invoke();
        }

        public void Choose(string id)
        {
            var opt = options.Find(o => o.id == id);
            if (opt == null || opt.disabled) return;

            // OPT-VALUE: 控件回報一次所選選項的 ID，不自行改變目前的值
            OnChosen?.Invoke(id);
            Close();
        }

        public void HandleTriggerTap()
        {
            if (interaction == OptionPickerInteraction.Picker)
            {
                TryOpen();
            }
            else // Cycle or Both
            {
                TryCycleNext(out _);
            }
        }

        public void HandleTriggerHold()
        {
            // OPT-OPEN: picker 與 both 長按依 INPUT-CHOICE-HOLD 開啟；cycle 不開啟
            if (interaction != OptionPickerInteraction.Cycle)
            {
                TryOpen();
            }
        }

        public void HandleKey(string key)
        {
            if (isOpen)
            {
                var available = GetAvailableOptions();
                if (available.Count == 0)
                {
                    Close();
                    return;
                }

                int focusedIndex = available.FindIndex(o => o.id == focusedId);
                if (focusedIndex < 0) focusedIndex = 0;

                bool isPrev = (orientation == OptionPickerOrientation.Horizontal && key == "ArrowLeft") ||
                              (orientation == OptionPickerOrientation.Vertical && key == "ArrowUp");
                bool isNext = (orientation == OptionPickerOrientation.Horizontal && key == "ArrowRight") ||
                              (orientation == OptionPickerOrientation.Vertical && key == "ArrowDown");

                if (isPrev)
                {
                    int prevIndex = (focusedIndex - 1 + available.Count) % available.Count;
                    focusedId = available[prevIndex].id;
                }
                else if (isNext)
                {
                    int nextIndex = (focusedIndex + 1) % available.Count;
                    focusedId = available[nextIndex].id;
                }
                else if (key == "Home")
                {
                    focusedId = available[0].id;
                }
                else if (key == "End")
                {
                    focusedId = available[available.Count - 1].id;
                }
                else if (key == "Enter" || key == "Space" || key == " ")
                {
                    Choose(focusedId);
                }
                else if (key == "Escape" || key == "Tab")
                {
                    Close();
                }
            }
            else
            {
                // 面板關閉時
                if (interaction == OptionPickerInteraction.Cycle)
                {
                    if (key == "Enter" || key == "Space" || key == " ")
                    {
                        TryCycleNext(out _);
                    }
                    // option-cycle-keyboard-never-opens: 方向鍵不開啟
                }
                else if (interaction == OptionPickerInteraction.Both)
                {
                    if (key == "Enter" || key == "Space" || key == " ")
                    {
                        TryCycleNext(out _);
                    }
                    else
                    {
                        bool isAxisKey = (orientation == OptionPickerOrientation.Horizontal && (key == "ArrowLeft" || key == "ArrowRight")) ||
                                         (orientation == OptionPickerOrientation.Vertical && (key == "ArrowUp" || key == "ArrowDown"));
                        if (isAxisKey || key == "Home" || key == "End")
                        {
                            TryOpen();
                        }
                    }
                }
                else // Picker
                {
                    bool isAxisKey = (orientation == OptionPickerOrientation.Horizontal && (key == "ArrowLeft" || key == "ArrowRight")) ||
                                     (orientation == OptionPickerOrientation.Vertical && (key == "ArrowUp" || key == "ArrowDown"));
                    if (key == "Enter" || key == "Space" || key == " " || isAxisKey || key == "Home" || key == "End")
                    {
                        TryOpen();
                    }
                }
            }
        }
    }
}
