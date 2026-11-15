# ParticleSolver：粒子求解

> 規格版本 1.2.0 · 2026-11-15 · 目標已確認；Motion package 的 GPU 實作已通過全部 23 個案例（第 3 階段）；與 Unified-Solver 的網子下垂與擋魚對照已執行，結果記在 Motion repo 的遷移計畫
>
> 1.0.1 補上〈來源與致謝〉；1.0.2 補上 32 位元浮點數的精度說明與剛體群旋轉的初值，行為不變。案例檔升為 1.1.0：剛體群案例改以配置姿勢放置，三個案例的起點、子步驟與容差依精度說明調整。
>
> 1.1.0 修正 SOLVER-01 的自相矛盾：原文「以步驟開始時的位置建立鄰近結構」與同一條的「結果不得因格子大小而改變」衝突；粒子一步移動超過一格時會漏掉接觸（與 Unified-Solver 對照時，10 m/s 落下的魚有一半粒子嵌進網子）。改為每個子步驟在預測後重建。案例檔升為 1.2.0，新增 PSOLVE-CASE-22。
>
> 1.2.0 新增世界設定 contactWeight（使用者 2026-11-15 決定）：與 Unified-Solver 對照時，每對接觸只處理一次讓接觸在平均中的比重只有第三方求解器的一半，20 m/s 的魚會穿過網子。contactWeight 預設 1（行為不變），設 2 等同第三方求解器的雙邊處理。案例檔升為 1.3.0，新增 PSOLVE-CASE-23。
>
> 與其他文件的關係：本文件規定粒子的數值行為；粒子怎麼配置、登記、釋放，以及擴充行為掛在哪個階段，由 [ParticleWorld](ParticleWorld.md) 規定。解析解案例在 [ParticleSolverCases.json](ParticleSolverCases.json)，由 [generate_particle_solver_cases.py](generate_particle_solver_cases.py) 依本文件的公式產生。遷移順序與擁有權決定見 Motion repo 的 `Documentation/GpuPhysicsMigrationPlan.md`。

## 系統階層與關係

```text
Motion package（com.yu5h1.motion）
└─ 粒子物理
   ├─ ParticleWorld：場景中的世界、配置與釋放、登記、擴充階段、讀回
   │  └─ 每個固定步驟呼叫一次求解
   └─ ParticleSolver（本文件）：一個固定步驟內的數值行為
      ├─ 子步驟管線：預測 → 距離約束與接觸 → 平均套用 → 碰撞形狀 → 剛體群 → 速度更新
      ├─ 碰撞形狀：地面、球、膠囊、方塊；碰撞分類；移動碰撞體的表面速度
      └─ 介質受力：步後階段
```

ParticleSolver 只讀寫粒子、約束、剛體群、碰撞體與介質的數值狀態。它不知道 GameObject、Unity layer、Timeline 或繪製；這些在 ParticleWorld 與宿主。

## 定義

ParticleSolver 是批量粒子的位置式動力學求解：每顆粒子帶位置、前一位置、速度、反質量與半徑，在固定步驟內以多個子步驟推進，並滿足距離約束、粒子間接觸、碰撞形狀與剛體群。它的範圍到「一個步驟後粒子在哪裡、速度多少」為止。

## 目標

- **為誰服務**：BonghuoVR 的捕撈表演（目前唯一的消費者）：捕撈網、魚群、水管、介質、打撈勺與甲板。求解來源是 Unified-Solver 套件依賴的第三方求解器；本規格以它的算法為基礎，改掉三個非標準算法（見〈與第三方求解器的差異〉）。
- **情境**：
  - 自由落體、阻尼與釘住的粒子符合離散公式（預測（SOLVER-02）、速度更新與阻尼（SOLVER-16））。
  - 網子與繩索以距離約束連結，柔度、阻尼與斷裂可預測，斷掉後保持斷開（距離約束（SOLVER-03）、約束斷裂（SOLVER-04））。
  - 魚與網、魚與魚以粒子半徑互相推開，同群組可設為不互碰（粒子間接觸（SOLVER-06）、群組互斥（SOLVER-08））。
  - 甲板、勺斗與擋牆以球、膠囊、方塊推出粒子；勺斗可依分類只擋魚、不擋網（碰撞分類（SOLVER-13））；移動中的勺斗與甲板能以摩擦帶動東西（移動碰撞體的表面速度（SOLVER-14））。
  - 冰塊碎片以剛體群保持形狀（剛體群（SOLVER-15））。
  - 魚與碎片在水中浮沉並被水流帶走（介質受力（SOLVER-17））。
