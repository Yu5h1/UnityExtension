# ChatPanel 驗收與來源基線（Yu5h1Lib.UIToolkit）

## 2026-11-15 UI Toolkit 驗收（0.5.0-draft）

本輪驗收 `com.yu5h1.uitoolkit` 0.1.0（UnityExtension `9469710`）的 `ChatPresenter`、`ChatComposer` 與 `ChatReplyPerformance`，涵蓋訊息區與輸入列。代表性消費端為 [Chat Module 場景](../../Samples~/ChatModule/ChatModuleDemo.unity)，由宿主提供參與者、訊息與回覆。結果見 [驗收清單](../../../../checklists/uitk-chatpanel.checklist)。

### 規範與案例

| 來源 | 版本 | SHA-256 |
|---|---|---|
| [ChatPanel](../../../../../../UX/Documentation/ModuleSpec/Modules/ChatPanel.md) | 0.5.0-draft | `43dec04d449afb88d738ad2fa059c106ff4411a747ebb370207134e4524b08ff` |
| [設計資料](../../../../../../UX/Documentation/ModuleSpec/Modules/ChatPanel/chat-panel-design.json) | 0.3.0-draft | `e81b6d93440c179c7f706c3bb165e03511696c76981b72be280a4ffa9f5cbb04` |
| [46 個中性案例](../../../../../../UX/Documentation/ModuleSpec/Modules/ChatPanel/chat-panel-cases.json) | 0.5.0-draft，schema 1 | `6cc9e7b465ddf8c4e32abb81348bd2934bbf984a07b102ade985752eb352fa0b` |
| [交付 manifest](../../../../../../UX/Documentation/ModuleSpec/manifest.json) | UX `71aa3c1` | `1cb6b24386786fc83ab20b6a5560f77c92b496c9012ea5273caf8aa4f743f794` |
| [DX Guidelines](../../../../../../UX/Documentation/DX/DX-Guidelines.md) | 1.1.1，UX `71aa3c1` | `005a2d8d946117b712f4eabc38d37f0298bb680b8edc649d1d4b5e7231a57678` |

前三項雜湊均符合 UX 交付 manifest。Windows `Yu5h1LibTest` 使用 Unity `6000.6.0f1`，將 UI Toolkit 套件及 Apps Menu、Chat Module、UxAcceptance 三個樣本連結暫時指向乾淨的 `9469710` 工作樹。其餘套件沿測試專案既有參照。測試前後透過 Package Manager 核對套件解析路徑。樣本連結、manifest 和 lock 都已還原，後兩者與原始 SHA-256 相同。原始證據留在本機暫存目錄 `yu5h1-uitk-chat-20261115-9469710`，含 `editmode.xml`、`playmode.xml` 與解析路徑。

### 行為與覆蓋範圍

