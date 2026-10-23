# Yu5h1Lib.Addressables

Unity Addressables 的擴充：依需求下載與載入建置好的內容、回報進度與狀態，載入時的非同步表演與初始化流程由消費端定義。

目前有讀取處理器 `LoaderHandler`（全域唯一，讀取器欄位空著時用 common 的 `ResourcesLoader`）、場景裡的代理 `LoaderAgent`（`Prepare(key)` 讀取 prefab、產生實例）、消費端繼承的初始化器 `InstanceInitializer`，以及 Addressables 讀取器 `AddressablesLoader`（放進讀取處理器的讀取器欄位；照 key 載入、回報進度、可取消，內容要先建置並以 Use Existing Build 驗證）。用法與接線範例見 [loader-handler](../../.agents/skills/loader-handler.md)。設計與進度：[Addressables 套件計畫](../../Documentation/Addressables/plan.md)。

## 安裝

在專案的 `Packages/manifest.json` 加入：

```json
"com.yu5h1.addressables": "file:C:/Users/Yu5h1/Dev/VSProjects/Yu5h1Lib/Unity/UnityExtension/Packages/Addressables"
```

依賴 `com.yu5h1.common` 與 `com.unity.addressables`（最低 2.8.1，Unity 6000.3 隨附的版本）。
