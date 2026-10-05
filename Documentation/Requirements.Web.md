# Requirements — Web

- **目標專案（Target）**：Web 庫 `Yu5h1Lib.Web`，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Web`。
- **提出專案（Requester）**：UnityExtension，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension`。
- **目前依賴方式**：尚未接入；本檔規劃首次使用 Web 設計契約與建置產物。
- **未結案需求數**：4。
- **提出日期**：2026-10-02；2026-10-05 改為 Web 來源、AppsMenu 試點。
- **順序**：先完成 WEB-1、WEB-3，配合 UnityExtension 的 AppsMenu 試點，從小規模驗證開始；試點通過後才啟動 WEB-2。WEB-4 延後。Apps 物理部分另依 Physics 試作。
- **背景**：Yu5h1Lib.Web 是 UI 設計與實作的來源，Unity 以 Yu5h1Lib.UIToolkit 復刻：色票與設計參數由 Unity 工具轉換，結構與互動依 Web 規格手寫。Unity 先做出來的 UI，先以本檔把設計交給 Web。共用規格不代表共用執行碼。
- **驗收責任**：Web 提供產物與證據；UnityExtension 在試點中確認符合需求後更新本檔狀態。完整分工與門檻見 [跨平台 UI 一致性](跨平台UI一致性.md)。

