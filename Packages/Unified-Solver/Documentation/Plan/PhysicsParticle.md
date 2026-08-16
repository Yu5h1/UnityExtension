# PhysicsParticle：三項抽象責任與後端整合

> 狀態：**方向已完整並定案；尚未授權實作。** 本文件不含實作步驟、程式碼或排程。真的開始實作前，才把本方向轉成實作清單並定案 public Core / backend API。
>
> 與其他文件的關係：`plan.md` 記錄**現況架構與已實作的設計**；本文件是**還沒做的方向**。真的動工時，實作細節回到 `plan.md`，活的狀態回到 `handoff.md`。
>
> **已定案**：`PhysicsParticle` 是新的後端中立 MonoBehaviour，也是永遠具備批次能力的上層驅動器；舊的 `SolverParticleEmitter` 不是它的核心型別。`PhysicsParticle` 巢狀持有 **layout / interaction / appearance** 三個抽象 profile。第一項叫 `layout`，不叫 `shape`；它描述每個實體的粒子靜止佈局，不包含連結關係。三項責任不合併，layout 併進 appearance 的可能性經評估後排除，理由在 3.3。
>
> `appearance` 是「所有可見結果」的單一 profile。它內部同時引用 mesh source / binding 與 material；兩者責任仍分開，不表示 mesh 與 shader 資料被揉成同一種資料。
>
> **後端邊界**：Unified Solver 是第一個執行後端，不是 `PhysicsParticle` 的概念核心。核心不引用 `SolverManager`、Unified Solver 的 buffer layout 或相容 reflection；這些由整合元件負責，見第 4 節。

---

## 系統階層與關係

```text
場景 / Physics World
├─ SolverManager
│  └─ 擁有 Unified Solver 的模擬、buffer 與全域容量
│
└─ UnifiedSolverPhysicsParticleBackend（場景共享）
   ├─ 實作 PhysicsParticleBackend 抽象契約
   ├─ 關聯一個 SolverManager
   └─ registrations[]
      ├─ PhysicsParticle A（場景驅動器，管理 0..N 個 runtime 實體）
      │  ├─ layout : PhysicsParticleLayout
      │  │  └─ 每個實體的粒子位置、數量、索引與角色
      │  │
      │  ├─ interaction : PhysicsParticleInteraction
      │  │  ├─ particleMass
      │  │  ├─ constraint / rigid group 配方與材質
      │  │  └─ behaviors[]（有序、可重疊）
      │  │
      │  └─ appearance : PhysicsParticleAppearance
      │     ├─ meshSource / binding
      │     │  ├─ AuthoredMesh
      │     │  ├─ ArticulatedMesh
      │     │  └─ ParticleHull
      │     └─ material
      │
      ├─ PhysicsParticle B（可重用相同或改用其他 profiles）
      └─ PhysicsParticle ...
```

這張圖表示場景與執行期的關係，不改變依賴方向：`PhysicsParticle` Core 只認識抽象 backend 契約；`UnifiedSolverPhysicsParticleBackend` 位於整合層，才同時認識 Core 與 `SolverManager`。profile 描述一個實體，`PhysicsParticle` 決定實體數量，共享 backend 負責把所有已註冊驅動器的完整實體放進同一套容量與批次管理。

---

## 1. 今天的耦合在哪裡

三處，各自獨立。解耦時知道要拆的是這三個，而不是憑印象重推一次。

### 1.1 `topology` 同時決定佈局與約束

今天 `SolverParticleTopology` 這一個 enum 同時在做兩件不同的事：

- **粒子放在哪裡**（Chain3 是一直線三顆、RigidCluster4 是一個四面體）
- **這些粒子被什麼綁在一起**（距離約束？shape matching？幾條？）

而 `SolverShapeSource` 是 `topology` 的**可選覆寫**，不是對等的一項抽象：

```csharp
// SolverParticleProfile
public SolverParticleTopology topology = Chain3;

// Overrides Topology per instance.
public SolverShapeSource shapeSource;
```

魚把 `shapeSource` 留 null，佈局來自 emitter 內建的 topology builder；冰塊才設它。而且 `SolverIceFragmentShapeSource.BuildTemplate` **回傳的仍是一個 `SolverParticleTopology`** —— 它只填頂點位置，「這是哪種身體」還是交還給 topology。

所以那條線其實已經畫出一半了，只是沒有名字，而且只在剛體路徑上有效。

**後果**：想表達「任意粒子佈局 ＋ 任意材質行為」沒有位置可以寫。想要一個彎曲的鏟斗、一塊布丁、一片布，都得回去動 enum 和 emitter 內建的 builder。

### 1.2 render profile 混了「綁定」與「著色」

第二處耦合，比第一處隱蔽。`SolverRenderProfile` 今天裝的是兩種不同的東西：

| 欄位 | 實際上是什麼 |
|---|---|
| `material`、顏色 | **著色** |
| `mesh` | **幾何來源** |
| `forwardAxis` | **綁定**：mesh 的哪一端是頭 |
| `flipForward` | **綁定**：作者把頭畫反了 |
| `fitMeshToDimensions` | **綁定**：縮到 `baseDimensions` |

