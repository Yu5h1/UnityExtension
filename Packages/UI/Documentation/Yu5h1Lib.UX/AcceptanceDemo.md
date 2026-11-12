# AcceptanceDemo — Yu5h1Lib.UI

```text
UxAcceptanceDemo
├─ 展示目錄 → 模組登錄、即時搜尋、響應式按鈕格線
└─ 展示頁 → 目前案例、返回、重設與錯誤回復
```

## 規範與實作

行為依 [AcceptanceDemo UX](../../../../../../UX/Documentation/ModuleSpec/Modules/AcceptanceDemo.md)。[Web 展示入口基線](../../../../../../Web/Documentation/Yu5h1Lib.UX/AcceptanceDemo.md) 提供搜尋、格線與模組生命週期的來源觀察。

釘選 UX `0f3bb0b25c85e0e86d5bef3fa14fcfe3da3516cd` 的規範、設計與案例 `0.1.0-draft`。套件內 [快照](../../Tests/Editor/UX~/AcceptanceDemo/) 的三份 SHA-256 均與 [UX manifest](../../../../../../UX/Documentation/ModuleSpec/manifest.json) 一致。

| 行為 | uGUI 入口 |
|---|---|
| 模組目錄、格線、搜尋（ACC-CATALOG、ACC-GRID、ACC-FILTER） | `UxUiModuleRegistry` 提供模組順序、顯示名稱、說明與 ID。`UxUiAcceptanceCatalog` 以原生 `InputField`、`GridLayoutGroup` 與 `ScrollRect` 適應視窗寬度，查詢會即時更新按鈕，並提供清除。欄寬和間距可由場景宿主設定。 |
| 常駐目標（ACC-CURRENT） | 場景進入可搜尋目錄，鍵盤焦點先落在第一張卡片。由 `CurrentTarget.json` 在目錄標示當前驗收模組。`InitializeHost` 供既有直接開啟入口，`SelectCase` 供指定 ID。 |
| 開啟、返回、重設與失敗（ACC-OPEN、ACC-RETURN、ACC-RESET、ACC-FAIL） | `UxUiAcceptanceHost` 掛載所選案例。返回保留搜尋，重設重新掛載同一案例。無效或失敗案例顯示原因與返回入口。 |

Button、SearchField、Grid 和 VisualFoundation 的完整 uGUI 採用沿各自規範。這一輪以原生控制項驗證展示入口的組合行為，按鈕和搜尋欄位的共用色彩、焦點狀態及裝置輸入見 [ChatPanel 示範版對齊計畫](../../../../Documentation/ChatPanelDemoParityPlan.md#共用基礎的取捨)。

## VCP 試用

開啟 VCP 已匯入的 `UxUiAcceptanceDemo.unity` → Play → 在目錄搜尋模組 → 點選按鈕。用「返回」回到原查詢，或在展示頁用「重設」重新開始。

## 驗證與後續

VCP 的 Unity 6000.6.0f1 batchmode 以實際匯入的 sample 執行。EditMode 22/22（`20261112-123151-tests.xml`）涵蓋搜尋、清除、窄寬格線、卡片捲動事件路由、返回焦點、重設、掛載失敗回復和既有 Button／ChatPanel。PlayMode 1/1（`20261112-123230-tests.xml`）從場景啟動到搜尋、鍵盤提交、開啟 ChatPanel 和返回原查詢。報告在 VCP 專案旁的 `Virtual-Chat-Partner-Build/logs`。

這一輪以聚焦案例驗證場景與組合行為。完整中性案例 adapter、任意長標籤的窄版外觀，以及實體鍵盤、觸控和中文 IME 仍待驗收。現有目標解析器在 `CurrentTarget.json` 缺少時會標示 ChatPanel，VCP 匯入的檔案包含明確的 ChatPanel 目標。
