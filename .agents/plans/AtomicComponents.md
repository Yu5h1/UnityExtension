# Yu5h1Lib 原子組件（Atomic Components）

> Yu5h1Lib 核心概念 — 「橋樑型 abstraction」。先建空殼介面，使用者依賴 interface，實作隨用隨補。

---

## 概念定義

**原子組件** = 小到不能再分的 abstraction，**只負責一件事的最小契約**。

特徵：
- **單一介面** — 1-3 個方法
- **零外部庫依賴** — 純 .NET，可在 Unity / Console / Web / Test 任意 host 使用
- **State 內聚** — 介面隱藏 state shape，呼叫者只看 input/output
- **多型友善** — 可有多種實作，可被組合

> ⚠️ **「原子」形容介面，不形容實作（2026-06-12 釐清）**
> 原子層 = **介面**（`IResolver`/`IResolver<T>`），嚴守上述特徵。
> **具體實作（如 `Counter`）不是原子組件**，是普通類別 — 該有的欄位/state 就給，不受「1-3 方法」約束。`Counter` 住 Core 只因它剛好純 .NET（命中「Interface in Core, impl **anywhere**」的 anywhere），實作 `IResolver<int>` 是 bonus 不是存在理由。
> **別拿原子尺量實作**。實作該守的是「正常好類別設計 + 不沾不屬於它的責任（如 observer/event 歸擁有者）」。

## 為什麼要原子組件層

> 「**只在實作邏輯時需要考慮引用的庫是甚麼**」 — 使用者語

設計目的：

1. **解耦上層邏輯與底層工具** — 上層只認 interface，底層實作可換
2. **跨庫能力共用** — 同一介面在 Unity / Console 共享
3. **Unity SO 資料驅動的基礎** — interface 是 SO factory 的契約
4. **AI/協作時溝通成本低** — 對 LLM/同事說「這是 IFoo」比解釋整段邏輯快

## 設計原則

| 原則 | 說明 |
|------|------|
| **Interface in Core, impl anywhere** | 介面定義在 `DotNet/Source/`，實作可在 `DotNet/`、`Unity/`、上層應用 |
| **No library leak** | Interface 簽名不 import 第三方型別（不能有 `UnityEngine.Object` 之類） |
| **Resolve-on-demand** | 介面以「呼叫時取得結果」為主，避免 long-lived state contract |
| **Atomic, not hierarchical** | 不繼承其他 interface（除非真有 is-a 關係），避免 abstraction 鏈 |
| **Defer organization** | 單檔放 root，2+ 個檔再 promote 到資料夾 |

## 當前項目

### `IResolver` 家族 + `Resolver<T>` + `Repeater` — 第一個實作完成（2026-06-12）

> 介面從 `IIntegerResolver` 泛型化為 **`IResolver<T>`**（2026-06-12，使用者提）— `IIntegerResolver` 太長，且 `T`=產出值型別可收編整個家族（`IResolver<int/float/bool>`）。

**⚠️ 不要跟「設計拒絕」那條搞混**：被否決的 `IIntegerResolver<T> where T : IIntegerProvider` 的 `T` 是**輸入 provider**（Visitor，型別爆炸+SerializeReference→WebGL crash）；現在的 `IResolver<T>` 的 `T` 是**產出值**，無約束、無 provider。concrete class（`Counter`）序列化/IL2CPP AOT 皆安全。

**位置（三檔，`DotNet/Source/Resolvers/`，namespace flat `Yu5h1Lib`）**：
- `IResolver.cs` — `IResolver` 非泛型 base + `IResolver<T>`
- `Resolver.cs` — abstract `Resolver<T>`（事件 + Resolve 模板，可選基底）
- `Repeater.cs` — `Repeater : Resolver<int>`（第一個實作）

**最終介面設計（2026-06-12 定版，try-pattern）**：
- `IResolver`（非泛型 base）：`System.Type ResultType { get; }` + `object Result { get; }` + `bool TryResolve(out object result)` + `void Resolve()` + `void Reset()`
- `IResolver<T> : IResolver`：`new T Result { get; }` + `bool TryResolve(out T result)`
- **`TryResolve(out)` = 靜默取值（不觸發事件）**；**`Resolve()` = 整套（觸發事件）**。擁有者自己選路徑 → 這就是「觀察歸擁有者」的精緻版
- `TryResolve(out object)` vs `(out T)` 是**多載**（out 型別不同），非遮蔽 → TryResolve 不用 `new`；只有 `Result` 需要 `new`
- 失敗時 `out result = default(T)`，`Result` 屬性**只在成功時更新**（保留最後成功值）
- `GetReturnType` 已改名 **`ResultType`**（配 `Result`）

