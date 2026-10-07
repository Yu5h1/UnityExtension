# Requirements — Motion

- **目標專案（Target）**：Motion，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Motion`。
- **提出專案（Requester）**：UnityExtension，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension`。
- **目前依賴方式**：`Packages/Motion` 已交付，AppsMenu 尚未接入。Unity 使用自己的 C#（`Reaction.Throw`、`GroupDragLayout`），也是 Motion 數值基準的來源。
- **未結案需求數**：4。
- **提出日期**：2026-10-07；同日依決策 `layering-doc-owner`、`core-contracts` 新增 MOTION-3、4。
- **背景**：Yu5h1Lib.UIToolkit 的 AppsMenu 復刻，甩飛與吸塵器的數值計算使用 Motion 原生核心（[跨平台 UI 一致性](跨平台UI一致性.md) 決策 `physics-scope`）。Motion 已有 [Unity 原生核心接入計畫](../../../Motion/Documentation/UnityIntegrationPlan.md)，本檔只提出 Unity 這邊的使用需求，不重述該計畫。
- **驗收責任**：Motion 提供產物與證據；UnityExtension 在 AppsMenu 接入後確認並更新本檔狀態。

## UNITYEXTENSION-MOTION-1 — AppsMenu 使用 Motion 原生核心（Windows、Android）

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-07 提出）。Motion 已交付（Motion `bb0dcca`，`Packages/Motion` 進 UnityExtension `8977d46`），Unity 尚未驗收；逐項結果見 [Throw](../Packages/UIToolkit/Documentation/Motion/Throw.md)、[Scatter](../Packages/UIToolkit/Documentation/Motion/Scatter.md)、[Attraction](../Packages/UIToolkit/Documentation/Motion/Attraction.md) 驗收紀錄 |
| **Requirement** | 依 Motion 的接入計畫交付 `Packages/Motion`（Throw2D、Scatter2D），平台涵蓋 Windows x64（Editor 與 Player）及 Android arm64-v8a。 |
| **Evidence** | Motion 計畫的第一個切片只有 Windows x64，Android 列為依需求追加；現在的具名使用端是 Yu5h1Lib.UIToolkit 的 AppsMenu，平台為 Windows 與 Android。Motion 環境清單沒有 Android NDK。 |
| **Acceptance** | 一、Windows 依 Motion 計畫第 1–3 階段驗收。二、Android 由同一份來源建置 `.so`，插件設定只啟用已驗證的平台；IL2CPP 建置可載入並通過同一份 fixtures，manifest 記錄平台、架構與雜湊；實機驗證可由使用者執行，未驗部分列明。三、驗證用 Unity 專案為 Yu5h1LibTest，使用端是 AppsMenu 復刻的 demo（`Samples~/AppsMenuModule`）。四、Motion 只驗收數值服務；捕獲、釋放條件等操作規則由各使用端自行驗收（Web、Unity 各自），Motion 計畫第 4 階段不以「全部收納後釋放」為準。五、C# 命名空間不使用 `Yu5h1Lib.Physics`、`Yu5h1Lib.Motion`。 |
| **Impact** | Unity AppsMenu 的甩飛與吸塵器；之後其他 Unity 使用端。 |
| **Workaround** | Unity 的 AppsMenu 復刻等本需求交付後開始；既有 `Samples~/AppsMenu` 繼續使用 C# 實作。 |

