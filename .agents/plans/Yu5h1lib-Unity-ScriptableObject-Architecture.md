# Yu5h1Lib Unity ScriptableObject Architecture — ParameterObject 系列

> 本文件記錄 Yu5h1Lib 中以 ScriptableObject 為基礎、由 `ParameterObject` 衍生的整套架構。
> Future Claude / Developer 讀本文即可理解系統設計、應用範圍與待處理議題。

## 一句話總結

**`ParameterObject<T>` 是一個 SO 容器，把「目標屬性名 + 值 + 反射寫入機制」打包成 asset。**
透過 `name` 欄位（SO 自帶）當目標屬性名，`value` 欄位帶值，`ApplyTo(target)` 用快取反射寫入。

> ⚠️ **2026-07-04 大演化**：抽出 `IParameter` 介面 + `ParameterMember`（共用反射）+ `ParameterBehaviour`（MonoBehaviour 版，解決 SO 存不了場景引用）。整個家族移到 `Packages/common/Runtime/Data/Parameter/`。**先讀下方「2026-07-04 演化」段落** — 本文其餘 SO 細節概念仍有效，但反射已外移到 ParameterMember。

## 核心檔案

### `Packages/common/Runtime/Event/ParameterObject.cs`

```csharp
public abstract class ParameterObject : ScriptableObject
{
    public abstract Type DeclaredType { get; }
    public abstract void ApplyTo(Object target);
}

public abstract class ParameterObject<T> : ParameterObject
{
    [Decorable, Inline(true)] public T value;
    public override Type DeclaredType => typeof(T);
    public static implicit operator T(ParameterObject<T> obj) => obj.value;

    // ApplyTo: name (SO 名稱) → 目標屬性 → 反射 SetValue
    // 支援 "a.b.c.MemberName" 取最後一段
    // 反射有 SetterKey 三段快取，含 negative cache
    // Property 優先，Method 為 fallback
    // null guard for non-nullable value types
    // try/catch 包 invoke，失敗時 printWarning
}

public abstract class ParameterCollection<T> : ParameterObject<T[]>
{
    public T Random();
    public T GetRandomElement(params T[] excludeElements);
}
```

**關鍵設計**：
- `name`（SO 內建欄位） = 目標屬性 / 方法名
- 設計師建 asset、命名 "CanSkip"、設 value → `obj.ApplyTo(controller)` 會反射呼叫 `controller.CanSkip = value`
- 快取使用 `Dictionary<SetterKey, MethodInfo>`，key 含 (targetType, memberName, declaredType) → 跨 instance 共用

## 2026-07-04 演化：IParameter 抽象層 + Behaviour 版 + ParameterMember

> 家族已從 `Event/` 移到 **`Packages/common/Runtime/Data/Parameter/`**（同 assembly、namespace 不變、GUID 保留）。

### 核心問題（為什麼需要 Behaviour 版）
`ParameterObject` 是 **SO asset** → **序列化不了場景引用**（Transform、場景 component）。
但 prefab（如 fish zone）的視覺物件 Transform 是 prefab 內部引用，SO 拿不到；且要「改 base prefab → 連動所有 instance」。
→ 需要一個 **MonoBehaviour 版**（能持 prefab/scene 內引用）。

### 結構
```
IParameter                         介面：name + GetValue() + ApplyTo(Object)
  ├─ ParameterObject<T>   (SO)     memberName => name（asset 名即成員名，天生合一）
  └─ ParameterBehaviour<T> (MB)    獨立 _memberName 欄位；能持 prefab/scene 引用
       ├─ ObjectParameter          : ParameterBehaviour<Object>，一個涵蓋所有 Object 系列（不建一堆 typed）
       └─ ParameterReference        : ParameterBehaviour<ParameterBehaviour>，包另一個 parameter，ApplyTo 轉發
ParameterMember (static)           SO/Behaviour 共用的反射（cache 一份，非 per-T）
SignalDispatcherProxy              is IParameter → SignalDispatcher.Dispatch(parameter.name, obj)
```

### 檔案（`Data/Parameter/`）
- `IParameter.cs`：`string name { get; }` + `object GetValue()` + `void ApplyTo(Object)`
- `ParameterMember.cs`（static）：`Apply(target, memberName, value, declaredType)`。搬走了原 ParameterObject 內建的 `flags`/`SetterKey`/`_setterCache`/`GetSetterCached`。**lookup 用 `value.GetType()`**（非 declaredType）→ 讓 ObjectParameter（declaredType=Object）也能對到 Transform 屬性。`_args1` 改本地陣列（static 共用不能有可變狀態）。`setter==null` 加 editor 警告。
- `ParameterObject.cs`：反射抽走，`ApplyTo => ParameterMember.Apply(target, memberName, value, DeclaredType)`，`memberName => name`。
- `ParameterBehaviour.cs`：`ParameterBehaviour<T>` 有 `_memberName`（獨立欄位、**非** GameObject 名）+ `_value`；`Reset()` 預設 `_memberName = name`（可覆寫）。
- `ObjectParameter.cs`：`class ObjectParameter : ParameterBehaviour<Object> {}`。

