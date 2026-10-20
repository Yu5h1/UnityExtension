# Button 驗收紀錄（Yu5h1Lib.UIToolkit）

> Task ID: uitk-button
>
> 狀態：verified（2026-10-19，第二輪：Button 2.0.0）。自動檢查在乾淨 worktree 通過，使用者同日與 Web demo 並排確認外觀。依使用者指示「在 Yu5h1Lib.UIToolkit 引用 UX 的 Button」，流程見 referenced-module-acceptance。實作方式採「原生 Button + USS」（決策 64-66，uitk-button-native），移除自訂封裝元件 UxButton 與 ButtonDesign 常數。自動檢查涵蓋數值與規則邏輯（BTN-DISPLAY、BTN-SIZE、BTN-LAYOUT），外觀（solid/ghost 與狀態差異）交由使用者直接確認。

```text
UnityExtension
└─ Yu5h1Lib.UIToolkit（實作套件）
   └─ Button（本紀錄）
      ├─ 體驗規範 ← Yu5h1Lib.UX ModuleSpec/Controls/Button（BTN-DISPLAY、BTN-APPEARANCE、BTN-SIZE、BTN-LAYOUT）
      ├─ 規範引用、本輪未引用 ← 操作（INPUT-PRESS-TAP、INPUT-KEY-COMMAND、INPUT-NAME）、VisualFoundation、Icon
      └─ 驗證專案 → Yu5h1LibTest（Unity 6000.6），驗收時 manifest 暫時指向乾淨 worktree
```

## 身分

| 項目 | 內容 |
|---|---|
| 實作套件 | `com.yu5h1.uitoolkit`（`Packages/UIToolkit`）。採用原生 `UnityEngine.UIElements.Button` 與樣式表 `Yu5h1Button.uss` |
| UX 規範 | [UX ModuleSpec/Controls/Button.md](../../../../../../UX/Documentation/ModuleSpec/Controls/Button.md) 2.0.0，SHA-256 `9662a105972563ca4b546aac245aa5321761028b7aa96c6de7877f6ed77f2567` |
| 設計資料 | `Button/button-design.json` 3.0.0，SHA-256 `e2568af28f704603b2a0d696324f2389a81a685c5ad96049a38c4008cde5c0f9`：`size` 32、`iconRatio` 0.75、`iconLabelGap` 8 |
| UX commit | `8c1f764e22dc54960558c6bc81b364c31d3760b5` |
| DX 基準 | 未釘選：本輪早於 DX 1.0.0（UX 第 81 題，DX §1） |
| 套件釘選 | 快照 `Tests/Editor/UX~/Button/`（`Button.md`、`button-design.json`，位元組與 UX 相同，`.gitattributes` 設 `-text`）；`ButtonDesignTests` 核對雜湊與數值 |
| 範圍 | BTN-DISPLAY、BTN-SIZE、BTN-LAYOUT 全部；BTN-APPEARANCE 依樣式提供 solid/ghost |
| 環境 | Windows 11、Unity 6000.6.0f1 Editor、PlayMode（runtime 面板只在 Game view 繪製時才解析排版） |

## 本輪未引用的部分（缺口）

| 規範內容 | 依賴 | 本輪處理 |
|---|---|---|
| BTN-APPEARANCE「顏色依 VisualFoundation 的角色」 | VisualFoundation | partial（2026-10-21 更新）：色票、無底外框、滑過、選取、停用與焦點外框已依 VisualFoundation 1.3.1 的角色，使用者已在人工 checklist 記為通過，見 [VisualFoundation 紀錄](VisualFoundation.md)；實心靜止底色仍 blocked，等 UX 指定角色（UNITYEXTENSION-UX-1）。第二輪（2026-10-19）當時的記載：UIToolkit 沒有色票，顏色取 Unity 預設主題的按鈕變數 |
| BTN-DISPLAY「顯示設定不改變無障礙名稱」 | 操作 INPUT-NAME | blocked：UI Toolkit 元素沒有對應到 `UnityEngine.Accessibility` 的無障礙名稱 |
| 按下與鍵盤 | 操作 INPUT-PRESS-TAP、INPUT-KEY-COMMAND | 沿用 UI Toolkit 原生 Button，本輪不另寫程式 |
| 圖示的三種來源與著色 | Icon | blocked：原生 Button `iconImage` 支援 Texture2D、Sprite、RenderTexture、VectorImage |
| 外觀與滑過狀態確認 | — | 外觀交付使用者在 demo 中直接確認（決策 65），不再使用自動讀取 RenderTexture 像素之檢查 |

