# ParticleExtensions：粒子擴充行為

> 規格版本 1.2.1 · 2026-11-16 · 第 4 階段：EXT-01～EXT-14 與錨定編寫工具都已在 Motion package 實作，PEXT-CHECK-01～14 在 Yu5h1LibTest EditMode（DX11，Unity 6000.6.0f1）通過
>
> 1.2.1 依實作補上兩項釐清，行為不變：睡眠者「本來會被推動的量」的定義，以及睡眠者不被碰撞形狀推動或喚醒的限制（EXT-13）；身體顏色改成獨立的整數欄位（EXT-09），避免 RGBA8 位元被當成浮點數時變成 NaN。
>
> 1.1.0 新增繪製存取（EXT-08）：繪製直接讀模擬資料，剛體群的姿勢在 GPU 上組成矩陣（ParticleWorld WORLD-14 的〈剛體姿勢〉）。
>
> 1.2.0 新增身體、發射器、行為（modifier）、睡眠與 volume（EXT-09～EXT-14）與錨定的編寫工具，行為來源是 Unified-Solver 的 `SolverParticleEmitter`、`SolverParticleModifierRunner`、`SolverParticleModifiers.compute`、`SolverVolume` 與相關 profile。睡眠的做法同時修訂 ParticleWorld WORLD-10（1.2.0）。
>
> 與其他文件的關係：擴充行為掛在哪個階段、能讀寫什麼由 [ParticleWorld](ParticleWorld.md) 的擴充階段（WORLD-13）規定，本文件補上它在 Motion package 的實作規則，並規定各擴充行為本身的結果。一個步驟內的數值行為由 [ParticleSolver](ParticleSolver.md) 規定。行為來源是 Unified-Solver 套件的擴充層（`PhysicsParticleAnchor`、`ClothGrabber`），差異列在〈與 Unified-Solver 擴充層的差異〉。遷移順序見 Motion repo 的 `Documentation/GpuPhysicsMigrationPlan.md` 第 4 階段。

## 系統階層與關係

```text
ParticleWorld
└─ 擴充階段（WORLD-13）
   ├─ 擴充的註冊、時機與資料存取（EXT-01～EXT-04）
   ├─ 繪製存取（EXT-08）：繪製直接讀粒子與剛體群
   └─ 擴充行為
      ├─ 運動學固定（EXT-05）：讓粒子跟著一個 Transform 走
      │  ├─ 錨定（EXT-06）：編寫時選好的粒子，持續固定
      │  └─ 抓取（EXT-07）：執行時抓起範圍內的粒子，放開後恢復
      ├─ 身體來源（EXT-09）與身體佈局（EXT-10）：魚、碎片這類由幾顆粒子組成的身體
      ├─ 發射器（EXT-11）：生成、重生、收起一批身體
      ├─ 行為（EXT-12）：擺動、跳躍、滾轉阻尼、限速、介質狀態、游動、邊界回收
      ├─ 睡眠（EXT-13）：靜止的身體不進入求解器的 kernel
      └─ volume（EXT-14）：介質與邊界的場景區域
```

擴充行為只透過世界公開的資料與實體 handle 工作，不讀求解器的私有狀態。

## 目標

- **為誰服務**：BonghuoVR 的捕撈表演：網子沿兩根木桿錨定、NPC 抓網收網、水管錨在 NPC 手上。
- **情境**：
  - 魚群在水中游向目標、擺尾、被水流帶走，離開範圍時淡出並回到生成區；靜止在甲板上的魚與碎片不耗求解器運算，被碰到時醒來（EXT-09～EXT-14）。
  - 網子邊緣固定在木桿上，木桿移動時網子跟著走，並保持被錨定那一段的形狀（錨定（EXT-06））。
  - 手伸進網子抓起一團，移動手時那一團跟著手平移與旋轉，放開後依手的速度飛出（抓取（EXT-07））。
  - 固定的點在子步驟之間平滑移動，不是每步跳一次（運動學固定（EXT-05））。
