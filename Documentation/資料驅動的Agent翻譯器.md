# 資料驅動的 Agent 翻譯器

> Task ID: agent-data-translator

狀態：計畫（2026-10-22），尚未實作。位置：`com.yu5h1.common` 的 Editor（`Yu5h1Lib.Common.Editor`），和 UnityEvent 監控同處。

## 系統位置與關係

```text
Unity 專案的場景、Prefab、ScriptableObject（YAML：fileID、GUID）
└─ 翻譯器（com.yu5h1.common Editor，batchmode 可跑，只讀）
   ├─ 用 Unity 自己的 SerializedObject 讀取並解析引用
   ├─ 輸出 agent 可直接讀的文字（JSON／Markdown）
   │  ├─ UnityEvent 接線        ← 已由 UnityEvent監控 提供
   │  └─ inline SO 設定匯出     ← 第一批新增
   └─ 讀者：agent（分析、遷移、驗收）與人（審查 diff）
反方向：agent 寫文字 → 同步成 Unity 物件，由 Agent friendly workflow／文字結構同步 擁有
```

相關：[UnityEvent監控](UnityEvent監控.md)（接線報告，本工具共用它的掃描核心）、
[Agent friendly workflow](Agent%20friendly%20workflow%20Yu5h1Lib.UnityExtension.md)（文字 → Unity 的方向）、
[plan〈發展方向〉](../plan.md#6-發展方向讓資料驅動對-agent-可讀)。

## 為什麼需要

資料驅動的組裝存在場景與資產的 YAML 裡，引用寫成 fileID 與 GUID。程式碼 agent 可以直接讀，這些資料不行。

2026-10-22 分析 VCP ChatScene 的 `TaskEvent` 監聽器時，agent 臨時寫 Python 解析場景：把 script GUID 經 `.meta`
對回類別（要同時掃 `Assets` 與套件路徑）、把 fileID 對回 GameObject 名稱（inline SO 的 `m_GameObject` 是 0，要改讀 `m_Name`）、
處理 prefab instance 的 stripped 物件、靠縮排切出 Begin／End／True／False／Completed。結果出錯兩次，而且都沒有任何警告：

- 第一次只在 `lifeCycle` 附近找 `end:`，漏掉了 End 的 7 筆監聽器，錯誤的「End 0 筆」還寫進了文件與決策背景。
- 第二次用跨行的正規表示式，掃進了相鄰的 `_textRecived`、`_SpeakCallback` 事件，多算了好幾筆。

手寫的 YAML 解析每次都要重寫，錯了也不會報錯。這是資料驅動在 AI 時代要補的缺口。

## 目標

給 agent 一份可以直接讀的文字：場景、Prefab、SO 裡的物件、元件、inline SO 的設定與 UnityEvent 接線，
所有引用都解析成「名稱路徑＋型別」。agent 讀這份輸出就能分析，不用自己解析 YAML。

## 原則

- 用 Unity 的 `SerializedObject`／`SerializedProperty` 讀取，不手寫 YAML 解析。引用由 Unity 自己解析，不會漏抓，也不會掃到別的欄位。
- 只讀不寫。寫入方向由 Agent friendly workflow 擁有。
- 能在 batchmode 跑，不必開編輯器畫面；也能在編輯器裡用。
- 和 UnityEvent 監控共用掃描核心，不另寫一套收集程式。
- 輸出要穩定排序，同一份場景重跑結果相同，改動前後的輸出可以直接 diff。

## 第一批：inline SO 設定匯出

每個 inline SO 一筆：所在物件的名稱路徑、欄位名稱、型別、名稱（例如 `RequestData` 的 command），以及它的序列化欄位：

- 一般欄位：值。
- 物件引用：名稱路徑與型別；引用到其他 inline SO 時，用它的名稱路徑標示。
- UnityEvent：每個事件通道（例如 `_callbacks.lifeCycle.end`）各自列出目標、方法、模式（動態或寫死）、寫死的參數值。

## 驗收

不寫任何臨時腳本，只讀工具輸出，重現 2026-10-22 的 ChatScene 分析（[非同步工作〈ChatScene 的監聽器分布〉](非同步工作.md#chatscene-的監聽器分布)）：

- 14 個請求物件與 `ChatRequest` 的全域回呼；
- 各事件的筆數：Begin 13＋2、End 5＋2、True 18、False 6＋1、Completed 6；
- 哪些監聽器讀取動態 string（True 5 筆、False 4 筆）。

## 之後

- 擴充到一般物件：階層、元件與欄位（給 agent 理解 prefab 組裝，例如角色 prefab 的元件組成）。
- 在建置流程中跑報告並和上次 diff，讓接線斷掉在建置時就被看到。

## 決策理由與替代方案

- **放在 `com.yu5h1.common` 的 Editor**（2026-10-22）。不另開 AgentTool 之類的套件：翻譯器只用 Unity Editor API，
  沒有必要的額外依賴；依 [plan〈設計原則〉](../plan.md#2-設計原則)，只有必要而且無法改成可選的依賴才另開套件，
  套件也以它擴充的 Unity 功能命名，「AgentTool」不是 Unity 的功能。和 UnityEvent 監控同處，才能共用掃描核心。
