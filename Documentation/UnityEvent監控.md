# UnityEvent 監控

> Task ID: unityevent-monitor

> 狀態：方向已定案（只做靜態檢查，2026-10-20）；實作中
>
> 與其他文件的關係：本文件負責設計；進度與待決題目指標在 [handoff.md](../handoff.md)；
> UnityEvent 的參數擴充技術在 [unity-event-extensions](../.agents/skills/unity-event-extensions.md)。

## 系統階層與關係

```text
com.yu5h1.common（Editor：Yu5h1Lib.Common.Editor）
└─ UnityEvent 監控
   ├─ 掃描：從場景、Prefab、ScriptableObject 收集每一筆 persistent call
   ├─ 驗證：用 Unity 自己的方法比對規則判斷每一筆是否有效
   ├─ 檢視視窗（UnityEvent Viewer）：表格、篩選、分組、跳到物件
   └─ 命令列報告：batchmode 輸出 JSON，不開編輯器畫面
        └─ 依賴：UnityEngine.Events 的公開 API（UnityEventBase.GetValidMethodInfo）
```

掃描與驗證是核心，不知道視窗或命令列的存在；兩個前端都只讀核心產出的紀錄。

## 摘要

UnityEvent 讓接線變成資料：誰呼叫誰寫在場景與資產裡，不在程式碼裡。重新命名方法、搬命名空間、
刪除元件時，編譯器不會報錯，壞掉的接線只在有人剛好點開那個 Inspector 時才看得到。這個工具把
一個場景或整個專案的接線列成一張表，標出壞掉的，並能從方法或物件反查是誰在呼叫。

## 問題與目標

實例（2026-10-20，VCP `ChatScene`）：216 筆 persistent call 中有 23 筆失效，分成五種原因，
平常完全看不出來：

| 原因 | 筆數 | 例子 |
|---|---|---|
| 方法已從類別刪除 | 7 | 使用者資料的載入／更新／存檔方法在 2026-04 重構時移除 |
| 方法所在的基底類別被換掉 | 3 | 輸入框不再有「啟用互動」與「顯示並聚焦」 |
| 方法參數型別改了 | 2 | 輸入框的設值方法改收資料埠介面，Inspector 無法再指定物件 |
| 存下來的參數型別名稱過期 | 8 | 商店按鈕的換頁參數仍寫舊型別名稱 `Page`，類別已搬到 `VCP` 命名空間 |
| 目標物件是空的 | 3 | 換行按鈕、主頁載入、對話失敗彈窗 |

目標：

- 一次看到一個範圍內所有接線與它們的狀態，失效的排在前面。
- 判斷規則和 Unity 執行時一致：工具說有效，執行時就能呼叫到。
- 能反查：選一個物件或方法，列出所有指向它的接線。
- 不開編輯器畫面也能跑（batchmode），結果可以存檔比對。

非目標：

- 取代 UnityEvent 或改變它的序列化格式。
- 圖形化節點編輯接線。

## 責任邊界

擁有：收集 persistent call、判斷有效性、呈現與匯出結果。

不擁有：UnityEvent 的參數擴充（ParameterObject、ArgumentInfo 屬
[unity-event-extensions](../.agents/skills/unity-event-extensions.md)）、遺失腳本偵測（既有的
`FindMissingScriptsWindow`）。

## 設計

### 一筆紀錄

每筆 persistent call 產生一筆紀錄：

- 位置：所屬資產（場景或資產路徑）、擁有者物件與它的階層路徑、事件欄位的序列化路徑、第幾筆。
- 內容：目標物件、方法名稱、呼叫模式（無參數、物件、整數、浮點、字串、布林、事件傳入）、靜態參數、
  呼叫狀態（關閉、僅編輯器、執行時）。
- 狀態：有效、目標為空、目標遺失、找不到方法、參數不符、型別名稱過期（仍可呼叫，但目標一旦遺失就無法顯示）。

### 收集來源

| 範圍 | 怎麼讀 |
|---|---|
| 已開啟的場景、Prefab 編輯模式 | 走每個元件的序列化資料，找出事件欄位（含巢狀在一般類別、清單裡的事件） |
| Prefab、ScriptableObject 資產 | 載入資產後同上，不打開任何畫面 |
| 未開啟的場景 | 只在命令列模式處理：batchmode 沒有使用者的階層視窗，直接開啟場景掃描 |

