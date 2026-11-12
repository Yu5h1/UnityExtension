# AcceptanceDemo（模組驗收展示）

> Task ID: ux-acceptance-demo
> Layer: Modules（模組層）。Module ID: `ux.acceptancedemo`。
> Specification version: 0.1.0-draft。設計資料 0.1.0-draft、案例 0.1.0-draft。

模組驗收展示以可搜尋的網格提供已登錄案例的共同入口，讓驗收者定位、開啟及重設案例；各模組規範擁有該案例內的介面與驗收判定。

## 系統階層與關係

```text
模組驗收展示
├─ 目錄
│  ├─ SearchField → 回報查詢文字
│  └─ Grid → 排列以 Button 操作的模組入口
├─ 展示區 → 案例、返回目錄、重設
└─ 宿主 → 案例登錄、當前驗收對象、可用尺寸及主題
```

## 目標 1：在窄螢幕與桌面快速找到並操作驗收案例

- **為誰服務**：要驗收共用介面模組的人。第一個採用端是 [Yu5h1Lib.UI 的 UxAcceptance sample](../../../../Unity/UnityExtension/Packages/UI/Samples~/UxAcceptance/UxUiAcceptanceHost.cs)，VCP 使用其匯入場景驗收 ChatPanel。設計取自 [Web 展示首頁](../../../../Web/demo/home.html) 的搜尋與網格，以及 [Web 共用驗收入口](../../../../Web/Documentation/AcceptanceEntry.md) 的案例生命週期。
- **情境**：一般開啟先看目錄，從有名稱的按鈕選取案例（模組目錄，ACC-CATALOG）；窄寬尺寸都可讀、可點選（響應式網格，ACC-GRID）；輸入查詢縮小可見範圍，零結果可辨識且清空可復原（搜尋篩選，ACC-FILTER）；驗收者可直接選已配置的當前案例（當前驗收對象，ACC-CURRENT）。開啟與返回會切換目錄和一個可操作的案例（開啟案例，ACC-OPEN；返回目錄，ACC-RETURN），可重設當前案例或從開啟失敗恢復（重設案例，ACC-RESET；錯誤與恢復，ACC-FAIL）。
- **本輪範圍**：驗收入口與案例生命週期。案例內容、驗收結論、專案正式 UI、各平台的主題偏好與網址管理由各自的擁有者處理。
- **完成條件**：Unity 從實際匯入的共用 sample 走完中性案例與窄版、寬版、鍵盤及觸控檢查；同一場景至少可切換 Button 與 ChatPanel，並依 DX 1.1.1 核對組裝與下層引用。
- **狀態**：進行中（2026-11-12）。本版交付草案。來源證據見 [Web 紀錄](../../../../Web/Documentation/Yu5h1Lib.UX/AcceptanceDemo.md)，Unity 結果由採用端記錄。

### 採用適配

| 行為 | Unity uGUI 的需要與原生機制 |
|---|---|
| 模組目錄（ACC-CATALOG） | 需要。既有 `UxUiModuleRegistry` 提供案例資料，原生 Button 提供提交與點擊。 |
| 響應式網格（ACC-GRID） | 需要。原生格線可配置欄數與尺寸，宿主依可用寬度提供排版參數。 |
| 搜尋篩選（ACC-FILTER） | 需要。原生文字欄位能回報文字，模組擁有比對、結果集合與空狀態。 |
| 當前驗收對象（ACC-CURRENT） | 需要。既有 `CurrentTarget.json` 為宿主資料，入口決定何時解析及顯示。 |
| 開啟案例（ACC-OPEN） | 需要。沿用現有案例的掛載及卸載入口。 |
| 返回目錄（ACC-RETURN） | 需要。單場景宿主保有查詢，並恢復可操作的焦點。 |
| 重設案例（ACC-RESET） | 需要。沿用同一案例的卸載及重新掛載。 |
| 錯誤與恢復（ACC-FAIL） | 需要。宿主呈現失敗狀態並保留切換途徑。 |

### 能力盤點與前置條件

