# Web 套件遷移

> Task ID: web-package-migration

> 狀態：**方向已定案，尚未搬遷**（2026-10-06）。`Packages/Web` 套件骨架已建立；`common` 的 Web 程式還在原處。
>
> 與其他文件的關係：本文件負責搬遷範圍、步驟與驗收；執行進度由 [handoff.md](../handoff.md) 擁有；WebGL 與 Yu5h1Lib.Web 前端庫之間的橋接方向由[跨平台 UI 一致性 § WebGL](跨平台UI一致性.md#webgl直接使用-web-庫)負責。

## 系統階層與關係

```text
UnityExtension
├─ com.yu5h1.library（Core DLL：SingletonBehaviour 等）
├─ com.yu5h1.common（Yu5h1Lib.Common：DataView 等）
│  └─ VersionChecker ── 留在 common
└─ com.yu5h1.web（Yu5h1Lib.Web，本套件）
   ├─ Runtime
   │  ├─ WebConfig ── 讀取 WebGL 頁面的全域 config（WebConfig.jslib）
   │  └─ WebInput ── 瀏覽器 keydown／keyup 轉成 Unity 事件（WebInputBridge.jslib）
   └─ Editor
      └─ IndexHtmlEditor ── 改寫建置輸出的 index.html

使用者：Virtual-Chat-Partner（WebConfig、WebInput）
```

依賴方向固定為 web → common → library。搬遷後 common 不得回頭引用 web。

## 摘要

把 `common` 裡只在 WebGL 有意義的程式搬到獨立的 `com.yu5h1.web`。所有專案都裝 common，但只有 WebGL 專案需要 jslib、`DllImport("__Internal")` 與 `WebGLInput` 設定。搬遷後 common 只留平台無關的程式，Web 平台能力有一個明確的歸屬；跨平台計畫裡的 jslib 橋接與模板打包工具日後也放在這裡。

## 問題與目標

- **問題**：Web 平台程式混在 common，非 WebGL 專案也一起背著；跨平台計畫預定的 Web 橋接沒有歸屬。
- **目標**：Web 平台程式集中到 `com.yu5h1.web`；common 不再含任何 Web 專屬程式；既有場景引用與 VCP 程式不需修改就能繼續運作。
- **非目標**：這次只搬位置，不改行為、不改 namespace。IndexHtmlEditor 的既有缺陷列在[已知問題](#已知問題)，另行處理；與 Yu5h1Lib.Web 前端庫的橋接依跨平台計畫的啟動條件進行。

## 套件設定

骨架已建立，內容如下：

| 項目 | 設定 | 理由 |
|---|---|---|
| 套件 | `com.yu5h1.web` 0.1.0，`unity` 2024.1 | 與 common 的最低版本一致 |
| 相依 | `com.yu5h1.common` 1.0.0 | `WebConfig` 使用 common 的 `DataView`；`SingletonBehaviour` 來自 Core，經 common 間接取得 |
| Runtime 組件 | `Yu5h1Lib.Web`，引用 `Yu5h1Lib.Common`，所有平台 | VCP 在 PC、Android 也會呼叫 `WebConfig.TryGetValue`（回傳 false）；`WebInput` 自己用前置處理器隔開 WebGL 分支 |
| Editor 組件 | `Yu5h1Lib.Web.Editor`，只在 Editor | 放 `IndexHtmlEditor` 與日後的模板工具 |
| Namespace | 保持 `Yu5h1Lib.WebSupport` | VCP 的 `using Yu5h1Lib.WebSupport;` 不用改；VCP 程式在 Assembly-CSharp，會自動引用新組件 |

所有 `.meta` 已隨骨架建立並固定 GUID，避免各專案第一次匯入時各自產生不同的 GUID。

## 搬遷清單

| common 來源 | Web 目的地 | GUID | 處理 |
|---|---|---|---|
| `Runtime/WebSupport/Scripts/WebConfig.cs` | `Runtime/WebConfig.cs` | `710e27ce…` | 原樣搬移 |
| `Runtime/WebSupport/Scripts/WebInput.cs` | `Runtime/WebInput.cs` | `8066e72c…` | 原樣搬移；VCP 場景以此 GUID 掛載元件 |
| `Runtime/WebSupport/Scripts/jslib/`（資料夾） | `Runtime/jslib/` | `81020716…` | 連同資料夾 meta 搬移 |
| `…/jslib/WebConfig.jslib` | `Runtime/jslib/WebConfig.jslib` | `64313bed…` | 原樣搬移，保留只啟用 WebGL 的匯入設定 |
| `…/jslib/WebInputBridge.jslib` | `Runtime/jslib/WebInputBridge.jslib` | `937a3bf8…` | 原樣搬移；meta 沒有平台設定，依 `.jslib` 預設只進 WebGL |
| `Editor/WebSupport/IndexHtmlEditor.cs` | `Editor/IndexHtmlEditor.cs` | `f0d8e342…` | 搬移；檔案是 Big5 編碼，見[尚未決定](#尚未決定) |
| `Runtime/Event/WebRequestEvent.cs` | — | — | 全檔都是註解，沒有編譯內容，見[尚未決定](#尚未決定) |
| `Runtime/Component/VersionChecker.cs` | 留在 common | — | 只讀 `Application.platform` 與版本；用 `absoluteURL` 判斷 WebGL 只是加一段標籤，沒有用到 WebGL API |

搬完後刪除變空的 `Runtime/WebSupport/`、`Runtime/WebSupport/Scripts/`、`Editor/WebSupport/` 及它們的 `.meta`。

## 頁面與場景契約

搬遷不改這些行為，但它們是使用這個套件的前提，搬遷後寫進套件 README：

- `WebConfig` 讀取頁面全域的 `config`（`JSON.stringify(window.config)`）。VCP 的 `Assets/WebGLTemplates/VCP/index.html` 以頂層 `var config` 宣告 Unity loader 設定，並在同一個物件加上 `TARGET_SERVER`、`PROXY_SERVER`。
- `WebInput` 以 `SendMessage(gameObject.name, …)` 接收按鍵，掛載它的 GameObject 名稱在場景中必須唯一。

## 使用者影響

| 專案 | 取得方式 | 用到的部分 | 需要的動作 |
|---|---|---|---|
| Virtual-Chat-Partner | `file:` | `Assets/Scripts/Request/ChatRequest.cs` 呼叫 `WebConfig`；`Assets/Scene/ChatScene.unity`、`vcp_AudioClient.unity` 與 `_Recovery/` 下的場景掛著 `WebInput` | 搬遷前在 `Packages/manifest.json` 加入 `com.yu5h1.web` |
| BonghuoVR、KFC、Yu5h1LibTest | `file:` | 無 | 無 |
| MotionGen | GitHub URL | 無 | 無；推送後可用 `?path=/Packages/Web` 取得 |

HealthAI 沒有引用 common，不受影響。

## 步驟

1. **建立套件骨架**（已完成，2026-10-06）：`package.json`、README、兩個 asmdef 與所有 `.meta`。
2. **VCP 先加入套件**：在 VCP 的 `Packages/manifest.json` 加入 `"com.yu5h1.web": "file:C:/Users/Yu5h1/Dev/VSProjects/Yu5h1Lib/Unity/UnityExtension/Packages/Web"`。此時套件還是空的，不影響現況。VCP 以 `file:` 直接讀工作目錄，若不先加，common 一搬走 VCP 就會編譯失敗。
3. **一次搬移**：在 UnityExtension 用 `git mv` 把搬遷清單的檔案連同 `.meta` 移到 Web，刪除空資料夾，放在同一個 commit。不要先複製再刪除：同一個專案裡出現兩份相同 GUID 時，Unity 會替其中一份重新指派 GUID，場景引用可能指到錯的那份。
4. **處理未決項目**：依[尚未決定](#尚未決定)的結果處理 `WebRequestEvent.cs` 與 `IndexHtmlEditor.cs` 的編碼。
5. **更新文件**：套件 README 補上[頁面與場景契約](#頁面與場景契約)；本文件狀態改為已實作。
6. **驗收**：依下一節。

## 驗收

- VCP 編譯無錯誤；`ChatScene` 與 `vcp_AudioClient` 的 WebInput GameObject 不是 Missing Script，`_keybindings`、`_mobileKeyboardSupport`、`_captureAllKeyboardInput` 的序列化值保留。
- Yu5h1LibTest 與 BonghuoVR 編譯無錯誤，確認 common 拿掉 Web 程式後沒有殘留引用。
- common 內搜尋不到 `WebSupport` 與 `DllImport("__Internal")`。
- VCP WebGL 建置成功（`GetConfig`、`RegisterKeyListeners` 有連結到）；以本機託管開啟後，`ChatRequest` 讀得到 `TARGET_SERVER`，焦點不在輸入框時按鍵能觸發 `WebInput` 的綁定。WebGL 建置耗時較長，是否執行由使用者決定。

## 決策理由與替代方案

- **保留 namespace**：改成 `Yu5h1Lib.Web` 會讓 VCP 跟著改程式，而這次的目標只是換位置。
- **Runtime 組件不限 WebGL 平台**：若只開 WebGL 與 Editor，VCP 的 PC、Android 版會因為 `ChatRequest` 引用 `WebConfig` 而編譯失敗。
- **不讓 common 相依 web 來維持相容**：這會反轉依賴方向，所有專案又重新背上 Web 程式，搬遷就失去意義。

## 已知問題

搬遷時保持原樣，各自在需要時處理：

- `IndexHtmlEditor.ExtensionMethod` 組出新的 html 後沒有寫回檔案，目前也沒有任何呼叫者；方法名稱是暫名。跨平台計畫預定延伸它來打包 Web 庫產物，屆時一併修正並命名。
- `WebInputBridge.jslib` 的 `UnregisterKeyListeners` 沒有 C# 端呼叫，`WebInput` 被銷毀後監聽仍留在頁面上。
- `JS_MobileKeyboard_SetTextSelection` 沒有 C# 端呼叫，名稱與 Unity WebGL 行動鍵盤的內部函式相同，推測是覆寫 Unity 內建行為；升級 Unity 時要確認仍相容。

## 尚未決定

1. **`WebRequestEvent.cs`**：全檔註解，內容是 VCP `ChatRequest` 時期的草稿。建議直接從 common 刪除、不搬進 Web，git 保留歷史。替代：原樣搬進 Web 保留草稿。
2. **`IndexHtmlEditor.cs` 的編碼**：目前是 Big5，在 UTF-8 下註解與 log 字串是亂碼。建議搬移時轉成 UTF-8、不改行為；寫回缺陷留到模板打包工作。替代：搬移時順便修正寫回。
3. **common 版本號**：common 移除公開型別是破壞性變更。建議維持 1.0.0：所有使用者都以 `file:` 或 GitHub URL 取得，不依版本號解析，UIToolkit 與 Web 也宣告相依 1.0.0。替代：升到 2.0.0 並同步更新各套件的相依宣告。
