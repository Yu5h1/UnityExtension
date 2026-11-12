
# ChatPanel 訊息區（Yu5h1Lib.UI）

> Task ID: ui-chatpanel

本輪採用 VCP 的一對一聊天訊息區。VCP 的輸入列、快捷提問、請求、語音與舊聊天場景由 VCP 管理。共用驗收場景用本機資料模擬送出、快捷提問及回覆。

```text
VCP 宿主／UxAcceptance sample
├─ 輸入列、快捷提問與非同步資料
└─ Yu5h1Lib.UI.ChatPanel
   ├─ 訊息列、日期、外側時間與圖片
   ├─ 累積快照、逐字揭露與完成通知
   ├─ FollowingScrollView → 原生 uGUI ScrollRect
   └─ 圖片檢視 → 原生 uGUI Canvas 與事件系統
```

## 釘選版本

| 交付 | 版本與來源 |
|---|---|
| [ChatPanel](../../../../../../UX/Documentation/ModuleSpec/Modules/ChatPanel.md) | 規範 0.4.0-draft、設計 0.3.0-draft、案例 0.4.0-draft |
| [ScrollView](ScrollView.md#ugui-訊息區採用) | 規範 0.2.2-draft、設計 0.2.0-draft、案例 0.2.1-draft |
| [UX manifest](../../../../../../UX/Documentation/ModuleSpec/manifest.json) | UX `b95b80af4611efd03995de8b90b53fd692a4326d` |
| 快照 | [ChatPanel](../../Tests/Editor/UX~/ChatPanel/) 與 [ScrollView](../../Tests/Editor/UX~/ScrollView/)。EditMode 檢查六份 SHA-256 及套件的具名預設值。 |

## 範圍與對照

| UX 行為 | 入口與檢查 |
|---|---|
| 左右對齊、固定文字寬度、日期和外側時間、泡泡樣式（CHAT-LAYOUT-ALIGN、CHAT-LAYOUT-TEXT-WIDTH、CHAT-LAYOUT-TIMESTAMP、CHAT-STYLE-MODE） | `ChatPanel.Configure`、`AddMessage`。原生 LayoutGroup 排列。可設定自己／對方的底色、文字色與九宮格 Sprite。Sample 用 70% 列寬。 |
| 貼底、跟隨、保留閱讀位置、恢復跟隨（ScrollView 目標 1、CHAT-SCROLL-REENGAGE） | `FollowingScrollView.RefreshContent` 與 `RequestFollow`。原生捲輪、拖曳與捲軸輸入。訊息區新增自己的即時訊息及開始回覆時要求貼底，用 `ReplaceHistory` 和穩定訊息 ID 重建歷史時保留閱讀位置及跟隨狀態。Sample 採用觸控式覆蓋捲軸，捲動時顯示。 |
| 思考中、累積快照、完整字素、宿主字速、減少動態與一次完成通知（CHAT-TIMER-THINKING、CHAT-STREAM-*） | `BeginReply`、`Reply.Push`、`Reply.Complete`、`Reply.RevealCompleted`。宿主決定何時開始下一句。與 UI Toolkit 共用字素邊界計算。Sample 採 20 字素／秒並顯示思考秒數，宿主可關閉秒數。 |
| 縮圖、佔位、載入失敗與圖片預覽（CHAT-IMAGE-*、CHAT-VIEWER-*） | `AddImage`、`ImageMessage.SetLoaded`／`SetFailed`，由宿主管理 Sprite 和載入。縮圖可用原生 Button 選取。檢視器支援宿主安全區、預覽／滿版、點擊、滾輪、雙指、可否決的取消、失焦與焦點回復。Sample 縮圖採矩形。圓角裁切、輔助技術讀名與實機手勢屬後續驗收。 |
| 可用範圍改變（CHAT-LAYOUT-ADAPT） | 改變 RectTransform 後重排目前訊息，回覆狀態維持。 |
| 本輪能力選擇 | 一對一隱藏頭像及名稱。輸入列歸 VCP 宿主；漸層淡出、文字表演擴充、拖曳調高與共用設定留待後續選用。 |

[TransientSurface 1.1.2](../../../../../../UX/Documentation/ModuleSpec/TransientSurface.md) 有共用規則，uGUI 尚無獨立採用驗證。圖片檢視器使用原生 Canvas 與 EventSystem，證據只涵蓋下列 ChatPanel 測試範圍。Button 沿用原生 Selectable 行為，完整的 [uGUI Button](Button.md) 與 VisualFoundation 色彩驗收沿各自紀錄。TextField、Toggle、DirectionField 與聊天設定未列入本輪範圍。組裝入口以既有 UxAcceptance 場景及 `ChatPanel.Configure` 驗證，產品場景的組裝仍待後續 VCP 採用。

## 試用範圍

使用者在 VCP 直向試用發現訊息區遭工具列與輸入列擠壓。成因與共用元件取捨見 [uGUI ChatPanel 示範版對齊計畫](../../../../Documentation/ChatPanelDemoParityPlan.md)。

VCP → Package Manager → Yu5h1Lib.UI → Samples → UxAcceptance → Import。開啟 `UxUiAcceptanceDemo.unity` → Play → 選 **ChatPanel (0.4.0-draft)**。

- 短對話 → 送出一句 → 觀察貼底、等待 6 秒與逐字完成通知。
- 載入歷史 → 往回捲 → 觀察圖片載入及閱讀位置 → 捲到底或點「回到最新」。
- 角色貼文 → 點已載入縮圖 → 試點擊、縮放與關閉。另一張圖片示範載入失敗。
- 改變 Game View 的寬度與方向 → 觀察回覆和閱讀位置。手機確認捲動、直橫版與雙指手感。

圖片高度 240、佔位 160、預覽留白 16、雙指門檻 10%、恢復跟隨容許距離 8，均為草案暫記值。Sample 的滿版保持完整圖片，黑邊點擊回預覽。載入與失敗用「…」和「×」。這些外觀與手勢細節在 [UX 待定項目](../../../../../../UX/Documentation/ModuleSpec/Modules/ChatPanel.md#web-demo-定案項目) 定稿後再重測。

共用元件此次驗證 VCP 的 Input System 滾輪步距與草案列出的字素。其他宿主的精細滾輪需依輸入模組設定 `ScrollRect.scrollSensitivity`，舊 Mono 的少數 ZWJ 字素和長按捲軸軌道時的跟隨交接仍待擴充驗證。

## 示範版高度與共用控件

工具列使用 uGUI 格線，每列高 36，窄版按可用寬度換行。輸入列的單行高度是 44。草稿逐行增加，最多顯示五行，超出時右側出現捲軸。送出按鈕固定在輸入列右下。角色貼文與示範狀態放在聊天框上方。

`UguiControlPalette` 提供明暗色彩角色、鍵盤焦點與停用外觀。驗收入口及聊天 sample 使用暗色配色。入口搜尋採可見焦點框，聊天的內嵌欄位用無框 caret。`InputFieldState` 回報原生欄位狀態，`MultilineInputField` 負責高度、可見行數及捲動，編輯與選取由原生 InputField 處理。

| 首輪直接量測的 sample 外框 | 工具列 | 輸入列，單行 → 五行 → 單行 | 訊息區，單行 → 五行 → 單行 |
|---|---:|---:|---:|
| 桌面 1280 × 720 | 36 | 44 → 87 → 44 | 574 → 531 → 574 |
| 手機直向 360 × 600 | 36 | 44 → 87 → 44 | 454 → 411 → 454 |
| 手機橫向 640 × 360 | 36 | 44 → 87 → 44 | 214 → 171 → 214 |

尺寸為 Canvas 邏輯單位。VCP 6000.6.0f1 的 batchmode EditMode 42/42（`20261112-131032-tests.xml`），包含色彩與焦點、欄位狀態、捲動、聊天和入口回歸。PlayMode 1/1（`20261112-131050-tests.xml`）載入 VCP 實際匯入的場景，在三種視口測得上表並確認右側捲軸顯示和隱藏。後續並排驗收量測的是扣除操作列和狀態文字後的聊天框。

## Web 與 uGUI 並排驗收

2026-11-12 使用同一組跨日短對話、長歷史、5 秒思考提示、七行草稿和大圖預覽。兩端使用暗色、70% 訊息列寬及外側時間，關閉頭像、麥克風和訊息漸層。圖片取自 Web demo 的 `scene.svg`，sample 使用同一素材轉成的 Sprite。

| 聊天框尺寸 | Web 與 Unity 的輸入列，單行 → 五行 | Web 與 Unity 的訊息區，單行 → 五行 |
|---|---:|---:|
| 桌面 1280 × 720 | 44 → 111 | 670 → 603 |
| 手機直向 360 × 600 | 44 → 111 | 550 → 483 |
| 手機橫向 640 × 360 | 44 → 111 | 310 → 243 |

兩端輸入列左右各內縮 6，與訊息區間距為 6。送出按鈕以 38 × 38 固定在右下，圓形底上使用白色實心信封。uGUI sample 採用共用色彩、14 號訊息字級、單行外側時間與 8 單位底部留白。大圖及關閉按鈕收在聊天框內，預覽保留至少 16 單位留白。原生 uGUI 字形、長文換行、捲軸和關閉按鈕的觸控區仍有平台差異。

Web `verify-web` 通過 15/15，擷取與量測存於 Web 的 `.verify/chat-panel-parity-probe/2026-11-12T05-32-01-221Z/`，`side-by-side.html` 收錄全部 15 組畫面。比對腳本核對兩端的訊息文字、聊天框、訊息區、輸入列、送出按鈕、泡泡寬度及大圖矩形。VCP 6000.6.0f1 的 EditMode 42/42（`20261112-133340-tests.xml`），PlayMode 1/1（`20261112-133401-tests.xml`），0 個編譯錯誤。PlayMode 從 VCP 實際匯入的 sample 場景擷取畫面，並檢查草稿捲動、訊息貼底和角色圖片的載入與失敗。實體手機觸控與中文 IME 待試用。VCP 正式場景仍使用既有聊天實作。

## VCP batchmode 驗證

以 VCP 現有的 `file:` 套件參照載入 Yu5h1Lib.UI。驗證時暫時匯入 sample 與這一輪 EditMode 測試，執行後移除暫用 Assets。EditMode 23/23（`20261112-085728-tests.xml`），PlayMode 1/1（`20261112-090058-tests.xml`）。報告在 VCP 專案旁的 `Virtual-Chat-Partner-Build/logs`。結果與人工確認見 [驗收清單](../../../../checklists/ui-chatpanel.checklist)。EditMode 驗證元件行為，PlayMode 驗證 sample 場景的宿主流程。手機觸控與產品外觀由使用者試用確認。
