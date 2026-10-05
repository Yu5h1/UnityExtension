# Yu5h1Lib.Web

Unity 端的 Web 平台能力：WebGL 頁面設定讀取、瀏覽器鍵盤橋接與 WebGL 模板工具。

目前只有套件骨架；`common` 的 WebSupport 程式依 [Web 套件遷移計畫](../../Documentation/Web套件遷移.md) 搬入。

## 安裝

在專案的 `Packages/manifest.json` 加入：

```json
"com.yu5h1.web": "file:C:/Users/Yu5h1/Dev/VSProjects/Yu5h1Lib/Unity/UnityExtension/Packages/Web"
```

依賴 `com.yu5h1.common`。
