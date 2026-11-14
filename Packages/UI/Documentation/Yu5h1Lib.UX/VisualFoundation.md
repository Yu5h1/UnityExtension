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

### Config 與 Setting 執行中切換明暗（2026-11-14）

見[實作計畫](../../../../Documentation/UiSettingsLiveThemePlan.md)。範例用標題列的「亮色／暗色」選擇匯入的 Theme。範例背景及欄位透過既有 GraphicColorBinding 套用，兩個面板呼叫 `ApplyPalette` 更新已存在的原生控件，並沿用相同色票建立後續分類。進入案例時仍使用原先的起始色票。

- 執行中切換（VIS-SWITCH）的亮暗場景在 Unity 6000.6.0f1 通過。Config 直欄與 Setting 橫列都以既有面板及控件從深色切到亮色再切回。PlayMode 分別驗證指標和鍵盤事件、相同焦點物件、原分頁和欄位、Config 群組的個別分頁記憶及未提交的手動草稿。切換期間沒有額外保存或套用設定。
- EditMode 29/29、PlayMode 11/11 通過，零編譯錯誤。XML 位於 `WIP/Validation/UiConfigSettings/Project-Build/logs/`，分別為 `20261114-113940-tests.xml` 及 `20261114-114125-tests.xml`。
- 截圖位於 `WIP/Validation/UiConfigSettings/VisualAcceptance/live-theme-settings-verified/`。包含原有八種 440×720 版面、兩張關閉畫面及四張在同一開啟面板切換後的畫面。外層 UxAcceptance 工具列使用宿主色票。