| 規範行為 | 公開入口與本輪核對範圍 |
|---|---|
| CHAT-LAYOUT-ALIGN、CHAT-LAYOUT-AVATAR、CHAT-LAYOUT-NAME、CHAT-LAYOUT-TIMESTAMP | `ChatPresenter.SetMessages` 與 `ChatModuleConfig`。已有左右對齊、頭像、名稱、跨日日期與泡泡內時間。列寬上限固定，泡泡外時間缺少入口。外觀與版面量測未執行。 |
| CHAT-STYLE-MODE、CHAT-STYLE-PLAIN | `ChatModuleConfig`、`BubbleStyle` 與參與者樣式。現有公開入口可設定，組合解析與無框內距待驗。 |
| CHAT-SCROLL-REENGAGE、CHAT-SCROLL-FADE、CHAT-SCROLL-BAR | `ChatPresenter.Root` 為原生 ScrollView，宿主送出時呼叫 `FollowBottom`。開始回覆也會強制跟隨，缺少捲回底部後自動恢復的接線。既有測試量到向下捲動及訊息列淡出，尚未驗證精確貼底、閱讀位置、日期列淡出、上緣透明區及捲軸外觀。 |
| CHAT-TIMER-THINKING、CHAT-STREAM-SNAPSHOT、CHAT-STREAM-GRAPHEME、CHAT-STREAM-PACING、CHAT-STREAM-REDUCEMOTION、CHAT-STREAM-DONE | `ChatReplyPerformance.Begin/Push/Complete/Tick` 由 `ChatPresenter` 呼叫。實際重播結果見下。秒數隱藏、宿主覆寫基本字速、一次性完成通知缺少入口，`ReplyDone` 可供宿主輪詢。 |
| CHAT-COMPOSE-CAPSULE、CHAT-COMPOSE-DRAFT、CHAT-COMPOSE-SUBMIT、CHAT-COMPOSE-MIC、CHAT-COMPOSE-MULTILINE、CHAT-COMPOSE-FOCUS、CHAT-STYLE-HOST | `ChatComposer.Draft`、`Sent`、`ClearDraft`、`MicClicked`、`Recording`。草稿、送出啟用狀態及宿主清空已測。樣本以單行欄位組裝，Enter 處理未分流 Shift+Enter，無五行上限、欄內捲動與右下定位行為。麥克風隱藏未提供專用選項。可透過公開 VisualElement 造型，未驗宿主造型、實際焦點外觀與中文 IME。 |
| CHAT-LAYOUT-ADAPT | `ChatModuleDemo` 透過宿主的裝置容器供應可用範圍。未實測切換尺寸時的閱讀位置、草稿和串流進度。 |
| CHAT-LAYOUT-TEXT-WIDTH、CHAT-RESIZE-MESSAGES、CHAT-EFFECT-COMPOSE、CHAT-EFFECT-SEQUENCE、CHAT-EFFECT-RANDOM、CHAT-EFFECT-RANGE、CHAT-EFFECT-LIFECYCLE、CHAT-EFFECT-LAYOUT、CHAT-EFFECT-SETTINGS、CHAT-SETTINGS-GROUP、CHAT-SETTINGS-SYNC | 此套件的 Chat Module 未採用固定泡泡寬度、訊息區把手、可組合淡入／位移和共用聊天設定。 |
| CHAT-IMAGE-BUBBLE、CHAT-IMAGE-LOADING、CHAT-IMAGE-OPEN、CHAT-VIEWER-PREVIEW、CHAT-VIEWER-FULL、CHAT-VIEWER-SWITCH、CHAT-VIEWER-DISMISS | 來源套件沒有圖片訊息和大圖入口。依 UX 目標 1 完成條件第 4 項，這兩個情境不列入 UI Toolkit 來源基線分母。 |

