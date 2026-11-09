# Broadcaster：對已註冊物件呼叫方法

```text
common 訊息投遞
├── Broadcaster：保存註冊物件、分派方法呼叫、清理註冊
├── BroadcasterProxy：保存 methodName，提供 UnityEvent 入口
└── 接收物件：執行自己的公開方法
    └── MessageReceiver：Call(signal) → 對應 UnityEvent
```

Broadcaster 的對象是明確註冊的 `UnityEngine.Object`，可以是 Component 或 ScriptableObject。它不認識 MessageReceiver、不保存 signal 表，也不遍歷 GameObject 的組件或子階層。對 GameObject 本身註冊時，只能呼叫 GameObject 自己的公開方法；要呼叫組件的方法，註冊該組件。

`Register(Object)` 重複呼叫不會增加重複訂閱，`Unregister(Object)` 解除訂閱。訂閱生命週期由場景事件配置，例如 MonoEvent 的 Enabled／Disabled；MessageReceiver 自己不再訂閱，也不再建立 Broadcaster。

## 方法與參數

```csharp
hub.Dispatch("ResetState");
hub.Dispatch("Call", "signal-key");
hub.Dispatch("SetNumber", 42);
hub.Dispatch<string>("Call", null);
```

無參數與一個 null 參數是不同的呼叫。單參數 `Dispatch<T>` 使用非 null 值的實際型別；null 使用 T。因此，宣告為 Object 而實際為具體組件的參數，會選擇具體型別適用的方法。方法解析沿用 MethodInvoker 的公開實例方法與相容簽章規則，支援繼承方法。找不到方法或個別目標發生例外會回報警告，其他對象繼續接收；回傳值代表至少成功呼叫一個對象。

Proxy 的 `methodName` 預設為 `Call`。`Dispatch()` 使用零個參數；`Dispatch(string/int/float/bool/UnityEngine.Object)` 將單個參數連同已設定的方法名稱交給 Broadcaster。這些入口保留 Inspector 的單參數接線方式。

## 派送與清理

派送使用當次註冊清單的快照；回呼期間新增對象會在下次派送接收，已解除註冊的對象會跳過。巢狀派送使用自己的快照。已銷毀的物件會移除，停用的 Behaviour 或 inactive GameObject／Component 不接收。

Hub 停用或銷毀時只清空自己的註冊清單。`instance`、實例快取與建立限制完全沿用 `SingletonBehaviour<Broadcaster>`，Broadcaster 不遮蔽入口、不保存退出旗標，也不重設基類快取。基類依既有規則處理退出／Edit mode 的建立限制；取得既有實例不等於禁止所有退出期間的方法呼叫。Proxy 的 `Unregister` 仍只查既有 Hub，不經可能建立實例的入口。

## 相容性與驗證界線

MessageReceiver 的 `_events`、FormerlySerializedAs、Call／TryInvoke 保持原有合約；移除自動註冊、退訂與 RefreshRegistration。以前依賴自動廣播訂閱的物件，需要明確配置 Register／Unregister。Broadcaster 原本的 `Dispatch(string signal)` 改為 `Dispatch(string methodName)`；原訊號式呼叫改為 `Dispatch("Call", signal)`，或由 methodName 設為 Call 的 Proxy 傳送。此變更影響所有透過本機 file: 套件引用 common 的專案。

2026-11-10 此輪只允許離線檢查與提交，不啟動 Unity，不改寫任何場景／prefab。離線編譯與受控替身下的 managed 行為檢查，不等於 Unity 的 Play、Inspector 或原生物件銷毀順序驗收。正式 NUnit 案例位於 `Packages/common/Tests/Editor/BroadcasterObjectDispatchTests.cs`，後續需在允許啟動 Editor 時執行。
