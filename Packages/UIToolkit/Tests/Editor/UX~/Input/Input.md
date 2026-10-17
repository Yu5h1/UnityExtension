# 操作（Input）

操作定義滑鼠、觸控、觸控筆與鍵盤的輸入怎麼辨識成短按、長按、拖曳、放手速度、取消與鍵盤命令，是各層共用的基礎；控件、集合與模組以行為 ID 引用。

> 狀態：目標 1 進行中（2026-10-18）；Web 已依 2.0.0／3.0.x 匯入並通過中性案例（UX-WEB-1、UX-WEB-15）；Unity UI Toolkit 引用與驗收進行中。
>
> Specification version: 3.0.1（2026-10-18；補入〈目標〉，並明訂長按操作基底（Hold Manipulator）的跨平台約束：長按觸發、超門檻或中斷後抑制原生短按，以及長按後不放開連續滑動（Hold-and-Slide）時的指標捕獲與跨元素座標命中懸停更新；設計資料 2.0.0、案例 2.0.0 不變。3.0.0（2026-10-16）依 plan〈UX 內部分層〉規則 4，選項面板的開啟（INPUT-CHOICE-OPEN）與位置（INPUT-CHOICE-PLACE）、目前值與停用的語意、`pickerGap`／`pickerViewportMargin` 與 7 個案例移到控件 [OptionPicker](../Controls/OptionPicker.md)；本文只留選項面板的手勢 HOLD、KEY、CLOSE。設計資料 2.0.0、案例 2.0.0，案例的 `activate` 欄位改為 `interaction`。2.0.0 同日由 Primitives 1.3.1 改名：名稱改為「操作」（Input），移到 `Foundation/`，行為 ID 前綴 `PRIM-` 改為 `INPUT-`、後綴不變（例如 PRIM-PRESS-TAP → INPUT-PRESS-TAP），參數與案例檔改名為 input-design.json、input-cases.json 且內容不變；行為不變。1.3.1（2026-10-14）邊界案例改取各邊中點。1.3.0 起 INPUT-PRESS-TAP 的目標範圍改為左、上緣在內，右、下緣不在內，依 Web 的 WEB-UX-1 與 Web、Unity 的原生命中測試。1.2.0 新增排序與選項面板，鍵盤排序依排列軸，皆自 Web 現有實作收斂。1.1.x 自 [AppsMenu](../AppsMenu.md) 1.0.1 抽出並定案長按容許量、放開位置與 Escape 焦點）

## 系統階層與關係

```text
Yu5h1Lib.UX
├─ 操作（Input）：Press、Drag、Velocity、Cancel／Lifecycle、Keyboard／Focus
│  ├─ 組合操作：Reorder（排序）、Choice（選項面板的手勢；開啟、目前值與位置屬控件 OptionPicker）
│  └─ 輸出語意事件：activate、hold、dragBegin／dragMove／dragEnd、cancel、moveBackward／moveForward、dismiss、reorder、open／close／choose
├─ 控件、集合與模組（OptionPicker、AppsMenu 等）：引用操作，決定事件的產品意義
└─ 實作端：Web（bindLongPress／HoldManipulator）、UI Toolkit（HoldManipulator／Gesture.LongPress）、UGUI、WinForms、WPF 等，把實體輸入對應到操作
```

## 目標

### 目標 1：共用按壓、長按抑制與長按滑動選取手勢（進行中，2026-10-18）