後面三個不是 shader 工作，它們是「這塊美術資產怎麼貼到物理結構上」。**綁定不是著色。**

### 1.3 「三角形從哪來」有兩條寫死的路徑

延伸自 1.2，而且是三處耦合裡最容易咬人的：

- 有作者指定的 mesh → 拿它，用 1.2 那三個欄位綁到控制 frame
- 沒有 → 拿粒子凸包生 hull mesh

走哪一條是**推導**出來的：`MeshMode => shapeSource != null || IsRigidCluster(topology)`。

這個形狀應該眼熟：它是被刪掉的 `hullFromParticles` 的下一代。一個布林決定走哪條硬編碼路徑，兩條路徑各有自己的欄位，而編寫者看不到自己走在哪一條上。差別只在於這次布林是推導的而不是手勾的，所以不會「忘了勾就什麼都不畫」——但「兩個來源、一個隱藏的選擇」這個結構本身還在。

**這就是「魚的 mesh 放在 render profile 上」不對的地方。** 不是位置錯，是那裡同時是綁定資料的家、又是兩個 mesh 來源中的一個，而另一個來源在別的地方。

---

## 2. 三項抽象責任，依資料生命週期劃分

這是本文件的核心主張。三項責任不是按「聽起來像什麼」分，而是按資料在何時建立、由誰擁有，以及下游怎麼消費來分。

| 責任 | 何時作用 | 產出什麼 | 今天住在哪裡 |
|---|---|---|---|
| **layout** | CPU，生成當下，每個實例一次 | 粒子靜止位置、粒子數量、索引與角色 | `SolverShapeSource`（僅剛體路徑）＋ emitter 內建 builder |
| **interaction** | 生成時建立 solver 資料；模擬時由 solver 與 modifier 消費 | 約束、剛體群、modifier | `SolverParticleTopology` enum ＋ profile 上的物理欄位 |
| **appearance** | 生成或載入時準備幾何；每幀繪製 | mesh 的來源／綁定，以及著色 | `SolverRenderProfile` ＋ renderer 內的推導 |

三者的資料生命週期不同：layout 建立一次靜止佈局；interaction 把它編成 solver 資料，並可帶有每步執行的 modifier；appearance 只讀模擬結果，不回寫物理狀態。這就是為什麼它們可以、也應該分開授權。

作者面對的組合保持只有一層，如開頭的系統階層圖所示。layout、interaction、appearance 三個欄位都是抽象 profile reference，可以跨元件重用。`PhysicsParticle` 本身是場景組件，擁有生成位置、生命週期、批次數量與後端選擇；它不是另一個要被嵌進 profile 的資料資產。

**批次能力是永久契約，不是某個 profile 的特例：**

- 一個 `PhysicsParticle` 可管理 `0..N` 個 runtime 實體；單一實體就是 batch count = 1。
- profile 描述的是**一個實體**：layout 決定每個實體有幾顆粒子，interaction 與 appearance 解讀同一實體。
- 要生成幾個實體，由 `PhysicsParticle` 或外部 spawn request 決定；layout / interaction / appearance 不擁有 population size。
- runtime 實體由後端批次持有，不因為上層驅動器是 MonoBehaviour 就替每個實體建立 GameObject。

---

## 3. 每一項的責任邊界

### 3.1 layout — 粒子的靜止佈局

**產出**：粒子靜止位置、數量、索引與角色，以及相容性檢查需要的佈局資訊（見第 5 節）。

**不做**：不決定自己被畫成什麼樣子，也不決定自己被什麼約束綁住。

有一個容易混淆的地方要講清楚：**hull mesh**。今天沒有作者指定 mesh 的剛體 profile，會拿自己的粒子凸包來畫。看起來像是 layout 在負責顯示，其實不是 —— layout 只產出粒子的靜止佈局，「要不要拿這份佈局生凸包來畫」是 appearance 的決定。方向不能反過來。

**layout 不可以知道自己被畫成什麼。** 這條規則不是潔癖，它是 `meshMode` 與 `hullFromParticles` 兩個欄位被刪掉的原因：它們重述了別處已經決定的事，而重述的兩份資料可以互相矛盾，症狀是「什麼都沒發生，也沒有錯誤」。

### 3.2 interaction — 物理關係與可重疊行為

**產出**：根據 layout 建立哪些約束與群組，以及執行哪些持續行為。它產出的是後端中立的意圖，不直接呼叫 `SolverManager`。

今天 Unified Solver 已公開三個足以承接第一版整合的原語：

```
AddParticle(position, velocity, mass, color, phase, visible)
AddDistanceConstraint(a, b, compliance, breakForce, damping)
AddRigidBody(particleIndices, spawnOrigin, spawnRotation)
```

這些是 `UnifiedSolverPhysicsParticleBackend` 要翻譯到的目標，不是核心 API。未來換後端時，layout / interaction / appearance 不應因為呼叫名稱不同而重新編寫。

**這是 `topology` 長大之後的樣子** —— 從六個寫死的 enum 值，變成可編寫的配方。布料與布丁不是新系統，是站在 Chain3 旁邊的另外兩個項目。

配方分成兩層，要分清楚，否則「同一個佈局換不同硬度」會變成必須重生一次晶格：

