# Recycler 重構計畫（Pool / Spawn 系列統合）

> 階段：**問題盤點**。架構、抽象、命名的方案**不在本文件討論** — 留待後續 session。
> 使用者偏好以 **Recycler** 取代 Pool 作為主要命名（最終決議待後續）。

---

## 目標

把目前 Yu5h1Lib 與專案內**三個世代**的物件池/回收實作，統合成一套**單一、可長期維護**的設計。

**成功條件**：

1. 一份核心 API，覆蓋目前三套各自負責的情境
2. 命名一致（Recycler 系列），無 `Pool` / `PooL` / `Recyclable` 混雜
3. 抽象層次清楚 — 「儲存／生命週期／查找／回收觸發／來源類型」可獨立替換
4. 舊三套可以 deprecate 並提供 migration 路徑（不一定要逐字對應，但行為要能對接）
5. 不繼承 `List<T>`、不 mutate 使用者場景物件、不在 spawned 物件上自動 `AddComponent`（除非 opt-in）

---

## 歷史版本盤點

| 版本 | 檔案 | 年代 | 狀態 |
|------|------|------|------|
| V1 | `Yu5h1Lib/Unity/Runtime/Base/Unorganized/Singleton/Recycler.cs` | 2017 | 仍存在，包含 `Recyclable` / `RecyclableByEnumerator` / `RecyclableParticleSystems` / `RecyclableBySeconds` |
| V2 | `Yu5h1Lib/Unity/CombatAesthetic/Runtime/Pattern/Pool/PooL.cs` | 2024 | **整檔註解掉** — V2 的廢棄前身 |
| V2 | `Yu5h1Lib/Unity/CombatAesthetic/Runtime/Pattern/Pool/ComponentPooL.cs` | 2024 | 主實作 (`ComponentPool` + `Pool<T>`) |
| V2 | `Yu5h1Lib/Unity/CombatAesthetic/Runtime/Pattern/Pool/PoolManager.cs` | 2024 | Singleton + 三套靜態字典 |
| V2 | `Yu5h1Lib/Unity/CombatAesthetic/Runtime/Pattern/Pool/PoolElementHandler.cs` | 2024 | per-instance 回收用 MonoBehaviour |
| V2 | `Yu5h1Lib/Unity/CombatAesthetic/Runtime/Pattern/Pool/MeshPool.cs` | 2024 | 包裝 Unity 內建 `ObjectPool<Mesh>` 的靜態類 |
| V3 | `Yu5h1Lib/Unity/UnityExtension/Packages/common/Runtime/Pool/SpawnPool.cs` | 2026 | 場景內 pre-place + 不增長 |
| V3 | `Yu5h1Lib/Unity/UnityExtension/Packages/common/Runtime/Pool/SpawnPoolProxy.cs` | 2026 | UnityEvent 接口 |
| V3 | `Yu5h1Lib/Unity/UnityExtension/Packages/common/Runtime/Pool/SpawnPoolRegistry.cs` | 2026 | 靜態 registry + Play Mode reset |

---

## V1 (Recycler 2017) 問題

1. **`Recyclable<T> : List<T>`**（line 71）— 繼承容器是 anti-pattern；active 集合應為組合 field
2. **GameObject.name 當 dictionary key**（line 23, 33）— 重命名/重複名稱直接壞
3. **`Push` 的 error message 邏輯反了**（line 127）— `"Push unknow Object"` 實際發生時是「已 push 過」
4. **沒有 try-spawn 語意** — `Pull()` 必成功；pool 滿時走 `Push(this[0]); Pull();` 強制 evict 最舊
5. **三層深泛型繼承** — `Recyclable<T>` → `Recyclable` → `RecyclableByEnumerator<T>` → `RecyclableBySeconds`，職責難切分
6. **`IRecyclable.Count` 介面要求** 跟 `List<T>.Count` 重複
7. `Recyclable<T> where T : class, new()` 限制 T 必須有無參 ctor — 對 GameObject/Component 沒意義（用 `Instantiate` 而非 `new`）
8. **每 Pull / Push 改 parent** — 沒有可關閉的 hierarchy 操作開銷
9. `RecyclableByEnumerator` 用 Coroutine — 綁死 MonoBehaviour singleton，無法用於 edit mode 或非場景情境
10. **`Init(GameObject[])` 與 `Init(RecyclableGameObject[])` 兩個重載** — `RecyclableGameObject` 的 `TypeOfRecyclable` enum 寫死兩種（Timer / ParticleSystem），擴充等於改 enum
11. 全 public mutable field（如 `Recyclable.limit`）— 無 invariant 保護

---

## V2 (Pool 2024) 問題

### 命名

12. **`PooL` / `ComponentPooL`** 類別名拼寫怪異（`L` 大寫）— 看不出是設計還是 typo；檔名也 inconsistent
13. **`Pool<T>` / `ComponentPool` / `PoolManager` 三類同名根概念** — `Pool<T>` 是只覆寫 Source 的薄殼，沒清楚定位

### 架構