- 為誰服務：需要區分短按（Tap）、長按（Hold）與長按後不放開連續滑動（Hold-and-Slide）的控件與模組，包含 [OptionPicker](../Controls/OptionPicker.md)（TaskProgress 決策面板的可見性與排序按鈕，以及 HealthAI 背景單色選擇列）與 [AppsMenu](../AppsMenu.md)（圖示短按啟動、長按標記、拖曳與甩動）。目的是由操作層提供統一的手勢基底（Web `bindLongPress`／`HoldManipulator`、Unity UI Toolkit `HoldManipulator`），避免各控件各自手寫計時器、指標捕獲與點擊抑制而產生跨平台落差。
- 情境：
  1. 短按與邊界判定：主指標按下目標，未滿 `holdDuration`（400ms）且位移未超過 `dragThreshold`（局部邏輯 px 6px），在目標範圍內（含左、上緣，不含右、下緣）放開才發出一次 `activate`；滑出範圍外放開不發任何事件。（INPUT-PRESS-TAP）
  2. 長按觸發與原生點擊抑制（Click Suppression）：主指標按住達 `holdDuration` 且未超過 `dragThreshold` 時發出一次 `hold`；一旦發出 `hold`、位移超過 `dragThreshold`、滑出目標外放開或發生中斷，同一指標放開時必須抑制平台原生的短按點擊（`click`／`activate`），避免長按放開後誤觸短按動作。（INPUT-PRESS-HOLD、INPUT-PRESS-SUPPRESS）
  3. 長按後不放開連續滑動與跨元素懸停（Hold-and-Slide）：長按觸發後手指或滑鼠不放開繼續移動時，手勢持續追蹤該指標的移動與放開；因指標捕獲（Pointer Capture）期間平台不會對面板內其他元素自動觸發原生 `:hover`，實作端須於移動時以指標座標對可用選項做命中測試，即時更新下方選項的懸停狀態，並在放開或取消時清除懸停並完成選定或關閉。（INPUT-CHOICE-HOLD、INPUT-DRAG-START）
  4. 中斷與生命週期：手勢進行中發生第二指標按下、指標捕獲丟失、Escape、捲動、失焦、頁面隱藏、尺寸變更或釋放元件時，立即取消計時與手勢、清除懸停狀態，之後不再遲發 `hold` 或觸發放開事件。（INPUT-CANCEL、INPUT-LIFECYCLE、INPUT-CHOICE-CLOSE）
- 不在目標 1 內：多指縮放與旋轉、手把輸入、觸控與螢幕閱讀器的實機全量驗收。
- 完成條件：Web 與 Unity UI Toolkit 均具備符合上述規則的共用長按操作基底（`bindLongPress`／`HoldManipulator`），OptionPicker 透過該操作基底運作，通過中性案例與平台自動檢查，並由使用者在驗收入口確認長按開面板、滑動懸停變色與放開選取的手感（PP01、PP06、OP02）。

## 規範來源與範圍

操作是各層共用的最小操作單位。本文定義實體輸入如何辨識成語意事件，以及手勢何時結束、何時取消；事件的產品意義（例如 hold 切換 mark、drop 收納）由引用的模組決定。

- 參數：[input-design.json](Input/input-design.json)，設計版本 2.0.0。
- 中性案例：[input-cases.json](Input/input-cases.json)，schema 1／版本 2.0.0。
- 來源：AppsMenu 的 APPS-INPUT、APPS-VELOCITY、APPS-LIFECYCLE、APPS-PANEL；Web `src/ui/long-press.ts`、`src/ui/apps-menu.ts` 為實作證據。排序與選項面板收斂自 Web `src/ui/reorder.ts`、`src/ui/choice-picker.ts` 與 [NativeUIAndDataUpdatePlan](../../../../Web/Documentation/NativeUIAndDataUpdatePlan.md)〈通用排序請求切片〉〈長按選項面板與拖曳選取〉（Web `30cc4f7`）；Unity UI Toolkit `HoldManipulator`／`Gesture.LongPress` 為長按的第二個實作。

涵蓋滑鼠、觸控、觸控筆與鍵盤。手把（Joystick／Gamepad）、多指手勢（縮放、旋轉）與滾輪不在本版。視覺回饋（落點外框、拖曳中樣式、排序預覽、選項懸停）的樣式屬引用模組；選項面板的開啟、目前值與位置屬 [OptionPicker](../Controls/OptionPicker.md)。

## 座標、時間與參數