- **拓樸**：生成哪些約束（結構／剪力／彎曲／shape matching 群）
- **材質**：那些約束的 `compliance` / `damping` / `breakForce`

只有第二層才是「physics material」那個直覺對應的東西。

最上層仍是一個 `interaction` profile，但它內部可以持有複數行為：

```
Interaction
├─ 結構配方：constraint / rigid group
├─ 約束材質：compliance / damping / breakForce
├─ particleMass
└─ behaviors[]
   ├─ 舊版 SolverParticleModifierProfile 對應的持續行為
   └─ 未來其他後端可實作的行為
```

`particleMass` 表示**每顆粒子的質量**。每個實體的推導總質量為 `layout particleCount × particleMass`；Custom Editor 顯示「總質量（Total Mass）」供作者核對。若 layout 有 4 / 6 / 8 等生成變體，就顯示各變體的總質量或範圍。這裡不是「重量」；重量還需要再乘重力加速度。

`behaviors[]` 是**有序且可重疊**的清單。第一輪遷移必須保留舊版 `profile.modifiers[]` 的序列化順序，以及現有 runner 的外層階段相對關係，避免同一組內容只因換系統就改變結果。新系統最終如何排程、宣告讀寫衝突與處理相同 position writer，留到實作開始再決定，不在方向文件假定一套尚未驗證的 scheduler。

行為可以重疊，但不是無條件相加。兩份相同 constraint、同一批粒子同時被 cloth 與 rigid recipe 接管、或多個行為同時寫 position，都需要由 interaction 與後端共同驗證；持續驅動與 Sleep 的喚醒規則也必須保留。舊版 modifier 因為直接讀寫 Unified Solver buffer，第一版可由整合層包裝，但不能假裝它本身已經後端中立。

### 3.3 appearance — 可見結果

**產出**：畫面。它是上層責任，內部有兩個不同問題：mesh 從哪裡來、怎麼綁到物理結構，以及它如何著色。

這一節與 1.2、1.3 對應。**已定案只有一個 `PhysicsParticleAppearance` profile**，內部引用 mesh source / binding 與 material。這不代表兩者合併成同一份資料：mesh 的來源／綁定可以獨立於 material、顏色與 shader keyword。它們也不是保證可以任意配對；articulated 綁定需要 vertex shader 的配合，appearance 要負責驗證這對組合是否相容。

**「三角形從哪來」要有唯一的答案**，而不是兩條路徑加一個推導的布林：

```
PhysicsParticleAppearance
├─ meshSource / binding
│  ├─ AuthoredMesh    : Mesh + forwardAxis + flipForward + fitToDimensions
│  ├─ ArticulatedMesh : Mesh + 粒子控制點綁定
│  └─ ParticleHull    : 讀 layout 的粒子位置生凸包
└─ material           : Material + 顏色 + shader keyword
```

一次解掉三件事：

- **來源一致**，不必推導走哪條，`MeshMode` / `UsesHullRendering` 這類推導可以消失
- **魚的 mesh 不再「在 render profile 上」**，它在一個 mesh 來源上，跟它自己的綁定參數放在一起 —— 因為「一塊 mesh 加上它怎麼貼」本來就是一個單位
- **mesh 與著色仍可獨立重用**，只是一起由 appearance 這項上位責任組合

方向仍然守得住：**hull 那個來源是「讀」layout，不是「屬於」layout**，讀取是下游，合法。

這不是第四項抽象，而是 appearance 內部分成「三角形從哪來、怎麼跟隨物理結構」與「怎麼著色」兩段。

Custom Editor 應在編輯期把結果分級顯示：缺少必要 mesh / material、binding 與 shader 能力不相容等無法成立的組合是 **Error**；可執行但可疑的軸向、翻轉或尺寸 fitting 是 **Warning**；一般設定引導是 **Info**。`ArticulatedMesh` 不因為需要特殊 shader 就另立第四個頂層 profile。

#### 為什麼 layout 仍然不能併進 appearance

拿冰塊測，兩個方向都會斷：

- **同一個佈局畫兩種樣子**：同一份 4/6/8 碎片庫要當冰、也要當石頭 → 合併後得複製整份碎片庫
- **同一種外觀套兩個佈局**：同一份冰材質套在 4/6/8 三種變體上 → 合併後材質複製三份

魚看起來沒差，是因為**魚只有一種佈局配一種外觀，兩項都不變**，所以拆分在魚身上看起來是純開銷。對魚那個判斷是對的，對冰塊會壞。

另外，粒子數與頂點數毫無關係：一條魚是 **4 顆粒子配兩千個頂點**。布料是唯一兩者碰巧相等的情況，而那正是「不都是 mesh 嗎」這個錯覺的來源。

**已定案：不合併。`PhysicsParticle` 巢狀持有 layout / interaction / appearance 三個抽象 profile。**

---

## 4. 依賴方向與後端整合

### 4.1 三項抽象之間：單向，不可逆

```
layout ──→ interaction ──→ appearance
  │                            ▲
  └────────────────────────────┘
       （appearance 可讀 layout）
```

- **appearance 可以讀 layout 和 interaction**（推導）
- **interaction 可以讀 layout**（要知道有幾顆粒子、排成什麼樣才能生約束）
- **layout 不知道任何下游的存在**

