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
