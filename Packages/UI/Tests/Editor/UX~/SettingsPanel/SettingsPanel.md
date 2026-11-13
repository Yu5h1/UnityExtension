# SettingsPanel

> 狀態：規範草案；尚無實作端驗收。
>
> Specification version: 1.1.0（2026-10-25：依 async-operation.decisions configuration-boundary 與 lock-scope 已確認答案，SET-LOCK／SET-CLOSE 及 SaveBar 引用 AsyncOperation 0.1.0-draft 共用規則，保留設定面板分頁、內容、關閉與保存列等具名全域鎖定情境；案例相容）。前一版：1.0.2（2026-10-19；只改引用：Configuration 搬到 [Foundation/Configuration.md](Foundation/Configuration.md)（1.2.0，第 87 題）。1.0.1 2026-10-13；自 Web `ui-settings-panel`、`ui-save-bar` 收斂；1.0.1 改連到 UX 的 Configuration）

## 系統階層與關係

```text
Yu5h1Lib.UX
├─ SettingsPanel
│  ├─ 設定入口：齒輪按鈕 → 以 TransientSurface 的 Dialog 開啟面板
│  ├─ 分類分頁：橫列或直欄、鍵盤切換、內容鎖定
│  └─ 保存列（SaveBar）：自動儲存開關、保存／重試按鈕、狀態訊息；也可單獨用在一般表單
├─ TransientSurface：開關、關閉入口、焦點返回
├─ VisualFoundation：色彩角色、焦點外框
├─ AsyncOperation：非同步狀態語意、鎖衝突與舊結果隔離基礎（本模組鎖定為具名全域情境）
└─ Configuration：保存、套用、失敗重試的語意與設定宣告
   ↑ 宿主：持有草稿、驗證、保存流程，把狀態回寫給面板與保存列
```

## 規範來源與範圍