任何一條反向的讀取，都會讓「一個佈局畫成兩種樣子」或「一種外觀套到兩個佈局」其中一件事變成不可能 —— 而那兩件事正是拆開的全部理由。

### 4.2 PhysicsParticle Core 不依賴 Unified Solver

`PhysicsParticle`、三個抽象 profile 與它們的相容性契約構成 Core。Core 描述要建立什麼，不擁有特定 solver 的 manager、offset、buffer stride、reflection contract 或 shader buffer declaration。

```
PhysicsParticle Core
├─ PhysicsParticle
├─ PhysicsParticleLayout
├─ PhysicsParticleInteraction
├─ PhysicsParticleAppearance
└─ PhysicsParticleBackend（抽象整合契約）
             ▲
             │ 實作
UnifiedSolverPhysicsParticleBackend
├─ SolverManager 呼叫
├─ instance / capacity / offset
├─ modifier compute 與執行順序
├─ GPU buffer binding
└─ renderer / shader 整合
```

依賴只能從 Unified Solver 整合層指向 Core。Core 不得引用 `SolverManager`、`SolverParticleInstance`、`SolverManagerAccess`，也不得把 Unified Solver 的 ComputeBuffer layout 當成 appearance 或 interaction 的公開契約。未來拆成獨立 package 時，Core 可以原樣搬走；新後端只需實作同一整合契約及其能力檢查。

### 4.3 整合元件是後端選擇，不是隱藏的實作細節

`PhysicsParticleBackend` 是 Core 的抽象整合契約；Unified Solver 的 concrete implementation 是場景中的共享 `UnifiedSolverPhysicsParticleBackend`。它與一個 `SolverManager` 關聯，讓多個 `PhysicsParticle` 向同一後端註冊；後端統一把三個 profile 編譯成 solver 資料，持有執行期 handle，並協調生成、modifier 與繪製。

後端是會改變能力與結果的真實選擇，所以不能由 Core 寫死或偷偷推導：

- 同一個 `PhysicsParticle` 同時只能解析到一個 active backend；沒有或多於一個都在編輯期報錯。
- concrete backend 宣告自己支援哪些 layout、interaction behavior 與 appearance binding。
- Core 不直接新增 `UnifiedSolverPhysicsParticleBackend`，否則依賴方向會反過來。整合 package 可以用 Inspector 提供「加入可用 backend」的明確入口。
- backend 可由明確 reference 或場景 registry 解析；不要求再建立獨立的 `Backend.Instance` 靜態 singleton API。這也保留未來同場景多個 physics world 的可能性。
- 既有 `SolverManagerAccess` 只保留為 Unified Solver backend 內部的窄幅相容／reflection helper；它不是 backend，也不升格成 singleton behaviour。
- 若專案只有 Unified Solver，一次選定後可以把共享整合元件的機械性欄位折疊或隱藏，但不能讓 backend 身分消失。
- 現有 `SolverParticleEmitter`、`SolverMeshRenderer` 與 `SolverParticleModifierRunner` 的責任，第一版由 Unified Solver 整合元件吸收或在其內部協調；它們是遷移來源，不是新 Core 的 public 組合方式。

整合元件最後是直接持有所有 runtime 邏輯，還是再擁有隱藏 runner／renderer companion，屬於 Unified Solver adapter 的內部決策，不得滲回三個核心 profile。

---

## 5. 三項抽象不是自由組合

這是本文件裡最容易被忽略、代價最高的一節。

軸拆開之後組合數是乘起來的，但**有些組合是無意義的**：

- 布料配方需要 2D 網格。套在四面體上沒有意義。
- articulated 的 appearance 需要鏈狀三控制點佈局。四面體配 articulated shader 會安靜地畫出垃圾。

所以每一項都要**宣告**：

- layout 宣告它產出什麼佈局（晶格維度／鏈／點雲，以及索引角色）
- interaction 宣告它接受什麼佈局
- appearance 宣告它需要什麼佈局或 interaction 產出
- backend 宣告它能實作哪些 interaction behavior、binding 與執行能力

不合就在**編輯期當場報錯**，不是執行期靜靜地不動。

這個檢查在今天還不是必要的，因為 `topology` 同時管兩件事，錯誤組合根本表達不出來。**拆開之後它就變成必要的，不是加分項。** 這個 package 已經反覆吃過同一個虧：一個設定完整的 profile，和一個會跑但什麼都不做的 profile，長得一模一樣。

---

## 6. 這樣拆買到什麼

- **布料變成一個 interaction profile。** `ClothAnchor`、`ClothGrabber` 與既有場景全數遷移後，`ClothGenerator._particleOffset` 那條 reflection 才能拿掉；建立 profile 本身不會自動移除舊工作流的依賴。
- **Soft Body 得到缺的 authoring 入口。** `Documentation/Plan/ParticleSystem x Unified Solver.md` 第 8 節（瑜珈球、氣球、動物軟組織）目前缺的正是「怎麼編寫一個軟體」，而不是缺演算法。
- **容量可以精算。** 今天剛體路徑用 `shapeSource.MaximumParticles` 抓最壞情況，因為變體要到生成當下才知道。一個 x·y·z 晶格的粒子數是確定的。
- **任意粒子佈局的碰撞體。** 體素化一個現成 mesh 就是一個 layout，不必用五個 box 拼一個桶子。

