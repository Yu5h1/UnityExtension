# PhysicsParticleAnchor：把 ClothAnchor 從布料身上解開

> 狀態：**設計已定案；程式碼已寫，尚未編譯、尚未在 Unity 確認。** 活的狀態看 `handoff.md`「Anchors」一節。
>
> 與其他文件的關係：`plan.md` 記錄現況架構與已實作的設計；`handoff.md` 記錄活的狀態；本文件是一個**已談定但還沒動工**的重構。
>
> 與 `PhysicsParticle.md` 是**不同主題**：那份談 layout / interaction / appearance 三項責任，這份談「把場景 Transform 綁到既有粒子上」。兩者不互相依賴，**本項先做**：`PhysicsParticle` 方向暫不執行，這份是為了推進度而先行的一塊。
>
> **命名與邊界沿用 `PhysicsParticle.md` 的定案**：核心是後端中立的，不引用 `SolverManager`、Unified Solver 的 buffer layout 或相容 reflection。那些一律由整合層負責 —— 對本文件而言，就是第 4 節的膠水。這一點比原本的草稿更嚴格，見 4.1。

---

## 1. 今天的問題

`ClothAnchor` 有三處，各自獨立。

### 1.1 一個 `Info` 只綁一個節點

```csharp
public sealed class Info {
    public Transform transform;
    [HideInInspector] public Vector2Int node;
}
public Info[] anchors;
```

一隻手抓 10 顆粒子，就是 **10 筆各自重複同一個 Transform**。

### 1.2 直接寫 `transform.position`，沒有 offset

```csharp
position = info.transform.position
```

所以綁到同一個 Transform 的粒子會**全部塌到同一點**。布料被抓住的那一段會皺成一個點，而不是保持形狀被提起來。

### 1.3 `Vector2Int` ＋ `[RequireComponent(typeof(ClothGenerator))]`

一個假設身體是 2D 網格，一個假設身體就是布料。這兩個是擋住 `RopeGenerator`（1D）的東西。

---

## 2. 目標形狀

```
PhysicsParticleAnchor
└─ bindings : Binding[]
     └─ Binding
          ├─ target : Transform
          └─ points : Point[]
               ├─ index       : int    （哪一顆，來源自己的扁平編號）
               └─ localOffset : Vector3（相對 target 的位置）
```

一個 Transform 綁多顆粒子，每顆帶自己的相對位置。

---

## 3. 逐一對齊

| 現況 | 解方 |
|---|---|
| 1.1 一筆一節點 | Transform 提到外層，`Binding` 裝一串 `Point` |
| 1.2 無 offset | `position = target.TransformPoint(point.localOffset)` |
| 1.3 綁死布料 | 扁平 `int` index ＋ 來源介面，`RequireComponent` 拿掉 |

### 3.1 offset 在**編寫期**捕捉，不是執行期

選到一顆粒子的當下，記錄它的靜止位置相對於 target 的值；之後可手動微調。

**時機不能改成執行期**，因為 `ClothGenerator` / `RopeGenerator` 都在執行期才生成粒子，編輯模式下**粒子不存在**，讀不到位置。捕捉必須用生成器自己的靜止佈局算式 —— 布料現有的 `GetNodeWorldPosition` 就是這個。

這也是為什麼來源需要第二個能力（見第 4 節）。

`ClothGrabber` 執行期已經在做同一件事（抓取時保存相對位置），這只是把同一招搬到編寫期。

### 3.2 `ClothAnchor.compute` 不用改

offset 在 **CPU 端**算完再上傳，GPU struct 維持 `{ int particleIndex; float3 position; }`，16 bytes。

幾十顆粒子的矩陣運算在 CPU 上不值一提，不值得為它加一個 GPU 端的 Transform 矩陣上傳與第二套解算路徑。

### 3.3 執行順序維持 `-100`

也就是在 `SolverManager`(0) **之前**。anchor 是運動學約束：先寫下位置，讓 solver 從那裡開始解。

這與 `SolverParticleModifierRunner` 在 `+50`（solver **之後**，觀察並修正）相反。兩者都對，但不能弄混 —— 把 anchor 移到 solver 之後，它會變成每幀把已經解算完的布料再拉一次。

---

## 4. 來源介面與膠水

anchor 需要知道三件事，前兩件放得進一個元件，第三件放不進去：

| 要知道 | 住在哪 |
|---|---|
| 哪些粒子 | binding |
| 該去哪裡 | binding |
| **怎麼把本地 index 變成全域 index** | **來源** |

寫成 anchor 內部的 enum 分支就是 `topology` 那個錯誤再犯一次：一個欄位管兩件事，每加一種身體都要回頭改 anchor。

```
PhysicsParticleAnchor          執行期主體，不認識任何身體
    ↓
IPhysicsParticleSource
    ├─ SolverClothParticles   包 ClothGenerator
    └─ SolverRopeParticles    包 RopeGenerator
```

來源提供兩件事，第二件由 3.1 逼出來：