- **不在此目標**：網子、水管、魚身與碎片的繪製元件（它們使用繪製存取（EXT-08），規則在 [ParticleRendering](ParticleRendering.md)）；Unity ParticleSystem 轉成粒子的橋接；球、圓柱、圓錐形的 volume 與「作用在外側」的介質（BonghuoVR 只用方塊）。
- **完成條件**：本文件的平台檢查在 Motion package 的 EditMode 測試通過。

## 擴充階段的實作

### 註冊（EXT-01）

- **規則**：擴充行為以物件向世界註冊，宣告階段（子步驟內或步後）與會寫入的資料（位置、速度）。註冊與取消註冊都在下一個步驟開始時生效（同 WORLD-03）；同一物件重複註冊不改變順序。同一階段依註冊順序執行。
- **驗證**：PEXT-CHECK-01、02。

### 時機（EXT-02）

- **規則**：
  - **準備**：每個會模擬的步驟在第一個子步驟之前呼叫一次，給擴充上傳本步的輸入（例如 Transform 的姿勢）。
  - **子步驟內**：每個子步驟在碰撞形狀之後、剛體群之前執行（ParticleSolver SOLVER-01 的順序中），可取得子步驟序號 s 與子步驟數 n。
  - **步後**：所有子步驟之後執行，在介質受力（ParticleSolver SOLVER-17）之後。
  - 沒有任何參與模擬的粒子時，準備與執行都不呼叫（WORLD-10）。
- **驗證**：PEXT-CHECK-01、03。

### 資料存取（EXT-03）

- **規則**：擴充以世界提供的綁定取得粒子的位置（含反質量）、前一位置、速度、每粒子資料（群組、分類、半徑）與參與模擬的粒子清單；這些資料的格式在一份共用的 HLSL include 中宣告，求解器本身也使用它。擴充只處理參與模擬的實體（收起、已釋放的不在範圍內）；以實體為範圍的擴充在準備時檢查實體是否參與模擬。
- **驗證**：PEXT-CHECK-03。

### 寫入宣告（EXT-04）

- **規則**：子步驟內的擴充不能宣告寫入速度：速度更新會在同一子步驟內由位置重建速度（SOLVER-16），寫入的速度會被蓋掉。這樣的註冊被拒絕並報錯。步後的擴充可以寫入位置與速度。
- **驗證**：PEXT-CHECK-02。

### 繪製存取（EXT-08）

- **規則**：
  - 世界可把粒子資料與剛體群綁到繪製用的 material 屬性，格式與擴充相同（同一份 HLSL include）；繪製不需要把粒子讀回 CPU。
  - 剛體群的姿勢在 GPU 上組成：一個以實體本地座標編寫、由剛體群 g 帶動的點，世界位置 = g 的模擬質心 + g 的旋轉 ×（點 − g 的靜止質心）。靜止質心是成員靜止位置依質量加權的平均，成員全部固定時取平均；實體 handle 提供它與成員清單。
  - 世界不在每步讀回剛體群（WORLD-14）；需要在 CPU 使用姿勢時以非同步讀回取得。
- **證據**：Unified-Solver 的剛體路徑每步同步讀回剛體 buffer，再在 CPU 組矩陣繪製冰塊碎片（套件 handoff Known limitations；遷移計畫〈消費情境需要的能力〉）。
- **驗證**：PEXT-CHECK-08。

## 運動學固定（EXT-05）

- **規則**：一顆被固定的粒子屬於一個持有者（Transform），帶一個相對持有者的本地位置：
  - 每個步驟開始時取得持有者上一步與這一步的 localToWorld 矩陣 M₀、M₁；子步驟 s（從 0 起）的目標位置 x(f) = lerp(M₀·offset, M₁·offset, f)。
  - 子步驟 s 結束時，粒子位置 = x((s+1)/n)，前一位置 = x(s/n)，速度 = (x((s+1)/n) − x(s/n)) / h，反質量 = 0。持有者第一次出現的那一步 M₀ = M₁。
  - 固定開始時記下粒子原本的反質量；放開時恢復它，位置與前一位置留在放開當下的目標，速度是持有者在該點的速度，所以粒子會以手的速度飛出。
  - 被固定的粒子對約束與接觸是無限質量（反質量 0 的一般規則，SOLVER-03、06）。