## 實作對照

| UX 行為 | UI Toolkit 實作 | 說明 |
|---|---|---|
| 公開入口 | `UnityEngine.UIElements.Button` + `Resources/Yu5h1Lib.UIToolkit/Yu5h1Button.uss` | 使用原生按鈕搭配樣式類別 `.yu5h1-button`，不再維護自訂 `UxButton` 類別 |
| BTN-DISPLAY | 原生 `iconImage` 與 `text`；`.yu5h1-button--icon-only` | 只顯示圖示時套用 `.yu5h1-button--icon-only` 設定 32×32 正方形；有名稱時高度為 32；無圖示或兩者關閉時備援顯示名稱 |
| BTN-SIZE | USS 百分比 `height: 75%`、`width: 75%` | 圖示一律 fit 按鈕：大小為 `size × 0.75`，不隨字型縮放（Button 2.0.0 決策 63） |
| BTN-LAYOUT | USS `flex-direction: row`、`align-items: center`、`margin-left: 8px` | 圖示在名稱前，水平間距 8px，兩者垂直置中 |
| BTN-APPEARANCE | `.yu5h1-button`（Solid）、`.yu5h1-button--ghost`（Ghost）、`.yu5h1-button--selected` | 實心預設底色；無底靜止透明，滑過/焦點/選中顯色 |

## 驗收對照

每項檢查的結果記在 [checklist](../../../../checklists/uitk-button.checklist)，本文不重複。

| UX 行為 | 檢查 | 結果所在 |
|---|---|---|
| 釘選 | 快照雜湊、合約版本與數值核對、乾淨 checkout 編譯 | checklist 第 1 項 |
| BTN-DISPLAY | 四種組合、名稱後備、只顯示圖示的正方形、有名稱時的高度 | checklist 第 2 項 |
| BTN-SIZE & BTN-LAYOUT | 各 size 下圖示 fit 按鈕（size × 0.75）、圖示在前、間距 8px、垂直置中 | checklist 第 3 項 |
| BTN-APPEARANCE | 使用者在範例中確認外觀（Solid/Ghost、Hover、Focus、Selected） | checklist 第 4 項 |
| 回歸 | UIToolkit 既有測試 | checklist 第 5 項 |

## 已知偏差

無。

## 輪次

| 規範版本 | UX commit | UnityExtension commit | checklist commit | 結論 |
|---|---|---|---|---|
| Button 1.1.0 | `d9a9790` | `9b53f9b`（範例 `9b2a2f6`） | `e70ad14` | 本輪範圍內相容：自動檢查全部通過，使用者確認外觀；缺口見第一輪紀錄 |
| Button 2.0.0 | `8c1f764` | `fb13d38`（原引用 `5db20f7` 已被 amend，只差本格，不在任何分支） | 本紀錄所在提交 | 本輪範圍內相容：釘選核對、乾淨 worktree EditMode 215/215、PlayMode `NativeButtonTests` 11/11（BT01、BT03）通過，使用者 2026-10-19 依第 83 題與 Web demo 並排確認外觀（BT02）；缺口見〈本輪未引用的部分〉 |

## 結論

### 第二輪（Button 2.0.0，原生 Button + USS，UnityExtension `fb13d38`）

狀態：verified（2026-10-19）。使用者確認：在 Yu5h1LibTest 的 `Samples~/UxAcceptance`（`button`）與 Web OptionPicker demo 的「TaskProgress 外觀」並排，比例、排列、顯示組合與狀態差異一致；顏色未比（VisualFoundation 未引用）。在本輪範圍內（BTN-DISPLAY 除無障礙名稱、BTN-SIZE、BTN-LAYOUT，BTN-APPEARANCE 的 solid／ghost 結構），原生 Button + USS 於 `fb13d38` 符合 Button 2.0.0。