- **不在此目標**：
  - 撰寫用的 profile（佈局、互動、外觀）：第 2 階段，依 Unified-Solver 的 `PhysicsParticle.md` 設計另寫。
  - 魚的游動、轉向、驚嚇等表演行為：它們是擴充行為，掛在 ParticleWorld 的擴充階段。
  - 長鏈收斂的改善（tether 長距離約束、更多迭代）：BonghuoVR 抓網方案仍待決。
  - 連續碰撞偵測、凹形或網格碰撞體：目前形狀只有地面、球、膠囊、方塊。
  - 與 Unity PhysX 雙向耦合、流體、執行期碎裂。
  - 跨平台位元一致：只要求在容差內一致。
- **完成條件**：Motion package 的實作通過 [ParticleSolverCases.json](ParticleSolverCases.json) 全部案例，並在 Motion package 的測試場景中重現 BonghuoVR 的網、魚、水管、勺斗情境，達到 BonghuoVR 需求 1–5 的驗收條件。
- **狀態**：2026-11-14 目標由使用者確認；尚無實作，目標未達成。

## 單位與座標

公尺、秒、公斤。座標為 Unity 世界座標（左手系，Y 朝上）。重力預設 (0, −9.81, 0) m/s²。所有長度、半徑、位置都是世界座標的公尺。

數值以 32 位元浮點數計算。速度由 (x − p)/h 重建（SOLVER-16），所以每個子步驟的速度誤差約為 ulp(|x|)/h：離原點越遠、子步驟越短，誤差越大；穿透量很小時，由兩個相近座標相減得到的穿透量相對誤差也會變大。容差的訂定與案例設計都以此為依據；需要在遠離原點處精確模擬的場景，應把世界座標原點移近模擬區域。

## 狀態與資料

| 資料 | 欄位 | 說明 |
|---|---|---|
| 粒子 | 位置 x、前一位置 p、速度 v | 每個子步驟開始時 p 記錄預測前的 x |
| | 反質量 w | w = 1/質量；w = 0 表示釘住，不受預測、碰撞、速度更新影響 |
| | 半徑 r | 每顆粒子各自的半徑，r > 0 |
| | 群組 group | 整數；0 表示不屬於任何群組 |
| | 分類 category | 32 位元；預設 1（第 0 位） |
| 距離約束 | 粒子 a、b，靜止長度 L，柔度 compliance（m/N），阻尼 damping，斷裂力 breakForce（N），是否已斷 | breakForce = 0 表示不會斷 |
| 剛體群 | 成員粒子、靜止偏移 q_i、目前質心與旋轉 | 靜止偏移由建立時的佈局決定 |
| 碰撞體 | 形狀與姿勢、碰撞對象 mask（32 位元，預設全部位元）、表面速度 | 形狀推導與登記見 ParticleWorld |
| 介質 | 區域、密度 ρ（以 profile 質量為單位）、流速 flow、作用於粒子的拖曳係數 Cd | 區域幾何由宿主提供 |

### 世界設定與預設值

| 設定 | 預設 | 範圍 |
|---|---|---|
| 子步驟數 substeps | 30 | ≥ 1 的整數 |
| 重力 gravity | (0, −9.81, 0) | 任意有限向量 |
| 全域阻尼 damping | 0 | ≥ 0（1/s） |
| 平均放大係數 sor | 1.0 | 1～2 |
| 接觸權重 contactWeight | 1.0 | 1～2 |
| 靜摩擦 μs | 0.4 | ≥ 0 |
| 動摩擦 μk | 0.2 | ≥ 0 |
| 最大去穿透速度 maxDepenetrationSpeed | 5 m/s | > 0 |
| 地面高度 groundY | 0 | 任意有限值 |

