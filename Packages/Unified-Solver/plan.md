# Unified Solver 通用粒子實體架構計畫

## 1. 目標

將目前集中在 `FishGenerator`、魚用 Renderer 與 `FishFlop.compute` 的功能，逐步拆成可組合的通用粒子物理架構。

完成後，同一套元件應能描述：

- 三點可彎曲結構。
- 四點方向引導結構。
- 六點雙軌結構。
- 十二點三段剛體關節結構。
- 單一剛體 Cluster。
- 未來其他柔性貨物、碎片、浮體或主動運動物件。

內容用途可以出現在 Profile Asset 名稱中，但不應出現在底層通用元件類別名稱中。

本計畫不打算重新實作 Unity ParticleSystem，也不要求每一種用途建立新的 Generator、Renderer 或 Compute Shader。

---

## 2. 已確定的命名

| 名稱 | 責任 |
|---|---|
| `SolverParticleEmitter` | 接收生成請求、排隊、檢查容量並在安全時機建立粒子實體 |
| `SolverParticleProfile` | 組合 Topology、物理尺寸、Renderer 與可選 Modifier |
| `SolverParticleSpawnRequest` | 描述尚未生成的一次請求 |
| `SolverParticleInstance` | 記錄已生成實體對應的 GPU 資料位置 |
| `SolverMeshRenderer` | 根據 Render Profile 選擇 Rigid 或 Articulated 顯示算法 |
| `SolverParticleModifierRunner` | 依 Modifier 類型批次 Dispatch Compute Kernel |
| `ParticleSystemSolverBridge` | 將 ParticleSystem Trigger 粒子轉換成 Solver Spawn Request |

不再以 `Fish...`、`Ice...` 等用途名稱建立通用元件。

### 內部算法名稱

Topology 建議使用工程名稱：

- `Single`
- `Chain3`
- `GuideChain4`
- `DualRail6`
- `RigidCluster4`
- `ArticulatedCluster12`

Render Mode：

- `Rigid`
- `Articulated`

Modifier：

- `Oscillation`
- `SurfaceImpulse`
- `Buoyancy`
- `Attraction`
- `Drag`

---

## 3. 責任邊界

### 3.1 SolverParticleEmitter

Emitter 只負責生成流程：

- 接收單筆或批次 `SolverParticleSpawnRequest`。
- 將請求放入可重複使用的 Queue/List。
- 預先檢查粒子、Constraint 與 Rigid Body 容量。
- 在 Solver 上傳 GPU Buffer 前統一完成生成。
- 成功後建立 `SolverParticleInstance`。
- 回報實際接受的請求數量。

Emitter 不應知道：

- 物件是什麼用途。
- 是否需要擺動。
- 使用哪一張 Mesh 或貼圖。
- Trigger 來源是不是 ParticleSystem。

### 3.2 SolverParticleProfile

Profile 是 ScriptableObject 組合設定，保存：

- Topology 定義。
- Base Dimensions。
- Mass 與 Constraint 參數。
- Render Profile。
- 可選 Modifier Profiles。

Profile 是模板，不保存場景中實體的即時狀態。

### 3.3 SolverMeshRenderer

Renderer 只負責：

- 讀取 `SolverParticleInstance`。
- 取得對應粒子或 Rigid Body Pose。
- 將 Instance 參數傳入 GPU。
- 根據 Render Mode 選擇正確 Shader。
- 使用同一份 Dimensions/Scale 對齊物理與視覺尺寸。

Renderer 不負責：

- Flop、跳動或浮力。
- 建立粒子與 Constraints。
- 判斷 ParticleSystem Trigger。

### 3.4 SolverParticleModifierRunner

Modifier Runner 負責主動或附加物理行為：

- 依 Modifier 類型將 Instance 分組。
- 每個 Modifier Kernel 一次 Dispatch 一整批 Instance。
- 從 StructuredBuffer 讀取每個 Instance 的不同參數。

不得為每個 Instance 建立一個 Compute Shader 或每條物件個別 Dispatch。

### 3.5 ParticleSystemSolverBridge

Bridge 是膠水元件：

- 使用 `void OnParticleTrigger()` 接收 Unity callback。
- 一次取得所有 Enter 粒子。
- 正確轉換 Local、World 或 Custom Simulation Space。
- 建立批次 `SolverParticleSpawnRequest`。
- 將批次送入指定 Emitter。
- 只刪除 Emitter 已接受的 ParticleSystem 粒子。

Bridge 不知道目標 Topology，也不直接操作 Solver GPU Buffer。

---

## 4. 核心資料模型

### 4.1 SolverParticleSpawnRequest

```csharp
public struct SolverParticleSpawnRequest
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public Vector3 scale;
    public Color color;
}
```

`scale` 必須同時影響：

- Topology 的粒子位置。
- 實際碰撞形狀。
- Renderer Mesh 尺寸。

只放大 Mesh、不放大物理結構是不允許的。

Rigid Topology 的初始粒子速度應包含角速度：

```text
particleVelocity = linearVelocity + angularVelocity × restOffset
```

### 4.2 SolverParticleInstance

```csharp
public struct SolverParticleInstance
{
    public int particleOffset;
    public int particleCount;

    public int constraintOffset;
    public int constraintCount;

    public int rigidBodyOffset;
    public int rigidBodyCount;

    public int profileId;
    public Vector3 scale;
}
```

第一階段可以讓 `SolverParticleInstance` 保持為 Emitter 的內部資料。

Renderer 與 Modifier 必須透過 Instance Record 找到資料，不再假設：

```text
baseOffset + instanceIndex × fixedParticleCount
```

這項改動是動態交錯生成多種 Profile 的必要條件。

---

## 5. Profile 組合

概念結構：

```text
SolverParticleProfile
├─ Topology Profile
├─ Base Dimensions
├─ Physical Settings
├─ Render Profile
└─ Modifier Profiles[]
```

### Profile A：三點關節結構

```text
Topology       = Chain3
Dimensions     = (width, length, thickness)
Render Mode    = Articulated
Modifiers      = Oscillation + SurfaceImpulse
```

### Profile B：剛體方塊結構

```text
Topology       = RigidCluster
Dimensions     = (width, height, depth)
Render Mode    = Rigid
Modifiers      = none
```

兩者使用同一個：

- `SolverParticleEmitter`
- `SolverParticleProfile` 類別
- `SolverMeshRenderer`
- `ParticleSystemSolverBridge`

差異只存在於 Profile 資料與選擇的算法。

---

## 6. Compute Shader 原則

### 6.1 核心物理

