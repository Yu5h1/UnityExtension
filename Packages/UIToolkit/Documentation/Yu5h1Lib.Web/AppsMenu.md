# AppsMenu 驗收紀錄（Yu5h1Lib.UIToolkit）

> Task ID: appsmenu-library-adoption

Yu5h1Lib.UIToolkit 依 Web 的 AppsMenu 規格實作原生 AppsMenu，並以 Motion 的數值服務處理甩飛與吸塵器。本文件記錄 Unity 端的實作對應、採用的來源版本與驗收結果；行為要求以 Web 規格為準，不在此重寫。

```text
UnityExtension
└─ Yu5h1Lib.UIToolkit（實作套件）
   └─ AppsMenu（本紀錄）
      ├─ 行為規格 ← Yu5h1Lib.Web ModuleSpec/AppsMenu（APPS-*、P01–P08、11 項中性案例）
      ├─ 數值服務 ← Motion：Throw、Scatter（含 Attraction），經 Packages/Motion 原生接入
      │    └─ Unity 端數值驗收：../Motion/Throw.md、Scatter.md、Attraction.md
      └─ 驗證專案 → Yu5h1LibTest（Unity 6000.6）
```

## 身分

| 項目 | 內容 |
|---|---|
| 實作套件 | `com.yu5h1.uitoolkit`（`Packages/UIToolkit`）。目前只有既有範例 `Samples~/AppsMenu/AppsMenuDemo.cs`，尚無依規格實作的模組 |
| 來源規格 | [Web ModuleSpec/AppsMenu.md](../../../../../../Web/Documentation/ModuleSpec/AppsMenu.md)，規格 1.0.0；Web `f7abccf`，乾淨；檔案 SHA-256 `658fd825…cece`（與 `apps-delivery.json` 記錄一致） |
| 交付資料 | `Web/dist/contracts/apps-delivery.json`：設計資料 2.1.0、案例 1.0.0（schema 1，11 項）；`apps-design.json` `fad0f43b…`、`apps-cases.json` `4075be6d…`。設定見 Web [ModuleSpec/Configuration.md](../../../../../../Web/Documentation/ModuleSpec/Configuration.md) |
| Motion | Motion 規格包 `8b53e5f`（Throw、Scatter、Attraction 1.0.0）；Unity 端數值驗收見 [Motion/](../Motion/) 三份紀錄 |
| 驗收範圍 | APPS-DATA～APPS-OPENING 全部、P01–P08、11 項中性案例。平台：Windows Editor 先行；Windows Player、Android 列入但未排程 |
| 紀錄狀態 | **planned**：完成既有範例對照，尚未實作、尚未執行任何 APPS 驗收 |

Unity 端的選擇（模組位置、參數匯入、案例來源、鍵盤、入場、物理範圍）在 [決策檔](../../../../decisions/cross-platform-ui-parity.decisions)。

## 實作對照

決策 `appsmenu-approach` 要求先逐條比對既有範例與規格，再決定改既有範例或另建模組。下表是 2026-10-07 對 `AppsMenuDemo.cs` 的靜態閱讀結果，不是執行證據。

| 來源 ID | 既有範例的對應 | 共用依賴 | 對照 |
|---|---|---|---|
| APPS-DATA | 狀態散在元件欄位（`apps`、`onDesktop`、`marked`）；ID 自動產生 `app-{i}`，前 5 項在桌面 | — | **缺少**：沒有模型、快照、政策注入與輸入驗證；初始位置由 `GroupDragLayout` 散佈，不是規格的格狀公式 |
| APPS-ACTION | `Gesture.LongPress` 400ms 切換 mark；放手計數為 click | `Gesture.LongPress` | **部分**：mark 相同；沒有 activate 動作通知與 activate／mark／none 政策 |
| APPS-DRAG | 桌面圖示跟手（`Resolve`）；背景拖曳整組移動 | `GroupDragLayout.DragGroup` | **部分**：沒有取消還原；背景拖曳放手帶慣性，規格未提供群組慣性 |
| APPS-DROP | `DropTarget`：Apps 按鈕 → 已開面板 → 桌面；面板內依游標下的項目排序；拉回桌面後關閉面板 | — | **相同**（判斷順序、排序、拉回關閉）；**缺少**元件外拒絕與自訂 drop 政策 |
| APPS-INPUT | 長按 400ms、移動容許 6px；`drop-ready` 外框，手勢結束清除 | `Gesture.LongPress` | **不同**：app-0 容許量為 0（規格無此例外）；圖示一移動就拖曳，沒有 >6px 門檻；拖曳後放手仍算 click |
| APPS-PANEL | 短按 Apps 開關；背景按下關閉；面板吃掉事件，不啟動背景拖曳 | `Reaction.Spring` | **缺少**：Escape 與鍵盤操作（Enter／Space、Shift+Enter、Alt+左右）；面板開合用彈簧動畫，規格未提供 |
| APPS-VACUUM | 拖動超過 8px 才吸收；按鈕跟手；捕獲距離圖示尺寸 × 0.6 | `GroupDragLayout.Pull`（C#） | **不同**：恰好 8px 時已啟動（規格要求 >8）；捕獲距離未取 min(尺寸×0.6, 半徑)；吸收用 C# 而非 Motion Scatter |
| APPS-RAMP | 固定半徑 170、力道 5 | — | **缺少**：沒有隨時間增長的範圍與吸力包絡 |
| APPS-RELEASE | 放手時桌面已清空才釋放全部，從放手點靜態散佈 | `GroupDragLayout` | **不同**：規格是本次零捕獲才釋放、以 Throw 往外拋；origin 取放手點相同 |
| APPS-VELOCITY | 最後取樣距放手 <0.1s 且平滑速度 >180 才甩飛 | `PointerVelocity`、`Reaction.Throw`（C#） | **不同**：不是 100ms 視窗首末取樣；沒有 Apps 的 releaseForce 與徑向最低速度；甩飛用 C# 而非 Motion Throw |
| APPS-LIFECYCLE | capture 遺失時清除手勢；背景按下停止飛行；`OnDisable` 停止動畫 | — | **缺少**：取消不還原位置；沒有失焦、隱藏、尺寸變更、第二指標、捲動的處理 |
| APPS-CONFIG | 參數只在 Inspector | — | **缺少**：沒有 JSON 宣告、保存、套用與失敗重試 |
| APPS-OPENING | `Transition.Opening`：總長 1.4s、間隔 0.075、收攏 0.35、逾時 3s、按下跳過 | `Transition.Entrance` | **待比對**：Web〈入場規範〉沿用這套 Unity 入場，數值與回饋基數尚未逐項核對 |

