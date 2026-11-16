# ParticleRendering：粒子繪製

> 規格版本 1.0.1 · 2026-11-16 · 第 4 階段：網子、水管、魚身與碎片的繪製，含 VR 單通道立體繪製；Motion package 已實作，PREND-CHECK-01～06 在 Yu5h1LibTest EditMode（DX11）通過；頭盔上的 PREND-CHECK-07 尚未執行
>
> 1.0.1 依實作釐清三處用詞，行為不變：鏈帶寬度是中心線到單邊的距離（REND-04）；網格表面的顏色取自外觀，因為粒子沒有逐顆顏色（REND-03）；繪製範圍以世界的區段讀回一次取得（REND-01）。
>
> 與其他文件的關係：繪製讀取的資料與剛體群姿勢的組法由 [ParticleExtensions](ParticleExtensions.md) 的繪製存取（EXT-08）與身體來源（EXT-09）規定；粒子怎麼動由 [ParticleSolver](ParticleSolver.md) 與 [ParticleWorld](ParticleWorld.md) 規定。外觀來源是 Unified-Solver 套件的 `NetRenderer.shader`、`SolverArticulatedMesh.shader`、`SolverMeshRenderer.cs`、`SolverHullMesh.cs` 與第三方的 `RopeRenderer.shader`，差異列在〈與 Unified-Solver 的差異〉。遷移順序見 Motion repo 的 `Documentation/GpuPhysicsMigrationPlan.md` 第 4 階段。

## 系統階層與關係

```text
來源（PhysicsParticle 驅動器、ParticleEmitter 發射器）
└─ 外觀 profile（第 2 階段已有資料與相容性檢查）
   ├─ 網格表面（REND-03）：網子與布料
   ├─ 鏈帶（REND-04）：水管與繩子
   ├─ 關節網格（REND-05）：魚身
   └─ 剛體網格（REND-06）：碎片的凸包或編寫的網格
繪製元件（REND-01）：讀來源的外觀，每幀從 GPU 資料直接繪製
```

## 目標

- **為誰服務**：BonghuoVR 的捕撈表演：網子（60 × 44）、水管（59 段）、魚群（最多 512 條，GuideChain4）、冰塊碎片。
- **情境**：
  - 網子、水管、魚、碎片都直接從模擬資料繪製，CPU 不讀回、不組矩陣（REND-01、REND-06）。
  - 頭盔改成單通道立體繪製（Single Pass Instanced）後，四種外觀在兩眼都正確出現（REND-02）。
  - 繪製範圍跟著物體走，陰影的裁切正常（REND-01）。
  - 外觀與 Unified-Solver 版本看起來相同，BonghuoVR 切換時不必重做美術（REND-03～REND-06）。
- **不在此目標**：除錯用的粒子球與約束線（BonghuoVR 沒有使用）；改用 URP 光照或改變美術外觀（屬表演或美術決定，BonghuoVR `效能評估.md`〈需要表演或美術決定〉）。
- **完成條件**：本文件的平台檢查通過；頭盔上的單通道檢查交給使用者。

## 繪製元件（REND-01）

- **規則**：
  - 繪製由 `PhysicsParticleRenderer` 元件負責：它讀同一個 GameObject 上的身體來源（`IParticleBodySource`），依來源的外觀 profile 繪製來源的實體。來源有外觀時自己確保這個元件存在（驅動器與發射器各一行），不需要使用者另外加。
  - 每幀以世界的 `TryBind(MaterialPropertyBlock)` 綁定粒子與剛體群，shader 以 `PARTICLE_WORLD_READ_ONLY` 引入 `ParticleWorldData.hlsl`（魚身另引入 `ParticleBodyFrame.hlsl`）。不讀回粒子到 CPU，不在 CPU 組矩陣。
  - 收起與已釋放的實體不畫；睡眠中的實體照常畫。
  - 繪製範圍：先用佈局的靜止範圍放在生成姿勢；之後以世界的區段讀回（`ParticleWorld.RequestPositions`，涵蓋所有繪製中實體的槽位，一次請求）取得實體粒子的範圍，每秒最多 4 次，加上粒子半徑與一個邊距。不使用固定的 1000 m 範圍。
  - 外觀 profile 新增是否投射陰影、是否接收陰影（預設都開），繪製圖層是來源 GameObject 的 layer。
