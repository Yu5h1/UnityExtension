# 捲動視窗（ScrollView）

> 狀態：規範草案；目標 1 進行中（2026-11-10 確認），尚無平台結果
>
> Layer: Collections（集合層）
>
> Specification version: 0.2.2-draft（2026-11-10；案例勘誤：保留位置情境的可視高度由 600 改為 500，使起始 offset 500 位於合法範圍；行為與設計資料不變）。前一版：0.2.1-draft（2026-11-10；只改來源基線紀錄的連結，行為、參數與案例不變）。前一版：0.2.0-draft（2026-11-10；目標 1 確認，寫入目標 1 的六條行為、參數與案例，登錄 manifest）、0.1.0-draft（2026-10-19，自 `OptionPicker`、`TransientSurface`、`Input` 與 Unity `ChatPresenter`／`ScrollRectEx` 收斂的規劃草案）
>
> 設計資料版本：0.2.0-draft（[`ScrollView/scroll-view-design.json`](ScrollView/scroll-view-design.json)）
>
> 案例資料版本：0.2.1-draft（[`ScrollView/scroll-view-cases.json`](ScrollView/scroll-view-cases.json)）

捲動視窗在內容超出可視範圍時裁切超出部分、讓使用者捲動，並決定內容增長時可視位置怎麼變；它不決定內容是什麼，也不處理內部項目自己的按壓與拖曳。

## 系統階層與關係

```text
Yu5h1Lib.UX
├─ 捲動視窗（ScrollView，集合層）
│  ├─ 目標 1：溢位與夾限、短內容貼底、跟到最新、使用者捲動停止跟隨、保留閱讀位置、恢復跟隨
│  └─ 後續候選：鍵盤捲入視野、手勢仲裁、邊緣自動捲動、捲軸
├─ 引用：操作（Input）的滾輪與拖曳語意
└─ 消費端
   └─ 聊天面板（ChatPanel）訊息區：決定何時要求恢復跟隨；距離淡出與覆蓋捲軸留在 ChatPanel
```

## 目標 1：訊息流貼底、跟到最新、往回看不被拉走

使用者 2026-11-10 在 [ux-chat-panel 決策檔](../../../decisions/ux-chat-panel.decisions) 確認（`scrollview-goal-1` 採用提案；`scrollview-follow-resume` 選捲回底端就恢復）。

- **為誰服務**：內容從尾端增加、從最新一端閱讀的捲動區，例如對話訊息流。證據路徑：聊天面板（[ChatPanel](../Modules/ChatPanel.md)）的訊息區——來源是 UnityExtension UI Toolkit 的聊天呈現（`Packages/UIToolkit/Runtime/Chat/ChatPresenter.cs` 與 `ChatPresenterScrollTests`），第一個非來源消費端是 VCP 的 uGUI 聊天面板（ChatScene）。目前只有聊天面板一個消費端；獨立成集合層規範的理由是平台差異必須釘住（第 78 題已定貼底歸 ScrollView）：uGUI、UI Toolkit 與瀏覽器的原生捲動區都從頂端排、不會跟到底，UI Toolkit 用底端對齊時高度會鎖死，VCP 的回收式清單重建時會跳回頂端。
- **情境**：
  1. 內容不超過可視範圍時不能捲、不顯示捲軸；超過時捲動位置始終在頂端到底端之間，可視範圍改變尺寸後也一樣（溢位與夾限，SCROLL-OVERFLOW）。
  2. 內容短於可視範圍時貼齊底端，從底部往上長；長到超過後轉為一般捲動（短內容貼底，SCROLL-BOTTOM-ANCHOR）。
  3. 跟隨中，新增內容、既有內容長高（例如逐字顯示、圖片載入完成）或可視範圍改變尺寸時，版面更新後畫面停在最底端（跟到最新，SCROLL-FOLLOW）。
  4. 使用者轉動滾輪、拖曳內容超過門檻或操作捲軸時立即停止跟隨；之後內容繼續增加也不把畫面拉回底端（使用者捲動停止跟隨，SCROLL-FOLLOW-STOP）。
  5. 不跟隨時，內容在尾端增加、既有項目重新排版或整份重建，使用者正在看的內容留在原位，不跳回頂端（保留閱讀位置，SCROLL-KEEP-POSITION）。
  6. 宿主要求跟隨（例如使用者送出訊息、開始新回覆）時立即到底並恢復跟隨；使用者自己捲回底端附近時也恢復（恢復跟隨，SCROLL-FOLLOW-RESUME）。
