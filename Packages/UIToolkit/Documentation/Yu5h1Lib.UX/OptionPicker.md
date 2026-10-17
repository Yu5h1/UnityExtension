# OptionPicker 驗收紀錄（Yu5h1Lib.UIToolkit）

> Task ID: uitk-optionpicker
>
> 狀態：in-progress（2026-10-17，第一輪：OptionPicker 3.0.2）。依使用者指示「在 Yu5h1Lib.UIToolkit 引用 UX 的 OptionPicker」，流程見 referenced-module-acceptance。實作方式採「原生 Button 與 VisualElement 面板搭配 C# 控制邏輯」（原生優先原則）。觸發鈕與選項直接沿用已採用的 Button 2.0.0，C# 控制邏輯負責 interaction 模式（picker/cycle/both）、400ms 長按手勢、aligned/adjacent 面板定位與方向鍵/Escape 導覽。自動檢查涵蓋數值與規則邏輯，手感與外觀交由使用者直接確認。

```text
UnityExtension
└─ Yu5h1Lib.UIToolkit（實作套件）
   └─ OptionPicker（本紀錄）
      ├─ 體驗規範 ← Yu5h1Lib.UX ModuleSpec/Controls/OptionPicker（OPT-VALUE、OPT-OPTIONS、OPT-OPEN、OPT-CYCLE、OPT-PLACE、OPT-DISPLAY、OPT-APPEARANCE、OPT-ICON）
      ├─ 依賴模組 ← Button 2.0.0（Yu5h1Button.uss）、Input 3.0.0（INPUT-CHOICE-HOLD、INPUT-CHOICE-KEY、INPUT-CHOICE-CLOSE）
      └─ 驗證專案 → Yu5h1LibTest（Unity 6000.6），驗收時 manifest 暫時指向乾淨 worktree
```

## 身分

| 項目 | 內容 |
|---|---|
| 實作套件 | `com.yu5h1.uitoolkit`（`Packages/UIToolkit`）。採用原生 `Button` + `Yu5h1Button.uss`、原生 `VisualElement` 面板 + `Yu5h1OptionPicker.uss` 與 C# 控制邏輯 |
| UX 規範 | [UX ModuleSpec/Controls/OptionPicker.md](../../../../../../UX/Documentation/ModuleSpec/Controls/OptionPicker.md) 3.0.2，SHA-256 `31591500f70d9738be80ca87fd75b130a17ee6136b1e81fc830c1d3c2e9a14f2` |
| 設計資料 | `OptionPicker/option-picker-design.json` 2.0.0，SHA-256 `551517bc380470057011e41ca7e8366af7dade1f432e3a1cd60134a629417e5a`：`pickerGap` 4、`pickerViewportMargin` 4、`panelPadding` 4、`optionGap` 4 |
| 中性案例 | `OptionPicker/option-picker-cases.json` 1.0.0，SHA-256 `3d4d2a26a63e603267ac360f0f2a3ee6c8893a7bfb5e924b87b96dac0b58ccd4` |
| UX commit | `8c1f764e22dc54960558c6bc81b364c31d3760b5` |
| 依賴模組 | `Button` 2.0.0（已引用，`Yu5h1Button.uss`）、`Input` 3.0.0（`holdDuration` 400ms） |
| 套件釘選 | 快照 `Tests/Editor/UX~/OptionPicker/`（`OptionPicker.md`、`option-picker-design.json`、`option-picker-cases.json`）；`OptionPickerDesignTests` 核對雜湊與數值 |
| 範圍 | OPT-VALUE、OPT-OPTIONS、OPT-OPEN、OPT-CYCLE、OPT-PLACE、OPT-DISPLAY、OPT-APPEARANCE、OPT-ICON 全部 |
| 環境 | Windows 11、Unity 6000.6.0f1 Editor、PlayMode |

## 本輪未引用的部分（缺口）

