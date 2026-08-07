# SolverParticle 系統：三軸解耦

> 狀態：**方向文件，不含實作排程。** 這裡寫的是「正確的拆法長什麼樣」與「為什麼是這個拆法」，不寫步驟、不寫程式碼、不排期。
>
> 與其他文件的關係：`plan.md` 記錄**現況架構與已實作的設計**；本文件是**還沒做的方向**。真的動工時，實作細節回到 `plan.md`，活的狀態回到 `handoff.md`。
>
> **已定案**：維持 **shape / interaction / render 三個 profile，不合併**。shape 併進 render 的可能性經評估後排除，理由在 3.3。

---

## 1. 今天的耦合在哪裡

三處，各自獨立。解耦時知道要拆的是這三個，而不是憑印象重推一次。

### 1.1 `topology` 同時決定佈局與約束

今天 `SolverParticleTopology` 這一個 enum 同時在做兩件不同的事：

- **粒子放在哪裡**（Chain3 是一直線三顆、RigidCluster4 是一個四面體）
- **這些粒子被什麼綁在一起**（距離約束？shape matching？幾條？）

而 `SolverShapeSource` 是 `topology` 的**可選覆寫**，不是對等的一軸：

```csharp
// SolverParticleProfile
public SolverParticleTopology topology = Chain3;

// Overrides Topology per instance.
public SolverShapeSource shapeSource;
```

魚把 `shapeSource` 留 null，佈局來自 emitter 內建的 topology builder；冰塊才設它。而且 `SolverIceFragmentShapeSource.BuildTemplate` **回傳的仍是一個 `SolverParticleTopology`** —— 它只填頂點位置，「這是哪種身體」還是交還給 topology。

所以那條線其實已經畫出一半了，只是沒有名字，而且只在剛體路徑上有效。

**後果**：想表達「任意形狀 ＋ 任意材質行為」沒有位置可以寫。想要一個彎曲的鏟斗、一塊布丁、一片布，都得回去動 enum 和 emitter 內建的 builder。

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

## 2. 三軸，依執行階段劃分

這是本文件的核心主張。三軸**不是按「聽起來像什麼」分的，是按程式在哪裡跑分的** —— 因為那決定了誰有能力擁有什麼資料，也決定了資料只能往哪個方向流。

| 軸 | 在哪裡跑 | 產出什麼 | 今天住在哪裡 |
|---|---|---|---|
| **shape** | CPU，生成當下，每個實例一次 | 粒子靜止位置、粒子數量、結構宣告 | `SolverShapeSource`（僅剛體路徑）＋ emitter 內建 builder |
| **interaction** | Compute kernel，每個模擬步 | 約束、剛體群、modifier | `SolverParticleTopology` enum |
| **render** | Vertex / Fragment shader，每幀 | 畫面 | `SolverRenderProfile` |

三個階段的資料生命週期完全不同：shape 的產出**寫一次就不再變**，interaction 的產出**每步被讀寫**，render 的產出**不回寫任何東西**。這就是為什麼它們可以、也應該分開授權。

---

## 3. 每一軸的責任邊界

### 3.1 shape — 幾何事實

**產出**：粒子靜止位置、數量，以及一份**結構宣告**（見第 5 節）。

**不做**：不決定自己被畫成什麼樣子，也不決定自己被什麼約束綁住。

有一個容易混淆的地方要講清楚：**hull mesh**。今天沒有作者指定 mesh 的剛體 profile，會拿自己的粒子凸包來畫。看起來像是 shape 在負責顯示，其實不是 —— shape 產出的是**幾何事實**，「要不要拿這份幾何去畫」是 render 的決定。方向不能反過來。

**shape 不可以知道自己被畫成什麼。** 這條規則不是潔癖，它是 `meshMode` 與 `hullFromParticles` 兩個欄位被刪掉的原因：它們重述了別處已經決定的事，而重述的兩份資料可以互相矛盾，症狀是「什麼都沒發生，也沒有錯誤」。

