# Yu5h1 Unified Solver UPM Extension

這是一個建立在原作者 `unified-solver` 上的非侵入式 UPM 擴充套件。

原作者將 Solver 放在完整 Unity Project 的 `Assets/unified-solver` 中，沒有提供可直接引用的 UPM package。為了保持原始 Runtime 原始碼不變，本套件不直接修改 `SolverManager` 或 `ClothGenerator`，而是透過集中式相容橋接層存取必要狀態。

## 安裝前置需求

本套件目前不包含原作者的 Solver 原始碼，因此不是 self-contained package。使用者必須自行取得相容版本的原作者 Unity Project。

乾淨安裝流程：

1. Clone 或下載本 `UnityExtension` repository。
2. Clone 或下載原作者的完整 Unity Project。
3. 從原作者 Project 複製：
   - `Assets/unified-solver/Runtime`
     到本套件的
     `Runtime/Dependencies/unified-solver/Runtime`
   - `Assets/unified-solver/Editor`
     到本套件的
     `Editor/Dependencies/unified-solver/Editor`
4. 在 Unity Package Manager 選擇 **Add package from disk**，並指定本目錄的 `package.json`。

`Runtime/Dependencies` 與 `Editor/Dependencies` 不由本 repository 追蹤；每次乾淨 clone 後都必須重新放入相依原始碼。

不要同時在 Unity Project 的 `Assets` 下保留另一份相同的 Solver 原始碼，否則 `SolverManager`、`ClothGenerator` 等 global type 會重複定義。

## 已驗證的原版

目前相容橋接以以下原版檔案為基準：

- `SolverManager.cs` SHA-256  
  `4E902F723AF3B6C6D2640683A517340F24D12651BC328EBE49C5C24A27992483`
- `ClothGenerator.cs` SHA-256  
  `EF927603C0D7A9A9B7A118FA7C0EBC4420AC02B6EB178548615F8142E744566B`

若原作者更新過這兩個檔案，請先執行 `SolverManagerAccessTests`，不要假設 private field contract 仍然相容。

## 原版相容邊界

原版 `SolverManager` 沒有公開剛體 GPU Buffer 與 rigid particle reference count，原版 `ClothGenerator` 也沒有公開 particle range。擴充層只允許 `SolverManagerAccess` 集中讀取以下 private 欄位：

- `_rigidBodyBuffer`
- `_rigidParticleIndexBuffer`
- `_rigidParticleRefCount`
- `ClothGenerator._particleOffset`

Emitter、Renderer、ClothAnchor 與 ClothGrabber 不直接依賴欄位名稱。橋接契約不相容時，相關操作會停止並輸出明確錯誤。

`Runtime/link.xml` 會在 IL2CPP 建置保留必要欄位 metadata。

## Runtime 元件

- `SolverParticleEmitter`
  - 依 Profile 批次或動態生成 Solver Particle Instance。
- `SolverMeshRenderer`
  - 共用元件，內部選擇 Rigid 或 Articulated Shader。
- `SolverParticleModifierRunner`
  - 批次執行 Oscillation 與 Surface Impulse。
- `ParticleSystemSolverBridge`
  - 將 ParticleSystem Trigger Enter 粒子轉成 Solver Instance。
- `ClothAnchor`
  - 將指定 Cloth 節點固定到 Transform。
- `ClothGrabber`
  - 在 GPU 上選取、拖曳與釋放 Cloth 節點。

## 建立第一個 Chain3 Profile

1. 建立 `Solver Render Profile`。
2. 指定 Mesh、Material。
3. `Mesh Mode` 設為 `Articulated`。
4. 建立 `Solver Particle Profile`。
5. `Topology` 設為 `Chain3`。
6. 指定剛才建立的 Render Profile。
7. 在 GameObject 加入：
   - `SolverParticleEmitter`
   - `SolverMeshRenderer`
   - `SolverParticleModifierRunner`（需要主動行為時）
8. 將 Particle Profile 指定給 Emitter。

## Oscillation 身體彎曲

`SolverOscillationProfile` 會驅動三段身體形成左右交替的 C 型彎曲：

- `Bend Ratio`：中節相對頭尾中點的目標偏移，占魚身長度的比例。`0` 不彎曲，三控制點拓樸的幾何上限是 `0.5`。
- `Bend Randomness`：每個 Instance 的彎曲比例差異。
- `Frequency` / `Frequency Randomness`：拍動頻率及每個 Instance 的頻率差異。
- `Acceleration`：讓粒子速度跟上形變速度時可使用的最大加速度；不再限制可見彎曲幅度。
- `Direction Angle` / `Direction Randomness`：彎曲平面及每個 Instance 的方向差異。

每個 Instance 已有固定的隨機相位，因此同一時間會自然分布在 `+X`、`-X` 與過渡狀態。三個縱向控制點只能形成左右 C 型；真正包含反曲點的 S 型需要至少四個縱向控制點或額外的 Shader 空間波形。

Oscillation 在 Solver 約束完成後投影主動彎曲形狀，並在縮短頭尾間距的同時保持兩段魚身長度。因此 `Frequency` 只控制每次左右擺動的時間間距，`Bend Ratio` 只控制曲度。若使用 GuideChain4 或 DualRail6，仍應給 `Bend Compliance` 足夠的柔度，避免被動距離約束在下一個 Solver step 強力拉直。

## 建立 Rigid Cluster Profile

1. Render Profile 的 `Mesh Mode` 設為 `Rigid`。
2. Particle Profile 的 `Topology` 設為 `RigidCluster4`。
3. 不需要 Modifier。
4. `SpawnRequest.scale` 會同時縮放粒子結構與 Mesh。

## ParticleSystem Trigger 轉換

1. 在 ParticleSystem 開啟 Trigger Module。
2. 將場景 Box Collider 加入 Trigger Collider 清單。
3. `Inside` 設為 `Callback`。
4. 在 ParticleSystem GameObject 加入 `ParticleSystemSolverBridge`。
5. 指定 `Target Emitter`。

Bridge 只會刪除 Emitter 已接受的 Particle。

## 更新原版後的驗證

更新 Dependencies 中的原作者原始碼後，至少執行：

1. 確認 Unity Console 沒有 script、shader 或 compute shader import error。
2. 在 Unity Test Runner 執行 `SolverManagerAccessTests`。
3. 測試 Chain3 與 RigidCluster4 的生成及渲染。
4. 測試 ClothAnchor 與 ClothGrabber。
5. 建立一次 IL2CPP Development Build，確認反射欄位 metadata 有被保留。

## 目前限制

- Solver 的 Particle Radius 仍是全域值。
- Instance 採 append-only，尚未回收。
- 不同 Emitter 同時接近容量上限時，Queue 預留不是全域原子操作。
- Renderer 使用 GPU procedural instancing，不建立每個 Instance 的 GameObject。
- 相容橋接依賴上述四個原版 private 欄位名稱與型別。
- Cloth 生成完成後，不要在執行期間修改 `resolutionX` 或 `resolutionY`。
- 原版仍會同步 readback rigid-body pose；不修改原版的前提下無法關閉這項成本。
