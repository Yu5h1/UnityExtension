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

## 13. 箱裝冰塊、程序化外殼與休眠計畫

### 13.1 目標

在同一個 `SolverManager` 中支援：

- 不同尺寸的剛體冰塊。
- 每個冰塊生成時隨機選擇 4、6 或 8 個剛體粒子。
- 冰塊、魚與箱體三方碰撞。
- 不需人工指定獨立 Mesh Asset，由 Editor 依粒子 Group 的 Rest Shape 建立程序化凸包 Mesh。
- 冰塊低速穩定後停止可見抖動。
- 保留 Asset Mesh 路徑，讓魚繼續使用既有模型與 Articulated Renderer。

第一階段只要求有限場景中的穩定裝箱，不將完整 Despawn、Free List 或任意凹面重建納入本計畫。

### 13.2 已確定的分層

Mesh 來源只有兩類：

```csharp
public enum SolverMeshSource
{
    Asset,
    Procedural
}
```

`SolverMeshSource` 是 Authoring 選擇；Runtime Renderer 最終一律取得已存在的 `Mesh`：

```text
Asset
    → 使用人工指定 Mesh

Procedural
    → Editor 依 Particle Group Rest Shape 生成 Mesh
    → 保存為 SolverRenderProfile 的隱藏 Sub-Asset
    → Runtime 直接讀取已 Bake Mesh
```

程序 Mesh 方法以無參數 Attribute 標記：

```csharp
[ProceduralMeshMethod]
public sealed class ParticleConvexHullMethod :
    IProceduralMeshMethod
{
}
```

Render Profile 不序列化 `System.Type`，只保存 Editor Registry Key：

```csharp
[SerializeField]
string proceduralMeshMethodType;
```

第一版以 `Type.FullName` 作為 Registry Key：

```text
proceduralMeshMethodType
        ↓
Editor Registry.TryGet(string, out Type)
        ↓
首次顯示 Inspector 或按下 Bake 時初始化
        ↓
UnityEditor.TypeCache 掃描 [ProceduralMeshMethod]
```

不另外要求 Attribute 提供 ID 或 Display Name：

- 身分來自實作 `Type`。
- 序列化 Key 來自 `Type.FullName`。
- Inspector 名稱由 `ObjectNames.NicifyVariableName(type.Name)` 產生。
- 未來只有在需要分類、本地化或自訂排序時，才擴充 Attribute metadata。

Runtime 不解析 `proceduralMeshMethodType`，也不執行程序 Mesh 方法；Player 只消費已序列化的 Bake 結果。

### 13.3 Particle Group 共用資料

魚與冰塊共用相同的 Group Definition 與 Instance 管線，不建立 `IceParticleGroup` 或 `FishParticleGroup`：

```text
結構差異 → SolverParticleGroupBakeData
生成隨機 → Spawn Variant Policy
行為差異 → Modifier
顯示差異 → Render Profile
即時狀態 → SolverParticleInstance
```

Editor Bake Data 至少包含：

```text
SolverParticleGroupBakeData
├─ variant
├─ restPositions[]
├─ constraints[]
├─ rigidParticleIndices[]
├─ rigidGroups[]
├─ optional controls[]
└─ requirements
```

現有 `SolverParticleInstance` 繼續作為 Runtime Group Instance，不建立第二份重複的 Instance 型別。它保存實際的 particle、constraint、rigid offsets/counts，以及對應的 shape variant。

對應關係：

| Group | Rest Points | Constraints | Rigid Groups | Modifier | Renderer |
|---|---:|---:|---:|---|---|
| Chain3 | 3 | Joint＋Bend | 0 | Oscillation | Asset Articulated |
| DualRail6 | 6 | Joint＋Bend | 0 | Oscillation | Asset Articulated |
| Articulated12 | 12 | 關節連接 | 3 | Oscillation | Asset Articulated |
| Ice4 | 4 | 0 | 全部粒子一組 | Settling | Baked Procedural |
| Ice6 | 6 | 0 | 全部粒子一組 | Settling | Baked Procedural |
| Ice8 | 8 | 0 | 全部粒子一組 | Settling | Baked Procedural |