- **證據**：Unified-Solver 的繪製範圍固定為 1000 m，陰影裁切因此失效（BonghuoVR `效能評估.md`）；剛體路徑每幀在 CPU 組矩陣（`SolverMeshRenderer.cs`）。
- **驗證**：PREND-CHECK-01。

## 單通道立體繪製（REND-02）

- **規則**：
  - Motion package 的每個繪製 shader（含陰影 pass）支援 Single Pass Instanced 與 Multiview，並維持多通道可用：頂點輸入宣告實例 ID、輸出宣告立體輸出、片段階段設定眼睛索引，並加上 `multi_compile_instancing`。
  - 以實例 ID 當作身體編號的 shader，在單通道下使用 Unity 巨集取出真正的實例編號，不能直接用 `SV_InstanceID`。
  - 依視點計算的部分（網子雙面法線翻轉、水管朝向攝影機）使用該眼睛的攝影機位置。
- **證據**：Unified-Solver 的六個自訂 shader 都沒有立體實例化的宣告，切到單通道時只會出現在一隻眼睛（BonghuoVR `效能評估.md` 第 43、76 行；目前 OpenXR 設定為多通道）。
- **驗證**：PREND-CHECK-02、PREND-CHECK-07（頭盔，交給使用者）。

## 網格表面（REND-03）

- **規則**：
  - 外觀 profile `GridSurfaceAppearance`，用於網格佈局（`GridParticleLayout`）的實體：每顆粒子是一個頂點，相鄰四顆組成兩個三角形（雙面時四個），頂點位置直接讀粒子，顏色取自外觀（Unified-Solver 的網子 fragment 也不使用逐顆粒子顏色）；法線由上下左右鄰居的差計算，邊緣用單側差。
  - 外觀欄位照 `NetRenderer.shader`：顏色、網目貼圖與是否使用、網目樣式（方格、菱形）、網目重複數（預設 20 × 20）、線寬、透明裁切值、邊框顏色與寬度、金屬度、平滑度；不使用網目時是一般布料（不透明）。
  - 著色照 `NetRenderer.shader`：雙面（法線翻向攝影機）、固定光線方向 (0.5, 1, 0.3)、0.3 + 0.7 × N·L 的漫射加 Blinn-Phong 高光；陰影 pass 套用同樣的網目裁切。
- **證據**：套件 `Shaders/NetRenderer.shader`、第三方 `ClothGenerator.cs`（網格與 UV）。
- **驗證**：PREND-CHECK-03。

## 鏈帶（REND-04）

- **規則**：
  - 外觀 profile `ChainStripAppearance`，用於鏈佈局的實體：每顆粒子兩個頂點，沿鏈組成面向攝影機的帶子；切線是鄰居的中央差（端點單側差），帶寬是中心線到單邊的距離（預設 0.03 m，總寬 0.06 m，與 `RopeRenderer` 的 `_RopeWidth` 相同），兩側依 cross(切線, 視線) 展開。
  - 著色照 `RopeRenderer.shader`：單一顏色、橫向 1 − edge² × 0.4 的明暗、不受光、雙面。
  - 外觀可以開啟陰影投射（Unified-Solver 的水管沒有陰影 pass；這裡補上，預設關閉以維持原樣）。
- **證據**：第三方 `RopeRenderer.shader`、BonghuoVR `Fishing Hose.prefab`（59 段、寬 0.03）。
- **驗證**：PREND-CHECK-04。

## 關節網格（REND-05）

- **規則**：
  - 外觀 profile 是既有的 `ArticulatedMeshAppearance`，用於身體來源的 Chain3、GuideChain4、DualRail6、ArticulatedCluster12 身體：一次 instanced 繪製畫出來源的所有身體，實例編號就是身體編號。
  - 頂點變形照 `SolverArticulatedMesh.shader` 的 DeformVertex：以 `ParticleBodyFrame.hlsl` 的頭、中、尾座標框，依頂點在前進軸上的位置（網格範圍內 0～1）在尾與中、中與頭之間以 smoothstep 混合；外觀的前進軸與反向、依身體尺寸縮放網格（預設開啟）、視覺縮放照 `SolverRenderProfile`。
  - 生命週期的隱藏比例讓網格縮向中段中心（`_Lifecycle.z`），沒有生命週期 buffer 時全部可見。
  - 顏色 = 貼圖 × 外觀顏色 × 身體顏色（EXT-09）；著色照原 shader（固定光線方向、陰影 pass）。