---

## 7. 這樣拆的代價，要先認清

- **組合數乘起來**，所以第 5 節的契約檢查是必要成本，不是可選的。
- **晶格是很差的碰撞體。** 解析 OBB 是精確的、O(1) 的；晶格是球堆表面（會有搓衣板感）、間距大於 `2 × particleRadius` 就會漏、而且所有粒子都要進 spatial hash。**這個方向該爭取的是會變形的東西，不是鏟子。** 形狀本身就是方盒時，box 群仍然贏。
- **魚的佈局要從 emitter 搬出來**，那是既有可運作行為的搬遷，風險不在設計而在回歸。
- **抽象出 backend 不等於現有功能自動可攜。** Modifier compute、GPU instance layout 與 articulated shader 都仍是 Unified Solver 實作；Core 只保證它們不會成為所有後端必須照抄的公開契約。
- **第一版只實作 Unified Solver backend。** 本方向保留替換縫，不為了證明抽象而同時維護第二套 solver。

---

## 8. 驗收案例、容量與遷移保證

這些不是要現在實作的功能清單，而是用來判斷抽象是否真的成立。若設計只能漂亮地描述其中一種，就還沒有完成解耦。

### 8.1 四個驗收案例

| 案例 | layout | interaction | appearance | 驗收重點 |
|---|---|---|---|---|
| **魚** | articulated 控制點佈局 | 複數可重疊 behavior | `ArticulatedMesh` + material | 既有游動、轉向與 Sleep 行為不因遷移改變 |
| **冰塊** | 4 / 6 / 8 粒子變體 | rigid group 與既有 modifier | `ParticleHull` 或 authored mesh + material | 同一批次可容納不同粒子數的完整實體 |
| **布料** | 2D grid 與索引角色 | structural / shear / bend constraint + behavior | 跟隨粒子的布面 mesh + material | Anchor / Grab 工作流有明確遷移路徑，不再依賴舊 generator offset |
| **繩索** | 1D chain | distance / bend constraint + behavior | tube、line 或 authored mesh + material | 新增 rope 只新增 profile / 可重用能力，Core 不出現 `isRope` 特例 |

差異應優先由 profile 創作完成，但 profile 不是拿來假裝後端已具備演算法。判斷規則是：

- 後端已有的粒子、distance constraint、rigid group 或持續行為能力，只需新增／組合 profile。
- 若需求需要後端根本沒有的新物理原語，例如 pressure / volume、torsion、tearing 或不同 dispatch model，新增的是一項**可重用 backend capability**及其 profile 表達，不是把魚、布、繩各自重寫一套 solver，也不是在 Core 加產品名稱分支。

### 8.2 容量與批次原子性

- 每個 runtime 實體的需求由 layout 粒子數、interaction constraints / groups / behaviors 與 appearance instance data 聯合計算。
- 共享 backend 擁有全域容量與 offset；profile 不自行保留 buffer 區段。
- batch 建立前必須 preflight 全部需求。容量不足時整批 request 拒絕；若呼叫契約明確允許回傳 accepted count，也只能建立該數量的**完整實體**，不可留下只有部分粒子、約束或外觀的半成品。
- 4 / 6 / 8 等變體按實際被選中的每個實體計算需求；maximum 只可用於保守預估，不能取代建立前的精確檢查。

### 8.3 遷移保證

- 舊魚與冰塊遷移後，以既有可見結果、物理行為、批次能力與 modifier 順序相容為基準；不能以「新架構比較乾淨」接受無說明的行為改變。
- 第一輪 interaction 遷移保留舊 `modifiers[]` 順序與 runner 外層階段關係；新的 scheduler 只有在另行驗證後才能改變結果。
- 舊 scene / prefab / profile 不可靜默失效。採兼容讀取、明確轉換工具或編輯期錯誤何者，留到實作清單定案，但「看似成功、實際沒有生成」不屬於可接受方案。
- `ClothAnchor`、`ClothGrabber` 與既有 Cloth scene 完成遷移前，不移除 `ClothGenerator._particleOffset` 相容路徑。
- 舊 `SolverParticleEmitter`、`SolverMeshRenderer` 與 `SolverParticleModifierRunner` 是遷移來源／暫時相容層，不是新 public authoring API。

---

## 9. 命名慣例，與場域／影響器

三項抽象講的是**身體自己**。這一節講**場景對身體做的事** —— 今天的 `SolverVolume` 與 `SolverMotionTarget`。兩者的命名與歸屬在同一次重構裡定案，理由如下。

### 9.1 `Physics*` 中立，`Solver*` 專屬

這是 `PhysicsParticleAnchor` 已經在用的慣例，寫下來讓之後新增的型別從出生就叫對名字：

| 字首 | 意思 | 例 |
|---|---|---|
| `Physics*` | **後端中立**，不引用 `SolverManager`、buffer layout 或相容 reflection | `PhysicsParticleAnchor`、`IPhysicsParticleSource` |
| `Solver*` | **Unified Solver 專屬**，整合層 | `SolverClothParticles`、`SolverParticleSource` |