- **不在本目標內**：鍵盤移動焦點時捲入視野、內部項目的手勢仲裁（第 69 題的原型候選）、拖曳到邊緣自動捲動、捲軸外觀與翻頁、在頂端插入較舊內容時保持位置（往回載入更早的歷史）、水平捲動、超出邊界的回彈曲線（屬 Motion）、到底的捲動動畫（本目標只規定最後停在哪裡）、清單回收等效能做法（屬實作）。
- **完成條件**：本目標行為的中性案例透過公開入口，在 Web demo 與 UnityExtension uGUI（VCP 採用的實作）都通過；UI Toolkit 來源建立基線紀錄；使用者在手機上確認觸控拖曳會停止跟隨、捲回底端會恢復（實體手感）。
- **狀態**：進行中（2026-11-10）。

### 適配表：VCP uGUI 聊天面板

| 行為 | VCP 需要嗎（現況） | uGUI 原生機制 |
|---|---|---|
| 溢位與夾限 | 需要（清單用回收式捲動，拖曳可回彈） | ScrollRect 會夾限；回彈屬允許差異 |
| 短內容貼底 | 需要（現況短對話排在頂端） | 相反：從頂端排 |
| 跟到最新 | 需要（現況每打一字都強制捲到底） | 沒有 |
| 使用者捲動停止跟隨 | 需要（現況沒有，捲上去會被拉回） | 沒有；拖曳開始門檻由 EventSystem 判定 |
| 保留閱讀位置 | 需要（現況重建清單時跳回頂端，VCP `chat-list-scroll-zero`） | 一般 ScrollRect 會保留；VCP 的回收式清單重建時不保留 |
| 恢復跟隨 | 需要（送出、角色開始回覆、捲回底端） | 沒有 |

### 前置條件

| 引用 | 狀態 |
|---|---|
| [操作（Input）](../Foundation/Input.md) 4.0.0：滾輪與拖曳的判定語意 | 目標 1 已達成（Web、UI Toolkit）；uGUI 無結果，本目標只用平台原生的滾輪與拖曳事件 |
| 外觀 | 本目標不規定外觀，不需要 VisualFoundation |

### 能力盤點

| 能力 | 擁有者 |
|---|---|
| 溢位、貼底、跟隨、停止與恢復跟隨、保留閱讀位置 | 本規範目標 1 |
| 什麼時候要求恢復跟隨（送出、開始回覆） | ChatPanel（組合規則） |
| 由下往上距離淡出、懸停覆蓋捲軸 | ChatPanel |
| 鍵盤捲入視野、手勢仲裁、邊緣自動捲動、捲軸 | 本規範後續目標（〈後續候選〉） |

## 目標 1 行為

座標：垂直方向，以 px 計。`viewportSize` 是可視範圍高度，`contentSize` 是內容高度，`offset` 是可視範圍頂端到內容頂端的距離，`maxScroll = max(0, contentSize − viewportSize)`，「在底端」指 `offset = maxScroll`。`following` 是跟隨狀態；宿主以 `followBottom` 啟用跟隨能力（預設關閉，聊天面板開啟），啟用時 `following` 初始為 true。宿主以 `bottomAnchor` 啟用貼底（預設關閉）。

