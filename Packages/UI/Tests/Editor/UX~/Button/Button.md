# Button（按鈕）

Button 定義可按控件共用的外觀：平時與滑過、選中、焦點時看起來怎樣，按鈕與圖示多大，以及怎麼顯示、排列圖示與名稱。按下後做什麼由使用它的控件或宿主決定；按下、鍵盤與名稱的判定引用 [操作](../Foundation/Input.md)。

> 狀態：目標 1 已達成（2026-10-17，Web）。BTN-SIZE、BTN-LAYOUT 尚無目標；Yu5h1Lib.UIToolkit 已依 1.1.0 引用，需依 2.0.0 重新引用。
>
> Specification version: 2.0.0（2026-10-17；依第 63 題，圖示一律 fit 按鈕：名稱旁的圖示也是 `size` × `iconRatio`，移除「跟著字型縮放」與 `iconWithLabelSize`、`labelFontSize`，設計資料 3.0.0。1.1.0 同日依決策檔 `ux-foundation` 第 58～62 題新增按鈕大小 `size` 屬性、BTN-SIZE、BTN-LAYOUT 與 BT03；`iconOnlySize` 改名為 `size`，有名稱時的高度也是 `size`；設計資料 2.0.0。1.0.1 同日目標 1 改為已達成，行為不變。1.0.0 同日目標 1 依決策檔 `ux-foundation` 第 49 題確認。BTN-DISPLAY、BTN-APPEARANCE 與 `iconOnlySize` 自 [OptionPicker](OptionPicker.md) 2.4.1 的 OPT-DISPLAY、OPT-APPEARANCE 按鈕部分搬來，數值與行為不變）

## 系統階層與關係

```text
Yu5h1Lib.UX
├─ 基礎：操作（Input）── 按下 INPUT-PRESS-TAP、鍵盤 INPUT-KEY-COMMAND、名稱 INPUT-NAME
│        VisualFoundation ── 色彩角色、焦點、對比
├─ 控件：Button（本文）── 外觀 solid／ghost、圖示與名稱的顯示與排列、按鈕大小與圖示大小
│     ▲ 引用
│     ├─ OptionPicker 的觸發鈕與選項
│     └─ 之後的 toggle
└─ 宿主：決定按下後做什麼、提供圖示與名稱
```

## 目標

### 目標 1：可按控件的共用外觀（已達成，2026-10-17）

- 為誰服務：TaskProgress 工具列的圖示按鈕（`card-toolbar-icon`：CardList「全部展開／收合」、CardDisclosure 可見性、IconChoice）與 Web 的 OptionPicker；之後的 toggle 直接引用同一套外觀。這些按鈕都是 toggle 或 choice，目前沒有單純的動作按鈕。
- 情境：
  1. 外觀 `solid`（實心）／`ghost`（無底），無底時滑過、選中、焦點仍看得出來。（BTN-APPEARANCE）
  2. 顯示圖示、顯示名稱的組合，沒有圖示或兩者都關時顯示名稱，無障礙名稱不隨顯示改變。（BTN-DISPLAY）
  3. 只顯示圖示時是 `iconOnlySize` 的正方形，圖示置中。（BTN-DISPLAY）
- 不在目標 1 內（進下一個目標）：toggle 的按下狀態與 plan 規則 6 的標籤；其他外觀樣式（例如 Web item-list 的強調按鈕 `button.primary`、只有外框）；圖示系統；UnityExtension。
- 完成條件：Web 重新匯入 Button 1.0.0 與 OptionPicker 3.0.0，BT01 通過（OptionPicker 既有的外觀檢查改對照 BTN-*），不需要新的實作；Web 建立 Button 驗收紀錄；使用者確認 demo 外觀沒有變化（BT02）。達成時本節改為「已達成」並記日期。
- 進度看 UX `report.json`，證據看各平台的驗收紀錄。