- **證據**：Unified-Solver 的錨定與抓取每個 FixedUpdate 在求解器之前寫一次位置，30 個子步驟都看到同一個位置，持有者移動時目標每步跳一次（套件 `PhysicsParticleAnchor.cs`、`ClothGrab.compute`）。
- **驗證**：PEXT-CHECK-04、05。

### 錨定（EXT-06）

- **規則**：
  - 錨定以「持有者 Transform → 一串點」編寫；每個點是來源驅動器（`PhysicsParticle`）的扁平粒子編號與相對持有者的本地位置。扁平編號依驅動器實體的順序把各實體的粒子接起來。
  - 本地位置在編寫時以佈局的靜止位置與驅動器的生成姿勢算出，可手動調整；同一持有者的多顆粒子保持彼此的相對位置。
  - 錨定啟用期間持續固定這些點；停用或取消註冊時放開（EXT-05），反質量恢復成實體配置時的值。
  - 來源實體收起時不處理它的點；實體重設後下一個子步驟重新固定。
  - 編號超出來源粒子數的點被略過並在編輯器報錯，不影響其他點。
  - 布料與繩子的配方不提供固定邊或固定端點：不動的固定點也是錨定到一個不動的 Transform，同一顆粒子只由一種機制固定。
- **證據**：一個 Transform 綁多顆粒子、帶各自偏移的設計來自套件 `Documentation/Plan/PhysicsParticleAnchor.md`；同一份文件第 5 節記錄了繩子的 fixStart／fixEnd 與錨定同時管同一顆粒子、行為來源難追的問題。
- **驗證**：PEXT-CHECK-04、06。

#### 錨定的編寫工具

- **規則**：
  - 檢視器顯示來源的粒子數（網格佈局另顯示行列）與已綁定的點數；「編輯綁定」開啟時，在場景中點選粒子把它加入或移出目前編輯中的持有者，加入時以佈局的靜止位置算出本地位置（EXT-06）。播放中不能編輯。
  - 「重新擷取偏移」依目前的持有者姿勢與靜止位置重算所有點的本地位置。
  - 新增錨定元件時，自動把同一個 GameObject（或父物件）上的 `PhysicsParticle` 設為來源。
  - 效能：靜止位置與格線只在來源的佈局、數量或 Transform 改變時重建；點選只在滑鼠事件做一次最近粒子搜尋，重繪時不做點選；只畫已綁定與滑鼠所指的粒子，其餘以格線表示。
  - 修改以 Undo 記錄，可復原。
- **證據**：套件 `Editor/PhysicsParticleAnchorEditor.cs` 與 `Documentation/Plan/PhysicsParticleAnchor.md` 第 6 節（每次重繪對每個節點各做一次 Handles.Button 造成 50 × 50 網子卡頓）。
- **驗證**：PEXT-CHECK-09。

### 抓取（EXT-07）

- **規則**：
  - 抓取器以一個手的 Transform、一個半徑與一個目標驅動器（`PhysicsParticle`）運作。呼叫抓取後，下一個步驟的第一個子步驟把目標實體中距手中心不超過半徑、反質量大於 0 的粒子固定在手上，本地位置以那一刻的手姿勢 M₀ 算出。已被其他錨定或抓取固定的粒子（反質量 0）不會被抓。
  - 抓到的數量以非同步讀回送達，標明它屬於哪一個步驟（WORLD-14）；抓取器不做同步讀回。
  - 放開時所有抓到的粒子依 EXT-05 恢復。再次抓取前必須先放開。
  - 目標實體收起時不處理；被釋放時抓取狀態作廢。
- **證據**：Unified-Solver 的 `ClothGrabber` 開始抓取時同步讀回抓到幾顆（套件 handoff Known limitations；BonghuoVR `效能評估.md`）。
- **驗證**：PEXT-CHECK-05、07。

## 身體與發射器

### 身體來源（EXT-09）