| ID | 前提與操作 | 必須觀察到的結果／邊界 | 可覆寫與允許差異 | 案例 |
|---|---|---|---|---|
| SCROLL-OVERFLOW | 內容或可視範圍尺寸改變；使用者或程式捲動 | `contentSize ≤ viewportSize` 時 `maxScroll = 0`、`offset = 0`，不顯示捲軸，滾輪與拖曳不移動內容。超過時，每次捲動、內容改變或可視範圍改尺寸後，`offset` 夾在 `[0, maxScroll]` | 拖曳中可以暫時超出範圍並回彈，放開後回到範圍內；回彈曲線屬 Motion | `scroll-overflow-clamp` |
| SCROLL-BOTTOM-ANCHOR | `bottomAnchor` 開啟，`contentSize < viewportSize` | 內容底緣貼齊可視範圍底緣，上方留白 `leadingSpace = viewportSize − contentSize`；`contentSize ≥ viewportSize` 時留白為 0，轉為一般捲動 | 不規定版面做法（外距、對齊或位移皆可），只看內容底緣位置 | `scroll-bottom-anchor-and-follow` |
| SCROLL-FOLLOW | `following = true`；新增內容、既有內容長高或縮短、可視範圍改尺寸 | 版面更新完成後的第一次畫面更新內 `offset = maxScroll`。直接到底，不規定動畫 | — | `scroll-bottom-anchor-and-follow`、`scroll-follow-resume-at-bottom` |
| SCROLL-FOLLOW-STOP | `following = true`、`maxScroll > 0`；使用者轉動滾輪或觸控板、按下內容後移動距離 ≥ `scrollDragThreshold`、拖曳捲軸滑塊或點軌道 | 立即 `following = false`；之後 SCROLL-FOLLOW 不再移動畫面，內容依 SCROLL-KEEP-POSITION。移動距離取按下點到目前點的直線距離，等於門檻即停止 | 門檻可改用平台判定「拖曳開始」的門檻；程式改變 `offset` 不算使用者捲動 | `scroll-follow-stop-and-resume`、`scroll-drag-below-threshold`、`scroll-wheel-stops-follow` |
| SCROLL-KEEP-POSITION | `following = false`；內容在尾端新增、既有項目長高或縮短，或同一份內容整份重建（例如回收式清單重新填入） | 可視範圍頂端那一項在畫面上的位置不變；那一項已不存在時 `offset` 不變。`offset` 只會因 SCROLL-OVERFLOW 的夾限而改變，不會被重設為 0 | — | `scroll-keep-position` |
| SCROLL-FOLLOW-RESUME | (a) 宿主要求跟隨；(b) `following = false` 時，使用者的一次捲動結束（放開指標且慣性停止，或一次滾輪事件之後）時 `maxScroll − offset ≤ followResumeTolerance` | (a) `following = true` 並立即到底；(b) `following = true`，之後依 SCROLL-FOLLOW。拖曳進行中不恢復 | 宿主何時要求跟隨由消費端決定（聊天面板：CHAT-SCROLL-REENGAGE） | `scroll-follow-stop-and-resume`、`scroll-follow-resume-at-bottom` |

內容不超過可視範圍時 `offset = maxScroll = 0`，使用者在這時的操作不會停止跟隨（SCROLL-FOLLOW-STOP 要求 `maxScroll > 0`）。

## 具名參數

完整資料在 [`ScrollView/scroll-view-design.json`](ScrollView/scroll-view-design.json)。

| 參數 | 值 | 單位 | 依據 |
|---|---|---|---|
| `scrollDragThreshold` | 4 | px | UI Toolkit `ChatPresenter.cs` `ScrollDragThreshold = 4f`（原 ChatPanel 的同名參數） |
| `followResumeTolerance` | 8 | px | 暫記值：來源沒有這條規則（2026-11-10 使用者決定加入），沿用舊草案的 `followSnapTolerance` 8，在 Web demo 定案 |

## 案例與交付

