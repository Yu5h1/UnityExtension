# Scatter 驗收紀錄（Yu5h1Lib.UIToolkit 使用的 Unity 原生接入）

AppsMenu 的吸塵器吸收（Pull）與釋放散佈使用 Motion 的 Scatter；Pull 內含 [Attraction](Attraction.md)。本文件記錄 Scatter 在 Unity 經原生接入的驗收；行為要求以 Motion 規格為準。接入路線與紀錄位置的理由同 [Throw](Throw.md)。

```text
Motion（規格與 C++ 核心）
└─ Packages/Motion（com.yu5h1.motion，Motion 交付的原生接入）
   └─ Yu5h1Lib.UIToolkit AppsMenu（使用端；本紀錄由此擁有）
```

## 身分

| 項目 | 內容 |
|---|---|
| 接入路線 | 與 [Throw](Throw.md#身分) 相同的 `com.yu5h1.motion` 0.1.0 與 DLL |
| 公開入口 | `MotionScatter2D`（命名空間 `Yu5h1Lib`） |
| 來源規格 | [Motion ModuleSpec/Scatter.md](../../../../../../Motion/Documentation/ModuleSpec/Scatter.md) `motion.scatter` 1.0.0；Motion `8b53e5f`；SHA-256 `097c4ddf…2ef`；依賴 Attraction 1.0.0 |
| Fixtures | `tests/fixtures/scatter2d.json` `2dfeb835…6bf`，23 組；與套件內 fixtures 的 `sourceSha256` 相同 |
| 驗收範圍 | SCATTER-01～08、SCATTER-P1、SCATTER-C1／C2、ARCH-01～04；平台同 Throw |
| 紀錄狀態 | **verification pending** |

## 實作對照

| 來源 ID | Unity 入口 | Unity 端對應 |
|---|---|---|
| SCATTER-01 | 建構子、`SetItems`、`SetObstacles` | 尺寸非正時丟 `ArgumentOutOfRangeException`；重複 ID 丟 `ArgumentException` |
| SCATTER-02～05 | `Resolve`、`Commit`、`Reset`、`SetSpread` | `Resolve` 回傳新陣列；`MotionPlacement.Position == null` 表示無位置 |
| SCATTER-06～07 | `Pull` | 每次 Pull 讀取前一次的最新位置 |
| SCATTER-08 | `Positions` 快照、`Dispose` | — |
| SCATTER-C1／C2 | 無公開放置原語 | Unity 只有組合後的 Scatter，沒有單獨的放置入口 |
| ARCH-01～04 | 無 canonical Scatter 入口 | `MotionScatter2D` 是以維度命名的類別 |

## 驗收對照與結果

| 來源 ID | 檢查 | 環境 | 結果 | 證據／差異 |
|---|---|---|---|---|
| SCATTER-01～07 | 23 組 fixtures 全部重播（含 `pull-*`、`vacuum-moving-target`、`release-then-vacuum`） | Windows Editor，Unity 6000.6，Yu5h1LibTest EditMode，2026-10-07 | **通過** | 與 Throw 同一次執行，779 狀態 |
| SCATTER-08、SCATTER-P1（已執行部分） | 兩個佈局獨立；重設、重複釋放；重複 ID 被拒且保留原清單；非成員 Pull 回 false；釋放後操作被拒；尺寸 0 被拒 | 同上 | **通過** | `EditorLifecycle` |
| SCATTER-08、SCATTER-P1（未執行部分） | 非有限尺寸、超出範圍 ID；修改快照不影響內部狀態；宿主關閉重建 | — | 未執行 | 現有測試沒有涵蓋 |
| SCATTER-C1／C2 | 放置原語直接測試 | — | 未執行 | Unity 無公開原語；規格允許只在原語可用時測 |
| SCATTER-01～08 | 同上 | Windows Player、Android arm64 IL2CPP | 未執行 | 使用者 2026-10-07 決定暫不建置 |
| ARCH-01～04 | 公開入口與維度模組檢查 | — | **失敗** | 沒有 canonical Scatter 入口；屬 Motion 端遷移 |

## 結論與下一步

Scatter 的數值行為在 Windows Editor 與規格 1.0.0 相符（SCATTER-01～07）；生命週期部分驗證，其他平台未執行，架構不符合 ARCH-01。

下一步：

- 補 SCATTER-P1 未執行的項目。
- AppsMenu 改用 Scatter 時，在 [AppsMenu 紀錄](../Yu5h1Lib.UX/AppsMenu.md) 的 APPS-VACUUM、APPS-RAMP、APPS-RELEASE 驗收接入行為。
- 每幀 Pull 前確認 `Resolve` 的陣列配置不造成問題。