14. **`PoolManager` 三套查找鍵** — `pools[Component]` / `TypeMaps[Type]` / `NameMaps[string]` 並存，三 source of truth 同步成本高
15. **靜態字典 + Singleton 並存** — `instance._pools` 透過 instance 取，但 `TypeMaps` / `NameMaps` / `element_source_Maps` 是 static — 跨 scene 行為不一致
16. **`OnDestroy` 才清靜態 dict** — Domain Reload disabled 模式下若重 enter Play 不會清乾淨
17. **`PoolElementHandler` 與 `element_source_Maps` 重複機制** — 兩者都解決「從元素回查 pool」，存在卻沒互斥
18. **`Spawn<T>` 三重載** — 同名不同 dispatch（type / name / source ref），呼叫者要記哪個用哪個
19. **`Despawn` 對未知元素 silently destroy**（line 170）— 副作用大，無 opt-in
20. **`Spawn` fallback 路徑會 `Create<T>` 但是「unmanaged」instance**（line 158-162）— 創出來不加進 `elements`/`list`，直接漏

### 設計細節

21. **`Capacity` 一個值多重語意** — 同時是 growth limit / FIFO 觸發點 / create-fallback 觸發點
22. **`UseFIFO` 用 `Queue<Component> history` 維護** — 實際做法是「滿了就 Despawn 最舊」，但 `history` 在 `IsEmpty` 才一次填滿、一直 enqueue 不 dequeue → 演算法可疑，需重審
23. **`PoolManager.canvas`** — Manager 內建 UI 特例（RectTransform 偵測 → 自動建 Canvas from Resources），破壞單一職責
24. **`ComponentPool` 標 `[Serializable]` 並有 `[SerializeField]`** 但專案內無 serialized 引用 — 屬性無意義且妨礙重構
25. **`source.gameObject.IsBelongToActiveScene()` → 自動 SetActive(false) + reparent**（line 62-66）— 自動 mutate 使用者場景物件，行為不可預期
26. **`element.GetOrAdd(out PoolElementHandler handler)`**（line 85）— 自動 AddComponent，無 opt-out
27. **`init` event 是 `Action<Component>`** — callback 拿不到 typed `T`
28. **`where T : Component` 寫死**（API 多處）— 無法用於 ScriptableObject、純 C# 物件、純 GameObject
29. **每次 Despawn 都 `SetParent(parent, false)`**（line 177）— 共通情況下無變動仍付 hierarchy 開銷
30. **`MeshPool` 全 static** — 包裝 `ObjectPool<Mesh>`，但 `Dispose()` 不會被自動呼叫；scene 切換 / domain reload 後可能持有失效引用

### 錯誤處理

31. 多處用 `printWarningIf` / `printErrorIf` — 把 log 副作用混進 boolean 判斷裡，可讀性差且難 unit test
32. `Spawn` 在 `elements.IsEmpty()` 時 throw `InvalidOperationException`（line 116）— 但 caller 通常無從預知，沒有 `TrySpawn` 對應

---

## V3 (SpawnPool 2026) 問題

> V3 是為單一場景情境（VR demo 預先擺放魚群）寫的，**範圍刻意縮小**，因此「問題」其實是「未涵蓋的維度」。

33. **只支援 GameObject** — 沒有 Component / ScriptableObject / 純 C# 物件支援
34. **不從 prefab Instantiate** — 與 V1/V2 paradigm 相反，無法做為通用替代
35. **不能成長、不能 evict** — 只回 false。對需要 dynamic 規模的情境不夠
36. **沒有 spawn-time hook** — 沒有 V2 的 `init` / `beginSpawn` 等 callback
37. **沒有 auto-recycle 機制** — 依賴外部 Timer / 自呼叫 `SetActive(false)`；V1 的 `RecyclableBySeconds` / `RecyclableParticleSystems` 模式沒繼承
38. **Registry 用字串 key** — 跟 V1 / V2 同樣的 stringly-typed 問題（雖然這次是顯式 SerializeField，比 GameObject.name 好）
39. **未處理 reparent 場景** — Recycle 不 reparent 回 pool，spawn 後若被外部移走，外部 parent 死掉時 instance 跟著死

---

## 跨版本反覆出現的設計痼疾

A. **字串 / 名稱當 key** — V1 (GameObject.name)、V2 (NameMaps)、V3 (SerializeField string)。三代都用，refactor 安全性低
B. **「儲存」與「生命週期」黏在一起** — 三版的 Pool 同時負責「我有多少 instance」、「何時建/銷」、「何時長大/淘汰」、「如何分派」。職責沒切
C. **泛型 T 處理不一致** — V1 `Recyclable<T>`、V2 `Pool<T>` 半成品、V3 純非泛型。沒一個版本把 T 一路帶到 callback / Spawn 出口
D. **Eviction / Growth 策略寫死或不可選** — V1 強制 evict-oldest、V2 三種 fallback 混合 (grow / FIFO / unmanaged)、V3 hard-fail。三代都缺「可替換策略」設計
E. **Auto-recycle 觸發機制散在子類** — V1 在 `RecyclableByEnumerator` 子類做、V2 用 `PoolElementHandler` UnityEvent、V3 完全外推
F. **Singleton vs Static 反覆** — V1 SingletonBehaviour、V2 SingletonBehaviour + 靜態 dict 並存、V3 純靜態。沒定論
G. **Hierarchy 操作隱含開銷** — V1/V2 每 spawn/despawn 都 reparent；無「保持原 parent」選項
H. **副作用沒有 opt-out** — 自動 SetActive、自動 AddComponent、自動 reparent、自動 destroy 未知元素、自動建 Canvas

---

## 設計決議 — Phase 1（議題 1, 2, 3, 7）

> 決於 2026-05-11 session。**Core 只放介面（角色契約），實體都在 Unity / 應用層。**

### 命名

