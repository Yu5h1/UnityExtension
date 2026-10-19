# Button 驗收紀錄（Yu5h1Lib.UI）

> Task ID: ui-button
>
> 狀態：implementing（2026-10-18，第一輪：Button 2.0.0）。依使用者指示「在 Yu5h1Lib.UI 引用 UX 的 Button」，流程見 [referenced-module-acceptance](../../../../../.agents/skills/referenced-module-acceptance/SKILL.md)。實作方式採「UGUI 原生 `UnityEngine.UI.Button`／`HorizontalLayoutGroup`／`LayoutElement` 搭配既有 [`ButtonAddon`](../../Runtime/Component/Addon/ButtonAddon.cs) 補足 UGUI 無樣式表之規則缺口」（原生優先原則）。自動檢查涵蓋數值與規則邏輯（BTN-DISPLAY、BTN-SIZE、BTN-LAYOUT），外觀（solid/ghost 與狀態差異）交由使用者直接確認。

```text
UnityExtension
└─ Yu5h1Lib.UI（實作套件，UGUI）
   └─ Button（本紀錄）
      ├─ 體驗規範 ← Yu5h1Lib.UX ModuleSpec/Controls/Button 2.0.0（BTN-DISPLAY、BTN-APPEARANCE、BTN-SIZE、BTN-LAYOUT）
      ├─ 規範引用、本輪未引用 ← 操作（INPUT-PRESS-TAP、INPUT-KEY-COMMAND 由 UGUI 原生 Button 提供；INPUT-NAME 未引用）、VisualFoundation、Icon
      └─ 驗證專案 → Yu5h1LibTest（Unity 6000.6），驗收時於乾淨 worktree 驗證並提供 Samples~/UxAcceptance 統一入口
```

## 身分

| 項目 | 內容 |
|---|---|
| 實作套件 | `com.yu5h1.ui`（`Packages/UI`，UGUI）。採用原生 `UnityEngine.UI.Button`、`Image`、`Text`、`HorizontalLayoutGroup`、`LayoutElement` 搭配 [`ButtonAddon`](../../Runtime/Component/Addon/ButtonAddon.cs) |
| UX 規範 | [UX ModuleSpec/Controls/Button.md](../../../../../../UX/Documentation/ModuleSpec/Controls/Button.md) 2.0.0，SHA-256 `9662a105972563ca4b546aac245aa5321761028b7aa96c6de7877f6ed77f2567` |
| 設計資料 | `Button/button-design.json` 3.0.0，SHA-256 `e2568af28f704603b2a0d696324f2389a81a685c5ad96049a38c4008cde5c0f9`：`size` 32、`iconRatio` 0.75、`iconLabelGap` 8 |
| UX commit | `c7c5438b112a660b499dde27f9b019bfa5913ef1`（Button 2.0.0 自 `1988029`／`8c1f764` 起未變） |
| 套件釘選 | 快照 `Tests/Editor/UX~/Button/`（`Button.md`、`button-design.json`，位元組與 UX 相同，`.gitattributes` 設 `-text`）；`ButtonDesignTests` 核對雜湊與數值 |
| 範圍 | BTN-DISPLAY、BTN-SIZE、BTN-LAYOUT 全部；BTN-APPEARANCE 依 `ButtonAddon.appearance`（`Solid`／`Ghost`）與 `isSelected` 設定 `Selectable.colors` |
| 環境 | Windows 11、Unity 6000.6.0f1 Editor |

## 目標對照與本輪未引用的部分（缺口）

- **UX 目標章節缺口**：UX `Button.md` 2.0.0 的〈目標 1：可按控件的共用外觀〉仍將 `UnityExtension` 列在目標 1 外，且 `BTN-SIZE`、`BTN-LAYOUT` 尚無目標（屬 UX 待討論項 `ux-goal-consumer-evidence`）。本紀錄對照目標 1 的 3 個情境（1. `solid`／`ghost` 外觀、2. 圖示與名稱顯示組合與非空後備、3. 只顯示圖示時 `size` 正方形且圖示置中）及 `BTN-SIZE`、`BTN-LAYOUT`（BT03）：目前自動檢查已通過情境 2、3 與 BT03（此平台目標情境通過數 2/3，情境 1 外觀待使用者於已載入之 `UxUiAcceptanceDemo.unity` 視覺確認）。