- **規則**：
  - 身體來源是一個場景元件，它的每個實體是一個身體。它公開實體清單、一個 GPU buffer（每個實體一筆 `ParticleBodyData`，順序與實體清單相同）、可選的生命週期 buffer（每個身體一個 float4：狀態、進度、隱藏比例、淡入淡出速率）、外觀 profile，以及一個在實體清單或 buffer 內容改變時遞增的版本號（C# `IParticleBodySource`）。
  - `ParticleBodyData` 的格式在 `Shaders/ParticleBodyFrame.hlsl` 宣告一次，行為 kernel 與魚身繪製共用同一份頭、中、尾座標框計算。欄位：粒子起點與數量、佈局（EXT-10）、旗標（第 0 位：實體正在模擬）、縮放（w 為拖曳係數）、生成旋轉、基本尺寸、顏色（RGBA8 整數，r 在最低位元組）。
  - 實體被收起、睡眠或釋放時，對應身體的模擬旗標清除；行為 kernel 跳過沒有旗標的身體（睡眠檢查除外，見 EXT-13）。
- **證據**：Unified-Solver 的 `SolverParticleInstance`（64 bytes）與 `SolverBodyFrame.hlsl`，後者同時被 modifier compute 與 articulated shader 使用。
- **驗證**：PEXT-CHECK-10。

### 身體佈局（EXT-10）

- **規則**：
  - 佈局與 Unified-Solver 的 topology 一一對應，粒子的本地位置與連結照它的 `SolverParticleEmitter.BuildLocalShape` 與生成時建立的約束（半尺寸 h = 基本尺寸 × 身體縮放 × 0.5，再乘生成旋轉）：

| 佈局 | 粒子 | 本地位置 | 約束 |
|---|---|---|---|
| Single | 1 | 原點 | — |
| Chain3 | 3 | 頭 +Y·hy、中 0、尾 −Y·hy | 2 個關節、1 個彎曲 |
| GuideChain4 | 4 | Chain3 再加導向粒子 +X·hx | 3 個關節、3 個彎曲 |
| DualRail6 | 6 | 三對 (0,1)(2,3)(4,5) 在 y = +hy、0、−hy，x = ±hx | 7 個關節、6 個彎曲 |
| ArticulatedCluster12 | 12 | 3 段各 4 顆，段中心相隔 基本尺寸.y / 3 | 3 個剛體群各 4 顆、2 個關節 |
| RigidCluster4／6／8 | 4／6／8 | 形狀來源的模板，否則四面體／八面體／方塊的基本頂點乘半尺寸 | 1 個剛體群 |

  - 關節用關節柔度與關節阻尼，彎曲用彎曲柔度與關節阻尼；哪兩顆粒子相連照 Unified-Solver 的對應程式。每顆粒子的質量 = 身體質量 ÷ 粒子數（保留 Unified-Solver 的語意，所以 4 顆的碎片比 8 顆的密；這是套件 handoff 的已知問題 2，調整屬第 5 階段）。
  - 形狀來源（冰塊碎片）以種子決定模板，同一個種子、同一個模板編號在生成與繪製得到相同的頂點；欄位與預設值照 `SolverIceFragmentShapeSource`。
  - 這些佈局以 Motion package 的佈局與互動 profile 提供，驅動器與發射器都能使用。
- **證據**：套件 `SolverParticleEmitter.cs`（BuildLocalShape 與約束建立）、`Core/SolverHullShapes.cs`、`Profiles/SolverIceFragmentShapeSource.cs`。
- **驗證**：PEXT-CHECK-10。

### 發射器（EXT-11）

- **規則**：
  - 發射器是身體來源。它使用佈局、互動與外觀 profile，加上一個行為 profile（EXT-12），最多 maxInstances 個身體（預設 2048）。
  - 第一次啟用時，若「啟用時播放」開啟，在本地方塊生成區（預設 5 × 2 × 5 m）內的隨機位置生成 initialCount 個身體（預設 100），速度 = 初速 + 球內隨機 × 速度變化（預設 0.5 m/s），再乘發射器的旋轉；顏色以 HSV 依顏色變化抖動。容量不足時盡量建立（WORLD-04），缺額以一則錯誤回報。
  - 腳本可排入生成要求（位置、旋轉、速度、縮放、顏色），在下一個固定步驟一次配置。
  - 「與同 profile 互碰」開啟（預設）時每個身體自己一個群組；關閉時整個發射器共用一個群組，彼此不碰。
  - 停用時收起所有身體；再次啟用時把每個身體重設到生成區內的新隨機位置（WORLD-07、WORLD-08），清除生命週期與行為狀態。腳本也可以呼叫重生與收起。
  - 發射器不做同步讀回。