### 兩個字串角色（別搞混）
- **`name`**（Object.name）= 身分 / **signal key**（dispatcher 路由）
- **`memberName`** = 反射目標成員（SO: = name；Behaviour: 獨立 `_memberName`）

SO 天生合一（name = memberName = signal key）；Behaviour 分開（name = signal key、_memberName = 反射目標）。

### 關鍵決策
- **反射共用**：SO 和 Behaviour 都呼叫同一個 `ParameterMember.Apply`，cache 一份，零重複。
- **lookup 用 value 實際型別**：`prop.PropertyType == value.GetType()` 精確比對；base 型別屬性（屬性是 Component、值是 Transform）目前**不解析**（可 assignable 化，尚未做）。
- **拖值要對型別**：ObjectParameter 從階層拖 GameObject 存的是 GameObject（≠ Transform）→ 要拖 Transform 元件。**刻意不做 GameObject→transform 魔法解析**（explicit 勝 magic）。
- **string signal key 怕改名**：`parameter.name` 當 signal key，改物件名 = 訊號斷（輕量 string-signal 的固有代價）。

### 未決 / 待辦
- 具體 19 型別是否也移到 `Data/Parameter/Object/`（原在 `Event/ParameterObject/`）。
- **namespace 不一致**：`ParameterObject`/`ParameterMember`/`IParameter` 在 `Yu5h1Lib`，`ParameterBehaviour`/`ObjectParameter` 在 `Yu5h1Lib.Parameter`（能編譯，未來可統一）。
- `ParameterReference.ApplyTo` 記得 null guard（value 未設會 NRE）。

## 已有的 19 個具體型別

```
Packages/common/Runtime/Event/ParameterObject/
  Primitives:    BooleanObject, IntObject, FloatObject, StringObject
  Math:          Vector2Object, Vector3Object, Vector4Object,
                 RectObject, BoundsObject, RectOffsetObject
  Visual:        ColorObject, GradientObject, SpriteObject, AnimationCurveObject
  Special:       TransformInfoObject, UnityEventObject
  Collections:   IntegerArrayObject, StringArrayObject, ParameterCollectionObject
```

每個 concrete class 都是 1 行：
```csharp
public class BooleanObject : ParameterObject<Boolean> { }
```

## 已整合的上層系統

### 1. GenericPresetObject
`Packages/common/Runtime/Data/GenericPresetObject.cs`

把多個 `ParameterObject` 包成一個 preset 一次套用。
```csharp
public class GenericPresetObject : ParameterObject<GenericObjectPreset>
{
    public override void ApplyTo(Object target)
        => value.properties.ForEach(p => p.ApplyTo(target));
}

public class GenericObjectPreset : Preset<Object>
{
    public SerializedAssembly targetAssembly;
    public SerializedType targetType;
    [Inline(true), StringOptionsContext("Properties")]
    public List<ParameterObject> properties;
}
```

### 2. GenericComponentPresetObject
`Packages/common/Runtime/Data/GenericComponentPresetObject.cs`

同上，但目標限定為 Component 並做型別檢查。

### 3. Theme 系統
`Packages/common/Runtime/Theme/Theme.cs`

```csharp
public class Theme : ScriptableObject
{
    [SerializeField, Inline(true)] private List<ParameterObject> _items;
    [SerializeField, NotSelf] private Theme _schema;  // 繼承鏈

    public bool TryGet<T>(string key, out T value)
    {
        // 從 _items 找 name == key 的 ParameterObject<T>
        // 找不到 → 往 _schema 繼續查
    }

    public abstract class BindingObject<TUnityObject, TValue> : ScriptableObject, IBinding
    {
        // 把 theme value 套用到一組 _targets[]
    }
}
```

支援 schema 繼承（parent theme），key-based lookup，可用於 UI 主題、color palette 等。

## 應用範圍 — 既有 + 計畫中

| 用途 | 狀態 | 說明 |
|------|------|------|
| Event 參數攜帶（原始動機）| ✅ | 設計初衷：UnityEvent 只能傳 int/float/string/bool/Object，用 SO 包複雜參數 |
| Property 反射寫入 | ✅ | 後續演化：用 SO name 當屬性名，ApplyTo 統一機制 |
| Preset / Theme | ✅ | 多個 ParameterObject 組合套用 |
| Timeline Marker 整合 | ✅ | 已實作（2026-06-08）：`ParameterSignal` marker 攜帶 ParameterObject，`ParameterReceiver` re-emit + ApplyTo。詳見下方「與 Timeline 整合」段落 |
| 設定檔 / SaveData | 🤔 | 未來可能：用 ParameterObject 序列化偏好設定 |

