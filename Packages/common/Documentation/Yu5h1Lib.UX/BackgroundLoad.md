# BackgroundLoad 驗收紀錄（Yu5h1Lib.Common）

> Task ID: common-backgroundload
>
> 狀態：模組輪通過，demo 的人工檢查還沒跑（2026-10-30）。實作從 Yu5h1Lib.Addressables 移到 Yu5h1Lib.Common，依據是使用者 2026-10-30 的決定：背景讀取的規則和讀取機制無關，和呈現技術也無關。規範擁有的行為改在本套件驗收；VCP 只驗它自己接的部分，見 VCP `Documentation/Yu5h1Lib.UX/BackgroundLoad.md`。

```text
UnityExtension
└─ Yu5h1Lib.Common（實作套件，Packages/common）
   └─ BackgroundLoad 目標 1（本紀錄）
      ├─ 體驗規範 ← Yu5h1Lib.UX ModuleSpec/Foundation/BackgroundLoad 0.1.2-draft
      ├─ 規則 → ContentSlot（位置的狀態與決定）
      ├─ 推導 → ContentSlotPresentation（指示、替代畫面、提示要顯示什麼；和 UI 技術無關）
      ├─ 場景接線 → ContentSlotView（把推導結果送到 UnityEvent，供 uGUI 在 Inspector 接線）
      ├─ 呈現與人工檢查 → Yu5h1Lib.UI 的 Samples~/UxAcceptance，案例 backgroundload（模擬讀取器）
      ├─ 真實下載的進度 → Yu5h1Lib.Addressables 的 AddressablesLoader（RemoteLoadTests 驗）
      └─ 驗證專案 → Yu5h1LibTest（Unity 6000.6）
```

## 身分

| 項目 | 內容 |
|---|---|
| 實作 | [`ContentSlot.cs`](../../Runtime/Content/ContentSlot.cs)、[`ContentSlotPresentation.cs`](../../Runtime/Content/ContentSlotPresentation.cs)、[`ContentSlotView.cs`](../../Runtime/Content/ContentSlotView.cs)，命名空間 `Yu5h1Lib`。前兩個檔案沿用原本的 `.meta`，GUID 不變，VCP 場景的引用不受影響 |
| UX 規範 | `UX/Documentation/ModuleSpec/Foundation/BackgroundLoad.md` 0.1.2-draft，UX commit `2b626c7`，SHA-256 `371eb2142d374ecf59ed6f71ea9dd0e6c0b541a56c4b9945d404a0f64f7a29a2`；設計資料 0.1.1-draft `13387901…`；案例 0.1.1-draft（15 個）`b6f4f74f…` |
| 套件釘選 | 快照 `Packages/common/Tests/Editor/UX~/BackgroundLoad`（`.gitattributes` `-text`），由 `BackgroundLoadCaseTests.SnapshotMatchesThePinnedHashes` 核對 |
| UI Toolkit | 目前沒有使用端。要用時，在 Yu5h1Lib.UIToolkit 寫一個 binder 讀取 `ContentSlotPresentation`，推導規則不必重寫（使用者 2026-10-30） |
| 環境 | Windows 11、Unity 6000.6.0f1、Yu5h1LibTest（UnityExtension 以 `file:` 引用，UI 範例以 junction 掛入） |

## 驗收對照

| 行為 | 自動（EditMode） | 人工（demo） |
|---|---|---|
| BGL-NOBLOCK | nonblocking-during-load（只比對 slot 看得到的欄位） | 第 2 項：讀取中打字、捲動清單 |
| BGL-PREPARE | prepare-first-then-rest、same-content-shares-load | 第 1 項：開啟就讀 A，其餘依序讀 |
| BGL-SWAP | background-ready-no-swap、latest-selection-wins、reselect-ready-during-wait | 第 3、6 項 |
| BGL-PROGRESS | progress-known-percent、progress-unknown、progress-preparing、indicator-hides-when-done、indicator-follows-selected；`ContentSlotPresentationTests` 驗三種文字由宿主決定 | 第 1、4 項 |
| BGL-PLACEHOLDER | select-not-ready-shows-placeholder、placeholder-replaced-when-ready；`ContentSlotPresentationTests` 驗等待時替代畫面帶進度 | 第 3、7 項 |
| BGL-FALLBACK | selected-fail-fallback、background-fail-silent；`ContentSlotPresentationTests` 驗備用內容不帶進度、提示出現 | 第 5、8 項 |

demo 的捲動清單裡，每張圖都是只放一份內容的位置。「進入畫面就讀」「捲出畫面仍讀完」「清單不顯示角落指示」都是 demo 這個宿主自己的選擇，不是規範的規則：規範把讀什麼、何時讀交給宿主，也不涵蓋多份內容的合計進度。這個邊界由使用者 2026-10-30 決定（選項 A）；等有實際的使用端時，再到 UX 開目標 2。

## 輪次

| 規範版本 | 範圍 | checklist | 結論 |
|---|---|---|---|
| 0.1.2-draft | 目標 1：從 Addressables 移入 common、抽出推導、建立 UI demo | [模組輪](../../../../checklists/common-backgroundload.checklist)、[demo 人工檢查](../../../../checklists/common-backgroundload-demo.checklist) | 模組輪通過：Yu5h1LibTest 以 unityMCP 跑 EditMode，`Yu5h1Lib.Common.Editor.Tests` 與 `Yu5h1Lib.Addressables.Editor.Tests` 共 148 項全部通過（其中 BackgroundLoad 21 項）。demo 未在編輯器開過，因為編輯器當時開著別人未存的場景；由使用者執行人工檢查 |

外觀是暫代的白字，加上黑色 35% 半透明面板，等 VisualFoundation 的 uGUI 結果出來再換。
