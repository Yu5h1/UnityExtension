using UnityEngine;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Pure numerical placement solver for OptionPicker panel (OPT-PLACE).
    /// Computes aligned vs adjacent panel position, flip-on-overflow, and viewport clamping.
    /// </summary>
    public static class OptionPickerPlacementSolver
    {
        public const float DefaultPickerGap = 4f;
        public const float DefaultPickerViewportMargin = 4f;

        public static Vector2 ComputePanelPosition(
            Rect triggerRect,
            Vector2 panelSize,
            Rect selectedItemLocalRect,
            OptionPickerPlacement placement,
            OptionPickerOrientation orientation,
            Rect viewportRect,
            float pickerGap = DefaultPickerGap,
            float pickerViewportMargin = DefaultPickerViewportMargin)
        {
            Vector2 pos = Vector2.zero;

            if (placement == OptionPickerPlacement.Aligned)
            {
                // OPT-PLACE: 目前的值（或第一個可用選項）的中心與觸發鈕中心重合
                pos.x = triggerRect.center.x - selectedItemLocalRect.center.x;
                pos.y = triggerRect.center.y - selectedItemLocalRect.center.y;
            }
            else // Adjacent
            {
                if (orientation == OptionPickerOrientation.Horizontal)
                {
                    // 水平面板放在觸發鈕右側、間隔 pickerGap、垂直置中
                    pos.x = triggerRect.xMax + pickerGap;
                    pos.y = triggerRect.center.y - (panelSize.y / 2f);

                    // 超出可視範圍（扣 pickerViewportMargin）時改放左側
                    if (pos.x + panelSize.x > viewportRect.xMax - pickerViewportMargin)
                    {
                        pos.x = triggerRect.xMin - pickerGap - panelSize.x;
                    }
                }
                else // Vertical
                {
                    // 垂直面板放在下方、水平置中
                    pos.x = triggerRect.center.x - (panelSize.x / 2f);
                    pos.y = triggerRect.yMax + pickerGap;

                    // 超出時改放上方
                    if (pos.y + panelSize.y > viewportRect.yMax - pickerViewportMargin)
                    {
                        pos.y = triggerRect.yMin - pickerGap - panelSize.y;
                    }
                }
            }

            // 最後把面板限制在距可視範圍四邊 pickerViewportMargin 內
            float minX = viewportRect.xMin + pickerViewportMargin;
            float maxX = viewportRect.xMax - pickerViewportMargin - panelSize.x;
            if (minX <= maxX)
            {
                pos.x = Mathf.Clamp(pos.x, minX, maxX);
            }
            else
            {
                pos.x = minX;
            }

            float minY = viewportRect.yMin + pickerViewportMargin;
            float maxY = viewportRect.yMax - pickerViewportMargin - panelSize.y;
            if (minY <= maxY)
            {
                pos.y = Mathf.Clamp(pos.y, minY, maxY);
            }
            else
            {
                pos.y = minY;
            }

            return pos;
        }
    }
}
