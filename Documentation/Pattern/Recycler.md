# Recycler 的 UnityExtension Core 驗收

本輪對照 [Pattern Recycler 1.0.0](../../../../Pattern/Documentation/ModuleSpec/Recycler.md)
與 [.NET 驗收紀錄](../../../../DotNet/Documentation/Pattern/Recycler.md)。[Unity Core Runtime 專案](../../../Core/Runtime/Yu5h1Lib.Runtime.csproj)
直接連結 `DotNet/Source/Recycling`。`com.yu5h1.library` 1.0.1 透過
[Yu5h1Lib.Runtime.dll](../../Packages/Core/Runtime/Yu5h1Lib.Runtime.dll) 提供
`Yu5h1Lib.Recycling.Recycler` 及 `Recycler<T>`。

## 2026-11-12 的 Yu5h1LibTest Editor 驗證

Runtime 的 Release `netstandard2.1` 分別用 Unity 6000.3.9f1 與 6000.6.0f1 的參考組件
編譯成功，交付採用 6000.3.9f1 建置。Yu5h1LibTest 在 Unity 6000.6.0f1 的
Editor 載入本機套件並執行同一份 prefab。Editor 提供的組件路徑與套件實體檔案相同，
兩者的 SHA-256 亦相同。

| 驗證 | 結果 |
|---|---|
| 套件來源 | Editor 從 `Packages/Core/Runtime/Yu5h1Lib.Runtime.dll` 載入 Recycler，載入檔案的 SHA-256 與交付檔一致。 |
| 借出與歸還 | 待命區空時建立三個不同的 prefab 實例。借出啟用，歸還停用並保留第一個實例。 |
| 同實例重用 | 再借出取得第一個實例的同一參照，建立次數維持 3。 |
| 待命上限 | 上限 1 只丟棄本次超額歸還，另一個借出實例仍有效。上限 0 歸還後不保留。 |
| 參照身分 | 值相等的不同物件獨立追蹤。另一個 Recycler 忽略值相等的外來實例。 |

本輪通過 7 項定向檢查，涵蓋套件來源和 prefab 接合。平台證據範圍為上述
Unity Editor。舊池遷移與 Player／IL2CPP 驗收依 [重構計畫](../Recycler_Refactor.md) 各自處理。

- 交付與載入 DLL SHA-256 `d6120400d738b5376bcd1085ae7deecfc3ad7ce543e90ce9a04f36eae04cb747`
- 與前次隔離驗證相同的 prefab SHA-256 `25ceb66384683e9600f5a5399acfb79bd253c681831764c2da8ecbee4274c14d`
- Pattern 規格 SHA-256 `89954234321b19d06b25cd972d850a1e23134532585113e25c6f24ebecc82827`

## 2026-11-12 移除 V1 後重跑

移除 Unity Core 的 `Runtime/Unorganized/Singleton/Recycler.cs` 後，使用 Unity 6000.3.9f1
和 6000.6.0f1 的參考組件重建 Release Runtime。組件比對確認公開型別差異只有 V1 檔案
定義的 11 個型別移除。`Yu5h1Lib.Recycling.Recycler` 及 `Recycler<T>` 仍由 Core 提供。

Yu5h1LibTest 以相同 prefab 重新執行上述 **7 項 Editor 檢查，全部通過**。已載入的
DLL 路徑指向本機 `com.yu5h1.library` 套件，載入與交付的 SHA-256 一致。交付使用
6000.3.9f1 參考組件建置，實測 Editor 版本為 6000.6.0f1。

- 交付與載入 DLL SHA-256 `1ee5cb5fc1905cca835fd4eb10ad33df92875875bd25fb5621b203c371fe733e`
- 同一 prefab SHA-256 `25ceb66384683e9600f5a5399acfb79bd253c681831764c2da8ecbee4274c14d`