**命名空間是 `Yu5h1Lib.ParticlePhysics`，資料夾同名。** 中立核心已經有自己的位置（`Runtime/ParticlePhysics/`），未來以 `com.yu5h1.particle-physics` 獨立成套件。字序是刻意的：命名空間講領域，型別講領域裡的東西，所以 `PhysicsParticle` 這個元件不會和裝著它的命名空間同名。`Yu5h1Lib.Physics` 評估後排除 —— 外層命名空間的解析優先於 `using`，它會在**每一個** `Yu5h1Lib.*` 檔案裡遮蔽 `UnityEngine.Physics`（庫內已有五個檔案直接使用），對下游 `using Yu5h1Lib;` 的專案也一樣；而 `Physics` 這個資料夾名在庫裡已經有既定意思，指的是 Unity 內建物理的周邊工具。

**改名的前提是先做到中立，不是反過來。** `PhysicsParticleAnchor` 能叫 `Physics`，是因為它真的只產出 `(selector, 世界座標)`；`SolverVolume` 今天只中立一半 —— 幾何那半不引用任何後端，但它承載的 `Write(volume, ref SolverVolumeGPU)` 直接填一個對應 compute shader 的 96-byte struct。**先抽象化 payload，再改名**，否則名字在宣稱一個還不存在的性質。

### 9.2 不叫 Volume，改叫 `PhysicsField`

兩個理由，第二個更重要。

**一、URP 把 Volume 這個字佔走了。** 在 Unity 專案裡看到 `Volume` 的人會先想到後處理。用它命名物理區域，借到的是一個**指向錯誤方向**的熟悉感，那比取一個陌生名字更糟。

**二、我們的組合語意是 effector，不是 Volume。**

| | URP Volume | Effector2D | 我們 |
|---|---|---|---|
| 重疊時 | 依 weight／priority **混合** | 各自獨立 | **依序套用，不混合** |
| 作用對象 | 相機，一個觀察者 | 進入的每個 body | 成千上萬顆粒子 |

URP Volume 能混合，是因為**只有一個觀察者**可以內插。我們同時作用在許多位置不同的粒子上，沒有可以內插的目標 —— 現有實作也確實寫了「Overlapping volumes apply in sequence」。

當初借 Volume 的形狀是為了「幾何是貴的那一半」，那個理由成立，但借的是**組裝方式**，不是語意。

### 9.3 影響器叫 effector，但保留 `...Profile` 字尾

```
PhysicsField（幾何：向一個 IShapeProvider 取得，見 9.7）
  └─ effectors : PhysicsEffectorProfile[]
       ├─ MediumEffectorProfile    密度、流動、黏滯      per particle
       ├─ BoundsEffectorProfile    淡出、送回、淡入      per instance
       └─ HeadingEffectorProfile   集中 / 環繞 / 對齊    per instance
```

- **effector 是施加影響的那個東西，也就是 profile，不是幾何。** 把 effector 放回區域元件上，會把「一個箱子內是水、外是回收」那個拆分又黏回去 —— 而那正是 volume/effect 分開唯一的理由。
- **`...Profile` 字尾不能省。** Unity 的 `Effector2D` 是**元件**，純叫 `MediumEffector` 會讓人想拖到 GameObject 上。這個 codebase 用 `Profile` 標記 ScriptableObject，名字長一點換掉一個錯誤預期，划算。
- **ScriptableObject 共用是我們比 `Effector2D` 強的地方**，不要為了對齊而丟掉：十二個水槽共用同一份水，Effector2D 做不到。
- **採用名字，不要採用 Volume 的混合語意。** 若哪天名字裡出現 Volume，使用者會預期 weight／priority 混合，而我們刻意不做。

### 9.4 `SolverMotionTarget` 是一個漏網的 field

它今天是獨立元件，帶一個 reach radius（幾何）、一個 mode（行為）、自己的 `SolverMotionTargetGPU`、自己的 buffer、自己的 inside test。

拆 volume/effect 當初要避免的就是這個：「第二份形狀程式碼、第二份註冊清單、第二個 GPU struct、第二次 inside test」。**`SolverMotionTarget` 就是那第二份**，它寫在拆分之前，之後沒有被收回去。

語意上也對得起來 —— 集中／環繞／某方向就是經典力場原語，和 Unity 的 effector 家族幾乎一一對應（`PointEffector2D` 吸引、`AreaEffector2D` 方向）。三者參數不同（環繞要軸、對齊要方向），而那正是無型別 payload 存在的用途，不需要三個 effector。

**命名取 `HeadingEffectorProfile` 而不是 `Flock` / `Herd`**：它描述對身體做了什麼（給一個朝向），而不是什麼動物在用。任何具備 locomotion 的東西都能被它導向。

### 9.5 field 對身體有前置條件，medium 沒有

這是 heading 與另外兩個 effector 的關鍵差異，必須寫下來：

- **medium 是推你** —— 直接寫粒子速度，對任何粒子都有效
- **heading 是告訴你往哪走** —— 只有身體具備 locomotion 時才有作用