### 13.4 Editor Method Registry 與 Bake

Editor Registry 在第一次 Inspector 查詢時使用：

```csharp
UnityEditor.TypeCache
    .GetTypesWithAttribute<ProceduralMeshMethodAttribute>()
```

每個 Type 必須：

- 不是 abstract。
- 實作 `IProceduralMeshMethod`。
- 可以建立無參數實體，或由 Editor Registry 建立對應 Build Delegate。

Editor 整合需：

1. 偵測重複 `Type.FullName`、無效介面與無法建立實體的錯誤。
2. Inspector 找不到已序列化 Type 時保留原字串並顯示警告，不自動改成清單第一項。
3. 提供 `Bake`、`Rebuild`、`Clear Baked Data` 操作。
4. 以輸入 Hash 判斷 Rest Shape 或生成設定是否已改變；過期時顯示需要 Rebuild。
5. 不在每次 `OnValidate()` 自動執行昂貴幾何重建。

不需要讓 Runtime asmdef 引用 `Yu5h1Lib.Common`，也不需要 Player 使用 `RuntimeTypeCache`。若未來確定需要 Runtime 動態生成，再另行加入 Runtime Registry 與 IL2CPP preservation。

程序 Mesh 契約只存在於 Authoring/Bake 路徑：

程序 Mesh 方法接收 Rest Shape，不讀取模擬中的即時 GPU Position：

```csharp
public interface IProceduralMeshMethod
{
    bool Build(
        in SolverProceduralMeshContext context,
        Mesh target,
        out string error);
}
```

Context 至少包含：

```text
Local Rest Positions
Topology / Shape Variant
Base Dimensions
可選的 Method Settings
```

Editor Bake 流程：

```text
Particle Group Rest Points
        ↓
Convex Hull 找出外殼 Face
        ↓
以 Group Center 修正 Triangle Winding
        ↓
計算朝外 Flat Normals
        ↓
寫入 Generated Mesh
        ↓
加入 SolverRenderProfile Sub-Asset
        ↓
保存 Source Hash 與 Variant Mapping
```

Group Center 只負責判斷已找到 Face 的朝向；哪些點屬於表面、哪些三點形成 Face，仍由 Convex Hull 決定。

第一個內建方法為：

```text
ParticleConvexHullMethod
```

Voxel Surface、Marching Cubes、Metaball、任意凹面重建與來源 Mesh 碎片切割不屬於第一階段。

### 13.5 Rest Shape 單一來源

目前 `SolverParticleEmitter.BuildLocalShape()` 同時隱含了物理拓樸定義。實作前應將它抽成共用的 Group Definition Builder：

```text
SolverParticleGroupDefinitionBuilder
        ↓ Editor Bake
SolverParticleGroupBakeData
        ├─ Runtime Emitter：建立物理粒子與 Rigid Body
        └─ Editor Mesh Method：建立視覺外殼
```

不得讓 Emitter 與 Mesh Baker 各自維護不同的 4、6、8 點座標，也不得讓 Runtime 重新推導另一份 Rest Shape。

第一版使用穩定模板，不使用可能退化的任意隨機座標：

- 4 點：不共平面的四面體模板。
- 6 點：沿正負 X、Y、Z 的八面體模板。
- 8 點：立方體八角模板。

「隨機 4、6、8」表示每次 Spawn 隨機選擇模板，不表示每顆粒子位置完全隨機。

可見差異優先來自：

- Instance Scale。
- 非等比長寬高。
- Spawn Rotation。
- Color。
- Shader Random Seed。

若未來加入 Rest Position Jitter，必須先驗證：