## 步驟管線

宿主每個固定步驟提供 dt（Unity 為 `Time.fixedDeltaTime`）。子步驟長度 h = dt / substeps。

### 步驟與子步驟順序（SOLVER-01）

- **規則**：一個步驟依序做：(1) 執行 substeps 次子步驟；(2) 執行步後擴充階段（介質受力在此，見 SOLVER-17）。
- **每個子步驟的順序**：預測（SOLVER-02）→ 以預測後的位置重建鄰近結構 → 清空累加 → 距離約束（SOLVER-03）→ 粒子間接觸（SOLVER-06、07）→ 平均套用（SOLVER-05）→ 地面（SOLVER-09）→ 所有球（SOLVER-10）→ 所有膠囊（SOLVER-11）→ 所有方塊（SOLVER-12）→ 剛體群（SOLVER-15）→ 速度更新（SOLVER-16）。子步驟內的擴充掛勾點由 ParticleWorld 定義。
- **迭代**：每個子步驟中，距離約束與接觸各解一次。
- **鄰近結構**：只用來找接觸候選；格子邊長不得小於 2 × 最大粒子半徑。每個子步驟以預測後的位置建立，並以粒子 i 的目前位置查詢它周圍的格子；結果不得因格子大小、粒子速度或雜湊碰撞而改變，同一候選在同一子步驟只處理一次。只在步驟開始建立一次時，一步移動超過一格的粒子會漏掉接觸。
- **邊界**：dt ≤ 0 或非有限值時整個步驟不做任何事（SOLVER-18）。
- **驗證**：PSOLVE-CASE-01、PSOLVE-CASE-22（步驟開始時不在鄰格、步驟中途才接觸）。

### 預測（SOLVER-02）

- **規則**：對 w > 0 的粒子：p ← x；v ← v + h·gravity；x ← x + h·v。
- **釘住**：w = 0 的粒子在整個子步驟中位置、前一位置、速度都不變。
- **外力**：預測只加重力；其他力由擴充階段或介質寫入速度。
- **驗證**：PSOLVE-CASE-01、03。

### 距離約束（SOLVER-03）

- **規則**：以預測後的位置計算（所有約束讀同一份位置，修正先累加，見 SOLVER-05）。對未斷的約束 (a, b)：

  ```text
  d     = x_b − x_a,  dist = |d|,  n = d / dist
  C     = dist − L
  α̃     = compliance / h²
  γ     = compliance · damping / h
  Δλ    = (−C − γ · n·((x_b − p_b) − (x_a − p_a))) / ((1 + γ)(w_a + w_b) + α̃)
  δ_a   = −w_a · Δλ · n        δ_b = +w_b · Δλ · n
  ```

  δ_a、δ_b 各計入一次累加，a、b 的計數各加 1。
- **略過**：約束已斷、w_a + w_b = 0、dist < 1e-7 時不產生修正。
- **柔度為 0**：γ = 0，阻尼不起作用；這是 XPBD 的定義，不是例外。
- **靜止長度**：建立約束時未指定 L，則取建立當下兩顆粒子目前位置的距離（ParticleWorld 規定「目前位置」指模擬中的位置，不是出生時的位置）。
- **驗證**：PSOLVE-CASE-05、07、08。

### 約束斷裂（SOLVER-04）

- **規則**：breakForce > 0 且約束力 |Δλ| / h² > breakForce 時，約束標為已斷，本次不產生修正。
- **持續**：已斷的約束之後一律略過，直到 ParticleWorld 明確重置或釋放它；重新上傳資料、其他粒子的新增或釋放都不會讓它復原。
- **驗證**：PSOLVE-CASE-09、10。