- **證據**：套件 `SolverParticleEmitter.cs`、`Compatibility/SolverReuse.cs`。與第三方求解器不同，這裡不需要「先配置再重用」或啟動時暫時啟用的繞道（WORLD-06、WORLD-08）。
- **驗證**：PEXT-CHECK-11。

## 行為（EXT-12）

- **規則**：
  - 每個發射器向世界註冊一個步後擴充，依下列順序執行行為 kernel，每個身體一個執行緒，與 Unified-Solver 的 `SolverParticleModifierRunner.FixedUpdate` 相同：限速（限速 > 0 時）→ 介質狀態 → 滾轉阻尼（含頭尾展開，永遠執行）→ 行為 profile 清單中啟用的擺動、跳躍、游動（依清單順序）→ 邊界回收 → 睡眠（睡眠速度 > 0 時）。
  - 每個 kernel 的公式、常數、亂數與 profile 欄位（名稱、單位、預設值）照 Unified-Solver `SolverParticleModifiers.compute` 的同名 kernel 與對應 profile：ApplySpeedLimit、ApplyRollDamping（UnfoldHairpin、DampGuideOrbit、DampGroupRoll）、ApplyOscillation、ApplySurfaceImpulse、ApplyLocomotion、ApplyBounds、ApplySleep。差異只有下列幾項：
    - 資料從 `ParticleWorldData.hlsl` 與 `ParticleBodyFrame.hlsl` 讀寫：反質量是位置的 w，前一位置與速度各自一個 buffer，粒子半徑是每顆粒子自己的。
    - **介質力只有一套**：浮力與拖曳由世界的介質受力（ParticleSolver SOLVER-17）對所有粒子施加，介質來自 volume（EXT-14）；發射器的介質狀態 kernel 不再施力，只計算睡眠與游動要用的每身體狀態：浸入比例、漂移、淨升力加速度、介質對身體的密度比，判斷方式與 SOLVER-17 相同。漂移以介質施力之後的速度計算。拖曳係數用介質的值；BonghuoVR 的魚身體拖曳係數是 1，結果相同。
    - 擺動、游動、跳躍有任何一個啟用就讓身體保持清醒，與 Unified-Solver 相同。
    - 邊界回收的瞬移直接寫位置與前一位置並把速度歸零，與 Unified-Solver 相同；生成區是發射器的方塊。
  - 行為 profile 的欄位：限速、限速衰減率、滾轉阻尼、睡眠速度、睡眠延遲、喚醒距離、行為清單（擺動、跳躍、游動三種 profile 的參照）。
  - 游動目標是場景元件（點或方向、半徑），向它解析到的世界登記；選目標的規則照 ApplyLocomotion。
- **證據**：套件 `Modifiers/SolverParticleModifierRunner.cs`、`Resources/SolverParticleModifiers.compute`、`Profiles/*`、`Components/SolverMotionTarget.cs`；兩套介質會讓 volume 同時餵兩個系統。
- **驗證**：PEXT-CHECK-12。

## 睡眠（EXT-13）