設定面板讓使用者分類修改介面設定，保存列讓使用者選擇自動或手動保存並在失敗時重試。本文定義面板與保存列看得到、按得到的部分；什麼時候寫入、失敗後保留什麼，依 [Configuration](Foundation/Configuration.md#設定與保存) 與 [AsyncOperation](Foundation/AsyncOperation.md)（共用非同步作業基礎）。面板與保存列都不持有草稿、不寫入資料：使用者的操作送出請求，宿主決定採不採用並回寫狀態。

- 參數：[settings-design.json](SettingsPanel/settings-design.json)，設計版本 1.0.0。
- 中性案例：[settings-cases.json](SettingsPanel/settings-cases.json)，schema 1／版本 1.0.0。
- 來源：Web `src/ui/settings-panel.ts`、`src/ui/save-bar.ts`、`src/design/settings.json`、`demo/settings/demo.js`，[SettingsPanelPlan](../../../Web/Documentation/SettingsPanelPlan.md)〈介面〉〈驗收條件〉，[SaveBar](../../../Web/Documentation/SaveBar.md)〈公開用法〉（Web `30cc4f7`）。SaveBar 抽取自 TaskProgress 的 `SaveBar.svelte`。

由設定宣告產生欄位（Web `mountSettingsFields`）屬 Configuration 的 CFG-11，不在本文。主題編輯器是盤點第 8 項。

## 設定面板

分類有唯一 ID（小寫英文字母開頭，只含小寫英數與連字號）與非空白標籤；不符合時整批拒絕，保留原分類。目前選中的分類不在新清單中時改選第一個；沒有分類時顯示空狀態「尚無設定分類。」。

| ID | 前提與操作 | 必須觀察到的結果／邊界 |
|---|---|---|
| SET-OPEN | 按齒輪按鈕（名稱「設定」，顯示面板是否開啟） | 以 Dialog 開啟面板，標題「介面設定」，發出 `open`；焦點落在目前選中的分類分頁，沒有分類時依 SURF-OPEN。面板停用或已開啟時不動作。關閉時發出 `close`，焦點回到齒輪按鈕 |
| SET-CLOSE | 使用者按關閉鈕、點遮罩或按 Escape | 依 SURF-DISMISS；處理中（busy）時三種入口都停用，面板保持開啟，引用 AsyncOperation ASYNC-LOCK、ASYNC-CANCEL（具名全域情境：設定面板處理中暫停關閉以避免取消競態）。面板底部不另設關閉按鈕 |
| SET-TABS | 面板開啟、焦點在分頁上 | 橫列分頁用 ←／→，直欄用 ↑／↓，頭尾循環；Home／End 到第一個／最後一個；焦點跟著移到新分頁，另一軸的方向鍵不處理。只有選中的分頁在 Tab 順序中。點分頁直接選中。切換分類不重建內容、不清除宿主草稿 |
| SET-LAYOUT | 分頁方向為橫列（預設）或直欄 | 橫列：分頁在內容上方，選中分頁的上緣有 `selectionWidth` 粗線；直欄：分頁在內容左側，寬度為版面的 `columnTabShare` 且至少 `columnTabMinWidth`，選中分頁的起始側有粗線。方向改變不重建內容、不清除草稿。窄畫面（寬度 ≤ `narrowWidth`）仍保留使用者選的方向：橫列可橫向捲動，直欄的分頁文字可換行，內距改用 `narrow*` |
| SET-LOCK | 宿主回報處理中、無法套用（例如讀取失敗）或等待重試套用 | 處理中：分頁、內容、恢復預設與保存列全部停用，分頁鍵盤也不動作；此為 AsyncOperation ASYNC-LOCK 的具名全域鎖定情境（防止破壞進行中保存／載入），不推廣為一般非同步作業的通用鎖定規則。無法套用或等待重試套用：內容與恢復預設停用，分頁仍可切換；無法套用時保存列整列停用 |
| SET-RESET | 按「恢復預設」（保存列左側） | 發出 `reset`；由宿主走與一般修改相同的保存流程。SET-LOCK 停用時不發 |
| SET-INTRO | 面板顯示說明文字 | 自動模式：「修改後自動保存，下次開啟仍會保留。」；手動模式：「修改先保留在草稿，按套用後才保存；關閉會放棄尚未提交的修改。」 |

## 保存列

保存列由宿主提供狀態：自動模式（預設開）、處理中、停用、可保存（預設 true）、鎖定開關、待重試儲存、待重試套用、狀態訊息、保存按鈕文字（預設「套用並保存」）。設定面板內的保存列：可保存 = 有分類，停用 = 無法套用。

| ID | 前提與操作 | 必須觀察到的結果／邊界 |
|---|---|---|
| SAVE-BUTTON | 保存按鈕常駐顯示 | 文字依序取第一個成立的：處理中「處理中…」（引用 ASYNC-STATE、ASYNC-BUSY）、待重試套用「重試套用」、待重試儲存「重試儲存」（引用 ASYNC-RETRY）、否則保存按鈕文字。停用條件：處理中、停用、（不是待重試套用且不可保存），或（自動模式且沒有任何待重試）（引用 ASYNC-LOCK 防重複提交）。可用時按下發出一次：待重試套用時 `retryApply`（只重新套用已保存的資料，不再寫入），否則 `save` |
| SAVE-TOGGLE | 切換「自動儲存」開關 | 發出 `autoSaveChange`（enabled 為要切換到的值），開關外觀維持宿主目前的值，直到宿主回寫；宿主不回寫即拒絕，不留下切換後的外觀。處理中、停用、鎖定開關或待重試套用時開關停用、不發事件（引用 ASYNC-LOCK 鎖衝突操作） |
| SAVE-STATUS | 宿主提供狀態訊息 | 非空時顯示在按鈕列上方，以禮貌的即時區域讓輔助技術讀出（引用 ASYNC-BUSY、ASYNC-FEEDBACK，保留至下次操作）；空字串時不佔空間 |
| SAVE-ACTIONS | 宿主在保存列放入自己的按鈕（例如恢復預設、復原） | 宿主的按鈕靠左，自動儲存開關與保存按鈕靠右；宿主按鈕的事件與停用由宿主處理 |

每次使用者操作最多發出一個事件；保存列本身不決定何時寫入、不防抖、不處理版本衝突。

## 外觀

尺寸取設計資料，量測依 VisualFoundation VIS-UNIT；色彩依 VisualFoundation：

- 面板寬度上限 `panelMaxWidth`（取代 Dialog 的 `dialogMaxWidth`，其餘 Dialog 規則不變）。說明文字 `introFontSize`／`introLineHeight`，色彩 `secondaryText`。
- 分頁與內容外框圓角 `layoutRadius`、上下外距 `layoutMarginY`、外框 `border`；分頁列底色 `hover`，分頁內距 `tabPaddingY`／`tabPaddingX`（直欄 `columnTabPaddingX`）；未選中分頁文字 `secondaryText`，選中分頁底色 `panelBackground`、文字與粗線 `accent`、粗體。內容內距 `contentPadding`、最小高度 `contentMinHeight`。
- 按鈕圓角 `buttonRadius`、內距 `buttonPaddingY`／`buttonPaddingX`；一般按鈕底色 `panelBackground`、外框 `border`、懸停 `hover`；保存按鈕底色 `accent`、文字 `onAccent`。停用時降低不透明度。焦點外框 `focusWidth`、間距 `focusOffset`、色彩 `focus`。
- 齒輪按鈕圖示 `triggerIconSize`，與文字「設定」間隔 `gapSmall`。
- 保存列上方有 `border` 分隔線、上內距 `barPaddingTop`、元素間隔 `barGap`；狀態訊息 `statusFontSize`、色彩 `secondaryText`。自動儲存開關寬 `switchWidth`、高 `switchHeight`、全圓角，圓鈕 `switchKnobSize`、距邊 `switchKnobInset`；關閉時軌道 `secondaryText`、開啟時 `accent`，圓鈕 `panelBackground`。

## 驗收案例與覆蓋

`SettingsPanel/settings-cases.json` 沿用 cases schema 1：每個案例含 id、initial、steps，每個 step 含 action、expect。案例描述宿主狀態與使用者操作的結果，不含版面。

- **保存列**（`initial.kind` 為 `saveBar`）：`initial.state` 與 `set` 的 state 欄位為 autoSave、busy、disabled、canSave、toggleDisabled、retrySave、retryApply、message、saveLabel；操作 `pressSave`、`toggle`。觀察欄位：saveLabel、saveEnabled、toggleEnabled、autoSave、status。
- **設定面板**（`initial.kind` 為 `panel`）：`initial.categories` 與 `initial.state`（tabDirection、busy、disabled、canApply、retryApply、autoSave）；操作 `show`、`dismiss`（使用者關閉入口）、`selectTab`（id）、`key`（key）、`pressReset`、`set`、`setCategories`（categories；不符合時發出 `rejected`）。觀察欄位：open、active、focus（`trigger`、`tab:<id>` 或 `inside`）、contentEnabled、resetEnabled、tabsEnabled、intro（auto 或 manual）、empty。
- **比較**：`expect.emitted` 依序完整比較，未列出事件即應為空；`expect.view` 只比較列出的欄位，完全相等。

| 行為 ID | 中性案例 | 平台檢查 |
|---|---|---|
| SET-OPEN | panel-opens-on-selected-tab、panel-disabled-does-not-open | SP01 |
| SET-CLOSE | panel-opens-on-selected-tab、panel-busy-locks-tabs-content-and-close | SP01 |
| SET-TABS | panel-row-tabs-cycle-with-arrows、panel-column-tabs-use-up-down、panel-categories-validated | SP01 |
| SET-LAYOUT | — | SP02 |
| SET-LOCK | panel-busy-locks-tabs-content-and-close、panel-load-error-or-retry-apply-locks-content | SP03 |
| SET-RESET | panel-load-error-or-retry-apply-locks-content、panel-busy-locks-tabs-content-and-close | SP03 |
| SET-INTRO | panel-intro-follows-save-mode | — |
| SAVE-BUTTON | save-bar-auto-mode-disables-save、save-bar-manual-follows-can-save、save-bar-busy-blocks-everything、save-bar-retry-save-in-auto-mode、save-bar-retry-apply-wins-and-locks-toggle、save-bar-disabled-and-toggle-lock | SP03 |
| SAVE-TOGGLE | save-bar-toggle-is-a-request、save-bar-auto-mode-disables-save、save-bar-retry-apply-wins-and-locks-toggle、save-bar-disabled-and-toggle-lock | SP03 |
| SAVE-STATUS | save-bar-retry-save-in-auto-mode | SP03 |
| SAVE-ACTIONS | — | SP02 |

| 檢查 | 初始條件 → 操作 → 必須觀察到的結果 |
|---|---|
| SP01 開關與鍵盤 | 以鍵盤按齒輪開啟、方向鍵與 Home／End 切換分頁、Tab 進入內容、Escape 關閉；滑鼠點分頁、點遮罩 → 依 SET-OPEN、SET-TABS、SET-CLOSE；輔助技術讀出分頁數量、選中狀態與對應內容 |
| SP02 版面 | 縮放 1 與 2，light 與 dark，寬度 > 480 與 ≤ 480：橫列與直欄、分類很多與標籤很長、宿主放入兩個左側按鈕 → 依〈外觀〉與 SET-LAYOUT、SAVE-ACTIONS；切換方向後草稿與焦點仍在 |
| SP03 保存流程接線 | 接上真實保存流程：自動與手動各改一次、注入保存失敗與套用失敗、讀取失敗、保存中嘗試關閉與切換、恢復預設 → 面板與保存列的狀態與事件同中性案例；保存語意依 Configuration |

## 版本與交付

規範、設計資料與案例各自版本化：不相容變更升 major、相容新增升 minor、澄清升 patch；拒絕未知 major 與未知操作。內容雜湊記錄在 [manifest.json](manifest.json)。實作端依 spec-adoption 在自己的套件逐 ID 記錄結果；Web 通過不代表其他平台通過。

## 決策紀錄

目前只有 Web 實作，規則與數值直接採用，沒有需要出題的衝突。

- **字級改用 px**：Web 的說明與狀態文字用 rem（0.9rem、0.85rem），依 VisualFoundation VIS-TEXT-SCALE 記為 16px 基準下的 14.4px、13.6px，外觀不變；Web 改用 px 已在 UX-WEB-2。
- **Dialog 相關值歸 TransientSurface**：Web `settings.json` 的 `surfaceRadius`、`surfacePadding`、`viewportInset`、`headingFontSize`、`dialogOpen*` 屬於 Dialog，已在 TransientSurface；本文只收面板與保存列的值，token 名稱沿用 Web。
- **保存語意不在本文**：自動保存的時機、失敗後保留草稿、只重試套用等規則屬 Configuration；本文只定義使用者看到的狀態與送出的請求，避免兩份規格各寫一次。
- **共用非同步基礎與具名鎖定情境**：依 `configuration-boundary`（`shared-foundation`）與 `lock-scope`（`conflicting-only`）決策，AsyncOperation 規範防重複提交與鎖衝突操作；SettingsPanel 的全面鎖定（分頁、內容、關閉入口與保存列）保留為具名設定情境，不推廣為所有非同步作業的通用行為。

## 來源對照

| 本文 | 來源 |
|---|---|
| SET-OPEN、SET-CLOSE | `UiSettingsPanel.show`／`close`、`ui-dialog` 的 `dismissDisabled = busy` |
| SET-TABS、SET-LAYOUT | `UiSettingsPanel.keyboard`、`willUpdate`、樣式中的 `.column` 與 `@media` |
| SET-LOCK、SET-RESET | `fieldset ?disabled`、恢復預設按鈕的停用條件、`demo/settings/demo.js` 的 `canApply`／`retryApply` 對應 |
| SET-INTRO | 面板 `.intro` 文字 |
| SAVE-* | `UiSaveBar.saveDisabled`、`toggle`、`render`；SaveBar 文件〈公開用法〉 |