`UnifiedSolver.compute` 繼續負責通用算法：

- Predict。
- Distance Constraints。
- Particle Contacts。
- Ground/Collider Contacts。
- Friction。
- Rigid Shape Matching。
- Velocity Update。

新增 Topology 不應自動要求修改核心 Solver。

### 6.2 Modifier

相同算法、不同參數必須共用 Kernel。

以下差異不應建立新 Compute Shader：

- 大小不同。
- 擺動頻率不同。
- 擺動力量不同。
- 方向不同。
- 顏色不同。

只有算法不同才增加 Kernel，例如：

```text
SolverParticleModifiers.compute
├─ ApplyOscillation
├─ ApplySurfaceImpulse
├─ ApplyBuoyancy
├─ ApplyAttraction
└─ ApplyDrag
```

是否拆成多個 `.compute` 檔案是維護決策；真正影響效能的是 Dispatch 次數、Buffer 存取與分支，不是檔案數量。

原本 `FishFlop.compute` 的算法應拆成：

- `Oscillation`：對結構前後端施加週期性反向力量。
- `SurfaceImpulse`：靠近指定表面時施加脈衝。

---

## 7. Rendering Shader 原則

公開元件統一為：

```text
SolverMeshRenderer
```

內部至少保留兩種視覺算法：

```text
UnifiedSolver/RigidMesh
UnifiedSolver/ArticulatedMesh
```

### RigidMesh

- 整張 Mesh 使用一個剛體位置與旋轉。
- 適用於 Rigid Cluster。

### ArticulatedMesh

- 從粒子或剛體段建立多個控制 Frame。
- 根據 Mesh 長軸計算程序化權重。
- 在 GPU 混合控制 Frame 並改變頂點位置。

Renderer 應持有明確 Shader Reference 或 Render Profile Reference，避免只依賴 `Shader.Find` 而在 Build 中被 Shader Stripping 移除。

不同材質、貼圖、顏色或尺寸不需要新的 Shader。

---

## 8. 動態生成流程

```text
ParticleSystem Trigger
        ↓
ParticleSystemSolverBridge
        ↓ batch
SolverParticleSpawnRequest Queue
        ↓ safe simulation point
SolverParticleEmitter
        ↓ read Profile
Particles / Constraints / Rigid Groups
        ↓
SolverParticleInstance Buffer
        ├─ SolverParticleModifierRunner
        └─ SolverMeshRenderer
```

生成必須批次處理，避免：

- 每顆來源粒子呼叫一次 delegate。
- 每次生成都配置新 List/Array。
- Renderer 在 GPU Buffer 尚未上傳時提前增加 Draw Count。
- 來源 Particle 已刪除，但 Solver 因容量不足沒有成功生成。

---

## 9. 遷移階段

### Phase 0：鎖定目前可用基準

- 保存現有場景與備份。
- 記錄 Chain3、4、6、12 的可用參數。
- 記錄現有效能與生成數量。
- 暫時不刪除任何既有元件。

### Phase 1：建立通用資料契約

- 新增 `SolverParticleSpawnRequest`。
- 新增 `SolverParticleInstance`。
- 新增 `SolverParticleProfile` 基礎結構。
- 定義 Topology 與 Render Mode。
- 不改變現有場景行為。

### Phase 2：建立 SolverParticleEmitter

- 將生成 Queue、容量檢查與安全 Flush 實作在 Emitter。
- 先支援 `Chain3` 與 `RigidCluster`。
- 支援 Spawn On Start 與動態批次 Spawn。
- 第一階段維持 append-only，不處理刪除與回收。

### Phase 3：建立 Instance Mapping

- 每次成功生成建立一筆 Instance。
- 建立 Renderer/Modifier 可讀取的 Instance Buffer。
- 允許不同 Profile 交錯生成。
- 移除 Renderer 對連續固定 Offset 的假設。

### Phase 4：建立 SolverMeshRenderer

- 將現有 Rigid 與 Articulated Renderer 路徑移入通用元件。
- Render Profile 決定使用的 Shader。
- 使用 Instance Scale 同步物理與 Mesh 尺寸。
- 保留現有 Renderer 作為暫時相容 Wrapper。

### Phase 5：泛化主動行為

- 將 Flop 改成 `Oscillation` Modifier。
- 將近地面跳動改成 `SurfaceImpulse` Modifier。
- 建立 Modifier 參數 Buffer。
- 每個 Modifier 類型一次 Dispatch 全部符合的 Instance。

### Phase 6：ParticleSystem Trigger Bridge

- 實作 `ParticleSystemSolverBridge`。
- 支援 World、Local、Custom Simulation Space。
- 批次傳遞位置、速度、旋轉與大小。
- 只移除成功轉換的來源粒子。
- 驗證大量 Enter 事件沒有每幀 GC Allocation。

### Phase 7：遷移既有內容

- 以 Chain3 Profile 重現目前大量柔性貨物效果。
- 以 RigidCluster Profile 驗證方塊狀剛體。
- 將舊 `FishGenerator` 改為相容 Wrapper 或標記 Deprecated。
- 保留 `.meta` GUID，必要時使用 Unity 遷移屬性，避免場景 Missing Script。
- 完成所有場景遷移前，不刪除舊類別。

### Phase 8：壓力測試與回收決策

- 測試 512、1,000、2,000 個 Chain3 Instance。
- 測試 Rigid 與 Articulated Profile 交錯生成。
- 量測 Solver、Hash、Contact、Modifier 與 Renderer GPU 時間。
- 驗證 Trigger 高峰批次轉換。
- 根據實際遊戲生命週期決定是否加入 Free List/Pool。

---

## 10. 驗收標準

### 相容性

- 現有場景開啟後沒有 Missing Script。
- Chain3、4、6、12 的行為與重構前一致。
- 舊元件在遷移期間仍能運作。

### 解耦

- 建立新 Profile 不需要新增 Generator 類別。
- 改變尺寸不需要新 Compute Shader。
- 改變材質不需要新 Renderer 元件。
- 沒有用途名稱出現在通用 Runtime 類別中。

### 動態生成

- Trigger 可以一次轉換多個 Particle。
- 只刪除成功接受的來源 Particle。
- 多種 Profile 交錯生成時 Renderer 不會讀錯 Offset。
- 容量不足時有明確回傳與警告。

### 效能

- Modifier 以類型批次 Dispatch，不逐 Instance Dispatch。
- 穩定生成期間沒有持續 GC Allocation。
- GPU Renderer 不需要同步 Readback。
- 不為每個 Instance 建立 Material 或 GameObject。