### 平均套用（SOLVER-05）

- **規則**：一個子步驟中，距離約束與接觸（含摩擦）的所有修正先對每顆粒子累加總和 Σδ 與計數 count；全部計算完才套用：對 w > 0 且 count > 0 的粒子，x ← x + sor · Σδ / count。距離約束的修正以權重 1 計入（Σδ 加 δ、count 加 1）；接觸與接觸摩擦以權重 contactWeight 計入（Σδ 加 contactWeight · δ、count 加 contactWeight）。
- **contactWeight**：預設 1.0。調高時接觸在與約束一起平均時占比較重，網子比較擋得住高速的物體；設 2 等同第三方求解器每對從兩邊各處理一次的結果。
- **sor**：預設 1.0，此時單一修正剛好補到位；設大於 1 時在多個修正平均後放大。
- **精度**：累加可用定點數，但解析度不得粗於 1e-5 m；總和不得因累加順序而改變。
- **驗證**：PSOLVE-CASE-05、06、23。

### 粒子間接觸（SOLVER-06）

- **規則**：對每一對不同粒子 (i, j)（每對無序粒子在一個子步驟只處理一次），以預測後的位置：

  ```text
  d   = x_i − x_j,  dist = |d|,  R = r_i + r_j
  接觸條件：dist² < R² 且 dist² > 1e-10，且不被群組互斥（SOLVER-08）
  pen = min(R − dist, maxDepenetrationSpeed · h)
  n   = d / dist,  s = pen / (w_i + w_j)
  δ_i = +n · s · w_i        δ_j = −n · s · w_j
  ```

  δ_i、δ_j 各以權重 contactWeight 計入累加（SOLVER-05）。w_i + w_j = 0 時略過。
- **驗證**：PSOLVE-CASE-11、12、14。

### 接觸摩擦（SOLVER-07）

- **規則**：接觸成立時，接著算相對切向位移：

  ```text
  Δrel = (x_i − p_i) − (x_j − p_j)
  Δt   = Δrel − (Δrel·n) n,  len = |Δt|
  len > 1e-7 時：
    limit = (len < μs · pen) ? len : μk · pen     （pen 為 SOLVER-06 取 min 之後的值）
    m     = min(len, limit)
    f_i   = −(Δt / len) · m · w_i / (w_i + w_j)
    f_j   = +(Δt / len) · m · w_j / (w_i + w_j)
  ```

  f_i、f_j 各以權重 contactWeight 計入累加（SOLVER-05）。
- **驗證**：靜摩擦與動摩擦的粒子對粒子案例尚無；碰撞體摩擦由 PSOLVE-CASE-18 驗證同一條 μs／μk 規則。

### 群組互斥（SOLVER-08）

- **規則**：group_i ≠ 0 且 group_i = group_j 時，這一對不接觸、無摩擦。group 為 0 的粒子與所有粒子都可接觸，包括其他 group 0 的粒子。
- **用途**：一條魚、一個冰塊、一張不開自身碰撞的網，各用一個非零群組。
- **驗證**：PSOLVE-CASE-13。

## 碰撞形狀

碰撞形狀在平均套用之後直接修正位置，不經累加。各形狀依序處理：地面 → 球 → 膠囊 → 方塊；同一類型依 ParticleWorld 規定的登記順序逐一處理，後處理的看見前一個的結果。w = 0 的粒子略過。每個碰撞體先做碰撞分類檢查（SOLVER-13）。

球、膠囊、方塊共用同一套推出與摩擦：

```text
找出最近點 c 與外法向 n、穿透量 pen（形狀各自定義，見下）
Δ    = x − p                              （在修正位置之前擷取）
x    ← c + n · r
若 (p − c)·n < r：p ← p + (r − (p − c)·n) · n     （去掉朝內的速度）
Δrel = Δ − u · h                          （u 為碰撞體在 c 的表面速度，SOLVER-14）
Δt   = Δrel − (Δrel·n) n,  len = |Δt|
len > 1e-7 時：limit = (len < μs·pen) ? len : μk·pen；x ← x − (Δt/len)·min(len, limit)
```