- **規則**：
  - 入睡與喚醒的判斷照 ApplySleep：身體所有粒子的最大速率低於睡眠速度並持續睡眠延遲秒才入睡；有持續行為啟用、正在邊界回收、或介質的漂移加上 max(0, 升力) × 睡眠延遲超過睡眠速度時保持清醒。
  - 入睡的身體以 `ParticleEntity.SetSleeping` 在下一個步驟邊界標記（判斷在 GPU，結果以非同步讀回送到 CPU）。睡眠中的實體：
    - 不進入預測、約束、碰撞形狀、剛體群、速度更新、介質與其他擴充的範圍，所以自然停在原地，不需要每步寫回姿勢。
    - 仍放進鄰近結構，當作不動的接觸對象：醒著的粒子撞上它會被推開，它自己不動。
    - 每個步驟記錄它「本來會被推動」的量：每次接觸修正乘上本步剩餘的子步驟數，取最大值，近似 Unified-Solver 以一整步位移判斷喚醒；只取單一子步驟的修正時，5 mm 的喚醒距離下 2 m/s 的撞擊不一定喚得醒。超過喚醒距離，或超過喚醒距離的 10% 並持續睡眠延遲秒，就標記喚醒，結果以非同步讀回送到 CPU，在下一個步驟邊界清除睡眠。
  - 傳送、重置、收起、重生、行為啟用都立刻清除睡眠。
  - 喚醒有 1～3 幀的延遲，這段時間睡著的身體像牆一樣不動。
  - 已知限制：睡眠者不進入碰撞形狀，所以移動的碰撞體不會推動或喚醒它（Unified-Solver 的睡眠身體會被碰撞體推開而醒）；世界中沒有任何醒著的粒子時整步不執行，介質改變也喚不醒睡眠者，只有傳送、重生等 CPU 動作能喚醒。BonghuoVR 的魚有游動與擺動，永遠保持清醒，受影響的是沒有行為的碎片。
- **證據**：Unified-Solver 的睡眠每步寫回姿勢，求解器的 kernel 全部照跑（套件 plan §21：不省 GPU）；它的睡眠身體仍擋住其他物體，被推開超過喚醒距離才醒。ParticleWorld WORLD-10 要求睡眠不耗運算。這裡同時滿足兩者：省掉求解器對睡眠粒子的運算，接觸仍在。
- **驗證**：PEXT-CHECK-13。

## Volume（EXT-14）

- **規則**：
  - volume 是場景元件，形狀是 Transform 推導的有向方塊（中心 = 位置、半邊長 = lossyScale × 0.5、軸 = right、up、forward），帶一串效果 profile：
    - **介質**：密度（profile 質量單位，同 SOLVER-17）、流速、流速是否在本地座標、拖曳係數（預設 1）。每個步驟開始時，世界把登記中的介質 volume 轉成介質清單（排在腳本加入的介質之前），由 SOLVER-17 施力。
    - **邊界**：模式 Contain（預設）或 Exclude、淡出 0.5 s、淡入 0.5 s；由發射器的邊界回收 kernel 讀取。
  - volume 向它解析到的世界登記，登記先後無關（WORLD-02）。
  - 每個發射器的過濾照 Unified-Solver：發射器的生成區中心不在任何 Contain 內時，略過所有 Contain；生成區中心在某個 Exclude 內時，略過那個 Exclude；兩種情形各報一次警告。
- **證據**：套件 `Components/SolverVolume.cs`、`Profiles/SolverMediumProfile.cs`、`SolverBoundsProfile.cs`、`SolverParticleModifierRunner` 的過濾段落；BonghuoVR 的 volume 全是方塊。
- **驗證**：PEXT-CHECK-14。

## 與 Unified-Solver 擴充層的差異

| 項目 | Unified-Solver | 本規格 |
|---|---|---|
| 固定點的時機 | 每步在求解器前寫一次，子步驟內不動 | 每個子步驟依持有者姿勢內插（EXT-05） |
| 錨定的反質量 | 寫成 0 後不恢復 | 停用時恢復配置時的值（EXT-06） |
| 抓取數量 | 同步讀回 | 非同步讀回並標明步驟（EXT-07） |
| 執行順序 | `DefaultExecutionOrder` | 註冊順序（EXT-01） |
| 來源 | 包裝第三方 generator 並以反射讀私有欄位 | 驅動器的實體 handle（WORLD-12） |
| 介質 | 發射器自己的介質 kernel 施力，與其他物體分開 | 世界的介質受力一套，發射器只算狀態（EXT-12） |
| 睡眠 | 每步寫回姿勢，求解器照跑 | 睡眠粒子不進求解器 kernel，只留在接觸中（EXT-13） |
| 回收 | 只能重寫既有身體，不能釋放 | 可以釋放與重用槽位（WORLD-05）；邊界回收仍在 GPU 上瞬移 |

