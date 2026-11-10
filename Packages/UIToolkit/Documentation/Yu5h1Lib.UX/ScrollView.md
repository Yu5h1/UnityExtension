# ScrollView 來源基線紀錄（Yu5h1Lib.UIToolkit）

> 這是 UX [ScrollView](../../../../../../UX/Documentation/ModuleSpec/Collections/ScrollView.md) 目標 1 的來源基線：目標 1 的貼底與跟隨收斂自本套件聊天呈現 `Runtime/Chat/ChatPresenter.cs`。本紀錄依 [spec-author〈Make acceptance portable〉](../../../../../../.agents/skills/spec-author/SKILL.md#make-acceptance-portable) 於 2026-11-10 補建，不是另一輪驗收。uGUI 來源 `ScrollRectEx` 的基線另見 [UI 套件的 ScrollView 紀錄](../../../UI/Documentation/Yu5h1Lib.UX/ScrollView.md)。
>
> **本紀錄只證明規範符合來源**，不證明其他實作（Web、uGUI）通過，也不代表其他消費端需要每一條行為。

```text
UnityExtension
└─ Yu5h1Lib.UIToolkit（來源套件）
   └─ ChatPresenter 的捲動部分（本紀錄）
      ├─ 規範 ← Yu5h1Lib.UX ModuleSpec/Collections/ScrollView 0.2.1-draft 目標 1
      ├─ 實作 → Runtime/Chat/ChatPresenter.cs（UpdateBottomAnchor、FollowBottom、Tick、滾輪與拖曳監聽）＋ UI Toolkit 原生 ScrollView
      └─ 測試 → Tests/Runtime/ChatPresenterScrollTests（PlayMode）
```

## 身分

| 項目 | 內容 |
|---|---|
| 來源套件 | `com.yu5h1.uitoolkit`（`Packages/UIToolkit`），`ChatPresenter.cs` 最後變更 `674b3f9`（2026-10-01） |
| UX 規範 | [ScrollView.md](../../../../../../UX/Documentation/ModuleSpec/Collections/ScrollView.md) 0.2.1-draft，SHA-256 `4d3702cc8da9f8595342196ac592933c718e056fa87679ce43d62f8a47819b52` |
| 設計資料 | `ScrollView/scroll-view-design.json` 0.2.0-draft，SHA-256 `45cd604c41a817542c9f23a17c626bf48c67d6ed04d70a066ec216cc2d503e8f` |
| 中性案例 | `ScrollView/scroll-view-cases.json` 0.2.0-draft，SHA-256 `11a52eef37938a94f8ede12e8182e4b345702eb2a4e211f2e192734826bf9b0f` |
| UX commit | `618d39ec1709dc214cb81c1314c3eb4636d2ea41` |
| 範圍 | ScrollView 目標 1 的六條行為。聊天面板何時要求跟隨與淡出見 [ChatPanel 紀錄](ChatPanel.md) |
| 測試證據 | 開發紀錄 [聊天面板模組.md](../../../../Documentation/聊天面板模組.md)〈驗證總結〉：2026-10-01 PlayMode 13/13 全過（Windows、Unity 6 Editor，Device Simulator 關閉）。本次沒有重跑 |
| 中性案例執行 | 沒有：本套件沒有 ScrollView 案例 adapter，案例未經公開入口執行 |
| DX | 不適用：來源基線，不是組裝驗收 |

## 實作對照與基線結果

結果用語同 [ChatPanel 紀錄](ChatPanel.md#實作對照與基線結果)：來源測試通過（本次未重跑）、未測、未實作、偏差。

| UX 行為 | 來源實作 | 證據 | 基線結果 |
|---|---|---|---|
| 溢位與夾限（SCROLL-OVERFLOW） | UI Toolkit 原生 `ScrollView` 夾限 `scrollOffset`（`ScrollToBottom` 寫入 `float.MaxValue` 由原生夾到底端）；覆蓋捲軸平時透明 | 無專屬測試；`TickScrollsToTheBottomWhileFollowing` 的前置檢查只確認內容超出 | 未測（依原生行為）；內容不超出時捲軸隱藏、拖曳回彈未測 |
| 短內容貼底（SCROLL-BOTTOM-ANCHOR） | `UpdateBottomAnchor`：`stack.marginTop = max(0, viewportHeight − contentHeight)`，每次 `Tick` 計算 | 開發紀錄：以 Play Mode 截圖確認三則訊息貼底；無數值斷言 | 未測（只有截圖，沒有量測留白） |
| 跟到最新（SCROLL-FOLLOW） | `Tick` 中 `followBottom` 為 true 時 `ScrollToBottom` | `TickScrollsToTheBottomWhileFollowing` | 部分：測試只斷言 `scrollOffset.y > 0`，沒有斷言等於底端；可視範圍改尺寸未測 |
| 使用者捲動停止跟隨（SCROLL-FOLLOW-STOP） | 滾輪事件、內容區按下後移動距離 ≥ 4px（`ScrollDragThreshold`，二維距離）設 `followBottom = false` | 無自動化測試；開發紀錄：手勢用 demo 手動驗證 | 滾輪與內容拖曳：未測（demo 手動）；偏差：內容不可捲時滾輪也會停止跟隨（規範要求 `maxScroll > 0`）；拖曳捲軸滑塊或點軌道：未實作（只監聽滾輪與內容區） |
| 保留閱讀位置（SCROLL-KEEP-POSITION） | 不跟隨時 `Tick` 不改 `scrollOffset`，尾端增加時由原生保留 | 無測試 | 尾端增加：未測（依原生行為）；上方項目改高的錨定補償、`SetMessages` 整份重建後的位置：未實作／未測 |
| 恢復跟隨（SCROLL-FOLLOW-RESUME） | (a) `FollowBottom()`：設 true 並立即到底（`BeginReply` 也呼叫）；(b) 無 | `FollowBottomJumpsToTheBottomImmediately` | (a) 部分：測試只斷言 `scrollOffset.y > 0`；(b) 捲回底端恢復：未實作（2026-11-10 使用者決定的新規則） |

## 待使用者確認

無：目標 1 沒有外觀條文。手機實體拖曳（ScrollView 的 SV03）在 Web demo 與 uGUI 消費端檢查，不在來源基線。

## 輪次

| 規範版本 | UX commit | 範圍 | 實作 commit | Checklist commit | 結論 |
|---|---|---|---|---|---|
| ScrollView 0.2.1-draft | `618d39e` | 來源基線（補建） | `674b3f9`（`ChatPresenter.cs` 最後變更） | 無 | 見下 |

## 結論

2026-11-10 來源基線（補建）：沿用開發紀錄的測試結果，未重跑、中性案例未經 adapter 執行。跟到最新與宿主要求恢復跟隨有來源測試，但只斷言「已往下捲」，沒有斷言停在底端，記為部分；溢位夾限、貼底留白、停止跟隨與保留位置沒有數值測試；拖曳捲軸停止跟隨、上方項目改高的位置補償、捲回底端恢復跟隨未實作；內容不可捲時滾輪也停止跟隨是偏差。這些結果只證明規範與來源的對應。