- 四點體積大於退化門檻。
- Hull topology 沒有翻面或零面積 Triangle。
- 相同 Batch 的頂點／索引契約仍可共用。

### 13.6 4／6／8 剛體生成

原始 `SolverManager.AddRigidBody()` 已能接收可變數量的 particle indices；主要修改集中在 Extension：

- 將固定長度 4 的 rigid index scratch 改為至少 8，或改為可重用陣列。
- 新增 4、6、8 Rest Shape 定義。
- 容量預留依實際 variant 計算 particle 與 rigid particle reference 數量。
- `SolverParticleInstance.particleCount` 保存實際點數。
- Instance 記錄可辨識 render/shape variant。
- 不讓每次 Spawn 配置新的 List 或 Array。

需要決定並驗證 Instance 欄位配置：

1. 使用目前未使用的 32-bit padding 保存 `shapeVariant`，維持 64-byte stride。
2. 或正式擴充 struct 並同步修改所有 C#、Compute 與 Shader layout。

優先選擇不增加 stride、但名稱與型別明確的欄位替換；不得只在 C# 端改 layout。

### 13.7 Editor Bake 儲存與繪製批次

程序 Mesh 不在 Type 掃描期間建立。真正的 `Build()` 只在 Editor 使用者按下 Bake/Rebuild 時發生：

```text
SolverRenderProfile Custom Editor
        ↓
Editor Registry.TryGet(methodType)
        ↓
讀取 SolverParticleGroupBakeData
        ↓
Build → Validate
        ↓
寫入／更新 SolverRenderProfile Sub-Asset
```

Bake Mapping 至少包含：

```text
Procedural Method Type
Shape Variant（4／6／8）
Source Hash
會影響幾何的設定版本或 Hash
Generated Mesh Reference
```

Instance Scale 不應造成新的 Bake Mesh；尺寸由 GPU Instance Scale 套用。Runtime 不建立或釋放 Generated Mesh，只讀取已序列化的 Mesh reference。

若同一個 Emitter 混合 4、6、8，Renderer 不能用一個 Mesh 對全部 Instance 單次 Draw。第一版採每個 Mesh Variant 一個 Render Batch：

```text
RigidCluster4 Batch → 一次 Draw
RigidCluster6 Batch → 一次 Draw
RigidCluster8 Batch → 一次 Draw
```

需要建立每批 Instance Index Mapping，讓 Shader 由 batch-local instance ID 找回原始 `SolverParticleInstance`。不得為每顆冰塊建立獨立 GameObject、Material 或 Mesh。

若第一階段希望先降低改動風險，可先使用三個 Emitter／Profile 各自持有 4、6、8 variant，再由 Spawn Controller 隨機分流；完成穩定驗證後才合併為單一 Emitter 多 Batch。

Generated Mesh 優先保存為既有 `SolverRenderProfile` 的隱藏 Sub-Asset，而不是建立散落的獨立 `.asset` 或把大量重複幾何內嵌於每個 Scene。材質仍由 Render Profile 引用；同一組 4／6／8 Mesh 可以共用 Ice Material。

### 13.8 箱體與魚／冰塊碰撞

場景基準：

```text
SolverManager
├─ Fish Emitter
├─ Ice Emitter / Ice Spawn Controller
└─ 五個 SolverBoxCollider
   ├─ Bottom
   ├─ Left
   ├─ Right
   ├─ Front
   └─ Back
```

必要設定：

- `enableParticleCollisions = true`。
- `cellSize >= 2 * particleRadius`。
- 冰塊 Instance 之間不得共享會略過互撞的 Phase。
- 魚與冰塊使用可互相碰撞的不同 Phase。
- 牆角適度重疊，避免薄牆接縫。
- 依堆積穩定度調整 `substeps`、friction 與 `maxDepenetrationSpeed`。

限制：