也就是 §5「三項抽象不是自由組合」在 field 這一側的翻版：**field 也不是對任何身體都成立**。沒有這個檢查，把 heading field 蓋在一堆冰塊上會安靜地什麼都不發生。

順帶把兩者的分工釘死，這正是「魚群行為不是個體」那句話的結構：

| | 屬於誰 | 管什麼 |
|---|---|---|
| `LocomotionProfile` | 身體（interaction） | **怎麼動**：速度、節奏、轉向率 —— 個體的能力 |
| `HeadingEffectorProfile` | 場景（field） | **往哪動**：集中／環繞／方向 —— 群體的指示 |

分開之後，同一群魚換一個 field 就從聚集變成繞圈，不必碰任何 profile。

### 9.6 現在不動它

`SolverMotionTarget` 合併進體積系統**不在這次做**，理由不是懶：

- 它**現在能用**且已在 Unity 確認（locomotion 與轉向都驗過）。
- 現在合併等於做一次遷移，重構時再做第二次 —— `SolverVolume → PhysicsField`、`SolverMotionTarget → HeadingEffectorProfile` 都會再改一遍。同一份場景資料遷移兩次，比一次到位貴。
- ~~reach radius 是球，而 field 的幾何只有 box／ellipsoid。~~ **這條已不成立**：9.7 之後 field 認得 sphere，`PrimitiveShape` 也能直接表達一顆球。剩下的仍是把半徑改寫成 scale 這件編寫遷移，但不再需要為此先擴充形狀集合。
- 體積那批**還沒編譯**。往未編譯的東西上疊加，正是 `handoff.md` 的「Next, in order」第 1 項在擋的事。

**留著的代價要講清楚**：重複的 GPU struct、buffer 與 inside test 會續存，每個 emitter 的 runner 各上傳兩份清單。這是每個 emitter 的小額固定成本，不是正確性問題 —— 資訊充分的延後，不是忽略。

### 9.7 幾何向 provider 取得，不是欄位

field 不再擁有自己的幾何。它持有一個 `IShapeProvider`，位置、朝向、尺寸、形狀種類全部向它要。

```
IShapeProvider（common）
  ├─ PrimitiveShape        形狀取自自己的 Transform
  └─ ParticleSystemAddon   形狀取自 ParticleSystem 的 shape module
```

**只有一個來源，沒有回退。** 早期的草案是「有 provider 就用它，沒有就用 field 自己的 Transform」，那個回退被否決：兩個可能來源代表拖動 field 有時會移動區域、有時什麼都不做，而畫面上沒有任何東西說明是哪一種。沒有 provider 的 field 直接不運行，`Reset` 會補上一個 `PrimitiveShape`，所以那個狀態不會是任何人的起點。

**形狀種類是 enum，不是 ScriptableObject。** 判準是這個集合由誰封閉：每一種形狀都必須有一個 inside test，所以封閉它的是消費端的 kernel，不是作者。做成資產等於宣告一個 GPU 兌現不了的擴充性，而它的失敗方式是資產宣稱一個沒有分支的種類、於是安靜地什麼都不發生。另外，若 SO 只決定種類，它的全部內容就是一個 enum 值 —— 那是把下拉選單換成檔案。

**provider 必須是元件。** 沒有 Transform 的實作只能回答「是什麼形狀」而答不出「在哪裡」，那會逼每一個消費端為缺席的那一半各寫一份回退。C# 無法在 interface 上表達這個限制，所以它是契約而非約束，靠 `[TypeRestriction(typeof(IShapeProvider))]` 讓 inspector 守住編譯器守不住的部分。

**錐體的軸是 +Z，兩個半徑寫在截面的兩個軸上。** `SolverVolumeGPU` 是 96 bytes、24 個 float，沒有空位；而圓錐只用到三個軸中的兩個。所以 `Size.x` 是寬端直徑、`Size.y` 是窄端直徑（0 是真圓錐，非 0 是圓台）、`Size.z` 是長度 —— 這讓 ParticleSystem 的錐體（`radius > 0` 時本來就是圓台）能完整通過一個沒有多餘空間的結構。

軸選 +Z 而不是 +Y，是因為 Unity 的前方就是 +Z，ParticleSystem 的錐體也朝 +Z，**於是 provider 不需要在兩個座標系之間轉換**。第一版把軸定在 +Y 以對齊箱子的平頂，但錐體沒有平頂，那個理由不成立，而那個四分之一轉製造了兩個 bug：錐體整根反向，以及「local」的軸和 Transform 箭頭對不起來。

**`ParticleSystemAddon` 依幾何家族對應，忽略發射變體。** Shell 與 Edge 講的是粒子從哪裡生出來，不是形狀圍出什麼空間，而區域只在乎後者 —— 所以 `Cone`、`ConeShell`、`ConeVolume`、`ConeVolumeShell` 在這裡是同一個錐體，Box 與 Sphere 同理。第一版只接受 `ConeVolume`，結果 Inspector 預設的那個錐體（`Emit from: Base`，列舉值是 `Cone`）變成一個安靜什麼都不做的區域。Circle、Edge、Mesh、Donut、Sprite 沒有能表達的體積，`IsUsable` 回 false，**並且說出原因** —— 消費端跳過不可用的 provider 時不會有任何訊息，所以解釋必須由 provider 自己給。它同時負責兩件容易寫錯的事：shape module 的 `position`／`rotation` 是疊在 Transform 之上的偏移，不能忽略；以及 ParticleSystem 的錐體沿 local +Z 張開，而形狀的軸是 +Y。

