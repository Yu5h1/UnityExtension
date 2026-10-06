# 跨平台 UI 一致性

> Task ID: cross-platform-ui-parity
>
> 狀態：**已排入、暫緩**（2026-10-05）：其他任務優先，未來確定走這條路時從本文件接續。Yu5h1Lib.Web 是 UI 設計與實作的來源；Web 端的 AppsMenu 試點與色票契約已交付，Unity 端的復刻與 WebTheme 轉換依需求啟動，見[啟動條件](#啟動條件)。WebGL 直接使用 Web 庫，見 [WebGL](#webgl直接使用-web-庫)。執行進度由 [handoff.md](../handoff.md) 擁有。
> Web 端需求由 [Requirements.Web.md](Requirements.Web.md) 擁有；元件規格、參數與案例集中在 Web，Unity 的復刻方式與平台差異留在本專案。

## 系統階層與關係

```text
Yu5h1Lib
├─ Yu5h1Lib.Web（Web 庫）：UI 設計與實作的來源
│  ├─ Theme：resolveTheme → 建置色票 theme.css
│  └─ 元件：Lit 實作、設計參數、共用案例、可直接使用的模組與範例
├─ UnityExtension
│  ├─ Yu5h1Lib.UIToolkit：復刻 Web 元件，範圍只限 UI 互動
│  │  ├─ Editor 轉換：theme.css 與設計參數 → WebTheme（USS／TSS）
│  │  └─ 執行期：依 Web 規格手寫的 UXML 與 C# 元件
│  └─ common／animation：既有的手勢、排列與運動能力
├─ 使用端：內容、資料，以及要覆寫的政策
└─ Motion：運動算式的共用（物理互動共用 C++ 核心、插值表現共用規格與 fixtures），獨立專案決定
```

Web 是來源，Unity 是復刻。Unity 先做出來的 UI，先把設計搬到 Web 成為來源，再由 Unity 依 Web 復刻回來。Web 的 DOM、slot 與事件 API 不是 Unity 必須模仿的實作；Unity 依 UI Toolkit 慣例對齊行為與狀態的意義，平台差異明確記錄。

| 名稱 | 位置 | 角色 |
|---|---|---|
| Yu5h1Lib.Web | `Yu5h1Lib/Web` | UI 設計與實作的來源 |
| Yu5h1Lib.UIToolkit | `UnityExtension/Packages/UIToolkit` | 擴充 UI Toolkit，負責復刻 Web 的 UI 互動，轉換工具也放在這裡 |
| Yu5h1Lib.Web package | `UnityExtension/Packages/Web`（UPM） | Unity 端的 Web 平台能力，承接 `common` 的 WebSupport 等程式（[遷移計畫](Web套件遷移.md)）；不屬於本計畫的 UI 復刻 |

## 問題與目標

同一個 UI 在不同平台各自發明，外觀和行為會逐漸漂移，也重複處理手勢與狀態。跨平台共用執行碼的成本過高，因此改由 Web 維護一次設計與實作，Unity 以低成本復刻。

- 設計資料維護一次：色票與尺寸參數由 Web 產出，Unity 以工具轉換，不靠人工抄值。
- 結構與互動依 Web 規格在 Unity 手寫；兩端以同一份案例驗證行為。
- 每個平台的模組實作一次，另一個使用端只換內容、資料與政策。
- 本輪不建立跨語言 UI 框架，不模擬 DOM，不轉譯 Web 元件的 CSS，不引入 WebView；執行期自訂配色另列待決。

## 共用層次與責任

| 層 | 做法 | Unity 負責 |
|---|---|---|
| 色票、設計參數 | Web 產出，Unity 工具轉成 USS | 轉換、過期偵測與相容性驗證 |
| 元件結構與樣式 | Web 規格；Unity 手寫 UXML／USS | 元件對照紀錄與截圖比較 |
| 互動行為與狀態 | Web 實作與案例；Unity 以 C# 復刻 | 執行同一份案例 |
| 物理運動 | Motion 定案前屬平台限定 | 沿用既有 common／animation |
| 輸入、焦點與生命週期 | 各平台原生 | 平台接線 |

行為由 TS 與 C# 各寫一份，就如實記為兩份實作；以共用案例控制漂移。

| 擁有者 | 責任 |
|---|---|
| Yu5h1Lib.Web | 元件設計與實作、設計參數、共用案例、可直接使用的模組與範例 |
| UnityExtension | 轉換工具、Yu5h1Lib.UIToolkit 復刻、平台差異與驗收紀錄；把 Unity 先做的 UI 整理成給 Web 的需求依據 |
| 使用端 | 內容、資料，以及覆寫的政策 |

## 設計

### 啟動條件

設計抽到 Web 庫是常態；Unity 端的工作只在有具名的產品需求時才啟動，不為「將來可能用到」或兩端一致本身而做。

| 工作 | 啟動條件 |
|---|---|
| 新 UI 的設計與實作 | 一律以 Web 庫為來源 |
| AppsMenu 修正：入場動畫 | **現在進行**（暫緩的例外，屬於 Web）：屬於已交付的 AppsMenu 試點本身；AppsMenu 也是 Motion 的驗證場景。Apps 按鈕吸塵器已由 Web 以 Motion Scatter2D 完成（2026-10-06） |
| Unity 復刻 Web 元件（P3 起） | 某個 Unity 產品確定要使用該元件的 Web 設計 |
| WebTheme 轉換工具（P2） | 第一個需要 Web 色票的 Unity UI Toolkit 產品出現 |
| 既有 Unity 元件（AppsMenu、聊天模組） | 維持現狀照常使用；不為一致性而依 Web 重做 |

停損：沒有具名產品需求時，P2 之後的階段凍結；已交付的 Web 契約、模組與案例保留，需求出現時直接接續，不重做。

### 復刻方式

只轉設計資料：色票與尺寸參數由 Yu5h1Lib.UIToolkit 的 Editor 轉換工具自動產生 USS；UXML 結構與 C# 互動依 Web 規格手寫。UI Toolkit 以 flexbox 排版，UXML／USS 與 HTML／CSS 概念接近，結構大多能一對一手寫對應；容易出錯又不值得人工抄的是數值，這部分自動化。

漂移由三項控制：同一份案例資料、兩端並排截圖、元件對照紀錄（Web 規格章節、Unity 入口、平台差異及原因）。視覺驗收比較同一內容與主題下的層級、間距、可讀性與操作區域，允許字型渲染差異，不要求逐像素相同。走完一輪、證明手寫結構的漂移成本確實很高，才另外評估自動產生結構。

### 主題轉換

現有 `scripts/build-ui.mjs` 的 `buildTheme` 使用 `src/theme/index.ts` 的 `resolveTheme`，輸出 `dist/theme/theme.css`。它包含 light／dark 兩個 scheme 區塊，每個區塊有 `color-scheme` 及已推導的 `--ui-*` 色票；檔頭 SHA-256 由 Theme 原始碼與建置腳本共同產生。

1. **固定既有產物契約**：先完成 [UNITYEXTENSION-WEB-1](Requirements.Web.md#unityextension-web-1--將產生的主題色票檔宣告為穩定契約)。沿用 CSS，不另做 JSON 輸出。指紋用於追蹤來源，不當成格式版本。
2. **受限解析與驗證**：轉換工具只接受契約指定的 scheme 與宣告；驗證並略過 `color-scheme`，擷取色票。支援註解、空白及宣告順序差異；缺必要變數、重複名稱、未知選擇器或不支援值格式都報錯。新增色票是否相容由契約規則判斷，不把所有新增變數一律視為破壞性變更。
3. **產生 Unity 資產**：輸出 USS／TSS，保留變數名稱；記錄自動產生標記、來源版本或 commit、來源指紋、輸入內容雜湊與工具版本。元件引用變數，不抄寫色碼。必要時把 8 位十六進位轉成 `rgba()`，驗收比較 RGBA 語意值，不要求字串完全相同。
4. **更新與過期檢查**：由可重現的 Editor 入口讀取指定 Web 建置產物；來源更新後重新轉換。整合／交付檢查核對輸入雜湊與生成紀錄，過期時不得宣稱同步完成。同一輸入重新轉換不產生差異；失敗不覆寫上一份有效產物，明確標為未更新。

設計參數（圓角、間距、字級等）依 Web 交付的格式以同一工具轉換，只納入試點元件需要的項目，不先整理全庫。Unity 目標版本的透明色、變數及主題切換仍須實測。既有 `customStyle` 唯讀的確認只支持「不能直接經此 API 改變變數」，不推論所有執行期配色方案都不可行。

### Unity 端的主題載體與切換

Unity 有三種同樣叫「theme」的東西，各自分工，不互相取代：

| 名稱 | 是什麼 | 用在哪裡 |
|---|---|---|
| WebTheme | Yu5h1Lib.UIToolkit 的轉換工具由 Web `theme.css` 產生的 USS／TSS（`WebTheme-Light.tss`、`WebTheme-Dark.tss`） | UI Toolkit |
| common `Theme` | `Packages/common` 的 ScriptableObject，以 binding 把具名參數推到 Unity 物件 | uGUI、SpriteRenderer |
| `ThemeStyleSheet` | Unity UI Toolkit 原生的主題樣式表格式；WebTheme 就是這種檔案 | UI Toolkit |

- **UI Toolkit**：WebTheme 透過 `PanelSettings.themeStyleSheet` 套用，切換明暗就是換成另一份 TSS。UI Toolkit 不另寫主題切換器，也不在 `Yu5h1Lib` 新增名為 `Theme` 的型別。
- **uGUI**：沿用 common `Theme` SO 與既有 binding。
- **切換的觸發與保存只有一個擁有者**：由誰觸發（使用者選擇、跟隨系統、程式指定），以及選擇怎麼保存，兩個 UI 系統共用同一套。該擁有者在 uGUI 切換機制復盤時決定；UI Toolkit 只負責依結果替換 `themeStyleSheet`，不自行保存選擇。

### 試點：AppsMenu

AppsMenu 目前的設計在 Unity 的 `Packages/UIToolkit/Samples~/AppsMenu`。試點走完整個循環，先從小規模開始：

1. Unity 整理 AppsMenu 的行為清單、接線規則、參數與既有機制，寫進 [UNITYEXTENSION-WEB-3](Requirements.Web.md#unityextension-web-3--appsmenu-模組) 的依據。接線規則見 [指標互動與世界橋接 — 一條規則管所有手勢](指標互動與世界橋接.md#一條規則管所有手勢)。
2. Web 建立可直接使用的 Apps 模組：有預設行為，政策可以覆寫。
3. Unity 依 Web 的模組在 Yu5h1Lib.UIToolkit 復刻，沿用 `GroupDragLayout`、`PointerVelocity`、`ElementTarget`、`Gesture.LongPress` 等既有能力。
4. 兩端執行同一份非物理案例。
5. 在同一個 demo 做一個小變體（例如不同圖示數量、障礙物或落點動作），證明不改模組就能使用。

| 行為 | 類別 |
|---|---|
| 長按 400ms 與接續點擊抑制、移動容許量、取消與卸載 | 共用 |
| 圖示拖曳、拖進 Apps 按鈕或面板、從面板拖回桌面、面板內排序 | 共用 |
| 群組拖曳（直接跟手）、面板開合、入場錯開（時間驅動） | 共用 |
| 單一圖示甩飛與外框反彈 | Motion Throw2D；Web 已接入（設計資料 1.1.0） |
| Apps 按鈕吸塵器與釋放散佈，含吸收／釋放時的矩形障礙投影 | Motion Scatter2D；Web 已接入（設計資料 2.0.0）。Web 的規則與 Unity 範例不同：吸收範圍預設無上限並持續擴大、吸力在 0.8 秒內漸強、只有本次零捕獲才釋放、釋放接 Throw 往外拋；Unity 範例是桌面清空才釋放 |
| 甩飛撞擊障礙、群組放手慣性、甩進 Apps 自動收納、面板彈簧、收納抖動 | 依賴 Motion，Web 未實作；Unity 範例沿用既有實作，不列入一致性驗收 |

模組可覆寫的政策：障礙物集合、項目初始位置（桌面或面板）、落點判定、點擊與長按的產品動作、甩飛門檻，預設值由 Web 定。模組不得寫死產品政策，也不得出現產品名稱分支。

共用案例以穩定 ID 記錄初始狀態、操作序列、預期狀態；案例的權威來源在 Web，Unity 記錄所驗版本與結果。指標捕獲、事件時序等平台操作另由各端驗證。

### 現有元件的去向

| 元件 | 去向 |
|---|---|
| Apps | 第一個試點 |
| 聊天面板 | 試點後依 [UNITYEXTENSION-WEB-2](Requirements.Web.md#unityextension-web-2--聊天面板設計)；是否同樣走「Unity 設計搬到 Web，再復刻回 Unity」待決 |
| 設定面板 | 延後；保存語意與 Unity Preferences 的對應尚未討論 |
| 向量圖示 | 依 [向量圖示系統](向量圖示系統.md) 與 Web 圖示計畫核對 SVG 子集；能共用資產不代表動態渲染可直接共用 |

聊天的訊息來源、網路串流、語音服務與產品工作流留在使用端；UI 只呈現資料及提出請求。HealthAI 的 [聊天模組遷移](file:///W:/UnityProject/HealthAI/docs/unity/聊天模組遷移計畫.md) 照常進行。

### WebGL：直接使用 Web 庫

WebGL 版本身跑在瀏覽器頁面裡，Web 庫的元件在那裡是原生的：不復刻、不轉換、不需要 WebView，Unity 只以 jslib 補互動與通知。第一個實例是多平台的 [Virtual-Chat-Partner](file:///W:/UnityProject/Virtual-Chat-Partner)（PC、Android、WebGL；UI 以 uGUI 為主，已有自訂模板 `Assets/WebGLTemplates/VCP/index.html`）。

- **疊層**：Web UI 以 DOM 疊在 Unity canvas 上方；沒有 UI 的區域讓指標穿透到 canvas。同一畫面只由一邊負責，不在 DOM 與 Unity UI 各畫一份。
- **鍵盤焦點**：Unity WebGL 預設攔截全部鍵盤輸入（common `WebInput` 的 `captureAllKeyboardInput` 預設也是 true）。Web UI 的輸入框取得焦點時，Unity 不得攔截。中文輸入法由 DOM 原生處理；改用 DOM 的畫面不再需要第三方 `WebGLInput` 的隱藏輸入框。
- **橋接**：C# 呼叫 JS 用 jslib；JS 通知 C# 用 `SendMessage`（字串、指定 GameObject）。訊息一律 JSON 並帶版本欄位，沿用 Dandelion message 2.0 的信封形式，不各功能自訂格式。
- **打包**：Web 庫建置產物隨 WebGL 模板一起輸出，延伸既有 `IndexHtmlEditor` 處理。
- **保存**：同一設定只由一邊保存：Web 庫 `ConfigStore`（localStorage）或 Unity PlayerPrefs（WebGL 為 IndexedDB），不並存兩份真值。
- **歸屬**：橋接與模板工具放在計畫中的 `Packages/Web`（承接 common 的 `WebSupport`），不放進 Yu5h1Lib.UIToolkit。

同樣依[啟動條件](#啟動條件)：VCP 的 WebGL 版確定要用某個 Web UI 時才實作。PC、Android 版沿用既有 uGUI。

### 運動共用（Motion）

物理互動由 Motion 的單一 C++ 核心共用，能力範圍由 Motion 的 [盤點計畫](file:///C:/Users/Yu5h1/Dev/VSProjects/Yu5h1Lib/Motion/Documentation/InventoryPlan.md) 決定。Web 透過 Wasm 使用 Throw2D 與 Scatter2D；兩者的數值基準是從 Unity 既有 C#（`Reaction.Throw`、`GroupDragLayout`）擷取的 fixtures。Unity 改用原生核心（DLL）由 Motion 依產品需求另行決定；在那之前 Unity 沿用既有 common／animation 實作，Web 不另造物理引擎。

AppsMenu 是 Motion 的驗證場景，甩飛由 Motion 的 [Throw 驗證計畫](file:///C:/Users/Yu5h1/Dev/VSProjects/Yu5h1Lib/Motion/Documentation/ThrowValidationPlan.md)（Task ID: physics-throw-validation）擁有，吸塵器與釋放由 Motion 的 Scatter2D 契約擁有。入場動畫是時間驅動（`Transition.Opening`），屬 Motion 的插值表現：以規格與 fixtures 共用，Web 對齊 `@yu5h1/web/animation`，不經 C++ 核心。

Unity C# namespace 不得使用 `Yu5h1Lib.Physics` 或 `Yu5h1Lib.Motion`，避免遮蔽 `UnityEngine.Physics`、`UnityEngine.Motion`；資料夾與 repo 名稱不受此限。

## 決策理由與替代方案

- **復刻而非共用執行碼**：跨平台共用執行碼的方案經搜尋後不可行或成本過高；Unity 依 Web 規格復刻，接受兩份實作，以案例控制漂移。
- **只轉設計資料**：轉譯 Web 元件的 CSS 會因 USS 只支援 CSS 子集（沒有 grid、media query、`::before`、shadow DOM 的 `:host`／`part`）而靜默出錯；另訂中立格式同時產生兩端，會牴觸 Web 的 Lit-first 與不新增自訂格式的方向。
- **WebView 排除**：可藉外掛提供場景呈現與雙向通訊（例如 [Vuplex 官方文件](https://developer.vuplex.com/)），本輪因選擇原生 UI 與避免新增瀏覽器引擎及橋接依賴而排除；需求改變再評估。
- **Apps 做成可直接使用的模組**：模組提供預設政策、使用端可以覆寫，就能直接使用，不必每個使用端自行接線。只有機制加組合範例時，使用端仍要自己處理捕獲、速度取樣與落點提交，而這些接線寫錯時會靜默失效。
- **色票推導只由 Web 執行**；生成資產必須可追溯並能偵測過期。

## 抽離工作入口

Unity 端的抽離與復刻使用 [unity-ui-extraction](../.agents/skills/unity-ui-extraction/SKILL.md)。Web 端的抽離由 [web-integration](../../../Web/.agents/skills/web-integration/SKILL.md) 擁有。本計畫擁有跨平台方向與 P1–P5 門檻，技能不複製設計或任務狀態。

## 驗收與遷移

P1 的 Web 部分已交付：WEB-1 契約；WEB-3 的非物理模組 1.0.0，以及接入 Motion 的甩飛與吸塵器（設計資料 2.0.0）。共用案例仍是 1.0.0，只涵蓋非物理行為；物理行為目前只有 Web 的瀏覽器測試與 Motion 的數值 fixtures。P2 起依[啟動條件](#啟動條件)啟動；未啟動前不視為進行中。Unity 端啟動與復刻細節的待決項見 [決策檔](../decisions/cross-platform-ui-parity.decisions)。

| 階段 | 完成條件 |
|---|---|
| P1 來源與契約 | WEB-1 交付；WEB-3 的 Apps 模組規格（非物理行為、政策覆寫點、參數、案例）在 Web 定案；行為分類完成，物理項目標出；三種 theme 的分工與切換擁有者已列入盤點，切換擁有者未決時明確標示 |
| P2 轉換 | 色票與 Apps 參數轉成 USS；light／dark 換算成 RGBA 後一致；透明色實測；重新轉換無差異；來源變更可偵測；失敗不覆寫有效產物；產物以 WebTheme 命名，經 `PanelSettings.themeStyleSheet` 切換 light／dark 實測 |
| P3 行為驗證 | 兩端執行同一版非物理案例並記錄結果；先驗證滑鼠操作，觸控與明暗呈現等目標環境確定後再驗，未驗項不記通過 |
| P4 重用驗證 | 同一個 demo 的小變體只換內容、資料與政策，不改模組、不複製手勢規則 |
| P5 擴大採用 | P1–P4 通過後，聊天依 WEB-2；物理項目等 Motion 定案後補驗收；設定面板等保存語意討論後再排 |

每次驗收記錄 Web 來源版本、Unity 版本、產物指紋、案例版本及差異；改色須有實際截圖與操作驗收，不能只靠編譯。

規模：先從小規模驗證開始；檔案與工時估算在 P1 的 Apps 模組規格定案後提出。驗證採資料／案例檢查加上兩端實際 UI 操作。

## 尚未決定

- **Apps 範例的去留**：Unity 復刻完成後，`Samples~/AppsMenu` 改為模組的使用範例，或退役。
- **聊天的方向**：Unity 已有聊天模組，是否同樣走「Unity 設計搬到 Web，再復刻回 Unity」。
- **設定面板的保存語意**：保存語意與 Unity Preferences 的對應，以及 Preferences 是否需要對齊 Yu5h1Lib.Web，尚未討論。
- **uGUI 主題切換機制復盤**：檢視 common `Theme` + `ThemeOptionSet` 的切換時機、Preferences 保存選擇的方式與 `_schema` 繼承是否足夠，並決定 uGUI 與 UI Toolkit 共用的觸發與保存擁有者。
- **uGUI 色票來源**：common `Theme` SO 的顏色繼續手填，或由轉換工具同樣從 Web `theme.css` 產生；未決前 uGUI 顏色不保證與 Web 一致。
- **執行期自訂配色**：本輪只驗證預先產生的 light／dark；是否需要動態配色及其 Unity 機制，留到有消費需求時決定。