### 物理與視覺一致

- Instance Scale 同時影響碰撞結構與 Mesh。
- Rigid 初始角速度能正確反映到各粒子速度。
- Articulated Renderer 使用 Instance Mapping，不依賴固定連續排列。

---

## 11. 已知風險與待決策

### 全域 Particle Radius

目前 Solver 使用全域 `_ParticleRadius`。不同 Profile 若需要不同碰撞半徑，不能只在 Profile 增加欄位。

未來選項：

1. 第一版維持全域半徑，Profile 僅調整粒子排列。
2. 將 radius 加入 `ParticleGPU`，修改 Contact、Collider、Renderer 與 Spatial Hash。

若採用每粒子半徑，Hash Cell Size 必須以場景最大半徑計算。

### 回收與刪除

第一版採 append-only，適合有限捕撈流程。

只有壓力測試證明遊戲需要長時間循環生成時，才加入：

- Alive flag。
- Free List。
- Instance generation/version。
- Constraint 與 Rigid Body 回收策略。

### Profile 多型方式

第一版優先使用少量明確 Enum 與資料結構，避免過早建立複雜介面階層。

當第三方需要自訂 Topology 或 Modifier 時，再評估：

- Abstract ScriptableObject。
- `ISolverParticleTopology`。
- `ISolverParticleModifier`。

---

## 12. 實作原則摘要

```text
不同用途       → 不新增通用元件類別
不同 Profile   → 組合既有 Topology / Renderer / Modifier
不同參數       → 共用 Kernel 與 Shader，資料放 Buffer
不同算法       → 新增 Kernel 或 Shader Strategy
不同實體       → 使用 SolverParticleInstance 對應 GPU 資料
```

優先順序：

1. 保持現有成功結果。
2. 先建立資料契約與 Instance Mapping。
3. 再抽出 Emitter 與 Renderer。
4. 最後泛化 Modifier 與 Trigger Bridge。
5. 壓力測試後才決定是否加入回收系統。

---

## 13. 冰塊碎片物理 P0 計畫

### 13.1 產品目標與範圍

目前最優先的 Solver 應用是「冰塊碎片物理」：

- 在 Editor 將一個來源冰塊 Mesh 切成多個不規則碎片。
- 每個碎片 Bake 成一個可序列化的剛體 Particle Group。
- 每個碎片依 Seed 與幾何條件選擇 4、6 或 8 個非退化物理粒子。
- Runtime 中碎片可與魚、其他碎片及箱體碰撞。
- 碎片動量接近停止並持續一段時間後必須真正停止抖動。
- 魚、其他碎片、外力或 Collider 的有效撞擊必須喚醒碎片。
- 具有持續作用 Modifier 的 Instance 必須保持 Awake。

這不是一般 ParticleSystem 特效，也不是只把 4／6／8 粒子外面包一層程序凸包。來源 Mesh 切割、碎片資料 Bake、Runtime 剛體群組、碰撞與 Sleep/Wake 是同一條 P0 垂直功能。

`Documentation/ParticleSystem x Unified Solver.md` 保留為長期能力邊界文件，但排在本計畫最後，不阻塞冰塊碎片物理。

### 13.2 系統邊界

```text
Editor Authoring
Source Ice Mesh
    → Fracture／切割
    → 封閉切面、材質與 UV
    → 每片選取 4／6／8 個 Rest Particles
    → 幾何與粒子契約驗證
    → SolverFractureBakeData

Runtime
SolverFractureBakeData
    → 每片建立一個 SolverParticleInstance
    → 每片建立一個 Rigid Group
    → Fragment Renderer 批次繪製
    → 魚／碎片／箱體碰撞
    → Awake／Candidate／Sleeping 狀態
```

原始來源 Mesh 不在 Runtime 動態切割。Player 只消費 Editor 已 Bake、版本化且通過驗證的資料，避免執行昂貴幾何運算與不確定的配置。

### 13.3 碎片序列化資料契約

新增單一 Fracture Asset，至少保存：

```text
SolverFractureBakeData
├─ dataVersion
├─ sourceMeshReference
├─ sourceMeshHash
├─ fractureSettingsHash
├─ seed
├─ fragments[]
│  ├─ fragmentId
│  ├─ mesh / combinedMeshRange
│  ├─ localPivot
│  ├─ localCenterOfMass
│  ├─ localBounds
│  ├─ volume
│  ├─ mass
│  ├─ material / subMesh mapping
│  ├─ particleVariant（4／6／8）
│  ├─ restPositions[]
│  ├─ rigidParticleIndices[]
│  └─ requirements
└─ bakeDiagnostics
```

`SolverParticleInstance` 繼續作為唯一 Runtime Group Instance，不建立冰塊專用的平行 Instance 型別。Fracture Bake Data 負責靜態定義；Instance 負責粒子、Rigid ranges、Fragment ID、Render mapping 與即時 Sleep state。

Bake 必須可重現：

- 相同來源 Mesh、設定、Seed 與版本產生相同結果。
- Source Hash 或 Settings Hash 改變時，Inspector 明確標示 Stale。
- 提供 `Bake`、`Rebuild`、`Clear Baked Data`。
- 不在 `OnValidate()` 自動執行切割。
- Bake 失敗時保留上一份有效資料，不留下半成品。

### 13.4 Editor Mesh Fracture

第一版只需要可靠的 Editor 工作流，不追求通用 Runtime 破壞系統：

1. 以固定 Seed 產生切割平面或 Voronoi Sites。
2. 將來源 Mesh 切成目標數量的封閉碎片。
3. 為新切面建立朝外 Winding、Normals、UV 與 Interior Material/SubMesh。
4. 移除零面積 Triangle、極小孤島與非封閉結果。
5. 計算每片 Pivot、Center of Mass、Bounds、Volume 與 Mass。
6. 為每片建立物理 Rest Particles。
7. 驗證後寫入 `SolverFractureBakeData`。

第一版允許只支援可讀、封閉且拓樸合法的 Mesh。Skinned Mesh、非流形 Mesh、多層破碎、Runtime 再切割可明確拒絕並顯示原因。

原有 `ParticleConvexHullMethod` 可保留作為測試或簡單形狀工具，但不再是冰塊碎片的主要 Mesh 來源。

### 13.5 每片 4／6／8 Particle Group

「隨機 4／6／8」表示每個碎片在 Bake 時以可重現 Seed 選擇候選點數，再依碎片幾何驗證，不是無限制地在 Bounds 內亂數取點。

候選策略：

