# Requirements — Motion

- **目標專案（Target）**：Motion，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Motion`。
- **提出專案（Requester）**：UnityExtension，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension`。
- **目前依賴方式**：尚未接入。Unity 使用自己的 C#（`Reaction.Throw`、`GroupDragLayout`），也是 Motion 數值基準的來源。
- **未結案需求數**：1。
- **提出日期**：2026-10-07。
- **背景**：Yu5h1Lib.UIToolkit 的 AppsMenu 復刻，甩飛與吸塵器的數值計算使用 Motion 原生核心（[跨平台 UI 一致性](跨平台UI一致性.md) 決策 `physics-scope`）。Motion 已有 [Unity 原生核心接入計畫](../../../Motion/Documentation/UnityIntegrationPlan.md)，本檔只提出 Unity 這邊的使用需求，不重述該計畫。
- **驗收責任**：Motion 提供產物與證據；UnityExtension 在 AppsMenu 接入後確認並更新本檔狀態。

## UNITYEXTENSION-MOTION-1 — AppsMenu 使用 Motion 原生核心（Windows、Android）

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-07 提出） |
| **Requirement** | 依 Motion 的接入計畫交付 `Packages/Motion`（Throw2D、Scatter2D），平台涵蓋 Windows x64（Editor 與 Player）及 Android arm64-v8a。 |
| **Evidence** | Motion 計畫的第一個切片只有 Windows x64，Android 列為依需求追加；現在的具名使用端是 Yu5h1Lib.UIToolkit 的 AppsMenu，平台為 Windows 與 Android。Motion 環境清單沒有 Android NDK。 |
| **Acceptance** | 一、Windows 依 Motion 計畫第 1–3 階段驗收。二、Android 由同一份來源建置 `.so`，插件設定只啟用已驗證的平台；IL2CPP 建置可載入並通過同一份 fixtures，manifest 記錄平台、架構與雜湊；實機驗證可由使用者執行，未驗部分列明。三、驗證用 Unity 專案為 Yu5h1LibTest，使用端是 AppsMenu 復刻的 demo（`Samples~/AppsMenuModule`）。四、Motion 只驗收數值服務；捕獲、釋放條件等操作規則由各使用端自行驗收（Web、Unity 各自），Motion 計畫第 4 階段不以「全部收納後釋放」為準。五、C# 命名空間不使用 `Yu5h1Lib.Physics`、`Yu5h1Lib.Motion`。 |
| **Impact** | Unity AppsMenu 的甩飛與吸塵器；之後其他 Unity 使用端。 |
| **Workaround** | Unity 先做非物理部分；既有 `Samples~/AppsMenu` 繼續使用 C# 實作。 |
