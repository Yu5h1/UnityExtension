# Requirements — UX

- **目標專案（Target）**：Yu5h1Lib.UX，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\UX`。
- **提出專案（Requester）**：UnityExtension，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension`。
- **目前依賴方式**：Unity 實作套件依 referenced-module-acceptance 釘選 UX 的模組規範快照，見[跨平台 UI 一致性 § 引用模式](跨平台UI一致性.md#引用模式)；規格缺口只走本檔，不在實作端補寫規則。
- **未結案需求數**：0。
- **提出日期**：2026-10-21。
- **驗收責任**：UX 改規格；UnityExtension 依新版重跑對應檢查，通過後更新本檔狀態。

## UNITYEXTENSION-UX-1 — Button 與 OptionPicker 面板還有幾個狀態沒有指定色彩角色

| 欄位 | 內容 |
|---|---|
| **Status** | Implemented（2026-10-21 提出並驗證：UX 同日在 VisualFoundation 1.4.0、Button 2.1.0、OptionPicker 3.1.0 補齊四點，UX `5205838`；UnityExtension 依新版實作並重跑，乾淨 worktree EditMode 111/111、PlayMode 30/30，合併 `98688b2`，使用者在人工 checklist 確認 light、dark 頁含實心靜止鈕通過）。前一版狀態：UX 已於同日在 VisualFoundation 1.4.0、Button 2.1.0、OptionPicker 3.1.0 補齊四點（UX `5205838`） |
| **Requirement** | 在 [VisualFoundation](../../../UX/Documentation/ModuleSpec/VisualFoundation.md) 的〈狀態對應的色彩角色〉（或 Button、OptionPicker 規格）補上下列狀態用的角色：(1) `solid` Button 靜止時的底色與外框；(2) OptionPicker `solid` 面板的外框；(3) 沒有填色（`ghost`）的 Button 靜止時是否有 `controlBorder` 外框；(4) 一般「按下中」（滑鼠按住、不是切換開著）與「選取且滑過」兩種疊加狀態的底色。 |
| **Evidence** | (1) Button 的 BTN-APPEARANCE 只寫「`solid`：靜止時有底色……顏色依 VisualFoundation 的角色」，〈狀態對應的色彩角色〉沒有任何一列說實心按鈕靜止時用哪個角色；〈需要外框才能辨識的控制項〉一列只舉「沒有填色的按鈕」為例。(2) OPT-APPEARANCE 寫 `solid` 面板有 `panelBackground`、「外框與陰影」，沒說外框用 `border` 還是 `controlBorder`。(3) 同一列「沒有填色的按鈕」字面上包含 `ghost` 靜止；Web 的 demo（`demo/option-picker/demo.css`）`ghost` 靜止的外框是透明，與這一列不同，UnityExtension 先照字面畫 `controlBorder` 外框，等 UX 確認。(4) 〈狀態對應的色彩角色〉有「選取、按下中的切換、開啟，以及主要動作」一列，沒有一般按下、也沒有選取後滑過。參考：Web demo 的 `solid` 是 `--ui-surface`（`panelBackground`）底加 1px `--ui-control-border`（`controlBorder`），面板外框是 `--ui-border`（`border`）；這只是證據，不是規格。Web 的選取是 `--ui-border` 底加 focus 色字，也與表中的 `accent`／`onAccent` 不同。 |
| **Acceptance** | 規格寫明上列四點各用哪個角色（或寫明由模組自訂並給出理由），VisualFoundation 的 VP03 與 Button、OptionPicker 的外觀檢查涵蓋這些列。UnityExtension 依新版釘選重跑 `uitk-visualfoundation` 輪，並讓 light、dark 頁的實心靜止鈕通過使用者確認。 |
| **Impact** | Yu5h1Lib.UIToolkit 的 Button 與 OptionPicker（以及之後的 Toggle 等引用 Button 外觀的控件）；uGUI 之後引用時同一缺口。dark 主題下實心鈕靜止的底色與文字對比目前不成立。 |
| **Workaround** | 實心鈕靜止底色與 `solid` 面板外框維持 Unity 預設主題變數（`--unity-colors-button-background`、`--unity-colors-default-border`），驗收紀錄標為 blocked；一般按下用與選取相同的 `accent`／`onAccent`，選取後滑過維持選取外觀，都標為 UnityExtension 暫行解讀，不是規格。 |

## UNITYEXTENSION-UX-2 — AppsMenu 設定檢查與佈局保存說法矛盾及入場尺寸未明

| 欄位 | 內容 |
|---|---|
| **Status** | Implemented（2026-10-24：UX `4dcc807dff138d89b12f6980d9e840bec9c8bf53` 交付 AppsMenu 5.1.4；提出方從 Git blob 核對四份交付雜湊、兩處文字與未改的設計／案例／設定資料，獨立審查通過，保留 ChatPanel 交付。此需求驗收範圍是規格可判定性；Unity P07／P08／P11 尚未執行） |
| **Requirement** | 澄清 AppsMenu 5.1.3 的設定與佈局保存邊界，以及入場 reach 公式中的 size 定義，並在來源交付更新相關檢查文字。 |
| **Evidence** | [AppsMenu](../../../UX/Documentation/ModuleSpec/AppsMenu.md#驗收案例與覆蓋) P07 仍寫「位置仍非持久化」，APPS-CONFIG 也寫「位置不保存」；但目標 1 情境 6、佈局保存（APPS-LAYOUT）與 P11 要求重載恢復位置、順序與收納。另〈入場規範〉稱依縮放後尺寸算 reach，但公式中的 size 未定義；Unity Transition.Entrance.Plan 先計 size=baseSize×scale，Motion Interpolation 1.1.0 的固定起點規劃（ENT-03）也明定該乘積。不是原始 Unity 實作缺陷。 |
| **Acceptance** | 明列設定存檔不含佈局、佈局另依宿主保存且應恢復，P07 不再與 P11 矛盾；入場公式明確使用縮放後尺寸，或核對並引用 Motion ENT-03。同步規格版本及 manifest 雜湊，保持現有案例 ID；實作端可依唯一預期執行對應檢查。 |
| **Impact** | AppsMenu 的設定（APPS-CONFIG／P07）、佈局保存（APPS-LAYOUT／P11）、入場（APPS-OPENING／P08）；Web 與 Unity 後續採用。來源 sample 驗收不必等共用模組完成。 |
| **Workaround** | 保留現有 sample；平台紀錄列出兩處含糊，不以靜態閱讀宣稱相關檢查通過。舊輪釘選與證據保留；不在 Unity 副本補寫規範。 |
