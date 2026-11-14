# UxAcceptance 的 Config／Setting 主題檔案交換

> Task ID: ugui-settings-theme-exchange
>
> 延續 [自訂主題](UiSettingsCustomThemePlan.md)與[明暗偏好](UiSettingsThemePreferencePlan.md)。

## 系統階層與關係

```text
UxAcceptance Settings 案例
├─ Config／Setting 的 JSON 入口 → 選擇匯入、匯出與檔案路徑
│  ├─ UTF-8 JSON ↔ Web 與 Unity 共用的 UX Theme v1
│  └─ 已驗證的偏好 → .Theme 偏好鍵 → 現有 Theme、色票與 Binding
└─ ConfigStore → 設定欄位的草稿、保存與套用
```

## 交換行為

- 兩個面板從標題列的「JSON」入口打開同一個檔案操作區。可直接輸入檔案路徑，Unity Editor 也提供原生檔案選擇。關閉操作區後回到原面板的焦點。
- 匯入共用既有 Theme v1 嚴格解碼器，驗證版本、模式、明暗基底、八個 `#RRGGBB` 欄位及未知欄位。先驗證完整檔案，再保存並即時套用。讀取、格式或保存失敗時顯示提示，保留現有偏好和色票。
- 匯出目前的模式與八色資料。自訂資料在固定明暗或系統模式中仍隨檔案交付。使用同一個 v1 編碼器寫出 UTF-8 JSON，匯出本身只讀取目前主題狀態。建立新檔案時若路徑已有檔案，顯示提示並請使用者指定其他檔名。[UX Theme v1 schema](../../../UX/Documentation/ModuleSpec/Foundation/Configuration/theme-preference.schema.json)
- 成功匯入以檔案內容更新整筆偏好，系統模式重新讀取目前平台設定。兩個面板重新套色時保留原控件、分頁與設定草稿。

## 實作與驗收

1. 並行實作共用編解碼與偏好提交、UTF-8 檔案操作、兩個面板共用的操作區。案例接線與整合驗收沿用隔離的 Unity Editor。
2. 從 Web 既有公開解碼器產生一份 v1 檔案。PlayMode 由 Config 匯入，核對基底、八色、畫面和偏好，再從 Setting 匯出供 Web 解碼確認。以換過順序及大小寫的等價資料驗證內容互通。
3. 匯入未知版本、缺色、非法色值、損毀 JSON 與不存在的檔案，核對現有偏好原文和畫面。匯出前後及有效匯入期間檢查草稿、保存與套用次數，重跑現有主題與設定整合測試。
4. 檢查窄版的操作區和鍵盤焦點。通過後記錄 batchmode 結果及 Web 讀回結果，再提交本輪檔案。
