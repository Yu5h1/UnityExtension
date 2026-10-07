# Attraction 驗收紀錄（Yu5h1Lib.UIToolkit 使用的 Unity 原生接入）

Attraction 是吸塵器每一步的向目標收斂計算。Unity 端只能透過 [Scatter](Scatter.md) 的 `Pull` 使用它，所以本紀錄的證據來自 Scatter 的組合案例；行為要求以 Motion 規格為準。

## 身分

| 項目 | 內容 |
|---|---|
| 接入路線 | 與 [Throw](Throw.md#身分) 相同的 `com.yu5h1.motion` 0.1.0 與 DLL；無獨立公開入口 |
| 來源規格 | [Motion ModuleSpec/Attraction.md](../../../../../../Motion/Documentation/ModuleSpec/Attraction.md) `motion.attraction` 1.0.0；Motion `8b53e5f`；SHA-256 `36af1f5d…6d3a` |
| 驗收範圍 | ATTR-01～05、ATTR-C1～C6；2D |
| 紀錄狀態 | **verification pending**：組合案例通過，直接案例無法執行 |

## 驗收對照與結果

| 來源 ID | 檢查 | 環境 | 結果 | 證據／差異 |
|---|---|---|---|---|
| ATTR-01、02、05 | Scatter fixtures `pull-disabled-inputs`、`pull-bounds-threshold`、`pull-ieee-source` | Windows Editor，Unity 6000.6，2026-10-07 | **通過**（組合） | 見 [Scatter](Scatter.md#驗收對照與結果) |
| ATTR-03、04 | Scatter fixtures `pull-30hz`、`pull-60hz`、`pull-120hz`、`pull-sequential` | 同上 | **通過**（組合） | 同上 |
| ATTR-C1～C6 | 直接呼叫 Attraction | — | **blocked** | Motion 尚未提供獨立公開的 Attraction 接口；規格亦說明此點 |
| 全部 | 同上 | Windows Player、Android arm64 | 未執行 | 使用者 2026-10-07 決定暫不建置 |

## 結論

Attraction 經 Scatter 組合在 Windows Editor 與規格 1.0.0 相符；直接案例要等 Motion 提供獨立接口。AppsMenu 只經 Scatter 使用它，不需要另外接入。
