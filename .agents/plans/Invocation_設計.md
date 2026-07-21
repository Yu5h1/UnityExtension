# Yu5h1Lib.Invocation — 結案:由 UnityEvent ArgumentCache 取代

> Status: **closed / superseded**  
> Date: 2026-07-06  
> Final implementation direction: **不要重做 Invocation / Action framework**。  
> 直接使用 UnityEvent 既有的 `PersistentCall` 作為 target + method 的序列化呼叫資料,只用 reflection 替換它的 `ArgumentCache`。

## 終局結論

原本的 Invocation 設計想做:

```text
ParameterSet
-> ParameterTargetBindingGroup
-> ParameterTargetBinding
-> ParameterMember.Apply / MethodInfo.Invoke
```

這條路可以成立,但對目前需求太大。真正需求是:

```text
prefab / event 傳入 runtime argument
-> 找到 receiver 既有 UnityEvent 裡的 PersistentCall
-> 替換該 call 的 argument
-> Invoke UnityEvent
```

所以 Invocation 不再作為主要框架推進。UnityEvent 本身已經是可序列化的 invocation/action:

```text
UnityEvent.PersistentCall
= target object reference
+ methodName
+ listenerMode
+ ArgumentCache
```

我們只補 UnityEvent 缺的這塊:

```text
runtime argument override
```

## 現行核心

涉及檔案:

```text
Packages/common/Runtime/Event/ArgumentInfo.cs
Packages/common/Runtime/Extension/UnityEventEx.cs
```

### `ArgumentInfo`

`ArgumentInfo` 是外部 payload,對應 UnityEvent 的 persistent argument override。

它保存:

```text
targetName
methodName
listenerMode
objectArgument
intArgument
floatArgument
stringArgument
boolArgument
```

其中:

```text
targetName + methodName + listenerMode
= 查找既有 PersistentCall 的 key

listenerMode + 對應 argument 欄位
= 要覆寫進 ArgumentCache 的 value
```

不保存 `ObjectArgumentAssemblyTypeName`。那是 Unity 內部 `ArgumentCache` 對既有 PersistentCall 的型別資訊;目前只是替換 value,不建立新 PersistentCall,所以不應由外部 payload 覆寫。

### `UnityEventEx.LoadArgument`

`UnityEventEx.LoadArgument(UnityEventBase, ArgumentInfo)` 使用 reflection:

```text
UnityEventBase.m_PersistentCalls
-> PersistentCallGroup.m_Calls
-> match target.name / methodName / listenerMode
-> PersistentCall.m_Arguments
-> write m_IntArgument / m_FloatArgument / m_StringArgument / m_BoolArgument / m_ObjectArgument
-> DirtyPersistentCalls
```

支援 UnityEvent 原生 persistent listener mode:

```text
int
float
string
bool
UnityEngine.Object
```

不處理 runtime listener;不建立新 call;不重新選 method。

## 應用流程

```text
MessageReceiver.TryInvoke(msg, ArgumentInfo[])
-> 找到 msg 對應 UnityEvent
-> foreach ArgumentInfo: unityEvent.LoadArgument(argument)
-> unityEvent.Invoke()
```

UnityEvent 仍負責:

```text
Inspector method picker
target reference
methodName
listenerMode
PersistentCall serialization
```

ArgumentInfo 只負責:

```text
跨 prefab / runtime 邊界帶入新的 argument value
```

## 舊設計歸檔

以下舊型別/概念不再是目前主線:

```text
ParameterTargetBinding
ParameterTargetBindingGroup
InvocationReceiver
ParameterSet 作為 Invocation kwargs
ParameterMember fan-out binding
```

它們代表「自製資料驅動反射呼叫框架」。這在未來仍可能有用途,例如:

```text
不想使用 UnityEvent
需要多欄位 fan-out setter
需要非 UnityEvent 的資料呈現 binding
```

但對目前 Transmission + UnityEvent event payload 需求而言,這是過度設計。主路線已切換為:

```text
Transmission 負責送 msg + ArgumentInfo[]
UnityEventEx 負責替換 UnityEvent argument
UnityEvent 自己負責真正的 invocation
```

## 關閉理由

這次最大的設計修正:

```text
問題不是缺 Action 系統
問題只是 UnityEvent PersistentCall 的 argument 需要 runtime override
```

因此 Invocation 設計到此結束。保留本文作為歷史紀錄與避免回頭重做大框架的警示。