**abstract `Resolver<T>`（可選事件基底）**：
- C# 事件 `event Action Resolving`（每次**成功產出一步**後 fire，值去讀 `Result`）、`event Action Resolved`（在**完成那一步**fire；天然只一次——完成後 TryResolve 永遠 false 直接 return，**不需 fire-once flag**，2026-06-12 移除 `_resolvedFired`）
  - **砍掉舊的 `Attempted(bool)`**（2026-06-12，使用者質疑）：bool 不精準——`false` 那半跟「完成」冗餘（Repeater 的 TryResolve=false 正好＝完成）；改 `Resolving`/`Resolved` 用**時態**區分（Resolve→Resolving→Resolved），單一字根、保住 `IsResolved`。⚠️ `Resolving` 是「產出後通知」不是可取消的 pre-hook（doc 已註明）
  - 「失敗的嘗試」目前無需求（Repeater false=完成）；未來 retry 型 resolver 再加 `Failed` 事件，別現在為假設焊 bool
- `Result {get; protected set;}`、`ResultType => typeof(T)`、abstract `IsResolved`（impl 定義「完成」）、abstract `TryResolve`、virtual `Reset`（清 flag+Result，子類 override 加自己 state 並 `base.Reset()`）
- `Resolve()` 模板：`if(!TryResolve(out r)) return; Result=r; Resolving(); if(IsResolved) Resolved();`（完成檢查在成功分支內 → 自動 fire-once）
- 非泛型橋接：`object IResolver.Result`、`bool IResolver.TryResolve(out object)`
- ⚠️ `Resolved` 完成條件用獨立 `IsResolved`（**不能**用「TryResolve 回 false」——對 retry 型 false=還沒拿到，意思相反）
- **事件是 C# event，Unity 不序列化** → behaviour 用 `UnityEvent` 橋接（Timer 慣例：`Awake(){ resolver.Resolved += _resolved.Invoke; }`），resolver 保持乾淨
- **進度（normalized 0..1）不放 base**（2026-06-12 拍板）：只有「有限」resolver 有進度（Repeater = `index/count`），Random 無進度。要進度的繼承者自己加 `normalized` property（仿 `Timer.normalized`），base/介面維持乾淨
- **`Resolving`/`Resolved` 不互斥**（2026-06-12 拍板，使用者否決互斥案）：完成那一步兩個都 fire。re-arm 靠 `Reset()`（Unity 端在 `OnEnable` reset repeater；Timer 自己有 reset）。⚠️ watch-point：若 inspector 把 `_resolving → Timer.restart` 接線，完成步也 fire `_resolving` → 可能多跑一個 interval；使用者知情、選擇用 Reset 處理而非互斥

## Timer 整合（real consumer，planned；2026-06-12）

- **Timer 不繼承 `Resolver<T>`**：繼承後序列化事件名變 `resolved`，跟 Timer 既有 `completed` 語意打架——**vocabulary mismatch = 「不是 is-a」的訊號**。Timer 是 Repeater 的 **consumer/composer，不是 is-a Resolver**。
- 做法：**Timer 保留現狀、只刪內部 repeat 功能**，改用 UnityEvent 組合：`TimerBehaviour._completed → RepeaterObject.Resolve()`（void，UnityEvent 接得了；`bool TryResolve(out)` 接不了）→ RepeaterObject 的 `_resolving` → `Timer.Start()` 重啟，`_resolved` → 停。全程 inspector 接線、零膠水 = 可組合的 `repeatTimer`。
- 這個 consumer **驗證了**：Repeater 核心 + SO 層的 UnityEvent 橋接（無程式碼組合的理由）。**仍未驗**：`IResolver<int>` 的「值」面（restart 用不到 int）、`IResolver<T>` 多型抽換（直接接 concrete RepeaterObject）。

## 未來方向：IValuePort 綁定（breadcrumb，2026-06-13）

- 用 `Yu5h1Lib.MVVM.IValuePort`/`IGetter<int>` 把 Repeater 的 `current`/`Result` 直接綁到 UI 顯示（「3/5」、進度）。
- **這是會真正用到「值」的 consumer**——補上 Timer 沒驗到的 `IResolver<int>` 值面。
- 接法構想：`Repeater.Resolving`（事件）→ ValuePort 的 `ChangedCallback` → UI 自動刷新。`current` 已是 `{get; private set;}` 可讀,接得上。
- 「之後再看怎麼裝」——還沒實作,先記方向。

