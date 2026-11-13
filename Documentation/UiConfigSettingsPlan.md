# Yu5h1Lib.UI 的 Config 與 Setting 實作計畫

> Task ID: ugui-config-settings
>
> 採用來源為 UX Configuration 1.5.0、SettingsPanel 1.1.0，以及 DX Guidelines 1.1.1。

## 系統階層與關係

```text
UnityExtension
├─ Yu5h1Lib.Common
│  ├─ Preferences → 直接保存
│  └─ ConfigStore → 載入、接受值、確認保存、套用與重試
└─ Yu5h1Lib.UI
   └─ SettingsPanel
      ├─ Config → 多群組，引用 GroupList
      ├─ Setting → 單一群組
      ├─ Tabs → 分類選擇及原有內容
      └─ SaveBar → 模式、保存、重試及狀態
宿主 → 預設、草稿、資料驗證、保存後端、套用與面板接線
```

## 實作順序

1. 核對已上版的 UX 規格、設計參數與案例雜湊。沿用 Common 的 Preferences 直接保存能力及既有驗收。盤點 uGUI 的 ButtonAddon、ScrollView、Toggle、LayoutGroup 與 EventSystem。
2. 在 Common 補確認保存的公開入口。宿主提供驗證與快照複製、讀寫及套用。服務依讀取、寫入提交、套用三個階段回報狀態，處理重試、命令互斥及卸載取消。既有直接保存可繼續使用 Preferences。
3. 採用原生 Button、Toggle、ScrollRect 佈局及事件系統。以共享 Tabs 保存選擇、方向、鍵盤導覽和分類內容；以共享 GroupList 提供群組選擇。SettingsPanel 讓多群組 Config 與單群組 Setting 使用相同分頁及保存列；Setting 可嵌入或使用單群組對話框。
4. 在 uGUI 的 UxAcceptance 加一個案例。使用相同的群組定義和草稿接線展示兩個入口，讓面板的實際事件驅動服務。展示資料使用專用偏好鍵及既有 Unity 保存格式。宣告 JSON 產生欄位、Theme 格式及 Web mini 的拖曳與定位持久化各有獨立契約，留待相應的 Unity 消費端採用。
5. 執行本輪 Unity batchmode 驗收，保存 XML 與日誌。檢查差異後提交本輪檔案並推送至 UnityExtension 遠端。

## 驗收條件

- ConfigStore 經公開入口驗證缺失與損毀資料、保存提交順序、寫入與套用失敗、重試、拒絕重複命令、卸載及快照隔離。
- uGUI 驗證 Config 群組路由、Setting 直接開分類、橫列與直欄選擇、切換保留欄位物件、忙碌防關閉、保存列模式和重試事件。從 UxAcceptance 的公開入口確認消費組裝。
- 以 Unity 6000.6.0f1 的測試 runner 聚焦 ConfigStore 和 SettingsPanel 的 EditMode／PlayMode 檢查。證據記錄於套件的 UX 採用紀錄。輔助技術與實體裝置輸入依實際測試覆蓋記錄。
- 定稿前核對已提交來源、測試驗收入口與遠端 commit。

## 版本依據

- Configuration 1.5.0 的 SHA-256 為 `1b4928d75c16b7610be7e377b8d2aafed845fca42f1475866baf4eea0a7234b8`。
- SettingsPanel 1.1.0 的 SHA-256 為 `a87560ce27b8190cb4e7f584a6565283c29f4ef9b382ae451ab266785697690e`。
- SettingsPanel 設計及案例皆為 1.0.0，來源是 UX `71aa3c1` 的 ModuleSpec manifest。DX Guidelines 1.1.1 來自 `9c48bc1`，SHA-256 為 `005a2d8d946117b712f4eabc38d37f0298bb680b8edc649d1d4b5e7231a57678`。
- AsyncOperation 的取消、互斥及重試行為由 ConfigStore 的設定操作驗證。TransientSurface 的對話框關閉與焦點回復在本輪直接驗證。共用色彩使用既有 UguiControlPalette，VisualFoundation 全模組驗收沿用其原有追蹤。