- 座標使用目標所在元件的局部邏輯 px；裝置像素與縮放由平台轉換。距離是歐氏距離 `hypot(dx, dy)`。
- 時間使用單調時鐘的 ms；速度單位 px/s。
- 參數名稱與預設值見設計資料：`holdDuration` 400ms、`dragThreshold` 6px、`velocityWindow` 100ms、`reorderThreshold` 8px。
- 引用模組可用具名參數覆寫單一門檻（例如 AppsMenu 的 Apps 按鈕以 8px 啟動吸收），覆寫值須有限且非負；比較規則不變。

## 行為

一次手勢從主指標按下開始，到放開、取消或釋放元件為止；同時最多一個手勢。

| ID | 前提與操作 | 必須觀察到的結果／邊界 |
|---|---|---|
| INPUT-PRESS-TAP | 主指標按下目標，未滿 `holdDuration` 且未成立拖曳就放開 | 放開點在目標範圍內才發出一次 `activate`；範圍含左緣與上緣，不含右緣與下緣（x ≤ 放開點 x < x + width，y 同理）；在範圍外放開不發任何事件，讓使用者可滑開取消。非主要按鍵、非主指標、停用目標不開始手勢 |
| INPUT-PRESS-HOLD | 主指標按住，未成立拖曳 | 時間達 `holdDuration`（≥，399ms 不發、400ms 發）發出一次 `hold`；之後放開不再發 `activate`。拖曳成立或取消即停止計時，不得遲發。長按的移動容許量就是拖曳門檻：同一個 `dragThreshold`、同樣以局部邏輯 px 判定，長按取消與拖曳成立發生在同一點。長按觸發後手勢可繼續追蹤同一指標的移動（HoldMove）與放開（HoldEnd） |
| INPUT-PRESS-SUPPRESS | hold、拖曳、滑出目標外放開或手勢中斷之後，同一指標的放開 | 必須攔截並抑制平台原生的指標 `click`／`activate`（即使目標持有指標捕獲將放開事件送回目標本身）。鍵盤觸發的 `activate` 不受影響 |
| INPUT-DRAG-START | 按下後移動 | 與按下點距離 > `dragThreshold` 才成立，= 不成立。成立時依序發 `dragBegin` 與第一個 `dragMove`。hold 之後仍可成立拖曳 |
| INPUT-DRAG-MOVE | 拖曳中移動 | `dragMove` 的 dx、dy 是相對按下點的總位移，不是增量；成立當下的位移包含門檻內的距離 |
| INPUT-DRAG-END | 拖曳中放開 | 發出一次 `dragEnd`，帶放手速度（INPUT-VELOCITY）。是否提交結果由引用模組決定 |
| INPUT-VELOCITY | 放開時計算 | 取樣包含按下點、每次移動與放開點。保留時間 ≥ 放開時間 − `velocityWindow` 的樣本（邊界保留），速度 =（放開點 − 最早保留樣本）／時間差；時間差為 0 時速度為 0。停住超過視窗後放開，速度為 0 |
| INPUT-CANCEL | 手勢進行中發生中斷：第二指標按下、指標捕獲丟失、捲動、Escape、失焦、頁面隱藏、尺寸變更、釋放元件 | 發出一次 `cancel`，停止 hold 計時；同一手勢之後的移動與放開不再發事件。第二指標不開始新手勢，所有指標放開後才能開始。沒有進行中手勢時中斷不發事件 |
| INPUT-LIFECYCLE | 取消或釋放元件 | 引用模組必須把未提交的結果還原到手勢開始前；已提交的結果不撤回。釋放元件後忽略所有輸入，不留下計時或回呼 |
| INPUT-KEY-COMMAND | 焦點在可操作目標上按鍵 | Enter 或 Space → `activate`；Shift+Enter → `hold`；沿排列軸往前的 Alt+方向鍵 → `moveBackward`，往後 → `moveForward`：水平排列用 Alt+←／→，垂直排列用 Alt+↑／↓，另一軸的 Alt+方向鍵不處理。指標手勢進行中忽略這兩個命令。已在最前或最後時由引用模組決定不變動 |
| INPUT-KEY-DISMISS | 按 Escape | 有手勢時先 `cancel`，再發 `dismiss`。引用模組關閉最上層的暫時介面後，焦點回到開啟它的控制項；沒有開啟的暫時介面時焦點不動 |
| INPUT-NAME | 任何可操作目標與暫時介面 | 有非空的無障礙名稱，輔助技術可讀；沒有可見標題的容器也要有名稱 |