沿用 cases schema 1：[`ScrollView/scroll-view-cases.json`](ScrollView/scroll-view-cases.json)。每步 `expect` 是局部觀測；尺寸與位置以 px 精確比較。`pointerDrag` 的 `deltaY` 為正表示指標往下移、畫面往頂端捲；`wheel` 的 `deltaY` 為負表示往頂端捲；「放開」指指標抬起且慣性已停；`topVisibleItemScreenY` 是可視範圍頂端那一項的上緣相對可視範圍頂端的位置。`scroll-bottom-anchor-and-follow` 自 ChatPanel 0.1.0-draft 的 `chat-scroll-anchor-and-follow` 搬來，欄位改名 `stack.marginTop` → `leadingSpace`、`scrollView.pinnedToBottom` → `atBottom`、`followBottom` → `following`；`scroll-follow-stop-and-resume` 是 `chat-scroll-disengage-and-reengage` 的捲動部分，`pushStreamingChunk` 改為 `growContent`，`submitUserMessage` 改為宿主的 `requestFollow`（聊天面板的觸發留在 ChatPanel 的 `chat-scroll-reengage`）。平台 adapter 操作真實公開元件，不能用 reference model 代算。manifest 登錄規格、參數與案例的 SHA-256。

來源基線（2026-11-10 補建，只證明規範符合來源）：UI Toolkit 聊天呈現記在 [UIToolkit ScrollView 紀錄](../../../../Unity/UnityExtension/Packages/UIToolkit/Documentation/Yu5h1Lib.UX/ScrollView.md)，uGUI 的 `ScrollRectEx` 記在 [UI ScrollView 紀錄](../../../../Unity/UnityExtension/Packages/UI/Documentation/Yu5h1Lib.UX/ScrollView.md)。來源差異記在那裡，不改規範。

| 行為 | 中性案例 | 平台檢查 |
|---|---|---|
| SCROLL-OVERFLOW | `scroll-overflow-clamp` | — |
| SCROLL-BOTTOM-ANCHOR | `scroll-bottom-anchor-and-follow` | — |
| SCROLL-FOLLOW | `scroll-bottom-anchor-and-follow`、`scroll-follow-resume-at-bottom` | — |
| SCROLL-FOLLOW-STOP | `scroll-follow-stop-and-resume`、`scroll-drag-below-threshold`、`scroll-wheel-stops-follow` | SV03 |
| SCROLL-KEEP-POSITION | `scroll-keep-position` | SV03 |
| SCROLL-FOLLOW-RESUME | `scroll-follow-stop-and-resume`、`scroll-follow-resume-at-bottom` | SV03 |

SV03（手機實體操作）：在持續加入內容的訊息流中，用手指往上拖閱讀舊內容，確認畫面不被拉回；再拖回最底端放開，確認之後的新內容會跟到底。

## 決策紀錄

- 2026-11-10 `scrollview-goal-1`：目標 1 只服務訊息流的六個情境，其他候選等各自消費端點名。
- 2026-11-10 `scrollview-follow-resume`：使用者捲回底端附近就恢復跟隨（來源只在送出或新回覆時恢復）。

## 後續候選

2026-10-19 草案原本的目標 1 以 OptionPicker／Dropdown、AppsMenu、TransientSurface、SettingsPanel 為對象，涵蓋鍵盤捲入視野、手勢仲裁與邊緣自動捲動；這些等各自的消費端點名時才訂目標，候選條文保留在下方，不屬目標 1。SCROLL-ARBITRATE 是第 69 題的原型候選，不是通用預設。