範例另有規格沒有的行為：飛行撞擊障礙物、甩進 Apps 自動收納、收納時按鈕抖動、背景群組慣性、面板彈簧。規格說明這些不是本版復刻條件。

## 驗收對照與結果

每項的預期以 Web 規格為準。目前沒有任何依規格的實作，所以全部未執行。

| 來源 ID | 中性案例／補充檢查 | 環境 | 結果 | 證據／差異 |
|---|---|---|---|---|
| APPS-DATA | second-host-policies；P01 | Windows Editor | 未執行 | — |
| APPS-ACTION | hold-marks-click-activates、second-host-policies；P02 | Windows Editor | 未執行 | — |
| APPS-DRAG | group-follows-pointer、cancel-rolls-back、rejected-drop-rolls-back；P01、P03 | Windows Editor | 未執行 | — |
| APPS-DROP | stash-in-apps、stash-before-panel-slot、pull-back-to-desktop、panel-reorder；P03 | Windows Editor | 未執行 | — |
| APPS-INPUT | P02、P03（實體輸入） | Windows Editor；Android 觸控 | 未執行 | — |
| APPS-PANEL | panel-open-close；P03 | Windows Editor | 未執行 | — |
| APPS-VACUUM、APPS-RAMP | P04 | Windows Editor | 未執行 | 依賴 Motion Scatter 接入 |
| APPS-RELEASE、APPS-VELOCITY | P05 | Windows Editor | 未執行 | 依賴 Motion Throw、Scatter 接入 |
| APPS-LIFECYCLE | dispose-cancels-and-ignores；P06 | Windows Editor | 未執行 | — |
| APPS-CONFIG | P07 | Windows Editor | 未執行 | 依 Configuration 規格；Unity 端與 Preferences 的對應待討論 |
| APPS-OPENING | P08 | Windows Editor | 未執行 | — |

11 項中性案例要透過 Unity adapter 執行；adapter 尚未建立。手感、觸控與視覺屬人工檢查，執行時列入 `checklists/`，本文件只連結結果。

## 結論與下一步

目前沒有任何 APPS 項目可宣稱相容。對照結果：既有範例的組裝方式可沿用（單一手勢擁有者、指標捕獲、`Gesture.LongPress`、`ElementTarget`、`Transition.Opening`），但缺少模型與政策層，吸收、釋放、取消、鍵盤與設定都與規格不同，數值也還沒改走 Motion。

尚未決定：依上表選擇改既有範例或另建模組（決策 `appsmenu-approach` 的後續），以及 `Samples~/AppsMenu` 之後改為模組範例或退役。

**2026-10-07 暫停，先打基礎。** 依決策 `layering-doc-owner`、`core-contracts`、`ui-model-location`：AppsMenu 的操作模型（狀態、命令、行為規則、手勢判斷）放 DotNet Core，甩飛與吸塵器經 Core 的物理互動入口，Unity 只寫畫面適配。前置需求：

- [UNITYEXTENSION-DOTNET-2](../../../../Documentation/Requirements.DotNet.md#unityextension-dotnet-2--在-core-建立-appsmenu-的操作模型)：Core 的 AppsMenu 操作模型，11 項中性案例在模型上執行。
- [UNITYEXTENSION-DOTNET-1](../../../../Documentation/Requirements.DotNet.md#unityextension-dotnet-1--依-motion-規格在-core-建立動態合約)：Throw、Scatter 的單一入口。

前置完成後的順序：

1. 依上表決定改既有範例或另建模組；UIToolkit 的畫面與控制接上 Core 模型與物理互動入口。
2. 建立參數與案例匯入，記錄採用的 Web commit 與雜湊（[web-replication](../../../../.agents/skills/web-replication/SKILL.md)）。
3. 逐項執行上表的 Unity 端檢查（輸入、畫面、生命週期、平台），結果回報 Web 的 [WEB-UNITYEXTENSION-2](../../../../../../Web/Documentation/Requirements.UnityExtension.md#web-unityextension-2--appsmenu-依-web-主規格復刻)。

UGUI 版等有產品需要再做（決策 `ugui-appsmenu-now`），屆時另建 `Packages/UI/Documentation/Yu5h1Lib.Web/AppsMenu.md`。

Web 規格、設計資料、案例或 Motion 規格包改版時，找出受影響的 APPS-ID 重新驗收；舊結果只對應它記錄的版本。