目標 1 的情境 3 以 1.0.x 的 `iconOnlySize` 寫成，1.1.0 起同一個值改名為 `size`。BTN-SIZE、BTN-LAYOUT 與 BT03 在下一個目標驗收；下一個目標預定是 UnityExtension 的 Button，尚未訂定。Web 採用的是 1.0.0，尚未對照 2.0.0（Web 現況：觸發鈕高 38px、選項高 34px、名稱旁圖示 18px）。

## 規範來源與範圍

- 按下、鍵盤與名稱：引用操作的 INPUT-PRESS-TAP、INPUT-KEY-COMMAND、INPUT-NAME；顏色、焦點與對比：引用 [VisualFoundation](../VisualFoundation.md) 的色彩角色與狀態對應。本文不重寫這些規則。
- 參數：[button-design.json](Button/button-design.json)，設計版本 3.0.0：`size` 32px、`iconRatio` 0.75、`iconLabelGap` 8px。
- 實作證據：TaskProgress `viewer/assets/styles.css` 的 `.card-toolbar-icon`（32px 正方形、padding 4px、背景透明、滑過與選中才有底色；圖示區 24px）；Web `src/ui/choice-picker.ts` 的顯示與外觀（`data-choice-show-icon`、`data-choice-show-label`、`data-choice-appearance`、`--choice-icon-only-size`）與 demo 的 CSS（`.picker-icon` 18px、圖示與名稱間距 8px、14px 字）。兩者都沒有讓設定修改按鈕大小的實作；`size` 可設定與圖示的縮放規則來自第 60～62 題。
- 名稱的字型大小不屬於 Button，由使用它的控件或宿主決定；圖示大小不隨字型改變。

## 屬性

| 屬性 | 值 | 預設 | 說明 |
|---|---|---|---|
| `icon` | 布林值 | `true` | 是否顯示圖示，見 BTN-DISPLAY |
| `label` | 布林值 | `true` | 是否顯示名稱，見 BTN-DISPLAY |
| `appearance` | `solid`、`ghost` | `solid` | 外觀樣式，見 BTN-APPEARANCE |
| `size` | 正數，px | 設計資料 `size`（32） | 按鈕大小，可由設定修改，見 BTN-SIZE |

## 行為

| ID | 前提與操作 | 必須觀察到的結果／邊界 |
|---|---|---|
| BTN-DISPLAY | 依 `icon`、`label` 顯示按鈕 | `icon` 為 true 且有圖示時顯示圖示；`label` 為 true 時顯示名稱。兩者都是 false，或 `label` 為 false 但沒有圖示可顯示時，仍顯示名稱，內容不會變空。顯示設定不改變無障礙名稱（INPUT-NAME）。只顯示圖示（圖示顯示、名稱不顯示）時，按鈕是 `size` × `size` 的正方形，圖示置中；有名稱時按鈕高度是 `size`。同一個控件內的按鈕用同一個 `size` |
| BTN-APPEARANCE | 依 `appearance` 設定 | `solid`（實心）：靜止時有底色。`ghost`（無底）：靜止時沒有底色，滑過、選中與焦點時仍有看得出來的狀態外觀。顏色依 VisualFoundation 的角色 |
| BTN-SIZE | 依 `size` 畫圖示 | 圖示邊長 = `size` × `iconRatio`（32 → 24、40 → 30、24 → 18），不論旁邊有沒有名稱。圖示是正方形區域，依 [Icon](../Foundation/Icon.md) 的來源等比放入、置中。改變 `size` 時圖示依此規則跟著變，不另外設定 |
| BTN-LAYOUT | 圖示與名稱同時顯示 | 圖示在閱讀方向的起始側、名稱在後，間距 `iconLabelGap`（8px），兩者垂直置中。內容水平置中或靠起始側由使用 Button 的控件決定 |

## 驗收

| 行為 ID | 中性案例 | 平台檢查 |
|---|---|---|
| BTN-DISPLAY | — | BT01、BT02 |
| BTN-APPEARANCE | — | BT01、BT02 |
| BTN-SIZE | — | BT03 |
| BTN-LAYOUT | — | BT03 |

