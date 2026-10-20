# OptionPicker（選項鈕）

OptionPicker 是 choice 控件：一個觸發鈕代表一個列舉值，使用者短按切到下一個值，或從選項面板選一個；值由宿主擁有，控件只回報選了哪個選項。命令選單（選項本身執行動作）、平鋪的單選群組、多選，以及多個控件之間的關係（屬集合，例如 filterbar）都不在本控件內。

> 狀態：目標 1、2、3 已達成（2026-10-16，Web）；3.0.0 把按鈕外觀移到 Button，Web 已採用（2026-10-17）。
>
> Specification version: 3.1.0（2026-10-21：依 UnityExtension 的 UNITYEXTENSION-UX-1，OPT-APPEARANCE 寫明 `solid` 面板的外框用 `border`、陰影形狀由平台決定；來源是 Web demo 的面板現值，設計資料不變，相容新增）。前一版：3.0.2（2026-10-17；Button 1.1.0 把 `iconOnlySize` 改名為 `size`，本文只改引用名稱，行為不變。3.0.1 同日 Web 已採用 3.0.0，只改狀態，行為不變。3.0.0 同日依決策第 49 題，觸發鈕與選項的顯示與外觀規則（原 OPT-DISPLAY、OPT-APPEARANCE 的按鈕部分）與 `iconOnlySize` 移到 [Button](Button.md)，本文改為引用 BTN-DISPLAY、BTN-APPEARANCE；OPT-DISPLAY 只留觸發鈕與選項顯示什麼內容、面板內距與間距，OPT-APPEARANCE 只留面板外觀；設計資料 2.0.0。行為不變。2.4.1 2026-10-16目標 3 改為已達成，行為不變。2.4.0 同日只顯示圖示時，觸發鈕與選項是同樣大小的正方形 `iconOnlySize`（32px），面板內距 `panelPadding` 與選項間距 `optionGap`（4px），數值取自 TaskProgress；設計資料 1.1.0。使用者同日在 OP05 指出選項貼著圖示大小顯得擁擠；2.3.0 尚未結案。2.3.0 同日面板也有外觀屬性 `panel.appearance`（`solid`／`ghost`，預設 `solid`），使用者同日決定；2.2.0 尚無平台驗收。2.2.0 同日背景開關改為外觀樣式 `appearance`：`solid`（實心）／`ghost`（無底），OPT-BACKGROUND 改名為 OPT-APPEARANCE（使用者同日決定；2.1.0 尚無平台驗收）。2.1.0 同日目標 2 改為已達成；新增目標 3：觸發鈕與選項各自的顯示屬性（圖示、名稱、背景），OPT-DISPLAY、OPT-BACKGROUND，預設值維持既有外觀。2.0.0 同日目標 2 依決策檔 `ux-foundation` 第 41、42 題確認。依 plan〈UX 內部分層〉規則 4，選項面板的開啟、目前值、停用與位置從 [操作](../Foundation/Input.md) 3.0.0 移入本文（OPT-OPEN、OPT-PLACE），新增點擊模式、方向、面板位置三個屬性與 OPT-CYCLE、OPT-ICON；有自己的設計資料與案例。1.0.1 同日目標 1 改為已達成。1.0.0 同日目標 1 依第 37～39 題確認）

## 系統階層與關係

```text
Yu5h1Lib.UX
├─ 基礎：操作（Input）── INPUT-CHOICE-HOLD／KEY／CLOSE：長按選取、面板內鍵盤、關閉時機
├─ 控件：OptionPicker（本文）
│  ├─ 屬性：interaction、orientation、placement
│  ├─ 行為：OPT-VALUE、OPT-OPTIONS、OPT-OPEN、OPT-CYCLE、OPT-PLACE、OPT-ICON
│  └─ 參數：option-picker-design.json；案例：option-picker-cases.json
│     ▲ 引用
└─ 宿主：擁有目前的值、選項清單、名稱與圖示，決定選定後做什麼
      例：TaskProgress 決策面板的可見性按鈕與排序按鈕
```