平台缺少某種輸入時，實作端記錄替代方式與未驗項目；替代輸入必須產生相同的語意事件。

## 排序

在一個有序清單內移動一個項目。每個項目有唯一 ID 與專用的排序把手；引用模組可把項目標為固定，固定項目不能被移動、不能當落點，鍵盤移動時被跳過，但不阻擋其他項目越過。排序只發出 `reorder` 請求（id、targetId、placement：before 或 after），不自行改變順序；引用模組採用後更新順序並把焦點留在被移動項目，也可以拒絕。

| ID | 前提與操作 | 必須觀察到的結果／邊界 |
|---|---|---|
| INPUT-REORDER-START | 主指標在可排序項目的把手上按下後移動 | 與按下點距離 > `reorderThreshold` 且沿排列軸的位移大於另一軸（> 不含等於）才開始排序；距離 = 門檻不開始。距離超過門檻但另一軸位移 ≥ 排列軸時，這次按壓放棄排序，之後再怎麼移動都不開始。開始排序後，同一指標的放開不發 `activate` |
| INPUT-REORDER-TARGET | 排序中移動 | 指標下方的可排序項目（不含來源本身、被其他元素遮住的項目）是落點；指標在排列軸上小於落點中點為 before，≥ 中點為 after。引用模組顯示落點與前後位置的預覽 |
| INPUT-REORDER-DROP | 排序中放開 | 落在有效落點時發出恰一次 `reorder`；落在來源本身、固定項目或清單外不發任何請求 |
| INPUT-REORDER-KEY | 焦點在可排序項目的把手上，按沿排列軸的 Alt+方向鍵（INPUT-KEY-COMMAND） | 往前時 targetId 是前一個可排序項目、placement before；往後時是下一個、placement after；跳過固定項目。已在最前或最後時不發請求。加按 Shift 或在輸入法組字中不處理 |
| INPUT-REORDER-CANCEL | 排序中發生：第二指標、指標捕獲丟失、Escape、失焦、捲動、頁面隱藏、尺寸變更、項目集合改變（ID、順序、可排序狀態）、釋放元件 | 清除預覽，不發任何請求，同一按壓之後的移動與放開也不發。排序請求發出前沒有改變任何狀態，因此不另發 `cancel` |

## 選項面板

一個觸發鈕開啟一組選項，從中選一個。本文只定義面板開啟後的手勢：長按開啟並拖曳選取、面板內的鍵盤移動、何時關閉與焦點回到哪裡。何時以短按開啟、目前值、停用與面板位置由控件 [OptionPicker](../Controls/OptionPicker.md) 定義。停用或隱藏的選項不可選、鍵盤跳過。選擇只發出 `choose`（id），值由宿主保存。

| ID | 前提與操作 | 必須觀察到的結果／邊界 |
|---|---|---|
| INPUT-CHOICE-HOLD | 主指標在觸發鈕上按住 `holdDuration`（長按規則同 INPUT-PRESS-HOLD，容許量是 `dragThreshold`），且控件允許長按開啟 | 發出 `open`，焦點不移動；按住移動時以指標座標對面板做命中測試，即時標示指標下方的可用選項（因觸發鈕持有指標捕獲時原生 `:hover` 不會派發給面板選項，須由手勢移動回呼主動更新懸停狀態，並在放開或取消時清除）；放開在可用選項上時依序發出 `close`、`choose`，放在其他位置只發出 `close`。這次放開不再觸發短按動作 |
| INPUT-CHOICE-KEY | 面板開啟、焦點在選項上 | 沿面板方向的方向鍵在可用選項間移動焦點並頭尾循環，Home／End 到第一個／最後一個；另一軸的方向鍵不處理。Enter／Space 依序發出 `close`、`choose`；Escape 發出 `close`；Tab 先關閉面板，再從觸發鈕往下一個焦點移動 |
| INPUT-CHOICE-CLOSE | 面板開啟時：再次短按觸發鈕、點選選項、在觸發鈕與面板之外按下指標或移出焦點；或發生第二指標、指標捕獲丟失、面板外捲動、失焦、頁面隱藏、尺寸變更、選項或觸發鈕改變、釋放元件 | 發出一次 `close`，點選選項時接著發出 `choose`。選擇與再次短按觸發鈕後焦點回到觸發鈕；Escape 與中斷時，焦點原在觸發鈕或面板內才回到觸發鈕；在外面按下指標或焦點移出時，焦點不被搶回 |

