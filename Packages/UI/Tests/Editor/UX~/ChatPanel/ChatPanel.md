# 聊天面板（ChatPanel）

> Task ID: ux-chat-panel
>
> 狀態：規範草案；目標 1 進行中（2026-11-10 確認）；平台證據由各實作端紀錄持有
>
> Layer: Modules（模組層）
>
> Specification version: 0.4.0-draft（2026-11-11，補充選用的固定文字框寬度與共用聊天設定）。前一版：0.3.1-draft（2026-11-11；修正依序觸發的前提：僅跟隨逐字揭露，取消獨立排程；補位移不得改變捲動幾何）。前一版：0.3.0-draft（2026-11-11；納入近期 Web 已確認的多行輸入、訊息高度、可組合文字效果與宿主外觀選項）。前一版：0.2.2-draft（2026-11-10；滿 5 秒顯示秒數後固定三個句點，只更新整數秒數）。前一版：0.2.1-draft（2026-11-10；案例勘誤：減少動態仍保留未完成串流的最後開放字素，補 Complete 後釋放的觀測；行為與設計資料不變）。前一版：0.2.0-draft（2026-11-10；目標 1 確認：捲動改引用 ScrollView，加入圖片訊息與大圖、逐字揭露完成通知與宿主選項）。前一版：0.1.0-draft（2026-10-24，自 UnityExtension `Packages/UIToolkit/Samples~/ChatModule` 與 `Runtime/Chat/` 實作及測試收斂）
>
> 設計資料版本：0.3.0-draft（`ChatPanel/chat-panel-design.json`）
>
> 案例資料版本：0.4.0-draft（`ChatPanel/chat-panel-cases.json`）

承載一對一或多方對話的互動模組：排列訊息、顯示 AI 角色或助理的思考與逐字回覆、顯示對方貼的圖片並可開大圖，以及底部的文字／語音輸入列。它定義對話版面、時間、思考與逐字揭露、圖片與大圖、輸入列，以及何時要求捲動跟隨；網路通訊、LLM 串流協定、語音辨識與語音合成由宿主負責。

## 系統階層與關係

```text
Yu5h1Lib.UX
├─ 聊天面板（ChatPanel，模組層）
│  ├─ 訊息區
│  │  ├─ 1. 參與者與泡泡排版、樣式、時間
│  │  ├─ 2. 捲動：貼底、跟隨、保留位置引用 ScrollView；送出與回覆時要求跟隨、距離淡出、覆蓋捲軸由本模組擁有
│  │  ├─ 3. 回覆演出：思考中計時、揭露節奏、可組合效果、完成通知
│  │  └─ 5. 圖片訊息與大圖（預覽 ↔ 滿版；開啟、模態、關閉、焦點返回引用 TransientSurface）
│  ├─ 輸入列：4. 多行 TextField、麥克風 Toggle、送出 Button
│  └─ 選用設定：同一聊天範圍共用設定值，呈現與保存引用 SettingsPanel、Configuration
├─ 引用下層 UX 規範
│  ├─ Collections/ScrollView（目標 1：貼底、跟隨、停止與恢復跟隨、保留閱讀位置）
│  ├─ TransientSurface（共同規則：開啟、模態、使用者關閉、關閉通知、焦點返回、卸載）
│  ├─ Controls/TextField、Controls/Button、Controls/Toggle
│  ├─ Foundation/Icon、Foundation/Input
│  └─ VisualFoundation（色彩角色、遮罩、對比度、字級縮放）
└─ 宿主：訊息資料、網路與串流、語音、角色台（情緒與表情）、安全區域與最大欄寬
```

消費端宣告採用哪些部分：訊息區、輸入列，或兩者。宣告只採用訊息區時，送出由宿主自己的輸入介面完成，宿主新增自己的訊息時本模組照樣要求跟隨（CHAT-SCROLL-REENGAGE）。

---

## 所需能力盤點與跨層分工（Capabilities Inventory）