### 地面（SOLVER-09）

- **規則**：地面是 y = groundY 的水平面，永遠存在。若 x.y < groundY + r：pen = groundY + r − x.y；x.y ← groundY + r。之後才取 Δ = x − p 的 xz 分量做摩擦（同一條 μs／μk 規則）。地面不改前一位置，表面速度為 0。
- **驗證**：PSOLVE-CASE-04。

### 球（SOLVER-10）

- **規則**：球心 C、半徑 R。dist = |x − C|，當 dist < R + r 且 dist > 1e-7：n = (x − C)/dist，pen = R + r − dist，最近點 c = C + n·R，之後套共用推出與摩擦（推出後 x = C + n·(R + r)）。
- **驗證**：PSOLVE-CASE-15。

### 膠囊（SOLVER-11）

- **規則**：線段 A→B、半徑 R。先取線段上離 x 最近的點 Q（t = clamp(((x − A)·(B − A)) / |B − A|², 0, 1)；|B − A|² < 1e-10 時 Q = A），再把 Q 當球心，照球的規則處理。
- **驗證**：尚無案例；與球共用推出規則。

### 方塊（SOLVER-12）

- **規則**：有向方塊，中心 C、半邊長 e、單位軸 X、Y、Z。把 x 轉到方塊局部座標 l，clamp 到 ±e 得 k，差 dl = l − k。
  - **在外部**（|dl| > 1e-7）：若 |dl| ≥ r 則不接觸；否則 n = dl/|dl| 轉回世界座標，c = k 轉回世界座標，pen = r − |dl|。
  - **在內部**：各軸到面的距離 e − |l|，取最小者為推出方向（並列時依 x、y、z 的順序），n 為該軸往 l 同側的方向，pen = r + 該距離，c 為該面上的點。
  - 之後套共用推出與摩擦。
- **驗證**：PSOLVE-CASE-17、18。

### 碰撞分類（SOLVER-13）

- **規則**：粒子的 category 與碰撞體的 mask 做位元 AND，結果為 0 時這個碰撞體不影響這顆粒子。地面也有 mask，預設全部位元。
- **預設**：category = 1、mask = 全部位元，所有粒子與所有碰撞體都會碰撞。
- **生效時機**：在子步驟內，與推出同時判斷；球、膠囊、方塊、地面都適用。
- **對應 Unity layer**：由 ParticleWorld 與宿主決定（WORLD-11）。
- **驗證**：PSOLVE-CASE-16。

### 移動碰撞體的表面速度（SOLVER-14）

- **規則**：碰撞體在一個步驟內維持步驟開始時的姿勢。表面速度 u 由上一步與這一步的姿勢差除以 dt 取得：u(c) = 線速度 + 角速度 × (c − 中心)。摩擦以粒子相對於表面的位移計算（共用規則的 Δrel），所以移動中的碰撞體會以 μs／μk 帶動粒子。
- **第一步**：碰撞體剛啟用或被傳送的那一步，表面速度為 0。
- **只影響摩擦**：法向推出仍以推出位置與前一位置的修正為準，不另外加法向速度。
- **驗證**：PSOLVE-CASE-18（靜止方塊時粒子不動，移動方塊時以 μk·|g| 加速）。

## 剛體群（SOLVER-15）

- **規則**：在碰撞形狀之後，對每個剛體群：
  1. 以 m_i = 1/w_i 加權求目前質心 x_cm（w = 0 的成員權重為 0）；總質量 < 1e-9 時略過。
  2. A 的第 k 欄 = Σ m_i · q_i[k] · (x_i − x_cm)。
  3. 從上一次的旋轉 R（以四元數保存，初值為單位四元數）起算，迭代 20 次：ω = Σ_k (R 第 k 欄 × A 第 k 欄) / (|Σ_k R 第 k 欄 · A 第 k 欄| + 1e-12)；|ω| < 1e-12 時停止；否則 R ← 繞 ω/|ω| 轉 |ω| 的旋轉 × R，並正規化。
  4. 對 w > 0 的成員：x_i ← R·q_i + x_cm。保存 R 與 x_cm。