**`Repeater : Resolver<int>`（第一個實作，取代 Counter）**：
- 唯一責任：「重複 N 次 / 一直重複」（Timer repeater 概念抽出）。全正向、無 step
- 序列化欄位只有 `count`（`<0`=無限 / `n`=n 次）；`public int current { get; private set; }` = 0-based 位置/已產出數，**runtime（auto-prop 不序列化）但外部可讀**（2026-06-12 使用者要求恢復 Counter 的 `current`，補互補 `Result`：`current`=位置不論走哪條路徑，`Result`=只在 `Resolve()` 路徑更新的值）
- `TryResolve(out int)` 回 0-based 索引、`true` 重複中 / `false` 完成 = `IEnumerator.MoveNext` 形狀
- `IsResolved => count>=0 && current>=count`
- **try-pattern 連 lazy-init 都消掉了**（Result 預設值天然處理「尚未解出」，無需 `_started` flag）

> **為何 Counter→Repeater**：Counter 把「計次+縮放(step)+方向+邊界」綁一顆 = machine（責任可分）。拆出原子 `Repeater`；倒數/step 等用獨立 **`Operator`**（affine `scale*x+offset`）組合，組合的序列化走 **SO 資產引用**（[[reference_no_serializereference]]），屬 SO 層、之後做。Counter.cs 已刪。

**`RandomResolver : Resolver<int>`（2026-06-13）— 含可調換 backend**