| 方法 | 何時用 |
|---|---|
| `ParticleCount` / `TryApplyTargets` | 執行期，找到並寫入 solver 裡的粒子 |
| `TryGetRestPosition(int) → Vector3` | 編寫期，捕捉 offset、畫 handle |

### 4.1 膠水比原本設想的多扛一件事

沿用 `PhysicsParticle.md` 的後端中立邊界之後，膠水不只負責「本地 index → 全域 index」，**Unified Solver 專屬的東西全部歸它**：

| 歸誰 | 什麼 |
|---|---|
| `PhysicsParticleAnchor`（核心） | bindings、offset、算出每顆粒子的目標世界座標 |
| 膠水（整合層） | `SolverManager.ParticleBuffer`、`ClothAnchor.compute` 的 dispatch、`SolverManagerAccess` 的 reflection |

也就是核心只產出「**這些粒子要去這些位置**」，怎麼把它寫進某個後端是整合層的事。今天的 `ClothAnchor` 是把兩者混在一起的（它自己 `FindKernel`、自己抓 `ParticleBuffer`），所以這是這次重構要一併拆開的第四處，原本的草稿沒有涵蓋。

好處很直接：換後端時核心與編輯器一行不動；壞處是多一次介面往返，在幾十顆粒子的量級下不值得計較。

### 膠水是被逼的，不是設計選擇

`ClothGenerator` 與 `RopeGenerator` **都是 vendored 唯讀**，而且兩個的粒子起點都是 private：

```csharp
// ClothGenerator.cs
int _particleOffset;
// RopeGenerator.cs:44
int _particleOffset;
```

唯讀的類別不可能實作我們定義的介面，所以兩者都必須有一層包裝。**膠水只出現在我們不擁有原始碼的地方** —— 將來若有自己寫的身體，它直接實作介面，不需要膠水。

---

## 5. 代價，先認清

- **reflection 契約從 2 個私有欄位變成 3 個。** 目前是 `SolverManager._rigidParticleRefCount` 與 `ClothGenerator._particleOffset`；rope 會加上 `RopeGenerator._particleOffset`。這是既有模式的延伸而不是新破口（`SolverManagerAccess.TryGetClothParticleRange` 已經是這個形狀），但 `handoff.md` 上那個數字要跟著改，而且原版升級時要多驗一個欄位。
- **`fixStart` / `fixEnd` 與 anchor 重疊。** `RopeGenerator` 這兩個開關會把端點 invMass 設為 0（釘死原地）。用 anchor 綁同一顆粒子**仍然會動**（anchor 只是寫位置），但同一顆粒子有兩套機制在管，行為來源難追。**規則：被 anchor 綁的端點，把 `fixStart`/`fixEnd` 關掉。** 這條要寫進 `.agents/skills/unified-solver.md`，否則一定有人踩。
- **`ClothAnchor` 改名會讓既有場景元件失去參照。** 需要 `[MovedFrom]`，並且 `.cs` 與 `.meta` 一起 `git mv` 保住 GUID —— 與 `SolverMediumVolume → SolverVolume` 同一套做法，`plan.md` 16.2 有前例。
- **既有 `anchors` 欄位的資料要遷移。** 舊的一筆一節點要折進新的 binding 結構，而舊資料**沒有 offset** —— 遷移時只能補 0（維持舊行為：塌到 Transform 上），不能假裝它本來就有相對位置。是否值得寫遷移程式碼，取決於現場有幾個 anchor，動工前先數。

---

## 6. 編輯器卡頓：已診斷

**這是這次重構的起因。** 動工前讀了 `ClothAnchorEditor`，原因比原本的假設更嚴重。

`OnSceneGUI` 每次重繪 —— **包含滑鼠移動** —— 都做：

| 項目 | 50×50 的次數 |
|---|---|
| `Handles.Button`（各自 control ID＋hit test＋繪製） | 2500 |
| `HandleUtility.GetHandleSize`（各一次相機投影） | 2500 |
| `GetNodeWorldPosition` 矩陣運算 | ~7500 |
| 陣列／Dictionary 配置 | ~101 |

外加 `AddAnchor` **每個頂點生一個 GameObject**。

所以卡頓不是資料形狀造成的，是編輯器把「每幀」的工作做成了「每節點每幀」。四個修法：靜止位置與格線快取（鍵為粒子數、`LayoutSize` 與來源的 `localToWorldMatrix`）、挑選只在滑鼠事件跑、只畫已綁定與 hover 的點、一個 binding 一個 Transform。

---

## 7. 明確不做

- 不在本文件排步驟、不寫程式碼。
- 不動 vendored 相依。膠水在擴充層，`RopeGenerator` 與 `ClothGenerator` 一行不改。
- 不把 anchor 擴張成 grabber。`ClothGrabber` 是執行期抓放，anchor 是持續綁定，兩者共用「保存相對位置」這個想法但生命週期不同，合併會讓兩邊都變複雜。