- **初值**：剛體群的旋轉初值是實體配置姿勢的旋轉；實體被傳送時，旋轉隨傳送一起轉動，被重置時回到重置姿勢的旋轉（ParticleWorld 傳送與重置（WORLD-07））。
- **依賴上一次的旋轉**：旋轉萃取從上一次的結果起算，單一子步驟內的旋轉變化很小時才保證在 20 次內收斂；從單位四元數起算 90° 的差距不會收斂。
- **順序**：剛體群在碰撞形狀之後執行，可能把成員推回碰撞體內，下一個子步驟再推出。
- **驗證**：PSOLVE-CASE-19。

## 速度更新與阻尼（SOLVER-16）

- **規則**：對 w > 0 的粒子：v ← (x − p) / h；v ← v · max(0, 1 − damping · h)。
- **無**速度上限或睡眠；這兩者是擴充行為（ParticleWorld 的擴充階段與 WORLD-10）。
- **驗證**：PSOLVE-CASE-01、02。

## 介質受力（SOLVER-17）

- **規則**：在步後階段、以整步的 dt 執行。對每顆 w > 0 的粒子，依介質的登記順序，對每個包含粒子位置的介質：

  ```text
  V     = (4/3) π r³                 （這顆粒子的體積）
  ratio = ρ · V · w                  （介質密度 / 粒子密度）
  v     ← v − gravity · ratio · dt     （浮力）
  b     = 1 − exp(−ratio · Cd · dt)
  v     ← v + (flow − v) · b           （向流速收斂）
  ```

- **重疊**：重疊的介質依序作用，每個都用前一個作用後的速度。
- **只寫速度**：介質不改位置；下一步的預測使用新的速度。
- **介質狀態輸出**：沒入比例、與流速的差、淨加速度、平均密度比，是擴充行為（游動、睡眠）的輸入，由 ParticleWorld 的擴充階段規定，不在本規格。
- **驗證**：PSOLVE-CASE-20、21。

## 無效輸入（SOLVER-18）

- dt ≤ 0、dt 非有限值、substeps < 1：步驟不做任何事。
- 粒子半徑 ≤ 0、反質量 < 0、非有限的位置或速度：ParticleWorld 在配置時拒絕（WORLD-04），求解不處理。
- 約束兩端是同一顆粒子：配置時拒絕。

## 來源與致謝

