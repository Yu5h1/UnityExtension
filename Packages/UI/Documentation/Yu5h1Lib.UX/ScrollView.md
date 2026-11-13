# ScrollView 來源基線紀錄（Yu5h1Lib.UI）

> 這是 UX [ScrollView](../../../../../../UX/Documentation/ModuleSpec/Collections/ScrollView.md) 的 uGUI 來源基線：規範開頭把本套件的 `Runtime/Extension/ScrollRectEx.cs` 列為收斂來源之一。本紀錄依 [spec-author〈Make acceptance portable〉](../../../../../../.agents/skills/spec-author/SKILL.md#make-acceptance-portable) 於 2026-11-10 補建，不是另一輪驗收，也不是 uGUI 聊天面板的實作紀錄（那是之後 UI 套件實作 ChatPanel 訊息區時的事）。目標 1 的主要來源是 UI Toolkit 的聊天呈現，見 [UIToolkit 的 ScrollView 紀錄](../../../UIToolkit/Documentation/Yu5h1Lib.UX/ScrollView.md)。
>
> **本紀錄只證明規範符合來源**，不證明其他實作通過，也不代表其他消費端需要每一條行為。

## 身分

| 項目 | 內容 |
|---|---|
| 來源套件 | `com.yu5h1.ui`（`Packages/UI`），`ScrollRectEx.cs` 最後變更 `e494fad`（2025-10-14） |
| UX 規範 | [ScrollView.md](../../../../../../UX/Documentation/ModuleSpec/Collections/ScrollView.md) 0.2.1-draft，SHA-256 `4d3702cc8da9f8595342196ac592933c718e056fa87679ce43d62f8a47819b52` |
| 設計資料／中性案例 | `scroll-view-design.json` 0.2.0-draft `45cd604c41a817542c9f23a17c626bf48c67d6ed04d70a066ec216cc2d503e8f`；`scroll-view-cases.json` 0.2.0-draft `11a52eef37938a94f8ede12e8182e4b345702eb2a4e211f2e192734826bf9b0f` |
| UX commit | `618d39ec1709dc214cb81c1314c3eb4636d2ea41` |
| 範圍 | ScrollView 目標 1 的六條行為 |
| 測試證據 | 沒有：`ScrollRectEx` 沒有自動化測試 |
| 中性案例執行 | 沒有：本套件沒有 ScrollView 案例 adapter |
| DX | 不適用：來源基線，不是組裝驗收 |

## 實作對照與基線結果

`ScrollRectEx` 是 uGUI `ScrollRect` 的擴充方法：`ScrollToPosition`／`ScrollToLocalPosition` 把指定位置捲進可視範圍，並依內容的 pivot（底端或頂端）把位置夾在 `[0, maxScroll]` 對應的範圍內；本套件的 `InputFieldAddon` 用它讓游標所在行保持可見（屬 ScrollView 後續候選的捲入視野，不在目標 1）。

| UX 行為 | 來源實作 | 基線結果 |
|---|---|---|
| 溢位與夾限（SCROLL-OVERFLOW） | `ScrollToLocalPosition`：`maxY = max(0, contentSize.y − viewportSize.y)`，程式捲動的位置夾在範圍內，底端 pivot 時範圍為 `[−maxY, 0]`；使用者捲動的夾限由 uGUI 原生 `ScrollRect` 負責 | 程式捲動的夾限：未測（無測試）；捲軸隱藏與使用者捲動：屬原生，未測 |
| 短內容貼底（SCROLL-BOTTOM-ANCHOR） | 無 | 未實作 |
| 跟到最新（SCROLL-FOLLOW） | 無（`InputFieldAddon.ScrollToBottom` 是一次性的程式捲動，不是跟隨） | 未實作 |
| 使用者捲動停止跟隨（SCROLL-FOLLOW-STOP） | 無 | 未實作 |
| 保留閱讀位置（SCROLL-KEEP-POSITION） | 無；一般 `ScrollRect` 在內容尾端增加時由原生保留位置 | 未實作（依原生行為，未測） |
| 恢復跟隨（SCROLL-FOLLOW-RESUME） | 無 | 未實作 |

## 輪次

| 規範版本 | UX commit | 範圍 | 實作 commit | Checklist commit | 結論 |
|---|---|---|---|---|---|
| ScrollView 0.2.1-draft | `618d39e` | 來源基線（補建） | `e494fad`（`ScrollRectEx.cs` 最後變更） | 無 | 見下 |

## 結論

2026-11-10 來源基線（補建）：`ScrollRectEx` 只對應程式捲動時的位置夾限，且沒有測試；目標 1 的其他五條在本套件沒有實作。VCP 現有的 uGUI 聊天清單（`LoopScrollRect`）不屬本套件，它的捲動問題由 ChatPanel 目標 1 的 uGUI 實作取代。這些結果只證明規範與來源的對應。

## uGUI 訊息區採用

ChatPanel sample 使用 `Yu5h1Lib.UI.ScrollView` 採用 ScrollView 0.2.2-draft。原生 `ScrollRect` 處理拖曳、滾輪、夾限與慣性。ChatPanel 選用 `ContentUpdateBehavior.FollowLatest`，直接子項使用穩定名稱，變更資料後呼叫 `RefreshContent`。`ScrollViewScrollbar` 會將捲軸操作通知視窗。

[ChatPanel 採用紀錄](ChatPanel.md#釘選版本)保存本輪釘選版本與 VCP batchmode 證據。[驗收清單](../../../../checklists/ui-chatpanel.checklist#2-驗證訊息區與-scrollview)記錄目標 1 的自動與人工檢查。

## 單軸循環與項目重用

同一個 `ScrollView` 使用原生 `horizontal` 或 `vertical` 設定方向。以 `SetItems(prefab, count, size, bind)` 提供固定尺寸的項目，元件依可視範圍使用 Core `Recycler<RectTransform>` 借還實例。`Loop` 開啟且內容溢位時可以跨過首尾，捲軸對應本圈位置。有限清單可選擇 `PreservePosition` 或 `FollowLatest`，循環啟用時暫停跟隨。虛擬清單可使用 `ScrollViewScrollbar`，將其可操作區放在視窗邊緣，指標進入該區、拖曳或鍵盤聚焦時顯示。

2026-11-13 在 Yu5h1LibTest（Unity 6000.6.0f1）跑定向驗收。EditMode 32/32，涵蓋兩軸的滾輪與拖曳跨圈、滑塊定位、靠邊顯示、關閉循環、同一批 prefab 實例重用，以及原有 ChatPanel 案例。既有 ChatPanel sample 的 PlayMode 案例 1/1 通過，驗證歷史載入維持閱讀位置及手動捲回底端後恢復跟隨。