| 規範內容 | 依賴 | 本輪處理 |
|---|---|---|
| OPT-APPEARANCE「顏色依 VisualFoundation 的角色」 | VisualFoundation | blocked：UIToolkit 沒有 VisualFoundation 色票；本輪取 Unity 預設主題變數或對應底色 |
| 觸發鈕與選項「顯示設定不改變無障礙名稱」 | 操作 INPUT-NAME | blocked：UI Toolkit 元素無對應 `UnityEngine.Accessibility` 之完整無障礙名稱 |
| 手感與外觀確認 | — | 手感與外觀交付使用者在 demo 中直接確認（OP02、OP05） |

## 實作對照

| UX 行為 | UI Toolkit 實作 | 說明 |
|---|---|---|
| 公開入口 | `OptionPicker` / `OptionPickerModel` + `Resources/Yu5h1Lib.UIToolkit/Yu5h1OptionPicker.uss` | 觸發鈕與選項使用原生 `Button` + `Yu5h1Button.uss`，面板使用 `VisualElement` + `Yu5h1OptionPicker.uss` |
| OPT-VALUE | `OptionPicker.Choose` / `OnChosen` 回報 | 選取後發出回報，不自行修改目前值，下次開啟依新值或原值 |
| OPT-OPTIONS | `OptionPicker.SetOptions` 清單檢驗 | 唯一 ID 檢核、停用選項跳過、無可用選項時不開啟/不循環 |
| OPT-OPEN | `interaction`：`picker`、`cycle`、`both` | 短按、長按（400ms）、方向鍵開啟行為依模式分流 |
| OPT-CYCLE | `OptionPicker.CycleNext` | 依序切到下一個可用項目，末項循環回首項 |
| OPT-PLACE | `OptionPickerPlacement` 計算 | 對齊式：選中項中心對準觸發鈕；相鄰式：間距 4px，水平/垂直超出翻轉，限制在視窗 4px 邊界內 |
| OPT-DISPLAY | 原生 `Button` 樣式 + USS `padding: 4px; gap: 4px;` | 觸發鈕與選項套用 Button 2.0.0 樣式；面板內距 4px、選項間距 4px |
| OPT-APPEARANCE | `.yu5h1-optionpicker__panel--solid` / `--ghost` | solid 有底色、外框與陰影；ghost 無底色外框 |
| OPT-ICON | `button.iconImage` 支援 | 觸發鈕與選項套用宿主提供之圖示 |

## 驗收對照

每項檢查的結果記在 [checklist](../../../../checklists/uitk-optionpicker.checklist)，本文不重複。

| UX 行為 | 檢查 | 結果所在 |
|---|---|---|
| 釘選 | 快照雜湊、合約版本與數值核對、乾淨 checkout 編譯 | checklist 第 1 項 |
| 選擇與選項 | OPT-VALUE、OPT-OPTIONS、OPT-CYCLE 案例 | checklist 第 2 項 |
| 模式與手勢 | OPT-OPEN、INPUT-CHOICE-HOLD、KEY、CLOSE 案例 | checklist 第 3 項 |
| 面板定位 | OPT-PLACE 對齊與相鄰翻轉案例 | checklist 第 4 項 |
| 顯示與外觀 | OPT-DISPLAY、OPT-APPEARANCE、OPT-ICON 排版量測 | checklist 第 5 項 |
| 手感與外觀 | 使用者在範例中確認手感與外觀（OP02、OP03、OP05） | checklist 第 6 項 |
| 回歸 | UIToolkit 既有測試（含 ButtonDesignTests） | checklist 第 7 項 |

## 已知偏差

無。

## 輪次

| 規範版本 | UX commit | UnityExtension commit | checklist commit | 結論 |
|---|---|---|---|---|
| OptionPicker 3.0.2 | `8c1f764` | （進行中） | （進行中） | 依原生 Button + VisualElement 面板與 C# 控制邏輯進行引用 |

## 結論

（待本輪執行與使用者確認完成後填寫）