- 4 點：適合小型或接近四面體的碎片；四點必須不共平面。
- 6 點：適合中型、長條或扁平碎片；點需覆蓋主要軸正負方向。
- 8 點：適合大型或接近盒狀的碎片；點需覆蓋體積與各方向。

每片必須通過：

- Rest Positions 位於碎片內部或表面容差內。
- 四面體體積或協方差秩高於退化門檻。
- 粒子分布覆蓋碎片主要範圍，不集中在單一角落。
- 粒子間距符合全域 `particleRadius` 與碰撞需求。
- 視覺 Mesh 相對 Pivot、Center of Mass 與 Rest Pose 對齊。

若隨機選定的 4 點或 6 點無法可靠覆蓋碎片，Baker 必須升級到 6 或 8 點，或拒絕該碎片，不可序列化退化剛體。

現有 `SolverManager.AddRigidBody()` 支援可變 particle indices；Extension 仍需：

- 將固定長度 4 的 scratch 擴充為可重用的 8 點容量。
- 依每片實際 variant 計算 Particle、Rigid Body 與 Rigid Reference requirements。
- 在 C#、Compute 與 Shader 一致保存 Fragment/Variant mapping。
- Spawn 容量不足時整片拒絕，不建立半個 Instance。

### 13.6 Runtime 生成與繪製

初版以整份 Bake Asset 一次生成為基準：

- 每個 Fragment 建立一個 `SolverParticleInstance` 與一個 Rigid Group。
- 碎片 Mesh 不建立逐片 Material。
- 可先依 Fragment Mesh/Material 分批，再評估 Combined Fragment Mesh 與 per-vertex Fragment ID。
- Renderer 必須保留 batch-local → global Instance mapping。
- Runtime 不執行 Fracture、Convex Hull 或 Type 掃描。

若冰塊只破碎一次並在場景中保留，可沿用 append-only 容量模型。只有產品需要反覆生成、銷毀與再破碎時，才將 Free List、Despawn 與 slot recycling 升為必要項目。

### 13.7 魚、碎片與箱體碰撞

基準驗證場景：

```text
SolverManager
├─ Fish Emitter
├─ Ice Fracture Runtime
└─ SolverBoxCollider Container
```

必須驗證：

- `enableParticleCollisions = true`。
- `cellSize >= 2 * particleRadius`。
- 碎片之間不使用會排除互撞的 Phase。
- 魚與碎片可互撞。
- 碎片與箱體底面、牆面及接縫可穩定碰撞。
- 大碎片的 Rest Particles 足以代表其體積，不因視覺 Mesh 遠大於粒子覆蓋而穿透。
- 調校 `substeps`、friction、`maxDepenetrationSpeed`、牆厚與初始 Spawn spacing。

視覺 Mesh 不是 Collision Mesh。4／6／8 粒子只是低成本近似；若某碎片無法由最多 8 點形成足夠的碰撞覆蓋，Baker 必須拆得更小、拒絕該片，或未來採用更高點數 Profile。

### 13.8 Sleep／Wake 是必要功能

冰塊碎片不接受「速度很低但持續抖動」作為完成狀態。Sleep 必須是明確的 per-instance 狀態機：

```text
Awake
  └─ 低於 Sleep Threshold
      → Candidate
          ├─ 再次運動 → Awake
          └─ 持續超過 Sleep Delay → Sleeping

Sleeping
  └─ Impact／Force／Active Modifier／Manual Wake
      → Awake
```

判定至少考慮：

- 平均線速度。
- 角速度或 Rest Pose 旋轉殘差。
- 接觸／Constraint 修正量。
- 低速持續時間。
- 不同的 Sleep 與 Wake thresholds，形成 hysteresis。

Sleeping 行為：

- 將 velocity 歸零並同步 `prevPosition = position`。
- 整個 Rigid Group 一起睡眠或喚醒。
- 保留為碰撞障礙，不能因睡眠消失。
- 有效碰撞 impulse、穿透修正、移動 Collider、外力或手動要求能喚醒。
- 碎片堆疊需驗證連鎖喚醒不會漏掉，也不會因微小接觸噪音整堆反覆醒來。

Modifier 規則：

- 任何會持續寫入 Position、Velocity、Force、Pose 或 Constraint target 的 Modifier 必須宣告 `KeepAwake`。
- Modifier 啟用時立即喚醒對應 Instance。
- Modifier 持續作用期間不得進入 Candidate 或 Sleeping。
- Modifier 停止後重新開始計算 Sleep Delay。
- 一次性 Modifier 可在完成後釋放 `KeepAwake`，但其施加的有效動量仍會自然保持 Awake。

Extension-only 的 Settling 可先完成視覺停止與狀態機驗證，但它仍會經過原始 Solver kernels，不能宣稱節省 GPU 成本。要做到真正略過 Predict、Rigid Shape Matching 或其他核心運算，需要改動 Compute Pipeline；原始 vendored dependency 目前保持唯讀，因此在實作核心 Sleep 前必須由使用者另行授權受控修改或建立可維護 Fork。這個授權邊界不降低 Sleep/Wake 的產品優先級。

建議初始參數僅作調校起點：

```text
Linear Sleep Threshold：0.01～0.03 m/s
Sleep Delay：0.3～1.0 s
Wake Threshold：明顯高於 Sleep Threshold
```

### 13.9 實作順序

#### Stage 1：Fracture 與 Bake 契約

- 定義 `SolverFractureBakeData`、Fragment Record、版本、Hash、Seed 與 Requirements。
- 建立 Editor Fracture API 與可重現測試。
- 完成切面封閉、Winding、Normals、UV、Interior Material 與錯誤診斷。

#### Stage 2：Fragment Particle Groups

- 為每片選擇並驗證 4／6／8 Rest Particles。
- 建立非退化與覆蓋率驗證。
- 擴充 variable rigid count、capacity estimate 與 scratch。
- 保存 Fragment ID、Variant、Pivot、Center of Mass 與 Render mapping。

#### Stage 3：Runtime Spawn 與 Render

- 從 Bake Asset 一次建立所有 Fragment Instances。
- 完成 Mesh/Material batches 與 instance index mapping。
- 驗證視覺 Mesh、Rest Pose 與剛體運動一致。

#### Stage 4：互動碰撞

- 建立魚、碎片與箱體基準場景。
- 驗證碎片互撞、魚撞碎片與箱體接觸。
- 記錄容量、穿透、接縫與不同碎片尺寸的限制。

#### Stage 5：Sleep／Wake 與 Modifier KeepAwake

