# VisualFoundation

> 狀態：目標 1「預設色票與狀態外觀在 Unity UI Toolkit 成立」進行中（2026-10-21，第 94、95 題）。Web 已採用（UX-WEB-2），是本規格的來源側證據。
>
> Specification version: 1.4.0（2026-10-21：依 UnityExtension 的 UNITYEXTENSION-UX-1，〈狀態對應的色彩角色〉補「指標按住中」與「選取且滑過」兩列，`controlBorder` 的用途與「需要外框才能辨識的控制項」一列寫明實心按鈕用、只靠圖示或文字辨識的無底按鈕不用；色值、案例不變，相容新增）。前一版：1.3.1（2026-10-21：目標 1 完成條件的人工 checklist 改寫為一頁一份、light 與 dark 同一份，原「各一頁」被讀成兩份；行為不變）。前一版：1.3.0（2026-10-21：依第 94、95 題加入〈目標〉與目標 1 的對照表；角色、色值、案例不變，Web 不需重新匯入）；1.2.3（2026-10-19；只改引用：Configuration 搬到 [Foundation/Configuration.md](Foundation/Configuration.md)（1.2.0，第 87 題）。1.2.2 2026-10-16；只改引用：Primitives 改名為「操作」（Input，[Foundation/Input.md](Foundation/Input.md)），行為 ID 前綴改為 INPUT-。1.2.1 2026-10-13；1.2.1 改連到 UX 的 Configuration；1.0.0 自 Web Theme 的 `resolveTheme` 與 `theme.css` 抽出色彩角色與數值，不改配色；1.1.0 加入文字縮放與 `controlBorder`；1.2.0 取消 1.1.0 自訂的間距、圓角、字級級數，改以各模組現有的具名 token 為準）

## 系統階層與關係

```text
Yu5h1Lib.UX
├─ VisualFoundation：色彩角色與明暗、狀態對應的角色、對比、文字縮放、單位與量測
│  └─ 設計資料：light／dark 各 15 個角色色值、對比門檻、焦點外框下限
├─ 操作（Input）：輸入判定（不涉及外觀）
├─ 互動模組（AppsMenu 等）：以名稱引用角色；自己的尺寸、間距、圓角、字級 token
└─ 實作端：Web（CSS 變數、theme.css）、UI Toolkit（USS／TSS）、uGUI、WinForms、WPF
```

## 規範來源與範圍

視覺基礎是各模組共用的外觀規則：畫面上每種用途用哪個色彩角色、light 與 dark 的色值、色彩之間的最低對比、文字縮放，以及尺寸的單位與量測方式。模組自己的尺寸、間距、圓角與字級以具名 token 記錄在模組的設計資料（例如 AppsMenu 的 `iconRadius`），數值取自現有實作。