- 全部 Profile 仍共用全域 `particleRadius`。
- 4、6 點 Hull 是凸多面體，不是完整 Cube。
- 8 點模板才直接形成 Cube 八角凸包。
- 大尺寸 Group 若粒子間距遠大於 `2 * particleRadius`，魚仍可能穿過物理點之間的空隙；視覺 Convex Hull 不會自動成為連續碰撞面。
- 真正封閉的大型冰塊碰撞需要增加表面／體積粒子，不能只靠 Renderer Mesh 解決。

### 13.9 Settling 與 Sleep 分級

#### Phase A：Settling Modifier

先實作不修改原始 Solver 核心的低速穩定器：

```text
Linear Speed Threshold
Angular Speed Threshold
Settle Delay
Wake Threshold
```

行為：

1. 以 Instance 為單位計算平均線速度與旋轉殘差。
2. 低於門檻並持續指定時間後進入 settled 狀態。
3. settled 時將該 Instance 粒子 velocity 歸零，並同步 `prevPosition = position`。
4. 碰撞造成速度高於 Wake Threshold 時立即離開 settled。
5. 使用不同的 sleep/wake threshold，避免門檻附近反覆切換。

Settling 目標是降低可見抖動，不宣稱節省 Solver 運算；粒子仍存在於 Predict、Contact、Constraint 與 Rigid Body Kernel。

建議初始調校範圍：

```text
Linear Threshold：0.01～0.03 m/s
Angular Threshold：依尺寸換算後調校
Settle Delay：0.3～1.0 s
Wake Threshold：高於 Sleep Threshold
```

魚的主動 Oscillation Profile 預設禁止 Settling；冰塊 Profile 才啟用。

#### Phase B：真正 Sleep/Wake

只有 Phase A 仍無法穩定或 GPU 成本需要下降時才進入：

- sleeping particle 不執行重力積分。
- sleeping rigid body 略過 Shape Matching。
- Contact 將 sleeping body 視為零有效 inverse mass。
- 撞擊、深度穿透、移動 Collider 或支撐物變化可以喚醒。
- 一個 Instance 任一粒子被喚醒時，整個剛體一起喚醒。
- 堆疊接觸需要驗證連鎖喚醒。

真正 Sleep/Wake 會修改原始 Solver Compute Pipeline；依目前非侵入式邊界，實作前需要另外確認是否允許修改 vendored dependency，或先建立可維護的 upstream fork。

### 13.10 實作階段

#### Stage 1：Particle Group 資料契約

- 新增 `SolverParticleGroupBakeData`、constraint、rigid range 與 variant 資料。
- 保留 `SolverParticleInstance` 作為唯一 Runtime Group Instance。
- 將硬編碼 Requirements 改由 Group Definition/Bake Data 提供。
- 定義 Bake Data 版本與 Source Hash。

#### Stage 2：Rest Shape 與 4／6／8 Rigid Cluster

- 抽出共用 Group Definition Builder。
- 建立穩定的 4、6、8 模板。
- 擴充容量估算與可重用 scratch。
- 讓 Instance 保存實際 particle count 與 shape variant。
- 驗證三種剛體自由落下、旋轉與地面碰撞。

#### Stage 3：Editor Mesh Bake

- 新增 `SolverMeshSource.Asset/Procedural`。
- 新增 `[ProceduralMeshMethod]`。
- 新增 `IProceduralMeshMethod` 與 Build Context。
- 新增 Editor Lazy Registry 與 Type Popup。
- 實作 Particle Convex Hull Builder。
- 自動修正 outward winding，建立 Flat Normals、Bounds 與退化檢查。
- 提供 Bake、Rebuild、Clear Baked Data。
- 將 4／6／8 Generated Mesh 保存為 `SolverRenderProfile` Sub-Assets。
- 保存 Source Hash，偵測過期 Bake。
- 驗證 Asset 模式完全維持現有行為。

#### Stage 4：Renderer Variant Batching