- 建立 Awake/Candidate/Sleeping state buffer 與 timer。
- 實作線速度、角運動、接觸修正與 hysteresis 判定。
- 實作 Impact、Force、Collider、Manual 與 Modifier wake。
- 驗證無 Modifier 的靜置碎片停止抖動。
- 驗證持續 Modifier 永不睡眠，停止後可以正常入睡。
- 分別量測 Extension Settling 與核心 Sleep 的視覺穩定度及 GPU 成本。

#### Stage 6：生命週期與效能擴充

- 只有反覆生成／銷毀成為需求時才加入 Despawn、Free List 與 slot recycling。
- 大量碎片時評估 Combined Fragment Mesh、Fragment ID 與更少 Draw Calls。
- 取得壓力測試數據後再決定更高粒子數的碰撞 Profile。

#### Stage 7：ParticleSystem × Unified Solver

- 保留現有 `ParticleSystemSolverBridge` 的單向轉移用途。
- `Documentation/ParticleSystem x Unified Solver.md` 暫時只維護能力邊界與長期方向。
- 不在碎片物理完成前擴充 ParticleSystem 整合、通用 Emission、Lifetime 或 Renderer Proxy。

### 13.10 驗收標準

#### Authoring

- 相同 Mesh、設定、Seed 與版本產生相同碎片。
- 所有輸出碎片封閉、無退化 Triangle，切面 Winding、Normals、UV 與 Material 正確。
- Source/Settings Hash 過期時能偵測；Bake 失敗不破壞上一份有效資料。
- 每片皆具有有效的 Pivot、Center of Mass、Mass、Bounds 與序列化 Mesh mapping。

#### Particle Groups

- 每片只使用 4、6 或 8 個 Rest Particles，且通過非退化與覆蓋驗證。
- Bake 可在候選失敗時升級點數或明確拒絕，不輸出不穩定 Group。
- Spawn 容量不足時整片拒絕，不留下半生成資料。

#### Physics

- 碎片保持剛體形狀，視覺 Mesh 與物理 Rest Pose 對齊。
- 碎片之間、魚與碎片、碎片與箱體都有可重現碰撞。
- 合理尺寸的碎片不因粒子覆蓋不足而明顯穿透。

#### Sleep／Wake

- 無持續 Modifier 的低動量碎片在延遲後停止可見抖動。
- Sleeping 碎片仍阻擋其他物體。
- 魚、其他碎片、外力、移動 Collider 或手動命令可喚醒碎片。
- 持續 Modifier 作用期間 Instance 不會睡眠；停用後可重新進入 Sleep。
- Sleep/Wake threshold 附近不會產生快速反覆切換。

### 13.11 明確延後

以下項目不阻塞第一個冰塊碎片垂直切片：

- Runtime 動態 Fracture 與碎片再次破碎。
- Skinned Mesh、非流形 Mesh 與任意開放 Mesh 的自動修復。
- MeshCollider／Triangle BVH 碰撞。
- 粒子對 Unity Rigidbody 的雙向作用力。
- 完整 Despawn、Free List 與 Constraint/Rigid slot 回收。
- ParticleSystem 的通用系統整合與 Soft Body 產品化。

## 14. 身體形變的未來計畫

本節記錄兩項已完成設計評估、但明確延後實作的項目。兩者都不阻塞目前的彎曲表演修正。

### 14.1 Muscle 物理：以 restLength 調變產生真實力道

#### 前提修正（2026-07-31）

本節原本主張「kinematic 驅動永遠產生不了推地反作用力，因此非做 Muscle 不可」。**這個前提已由實測推翻，優先級隨之下降。**

實際情形是：Modifier 的位置瞬移發生在 Solver 之後，該瞬移會把粒子壓進支撐物；下一幀 Solver 的碰撞把它夾回來，而 `UpdateVelocity` 用 `(position - prevPosition) / subDt` 把那段修正換算成速度。於是**外部反作用力一直都在被計算**，身體本來就頂得起來 —— 甚至強到需要另外加上限。詳見第 15 節。

所以 Muscle 的價值不再是「唯一能產生彈跳的路」，而是把目前這個**偶發副作用**轉成**受控的設計特性**：

- 力道由 `compliance` 決定，而不是由「瞬移穿透多深」決定。
- 消除彈跳力道與 `substeps` 的耦合（見 15.2）。
- 消除高速瞬移穿過薄物件的風險。
- 形變與接觸在同一次求解內協商，方向與力矩自然成立，不需要外部再假造。

動量中性本身不是缺陷。魚的肌肉是內部的，本來就不該憑空產生淨動量；增益來自地面，那是正當的外部來源。

#### 可行性（已驗證）

不需要修改唯讀的 vendored 依賴：

- `DistanceConstraintGPU.restLength` 是 GPU buffer 中可寫的 float。
- 原始 Solver 的 Compute 以 `RWStructuredBuffer<DistanceConstraint> _Constraints` 宣告。
- `SolverManager.ConstraintBuffer` 是 public 屬性，不需反射，也不需經過 `SolverManagerAccess` 相容橋接。

#### 設計

把彎曲從「事後擺姿勢」改為「每幀調變 restLength」，由 Solver 自己產生彎曲。

DualRail6 的兩條軌即為魚的兩側肌肉：

```text
rail A (x = +hx)：粒子 0, 2, 4   →  AddJoint(0,2) AddJoint(2,4)
rail B (x = -hx)：粒子 1, 3, 5   →  AddJoint(1,3) AddJoint(3,5)
rung（寬度）    ：AddJoint(0,1) (2,3) (4,5)
```

縮短 rail A 的 restLength 並拉長 rail B，身體即往 +X 彎；週期交替產生 C／反 C。

得到的性質：

- 彎曲與接觸在同一次求解內協商，反作用力自動成立，身體可頂起。
- 接觸中的實體仍可彎曲，肌肉約束與接觸約束在同一輪公平競爭。
- `compliance` 天然成為力道上限，是力而非瞬移。
- 動量守恆天然成立，不需要手動扣除加權平均。

#### 實作需求

- 執行順序必須改到 Solver 之前（`SolverParticleModifierRunner` 目前是 `[DefaultExecutionOrder(50)]`）。
- Emitter 必須在 Spawn 時記錄每條約束的 baseline restLength，每幀在 baseline 上調變。`SolverParticleInstance` 既有的 `constraintOffset` 與 `constraintCount` 足以定位範圍。
- `_constraintsDirty` 為真的那一幀，Solver 會從 CPU list 重傳整個 buffer 並覆蓋調變。因為調變是每幀重算，下一幀即恢復，可接受。

### 14.2 Topology 資料化

