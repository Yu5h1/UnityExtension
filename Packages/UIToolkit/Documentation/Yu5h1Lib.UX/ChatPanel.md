# ChatPanel 來源基線紀錄（Yu5h1Lib.UIToolkit）

> 這是 UX [ChatPanel](../../../../../../UX/Documentation/ModuleSpec/Modules/ChatPanel.md) 的來源基線：ChatPanel 0.1.0-draft 是 2026-10-24 從本套件的 `Runtime/Chat/` 與 `Samples~/ChatModule` 抽取的。本紀錄依 [spec-author〈Make acceptance portable〉](../../../../../../.agents/skills/spec-author/SKILL.md#make-acceptance-portable) 於 2026-11-10 補建（當時的基線只寫在規範自己的驗收對照表），對照 0.2.0-draft，不是另一輪驗收。
>
> **本紀錄只證明規範符合來源**，不證明其他實作（Web、uGUI）通過，也不代表其他消費端需要每一條行為；第一個非來源消費端（VCP uGUI）的適配表在規範〈目標 1〉。

```text
UnityExtension
└─ Yu5h1Lib.UIToolkit（來源套件）
   └─ Chat（本紀錄）
      ├─ 規範 ← Yu5h1Lib.UX ModuleSpec/Modules/ChatPanel 0.2.0-draft
      ├─ 實作 → Runtime/Chat/（ChatPresenter、ChatReplyPerformance、ChatComposer、ChatModuleConfig）
      ├─ 範例 → Samples~/ChatModule（ChatModuleDemo）
      └─ 測試 → Tests/Editor/ChatReplyPerformanceTests、ChatComposerTests；Tests/Runtime/ChatPresenterScrollTests
```

## 身分

| 項目 | 內容 |
|---|---|
| 來源套件 | `com.yu5h1.uitoolkit`（`Packages/UIToolkit`），聊天程式最後變更 `ddf5877`（2026-10-26）；本紀錄撰寫時 UnityExtension HEAD `c382062` |
| UX 規範 | [ChatPanel.md](../../../../../../UX/Documentation/ModuleSpec/Modules/ChatPanel.md) 0.2.0-draft，SHA-256 `9e428f9f1fc12e086d80f09de42f32d4cdf7879fd7dfbe47074f62c2bbba792f` |
| 設計資料 | `ChatPanel/chat-panel-design.json` 0.2.0-draft，SHA-256 `2b229c6d305b9af43e8f31f2700105be24068d6717ed121d31f0406fef07c90f` |
| 中性案例 | `ChatPanel/chat-panel-cases.json` 0.2.0-draft，SHA-256 `dd0ef290a2a06319fe8b95eda1e54f309b15fdf5d0a5fca05b3e4dac69454aa1` |
| UX commit | `50e2657ba263877781fe0dabade39ca0907973b3` |
| 範圍 | ChatPanel 0.2.0-draft 全部行為。捲動的貼底、跟隨、停止與恢復跟隨屬 [ScrollView](../../../../../../UX/Documentation/ModuleSpec/Collections/ScrollView.md)，基線另記於 `ScrollView.md`（尚未建立） |
| 測試證據 | 開發紀錄 [聊天面板模組.md](../../../../Documentation/聊天面板模組.md)〈驗證總結〉：2026-10-01 EditMode 210/210、PlayMode 13/13 全過（Windows、Unity 6 Editor）。本次沒有重跑 |
| 中性案例執行 | 沒有：本套件沒有 ChatPanel 案例 adapter，案例未經公開入口執行 |
| DX | 不適用：本紀錄是來源基線，不是組裝驗收 |

## 實作對照與基線結果

結果用語：**來源測試通過**＝既有測試涵蓋並在上列紀錄通過（本次未重跑）；**未測**＝程式有實作但沒有自動化測試，規則邏輯應由 agent 補測；**使用者確認**＝外觀，基線是使用者在 `ChatModuleDemo` 上的確認（[spec-author](../../../../../../.agents/skills/spec-author/SKILL.md#make-acceptance-portable)：外觀沒有 agent 基線）；**未實作**＝0.2.0-draft 新增、來源沒有；**偏差**＝來源做法與規範不同，記錄不改規範。

| UX 行為 | 來源實作 | 證據 | 基線結果 |
|---|---|---|---|
| 並排與列寬（CHAT-LAYOUT-ALIGN） | `ChatPresenter` 列建立（`RowReverse`、`maxWidth: 85%`） | `ChatModuleDemo` | 使用者確認；列寬上限的宿主覆寫未測 |
| 頭像（CHAT-LAYOUT-AVATAR） | `ChatPresenter`、`ProceduralAvatarGenerator`（demo） | `ChatModuleDemo` | 使用者確認 |
| 名稱（CHAT-LAYOUT-NAME） | `ChatPresenter.ShouldShowName` | 無測試 | 未測 |
| 時間與日期分隔（CHAT-LAYOUT-TIMESTAMP） | `ChatPresenter.SetMessages`（`yyyy/MM/dd` 分隔、泡泡內 `HH:mm`） | 無測試 | `inside`：未測（格式與跨日邏輯）＋使用者確認（位置）；`beside`：未實作 |
| 樣式模式（CHAT-STYLE-MODE） | `ChatPresenter.ResolveStyle` | 無測試；開發紀錄：`PerParticipant` 已寫但未填值驗證 | 未測 |
| 無框純文字（CHAT-STYLE-PLAIN） | `ChatPresenter.ApplyStyle` | 無測試 | 未測＋使用者確認 |
| 送出與回覆時恢復跟隨（CHAT-SCROLL-REENGAGE） | `BeginReply` 內呼叫 `FollowBottom()`；自己的訊息由宿主（demo 的送出流程）呼叫 `FollowBottom()` | `ChatPresenterScrollTests.FollowBottomJumpsToTheBottomImmediately` | 開始回覆：來源測試通過；新增自己的訊息：偏差（來源不自動恢復，要宿主呼叫） |
| 距離淡出（CHAT-SCROLL-FADE） | `ChatPresenter.Tick` 淡出 | `ChatPresenterScrollTests.NewestRowIsOpaqueAndRowsFadeTowardTheOldestEdge`、`TurningFadeOffLeavesEveryRowOpaque` | 來源測試通過 |
| 覆蓋捲軸（CHAT-SCROLL-BAR） | `ChatPresenter.StyleOverlayScrollbar` | `ChatModuleDemo`（開發紀錄的 Play Mode 截圖） | 使用者確認；觸控裝置的允許差異不適用（桌面來源） |
| 思考計時（CHAT-TIMER-THINKING） | `ChatReplyPerformance.Waiting` | `BeforeAnyPushItShowsThinkingWithOneDot`、`DotsCycleThroughOneToThreeEverySecond`、`AppendsElapsedSecondsOnceItReachesFive` | 來源測試通過；`showThinkingSeconds = false` 未實作 |
| 累積快照（CHAT-STREAM-SNAPSHOT） | `ChatReplyPerformance.Push`、`Complete` | `AnExtendingPushKeepsWhatWasAlreadyRevealed`、`ADifferentSnapshotResetsTheReveal`、`BeginClearsEverything` | 來源測試通過 |
| 字素邊界（CHAT-STREAM-GRAPHEME） | `ChatReplyPerformance` 字素切分 | `DoesNotSplitAFlagEmojiAcrossTwoTicks`、`EvenReduceMotionHoldsBackTheLastElementUntilComplete` | 國旗與保留最後開放字素：來源測試通過；ZWJ 與膚色修飾符：未測 |
| 配速（CHAT-STREAM-PACING） | `ChatReplyPerformance.Tick(dt, reduceMotion, pace)` | `TickRevealsTextGraduallyAtAboutThirtyPerSecond` | 30 字素/秒：來源測試通過；宿主覆寫速率：偏差（來源只有 `pace` 0.1～1 倍，可調慢、不能高於 30） |
| 減少動態（CHAT-STREAM-REDUCEMOTION） | `Tick(reduceMotion: true)` | `ReduceMotionRevealsTheCompletedTextImmediately` | 來源測試通過 |
| 揭露完成（CHAT-STREAM-DONE） | `ChatReplyPerformance.Done`、`ChatPresenter.ReplyDone`（可輪詢的狀態） | `PushWithoutTickingStillReportsNotDone` | 偏差：來源只提供可輪詢的完成狀態，沒有「每次回覆恰好一次」的通知 |
| 膠囊（CHAT-COMPOSE-CAPSULE） | `ChatComposer` 版面 | `ChatModuleDemo`；開發紀錄的 320／375／750 寬量測表 | 使用者確認；`showMic = false` 未實作 |
| 草稿（CHAT-COMPOSE-DRAFT） | `ChatComposer` 草稿與送出鈕啟用 | `SendStartsDisabledWithAnEmptyDraft`、`SendEnablesOnceTheDraftHasNonWhitespaceText`、`SendStaysDisabledForWhitespaceOnlyText` | 來源測試通過 |
| 送出（CHAT-COMPOSE-SUBMIT） | `ChatComposer`：送出鈕點擊、Return／KeypadEnter（`Input.compositionString` 非空時不送出）、`ClearDraft` | `ClearDraftEmptiesTheFieldAndDisablesSend`、`ClearDraftDoesNotFireTheChangedCallbackAsASend`；開發紀錄：Enter 以 `KeyDownEvent` 經 `SendEvent` 實測 | 受控清空：來源測試通過；Enter 送出：開發紀錄實測、無自動化測試；實機輸入法：未驗（由 TextField 的 TF-P01 承擔） |
| 麥克風（CHAT-COMPOSE-MIC） | `ChatComposer` 麥克風鈕、`Recording` | `MicButtonHasNoTextGlyph`、`RecordingReadsBackWhatWasLastSet` | 受控雙態：來源測試通過；點擊送出 `MicClicked`：demo 手動；`showMic = false` 未實作 |
| 圖片泡泡、載入與失敗、開大圖（CHAT-IMAGE-BUBBLE、CHAT-IMAGE-LOADING、CHAT-IMAGE-OPEN） | 無 | — | 未實作（UI Toolkit 目前沒有圖片訊息的消費端，不算 ChatPanel 目標 1 失敗） |
| 大圖預覽、滿版、切換、關閉（CHAT-VIEWER-PREVIEW、CHAT-VIEWER-FULL、CHAT-VIEWER-SWITCH、CHAT-VIEWER-DISMISS） | 無 | — | 未實作（同上） |

## 待使用者在來源上確認的外觀

在 `Yu5h1LibTest` 開 `ChatModuleDemo`（Device Simulator 關閉），對照 ChatPanel 0.2.0-draft 的條文與設計資料確認：自己靠右、對方靠左、列寬 85%（CHAT-LAYOUT-ALIGN）；頭像 32px、無圖時首字縮寫且對齊泡泡頂端（CHAT-LAYOUT-AVATAR）；泡泡內右下時間與跨日置中日期（CHAT-LAYOUT-TIMESTAMP）；透明底時無框純文字（CHAT-STYLE-PLAIN）；滑鼠進入時 180ms 淡入的覆蓋捲軸（CHAT-SCROLL-BAR）；52px 膠囊與 34px／38px 圓鈕（CHAT-COMPOSE-CAPSULE）。確認結果的落點（UnityExtension 卡片的 close-out 項目）尚未建立。

## 輪次

| 規範版本 | UX commit | 範圍 | 實作 commit | Checklist commit | 結論 |
|---|---|---|---|---|---|
| ChatPanel 0.2.0-draft | `50e2657` | 來源基線（補建） | `ddf5877`（聊天程式最後變更） | 無 | 見下 |

## 結論

2026-11-10 來源基線（補建）：沿用開發紀錄的測試結果，未重跑、中性案例未經 adapter 執行。規則邏輯中，思考計時、累積快照、國旗字素、30 字素/秒配速、減少動態、草稿、受控清空、麥克風回寫、距離淡出與開始回覆時恢復跟隨有來源測試通過；名稱、時間格式、樣式模式、ZWJ 與膚色字素未測；新增自己的訊息時恢復跟隨、宿主覆寫速率、揭露完成通知三項是來源偏差（記錄，不改規範）；0.2.0-draft 新增的宿主選項與圖片、大圖未實作；外觀待使用者確認。這些結果只證明規範與來源的對應。