- **可調換 backend = 注入式 `IRandomSource`**（[[reference_no_serializereference]] 同理：Core 不能引用 UnityEngine，所以抽象 + 注入）：
  - Core `DotNet/Source/Resolvers/`：`IRandomSource`（`int Next(min,maxExcl)` + `float NextFloat(min,maxIncl)`）+ `SystemRandomSource`（System.Random 預設，可 seed）。⚠️ **放 Resolvers/ 不放 Random/**：Unity csproj 的 `<Compile Include>` glob 只涵蓋 `Resolvers\**`,新資料夾不會被匯入（見 lib `.claude/build-notes.md`）
  - Unity `common/Runtime/Resolver/`：`UnityRandomSource`（UnityEngine.Random，`Default` 單例）
- **`RandomResolver`（`Resolvers/`）**：欄位 `min`/`max(exclusive)`；`IRandomSource source { get => _source ??= new SystemRandomSource(); set; }`（`_source` 是 `IRandomSource?` 私有、**不序列化** → backend 是 runtime 注入,不是序列化資料）
- **是無限亂數流**：`IsResolved => false`（永不完成）、`TryResolve` 永遠 true 回 `source.Next(min,max)`。要「N 次抽」就**跟 Repeater 組合**（呼應分責原則,不把 count 焊進來）
- **Unity `RandomResolverObject : ResolverObject<RandomResolver,int>`**：`Initialize()` 先 `base.Initialize()` 再 `Data.source = UnityRandomSource.Default`（Unity asset 預設用 Unity RNG;仍可 runtime 換回 System）
- 五個檔已寫,**未編譯驗證**（使用者偏好自驗,見 [[feedback_no_self_build]]）

**設計決議**（決於 2026-05 對話）：
- 名稱用 `Integer` 而非 `Int` — 比較正式、符合 .NET 慣例
- 用 `Resolver` 而非 `Source` / `Provider` — 暗示「處理產出」，比被動 source 強
- State 由各實作自己持有 — 不外部化（避免 Visitor pattern 的 Provider 型別爆炸）
- 方法 `Get()` + `Reset()`，不暴露 state — 隱藏細節

**設計拒絕**（已討論否決）：
- ❌ `Resolve(ref int current)` — 對 Shuffle 不夠用（Shuffle 需 array state）
- ❌ `Resolve(IIntegerProvider p)` Visitor pattern — Provider 型別跟 Resolver 強耦合
- ❌ `IIntegerResolver<T> where T : IIntegerProvider` 泛型版 — Unity Serialize 不友善 + WebGL 風險
  - 註：此處 `T`=輸入 provider。**與後來採用的 `IResolver<T>`（`T`=產出值）無關**，後者安全。
- ❌ `[SerializeReference]` 配置 — WebGL 易 crash

**實作策略**（未來實作時遵循）：
- 純 inline 用法 → Resolver 內部持 state（per-instance）
- **目前 SO 用法 = scene-serialized SO（per-instance、隨場景一份）→ 無 cross-talk，config+runtime state 同一顆即可，不需 CreateRuntime**（使用者 2026-06-12 澄清）
- **asset SO = 刻意的全域共享**（一個 `.asset` 多處引用，共享 runtime state 是 *feature* 不是 bug，使用者視為「全域的一種」）。所以「config+runtime 同一顆」在兩模式下都對：scene=本地不共享、asset=刻意全域。`CreateRuntime()`/clone 只在罕見的「想共享 config 但要獨立 runtime」才需要（2026-06-12 釐清）

---

## 已登記未來任務（按可預期實作順序）

### 任務 1：`Counter` — ✅ 完成（2026-06-12）

> 原名 `CounterResolver`，實作時砍成 `Counter`。下方草稿與 Timer 動機保留供參，實際細節見上方「當前項目」。

**動機（原）**：`Timer.cs` 內部有 repeater 邏輯（重複次數計數）。
**⚠️ Timer 重構放棄由 Claude 做** — 使用者要自己拔（repeater 欄位有序列化資料需遷移，手動處理）。Counter 設計**不為 Timer 量身訂做**，純獨立物件。

**位置（實際）**：`DotNet/Source/Resolvers/Counter.cs` + `Resolvers/IResolver.cs`（已建 `Resolvers/` 資料夾，解掉開放議題 #2）

**API 草稿**：
```csharp
[Serializable]
public class CounterResolver : IResolver<int> {  // 實作時已改名 Counter
    public int start;
    public int step = 1;
    public int max = -1;        // -1 = 無上限
    public bool wrap = false;   // 達 max 後是否回到 start

    private int _current;
    public int Get() { /* 回 _current 然後步進 */ }
    public void Reset() => _current = start;
}
```

**Timer 重構步驟**（任務 2 才做，先記下）：
1. Timer 內部找 repeater 相關欄位
2. 替換成 `CounterResolver _repeatCounter`
3. 既有 callsite 行為 100% 對齊
4. 測試 / 既有 scene 驗證

### 任務 2：`RandomIntegerResolver`

**動機**：跟 Counter 配對，作為「隨機產 int」的標準形式。

**設計考量** — Random 實作差異：
- **`UnityEngine.Random`** — Unity 唯一可用，全域 static state，`InitState(seed)` 控制
- **`System.Random`** — 純 .NET，可 instance 化，可 seed
- **`Unity.Mathematics.Random`** — Burst-compatible，純 struct，無 GC

**問題**：Resolver 在 Core (DotNet)，**不能直接 reference `UnityEngine.Random`**。怎麼辦？

**候選方案**：
| 方案 | 描述 | 評估 |
|------|------|------|
| A | Core 用 `System.Random`，Unity 端不另開 | 簡單但失去 UnityEngine.Random 的 InitState 全域控制 |
| B | Core 提供抽象 `IRandomBackend`，Unity 端注入 `UnityEngine.Random` 實作 | 乾淨但增加配置 |
| C | Core 不放 RandomResolver，Unity 端各自實作 `RandomIntegerResolver : IResolver<int>` | 純 — Core 只 interface，實作就近放 |
| D | Core 有 default 用 `System.Random` 的版本，Unity 端可選用 Unity 的 | 雙實作並存 |

**待決**：實作前先決定（建議 C — 最符合「Core 只放契約」原則）。

### 任務 3：`ShuffleIntegerResolver`

**動機**：保證 N 次內每個 index 都會被選到一次（公平輪播 / 抽獎 / 對話台詞 / BGM 等）。

**演算法**：Fisher-Yates shuffle + cursor 指向當前位置 + 輪完重洗。

**經驗值**：使用者說「比較沒經驗」 — 我寫過草稿（見對話 2026-05 ShuffleBag 段）：
```csharp
[Serializable]
public class ShuffleResolver : IResolver<int> {
    public int count;
    private int[] _order;
    private int _cursor;

    public int Get() {
        if (_order == null || _cursor >= _order.Length) Shuffle();
        return _order[_cursor++];
    }
    public void Reset() => _cursor = _order?.Length ?? 0;  // 觸發下次重洗

