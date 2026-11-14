# UxAcceptance 的 Config／Setting 自訂主題

> Task ID: ugui-settings-custom-theme
>
> 延續 [明暗偏好](UiSettingsThemePreferencePlan.md)與[跟隨系統](UiSettingsSystemThemePlan.md)。

## 系統階層與關係

```text
UxAcceptance Settings 案例
├─ Config／Setting 標題列 → 選擇與編輯共用的自訂配色
│  ├─ 八個 RGB 色值與明暗基底 → 現有 .Theme 偏好鍵（UX Theme v1）
│  └─ 匯入的基底 Theme → 執行時 Theme 與 ColorObject → 現有 Binding／面板控件
└─ ConfigStore → 一般設定草稿、保存與套用
```

## 行為

- 兩個面板的「自訂」使用同一組八色。初次選擇時，以目前有效的亮色或暗色建立預設配色。再次選擇時還原上次的基底與八色。重開案例先套用存檔，使用者可再按「自訂」打開編輯欄位。
- 編輯使用 uGUI 的十六進位輸入欄位，每筆完整的 `#RRGGBB` 即時套色並以小寫保存。尚未完成或含無效字元的輸入在編輯時顯示提示，離開欄位後回到上一個有效值。明暗基底可在編輯區切換，保留八個已設定的色值。低對比提醒依規範順序顯示，仍可套用。
- `custom` 內包含 `base`、`pageBackground`、`panelBackground`、`heading`、`panelHeading`、`itemText`、`secondaryText`、`border`、`accent`。固定明暗或跟隨系統時保留原有 `custom`。未知版本與無效記錄保留原文。[Theme v1 schema](../../../UX/Documentation/ModuleSpec/Foundation/Configuration/theme-preference.schema.json)
- 自訂 Theme 在案例掛載期間持有。依基底匯入的 Theme 保留固定角色，依 VisualFoundation 的色彩推導（VIS-DERIVE）重新計算焦點、停用及強調前景。色值透過現有 Binding 和面板套色套用到同一批控件。模式切換與編輯均沿用獨立的主題保存鍵。[VisualFoundation](../../../UX/Documentation/ModuleSpec/VisualFoundation.md#行為編號)

## 實作與驗收

1. 並行實作 v1 偏好讀寫、執行時色票與色彩編輯區。整合由案例接線，重用原有標題列、主題切換、系統監看和 ConfigStore。
2. 透過 Config 選自訂，逐欄編輯八色並驗證即時呈現、合法格式、錯誤輸入及對比提醒。驗證原視窗、分頁與設定草稿，並檢查一般設定的保存與套用次數。
3. 透過 Setting 檢查兩個面板共用的八色，切換到其他模式再切回，確認基底與色值都有保留。以獨立的 Unity batchmode 啟動驗證重開首畫面。
4. 重跑原有明暗／系統、草稿和窄版畫面驗收。全部通過後記錄證據並提交。