- **釘選與編譯**：快照 SHA-256 與 UX commit `8c1f764` 完全一致，乾淨 worktree 下 `Yu5h1LibTest` 通過 215 項 EditMode 測試無編譯錯誤。
- **架構轉換**：移除自訂 `UxButton` 與 `ButtonDesign` 常數，改為原生 `UnityEngine.UIElements.Button` 與樣式表 `Yu5h1Button.uss`。
- **規則對照**：BTN-DISPLAY、BTN-SIZE、BTN-LAYOUT 的規則由 USS 與 PlayMode `NativeButtonTests` 表達；BT01（5 項）與 BT03（6 項，size 24／32／40）於 2026-10-19 在乾淨 worktree 全部通過（checklist 第 2、3 項）。BTN-DISPLAY 的「無障礙名稱不變」（INPUT-NAME）仍 blocked，該行為記 partial。外觀交付使用者在 `Samples~/UxAcceptance`（`CurrentTarget.json` 指向 `button`）依 UX 第 83 題與 Web OptionPicker demo 的「TaskProgress 外觀」並排確認。
- **DX**：未釘選（本輪早於 DX 1.0.0；UX 第 81 題）。

驗收環境與做法（2026-10-19，由未參與實作的 agent 執行）：

- 乾淨 checkout：`git worktree add --detach ../UnityExtension-accept-button fb13d38`，工作目錄乾淨。
- 宿主：Yu5h1LibTest（Unity 6000.6.0f1，Windows 11）。備份 `Packages/manifest.json` 與 `packages-lock.json` 後只把 `com.yu5h1.uitoolkit` 改指向 worktree，強制重新整理並要求編譯：Console 沒有錯誤，`PackageInfo.resolvedPath` 是 worktree 路徑。驗收後還原 manifest 並重新整理，`resolvedPath` 回到主 checkout；worktree 已移除。
- PlayMode `Yu5h1Lib.UIToolkit.Tests.NativeButtonTests`：11／11 通過（BT01 5 項、BT03 6 項），經 MCP 的 Test Runner 執行，未搶焦點。每項測試輸出 Unity 的 “No Theme Style Sheet set to PanelSettings” 警告：量到的是無主題面板的排版結果，與第一輪的審查備註相同。
- 限制：Yu5h1LibTest 以 junction 掛入主 checkout（分支 `adopt-ux-optionpicker`）的 `Samples~/UxAcceptance`，它引用 `Yu5h1Lib.UIToolkit.Acceptance`，而 `fb13d38` 的 Runtime 沒有這個命名空間；驗收後第二次強制重新整理時 Console 出現該範例 asmdef 的 5 個 CS0234／CS0246 錯誤，重新解析套件回主 checkout 後清除。測試執行前讀到的 Console 沒有錯誤，Test Runner 也回報 11 項結果，但無法從 Console 證明執行當下範例 asmdef 已編譯；它不在 `NativeButtonTests` 的相依鏈內，不影響量測結果。下次把範例 junction 一起指向 worktree，或在 worktree 內另建測試宿主。

審查備註（2026-10-19 流程審查，不影響結果）：

- 本輪的實作提交（`923574c`、`fb13d38`）由實作方同時寫入紀錄與 checklist 的釘選項；BT01／BT03 的執行與記錄改由未參與實作的 agent 在乾淨 worktree 完成（見上）。
- 紀錄原引用的 `5db20f7` 已被 amend 為 `fb13d38`（只差輪次表這一格），不在任何分支；已改引用。
- 第一輪審查備註中的狀態疊加（實心鈕 hover 蓋過 selected）仍未在規格定義，原生實作是否相同待使用者看時留意；屬規格缺口，應走 `Documentation/Requirements.UX.md`。

### 第一輪（Button 1.1.0，UxButton，UnityExtension `9b53f9b`）

