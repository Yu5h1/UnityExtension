# Agent 可執行項目

> Task ID: agent-executable-items
>
> 狀態：進行中。進度由報表卡片「Agent 可執行項目」（scope `unity-extension`）擁有；每個項目的驗收證據由它綁定的 checklist 擁有。

使用者交給 agent 的指示，一條指示就是卡片上的一個子項目，一個子項目綁一份 `checklists/<指示 ID>.checklist` 作驗收證據。checklist 全部打勾，報表就把該項目顯示為完成；卡片的完成狀態由子項目彙總。

## 寫法

- 每條指示在本文有一節：標題是指示，標題下一行宣告 `> Task ID: <指示 ID>`，內容寫要達成的結果與驗收條件。checklist 的 `Current round` 指向這一節。
- 指示必須是開發結果（程式、設定、文件、驗證），checklist 的檢查項才有東西可證明。commit、push、handoff 更新這類收尾工作不當指示。
- 每次都要做的規則（例如 commit 前確認分支）不當指示，因為 checklist 一輪只跑一次；這類規則寫進 skill。
- 指示若屬於既有需求卡片（例如 Configuration 的驗收），子項目放在那張卡片之下，不放這裡，證據才不會散在兩張卡。
- 需要使用者的螢幕或判斷的檢查，另開人工 checklist 與自己的子項目，依 [checklist-round](../../../.agents/skills/checklist-round/SKILL.md#manual-checklists)。

## 驗證入口 view 為載入複本的回歸修正

> Task ID: preferences-entry-view-copy

2026-10-20 統一入口 `Preferences` 曾把 storage 載入的物件直接當成共用 view，VCP 的 `UserDataView` 交出自己保存的紀錄物件，存回去時對同一物件先清空再複製，把 `Users` 寫成空紀錄。修正已在 `master`（`52ada54`）：入口的 view 改回載入結果的複本，`KeyValues.CopyFrom` 對自己不動作；契約寫在 [偏好設定 § 擴充點](偏好設定.md#擴充點)。

要達成的結果：修正附帶的兩項 EditMode 測試在 Yu5h1LibTest 跑過，`Yu5h1Lib.Common.Editor.Tests` 整組沒有退步。

驗收條件：

- `ViewIsACopyOfTheLoadedObject_SoSavingBackIntoItKeepsTheRecord` 與 `CopyFromItselfOrItsOwnEntriesKeepsTheEntries` 通過。
- `Yu5h1Lib.Common.Editor.Tests` 全部通過，沒有編譯錯誤。
- VCP 登入後帳號紀錄是否保住屬使用者在 VCP 專案確認，不在這一輪。