#### 現況

`Chain3`、`GuideChain4`、`DualRail6`、`ArticulatedCluster12` 四者的脊椎控制點數皆為 **3**。離軸粒子提供的是 Body Frame 與面內剛度，不是額外的彎曲自由度。因此四者皆只能表現 C-bend，無法表現具反曲點的 S-curve。

#### 硬編碼位置

資料化容易的部分：

- `SolverParticleEmitter.BuildLocalShape()` 的 Rest 位置。
- `SolverParticleEmitter.AddTopologyData()` 的約束接線。
- `SolverParticleProfile.Requirements` 的數量宣告。

真正的工作量在：

- `SolverParticleModifiers.compute` 的 `ControlCenter(0/1/2)` 與 `BodyRanges()` 的頭／中／尾三段假設。
- `SolverArticulatedMesh.shader` 的 `DeformVertex()`：目前是以 `longitudinal < 0.5` 分段、在相鄰兩個 Frame 之間混合的三骨骼 Linear Blend Skinning，必須推廣為 N 段迴圈。

#### 觸發條件

當「游動的行進波」成為需求時再實作。行進波需要 4 個以上的脊椎控制點才有意義；在此之前推廣為 N 段，換不到可見的表現力，卻要先重寫 Shader 與 Kernel。

## 15. 身體彎曲與接觸彈跳（已實作）

本節記錄 2026-07-31 完成的彎曲表演與彈跳機制，以及推導過程中確立的幾條物理關係。這些關係不是實作細節，調任何相關參數之前都需要先理解。

### 15.1 彎曲驅動：角度式而非弦長式

早期版本把頭尾投影回「當前頭尾軸」再計算弦長，導致該軸在數學上永遠無法擺動，身體只能中段鼓包。現版本改為兩段身體繞中段各旋轉 ±halfAngle：

```text
headOffset =  segmentLength × (tangent × cos + direction × sin)
tailOffset = -segmentLength × (tangent × cos − direction × sin)
```

- 頭尾側向位移放大為原本的三倍，成為真正的圓弧掃掠。
- 振幅由 `muscleTension` 反向決定（見 15.3），峰值半角為 `asin(1 − muscleTension)`。
- 姿勢繞**加權質心**建構，權重與動量平衡一致，因此 delta 的加權和天然為零。GuideChain4 先前的 `positionMean = mΔ/4` 系統性側移殘差隨之消失。

對稱 C 彎在幾何上不會旋轉頭尾弦向量，這是正確行為。若日後需要「頭穩、尾大幅掃」那種偏擺，需要的是**非對稱**驅動（只旋轉尾段），不是更多控制點。

### 15.2 彈跳的來源：接觸反作用力，以及它與 substeps 的耦合

彈跳不是任何欄位直接施加的，而是這條鏈的產物：

```text
Modifier 位置瞬移（無上限）
  → 粒子被壓進支撐物
  → Solver 硬夾回表面    p.position.y = minY
  → UpdateVelocity       v = (position − prevPosition) / subDt
```

**關鍵是分母為 `subDt` 而非幀時間。** 以 `substeps = 30`、`fixedDeltaTime = 0.02` 計，`subDt = 6.67e-4`，穿透被放大約 1500 倍換算成速度；1 mm 穿透即產生 1.5 m/s。

由此得到兩條必須記住的關係：

- **彈跳力道與 `substeps` 成反比。** 把 `substeps` 從 30 降到 5，彈跳力道降為六分之一。這曾被誤認為「調 substeps 修好了彈跳」，實際上是拿布料剛性去換。
- **目標為筆直時（現在的 `muscleTension = 1`）仍會彈跳。** 此時速度通道寫入完全為零，但 Modifier 仍每幀全力把被重力與接觸弄歪的身體扳正。位置修正的內容是「撤銷 Solver 剛做的事」，不是「推進彎曲動畫」，所以與彎曲振幅無關。這條由實測隔離出來：把振幅歸零之後彈跳依舊存在，證明來源是位置通道。

### 15.3 三個表演軸

彎曲表演由三個互相獨立的軸決定，都在 `SolverOscillationProfile`：

```text
stiffness       0~1   硬度。整體速率，語意反轉
vitality        0~1   活力。願不願意自主動作
muscleTension   0~1   肌肉張力。目標形狀
frequency       Hz    播放頻率（第四個，與上述無關）
```

驅動強度是前兩者的乘積：

```csharp
drive = vitality × (1 − stiffness)
delta = (目標姿勢 − 目前位置) × drive
```

**`stiffness` 有兩個方向相反的作用**，這是它與 `vitality` 真正分開的地方：

- 提高抗變形能力：把粒子速度收斂到實體平均速度，移除相對運動，於是**當下是什麼形狀就凍在什麼形狀**。它不持有任何目標形狀。
- 降低自主動作：透過 `1 − stiffness` 縮放 drive。

`stiffness = 1` 是凍結；`vitality = 0` 是癱軟。兩者都讓 `drive = 0`，但前者形狀鎖死、後者任由物理擺布，這是兩軸的分界。

**`muscleTension` 決定目標形狀**：

```hlsl
peakHalfAngle = asin(saturate(1.0 - muscleTension));
```

- `0` → `asin(1)` = 90°，幾何極限（身體對折）
- `1` → `asin(0)` = 0°，目標即 topology 的骨架形狀

`1` 對應的是「肌肉抽筋」：身體漸進收斂回預設形狀。**注意 0 是幾何極限而非自然幅度**，自然範圍約在 0.2 ~ 0.4；彎曲角速度是 `peakHalfAngle × ω`，所以振幅與頻率同樣影響觀感速度。

### 15.4 彈跳預算

彈跳是接觸反作用的產物（見 15.2），由 `drive` 編列預算：

```csharp
maximumDrop = drive × SURFACE_PUSH_SPEED × subDt
```

`SURFACE_PUSH_SPEED` 是 kernel 內的常數（3 m/s，約 46 cm）。`vitality = 0` 或 `stiffness = 1` 任一成立，彈跳即歸零 —— 對應「沒力氣推不動」與「太硬動不了」。

**只縮放姿勢位移的垂直分量，水平完全不動。** 三個垂直分量的加權和本來為零，乘上同一係數仍為零，不需補償。早期版本等比縮放整個向量，導致「達成目標形狀」這件事（大部分是水平運動）被一併砍到二十分之一，`muscleTension = 1` 因此無法收斂。

### 15.5 動畫的時間模型

`frequency` 與 `duration` 是兩件事，不能由同一個參數表達：

