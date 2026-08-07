# ParticleSystem × Unified Solver 能力互補計畫

> 狀態：長期能力邊界文件，當前優先級最後。現階段先完成 `plan.md` 第 13 節的冰塊碎片物理；除維護既有單向 Bridge 外，本計畫不進入實作排程。

## 1. 計畫目的

本計畫的目標不是以 Unified Solver 重新製作一套 ParticleSystem，而是：

- 保留 Unity ParticleSystem 已成熟的 Inspector、Emission、Shape、Lifetime、Renderer 與內容製作工作流。
- 由 Unified Solver 補足 ParticleSystem 不擅長或沒有提供的互動物理能力。
- 讓一般特效仍可直接使用 ParticleSystem，只有真正需要粒子互撞、約束、布料、剛體群組或未來 Soft Body 的內容才加入 Solver。
- 建立清楚的資料所有權，避免 ParticleSystem 與 Solver 同時控制同一粒子的位置與速度。
- 保持原版 Unified Solver 原始碼為唯讀相依，新增能力集中在擴充層。

這是一份能力邊界與演進計畫。近期不以完成所有 ParticleSystem Module 相容、不以立即完成通用 Soft Body 為目標。

---

## 2. 核心定位

### 2.1 ParticleSystem 的責任

ParticleSystem 優先負責 Unity 已經做好的內容工作流：

- Emission Rate 與 Burst。
- Shape 與初始方向。
- Start Lifetime、Speed、Size、Rotation、Color。
- Color、Size、Rotation over Lifetime。
- Texture Sheet Animation。
- Mesh、Material、Billboard 與 Renderer 設定。
- 一般 Noise、Force、Trails、Sub Emitters 與場景 Collider 效果。
- Inspector 預覽、Scene 操作與既有使用習慣。

只要內容不需要 Solver 的特殊物理，就不應加入 Solver。

### 2.2 Unified Solver 的責任

Unified Solver 專注在 ParticleSystem 缺少或難以高效完成的互動：

- 粒子彼此碰撞。
- Spatial Hash 與大量鄰近粒子查詢。
- Distance Constraint。
- Shape Matching 與 Rigid Cluster。
- Articulated Cluster。
- Cloth 與節點約束。
- 粒子與 Solver Collider、地面、摩擦的互動。
- GPU 批次 Modifier。
- 未來的 Volume、Pressure、Soft Body、肌肉與脂肪層互動。

Solver 不應自行複製整套 ParticleSystem Emission、Shape、Lifetime、Color、Texture Sheet 或 Inspector 工作流。

### 2.3 擴充層的責任

擴充層負責在兩者之間建立明確、有限的合作方式：

- 將 ParticleSystem 粒子一次性轉交給 Solver。
- 在必要時以 ParticleSystem Renderer 顯示 Solver 狀態。
- 為既有 Solver 結構增加 Anchor、Grabber 與其他互動工具。
- 提供 Profile、Bridge、Renderer 與相容存取層。
- 不讓內容元件直接依賴原版 Solver 的 private 實作。

---

## 3. 最重要的設計規則：單一權威

同一項狀態在同一時間只能有一個權威來源。

| 狀態 | ParticleSystem 模式 | Solver 模式 |
|---|---|---|
| 生成時間與數量 | ParticleSystem | Emitter 或 Bridge |
| Lifetime | ParticleSystem | Solver Instance Lifecycle |
| Position | ParticleSystem | Solver Particle Buffer |
| Velocity | ParticleSystem | Solver Particle Buffer |
| Rotation | ParticleSystem | Solver Pose 或 Particle Frame |
| Color、Size | ParticleSystem Module | Instance/Profile Buffer |
| 粒子互撞 | 不提供通用自碰撞 | Solver |
| Cloth／Soft Body 約束 | 不負責 | Solver |
| Mesh 與材質 | ParticleSystemRenderer | Solver Renderer 或 Proxy Shader |

禁止以下模式：

```text
ParticleSystem 每幀更新 Position
             同時
Solver 每幀更新同一粒子的 Position
```

這會造成碰撞、Trigger、Trails、Sorting、Bounds 與畫面位置互相矛盾。

---

## 4. 合作模式

### Mode A：ParticleSystem Only

適用：

- 煙、霧、火花、塵土與一般裝飾特效。
- 只需要 ParticleSystem 內建 Collision Module 與場景 Collider 的內容。
- 不需要粒子彼此碰撞或結構約束。

特點：

- 完整保留 ParticleSystem UX。
- 不配置 Solver 粒子。
- 不增加同步與映射成本。

這應該是預設模式。

### Mode B：ParticleSystem + CPU/Burst Interaction

適用：

- 希望保留完整 ParticleSystem 工作流。
- 主要缺少的是粒子彼此碰撞。
- 實際粒子量能由 CPU Job 與 Burst 承擔。