| 能力 | 規範擁有者與 uGUI 前置狀態 |
|---|---|
| 查詢通知、組字、清除、焦點 | [SearchField 1.0.1](../Controls/SearchField.md) 目標 1 進行中，uGUI 證據待補。搜尋欄外觀沿該規範引用的 Field。 |
| 按鈕名稱、啟動和焦點 | [Button 4.0.0](../Controls/Button.md)。[uGUI 既有紀錄](../../../../Unity/UnityExtension/Packages/UI/Documentation/Yu5h1Lib.UX/Button.md) 驗過 2.0.0 的部分範圍，本版引用的狀態與外觀依採用輪次補證據。 |
| 格位、平分寬度、重排 | [Grid 0.2.1-draft](../Collections/Grid.md) 目標 1 進行中，uGUI 證據待補。本模組只選擇欄數策略和結果集合。 |
| 查詢比對、案例目錄、當前目標及掛載生命週期 | 本模組。它們使用驗收案例的資料與狀態。 |

## 行為

| ID | 前提與操作 | 可觀察結果 |
|---|---|---|
| ACC-CATALOG 模組目錄 | 一般開啟，或宿主提供已登錄案例 | 初始畫面顯示目錄、搜尋欄與網格。每項有唯一穩定 ID、可讀名稱和可選摘要。每個格子有一個具名的啟動操作，支援點擊／觸控與平台鍵盤提交。顯示順序及鍵盤瀏覽順序沿登錄順序，僅啟動才開啟案例。新增案例由登錄入口提供其內容及生命週期。 |
| ACC-GRID 響應式網格 | 容器可用寬度、可見案例數改變 | 可見項目依 Grid 逐列排列。選擇可容納的最多欄數，使各欄達到目標欄寬及指定間距；欄數至多為可見項目數。放不下一欄目標寬度時使用單欄全寬。有項目時各欄平分空間，零項目時無格子。名稱可換行，格子不互疊或溢出可用範圍，內容超高可垂直捲動。 |
| ACC-FILTER 搜尋篩選 | SearchField 通知查詢已更新 | 去掉前後空白，將連續空白視作分隔，逐詞做不分大小寫的子字串比對。每個詞都要出現在同一項的名稱、摘要或 ID 其中一欄，各詞可比對不同欄。保留登錄順序。空查詢顯示全部。零結果顯示可辨識提示並讓欄位繼續可用。欄位保有焦點，組字與清除沿 SearchField。 |
| ACC-CURRENT 當前驗收對象 | 宿主配置一個當前案例 ID | 一般開啟仍顯示目錄，並提供可理解的「當前驗收對象」捷徑或對應格的標示與啟動操作。宿主可顯式指定直接開啟模式；直接開啟與捷徑都使用所配置的 ID。探索其他案例、搜尋及重設都不回寫此配置。沒有配置時目錄仍可用。 |
| ACC-OPEN 開啟案例 | 啟動可見案例、當前對象或宿主顯式切換 | 展示區只掛載選定案例，顯示可辨識名稱及返回目錄。從一個案例切換至另一個時，先釋放舊例，再掛載新例。被篩掉的格子退出佈局及焦點順序。 |
| ACC-RETURN 返回目錄 | 從案例選擇返回 | 釋放案例，還原本次目錄的查詢與結果。焦點回到原觸發格或當前對象捷徑；若觸發來源已不存在或不可見，焦點到搜尋欄。從宿主直接開啟而無觸發項時，亦回搜尋欄。 |
| ACC-RESET 重設案例 | 當前案例已就緒，選擇重設 | 釋放並重新掛載同一案例一次，回到該案例的初始狀態，保留目錄查詢與當前對象配置。 |
| ACC-FAIL 錯誤與恢復 | 使用失效的配置 ID，或案例素材缺失、掛載失敗 | 顯示具體且可理解的錯誤，釋放殘留展示內容。目錄及其他可用入口可繼續操作，驗收者可選另一例；失敗不代開其他案例。宿主離開入口時釋放仍在使用的案例。 |

## 參數與宿主入口

