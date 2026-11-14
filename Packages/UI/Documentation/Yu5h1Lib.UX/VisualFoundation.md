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

### Config 與 Setting 保存明暗偏好（2026-11-14）

[實作計畫](../../../../Documentation/UiSettingsThemePreferencePlan.md)記錄 UX Theme v1 schema 的來源與雜湊。案例開啟時從獨立的 `.Theme` PlayerPrefs 鍵還原明暗。主題按鈕使用現有 Theme 與 Binding 套色，儲存 UX Theme v1 JSON。

- Unity 6000.6.0f1 隔離專案以 `2fa3c17` 加上本輪變更執行。EditMode 86/86、PlayMode 19/19。兩個面板在重建主機後第一個畫面都套用已保存的色票。手動草稿保持未提交，進行中的自動保存仍由原始設定動作完成。未知版本及無效資料保留原文，有效的自訂八色隨明暗切換一併保留。
- 三次獨立的 PlayMode batchmode 啟動各通過 1/1。第一次從 Config 存亮色，第二次讀回亮色並從 Setting 存暗色，第三次讀回暗色。每次都確認兩個面板的色票及一般設定鍵狀態，最後清除驗收用偏好。
- XML 在 `WIP/Validation/UiConfigSettings/Project-Build/logs/`，檔名為 `theme-pref-editmode.xml`、`theme-pref-playmode.xml` 與 `theme-pref-process-1.xml` 至 `theme-pref-process-3.xml`。跨程序案例放在隔離專案的 `Source/Packages/UI/Tests/Runtime/SettingsThemePreferenceProcessAcceptance.cs`。

### Config 與 Setting 跟隨系統（2026-11-14）

兩個面板的「跟隨系統」共用 UX Theme v1 的 `system` 模式。選中時以目前色票的強調色標示。[實作計畫](../../../../Documentation/UiSettingsSystemThemePlan.md)

- Unity 6000.6.0f1 隔離專案以 `37b52db` 加上本輪變更執行。EditMode 51/51、PlayMode 21/21。PlayMode 模擬系統在明暗間切換，即使遊戲時間暫停也會更新兩個面板的色票。切換期間保持原分頁、焦點、手動草稿與偏好原文，ConfigStore 的保存及套用次數維持原值。改用固定主題或離開案例會停止監看。
- 兩次獨立的 PlayMode batchmode 啟動各通過 1/1。第一次在 Config 選擇 `system`，第二次重開驗證存檔與首畫面。兩次都以與 Windows 應用程式偏好相反的起始色票驗證原生讀取。即時變更透過可替換的系統訊號驗收，Windows 全域外觀保持原設定。
- XML 位於 `WIP/Validation/UiConfigSettings/Project-Build/logs/` 下的 `system-theme-editmode.xml`、`system-theme-playmode.xml`、`system-theme-process-1.xml` 和 `system-theme-process-2.xml`。跨程序案例位於隔離專案的 `Source/Packages/UI/Tests/Runtime/SettingsSystemThemeProcessAcceptance.cs`。440×720 窄版回歸截圖位於 `WIP/Validation/UiConfigSettings/VisualAcceptance/system-theme-regression/`。

### Config 與 Setting 自訂主題（2026-11-14）

兩個面板的「自訂」共用一組 UX Theme v1 色值。八個欄位接受 `#RRGGBB`，有效輸入立即套用並保存。明暗基底提供固定角色，焦點、停用及強調前景按色值推導。匯入的 Theme 供執行時 Theme 取用。[實作計畫](../../../../Documentation/UiSettingsCustomThemePlan.md)

- Unity 6000.6.0f1 隔離專案以 `e78bafa` 加上本輪變更執行。EditMode 51/51、PlayMode 22/22。PlayMode 逐欄編輯並核對實際控件，確認無效輸入不保存、低對比依序提醒且可套用、切換基底及固定明暗／跟隨系統後返回自訂都保留八色。關閉自訂編輯器後返回原焦點和分頁，其他設定草稿仍待套用。
- 兩次獨立 PlayMode batchmode 啟動各通過 1/1。第一次從 Config 操作並保存八色，第二次重開確認兩個面板還原自訂色並從 Setting 讀回全部欄位。
- XML 位於 `WIP/Validation/UiConfigSettings/Project-Build/logs/` 下的 `custom-theme-editmode.xml`、`custom-theme-playmode.xml`、`custom-theme-process-1.xml` 和 `custom-theme-process-2.xml`。兩個面板及其自訂編輯器的 440×720 截圖位於 `WIP/Validation/UiConfigSettings/VisualAcceptance/custom-theme-regression/`。