    private void Shuffle() {
        if (_order == null || _order.Length != count) _order = new int[count];
        for (int i = 0; i < count; i++) _order[i] = i;
        for (int i = count - 1; i > 0; i--) {
            int j = /* random 0..i */;
            (_order[i], _order[j]) = (_order[j], _order[i]);
        }
        _cursor = 0;
    }
}
```

**待決**：與 RandomResolver 相同 — Random 來源（System.Random / UnityEngine.Random / 注入式 IRandomBackend）。

---

## 候選的下一批原子組件（未來考慮）

> 觀察到的可能 candidate，待真有需求再啟動。

- ~~`IFloatResolver` / `IBoolResolver`~~ — **已被 `IResolver<float>` / `IResolver<bool>` 收編**，不需獨立介面
- **`IConditionResolver`** — 接收 context 回 bool — 行為樹條件節點
- **`IScheduler`** — `bool ShouldFire(deltaTime)` — Timer / Cooldown / Throttle 共用
- **`IFilter<T>`** — `bool Accept(T)` — query 條件、collision 過濾
- **`ISelector<T>`** — `T Choose(IEnumerable<T>)` — 升級版 RandomElement，支援策略

不急著抽，看實作多次後是否真的需要 abstract。

---

## 工作流程提示（給未來 session）

1. **新原子組件 = 先寫 interface，零實作**
   - 證明「我目前就需要這個 abstraction」
   - 沒實作不算過早抽象，是占位
2. **第二實作出現才開資料夾**
   - 第一個 impl 跟 interface 同層
   - 第二個 impl 加入 → 此時 promote 到 `Resolvers/` 之類資料夾
3. **介面變動 = 寫到本檔「設計拒絕」段**
   - 避免下次 session 又走回頭路
4. **Random 庫選擇**是一個會反覆出現的問題
   - 任何「需要隨機」的原子組件都要面對
   - 統一決議後可寫進本檔頂部當 project rule

---

## 開放議題（決定前不要動實作）

1. ~~**Random backend 統一策略**~~ — ✅ 已決（2026-06-13）：**注入式 `IRandomSource`**（候選 B/D）。Core 抽象 + `SystemRandomSource` 預設，Unity 注入 `UnityRandomSource`。`ShuffleResolver` 將重用同一個 `IRandomSource`。（`RandomUtility.cs` 沒動用,維持現狀）

2. ~~**Resolvers 資料夾 vs root**~~ — ✅ 已決（2026-06-12）：建 `Resolvers/`，介面 + 第一個 impl 一起放，namespace 維持 flat `Yu5h1Lib`

3. **`Reset()` 語意**
   - 對 Repeater：✅ `current=0` + base 清 Result（已實作）
   - 對 Shuffle：重洗 vs 觸發下次取值時重洗 — 待實作時決

4. ~~**Timer 重構時機**~~ — ✅ 已決：**Claude 不做**，使用者自己拔（序列化資料遷移）

5. **`Operator`（affine scale*x+offset）** — 倒數/step 靠它組合 Repeater。組合序列化走 SO 資產引用，屬 SO 層、之後做。未啟動

---

## 跟其他計畫的關聯

- `Recycler_Refactor.md` — Recycler 系列重構，獨立軌道
- `Motion_System_Refactor.md` — Motion/Tween 重構（前身 Tween_Refactor，已併入），可能受益 `IResolver<float>`（未來）
- `InputLayer_Refactor.md` — Input 重構，獨立
- 本計畫 = 為上述計畫之外的「跨領域 primitive」做準備

---

## 進度標記

- [x] `IIntegerResolver` → 泛型化 `IResolver<T>` → 定版 try-pattern `IResolver`/`IResolver<T>`（2026-06-12）
- [x] abstract `Resolver<T>` 事件基底（`Resolving`/`Resolved` + Resolve 模板）（2026-06-12）
- [x] `Repeater : Resolver<int>` 實作（2026-06-12，取代廢棄的 Counter）
- [x] `Resolvers/` 資料夾建立（2026-06-12）
- [x] **SO 層開工（2026-06-12）**：`common/Runtime/Resolver/` — `ResolverObject<TData,TValue> : BehaviourObject<TData>, IResolver<TValue> where TData : Resolver<TValue>`（橋接 C# 事件→UnityEvent `_resolving`/`_resolved`，委派 TryResolve/Resolve）+ concrete `RepeaterObject`。`BehaviourObject`(SO) 加 `IsInitialized`/`LazyInitialize`/`virtual Initialize`。注意：必須**兩個泛型參數**（事件在 `Resolver<T>` 類別非介面 + Data 要 concrete 才能免 SerializeReference 序列化）
- [~] Timer 重構 — 移出 Claude 範圍，使用者自行處理（序列化遷移）
- [x] **Random backend 策略拍板（2026-06-13）：注入式 `IRandomSource`**（Core 不能引用 UnityEngine → 抽象 + System.Random 預設；Unity 端注入 UnityEngine.Random）
- [x] **`RandomResolver : Resolver<int>` + backend 實作（2026-06-13）** — 見下方設計
- [ ] `ShuffleResolver : Resolver<int>` 實作（會重用 `IRandomSource`）
- [ ] `Operator`（affine）+ SO 層組合 — 之後

> ⏳ 下一步：使用者 VS rebuild + Unity 驗證 Resolver 全家族 → 之後 `ShuffleResolver`（Fisher-Yates + cursor，重用 `IRandomSource`）
