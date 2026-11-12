# uGUI ChatPanel 示範版對齊計畫

> Task ID: ui-chatpanel-demo-parity

```text
Yu5h1Lib.UI／UxAcceptance
├─ Demo 入口 → 搜尋、模組格線、返回與重設
└─ ChatPanel 示範 → 預覽範圍、測試操作與模擬宿主
   ├─ 共用基礎 → 色彩角色、按鈕、文字輸入與捲動
   └─ ChatPanel 訊息區 → 泡泡、歷史、逐字回覆、圖片與大圖
```

## 試用發現

使用者提供的 VCP 直向截圖中，頂列的模組名稱與狀態文字被截斷，聊天工具列和輸入列的按鈕拉成高矩形，擠壓訊息區。Web 的聊天框把測試操作放在聊天框之外，框內讓訊息區吃剩餘高度，底部輸入列維持內容高度。這些差異直接影響使用者能否找到 ChatPanel、讀到足夠訊息，以及在手機上輸入和送出。[Unity 驗收宿主](../Packages/UI/Samples~/UxAcceptance/UxUiAcceptanceHost.cs) · [Unity 聊天示範](../Packages/UI/Samples~/UxAcceptance/Cases/UxUiChatPanelDemo.cs) · [Web 版面](../../../Web/demo/chat-panel/demo.css) · [Web 示範資料](../../../Web/demo/chat-panel/fixture.mjs)