場景中 Prefab 實例的覆寫（只改了某一筆接線）也要列出來，並標出來源 Prefab。

### 驗證規則

用 Unity 執行時使用的同一個公開方法 `UnityEventBase.GetValidMethodInfo` 判斷，不自己重寫比對規則：

- 型別取目標物件的實際型別；目標為空時才退回存下來的型別名稱。
- 物件模式的參數型別依存下來的名稱解析，解析不到就當成 `UnityEngine.Object`——這正是
  「型別名稱過期」會讓方法找不到的原因。
- 事件傳入模式的參數型別取自事件欄位本身的泛型參數。

### 檢視視窗

選單 `Tools/UnityEvent Viewer`。用 Unity 原生的多欄表格（UI Toolkit `MultiColumnTreeView`）：

- 分組：依資產 → 擁有者物件，或依失效原因。
- 篩選：只看失效、文字搜尋（物件、方法、型別）。
- 點一列：選取並 Ping 擁有者。
- 擁有者與目標只顯示最後一層（`.../ChatRequest (ChatRequest)`），完整路徑在提示與複製內容裡。
- 點一格複製該欄的完整內容；Ctrl+C 複製選取列的整行（Tab 分隔，多選時一列一行）。
- 表格有格線。
- 反查：在 Hierarchy 或 Project 選物件後切到「指向此物件」；輸入方法名稱列出所有呼叫它的接線。

### 程式位置

`Packages/common/Editor/Tool/UnityEventMonitor/`，命名空間 `Yu5h1Lib.EditorExtension`：
`UnityEventScanner`（收集與驗證）、`UnityEventRecord`（一筆紀錄與狀態）、`UnityEventViewer`（視窗）、
`UnityEventReport`（batchmode 進入點與 JSON 匯出，視窗的匯出也走它）。

### 命令列報告

batchmode 進入點輸出 JSON（每筆一個物件，欄位同「一筆紀錄」），結束碼表示是否有失效。
用途：不開畫面檢查、交付前檢查、比較兩次掃描。

## 案例

- VCP `ChatScene`：上表 23 筆都要被列出，且分類與上表一致。2026-10-20 batchmode 結果：226 筆、24 筆失效（多出的 1 筆在 Prefab 內部，原型沒掃到），通過。
- 第二個案例（抽象檢驗）：同一套規則掃 `Yu5h1LibTest` 中使用 ParameterObject 參數的接線，
  ParameterObject 走物件模式，不應需要任何特例。

## 決策理由與替代方案

- **表格而非節點圖。** 現有工具有兩派：節點圖（UnityEventVisualizer、unity-event-graph、
  EventVisualizer）與清單（UnityEventTracker）。一個場景 200 多筆接線，節點圖會糊成一團，
  而且「找出壞掉的」是篩選問題，不是拓樸問題。
- **用 Unity 的比對方法，不自己寫規則。** 2026-10-20 的原型用外部反射自寫規則，前兩輪都誤判
  （用存下來的型別名稱而非目標實際型別，以及泛型型別名稱的切割），換成實際型別後才和 Unity 一致。
- **未開啟的場景只在命令列處理。** 在互動編輯器裡額外開場景會改動使用者的階層視窗；解析場景
  YAML 雖然可行，但要自己重做型別解析，正是上一條的誤判來源。
- **不直接採用 UnityEventTracker。** 它整個專案常駐掃描並維護自己的資料庫；Yu5h1Lib 需要的是
  按需掃描、能跑 batchmode、與既有 Editor 工具同一套樣式。修復功能（全專案換方法）是它的長處，
  本工具是否要提供修復見下節。

- **只做靜態檢查（使用者 2026-10-20 決定）。** 執行時追蹤（記錄事件觸發順序）需要用反射替換
  UnityEvent 內部呼叫清單，Unity 改版就可能失效；這次的問題全部靜態可見。等有具體的除錯場景再開新題。

## 驗收與遷移

- VCP `ChatScene` 的 23 筆失效全部列出，分類正確；不多報。
- 命令列模式在不開畫面的情況下跑完，輸出的 JSON 與視窗內容一致。
- 點選失效列能選取並 Ping 正確的擁有者物件。
- 新增工具不改任何既有序列化資料，沒有遷移。

## 尚未決定

- 是否提供修復操作（例如把過期的型別名稱更新為目前型別、批次換方法）：第一版完成並在 VCP 驗收後再出題。