使用者確認（2026-10-17）：在 Yu5h1LibTest 開啟範例 `Samples~/UxButton`（Gemini `9b2a2f6`，Unity 預設 runtime 主題），實心／無底、四種顯示組合與兩種後備、size 24／32／40、選中狀態，外觀沒有問題。Claude 也在同一場景進入 Play，Console 沒有錯誤。範例不在 checklist 內，是給使用者直接看的工具。

在本輪範圍內（BTN-DISPLAY、BTN-SIZE、BTN-LAYOUT，BTN-APPEARANCE 的結構），`UxButton` 於 UnityExtension `9b53f9b` 符合 Button 1.1.0；9 項自動檢查全部通過，沒有偏差。「本輪未引用的部分」所列缺口不在本結論內，滑過與整體外觀由使用者直接確認。這是自動檢查完成，不是驗收完成。

驗收環境與做法（2026-10-17，由未參與實作的 agent 執行）：

- 乾淨 checkout：`git worktree add --detach ../UnityExtension-accept-button 9b53f9b`。快照 `Button.md`、`button-design.json` 的 SHA-256 與 UX `manifest.json`、UX 工作目錄的檔案相同；`git check-attr` 顯示兩檔 `text: unset`，新 checkout 保留位元組。
- 宿主：Yu5h1LibTest（Unity 6000.6.0f1，Windows 11）。備份 `Packages/manifest.json` 後只把 `com.yu5h1.uitoolkit` 改指向 worktree，強制重新整理並要求編譯：Console 沒有錯誤，`packages-lock.json` 與 `PackageInfo.resolvedPath` 都是 worktree 路徑。驗收後還原 manifest（SHA-256 與備份相同），重新整理後 Console 沒有錯誤，`resolvedPath` 回到主 checkout，`packages-lock.json` 與驗收前相同；worktree 已移除。
- EditMode `Yu5h1Lib.UIToolkit.Editor.Tests`：79／79 通過（分支前 76，加上 `ButtonDesignTests` 3 項）。
- PlayMode `Yu5h1Lib.UIToolkit.Tests`：27／27 通過（分支前 13，加上 `UxButtonTests` 14 項，含參數化案例）。

審查備註（不違反規範，不影響結果；值得修正的放進下一次給實作者的指示）：

- 測試品質：PlayMode 面板沒有設定 theme style sheet（每項測試都輸出 Unity 的 “No Theme Style Sheet set to PanelSettings” 警告）。因此量到的是無主題面板的結果：Unity 預設主題的 `.unity-button` 內距、邊框與 `--unity-colors-button-*` 顏色變數沒有參與，外觀像素測試用的是 USS 的備用色；名稱是否真的畫出文字也沒有被驗證。
- 測試品質：顯示與否用 `resolvedStyle.display` 判斷，屬讀回設定；名稱「仍顯示」沒有另外確認名稱元素有非零的實際大小。尺寸類檢查則是量 `worldBound`，符合 BT03。
- 測試品質：名稱旁圖示的測試把字型設成 14px，正好等於程式在讀不到字型大小時的預設值，所以「跟著字型等比例變化」的路徑（例如 12px → 約 15.4px）沒有被觀察到；`size` 24 時上限 18 剛好等於 18，上限真正生效的情況（例如 `size` 20 → 15）也沒有被觀察到。本輪 checklist 只要求 14px 的結果，所以不算失敗。
- 測試品質：接受圖示的測試用空白的 `VectorImage`，確認的是圖示元素顯示且尺寸正確，不是圖示有畫出來；圖示來源屬本輪缺口（Icon）。
- 實作：實心按鈕的 `.yu5h1-button:hover` 選擇器權重高於 `.yu5h1-button--selected`，已選中的實心按鈕在滑過時改顯示滑過色、看不出選中。規範沒有規定狀態疊加，屬待使用者確認外觀時留意的點。
- 實作：實心按鈕沒有自己的焦點樣式，完全依賴主題；規範只要求無底按鈕的焦點看得出來，所以不違反。
- 實作：繼承自原生 `Button` 的 `text` 沒有被擋下，宿主若設定 `text` 會在子元素之外再畫一份文字；文件已說明應使用 `LabelText`。
