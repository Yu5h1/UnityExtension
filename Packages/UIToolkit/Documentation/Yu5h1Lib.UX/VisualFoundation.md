# VisualFoundation 驗收紀錄（Yu5h1Lib.UIToolkit）

> Task ID: uitk-visualfoundation
>
> 狀態：模組輪通過，人工 checklist 使用者已記為通過（2026-10-21，light、dark 共 4 項，結果以 `uitk-visualfoundation-vp03.checklist` 為準）；實心鈕靜止底色等兩列仍 blocked，目標 1 的「達成」由 UX 依需求標記。依使用者指示「在 Yu5h1Lib.UIToolkit 引用 UX 的 VisualFoundation」（UX-UNITYEXTENSION-2），流程見 referenced-module-acceptance。實作 `603ec74` 已合併到 `master`（`a6cfd29`，分支已刪除）；自動檢查在乾淨 worktree 通過，light、dark 各一頁的人工 checklist 已可在 Yu5h1LibTest 開啟。實心按鈕靜止底色與實心面板外框的角色規格沒有寫（UNITYEXTENSION-UX-1），兩列 blocked。

```text
UnityExtension
└─ Yu5h1Lib.UIToolkit（實作套件）
   └─ VisualFoundation（本紀錄）
      ├─ 體驗規範 ← Yu5h1Lib.UX ModuleSpec/VisualFoundation 1.3.1 目標 1
      ├─ 被引用的消費端 → Button 2.0.0、OptionPicker 3.0.2（本套件各自的紀錄）
      └─ 驗證專案 → Yu5h1LibTest（Unity 6000.6），驗收時 manifest 暫時指向乾淨 worktree
```

## 身分

| 項目 | 內容 |
|---|---|
| 實作套件 | `com.yu5h1.uitoolkit`（`Packages/UIToolkit`）：`Yu5h1VisualFoundation.uss`（產生）、`VisualFoundationTheme`（範圍）、`Yu5h1Button.uss`、`Yu5h1OptionPicker.uss`、`Yu5h1FocusRing.png` |
| UX 規範 | [UX ModuleSpec/VisualFoundation.md](../../../../../../UX/Documentation/ModuleSpec/VisualFoundation.md) 1.3.1，SHA-256 `2a9d2881889686cf8adff3ff0b77e9e3e5b9648badeeb6f4a11a6fa826b5a0ec`（1.3.0 是 `7a95b79e…`，只差完成條件的人工 checklist 寫法） |
| 設計資料 | `VisualFoundation/visual-design.json` 合約 1.2.0（schemaVersion 1），SHA-256 `7d41c8287ef9aafe068280459f10cf906f616c803c530e16bb469843940b42c9`：light、dark 各 15 個角色，`focusMinWidth` 2 |
| 案例 | `VisualFoundation/visual-cases.json` 1.1.0，SHA-256 `06a9edb4e1d450c45718408d897e434cee093cfab248f97aba67eb4a3aaba807`（隨快照釘選；本輪色票由面板讀回，不經 adapter 執行中性案例，見〈未引用的部分〉） |
| UX commit | `879ca0f7e861b33674eef61e8ca28304f08260f4`（1.3.1；模組輪跑在 `855479e` 的 1.3.0），三個雜湊與 UX `manifest.json` 相同 |
| DX 基準 | 未釘選：本輪早於 DX 1.0.0 的首次驗證範圍（UX 第 81 題，DX §1） |
| 套件釘選 | 快照 `Tests/Editor/UX~/VisualFoundation/`（位元組與 UX 相同，`.gitattributes` 已有 `-text` 規則）；`VisualFoundationDesignTests` 核對雜湊與版本 |
| 範圍 | 目標 1 的三條情境：色票交付（VIS-PALETTE 與預設配色的 VIS-DERIVE）、Button 與 OptionPicker 的狀態外觀（VIS-STATE）、鍵盤焦點外框（VIS-FOCUS） |
| 環境 | Windows 11、Unity 6000.6.0f1 Editor、PlayMode（runtime 面板，無主題樣式表） |

## 引用的模組與缺口分析

VisualFoundation 本身不引用其他模組；它被 Button 與 OptionPicker 引用。