```text
frequency   Hz   多久開始一次
duration    秒   一次播放多久，直接指定
```

單次時長是**授權值而非推導值**：設 1 秒就是 1 秒，跟形狀無關。時長同時決定姿勢位移的速率上限 `2π × segmentLength × peakHalfAngle / duration × dt`，所以拉長 duration 會同時讓動作變慢、彈跳變輕，符合「彎得越慢力道越小」的直覺。

`burstFraction = saturate(duration × frequency)`。時長超過間隔時自動退化為連續播放，不需手動避開衝突。

**待機期間釋放 `drive`，而不是把 `wave` 歸零。** 歸零 `wave` 會讓目標停在骨架形狀，於是身體在「應該什麼都不做」的時候被主動扳直，而且 frequency 越低被扳直的時間越長。釋放 drive 才是真正的癱軟，與 `vitality = 0` 行為一致。

`frequency = 0` 因此代表「永不觸發」而非「凍在隨機彎度」。

### 15.6 結構屬性：roll 阻尼與 settle

兩者都在 `SolverParticleProfile`，無條件執行，不需要掛任何 modifier。

**`rollDamping`** 移除控制群組繞身體長軸的角速度，以群組質心為軸等量反向施加，線動量不受影響。

GuideChain4 最需要它，因為它的扭轉恢復力是**精確為零**：導引粒子的三條約束（`AddJoint(1,3)`、`AddBend(0,3)`、`AddBend(2,3)`）端點全在脊椎這條直線上，把導引粒子繞該線旋轉，三個距離完全不變。DualRail6 的對角線只提供二階恢復力（`Δ長度 ≈ hx²φ²/(4hy)`），弱但非零，所以漂移較慢。

**`settleSpeed`** 在相對速度低於門檻時，把粒子速度收斂到實體平均速度，平滑淡入而非門檻硬切。只移除相對運動，身體仍會移動、掉落、滑動，只是不再改變形狀。

用意是清除數值殘留：繞長軸的旋轉沒有恢復力，Solver 每步留下的一點永遠不會被還回去，累積到一定程度身體的橫向就轉了一整圈。趁殘留還小的時候清掉，就到不了那個程度。


### 15.7 已確立的邊界條件

以下關係在調參前必須知道，否則會像本次一樣繞遠路：

- **XPBD 沒有施力 API。** `Predict` 中的重力是唯一的力，且為寫死的 uniform，沒有 `_ExternalForce` buffer。外部只有兩個管道：寫 `velocity` 或寫 `position`。
- **`Particle` 沒有接觸旗標。** 五個碰撞 kernel 只改 `position`，不寫任何狀態。接觸只能從速度特徵推論。
- **約束 damping 被 compliance 縮放：** `gamma = compliance × damping / subDt`。**`compliance = 0` 時 gamma 恆為 0，`constraintDamping` 無論設多少都無效。** `ClothGenerator.compliance` 預設即為 0。
- **全域 damping 是單一 uniform**，`UpdateVelocity` 不讀 `phase`，Solver 本身沒有分組機制。要 per-group damping 只能改 vendored 依賴（見 15.5）。
- **從 Solver 外部注入的 velocity 幾乎無效。** `UpdateVelocity` 每個 substep 都以位置差覆寫 velocity，外部注入只在下一次 `Predict` 存活一個 substep。位置寫入才有實效 —— 但位置寫入會繞過碰撞偵測，量大時造成穿透。

### 15.8 未採用的方案與原因

- **Per-instance damping 補償（幀尾放大速度）** —— 對自由飛行數學上精確，但對受約束的實體完全失效，因為速度會被約束求解覆寫。damping 必須在 substep 迴圈內作用才有效，從迴圈外複製不出來。已移除。
- **以速度上限限制彈跳** —— 太晚。位移在 Solver 的 substep 迴圈內就已完成，幀尾夾速度只是把身體停在它已經到達的高度，而下一步又再穿透一次，於是每幀往上棘輪累積。必須在穿透發生前限制位移。已移除。
- **`torsionAlign`（段間扭轉對齊）** —— 曾新增一個 kernel，把三段控制群組的 rail 方向旋轉回互相一致。實測**解決不了任何問題**：沙孔狀變形其實是 Shader 端框架反號造成的，已由 15.9 的符號對齊解掉；而整體滾動時三段始終一致，這個機制偵測不到。反而自己製造兩個新問題 —— 直接旋轉粒子位置造成穿透與跳躍，以及增益寫成 `strength × angle / dt`（單步就要轉完整個偏差，速度延續必然過衝）造成靜止時的持續擺尾。已完整移除。
- **`burstDuration`（爆發／待機分離）** —— 曾嘗試讓 `frequency` 只管觸發頻率、`burstDuration` 只管單次彎曲時長。實作可行且無狀態，但沒有解決實際問題：觀感速度是 `振幅 × 角頻率`，而連續波裡這兩者已經分別由 `muscleTension` 與 `frequency` 控制，再切一刀只是換個地方表達同一件事。已回退。
- **Fork vendored solver** —— 曾為了 per-group damping 評估。因 `ClothGenerator.compliance` 設為非零後 `constraintDamping` 即可運作，暫不需要。Sleep/Wake 核心仍會迫使這個決定重新浮上檯面（見 13.8）。
- **`SolverManagerAdvanced` / `UnifiedSolverAdvanced`（在 extension 內複製一份）** —— 評估後否決。`SolverManager` 的 36 個欄位全為 private、0 個 protected，子類別無法存取模擬迴圈所需狀態，實際上等於複製 1558 行去改 4 行。且 handoff 的 SHA-256 保證會在字面成立的同時失去意義（證明的是一個沒在跑的檔案）。若真需修改，原地小改或完整 fork 都優於部分複製。


### 15.9 Shader 框架的健壯性

蒙皮框架的每一項退化都會直接變成畫面上的破圖，而且症狀各不相同：

- **相鄰框架的 `side` 反號** → `FramePosition` 以 `side × localPosition.x` 撐出寬度，兩端反號時 lerp 在中點寬度抵消為零，mesh 被捏成一點、前後張開，呈沙漏或 X 形。修法是三段共用中段的 side 來源，並在正交化**之前**強制與中段同號。
- **`head − tail` 當中段切線** → 該向量長度是 `2L × cos(halfAngle)`，身體對折時歸零，`normalize` 回傳捨入誤差。改為沿鏈取兩段單位方向的平均。**但平均本身也會在 180° 對折時歸零**，這一步只是把退化點往後推，並沒有消除它；真正的處理見 15.11。
- **退化時掉回 `spawnRotation`** → 它只在 spawn 寫入一次、之後永不更新，對翻滾過的實體完全無關。掉進去會讓框架瞬間跳到陌生朝向。改為先沿鏈找備案（head 段 → tail 段），最後才是 spawn 軸。
- **`SafePerpendicular` 用絕對門檻** → `1e-5` 會讓「數值上極小但仍過門檻」的投影被完整採用，normalize 把捨入誤差放大成方向、逐幀亂跳。改為相對於候選向量自身長度（十分之一），並以 lerp 平滑交棒而非硬切。