## 目標

### 目標 1：TaskProgress 決策面板的選項按鈕（已達成，2026-10-16）

- 為誰服務：TaskProgress 決策面板工具列的可見性按鈕（啟用、全體關閉、禁用），以及同一工具列的排序按鈕（順排、逆排、自由排序）。目前它們用 TaskProgress 自己的 IconChoice（Svelte）；TaskProgress 最終改用 Web 庫，所以目標 1 的驗收對象是 Web 庫，由 Web demo 重現這兩種用法。
- 情境（只寫 Web 已經做到的行為）：
  1. 短按觸發鈕開啟面板，焦點在目前的值；點一個選項就選定，面板關閉，焦點回到觸發鈕。（INPUT-CHOICE-OPEN、CLOSE）
  2. 長按觸發鈕開啟面板，按住移到選項上放開即選定；在面板外放開只關閉；原地放開選到目前的值。（INPUT-CHOICE-HOLD）
  3. 宿主處理短按（排序按鈕的用法）：短按交給宿主，例如切到下一個值；長按仍開啟面板。（INPUT-CHOICE-OPEN）
  4. 鍵盤：Enter／Space 開啟；沿面板方向的方向鍵移動並頭尾循環，Home／End 到頭尾；Enter／Space 選定；Escape 關閉並回到觸發鈕；Tab 關閉後移到下一個焦點。（INPUT-CHOICE-KEY）
  5. 停用的選項會被跳過；沒有可用選項時不開啟。（INPUT-CHOICE-OPEN、KEY）
  6. 面板位置：對齊式讓目前的值對準觸發鈕；相鄰式放在旁邊，放不下換邊；都限制在畫面內，太大時可捲動。（INPUT-CHOICE-PLACE）
  7. 中斷：在外面按下、失焦、捲動、改變尺寸、第二指標、選項改變時關閉；焦點依規則回到觸發鈕或不搶回。（INPUT-CHOICE-CLOSE）
- 不在目標 1 內（進下一個目標）：選項狀態從操作規範搬到本控件（plan〈UX 內部分層〉規則 4）；觸發鈕的標籤規則（規則 6 的 choice 部分「顯示目前值並帶主題詞」，目前由宿主決定標籤）；一次性動作選項（可見性的「重置」）；不開面板的純點擊循環；TaskProgress 改用 Web 元件；UnityExtension 實作；觸控、觸控筆與螢幕閱讀器的實機驗證。
- 完成條件：Web 經 adapter 通過操作規範的 16 個 choice 案例；Web demo 重現可見性與排序兩種設定並通過 OP01；使用者在 demo 確認以上 7 個情境的手感（OP02）；Web 依 referenced-module-acceptance 建立驗收紀錄。達成時本節改為「已達成」並記日期。
- 達成紀錄：Web 通過 16 個 choice 案例、OP01 與使用者的 OP02（Web `315a350`，[驗收紀錄](../../../../Web/Documentation/Yu5h1Lib.UX/OptionPicker.md)），UX-WEB-15、16 為 Implemented。目標 1 依當時的 1.0.x 驗收；2.0.0 起 INPUT-CHOICE-OPEN、PLACE 改為本文的 OPT-OPEN、OPT-PLACE，案例也一併搬來。
### 目標 2：OptionPicker 的屬性（已達成，2026-10-16）

- 為誰服務：同目標 1，TaskProgress 決策面板的可見性與排序按鈕；目的是兩種用法都靠控件屬性設定，不再由宿主自己實作循環。
- 情境：
  1. 點擊模式 `interaction`：cycle（短按切到下一個值，不開面板）、picker（短按或長按開面板）、both（短按切到下一個值、長按開面板）。（OPT-OPEN、OPT-CYCLE）
  2. 方向 `orientation`：horizontal／vertical，決定面板排列與方向鍵。（OPT-PLACE、INPUT-CHOICE-KEY）
  3. 面板位置 `placement`：aligned／adjacent；兩種都列出全部選項，開啟時焦點在目前的值（第 42 題）。（OPT-PLACE）
  4. 選項與觸發鈕可顯示宿主提供的圖示；只有圖示時，觸發鈕的無障礙名稱仍是「主題詞：目前值」的文字。（OPT-ICON）
  5. 規則 4 的搬移：開啟、目前值、停用與位置由本控件擁有，操作規範只留手勢。（本版已完成，平台需重新匯入操作 3.0.0）