## 驗收案例與覆蓋

`Input/input-cases.json` 沿用 AppsMenu 的 cases schema 1：每個案例含 id、initial、steps，每個 step 含 action、expect。

- **目標**：`initial.target` 是目標矩形（x、y、width、height，局部邏輯 px），左緣與上緣算在範圍內，右緣與下緣不算。邊界案例取各邊中點、不取角落：實作的目標常有圓角，角落不在原生命中範圍內。`down` 表示在目標上按下；第二指標可在任何位置。
- **操作**：`down`、`move`、`up`（pointer、button、t、x、y；`down` 可加 pointerType，省略為 mouse）；`advance`（t）；`interrupt`（t、reason：scroll、capture-lost、blur、hidden、resize、dispose）；`key`（t、key、shift、alt，key 採 UI Events 的 key 名稱；排序案例另有 target 指出焦點所在項目）。`initial.parameters` 覆寫參數。
- **組合操作**：`initial.kind` 為 `reorder` 時，`initial` 有 axis 與 items（id、x、y、width、height、sortable，把手即項目矩形）；為 `choice` 時有 interaction、orientation、placement（OptionPicker 的屬性）、viewport、trigger 矩形、focus 與 options（id、width、height、pressed、disabled；面板內依序排列、從面板原點開始），OptionPicker 的案例沿用同一格式。省略 kind 為單一目標的按壓案例。排序案例用 touch，對應 PP05 的允許差異。
- **時鐘**：t 是絕對 ms。執行每個操作前，先觸發所有到期時間 ≤ t 的計時，因此 400ms 的放開晚於 hold。
- **比較**：`expect.emitted` 是該步驟發出的事件，依序完整比較；未列出事件即應為空。`expect.focus`（`trigger` 或選項 ID）與 `expect.panel`（面板左上角 x、y）有列出時才比較，位置完全相等。事件類型、順序、數量精確比較；dx、dy、vx、vy 採 `abs(actual−expected) ≤ 1e−3 + 1e−5×abs(expected)`，非有限值失敗。