本規格的算法基礎是 Johann Hotzel 的 [unified-solver](https://github.com/JohannHotzel/unified-solver)（MIT 授權，Copyright (c) 2026 Johann），Unified-Solver 套件以它為依賴擴充。本規格整理自對它的行為研讀；Motion package 的實作依本規格重新撰寫，不複製其原始碼。所用方法出自下列公開研究：

- 位置式動力學與柔度約束：Macklin, Müller, Chentanez，〈XPBD: Position-Based Simulation of Compliant Constrained Dynamics〉，MIG 2016。
- 統一粒子表示與約束平均（Jacobi 平均與 SOR）：Macklin, Müller, Chentanez, Kim，〈Unified Particle Physics for Real-Time Applications〉，SIGGRAPH 2014。
- 剛體群的形狀匹配：Müller, Heidelberger, Teschner, Gross，〈Meshless Deformations Based on Shape Matching〉，SIGGRAPH 2005。
- 旋轉萃取：Müller, Bender, Chentanez, Macklin，〈A Robust Method to Extract the Rotational Part of Deformations〉，MIG 2016。
- 鄰近搜尋的空間雜湊：Teschner, Heidelberger, Müller, Pomeranets, Gross，〈Optimized Spatial Hashing for Collision Detection of Deformable Objects〉，VMV 2003。

## 與第三方求解器的差異

本規格以上述第三方求解器為算法基礎。下列項目刻意不同，使用者 2026-11-14 決定改成標準做法（BonghuoVR 繼續使用 Unified-Solver，不受影響）：

| 項目 | 第三方求解器 | 本規格 |
|---|---|---|
| 平均套用 | 單一修正也乘 1.5 | sor 預設 1.0，單一修正剛好補到位（SOLVER-05） |
| 粒子間接觸 | 每對從兩顆粒子各處理一次，修正與計數加倍 | 每對只處理一次（SOLVER-06）；要相同的接觸比重時設 contactWeight = 2（SOLVER-05） |
| 斷裂判定 | \|Δλ\| / h | 約束力 \|Δλ\| / h²，單位為牛頓（SOLVER-04） |
| 已斷約束 | 重新上傳資料時復原 | 保持斷開（SOLVER-04） |
| 雜湊碰撞 | 同一候選可能被處理多次 | 只處理一次（SOLVER-01） |
| 碰撞體運動 | 摩擦只看粒子自己的位移，移動碰撞體帶不動東西 | 以表面速度計算相對位移（SOLVER-14） |
| 碰撞分類 | 無 | category 與 mask（SOLVER-13） |
| 粒子半徑 | 全域一個 | 每顆粒子各自（SOLVER-06、WORLD-11） |

把 BonghuoVR 的網子與魚改用本實作時，compliance、阻尼與斷裂力需要重調；這發生在 BonghuoVR 決定切換時，不在本規格。

## 驗收與案例

案例檔 [ParticleSolverCases.json](ParticleSolverCases.json) 的每個案例列出世界設定、初始粒子、約束、碰撞體、剛體群、介質、步數與預期值；比對規則寫在檔案的 `comparison` 欄。預期值由 [generate_particle_solver_cases.py](generate_particle_solver_cases.py) 依本文件的公式以解析解或封閉遞推算出，不經任何實作；每個案例的 `derivation` 寫出推導。實作的重播在 Motion package 的 `Tests/Editor/Particles/ParticleSolverCaseTests.cs`。

| 行為 | 案例 | 尚未覆蓋 |
|---|---|---|
| 步驟與子步驟順序（SOLVER-01） | 01、22 | — |
| 預測（SOLVER-02） | 01、03 | — |
| 距離約束（SOLVER-03） | 05、07、08 | 約束阻尼不為 0 且柔度不為 0 |
| 約束斷裂（SOLVER-04） | 09、10 | 斷後經資料重新上傳仍斷開（屬 ParticleWorld 平台檢查） |
| 平均套用（SOLVER-05） | 05、06、23 | — |
| 粒子間接觸（SOLVER-06） | 11、12、14、22、23 | — |
| 接觸摩擦（SOLVER-07） | — | 粒子對粒子的靜摩擦與動摩擦 |
| 群組互斥（SOLVER-08） | 13 | — |
| 地面（SOLVER-09） | 04 | 地面摩擦 |
| 球（SOLVER-10） | 15 | — |
| 膠囊（SOLVER-11） | — | 全部 |
| 方塊（SOLVER-12） | 17、18 | 外部接觸、旋轉的方塊 |
| 碰撞分類（SOLVER-13） | 16 | — |
| 移動碰撞體的表面速度（SOLVER-14） | 18 | 旋轉碰撞體的角速度項 |
| 剛體群（SOLVER-15） | 19 | — |
| 速度更新與阻尼（SOLVER-16） | 01、02 | — |
| 介質受力（SOLVER-17） | 20、21 | 重疊介質 |
| 無效輸入（SOLVER-18） | — | 全部 |

實作以 EditMode 測試讀取案例檔、透過 Motion package 的公開入口建立世界並執行，再以非同步讀回取得結果比對；轉接層不得自行計算預期值或略過不支援的動作。實際結果記在 Motion package 的驗收紀錄，不寫回本文件。

## 版本

規格版本、案例檔版本（`version` 欄）與實作版本分開記錄。改變任何公式或預設值是新的次版本或主版本，並同時更新案例檔與推導腳本。
