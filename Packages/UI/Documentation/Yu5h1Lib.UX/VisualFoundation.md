# VisualFoundation 色票採用紀錄（Yu5h1Lib.UI）

[Config 與 Setting 共用色票的實作計畫](../../../../Documentation/UiSettingsVisualFoundationPlan.md)

## 系統階層與關係

```text
UX VisualFoundation 色彩資料
└─ Unity 匯入 → 明暗 Theme 各 17 個 ColorObject
   ├─ Theme.TryGet → 現有場景 Binding
   └─ UguiControlPalette → Config、Setting 與 UxAcceptance 範例
```

## 來源與套用

採用 VisualFoundation 規範 3.1.0 的色彩資料 1.3.0，來源及 SHA-256 見實作計畫。Unity 的 Resource 資產與 UX 來源、Web 釘選副本逐位元組相同。Unity 匯入時建立 Light 與 Dark 兩個 `Theme` 資產，角色沿用 UX 名稱。場景可接現有 Binding，程式建立的控件可用 `UguiControlPalette.Light`、`.Dark`，或從符合角色契約的既有 Theme 呼叫 `FromTheme`。

範例的 Config、Setting、頁面背景和分頁方向按鈕接收同一色票。外框、焦點與按鈕狀態沿用原生 uGUI 接線。套用共用色票後，ButtonAddon 的重建版面仍保留其色彩所有權。UxAcceptance 的外層工具列與預設範例使用深色，截圖透過範例建構子選定場景內的色票。

## Batchmode 驗收

Unity 6000.6.0f1 使用隔離專案，從 `7506286` 加上本輪檔案。此輪驗收範圍是預設明暗色票、既有 Binding 與 Config／Setting。

- EditMode 29/29。核對 SHA-256、明暗各 17 個角色的 RGBA、重匯入的資產識別碼及帶 schema 的既有 GraphicColorBinding；按鈕、目錄與設定回歸一併通過。XML 為 `WIP/Validation/UiConfigSettings/Project-Build/logs/20261114-105943-tests.xml`。
- PlayMode 9/9。涵蓋現有 Tabs、GroupList、保存整合與新增的截圖驗收。XML 為 `WIP/Validation/UiConfigSettings/Project-Build/logs/20261114-105850-tests.xml`。
- 逐張檢查 Config 與 Setting 在 440×720、1:1 的橫列、直欄、明暗八種組合，以及兩張關閉狀態圖。明亮與深色分頁及群組的文字對比分別為 12.78:1、10.24:1。「恢復預設」在此寬度換成兩行，字未裁切。
- PNG 與 `geometry.txt` 位於 `WIP/Validation/UiConfigSettings/VisualAcceptance/ux-vf-1.3.0-settings-verified/`。可用 Unity PlayMode batchmode 篩選 `SettingsPanelVisualAcceptanceTests`，以支援圖形的 Unity Editor 執行並提供 `-settingsVisualOutput <輸出目錄>` 重跑。