- `Recycler` / `IRecyclable` 直接奪名 — V1 同名檔（`Recycler.cs` / `Recyclable*.cs`）全刪，無相容包袱
- 動詞 **`Get` / `Return`**（對齊 `Microsoft.Extensions.ObjectPool`）
- `Spawn` / `Despawn` 退位為遊戲語境別名，不進核心；Unity 層**暫不改**（使用者要求避免大規模 break，待 Core 落地驗證後再評估是否加 alias extension）
- callback `OnAcquired` / `OnReleased` — 中性、被動完成式（避開 `OnGet` 像 getter 的歧義）

### 抽象骨架（4 個 Core 介面）

| 介面 | 角色 |
|------|------|
| `IRecyclable` | 物件側 marker + opt-in 生命週期回呼 |
| `IRecyclerSource<T>` | 工廠（與 storage 分離 — 解 V2 職責糾纏） |
| `IRecycler<T>` | Pool 本體 |
| `IRecyclerRegistry<TKey>` | 依 key 查找 + 終端使用者 facade |

延後 phase 2：`IEvictionPolicy` / `IGrowthPolicy`（議題 5）、`IRecycleTrigger`（議題 6）、storage 介面（保留 Recycler 內部細節）。

### 泛型策略

- `IRecycler<T>` 單一介面走到底，**不**分裂為 GameObjectRecycler / ComponentRecycler<T>
- `T` 無 constraint — 純 C# 物件、ScriptableObject、GameObject、Component 共用同一抽象
- Unity 具體 class 才填具體 T（`GameObjectRecycler : IRecycler<GameObject>` 等）

### Lookup key

- TKey 開放在 `IRecyclerRegistry<TKey>`
- 預期 Unity 層用 `UnityEngine.Object`（prefab 引用即 key — refactor 安全、type-safe、IDE 可追蹤）
- 純 .NET 情境若需要再加 `RecyclerRegistry<Type>` 第二實作
- ❌ 不接受 `string` key（line 107 A 點：三代反覆痼疾）

### Registry API 切分

使用者拍板：**終端使用者只看見單一 `Get<T>(key)`**；`TryGetRecycler + recycler.Get` 的組合由框架內部處理。

`TryGet<T>(key, out T)` 與 `TryGetRecycler<T>(key, out IRecycler<T>)` 命名刻意分開 — 避開 overload 解析 ambiguity。

### Core 完整介面草案

```csharp
namespace Yu5h1Lib.Recycling;

public interface IRecyclable
{
    void OnAcquired();
    void OnReleased();
}

public interface IRecyclerSource<out T>
{
    T Create();
}

public interface IRecycler<T>
{
    int CountActive { get; }
    int CountAvailable { get; }

    T Get();                           // 取不到會 throw
    bool TryGet(out T instance);       // V1/V2 缺的 try-語意
    void Return(T instance);
    void Clear();
}

public interface IRecyclerRegistry<TKey>
{
    // 終端使用者面 — 多數 callsite 只用這三個
    T Get<T>(TKey key);
    bool TryGet<T>(TKey key, out T instance);
    void Return<T>(TKey key, T instance);

    // 框架 / 進階 — 批次操作或 introspection 才用
    IRecycler<T> Register<T>(TKey key, IRecyclerSource<T> source);
    bool TryGetRecycler<T>(TKey key, out IRecycler<T> recycler);
}
```

### 放置 / 拆檔

- 路徑：`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\DotNet\Source\Recycling\`
- 一檔一介面：`IRecyclable.cs` / `IRecyclerSource.cs` / `IRecycler.cs` / `IRecyclerRegistry.cs`
- Unity packages 此階段**不動**

### 進入下一階段的條件

1. 使用者驗證介面（在 IDE 看 surface、能否寫出想用的 callsite）
2. 介面落地後再開議題 5 / 6（policy / trigger）
3. policy 鎖定後才談 migration（議題 10）— Unity 層在此之前不動

---

## 設計修訂 — Phase 1.1（2026-05-19 起；2026-05-20 收斂）

> ⚠️ **本節 API 已被下方「Phase 1.2」取代**（2026-05-21 Core 實作時又收斂三件大事）。本節保留作設計軌跡,**最新且已實作的設計以 Phase 1.2 為準**。
>
> 經多輪 callsite 驗證收斂。中間迭代過 `IAllocator` 命名、abstract class 路線、`protected abstract T Create()` 等版本均被取代。

### 介面收斂 — 4 → 2

| 原 Phase 1（2026-05-11） | Phase 1.1 修訂 | 原因 |
|------------------------|---------------|------|
| `IRecyclerSource<T>` 獨立介面 | **取消** — Source 內化為 `IRecycler.Source` property，「怎麼生」由 `Recycler<T>` ctor 收 `Func<T> factory` 注入 | 沒人會單獨持有 Source 引用；獨立介面只多一層抽象沒實益 |
| `IRecyclerRegistry<TKey>` 介面 | **取消** — 改 `class Recycler`（非 static 非 abstract）host 兩個 Dictionary + 一個 `TryBuildRecyclerDelegate` | Registry 永遠單例，介面+多實作可能性近零 |
| `IRecycler<T>` 介面 | **保留** + 拆出非泛型 `IRecycler` base | 包 Unity `IObjectPool<T>` / 第三方 pool 必要；非泛型 base 給 Dictionary value 用 |
| `IRecyclable` 介面 | **保留** — opt-in callback contract | 不變 |

### 4 個檔案

`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\DotNet\Source\Recycling\`，`namespace Yu5h1Lib.Recycling`

```csharp
// 1. IRecyclable.cs
public interface IRecyclable {
    void OnAcquired();
    void OnReleased();
}