## 待討論議題

### ⚠️ 命名與設計矛盾

**原始設計目標**：
> ParameterObject 原本是針對 event 不能指定 int/float/string/bool 以外的 parameter，
> 用 Unity.Object 解決（也就是用 ScriptableObject 解決這件事）。

**目前實際用途**：
> 後續發展，甚至可以用於修改 property。

**問題**：當功能從「event 參數容器」擴展到「property setter / preset / theme value」之後，
`ParameterObject` 這個名字不再準確反映職責。它現在更像是：

- 一個「named typed value holder」
- 一個「reflection-based property setter」
- 一個「serializable typed reference for editor」

**未來可能的更名方向（討論用）**：
- `TypedValueObject<T>` — 強調 typed value 容器
- `NamedValue<T>` — 強調 name = key 的設計
- `PropertyObject<T>` — 強調 property setter 用途
- `ParameterObject<T>` 保留 — 接受 historical name 不改（避免大規模 refactor）

**決策考量**：
- 大量現有 code / asset 已用 `ParameterObject` 命名
- 改名牽動 Theme / Preset / 所有具體型別
- 是否值得？或者僅在文檔中釐清「名稱保留但職責已擴大」即可？

**建議**：先不改名，但在 ParameterObject.cs 加 doc comment 說明：
- 歷史動機
- 現在的多重職責
- 何時用哪個衍生 class

## 與 Timeline 整合（已實作 2026-06-08）

> ⚠️ 舊 sketch 用過的名字 `SignalEventMarker` / `DirectorController` / `ParameterApplier` **都不存在於最終實作**，別被舊文誤導。

### 既有基建（早就存在，非本次新增）

`Packages/Animation/Runtime/TimeLine/Core/`，namespace `Yu5h1Lib.Timeline`：

```csharp
// SignalMarker{TMarker, TValue}.cs
public abstract class SignalMarker<TMarker, TValue> : Marker, INotification, INotificationOptionProvider
    where TMarker : SignalMarker<TMarker, TValue>
{
    public PropertyName id => new PropertyName(typeof(TMarker).Name);
    public TValue Value;
    public NotificationFlags flags => TriggerInEditMode | Retroactive;  // ⚠️ edit-mode 也會觸發
}

// SignalReceiver{TMarker, TValue}.cs
public abstract class SignalReceiver<TMarker, TValue> : BaseMonoBehaviour, INotificationReceiver
    where TMarker : SignalMarker<TMarker, TValue>
{
    [SerializeField] private UnityEvent<TValue> notified;
    public virtual void OnNotify(...) { if (notification is TMarker m) notified?.Invoke(m.Value); }
    // OnNotify 為 virtual（2026-06-08 從 non-virtual 改成 virtual，讓子類可加行為）
}
```

已有具體：`FloatSignal`/`FloatSignalReceiver`、`IntegerSignal`/`IntegerSignalReceiver`。
（`BoolSignal.cs` 是壞掉的空殼 — 無 namespace、無繼承，與本架構無關，待修。）

**關鍵洞察**：`ParameterObject` 是 ScriptableObject **asset**，marker 的 `Value` 欄位存的是 asset 引用（GUID+fileID），能正常序列化進 `.playable`。換成場景引用就不行（要 ExposedReference）。同一個「它是 asset」的性質，當初讓它塞進 UnityEvent，現在讓它塞進 marker。

### 本次新增的檔案

**1. `TimeLine/ParameterSignal.cs`** — marker，A/B 共用
```csharp
public class ParameterSignal : SignalMarker<ParameterSignal, ParameterObject> { }
```

**2. `TimeLine/ParameterReceiver.cs`** — 兩模式合一
```csharp
public class ParameterReceiver : SignalReceiver<ParameterSignal, ParameterObject>
{
    [SerializeField] private Object _target;
    public override void OnNotify(Playable origin, INotification notification, object context)
    {
        base.OnNotify(origin, notification, context);   // 模式 A：re-emit UnityEvent<ParameterObject>
        if (notification is not ParameterSignal signal || signal.Value == null || _target == null) return;
        signal.Value.ApplyTo(_target);                  // 模式 B：反射寫入 _target 的 property
    }
}
```
- `_target` 留空 → 純 re-emit（設計師接線）。
- `_target` 拖 component → 額外用 asset 名當 property 名反射寫入。
- 兩者可同時。`ParameterApplier`（曾短暫存在的獨立 receiver）已刪，職責併入這裡。