前置條件以 [UX ChatPanel 引用表](../../../../../../UX/Documentation/ModuleSpec/Modules/ChatPanel.md#前置條件) 為準。[ScrollView](ScrollView.md) 仍是舊版來源基線。[Input](Input.md) 有長按驗證紀錄，本輪尚未實測聊天的觸控捲動。聊天的原生欄位、按鈕、麥克風狀態、圖示與色值，尚無證據證明實際組合已使用該版通過驗證的 TextField、Button、Toggle、Icon 與 VisualFoundation 交付。圖片未採用，因此未執行 TransientSurface 前置檢查。方向及設定前置條件隨對應選用能力。

### 中性案例重播

本輪以 `replay-public-reply.ps1` 讀取釘選案例並呼叫 Unity 編譯的公開 `ChatReplyPerformance`。結果存於本機證據目錄的 `neutral-reply-results.json`。其餘 41 案未經案例轉接執行。

| 案例 | 結果 |
|---|---|
| `chat-timer-thinking` | 失敗。前段句點與 `思考中...5s` 符合，6 秒顯示 `思考中.6s`。新回覆在非空快照後以零時間更新，仍顯示思考提示。 |
| `chat-stream-snapshot` | 失敗。延伸保留前綴，具名 final 可正常顯示。重寫前綴時舊配額讓零時間更新仍可揭露新文字。 |
| `chat-stream-grapheme` | 失敗。首次揭露保持國旗完整，暫留尾端 ZWJ 字素。呼叫 `Complete(null)` 後文字變成空字串。 |
| `chat-stream-pacing` | 通過。0.1 秒揭露 3 個字素，長 tick 最多再揭露 8 個。 |
| `chat-stream-reducemotion` | 失敗。配速略過時立即顯示安全前綴，`Complete(null)` 後未釋放尾端字素。 |

### 本輪結論

0.5.0-draft／UX `71aa3c1`／UnityExtension `9469710`。來源基線的五個適用核心情境皆有未驗或未達項目，因此本輪完整通過為 **0/5**。有關聯的既有測試與個別通過案例見驗收清單。

組裝驗收方面，`DX-ENTRY` 與 `DX-ASSEMBLY` 已檢視套件公開入口及樣本接線，樣本場景仍待實際開啟確認。`DX-REFERENCE` 受上述下層交付驗證缺口阻擋。`DX-SETTINGS`、`DX-STRUCTURE`、`DX-PREVIEW` 尚未執行完整驗收。獨立 Chat Module 場景可供後續試用，UIToolkit 的共用 UxAcceptance 未註冊聊天案例。待核心落差修正，再以同一組案例驗證，並檢查畫面、中文 IME 與觸控閱讀位置。

## 2026-11-10 來源基線（0.2.0-draft）

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
| 範圍 | ChatPanel 0.2.0-draft 全部行為。捲動的貼底、跟隨、停止與恢復跟隨屬 [ScrollView](../../../../../../UX/Documentation/ModuleSpec/Collections/ScrollView.md)，基線另記於 [ScrollView.md](ScrollView.md) |
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

在 `Yu5h1LibTest` 開 `ChatModuleDemo`（Device Simulator 關閉），對照 ChatPanel 0.2.0-draft 的條文與設計資料確認：自己靠右、對方靠左、列寬 85%（CHAT-LAYOUT-ALIGN）；頭像 32px、無圖時首字縮寫且對齊泡泡頂端（CHAT-LAYOUT-AVATAR）；泡泡內右下時間與跨日置中日期（CHAT-LAYOUT-TIMESTAMP）；透明底時無框純文字（CHAT-STYLE-PLAIN）；滑鼠進入時 180ms 淡入的覆蓋捲軸（CHAT-SCROLL-BAR）；52px 膠囊與 34px／38px 圓鈕（CHAT-COMPOSE-CAPSULE）。結果記在 `cross-platform-ui-parity` 卡片的 close-out：[cross-platform-ui-parity-closeout.checklist](../../../../checklists/cross-platform-ui-parity-closeout.checklist)，本文不重複勾選狀態。

## 輪次

| 規範版本 | UX commit | 範圍 | 實作 commit | Checklist commit | 結論 |
|---|---|---|---|---|---|
| ChatPanel 0.2.0-draft | `50e2657` | 來源基線（補建） | `ddf5877`（聊天程式最後變更） | 外觀確認見 close-out checklist | 見下 |

## 結論

2026-11-10 來源基線（補建）：沿用開發紀錄的測試結果，未重跑、中性案例未經 adapter 執行。規則邏輯中，思考計時、累積快照、國旗字素、30 字素/秒配速、減少動態、草稿、受控清空、麥克風回寫、距離淡出與開始回覆時恢復跟隨有來源測試通過；名稱、時間格式、樣式模式、ZWJ 與膚色字素未測；新增自己的訊息時恢復跟隨、宿主覆寫速率、揭露完成通知三項是來源偏差（記錄，不改規範）；0.2.0-draft 新增的宿主選項與圖片、大圖未實作；外觀待使用者確認。這些結果只證明規範與來源的對應。