| 檢查 | 內容 |
|---|---|
| BT01 外觀與顯示 | 自動：在使用 Button 的控件上（目標 1 為 OptionPicker 的觸發鈕與選項），`icon`、`label` 的四種組合顯示正確內容，沒有圖示或兩者都關時顯示名稱；只顯示圖示時量得 `size`（1.0.x 稱 `iconOnlySize`）的正方形、圖示置中；`ghost` 靜止沒有底色，滑過、選中與焦點與靜止有可量測的差異，`solid` 靜止有底色；各組合下無障礙名稱不變 |
| BT02 外觀未變 `[manual]` | 使用者在 demo 確認搬移後的預設外觀與 TaskProgress 外觀，和搬移前看起來相同 |
| BT03 大小與排列 | 自動：`size` 為預設 32 與另一個值（例如 40、24）時，只顯示圖示的按鈕量得 `size` × `size`，有名稱的按鈕高度為 `size`；兩種按鈕的圖示邊長都是 `size` × `iconRatio`；圖示在名稱起始側、間距 8px、兩者垂直置中。量排版後的實際尺寸，不以設定值推算 |

本文沒有中性案例；外觀屬視覺結果，由平台檢查判定。

## 版本與交付

規範與設計資料各自版本化，依 AppsMenu 的相容規則：不相容變更升 major、相容新增升 minor、澄清升 patch。內容雜湊記錄在 [manifest.json](../manifest.json)。實作端依 referenced-module-acceptance 在自己的套件記錄結果。

## 決策紀錄

- 定位（第 49 題）：TaskProgress 工具列用 `card-toolbar-icon` 的三個按鈕都是 toggle 或 choice，沒有單純的動作按鈕；按下的判定已在操作規範。所以 Button 擁有的是可按控件共用的外觀，不重寫按下的行為。
- 外觀用樣式列舉，不用布林值或 alpha 數值（使用者 2026-10-16，原寫在 OptionPicker）：布林值容易被讀成連滑過、選中的底色一起關掉，也無法再加樣式（例如只有外框）；alpha 數值沒有實作用到中間值，作用在整個背景時會讓狀態外觀消失，而且每個實例自訂透明度會讓 VisualFoundation 的對比要求無法驗收。
- 只顯示圖示的尺寸（使用者 2026-10-16，原寫在 OptionPicker）：Web demo 原本觸發鈕 38px、選項 34px，兩者不同且擁擠；TaskProgress 的工具列按鈕都是 32px 正方形。TaskProgress 是實際消費端，採用它的數值。
- 圖示大小分兩種情況（第 58 題）：只顯示圖示時採用 TaskProgress 的圖示區 24px（32 減去兩側 4px）；圖示在名稱旁時跟著字型，採用 Web 的 18px（14px 字），也符合常見設計系統「名稱旁的圖示跟著字型」的做法。TaskProgress 沒有圖示加名稱的按鈕，所以 24 只對只顯示圖示有依據。
- 圖示與名稱的排列（第 59 題）：只有 Web 一個實作，沒有衝突，採用它的做法。
- 按鈕大小可由設定修改，圖示受按鈕大小約束（第 60 題，使用者答「其他」）：現有實作都沒有這個設定，屬產品決定；因此不把圖示寫成固定數值。
- 圖示依比例縮放（第 61 題）：比例 0.75 取自第 58 題的 24／32，讓不同大小下外觀一致；名稱旁的圖示仍跟字型，只受比例上限約束。
- 圖示一律 fit 按鈕（第 63 題，使用者 2026-10-17）：按鈕是成熟的標準 UI，圖示跟著按鈕大小就好；「名稱旁的圖示跟著字型縮放」是唯一需要額外程式碼的規則，會提高實作與驗收成本，因此移除。第 58 題「旁邊有名稱時跟著字型」與第 61 題的「名稱旁只受上限約束」不再適用。
- 同一個大小設定（第 62 題）：同一列的圖示鈕與名稱鈕等高，設定修改大小時只動一處。`iconOnlySize` 不再只用於只顯示圖示，所以改名為 `size`，設計資料升 2.0.0。名稱按鈕的左右內距尚未定，待有目標需要時再定。