- 不在目標 2 內（進下一個目標）：圖示系統（格式、線寬、尺寸、動態圖示；基礎層，等 Web 或 UnityExtension 有實作）；一次性動作選項（可見性的「重置」）；TaskProgress 改用 Web 元件；UnityExtension 實作；觸控、觸控筆與螢幕閱讀器的實機驗證。
- 完成條件：Web 提供三個屬性（含原生 cycle 模式）與圖示顯示，經 adapter 通過本文 10 個案例與操作規範 9 個選項面板手勢案例；demo 以屬性設定兩個按鈕並加上圖示，通過 OP01、OP03；使用者在 demo 確認手感（OP02）；Web 驗收紀錄依 referenced-module-acceptance 更新。達成時本節改為「已達成」並記日期。
- 達成紀錄：Web 通過本文 10 個案例、操作規範 9 個面板手勢案例、OP01、OP03 自動 9/9，使用者確認 OP02、OP03（Web 合併於 master `16bdcac`，[驗收紀錄](../../../../Web/Documentation/Yu5h1Lib.UX/OptionPicker.md)），UX-WEB-18 為 Implemented。

### 目標 3：觸發鈕與選項的顯示屬性（已達成，2026-10-16）

- 為誰服務：同前。TaskProgress 的兩個按鈕是只有圖示、沒有底色的工具列按鈕（`card-toolbar-icon`：背景透明，滑過與選中時才出現底色）；Web demo 目前是圖示加名稱、有底色。兩種外觀都要能用屬性設定（使用者 2026-10-16）。
- 情境：
  1. 觸發鈕與選項各自設定是否顯示圖示與名稱。（OPT-DISPLAY）
  2. 觸發鈕、選項與面板各自選擇外觀：實心（平時有底色）或無底（平時沒有底色；按鈕在滑過、選中與焦點時仍有看得出來的狀態外觀）。（OPT-APPEARANCE）
  3. 不論顯示什麼，無障礙名稱不變，畫面內容也不會變空。（OPT-DISPLAY、OPT-ICON）
  4. 只顯示圖示時，觸發鈕與選項是同樣大小的正方形，圖示置中，選項之間留有間距，不會擠在一起。（OPT-DISPLAY）
- 不在目標 3 內（進下一個目標）：背景與狀態的顏色本身（沿用 VisualFoundation 的色彩角色）；圖示系統；一次性動作選項；TaskProgress 改用 Web 元件；UnityExtension 實作。
- 完成條件：Web 提供觸發鈕、選項的顯示屬性與面板的外觀屬性，demo 可切換，並能重現 TaskProgress 的「只有圖示、無底」外觀；通過 OP04（自動）；使用者確認 OP05；Web 驗收紀錄依 referenced-module-acceptance 更新。達成時本節改為「已達成」並記日期。
- 達成紀錄：Web 通過 OP01／OP03／OP04 16/16、本文 10 個與操作規範 9 個面板手勢案例，使用者確認 OP05（Web 合併於 master `7761edc`，[驗收紀錄](../../../../Web/Documentation/Yu5h1Lib.UX/OptionPicker.md)），UX-WEB-19 為 Implemented。這一輪的規範經過 2.1.0～2.4.0：背景開關改為外觀樣式、面板加上外觀、只顯示圖示時統一為正方形，都是使用者在 demo 上試出來的。
- 進度看 UX `report.json` 的 ux-integration-pilot 卡片，證據看各平台的驗收紀錄。

## 規範來源與範圍