### 3.2 interaction — 每一步的計算

**產出**：要對 solver 下哪些呼叫，以及掛哪些 modifier。三個原語都是 public 的：

```
AddParticle(position, velocity, mass, color, phase, visible)
AddDistanceConstraint(a, b, compliance, breakForce, damping)
AddRigidBody(particleIndices, spawnOrigin, spawnRotation)
```

**這是 `topology` 長大之後的樣子** —— 從六個寫死的 enum 值，變成可編寫的配方。布料與布丁不是新系統，是站在 Chain3 旁邊的另外兩個項目。

配方分成兩層，要分清楚，否則「同一個形狀換不同硬度」會變成必須重生一次晶格：

- **拓樸**：生成哪些約束（結構／剪力／彎曲／shape matching 群）
- **材質**：那些約束的 `compliance` / `damping` / `breakForce`

只有第二層才是「physics material」那個直覺對應的東西。

### 3.3 render — 每一幀的繪製

**產出**：畫面。material、顏色、shader keyword —— **著色，僅此而已**。

這一節與 1.2、1.3 對應：今天的 `SolverRenderProfile` 比這個範圍大，多裝了綁定資料和其中一個 mesh 來源。拆解時它要瘦回來。

**「三角形從哪來」要有唯一的答案**，而不是兩條路徑加一個推導的布林：

```
MeshSource（抽象）
├─ AuthoredMesh  : Mesh + forwardAxis + flipForward + fitToDimensions
└─ ParticleHull  : 讀 shape 的粒子位置生凸包
```

一次解掉三件事：

- **來源一致**，不必推導走哪條，`MeshMode` / `UsesHullRendering` 這類推導可以消失
- **魚的 mesh 不再「在 render profile 上」**，它在一個 mesh 來源上，跟它自己的綁定參數放在一起 —— 因為「一塊 mesh 加上它怎麼貼」本來就是一個單位
- **render profile 真的只剩 shader 工作**

方向仍然守得住：**hull 那個來源是「讀」shape，不是「屬於」shape**，讀取是下游，合法。

這不是第四軸，是 render 這一軸內部分成「三角形從哪來」與「怎麼著色」兩段 —— 剛好對應 vertex 階段與 fragment 階段。

#### 為什麼 shape 仍然不能併進 render

拿冰塊測，兩個方向都會斷：

- **同一個形狀畫兩種樣子**：同一份 4/6/8 碎片庫要當冰、也要當石頭 → 合併後得複製整份碎片庫
- **同一種外觀套兩個形狀**：同一份冰材質套在 4/6/8 三種變體上 → 合併後材質複製三份

魚看起來沒差，是因為**魚只有一種形狀配一種外觀，兩軸都不變**，所以拆分在魚身上看起來是純開銷。對魚那個判斷是對的，對冰塊會壞。

另外，粒子數與頂點數毫無關係：一條魚是 **4 顆粒子配兩千個頂點**。布料是唯一兩者碰巧相等的情況，而那正是「不都是 mesh 嗎」這個錯覺的來源。

**已定案：不合併。維持 shape / interaction / render 三個 profile。**

---

## 4. 依賴方向：單向，不可逆

```
shape ──→ interaction ──→ render
  │                          ▲
  └──────────────────────────┘
        （render 可讀 shape）
```

- **render 可以讀 shape 和 interaction**（推導）
- **interaction 可以讀 shape**（要知道有幾顆粒子、排成什麼樣才能生約束）
- **shape 不知道任何下游的存在**

任何一條反向的讀取，都會讓「一個形狀畫成兩種樣子」或「一種外觀套到兩個形狀」其中一件事變成不可能 —— 而那兩件事正是拆開的全部理由。

---

## 5. 三軸不是自由組合

這是本文件裡最容易被忽略、代價最高的一節。

