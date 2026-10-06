# 跨平台 UI 一致性

> Task ID: cross-platform-ui-parity
>
> 狀態：進行中。執行進度由 [handoff.md](../handoff.md) 擁有；Unity 端的選擇由 [決策檔](../decisions/cross-platform-ui-parity.decisions) 擁有，本文以決策 ID 引用。
> 行為規格由 Web 擁有（例如 [AppsMenu](../../../Web/Documentation/AppsMenu.md)）。Unity 對 Web 的需求在 [Requirements.Web.md](Requirements.Web.md)，對 Motion 的需求在 [Requirements.Motion.md](Requirements.Motion.md)；Web 對 Unity 的需求在 Web 的 [Requirements.UnityExtension.md](../../../Web/Documentation/Requirements.UnityExtension.md)。

## 系統階層與關係

```text
Yu5h1Lib
├─ Yu5h1Lib.Web：UI 行為規格與實作的唯一來源
│  ├─ 模組規格（行為條目，如 AppsMenu U1–U8）
│  └─ 版本化交付：theme.css、dist/contracts 的參數與案例、delivery manifest
│        │ 鎖定 Web commit
│        ▼
├─ UnityExtension／Yu5h1Lib.UIToolkit：復刻 Web 模組，範圍只限 UI 互動
│  ├─ Editor 匯入工具：驗證交付 → 產生 USS 變數／WebTheme、複製案例、記錄已匯入版本
│  └─ Runtime/WebUI/<模組>：依規格手寫的 UXML 與 C#
├─ Motion：數值核心（C++）；Unity 經 Packages/Motion 使用
└─ 使用端：內容、資料，以及覆寫的政策
```

| 名稱 | 位置 | 角色 |
|---|---|---|
| Yu5h1Lib.Web | `Yu5h1Lib/Web` | UI 行為規格與實作的唯一來源 |
| Yu5h1Lib.UIToolkit | `UnityExtension/Packages/UIToolkit` | 擴充 UI Toolkit：復刻 Web 的 UI 互動，匯入工具也在這裡 |
| Yu5h1Lib.Web package | `UnityExtension/Packages/Web` | Unity 端的 WebGL 平台能力（[遷移計畫](Web套件遷移.md)）；不屬於 UI 復刻 |

## 引用模式

Unity 無法執行 Web 程式碼，因此不引用程式，而是引用 Web 的**版本化交付**，在 Unity 端復刻並重新驗證（決策 `reference-model`）。流程由 [web-replication](../.agents/skills/web-replication/SKILL.md) 擁有：

1. **匯入**：鎖定一個 Web commit，驗證 manifest 的版本與雜湊，產生 USS 變數（及之後的 WebTheme）、複製案例，記錄已匯入版本；過期可偵測，失敗不覆寫既有產物。
2. **復刻**：依 Web 規格手寫 UXML 與 C#；模型命令與 Web 同形，讓案例可以直接執行。
3. **驗證**：在 Unity 執行 Web 的案例，並對照規格逐條驗證；Web 的測試結果不能代替 Unity 的驗證。
4. **回報**：逐條結果、採用的 Web commit 與版本、平台差異，回報到 Web 對 Unity 的需求（例如 WEB-UNITYEXTENSION-2）。

規則：

- **單一規範來源**：行為只以 Web 規格為準，逐條寫清楚。Unity 不另存行為描述，也不以舊 Unity 範例補足 Web 沒寫的行為（決策 `physics-cases`、`behavior-source`）。
- **規格缺口先補 Web**：Unity 需要的行為或資料 Web 沒有規格時，先在 [Requirements.Web.md](Requirements.Web.md) 提出，由 Web 寫進規格後再復刻（例如入場，決策 `entrance`）。舊範例與 Web 的差異本身不構成對 Web 的要求。
- **只轉設計資料**：數值由工具轉換；結構與互動手寫。理由見[決策理由](#決策理由)。

## 模組狀態

| 模組 | Web 來源 | Unity 狀態與下一步 |
|---|---|---|
| AppsMenu | [AppsMenu.md](../../../Web/Documentation/AppsMenu.md) U1–U8；設計資料 2.0.0、案例 1.0.0 | 等 Web 與 Motion 先依需求準備好再開始（決策 `appsmenu-start`）：Web 的 WEB-5～7，Motion 的 MOTION-1～2。物理（甩飛、吸塵器）經 Motion 原生核心（[UNITYEXTENSION-MOTION-1](Requirements.Motion.md#unityextension-motion-1--appsmenu-使用-motion-原生核心windowsandroid)）；入場沿用 `Transition.Opening`，等 Web 補規範（[WEB-5](Requirements.Web.md#unityextension-web-5--appsmenu-入場的設計規範)） |
| 設定（config） | 方向：各模組以 `.json` 宣告負責的設定項目，同一份 json 是 Web 設定模組的規格，設定面板依 json 加入分頁（決策 `demo-settings`）。Web 尚未定案 | 暫停，等 [WEB-7](Requirements.Web.md#unityextension-web-7--以-json-宣告的設定模式) 與 [MOTION-2](Requirements.Motion.md#unityextension-motion-2--以-json-宣告-motion-的設定項目)。Unity 端與 Preferences 的對應屆時討論 |
| Theme | 色票契約 1.0.0（`theme.css`） | 暫停，等 [WEB-6](Requirements.Web.md#unityextension-web-6--theme-與-config-的互動及存檔格式) 釐清 theme 與 config 的互動及存檔格式（決策 `theme-split`） |
| 聊天 | [WEB-2](Requirements.Web.md#unityextension-web-2--聊天面板設計) | 未開始；方向見[尚未決定](#尚未決定) |

AppsMenu 的復刻細節（位置、型別、參數、案例、demo、鍵盤、顏色）見決策檔 `module-shape`、`design-params`、`cases-source`、`demo-location`、`keyboard`、`colors`。

## Unity 端約定

- 復刻元件放在 `Packages/UIToolkit/Runtime/WebUI/<模組>/`，型別不加 Web 前綴，命名空間 `Yu5h1Lib.UIToolkit`。
- WebTheme 以 `PanelSettings.themeStyleSheet` 套用，切換明暗就是替換 TSS；UI Toolkit 不另寫主題切換器，也不在 `Yu5h1Lib` 新增名為 `Theme` 的型別。
- Unity C# 命名空間不得使用 `Yu5h1Lib.Physics` 或 `Yu5h1Lib.Motion`，避免遮蔽 `UnityEngine.Physics`、`UnityEngine.Motion`。

## Motion

數值核心由 Motion 擁有，Unity 經 `Packages/Motion` 使用，計畫見 Motion 的 [Unity 原生核心接入計畫](../../../Motion/Documentation/UnityIntegrationPlan.md)；平台先做 Windows 與 Android（決策 `physics-scope`）。操作規則（捕獲、釋放條件等）屬於 Web 規格，Motion 只提供數值服務，各使用端自行驗收。原生核心可用前，既有 `Samples~/AppsMenu` 保留 C# 實作。

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
- **`Samples~/AppsMenu` 的去留**：Unity 復刻完成後改為模組範例，或退役。
- **uGUI 的主題**：common `Theme` 的切換、保存與色票來源是否跟 WebTheme 共用擁有者；Theme 恢復時一起決定。
- **執行期自訂配色**：本輪只考慮預先產生的 light／dark。