// 2. IRecycler.cs（一檔放兩個介面）
public interface IRecycler {
    object Source { get; }
    int CountActive { get; }
    int CountAvailable { get; }
    void Clear();
}
public interface IRecycler<T> : IRecycler {
    T Get();
    bool TryGet(out T instance);
    void Return(T instance);
}

// 3. Recycler.cs — 非 static、非 abstract（Recycler<T> : Recycler 才能繼承）
//                  ctor 設 protected：外部 new Recycler() 編譯不過
public class Recycler {
    public delegate bool TryBuildRecycler(Type type, out IRecycler recycler);

    static readonly Dictionary<Type, IRecycler> typeRecyclers = new();
    static readonly Dictionary<object, IRecycler> sourceRecyclers = new();
    public static TryBuildRecycler TryBuild;

    protected Recycler() { }   // 禁止外部 new Recycler()，僅 Recycler<T> 能 chain

    public static void Register(IRecycler r) => sourceRecyclers[r.Source] = r;

    public static bool TryGet<T>(out T result) {
        if (!typeRecyclers.TryGetValue(typeof(T), out var r)) {
            if (TryBuild == null || !TryBuild(typeof(T), out r)) {
                result = default; return false;
            }
            typeRecyclers[typeof(T)] = r;
        }
        return ((IRecycler<T>)r).TryGet(out result);
    }

    public static bool TryGet<T>(object source, out T result) {
        if (!sourceRecyclers.TryGetValue(source, out var r)) {
            result = default; return false;
        }
        return ((IRecycler<T>)r).TryGet(out result);
    }

    public static void Return<T>(T instance) {
        if (typeRecyclers.TryGetValue(typeof(T), out var r))
            ((IRecycler<T>)r).Return(instance);
    }

    public static void Return<T>(object source, T instance) {
        if (sourceRecyclers.TryGetValue(source, out var r))
            ((IRecycler<T>)r).Return(instance);
    }

    public static void ClearAll() {
        foreach (var r in typeRecyclers.Values) r.Clear();
        foreach (var r in sourceRecyclers.Values) r.Clear();
    }

    // 把 user 提供的 Func<object> / Action<object> 包成 Func<T> / Action<T> 並建出 Recycler<T>
    // user 端 TryBuild 內不用碰 MakeGenericType / Activator / typed delegate
    public static IRecycler Build(Type type, object source,
                                  Func<object> factory,
                                  Action<object> onDiscard = null) {
        var m = typeof(Recycler)
            .GetMethod(nameof(BuildInternal), BindingFlags.Static | BindingFlags.NonPublic)
            .MakeGenericMethod(type);
        return (IRecycler)m.Invoke(null, new object[] { source, factory, onDiscard });
    }

    static IRecycler BuildInternal<T>(object source, Func<object> factory, Action<object> onDiscard)
        => new Recycler<T>(source, () => (T)factory(),
                           onDiscard == null ? null : x => onDiscard(x));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() {
        typeRecyclers.Clear();
        sourceRecyclers.Clear();
        TryBuild = null;
    }
}

// 4. Recycler{T}.cs — concrete 非 abstract，THE 標準可用 pool
public class Recycler<T> : Recycler, IRecycler<T> {
    public object Source { get; }
    public int MaxAvailable { get; init; } = int.MaxValue;
    public int CountActive => active.Count;
    public int CountAvailable => available.Count;

    readonly HashSet<T> active = new();
    readonly Stack<T> available = new();
    readonly Func<T> factory;
    readonly Action<T> onDiscard;

    public Recycler(object source, Func<T> factory, Action<T> onDiscard = null) {
        Source = source;
        this.factory = factory;
        this.onDiscard = onDiscard;
    }

    public T Get() {
        var instance = available.Count > 0 ? available.Pop() : factory();
        active.Add(instance);
        if (instance is IRecyclable r) r.OnAcquired();
        return instance;
    }

    public bool TryGet(out T instance) {
        if (available.Count == 0 && factory == null) {
            instance = default; return false;
        }
        instance = Get();   // factory 自己的例外不吞，讓 bug 大聲冒出來
        return true;
    }

    public void Return(T instance) {
        if (!active.Remove(instance)) {
            System.Diagnostics.Debug.WriteLine(
                $"Recycler<{typeof(T).Name}>.Return: instance 不在 active 集合，已忽略");
            return;
        }
        if (instance is IRecyclable r) r.OnReleased();
        if (available.Count >= MaxAvailable) onDiscard?.Invoke(instance);
        else available.Push(instance);
    }

