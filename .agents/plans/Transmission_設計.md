# Yu5h1Lib.Transmission — 結案:保留投遞層

> Status: **closed / usable**  
> Date: 2026-07-06  
> Final implementation direction: Transmission 只負責 **message routing + payload delivery**。  
> 收到後的實際呼叫交給 UnityEvent,argument override 交給 `ArgumentInfo` + `UnityEventEx.LoadArgument`。

## 終局結論

Transmission 目前設計有用,範圍也已經收斂:

```text
MessageSender
MessageReceiver
Broadcaster
BroadcasterProxy
```

它不再需要和舊 Invocation 框架綁死。最終分工是:

```text
Transmission
  負責找到 receiver,送出 msg + ArgumentInfo[]

UnityEvent / UnityEventEx
  負責根據 msg 找 UnityEvent,替換 argument,然後 Invoke
```

也就是:

```text
Transmission = 投遞層
UnityEvent = 執行層
ArgumentInfo = runtime argument payload
```

## 現行核心

涉及檔案:

```text
Packages/common/Runtime/Transmission/MessageSender.cs
Packages/common/Runtime/Transmission/MessageReceiver.cs
Packages/common/Runtime/Event/Broadcaster.cs
Packages/common/Runtime/Event/BroadcasterProxy.cs
Packages/common/Runtime/Event/ArgumentInfo.cs
Packages/common/Runtime/Extension/UnityEventEx.cs
```

### 定址投遞:MessageSender → MessageReceiver

用途:

```text
1 -> 1
source 在 runtime 知道 target GameObject
target 上有 MessageReceiver
```

流程:

```text
MessageSender.target
-> target.GetComponent<MessageReceiver>()
-> MessageReceiver.TryInvoke(msg, ArgumentInfo[])
```

`MessageSender` 可由 XR event / collision / trigger runtime ref 設定 target。這解掉 prefab asset 不能直接保存 scene instance reference 的問題。

### 接收:MessageReceiver

`MessageReceiver` 保存:

```text
KeyValues<string, UnityEvent>
```

收到:

```text
msg + ArgumentInfo[]
```

執行:

```text
evt = events[msg]
foreach argument in args:
    evt.LoadArgument(argument)
evt.Invoke()
```

因此 receiver 不需要知道 `ParameterTargetBindingGroup` 或 Invocation framework。

### 廣播:Broadcaster / BroadcasterProxy

用途:

```text
1 -> many
全域/階段/訊號式事件
channel/msg registry
```

保留給真正需要 pub/sub 的場合,例如:

```text
停船
階段切換
全域狀態通知
```

不再拿廣播處理明確 1→1 的互動呈現。

## XR / prefab 邊界接法

互動事件的 runtime args 可以提供實例參照,這是 prefab asset 無法序列化保存但 runtime 可以安全取得的路徑。

典型流程:

```text
XRGrabInteractable.hoverEntered/selectEntered
-> eventArg.interactorObject.transform
-> GetComponentInParent<MessageReceiver>()
-> MessageSender.target = receiver.gameObject
-> MessageSender.Send("Hover" / "Select")
```

專案側輔助:

```text
Assets/Scripts/XREventArgGameObjectResolver.cs
```

它實作 `Yu5h1Lib.MVVM.IGetter<GameObject>`,從 `HoverEnterEventArgs` / `SelectEnterEventArgs` 解析 `MessageReceiver` 所在 GameObject,供 `MessageSender` 讀取。

## 與 Invocation 的最終關係

舊想法:

```text
Transmission 搬運 ParameterSet
InvocationReceiver 執行 ParameterTargetBindingGroup
```

現行結論:

```text
Transmission 搬運 msg + ArgumentInfo[]
MessageReceiver 找 UnityEvent
UnityEventEx.LoadArgument 替換 PersistentCall ArgumentCache
UnityEvent.Invoke 執行
```

所以 Transmission 不再依賴 `ParameterSet` / `ParameterTargetBindingGroup` 作為主線資料格式。

## 命名收束

目前命名可以先維持:

```text
MessageSender
MessageReceiver
Broadcaster
BroadcasterProxy
ArgumentInfo
UnityEventEx.LoadArgument
```

`Message` 在這裡代表 routing key / command key。  
`ArgumentInfo` 代表要覆寫到 UnityEvent PersistentCall 的 argument payload。

## 關閉理由

Transmission 的有效範圍已經清楚:

```text
找 receiver
送 msg
帶 ArgumentInfo payload
```

不要再把它拉回大型 Invocation / Action framework。  
需要 1→1 時用 `MessageSender`。  
需要 1→多時用 `Broadcaster`。  
收到後的行為交給 UnityEvent。

此設計到此結束;後續只做小修與實作穩定化。
