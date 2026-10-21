# Yu5h1Lib.Addressables

Unity Addressables 的擴充：依需求下載與載入建置好的內容、回報進度與狀態，載入時的非同步表演與初始化流程由消費端定義。

目前只有套件骨架。設計與進度：[Addressables 套件計畫](../../Documentation/Addressables/plan.md)。

## 安裝

在專案的 `Packages/manifest.json` 加入：

```json
"com.yu5h1.addressables": "file:C:/Users/Yu5h1/Dev/VSProjects/Yu5h1Lib/Unity/UnityExtension/Packages/Addressables"
```

依賴 `com.yu5h1.common` 與 `com.unity.addressables`（最低 2.8.1，Unity 6000.3 隨附的版本）。