- 選項面板的手勢：引用 [操作](../Foundation/Input.md) 3.0.0 的 INPUT-CHOICE-HOLD、KEY、CLOSE，參數 `holdDuration`、`dragThreshold` 讀其設計資料。本文不重寫這些規則。
- 參數：[option-picker-design.json](OptionPicker/option-picker-design.json)，設計版本 2.0.0：`pickerGap` 4px、`pickerViewportMargin` 4px（自操作規範設計資料 1.1.0 移來，數值不變）；`panelPadding` 4px、`optionGap` 4px（取自 TaskProgress）。`iconOnlySize` 已移到 Button 的設計資料。面板位置以畫面可視範圍（viewport）的邏輯 px 計算。
- 中性案例：[option-picker-cases.json](OptionPicker/option-picker-cases.json)，schema 1／版本 1.0.0，格式同操作規範的 `kind: choice` 案例。
- 實作證據：Web `src/ui/choice-picker.ts`（`bindChoicePicker`）；TaskProgress `experiments/editor-svelte-spike/src/IconChoice.svelte`（`interaction`、`orientation`、`placement`、icon slot、`aria-label`）、`viewer/assets/icon-choice.js`（`nextChoice`）、`VisibilityMenu.svelte`、`CardList.svelte` 與 [IconChoicePlan](../../../../TaskProgress/Documentation/IconChoicePlan.md)。

## 屬性

| 屬性 | 值 | 預設 | 說明 |
|---|---|---|---|
| `interaction` | `cycle`、`picker`、`both` | `picker` | 用一個列舉，不用兩個布林值，避免出現既不能循環也不能開面板的組合 |
| `orientation` | `horizontal`、`vertical` | `horizontal` | 面板內選項的排列方向，也是方向鍵的軸 |
| `placement` | `aligned`、`adjacent` | `aligned` | 面板相對觸發鈕的位置，見 OPT-PLACE |
| `trigger` | [Button](Button.md) 的 `icon`、`label`、`appearance` | 同 Button | 觸發鈕的 Button 設定，見 OPT-DISPLAY |
| `option` | 同上 | 同上 | 面板中每個選項的 Button 設定，同上 |
| `panel` | `appearance`（`solid`、`ghost`） | `solid` | 面板容器用哪種外觀，見 OPT-APPEARANCE |

## 行為

| ID | 前提與操作 | 必須觀察到的結果／邊界 |
|---|---|---|
| OPT-VALUE | 宿主提供目前的值與選項清單；使用者選定一個選項（面板選取或 OPT-CYCLE） | 控件回報一次所選選項的 ID（`choose`），不自行改變目前的值；宿主更新值之後，下次開啟或循環依新的值。宿主沒有更新時仍依原來的值 |
| OPT-OPTIONS | 宿主提供的選項清單 | 每個選項有唯一、非空白的 ID 與文字名稱，可標記停用。ID 重複或空白時不開啟面板、不循環。停用或隱藏的選項不可選。目前的值不可用或沒有時，焦點與對齊改用第一個可用選項 |
| OPT-OPEN | 面板關閉 | `picker`：短按觸發鈕或焦點在觸發鈕上按 Enter／Space，發出 `open`，焦點移到目前的值。`cycle`、`both`：短按與 Enter／Space 改走 OPT-CYCLE，不開面板。`picker`、`both`：焦點在觸發鈕上按沿面板方向的方向鍵、Home 或 End 開啟面板，長按依 INPUT-CHOICE-HOLD 開啟；`cycle` 這兩種操作都不開啟。沒有可用選項時不開啟 |
| OPT-CYCLE | `interaction` 為 `cycle` 或 `both`，面板關閉；短按觸發鈕或焦點在觸發鈕上按 Enter／Space | 發出一次 `choose`，ID 是依清單順序在目前值之後的下一個可用選項，最後一個之後回到第一個；目前的值不可用或沒有時取第一個可用選項；只有一個可用選項時回報它本身。不開面板，焦點留在觸發鈕。沒有可用選項時不發出 |
| OPT-PLACE | 開啟面板 | 兩種位置都列出全部選項。對齊式（預設）：目前的值（或第一個可用選項）的中心與觸發鈕中心重合，因此長按後不移動就放開會選到目前的值。相鄰式：水平面板放在觸發鈕右側、間隔 `pickerGap`、垂直置中，超出可視範圍（扣 `pickerViewportMargin`）時改放左側；垂直面板放在下方、水平置中，超出時改放上方。最後把面板限制在距可視範圍四邊 `pickerViewportMargin` 內；面板比可視範圍扣邊界還大時，限制尺寸並可捲動 |
| OPT-DISPLAY | 依 `trigger`、`option` 顯示觸發鈕與選項 | 觸發鈕與每個選項都是 [Button](Button.md)，依各自的設定套用 BTN-DISPLAY、BTN-APPEARANCE：觸發鈕顯示目前值的圖示與名稱，選項顯示各自的圖示與名稱；觸發鈕與選項用同一個 Button `size`。面板內距 `panelPadding`，選項之間間距 `optionGap`，與顯示設定無關 |
| OPT-APPEARANCE | 依 `panel.appearance` 設定 | 面板：`solid` 有浮起表面的底色（`panelBackground`）、外框 `border`（面板的邊是分隔，不是需要辨識的控制項外框）與陰影（陰影的形狀與強度由平台決定，沒有陰影能力的平台只畫外框）；`ghost` 三者都沒有，選項直接顯示在下方內容上。觸發鈕與選項的外觀由各自的 Button 設定決定（BTN-APPEARANCE），三者互不影響；顏色依 VisualFoundation 的角色 |
| OPT-ICON | 宿主為選項提供圖示 | 面板中的選項顯示各自的圖示；觸發鈕顯示目前值的圖示。觸發鈕的無障礙名稱是「主題詞：目前值的名稱」的文字，選項的無障礙名稱是選項名稱，與是否顯示文字無關。圖示的繪製、尺寸與格式由宿主決定 |