- **聊天區高度。** `UxUiChatPanelDemo.AddRow` 將工具列與輸入列設為 36／44 的 preferred height，但沒有明訂 flexible height；原生 `HorizontalLayoutGroup.childForceExpandHeight` 預設開啟，列本身因子控件獲得 flexible height。外層 `VerticalLayoutGroup` 因此會將剩餘高度分給兩列與訊息區。 VCP 6000.6.0f1 EditMode 在 360 × 600 的 sample 框量得工具列 187.3、輸入列 195.3、訊息區 151.3（Canvas 邏輯單位，`20261112-122115-tests.xml`）。修正時先讓工具列依內容收合，輸入列高度由可見行數與控件決定，僅訊息區吸收剩餘空間。
- **宿主窄版。** 舊版 `UxUiAcceptanceHost` 的單列選擇器和固定 520 內容高度造成截斷。搜尋格線和獨立案例頁已由 [AcceptanceDemo](../Packages/UI/Documentation/Yu5h1Lib.UX/AcceptanceDemo.md) 交付，聊天室接收實際可用寬高。
- **比較基線。** uGUI 示範採 VCP 的 70% 泡泡寬、外側時間與一對一隱藏頭像。Web 示範另有頭像、桌面／手機直橫預覽、設定入口與表演選項。並排比較需先統一資料與呈現選項，才能分辨版面差異與已選定的產品差異。[VCP 適配表](../../../UX/Documentation/ModuleSpec/Modules/ChatPanel.md#適配表vcp-ugui-聊天面板) · [uGUI 採用紀錄](../Packages/UI/Documentation/Yu5h1Lib.UX/ChatPanel.md)

## 共用基礎的取捨

| 先後 | 能力 | 方向 |
|---|---|---|
| 先修 | 示範的可用空間與緊湊控制列 | 用原生 `RectTransform`、`LayoutElement`、`LayoutGroup` 表達伸展規則。操作列在桌面置於聊天框外側，窄版換行或收合；示範內先重用 `ButtonAddon`，保留清楚的文字與操作狀態。 |
| 同步補齊 | uGUI 的色彩與控件狀態 | `ButtonAddon` 已提供原生 Button、圖文配置與 Solid／Ghost，可直接重用。`GraphicColorBinding` 等既有綁定可接 Graphic。需要補的是同一組 VisualFoundation 角色到 uGUI 底色、文字、外框、焦點與停用的對應，供入口與聊天示範共用。驗收包含明暗與鍵盤焦點。 [Button 紀錄](../Packages/UI/Documentation/Yu5h1Lib.UX/Button.md) · [VisualFoundation](../../../UX/Documentation/ModuleSpec/VisualFoundation.md) |
| 同步補齊 | 欄位、輸入列與字型 | 入口搜尋先用原生單行欄位，依 SearchField 行為接值與清除。聊天示範重用原生輸入及 `InputFieldAddon` 的可見列數與游標捲動；宿主已有 TMP 時可用既有 adapter。多行增高、明確提交及傳送後清空屬 ChatPanel 組合，需另測 1 行、5 行、超過 5 行、中文 IME、鍵盤／觸控送出及中英混排。共用基礎維持既有套件依賴。 [SearchField](../../../UX/Documentation/ModuleSpec/Controls/SearchField.md) · [TextField](../../../UX/Documentation/ModuleSpec/Controls/TextField.md) · [ChatPanel](../../../UX/Documentation/ModuleSpec/Modules/ChatPanel.md) |
| 直接重用 | 訊息捲動與訊息區 | 沿用 `FollowingScrollView` 和既有 ChatPanel 公開入口驗證貼底、閱讀錨點、歷史重載、逐字及圖片。縮圖目前用原生 Image；大圖沿既有檢視器，等圖層裁切、焦點與暫定 UX 行為的證據齊備再決定共用 Surface 的範圍。 [ScrollView 紀錄](../Packages/UI/Documentation/Yu5h1Lib.UX/ScrollView.md) |
| 後續選用 | 頭像、圖片圓角、表演設定及拖曳調高 | 依使用者比對後選定的範圍排入後續。純呈現先採 Sprite／Mask 或既有組件；可重用的規則有實際第二個消費端後再抽取。 |

## 可分派的工作

| 工作 | 檔案邊界 | 完成條件與相依 |
|---|---|---|
| A．修正聊天示範排版 | `Packages/UI/Samples~/UxAcceptance/Cases/UxUiChatPanelDemo.cs` 與示範版面測試 | 先量測舊版工具列、輸入列與訊息區矩形，記錄直向重現。修正後在桌面、手機直向與橫向量測，工具列／輸入列維持內容高度，訊息區填滿其餘空間，窄版操作可見可點。可以與入口、色彩和欄位工作並行。 |
| B．補齊 uGUI 共用色彩與欄位 | 套件的 Button、Field／TextField、既有 adapter 與它們的測試；共用 UI 元件各有單一維護入口 | 先完成入口按鈕與搜尋欄位可共用的底色、焦點與文字；聊天再接同一色彩角色及多行組合。以入口搜尋和聊天輸入兩個使用情境驗證。 |
| C．以同一基線驗收 | 整合 A、B 與 AcceptanceDemo 入口後更新套件採用紀錄及驗收證據 | 在固定有效聊天框尺寸，以同一短對話、長歷史、等待回覆、長草稿和圖片資料比對 Web 與 Unity。使用相同泡泡寬度、頭像開關、時間位置與主題。擷取兩端版面、控件狀態與實際矩形。另跑捲動與串流回歸，實機檢查觸控、鍵盤與中文 IME。 |

畫面驗收以實際輸出為準。以桌面、直向及橫向各一個可重現視口比對可讀區、按鈕及輸入列高度，並核對新增長訊息時泡泡寬度穩定、只有文字換行增加高度。使用者試用的是 VCP 已匯入的獨立 sample；產品採用仍以訊息區為範圍，正式場景的遷移及舊版退役留待視覺和實機驗收後決定。

## 本輪交付

A 已在 sample 完成。工具列依寬度換行，輸入列由可見草稿行數決定高度，訊息區取得其餘空間。B 交付 `UguiControlPalette`、`InputFieldState` 和 `MultilineInputField`，共用入口和聊天 sample 都採用同一組配色、焦點與原生欄位狀態。聊天輸入列沿用無框 caret 的顯示方式，五行後在欄位右側捲動。三種視口的數字及驗證限制見 [ChatPanel 採用紀錄](../Packages/UI/Documentation/Yu5h1Lib.UX/ChatPanel.md#示範版高度與共用控件)。

C 已用同一組訊息、圖片和聊天框尺寸完成 Web 與 VCP sample 的並排驗收。修正輸入列、泡泡與圖片預覽，並在 VCP 重新執行聊天、捲動及共用控件回歸。尺寸、證據及剩餘差異見 [並排驗收紀錄](../Packages/UI/Documentation/Yu5h1Lib.UX/ChatPanel.md#web-與-ugui-並排驗收)。實體手機觸控、中文 IME 與產品場景採用留待試用。
