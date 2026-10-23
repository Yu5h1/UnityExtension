# Agent 在 Unity 的工作效率：翻譯器與工具

> Task ID: agent-data-translator

這是優化 agent 在 Unity 工作流程的唯一任務卡，依 CP 值分三層推進（[發展步驟](#發展步驟依-cp-值)）；
各步的進度看 UnityExtension Report 的這張卡。

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
   有逾時、回傳結束碼；專案已被編輯器開著（編輯器鎖住 `Temp/UnityLockfile`）時直接拒絕並說明；
   沒有編輯器鎖住的殘留鎖檔（batchmode 因編譯錯誤結束時會留下）則刪除、印一行說明後繼續。
4. **不開 Unity 的編譯檢查**：對一個套件的 asmdef 原始碼，用 6000.3.9f1 與 6000.6.0f1 的 Managed DLL 和 Core DLL 編譯，
   回報錯誤與警告。先支援 `com.yu5h1.common`。

2～4 放在 UnityExtension 根目錄 `Tools/agent/`，用 Python 寫、只用標準函式庫（決策 `cli-tool-home`，2026-10-22）。
它們要在不開 Unity 的情況下執行，所以不能是 Unity 套件裡的 Editor 程式；需要 .NET 的編譯檢查由腳本呼叫 dotnet。

### 第二層：成本中等

5. **inline SO 設定匯出**（翻譯器）：見下方〈翻譯器〉。它也是 ChatRequest 優化的遷移安全網，所以排在 ChatRequest 優化之前
   （[非同步工作〈遷移〉](非同步工作.md#遷移)）。

### 第三層：長期發展

6. 擴充到一般物件：階層、元件與欄位（給 agent 理解 prefab 組裝，例如角色 prefab 的元件組成）。
   設計見[一般物件匯出](#一般物件匯出)。
7. 在建置流程中跑報告並和上次 diff，讓接線斷掉在建置時就被看到。設計見[建置時比對](#建置時比對)。
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

## 一般物件匯出與建置比對

第三層第 6、7 步的設計（2026-10-23）。和 inline SO 設定匯出是同一個翻譯器：用 `SerializedObject` 讀、能在 batchmode 跑、
只讀、和 UnityEvent 監控共用掃描核心，輸出遵守〈[原則](#原則)〉。

### 一般物件匯出

對場景、Prefab 與 ScriptableObject 資產跑一次，列出每個 GameObject 的階層、元件與欄位，給 agent 理解組裝，
例如角色 prefab 的元件組成。

- 每個 GameObject 一筆：名稱路徑、是否啟用、Tag、Layer；Prefab 實例的根另記來源 Prefab 的資產路徑。
- 名稱路徑在同一個父物件下同名時，第二個起加 `#2`、`#3`；同一物件上重複的元件型別也一樣。這套路徑由掃描核心提供，
  UnityEvent 報告與 inline SO 匯出也一起改用，三者指向同一個物件時寫法相同。
- 元件依物件上的順序列出：型別、識別（型別名稱，重複時加 `#n`）、是否啟用、每個序列化欄位的值、每個 UnityEvent 通道的監聽器，
  欄位與監聽器的寫法和 inline SO 匯出相同。Transform 的父子欄位不列，階層已由名稱路徑表達。
- 浮點數一律寫完整精度，向量、四元數、顏色、Rect、Bounds 也一樣（Unity 預設的文字只留兩位小數，無法重建原值）；
  inline SO 匯出共用同一套寫法，一起改。
- 斷線分開標示：腳本遺失的元件列為 missing script；引用存在但物件已載不到（Inspector 顯示 Missing）的欄位寫 `missing`，
  沒指定的寫 `null`。
- ScriptableObject 資產列出它自己的欄位與監聽器；inline SO 一併列出，內容同 inline SO 匯出。

寫回前要補的寫法（2026-10-24，[改輸出的測試](#結果2026-10-24)找到的缺口）。三份輸出共用，掃描核心只在一處實作：

- **引用**依被引用的物件寫成：
  - 場景物件：`<名稱路徑> <型別>`，元件是 `<名稱路徑> (<元件識別>) <型別>`；
  - 資產檔裡的 GameObject 與元件（Prefab、模型）：`<資產路徑> > <名稱路徑> [(<元件識別>)] <型別>`；
  - 主資產：`<資產路徑> <型別>`；
  - 其他子資產：`<資產路徑> > <名稱> <型別>`，同一資產裡同名、型別名稱也相同的子資產依 local file ID 排序，第二個起加 `#2`、`#3`；
  - inline SO：`inline: <holder> > <field> <型別>`。
- **名稱裡的** `\`、`/`、`#` 寫成 `\\`、`\/`、`\#`，名稱路徑與 `#n` 才不會被名稱本身混淆。
- **監聽器的參數**：Object 模式用上面的引用寫法，沒指定寫 `null`；Float 模式寫完整精度。每筆監聽器另寫呼叫狀態
  `callState`（`Off`、`EditorAndRuntime`、`RuntimeOnly`）。UnityEvent 報告的參數用同一套寫法。
- **Prefab 覆寫**：實際值照舊全列，另外標出哪些來自覆寫。
  - 每個 GameObject 與元件有 `added`：它是 Prefab 實例裡新增的（Variant 裡新增的、加進實例的巢狀 Prefab 實例也算），
    新增物件底下的物件與元件也標 `added`。
  - 每個 GameObject 與元件有 `overrides`：被覆寫的欄位路徑，寫法和 `fields` 的 `path` 相同；
    GameObject 自己的欄位是 `m_Name`、`m_IsActive`、`m_TagString`、`m_Layer`；元件的 `enabled` 寫 `m_Enabled`；
    UnityEvent 通道裡有監聽器被覆寫時，寫通道的路徑（和 `events` 的 `path` 相同）。
  - Prefab 實例的根另列 `removedComponents` 與 `removedGameObjects`：從來源 Prefab 移除的元件與物件，
    寫成來源 Prefab 裡的名稱路徑（元件加識別）。

### 建置時比對

每次建置 player 時，對建置中的每個場景跑一般物件匯出，取出接線——每個物件引用欄位的值、每筆 UnityEvent 監聽器與它的狀態、
每個 missing script——一行一筆排序後存進專案的 `Library`，和同一平台上一次建置的結果比對：

- 寫出新增與消失的接線筆數；新出現的斷線（`missing` 引用、失效的監聽器、missing script）逐筆列在 Console 與建置 log 的警告裡。
- 只警告，不讓建置失敗；可在 Preferences 關閉。
- 沒有上一次的結果時（第一次建置、Library 重建後）只寫出本次。

範圍只有建置中的場景。只由程式載入的 Prefab 與資產（Resources、Addressables）不在建置比對裡，要看它們就用一般物件匯出。

## 改輸出的測試

第三層第 8 步的設計（2026-10-24）。只回答一個問題：agent 只拿翻譯器的輸出，讀得懂、改得動嗎？測的是輸出格式，
不是寫回工具。寫回由[文字結構同步](文字結構同步.md)負責；這裡用一段一般的 Editor 腳本把同樣的改動做進 Unity，
再匯出一次，當作標準答案。

- **案例**：BonghuoVR 的 `Assets/StaticResources/NPC/Prefab/MainGuy.prefab`（角色 prefab：骨架、SkinnedMeshRenderer、Animator）
  與 `Assets/Scenes/Main.unity`（UI 按鈕的監聽器、PlayableDirector 的事件與它持有的 inline SO）。先複製到暫存資料夾再匯出與修改，
  測完刪除，原檔不動。
- **受測的 agent**：一個不帶執行者對話的子 agent。它只拿到複本的匯出檔、
  [unity-agent-operations](../.agents/skills/unity-agent-operations/SKILL.md) 對一般物件匯出的說明，以及用一般話寫的題目；
  不讀 YAML、專案裡的其他檔案或匯出程式。
- **讀**：兩題，答案和匯出前的 Unity 組裝比對。
  1. MainGuy 每個 SkinnedMeshRenderer 用哪個 mesh、哪些材質、root bone 是誰。
  2. 按下 Settings Panel 的 Restart、Quit，以及 Global 的 PlayableDirector 停止時，各會做什麼。
- **改**：九題，涵蓋欄位值、資產引用、同檔物件引用、新增物件、新增元件、監聽器的新增與刪除、inline SO 的欄位、改名後引用跟著改。
  1. MainGuy 的 Animator 開啟 Apply Root Motion。
  2. `Boat_Past_MainGuy_Clothes` 的第 2 個材質換成 `Boat_Past_MainGuy_Body` 用的材質。
  3. `Boat_Past_MainGuy_Face` 的 root bone 改成 `mixamorig:Head`。
  4. 在 `mixamorig:Head` 下新增空物件 `HeadSocket`，local position (0, 0.15, 0)。
  5. MainGuy 根物件加一個 BoxCollider，center (0, 0.9, 0)、size (0.5, 1.8, 0.5)。
  6. 按下 Restart 時另外把 `Settings Panel` 關掉（`GameObject.SetActive(false)`）。
  7. Global 的 PlayableDirector 停止時不再開啟 Locomotion（刪掉那筆監聽器）。
  8. 停止時套用的位置設定，scale 也一併套用，值為 (1, 1, 1)。
  9. `Settings Panel/Panel/Quit` 改名為 `Exit`。
- **比對**：受測 agent 改出的輸出，和標準答案逐筆比對：物件依名稱路徑、元件依識別、欄位依路徑、監聽器依通道與順序。
  每題歸成一類：
  - 一致：兩邊相同。
  - 省略即預設：agent 沒寫的欄位，在 Unity 裡是預設值或由 Unity 推算（例如新元件的其他欄位）。寫回要補上，不算失敗。
  - agent 寫錯：格式表達得了，agent 寫錯；記下原因，例如說明不夠。
  - 格式不足：輸出表達不了這個改動，或寫法有歧義；記下違反〈[原則](#原則)〉的哪一條。
- **結論**：讀的兩題答對、改的九題沒有「格式不足」，就是可行，輸出成為
  [Agent friendly workflow〈先讀後寫〉](Agent%20friendly%20workflow%20Yu5h1Lib.UnityExtension.md#先讀後寫2026-10-22)
  的格式候選；有「格式不足」就還不是候選，補完缺口再測。結論寫在本節與〈先讀後寫〉第 2 步。
  另外記下受測 agent 的工具呼叫次數與格式錯誤，給〈驗證順序〉的對照實驗用。
- 題目沒碰到、但讀輸出時看到的〈原則〉缺口也一併列出。每個「格式不足」與缺口在這張任務卡記成一個「問題：」項目。

### 結果（2026-10-24）

可行：翻譯器的輸出是 Agent friendly workflow 的格式候選。

- 讀的兩題都答對。受測 agent 還從輸出看出 Restart 與它的父物件 `Panel` 存檔時是關閉的。
- 改的九題都是「一致」：受測 agent 改出的檔案和標準答案逐位元組相同，連新物件與新元件沒指定的欄位也照同類元件填對了。
  沒有「格式不足」，也沒有「agent 寫錯」。
- 受測 agent 用了 16 次工具呼叫，沒有格式錯誤。它用腳本查詢與改寫 25 MB 的輸出，沒有整份讀入。

輸出還有七個〈原則〉缺口。九題沒有踩到，但寫回前要補上（第 6、7 個由他驗找到）：

1. **Prefab 內的引用只寫物件名稱**（身分穩定）。場景裡的引用是名稱路徑加元件識別，Prefab 裡卻是
   `<Prefab 路徑> > <物件名稱> <型別>`，例如 `MainGuy.prefab > mixamorig:Spine2 <Transform>`：沒有父路徑、沒有 `#n`，
   引用的元件也沒有識別。同名物件或同一物件上的同型別元件會分不清。MainGuy 有 75 筆這種引用。
   unity-agent-operations 說引用一律是名稱路徑，和實際輸出不符。
2. **Object 模式監聽器的參數只寫物件名稱**（身分穩定、資產用可讀別名）。例如 `static_Interactable` 出現 6 次，
   但場景裡有好幾個同名物件；inline SO 參數寫成 `TransformInfoObject(Instance)`，而不是 `inline: <holder> > <field>`；
   空參數寫 `None`，不是 `null`。Main 有 23 筆 Object 模式的呼叫。
3. **監聽器沒有呼叫狀態**（內容完整）。Off／Editor and Runtime／Runtime Only 沒有輸出，關掉的監聽器和開著的分不清，
   新增監聽器時也指定不了。UnityEvent 報告有 `callState`，一般物件匯出與 inline SO 匯出沒有。
4. **Prefab 實例與 Variant 不分繼承值與覆寫**（宣告式、內容完整）。輸出只有實際值：Main 有 85 個 Prefab 實例的根，
   MainGuy 本身是模型的 Variant。哪些欄位是覆寫、哪些物件與元件是新增的，輸出看不出來；照實際值全部寫回，會把每個欄位都變成覆寫。
5. **inline SO 的身分是持有欄位的路徑，含監聽器的陣列索引**（身分穩定）。例如 `_Stopped.m_PersistentCalls.m_Calls.Array.data[0].m_Arguments.m_ObjectArgument`。
   刪除或重排前面的監聽器，後面持有的 inline SO 路徑就跟著變。第 7 題刪的是第 2 筆，第 1 筆沒動，所以沒有踩到。
6. **非 Prefab 資產的子物件只寫名稱**（身分穩定）。例如 Global 的 PlayableDirector 的 `m_SceneBindings` 有 6 筆 key
   都是 `Global.playable > Animation Track <AnimationTrack>`，各綁不同的 Animator；PlayableDirectorAddon 的 10 筆
   `_skipPointEvents` key 都是 `Global.playable > Skip Point <SkipPoint>`。分不出是哪條軌道或哪個標記。
7. **名稱裡的斜線沒有跳脫**（身分穩定）。`GameManager/XRDeviceSimulatorPlatformActivator/F10 TurnOn/Off Simulator`
   是一個名為 `F10 TurnOn/Off Simulator` 的物件，卻會讀成 `F10 TurnOn` 底下的 `Off Simulator`，而 `F10 TurnOn` 並不存在。

證據放在 `W:/UnityProject/BonghuoVR-Build/translator/writeback/`：`base.json`、`oracle.json`、`subject.json`、
`compare.py` 與 `compare.txt`、受測 agent 的 `prompt.md` 與 `notes.md`、標準答案的 `WriteBackOracle.cs`。
受測 agent 開工前依它環境的全域規則讀了 `Yu5h1Lib/.agents/AgentsRule.md`。那份檔案只有工作流程規則，不含這個格式、
場景或 Prefab 的內容。

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

- **建置時比對預設開啟、只警告、不擋建置**（2026-10-23，agent 依第 7 步的目標判斷，使用者尚未確認）：目標是「讓接線斷掉在建置時就被看到」，
  預設關就看不到，所以預設開；「被看到」不要求擋下建置，只警告就不改變任何專案的建置結果，日後要改成擋建置只是改一個判斷。
  開關沿用 common 既有建置掛鉤（`CICD_PreBuildProcess`）的 Preferences 作法。結果存在 `Library`：它是這台機器上次建置的紀錄，
  不進版本控制。

- **同名識別放在掃描核心**（2026-10-23）：一般物件匯出要用名稱路徑指向其他物件，〈原則〉要求同名時加穩定 ID；
  UnityEvent 報告與 inline SO 匯出也輸出名稱路徑，只在一處加識別，三份輸出指向同一物件時才寫法相同。

- **寫回前要補的寫法照 Unity 與既有輸出的作法定**（2026-10-24，agent 依現有實作判斷，沒有另出決策題）：
  - 資產檔裡的物件用名稱路徑：和場景引用同一套寫法，Prefab 與模型裡的物件本來就有階層。
  - 子資產的 `#n` 依 local file ID 排序：掃描核心走訪子資產時已用這個順序（`InWalkOrder`），重新載入也不變；
    同名同型別才編號，所以模型的 Mesh 不會因為同名的 GameObject 被編號。
  - 名稱跳脫用反斜線：建置比對的接線清單已用 `\r`、`\n` 跳脫換行；Unity 自己的 `Transform.Find` 沒有跳脫寫法可沿用。
  - 覆寫標記照 Unity Overrides 視窗的分法：修改的欄位、新增的元件與物件、移除的元件與物件。覆寫欄位列在元件的
    `overrides`，不在每個欄位加旗標，輸出才不會多出十幾萬行。
  - 接線清單沿用監聽器那一行的格式，不加呼叫狀態；Object 參數改了寫法，升級後第一次建置會把這些監聽器算成一增一減。