**3. `TimeLine/SkipPoint.cs`** — 純落點 marker（無 value）
```csharp
public class SkipPoint : Marker { }
```

**4. `Component/Addon/PlayableDirectorAddon.cs`**（既有檔，本次擴充）
```csharp
public class PlayableDirectorAddon : ComponentController<PlayableDirector>
{
    [SerializeField] private bool _canSkip = true;
    public bool CanSkip { get => _canSkip; set => _canSkip = value; }  // gate，可被 ParameterReceiver 寫入

    public bool SkipToNext()   // 找下一個 SkipPoint 跳過去；CanSkip=false 或無落點則回 false
    {
        if (!CanSkip || !director || director.playableAsset is not TimelineAsset timeline) return false;
        // EnumerateMarkers(timeline) 掃 markerTrack + GetOutputTracks()，挑 time>current 的最小 SkipPoint
        // director.time = target; director.Evaluate();
    }
    // [ContextMenu("Skip To Next")] 包了個 void wrapper（ContextMenu 要 void）
}
```

### 核心設計原則：gate 與 target 分離

| 概念 | 問題 | 由誰負責 |
|------|------|----------|
| **CanSkip** | 現在准不准跳？ | `PlayableDirectorAddon.CanSkip`（bool gate） |
| **Skip target** | 跳到哪？ | timeline 上的 `SkipPoint` marker（落點地標） |

兩者**徹底解耦** → 想單獨調整任一邊都不牽動另一邊：
- 某段不准跳 → `ParameterReceiver` + 名為 `CanSkip` 的 `BooleanObject` marker，在該段開頭設 false、結尾設 true。
- 改落點 → 純拖 `SkipPoint`，不碰程式。

### 使用流程

**Property 寫入（模式 B）**
1. Project 建 `ParameterObject` 衍生 asset（如 `BooleanObject`），**檔名 = 目標 property 名**（如 `CanSkip`），設 value。
2. 場景物件加 `ParameterReceiver`，`_target` 拖入要被改的 component（須有 public `CanSkip` 屬性或 `void CanSkip(bool)`）。
3. Timeline track 加 `ParameterSignal` marker，Inspector 把 asset 拖進 `Value`。
4. ▶️ → marker 觸發 → `_target.CanSkip = value`。

**Skip 跳轉**
1. Timeline 想讓玩家跳到的位置 → Add Marker → **Skip Point**。
2. 物件掛 `PlayableDirectorAddon`，`CanSkip` 勾起（或 runtime 由 marker/UI 設）。
3. skip 鍵 / UI button 接 `SkipToNext()`（editor 測試可用 gear 選單「Skip To Next」）。

**擴充規則**：想加任何可被 marker 寫入的 property → 目標 component 加 `public T MyProp { get; set; }` + 建命名 = property 名的 ParameterObject asset。不需改 `OnNotify`、不需新 marker class、不需新 receiver class。

## 關鍵原則

1. **`name` 即 contract**：SO 的內建 name 欄位作為目標屬性名，不需要額外 string field
2. **快取反射**：靜態 cache 跨 instance 共用，效能等同 dictionary lookup
3. **型別系統當守門員**：`DeclaredType` 檢查 + Property 型別比對，typo 不會誤寫
4. **可組合**：單個 ParameterObject、List<ParameterObject>（Preset）、Theme（key-based lookup）都共用同一抽象
5. **編輯器友好**：`[Inline(true)]` 讓 SO 內嵌顯示、`[Decorable]` 支援裝飾器擴充

## 反模式提醒

不要做的事：
- ❌ 在 marker 上加 `string targetProperty` 欄位 — 用 ParameterObject asset 的 name 就好
- ❌ 為每個「要被寫入的 property」寫 specialized receiver class — 一個 `ParameterReceiver` + `_target` + `ApplyTo` 通吃
- ❌ 把 gate（CanSkip）和 target（SkipPoint）綁進同一個 marker/欄位 — 兩者要分離

關於 typed signal 的界線（不是反模式，是選用準則）：
- `FloatSignal` / `IntegerSignal` 這類 typed marker **合法且已存在** — 適合「單純把一個 primitive 值經 UnityEvent re-emit」的場景。
- 當你需要的是「**寫入任意 property**」或「攜帶複雜/Object 型別」→ 用 `ParameterSignal` + `ParameterReceiver`，不要為此再增生 typed marker。

## 相關記憶 / 文件交叉

- 本文記錄系統「**是什麼**」與「**為什麼可重用**」
- Timeline 整合已實作（2026-06-08）→ 見上方「與 Timeline 整合」段落，含實際檔名與程式
- Memory file: `Yu5h1lib-Unity-ScriptableObject-Architecture.md` 引用本文