- **證據**：套件 `Shaders/SolverArticulatedMesh.shader`、`SolverBodyFrame.hlsl`、`Rendering/SolverMeshRenderer.cs` 的 DrawArticulated。
- **驗證**：PREND-CHECK-05。

## 剛體網格（REND-06）

- **規則**：
  - 外觀 profile 是既有的 `ParticleHullAppearance` 與 `AuthoredMeshAppearance`，用於帶剛體群的實體：凸包外觀以佈局變體（或形狀來源模板）的靜止粒子建出平面著色的凸包網格，頂點沿重心方向外推一個粒子半徑；編寫網格外觀直接用外觀的網格，依前進軸與尺寸配合。每個變體一個網格、只建一次，佈局改變才重建。
  - 姿勢在 GPU 上組成（EXT-08 的 RigidGroupMatrix）：Motion package 提供一個 procedural instancing 的 include，讓 shader 以 `#pragma instancing_options procedural:` 從剛體群設定物件矩陣；附一個使用它的預設 shader（固定光線方向、陰影 pass）。URP Shader Graph 可以透過 Custom Function 呼叫同一個 include。
  - 每個變體一次 instanced 繪製；實例對應的剛體群編號以一個 GPU buffer 傳入，實體清單改變才重建。
  - 生命週期的隱藏比例讓碎片縮小到重心（補上 Unified-Solver 剛體路徑缺少的淡出）。
- **證據**：套件 `SolverMeshRenderer.cs` 的 DrawRigid（每幀在 CPU 組矩陣、`RenderMeshInstanced` 每批 1023）、`SolverHullMesh.cs`、`plan.md` 中剛體路徑沒有回收淡出的已知問題。
- **驗證**：PREND-CHECK-06。

## 與 Unified-Solver 的差異

| 項目 | Unified-Solver | 本規格 |
|---|---|---|
| 剛體碎片姿勢 | 每步同步讀回、每幀 CPU 組矩陣 | GPU 上組成（REND-06） |
| 碎片淡出 | 沒有 | 依生命週期縮小（REND-06） |
| 繪製範圍 | 固定 1000 m | 依非同步讀回的實際範圍（REND-01） |
| 單通道立體 | 不支援 | 支援（REND-02） |
| 繪製入口 | generator 的 MeshRenderer 與發射器的隱藏元件 | 一個繪製元件，讀來源的外觀 profile（REND-01） |

## 平台檢查

| 檢查 | 涵蓋 | 做法與預期 |
|---|---|---|
| PREND-CHECK-01 | REND-01 | 驅動器設了外觀時自動有繪製元件；整個測試期間沒有同步讀回；收起的實體不畫；讀回後的繪製範圍包住所有粒子、不超過實際範圍加邊距 |
| PREND-CHECK-02 | REND-02 | 每個 shader 在開啟 `UNITY_STEREO_INSTANCING_ENABLED` 與 `UNITY_STEREO_MULTIVIEW_ENABLED` 時編譯無錯誤；原始碼含實例 ID、立體輸出與眼睛索引的宣告 |
| PREND-CHECK-03 | REND-03 | 4 × 3 網格的索引數與三角形方向正確；以攝影機繪製到 RenderTexture，網子在預期位置有像素、網目孔洞透出背景 |
| PREND-CHECK-04 | REND-04 | 3 顆粒子的鏈畫出寬度正確、面向攝影機的帶子（RenderTexture 取樣） |
| PREND-CHECK-05 | REND-05 | 直線 GuideChain4 身體的網格畫在身體位置、方向沿身體；隱藏比例 1 時縮成中段一點 |
| PREND-CHECK-06 | REND-06 | 旋轉中的碎片：凸包網格畫在剛體群姿勢上（RenderTexture 取樣與 EXT-08 的姿勢一致）；凸包頂點數與面數符合變體 |
| PREND-CHECK-07 | REND-02 | 頭盔上切到 Single Pass Instanced，網子、水管、魚、碎片在兩眼都正確出現（交給使用者，記在 checklist） |

## 版本

規格版本與實作版本分開記錄。改變任何保證（資料來源、立體支援、外觀欄位）是新的次版本或主版本，並同時更新平台檢查。