| 規範內容 | 依賴 | 本輪處理 |
|---|---|---|
| BTN-APPEARANCE「顏色依 VisualFoundation 的角色」 | VisualFoundation | blocked：`Yu5h1Lib.UI` 尚未引用 VisualFoundation 色票；本輪以 `ButtonAddon` 設定 `Selectable.colors` 的實心與無底（靜止透明、滑過/按下/焦點/選中顯色）結構 |
| BTN-DISPLAY「顯示設定不改變無障礙名稱」 | 操作 INPUT-NAME | blocked：UGUI `Button` 無內建對應 INPUT-NAME 的跨平台無障礙名稱映射 |
| 按下與鍵盤 | 操作 INPUT-PRESS-TAP、INPUT-KEY-COMMAND | 沿用 UGUI 原生 `UnityEngine.UI.Button`（`IPointerClickHandler`、`ISubmitHandler`），本輪不另寫程式 |
| 圖示的三種來源與著色 | Icon | blocked：UGUI 原生 `Image` 使用 `Sprite`（支援 `color` 相乘著色），尚未引用 Icon 的三種來源 |
| 外觀與滑過狀態確認 | — | 外觀交付使用者在 `Samples~/UxAcceptance`（匯入於 `Assets/Samples/Yu5h1Lib.UI/1.0.0/UxAcceptance/UxUiAcceptanceDemo.unity`）中直接確認（決策 65） |

## 既有實作與原生元件盤點（Step 2）

| UX 行為 ID | UGUI 原生元件能力 | 套件既有程式碼 | 盤點結論與實作選擇 |
|---|---|---|---|
| 按下與鍵盤（INPUT-PRESS-TAP、INPUT-KEY-COMMAND） | `UnityEngine.UI.Button` 原生支援點擊與 Enter/Space 提交 | `ButtonAddon : UIControl<Button>` 僅有 `InvokeOnClick()` | **已符合**：直接使用原生 `Button` |
| BTN-LAYOUT | `HorizontalLayoutGroup` 可設定 `spacing = 8`、`childAlignment = MiddleCenter` 將圖示排在名稱前並垂直置中 | 無 | **部分符合**：排版由原生 `HorizontalLayoutGroup` 負責；切換只顯示圖示與圖示加名稱時的內距與間距由 `ButtonAddon` 同步 |
| BTN-DISPLAY | 原生 `Button` 無 `icon`／`label` 顯示開關、無「兩者皆關或無圖示時仍顯示名稱」的非空後備規則，亦不會自動在 `size × size` 正方形與 `height = size` 間切換 | 無 | **缺失**：由於 UGUI 無 USS 樣式表，在既有 `ButtonAddon` 補上 `showIcon`、`showLabel`、非空後備規則與正方形／高度 `size` 切換 |
| BTN-SIZE | 純圖示正方形鈕雖可用錨點 `12.5%～87.5%` 表達 75%，但有名稱並排時按鈕寬度隨文字伸展，水平百分比錨點不再等於 `size × 0.75`，且 `HorizontalLayoutGroup` 會接管子物件位置與尺寸 | 無 | **缺失**：由 `ButtonAddon` 依 `size × 0.75`（`IconRatio = 0.75f`）同步圖示 `Image` 的 `LayoutElement`／`RectTransform` 正方形邊長並開啟 `preserveAspect` |
| BTN-APPEARANCE | 原生 `Selectable.colors`（`ColorBlock`）可表達一般、滑過、按下、焦點與停用色，但無 `Solid`／`Ghost` 切換與脫離焦點後仍維持的 `selected` 狀態色 | 無 | **部分缺失**：由 `ButtonAddon` 的 `appearance`（`Solid`／`Ghost`）與 `isSelected` 切換 `Button.colors`（`Ghost` 靜止時 `normalColor.a = 0`，滑過、焦點與選中時顯色） |

## 實作對照

