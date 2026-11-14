# UxAcceptance 的跟隨系統主題

> Task ID: ugui-settings-system-theme
>
> 延續 [明暗偏好](UiSettingsThemePreferencePlan.md)，沿用同一版 UX Theme v1。

## 系統階層與關係

```text
UxAcceptance Settings 案例
├─ Config／Setting 標題列 → 共用亮色、暗色或跟隨系統
│  ├─ 明確選擇 → 現有 .Theme PlayerPrefs 鍵
│  └─ 跟隨系統 → 讀取 Windows 的應用程式明暗設定
│     └─ 匯入的 Theme → 現有 GraphicColorBinding 與面板控件
└─ ConfigStore → 一般設定的草稿、保存與套用
```

## 行為

- 兩個面板在現有亮暗切換旁加入「跟隨系統」，共用選擇並顯示使用中狀態。明確選擇後保存 `{"version":1,"mode":"system"}`，已保存的有效自訂色資料一併保留。從此模式按下明暗切換時，選取目前顯示色票的相反色並保存為明確選擇。
- 重開案例時先讀取已保存的模式。若為 `system`，在建立畫面前讀取系統應用程式明暗。Windows Editor 與 Windows Player 透過唯讀的 `RegGetValueW` 取得 `AppsUseLightTheme`。系統值無法取得的平台或狀態沿用案例起始色票。[Windows 設定參考](https://learn.microsoft.com/en-us/windows/apps/develop/settings/settings-common#personalization---colors) · [Win32 讀取 API](https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-reggetvaluew)
- 只在案例掛載且處於 `system` 時監看。以不受遊戲時間影響的短週期檢查變更。有效色票改變時透過現有 Binding 和面板套色入口更新同一批控件，保留分頁、焦點與草稿。系統變更及重開的首次載入都只讀取偏好，寫入由使用者明確選擇觸發。

## 實作與驗收

1. 並行實作模式讀寫、Windows 系統訊號與監看、案例與標題列接線。接線沿用現有 ConfigStore 邊界。
2. 在隔離專案用 batchmode 啟動 UxAcceptance，從 Config 或 Setting 選擇跟隨系統，另一次啟動檢查保存模式和兩個面板的首畫面。
3. 在 PlayMode 提供可變的系統訊號，驗證亮暗雙向即時套色、草稿與焦點留在原控件、偏好原文及一般設定寫入／套用次數維持原值。切到固定明暗、離開案例或改用時間暫停時檢查監看的生命週期。Windows 實際讀取另外以目前系統值核對。
4. 重跑既有明暗及 ConfigStore 測試。驗收通過後記錄結果並提交。
