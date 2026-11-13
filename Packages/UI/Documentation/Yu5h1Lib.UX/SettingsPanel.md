# SettingsPanel 採用紀錄

實作計畫見 [Yu5h1Lib.UI 的 Config 與 Setting](../../../../Documentation/UiConfigSettingsPlan.md)。

## 系統階層與關係

```text
Yu5h1Lib.UI
└─ SettingsPanel.Create
   ├─ Config → GroupList → 選擇群組
   ├─ Setting／Embedded → 單一群組
   ├─ Tabs → 選擇分類、顯示宿主提供的內容
   └─ SaveBar → 顯示狀態並發出模式、保存及重試事件
UxAcceptance 的 Config 與 Setting 案例
└─ 共用草稿 → Common ConfigStore → Preferences.DefaultStorage → PlayerPrefs
```

## 公開入口

以 `SettingsPanel.Create(parent, SettingsPanel.Presentation.Config)` 建立設定對話框，或使用 `Setting`／`Embedded` 建立一個群組的區域設定。掛載後以 `SetGroups` 傳入群組及有 ID 的分類，分類的 `RectTransform` 會掛入面板。各入口可共用群組 ID 與欄位定義，每個面板提供自己的內容物件。用 `Tabs.Orientation` 選擇頂部或側邊分頁。

宿主訂閱 `Opened`、`Closed`、`ResetRequested` 及 `SaveBar` 的事件，並呼叫 `SetState` 同步狀態。要使用確認保存，可建立一個 `ConfigStore<T>`，供應驗證、建立獨立快照、讀取、寫入與套用函式。從 Unity 執行緒呼叫入口，卸載時解除訂閱並釋放 store。示範可從 uGUI 的 UxAcceptance 目錄選「Config 與 Setting」。[公開消費範例](../../Samples~/UxAcceptance/Cases/UxUiSettingsDemoCase.cs)

## 版本與前置

- 採用來源及 DX 版本見[實作計畫](../../../../Documentation/UiConfigSettingsPlan.md#版本依據)。套件測試逐檔核對 UX manifest 中的 SettingsPanel 規範、設計、案例及 Configuration 規範雜湊。
- 保存狀態參考 Configuration。對話框的忙碌關閉限制與焦點回復採用本輪的 uGUI 驗證。AsyncOperation、TransientSurface 與 VisualFoundation 的個別模組驗收仍有各自範圍。
- 以 `ButtonAddon` 作為按鈕入口、`ScrollView` 作為分頁列與群組列的捲動容器、原生 `Toggle` 作為模式選項。執行期組裝由公開工廠提供。編輯期預覽由本輪採用範圍之外的工作負責。

## Batchmode 驗收

以 Unity 6000.6.0f1 執行。隔離專案匯入已提交的 Core、Common、UI 基線和本輪檔案，並將同一份 UxAcceptance 範例匯入 Assets。

| 驗證 | 證據 |
|---|---|
| 預設與載入、載入失敗不覆寫（CFG-01、CFG-02） | EditMode 使用真實 PlayerPrefs 驗證無存檔、損毀、寫入與新 store 再讀 |
| 保存提交順序、保存失敗與重試、已保存未套用（CFG-03、CFG-04、CFG-05） | EditMode 驗證寫入完成才更新接受值、失敗保留前值，以及重試套用不再寫入 |
| 單一進行中命令、卸載與延遲完成、快照隔離（CFG-06、CFG-07、CFG-08） | EditMode 驗證寫入中拒絕重複命令、卸載中止待完成讀取，以及宿主取值與回呼的快照隔離 |
| 開關、分頁與鎖定（SET-OPEN、SET-CLOSE、SET-TABS、SET-LOCK）及保存按鈕與模式（SAVE-BUTTON、SAVE-TOGGLE） | EditMode 驗證群組路由、拒絕不合法分類、方向切換、欄位保留、忙碌鎖定、模式請求及兩種重試事件 |
| DX 公開入口、組裝及下層引用 | EditMode 透過 UxAcceptance 案例建立兩種面板並驗證共用元件與群組路由 |

- EditMode 11/11 通過，0 編譯錯誤。包含四項來源雜湊比對。XML 為 `WIP/Validation/UiConfigSettings/Project-Build/logs/20261114-022847-tests.xml`。
- PlayMode 1/1 通過，0 編譯錯誤。透過原生 uGUI Submit／Move 事件驗證初始分頁焦點、關閉回到設定入口、手動草稿關閉後捨棄、保存到 PlayerPrefs，以及在另一個面板重開時同步已接受值。XML 為 `WIP/Validation/UiConfigSettings/Project-Build/logs/20261114-022902-tests.xml`。

本輪範圍另見[驗收條件](../../../../Documentation/UiConfigSettingsPlan.md#驗收條件)。UX 的輔助技術、各縮放與配色版面檢查有各自的操作條件。宣告欄位、Theme 與其他平台的 UI 不列入此輪。