| ID | 前提與操作 | 必須觀察到的結果／邊界 |
|---|---|---|
| SCROLL-WHEEL | 指標位於 `ScrollView` 視口內並滾動滑鼠滾輪或觸控板 | 沿啟用的捲動軸推進 `scrollOffset`（若僅啟用 `horizontal` 且收到垂直滾輪，可依平台慣例或 `Shift+滾輪` 推進水平捲動）。若 `scrollOffset` 未達邊界而實際發生移動，消耗該滾輪事件，不往外層容器冒泡；若已達邊界且外層另有捲動容器，則依宿主設定決定是否冒泡（浮動面板如 `OptionPicker`、`Dialog` 預設鎖定不冒泡至背景頁面） |
| SCROLL-BAR | 內容溢位時顯示捲軸滑塊（Thumb）與軌道（Track） | 滑塊長度依可視比例計算：`thumbSize = max(minThumbSize, viewportSize * (viewportSize / contentSize))`。拖曳滑塊直接對應 `[0, maxScroll]`；點擊滑塊以外的軌道區域，向點擊側翻動一頁（`viewportSize - pageOverlap`） |
| SCROLL-ARBITRATE | 指標在 `ScrollView` 內的子項目上按下並移動 | 依下列優先序仲裁：<br>1. **專屬把手優先**：按下的是排序把手（`.drag-handle`）或捲軸滑塊時，立即由該把手捕獲指標，不觸發視口平移捲動。<br>2. **正交軸向立即分流**：在垂直 `ScrollView` 中，若指標首次超過 `dragThreshold`（6px）時水平位移大於垂直位移（`\|dx\| > \|dy\|`），手勢交給水平子控件（如 `SlideableField` 滑動調值、水平 `OptionPicker`），垂直視口不捲動；反之在水平 `ScrollView` 中垂直位移較大時交給垂直子控件。<br>3. **長按計時前移動取消長按**：若子項目需長按（`holdDuration` 400ms，如 `INPUT-PRESS-HOLD`、`INPUT-CHOICE-HOLD` 或長按啟動拖曳換位），而指標在 400ms 屆滿前已沿捲動軸移動超過 `moveTolerance`（6px），則取消子項目的按壓與長按（`INPUT-CANCEL`），轉為 `ScrollView` 平移捲動。<br>4. **長按成立後鎖定子手勢**：一旦 `holdDuration` 屆滿觸發長按，子項目（或面板）捕獲指標，後續指標移動皆屬於長按滑動或拖曳換位，`ScrollView` 不再攔截為平移捲動（改由 `SCROLL-EDGE-AUTO` 協助邊緣捲動） |
| SCROLL-EDGE-AUTO | 子項目已進入拖放配置（`DragPlacement`）的拖曳或長按滑動選取（`INPUT-CHOICE-HOLD`），且指標位於可捲動視口內距邊緣小於 `edgeScrollZone`（預設 24px）的區域 | 視口沿該邊緣方向自動連續捲動，捲動速率與進入感應帶的深度成正比（最高 `edgeScrollMaxSpeed` px/s），同時持續重新計算指標下的目標項目或格位，直到抵達 `0` 或 `maxScroll`、指標離開感應帶、或手勢結束 |
| SCROLL-ENSURE-VISIBLE | 鍵盤方向鍵／`Tab` 改變焦點項目，或 `OptionPicker`／`Dropdown` 面板開啟時帶有目前選中項 | 若目標項目的邊界 `[itemStart, itemEnd]` 已完整落在 `[scrollOffset, scrollOffset + viewportSize]` 內，不移動 `scrollOffset`；若 `itemStart < scrollOffset`，捲至 `itemStart - scrollPadding`；若 `itemEnd > scrollOffset + viewportSize`，捲至 `itemEnd - viewportSize + scrollPadding`（開啟選單時可選擇將選中項置中或貼齊可視區） |

候選參數（沒有來源量測，不作跨平台預設）：`scrollbarThickness` 6、`minThumbSize` 24、`edgeScrollZone` 24、`edgeScrollMaxSpeed` 320 px/s、`scrollPadding` 4（px）。

候選平台檢查：SV01 溢位與鍵盤捲入視野（方向鍵把焦點移到視口外項目，依 SCROLL-ENSURE-VISIBLE 捲入）；SV02 內部手勢仲裁與邊緣捲動（快速上下滑動、水平拖曳滑動調值、長按 400ms 後滑到視口邊緣，依 SCROLL-ARBITRATE、SCROLL-EDGE-AUTO 分流）。

## 2026-11-10 案例勘誤

使用者授權修正 Web 引用時發現的來源矛盾：`scroll-keep-position` 的內容總高 1000、起始 offset 500，原可視高 600 使最大 offset 只有 400，與溢位夾限條文矛盾。案例 0.2.1-draft 將可視高改為 500；項目尺寸、操作與原有預期位置保持，使第一步在合法範圍內開始。行為、參數與案例數不變，不代表平台驗收完成。