| 項目 | UX 有目標 | 本套件已有此版本的驗證 | 缺口的路由 |
|---|---|---|---|
| VisualFoundation 目標 1 | 有（1.3.0，第 94、95 題） | 無，本輪建立 | [UX-UNITYEXTENSION-2](../../../../../../UX/Documentation/Requirements.UnityExtension.md#ux-unityextension-2--webtheme-改讀-ux-的色票交付) |
| 實心 Button 靜止底色與外框、`solid` 面板外框、無底靜止外框、一般按下與選取後滑過 | 規格沒有指定角色 | — | [UNITYEXTENSION-UX-1](../../../../Documentation/Requirements.UX.md#unityextension-ux-1--button-與-optionpicker-面板還有幾個狀態沒有指定色彩角色)，2026-10-21 提出 |
| Icon（圖示著色） | 有 | 本套件 Button 紀錄 blocked | 不在目標 1；圖示不上色，見〈未引用的部分〉 |

## 實作對照

| UX 行為 | UI Toolkit 實作 | 說明 |
|---|---|---|
| 色票（VIS-PALETTE）、預設配色的推導（VIS-DERIVE） | `Yu5h1VisualFoundation.uss`：`.yu5h1-theme--light`、`.yu5h1-theme--dark` 各宣告 15 個 `--yu5h1-color-*` 與 `--yu5h1-focus-min-width` | 由 `VisualFoundationThemeGenerator`（選單 Tools/Yu5h1Lib/UIToolkit/Regenerate VisualFoundation Theme）自釘選的設計資料產生；推導結果已在設計資料裡，不重算。同一輸入產生相同位元組，檔頭記設計資料雜湊 |
| 明暗模式（VIS-SCHEME） | `VisualFoundationTheme.Apply(scope, Light｜Dark)`；沒有任何範圍的 OptionPicker 自己套 light | light、dark 固定；system 視為 light（沒有系統偏好來源，〈允許差異〉）。使用者的範圍元素決定整棵子樹的明暗 |
| 狀態外觀（VIS-STATE） | `Yu5h1Button.uss`、`Yu5h1OptionPicker.uss` | 一般文字 `itemText`；無底靜止外框 `controlBorder`；滑過底色 `hover`；選取與一般按下底色 `accent`、文字 `onAccent`；停用文字 `disabled`；`solid` 面板底色 `panelBackground` |
| 焦點外框（VIS-FOCUS） | `:focus` 時 `Yu5h1FocusRing.png`（16×16、環寬 2、九宮格切 6）以 `focus` 角色著色，畫在按鈕自己的範圍內 | USS 沒有 outline；用 `border` 會縮小內容區，圖示大小（size × 0.75）就不成立，所以用圖。環寬等於 `focusMinWidth`，由測試讀圖確認 |

## 本輪未引用的部分（缺口）

| 規範內容 | 本輪處理 |
|---|---|
| 實心 Button 靜止底色與外框 | blocked：角色沒有指定（UNITYEXTENSION-UX-1）；底色維持 Unity 預設主題變數。dark 頁上實心鈕靜止時淺灰底配淺色字，對比不足，已知 |
| `solid` 面板外框 | blocked：同上；外框維持 Unity 預設主題變數 |
| 一般按下、選取後滑過 | 暫行解讀（非規格）：一般按下同選取（`accent`／`onAccent`），選取後滑過維持選取；等 UX 回覆 |
| 無底靜止的外框 | 暫行解讀：照〈狀態對應的色彩角色〉字面畫 `controlBorder` 外框；Web demo 的無底靜止沒有外框，等 UX 確認 |
| 圖示顏色 | 不上色：`itemText` 對圖示的著色屬 Icon 模組，本套件 Button 紀錄已把 Icon 記為 blocked |
| 自訂配色（VIS-CUSTOM） | not run：沒有自訂配色的介面（目標 1 對照表），中性案例 `custom-*` 不經 adapter 執行 |
| 對比（VIS-CONTRAST） | 不是平台檢查：VC01 由 UX 執行；本套件色值來自同一份設計資料 |
| 系統明暗跟隨、執行中切換（VIS-SCHEME 的 system、VIS-SWITCH） | not run：不在目標 1；沒有系統偏好來源，也沒有消費端要求 |
| 原生部件（VIS-NATIVE） | 不適用：UI Toolkit 自繪，沒有原生部件 |
| 文字縮放（VIS-TEXT-SCALE） | 不適用：Unity 不套系統字級，縮放隨顯示 |
| 單位與量測（VIS-UNIT） | 沿用 Button 的 BTN-SIZE 量測；加入焦點外框後 `NativeButtonTests` 仍通過 |
| 中性案例 `visual-cases.json` | 隨快照釘選；色票由面板實際解析的值與設計資料逐角色比對（VP01），沒有為 `resolve`／`contrast` 操作另寫 adapter |

## 驗收對照

每項檢查的結果記在 [checklist](../../../../checklists/uitk-visualfoundation.checklist)，本文不重複；人工檢查在[一份 checklist](../../../../checklists/uitk-visualfoundation-vp03.checklist)，light、dark 是裡面的兩個工作項目（1.3.1：一頁一份）。

| UX 行為 | 檢查 | 結果所在 |
|---|---|---|
| 釘選與色票產生 | 快照雜湊、版本、產生結果與樣式表一致、樣式表只讀主題宣告的變數、焦點外框環寬、乾淨 checkout 編譯 | checklist 第 1 項 |
| VIS-PALETTE（VP01） | PlayMode 讀面板解析出的 15 個角色，light、dark 各一次 | checklist 第 2 項 |
| VIS-STATE、VIS-FOCUS（自動部分） | PlayMode 讀各狀態到達的角色與焦點外框 | checklist 第 3 項 |
| 回歸 | UIToolkit 既有 EditMode 與 PlayMode | checklist 第 4 項 |
| VIS-STATE、VIS-FOCUS（VP03，外觀） | 使用者在 `Samples~/UxAcceptance` 的 VisualFoundation 頁確認 light、dark | 一份人工 checklist |

驗收入口：Yu5h1LibTest 的 `Samples~/UxAcceptance`，模組下拉選「VisualFoundation (1.3.0) light」或「dark」（案例 `visualfoundation-light`、`visualfoundation-dark`，在 `Samples~/UxAcceptance/Cases/UxVisualFoundationDemoCase.cs` 註冊，沒有改動 coordinator 的分支）。

## 已知偏差

無已接受的偏差。blocked 與暫行解讀列在〈本輪未引用的部分〉，不算 pass。

## 輪次

| 規範版本 | UX commit | UnityExtension commit | checklist commit | 結論 |
|---|---|---|---|---|
| VisualFoundation 1.3.0 目標 1 | `855479e` | `603ec74`（已合併 `a6cfd29`） | 本紀錄所在提交 | 模組輪通過：乾淨 worktree EditMode 110/110、PlayMode 30/30；兩列 blocked |
| VisualFoundation 1.3.1 目標 1 | `879ca0f` | 重新釘選的提交（見 handoff） | 同上 | 只改完成條件的人工 checklist 寫法，行為與設計資料不變，不重跑模組輪；釘選測試 `VisualFoundationDesignTests` 在 Unity 重跑的結果見下 |

## 結論

狀態：模組輪通過、人工 checklist 已記為通過（2026-10-21）。1.3.1 重新釘選：只換快照與測試常數（雜湊 `2a9d2881…` 與 UX manifest 相同，版本字串 1.3.1），以 shell 核對雜湊；`VisualFoundationDesignTests` 在 Unity 的重跑待使用者離開 Play Mode 後補（目前編輯器在 Play 中，測試無法啟動）。在本輪範圍內（色票交付、Button 與 OptionPicker 的狀態角色、焦點外框），`603ec74` 符合 VisualFoundation 1.3.0 的自動可驗部分；這是自動檢查完成，不是驗收完成。目標 1 要達成，還需要 UX-UNITYEXTENSION-2 的完成條件：兩份人工 checklist 通過，Button 與 OptionPicker 紀錄的 blocked 列改為通過，而實心靜止底色的 blocked 要等 UNITYEXTENSION-UX-1。

驗收環境與做法（2026-10-21，由實作的 session 自己執行，不是未參與實作的 agent）：

- 乾淨 checkout：`git worktree add --detach ../UnityExtension-accept-vf 603ec74`，工作目錄乾淨，三個快照的 SHA-256 與 UX manifest 相同。
- 宿主：Yu5h1LibTest（Unity 6000.6.0f1，Windows 11）。備份 `Packages/manifest.json`（`efcafda0…`）與 `packages-lock.json`（`a2f1d8d6…`）後只把 `com.yu5h1.uitoolkit` 改指向 worktree，`Client.Resolve()` 並強制重新整理，`PackageInfo.resolvedPath` 是 worktree 路徑，Console 沒有編譯錯誤。驗收後還原 manifest、`Resolve()`，`resolvedPath` 回到主 checkout，lock 雜湊與備份相同；worktree 已移除。
- EditMode `Yu5h1Lib.UIToolkit.Editor.Tests`：110/110 通過（含新增 `VisualFoundationDesignTests` 9 項）。
- PlayMode `Yu5h1Lib.UIToolkit.Tests`：30/30 通過（含新增 `VisualFoundationTests` 6 項，`NativeButtonTests` 的 BT01、BT03 14 項）。
- 重新指向的輸入：套件路徑。未重新指向：Yu5h1LibTest 內 `Assets/Samples/…/UxAcceptance` 的 junction 仍指向主 checkout 的 `Samples~`（沒有本輪的案例，且不在被測相依鏈內）。
- 觀察到的既有狀況：重新整理時 Unity 對 `Yu5h1OptionPicker.uss.meta` 回報 “version 1, below the supported minimum (2)”（主線本來就有的舊格式 meta，與本輪無關）。套件路徑改回主 checkout 後，Unity 在主 checkout 把 7 個 OptionPicker 相關 `.meta` 改成精簡格式、尚未提交，我沒有碰。

審查備註（不違反規範，不影響結果）：

- 實作者在暫時的截圖測試（已刪除，僅供除錯，不算驗收）裡看到：dark 下實心鈕靜止是淺灰底配淺色字，確實不協調，這就是 UNITYEXTENSION-UX-1 要補的；無底鈕的 `controlBorder` 外框、選取的 `accent`／`onAccent`、焦點外框的形狀都如預期。
- 狀態測試是讀 resolvedStyle 的角色，焦點外框是圖，不能讀成 `border-width`；環寬由 `FocusRingIsAtLeastFocusMinWidthThick` 讀圖確認，完整包圍與不被蓋住要靠使用者看。
- light 的 `hover` 色（`#efeee8`）與頁面底色（`#f5f3ec`）很接近，滑過在頁面上幾乎看不出來；這是設計資料的值，不是實作問題，使用者看時留意，若不夠明顯屬規格問題。