## UNITYEXTENSION-MOTION-2 — 以 json 宣告 Motion 的設定項目

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-07 提出）。Motion 已交付（Motion `bb0dcca`，`Packages/Motion` 進 UnityExtension `8977d46`），Unity 尚未驗收（套件內 `Configuration/motion.settings.json`） |
| **Requirement** | Motion 以一份 `.json` 宣告自己負責的設定項目（例如 Throw 的 edgeBounce、drag、restingSpeed、lifetime），格式依 Web 的設定模式（UnityExtension 的 [WEB-7](Requirements.Web.md#unityextension-web-7--以-json-宣告的設定模式)）。 |
| **Evidence** | 跨平台 UI 一致性決策 `demo-settings`：Motion 庫與 Web 庫的設定遵循同一套 json 模式，json 同時是 Web 設定模組的規格；決策 `motion-settings-json` 決定現在就提出。Web AppsMenu demo 的 Motion 設定分類目前從 `motion.defaults` 取預設，沒有宣告檔。 |
| **Acceptance** | Motion 交付設定 json（項目、型別、預設、範圍、版本），預設值與核心一致並隨版本化交付；格式隨 WEB-7 定案，WEB-7 定案前可先列出項目與預設，格式變更時同步更新。 |
| **Impact** | Web 設定面板的 Motion 分頁；Unity 的設定復刻。 |
| **Workaround** | Web demo 繼續從 `motion.defaults` 取預設；Unity 參數暫放 Inspector。 |

## UNITYEXTENSION-MOTION-3 — 補間殼成為 Motion 的模組規格

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-07 提出） |
| **Requirement** | Motion 擁有補間殼（有起點、終點、時長的補間，可替換 DOTween、PrimeTween 或自製引擎）的唯一設計規範，以 cross-platform-module-spec 寫成 `Documentation/ModuleSpec/` 下的模組規格。 |
| **Evidence** | 決策 `layering-doc-owner`：補間殼、插值公式、物理互動三者都以 Motion 為唯一設計規範來源，DotNet Core 依規格以 C# 建立框架與介面（使用端不只 Unity，還有 WPF、WinForms、Web）；`Motion_System_Refactor` 應優先收斂進 Motion 成為一項服務。現有設計只在 UnityExtension 的 [Motion_System_Refactor](../.agents/plans/Motion_System_Refactor.md)（§1–§7 殼與 provider 差異、§11 待確認），尚未實作，且仍用已作廢的 `Yu5h1Lib.Animation.Motion` 命名。 |
| **Acceptance** | 一、模組規格含行為 ID 與案例：控制動詞、設定、引擎注入、回收語意，以及 DOTween 與 PrimeTween 兩種模型都能滿足的條件。二、名稱由 Motion 決定；使用者指出 `Transition` 可能不對或已過時。三、Motion 收錄後，UnityExtension 刪除 `Motion_System_Refactor` 裡被取代的規範內容，只留 Unity 端的引擎適配與既有組件遷移（§8–§9）。 |
| **Impact** | DotNet Core 的 C# 介面（[DOTNET-1](Requirements.DotNet.md#unityextension-dotnet-1--依-motion-規格在-core-建立動態合約)）；Unity 的 DOTween／PrimeTween 適配與 11 個既有組件；之後的 WPF、WinForms、Web 使用端。 |
| **Workaround** | Unity 繼續用既有 DOTween 組件；殼尚未實作。 |

## UNITYEXTENSION-MOTION-4 — 插值公式的規格

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-07 提出） |
| **Requirement** | Motion 擁有插值公式（緩動曲線、排程、入場的飛入、縮放與收攏）的規格與 fixtures，各平台依規格以原生方式實作。 |
| **Evidence** | 決策 `layering-doc-owner`：公式與方程是 Motion 的責任，C# 依規格復刻。Motion [plan](../../../Motion/plan.md#定位) 已把插值列為「共用規格與 fixtures、各平台原生實作」，但 `ModuleSpec` 目前只有 Throw、Scatter、Attraction。AppsMenu 入場公式現在寫在 UX 的 [AppsMenu 規範〈入場規範〉](../../../UX/Documentation/ModuleSpec/AppsMenu.md#入場規範)；Unity 的實作是 `com.yu5h1.animation` 的 `Transition.Entrance`、`Opening`。 |
| **Acceptance** | 一、模組規格含行為 ID 與 fixtures，至少涵蓋 AppsMenu 入場用到的公式。二、UX 規範、Web 與 Unity 都能引用同一份公式規格；入場公式從 UX AppsMenu 規範移到 Motion 的協調由 Motion 與 UX 處理。 |
| **Impact** | Unity `Transition`；Web `@yu5h1/web/animation` 與 AppsMenu 入場；補間殼的緩動（MOTION-3）。 |
| **Workaround** | Unity 沿用既有 `Transition`；AppsMenu 入場以 Web 規格驗收。 |
