# VisualFoundation 驗收紀錄（Yu5h1Lib.UIToolkit）

> Task ID: uitk-visualfoundation
>
> 狀態：第二輪（VisualFoundation 1.4.0、Button 2.1.0、OptionPicker 3.1.0）模組輪通過、人工待驗（2026-10-21）。依使用者指示「在 Yu5h1Lib.UIToolkit 引用 UX 的 VisualFoundation」（UX-UNITYEXTENSION-2），流程見 referenced-module-acceptance。實作 `7abbe0f` 已合併到 `master`（`98688b2`，分支已刪除）；人工 checklist 已可在 Yu5h1LibTest 開啟。第一輪（1.3.0／1.3.1）的人工 checklist 使用者已記為通過，但當時實心靜止底色與 `solid` 面板外框規格沒寫角色而 blocked；UX 在 1.4.0、2.1.0、3.1.0 補齊後，這兩列與其他暫行解讀在第二輪改依規格實作。目標 1 的「達成」由 UX 依需求標記。

```text
UnityExtension
└─ Yu5h1Lib.UIToolkit（實作套件）
   └─ VisualFoundation（本紀錄）
      ├─ 體驗規範 ← Yu5h1Lib.UX ModuleSpec/VisualFoundation 1.4.0 目標 1
      ├─ 被引用的消費端 → Button 2.1.0、OptionPicker 3.1.0（本套件各自的紀錄）
      └─ 驗證專案 → Yu5h1LibTest（Unity 6000.6），驗收時 manifest 暫時指向乾淨 worktree
```

## 身分

| 項目 | 內容 |
|---|---|
| 實作套件 | `com.yu5h1.uitoolkit`（`Packages/UIToolkit`）：`Yu5h1VisualFoundation.uss`（產生）、`VisualFoundationTheme`（範圍）、`Yu5h1Button.uss`、`Yu5h1OptionPicker.uss`、`Yu5h1FocusRing.png` |
| UX 規範 | [UX ModuleSpec/VisualFoundation.md](../../../../../../UX/Documentation/ModuleSpec/VisualFoundation.md) 1.4.0，SHA-256 `7ddc4139f980c279247ad46ae8fbc2f1d1a742be6d5983c856b6dab750999de8`；[Button](../../../../../../UX/Documentation/ModuleSpec/Controls/Button.md) 2.1.0，`0a121a5a0d96dde604dc1a737702e6155772bde4483e0b5454ff77f8b752c033`；[OptionPicker](../../../../../../UX/Documentation/ModuleSpec/Controls/OptionPicker.md) 3.1.0，`2adff90e313ce7c09041dc98d6696e9f9f278f4c9329ca9a4626a13ac44400b9` |
| 設計資料 | `VisualFoundation/visual-design.json` 合約 1.2.0（schemaVersion 1），SHA-256 `7d41c8287ef9aafe068280459f10cf906f616c803c530e16bb469843940b42c9`：light、dark 各 15 個角色，`focusMinWidth` 2。Button 與 OptionPicker 的設計資料雜湊不變（`e2568af2…`、`551517bc…`） |
| 案例 | `VisualFoundation/visual-cases.json` 1.1.0，SHA-256 `06a9edb4e1d450c45718408d897e434cee093cfab248f97aba67eb4a3aaba807`（隨快照釘選；色票由面板讀回，不經 adapter 執行中性案例，見〈未引用的部分〉） |
| UX commit | 第二輪 `5205838`；第一輪 `855479e`（1.3.0），1.3.1 重新釘選 `879ca0f`。雜湊與 UX `manifest.json` 相同 |
| DX 基準 | 未釘選：本輪早於 DX 1.0.0 的首次驗證範圍（UX 第 81 題，DX §1） |
| 套件釘選 | 快照 `Tests/Editor/UX~/VisualFoundation/`、`Button/`、`OptionPicker/`（位元組與 UX 相同，`.gitattributes` 已有 `-text` 規則）；`VisualFoundationDesignTests`、`ButtonDesignTests`、`OptionPickerDesignTests` 核對雜湊與版本 |
| 範圍 | 目標 1 的三條情境：色票交付（VIS-PALETTE 與預設配色的 VIS-DERIVE）、Button 與 OptionPicker 的狀態外觀（VIS-STATE）、鍵盤焦點外框（VIS-FOCUS） |
| 環境 | Windows 11、Unity 6000.6.0f1 Editor、PlayMode（runtime 面板，無主題樣式表） |