## 驗收

| 行為 ID | 中性案例 | 平台檢查 |
|---|---|---|
| OPT-VALUE | choice-hold-release-in-place-chooses-current、option-cycle-tap-wraps-to-first | OP01 |
| OPT-OPTIONS | choice-disabled-options-are-skipped、option-cycle-skips-disabled | OP01 |
| OPT-OPEN | choice-tap-opens-on-selected-and-escape-returns-focus、option-both-tap-cycles-and-arrow-opens、option-cycle-keyboard-never-opens | OP01、OP02 |
| OPT-CYCLE | option-both-tap-cycles-and-arrow-opens、option-cycle-tap-wraps-to-first、option-cycle-skips-disabled、option-cycle-keyboard-never-opens | OP01、OP02 |
| OPT-PLACE | choice-tap-opens-on-selected-and-escape-returns-focus、choice-hold-release-in-place-chooses-current、choice-adjacent-flips-to-fit、choice-adjacent-vertical-flips-above、choice-aligned-clamps-to-viewport | OP02 |
| OPT-ICON | — | OP03、OP04 |
| OPT-DISPLAY、OPT-APPEARANCE（觸發鈕與選項的部分經 BTN-DISPLAY、BTN-APPEARANCE） | — | OP04、OP05；Button 的 BT01、BT02 |
| INPUT-CHOICE-HOLD、KEY、CLOSE | 操作規範的 9 個選項面板手勢案例（`initial.kind` 為 `choice`） | OP02 |