可能實作：

- 使用 ParticleSystem C# Job System 取得粒子資料。
- 建立 CPU Spatial Hash。
- 以 Burst Job 建立碰撞 Pair。
- 批次修正位置與速度。

優點：

- 保留 Emission、Lifetime、Noise、Trails、Sub Emitters 與 Renderer。
- 不需要 ParticleSystem 與 GPU Solver 的雙重狀態。
- 只補足缺少的粒子互撞，不重做 ParticleSystem。

限制：

- 仍是 CPU 互動。
- 跨多個 ParticleSystem 的全域碰撞需要額外資料聚合。
- 高密度粒子數仍需實際壓力測試。

Unity 官方提供 ParticleSystem C# Job 與 Burst 整合：

- [Optimize the Particle System with the C# Job System](https://docs.unity3d.com/6000.0/Documentation/Manual/particle-system-job-system-integration.html)

### Mode C：ParticleSystem → Solver 單向轉移

適用：

- 粒子在一般階段使用 ParticleSystem。
- 進入特定區域或事件後才需要 Solver 互動。
- 轉移後可以放棄 ParticleSystem 的後續運動 Module。

資料流：

```text
ParticleSystem Particle
        ↓ Trigger／事件
ParticleSystemSolverBridge
        ↓ 建立 Spawn Request
SolverParticleEmitter
        ↓ 成功後
刪除原 ParticleSystem Particle
```

現有 `ParticleSystemSolverBridge` 屬於此模式。

規則：

- 只刪除 Solver 已接受的來源粒子。
- 轉移後 Position、Velocity、Rotation 由 Solver 擁有。
- 不做每幀雙向同步。
- Bridge 只轉換資料，不承擔 Topology 與渲染策略。

這是目前最安全的 GPU Solver 整合方式。

### Mode D：ParticleSystem Renderer + Solver GPU Proxy

適用：

- CPU/Burst 粒子互撞不符合效能需求。
- 希望繼續使用 ParticleSystem 的 Emission、Lifetime、Color、Size、Mesh 與 Renderer UX。
- 可以接受部分 ParticleSystem Module 不再具有正確物理語意。

概念：

```text
ParticleSystem
├─ 生成 Proxy Particle
├─ Lifetime／Color／Size
└─ Custom1 傳送 solverSlot
                    ↓
ParticleSystem Custom Shader
                    ↓
讀取 Solver ComputeBuffer 的 Position／Rotation
```

技術基礎：

- ParticleSystemRenderer 支援 Mesh GPU Instancing。
- Custom Vertex Streams 可傳送每粒子 ID。
- Material 可綁定 ComputeBuffer／GraphicsBuffer。
- Vertex Shader 可忽略 ParticleSystem Position，改用 Solver Pose。

官方參考：

- [ParticleSystem GPU Instancing](https://docs.unity3d.com/cn/current/Manual/PartSysInstancing.html)
- [ParticleSystem Custom Vertex Streams](https://docs.unity3d.com/cn/current/Manual/PartSysVertexStreams.html)
- [Material.SetBuffer](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Material.SetBuffer.html)

主要限制：

- ParticleSystem Collision／Trigger 仍使用 CPU Position。
- Trails 與 Sub Emitters 可能跟隨錯誤的位置。
- Sorting 與 Bounds 需要額外處理。
- Noise、Velocity over Lifetime、Force over Lifetime 不會自動作用到 Solver。
- 粒子死亡時必須回收 solverSlot。
- 目前 Solver append-only，尚未具備長時間 ParticleSystem 所需的 Free List。

此模式只作為 CPU/Burst 不足後的實驗方向，不列為近期預設架構。

### Mode E：Solver Native Structure

適用：

- Cloth。
- Rigid Cluster。
- Articulated Cluster。
- 軟體與有拓撲關係的結構。
- 未來瑜珈球、氣球、肌肉、脂肪與動物軟組織。

這類內容的核心不是大量獨立特效粒子，而是粒子之間的結構關係，因此應由 Solver 擁有完整物理狀態。

ParticleSystem 可以作為：

- 生成事件來源。
- 碰撞區域前的預覽或一般狀態。
- 附加煙塵、碎屑、液滴等視覺效果。

但不應成為結構粒子的物理權威。

---

## 5. 現有功能定位

### 5.1 ParticleSystemSolverBridge

目前定位：

- Mode C 單向轉移。
- 使用 ParticleSystem Trigger Enter 取得來源粒子。
- 轉換 World、Local、Custom Simulation Space。
- 傳遞位置、速度、旋轉、大小、顏色與角速度。
- 只移除 Solver 已成功接受的來源粒子。

不應擴張成：

- ParticleSystem 所有 Module 的複製器。
- 每幀 ParticleSystem ↔ Solver 雙向同步器。

### 5.2 SolverParticleEmitter 與 Profile

目前提供：

- Spawn Request Queue。
- 容量檢查。
- Instance Mapping。
- Single、Chain3、GuideChain4、DualRail6。
- RigidCluster4、ArticulatedCluster12。
- Profile 化尺寸、質量、Renderer 與 Modifier。

定位：

- Solver Native Structure 的生成入口。
- Bridge 的目標端。

不應取代 ParticleSystem 的通用 Emission、Shape 與 Lifetime Inspector。

### 5.3 SolverMeshRenderer

目前提供：

- Rigid Mesh GPU procedural rendering。
- Articulated Mesh GPU procedural rendering。
- 直接讀取 Solver Particle／Rigid Body Buffer。

定位：

- Mode E 的原生 Solver Renderer。
- 未來 Mode D Proxy Shader 的技術參考。

### 5.4 SolverParticleModifierRunner

目前提供：

- Oscillation。
- Surface Impulse。
- 同類 Modifier 批次 Dispatch。

定位：

- Solver Native Structure 的附加行為。
- 不模仿 ParticleSystem Noise 或 Force over Lifetime 的完整 UI。

只有當行為需要 Solver 結構、碰撞或拓撲資訊時，才應新增 Modifier。

### 5.5 ClothAnchor

目前提供：

- 將指定 Cloth 節點綁定到場景 Transform。
- 以 Compute Shader 批次更新 Anchor Position。
- 不修改原版 ClothGenerator。

它是本計畫的代表性模式：

```text
保留原版 Cloth 生成與模擬
            +
擴充層只補上缺少的 Anchor 工作流
```

### 5.6 ClothGrabber

目前提供：

- 以指定 Hand Transform 抓取附近 Cloth 節點。
- 保存抓取時相對位置。
- Apply 時更新節點位置與速度。
- Release 時恢復原本 inverse mass。
- Animation Event 可呼叫 Grab／Release。

它代表「互動能力擴充」，而不是另一套 Cloth 系統。

### 5.7 SolverManagerAccess

目前定位：

- 集中處理原版 Solver 未公開但擴充層必要的少量資料。
- 隔離 Reflection 與版本相容契約。
- 原版升級後提供單一驗證位置。

它不是任意繞過封裝的入口。只有無公開 API、且確實屬於擴充能力所需的資料才能加入。

---

## 6. 能力選擇準則

新增功能前依序詢問：

1. ParticleSystem 是否已經提供？
2. 是否可以用 ParticleSystem Module 或 Renderer 設定完成？
3. 是否可以用 ParticleSystem C# Job 補足？
4. 是否真的需要 GPU 粒子互撞或結構 Constraint？
5. 粒子狀態的唯一權威是 ParticleSystem 還是 Solver？
6. 功能應是一次性 Bridge、附加互動元件，還是 Solver Native Structure？
7. 是否正在重做 ParticleSystem 已成熟的 Inspector 工作流？

若第 7 項答案為「是」，應停止實作並重新檢查責任邊界。

---

## 7. 近期執行階段

### Phase 0：鎖定責任邊界

- 將本文件作為 ParticleSystem 與 Solver 的架構入口。
- 新功能必須標記使用 Mode A、B、C、D 或 E。
- 現有 `ParticleSystemSolverBridge` 保持單向轉移。
- 暫停擴充自製 ParticleSystem Emission、Lifetime 與通用 Renderer UX。

驗收：

- 每個 Runtime 元件都能說明自己的狀態權威與合作模式。

### Phase 1：ParticleSystem + Burst 粒子互撞可行性

建立最小原型：

- 單一 ParticleSystem。
- 統一粒子半徑。
- CPU Spatial Hash。
- Burst Job 粒子 Pair。
- 簡單位置與速度修正。

測試：

- 1,000 粒子。
- 5,000 粒子。
- 10,000 粒子。
- 目標硬體與實際 Fixed Timestep。

決策：

- 若效能足夠，普通互撞粒子維持 ParticleSystem。
- 若效能不足，才進入 Mode D GPU Proxy 實驗。

### Phase 2：統一 Bridge 契約

- 將 Trigger 轉換視為一種 Ownership Transfer。
- 明確定義 Spawn、Accepted、Rejected、Source Removal。
- 保持 Local、World、Custom Simulation Space 轉換。
- 不加入每幀 GPU Readback。
- 為未來其他事件來源保留共用 Spawn Request API。

可能來源：

- ParticleSystem Trigger。
- Collision Event。
- Gameplay Event。
- Animation Event。
- Pool 或批次生成請求。

### Phase 3：Solver Lifecycle 與回收

只有以下需求成立時才進行：

- 長時間持續生成與死亡。
- Mode D GPU Proxy。
- Soft Body 實體需要建立與銷毀。

需要設計：

- Alive flag。
- Free List。
- Instance generation/version。
- Particle、Constraint、Rigid Body slot 回收。
- 防止失效 ID 被 Shader 或 Modifier 使用。

### Phase 4：GPU Proxy 技術驗證

只有 Phase 1 證明 CPU/Burst 不足才執行。

最小驗證範圍：

- Mesh ParticleSystem。
- Custom1 solverSlot。
- Shader 讀 Solver Buffer。
- Position／Rotation 由 Solver 控制。
- Color／Size／Lifetime 保留 ParticleSystem 控制。
- 出生、死亡與 slot recycling。
- Bounds 與 Culling。

不在第一版支援：

- Trails。
- Sub Emitters。
- ParticleSystem Collision／Trigger 跟隨 Solver Position。
- 所有 Movement Module 的語意相容。

---

## 8. 長期能力方向：Soft Body

Soft Body 是 Solver Native Structure，先記錄目標，不進入近期解耦與實作。

### 8.1 瑜珈球

可能需要：

- Surface particle topology。
- Volume preservation。
- Pressure constraint。
- Contact 與摩擦。
- 局部凹陷後的形狀恢復。

### 8.2 氣球

可能需要：

- 薄膜拉伸。
- 內部 Pressure。
- 可變 Volume。
- 洩氣、破裂與 Constraint failure。
- 與外部物體和其他氣球接觸。

### 8.3 動物 Soft Tissue

可能需要：

- 骨架或剛體作為內層驅動。
- Muscle layer 主動收縮。
- Fat layer 被動形變與阻尼。
- Skin surface 與內層 Volume coupling。
- 不同材料的 Compliance、Damping 與 Mass。

### 8.4 肌肉與脂肪互動

可能方向：

```text
Skeleton／Rigid Frame
        ↓
Active Muscle Constraints
        ↓
Passive Fat Volume
        ↓
Skin Surface
```

主要風險：

- Topology 與 Skinning 資料複雜。
- 多層 Constraint 的穩定性。
- 接觸與自碰撞成本。
- Unity Mesh／Animation／Solver 之間的資料所有權。
- Editor Authoring 工具工程量大。

在進行這些功能前，必須先完成：

- Solver Lifecycle 與回收。
- 通用 Instance Mapping。
- Constraint 分類與 Profile 契約。
- 穩定的 Renderer／Skin coupling。
- 明確的 Authoring 資料來源。

---

## 9. 非目標

本計畫目前不做：

- 複製完整 ParticleSystem Inspector。
- 複製所有 ParticleSystem Module。
- 讓 Compute Shader 直接修改 Unity 未公開的 ParticleSystem 內部 Buffer。
- 每幀 GPU Readback 後再呼叫 `ParticleSystem.SetParticles()`。
- 讓 ParticleSystem 與 Solver 同時模擬同一份 Position／Velocity。
- 為每種用途建立新的 Generator、Renderer 或 Compute Shader。
- 立即完成通用 Soft Body、肌肉或脂肪系統。
- 修改原版 Unified Solver Runtime 原始碼。

---

## 10. 驗收原則

### 避免重造輪子

- 一般特效可完全不依賴 Solver。
- ParticleSystem 已有功能不在擴充層重新實作。
- 新增 Solver 功能前先評估 ParticleSystem Job。

### 資料一致性

- 每項狀態只有一個權威來源。
- Bridge 採一次性 Ownership Transfer。
- 不依賴每幀 GPU → CPU Readback 維持雙向同步。

### 效能

- 粒子 Pair 建立必須使用 Spatial Hash 或等效 Broad Phase。
- CPU 方案使用 Job／Burst 並以實機數據決定是否升級 GPU。
- GPU Modifier 與 Renderer 保持批次處理。
- 長時間生成前先完成 slot 回收。

### 可維護性

- 原版 Solver 保持唯讀。
- private 相容存取集中於 `SolverManagerAccess`。
- 新功能明確標記合作 Mode。
- Soft Body 在資料契約穩定前保持研究目標。

---

## 11. 下一個建議任務

下一個實作任務建議為：

```text
ParticleSystem Burst Particle Collision Prototype
```

範圍只包含：

- 一個 ParticleSystem。
- 同半徑球形粒子。
- Spatial Hash。
- 粒子彼此碰撞。
- Profiler 與 1,000／5,000／10,000 粒子測試。

原型完成後再決定：

```text
CPU/Burst 足夠
    → 保留完整 ParticleSystem 工作流

CPU/Burst 不足
    → 開始 GPU Proxy Renderer 技術驗證
```

在取得這項效能證據前，不擴充通用 ParticleSystem 替代工作流。