| 行為 ID | 中性案例 | 平台檢查 |
|---|---|---|
| INPUT-PRESS-TAP | tap-activates、release-on-left-edge-activates、release-on-top-edge-activates、release-on-right-edge-is-silent、release-on-bottom-edge-is-silent、release-outside-is-silent、non-primary-button-is-ignored | PP01 |
| INPUT-PRESS-HOLD | hold-at-duration-suppresses-pointer-activate、drag-starts-past-threshold-and-cancels-hold | PP01 |
| INPUT-PRESS-SUPPRESS | hold-at-duration-suppresses-pointer-activate | PP01 |
| INPUT-DRAG-START | threshold-equal-is-not-drag、drag-starts-past-threshold-and-cancels-hold、drag-after-hold、module-overrides-drag-threshold | PP01、PP02 |
| INPUT-DRAG-MOVE | drag-move-is-total-displacement | PP02 |
| INPUT-DRAG-END | drag-move-is-total-displacement | PP01 |
| INPUT-VELOCITY | velocity-window-keeps-boundary-sample、velocity-is-zero-after-pause、drag-move-is-total-displacement | — |
| INPUT-CANCEL | interrupt-cancels-drag-once、second-pointer-cancels-and-does-not-start、interrupt-without-gesture-is-silent、escape-cancels-pending-press | PP03 |
| INPUT-LIFECYCLE | dispose-cancels-and-ignores | PP03（還原由引用模組驗收） |
| INPUT-KEY-COMMAND | keyboard-commands、reorder-ignored-during-pointer-gesture | PP04 |
| INPUT-KEY-DISMISS | keyboard-commands、escape-cancels-pending-press | PP04 |
| INPUT-NAME | — | PP04 |
| INPUT-REORDER-START | reorder-starts-past-threshold-on-main-axis、reorder-cross-axis-cancels-press | PP05 |
| INPUT-REORDER-TARGET | reorder-midpoint-is-after、reorder-fixed-items-are-skipped | PP05 |
| INPUT-REORDER-DROP | reorder-starts-past-threshold-on-main-axis、reorder-release-on-source-or-outside-is-silent、reorder-fixed-items-are-skipped | PP05 |
| INPUT-REORDER-KEY | reorder-fixed-items-are-skipped、reorder-keyboard-follows-axis | PP04 |
| INPUT-REORDER-CANCEL | reorder-interrupt-and-second-pointer-cancel | PP03、PP05 |
| INPUT-CHOICE-HOLD | choice-hold-drag-release-chooses、choice-hold-release-outside-closes、choice-hold-tolerance-is-drag-threshold、choice-second-pointer-cancels-hold | PP06 |
| INPUT-CHOICE-KEY | choice-keyboard-cycles-and-chooses | PP04、PP06 |
| INPUT-CHOICE-CLOSE | choice-click-option-chooses、choice-tap-open-trigger-closes、choice-outside-pointer-closes、choice-interrupt-closes-and-returns-focus | PP06 |

中性案例驗證辨識規則，不能證明實體輸入、捕獲與焦點行為；平台檢查必須在真實輸入上執行。

允許差異：滑鼠排序可以使用平台原生的拖放，以原生的開始距離取代 `reorderThreshold`、不做軸向判定，須在驗收紀錄寫明；觸控與觸控筆必須依 INPUT-REORDER-START。

| 檢查 | 初始條件 → 操作 → 必須觀察到的結果 |
|---|---|
| PP01 實體輸入 | 滑鼠、觸控、觸控筆各自短按、長按、拖曳 → 事件同中性案例；右鍵與次要按鍵不開始手勢；停用目標無反應；長按後平台的系統選單或接續點擊不觸發 `activate` |
| PP02 縮放 | 縮放 1 與 2 → 拖曳門檻與長按容許量都以局部邏輯 px 判定，在同一點切換；`dragMove` 位移為局部值 |
| PP03 中斷 | 拖曳中與按住未拖曳時，分別觸發真實捲動、捕獲丟失、第二指標、失焦、頁面隱藏、視窗尺寸變更、卸載 → 恰一次 `cancel`，之後無遲發的 `hold` 或回呼 |
| PP04 鍵盤與名稱 | 以鍵盤操作 → 事件同中性案例；Escape 關閉暫時介面後焦點回到開啟者，沒有暫時介面時焦點不動；輔助技術讀出每個可操作目標與無標題容器的名稱 |
| PP05 排序 | 水平與垂直清單，各含一個固定項目：滑鼠、觸控、觸控筆從把手拖曳到另一項的前半與後半、拖到固定項目與清單外；拖曳中捲動、第二指標、Escape → 請求同中性案例；觸控在把手上沿另一軸滑動時頁面照常捲動、不開始排序；開始排序後放開不觸發把手的短按 |
| PP06 選項面板手勢 | 水平與垂直各一：長按後拖到選項放開、長按後在外面放開、面板內鍵盤循環與選擇、在外面按下 → 事件與焦點同中性案例；長按放開後沒有接續的短按動作或系統選單。面板位置的檢查見 OptionPicker |

## 版本與交付

