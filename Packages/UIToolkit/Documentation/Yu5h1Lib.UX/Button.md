# Button 驗收紀錄（Yu5h1Lib.UIToolkit）

> Task ID: uitk-button
>
> 狀態：in-progress（2026-10-17，第二輪：Button 2.0.0）。依使用者指示「在 Yu5h1Lib.UIToolkit 引用 UX 的 Button」，流程見 referenced-module-acceptance。實作方式採「原生 Button + USS」（決策 64-66，uitk-button-native），移除自訂封裝元件 UxButton 與 ButtonDesign 常數。自動檢查涵蓋數值與規則邏輯（BTN-DISPLAY、BTN-SIZE、BTN-LAYOUT），外觀（solid/ghost 與狀態差異）交由使用者直接確認。

```text
UnityExtension
└─ Yu5h1Lib.UIToolkit（實作套件）
   └─ Button（本紀錄）
      ├─ 體驗規範 ← Yu5h1Lib.UX ModuleSpec/Controls/Button（BTN-DISPLAY、BTN-APPEARANCE、BTN-SIZE、BTN-LAYOUT）
      ├─ 規範引用、本輪未引用 ← 操作（INPUT-PRESS-TAP、INPUT-KEY-COMMAND、INPUT-NAME）、VisualFoundation、Icon
      └─ 驗證專案 → Yu5h1LibTest（Unity 6000.6），驗收時 manifest 暫時指向乾淨 worktree
```

## 身分

| 項目 | 內容 |
|---|---|
| 實作套件 | `com.yu5h1.uitoolkit`（`Packages/UIToolkit`）。採用原生 `UnityEngine.UIElements.Button` 與樣式表 `Yu5h1Button.uss` |
| UX 規範 | [UX ModuleSpec/Controls/Button.md](../../../../../../UX/Documentation/ModuleSpec/Controls/Button.md) 2.0.0，SHA-256 `9662a105972563ca4b546aac245aa5321761028b7aa96c6de7877f6ed77f2567` |
| 設計資料 | `Button/button-design.json` 3.0.0，SHA-256 `e2568af28f704603b2a0d696324f2389a81a685c5ad96049a38c4008cde5c0f9`：`size` 32、`iconRatio` 0.75、`iconLabelGap` 8 |
| UX commit | `8c1f764e22dc54960558c6bc81b364c31d3760b5` |
| 套件釘選 | 快照 `Tests/Editor/UX~/Button/`（`Button.md`、`button-design.json`，位元組與 UX 相同，`.gitattributes` 設 `-text`）；`ButtonDesignTests` 核對雜湊與數值 |
| 範圍 | BTN-DISPLAY、BTN-SIZE、BTN-LAYOUT 全部；BTN-APPEARANCE 依樣式提供 solid/ghost |
| 環境 | Windows 11、Unity 6000.6.0f1 Editor、PlayMode（runtime 面板只在 Game view 繪製時才解析排版） |

## 本輪未引用的部分（缺口）

| 規範內容 | 依賴 | 本輪處理 |
|---|---|---|
| BTN-APPEARANCE「顏色依 VisualFoundation 的角色」 | VisualFoundation | blocked：UIToolkit 沒有 VisualFoundation 色票；本輪顏色取 Unity 預設主題的按鈕變數 |
| BTN-DISPLAY「顯示設定不改變無障礙名稱」 | 操作 INPUT-NAME | blocked：UI Toolkit 元素沒有對應到 `UnityEngine.Accessibility` 的無障礙名稱 |
| 按下與鍵盤 | 操作 INPUT-PRESS-TAP、INPUT-KEY-COMMAND | 沿用 UI Toolkit 原生 Button，本輪不另寫程式 |
| 圖示的三種來源與著色 | Icon | blocked：原生 Button `iconImage` 支援 Texture2D、Sprite、RenderTexture、VectorImage |
| 外觀與滑過狀態確認 | — | 外觀交付使用者在 demo 中直接確認（決策 65），不再使用自動讀取 RenderTexture 像素之檢查 |

## 實作對照

| UX 行為 | UI Toolkit 實作 | 說明 |
|---|---|---|
| 公開入口 | `UnityEngine.UIElements.Button` + `Resources/Yu5h1Lib.UIToolkit/Yu5h1Button.uss` | 使用原生按鈕搭配樣式類別 `.yu5h1-button`，不再維護自訂 `UxButton` 類別 |
| BTN-DISPLAY | 原生 `iconImage` 與 `text`；`.yu5h1-button--icon-only` | 只顯示圖示時套用 `.yu5h1-button--icon-only` 設定 32×32 正方形；有名稱時高度為 32；無圖示或兩者關閉時備援顯示名稱 |
| BTN-SIZE | USS 百分比 `height: 75%`、`width: 75%` | 圖示一律 fit 按鈕：大小為 `size × 0.75`，不隨字型縮放（Button 2.0.0 決策 63） |
| BTN-LAYOUT | USS `flex-direction: row`、`align-items: center`、`margin-left: 8px` | 圖示在名稱前，水平間距 8px，兩者垂直置中 |
| BTN-APPEARANCE | `.yu5h1-button`（Solid）、`.yu5h1-button--ghost`（Ghost）、`.yu5h1-button--selected` | 實心預設底色；無底靜止透明，滑過/焦點/選中顯色 |

## 驗收對照

每項檢查的結果記在 [checklist](../../../../checklists/uitk-button.checklist)，本文不重複。

| UX 行為 | 檢查 | 結果所在 |
|---|---|---|
| 釘選 | 快照雜湊、合約版本與數值核對、乾淨 checkout 編譯 | checklist 第 1 項 |
| BTN-DISPLAY | 四種組合、名稱後備、只顯示圖示的正方形、有名稱時的高度 | checklist 第 2 項 |
| BTN-SIZE & BTN-LAYOUT | 各 size 下圖示 fit 按鈕（size × 0.75）、圖示在前、間距 8px、垂直置中 | checklist 第 3 項 |
| BTN-APPEARANCE | 使用者在範例中確認外觀（Solid/Ghost、Hover、Focus、Selected） | checklist 第 4 項 |
| 回歸 | UIToolkit 既有測試 | checklist 第 5 項 |

## 已知偏差

無。

## 輪次

| 規範版本 | UX commit | UnityExtension commit | checklist commit | 結論 |
|---|---|---|---|---|
| Button 1.1.0 | `d9a9790` | `9b53f9b`（範例 `9b2a2f6`） | `e70ad14` | 本輪範圍內相容：自動檢查全部通過，使用者確認外觀；缺口見第一輪紀錄 |
| Button 2.0.0 | `8c1f764` | （進行中） | （進行中） | 依原生 Button + USS 重新引用 |

## 結論

（待本輪執行與使用者確認完成後填寫）
