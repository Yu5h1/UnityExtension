# 跨平台 UI 一致性

> Task ID: cross-platform-ui-parity
>
> 狀態：進行中。執行進度由 [handoff.md](../handoff.md) 擁有；Unity 端的選擇由 [決策檔](../decisions/cross-platform-ui-parity.decisions) 擁有，本文以決策 ID 引用。
> 體驗規範（操作與視覺）由 Yu5h1Lib.UX 擁有（例如 [AppsMenu](../../../UX/Documentation/ModuleSpec/AppsMenu.md)）；Web 是實作端。Unity 對 Web 的需求在 [Requirements.Web.md](Requirements.Web.md)，對 Motion 的需求在 [Requirements.Motion.md](Requirements.Motion.md)，對 Core 的需求在 [Requirements.DotNet.md](Requirements.DotNet.md)；Web 對 Unity 的需求在 Web 的 [Requirements.UnityExtension.md](../../../Web/Documentation/Requirements.UnityExtension.md)。

## 系統階層與關係

```text
Yu5h1Lib
├─ 規範來源（唯一）
│  ├─ Yu5h1Lib.UX：體驗規範（操作與視覺，ModuleSpec，如 AppsMenu 的 APPS-*）、設計資料、案例
│  ├─ Yu5h1Lib.Web：Theme 色票與設定格式（尚未移交 UX）；AppsMenu 等模組的 Web 實作
│  └─ Motion：動態規範（ModuleSpec）——補間殼、插值公式、物理互動（Throw、Attraction、Scatter）
│        │ 鎖定版本後復刻、驗收
│        ▼
├─ DotNet Core（C#，不依賴任何引擎）：依規範建立框架與介面
│  ├─ 動態合約：補間殼介面、物理互動的單一入口
│  └─ 操作模型：AppsMenu 的狀態、命令、行為規則與手勢判斷
├─ UnityExtension 共用層：Core 介面的 Unity 實作
│  ├─ Packages/Motion（Motion 交付的原生接入）、animation（C# 實作）、DOTween／PrimeTween 適配
│  └─ Unity 型別轉換與啟動註冊
├─ UI 系統適配（各自復刻畫面，元件不共用）
│  ├─ Yu5h1Lib.UIToolkit：Editor 匯入工具、Runtime/WebUI/<模組>
│  └─ uGUI（Packages/UI）：有產品需要時再做（決策 `ugui-appsmenu-now`）
└─ 使用端：選擇引擎與實作、內容、資料，以及覆寫的政策
```

