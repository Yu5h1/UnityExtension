# Throw 驗收紀錄（Yu5h1Lib.UIToolkit 使用的 Unity 原生接入）

AppsMenu 的單圖示甩飛與吸塵器釋放使用 Motion 的 Throw。本文件記錄 Throw 在 Unity 經原生接入的數值與生命週期驗收；行為要求以 Motion 規格為準。

```text
Motion（規格與 C++ 核心）
└─ Packages/Motion（com.yu5h1.motion，Motion 交付的原生接入）
   └─ Yu5h1Lib.UIToolkit AppsMenu（使用端；本紀錄由此擁有）
```

`Packages/Motion` 由 Motion 產生並以 `delivery.json` 追蹤內容，重新交付會覆寫，所以 Unity 端的驗收紀錄放在使用它的套件。

## 身分

| 項目 | 內容 |
|---|---|
| 接入路線 | 共用核心：`com.yu5h1.motion` 0.1.0，ABI 1；Motion 產出 `bb0dcca`（乾淨）；Windows DLL SHA-256 `1849e2e9…81b6`，與 [delivery.json](../../../Motion/delivery.json) 一致 |
| 公開入口 | `MotionThrow2D`（命名空間 `Yu5h1Lib`） |
| 來源規格 | [Motion ModuleSpec/Throw.md](../../../../../../Motion/Documentation/ModuleSpec/Throw.md) `motion.throw` 1.0.0；Motion `8b53e5f`；SHA-256 `7088b790…292e`；架構要求見 [DesignPrinciples](../../../../../../Motion/Documentation/DesignPrinciples.md) §5 |
| Fixtures | `tests/fixtures/throw2d.json` `515445d1…73b9`，19 組；套件內 `Tests/Runtime/Resources/MotionValidation/fixtures.json` 的 `sourceSha256` 相同 |
| 驗收範圍 | THROW-01～07、THROW-P1、ARCH-01～04；平台 Windows Editor、Windows Player、Android arm64 |
| 紀錄狀態 | **verification pending**：Windows Editor 數值通過；生命週期部分執行；Player、Android 未執行；架構有缺口 |

## 實作對照

| 來源 ID | Unity 入口 | 共用依賴 | Unity 端對應 |
|---|---|---|---|
| THROW-01～05 | `MotionThrow2D.Launch`、`Tick`、`State` | `MotionNative.dll` | C# 只轉送參數與讀取狀態，不自行計算 |
| THROW-06 | `Stop` 後 `Launch` | 同上 | — |
| THROW-07 | `State` 快照、多實例、`Dispose` | SafeHandle | 釋放後操作丟 `ObjectDisposedException` |
| ARCH-01～04 | 無 canonical Throw 入口 | — | `MotionThrow2D` 是以維度命名的類別；DesignPrinciples 視為僅供遷移的舊入口 |

## 驗收對照與結果

| 來源 ID | 檢查 | 環境 | 結果 | 證據／差異 |
|---|---|---|---|---|
| THROW-01～06 | 19 組 fixtures 全部重播（`MotionValidationBuild.EditorFixtures` → `MotionNativeTests.SharedReferenceFixtures`，連同 Scatter 共 779 狀態） | Windows Editor，Unity 6000.6，Yu5h1LibTest EditMode，2026-10-07 | **通過** | 測試 2/2 通過，Console 無錯誤 |
| THROW-07、THROW-P1（已執行部分） | A／B 兩實例獨立；A 重複 `Dispose` 安全；釋放後 `Stop`、讀取 `State` 被拒（`EditorLifecycle`） | 同上 | **通過** | — |
| THROW-07、THROW-P1（未執行部分） | 釋放後 `Tick`、`Launch` 被拒；修改快照不影響下次讀取；釋放後建立 C；宿主關閉重建；缺少 DLL、ABI 不符的診斷 | — | 未執行 | 現有測試沒有涵蓋 |
| THROW-01～07 | 同上 | Windows Player | 未執行 | 使用者 2026-10-07 決定暫不建置 |
| THROW-01～07 | 同上 | Android arm64 IL2CPP | 未執行 | 同上；Android importer 停用中 |
| ARCH-01～04 | 公開入口與維度模組檢查 | — | **失敗** | 沒有 canonical Throw 入口；由 Motion 擁有的套件提供，屬 Motion 端遷移 |

## 結論與下一步

Throw 的數值行為在 Windows Editor 與規格 1.0.0 相符（THROW-01～06）。THROW-07 只部分驗證，其他平台未執行，架構不符合 ARCH-01，因此整體仍是 verification pending。

下一步：

- 補 THROW-P1 未執行的項目。
- AppsMenu 改用 Throw 時，在 [AppsMenu 紀錄](../Yu5h1Lib.Web/AppsMenu.md) 的 APPS-RELEASE、APPS-VELOCITY 驗收接入行為。

Motion 規格、fixtures 或原生產物更新時，重新執行受影響的 THROW-ID；上表結果只對應上列版本。
