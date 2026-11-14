# UxAcceptance 的 Config 與 Setting 執行中切換明暗

> Task ID: ugui-settings-live-theme
>
> 色票來源沿用 [VisualFoundation 色票匯入](UiSettingsVisualFoundationPlan.md)。

## 系統階層與關係

```text
UxAcceptance Settings 案例
├─ 明暗按鈕 → 選取已匯入的 Theme
├─ 現有 GraphicColorBinding → 範例的背景、文字與欄位圖形
└─ SettingsPanel.ApplyPalette → 原有 Config／Setting 的控件
   ├─ GroupList、Tabs → 按鈕狀態與選中提示
   └─ SaveBar → 狀態文字與保存控件
```

明暗選擇由 Settings 案例管理。每個面板的標題列提供切換入口，兩個面板與案例內容使用同一個配色。Unity 場景與資產仍沿用原有 Theme、參數鍵及 Binding 引用。

## 實作順序

1. 加入面板的即時套色入口。以現有控件的參考套用色票，讓已建立與之後建立的分頁都使用目前主題。切換期間保留面板、控件、分頁選擇、捲動位置與焦點，不經開關面板或設定群組流程。
2. 讓範例從現有匯入 Theme 選擇明暗，透過現有 GraphicColorBinding 套用範例自身的顏色，用 uGUI palette 套用原生按鈕狀態。視窗內的切換按鈕接受鍵盤操作，滑鼠點擊保留原本聚焦的控件。主題狀態與 ConfigStore 的保存流程獨立。
3. 並行分工為面板套色、範例與既有 Binding 的接線、PlayMode 驗收。驗收方整合後跑目標範圍的 Unity batchmode，出現失敗時交回各負責項再重跑。
4. 驗收完成後提交實作、測試與紀錄。

## 驗收條件

- 從 UxAcceptance 開啟 Config 或 Setting，在面板內切換暗→亮→暗。確認畫面既有元件讀到兩份匯入色票，包含面板、文字、分頁與保存控制。
- 在手動模式修改設定後切換明暗，仍保有相同的面板、分頁按鈕與欄位，草稿及焦點保持，且沒有新增偏好寫入。額外確認 Config 的群組與每組分頁記憶，涵蓋橫列與直欄。
- 跑既有色票、按鈕、分頁與保存測試。以 Unity batchmode 擷取同一開啟面板切換前後的圖。
- 本輪為執行中亮暗切換，範例維持既有起始色票及保存格式。
