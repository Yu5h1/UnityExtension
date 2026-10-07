# Requirements — Motion

- **目標專案（Target）**：Motion，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Motion`。
- **提出專案（Requester）**：UnityExtension，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension`。
- **目前依賴方式**：`Packages/Motion` 已交付，AppsMenu 尚未接入。Unity 使用自己的 C#（`Reaction.Throw`、`GroupDragLayout`），也是 Motion 數值基準的來源。
- **未結案需求數**：2。
- **提出日期**：2026-10-07。
- **背景**：Yu5h1Lib.UIToolkit 的 AppsMenu 復刻，甩飛與吸塵器的數值計算使用 Motion 原生核心（[跨平台 UI 一致性](跨平台UI一致性.md) 決策 `physics-scope`）。Motion 已有 [Unity 原生核心接入計畫](../../../Motion/Documentation/UnityIntegrationPlan.md)，本檔只提出 Unity 這邊的使用需求，不重述該計畫。
- **驗收責任**：Motion 提供產物與證據；UnityExtension 在 AppsMenu 接入後確認並更新本檔狀態。

## UNITYEXTENSION-MOTION-1 — AppsMenu 使用 Motion 原生核心（Windows、Android）

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-07 提出）。Motion 已交付（Motion `bb0dcca`，`Packages/Motion` 進 UnityExtension `8977d46`），Unity 尚未驗收。2026-10-07 Yu5h1LibTest Windows Editor（6000.6）EditMode 套件測試 2/2 通過（fixtures 重播、生命週期）；Windows Player 與 Android IL2CPP 未跑 |
| **Requirement** | 依 Motion 的接入計畫交付 `Packages/Motion`（Throw2D、Scatter2D），平台涵蓋 Windows x64（Editor 與 Player）及 Android arm64-v8a。 |
| **Evidence** | Motion 計畫的第一個切片只有 Windows x64，Android 列為依需求追加；現在的具名使用端是 Yu5h1Lib.UIToolkit 的 AppsMenu，平台為 Windows 與 Android。Motion 環境清單沒有 Android NDK。 |
| **Acceptance** | 一、Windows 依 Motion 計畫第 1–3 階段驗收。二、Android 由同一份來源建置 `.so`，插件設定只啟用已驗證的平台；IL2CPP 建置可載入並通過同一份 fixtures，manifest 記錄平台、架構與雜湊；實機驗證可由使用者執行，未驗部分列明。三、驗證用 Unity 專案為 Yu5h1LibTest，使用端是 AppsMenu 復刻的 demo（`Samples~/AppsMenuModule`）。四、Motion 只驗收數值服務；捕獲、釋放條件等操作規則由各使用端自行驗收（Web、Unity 各自），Motion 計畫第 4 階段不以「全部收納後釋放」為準。五、C# 命名空間不使用 `Yu5h1Lib.Physics`、`Yu5h1Lib.Motion`。 |
| **Impact** | Unity AppsMenu 的甩飛與吸塵器；之後其他 Unity 使用端。 |
| **Workaround** | Unity 的 AppsMenu 復刻等本需求交付後開始；既有 `Samples~/AppsMenu` 繼續使用 C# 實作。 |

## UNITYEXTENSION-MOTION-2 — 以 json 宣告 Motion 的設定項目

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-07 提出）。Motion 已交付（Motion `bb0dcca`，`Packages/Motion` 進 UnityExtension `8977d46`），Unity 尚未驗收（套件內 `Configuration/motion.settings.json`） |
| **Requirement** | Motion 以一份 `.json` 宣告自己負責的設定項目（例如 Throw 的 edgeBounce、drag、restingSpeed、lifetime），格式依 Web 的設定模式（UnityExtension 的 [WEB-7](Requirements.Web.md#unityextension-web-7--以-json-宣告的設定模式)）。 |
| **Evidence** | 跨平台 UI 一致性決策 `demo-settings`：Motion 庫與 Web 庫的設定遵循同一套 json 模式，json 同時是 Web 設定模組的規格；決策 `motion-settings-json` 決定現在就提出。Web AppsMenu demo 的 Motion 設定分類目前從 `motion.defaults` 取預設，沒有宣告檔。 |
| **Acceptance** | Motion 交付設定 json（項目、型別、預設、範圍、版本），預設值與核心一致並隨版本化交付；格式隨 WEB-7 定案，WEB-7 定案前可先列出項目與預設，格式變更時同步更新。 |
| **Impact** | Web 設定面板的 Motion 分頁；Unity 的設定復刻。 |
| **Workaround** | Web demo 繼續從 `motion.defaults` 取預設；Unity 參數暫放 Inspector。 |