| UX 行為 | Yu5h1Lib.UI 實作 | 說明 |
|---|---|---|
| 公開入口 | `UnityEngine.UI.Button` + [`Yu5h1Lib.UI.ButtonAddon`](../../Runtime/Component/Addon/ButtonAddon.cs) | 沿用套件既有 `ButtonAddon : UIControl<Button>`（保留原 GUID 與 `InvokeOnClick()`），只補 UGUI 原生元件無法單靠樣式表達的 BTN-* 規則，並提供 `ButtonAddon.Create` 建立標準原生階層 |
| 實體 Prefab 資產 | `Packages/UI/Runtime/Prefabs/Button.prefab`、`Button-Ghost.prefab` | 開箱即用的標準 UGUI Prefab，符合 Agentic DX 要求：下游 Agent 單行或拖曳即可使用（極致 Token 節約），且完整保留 Inspector 原生可調性 |
| BTN-DISPLAY | `ButtonAddon.showIcon`、`showLabel`、`Apply()` | `displayIcon = showIcon && hasIcon`、`displayLabel = showLabel \|\| !displayIcon`（無圖示或兩者皆關時自動顯示名稱）；只顯示圖示時設為 `size × size` 正方形，有名稱時高度為 `size` |
| BTN-SIZE | `ButtonAddon.size`、`IconRatio`（`0.75f`） | 圖示 `Image`（`preserveAspect = true`）透過 `LayoutElement` 與 `RectTransform` 設為 `(size × 0.75) × (size × 0.75)`，不論有無名稱皆隨 `size` 自動縮放 |
| BTN-LAYOUT | 原生 `HorizontalLayoutGroup` + `ButtonAddon.IconLabelGap`（`8f`） | 圖示與名稱並存時，圖示位於起始側（左）、名稱在後，間距 `8px`，`TextAnchor.MiddleCenter` 垂直置中 |
| BTN-APPEARANCE | `ButtonAddon.appearance`（`Solid`、`Ghost`）、`isSelected` | 設定原生 `Button.colors`（`ColorBlock`）：`Solid` 靜止有底色；`Ghost` 靜止透明（`alpha = 0`），滑過、按下、焦點與 `isSelected` 時具可見底色 |
| 共享驗收入口 | [`Samples~/UxAcceptance/UxUiAcceptanceHost.cs`](../../Samples~/UxAcceptance/UxUiAcceptanceHost.cs) + [`UxUiButtonDemoCase.cs`](../../Samples~/UxAcceptance/Cases/UxUiButtonDemoCase.cs) | 驗收協調器位於 `Samples~/UxAcceptance/Coordinator/`，正式 Runtime DLL 不受測試框架污染；以 Junction 匯入 `Yu5h1LibTest/Assets/Samples/Yu5h1Lib.UI/1.0.0/UxAcceptance`；預設目標為 `"button"` |

## 驗收對照

每項檢查的結果記在 [checklist](../../../../checklists/ui-button.checklist)，本文不重複。

| UX 行為 | 檢查 | 結果所在 |
|---|---|---|
| 釘選 | 快照雜湊、合約版本與數值核對、編譯與測試 | checklist 第 1 項 |
| BTN-DISPLAY | 四種組合、名稱後備、只顯示圖示的正方形、有名稱時的高度（BT01） | checklist 第 2 項 |
| BTN-SIZE & BTN-LAYOUT | 各 size（24/32/40）下圖示 fit 按鈕（size × 0.75）、圖示在前、間距 8px、垂直置中（BT03） | checklist 第 3 項 |
| Agentic DX Prefab | 實體資產交付、Token 效率檢驗、Inspector 原生調整保留（BT04） | checklist 第 4 項 |
| 共享驗收入口與外觀確認 | `Samples~/UxAcceptance` 註冊 `"button"` 案例與第 2 模組擴充驗證；使用者確認外觀 | checklist 第 5 項 |

## 已知偏差

無。

## 輪次

| 規範版本 | UX commit | UnityExtension commit | checklist commit | 結論 |
|---|---|---|---|---|
| Button 2.0.0 | `c7c5438` | `2bda6ab` | `2bda6ab` | 自動檢查全部通過（`Yu5h1Lib.UI.Editor.Tests` 7/7 通過，含 Prefab 與 DX 檢驗），統一驗收入口場景已載入，待使用者視覺確認外觀 |

## 結論

- **釘選與自動檢查**：`Yu5h1Lib.UI.Editor.Tests` 7/7 全數通過（`ButtonDesignTests` 3/3 驗證 SHA-256 與設計數值；`UxUiButtonTests` 4/4 驗證 BT01 顯示組合與非空後備、BT03 各尺寸圖示 `size × 0.75` 與 `8px` 間距及垂直置中、BT04 實體 `Button.prefab` / `Button-Ghost.prefab` 與 DX Token 效率及 Inspector 調整保留、以及 `UxUiAcceptanceHost` 預設目標解析與第 2 模組切換／未知 ID `[Blocked]` 標示）。
- **實體資產交付 (Agentic DX)**：交付開箱即用之 `Packages/UI/Runtime/Prefabs/Button.prefab` 與 `Button-Ghost.prefab`，保證下游 Agent 一行實例化且具備 Scene View WYSIWYG 與 Inspector 微調能力；正式 Runtime 乾淨無測試協調代碼。
- **統一驗收入口**：已依 `referenced-module-acceptance` 建立 `Packages/UI/Samples~/UxAcceptance` 並匯入至 `Yu5h1LibTest` 的 `Assets/Samples/Yu5h1Lib.UI/1.0.0/UxAcceptance/UxUiAcceptanceDemo.unity`（場景驗證 `missingScripts: 0` 且已於 Editor 開啟），待使用者進行 `[manual]` 外觀視覺確認。