    public void Clear() => available.Clear();
    public void Prewarm(int count) { for (int i = available.Count; i < count; i++) available.Push(factory()); }
}
```

### 設計關鍵點

#### `Recycler` 非 static 非 abstract，但 ctor `protected`

需要 `Recycler<T> : Recycler` 繼承所以不能 static。ctor 設 `protected` 禁止外部 `new Recycler()`，僅 `Recycler<T>` 子類能 chain。使用者透過 `Recycler.TryGet<T>(...)` / `new Recycler<T>(...)` 使用。

#### Lookup 雙路徑 + 不對稱性

| 路徑 | 找不到時 |
|------|---------|
| `TryGet<T>(out)` by Type | 走 `TryBuild(typeof(T), out)` lazy 建立；失敗則 false |
| `TryGet<T>(source, out)` by source | **直接 false，無 fallback** — 從任意 object 反推不出 builder |

source-keyed pool 必須**顯式** `Register`，type-keyed 才有 lazy build。

#### `TryBuild` 由庫使用者實作 dispatch；`Recycler.Build` 隱藏反射

```csharp
// Unity 啟動時設定（[RuntimeInitializeOnLoadMethod]）
Recycler.TryBuild = (Type type, out IRecycler r) => {
    if (typeof(Component).IsAssignableFrom(type)) {
        var prefab = new GameObject($"{type.Name}(default)").AddComponent(type);
        r = Recycler.Build(type, prefab,
            () => Object.Instantiate(prefab),          // Func<object>
            obj => Object.Destroy((Component)obj));    // Action<object>
        return true;
    }
    r = null; return false;
};
```

Core 不知道 Unity、prefab、AddComponent — 所有 type→recycler 映射邏輯由使用者 `TryBuild` 內部 dispatch。但 `Recycler.Build(type, source, factory, onDiscard)` 把 `MakeGenericType` + `Activator.CreateInstance` + typed delegate 包裝全包進 Core，使用者只需提供 `Func<object>` / `Action<object>`，**零反射**。

#### 不做反查表

不維護 `Dictionary<object instance, IRecycler>` reverse map — V2 痼疾 17 已驗證代價：
- instance 變 GC root，使用者忘 Return 即 leak
- 額外字典同步成本

`Return<T>(T instance)` 無 source 版只在 Type-keyed 情境合法（用 `typeof(T)` 找 `typeRecyclers`，一個 Type 對一個 Recycler，unambiguous）。

#### Wrapper 不繼承 `Recycler<T>`，直接實作 `IRecycler<T>`

避免帶 `active` / `available` / `factory` / `onDiscard` 無用儲存。Dictionary value type 是 `IRecycler`（非泛型介面），裝得下 `Recycler<T>` 子類 + Wrapper 兩條路。

```csharp
// Unity 層（Phase 1.5）
public class UnityObjectPoolAdapter<T> : IRecycler<T> where T : class {
    readonly IObjectPool<T> inner;
    public object Source { get; }
    public int CountActive => inner.CountAll - inner.CountInactive;
    public int CountAvailable => inner.CountInactive;

    public UnityObjectPoolAdapter(object source, IObjectPool<T> inner) {
        Source = source; this.inner = inner;
    }
    public T Get() { var i = inner.Get(); (i as IRecyclable)?.OnAcquired(); return i; }
    public bool TryGet(out T instance) { instance = inner.Get(); return instance != null; }
    public void Return(T i) { (i as IRecyclable)?.OnReleased(); inner.Release(i); }
    public void Clear() => inner.Clear();
}
```

### 兩個應用面 + Wrapper

| 應用面 | 寫法 | 說明 |
|--------|------|------|
| **InstanceSource Recycler** | `new Recycler<Enemy>(prefab, () => Object.Instantiate(prefab), Object.Destroy)` + `Recycler.Register(...)` | Source = prefab；factory = Instantiate；onDiscard = Destroy |
| **TypeRecycler** | 由 `Recycler.trybuild` lazy 建構 — 使用者實作 builder 內 `new Recycler<T>(...)` | 適合純 C# 物件、ScriptableObject、無 prefab Component |
| **Wrapper** | `new UnityObjectPoolAdapter<Mesh>(key, meshObjectPool)` + `Recycler.Register(...)` | 包 `UnityEngine.Pool.IObjectPool<T>` |

### OnAcquired / OnReleased 呼叫位置

**由 `Recycler<T>` 的 `Get` / `Return` 內呼叫，Wrapper 自己的 Get/Return 也呼叫一次**。不在 static facade — 使用者直接持有 `IRecycler<T>` 引用不走 facade 時也要正常運作。

### Capacity 進 Phase 1（簡單版）

- `MaxAvailable` 純粹「pool 最多保留 N 個 idle instance」上限
- Active 數量**不限**
- Return 時 pool 滿 → `onDiscard?.Invoke(instance)`（Prefab pool callsite：`Object.Destroy`；純 C# 場景：null = 留給 GC）
- `Prewarm(count)` — V2 `Prepare` 對應
- ❌ Phase 1 不放：FIFO / LRU / Grow / HardFail 策略（議題 5 延 Phase 2）
- ❌ V2 `Capacity` 一值塞三種語意（痼疾 21）**不繼承**

### Naming 細節

- `factory` / `onDiscard` Action 風格對齊 Unity `ObjectPool` 的 `createFunc` / `actionOnDestroy`
- `Get` / `Return` — 對外動詞不變
- `IDisposable` 不進 `IRecycler<T>`，由需要的 Wrapper 自選實作
- `Clear()` vs `Dispose()`：`Clear` = 清 idle、pool 仍可用；`Dispose` = 終結 pool 本身

### V2 對應表（migration 提示）

| V2 | Phase 1.1 新設計 |
|----|----------------|
| `ComponentPool.Source` (Component) | `IRecycler.Source` (object) |
| `ComponentPool.Capacity` (overloaded) | `Recycler<T>.MaxAvailable`（純 idle 上限）+ Phase 2 policy |
| `ComponentPool.UseFIFO` + `history` | Phase 2 議題 5 — V2 演算法有 bug（痼疾 22）不繼承 |
| `ComponentPool.Prepare<T>(count)` | `Recycler<T>.Prewarm(count)` |
| `ComponentPool.Create<T>()` | 取消方法 — 改 ctor 收 `Func<T> factory`（callsite: `() => Instantiate(prefab)`） |
| `ComponentPool.Spawn<T>(pos, rot, parent, beginSpawn)` | Core: 只有 `Get()`。Spawn 為 Unity 應用層 extension，**不入 Core** |
| `ComponentPool.Despawn(element)` | Core: 只有 `Return()`。SetActive / reparent 為 Unity 應用層 |
| `ComponentPool.init` event | `IRecyclable.OnAcquired` 或 callsite wrap factory |
| `ComponentPool.parent` / `Root` Transform | Unity 應用層管 hierarchy，**不入 Core** |
| `ComponentPool.elements` HashSet + `list` LinkedList + `history` Queue | `HashSet<T> active` + `Stack<T> available` 兩個就夠 |
| `PoolManager` Singleton | **刪** — 改 `class Recycler` 純 facade |
| `PoolManager.pools` + `TypeMaps` + `NameMaps` | 兩套字典：`typeRecyclers` + `sourceRecyclers`。NameMaps 砍掉（痼疾 A） |
| `PoolManager.element_source_Maps` 反查表 | **刪** — 改顯式 `Return(source, instance)` |
| `PoolManager.canvas` 特例 | **刪**（痼疾 23） |
| `PoolElementHandler` MonoBehaviour | **變 opt-in** — Unity 層 `Recyclable : MonoBehaviour, IRecyclable`，使用者手動加 |
| `MeshPool` static class | **刪** — 改 `Recycler.Register(new UnityObjectPoolAdapter<Mesh>(key, meshObjectPool))` |

### 議題狀態更新

- ✅ 議題 4（Source 來源）— Source 內化為 property；factory 由 ctor 注入
- 部分 ✅ 議題 5（Eviction policy）— Phase 1 簡單 `MaxAvailable` + `onDiscard` callback；策略抽象延 Phase 2
- ✅ 議題 8（與 Unity ObjectPool 關係）— `UnityObjectPoolAdapter<T>` 包裝
- ✅ 議題 11（Domain Reload）— `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 清字典 + TryBuild
- 仍延後：議題 6（Auto-recycle trigger）、9（Inspector 整合）、10（Migration shim）、12（Edit mode）

