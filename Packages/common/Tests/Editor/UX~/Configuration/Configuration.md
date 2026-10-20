# Configuration（設定）

設定是各模組宣告可調整的欄位與預設，並以一致語意載入、保存、套用與失敗重試；值是什麼、怎麼套用由宿主決定。它不是設定面板的外觀與操作（[SettingsPanel](../SettingsPanel.md)），也不負責各模組的數值計算。分類為基礎層：不擁有領域狀態，是所有層共用的保存語意與宣告格式（第 87 題）。

> 狀態：目標 1 已達成（2026-10-20，Web）；目標 2（等級 1「直接存」在 Unity 原生 Preferences 成立）進行中（2026-10-20 點名）；目標 3（等級 2「確認保存」在 Unity 的第一個需要它的消費端成立）已定義，等消費端出現。
>
> 規範版本：1.4.1（2026-10-20：〈保存等級〉表等級 1 拿掉單一進行中命令 CFG-06，與〈目標 2〉一致）。前一版：1.4.0（2026-10-20：依第 93 題加入〈保存等級〉，目標 2 改為等級 1、目標 3 改為等級 2；案例與格式不變）；1.3.1（2026-10-20：依第 92 題加入〈能力與行為的分工〉）；1.3.0（2026-10-20：依第 90、91 題加入〈目標 2〉）；1.2.1（2026-10-20：目標 1 改為已達成）；1.2.0（2026-10-19：加入定義與〈目標〉，依第 87 題歸基礎層並自 `ModuleSpec/` 搬到 `ModuleSpec/Foundation/`；行為、案例、宣告格式與 Theme 存檔格式不變）；1.1.0（2026-10-13 自 Web 移交 UX，依決策檔 `ux-foundation` 第 20、21 題）。擁有者：Yu5h1Lib.UX。設定宣告契約 1.0.0、schemaVersion 1、Theme 偏好 version 1 各自獨立，都不變。
> 本文件是 Config 的行為與資料規範；Web 的實作驗證狀態見 [Web handoff](../../../../Web/handoff.md#cross-library-validation-tracking)，Web 的 settings-panel 任務仍由 [面板計畫](../../../../Web/Documentation/SettingsPanelPlan.md) 宣告。面板與保存列的操作見 [SettingsPanel](../SettingsPanel.md)。

## 目標

### 目標 1：保存設定的共同語意在來源平台完整通過（已達成，2026-10-20）

- 為誰服務：需要保存使用者設定的介面——AppsMenu 的設定分頁（Apps、Motion 兩份宣告）與 Theme 偏好；之後 ChatPanel 與 Unity 端引用同一語意。證據路徑是 Web 的呼叫端：`demo/apps/settings.mjs`（ConfigStore 與 `mountSettingsFields`）、`demo/settings/demo.js`（Theme 偏好）。
- 情境：
  1. 第一次開啟沒有紀錄時用宣告的預設；有紀錄時還原上次保存的值。（CFG-01）
  2. 存檔損毀或版本未知時不覆寫原資料、不能保存；修好後重新讀取即恢復。（CFG-02）
  3. 保存成功才生效；保存失敗保留草稿、可重試，不顯示成已保存。（CFG-03、CFG-04）
  4. 已保存但套用失敗時顯示「已保存、尚未生效」，重試只重新套用、不重寫。（CFG-05）
  5. 同時只有一個操作；關閉或卸載後，舊的回應不會更新畫面；不同範圍（A／B 工作區）互不覆寫。（CFG-06～CFG-08）
  6. 宣告產生分頁與欄位：一份宣告一個分頁，非法宣告整批拒絕，超出範圍的值拒絕。（CFG-09～CFG-11）
  7. 自動／手動保存模式與恢復預設；Theme 偏好的保存，OS 明暗變化不寫入。（CFG-12、CFG-13）
- 不在目標 1 內（進下一個目標）：Unity 實作與 Preferences 的對應（目標 2 保存能力、目標 3 保存行為）；Motion 宣告改格式（UX-MOTION-2，Web 維持過渡副本，由需求追蹤）；設定面板外觀與操作（SettingsPanel，暫停中）；同 key 多分頁衝突協調、雲端同步；Apps header 的 legacy Theme helper。
- 完成條件：Web 驗收紀錄的 CFG-01～CFG-14 全部 pass（補跑目前「部分」與 not run 的分支，adapter 走真實 ConfigStore），CFG-P01～CFG-P05 pass 或寫明不適用的理由；使用者在 settings demo 與 Apps 設定分頁確認保存、失敗與重試（CFG-P04）。這份結果同時是本規格的來源基準：規格當初自 Web 抽取時沒有建立基準。
- 達成紀錄：Web 於 2026-10-20 通過 CFG-01～CFG-14 與 CFG-P01～CFG-P05，使用者確認 CFG-P04；UX-WEB-23 為 Implemented；這份結果就是本規格的來源基準。進度看 UX `report.json`，證據看 [Web 驗收紀錄](../../../../Web/Documentation/Yu5h1Lib.UX/Configuration.md)。依〈保存等級〉，Web 的設定面板屬等級 2，這份結果同時涵蓋等級 1。

### 能力與行為的分工

保存設定分兩層，由不同的實作端各驗自己那一層（第 92 題）：

- 能力層：設定服務的命令與狀態——預設與載入（CFG-01）、載入失敗不覆寫（CFG-02）、保存提交順序（CFG-03）、保存失敗與重試（CFG-04）、已保存未套用（CFG-05）、單一進行中命令（CFG-06）、卸載與延遲完成（CFG-07）、通知與隔離（CFG-08）——由持有儲存入口的套件驗，不需要 UI；驗的是狀態與回傳值。
- 行為層：宿主面板接線的草稿、模式與顯示——草稿及模式（CFG-12），加上保存失敗與重試、已保存未套用裡使用者看得到的那一半（草稿還在、可重試、沒有顯示成已保存、「已保存、尚未生效」有顯示）——由各 UI 套件在自己的設定面板驗；保存列的外觀與位置在 [SettingsPanel](../SettingsPanel.md)。
- 同一條行為橫跨兩層時各驗自己那一半。例如保存失敗與重試（CFG-04）：能力層驗寫入失敗時已接受的值不變、命令回傳失敗、沒有多餘套用；行為層驗草稿還在、重試鈕可按、沒有顯示成已保存。

Web 在目標 1 一次驗完兩層（ConfigStore 與 settings demo 在同一個 repo）；Unity 依〈保存等級〉分成目標 2（等級 1）與目標 3（等級 2）。

### 目標 2：等級 1「直接存」在 Unity 原生 Preferences 上成立（進行中，2026-10-20 點名）

- 為誰服務：Unity 端以 `PlayerPreferences`（`Preferences<T>` host 與統一入口 `Preferences`）保存並套用設定的介面——VCP 的音量設定（三顆 Slider 經 host 保存、套用到 `AudioMixerProxy`）與使用者資料（`UserDataView`）、BonghuoVR 的設定面板（音量、舒適度）；都是同步、本地、拖了就存的等級 1 設定。證據路徑是 UnityExtension `Packages/common`（Yu5h1Lib.Common）的 Preferences 層，在 Yu5h1LibTest `PlayerPreferencesDemo.unity`（同一個 host 綁 uGUI 與 UI Toolkit 兩個面板）執行。
- 情境（等級 1）：
  1. 第一次開啟沒有紀錄時用 host 的 `defaultSetting`；有紀錄時還原上次保存的值。（預設與載入 CFG-01；有存檔但缺欄位時補預設並寫一次，是 Unity 宿主明定的新增欄位遷移，在紀錄寫明）
  2. 存檔讀不懂或版本未知時印 Error、用預設值繼續執行、不覆寫原存檔、該 KEY 不寫盤直到使用者明確重設；修好後重新讀取即恢復。統一入口的全部 KEY 都適用。（載入失敗不覆寫 CFG-02；第 91 題 `entry-wide`；存檔加版本欄位、解析改嚴格、舊存檔當版本 0——UnityExtension 決策 `config-storage-format`、`config-load-error-recovery`）
  3. 改了就存；不同 KEY 互不覆寫。（通知與隔離 CFG-08）
  4. 場景裡的 host 元件銷毀後，入口不再呼叫它；入口可釋放單一 KEY，釋放後下次存取重新讀取，等於重開。（卸載與延遲完成 CFG-07 在同步儲存下的原生等價檢查——UnityExtension 決策 `config-service-subject`）
- 不在目標 2 內（進下一個目標）：等級 2 的行為——保存提交順序（CFG-03）、保存失敗與重試（CFG-04）、已保存未套用（CFG-05）、單一進行中命令（CFG-06）、草稿及模式（CFG-12）——是目標 3；模組 JSON 宣告與宣告產生欄位（宣告驗證 CFG-09、值的界線 CFG-10、宣告映射 UI CFG-11、交付一致性 CFG-14、原生欄位 CFG-P03）等 Unity 出現宣告消費端（AppsMenu Unity 的 UX-UNITYEXTENSION-5 或 Motion 宣告的 UX-MOTION-2）再定目標；Theme 偏好保存（Theme 持久化 CFG-13）隨 WebTheme（UX-UNITYEXTENSION-2）；設定面板外觀與操作（SettingsPanel，暫停中）。
- 完成條件：UnityExtension 依 referenced-module-acceptance 在 `Packages/common/Documentation/Yu5h1Lib.UX/Configuration.md` 建立驗收紀錄並釘選本文雜湊（匯入與版本固定 CFG-P05）；上述四條情境經 adapter 走真實 `Preferences` 入口與 host 逐案 pass；真實 PlayerPrefs 的保存、重啟、重讀與 KEY 隔離（實際 backend CFG-P01）pass；Play 停止／重開後值仍在（生命週期 CFG-P02）由使用者在 Yu5h1LibTest demo 的 uGUI 與 UI Toolkit 兩頁各一份人工 checklist 確認（第 89 題）；原生與規格不一致處記 deviation，不算 pass。需求：[UX-UNITYEXTENSION-7](../../Requirements.UnityExtension.md#ux-unityextension-7--採用-configuration-目標-2保存語意對應原生-preferences)。

### 目標 3：等級 2「確認保存」在 Unity 的第一個需要它的消費端成立（已定義，等消費端出現）

- 為誰服務：需要草稿、確認保存、失敗重試，或使用非同步／遠端儲存的 Unity 介面；目前沒有，點名時先在程式碼找出消費端（候選：AppsMenu Unity 的設定分頁、Yu5h1LibTest 的設定 demo），UI Toolkit 與 uGUI 各自一輪。
- 情境（等級 2）：保存提交順序（CFG-03）、保存失敗與重試（CFG-04）、已保存未套用（CFG-05）、單一進行中命令（CFG-06）、卸載與延遲完成（CFG-07）、草稿及模式（CFG-12），含使用者看得到的那一半（草稿還在、可重試、沒有顯示成已保存、「已保存、尚未生效」有顯示）。
- 不在目標 3 內：保存列的外觀與位置（SettingsPanel，點名目標 3 時一併解凍）；宣告產生欄位要不要併入，點名時依消費端決定。
- 完成條件：目標 2 先達成；該消費端所在套件在自己的驗收紀錄釘選本文雜湊；六條行為在該介面逐項 pass；人工 checklist 每頁一份（面板與 Theme CFG-P04 的保存部分）由使用者確認。

## 系統階層與關係

```text
使用設定服務的應用
├─ Config
│  ├─ 宣告解析 → 欄位、預設與設定值驗證
│  └─ 設定生命週期 → 載入、保存、套用重試、通知、卸載
├─ 宿主 → 資料版本、草稿、儲存範圍、transport、apply
├─ 平台 UI → 分頁、欄位、保存列、輸入與焦點
└─ 領域模組 → Theme 偏好格式、Apps 設計預設、Motion 參數
交付 manifest → 具體預設 JSON、Theme schema、來源與產物雜湊
```

## 範圍與服務邊界

本規範供 Web 及 Unity 原生對應實作採用。Web 的 DOM、Lit、localStorage 與 AbortSignal 是平台映射，並非其他平台必須使用的技術。此輪整理既有規則並建立可逐項驗收的共同語意，不新增 runtime 或宣稱 Unity 已驗收。規模中型、架構影響限定於既有 Config 邊界；以來源核對、針對性測試及交付檢查驗證。

| 能力 | 公開入口／實際使用 | 狀態與輸入輸出擁有者 | 相依及移植方式 |
|---|---|---|---|
| 保存生命週期 | [ConfigStore](../../../../Web/src/config/index.ts)、[settings 宿主](../../../../Web/demo/settings/demo.js) | 核心持有 accepted/status/error；宿主提供 defaults/decode/storage/apply | Web 可直接重用；其他 runtime 原生實作同一命令與狀態語意 |
| 宣告與值 | [declarations](../../../../Web/src/config/declarations.ts)、[Apps 宿主](../../../../Web/demo/apps/settings.mjs) | 模組擁有欄位與預設；宿主擁有存檔 envelope | 讀取交付 JSON，不重算 Apps/Motion 預設 |
| 儲存轉接 | jsonConfigStorage | 宿主指定 key、錯誤、提交點與持久性 | localStorage／檔案／PlayerPrefs 各自 adapter；每 scope 單一 writer |
| 表單產生 | [mountSettingsFields](../../../../Web/src/ui/settings-fields.ts) | 宣告决定分類；宿主持有草稿及事件 | Web 既有面板 slots；Unity 用原生控件映射同一資料 |
| Theme | [Theme 核心](../../../../Web/src/theme/index.ts)、[偏好 schema](Configuration/theme-preference.schema.json) | Theme 擁有偏好驗證及顯示；Config 協調保存 | 移植偏好資料，不將 CSS token 當存檔 |

不包含任意 JSON 表單建構、帳號／雲端同步、多 writer 衝突合併、交易回滾、Dandelion Profile 或平台 UI 的完整視覺規範。面板樣式、Tab 方向、焦點、關閉入口與保存列見 [SettingsPanel](../SettingsPanel.md)。設定宣告不負責執行 Motion 數值求解器。

## 保存等級

保存語意分兩級，由擁有設定的模組在自己的規格裡宣告等級；實作端只驗該級的行為，驗收紀錄寫明等級（第 93 題）。分級依據是儲存器的失敗模型（同步本地幾乎不失敗；非同步、遠端、有配額的會失敗或變慢）與值丟失或錯誤的代價（音量下次再調；帳號資料不行），不是平台：VCP 的 `UserDataView` 是本地 PlayerPrefs 但存使用者資料，Web 的 Theme 偏好是本地同步的 localStorage，都要照這兩件事歸級。

| 等級 | 適用 | 行為 |
|---|---|---|
| 1 直接存 | 同步、本地、低代價的設定：Unity 以 `PlayerPreferences` 保存的消費端（VCP 音量、BonghuoVR 設定面板）、Web 的 Theme 偏好 | 預設與載入（CFG-01）、載入失敗不覆寫（CFG-02）、通知與隔離（CFG-08）、實際 backend（CFG-P01）；改了就存；沒有草稿、確認、重試與套用階段；卸載與延遲完成（CFG-07）在同步儲存下以原生等價檢查記錄一次（host 銷毀後不再被呼叫、可釋放單一 KEY）；單一進行中命令（CFG-06）屬等級 2 |
| 2 確認保存 | 非同步或遠端儲存、代價高的值（帳號、使用者資料）、介面本來就有草稿與「套用並保存」：Web 的設定面板（Apps、Motion 分頁） | 等級 1 全部，加保存提交順序（CFG-03）、保存失敗與重試（CFG-04）、已保存未套用（CFG-05）、單一進行中命令（CFG-06）、卸載與延遲完成（CFG-07）、草稿及模式（CFG-12） |

存檔損毀的處理兩級相同：執行期印 Error、用預設值繼續、不覆寫壞檔、該 KEY 不寫盤直到使用者明確重設；「格式修好後資料對不上」是 bug，以修正處理，不做執行期恢復功能。等級目前寫在模組規格與消費端紀錄；要不要寫進宣告 JSON，等有讀取端需要時再加，不改宣告契約 1.0.0。

## 狀態與操作

`accepted` 是經 decode 接受的快照，不等於目前已成功顯示的值。宿主另持有 draft；核心不包含 dirty、autoSave 或 UI 實例。資料需能複製；JSON transport 限 JSON 可表示值，產品 decode 擁有更嚴格限制。外部修改輸入、讀出的 state、傳給 storage/apply 的物件不得改變核心快照。

| 操作 | 前提及觀察 |
|---|---|
| 建立 | 驗證並複製 defaults；失敗拒絕建立。初始 idle，沒有 I/O 或 apply |
| 訂閱／取消 | 訂閱只註冊，不立即回放；以 state 讀初值。正常狀態發布各通知一次；回呼例外不能轉成保存失敗 |
| load | 無進行中操作且未 disposed；loading → read → decode → 接受值 → applying → ready 或 apply-error。讀取／decode 失敗為 load-error |
| save(draft) | 已成功解碼載入、無進行中操作、非 apply-error/disposed；saving → decode → write → 接受值 → applying → ready 或 apply-error。decode/write 失敗為 save-error |
| retryApply | 僅 apply-error 且無進行中操作；applying → ready 或 apply-error，不讀寫 storage |
| dispose | 取消目前操作、發布 disposed、釋放訂閱；重複呼叫無效果。完成中的外部副作用不能據此推定撤銷 |

load 可在空閒時重新讀取，也可從錯誤恢復。load-error 阻止 save；apply-error 的 accepted 已更新，但 load 的套用失敗不代表發生過寫入。save/load/retryApply 回傳成功與否；非法前提拒絕命令（Web 為 rejected Promise），不新增 I/O。操作持續至非同步 callback 完成，期間不排隊也不採 latest-wins。Web 執行同步通知時操作鎖仍在，觀察者應在命令完成後發起下一次命令。

storage.write 成功回覆是核心接受新值的提交界線；若 backend 已寫入卻回覆失敗，核心無法推斷遠端結果。宿主須定義其原子性、冪等及恢復策略，不能由重試或取消推定 exactly-once。訊息需可辨識失敗，不要求跨平台逐字相同。

## 設定與保存

預設自動儲存：欄位確認修改後自動保存並生效，套用按鈕保持顯示但停用；關閉「自動儲存」toggle 後進入手動模式，跨分類修改先留在草稿，按「套用並保存」才提交。手動切回自動會保存目前草稿。toggle 是本次面板工作階段的模式，每次載入 demo 預設開啟；設定值本身跨載入保存。

恢復預設也遵循目前模式：自動模式立即保存，手動模式只修改草稿。手動模式關閉或 Escape 捨棄尚未提交的草稿；保存失敗的草稿保留至重開以供重試，不宣稱已生效。自動模式的已保存修改不因關閉面板撤銷。

保存成功後才更新已接受快照、通知宿主套用並顯示成功；保存期間防止重複提交，失敗保留草稿並提供重試，不能把失敗顯示為已保存。首輪保存中暫停關閉，避免未定義的取消競態；卸載後不得由舊回應更新已卸載介面，下次載入以儲存結果為準。宿主套用失敗需明確回報，不能假裝介面已生效。

若保存成功但宿主套用失敗，已接受快照仍代表已保存值，重新開啟以該值建立草稿，並保留「已保存、尚未生效」狀態。重試只重新套用已保存快照，不重複寫入；待宿主回報成功才清除錯誤。

設定服務持有已載入快照與提交狀態；宿主的面板接線持有編輯草稿，共用面板僅管理 Dialog、Tab、模式切換與操作事件。宿主決定預設值、有效值、設定作用範圍及如何套用。Demo 使用同瀏覽器、同來源的 localStorage，A／B 工作區各用獨立 key；測試可替換為記憶體保存。WebView 或檔案保存由宿主接線，不由面板直接依賴平台。


## 模組 JSON 宣告

每個模組擁有自己的宣告，一份宣告是一個分類：AppsMenu 的宣告是 [AppsMenu/apps-settings.json](../AppsMenu/apps-settings.json)；Motion 的宣告是 Motion 的 [configuration/motion.settings.json](../../../../Motion/configuration/motion.settings.json)，由 Motion 改為本格式後成為唯一來源，在此之前 Web 的 `src/configuration/motion.json` 是過渡副本。以下 Web 的匯出名稱是 Web 實作的對應：`@yu5h1/web/config` 匯出這兩份宣告及 `resolveSettingsDeclarations`、`settingsDefaults`、`decodeSettingsValues`；`@yu5h1/web/ui` 匯出 `mountSettingsFields(panel, declarations)`。面板分類的數量、ID、名稱、順序與欄位完全取自傳入宣告陣列；加入第三份宣告不必修改產生器。一份宣告對應一個 Tab；AppsMenu 與 Motion 兩份即為兩個 Tabs。宿主明確載入並傳入宣告陣列，目前沒有掃描資料夾或監看新增 JSON 檔的機制；只把檔案放進目錄不會自動加入分頁。宿主仍須提供新模組的套用 callback。

| 欄位 | v1 意義 |
|---|---|
| `schemaVersion` | 宣告結構版本，目前 1；其他值拒絕 |
| `version` | 模組語意版本，目前 1.0.0；目前 reader 支援 major 1 |
| `module`／`label` | 唯一模組 ID（小寫字母開頭，字母數字及連字號）／分頁文字 |
| `fields` | 有序設定項目；每項 `id` 在模組內唯一 |
| `path` | 宿主值物件的點分隔路徑；不得與其他項目重疊或互為父子；保留 `version` 作存檔版本 |
| `type` | `boolean`、有限 `number`、字串 `enum` |
| `default` | 與型別、範圍相符的預設；交付 JSON 必有具體值 |
| `min`／`max`／`step` | number 的含端點範圍、建議調整幅度；step 不量化或拒絕中間有效值 |
| `options` | enum 的唯一 `value` 與顯示 `label` 陣列 |
| `unit`／`source` | 單位及來源；px 為邏輯像素，s 為秒，ratio 為無因次倍率 |

原始宣告可用 `defaultRef` 取代 `default`。Apps 預設指向 AppsMenu 設計資料的 token；Motion 的宣告由 Motion 提供具體預設或指向其核心預設。建置及 Web 宿主用同一 resolver 展開。`dist/contracts/configuration/*.json` 只保留具體 `default`，Unity 讀取這份即可，無須載入 Web 程式或 Motion Wasm。範圍是設定介面的可編輯範圍，由擁有宣告的模組決定，不代表重新定義 Motion 求解器。

宣告集合必須非空；每個模組的 fields 可以為空。field.id 與 path 每段採 `[a-zA-Z][a-zA-Z0-9-]*`，拒絕 `__proto__`、`prototype`、`constructor`；path 首段不得為 `version`。number 的 min/max 必須有限且 min≤max，step 有限且 >0；enum options 非空且 value 唯一。label 必須有非空白內容，unit/source 必須是字串；所有值均不做字串到數字或布林的隱式轉換。helper 的 settingsDefaults/decodeSettingsValues 前提是已驗證宣告；未知存檔欄位的保留或拒絕由宿主 envelope decoder 決定。

`mountSettingsFields` 只產生既有 `ui-settings-panel` 的分類和具名 slot，不新增面板。回傳 `controls`、`fill(value)`、`read()`、`dispose()`；宿主連接 change、busy、草稿及保存。label 當文字插入，不解析 HTML。匯入時先用 resolver 驗證全部宣告，再建立 UI；重複模組、路徑、非法版本、型別、預設或範圍直接拒絕。

實例見 [Apps 設定宿主](../../../../Web/demo/apps/settings.mjs)。Apps 的 enabled、釋放與吸收參數、分頁排列，Motion 的反彈、阻力、停止速度與生命期皆由 JSON 產生。宿主仍保持 `web.apps.settings.v1` 與既有 `{version:1, tabDirection, apps:{…}, motion:{…}}` 值格式；有效舊記錄不遷移、不重設。面板自動保存選擇是 session 狀態，不在此記錄。不同產品自行選擇儲存鍵與 envelope，不將完整宣告寫進使用者存檔。

### 宣告、設定值與保存的分工

| 資料／接口 | 用途 |
|---|---|
| 模組宣告 JSON | 描述 Tab、欄位、型別、範圍與預設，不是使用者存檔 |
| `resolveSettingsDeclarations(input, sources)` | 驗證宣告並展開 defaultRef；已交付的具體預設 JSON 可直接解析 |
| `settingsDefaults(declarations)` | 依 path 建立初始設定值物件 |
| `decodeSettingsValues(declarations, value)` | 驗證設定值，外層存檔 version 由宿主檢查 |
| `mountSettingsFields(panel, declarations)` | 按陣列順序建立 Tabs 與欄位；不讀寫 storage |
| `ConfigStore` | 經宿主 storage 保存，再呼叫宿主 apply 套用 |

新增模組時，宿主把已載入的新宣告加入陣列，並補上套用邏輯；既有產生器無須加模組名稱分支。原始宣告與展開後交付使用相同 schemaVersion，差別只在 defaultRef 已換成具體 default。

## Theme 與 ConfigStore

使用者透過 `ui-theme-control` 的 `theme-change` 提交偏好；程式也可提交相同資料。宿主將偏好放入草稿，依自動／手動保存模式呼叫 `ConfigStore.save`。storage 負責寫入，成功後才由 apply callback 呼叫 `ThemeController.update` 或設定 provider.preference。Theme 控制項與核心都不自行保存；同一範圍不可同時有另一個 `saveThemePreference` 寫入者。

`system` 的 OS 色系變化只重新解析與顯示，不修改 mode、不寫入 config。切換 light／dark／system 仍保留 inactive custom；切回 custom 恢復原八色。`custom.base` 決定衍生狀態色與 color-scheme，八色是實際覆寫。應保存偏好而非 resolveTheme 產生的全部 CSS token。

```js
import { ConfigStore, jsonConfigStorage } from '@yu5h1/web/config';
import { decodeThemePreference, applyTheme } from '@yu5h1/web/theme';
const controller = applyTheme(root, { version: 1, mode: 'system' });
const store = new ConfigStore({
  defaults: { version: 1, mode: 'system' },
  decode: decodeThemePreference,
  storage: jsonConfigStorage(() => localStorage, 'my-app.theme.v1'),
  apply: (preference, signal) => { signal.throwIfAborted(); controller.update(preference); }
});
await store.load();
// Host guards busy/errors, keeps draft, and sends accepted changes through store.save(draft).
// Unmount: store.dispose(); controller.dispose(); unsubscribe host listeners.
```

完整草稿及錯誤接線可引用 [settings demo](../../../../Web/demo/settings/demo.js)。保存失敗保留舊 accepted 值與新草稿；成功但套用失敗只 `retryApply()`，不重寫。損毀／未知版本不能 fallback 後覆蓋；`decodeThemePreference` 為嚴格入口。`normalizeThemePreference` 與 legacy load/save helpers 是寬容呈現／最佳努力接口，不能作為此持久化契約的成功證據。首屏可先用寬容 bootstrap 顯示，但交由 ConfigStore 後仍讀原資料；load-error 阻止保存並保留原始位元組。

## Theme v1 存檔格式

權威機器格式為 [theme-preference.schema.json](Configuration/theme-preference.schema.json)，JSON Schema 2020-12，契約 1.0.0。UTF-8 JSON 物件包含整數 `version:1`、`mode: system|light|dark|custom`，可選 `custom`；custom mode 必須提供 custom。custom 是 `base: light|dark` 加上八個必填 `#RRGGBB` 字串：pageBackground、panelBackground、heading、panelHeading、itemText、secondaryText、border、accent。接受大小寫，decode 後小寫；不接受三碼或八碼。CSS 色票可有 alpha 的契約與此偏好格式不同。v1 拒絕未知欄位，避免舊 writer 靜默丟棄新版資料。

```json
{"version":1,"mode":"dark","custom":{"base":"light","pageBackground":"#f5f3ec","panelBackground":"#fffdf8","heading":"#17211d","panelHeading":"#17211d","itemText":"#27342e","secondaryText":"#4f5e57","border":"#dcd9cf","accent":"#1f684f"}}
```

此例 dark 模式仍保留 custom。可獨立儲存此物件，也可作宿主 envelope 的 theme 欄位；外層版本、scope/key、磁碟／PlayerPrefs backend 與遷移由宿主擁有。無記錄時才用預設 `{version:1,mode:"system"}`；讀取及套用不主動寫出預設。

## 行為編號與共同案例

下表每列是一個獨立行為及其共享驗收案例；編號不隨措辭修正重排。規範案例版本為 1.0.0，與本文一起以 manifest 雜湊固定。案例使用語意命令，不要求執行 Web JavaScript；adapter 必須呼叫真實公開實作，不能以預期值代替操作。拒絕不支援的命令，不能略過後宣稱通過。

共同資料 D=`{version:1,count:1}`，N=`{version:1,count:4}`；decode 接受 version 1 與整數 count，其餘拒絕。測試 storage 初始無記錄，apply 記錄值與呼叫數；每案獨立。物件比較全部鍵值（鍵順序無關）、數值精確相等；狀態序列比較次序與次數，不比錯誤文字。非同步案例用可手動完成的 read/write/apply，不使用任意 sleep。下列序列均排除未訂閱的初始 idle。

| 行為 ID／規則 | 案例 ID：初態 → 操作 | 預期結果與邊界 | 來源證據／平台補驗 |
|---|---|---|---|
| CFG-01 預設與載入 | CFG-C01：以 D 建立、訂閱，load 無記錄；另測有 N 記錄 | 建立與訂閱無 I/O；各一次 read/apply、零 write；loading→applying→ready，accepted 分別為 D/N。無效 defaults 拒絕建立 | config.test；P01 |
| CFG-02 載入失敗不覆寫 | CFG-C02：逐一提供破損 JSON、null、version 2、讀取拒絕；load 後 save(N)，再修復記錄並 load | loading→load-error；舊 accepted 保留，零 write/apply，save 拒絕；重讀 N 成功才恢復。只有 transport 的缺失標記可選 defaults | config.test；P01 |
| CFG-03 保存提交順序 | CFG-C03：成功 load D 後清空觀測，save(N)，暫停 write 再完成 | saving 時 accepted 仍 D；write 成功後 accepted=N，再 applying→ready；一次 write(N)、一次 apply(N)，回傳成功。修改 draft、state、callback 參數不污染 accepted | config.test；P01 |
| CFG-04 保存失敗與重試 | CFG-C04：ready D，先 save 非整數 count，再讓 write 拒絕 N；最後恢復 write 重試 N | 無效值不 write；兩次失敗均 saving→save-error、accepted D、零新 apply；宿主保留 N 草稿；重試成功後 accepted N。backend 已提交卻拒絕由宿主恢復政策處理 | config.test、settings demo；P01 |
| CFG-05 已保存未套用 | CFG-C05：ready D，save N 的 apply 拒絕；save D 被拒；恢復 apply 並 retryApply；另測 load N 的 apply 拒絕 | 首次 write 一次，accepted N，saving→applying→apply-error；retry 僅 applying→ready、零額外 write/read。載入失敗套用同樣可 retry，但總 write=0 | config.test（load 分支須補驗）；P01 |
| CFG-06 單一進行中命令 | CFG-C06：分別暫停 read、write、apply；期间提出 load/save/retryApply | 第二命令拒絕、沒有額外副作用，不排隊；第一命令按結果完成。未 load 的 save、非 apply-error 的 retry 均拒絕 | config.test（全部交叉組合須補驗） |
| CFG-07 卸載與延遲完成 | CFG-C07：分別在 read/write/apply 待完成時 dispose 兩次，再完成舊 callback | disposed 僅一次；取消 signal 已設定；舊操作回傳 false，不再發布或啟動下一階段；disposed 後命令及訂閱拒絕。已寫入／已套用的外部效果不能保證回滾 | config.test（apply 分支須補驗）；P02 |
| CFG-08 通知與隔離 | CFG-C08：A/B 不同 key 各 load；A 訂閱一個拋錯 observer 及一個正常 observer，save N；取消正常 observer 再 save D | A 寫入不改 B；observer 例外不改成功結果，其他 observer 收到正常序列；取消後不再收到後續通知。不同 listener 得到獨立快照 | config.test（observer 拋錯須補驗）；P01 |
| CFG-09 宣告驗證與預設解析 | CFG-C09：有效 Apps/Motion 宣告解析，再逐一改成重複 module/id/path、父子路徑、危險 path、schema 2、major 2、無效預設、缺失 defaultRef、同時 default/defaultRef、field.id 為 1x、path 為 constructor.x 或 version.x、min>max、step=0/Infinity、空 enum options | 有效結果保序、defaultRef 展開為具體 default；無效整批拒絕且不建立 UI／寫入設定。宣告不等於使用者存檔 | config-declarations.test；P03 |
| CFG-10 值的界線與投影 | CFG-C10：使用 number min=0,max=10,step=2,default=4、boolean true、enum a/b；輸入 0、10、3，再試 -1、11、NaN、Infinity、缺欄位、字串數字與 enum c | 0/10/3 接受，step 不量化；其他拒絕。defaults/read 結果只含宣告路徑，decodeSettingsValues 不保留額外欄位；宿主擁有外層 version 與 migration | declarations.ts、config-declarations.test（完整邊界須補驗） |
| CFG-11 宣告映射 UI | CFG-C11：解析 Apps/Motion 加 host.hints boolean 分類，依序 mount、fill、修改、read、dispose | 分類與欄位依陣列順序、無核心名稱分支；label 按文字處理；read 回傳已驗證值；零 storage I/O。dispose 移除自己建立的欄位，分類與宿主事件的後续生命週期由宿主管理 | apps-settings.browser；P03 |
| CFG-12 草稿及模式 | CFG-C12：ready D 預設 auto on，改 N；關 auto，改 D、關閉重開；再改 D 並 Apply；manual reset；帶草稿切回 auto；另測 save-error 關閉重開 | auto 修改立即提交；manual 關閉捨棄未提交值，Apply 才保存；reset 遵循當前模式；切回 auto 提交草稿；save-error 草稿保留。一般 auto 模式 Apply 可見且停用；busy 阻止重複操作與關閉 | config.browser、settings demo；P04 |
| CFG-13 Theme 持久化 | CFG-C13：strict Theme decoder，custom→light→system；模擬 OS 明暗，再切回 custom；讀入 version 2、未知欄位、custom 缺色 | 模式切換保留 inactive custom；OS 變更零 write、mode 不變；八色恢復。無效記錄 load-error、原始資料保留；有效 hex 大小寫正規化為小寫 | config-declarations.test；P01、P04 |
| CFG-14 交付一致性 | CFG-C14：固定來源版本，build 後 check；逐一更動某 source 或 artifact 的內容且不重建，再 check | 原版通過，變更後拒絕過期；具體預設與同版 Apps/Motion 來源相同。重新建置才可採用新 manifest；禁止拼接不同版本的 schema/宣告/實作 | config-contracts.mjs；P05 |

表中檔名是可追溯的既有證據入口：[核心測試](../../../../Web/tests/config.test.mjs)、[宣告與 Theme 測試](../../../../Web/tests/config-declarations.test.mjs)、[面板測試](../../../../Web/tests/config.browser.mjs)、[宣告 UI 測試](../../../../Web/tests/apps-settings.browser.mjs)。它們不是上述所有分支均已執行的聲明；新增編號只是共同驗收契約，未覆蓋的分支須在各平台記錄 not run。

## 平台驗收與已知界線

| 平台檢查 ID | 動作與通過條件 | 對應行為 |
|---|---|---|
| CFG-P01 實際 backend | 於目標瀏覽器／PlayerPrefs／檔案環境保存、重啟、重讀；權限／quota 失敗不可報成功，scope 不互相覆蓋；提交點與無記錄表示有文件 | 01–05、08、13 |
| CFG-P02 生命週期 | 真實頁面卸載／原生視窗關閉及重開，舊 callback 不更新新 UI；若外部副作用已完成，下次載入反映實際記錄 | 07 |
| CFG-P03 原生欄位 | 三分類按序顯示，boolean/number/enum 可讀寫；label `<b>text</b>` 顯示為文字；空數字、非整數 step 值、鍵盤輸入各依 CFG-10 判定；解除掛載不重複事件 | 09–11 |
| CFG-P04 面板與 Theme | 執行 [settings checklist](../../../../Web/checklists/settings-panel.checklist) 的人工項目；確認窄畫面、原生 picker、Row/Column、Apply 狀態與關閉焦點；目標另驗 OS Theme 變更不保存 | 12–13 |
| CFG-P05 匯入與版本固定 | 目標記錄 spec 版本＋本文 hash、manifest 全檔與 artifact hashes；匯入具體 default JSON 不依賴 Web/Wasm 執行，修改 artifact 時拒絕原採用記錄 | 14 |

結果逐平台、逐行為記錄 pass／fail／not run（含 blocked 原因）／documented deviation。偏差需原因、影響與擁有者決定，不可算 pass；不適用需明確 scope 理由及必要替代檢查。Web 同樣受規範約束，不能用來源實作存在代替驗收。語意案例不證明實體輸入、原生控件、輔助技術或裝置操作。

已知界線：同 key 的多實例／多分頁不提供衝突協調；宣告 decoder 投影會丟棄未宣告欄位，產品保留政策須在宿主實作；Apps header Theme 仍使用 legacy best-effort helper，不能當作 strict ConfigStore 的完整驗收。settings demo 是 strict 路徑。Web mount helper 的 dispose 不清空 panel.categories，宿主應卸載面板或重設分類。這些邊界不可在 adapter 內假装成更強的保證。

實作端依 [referenced-module-acceptance](../../../../.agents/skills/referenced-module-acceptance/SKILL.md) 在自己的套件內建立映射／結果，固定本文及交付資料，將取消、缺失值、例外、storage 與 UI 操作映射到原生服務。Config 不預設指定 Unity backend。規範疑義由實作端在自己的 Requirements.UX.md 提出。

## 交付與相容性

Web 實作的交付：`npm run build:ui` 交付 `contracts/configuration/apps.json`、`motion.json`、`contracts/theme-preference.schema.json` 及 `contracts/config-delivery.json`（皆位於 dist）。manifest 記錄契約／模組版本、Theme version、來源 commit／dirty 與來源及產物 SHA-256；來源未提交時不能只用 commit 代表交付內容。`npm run check:config-contracts` 檢查過期、預設來源及產物；修改後重建，提交後亦重建。Unity 記錄完整 manifest 與自己驗收結果。

結構／型別／路徑／保存語意不相容變更升 major；模組新增項目升 minor，但宿主須明定舊記錄新增欄位遷移，不自動吞掉缺欄位。修正文字或預設升 patch 並重驗；新預設僅用於無記錄或使用者重設，不覆寫既有有效值。Theme 存檔新增或改變欄位須升整數 version 並提供明確遷移；未知版拒絕。語意版本、來源 commit 與內容 hash 分別表示相容性、來源與內容，互不取代。

| 版本 | 變更 |
|---|---|
| 1.0.0 | WEB-6 Theme v1 存檔與 ConfigStore 邊界；WEB-7 AppsMenu／Motion 宣告、產生器及解析後交付（Web 擁有） |
| 1.1.0 | 自 Web `30cc4f7` 移交 UX；AppsMenu 宣告移到 UX AppsMenu，Motion 宣告的唯一來源改為 Motion 的檔案；CFG 編號、宣告欄位、存檔格式與案例不變 |
| 1.2.0 | 加入定義與〈目標〉；歸基礎層並搬到 `ModuleSpec/Foundation/`（第 87 題）；行為、案例與格式不變 |
| 1.2.1 | 目標 1 改為已達成（2026-10-20，Web）；行為、案例與格式不變 |
| 1.3.0 | 依第 90、91 題加入〈目標 2〉（Unity 原生 Preferences）；行為、案例與格式不變 |
| 1.3.1 | 依第 92 題加入〈能力與行為的分工〉，目標 2 縮為保存能力、新增目標 3；行為、案例與格式不變 |
| 1.4.0 | 依第 93 題加入〈保存等級〉（直接存／確認保存），目標 2 改為等級 1、目標 3 改為等級 2；案例與格式不變 |
| 1.4.1 | 〈保存等級〉表等級 1 拿掉單一進行中命令（CFG-06），與〈目標 2〉一致；行為、案例與格式不變 |