## 平台檢查

| 檢查 | 涵蓋 | 做法與預期 |
|---|---|---|
| PEXT-CHECK-01 | EXT-01、02 | 兩個步後擴充與一個子步驟擴充：每步的呼叫順序是準備、n 次子步驟、步後依註冊順序；在步驟中註冊的擴充下一步才被呼叫 |
| PEXT-CHECK-02 | EXT-01、04 | 子步驟擴充宣告寫入速度：註冊被拒並報錯；重複註冊同一物件不改變順序 |
| PEXT-CHECK-03 | EXT-02、03、WORLD-13 | 子步驟擴充把粒子推出 0.02 m 的薄板：3 m/s 的粒子不穿透（PWORLD-CHECK-15）；全部實體收起時擴充不被呼叫 |
| PEXT-CHECK-04 | EXT-05、06 | 鏈的一端錨在以 1 m/s 移動的 Transform 上：每步結束時端點在 Transform 的目標位置、速度 1 m/s；停用錨定後端點反質量恢復並開始下落 |
| PEXT-CHECK-05 | EXT-05、07 | 抓取網子中央後手平移 0.5 m 並繞 Y 轉 90°：抓到的粒子保持相對手的本地位置；放開時速度等於手在該點的速度 |
| PEXT-CHECK-06 | EXT-06 | 同一持有者綁三顆粒子：三顆保持編寫時的相對位置；來源收起時不處理、重設後重新固定 |
| PEXT-CHECK-07 | EXT-07、WORLD-14 | 抓到的數量以非同步讀回送達並標明步驟，與半徑內的粒子數相同；被錨定的粒子不被抓 |
| PEXT-CHECK-08 | EXT-08、WORLD-14 | 旋轉中的剛體碎片：以 GPU 上組成的點與矩陣求出的成員位置，與模擬的粒子位置一致；世界可綁定到 material 屬性 |
| PEXT-CHECK-10 | EXT-09、EXT-10 | 每種佈局生成一個身體：粒子的本地位置、約束對與剛體群與 Unified-Solver 的生成程式相同；身體 buffer 的起點、數量、佈局、旗標與實體一致；GPU 上的頭中尾座標框對直線身體給出預期的切線與側向 |
| PEXT-CHECK-11 | EXT-11 | 啟用時在生成區內生成 initialCount 個身體；容量不足時建立能放下的並報錯；停用收起、再啟用重生到生成區內；排入的生成要求在下一步配置 |
| PEXT-CHECK-12 | EXT-12 | 每個行為 kernel 一個案例，預期值依 Unified-Solver kernel 的公式推導：限速的指數衰減、滾轉阻尼移除的角速度比例、頭尾展開、擺動的位移總和為 0、跳躍的上升速度、游動的平均速度朝目標收斂、邊界回收淡出後回到生成區且淡入；執行順序與保持清醒的規則 |
| PEXT-CHECK-13 | EXT-13、WORLD-10 | 靜止的身體在睡眠延遲後入睡，入睡後不在求解器的運算範圍；另一顆以 2 m/s 撞上的粒子被擋住並喚醒它；行為啟用與傳送立刻喚醒 |
| PEXT-CHECK-14 | EXT-14、SOLVER-17 | 介質 volume 讓身體的浮沉與 SOLVER-17 案例一致；旋轉 volume 的本地流速；Contain 與 Exclude 的回收；發射器生成區在 Contain 外時報警告並略過 |
| PEXT-CHECK-09 | EXT-06 | 50 × 50 網子開啟編輯：滑鼠移動時不重建靜止位置、重繪不做點選；點選一顆粒子加入綁定、再點一次移除，兩次都可復原；本地位置等於靜止位置在持有者座標中的值 |

## 版本

規格版本與實作版本分開記錄。改變任何保證（時機、恢復規則、讀回方式）是新的次版本或主版本，並同時更新平台檢查。