---

## 設計收斂 + Core 實作 — Phase 1.2（2026-05-21,取代 1.1 的 API）

> 1.1 之後實作時又收斂三件大事:**(a)** `Recycler` 從「facade-only + protected ctor」改回「**facade + 可用的 object pool**」;**(b)** Capacity/eviction 從「Core 內建策略」改為「**Core 只通知,使用者注政策**」(mechanism vs policy);**(c)** 砍掉 `Exhaustion` enum。**Core 4 檔已實作完成、build 通過。**

### 關鍵轉折:eviction/fail 對 gameplay 不公平 → Get 永遠成功

子彈池場景驗證出來的洞:
- `RecycleOldest`:最舊那顆還在飛(可能正命中)突然消失 → 玩家被坑
- `Fail`:射擊請求被吞 → 輸入掉了

**結論:「硬上限 + eviction/fail」只適合 cosmetic 物件**(彈孔、特效、飄字);gameplay 物件要 **Get 永遠成功**(無界成長,pool 純當 GC 優化)。pool 本身不限表演物件 ——「硬上限+eviction」這個**行為**才限。

→ `Recycler<T>.TryGet` 在 instance 層**永遠 true**。真正的 `false` 只在 facade 層(查無已註冊 recycler)。

### Mechanism vs Policy 分層

Core 只給**機制**,使用者注**政策**:

| 機制(Core) | 政策(使用者) |
|------------|--------------|
| `Capacity`(預設 ∞):Get 後 `CountActive > Capacity` → 呼叫 `onCapacityExceeded`,**但 Get 仍成功**、不 enforce、不 evict | `onCapacityExceeded` 內決定怎麼辦:`r.TryPeekOldest(out var o); r.Return(o);`(叫回重用)/ `Discard`(銷毀)/ 自訂 / 不處理(任其成長) |
| `TryPeekOldest`(看最舊不移除)/ `Return`(叫回 park)/ `Discard`(銷毀移除) 三原語 | 用這些原語組合出叫回邏輯 |
| `MaxAvailable`(預設 ∞):Return 時待命滿 → `onDiscard` 銷毀多的 | 設多少 = 用完一波願意留幾個待命(常態並存數附近) |

兩個天花板都預設 ∞ = 無界重用 pool,對 gameplay 最安全;各自 opt-in 收緊。

### `Recycler` = facade + 可用 object pool（**非** facade-only）

```csharp
public class Recycler : IRecycler
{
    // ---- static facade ----
    public delegate bool TryBuildRecycler(Type type, out IRecycler recycler);
    public static TryBuildRecycler? TryBuild;
    public static void Register(IRecycler r);
    public static bool TryGet<T>(out T result);                 // by Type,缺則走 TryBuild
    public static bool TryGet<T>(object source, out T result);  // by source
    public static void Return<T>(T instance);
    public static void Return<T>(object source, T instance);
    public static void ClearAll();
    public static void Reset();                                  // ← Unity 層用 [RuntimeInitializeOnLoadMethod] 呼叫(Core 不能 using UnityEngine)
    public static IRecycler Build(Type, object, Func<object>, Action<object>?);  // 包反射,使用者注入零 MakeGenericType

    // ---- instance:object pool ----
    public object Source { get; }
    public int Capacity { get; set; } = int.MaxValue;           // active 軟上限 → 通知
    public int MaxAvailable { get; set; } = int.MaxValue;       // idle 上限 → onDiscard
    public Action<Recycler>? onCapacityExceeded;
    public int CountActive { get; }
    public int CountAvailable { get; }

    public Recycler(object source, Func<object> factory, Action<object>? onDiscard = null);
    public virtual object Get();                                // 永遠成功(空則 factory)
    public virtual bool TryGet(out object instance);            // 永遠 true
    public virtual void Return(object instance);                // 叫回:untrack → park(滿則 onDiscard)
    public void Discard(object instance);                       // 銷毀:untrack → onDiscard,不 park
    public bool TryPeekOldest(out object oldest);               // 看最舊 active,不移除
    public void Prewarm(int count);
    public void Clear();                                        // 清待命,每個走 onDiscard
}
```