### 15.10 未解決：鏡像翻轉

躺著的實體偶爾會**整條瞬間鏡像**，骨架姿勢正確但左右對調，貼圖顯示另一側。

已確立的事實：

- `stiffness = 1` 完全不發生。它唯一多做的事是把粒子速度收斂到實體平均，移除相對運動。
- Runtime 把 `stiffness` 設 1 再設回 0，清掉累積後短時間內不會復發。
- 靜止的實體沒事；**尾巴還在緩慢擺動的會發生**。
- GuideChain4 比 DualRail6 頻繁（主觀觀察，無數據），與前者扭轉恢復力為零、後者為二階非零一致。

推論：繞長軸的旋轉在**位置層級**累積。Solver 的 substep 迴圈內已經轉過去了，Modifier 之後才能動手，所以任何只作用在速度上的機制（`rollDamping`、`settleSpeed`）都追不回來，只能降低累積速率。`settleSpeed` 的作用區間（接近靜止）又與問題發生的區間（緩慢擺動）錯開，因此只能減緩。

候選解法（未採用，保留備案）：給每個 instance 一個獨立的 side 記憶 buffer（不動 `SolverParticleInstance`，避免改變 stride 與四處 struct 定義），每幀比對上一幀朝向，撤銷低於門檻的慢速旋轉、放行高於門檻的真實翻滾。使用者認為 side cache 方向不正確，暫不採用。

### 15.11 頭尾黏在一起（髮夾對折）

**症狀**：身體頭尾兩端貼到同一個位置後就黏住不分開，中段連同整條 mesh 開始高速亂轉。地面上少見，網子裡魚被互相擠壓時常發生。

**這是同一個故障，不是兩個。** 髮夾姿勢下身體「沒有軸」—— 哪一端朝前在幾何上就是未定義的。而所有驅動與蒙皮都要先量出這條軸：

```
middleTangent = normalize(headDirection + tailDirection)   // = 2·cos(halfAngle)·axis
```

頭尾重合時這個和**恰好為零**；接近重合時它的方向由兩段殘餘的不對稱決定，方向誤差大約是「不對稱量 ÷ 和的長度」，會無上限放大，並在對折掃過 180° 的瞬間整個反號。原本的守門是 `tangentLength > 1e-5`，一個**絕對**門檻，套在自然尺度為 2 的向量上 —— 長度 3e-4 的雜訊向量遠高於門檻，照樣被 normalize 當成方向送出去。這正是 `SafePerpendicular` 已經學過一次、但沒有套用到切線上的教訓。

接著形成自我維持的迴圈：`ApplyOscillation` 就是拿這條軸去建姿勢的（`headDirection = tangent·cos + direction·sin`），軸一反號，頭與尾的目標位置就對調，Solver 把身體拉過去，下一幀再量到反號的軸。同時 mesh 三段的 `side` 全部以中段定號，於是整條一起翻。而黏住的原因也在這裡：驅動每幀都在把兩端推開，但推的方向每幀重骰，於是兩端只是在原地隨機遊走，不會分離。

**修法三層，由外而內：**

1. **不讓驅動自己折到髮夾** —— `peakHalfAngle` 加上 `MAXIMUM_HALF_ANGLE = 70°` 上限。弦長是 `2 × segmentLength × cos(halfAngle)`，90° 正好把兩端疊在一起，而 `asin(1 − muscleTension)` 在 tension 0 時要的就是 90°。70° 保留 34% 身長的弦。tension 高於 0.06 完全不受影響，既有調校不動。
2. **軸不可反號** —— 新增 `BisectDirections`：以和的長度**相對於自身自然尺度 2** 判斷可信度，在約 160° 之後線性交棒給一個**不對稱且連續**的備案（head 段方向）。備案必須不對稱，否則會在同一個地方以同樣的理由退化。交棒是 lerp 不是切換，所以方向不會跳。副作用是好的：深度對折時軸偏向還有方向的那半邊，驅動據此建的姿勢會把身體撬開，而不是壓住。
3. **結構性下限，與 modifier 無關** —— 新增 `UnfoldHairpin`，把頭尾弦長的下限固定在身長的 25%（低於驅動自己最多要到的 34%，所以不會和正常擺動打架）。三控制點拓樸對「折多深」沒有任何限制：頭尾之間只有一條軟的 bend 約束，其餘約束對對折完全無感，所以網子裡被兩側夾住時壓得到重合。用**位置下限**而不是力，因為折是接觸壓出來的，力得先贏過擠壓來源。

`UnfoldHairpin` 的推開方向：弦還有可信長度時就用弦本身（分開兩點只有沿兩點連線才有定義），弦垮掉時交棒給身體自己的 cross 方向（GuideChain4 的 guide、DualRail6 的 rail）—— 它由離軸粒子承載所以是真實的、垂直於脊椎所以兩端不必拉伸自己的段、而且在髮夾處**不**退化。兩者混合前先把 cross 對弦正交化：髮夾附近弦本身也垂直於脊椎，可能與 cross 反向而在中間權重整個抵消，正交化後弦的貢獻恰好等於混合權重，修正永遠是在撐開身體。

預算沿用既有的兩段式：橫向以 `UNFOLD_SPEED × _DeltaTime` 分幀走完，沿重力軸另外用 `UNFOLD_DROP_SPEED × _SubDeltaTime` 這個嚴格得多的上限 —— Solver 把位移除以 substep 而非幀來換算速度，橫向無害的一步指向下方時會把身體彈飛，而兩端必有一端朝下。以共同係數縮放，維持等量反向，只修朝下那端會在身體上留下淨推力。`prevPosition` 同步跟著位移，否則修正會被讀成速度。

因為這是結構性的，`SolverParticleModifierRunner` 對 roll damping kernel 改為**無條件 dispatch**；kernel 內部 roll damping 與 settle 仍各自依 profile 值自我把關。`_OscillationUpAxis` 相應改名為 `_UpAxis` 並移到共用參數，因為沒有掛 modifier 的實體也需要它。