依 [spec-author〈Capability inventory〉](../../../../.agents/skills/spec-author/SKILL.md#define-the-goal-before-the-specification) 列出目標 1 情境用到的能力。對應下層規範的列就是〈前置條件〉，狀態寫在那裡；留在模組內的列寫理由（停止規則：只有一個消費端、沒有平台差異要釘）。

| # | 能力 | 在聊天面板中的需求 | 擁有者 |
|---|---|---|---|
| 1 | 文字輸入框 | 提示字、草稿變更與是否含非空白、輸入法選字保護、受控清空 | [TextField](../Controls/TextField.md) |
| 2 | 按鈕 | 送出鈕尺寸、外觀、隨草稿啟用停用 | [Button](../Controls/Button.md) |
| 3 | 開關 | 麥克風受控雙態（點擊只送出請求，錄音狀態由宿主回寫） | [Toggle](../Controls/Toggle.md) |
| 4 | 捲動 | 短對話貼底、跟到最新、使用者捲動停止跟隨、保留閱讀位置、恢復跟隨 | [ScrollView](../Collections/ScrollView.md) 目標 1 |
| 4a | 送出與回覆時要求跟隨、距離淡出、覆蓋捲軸 | 何時要求跟隨是組合規則；淡出與覆蓋捲軸只有聊天面板用 | 本模組 |
| 5 | 思考計時與逐字揭露 | 思考點點與秒數、累積快照、字素邊界、配速、減少動態、揭露完成通知 | 本模組（只有聊天面板用；各平台時鐘排程不需跨平台規範，第 75、77 題） |
| 5a | 可組合文字效果與參數編輯 | 揭露節奏與淡入／位移獨立，各效果依序隨新字揭露／字詞可顯示即播放、比例與時間、完成與取消 | 本模組（當前具體消費端為 Web ChatPanel）；數值曲線不在此定義 |
| 5b | 位移方向與距離 | 選擇指向或全方位、距離 min／max | [DirectionField](../Controls/DirectionField.md) 0.2.0-draft；只引用其值語意，不複製拖曳規則 |
| 5c | 訊息區高度調整 | 隱藏把手、垂直調整、輸入列保持在底部 | 本模組的版面組合；拖曳及取消沿 Input |
| 6 | 圖示與頭像 | 送出箭頭、麥克風、錄音點；頭像 32px、無圖時首字縮寫 | [Icon](../Foundation/Icon.md)；頭像排版留本模組 |
| 7 | 泡泡樣式與時間 | 樣式模式、無框退化、時間開關與位置 | 色彩角色 [VisualFoundation](../VisualFoundation.md)；樣式模式與時間留本模組 |
| 8 | 圖片訊息 | 等比縮圖、載入中與失敗、點一下開大圖 | 本模組（只有聊天面板用，單獨成節，出現第二個消費端時整節搬出） |
| 9 | 大圖檢視 | 預覽與滿版、縮放手勢切換、點外面與縮小關閉、失去前景關閉 | 開啟、模態、關閉、焦點返回：[TransientSurface](../TransientSurface.md) 共同規則；版面、狀態切換、縮小關閉、失去前景關閉：本模組 |
| 10 | 安全區域與最大欄寬 | 瀏海與底部手勢區內距、主欄最大寬 430px | 宿主容器（各平台原生機制，無跨平台規範要釘） |
| 11 | 共用聊天設定 | 可從聊天面板或專案設定調整同一範圍的偏好，套用後兩處反映同一值 | 欄位與套用對象由本模組擁有，面板與保存引用 [SettingsPanel](../SettingsPanel.md) 1.1.0、[Configuration](../Foundation/Configuration.md) 1.5.0 |

---

## 目標 1：一輪安全送出、可閱讀的逐字回覆與角色貼圖

使用者 2026-11-10 在 [ux-chat-panel 決策檔](../../../decisions/ux-chat-panel.decisions) 確認（`chatpanel-goal-1` 採用；`chatpanel-image-open`、`chatpanel-composer-scope`、`transientsurface-goal-1` 的答案見〈決策紀錄〉）。取代 2026-10-24 的目標 1 提案。

- **為誰服務**：一對一和 AI 角色或助理對話的介面：使用者送出一句，看對方先「思考中」再逐字回覆，往回翻看時不被拉走，並能看對方貼的圖、點開看大圖。證據路徑：來源是 UnityExtension UI Toolkit 聊天模組（`Packages/UIToolkit/Samples~/ChatModule` 的 `ChatModuleDemo` 與 `Runtime/Chat/`）；第一個非來源消費端是 VCP 的 uGUI 聊天面板（ChatScene；VCP 計畫第 8 項角色貼文附圖、第 9 項重建聊天面板）。
- **情境**：
  1. 訊息排版與外觀：自己靠右、對方靠左；三人以上顯示他人名稱；跨日日期分隔與時間，時間可放在泡泡內右下或泡泡外側；透明底退化為無框純文字（並排與列寬，CHAT-LAYOUT-ALIGN；頭像，CHAT-LAYOUT-AVATAR；名稱，CHAT-LAYOUT-NAME；時間與日期分隔，CHAT-LAYOUT-TIMESTAMP；樣式模式，CHAT-STYLE-MODE；無框純文字，CHAT-STYLE-PLAIN）。
  2. 貼底與跟隨：短對話貼底、跟到最新、使用者捲動停止跟隨、保留閱讀位置與捲回底端恢復，由 [ScrollView](../Collections/ScrollView.md) 目標 1 擁有；本模組在新增自己的訊息與開始新回覆時要求跟隨（送出與回覆時恢復跟隨，CHAT-SCROLL-REENGAGE）。
  3. 距離淡出與覆蓋捲軸，宿主可關閉淡出（距離淡出，CHAT-SCROLL-FADE；覆蓋捲軸，CHAT-SCROLL-BAR）。
  4. 思考中計時與逐字揭露：前 5 秒點點循環、滿 5 秒顯示秒數（宿主可不顯示秒數）；零延遲回覆略過思考態；以累積快照驅動；字素（國旗、ZWJ、膚色）不被截斷；預設每秒 30 字素、宿主可調；減少動態時直接顯示；揭露完成時通知宿主，讓宿主決定何時接下一句（思考計時，CHAT-TIMER-THINKING；累積快照，CHAT-STREAM-SNAPSHOT；字素邊界，CHAT-STREAM-GRAPHEME；配速，CHAT-STREAM-PACING；減少動態，CHAT-STREAM-REDUCEMOTION；揭露完成，CHAT-STREAM-DONE）。
  5. 輸入列：宿主可選外框與背景；多行最多顯示 composerMaxVisibleLines 行後內部捲動，按鈕固定右下；空白草稿停用送出鈕；Enter 或點擊送出、成功後才清空；輸入法選字時按 Enter 不誤送；麥克風受控雙態，宿主可不顯示麥克風（膠囊，CHAT-COMPOSE-CAPSULE；草稿，CHAT-COMPOSE-DRAFT；送出，CHAT-COMPOSE-SUBMIT；麥克風，CHAT-COMPOSE-MIC）。
  5a. 選用擴充：文字可逐字或立即排版，淡入與位移可獨立組合、獨立選依序或隨機觸發；訊息區可調整高度且保持輸入列位置。選用能力與基本聊天分開宣告，未採用者不宣稱通過該能力。
  6. 圖片訊息：對方的訊息可以是一張圖，泡泡內等比顯示縮圖；載入中顯示佔位，失敗顯示失敗外觀且不能開啟；點一下已載入的縮圖開大圖（圖片泡泡，CHAT-IMAGE-BUBBLE；載入與失敗，CHAT-IMAGE-LOADING；開大圖，CHAT-IMAGE-OPEN）。
  7. 大圖檢視：先進入四周留間距的預覽；預覽中點一下圖片或放大進入滿版，滿版中點一下或縮小回到預覽；預覽中縮小、點空白處或間距區域關閉，Escape 或應用失去前景也關閉，焦點回到縮圖（預覽，CHAT-VIEWER-PREVIEW；滿版，CHAT-VIEWER-FULL；切換，CHAT-VIEWER-SWITCH；關閉，CHAT-VIEWER-DISMISS）。
- **不在目標 1 內**：回覆中切換停止鈕；三人以上每人自訂樣式；輸入框上下的擴充插槽（引文、工具列、快捷提問鈕）；向量圖示子系統；手機返回鍵與瀏覽器上一頁關閉大圖（使用者 2026-11-10 決定不做）；一則訊息多張圖、影片、圖文同泡、使用者傳圖；圖片儲存、分享與重試；角色情緒與表情（宿主的角色台呈現）；回覆途中「略過剩餘演出」命令。
- **完成條件**：
  1. 規格、具名設計資料與中性案例齊備並登錄 manifest；〈Web demo 定案項目〉在 Web demo 定案並改版。
  2. Web demo（Yu5h1Lib.Web）以公開入口通過全部中性案例，使用者確認外觀與觸控手感。
  3. UnityExtension uGUI 實作（UI 套件）被 VCP 採用，通過 VCP 宣告部分（訊息區）的案例，使用者在手機上確認。
  4. UI Toolkit 來源依本版建立基線紀錄；它沒有圖片訊息的消費端，圖片與大圖記為未實作，不算本目標失敗。
- **狀態**：進行中（2026-11-10）。

### 適配表：VCP uGUI 聊天面板

VCP 這一輪宣告只採用訊息區（`chatpanel-composer-scope`），現有多行輸入列與快捷提問鈕不動。

| 行為 | VCP 需要嗎（現況） | uGUI 原生機制 |
|---|---|---|
| 左右對齊與列寬上限 | 需要：角色左、使用者右；列寬約七成（宿主覆寫上限） | 版面群組可排，無聊天專用 |
| 頭像 | 現況不顯示（程式支援 56px 但沒接上）；宿主以開關決定 | 無 |
| 名稱 | 不需要：一對一，Adaptive 本來就隱藏 | 無 |
| 時間與日期分隔 | 需要：時間 `HH:mm` 在泡泡外側（宿主選位置）；日期分隔只在載入歷史時出現，且插在當天第一則之後（VCP 的錯，不照抄） | 無 |
| 泡泡樣式 | 需要：自己與對方各一套（SelfAndOthers），主題有底圖與外框效果（宿主樣式） | 圖片與顏色可設 |
| 無框純文字 | 不需要；保留不影響 | — |
| 貼底、跟隨、停止與恢復跟隨、保留閱讀位置 | 需要（見 ScrollView 適配表；現況跳回頂端是第 9 項重建的主因） | 見 ScrollView |
| 送出與回覆時恢復跟隨 | 需要：VCP 自己的輸入列送出後新增使用者訊息、角色每句開一則回覆 | 無 |
| 距離淡出 | 不需要：關閉 | — |
| 覆蓋捲軸 | 不需要懸停：手機沒有懸停；現況是自動隱藏捲軸（允許差異） | ScrollRect 自動隱藏捲軸 |
| 思考中計時 | 需要點點循環；秒數現況關閉（宿主可不顯示秒數） | 無 |
| 累積快照 | 需要：後端一次給一整句，一句一個泡泡，宿主每句開一則回覆 | 無 |
| 字素邊界 | 需要：現況逐 UTF-16 碼元打字，表情符號會出現半截 | 無 |
| 揭露速率 | 需要：現況每 0.05 秒一字（宿主調速率） | 無 |
| 揭露完成 | 需要：現況文字照固定速率打、語音同時播，兩者都結束才換下一句；不需要依音訊時間同步 | 無 |
| 減少動態 | 現況沒有；採用 | 無 |
| 輸入列 | 本輪不採用：現況多行（最多 5 行、Enter 換行、按鈕送出），送出當下就清空，沒有麥克風 | TMP 輸入框 |
| 圖片訊息 | 需要：角色貼文附圖（VCP 第 8 項） | Image 可顯示，無聊天專用 |
| 大圖檢視 | 需要（VCP 第 8 項） | 沒有模態介面；Android 返回鍵在 Unity 對應 Escape |
| 角色情緒 | 不在聊天面板：表情符號與姿勢在角色台 | — |
| 快捷提問鈕 | 需要（VCP 第 8 項觸發貼文），放在 VCP 現有輸入列旁，屬宿主內容 | — |

### 前置條件

| 引用 | 狀態 |
|---|---|
| [ScrollView](../Collections/ScrollView.md) 0.2.2-draft | 目標 1 進行中（2026-11-10），無平台結果 |
| [TransientSurface](../TransientSurface.md) 1.1.2 共同規則 | 目標 1 進行中（2026-11-10）：Web 對話框已有結果；uGUI 無 |
| [TextField](../Controls/TextField.md) 1.0.1 | 目標 1 進行中：Web 自動檢查通過，人工確認待做（UX-WEB-31）；uGUI 無結果，VCP 本輪不採用輸入列 |
| [Button](../Controls/Button.md) 4.0.0 | 目標 1、2 已達成；uGUI（Yu5h1Lib.UI）尺寸與版面通過，顏色列受阻 |
| [Toggle](../Controls/Toggle.md) 1.3.0 | 目標 1 已達成（Web）；只有輸入列的麥克風用到 |
| [Icon](../Foundation/Icon.md) 1.0.1 | 目標 1 已達成（Web） |
| [VisualFoundation](../VisualFoundation.md) 3.1.0 | 目標 1 已達成（UI Toolkit）；uGUI 顏色列受阻 |
| [DirectionField](../Controls/DirectionField.md) 0.2.0-draft | 選用位移設定時引用；來源基線與未確認邊界依該規範 |
| [操作（Input）](../Foundation/Input.md) 4.0.0 | 目標 1 已達成（Web、UI Toolkit） |

沒有平台結果的前置條件是目標 1 在該平台的受阻情境，不由實作端補規範。

---

## 行為條文（Behaviors）

### 1. 參與者、訊息列與泡泡樣式（Layout, Avatar, Name & BubbleStyle）

| ID | 前提與操作 | 必須觀察到的結果／邊界 | 驗證案例 |
|---|---|---|---|
| CHAT-LAYOUT-ALIGN | 呈現一筆訊息列（Message Row）；設定為 `SelfAlignRight`（預設 `true`） | 當 `SelfAlignRight = true` 且發話者為自己（`participant.id == "self"`）時，自己泡泡靠右、頭像在外側，其餘靠左；當 `SelfAlignRight = false` 時，所有參與者一律靠左對齊。每列最大寬度為容器寬度的 `rowMaxWidthPercent = 85%`（宿主可覆寫），列與列底部間距為 `rowGap = 8px` | `chat-layout-align` |
| CHAT-LAYOUT-TEXT-WIDTH | 消費端宣告採用固定文字框寬度，顯示文字訊息或揭露回覆 | 文字列從第一個可見字素起使用設定的最大列寬。扣除外側頭像、泡泡外時間和各自的間距後，泡泡使用剩餘寬度。可用寬度、附屬欄位與樣式相同時，新增文字只因換行增加高度，泡泡寬度與水平對齊保持一致。視窗或欄位改變時按新範圍重排，貼底與閱讀位置沿 ScrollView。圖片依 CHAT-IMAGE-BUBBLE 等比縮放 | `chat-layout-text-width`、CP05 |
| CHAT-LAYOUT-AVATAR | 設定為 `ShowAvatars`（預設 `true`） | `ShowAvatars = true` 時，於泡泡外側顯示 `32×32px` 圓形頭像（圓角 `16px`），與泡泡間距 `avatarGap = 8px`，且垂直固定對齊泡泡頂端，多行長文時頭像不置中漂移。若 `participant.avatar` 有圖片則以等比裁切填滿；若無圖片，顯示底色 `#667380` 並置中呈現 `displayName` 的首個字元大寫（字級 `32 × 0.45 = 14.4px`）或預設人物圖示。`ShowAvatars = false` 時不顯示頭像也不保留其空間 | `chat-layout-avatar` |
| CHAT-LAYOUT-NAME | 依 `NameDisplay`（`Adaptive`／`AlwaysShow`／`HideSelf`）與目前對話參與者總數 `ParticipantCount` 決定是否顯示發話者名稱 | 1. `Adaptive`（預設）：當 `ParticipantCount <= 2`（一對一對話）時隱藏所有名稱；當 `ParticipantCount > 2`（三人以上群組）時，隱藏自己（`self`）的名稱、顯示其他參與者名稱。<br>2. `AlwaysShow`：無論人數，所有參與者皆顯示名稱。<br>3. `HideSelf`：永遠隱藏自己的名稱，其他人無論一對一或群組皆顯示名稱。<br>名稱位於泡泡頂部（字級 `11px`、粗體、底部間距 `2px`） | `chat-layout-name` |
| CHAT-LAYOUT-TIMESTAMP | 設定為 `ShowTimestamps`（預設 `false`）與 `timestampPlacement`（`inside`／`beside`，預設 `inside`），訊息帶本地時間 `Timestamp` | 單一開關同時控制兩處時間：<br>1. **跨日分隔線**：當本則訊息與上一則已含時間戳的訊息本地日期（`Timestamp.Date`）不同（或為第一則訊息）時，先在訊息列**上方**插入置中日期標籤 `yyyy/MM/dd`（字級 `11px`、上下外距 `8px`）。<br>2. **訊息時間**：`HH:mm`（字級 `10px`、不透明度 `0.65`）。`inside`：在泡泡內右下角，頂部間距 `2px`；`beside`：在泡泡外側（朝列的內側）與泡泡底緣對齊。當 `ShowTimestamps = false` 時，跨日分隔線與訊息時間一併隱藏 | `chat-layout-timestamp`、`chat-layout-timestamp-beside` |
| CHAT-STYLE-MODE | 依 `StyleMode`（`Unified`／`SelfAndOthers`／`PerParticipant`）解析每則訊息的 `BubbleStyle` | 1. `Unified`：所有訊息共用 `UnifiedStyle`。<br>2. `SelfAndOthers`：自己（`self`）使用 `SelfStyle`（預設主色底 `#4F8CFF`、白字），其他人使用 `OthersStyle`（預設深灰底 `#262A34`、淺灰白字 `#EBEFF5`）。<br>3. `PerParticipant`：直接使用該發話者的 `participant.Style`。<br>泡泡圓角由 `BubbleStyle.CornerRadius`（預設 `12px`）決定；泡泡內所有文字（名稱、內文、時間戳）統一繼承 `BubbleStyle.TextColor`。宿主樣式可帶底圖與外框效果 | `chat-style-plain` |
| CHAT-STYLE-PLAIN | 解析出的 `BubbleStyle.BackgroundColor` 完全透明（`alpha <= 0.001`）且無 `BackgroundImage` | 泡泡自動退化為「無框純文字模式」：水平／垂直內距由 `8px` 降為 `0px`（適用於無底框的 AI 助理文字排版） | `chat-style-plain` |

### 2. 捲動、送出時跟隨、距離淡出與覆蓋捲軸

聊天面板開啟 ScrollView 的 `bottomAnchor` 與 `followBottom`。原本在本模組的三條改為引用：

| 0.1.0-draft ID | 現由 | 說明 |
|---|---|---|
| CHAT-SCROLL-ANCHOR | ScrollView 短內容貼底（SCROLL-BOTTOM-ANCHOR） | 案例搬到 `scroll-bottom-anchor-and-follow` |
| CHAT-SCROLL-FOLLOW | ScrollView 跟到最新（SCROLL-FOLLOW） | 同上 |
| CHAT-SCROLL-DISENGAGE | ScrollView 使用者捲動停止跟隨（SCROLL-FOLLOW-STOP）、保留閱讀位置（SCROLL-KEEP-POSITION） | 案例搬到 `scroll-follow-stop-and-resume`；`scrollDragThreshold` 搬到 ScrollView |

| ID | 前提與操作 | 必須觀察到的結果／邊界 | 驗證案例 |
|---|---|---|---|
| CHAT-SCROLL-REENGAGE | 新增一則自己（`self`）的訊息（輸入列送出，或宿主自己的輸入介面送出後新增），或宿主開始一輪新回覆（`BeginReply`） | 向 ScrollView 要求跟隨（SCROLL-FOLLOW-RESUME (a)）：立即到底並恢復跟隨。一次載入整段歷史不算新增 | `chat-scroll-reengage` |
| CHAT-SCROLL-FADE | 設定為 `FadeMessages`（可在執行期動態切換開關） | 1. 當 `FadeMessages = false` 時，所有訊息列與日期分隔線的 `opacity = 1.0`。<br>2. 當 `FadeMessages = true` 時，以每列**下緣外邊界**相對於視窗頂部與高度的比例計算：`towardNewest = clamp01((bottomEdge - viewport.y) / viewport.height)`，`row.opacity = lerp(minRowOpacity, 1.0, towardNewest)`（其中 `minRowOpacity = 0.35`）。此公式確保最新訊息恆為 `1.0`，僅往視窗頂部遠離的舊訊息漸變淡出 啟用時，紀錄上緣至 messageFadeTransparentEnd（含把手區）完全透明，至 messageFadeOpaqueStart 恢復不透明；淡出僅影響呈現，不遮擋把手操作。| `chat-scroll-fade` |
| CHAT-SCROLL-BAR | 指標移入或移出聊天面板容器 | 垂直捲軸採覆蓋式（Overlay，軌道寬 `12px`、滑塊寬 `4px`、最小高 `24px`、圓角 `2px`），不擠壓訊息寬度；平時 `opacity = 0`，滑鼠進入容器時以 `180ms` 淡入至 `1.0`，離開時淡出至 `0`。操作捲軸依 SCROLL-FOLLOW-STOP 停止跟隨。允許差異：沒有懸停的觸控裝置改為捲動時顯示、停止後隱藏 | `chat-scroll-overlay-bar` |

| CHAT-RESIZE-MESSAGES | 消費端選用訊息區高度調整 | 紀錄上緣把手平常不顯示，靠近、鍵盤聚焦或調整中可見；垂直拖拉只改訊息區高度，輸入列保持靠底且不變形。高度限制在 messageAreaMinHeight 與扣除輸入列後的可用高度，空間不足時以可用高度為上限。鍵盤可調整並到達兩端；拖曳取消沿 Input，不能丟失訊息、草稿或閱讀位置 | `chat-resize-messages`、CP04 |
| CHAT-LAYOUT-ADAPT | 宿主改變可用寬高或直橫方向 | 同一對話、草稿、效果設定與回覆進度保留；元件適應新可用範圍，不自行重建聊天狀態。PC／手機預覽切換器屬宿主工具 | `chat-layout-adapt`、CP04 |

### 3. 思考中計時器與串流逐字揭露演出（Thinking Timer & Streaming Reveal Clock）

| ID | 前提與操作 | 必須觀察到的結果／邊界 | 驗證案例 |
|---|---|---|---|
| CHAT-TIMER-THINKING | 宿主呼叫 `BeginReply(participant)` 啟動非同步回覆，且目前尚未揭露任何內容字元（`shown == 0` 且 `complete == false`） | 1. 回覆列立即插入訊息列表底部，進入思考中狀態。<br>2. **前 5 秒（`0 <= elapsed < 5s`）**：顯示 `"思考中"` 加上每秒循環變化的 1～3 個句點 `dots = (floor(elapsed) % 3) + 1`。<br>3. **滿 5 秒起（`elapsed >= 5s`）**：固定顯示三個句點 `...`，停止 1～3 點循環，後方附加整數秒數 `"{floor(elapsed)}s"`（如 `"思考中...5s"`、`"思考中...6s"`）；宿主設 `showThinkingSeconds = false`（預設 `true`）時不附加，句點照常循環。<br>4. **零延遲自動略過**：若 `BeginReply` 後立刻呼叫 `Push(nonEmpty)` 或 `Complete(nonEmpty)`，思考狀態持續時間為 `0s`，直接進入文字顯示 | `chat-timer-thinking`、`chat-timer-no-seconds` |
| CHAT-STREAM-SNAPSHOT | 宿主在回覆期間多次呼叫 `Push(snapshot)` 傳入**截至目前的完整累積文字**（非增量 delta），最後呼叫 `Complete(finalSnapshot)` | 1. 若新快照仍以目前已揭露的完整字素前綴開頭，保留目前顯示游標 `shown` 繼續往後揭露。<br>2. 若宿主修正或重寫了前綴（不再相符），自動將 `shown` 與配額重設為 `0` 重新揭露。<br>3. 呼叫 `Complete(finalSnapshot)` 時標記 `complete = true`，若傳入 `null` 則保留先前已 `Push` 的內容直接收尾。<br>一次回覆對應一個泡泡；宿主要一句一個泡泡時，每句開一次 `BeginReply` | `chat-stream-snapshot` |
| CHAT-STREAM-GRAPHEME | 串流配速器切分文字的推進單位 | 必須以 **Unicode 字素叢集（Grapheme Cluster）** 為最小不可分割單位，而非 UTF-16 `char`：<br>1. 結合字元維持完整。<br>2. 合併成對的區域指示符國旗（`U+1F1E6..U+1F1FF`）、零寬連字元（ZWJ `U+200D` 及其連結字素）與膚色修飾符（`U+1F3FB..U+1F3FF`）。<br>3. 當 `complete == false`（仍在串流中）且叢集數 $> 1$ 時，暫時保留最後一個開放叢集不揭露（上限設為倒數第二個邊界），避免結合字元尚未傳完前畫面閃現半截符號；`complete == true` 後釋放全部邊界 | `chat-stream-grapheme` |
| CHAT-STREAM-PACING | 有待揭露文字且未啟用減少動態 | 依經過時間揭露完整字素。速率為 `streamingCharsPerSecond × clamp(pace, 0.1, 1.0)`；`streamingCharsPerSecond` 預設 `30` 字素/秒，宿主可覆寫（大於 0）。單幀累積上限 `streamingMaxCredit = 8` | `chat-stream-pacing`、`chat-stream-rate-override` |
| CHAT-STREAM-REDUCEMOTION | 使用者或設定啟用減少動態（`reduceMotion = true`） | 略過配速累積，每次更新直接將 `shown` 推進至目前允許的最後邊界；同時完成所有已啟用的效果，不放出未完成串流的最後開放字素 | `chat-stream-reducemotion` |
| CHAT-STREAM-DONE | 回覆已 `Complete`，且 `shown` 抵達最後一個字素邊界，所有已啟用的效果均結束 | 對該回覆送出恰好一次 `RevealCompleted`；之後的更新不再送出。回覆在揭露完成前被移除（例如宿主清空對話）時不送出。宿主可依此決定何時開始下一則回覆（例如等語音也播完） | `chat-stream-done` |

### 3a. 可組合文字表演（選用能力）

這一節只決定何時啟動、輸入參數與可觀察結果，不規定插值公式、亂數演算法、平台類別或繼承方式。平台可用不同呈現器；都必須遵守下表。既有基本聊天與來源歷史輪次不自動視為通過此擴充。

| ID | 前提與操作 | 必須觀察到的結果／邊界 | 驗證案例 |
|---|---|---|---|
| CHAT-EFFECT-COMPOSE | 選擇揭露方式及效果 | 揭露方式為逐字（預設）或一次顯示安全前綴；淡入與位移為獨立開關，可同時啟用，預設都關閉。兩效果疊加於同一原始文字，不可改字序、字素、換行或最終排版；一次顯示也能播放效果 | `chat-effect-instant-sequence`、CP03 |
| CHAT-EFFECT-SEQUENCE | 某效果勾選「依序觸發」 | 只有逐字顯示開啟且該效果勾選依序時，才以完整字素為單位，隨字素揭露立即播放，不另加等待。關閉逐字顯示時，依序設定保留但不生效，效果回到字詞模式；不能由效果自行啟動逐字排程。淡入與位移各自設定，預設 false，不能互相改寫或新增第二套間隔秒數。前提不成立時改依序值、或只改揭露字速，不得重播進行中的效果 | `chat-effect-instant-sequence`、`chat-effect-follow-reveal`、`chat-effect-mixed-fade`、`chat-effect-mixed-move`、`chat-effect-inactive-settings` |
| CHAT-EFFECT-RANDOM | 某效果未勾選依序，或逐字顯示關閉 | 未勾選依序，或逐字顯示已關閉時，保留早期字詞模式：選中的詞語在收到並可顯示時立即播放，不抽取額外等待時間、不按字速排隊、不反覆重播。隨機只指參與比例、位移及表演時間；同一批可顯示字詞可以同時開始。詞語切分依語言服務，可有字典邊界差異，但不得拆字素 | `chat-effect-random-start`、CP03 |
| CHAT-EFFECT-RANGE | 指定比例、時間與方向距離 | 表演比例 0～100%，預設 effectPercentage；每效果各自抽選，依序按字素、隨機按詞語，非空白單位各自判定，不保證部分比例恰占全句同等比例；0 不等待，100 全部表演。時間在 effectDurationMin～effectDurationMax 間各自抽一次，不逐幀重抽；皆為 0 直接歸位。位移用 DirectionField 的指向／全方位與 minDistance～向量長度：指向沿選定方向，全方位方向可不同；起點在距離範圍內，結束回到原排版位置 | `chat-effect-zero-ratio`、`chat-effect-direction-range`、CP03 |
| CHAT-EFFECT-LIFECYCLE | 前綴重寫、移除、減少動態或效果設定變更 | 文字及所有效果共用回覆時鐘，不建立額外觸發時鐘。重寫清除舊效果；移除取消效果與完成通知；減少動態立即結束所有效果。完成通知遵守 CHAT-STREAM-DONE；自訂效果亦不得繞過字素、文字順序與一次完成邊界 | `chat-effect-reduce-pending`、`chat-effect-remove-pending` |
| CHAT-EFFECT-LAYOUT | 已揭露文字、可用範圍與宿主樣式保持不變，只推進淡入或位移 | 泡泡的寬高、文字排版位置、ScrollView 可捲範圍及閱讀／貼底位置均不得隨效果位移改變；不得以動畫的視覺溢出重算內容尺寸。只有新增／移除或逐字揭露改變實際文字時，才可依文字重排。平台自行隔離繪製與排版，不能用停止正常跟隨掩飾錯誤 | `chat-effect-layout-stability`、CP04 |
| CHAT-EFFECT-SETTINGS | 宿主提供文字表演設定介面 | 淡入／位移各有依序勾選；逐字顯示關閉時，依序勾選保留值但不生效，介面須說明前提或停用該勾選。效果關閉後可隱藏參數但保留有效值。表演時間最短／最長同排，比例與時間只呈現一份；字速只控制逐字揭露，不提供效果獨立速度。分類、可縮放群組、迷你視窗及保存方式由宿主／SettingsPanel 擁有 | CP03 |

### 4. 底部輸入列（ChatComposer：TextField、Mic Toggle、Send Button）

| ID | 前提與操作 | 必須觀察到的結果／邊界 | 驗證案例 |
|---|---|---|---|
| CHAT-COMPOSE-CAPSULE | 底部輸入列（`ChatComposer`）常態佈局 | 預設外框沿 composerHeight／composerRadius／composerBackground；宿主可獨立指定輸入列與訊息泡泡的背景、文字色、圓角或底圖，矩形與膠囊皆可。保留既有 CHAT-COMPOSE-CAPSULE ID 以相容舊案例。內距為 `left = 14, right = 6, top = 7, bottom = 7`；內部由左至右依序排列：`TextField`（佔用其餘可用寬度）、`MicButton`（`34×34px`，`showMic = false` 時不顯示也不保留空間）、`SendButton`（`38×38px`） | `chat-compose-capsule`、`chat-compose-mic-hidden` |
| CHAT-COMPOSE-DRAFT | 使用者在 `TextField` 輸入或修改文字（引用 `TF-CHANGE`、`TF-PLACEHOLDER`） | 空字串時顯示 `Placeholder`（預設 `"想說什麼?"`，不透明度 `0.666`）；每當文字改變時送出草稿變更，並依據是否含非空白字元即時同步 `SendButton` 啟用狀態：全空白時 `SendButton.enabled = false`（`opacity = 0.5`），含非空白時 `SendButton.enabled = true`（`opacity = 1.0`） | `chat-compose-draft` |
| CHAT-COMPOSE-SUBMIT | 使用者點擊啟用中的 `SendButton`，或在輸入框聚焦且**非 IME 組字中**按下 `Enter` | 當草稿含非空白字元時，送出 `Sent(draft)` 事件。`ChatComposer` **不自動清空**草稿；由宿主確認處理成功後呼叫 `ClearDraft()` 清空文字並將 `SendButton` 設回停用態，送出失敗則保留使用者原稿。Shift+Enter 交由輸入欄位換行，不送出；**中文 IME 選字組字中按 Enter 絕不觸發送出** | `chat-compose-submit`、`chat-compose-ime-guard` |
| CHAT-COMPOSE-MIC | 使用者點擊 `MicButton`（引用 `Toggle` `button-fixed`） | 送出 `MicClicked` 事件，不自行變更錄音狀態；由外部語音服務設定 `Recording = true / false`。`Recording = false` 時繪製麥克風圖示（`iconColor`）；`Recording = true` 時繪製錄音紅圓點（`recordingColor = #D94040`）。宿主可設 `showMic = false`（預設 `true`）不顯示麥克風 | `chat-compose-mic`、`chat-compose-mic-hidden` |

| CHAT-COMPOSE-MULTILINE | 輸入多行或換行 | 欄位隨內容增高，最多顯示 composerMaxVisibleLines 行，超過後固定顯示高度並在輸入框右側捲動；刪除內容可縮回單行。麥克風與送出維持右下定位及既有尺寸，不因文字高度置中漂移；不把欄位焦點移走 | `chat-compose-multiline`、CP03 |
| CHAT-COMPOSE-FOCUS | 輸入欄位取得焦點 | 複合輸入列的內部文字欄位採無框外觀，不重複繪製欄位底框／聚焦外框；文字插入點可見，名稱、游標與鍵盤焦點保持可用。此為 ChatPanel 組合選用的內框外觀，不變更獨立 TextField 的 Field 預設外觀 | `chat-compose-focus`、CP03 |
| CHAT-STYLE-HOST | 宿主指定訊息／輸入列外觀 | 使用宿主指定的背景、文字色與圓角，不以 demo 矩形或某一膠囊皮膚強制覆寫；頭像與送出按鈕保留圓形預設及原有啟用語意 | `chat-compose-host-style`、CP03 |

### 5. 圖片訊息與大圖（只有聊天面板用；出現第二個消費端時整節搬出）

圖片訊息的內容是一張圖：圖片來源、可選的原始寬高、可選的說明文字。縮圖最大寬 `thumbMaxWidth` 是該列泡泡可用的最大寬度（同文字泡泡，CHAT-LAYOUT-ALIGN）。大圖的開啟、模態、使用者關閉、關閉通知、焦點返回與卸載依 [TransientSurface](../TransientSurface.md) 共同規則（開啟，SURF-OPEN；模態，SURF-MODAL；使用者關閉，SURF-DISMISS；關閉，SURF-CLOSE；卸載，SURF-DISPOSE），本節只寫聊天面板自己的版面與操作。「可用畫面」是宿主給大圖的範圍（通常是整個畫面扣掉安全區域）。

| ID | 前提與操作 | 必須觀察到的結果／邊界 | 驗證案例 |
|---|---|---|---|
| CHAT-IMAGE-BUBBLE | 呈現一則圖片訊息，圖片已載入（原始寬高 `w × h`） | 縮圖等比縮放、不裁切、不放大超過原尺寸：`scale = min(1, thumbMaxWidth / w, imageMaxHeight / h)`，尺寸 `w × scale` 乘 `h × scale`。圓角為解析出的 `BubbleStyle.CornerRadius`，圖片貼齊泡泡邊緣、沒有內距。無障礙名稱為說明文字，沒有時為「圖片」；縮圖可聚焦、可用鍵盤啟用 | `chat-image-bubble-size` |
| CHAT-IMAGE-LOADING | 新增圖片訊息後圖片尚未載入，或載入失敗 | 帶原始寬高時立即以 CHAT-IMAGE-BUBBLE 的尺寸佔位；沒有時以 `imagePlaceholderSize` 正方形佔位，載入後改成實際尺寸（高度改變依 ScrollView 跟隨或保留閱讀位置）。載入中顯示佔位外觀；失敗顯示失敗外觀（失敗圖示，尺寸維持佔位），無障礙名稱加上「無法載入」。載入中與失敗都不能開大圖 | `chat-image-loading-and-failure` |
| CHAT-IMAGE-OPEN | 點一下（或鍵盤啟用）已載入的縮圖 | 開啟大圖並進入預覽（CHAT-VIEWER-PREVIEW）；依 SURF-OPEN，開啟者是這張縮圖，焦點移入大圖。點載入中或失敗的縮圖不開啟 | `chat-image-open` |
| CHAT-VIEWER-PREVIEW | 大圖開啟後，或從滿版回到預覽 | 圖片等比置中，縮到能完整放進「可用畫面四周各扣 `viewerPreviewInset`」的範圍，不裁切；圖片以外（間距與遮罩）是「外面」。遮罩用 VisualFoundation 的 `overlay` 色彩角色，聊天內容不能操作（SURF-MODAL）。無障礙名稱為圖片說明文字或「圖片」 | `chat-viewer-states` |
| CHAT-VIEWER-FULL | 進入滿版 | 圖片填滿整個可用畫面，四周不留間距；細節見〈Web demo 定案項目〉 | `chat-viewer-states` |
| CHAT-VIEWER-SWITCH | 預覽或滿版中，使用者點一下圖片，或做放大／縮小手勢（雙指張開／捏合；滑鼠滾輪往前／往後一格） | 預覽中點一下圖片或放大 → 滿版；滿版中點一下或縮小 → 預覽。一次雙指手勢結束時，比例相對開始時的變化超過 `viewerZoomGestureThreshold` 才算放大或縮小，否則維持原狀態 | `chat-viewer-states` |
| CHAT-VIEWER-DISMISS | 大圖開啟中 | 預覽中點外面 → 依 SURF-DISMISS（來源 backdrop）關閉；預覽中縮小手勢 → 關閉（原因 cancel）；任何狀態按 Escape → 依 SURF-DISMISS（來源 escape）關閉；應用或分頁失去前景 → 關閉（原因 programmatic）。每次開啟恰好一次關閉通知，焦點回到縮圖（SURF-CLOSE）。手機返回鍵與瀏覽器上一頁不在本目標；平台把返回對應成 Escape 時照 Escape 處理 | `chat-viewer-dismiss` |

### Web demo 定案項目

使用者 2026-11-10 同意下列細節在 Web demo 定案，定案後改版本規範與設計資料：`imageMaxHeight`、`imagePlaceholderSize`、`viewerPreviewInset`、`viewerZoomGestureThreshold` 的值（目前為暫記值）與 ScrollView 的 `followResumeTolerance`；圖片泡泡開時間時的時間位置；滿版是否裁切、能否再放大與平移、滿版時點到圖片外的黑邊是回預覽還是關閉；載入中與失敗的外觀。

### 6. 選用聊天設定

消費端可宣告提供聊天設定入口。設定範圍由宿主指定，例如一個專案或對話介面。可提供的欄位來自本規範的顯示、文字表演、圖片與跟隨選項，預設與有效範圍沿各條文及具名參數。訊息、輸入草稿、錄音狀態、裝置預覽與模擬回覆延遲由各自的執行期宿主擁有。

| ID | 前提與操作 | 必須觀察到的結果／邊界 | 驗證 |
|---|---|---|---|
| CHAT-SETTINGS-GROUP | 宿主在聊天介面或專案設定提供聊天選項 | 入口可以清楚辨識為聊天設定，欄位能找到對應的顯示、文字表演、圖片與跟隨設定。若兩處皆提供同一範圍的欄位，使用同一有效值與還原預設規則。介面外殼、分頁、群組與觸發控件採宿主適用的 SettingsPanel 呈現 | CP06 |
| CHAT-SETTINGS-SYNC | 宿主為同一範圍提供多個入口並採 Configuration 的保存等級 | 有效值成功套用後，聊天介面更新，其他入口的未編輯欄位反映新值。確認保存模式下，尚未提交或保存失敗的草稿只留在原入口；重試合併時保留從其他入口成功保存的相容欄位，包含獨立效果開關。預設與載入（CFG-01）、載入失敗不覆寫（CFG-02）、通知與隔離（CFG-08）沿 Configuration。採等級 2 確認保存時，另引用保存提交順序（CFG-03）、保存失敗與重試（CFG-04）、草稿及模式（CFG-12） | CP06 |

---

## 具名設計資料（Design Tokens）

完整中性設計資料定義於 [`ChatPanel/chat-panel-design.json`](ChatPanel/chat-panel-design.json)。數值源自 UnityExtension `Packages/UIToolkit` 之 C# 實作與範例程式碼，具備明確原始碼位置與 SHA-256 依據；標「暫記」者沒有來源，在 Web demo 定案：

| 參數名稱 | 數值 | 單位 | 來源依據 |
|---|---|---|---|
| `appMaxWidth` | `430` | `px` | `ChatModuleDemo.cs` `AppMaxWidth = 430`（手機欄寬標準） |
| `rowGap` | `8` | `px` | `ChatPresenter.cs` `row.style.marginBottom = 8` |
| `rowMaxWidthPercent` | `85` | `%` | `ChatPresenter.cs` `MaxWidthPercent = 85`（宿主可覆寫） |
| `avatarSize` | `32` | `px` | `ChatPresenter.cs` `AvatarSize = 32` |
| `avatarGap` | `8` | `px` | `ChatPresenter.cs` `AvatarGap = 8` |
| `avatarFallbackColor` | `#667380` | 色彩 | `ChatPresenter.cs` `FallbackAvatarColor = new Color(.4f, .45f, .5f)` |
| `avatarInitialFontScale` | `0.45` | 比例 | `ChatPresenter.cs` `AvatarSize * .45f = 14.4px` |
| `bubbleCornerRadius` | `12` | `px` | `BubbleStyle.cs` `CornerRadius` 預設 `12`（Demo 使用 `14`） |
| `bubblePadding` | `8` | `px` | `ChatPresenter.cs` `ApplyStyle` 內距 `8` |
| `nameFontSize` | `11` | `px` | `ChatPresenter.cs` `name.style.fontSize = 11`（粗體，下外距 `2px`） |
| `timestampFontSize` | `10` | `px` | `ChatPresenter.cs` `time.style.fontSize = 10`（不透明度 `0.65`，上外距 `2px`） |
| `timestampPlacement` | `inside` | 選項 | `ChatPresenter.cs` 泡泡內右下；`beside` 依 VCP `VCP_ChatElement` 的泡泡外側時間 |
| `dateDividerFontSize` | `11` | `px` | `ChatPresenter.cs` `dateDivider.style.fontSize = 11`（不透明度 `0.65`，上下外距 `8px`） |
| `minRowOpacity` | `0.35` | 比例 | `ChatPresenter.cs` `MinRowOpacity = .35f` |
| `scrollbarWidth` | `12` | `px` | `ChatPresenter.cs` `scroller.style.width = 12` |
| `scrollbarDraggerWidth` | `4` | `px` | `ChatPresenter.cs` `dragger.style.width = 4` |
| `scrollbarDraggerMinHeight` | `24` | `px` | `ChatPresenter.cs` `dragger.style.minHeight = 24` |
| `scrollbarDraggerRadius` | `2` | `px` | `ChatPresenter.cs` `dragger` 圓角 `2` |
| `scrollbarFadeDuration` | `180` | `ms` | `ChatPresenter.cs` `transitionDuration = 180ms` |
| `thinkingSecondsThreshold` | `5` | `s` | `ChatReplyPerformance.cs` `Waiting()` 滿 5 秒加後綴 |
| `thinkingDotCyclePeriod` | `1` | `s` | `ChatReplyPerformance.cs` `Waiting()` 句點循環週期 |
| `showThinkingSeconds` | `true` | 選項 | 來源永遠顯示；VCP `UseThinkingLabel = 0` 關閉秒數 |
| `streamingCharsPerSecond` | `30` | 字素/s | `ChatReplyPerformance.cs` 基準揭露速率（宿主可覆寫；VCP 每 0.05 秒一字） |
| `streamingMaxCredit` | `8` | 字素 | `ChatReplyPerformance.cs` 配額累積上限 |
| `composerHeight` | `52` | `px` | `ChatComposer.cs` `Height = 52` |
| `composerRadius` | `26` | `px` | `ChatComposer.cs` `Radius = Height / 2 = 26` |
| `composerBackground` | `#F2F2F2` | 色彩 | `ChatComposer.cs` `PillBackground = new Color(.95f, .95f, .95f)` |
| `micButtonSize` | `34` | `px` | `ChatComposer.cs` `MicSize = 34`（圓形半徑 `17px`） |
| `showMic` | `true` | 選項 | 來源永遠顯示；VCP 聊天沒有麥克風 |
| `sendButtonSize` | `38` | `px` | `ChatComposer.cs` `SendSize = 38`（圓形半徑 `19px`） |
| `sendButtonAccentColor` | `#4F8CFF` | 色彩 | `ChatComposer.cs` `AccentColor = new Color32(79, 140, 255, 255)` |
| `micRecordingColor` | `#D94040` | 色彩 | `ChatComposer.cs` `RecordingColor = new Color(.85f, .25f, .25f)` |
| `composerIconColor` | `#454A4F` | 色彩 | `ChatComposer.cs` `IconColor = new Color(.27f, .29f, .31f)` |
| `composerTextColor` | `#26292E` | 色彩 | `ChatComposer.cs` `TextColor = new Color(.15f, .16f, .18f)` |
| `composerPlaceholderColor` | `rgba(0,0,0,0.666)` | 色彩 | `ChatComposer.cs` `PlaceholderColor` |
| `imageMaxHeight` | `240` | `px` | 暫記 |
| `imagePlaceholderSize` | `160` | `px` | 暫記 |
| `viewerPreviewInset` | `16` | `px` | 暫記 |
| `viewerZoomGestureThreshold` | `0.1` | 比例 | 暫記（雙指比例變化超過 10% 才算放大或縮小） |
| `demoPageBackground` | `#1C1F24` | 色彩 | `ChatModuleDemo.cs` `PageBackground = new Color(.11f, .12f, .14f)` |
| `demoFramePadding` | `16` | `px` | `ChatModuleDemo.cs` `frame.style.padding = 16` |

新增參數取自 Web `73f6d0e` 的實際消費端並依本次使用者要求納規：

| 參數名稱 | 預設 | 單位／語意 |
|---|---|---|
| composerMaxVisibleLines | 5 | 行；超過轉內部捲動 |
| messageAreaMinHeight | 80 | 邏輯 px；仍受可用高度限制 |
| messageFadeTransparentEnd | 20 | 邏輯 px；紀錄上緣至此完全透明，含把手區 |
| messageFadeOpaqueStart | 100 | 邏輯 px；至此恢復完全不透明 |
| textDisplayMode | typewriter | 逐字或 instant |
| textEffects | 0 | 無效果；淡入與位移為可組合開關 |
| fadeInSequential／moveInSequential | false | 各效果獨立選擇依序／隨機 |
| effectPercentage | 100 | %；0～100 |
| effectDurationMin／effectDurationMax | 0.45／0.75 | 秒；非負、min ≤ max |
| effectDistanceMin／effectDistanceMax | 0／16 | 邏輯 px；預設全方位，指向值引用 DirectionField |

所有比例、時間、距離須有限並在有效範圍。設定編輯介面對無效草稿保留最後有效值，不以無效草稿重建進行中回覆；程式入口對無效參數須明確回報拒絕或錯誤，不要求各平台使用同一錯誤型別。這些是效果的體驗選項；插值曲線與取樣數值公式仍由呈現／Motion 能力擁有。

`scrollDragThreshold` 自本版起由 ScrollView 擁有。

---

## 驗收對照與實作依據（Acceptance Mapping）

案例沿用 cases schema 1；平台 adapter 操作真實公開元件，不用 reference model 代算。UnityExtension 欄是 0.1.0-draft 來源的既有測試，屬來源基線的證據路徑，不是本版的平台結果。

| 行為 ID | 中性案例（`chat-panel-cases.json`） | 來源測試與範例 |
|---|---|---|
| CHAT-LAYOUT-ALIGN | `chat-layout-align` | `ChatPresenter` 佈局、`ChatModuleDemo` |
| CHAT-LAYOUT-TEXT-WIDTH | `chat-layout-text-width`、CP05 | Web 選用固定寬度的來源檢查 |
| CHAT-SETTINGS-GROUP／CHAT-SETTINGS-SYNC | CP06 | Web 的聊天 Setting 與專案 Config |
| CHAT-LAYOUT-AVATAR | `chat-layout-avatar` | `ProceduralAvatarGenerator`、`ChatModuleDemo` |
| CHAT-LAYOUT-NAME | `chat-layout-name` | `ChatPresenter.ShouldShowName` |
| CHAT-LAYOUT-TIMESTAMP | `chat-layout-timestamp`、`chat-layout-timestamp-beside` | `ChatPresenter.SetMessages`（只有 `inside`） |
| CHAT-STYLE-MODE | `chat-style-plain` | `ChatPresenter.ResolveStyle` |
| CHAT-STYLE-PLAIN | `chat-style-plain` | `ChatPresenter.ApplyStyle` |
| CHAT-SCROLL-REENGAGE | `chat-scroll-reengage` | `ChatPresenter.FollowBottom`（來源由宿主在送出時呼叫） |
| CHAT-SCROLL-FADE | `chat-scroll-fade` | `ChatPresenterScrollTests.cs`（淡出與關閉 2 案） |
| CHAT-SCROLL-BAR | `chat-scroll-overlay-bar` | `ChatPresenter.StyleOverlayScrollbar` |
| CHAT-TIMER-THINKING | `chat-timer-thinking`、`chat-timer-no-seconds` | `ChatReplyPerformanceTests.cs`（無秒數選項） |
| CHAT-STREAM-SNAPSHOT | `chat-stream-snapshot` | `ChatReplyPerformanceTests.cs` |
| CHAT-STREAM-GRAPHEME | `chat-stream-grapheme` | `ChatReplyPerformanceTests.cs` |
| CHAT-STREAM-PACING | `chat-stream-pacing`、`chat-stream-rate-override` | `ChatReplyPerformanceTests.cs`（速率固定） |
| CHAT-STREAM-REDUCEMOTION | `chat-stream-reducemotion` | `ChatReplyPerformanceTests.cs` |
| CHAT-STREAM-DONE | `chat-stream-done` | 來源沒有 |
| CHAT-COMPOSE-CAPSULE | `chat-compose-capsule`、`chat-compose-mic-hidden` | `ChatComposerTests.cs`、`ChatModuleDemo` |
| CHAT-COMPOSE-DRAFT | `chat-compose-draft` | `ChatComposerTests.cs` |
| CHAT-COMPOSE-SUBMIT | `chat-compose-submit`、`chat-compose-ime-guard` | `ChatComposerTests.cs` |
| CHAT-COMPOSE-MIC | `chat-compose-mic`、`chat-compose-mic-hidden` | `ChatComposerTests.cs`（麥克風永遠顯示） |
| CHAT-IMAGE-BUBBLE | `chat-image-bubble-size` | 來源沒有 |
| CHAT-IMAGE-LOADING | `chat-image-loading-and-failure` | 來源沒有 |
| CHAT-IMAGE-OPEN | `chat-image-open` | 來源沒有 |
| CHAT-VIEWER-PREVIEW | `chat-viewer-states` | 來源沒有 |
| CHAT-VIEWER-FULL | `chat-viewer-states` | 來源沒有 |
| CHAT-VIEWER-SWITCH | `chat-viewer-states` | 來源沒有 |
| CHAT-VIEWER-DISMISS | `chat-viewer-dismiss` | 來源沒有 |

平台與人工檢查：CP01 中文輸入法選字時 Enter 不送出（由 TextField 的 TF-P01 承擔）；CP02 手機上點縮圖開預覽、點一下與雙指在預覽和滿版間切換、捏合關閉、點間距關閉、切到別的 app 後回來大圖已關閉（實體手感）；捲動的手機檢查見 ScrollView 的 SV03。外觀（泡泡、時間位置、遮罩、載入中與失敗）由 agent 以各平台的 verify skill 取證。

---

## 決策紀錄

[第 79 題](../../../decisions/ux-foundation.decisions) 選定最小聊天情境（`single-reply-loop`）；第 74 題定為基礎層先完成；第 75～78 題：UX 擁有語意，Core 抽取由消費端評估；TextField 獨立；沿用來源 snapshot，不新增 delta；貼底歸 ScrollView，聊天淡出留 ChatPanel。

2026-11-10（[ux-chat-panel 決策檔](../../../decisions/ux-chat-panel.decisions)）：

- `chatpanel-goal-1`：目標 1 含圖片訊息與大圖，VCP 第 8、9 項一起承接。依 VCP 現況加入的宿主選項（時間位置、不顯示思考秒數、揭露速率、不顯示麥克風）與揭露完成通知，依據是 VCP 的現有行為。
- `chatpanel-image-open`：點一下縮圖開預覽（四周留間距）；預覽中點一下圖片或放大進入滿版，滿版中點一下或縮小回到預覽；預覽中縮小、點空白處或間距關閉。不用點兩下。
- `transientsurface-goal-1`：不做手機返回鍵與瀏覽器上一頁；大圖以點外面、應用或分頁失去前景、Escape、縮小關閉。TransientSurface 條文不改，補目標 1；失去前景與縮小關閉只有大圖用，留在本模組。
- `chatpanel-composer-scope`：VCP 這一輪只採用訊息區；輸入列等 TextField 有多行目標、本模組加入擴充插槽後再換。

後續候選：停止鈕、串流中鎖定捲動、聚焦不透明、輸入列擴充插槽（含快捷提問鈕）、大圖返回鍵。

## 2026-11-10 案例勘誤

使用者授權修正 Web 引用時發現的來源矛盾：`chat-stream-reducemotion` 的 `pushAndTick` 未宣告 Complete，原預期卻顯示完整文字；依字素邊界與減少動態條文，未完成串流仍保留最後字素。案例 0.2.1-draft 改觀測保留後的文字，再 Complete 並更新，觀測完整文字。行為、參數與案例數不變，不代表平台驗收完成；Web 結果由其 [採用紀錄](../../../../Web/Documentation/Yu5h1Lib.UX/ChatPanel.md) 擁有。


## 2026-11-11 近期更動篩選與交付範圍

本次使用者授權整理近期 Web ChatPanel 更動。既有目標 1 的送出／閱讀／回覆組合保留，新增下列選用能力；沒有把新增條文或既有 Web 證據當成其他平台通過，也不把目標 1 改為已達成。Web 的來源基線、版本與精確結果見 [採用紀錄](../../../../Web/Documentation/Yu5h1Lib.UX/ChatPanel.md#近期更動納入-ux2026-11-11)。

| 近期更動 | 歸屬與處理 |
|---|---|
| 滿 5 秒後固定三點並更新秒數 | 已由 CHAT-TIMER-THINKING 擁有，不另訂規則 |
| 五行輸入、右側內部捲動、Caret 與按鈕右下 | 納入 CHAT-COMPOSE-MULTILINE／FOCUS；草稿、IME、提交仍由 TextField 擁有 |
| 紀錄上緣淡至完全透明、隱藏高度把手 | CHAT-SCROLL-FADE／CHAT-RESIZE-MESSAGES；ScrollView 跟隨規則不複製 |
| 一次／逐字顯示、組合效果、各自依序或隨機、比例與時間 | 納入選用文字表演；平台繼承類別、flags 型別與 DOM 結構留在實作端 |
| 指向／全方位與距離 min／max | 引用 DirectionField 值語意；控件本身的拖曳、鍵盤與鎖定鍵不抄入聊天 |
| 字速與設定顯隱、有效值保留 | 納入 CHAT-EFFECT-SETTINGS；不要求某種 dropdown／foldout 技術 |
| 原生設定 header、拖拉、透明度、鎖定、重開位置與大小 | 共用 SettingsPanel／宿主視窗責任，本輪不新增聊天專用視窗規範 |
| PC／手機直橫預覽容器 | Web demo 工具；聊天只要求可用範圍改變時保留狀態 |
| 思考時間隨機 0～5 秒 | 宿主模擬回覆延遲，非聊天計時器必須等待的規則 |
| 矩形 demo、實心白色三角信封 SVG | 矩形由 demo 宿主選擇；實心白色 SVG 是 Web 本輪預設圖示素材，未宣告為宿主可替換的圖示入口。背景／圓角可覆寫及圓形頭像／送出預設為本模組契約；特定 SVG 路徑與填色不成為所有平台必須複製的圖形 |

CP03：在來源實際輸入多行、Shift+Enter、刪除縮回，檢查游標、五行捲動及按鈕右下；切換兩效果、各自依序、共用字速、比例／時間與隱藏參數保留。CP04：實際拖拉訊息上緣、取消及鍵盤調整，切換可用範圍後核對訊息／草稿／回覆仍保留。隨機字詞的檢查使用平台可注入的亂數來源，觀察比例、位移及時間只抽樣一次，字詞可顯示時立即表演；不要求跨平台相同 PRNG 序列、抽樣呼叫次序或插值中間數值。中性案例只比較揭露邊界、效果進行／完成、範圍與最終歸位。這些可觀察畫面由平台 verify 工具取證；實體觸控手感與真實 IME 仍沿原人工界線。

既有〈Web demo 定案項目〉繼續待定，本輪沒有擅自確認暫記圖片參數或大圖細節。VCP 仍只採訊息區；輸入列更換及其他產品整合另依原計畫。


## 2026-11-11 依序觸發語意勘誤

使用者指出 0.3.0-draft 把「隨逐字出現就表演」誤解為獨立排程。依本次明確確認，只有「逐字顯示開啟＋該效果依序勾選」才跟隨新字素立即表演；其餘情況回到早期字詞效果，隨機選取比例、位移與時間，不抽等待、不排隊。字速不在效果層重用為另一個時鐘。0.3.0 的窗口／onset 與關閉逐字仍逐字觸發的條文及案例由本版取代，設計預設值未變。

位移改變 ScrollView 的 overflow 尺寸屬實作錯誤，不是允許的效果差異；本版以固定全文驗證文字框與捲動幾何在表演中及結束時一致。平台證據仍見 Web 採用紀錄，其他平台結果不延伸。

## 2026-11-11 固定文字框寬度與聊天設定

消費端各自宣告是否採用固定文字框寬度（CHAT-LAYOUT-TEXT-WIDTH）或聊天設定（CHAT-SETTINGS-GROUP、CHAT-SETTINGS-SYNC）。既有案例與設計預設維持相容，新增一個文字框案例。Web 的緊湊輸入列、實心麥克風和三角信封比例是展示與平台圖示的選擇。

- CP05：在一般與窄版視窗從首字揭露至多行，量測文字泡泡的寬度與高度。保持相同可用範圍時寬度一致，換行時高度增加。
- CP06：從聊天設定改顯示欄位，在專案設定讀回。確認保存模式再測手動草稿、套用與取消；若保存失敗後另一入口成功修改不同欄位，重試後兩項變更都保留。持久化的宿主重載後讀回有效值，另測還原預設與損毀記錄的讀取重試。只有一個入口時驗該入口的有效值、預設與所宣告的保存等級。

來源證據與平台範圍見 [Web 採用紀錄](../../../../Web/Documentation/Yu5h1Lib.UX/ChatPanel.md#固定文字框寬度與聊天設定採用)。目標 1 與暫記圖片參數仍沿原驗收路徑。