## 引用的模組與缺口分析

VisualFoundation 本身不引用其他模組；它被 Button 與 OptionPicker 引用。

| 項目 | UX 有目標 | 本套件已有此版本的驗證 | 缺口的路由 |
|---|---|---|---|
| VisualFoundation 目標 1 | 有（第 94、95 題） | 第一輪 1.3.0／1.3.1 通過；第二輪 1.4.0 本紀錄 | [UX-UNITYEXTENSION-2](../../../../../../UX/Documentation/Requirements.UnityExtension.md#ux-unityextension-2--webtheme-改讀-ux-的色票交付) |
| 實心 Button 靜止底色與外框、`solid` 面板外框、無底靜止外框、一般按下與選取後滑過 | 第一輪規格沒有指定角色；UX 已於 1.4.0／2.1.0／3.1.0 補齊 | 第二輪 | [UNITYEXTENSION-UX-1](../../../../Documentation/Requirements.UX.md#unityextension-ux-1--button-與-optionpicker-面板還有幾個狀態沒有指定色彩角色)，UX 已回覆 |
| Icon（圖示著色） | 有 | 本套件 Button 紀錄 blocked | 不在目標 1；圖示不上色，見〈未引用的部分〉 |

## 實作對照

| UX 行為 | UI Toolkit 實作 | 說明 |
|---|---|---|
| 色票（VIS-PALETTE）、預設配色的推導（VIS-DERIVE） | `Yu5h1VisualFoundation.uss`：`.yu5h1-theme--light`、`.yu5h1-theme--dark` 各宣告 15 個 `--yu5h1-color-*` 與 `--yu5h1-focus-min-width` | 由 `VisualFoundationThemeGenerator`（選單 Tools/Yu5h1Lib/UIToolkit/Regenerate VisualFoundation Theme）自釘選的設計資料產生；推導結果已在設計資料裡，不重算。同一輸入產生相同位元組，檔頭記設計資料雜湊 |
| 明暗模式（VIS-SCHEME） | `VisualFoundationTheme.Apply(scope, Light｜Dark)`；沒有任何範圍的 OptionPicker 自己套 light | light、dark 固定；system 視為 light（沒有系統偏好來源，〈允許差異〉）。使用者的範圍元素決定整棵子樹的明暗 |
| BTN-APPEARANCE 與狀態外觀（VIS-STATE） | `Yu5h1Button.uss` | 實心靜止：底色 `panelBackground`、外框 `controlBorder`、文字 `itemText`；無底靜止：沒有底色也沒有外框；兩者滑過底色 `hover`；選取底色 `accent`、文字 `onAccent`、沒有外框，選取且滑過維持選取；按住中不另定（沒有 `:active` 規則）；停用文字 `disabled`，滑過看不到外觀 |
| OPT-APPEARANCE（面板） | `Yu5h1OptionPicker.uss` | `solid`：底色 `panelBackground`、外框 `border`；USS 沒有陰影，只畫外框（規格允許沒有陰影能力的平台只畫外框）；`ghost`：都沒有 |
| 焦點外框（VIS-FOCUS） | `:focus` 時 `Yu5h1FocusRing.png`（16×16、環寬 2、九宮格切 6）以 `focus` 角色著色，畫在按鈕自己的範圍內 | USS 沒有 outline；用 `border` 會縮小內容區，圖示大小（size × 0.75）就不成立，所以用圖。實心靜止外框（`controlBorder`）也用同一張圖。環寬等於 `focusMinWidth`，由測試讀圖確認 |

## 本輪未引用的部分（缺口）

| 規範內容 | 本輪處理 |
|---|---|
| 圖示顏色 | 不上色：`itemText` 對圖示的著色屬 Icon 模組，本套件 Button 紀錄已把 Icon 記為 blocked |
| 面板陰影 | 平台差異：USS 沒有陰影，OPT-APPEARANCE 允許只畫外框；使用者看時留意 |
| 自訂配色（VIS-CUSTOM） | not run：沒有自訂配色的介面（目標 1 對照表），中性案例 `custom-*` 不經 adapter 執行 |
| 對比（VIS-CONTRAST） | 不是平台檢查：VC01 由 UX 執行；本套件色值來自同一份設計資料 |
| 系統明暗跟隨、執行中切換（VIS-SCHEME 的 system、VIS-SWITCH） | not run：不在目標 1；沒有系統偏好來源，也沒有消費端要求 |
| 原生部件（VIS-NATIVE） | 不適用：UI Toolkit 自繪，沒有原生部件 |
| 文字縮放（VIS-TEXT-SCALE） | 不適用：Unity 不套系統字級，縮放隨顯示 |
| 單位與量測（VIS-UNIT） | 沿用 Button 的 BTN-SIZE 量測；外框圖在靜止狀態也畫出後 `NativeButtonTests` 仍通過 |
| 中性案例 `visual-cases.json` | 隨快照釘選；色票由面板實際解析的值與設計資料逐角色比對（VP01），沒有為 `resolve`／`contrast` 操作另寫 adapter |

## 驗收對照

每項檢查的結果記在 [checklist](../../../../checklists/uitk-visualfoundation.checklist)，本文不重複；人工檢查在[一份 checklist](../../../../checklists/uitk-visualfoundation-vp03.checklist)，light、dark 是裡面的兩個工作項目（1.3.1：一頁一份）。

| UX 行為 | 檢查 | 結果所在 |
|---|---|---|
| 釘選與色票產生 | 三份快照雜湊、版本、產生結果與樣式表一致、樣式表只讀主題宣告的變數且沒有顏色字面值、焦點外框環寬、乾淨 checkout 編譯 | checklist 第 1 項 |
| VIS-PALETTE（VP01） | PlayMode 讀面板解析出的 15 個角色，light、dark 各一次 | checklist 第 2 項 |
| BTN-APPEARANCE、VIS-STATE、VIS-FOCUS（自動部分） | PlayMode 讀各狀態到達的角色與焦點外框 | checklist 第 3 項 |
| 回歸 | UIToolkit 既有 EditMode 與 PlayMode | checklist 第 4 項 |
| VIS-STATE、VIS-FOCUS、OPT-APPEARANCE（VP03，外觀） | 使用者在 `Samples~/UxAcceptance` 的 VisualFoundation 頁確認 light、dark | 一份人工 checklist |

驗收入口：Yu5h1LibTest 的 `Samples~/UxAcceptance`，模組下拉選「VisualFoundation (1.4.0) light」或「dark」（案例 `visualfoundation-light`、`visualfoundation-dark`，在 `Samples~/UxAcceptance/Cases/UxVisualFoundationDemoCase.cs` 註冊，沒有改動 coordinator 的分支）。

## 已知偏差

無已接受的偏差。面板陰影是規格允許的平台差異，列在〈本輪未引用的部分〉。

## 輪次

| 規範版本 | UX commit | UnityExtension commit | checklist commit | 結論 |
|---|---|---|---|---|
| VisualFoundation 1.3.0 目標 1 | `855479e` | `603ec74`（已合併 `a6cfd29`） | `a9cc836` | 第一輪：模組輪通過（乾淨 worktree EditMode 110/110、PlayMode 30/30），人工 checklist 使用者記為通過；實心靜止底色與 `solid` 面板外框 blocked |
| VisualFoundation 1.3.1 | `879ca0f` | 重新釘選 `cbfedb8` | `cbfedb8` | 只改完成條件的人工 checklist 寫法，行為與設計資料不變，不重跑模組輪；`VisualFoundationDesignTests` 在 Unity 重跑 110/110（宿主解析到主 checkout） |
| VisualFoundation 1.4.0、Button 2.1.0、OptionPicker 3.1.0 | `5205838` | `7abbe0f`（已合併 `98688b2`） | 本紀錄所在提交 | 第二輪：模組輪通過（乾淨 worktree EditMode 111/111、PlayMode 30/30）、人工待驗 |

## 結論

### 第二輪（1.4.0／2.1.0／3.1.0，UnityExtension `7abbe0f`）

狀態：模組輪通過、人工待驗（2026-10-21）。在本輪範圍內（色票交付、Button 的靜止、滑過、選取、選取且滑過、停用與焦點外框、OptionPicker 面板的底色與外框），`7abbe0f` 符合三份規格的自動可驗部分；這是自動檢查完成，不是驗收完成。目標 1 要達成，還需要使用者在 light、dark 兩個工作項目確認外觀，並由 UX 標記。

驗收環境與做法（2026-10-21，由實作的 session 自己執行，不是未參與實作的 agent）：

- 乾淨 checkout：`git worktree add --detach ../UnityExtension-accept-vf140 7abbe0f`，工作目錄乾淨，三份規範快照的 SHA-256 與 UX manifest 相同。實作期間另在開發 worktree 先跑過一次，結果相同。
- 宿主：Yu5h1LibTest（Unity 6000.6.0f1，Windows 11）。備份 `Packages/manifest.json`（`efcafda0…`）與 `packages-lock.json`（`a2f1d8d6…`）後只把 `com.yu5h1.uitoolkit` 改指向 worktree，`Client.Resolve()` 並強制重新整理，`PackageInfo.resolvedPath` 是 worktree 路徑，Console 沒有編譯錯誤。驗收後還原 manifest、`Resolve()`，`resolvedPath` 回到主 checkout，lock 雜湊與備份相同；worktree 已移除。
- EditMode `Yu5h1Lib.UIToolkit.Editor.Tests`：111/111 通過。
- PlayMode `Yu5h1Lib.UIToolkit.Tests`：30/30 通過（含 `VisualFoundationTests` 6 項、`NativeButtonTests` 的 BT01、BT03 14 項）。
- 重新指向的輸入：套件路徑。未重新指向：Yu5h1LibTest 內 `Assets/Samples/…/UxAcceptance` 的 junction 仍指向主 checkout 的 `Samples~`（不在被測相依鏈內）。

審查備註（不違反規範，不影響結果）：

- 狀態測試讀 resolvedStyle 的角色；外框是圖，只能讀貼圖是否存在與著色，完整包圍、不被蓋住與外觀靠使用者看。
- `solid` 面板外框的角色（`border`）由 EditMode 的「樣式表只讀主題宣告的變數」與「沒有顏色字面值」保障，沒有 PlayMode 讀回。
- 面板沒有陰影：USS 沒有 box-shadow，規格允許，使用者看時留意是否覺得面板太平。
- light 的 `hover` 色（`#efeee8`）與頁面底色（`#f5f3ec`）很接近，在頁面上幾乎看不出來；在面板內（`panelBackground` `#fffdf8`）比較明顯。這是設計資料的值。

### 第一輪（1.3.0，UnityExtension `603ec74`）

模組輪在乾淨 worktree 通過（EditMode 110/110、PlayMode 30/30），人工 checklist 的 light、dark 共 4 項使用者記為通過。實心靜止底色、`solid` 面板外框當時 blocked（規格沒寫角色），無底靜止外框、一般按下與選取後滑過是暫行解讀；這些在第二輪依 UX 補齊的規格改過。細節由 git 歷史保存。
