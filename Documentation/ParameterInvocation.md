# Parameter、Method 與 Invocation 計畫

## 狀態

本文件記錄目前已收斂的設計方向與後續實作項目，功能尚未實作完成。

## 已決定的方向

- `ParameterObject` 表示單一 Property value。
- `MethodObject` 表示單一 Method 與該 Method 的 parameters。
- `InvocationObject` 表示 Target 尋址與一組依序執行的 Methods。
- `MethodObject.Descriptor` 是給 `InvocationObject` inline 保存的 Method 資料。
- Property 使用 `ApplyTo(target)`。
- Method 使用 `Invoke(target)`。
- 暫時不建立 `IMemberDescriptor`、`MemberKind` 或其他共同介面。
- Reflection 執行邏輯集中到共用服務，不在各資料類別中重複實作。

## 設計原則

`ParameterObject`、`MethodObject` 與 `InvocationObject` 都維持單數語意：

```text
ParameterObject
→ 一個 Property value

MethodObject
→ 一個 Method

InvocationObject
→ 一次 Invocation，其中可以依序執行多個 Methods

GenericObjectPreset
→ 一整套 Property values
```

目前沒有呼叫端需要以同一個型別同時接收 Parameter、Method 與其他 Member 資料，因此不為名稱相似而提前建立共同介面。若未來出現實際的統一消費者，再從該使用情境反推介面。

## ParameterObject

`ParameterObject` 繼續保存具型別值，並將值套用到 Target 的 public Property。

```text
ParameterObject
├─ memberName → ScriptableObject.name
├─ value
├─ DeclaredType
└─ ApplyTo(target) → Set Property
```

它繼續支援：

- UnityEvent 特殊參數
- Inline 編輯
- Scene Serialization
- Asset 與 Sub-Asset
- Generic Preset Property values

`GenericObjectPresetDrawer` 只列出符合以下條件的 Properties：

```csharp
property.GetIndexParameters().Length == 0
property.SetMethod?.IsPublic == true
ParameterObjectUtility.IsSupported(property.PropertyType)
```

## MethodObject

`MethodObject` 是可獨立引用的單一 Method 資料。

```csharp
public class MethodObject : ScriptableObject
{
    [SerializeField, Inline(true)]
    private List<ParameterObject> _parameters;

    public string methodName => name;
    public IReadOnlyList<ParameterObject> parameters => _parameters;

    public bool Invoke(UnityEngine.Object target)
        => MethodInvoker.Invoke(target, methodName, _parameters);
}
```

責任：

- 使用 `ScriptableObject.name` 作為 Method name。
- 保存該 Method 的 parameters。
- 將實際 Reflection 呼叫委派給 `MethodInvoker`。
- 不處理 Path、Target Type 或 Method 集合。

例如：

```text
MethodObject
name = AddForce

parameters
├─ Vector3Object
└─ ForceModeObject
```

## MethodObject.Descriptor

`MethodObject.Descriptor` 是 Method 的 inline serializable representation，主要提供給 `InvocationObject` 使用。

```csharp
public class MethodObject : ScriptableObject
{
    [Serializable]
    public class Descriptor
    {
        [SerializeField]
        private string _methodName;

        [SerializeField, Inline(true)]
        private List<ParameterObject> _parameters;

        public string methodName => _methodName;
        public IReadOnlyList<ParameterObject> parameters => _parameters;

        public bool Invoke(UnityEngine.Object target)
            => MethodInvoker.Invoke(target, _methodName, _parameters);
    }
}
```

`MethodObject` 與 `Descriptor` 的差異：

```text
MethodObject
→ 有 Unity Object identity
→ Method name 來自 ScriptableObject.name
→ 可以獨立引用、Inline 或保存成 Asset

MethodObject.Descriptor
→ 是一般 Serializable data
→ Method name 由 _methodName 保存
→ 可以直接存在 InvocationObject 的 List 中
```

`Descriptor` 暫時維持巢狀類別，因為它目前只代表 `MethodObject` 的 inline data form。若未來成為多個系統直接使用的一級資料型別，再考慮提升成頂層 `MethodDescriptor`。

## InvocationObject

`InvocationObject` 負責 Target 尋址與 Method 執行順序。

```csharp
public class InvocationObject : ScriptableObject
{
    [SerializeField]
    private string _path;

    [SerializeField]
    private SerializedType _targetType;

    [SerializeField]
    private List<MethodObject.Descriptor> _methods;

    public bool Invoke(UnityEngine.Object target);
}
```

關係：

```text
InvocationObject
├─ Path
├─ TargetType
└─ Methods: List<MethodObject.Descriptor>
   ├─ Method A
   ├─ Method B
   └─ Method C
```

執行流程：

1. 解析 Target。
2. 依 List 順序執行每個 `MethodObject.Descriptor`。
3. Descriptor 將呼叫委派給 `MethodInvoker`。
4. 任一項失敗時回傳 `false`；是否繼續後續項目保留為可調整策略。

## Target 尋址

`InvocationObject` 採用與 `GenericObjectPreset` 一致的相對路徑規則：

- `_path` 為空時，直接使用傳入的 Target。
- Target 是 `GameObject` 時，以該 GameObject 作為 root。
- Target 是 `Component` 時，以其 GameObject 作為 root。
- Target 無法取得 GameObject 時，忽略 `_path`，直接使用原 Target。
- 找到 child 後，依 `_targetType` 取得 GameObject、Transform 或指定 Component。

`_path` 第一版只代表 child path。是否擴充為 Property path 留待後續需求確認。

## Method 支援範圍

Inspector 預計只提供：

- public instance Method
- 非 generic Method
- 非 special-name Method
- 不包含 `ref`、`out` 或 `in` parameters
- 每個 parameter type 都有可用的 `ParameterObject<T>`

