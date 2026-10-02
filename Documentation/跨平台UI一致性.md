# 跨平台 UI 一致性

> Task ID: cross-platform-ui-parity

> 狀態：方向已定案，尚未開始（2026-10-02）。先以設定面板一項試點驗證整條流程，見[試點](#試點)。
>
> 與其他文件的關係：Web 庫要做的事寫在 [Requirements.Web.md](Requirements.Web.md)，由 Web 庫實作；
> 各元件的 Web 端設計由 Web 庫自己的文件擁有。聊天面板目前的設計在 [聊天面板模組](聊天面板模組.md)，
> Apps 面板在 [指標互動與世界橋接](指標互動與世界橋接.md) 與 [運動系統](運動系統.md)。物理算式的共用是
> 獨立的 Physics 專案，見 [物理共用](#物理共用)。執行狀態留在 [handoff.md](../handoff.md)。

## 系統階層與關係

```text
Yu5h1Lib
├─ Web 庫：UI 設計的唯一來源
│  ├─ 主題：resolveTheme 決定 light／dark 色票與推導規則
│  ├─ 元件：Lit 實作＋規格文件（設定面板、save bar、對話框……）
│  └─ 主題輸出：產生不綁平台的色票檔（Requirements.Web.md）
├─ UnityExtension
│  └─ com.yu5h1.uitoolkit：UI Toolkit 實作
│     ├─ 主題匯入：色票檔 → USS 變數（Editor 工具）
│     ├─ 元件：照 Web 規格實作，引用同名變數
│     └─ 驗證：Yu5h1LibTest；使用端：HealthAI 等 Unity App
└─ Physics（獨立 repo）：物理算式共用，另案
```

色票和元件規格由 Web 庫流向 UI Toolkit，不反向。UI Toolkit 這邊發現規格缺漏或做不到的地方，以需求
回報 Web 庫，不在 Unity 端自行改設計。

## 摘要

讓 Unity 的 UI 和 Web 庫的 UI 看起來、用起來一致。Web 庫的 UI 是 Lit 寫的，依賴 DOM 與完整 CSS，Unity
不經過瀏覽器引擎無法執行，所以不共用渲染程式；改成在下面幾層共用唯一來源，兩邊各自原生渲染：

| 層 | 共用方式 |
|---|---|
| 主題色票 | 由 Web 庫輸出，自動轉成 USS 變數，兩邊同值 |
| 元件結構與狀態 | UI Toolkit 元件沿用 Web 元件的名稱、狀態與事件語意 |
| 行為 | 照 Web 庫的規格文件實作，用同一組案例驗證 |
| 渲染 | 各平台自己做：Web 用 Lit＋CSS，Unity 用 UI Toolkit＋USS |

## 問題與目標

- **問題**：同一個 UI（例如設定面板）在 Web 與 Unity 各做一份，設計會各自漂移；Unity 這邊目前也沒有
  任何來自 Web 庫的設計依據。
- **目標**：設計只在 Web 庫決定一次；Unity 實作一次後，所有 Unity 專案共用。
- **目標**：主題色票改一次，兩邊同時生效，不靠手動抄值。
- **非目標**：在 Unity 執行 Lit 或模擬 DOM；用 WebView 嵌入網頁；把元件的 CSS 規則自動轉成 USS；
  本輪不支援使用者在執行期自訂配色（見[尚未決定](#尚未決定)）。

## 責任邊界

| 擁有者 | 擁有 | 不擁有 |
|---|---|---|
| Web 庫 | 色票與推導規則、元件設計與規格、色票檔格式 | Unity 的 USS 與元件實作 |
| UnityExtension | 色票檔 → USS 的匯入工具、UI Toolkit 元件、與 Web 規格的對照紀錄 | 設計決定；推導規則的第二份實作 |
| Unity 使用端 | 元件的組合、內容與產品行為 | 元件本身的外觀與共用行為 |

## 設計

### 主題轉換

Web 庫的色票不是靜態 CSS：`src/theme/index.ts` 的 `resolveTheme` 先取 8 個基本色，再推導其他色
（`--ui-hover`、`--ui-overlay`、`--ui-danger` 依亮暗模式決定，`--ui-on-accent` 依對比度在黑白之間選）。

1. **Web 庫輸出色票檔**：建置時以 Node 呼叫 Web 庫自己的 `resolveTheme`，分別輸出 light 與 dark 的完整
   變數表。格式由 Web 庫決定，需求見 [UNITYEXTENSION-WEB-1](Requirements.Web.md#unityextension-web-1--輸出不綁平台的主題色票檔)。
2. **UnityExtension 匯入**：Editor 工具讀色票檔，產生 `.uss`／`.tss`，每個變數照原名輸出
   （`--ui-accent` 等），放在 `com.yu5h1.uitoolkit` 內，檔頭標明「自動產生、勿手改」與來源版本。
3. **元件只引用變數**：UI Toolkit 元件的 USS 一律寫 `var(--ui-*)`，不寫死顏色。

推導規則只存在 Web 庫一份。Unity 端不得重寫 `resolveTheme` 的推導，否則兩邊遲早算出不同顏色。

**限制**：

- 只轉色票，不轉元件樣式。USS 是 CSS 的子集，沒有 grid、`::after`、`::part`、`@media`、
  `:focus-visible`，元件版面由 UI Toolkit 自己實作。
- Web 庫目前只有顏色是變數；圓角、間距、字級寫死在各元件的 CSS 裡，這些要共用得先在 Web 庫變成變數。
- UI Toolkit 沒有公開 API 能在執行期改 USS 變數（`VisualElement.customStyle` 唯讀，2026-10-02 於
  Unity 6000.6.0f1 反射確認），所以預先產生的 light／dark 可以直接用，執行期自訂配色不行。

### 元件對照

- 每個 UI Toolkit 元件沿用 Web 元件的名稱、狀態與事件語意。例如設定面板的 `busy`、`canApply`、
  `retrySave`、`retryApply`、`autoSave`，以及分類 Tab、save bar、恢復預設。
- 每個元件的文件寫明對應的 Web 原始檔與規格章節，並列出和 Web 不同的地方與原因（通常是 USS 做不到）。
- Unity 端發現規格沒寫清楚，回報 Web 庫補規格，不在 Unity 端自行決定。

### 試點

整套做法先用**設定面板**一項驗證，通過後才擴大到其他元件。選它的原因：Web 庫已有完整規格與 Lit 實作，
不必等 Web 庫新寫設計；它同時走過主題轉換、元件對照與行為對照三層；也不牽涉物理。

試點通過的條件，即[驗收與遷移](#驗收與遷移)第 1–3 步全部成立：

1. Web 庫交付色票檔，UnityExtension 匯入後 light／dark 的 USS 變數與 Web 庫逐一相同。
2. 設定面板的 UI Toolkit 版照 Web 規格逐條對照，差異都有原因。
3. 自動儲存、手動套用、恢復預設、保存失敗重試在兩邊得到同樣的狀態。

試點沒通過前，不啟動 [UNITYEXTENSION-WEB-2](Requirements.Web.md#unityextension-web-2--聊天面板設計)、
[UNITYEXTENSION-WEB-3](Requirements.Web.md#unityextension-web-3--apps-面板設計)，也不把既有 UI Toolkit 元件改成匯入的色票變數。
試點中發現做法本身行不通（例如色票檔格式不足、規格無法對照），先修正本計畫再繼續。

### 現有元件的去向

| 元件 | 目前的設計來源 | 去向 |
|---|---|---|
| 設定面板 | Web 庫 `SettingsPanelPlan.md`（已實作） | **試點**：第一個照 Web 規格實作的 UI Toolkit 元件；保存接 UnityExtension 的 `Preferences`（見[偏好設定](偏好設定.md)） |
| 聊天面板 | UnityExtension [聊天面板模組](聊天面板模組.md)；Web 庫沒有 | 試點通過後，設計遷到 Web 庫，需求 [UNITYEXTENSION-WEB-2](Requirements.Web.md#unityextension-web-2--聊天面板設計) |
| Apps 面板 | UnityExtension `Samples~/AppsMenu`；Web 庫沒有 | 試點通過後，設計遷到 Web 庫，需求 [UNITYEXTENSION-WEB-3](Requirements.Web.md#unityextension-web-3--apps-面板設計)；物理部分另等 Physics 試作 |
| 向量圖示 | UnityExtension [向量圖示系統](向量圖示系統.md)；Web 庫有 `svg-animated-icons` 規劃 | 兩邊都以 SVG 為資產時可共用同一批檔案；對齊方式待兩份計畫各自推進後再談 |

聊天面板與 Apps 面板都已有 Unity 實作，遷移的意思是「設計的擁有者換成 Web 庫」：Web 庫依現有 Unity
設計與 HealthAI 網頁 demo 寫出自己的規格與實作，之後兩邊以 Web 庫的規格為準。進行中的工作不因此暫停：
HealthAI 的聊天模組遷移（[聊天模組遷移計畫](file:///W:/UnityProject/HealthAI/docs/unity/聊天模組遷移計畫.md)，
需求 HEALTHAI-UNITYEXTENSION-7–14）照常進行；Web 庫的聊天規格完成後，再把差異回補到 Unity。

### 物理共用

Apps 面板的慣性、甩飛、彈簧和桌面避障是物理算式，Web 與 Unity 都需要。是否以 C++ 寫成單一核心、
分別輸出 Unity 原生程式庫與 WebAssembly，或改為共用規格各自實作，由獨立的 Physics 專案決定：
[盤點計畫](file:///C:/Users/Yu5h1/Dev/VSProjects/Yu5h1Lib/Physics/Documentation/InventoryPlan.md)第 4 步「比較共用方式」已列入此選項。
本計畫不重複該比較。

Physics 也先以最小試作驗證（其盤點計畫第 5 步「提出最小試作」）。Apps 面板的物理部分等 Physics 試作通過、共用方式定案後才接；在那之前，Unity 端沿用 `com.yu5h1.common`／`com.yu5h1.animation` 現有的物理實作，WEB-3 的規格只描述行為與參數。

Unity 端的命名限制：C# namespace 不得取 `Yu5h1Lib.Physics`，它會在整個 library 遮蔽 `UnityEngine.Physics`
（見 [UnityExtension 入口文件](../.agents/introduction.md)的 house conventions）。資料夾或 repo 名稱不受影響。

## 案例

- **設定面板**：Web 庫已有完整規格與 Lit 實作。UI Toolkit 版照同一份規格做，色票來自匯入的 USS，
  保存接 `Preferences`。兩邊在同樣的操作下（自動儲存、手動套用、保存失敗重試）得到同樣的狀態。
- **聊天面板**：Unity 先有實作，Web 庫後補規格。驗證這套做法在「Unity 先做」的情況下也能把設計擁有權
  轉回 Web 庫，不需要重做 Unity 實作。

## 決策理由與替代方案

- **不共用渲染程式**：Unity 端執行 JS 的方案（OneJS、ReactUnity）都不模擬 DOM，Lit 無法執行；自己在
  UI Toolkit 上模擬 DOM 與 CSS，工程大且只能接近、不能一致。
- **已排除：WebView 嵌入**。目前需求不是瀏覽網頁，而是 Unity 原生顯示一致的 UI。嵌入網頁會多一個
  瀏覽器引擎，且主介面需要和 Unity 場景互動（遮擋、觸控、定位），網頁做不到。
- **已排除：Unity 端重寫色票推導規則**。兩份推導遲早不一致；改由 Web 庫輸出已算好的值。
- **已排除：解析 Web 庫的 CSS 產生 USS**。色票是程式算出來的，不在 CSS 檔裡；元件 CSS 又有 USS 不支援的語法。

## 驗收與遷移

第 1–3 步是[試點](#試點)，全部通過才進入第 4 步。

1. Web 庫交付色票檔（UNITYEXTENSION-WEB-1）。
2. UnityExtension 完成匯入工具：light 與 dark 產生的 USS 變數逐一等於色票檔的值；重新匯入不產生差異；
   在 Yu5h1LibTest 用一個測試面板切換 light／dark，顏色與 Web 庫 demo 的同一主題相符。
3. 設定面板的 UI Toolkit 版：照 Web 規格逐條對照，列出差異與原因；保存接 `Preferences`。
4. 試點通過後，Web 庫依 WEB-2、WEB-3 完成聊天面板與 Apps 面板的設計，Unity 端依 Web 規格回補差異。
   Apps 面板的物理部分另等 Physics 試作通過。

既有 UI Toolkit 元件（聊天模組、BottomSheet、Apps Menu 範例）改用匯入的色票變數時，視覺變化需在
Yu5h1LibTest 截圖確認，不靠編譯通過宣稱一致。

## 尚未決定

- **執行期自訂配色**：Web 庫的 `custom` 模式讓使用者執行時挑色，UI Toolkit 無法在執行期改 USS 變數。
  要支援的話需另找機制（例如執行期重新產生並套用 StyleSheet，或改由 C# 直接寫顏色），是否需要、何時需要待定。
- **圓角、間距、字級是否變成 Web 庫的變數**：決定兩邊能共用到多細；這是 Web 庫的設計決定，
  UnityExtension 只能提需求。