- Runtime Renderer 只解析 Asset/Baked Mesh reference，不執行 Type 掃描或 Convex Hull。
- 先以三 Emitter 路徑完成基準驗證。
- 再加入單 Emitter 4／6／8 Instance 分批。
- 建立 batch-local → global Instance Index Mapping。
- 驗證每個 Variant 一次 Draw，不逐 Instance Draw。

#### Stage 5：箱裝互動

- 建立五面 SolverBoxCollider 測試箱。
- 混合生成不同 Scale 的冰塊。
- 加入魚 Profile。
- 驗證冰塊互撞、魚冰互撞、箱體碰撞與溢出。
- 調校 substeps、friction、wall thickness 與 spawn spacing。

#### Stage 6：Settling Modifier

- 建立 per-instance settling state/timer。
- 加入 sleep/wake hysteresis。
- 驗證靜置冰塊不抖動。
- 驗證魚撞擊能重新推動冰塊。
- 比較啟用前後視覺穩定度與 GPU 時間。

#### Stage 7：後續決策

- 根據 Stage 6 壓力測試決定是否修改核心加入真正 Sleep/Wake。
- 若只剩極小視覺抖動且效能足夠，停止於 Settling。
- 若需要大量靜態堆積的實際 GPU 節省，再另立核心 Sleep/Wake 實作項目。
- 只有確定需要任意來源 Mesh 破碎時，才研究第三方 Fracture 專案與建立獨立 `SolverFractureBaker` 計畫。

### 13.11 驗收標準

#### Registry

- 新增帶 `[ProceduralMeshMethod]` 的有效 Type 後，不修改 enum 或中央註冊表即可出現在 Inspector。
- Editor 能由已序列化字串解析相同 Type。
- Missing Type 不會覆寫 Profile 資料。
- 重複或無效 Type 產生清楚錯誤。
- Player 不執行 Type 掃描、Activator 或程序 Mesh Build。

#### Mesh

- Asset 模式行為與目前一致。
- Procedural 模式不需要人工建立或指定獨立 Mesh Asset。
- 4、6、8 Rest Shape 都能建立朝外、無零面積 Triangle 的凸包。
- Mesh 只在明確 Bake/Rebuild 時生成，不在 `OnValidate` 或 Runtime 每幀重建。
- Generated Mesh 作為 `SolverRenderProfile` Sub-Asset 保存，Prefab、Scene 與 Build 可穩定引用。
- Source Hash 過期時 Inspector 明確提示 Rebuild。
- 相同 Variant 與設定共用同一份 Baked Mesh。

#### Physics

- 三種冰塊都保持剛體形狀。
- 不同 Scale 的物理點與視覺外殼對齊。
- 冰塊之間、魚與冰塊之間都有碰撞。
- 箱體靜置後沒有持續可見爆震或大量穿牆。
- 容量不足時 Spawn 明確拒絕，不留下半生成 Instance。

#### Settling

- 低速冰塊在延遲後穩定。
- 魚或其他冰塊的有效撞擊可以喚醒／推動已穩定冰塊。
- 主動 Oscillation 的魚不會被誤判為 settled。
- Settling 不改變 append-only 與容量語意。

### 13.12 明確延後

以下項目不屬於本階段：

- 每粒子不同 radius。
- 任意凹面 Particle Surface reconstruction。
- 每顆冰塊完全不同拓樸的獨立 Unity Mesh。
- 任意來源 Mesh 的 Voronoi／平面切割、切面補洞、切面 UV 與多材質 Fracture Bake。
- Combined Fragment Mesh、per-vertex Fragment ID 與專用 GPU Fracture Renderer。
- MeshCollider／Triangle BVH 碰撞。
- 粒子對 Unity Rigidbody 的雙向作用力。
- 完整 Instance Despawn、Free List 與 Constraint/Rigid slot 回收。
- 生命週期或高度 Fade 後的物理容量回收。