**它們住在 common，不住在這裡。** `ParticleSystemAddon` 屬於 common，而相依只能單向流動 —— 把介面放在這個套件會讓 common 反過來依賴它。這也讓形狀契約對 solver 以外的東西可用，那是額外的收穫，不是設計目標。

### 9.8 環境的表現方式（探索中，未設計、未排程）

**這一節記錄的是討論，不是決定。** 沒有任何一項被設計過或被安排過，寫下來只為了不要重推一遍。

**今天：解析場。** 一個區域帶常數的 density / flow / viscosity，每顆粒子逐一評估。便宜、穩定、沒有狀態 —— 也因此環境不會被改變：粒子推不動水，腳踩不出坑。

**下一步可能是：可取樣的場。** 讓 density / flow / 表面高度改為從貼圖取樣而不是常數，形式和 `IShapeProvider` 相同，差別在這個 sampler 是雙向的：

- 場 → 粒子：kernel 在粒子位置取樣，比現在的區域測試還便宜。
- 粒子 → 場：另一個 pass 把影響潑進貼圖。
- 場自己的動力學（淺水方程、擴散）跑在貼圖上，頻率可以遠低於 solver。

**貼圖本身就是場景狀態**，所以航跡、腳印、壓痕會留著。這在這套系統裡特別便宜，因為粒子已經在 GPU buffer、場也在 GPU 貼圖，**兩側都不需要回讀** —— 一般遊戲做水面互動貴，貴在 rigidbody 在 CPU 那一側。

**高度場做不到的事，正是再下一步存在的理由：**

- 懸空結構與洞穴
- 捲浪（浪的內側）
- 水下漩渦

三者的共同點是**環境需要有內部結構**，而高度場只有一個表面。

**再後面的想法：環境粒子。** 用粒子表現環境本身，讓上面三件事成為同一套演算的自然結果。目前只有一個雛形描述：

- 環境粒子**不與物體粒子碰撞**。它們交換的是密度與流動這類量，不是接觸。
- 區域可以被改變，而不只是被讀取。設想的例子：一個 volume 被打穿時裂成環繞孔洞的數個 volume，而孔洞本身成為另一個 volume —— 也就是漩渦。
- 因此需要的是**多種互動組合**，不是單一種介質行為。

這一段沒有量化、沒有成本估計、也沒有和現有 GPU 結構對接的方案。現代遊戲在環境互動上已經走得很遠且各家作法不同；真要往這裡走，第一步是調查既有作法，不是從這幾行推導。


## 10. 方向上的先後（不是實作排程）

1. **建立 PhysicsParticle Core 與 backend 整合邊界。** Core 先只定義三個 profile、後端能力與生命週期契約；Unified Solver adapter 承接現有 manager、buffer、runner 與 renderer。
2. **內建 topology builder → 預設 particle layout。** 讓佈局只有一個來源，而不是「有 shapeSource 就用它、沒有就用內建的」；最小 layout 相容性宣告同時加入，不能等拆完才補。
3. **`topology` enum → interaction profile。** 約束配方與複數 behavior 由單一 interaction 組合，Unified Solver backend 負責翻譯。
4. **appearance 內的 mesh 來源多型化**（1.3 與 3.3）：兩條寫死的路徑收成一個 MeshSource／Binding，綁定資料跟著 mesh 走，著色資料保持獨立。
5. **完成整體編輯期檢查**（第 5 節）：三個 profile 彼此相容，且 active backend 具備所需能力。
6. **以魚與冰塊完成既有行為遷移驗收，再用布料與繩索驗證可擴充性**，然後才是布丁等新增軟體案例。

第 4 項的內部重構可以獨立於 2、3 先驗證，因為它不動粒子佈局，只動「三角形從哪來」；但公開型別仍要服從第 1 項的 backend 邊界，不能先把 Unified Solver buffer 寫進 Core appearance。

第 1 項的位置不能動：若先搬 layout、interaction 或 appearance，之後才抽 backend，新的 profile 會先把 `SolverManager` 與 buffer 契約吸進去，再付一次代價把它們拔掉。

驗證方向的最小一刀，在第 1、2 項之後：一個純靜態晶格（`invMass = 0`、不生任何約束、不掛任何 modifier）。它能回答 Core authoring 好不好用、Unified Solver backend 的容量與渲染怎麼接，以及最重要的 —— **接觸品質對冰塊夠不夠好**。如果搓衣板感或漏粒子太嚴重，整個方向在寫任何約束程式碼之前就有答案。

---

## 11. 明確不做

- 本文件不排實作，不寫步驟，不寫程式碼。
- **不動 vendored 相依。** 第 3.2 節那三個 public 原語已經足夠，這個方向不需要 fork，也不需要新增任何 reflection。
- 不在第一版實作第二個 physics backend；只建立不讓 Core 依賴 Unified Solver 的整合邊界。
- 不在拆解完成前先做布丁。軟體是這個方向的獎品，不是它的第一刀。
