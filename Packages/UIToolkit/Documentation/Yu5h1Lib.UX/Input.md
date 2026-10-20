# Input / HoldManipulator 驗收紀錄（Yu5h1Lib.UIToolkit）

> Task ID: uitk-input
>
> 狀態：completed（待使用者視覺與手感確認）（2026-10-18，第一輪：Input 3.0.1）。依跨平台 `HoldManipulator` 驗收計畫，在 `Yu5h1Lib.UIToolkit` 實作 [`HoldManipulator : PointerManipulator`](../../Runtime/Gesture/HoldManipulator.cs) 對齊 UX `Foundation/Input` 3.0.1 之跨平台長按與按住滑選操作器契約，並重構 [`OptionPicker`](../../Runtime/WebUI/OptionPicker/OptionPicker.cs) 直接組合 `HoldManipulator`，同時於統一驗收入口 `UxAcceptanceHost` 註冊 `input` 互動案例。

```text
UnityExtension
└─ Yu5h1Lib.UIToolkit（實作套件）
   └─ Input / HoldManipulator（本紀錄）
      ├─ 體驗規範 ← Yu5h1Lib.UX ModuleSpec/Foundation/Input 3.0.1（INPUT-PRESS-TAP、INPUT-PRESS-HOLD、INPUT-PRESS-SUPPRESS、INPUT-DRAG-START、INPUT-CHOICE-HOLD、INPUT-CANCEL、INPUT-LIFECYCLE）
      ├─ 宿主採用 → OptionPicker（觸發鈕組合 HoldManipulator）
      └─ 驗證專案 → Yu5h1LibTest（EditMode 測試 + Samples~/UxAcceptance 統一入口 "input"）
```

## 身分

| 項目 | 內容 |
|---|---|
| 實作套件 | `com.yu5h1.uitoolkit`（`Packages/UIToolkit`），公開類別 `Yu5h1Lib.UIToolkit.HoldManipulator`（`Runtime/Gesture/HoldManipulator.cs`） |
| UX 規範 | [UX ModuleSpec/Foundation/Input.md](../../../../../../UX/Documentation/ModuleSpec/Foundation/Input.md) 3.0.1，SHA-256 `99583d7c704d8c586068e49e7c4e74b093bb4e3c241685aa73cf23a30ef88bcb` |
| 設計資料 | `Input/input-design.json` 2.0.0，SHA-256 `b31fb027abd5fad8a506bda2caa8ff35e1ece6813d61e6773e1ddf6ba5699d66`：`holdDuration` 0.4s（400ms）、`dragThreshold` 6px |
| 中性案例 | `Input/input-cases.json` 2.0.0，SHA-256 `203344648e246c75349444e07f3a0a36806610270140b901f6b2cb6bd9e19188` |
| UX commit | `c7c5438b112a660b499dde27f9b019bfa5913ef1` |
| 套件釘選 | 快照 `Tests/Editor/UX~/Input/`（`Input.md`、`input-design.json`、`input-cases.json`）；`HoldManipulatorTests` 核對雜湊與參數預設值 |
| 範圍 | 目標 1（跨平台長按與按住滑選操作器 `HoldManipulator`）：`INPUT-PRESS-TAP`、`INPUT-PRESS-HOLD`、`INPUT-PRESS-SUPPRESS`、`INPUT-DRAG-START`（容許量邊界）、`INPUT-CHOICE-HOLD`、`INPUT-CANCEL`、`INPUT-LIFECYCLE` |
| 環境 | Windows 11、Unity 6000.6.0f1 Editor |

## 實作對照

| UX 行為 | UI Toolkit 實作 | 說明 |
|---|---|---|
| 公開入口 | `HoldManipulator : PointerManipulator`（`Runtime/Gesture/HoldManipulator.cs`） | 暴露 `HoldDurationMs`（預設 400）、`DragThreshold`（預設 6f）、`CaptureOnHold`、`SuppressClickOnHold`、`CanStartHold`、`IsPressed`、`IsHolding` 與完整事件生命週期 |
| INPUT-PRESS-TAP | `TapTriggered`、`IsInsideHalfOpenRect` | 短按未超門檻且放開點在目標半開區間 `[xMin, xMax) × [yMin, yMax)`（含左、上緣，不含右、下緣）時觸發 `TapTriggered`；非主鍵忽略 |
| INPUT-PRESS-HOLD | `HoldStarted`、`HoldTriggered`、`target.schedule` | 主指標按下滿 `HoldDurationMs`（400ms）且局部邏輯像素位移 `<= DragThreshold`（6px）時觸發一次 `HoldTriggered`，並將 `IsHolding` 設為 `true` |
| INPUT-PRESS-SUPPRESS | `OnClick` + `StopImmediatePropagation` | 長按成立、移動超門檻取消、範圍外放開或中斷後，自動攔截並吞掉同一次按壓產生的原生 `ClickEvent` 與冒泡 |
| INPUT-DRAG-START（容許量） | `OnPointerMove` 局部座標距離判定 | 以 `target.WorldToLocal` 換算局部邏輯像素，位移 `= 6px` 仍觸發長按，位移 `> 6px` 立即取消待定長按並觸發 `HoldCanceled` |
| INPUT-CHOICE-HOLD | `CapturePointer` + `HoldMoved` + `HoldEnded` | 長按成立時在目標元素捕獲指標，後續移動持續發出 `HoldMoved(screenPos)` 供宿主（如 `OptionPicker`）以 `panel.Pick`／`worldBound` 更新懸停高亮，放開時發出 `HoldEnded(screenPos)` 並釋放捕獲 |
| INPUT-CANCEL / INPUT-LIFECYCLE | `OnPointerDown`（第二指標）、`OnKeyDown(Escape)`、`OnPointerCancel`、`OnDetachFromPanel` | 第二指標按下、Escape、系統取消或元素卸載時，中止計時器、釋放捕獲並發出 `HoldCanceled` |

## 驗收對照

每項檢查的結果記在 [checklist](../../../../checklists/uitk-input.checklist)，本文不重複。

| UX 行為 | 檢查 | 結果所在 |
|---|---|---|
| 釘選 | 快照雜湊、設計參數（400ms / 6px）核對 | checklist 第 1 項 |
| 短按與長按抑制 | INPUT-PRESS-TAP 半開邊界、INPUT-PRESS-HOLD、INPUT-PRESS-SUPPRESS | checklist 第 2 項 |
| 移動容許量與按住滑選 | INPUT-DRAG-START（`=6px` vs `>6px`）、INPUT-CHOICE-HOLD 滑選生命週期 | checklist 第 3 項 |
| 中斷、生命週期與 OptionPicker 整合 | INPUT-CANCEL、INPUT-LIFECYCLE、`OptionPicker.TriggerHoldManipulator` | checklist 第 4 項 |
| 統一入口與手感確認 | `UxAcceptanceHost` 註冊 `input` 案例，供使用者體驗 | checklist 第 5 項 |

## 輪次

| 規範版本 | UX commit | 結論 |
|---|---|---|
| Input 3.0.1 | `c7c5438` | `HoldManipulatorTests` 與全套件 EditMode 自動化測試通過，並重構 `OptionPicker` 共享 `HoldManipulator` |
