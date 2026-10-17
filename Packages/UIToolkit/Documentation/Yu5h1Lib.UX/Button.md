# Button 驗收紀錄（Yu5h1Lib.UIToolkit）

> Task ID: uitk-button
>
> 狀態：planned（2026-10-17，第一輪：Button 1.1.0）。依使用者指示「在 Yu5h1Lib.UIToolkit 引用 UX 的 Button」，流程見 referenced-module-acceptance。範圍依決策 `uitk-button-scope`（button-first）：只引用 Button，依賴部分記為缺口。檢查都還沒執行。

```text
UnityExtension
└─ Yu5h1Lib.UIToolkit（實作套件）
   └─ Button（本紀錄）
      ├─ 體驗規範 ← Yu5h1Lib.UX ModuleSpec/Controls/Button（BTN-DISPLAY、BTN-APPEARANCE、BTN-SIZE、BTN-LAYOUT）
      ├─ 規範引用、本輪未引用 ← 操作（INPUT-PRESS-TAP、INPUT-KEY-COMMAND、INPUT-NAME）、VisualFoundation、Icon
      └─ 驗證專案 → Yu5h1LibTest（Unity 6000.6），驗收時 manifest 暫時指向乾淨 worktree（決策 `uitk-clean-checkout-host`）
```

## 身分

| 項目 | 內容 |
|---|---|
| 實作套件 | `com.yu5h1.uitoolkit`（`Packages/UIToolkit`）。盤點時（UnityExtension `6cee41f`）沒有任何按鈕包裝、沒有 USS／UXML |
| UX 規範 | [UX ModuleSpec/Controls/Button.md](../../../../../../UX/Documentation/ModuleSpec/Controls/Button.md) 1.1.0，SHA-256 `3633818b362a902fbd53de13188b7757bf01c9db52339d332697f304b25e71c3` |
| 設計資料 | `Button/button-design.json` 2.0.0，SHA-256 `8082bb56a9eb493e9c9d338527659efa389d8b01817a72f4aec8b535e2974075`：`size` 32、`iconRatio` 0.75、`iconWithLabelSize` 18（名稱 14px）、`iconLabelGap` 8 |
| UX commit | `d9a9790`（Button 檔案自 `1e7152e` 起未變；雜湊與 UX `manifest.json` 一致） |
| 套件釘選 | 待實作：快照與雜湊放在套件內，C# 常數由測試核對與快照一致 |
| 範圍 | BTN-DISPLAY、BTN-SIZE、BTN-LAYOUT 全部；BTN-APPEARANCE 的結構（實心有底、無底沒有底、狀態看得出差異） |
| 環境 | Windows 11、Unity 6000.6.0f1 Editor、PlayMode（runtime 面板只在 Game view 繪製時才解析排版） |

## 本輪未引用的部分（缺口）

| 規範內容 | 依賴 | 本輪處理 |
|---|---|---|
| BTN-APPEARANCE「顏色依 VisualFoundation 的角色」 | VisualFoundation | blocked：UIToolkit 沒有 VisualFoundation 色票；本輪顏色取 Unity 預設主題的按鈕變數 |
| BTN-DISPLAY「顯示設定不改變無障礙名稱」 | 操作 INPUT-NAME | blocked：UI Toolkit 元素沒有對應到 `UnityEngine.Accessibility` 的無障礙名稱 |
| 按下與鍵盤 | 操作 INPUT-PRESS-TAP、INPUT-KEY-COMMAND | 沿用 UI Toolkit 原生 Button，本輪不驗 |
| 圖示的三種來源與著色 | Icon | blocked：本輪只驗按鈕能放入宿主提供的圖示（`Background`：Texture2D、Sprite、VectorImage） |
| 滑過狀態 | — | 測試無法可靠模擬指標滑過（同 `ChatPresenterScrollTests` 的限制），由使用者直接確認 |

規範側缺口：Button 1.1.0〈目標 1〉把 UnityExtension 列為範圍外，BTN-SIZE、BTN-LAYOUT 沒有目標。依使用者 2026-10-17，屬 UX report `ux-goal-consumer-evidence` 的待討論項，不阻擋本輪。

## 實作對照

待實作。

## 驗收對照

每項檢查的結果記在 [checklist](../../../../checklists/uitk-button.checklist)，本文不重複。

| UX 行為 | 檢查 | 結果所在 |
|---|---|---|
| 釘選 | 快照雜湊、常數與快照一致、乾淨 checkout 編譯 | checklist 第 1 項 |
| BTN-DISPLAY | 四種組合、名稱後備、只顯示圖示的正方形、有名稱時的高度 | checklist 第 2 項 |
| BTN-SIZE | `size` 32／40／24 下的圖示邊長，名稱旁圖示 | checklist 第 3 項 |
| BTN-LAYOUT | 圖示在前、間距、垂直置中 | checklist 第 4 項 |
| BTN-APPEARANCE（結構） | 實心與無底的靜止底色、無底在焦點與選中時的差異（量畫面像素） | checklist 第 5 項 |
| 回歸 | UIToolkit 既有測試 | checklist 第 6 項 |

## 已知偏差

無。

## 輪次

| 規範版本 | UX commit | UnityExtension commit | checklist commit | 結論 |
|---|---|---|---|---|
| Button 1.1.0 | `d9a9790` | — | 與本列同一提交（planned） | 未執行 |

## 結論

尚未驗收。
