# Requirements — UX

- **目標專案（Target）**：Yu5h1Lib.UX，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\UX`。
- **提出專案（Requester）**：UnityExtension，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension`。
- **目前依賴方式**：Unity 實作套件依 referenced-module-acceptance 釘選 UX 的模組規範快照，見[跨平台 UI 一致性 § 引用模式](跨平台UI一致性.md#引用模式)；規格缺口只走本檔，不在實作端補寫規則。
- **未結案需求數**：1。
- **提出日期**：2026-10-21。
- **驗收責任**：UX 改規格；UnityExtension 依新版重跑對應檢查，通過後更新本檔狀態。

## UNITYEXTENSION-UX-1 — Button 與 OptionPicker 面板還有幾個狀態沒有指定色彩角色

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-21 提出，來自 UX-UNITYEXTENSION-2 的 VisualFoundation 目標 1 引用）。UX 已於同日在 VisualFoundation 1.4.0、Button 2.1.0、OptionPicker 3.1.0 補齊四點（UX `5205838`）；UnityExtension 已依新版實作並重跑自動檢查（`7abbe0f`，未合併），人工確認 light、dark 頁的實心靜止鈕後改 Implemented |
| **Requirement** | 在 [VisualFoundation](../../../UX/Documentation/ModuleSpec/VisualFoundation.md) 的〈狀態對應的色彩角色〉（或 Button、OptionPicker 規格）補上下列狀態用的角色：(1) `solid` Button 靜止時的底色與外框；(2) OptionPicker `solid` 面板的外框；(3) 沒有填色（`ghost`）的 Button 靜止時是否有 `controlBorder` 外框；(4) 一般「按下中」（滑鼠按住、不是切換開著）與「選取且滑過」兩種疊加狀態的底色。 |
| **Evidence** | (1) Button 的 BTN-APPEARANCE 只寫「`solid`：靜止時有底色……顏色依 VisualFoundation 的角色」，〈狀態對應的色彩角色〉沒有任何一列說實心按鈕靜止時用哪個角色；〈需要外框才能辨識的控制項〉一列只舉「沒有填色的按鈕」為例。(2) OPT-APPEARANCE 寫 `solid` 面板有 `panelBackground`、「外框與陰影」，沒說外框用 `border` 還是 `controlBorder`。(3) 同一列「沒有填色的按鈕」字面上包含 `ghost` 靜止；Web 的 demo（`demo/option-picker/demo.css`）`ghost` 靜止的外框是透明，與這一列不同，UnityExtension 先照字面畫 `controlBorder` 外框，等 UX 確認。(4) 〈狀態對應的色彩角色〉有「選取、按下中的切換、開啟，以及主要動作」一列，沒有一般按下、也沒有選取後滑過。參考：Web demo 的 `solid` 是 `--ui-surface`（`panelBackground`）底加 1px `--ui-control-border`（`controlBorder`），面板外框是 `--ui-border`（`border`）；這只是證據，不是規格。Web 的選取是 `--ui-border` 底加 focus 色字，也與表中的 `accent`／`onAccent` 不同。 |
| **Acceptance** | 規格寫明上列四點各用哪個角色（或寫明由模組自訂並給出理由），VisualFoundation 的 VP03 與 Button、OptionPicker 的外觀檢查涵蓋這些列。UnityExtension 依新版釘選重跑 `uitk-visualfoundation` 輪，並讓 light、dark 頁的實心靜止鈕通過使用者確認。 |
| **Impact** | Yu5h1Lib.UIToolkit 的 Button 與 OptionPicker（以及之後的 Toggle 等引用 Button 外觀的控件）；uGUI 之後引用時同一缺口。dark 主題下實心鈕靜止的底色與文字對比目前不成立。 |
| **Workaround** | 實心鈕靜止底色與 `solid` 面板外框維持 Unity 預設主題變數（`--unity-colors-button-background`、`--unity-colors-default-border`），驗收紀錄標為 blocked；一般按下用與選取相同的 `accent`／`onAccent`，選取後滑過維持選取外觀，都標為 UnityExtension 暫行解讀，不是規格。 |