## UNITYEXTENSION-WEB-1 — 將產生的主題色票檔宣告為穩定契約

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-02 提出；2026-10-05 修正實際格式及更新驗收；同日 Web 已交付，見 Web commit `9528c9d` 與 [跨平台主題色票交付契約](../../../Web/Documentation/NativeUIAndDataUpdatePlan.md#跨平台主題色票交付契約)，待 UnityExtension 以轉換工具（產物 WebTheme）驗收後才改為 Implemented） |
| **Requirement** | 宣告既有 `dist/theme/theme.css` 的跨平台讀取契約，涵蓋格式、相容變更、來源追蹤與重新交付方式。 |
| **Evidence** | `scripts/build-ui.mjs` 的 `buildTheme` 已以 `resolveTheme` 產生 light／dark 完整色票。兩個 `[data-ui-scheme]` 區塊各含 `color-scheme` 與 `--ui-*` 宣告；值目前為 6／8 位十六進位。檔頭 SHA-256 是 `src/theme/index.ts` 與建置腳本的組合指紋，不是格式版本，也不是 CSS 內容雜湊。 |
| **Acceptance** | 文件明定位置、scheme、必要變數與值格式；明確允許並驗證 `color-scheme`，不再宣稱只有自訂屬性。定義空白／註解／宣告順序不影響解析，以及新增變數、改名／移除、值格式改變的相容規則與變更紀錄位置。交付記錄可辨識契約版本及來源版本或 commit，不以指紋代替相容性判斷。Web 來源改變須重新建置；Unity 以該產物匯入後，兩個 scheme 的 RGBA 語意值相同，同一輸入重匯入無差異。來源／產物變更可被檢查發現，格式錯誤不能覆寫上一份有效 USS。 |
| **Impact** | Unity UI Toolkit 主題匯入；之後其他平台可依同一契約接入。 |
| **Workaround** | 暫時可讀目前產物作開發驗證，但不算穩定契約已交付；匯入失敗保留舊 USS 並標明未更新，不能視為同步完成。 |

試點沿用現有 CSS，不要求額外 JSON 輸出或完整 CSS 轉譯器。Unity 匯入與過期檢查的實作由 UnityExtension 擁有，細節見 [主題轉換](跨平台UI一致性.md#主題轉換)。

## UNITYEXTENSION-WEB-2 — 聊天面板設計

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-02 提出；等 [AppsMenu 試點](跨平台UI一致性.md#試點appsmenu)通過後再開始；是否改走「Unity 設計搬到 Web，再復刻回 Unity」待決） |
| **Requirement** | 對照既有 Unity 與 Web 消費端聊天能力，集中共用契約並補足 Web 通用元件，保留可用實作。 |
| **Evidence** | Unity 已有 [聊天面板模組](聊天面板模組.md) 與 `com.yu5h1.uitoolkit` 實作；HealthAI 網頁 demo 有 `W:/UnityProject/HealthAI/docs/demo/HealthAI/js/conversation.js`。Web 的 [方向分析](../../../Web/Documentation/WebLibraryDirectionAnalysis.md) 已列該 demo 為分析對象；HealthAI 的 Unity 需求見其 `docs/unity/Requirements.UnityExtension.md` 的 HEALTHAI-UNITYEXTENSION-7–14。實作前重新核對來源現況。 |
| **Acceptance** | 先提交兩端能力對照：直接重用、平台接線、通用缺口與產品政策。共用契約涵蓋排列／對齊、泡泡、串流呈現／思考中、捲動跟隨、依位置淡出、多行輸入、送出／麥克風請求及上下擴充列；平台呈現差異有理由。Web demo 使用正式共用元件與主題資料；共用案例驗證追加、串流中斷、使用者捲動與輸入狀態。第二個中性宿主只更換資料／接線即可使用，不能複製聊天 UI 邏輯。網路、語音服務與產品流程留在宿主。 |
| **Impact** | Unity 聊天模組、HealthAI 網頁 demo 及其他聊天 UI 使用端。 |
| **Workaround** | 保留現有實作，HealthAI 遷移照常進行；後續按共用契約差異表補齊，不為轉移文件維護位置而重做 Unity 元件。 |

## UNITYEXTENSION-WEB-3 — AppsMenu 模組

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-02 提出；2026-10-05 改為可直接使用的模組，作為 AppsMenu 試點，從小規模驗證開始） |
| **Requirement** | 依 Unity AppsMenu 的設計，在 Yu5h1Lib.Web 建立可直接使用的 Apps 模組：有預設行為，政策可以覆寫；附 Unity 能轉換的設計參數，以及兩端都能執行的案例資料。 |
| **Evidence** | 設計來源是 Unity 的 `Packages/UIToolkit/Samples~/AppsMenu`（`AppsMenuDemo.cs`），行為分類見 [試點](跨平台UI一致性.md#試點appsmenu)；接線規則「手勢屬於哪個元素，那個元素就同時捕獲指標、跑自己的計時器」見 [指標互動與世界橋接](指標互動與世界橋接.md#一條規則管所有手勢)，違反時會靜默失效。Web 已有 `long-press.ts`、`reorder.ts`、`item-list.ts`；HealthAI demo 的 `desktop.js` 另有桌面行為。實作前重新核對來源現況。 |
| **Acceptance** | 非物理行為（長按 400ms 與接續點擊抑制、移動容許量、取消／卸載、圖示拖曳、拖進 Apps 按鈕或面板、從面板拖回桌面、面板內排序、群組拖曳、面板開合、入場錯開）由正式公開入口提供，並有可執行的範例。覆寫點至少涵蓋障礙物集合、項目初始位置、落點判定、點擊與長按的產品動作、甩飛門檻，各有預設值；不寫死產品政策，不出現產品名稱分支。範例的小變體只換內容、資料與政策就能使用，不改模組。設計參數有單位與來源，格式可被 Unity 轉換；案例資料有穩定 ID、初始狀態、操作序列與預期狀態，標明版本。避障待 Physics 盤點確認分類；慣性、甩飛／反彈、撞擊、吸入、彈簧與抖動在 Physics 定案前明列未驗收，不另造物理引擎，也不能因非物理部分通過就將整項標為完成。 |
| **Impact** | Unity 的 AppsMenu 復刻、HealthAI 桌面，以及需要長按／拖曳／排序的其他介面。 |
| **Workaround** | Unity 沿用現有 `Samples~/AppsMenu` 組合與 common／animation 能力；物理展示延後。 |

## UNITYEXTENSION-WEB-4 — 設定面板的最小共用設計參數與案例

| 欄位 | 內容 |
|---|---|
| **Status** | Deferred（2026-10-05 提出；同日延後：試點改為 AppsMenu，設定面板的保存語意與 Unity Preferences 的對應尚未討論。Web 在延後前依舊順序已先交付契約 1.0.0（Web commit `9528c9d`，[跨平台試點交付](../../../Web/Documentation/SettingsPanelPlan.md#跨平台試點交付)），予以保留、暫不擴充；保存語意討論後再核對是否符合並驗收） |
| **Requirement** | 從既有設定面板抽出試點必要的設計參數與中性狀態案例，讓兩端引用同一維護來源。 |
| **Evidence** | [SettingsPanelPlan.md](../../../Web/Documentation/SettingsPanelPlan.md) 已區分面板、宿主草稿、ConfigStore 與 storage／apply，含保存成功但套用失敗的契約；現有色票輸出不足以交付間距、圓角、字級等視覺參數。只對齊狀態名稱無法驗證草稿與提交結果。 |
| **Acceptance** | 明列設定面板實際需要的參數、來源與單位，提供可被 Web 元件及 Unity 匯入／映射共同使用的單一資料來源與交付格式；不全面重整全庫。既有設定文件繼續擁有保存語意，補上可追蹤版本的中性案例資料（ID、初始狀態、操作、保存／套用結果、預期狀態），由兩端測試入口消費。案例範圍待保存語意討論後重新界定，至少涵蓋失敗、取消、卸載及另一個使用端的擴充。明確列出平台限定行為，不能為配合 DOM 結構要求 Unity 複製平台 API。 |
| **Impact** | 設定面板試點、SaveBar 的跨平台語意與後續元件對照方法。 |
| **Workaround** | 可先依既有規格盤點與實驗；人工抄參數或兩份各自維護的案例不算本需求通過。 |
