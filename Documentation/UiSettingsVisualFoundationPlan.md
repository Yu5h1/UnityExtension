# Config 與 Setting 共用 VisualFoundation 色票實作計畫

> Task ID: ugui-settings-visual-foundation

## 系統階層與關係

```text
UX VisualFoundation 色彩資料 1.3.0
├─ Web Theme → CSS 變數
└─ Unity uGUI 匯入 → Light／Dark Theme
   ├─ 共用角色 → 既有 ColorObject 與 Theme Binding
   └─ UguiControlPalette → Config／Setting 與 UxAcceptance 範例
```

UX 擁有語意角色和明暗預設。Unity 以既有 Theme、SO Parameter 和 Binding 套用角色。既有場景的 Theme、參數名稱、引用、Sprite 及佈局 Preset 都延續原有接線。

## 實作順序

1. 釘選 Web 現用的 UX `visual-design.json`。規範版本為 3.1.0，色彩資料版本為 1.3.0，來源 commit 為 `8f411275c0f0df115309d5a6dec670a82f09ee1c`，SHA-256 為 `ffcfb60703205089157835e6c15348798c43eb4b59b2df4a488b20a4b7e73250`。Unity 套件保留位元組相同的資料。Unity Editor 匯入兩個使用既有 `Theme` 類型的色票，每個角色是同名 `ColorObject`，含 alpha 的色值保留 RGBA。對既有專案可由各自的 Theme 和 Binding 繼續使用現有名稱，採用共用角色時明確對應。
2. 由 `UguiControlPalette` 讀取匯入的 Theme，維持既有 Light／Dark API 及按鈕狀態接線。Config／Setting 明確使用頁面、面板、標題、文字、遮罩和控制狀態的角色。兩個面板沿用同一組 palette。保留現有偏好格式及保存流程。
3. UxAcceptance 允許在建立 Settings 範例時指定亮或暗，維持原有預設。解除範例方向按鈕對預設灰色的依賴。截圖透過正式匯入與套用入口；畫布使用離屏 440×720、1:1。
4. 並行分工為匯入與來源驗證、uGUI 色彩接線、樣本及截圖驅動。合併後由驗收方檢查匯入、既有 Binding、Settings 行為，以及兩種面板乘以兩種方向和兩種主題的畫面。驗收通過後一併提交。

## 驗收條件

- 比對 Unity 內的來源檔與 UX、Web 的來源雜湊；比對全部 17 個明暗角色的 sRGB RGBA，包括遮罩的 alpha。測試走真正的匯入資產和既有 Binding。
- 既有按鈕、分頁、群組及保存回歸測試通過。截圖的八種組合保留原有可用操作，標題、文字、分頁提示與遮罩使用對應角色。方向切換按鈕在明暗圖中皆可辨識。
- 本輪涵蓋預設明暗色票與 Config／Setting。自訂色、模式存檔交換、執行中主題切換及 UI Toolkit 升級另有各自的採用範圍。
