# UnityExtension 開發日誌

## 2026-07-27 — Particle System 物理碰撞互動

### 嘗試

為 Unity `ParticleSystem` 製作 `ParticleSystemRigidbody` 元件，透過 `IJobParticleSystem` 在 CPU 端加入粒子彼此之間的球形碰撞、重疊修正、反彈與摩擦。

### 結果

Particle System 的 C# Job 操作仍在 CPU 執行。粒子碰撞需要同時讀寫碰撞雙方，限制了直接平行化；大量粒子或密集堆積時，碰撞配對與反覆求解的成本也會快速增加。

實際結果在碰撞穩定度、堆積效果與執行效能上都未達需求，因此不再繼續擴充 `ParticleSystemRigidbody`，相關實作已從 UnityExtension 移除。

### 決策

大量物件的碰撞、堆積與容器互動改由 `unified-solver` 處理，不再以 Unity Particle System 的 CPU Job 路線實作。