第一版支援：

- 無參數 Method
- 單一參數 Method
- 多參數 Method
- overload

Method result 目前不保存。是否只顯示 `void` Methods，或允許有回傳值但忽略結果，仍是待決項目。

## Overload

Method 由以下資料共同識別：

```text
Target Type + Method Name + Parameter Declared Types
```

Inspector 顯示完整 signature：

```text
SetValue (Int32)
SetValue (Single)
```

Runtime 使用每個 `ParameterObject.DeclaredType` 解析 overload，不將 signature 編碼進 Method name。

## Reflection 執行層

`MethodObject` 與 `MethodObject.Descriptor` 共用同一個執行服務：

```csharp
public static class MethodInvoker
{
    public static bool Invoke(
        UnityEngine.Object target,
        string methodName,
        IReadOnlyList<ParameterObject> parameters);
}
```

Resolver/cache key：

```text
Target Type + Method Name + Parameter Types
```

Reflection exception 應回報：

- Target Type
- Method name
- Parameter types
- Inner exception

`ParameterMember` 目前同時處理 Property 與 Method fallback。後續預計將新 API 拆成明確的 Property setter 與 Method invoker，但舊 fallback 暫時保留以維持資料相容。

## 資料夾與命名

目前的 `Runtime/Data/Parameter` 無法完整表達未來的 `MethodObject` 與 `InvocationObject`。暫定方向：

```text
Packages/common/Runtime/
├─ Data/
│  └─ Member/
│     ├─ ParameterObject.cs
│     ├─ MethodObject.cs
│     ├─ InvocationObject.cs
│     └─ Object/
│        ├─ BooleanObject.cs
│        ├─ FloatObject.cs
│        └─ ...
│
└─ Reflection/
   ├─ MethodInvoker.cs
   └─ PropertySetter.cs
```

這個路徑尚未正式決定。資料模型與 Reflection 執行機制應分開，避免只因內部使用 Reflection，就把所有可序列化資料都歸類為 Reflection。

## Inspector

`MethodObject` Inspector/Drawer 顯示單一 Method：

```text
Method        [AddForce (Vector3, ForceMode)]
Parameters
    Force       [0, 10, 0]
    Force Mode  [Impulse]
```

`InvocationObject` Inspector/Drawer 顯示 Target 與 Method List：

```text
Target Type  [Rigidbody]
Path         [Body]

Methods

▼ AddForce (Vector3, ForceMode)
    Force       [0, 10, 0]
    Force Mode  [Impulse]

▼ Sleep ()

[Add Method]
```

加入 Method 時，Drawer 依 parameter types 建立對應的 ParameterObject sub-assets；移除時必須支援 Undo 並清除所屬 sub-assets。

## UnityEvent 入口

Scene Component 提供無參數入口：

```csharp
public class InvocationReceiver : MonoBehaviour
{
    [SerializeField, Inline(true)]
    private InvocationObject _invocation;

    public void Invoke()
    {
        _invocation.Invoke(gameObject);
    }
}
```

UnityEvent 只需指定 `InvocationReceiver.Invoke()`。Path、Target Type、Methods 與 parameters 由 `InvocationObject` 保存。

## 不採用的設計

目前不採用：

- `IMemberDescriptor`
- `IPropertyDescriptor`
- `IMethodDescriptor`
- `MemberKind`
- `List<IMemberDescriptor>`
- 為了序列化 interface 而導入 `[SerializeReference]`
- 讓 `InvocationObject` 同時設定 Property 與呼叫 Method
- 讓單一 `MethodObject` 保存多個 Methods

原因是目前沒有實際呼叫端需要這些共同抽象，提前介面化只會增加序列化、型別與責任邊界的複雜度。

## 待決項目

1. 有回傳值的 public Method 是否出現在 Inspector。
2. Invocation 中某個 Method 失敗後，是否繼續後續 Methods。
3. `MethodObject.Descriptor` 未來是否提升為頂層類別。
4. `Data/Member` 與 `Reflection` 的最終資料夾名稱。
5. `ParameterMember.Apply()` Method fallback 的移除時程。
6. Scene 內 Inline Object 與 Sub-Asset 的實際建立、刪除流程。

## 預定實作順序

1. 確認資料夾與 namespace。
2. 新增 `MethodInvoker` 與 Reflection cache。
3. 新增單一任務的 `MethodObject`。
4. 新增 `MethodObject.Descriptor`。
5. 新增 `InvocationObject` 與 Target resolution。
6. 建立 Runtime tests，涵蓋無參數、單參數、多參數及 overload。
7. 建立 `MethodObject` Drawer。
8. 建立 `InvocationObject` Drawer。
9. 修正 `GenericObjectPresetDrawer` 的 public Property 篩選。
10. 新增明確的 Property setter API，保留舊 fallback 相容。
11. 新增 UnityEvent Scene 入口。
12. 執行 Unity Core build 與實際 Scene serialization 測試。

## 驗收條件

- `ParameterObject` 只負責單一 Property value 與 `ApplyTo`。
- `MethodObject` 只負責單一 Method 與 `Invoke`。
- `InvocationObject` 使用 `List<MethodObject.Descriptor>` 保存 Method sequence。
- `GenericObjectPreset` 只列出並設定 public Properties。
- 支援無參數、單參數、多參數與 overload Method。
- GameObject 與 Component 都能作為 child path root。
- 找不到 path、Component 或 Method 時安全失敗並提供明確訊息。
- Inline Objects 與 ParameterObject parameters 能隨 Scene 正確保存。
- Asset/Sub-Asset 建立、刪除與 Undo 不留下孤立資料。
- 既有 ParameterObject 與 GenericObjectPreset 資料保持相容。