儲存:`LinkedList<object> activeOrder` + `Dictionary<object, node> activeNodes`(O(1) 取最舊 + O(1) 依值移除)+ `Stack<object> available`。有序 active 是為了 `TryPeekOldest` / `Return` / `Discard`,即使 Capacity=∞ 也維持(runtime 可改)。

### `Recycler<T>` = typed 包裝（**無獨立儲存**）

```csharp
public class Recycler<T> : Recycler, IRecycler<T>
{
    public Recycler(object source, Func<T> factory, Action<T>? onDiscard = null)
        : base(source, () => factory()!, onDiscard==null ? null : o => onDiscard((T)o));
    public new T Get();                  // (T)base.Get()
    public bool TryGet(out T instance);  // 永遠 true
    public void Return(T instance);
    public void Discard(T instance);
    public bool TryPeekOldest(out T oldest);
}
```

`object` 儲存對 reference type(唯一該 pool 的東西)**零裝箱**,cast 約幾 ns。value type 會裝箱 + HashSet identity 壞 → 不該 pool value type。base(object)view 與 typed view 共用同一個 pool(`new T Get()` 只是 cast 包裝,因 `Recycler<T>` 無獨立儲存,隱藏不會造成兩個 pool)。

### 用法

```csharp
// gameplay(子彈):不設 Capacity → 永遠成長,公平
Recycler.Register(new Recycler<Bullet>(prefab, () => Instantiate(prefab), Object.Destroy));

// cosmetic(彈孔):設 Capacity,使用者注入叫回政策
var pool = new Recycler<Decal>(prefab, () => Instantiate(prefab), Object.Destroy) { Capacity = 50 };
pool.onCapacityExceeded = r => { if (r.TryPeekOldest(out var o)) r.Return(o); };  // 叫回最舊重用
Recycler.Register(pool);
```

### 與 1.1 的 API 差異

- ❌ 砍 `Exhaustion` enum(Fail/RecycleOldest)— eviction 政策外推給使用者 callback
- ❌ 砍 1.1 的 `protected Recycler()` facade-only — 改 `public Recycler(object, Func<object>, Action<object>?)` 可用 object pool
- ❌ `Recycler<T>` 不再有獨立 `Stack<T>`/`HashSet<T>` — 改 typed 包裝,儲存全在 base(object)
- ❌ Core 內的 `[RuntimeInitializeOnLoadMethod]` → 改 `public static void Reset()`,由 Unity 層呼叫(Core 是 .NET Standard,不能 using UnityEngine)
- ➕ `Capacity`(軟上限通知)、`onCapacityExceeded`、`TryPeekOldest`、`Discard`、有序 active

### 實作環境約束(實作時踩到的)

- `LangVersion 8.0` → **不能**用 `init` / target-typed `new()`;改 `{ get; set; }` + 顯式 `new Dictionary<...>()`
- 多目標 `netstandard2.1;net48` → net48 **沒有** `[MaybeNullWhen]`;不用該屬性,改 `default!` / `null!` 抑制 nullable 警告
- `factory` 必填(null 即 throw);`Recycler<T>` 用 `WrapFactory` 提早檢查

### Core 實作狀態:✅ 完成（2026-05-21,build 通過）

