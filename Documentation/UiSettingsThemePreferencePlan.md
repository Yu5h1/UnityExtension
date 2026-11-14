# UxAcceptance 的 Config 與 Setting 明暗偏好

> Task ID: ugui-settings-theme-preference
>
> 延續[執行中切換明暗](UiSettingsLiveThemePlan.md)。

## 系統階層與關係

```text
UxAcceptance Settings 案例
├─ Config 與 Setting 標題列 → 共用一個明暗偏好
│  ├─ PlayerPrefs 生成與讀取 UX Theme v1 JSON
│  └─ 匯入的明暗 Theme → 現有 GraphicColorBinding 與面板控件
└─ ConfigStore → 設定欄位的草稿、保存與套用
```

## 格式與行為

- 依照 UX Configuration 的 Theme 持久化（CFG-13）沿用 [ThemePreference v1 schema](../../../UX/Documentation/ModuleSpec/Foundation/Configuration/theme-preference.schema.json)。來源 commit 為 `71aa3c1e5c622df40952aacf819dc706ea3e5c25`，schema SHA-256 為 `7676597e169429dfdd4bf267fea0d8baaaeecebe7a628f0a2c9c5696a533babf3`。
- 在既有設定鍵加上 `.Theme` 保存獨立的 PlayerPrefs 字串。亮色與暗色分別寫入 `{"version":1,"mode":"light"}` 與 `{"version":1,"mode":"dark"}`。兩個面板用同一筆偏好。
- 建立案例畫面前讀取偏好。無記錄時沿用指定的起始色票，預設為暗色。有效的 `system` 沿用起始色票，有效的 `custom` 使用它指定的明暗基底。明暗切換保留有效的自訂色資料。未知版本或無效記錄沿用起始色票並保留原始資料，使用者仍可在本次執行中切換。
- 按下現有切換按鈕後即時套色並保存明暗選擇。場景中的 Theme、SO Parameter 與 Binding 繼續提供色票。一般設定欄位沿用 ConfigStore 原有保存與套用時機。

## 實作與驗收

1. 並行實作偏好讀寫、案例接線及 PlayMode 測試。讀寫部分負責 v1 格式和獨立的儲存鍵，案例接線負責首次呈現與既有切換入口，測試負責透過兩個面板操作。
2. 以 Unity batchmode 重建 UxAcceptance 主機和案例。先從其中一個面板改成亮色，重開後確認兩個面板第一個畫面為亮色，再從另一個面板改為暗色並重開確認。
3. 在手動與自動保存模式留下設定草稿，切換明暗時檢查寫入、套用次數與草稿值。驗證首次載入缺省值及讀取未知版本時的資料保存，並重跑既有主題切換與 ConfigStore 整合測試。
4. 修正驗收發現的問題並重跑受影響的測試，通過後提交實作與驗收紀錄。此輪驗收聚焦於明暗偏好及保存行為。
