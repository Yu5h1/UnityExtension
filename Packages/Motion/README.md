# Motion

`com.yu5h1.motion` 提供共用 C++ 核心的薄 C# 接口，公開型別位於 `Yu5h1Lib`。使用端 asmdef 引用 `Yu5h1Lib.Motion`。套件不依賴 Animation 或 UIToolkit。

Windows x64 DLL 已通過 C#／.NET Framework x64 的 779 狀態測試。Android arm64-v8a SO 已建置及檢查 ELF，尚未完成 Unity IL2CPP／裝置驗證，Android importer 保持停用。Unity Editor／Player 驗證仍待執行；詳見 `delivery.json`，不要將套件存在等同平台驗收完成。

```csharp
using Yu5h1Lib;
using UnityEngine;

using (var flight = new MotionThrow2D())
{
    flight.Launch(new Vector2(100, 100), new Vector2(250, 0));
    flight.Tick(1f / 60f, new Rect(0, 0, 800, 600), 31);
    Vector2 position = flight.State.Position;
}

using (var scatter = new MotionScatter2D())
{
    scatter.SetItems(new[] { 1, 2, 3 });
    MotionPlacement[] result = scatter.Resolve(new Rect(0, 0, 800, 600));
}
```

正式宿主持有每個互動的實例，逐幀傳入秒數與局部座標，結束／取消／卸載時 Dispose。各實例單執行緒使用；不可在每個 Tick 重建。Throw 使用中心座標，Scatter 使用項目左上角；`MotionPlacement.Position == null` 表示無空位。`Pull` 回傳是否嘗試求解，不表示捕獲成功。連續 Pull 必須讀取前一次的最新位置。

捕獲、釋放條件、成員管理、畫面更新與取消屬於使用端。Throw 目前不包含障礙物命中；不要直接替換既有 hit 路徑。

`Configuration/motion.settings.json` 是 WEB-7 定案前的版本化宣告，含六項設定、型別、預設和範圍。修改唯一來源 `Motion/configuration/motion.settings.json` 後重新交付；不要修改此副本。預設由原生核心讀取，設定 JSON 不包含算法。

測試專案 `manifest.json` 的 `testables` 加入 `com.yu5h1.motion`，即可執行 `Yu5h1Lib.Motion.Editor.Tests`。可重現建置、平台驗證命令與驗收限制見 Motion repository 的 `Documentation/NativeDelivery.md`。插件為預先編譯原生庫，C# 透過 C ABI 呼叫，無需在使用者裝置編譯 C++。