`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\DotNet\Source\Recycling\`
- `IRecyclable.cs` ✅(未動 — OnAcquired / OnReleased)
- `IRecycler.cs` ✅(`IRecycler` + `IRecycler<T> : IRecycler`)
- `Recycler.cs` ✅(facade + object pool,如上)
- `Recycler{T}.cs` ✅(typed 包裝)
- 已刪:`IRecyclerSource.cs` / `IRecyclerRegistry.cs`

### 下次 session 開場(待討論)

- **`IRecyclable` 太弱 + 最小注入問題**:目前 callback 行為(factory / onDiscard / onCapacityExceeded / IRecyclable.OnAcquired/OnReleased)全靠使用者注入。思考:庫怎麼讓使用者注入**最少**?`IRecyclable` 是否改成 **class**(讓使用者注入 Recyclable 行為,或提供預設實作),減少每個物件都要手動接 callback?— 見議題 13
- Core unit test(純 .NET 驗 lazy build / Capacity 通知 / onDiscard / TryPeekOldest / Return-Discard)
- 確認後才進 Unity Phase 1.5

---

## 待後續 session 討論的議題

> 本次**不出方案**，僅標記要決策的點。

1. ✅ **命名統一** — 已決議，見上方「設計決議」段
2. ✅ **抽象層** — 已決議：4 介面（Recyclable / Source / Recycler / Registry），policy/trigger 延後
3. ✅ **泛型策略** — 已決議：`IRecycler<T>` 單一介面走到底
4. **Source 來源**：prefab Instantiate vs scene pre-placed vs Resources/Addressables — 統一介面還是分類別？（已部分受 `IRecyclerSource<T>` 約束，但具體類別還沒定）
5. **Eviction policy**：策略模式（IEvictionPolicy: HardFail / Grow / EvictOldest / EvictNewest / Custom）還是固定提供 enum 選項？
6. **Auto-recycle**：trigger 抽象（IRecycleTrigger: Time / ParticleEnded / Disabled / Manual）還是讓使用者自己接 Timer + UnityEvent？
7. ✅ **Lookup key 型別** — 已決議：TKey 開放、Unity 層用 `UnityEngine.Object`、不用 string
8. **與 Unity 內建 `ObjectPool<T>`** 關係 — 包裝、平行、取代？V2 `MeshPool` 已採用 Unity 內建作 backend
9. **Inspector 整合需求**：是否仍需 `[Serializable]` Pool config / `SpawnPoolProxy` 風格的 UnityEvent 接口？
10. **Migration**：V1 / V2 既有 callsite 需要保留多少向後相容？是否提供 `[Obsolete]` shim？
11. **Domain Reload disabled** 環境下的 static state 重置策略 — `[RuntimeInitializeOnLoadMethod]` 是否一致使用？
12. **Edit mode 支援**：V1 用 Coroutine 綁死 runtime；V3 純 SetActive 可 edit mode。是否設計成 mode-agnostic？
13. **最小注入 + `IRecyclable` 強化**（2026-05-21 提出）：目前所有行為(factory / onDiscard / onCapacityExceeded / OnAcquired / OnReleased)靠使用者逐一注入,`IRecyclable` 僅兩個空 callback 很弱。思考方向:`IRecyclable` 改 **class**(提供預設行為 / 讓使用者注入一次)、或庫提供常用 callback 組合的 preset,降低每個 pool 的樣板注入量。

---

## Phase 1 實作清單（Core 已完成 — 2026-05-21）

> ⚠️ 下表為 1.1 規劃版,**實際實作以上方 Phase 1.2 為準**(API 有差:無 protected ctor、無 Exhaustion、Recycler 是可用 object pool、Reset() 取代 RuntimeInitializeOnLoadMethod)。Core 4 檔 ✅ 完成、build 通過。

**Core 端** — `C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\DotNet\Source\Recycling\`，`namespace Yu5h1Lib.Recycling`(以 Phase 1.2 的最終形為準)

| # | 檔案 | 狀態 |
|---|------|------|
| 1 | `IRecyclable.cs` | ✅ |
| 2 | `IRecycler.cs` | ✅ `IRecycler` + `IRecycler<T> : IRecycler` |
| 3 | `Recycler.cs` | ✅ facade + 可用 object pool(Capacity / MaxAvailable / onCapacityExceeded / TryPeekOldest / Return / Discard / Prewarm / Reset / Build) |
| 4 | `Recycler{T}.cs` | ✅ typed 包裝 |

已刪 `IRecyclerSource.cs` / `IRecyclerRegistry.cs`。無 `TypeRecycler.cs` / `StandardRecycler.cs` / `PrefabRecycler.cs` — TypeRecycler 由 `TryBuild` lazy 處理;Prefab pool 直接 `new Recycler<T>(prefab, () => Instantiate(prefab), Destroy)` 在 callsite 寫。

**Unity 端** — Phase 1.5（Core 落地後）：

- `UnityObjectPoolAdapter<T>` — 包 `UnityEngine.Pool.IObjectPool<T>`，放 `Packages\common\Runtime\Recycling\`
- `Recyclable : MonoBehaviour, IRecyclable` — opt-in 元件
- `RecyclerEx` static helpers — e.g. `ForPrefab<T>(T prefab) => new Recycler<T>(prefab, () => Object.Instantiate(prefab), Object.Destroy)`
- `[RuntimeInitializeOnLoadMethod]` 內呼叫 `Recycler.Reset()` + 設定 `Recycler.TryBuild`（Unity-aware 預設 builder：對 Component 子類用 `new GameObject().AddComponent(type)` + 對應 destroy）

**Phase 2 才碰**：
- 議題 5（Eviction policy 抽象 — `IEvictionPolicy`）
- 議題 6（Auto-recycle trigger — Time / ParticleEnded / Disabled）
- 議題 9（Inspector / SerializedField 整合）
- 議題 10（V2 callsite migration shim — 是否提供 `[Obsolete]` alias）
- 議題 12（Edit mode 支援）

**Migration 順序**（Core + Unity 1.5 落地後）：
1. Core 4 檔在 `Yu5h1Lib` 庫內落地、跑 unit test（純 .NET 環境驗證 lazy build + onDiscard + IRecyclable callback）
2. Unity 層 Phase 1.5 三件落地，註冊 Unity 版 trybuild
3. 把 V2 `MeshPool` 換成 `UnityObjectPoolAdapter<Mesh>` 註冊（最小破壞性 callsite 取代）
4. 改 `PoolManager.Spawn/Despawn` callsite → `Recycler.TryGet/Return` 或 Unity 層 `Spawn` extension（議題 10：是否留 `[Obsolete]` shim 拍板）
5. V2 `ComponentPool` / `PoolManager` / `PoolElementHandler` deprecate
6. V1 `Recycler.cs` / `Recyclable*.cs` 同名檔刪除（plan 開頭已決：奪名無相容包袱）

完成後將「Recycler 統一 API + V2/V3 整併」寫進 `.Claude` memory，未來專案直接引用 `Yu5h1Lib.Recycling`。
