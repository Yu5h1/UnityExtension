# Agent 在 Unity 的工作效率：翻譯器與工具

> Task ID: agent-data-translator

狀態：第一層與第二層已實作（2026-10-22）：agent 作業說明、`Tools/agent/` 三個命令列工具、inline SO 設定匯出；第三層未開始。這是優化 agent 在 Unity 工作流程的唯一任務卡，依 CP 值分三層推進
（[發展步驟](#發展步驟依-cp-值)）。

## 目標

降低 agent 操作 Unity 的成本，讓 agent 在 Unity 專案裡能讀得準、查得快、驗得到，不必每次臨時寫腳本。
先做成本低、立即見效的工具；成效大但開發成本高的列為長期發展。

## 系統位置與關係

```text
agent
├─ 第一層：檔案層的命令列工具（不開 Unity）＋ agent 作業說明（skill）
│  ├─ GUID／資產索引與反查
│  ├─ batchmode 執行器
│  └─ 不開 Unity 的編譯檢查
├─ 第二層：翻譯器（com.yu5h1.common Editor，batchmode 可跑，只讀）
│  ├─ 用 Unity 自己的 SerializedObject 讀取並解析引用
│  ├─ UnityEvent 接線        ← 已由 UnityEvent監控 提供
│  └─ inline SO 設定匯出
└─ 第三層（長期）：一般物件匯出、建置時比對、寫回 Unity
反方向：agent 寫文字 → 同步成 Unity 物件，由 Agent friendly workflow／文字結構同步 擁有
```

相關：[UnityEvent監控](UnityEvent監控.md)（接線報告，翻譯器共用它的掃描核心）、
[Agent friendly workflow](Agent%20friendly%20workflow%20Yu5h1Lib.UnityExtension.md)（文字 → Unity 的方向）、
[plan〈發展方向〉](../plan.md#6-發展方向讓資料驅動對-agent-可讀)。

## 成本分析（2026-10-22）

agent 在這個工作區操作 Unity 時實際付出的成本，以及對應的工具：

| agent 的成本 | 證據 | 工具 | 價值／開發成本 |
|---|---|---|---|
| 不知道這個工作區怎麼操作 Unity：哪個專案開著、怎麼跑 batchmode、MCP 的坑、怎麼驗證 | VCP 每次 batchmode 約 1–2 分鐘、使用者同時開著 Yu5h1LibTest；MCP `refresh_unity` 第一次沒觸發編譯，要 force；6000.3 編譯檢查的指令散在 handoff 的歷史紀錄裡 | agent 作業說明（skill） | 高／最低 |
| GUID 對不回資產與類別、找不到誰引用了某個資產 | 2026-10-22 找 PopupPanel prefab 的 GUID 時，掃描 Unity 安裝目錄超過 120 秒逾時；script GUID 要同時掃 `Assets` 與 `file:` 套件路徑才能對回類別 | GUID／資產索引與反查 | 高／低 |
| 跑 Unity 的步驟每次都要重組：編輯器路徑、版本、log、逾時、專案是否已開 | VCP 的 `BuildReportDump`、`CatSetup` 都靠 batchmode；專案已被編輯器開著時不能再開 | batchmode 執行器 | 中高／低 |
| 驗證編譯要開 Unity，或臨時拼 dotnet 指令 | Core 在 6000.3 的錯誤（`FindObjectsByType` 多載、`AssetCreationEndAction`）是靠 6000.3 編譯檢查抓到的；UnityEvent 監控也臨時用 dotnet 對兩個版本的 Managed DLL 編譯 | 不開 Unity 的編譯檢查 | 高／中低 |
| 讀不懂場景與資產裡的組裝（fileID、GUID） | 見 [為什麼需要翻譯器](#為什麼需要翻譯器) | inline SO 設定匯出（翻譯器） | 高／中 |
| 看不懂一般物件的階層與元件、斷線要等人點開 Inspector 才發現、寫入只能靠腳本或 MCP | — | 一般物件匯出、建置時比對、寫回 | 高／高 |

## 發展步驟（依 CP 值）

### 第一層：成本低、立即見效

1. **agent 作業說明**：UnityExtension 的 `.agents/skills/` 新增一份 skill，寫下這個工作區操作 Unity 的方式：
   哪些專案用哪個 Unity 版本、一個專案同時只能被一個編輯器開、batchmode 的指令與 log 位置、照 VCP `CatSetup.cs`
   寫可重跑的 DryRun／Apply Editor 腳本、MCP 的已知問題（例如 `refresh_unity` 要 force 才編譯）、6000.3 與 6000.6 的編譯檢查指令。
   從 introduction 的 skill 清單連過去。給 agent 看的文件最優先（[plan〈設計原則〉](../plan.md#2-設計原則)）。
2. **GUID／資產索引與反查**：從 `.meta` 建立 GUID ↔ 路徑 ↔ 類別的對照，涵蓋 `Assets` 與 manifest 裡的 `file:` 套件；
   能反查哪些場景、Prefab、SO 引用了某個 GUID。只讀 `.meta` 的 guid 行與資產檔裡的 GUID 字串，不解析 YAML 結構。
3. **batchmode 執行器**：一個指令就能對指定專案跑 Editor 方法：從 `ProjectVersion.txt` 找到對應的編輯器、log 寫到固定位置、
   有逾時、回傳結束碼；專案已被編輯器開著（`Temp/UnityLockfile`）時直接拒絕並說明。
4. **不開 Unity 的編譯檢查**：對一個套件的 asmdef 原始碼，用 6000.3.9f1 與 6000.6.0f1 的 Managed DLL 和 Core DLL 編譯，
   回報錯誤與警告。先支援 `com.yu5h1.common`。

2～4 放在 UnityExtension 根目錄 `Tools/agent/`，用 Python 寫、只用標準函式庫（決策 `cli-tool-home`，2026-10-22）。
它們要在不開 Unity 的情況下執行，所以不能是 Unity 套件裡的 Editor 程式；需要 .NET 的編譯檢查由腳本呼叫 dotnet。

### 第二層：成本中等

5. **inline SO 設定匯出**（翻譯器）：見下方〈翻譯器〉。它也是 ChatRequest 優化的遷移安全網，所以排在 ChatRequest 優化之前
   （[非同步工作〈遷移〉](非同步工作.md#遷移)）。

### 第三層：長期發展

6. 擴充到一般物件：階層、元件與欄位（給 agent 理解 prefab 組裝，例如角色 prefab 的元件組成）。
7. 在建置流程中跑報告並和上次 diff，讓接線斷掉在建置時就被看到。
8. 測試 agent 能不能直接改翻譯器輸出；可行就成為 Agent friendly workflow 的格式候選，寫回由文字結構同步負責。

## 翻譯器

### 為什麼需要翻譯器

資料驅動的組裝存在場景與資產的 YAML 裡，引用寫成 fileID 與 GUID。程式碼 agent 可以直接讀，這些資料不行。

2026-10-22 分析 VCP ChatScene 的 `TaskEvent` 監聽器時，agent 臨時寫 Python 解析場景：把 script GUID 經 `.meta`
對回類別（要同時掃 `Assets` 與套件路徑）、把 fileID 對回 GameObject 名稱（inline SO 的 `m_GameObject` 是 0，要改讀 `m_Name`）、
處理 prefab instance 的 stripped 物件、靠縮排切出 Begin／End／True／False／Completed。結果出錯兩次，而且都沒有任何警告：

- 第一次只在 `lifeCycle` 附近找 `end:`，漏掉了 End 的 7 筆監聽器，錯誤的「End 0 筆」還寫進了文件與決策背景。
- 第二次用跨行的正規表示式，掃進了相鄰的 `_textRecived`、`_SpeakCallback` 事件，多算了好幾筆。

手寫的 YAML 解析每次都要重寫，錯了也不會報錯。這是資料驅動在 AI 時代要補的缺口。

### 翻譯器的目標

給 agent 一份可以直接讀的文字：場景、Prefab、SO 裡的物件、元件、inline SO 的設定與 UnityEvent 接線，
所有引用都解析成「名稱路徑＋型別」。agent 讀這份輸出就能分析，不用自己解析 YAML。

### 原則

- 用 Unity 的 `SerializedObject`／`SerializedProperty` 讀取，不手寫 YAML 解析。引用由 Unity 自己解析，不會漏抓，也不會掃到別的欄位。
- 只讀不寫。寫入方向由 Agent friendly workflow 擁有。
- 能在 batchmode 跑，不必開編輯器畫面；也能在編輯器裡用。
- 和 UnityEvent 監控共用掃描核心，不另寫一套收集程式。
- 輸出要穩定排序，同一份場景重跑結果相同，改動前後的輸出可以直接 diff。
- 輸出設計成將來能寫回（2026-10-22）：宣告式、內容完整到足以重建、身分穩定（名稱路徑，同名時加穩定 ID）、
  資產引用用可讀別名而不是 GUID。寫回方向由 [Agent friendly workflow〈先讀後寫〉](Agent%20friendly%20workflow%20Yu5h1Lib.UnityExtension.md#先讀後寫2026-10-22) 擁有。

### inline SO 設定匯出

每個 inline SO 一筆：所在物件的名稱路徑、欄位名稱、型別、名稱（例如 `RequestData` 的 command），以及它的序列化欄位：

- 一般欄位：值。
- 物件引用：名稱路徑與型別；引用到其他 inline SO 時，用它的名稱路徑標示。
- UnityEvent：每個事件通道（例如 `_callbacks.lifeCycle.end`）各自列出目標、方法、模式（動態或寫死）、寫死的參數值。

### 驗收

不寫任何臨時腳本，只讀工具輸出，重現 2026-10-22 的 ChatScene 分析（[非同步工作〈ChatScene 的監聽器分布〉](非同步工作.md#chatscene-的監聽器分布)）：

- 14 個請求物件與 `ChatRequest` 的全域回呼；
- 各事件的筆數：Begin 11＋2、End 5＋2、True 18、False 5＋1、Completed 6（2026-10-22 寫成 13＋2 與 6＋1 是手算重複加了全域回呼，翻譯器的輸出更正了它）；
- 哪些監聽器讀取動態 string（True 5 筆、False 4 筆）。

## 執行方式

每一步做成任務卡上的 agent 可執行項目（🤖），各自綁一份 checklist 當指示與證據；使用者核准計畫與決策，agent 依卡片執行，
使用者看 checklist 的結果驗收。依賴使用者選擇的步驟，先出決策題，答完才建立可執行項目。

## 決策理由與替代方案

- **翻譯器放在 `com.yu5h1.common` 的 Editor**（2026-10-22）。不另開 AgentTool 之類的套件：翻譯器只用 Unity Editor API，
  沒有必要的額外依賴；依 [plan〈設計原則〉](../plan.md#2-設計原則)，只有必要而且無法改成可選的依賴才另開套件，
  套件也以它擴充的 Unity 功能命名，「AgentTool」不是 Unity 的功能。和 UnityEvent 監控同處，才能共用掃描核心。

- **第一層命令列工具放在 `Tools/agent/`，用 Python（只用標準函式庫）**（決策 `cli-tool-home`，2026-10-22）：不用編譯、沒有新依賴，
  agent 已經在用；UnityExtension 根目錄不是 Unity 專案，專案只透過 `file:` 引用 `Packages/` 底下的套件，所以 `Tools/` 不會被匯入。
  其他選項（C# 主控台、PowerShell、全部做成 batchmode 的 Editor 方法）的取捨記在決策檔
  [agent-data-translator.decisions](../decisions/agent-data-translator.decisions)。
