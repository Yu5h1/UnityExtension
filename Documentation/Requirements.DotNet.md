# Requirements — DotNet

- **目標專案（Target）**：Yu5h1Lib .NET Core，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\DotNet`（`DotNet/Source`）。
- **提出專案（Requester）**：UnityExtension，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension`。
- **目前依賴方式**：`Unity/Core/Runtime/Yu5h1Lib.Runtime.csproj` 以連結方式編入 `DotNet/Source` 的部分資料夾（例如 `Adapter`、`UX`、`MVVM`），建置成 `Packages/Core` 的 Core DLL。
- **未結案需求數**：2。
- **提出日期**：2026-10-07。
- **背景**：Yu5h1Lib 的動態與互動規範由來源庫擁有：補間殼、插值公式、物理互動的設計規範在 Motion（[Requirements.Motion.md](Requirements.Motion.md) MOTION-3、4 與既有 ModuleSpec），AppsMenu 的操作規範在 Web。Core 以 C# 依規範建立框架與介面，因為使用端不只 Unity，還有 WPF、WinForms。依決策 `layering-doc-owner`、`core-contracts`、`ui-model-location`，使用者已授權在 Core 放這些合約。
- **驗收責任**：DotNet 實作並以 referenced-module-acceptance 記錄對來源規格的驗收；UnityExtension 在 Unity 端接上後確認並更新本檔狀態。

## UNITYEXTENSION-DOTNET-1 — 依 Motion 規格在 Core 建立動態合約

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-07 提出）；補間殼部分等 MOTION-3 的規格 |
| **Requirement** | 在 `DotNet/Source` 依 Motion 規格以 C# 建立介面：補間殼（控制、設定、引擎注入）與物理互動的單一入口（Throw、Scatter；Attraction 經 Scatter）。Core 是依規格復刻的一端，不另訂規範。 |
| **Evidence** | 決策 `core-contracts` 選「兩者都放 Core」，並註明 Motion 是唯一設計規範來源、Core 是復刻端。Motion 規格 [DesignPrinciples §5](../../../Motion/Documentation/DesignPrinciples.md#5-public-service-interfaces) ARCH-01 要求每種能力只有一個入口；Unity 現有的 `MotionThrow2D`、`MotionScatter2D` 是以維度命名的舊入口，在 UIToolkit 的 [Throw 驗收紀錄](../Packages/UIToolkit/Documentation/Motion/Throw.md) 記為 ARCH-01 失敗。補間殼目前只有 UnityExtension 的 [Motion_System_Refactor](../.agents/plans/Motion_System_Refactor.md) 設計，未實作。 |
| **Acceptance** | 一、介面不依賴 UnityEngine，數值型別用 Core 的型別。二、Throw、Scatter 的入口符合 ARCH-01～04；Unity 的原生接入（`Packages/Motion`）與 C# 實作都能接到同一入口。三、在 DotNet 建立對 Motion 規格的驗收紀錄。四、Unity 端在 animation 或 Motion 套件提供實作並通過既有 fixtures。 |
| **Impact** | Unity AppsMenu 的甩飛與吸塵器；Unity 的 DOTween／PrimeTween 適配與既有組件；WPF、WinForms 使用端。 |
| **Workaround** | Unity 直接呼叫 `MotionThrow2D`、`MotionScatter2D`，補間繼續用既有 DOTween 組件。 |

## UNITYEXTENSION-DOTNET-2 — 在 Core 建立 AppsMenu 的操作模型

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-07 提出） |
| **Requirement** | 在 `DotNet/Source` 依 Web 的 [AppsMenu 規格](../../../Web/Documentation/ModuleSpec/AppsMenu.md) 建立不碰畫面的操作模型：項目狀態、命令、可覆寫的行為規則，以及手勢判斷（長按、移動門檻、速度取樣）。UGUI、UI Toolkit 與其他 UI 系統只寫畫面適配。 |
| **Evidence** | 決策 `ui-model-location`：操作方式與 UX 習慣的規範放 Core，因為使用端不只 Unity。UGUI 與 UI Toolkit 的物件模型不同，畫面元件無法共用；Web 的 AppsMenu 也分成不碰 DOM 的 AppsModel 與 Lit 畫面元件。既有先例：`IValuePort` 在共用層，uGUI 與 UI Toolkit 各有 adapter。UIToolkit 的 [AppsMenu 驗收紀錄](../Packages/UIToolkit/Documentation/Yu5h1Lib.Web/AppsMenu.md) 顯示既有範例沒有模型層。 |
| **Acceptance** | 一、模型命令與 Web 同形，Web 的 11 項中性案例在 Core 的模型上執行通過，結果記在 DotNet 的驗收紀錄。二、手勢判斷以時間與座標為輸入，不依賴任何 UI 系統。三、Unity 的 UIToolkit 版能只靠畫面適配接上。 |
| **Impact** | UIToolkit 的 AppsMenu；之後的 UGUI 版（決策 `ugui-appsmenu-now`：等有產品需要再做）；WPF、WinForms。 |
| **Workaround** | AppsMenu 暫停，等本需求與 DOTNET-1。 |