- 參數：[visual-design.json](VisualFoundation/visual-design.json)，設計版本 1.2.0。
- 中性案例：[visual-cases.json](VisualFoundation/visual-cases.json)，schema 1／版本 1.1.0。
- 來源：Web `src/theme/index.ts` 的 `DEFAULT_PALETTES`、`resolveTheme`、`contrastRatio`、`getContrastWarnings`；建置產出 `dist/theme/theme.css`（[跨平台主題色票交付契約](../../../Web/Documentation/NativeUIAndDataUpdatePlan.md#跨平台主題色票交付契約) 1.0.0）；Web 元件的狀態樣式；AppsMenu [apps-design.json](AppsMenu/apps-design.json) 的圓角、尺寸、字級、行高；決策檔 `ux-foundation` 第 14、15 題。

不在本版：Theme 偏好的存檔格式（[Configuration](Foundation/Configuration.md#theme-v1-存檔格式)）、主題範圍的劃分方式（整頁、容器或面板）、字型家族、動畫。產品自有的狀態色（例如 TaskProgress 的成功、警告）由產品擁有。

## 目標

### 目標 1：預設色票與狀態外觀在 Unity UI Toolkit 成立（進行中，2026-10-21）

- 為誰服務：以 UI Toolkit 呈現 UX 控件的 Unity 介面。證據路徑：Yu5h1Lib.UIToolkit 的 [Button](../../../Unity/UnityExtension/Packages/UIToolkit/Documentation/Yu5h1Lib.UX/Button.md) 2.0.0（`Yu5h1Button.uss`）與 [OptionPicker](../../../Unity/UnityExtension/Packages/UIToolkit/Documentation/Yu5h1Lib.UX/OptionPicker.md) 3.0.2 的驗收紀錄都把「顏色依 VisualFoundation 的角色」記 blocked，顏色取 Unity 預設主題變數；使用者 2026-10-19 並排確認 Button 外觀時顏色未比。驗收入口是 Yu5h1LibTest 的 `Samples~/UxAcceptance` 範例。
- 情境：
  1. 色票交付：套件從本設計資料產生 light、dark 兩套主題變數並核對 manifest 雜湊，15 個角色的 RGBA 與設計資料相同，同一輸入重匯入無差異。（VIS-PALETTE，VP01）
  2. 狀態外觀：Button 與 OptionPicker 的一般、滑過、鍵盤焦點、選取／按下、停用，以及面板底色與分隔，light、dark 各一次用〈狀態對應的色彩角色〉指定的角色。（VIS-STATE，VP03）
  3. 鍵盤焦點外框：寬度不小於 `focusMinWidth`、完整包圍目標、不被裁切或蓋住。（VIS-FOCUS，VP03）
- 對照表（第 95 題）：每條行為對這個消費端需不需要、原生機制怎麼做，決定它在目標 1 的處理。

  | 行為 | UI Toolkit 消費端需不需要 | 原生機制 | 目標 1 的處理 |
  |---|---|---|---|
  | 色票（VIS-PALETTE） | 需要 | USS 變數由套件從設計資料產生；Unity 預設主題是另一套變數 | 情境 1 |
  | 推導（VIS-DERIVE） | 只對預設配色需要 | 推導結果已在設計資料裡交付，不必自己算 | 情境 1 涵蓋 |
  | 自訂配色（VIS-CUSTOM） | 不需要，沒有自訂配色的介面 | — | 不在目標 1；依〈允許差異〉記 not run |
  | 對比（VIS-CONTRAST） | UX 端的工作 | — | VC01 由 UX 跑，不是平台檢查 |
  | 明暗模式（VIS-SCHEME） | light、dark 固定可選即可 | 沒有系統明暗偏好來源 | system 視為 light（允許差異）；跟隨不在目標 1 |
  | 執行中切換（VIS-SWITCH） | 目前沒有消費端要求 | 主題樣式可換 | 不在目標 1 |
  | 原生部件（VIS-NATIVE） | 不適用 | UI Toolkit 自繪，沒有原生部件 | 記不適用 |
  | 狀態外觀（VIS-STATE） | 需要 | USS 狀態偽類 | 情境 2 |
  | 焦點外框（VIS-FOCUS） | 需要 | UI Toolkit 自繪焦點外框 | 情境 3 |
  | 文字縮放（VIS-TEXT-SCALE） | 不適用 | Unity 不套系統字級，縮放隨顯示 | 允許差異 |
  | 單位與量測（VIS-UNIT） | 已在量測 | Button 的 BT03 以邏輯 px 量測 | 沿用，不另驗 |

- 不在目標 1 內（進下一個目標）：uGUI（Yu5h1Lib.UI 的 Button 同樣 blocked，依 plan 等 uGUI 產品）；自訂配色（VIS-CUSTOM）；system 跟隨與執行中切換（VIS-SCHEME、VIS-SWITCH）；文字縮放（VIS-TEXT-SCALE）；Theme 偏好保存（Configuration 的 Theme 持久化 CFG-13）。
- 完成條件：UnityExtension 依 referenced-module-acceptance 在 `Packages/UIToolkit/Documentation/Yu5h1Lib.UX/VisualFoundation.md` 建立驗收紀錄並釘選本文雜湊；色票交付（VP01）自動通過；狀態外觀（VP03）與焦點外框由使用者在 UxAcceptance 範例那一頁的一份人工 checklist 確認，light 與 dark 是同一份裡的兩組檢查（第 89 題：一頁一份，不是一種明暗一份）；Button 與 OptionPicker 紀錄的 blocked 列改為通過。達成時本節改為「已達成」並記日期。需求：[UX-UNITYEXTENSION-2](../Requirements.UnityExtension.md#ux-unityextension-2--webtheme-改讀-ux-的色票交付)。
- 進度看 UX `report.json`，證據看各平台的驗收紀錄。

## 色彩角色

八個基本角色可由使用者自訂，其餘七個由基本角色或明暗推導。色值是 sRGB `#RRGGBB` 或 `#RRGGBBAA`（末兩位是 alpha），大小寫等價，6 位等於 alpha `ff`；比較時按 RGBA 數值，不比字串。

| 角色 | 種類 | 用途 |
|---|---|---|
| `pageBackground` | 基本 | 頁面或桌面底色 |
| `panelBackground` | 基本 | 浮起的表面：對話框、面板、選單、設定區 |
| `heading` | 基本 | 頁面層級的標題 |
| `panelHeading` | 基本 | 面板內的標題 |
| `itemText` | 基本 | 一般文字與圖示 |
| `secondaryText` | 基本 | 說明、狀態等次要文字 |
| `border` | 基本 | 分隔線，以及不靠外框辨識的元素的外框 |
| `accent` | 基本 | 選取、開啟、主要動作的填色 |
| `hover` | 推導 | 指標停留時的底色 |
| `focus` | 推導 | 鍵盤焦點外框 |
| `onAccent` | 推導 | 放在 `accent` 上的文字與圖示 |
| `disabled` | 推導 | 停用控制項的文字與圖示 |
| `overlay` | 推導 | 模態介面下方的遮罩，帶 alpha |
| `danger` | 推導 | 錯誤訊息與無效欄位 |
| `controlBorder` | 推導 | 需要外框才能辨識的控制項：輸入框、多行輸入、下拉選單、核取方塊，以及實心按鈕的外框；只靠圖示或文字辨識的無底按鈕不用外框 |

角色名稱是跨平台的名稱；各平台的變數名稱（例如 Web 的 `--ui-surface`）屬實作端。

## 尺度、圓角與字級

間距、圓角、字級與行高不設共用級數，以各模組設計資料中的具名 token 為準；這些 token 的值取自現有實作，例如 AppsMenu 的 `panelPadding` 14、`iconRadius` 14、`buttonRadius` 20、`fontSize` 14、`lineHeight` 1.4，Web 與 Unity 一致。共用的部分只有 VIS-UNIT 的單位與量測，以及 VIS-TEXT-SCALE。

## 行為

| ID | 前提與操作 | 必須觀察到的結果／邊界 |
|---|---|---|
| VIS-PALETTE | 使用預設配色，有效明暗為 light 或 dark | 15 個角色的色值等於設計資料 `color.<light｜dark>.<角色>`，RGBA 數值完全相同 |
| VIS-DERIVE | 由八個基本角色產生其餘角色（預設與自訂都適用） | `focus` = `accent`；`disabled` = `secondaryText`；`onAccent` 取白 `#ffffff` 或黑 `#000000` 中對 `accent` 對比較高者，相等取白；`hover`、`overlay`、`danger`、`controlBorder` 取設計資料中與明暗相同的固定值，不隨自訂色改變 |
| VIS-CUSTOM | 使用者以 light 或 dark 為基底自訂基本角色 | 只接受 `#RRGGBB`；缺少或無效的基本角色以同一基底的預設值補上。`heading` 對 `pageBackground`，`panelHeading`、`itemText`、`secondaryText` 對 `panelBackground`，對比低於 `textContrastMin` 時依此順序列出提醒；提醒不阻止套用 |
| VIS-CONTRAST | UX 交付的每一組配色（目前是預設 light、dark） | 〈對比〉表中每一組都達到門檻。改動色值後任一組未達，該配色不得交付 |
| VIS-SCHEME | 明暗模式為 light、dark、system 或自訂 | light、dark 固定；system 跟隨平台目前的明暗偏好，平台沒有這項偏好時視為 light；自訂使用其基底。手動選定的模式不因系統偏好改變而變動 |
| VIS-SWITCH | 執行中切換模式或配色，或 system 模式下系統偏好改變 | 範圍內所有色彩更新為新的有效配色，不需重新開啟介面；焦點、選取、未保存的輸入與捲動位置不變 |
| VIS-NATIVE | 平台原生部件（捲軸、表單控制項、選色器等）出現在有效明暗下 | 原生部件採用相同的明暗，不出現 dark 介面中的 light 原生部件，反之亦然 |
| VIS-STATE | 介面元素處於〈狀態對應的色彩角色〉表中的狀態 | 前景與底色使用表中指定的角色 |
| VIS-FOCUS | 以鍵盤把焦點移到可操作目標 | 顯示完整包圍目標的 `focus` 外框，寬度 ≥ `focusMinWidth` 邏輯 px，不被容器裁切或被其他元素蓋住。指標操作取得焦點時可不顯示 |
| VIS-TEXT-SCALE | 使用者改變系統或瀏覽器的字級設定，或改變顯示縮放 | 文字大小不跟隨字級設定，維持 token 的邏輯 px；顯示縮放（含瀏覽器縮放）時文字與版面一起等比放大 |
| VIS-UNIT | 量測任何具名的長度 token（尺寸、間距、圓角、字級） | 以局部邏輯 px 量測，等於 token 值，容許平台像素對齊造成的 ±1 裝置像素。行高是字級的倍數（無單位）。圓角大於短邊一半時以一半呈現，形成膠囊或圓形 |

### 對比

對比依 WCAG 2 的相對亮度計算：每個 sRGB 通道 c = 值／255，c ≤ 0.04045 時線性值為 c／12.92，否則為 ((c + 0.055)／1.055)^2.4；亮度 L = 0.2126R + 0.7152G + 0.0722B；對比 = (較亮 L + 0.05)／(較暗 L + 0.05)，與前後順序無關。配對只用不透明色。

| 種類 | 門檻 | 必要配對（前景／背景） |
|---|---|---|
| 文字 | `textContrastMin` 4.5 | `heading`／`pageBackground`；`panelHeading`／`panelBackground`；`itemText`／`panelBackground`、`pageBackground`、`hover`；`secondaryText`／`panelBackground`、`pageBackground`；`onAccent`／`accent`；`danger`／`panelBackground` |
| 非文字 | `nonTextContrastMin` 3 | `focus`／`pageBackground`、`panelBackground`；`accent`／`pageBackground`、`panelBackground`；`controlBorder`／`pageBackground`、`panelBackground`、`hover` |

不要求對比的角色：`border`（只作分隔，或用在另有填色、文字、圖示可辨識的元素上；目前 1.27–1.57）、`hover` 對其他底色、`disabled`（停用控制項依 WCAG 不在要求內）、`overlay`。自訂配色只產生 VIS-CUSTOM 的提醒，不受本表約束。

現行預設配色全數通過：文字最低為 light 6.16（`secondaryText`／`pageBackground`）、dark 6.43（`secondaryText`／`panelBackground`）；非文字最低為 light 3.11、dark 3.14（皆為 `controlBorder`／`hover`）。

### 狀態對應的色彩角色

| 狀態 | 前景 | 底色／外框 |
|---|---|---|
| 頁面上的一般內容 | `itemText`；頁面標題 `heading` | `pageBackground` |
| 面板內的一般內容 | `itemText`；標題 `panelHeading`；次要文字 `secondaryText` | `panelBackground`；分隔 `border` |
| 需要外框才能辨識的控制項（輸入框、下拉選單、核取方塊、實心按鈕；只靠圖示或文字辨識的無底按鈕不算） | 不變 | 外框 `controlBorder` |
| 指標停在可用的控制項上 | 不變 | `hover` |
| 鍵盤焦點 | 不變 | `focus` 外框（VIS-FOCUS） |
| 選取、按下中的切換、開啟，以及主要動作 | `onAccent` | `accent` |
| 選取且指標停在上面 | `onAccent` | `accent`（選取優先，滑過不改變選取的外觀） |
| 指標按住中（不是切換、不是選取） | 不變 | 不另定外觀，沿用按住前的狀態（通常是 `hover`）；放開才依結果改變 |
| 停用 | `disabled`；可再降低不透明度 | 不顯示懸停外觀 |
| 錯誤或無效 | 錯誤訊息 `danger` | 無效欄位的外框 `danger` |
| 模態介面開啟 | — | 下層以 `overlay` 遮罩 |

模組特有的提示（例如 AppsMenu 的可放置外框）由模組規格決定，使用上表的角色。

## 允許差異

- **執行期自訂配色**：平台若只提供預先產生的 light、dark（Unity 目前未定），可不支援 VIS-CUSTOM，須在驗收紀錄標明 not run 與理由；VIS-DERIVE 仍適用於預設配色。
- **系統明暗偏好**：平台沒有偏好來源或無法在執行中得知變更時，system 視為 light，並記錄未驗的即時跟隨項目。
- **原生部件**：無法改變明暗的原生部件（例如作業系統標題列）記錄為差異，列出部件名稱。
- **系統強制字級**：平台強制把系統字級套到文字上、無法關閉時，記錄為 VIS-TEXT-SCALE 的差異。
- **色彩管線**：以線性色彩空間合成的平台，必須讓畫面上的 sRGB 結果等於交付色值；以交付後的色值驗收，不以截圖像素比對。反鋸齒與次像素造成的差異不算失敗。
- **焦點外觀**：平台自行繪製且無法替換的焦點提示，若無法達到 VIS-FOCUS 的寬度或顏色，記錄為差異。
- **字型**：字型家族與字形渲染不要求一致，不比對字型像素。

## 驗收案例與覆蓋

`VisualFoundation/visual-cases.json` 沿用 cases schema 1：每個案例含 id、initial、steps，每個 step 含 action、expect。各步驟彼此獨立，沒有共享狀態。

- **操作**：`resolve`（mode：light、dark、system、custom；systemScheme：light、dark，省略表示平台沒有偏好；custom 另有 base 與 colors，colors 以基本角色名稱為 key）；`contrast`（foreground、background）。這些欄位是語意輸入，不是 Theme 偏好的存檔格式。
- **比較**：`scheme` 完全相同；`roles` 只比較列出的角色，按 RGBA 數值；`warnings` 有列出時依序完整比較，空陣列表示沒有提醒；`ratio` 採 `abs(actual−expected) ≤ 1e−3 + 1e−5×abs(expected)`，非有限值失敗。

| 行為 ID | 中性案例 | 平台檢查 |
|---|---|---|
| VIS-PALETTE | light-mode-uses-light-palette、dark-mode-uses-dark-palette | VP01 |
| VIS-DERIVE | custom-derives-from-base-colors、custom-dark-base-uses-dark-fixed-roles、on-accent-switches-at-equal-contrast | VP01 |
| VIS-CUSTOM | custom-invalid-colors-fall-back、custom-low-contrast-warns-but-applies | — |
| VIS-CONTRAST | contrast-ratio-formula | VC01 |
| VIS-SCHEME | system-follows-platform-scheme、system-without-signal-is-light、light-mode-uses-light-palette、dark-mode-uses-dark-palette | VP02 |
| VIS-SWITCH | — | VP02 |
| VIS-NATIVE | — | VP02 |
| VIS-STATE | — | VP03 |
| VIS-FOCUS | — | VP03 |
| VIS-TEXT-SCALE | — | VP05 |
| VIS-UNIT | — | VP04 |

中性案例只驗證解析與計算規則，不能證明畫面上的顏色、切換與量測；平台檢查必須在真實畫面上執行。

| 檢查 | 初始條件 → 操作 → 必須觀察到的結果 |
|---|---|
| VC01 色票對比 | 讀設計資料的 light、dark → 依〈對比〉計算每組必要配對 → 全部達門檻。UX 在每次改動色值時執行 |
| VP01 色票交付 | 平台匯入或產生 light、dark 配色 → 讀回平台實際使用的 15 個角色值 → 與設計資料 RGBA 相同；同一輸入重複匯入無差異 |
| VP02 明暗切換 | 分別以 light、dark、system 開啟；在 system 下切換作業系統明暗；在手動模式下切換作業系統明暗；有輸入草稿與焦點時切換模式 → 有效明暗與 VIS-SCHEME 一致；system 即時跟隨、手動不變；焦點、選取、草稿、捲動不變；原生部件明暗一致 |
| VP03 狀態外觀 | light、dark 各一次：逐一呈現〈狀態對應的色彩角色〉的每列狀態，並以鍵盤移動焦點 → 每列使用指定角色；焦點外框寬度 ≥ 2 邏輯 px、完整可見；停用項目沒有懸停外觀。記錄平台、縮放、明暗與截圖 |
| VP04 量測 | 縮放 1 與 2：量測一個模組的尺寸、圓角、字級與行高 token，再量測一個圓角大於短邊一半的元素 → 誤差 ≤ ±1 裝置像素；過大圓角呈現為短邊一半 |
| VP05 文字縮放 | 把系統或瀏覽器字級設定改成預設的 2 倍 → 文字的邏輯 px 不變；把顯示或瀏覽器縮放改成 200% → 文字與版面一起放大，相對位置不變 |

## 版本與交付

規範、設計資料與案例各自版本化：不相容變更升 major、相容新增升 minor、澄清升 patch；拒絕未知 major 與未知操作。

- 新增角色屬 minor：兩種明暗都要有值，讀取端保留不認得的角色。
- 改名或移除角色、改變推導規則、改變色值格式屬 major。
- 只改色值屬 patch，必須重跑 VC01 並由各平台重做 VP01、VP03。
- 提高門檻或增加必要配對屬 minor，必須同時交付通過的配色。

1.1.0 新增 `controlBorder` 角色，屬 minor；1.0.0 的角色、色值與案例預期不變。1.2.0 移除 1.1.0 的級數 token 與 VIS-SPACING、VIS-RADIUS、VIS-TYPE；1.1.0 發布當天尚無使用端，因此以 minor 處理。內容雜湊記錄在 [manifest.json](manifest.json)。各平台的變數名稱、檔案格式與轉換工具屬實作端：Web 的 `theme.css` 格式與 CSS 名稱仍由 Web 的交付契約擁有，數值改以本設計資料為來源（[UX-WEB-2](../Requirements.Web.md#ux-web-2--色票數值改以-visualfoundation-為來源)）；Unity 直接讀本設計資料（[UX-UNITYEXTENSION-2](../Requirements.UnityExtension.md#ux-unityextension-2--webtheme-改讀-ux-的色票交付)）。實作端依 referenced-module-acceptance 在自己的套件逐 VIS-ID 記錄結果；Web 通過不代表其他平台通過。

## 決策紀錄

以下各點 2026-10-13 定案，理由保留以免重議：

- **文字只隨顯示縮放**（決策第 14 題）：Web 部分元件用 rem、AppsMenu 用 px，實作互相衝突。只隨顯示縮放是唯一能在所有平台一致驗收的做法，版面不會因只有字放大而擠壓；Web 上 WCAG 的放大文字要求由瀏覽器縮放滿足。之後做行動平台時再依需要重議。
- **新增 `controlBorder`**（決策第 15 題）：目前沒有實作。保留柔和的分隔線，同時讓輸入框外框達到 3:1；值固定隨明暗，與 `hover` 等推導角色相同，不擴大使用者可自訂的八個基本角色。
- **不設間距、圓角、字級級數**：1.1.0 依第 11–13 題訂了 4 的倍數、圓角五級、字級四級，但這些級數是新創的，不是從實作收斂，且與 Web、Unity 一致的現值（例如 AppsMenu 的 14、18、20）衝突。使用者同日指示以現成實作為準，1.2.0 取消級數，第 11–13 題的答案不再適用。不要為了湊整齊的級數去改動各平台已一致的現值。

## 來源對照

| 本文 | 來源 |
|---|---|
| 14 個既有角色與預設色值 | Web `DEFAULT_PALETTES`（八個基本角色，原為 TaskProgress 的 light／dark 配色）與 `resolveTheme` 推導值；與 `dist/theme/theme.css` 兩個區塊的 28 個值逐一相同 |
| `controlBorder` | 決策第 15 題；值取 `border` 與 `secondaryText` 之間第一個對 `pageBackground`、`panelBackground`、`hover` 皆 ≥ 3 的混色 |
| VIS-DERIVE | `resolveTheme`：`--ui-focus`、`--ui-disabled`、`--ui-on-accent` 的推導與 `--ui-hover`、`--ui-overlay`、`--ui-danger` 的固定值 |
| VIS-CUSTOM | `createCustomPalette`、`isHexColor`、`getContrastWarnings` |
| 對比公式與文字門檻 | `contrastRatio`、`getContrastWarnings`（4.5） |
| VIS-SCHEME、VIS-SWITCH、VIS-NATIVE | `resolveTheme`（系統偏好預設 light）、`applyTheme`；Web 共用 Theme 計畫〈行為契約〉的模式、生命週期與 `color-scheme` |
| VIS-STATE、VIS-FOCUS | Web 元件樣式：`apps-menu`、`dialog`、`item-list`、`playback-controls`、`property-panel`、`save-bar`、`settings-panel`、`theme-control`；焦點外框現值 2–3 px |
| VIS-TEXT-SCALE | 決策第 14 題；Web 設定元件用 rem，AppsMenu 用 px |
| VIS-UNIT | AppsMenu「圓角、間距、尺寸、字級與行高依具名 token 驗收」；apps-design.json `lineHeight` 1.4 為無單位倍數；Web 膠囊與圓形圓角 |

Unity 目前沒有色票實作：WebTheme 匯入尚未建立（UnityExtension UNITYEXTENSION-WEB-1 指向 Web `theme.css`），common `Theme` 是通用綁定機制，沒有色彩角色。以 Web 的 `dist/theme/index.js` 執行本文 10 個案例，只有含 `controlBorder` 的 4 個失敗（Web 尚無此角色），其餘通過；這只用來核對素材，不是 Web 的驗收紀錄。