| 檢查 | 內容 |
|---|---|
| OP01 設定重現 | demo 以屬性設定兩個 OptionPicker：可見性（啟用、全體關閉、禁用；`picker`、垂直、對齊式）與排序（順排、逆排、自由排序；`both`、垂直、對齊式）。自動檢查確認：選定或循環後宿主收到一次正確的 ID；宿主更新後再開啟或循環依新的值，未更新時依原值；ID 重複或空白時不開啟也不循環 |
| OP02 手感 `[manual]` | 使用者在 demo 以滑鼠與鍵盤操作三種點擊模式、兩種方向與兩種位置，觸發鈕放在畫面四角，確認反應與面板位置自然 |
| OP03 圖示 | 自動：兩個按鈕的選項與觸發鈕都有宿主提供的圖示，觸發鈕只顯示圖示時無障礙名稱是「主題詞：目前值」；`[manual]`：使用者確認圖示與目前的值一致、清楚可辨 |
| OP04 顯示屬性 | 自動：觸發鈕與選項在 `icon`、`label` 的四種組合下顯示正確內容，沒有圖示或兩者都關時顯示名稱；觸發鈕與選項的 `appearance` 為 `ghost` 時靜止狀態沒有底色，滑過、選中與焦點時與靜止狀態有可量測的差異；面板 `solid` 有底色、外框與陰影，`ghost` 三者都沒有；只顯示圖示時觸發鈕與選項都量得 Button `size` 的正方形，面板內距 `panelPadding`、選項間距 `optionGap`；各組合下無障礙名稱不變 |
| OP05 外觀 `[manual]` | 使用者在 demo 把兩個按鈕設成 TaskProgress 的外觀（只有圖示、`ghost`），確認像工具列按鈕、大小一致不擁擠、狀態清楚；再試幾種組合（含面板 `ghost`）確認看得懂 |

## 版本與交付

規範、設計資料與案例各自版本化，依 AppsMenu 的相容規則：不相容變更升 major、相容新增升 minor、澄清升 patch。內容雜湊記錄在 [manifest.json](../manifest.json)。實作端依 referenced-module-acceptance 在自己的套件記錄結果；Web 通過不代表其他平台通過。

## 決策紀錄

- 名稱：沿用使用者提的 OptionPicker，中文名「選項鈕」。2.0.0 起純循環也是它的一種點擊模式；名稱以有選項清單為準，不另取名。
- 長按時間：TaskProgress IconChoice 用 300ms，Web 與操作規範用 `holdDuration` 400ms。操作規範是各層共用的已定基礎，本文沿用 400ms；TaskProgress 改用 Web 庫時隨之改變。
- 目標 1 引用、不搬移（第 38 題）；目標 2 依規則 4 搬移（第 41 題）。界線：要知道目前的值或宿主允許怎麼開啟的規則屬本控件，與值無關的手勢留在操作規範。
- 相鄰式面板列出全部選項（第 42 題）：TaskProgress 會排除目前的值，Web 列出全部；採用 Web，焦點與鍵盤行為和對齊式一致。TaskProgress 改用 Web 庫時隨之改變。
- 循環跳過停用的選項：依 OPT-OPTIONS「停用不可選」推得；TaskProgress 的選項沒有停用狀態，Web 由宿主循環，兩者都沒有反例。
- 外觀的歸屬：觸發鈕與選項的外觀樣式、顯示圖示與名稱、只顯示圖示時的尺寸屬所有可按控件，原先寫在本文；Button 規範成立後（第 49 題，3.0.0）移到 [Button](Button.md)，本文改為引用。外觀用樣式列舉與 32px 的理由一併移到 Button 的決策紀錄。
- 面板外觀（使用者 2026-10-16）：兩邊實作的面板都有底色、外框與陰影（TaskProgress `icon-choice-options`、Web demo `.picker-panel`），`ghost` 沒有實作可收斂，是使用者的產品決定；`ghost` 依按鈕的同名樣式解讀為不畫底色、外框與陰影。面板與選項都是 `ghost` 時，選項文字直接落在宿主的內容上，VisualFoundation 的對比要求無法保證，選用這個組合時由宿主確保可讀。
- 顯示內容不會變空：圖示與名稱都關閉或沒有圖示時改顯示名稱，避免出現看不見的按鈕；兩邊實作都沒有反例。
- 案例：操作規範 2.0.0 的 7 個選項面板案例中，6 個原文移來（`activate: false` 改為 `interaction: "picker"`，`true` 改為 `both`）；`choice-tap-activates-when-module-handles-it` 依目標 2 改寫為 `option-both-tap-cycles-and-arrow-opens`（短按由控件循環，不再發出 `activate`）。新增 3 個 cycle 案例。
