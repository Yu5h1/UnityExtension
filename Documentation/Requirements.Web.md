# Requirements — Web

- **目標專案（Target）**：Web 庫 `Yu5h1Lib.Web`，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Web`。
- **提出專案（Requester）**：UnityExtension，`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension`。
- **目前依賴方式**：無。UnityExtension 目前沒有引用 Web 庫的任何檔案或輸出；本檔是第一次建立依賴。
- **未結案需求數**：3。
- **提出日期**：2026-10-02。
- **順序**：先做 WEB-1（範圍已縮小為宣告既有產物的格式），配合 UnityExtension 以設定面板試點；試點通過後才做 WEB-2、WEB-3。
- **背景**：設計統一在 Web 庫，UI Toolkit 照 Web 庫的規格實作，色票由 Web 庫輸出後自動轉成 USS。
  完整設計見 [跨平台 UI 一致性](跨平台UI一致性.md)。

## UNITYEXTENSION-WEB-1 — 將產生的主題色票檔宣告為穩定契約

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-02 提出，2026-10-05 依現況縮小範圍） |
| **Requirement** | 把建置產生的 `dist/theme/theme.css` 宣告為給其他平台讀取的穩定契約：寫明它的位置、區塊與變數格式，以及改變格式時如何通知使用端。 |
| **Evidence** | Web 庫建置時（`scripts/build-ui.mjs` 的 `buildTheme`）已用 `resolveTheme` 產生 `dist/theme/theme.css`：`[data-ui-scheme="light"]`、`[data-ui-scheme="dark"]` 兩個區塊各列出完整的 `--ui-*` 變數，含 `--ui-hover`、`--ui-on-accent` 等推導色，檔頭有 `src/theme/index.ts` 與建置腳本的 SHA-256 指紋。內容已足夠 UnityExtension 產生 USS，不需要另做輸出；但目前它只是網頁發佈用的產物，文件沒有承諾其他平台可以依賴它的格式。 |
| **Acceptance** | Web 庫文件寫明：檔案位置；兩個 scheme 區塊的選擇器；只含 `--ui-*` 自訂屬性宣告；值的格式（目前為 6 位或 8 位十六進位色碼）；指紋的意義；以及新增、改名或移除變數、改變值格式時要在哪裡記錄。UnityExtension 依此實作的匯入工具，產生的 USS 變數值與該檔逐一相同。 |
| **Impact** | UnityExtension 的 UI Toolkit 元件；之後任何需要和 Web 庫同色的平台（WPF、原生 App）都能讀同一份檔案。 |
| **Workaround** | 直接讀現有的 `dist/theme/theme.css`。格式若在沒有通知的情況下改變，匯入工具會報錯，Unity 端暫時沿用上次產生的 USS。 |

## UNITYEXTENSION-WEB-2 — 聊天面板設計

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-02 提出；等 [試點](跨平台UI一致性.md#試點)通過後再開始） |
| **Requirement** | Web 庫擁有聊天面板（訊息列表與輸入列）的設計：寫出規格文件並提供 Lit 實作，之後 UnityExtension 以它為準。 |
| **Evidence** | 聊天面板的設計目前只存在 UnityExtension 的 [聊天面板模組](聊天面板模組.md)（已實作於 `com.yu5h1.uitoolkit`）。Web 庫沒有對應設計，但 HealthAI 的網頁 demo 已自行實作一份（`W:/UnityProject/HealthAI/docs/demo/HealthAI/js/conversation.js`），Web 庫的 [WebLibraryDirectionAnalysis](file:///C:/Users/Yu5h1/Dev/VSProjects/Yu5h1Lib/Web/Documentation/WebLibraryDirectionAnalysis.md) 也將該 demo 列為分析對象。HealthAI Unity 版對聊天模組的需求見其 `docs/unity/Requirements.UnityExtension.md` 的 HEALTHAI-UNITYEXTENSION-7–14。 |
| **Acceptance** | Web 庫有聊天面板的規格文件，涵蓋訊息排列與對齊、泡泡樣式、串流逐字顯示與思考中狀態、捲動跟隨、依位置淡出、輸入列（多行、送出、麥克風、上方／下方擴充列）；樣式只用 Web 庫的主題變數；有可操作的 demo。規格中與 UnityExtension 現行設計不同的地方逐項列出，讓 UnityExtension 回補。 |
| **Impact** | UnityExtension 的聊天模組、HealthAI 網頁 demo，以及之後需要聊天介面的 Web 使用端。 |
| **Workaround** | UnityExtension 維持現行設計；HealthAI 的聊天模組遷移照常進行，Web 規格完成後再回補差異。 |

## UNITYEXTENSION-WEB-3 — Apps 面板設計

| 欄位 | 內容 |
|---|---|
| **Status** | Outstanding（2026-10-02 提出；等 [試點](跨平台UI一致性.md#試點)通過後再開始，物理部分另等 Physics 試作） |
| **Requirement** | Web 庫擁有 Apps 面板（可拖曳、可甩動的桌面圖示與浮動 Apps 面板）的設計：寫出規格文件並提供 Lit 實作，之後 UnityExtension 以它為準。 |
| **Evidence** | 設計目前只存在 UnityExtension 的 `Packages/UIToolkit/Samples~/AppsMenu` 與 [指標互動與世界橋接](指標互動與世界橋接.md)、[運動系統](運動系統.md)。Web 庫沒有對應設計，但已有相關基礎元件（`src/ui/long-press.ts`、`reorder.ts`、`item-list.ts`）；HealthAI 網頁 demo 也自行實作了桌面（`desktop.js`，速度衰減、邊界反彈、拖放排序）。 |
| **Acceptance** | Web 庫有 Apps 面板的規格文件，涵蓋群組拖曳與慣性、避障、甩飛與邊緣反彈、長按 400ms 與吞掉該次點擊、丟入面板、抖動、入場錯開；樣式只用主題變數；有可操作的 demo。物理算式的歸屬依 Physics 專案的共用方式決定，規格只描述行為與參數，不綁定實作語言。 |
| **Impact** | UnityExtension 的 Apps Menu、HealthAI 網頁 demo 的桌面。 |
| **Workaround** | UnityExtension 維持現行 Apps Menu 範例。 |