軸拆開之後組合數是乘起來的，但**有些組合是無意義的**：

- 布料配方需要 2D 網格。套在四面體上沒有意義。
- articulated 的 render profile 需要鏈狀三控制點結構。四面體配 articulated shader 會安靜地畫出垃圾。

所以每一軸都要**宣告**：

- shape 宣告它產出什麼結構（晶格維度／鏈／點雲）
- interaction 宣告它接受什麼結構
- render 宣告它需要什麼結構

不合就在**編輯期當場報錯**，不是執行期靜靜地不動。

這個檢查在今天還不是必要的，因為 `topology` 同時管兩件事，錯誤組合根本表達不出來。**拆開之後它就變成必要的，不是加分項。** 這個 package 已經反覆吃過同一個虧：一個設定完整的 profile，和一個會跑但什麼都不做的 profile，長得一模一樣。

---

## 6. 這樣拆買到什麼

- **布料變成一個 interaction profile。** 連帶地，`ClothGenerator` 那條 reflection 依賴（`_particleOffset`）可以拿掉，相容契約從兩個私有欄位掉到一個。
- **Soft Body 得到缺的 authoring 入口。** `plan.md` 第 8 節（瑜珈球、氣球、動物軟組織）目前缺的正是「怎麼編寫一個軟體」，而不是缺演算法。
- **容量可以精算。** 今天剛體路徑用 `shapeSource.MaximumParticles` 抓最壞情況，因為變體要到生成當下才知道。一個 x·y·z 晶格的粒子數是確定的。
- **任意形狀的碰撞體。** 體素化一個現成 mesh 就是一個 shape source，不必用五個 box 拼一個桶子。

---

## 7. 這樣拆的代價，要先認清

- **組合數乘起來**，所以第 5 節的契約檢查是必要成本，不是可選的。
- **晶格是很差的碰撞體。** 解析 OBB 是精確的、O(1) 的；晶格是球堆表面（會有搓衣板感）、間距大於 `2 × particleRadius` 就會漏、而且所有粒子都要進 spatial hash。**這個方向該爭取的是會變形的東西，不是鏟子。** 形狀本身就是方盒時，box 群仍然贏。
- **魚的佈局要從 emitter 搬出來**，那是既有可運作行為的搬遷，風險不在設計而在回歸。

---

## 8. 方向上的先後（不是實作排程）

1. **內建 topology builder → 預設 shape source。** 讓佈局只有一個來源，而不是「有 shapeSource 就用它、沒有就用內建的」。
2. **`topology` enum → `interactionProfile`。**
3. **mesh 來源多型化**（1.3 與 3.3）：兩條寫死的路徑收成一個 `MeshSource`，綁定資料跟著 mesh 走，render profile 瘦回純著色。
4. **結構契約與編輯期檢查**（第 5 節）。
5. **布料 profile**，然後才是布丁。

第 3 項可以獨立於 1、2 先做，因為它不動粒子佈局，只動「三角形從哪來」。若要一個風險低、又能立刻拿掉一個推導欄位的起點，它是最好的一刀。

第 1 項的位置不能動：**在它完成之前，三軸是講不通的**，因為魚根本沒有第一軸可以換。

驗證方向的最小一刀，也在第 1 項之後：一個純靜態晶格（`invMass = 0`、不生任何約束、不掛任何 modifier）。它能回答 authoring 好不好用、容量對不對、渲染怎麼接，以及最重要的 —— **接觸品質對冰塊夠不夠好**。如果搓衣板感或漏粒子太嚴重，整個方向在寫任何約束程式碼之前就有答案。

---

## 9. 明確不做

- 本文件不排實作，不寫步驟，不寫程式碼。
- **不動 vendored 相依。** 第 3.2 節那三個 public 原語已經足夠，這個方向不需要 fork，也不需要新增任何 reflection。
- 不在拆解完成前先做布丁。軟體是這個方向的獎品，不是它的第一刀。