分層依決策 `layering-doc-owner`、`core-contracts`、`ui-model-location`。各層對來源規格的驗收紀錄放在實作所在的套件（見[引用模式](#引用模式)）。

| 名稱 | 位置 | 角色 |
|---|---|---|
| Yu5h1Lib.UX | `Yu5h1Lib/UX` | 體驗規範（操作與視覺）的唯一來源，不寫呈現技術 |
| Yu5h1Lib.Web | `Yu5h1Lib/Web` | 實作端；Theme 色票契約與設定格式暫由 Web 擁有 |
| Motion | `Yu5h1Lib/Motion` | 動態規範的唯一來源；C++ 物理核心與各平台交付 |
| DotNet Core | `Yu5h1Lib/DotNet/Source` | 依 UX、Motion 規範建立 C# 介面與操作模型；Unity 經 `Packages/Core` 的 Core DLL 使用 |
| Yu5h1Lib.UIToolkit | `UnityExtension/Packages/UIToolkit` | 擴充 UI Toolkit：依 UX 規範實作 UI，匯入工具也在這裡 |
| Yu5h1Lib.Web package | `UnityExtension/Packages/Web` | Unity 端的 WebGL 平台能力（[遷移計畫](Web套件遷移.md)）；不屬於 UI 復刻 |

## 引用模式

Unity 引用的是規範與版本化資料，不是任何平台的程式：模組的體驗規範、設計資料與案例來自 UX；Theme 色票與設定格式目前仍是 Web 的交付（決策 `reference-model`）。流程由共用 skill [referenced-module-acceptance](../../../.agents/skills/referenced-module-acceptance/SKILL.md) 擁有（唯一的引用 skill，UX 第 82 題）；Unity 端的機制見 [.agents/introduction.md〈Adopting a UX module in a package〉](../.agents/introduction.md#adopting-a-ux-module-in-a-package)：

1. **釘選**：模組規範、設計資料與案例以套件內快照（`Packages/<套件>/Tests/Editor/UX~/<模組>/`，位元組相同、`-text`）釘住 UX commit，由 `<模組>DesignTests` 核對雜湊與數值；Theme 與設定仍用 Editor 匯入器鎖定 Web 交付（產生 USS 變數與 WebTheme，過期可偵測，失敗不覆寫既有產物）。
2. **實作**：原生優先（UX 第 64、80 題）：先用 UI Toolkit／UGUI 原生元件與樣式表，只為原生表達不了的規則寫程式；需要自寫模型時，模型命令與規範同形，讓案例可以直接執行。
3. **驗證**：依共用技能 [referenced-module-acceptance](../../../.agents/skills/referenced-module-acceptance/SKILL.md)，在實作套件建立驗收紀錄 `Packages/<套件>/Documentation/Yu5h1Lib.UX/<模組>.md`，逐項對應規範的行為 ID，在 Unity 執行案例與平台檢查；Web 等其他實作端的結果不能代替 Unity 的驗證。
4. **回報**：驗收紀錄記下採用的規範版本、逐項結果與平台差異，供提出方（UX-UNITYEXTENSION-1、WEB-UNITYEXTENSION-2）查核；需求狀態由提出方更新。

規則：

- **單一規範來源**：體驗只以 UX 規範為準。Unity 不另存行為描述，也不以舊 Unity 範例或 Web 實作補足規範沒寫的行為（決策 `physics-cases`、`behavior-source`）。
- **規範缺口先補 UX**：Unity 需要的行為或資料規範沒有寫時，在 `Requirements.UX.md` 提出，由 UX 寫進規範後再實作。舊範例與規範的差異本身不構成對 UX 的要求。Theme 與設定格式的缺口仍向 Web 提出（[Requirements.Web.md](Requirements.Web.md)）。
- **只轉設計資料**：數值由工具轉換；結構與互動手寫。理由見[決策理由](#決策理由)。

## 模組狀態

| 模組 | 規範來源 | Unity 狀態與下一步 |
|---|---|---|
| Button | [UX Controls/Button.md](../../../UX/Documentation/ModuleSpec/Controls/Button.md)（BTN-*） | Unity 第一個引用的模組（使用者 2026-10-17）：UGUI（[Yu5h1Lib.UI 驗收紀錄](../Packages/UI/Documentation/Yu5h1Lib.UX/Button.md)）與 UI Toolkit（[Yu5h1Lib.UIToolkit 驗收紀錄](../Packages/UIToolkit/Documentation/Yu5h1Lib.UX/Button.md)）各自引用、各有驗收紀錄；進度見 [handoff](../handoff.md) |
| AppsMenu | [UX ModuleSpec/AppsMenu.md](../../../UX/Documentation/ModuleSpec/AppsMenu.md)（APPS-*） | 暫停（UX AppsMenu 目標 1：Web 已通過，Unity 待恢復）。實作對照、採用版本與驗收結果見 [AppsMenu 驗收紀錄](../Packages/UIToolkit/Documentation/Yu5h1Lib.UX/AppsMenu.md) |
| 設定（config） | 各模組以 `.json` 宣告負責的設定項目，同一份 json 是 Web 設定模組的規格，設定面板依 json 加入分頁（決策 `demo-settings`）。Web 宣告格式 1.0.0（`d916b6c`） | WEB-7 與 MOTION-2 已交付，Unity 未驗收。Unity 端與 Preferences 的對應待討論 |
| Theme | 色票契約 1.0.0（`theme.css`）；Theme 存檔格式 v1（WEB-6，`d916b6c`） | WEB-6 已交付，Unity 未驗收；依決策 `phase-order` 排在設定之後 |
| 聊天 | [WEB-2](Requirements.Web.md#unityextension-web-2--聊天面板設計) | 未開始；方向見[尚未決定](#尚未決定) |

## Unity 端約定

- 復刻元件放在 `Packages/UIToolkit/Runtime/WebUI/<模組>/`，型別不加 Web 前綴，命名空間 `Yu5h1Lib.UIToolkit`。
- WebTheme 以 `PanelSettings.themeStyleSheet` 套用，切換明暗就是替換 TSS；UI Toolkit 不另寫主題切換器，也不在 `Yu5h1Lib` 新增名為 `Theme` 的型別。
- Unity C# 命名空間不得使用 `Yu5h1Lib.Physics` 或 `Yu5h1Lib.Motion`，避免遮蔽 `UnityEngine.Physics`、`UnityEngine.Motion`。

## Motion

補間殼、插值公式與物理互動的設計規範都由 Motion 擁有；DotNet Core 依規範以 C# 建立介面，Unity 經共用層實作（[MOTION-3、4](Requirements.Motion.md#unityextension-motion-3--補間殼成為-motion-的模組規格)、[DOTNET-1](Requirements.DotNet.md#unityextension-dotnet-1--依-motion-規格在-core-建立動態合約)）。物理互動目前經 `Packages/Motion` 原生接入，計畫見 Motion 的 [Unity 原生核心接入計畫](../../../Motion/Documentation/UnityIntegrationPlan.md)；平台先做 Windows 與 Android（決策 `physics-scope`）。操作規則（捕獲、釋放條件等）屬於 UX 規範，Motion 只提供數值服務，各使用端自行驗收；AppsMenu 使用的 Throw、Scatter、Attraction 的 Unity 端驗收紀錄在 [Packages/UIToolkit/Documentation/Motion](../Packages/UIToolkit/Documentation/Motion/Throw.md)。

## WebGL：直接使用 Web 庫

WebGL 版跑在瀏覽器頁面裡，Web 庫的元件在那裡是原生的：不復刻、不轉換，Unity 只以 jslib 補互動與通知。第一個實例是 [Virtual-Chat-Partner](file:///W:/UnityProject/Virtual-Chat-Partner) 的 WebGL 版；PC、Android 版沿用既有 uGUI。

- **疊層**：Web UI 以 DOM 疊在 Unity canvas 上方；沒有 UI 的區域讓指標穿透到 canvas。同一畫面只由一邊負責。
- **鍵盤焦點**：Web UI 的輸入框取得焦點時，Unity 不得攔截鍵盤（common `WebInput` 的 `captureAllKeyboardInput` 預設為 true）。中文輸入由 DOM 原生處理。
- **橋接**：C# 呼叫 JS 用 jslib；JS 通知 C# 用 `SendMessage`。訊息一律 JSON 並帶版本欄位，沿用 Dandelion message 2.0 的信封形式。
- **打包**：Web 庫建置產物隨 WebGL 模板一起輸出，延伸既有 `IndexHtmlEditor`。
- **保存**：同一設定只由一邊保存，不並存兩份真值。
- **歸屬**：橋接與模板工具放在 `Packages/Web`，不放進 Yu5h1Lib.UIToolkit。

VCP 的 WebGL 版確定要用某個 Web UI 時才實作。

## 決策理由

- **復刻而非共用執行碼**：跨平台共用執行碼的方案經搜尋後不可行或成本過高；接受兩份實作，以規格條目與案例控制漂移。
- **只轉設計資料**：轉譯 Web 元件的 CSS 會因 USS 只支援 CSS 子集（沒有 grid、media query、`::before`、shadow DOM）而靜默出錯；另訂中立格式同時產生兩端，會牴觸 Web 的 Lit-first 與不新增自訂格式的方向。UI Toolkit 以 flexbox 排版，結構手寫成本低。
- **WebView 排除**：可藉外掛做到（例如 [Vuplex](https://developer.vuplex.com/)），因選擇原生 UI、避免新增瀏覽器引擎與橋接依賴而排除。
- **模組直接可用、政策可覆寫**：只給機制時，每個使用端都要自己寫捕獲、速度取樣與落點提交，寫錯會靜默失效。模組不得寫死產品政策或出現產品名稱分支。

## 尚未決定

- **聊天的方向**：Unity 已有聊天模組，是否同樣走「Unity 設計搬到 Web，再復刻回 Unity」。
- **uGUI 的主題**：common `Theme` 的切換、保存與色票來源是否跟 WebTheme 共用擁有者；Theme 恢復時一起決定。
- **執行期自訂配色**：本輪只考慮預先產生的 light／dark。
- **補間殼的名稱**：`Transition` 可能不對或已過時，由 Motion 在 MOTION-3 決定。