| 名稱 | 預設及責任 |
|---|---|
| `preferredCardWidth`、`columnGap` | 240、10 邏輯像素，取自 Web 參考。宿主可覆寫正欄寬及非負間距。所量寬度為容器扣除外距後的可用寬度。 |
| `entries` | 宿主依顯示順序提供 `id`、`title`、可選 `description` 及案例掛載與釋放入口。入口文案可本地化。 |
| `currentEntryId`、`startMode` | 宿主可提供當前案例 ID；模式預設 `catalog`，顯式選 `current` 才在開啟時掛載當前案例。 |
| `query` | 目錄的工作階段狀態，初始空白。返回時沿用。 |

允許平台採原生按鈕或可啟動的連結語意，並依宿主主題呈現焦點與外觀。Web 展示首頁的路徑複製與導航箭頭屬該站功能。來源的兩個頁面分別提供目錄與驗收展示；目錄預設啟動、返回後保留查詢與還焦點，是本輪採用端需要一起交付的組合。

## 案例與驗收

[設計資料](AcceptanceDemo/acceptance-demo-design.json)與[中性案例](AcceptanceDemo/acceptance-demo-cases.json)各自版本化。[manifest](../manifest.json)記錄內容 SHA-256。中性案例採 schema 1。每步 `expect` 只比較列出的觀測值；ID 與生命週期事件陣列依順序完整比較。格子容器、欄寬和間距以邏輯像素度量，單一幾何值的容許誤差為 1 邏輯像素。案例使用同步掛載的宿主資料，觀測發生在輸入事件和佈局處理完成後；實作端另驗證實體輸入、延遲素材與場景退出。

| 行為 | 中性案例 | 平台檢查 |
|---|---|---|
| 模組目錄（ACC-CATALOG） | `catalog-open` | AD01、AD02 |
| 響應式網格（ACC-GRID） | `responsive-grid` | AD01 |
| 搜尋篩選（ACC-FILTER） | `multi-token-search`、`clear-and-restore` | AD01、AD02 |
| 當前驗收對象（ACC-CURRENT） | `current-target-opt-in`、`current-target-shortcut`、`invalid-target-recovery` | AD03 |
| 開啟案例（ACC-OPEN） | `catalog-open`、`switch-and-reset` | AD02、AD03 |
| 返回目錄（ACC-RETURN） | `return-with-search`、`return-without-trigger` | AD02 |
| 重設案例（ACC-RESET） | `switch-and-reset` | AD03 |
| 錯誤與恢復（ACC-FAIL） | `invalid-target-recovery`、`failed-case-recovery` | AD03 |

- **AD01 網格與文字**。在窄直向與寬版檢查完整標籤、搜尋列、格子位置、可見焦點與垂直捲動。縮窄時不出現因入口網格造成的水平捲動。
- **AD02 操作與返回**。用鍵盤及觸控操作一個搜尋結果，返回後檢查查詢和焦點。實體中文輸入法使用 SearchField 案例。
- **AD03 宿主生命週期**。在實際匯入場景切換 Button 和 ChatPanel、重設、返回、重進及退出場景，確認無舊回呼、殘留內容或失效目標的假就緒。

採用端在 [DX 1.1.1](../../DX/DX-Guidelines.md#5-組裝驗收) 映射 DX-ENTRY、DX-ASSEMBLY、DX-REFERENCE、DX-SETTINGS、DX-STRUCTURE、DX-PREVIEW。代表性消費情境為宿主登錄兩個既有案例、配置當前目標，從交付入口搜尋、掛載、重設及退出。缺少已驗證的下層交付時，採用紀錄列出影響範圍。

## 採用工作順序

1. **入口宿主**。把目前驗收頁的模組列改為搜尋目錄和網格。用現有登錄和案例入口完成 ACC-CATALOG 至 ACC-FAIL 的掛載接線。交付實際場景中的 Button 與 ChatPanel 切換。
2. **共用基礎證據**。可與入口接線並行，對照能力盤點逐項核查 SearchField、Button 和 Grid 可引用範圍，提供缺口及版本紀錄。下層負責自身控件案例，上層只驗證委派與組合。
3. **整合驗收**。合併前兩項後，以實際匯入的場景執行中性案例和 AD01～AD03，保留窄版與寬版證據，並分別記錄行為及 DX 結果。