規範、設計資料與案例各自版本化，相容規則同 AppsMenu：不相容變更升 major、相容新增升 minor、澄清升 patch；拒絕未知 major 與未知操作。內容雜湊記錄在 [manifest.json](../manifest.json)。實作端依 referenced-module-acceptance 在自己的套件逐 INPUT-ID 記錄結果；Web 通過不代表其他平台通過。

## 決策紀錄

3.0.0（2026-10-16）依 plan 規則 4 與決策第 41 題（OptionPicker 目標 2）拆分：凡是要知道「目前的值」或「宿主允許怎麼開啟」的規則（短按開啟、焦點落在目前值、停用、對齊與相鄰位置、原地放開選到目前值）屬控件，移到 OptionPicker；與值無關的手勢留在本文。被移走的 7 個案例原文搬到 OptionPicker 的案例檔，留下的 9 個案例只把 `activate: false` 改為 `interaction: "picker"`，事件與預期不變。

1.2.0 的排序與選項面板直接採用 Web 現有實作的規則與數值（`reorderThreshold` 8、面板間隔與邊界 4、中點歸 after、固定項目只被跳過不擋路），只有 Web 實作，沒有互相衝突的實作。選項面板的長按沿用 INPUT-PRESS-HOLD，容許量是 `dragThreshold`；Web `bindChoicePicker` 未指定容許量時用 `bindLongPress` 的預設 8 畫面 px，屬第 8 題已記錄的同一差異。

以下三點曾未定，2026-10-13 依決策檔 `ux-foundation` 第 8–10 題定案，理由保留以免重議：

- **長按容許量用局部邏輯 px**：與拖曳門檻同一點切換，不會出現「長按已取消、拖曳未成立」的空檔；Unity UI Toolkit 的 LongPress 已如此，Web 以畫面 px 判定屬差異。
- **在目標內放開才算短按**：與 Web 原生 click、UI Toolkit Clickable、uGUI Button 一致，保留滑開取消的方式；Unity 舊範例不檢查位置，不作依據。
- **沒有暫時介面時 Escape 焦點不動**：沒有東西被關閉時焦點跳走，鍵盤使用者容易迷失位置；Web AppsMenu 目前一律移到 Apps 按鈕，屬差異。

1.3.0（2026-10-14）把目標範圍改為不含右緣與下緣：1.2.0 寫成含邊界，但這不是第 9 題的選項內容；第 9 題的依據是平台原生行為。Web 瀏覽器命中測試實測左、上緣命中、右、下緣不命中（Web 的 WEB-UX-1）；Unity `Rect.Contains` 與 UI Toolkit `ContainsPoint` 同樣是 xMin ≤ x < xMax。兩個實作一致，直接採用，不另出題。以整數像素而言，右緣那一點已在目標可見像素之外。1.3.1 把 1.3.0 的左上角案例拆成左緣與上緣中點：Web AppsMenu 圖示圓角 14px，角落不在命中範圍內，案例不應依賴角落形狀。

## 來源對照

| 操作 | AppsMenu 行為 ID |
|---|---|
| INPUT-PRESS-*、INPUT-DRAG-START | APPS-INPUT |
| INPUT-DRAG-MOVE、INPUT-DRAG-END | APPS-DRAG（相對位移、單一交易） |
| INPUT-VELOCITY | APPS-VELOCITY |
| INPUT-CANCEL、INPUT-LIFECYCLE | APPS-LIFECYCLE（手勢部分；飛行停止仍屬 AppsMenu） |
| INPUT-KEY-*、INPUT-NAME | APPS-PANEL |
| INPUT-REORDER-* | Web `bindReorder`（無 AppsMenu 對應；AppsMenu 面板排序的鍵盤部分即 INPUT-KEY-COMMAND） |
| INPUT-CHOICE-HOLD、KEY、CLOSE | Web `bindChoicePicker`（無 AppsMenu 對應；開啟與位置見 OptionPicker） |

AppsMenu 1.1.0 起引用本文的 `holdDuration`、`dragThreshold`、`velocityWindow`，不再另記門檻值。
