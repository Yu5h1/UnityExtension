# TweenBehaviour 殼化與 KFC 驗證計畫

> 方向草案。本文規劃 Unity 元件的歸屬、舊入口相容性及消費端驗證。[Motion System Refactor](../.agents/plans/Motion_System_Refactor.md) 追蹤 Core 接入與雙 runner 首輪。[Tween 1.0.0](../../../Motion/Documentation/ModuleSpec/Tween.md#goal) 定義共用播放契約。

## 系統階層與關係

```text
Unity Tween 元件
├─ Motion package：TweenBehaviour 共用生命週期與播放控制 → Core Tween 契約
├─ DOTweenExtension：DOTween runner、舊元件與 Editor 入口 → Motion + DOTween
└─ 消費端：KFC 主回歸，BonghuoVR 發版相容性檢查
```

Motion 元件透過 `Tween.IHandle` 操作補間。DOTweenExtension 提供 `Tween.IRunner` 與引擎設定轉接，讓舊場景的元件沿用現有接線，並從同一條入口換掉建立、播放與回收流程。

## 消費端盤點

2026-11-15 在 `W:/UnityProject` 找到 12 個有 `Packages/manifest.json` 的專案。用 DOTweenExtension `Runtime/Component` 的腳本 GUID 比對各專案 `Assets`／`Packages` 中 `.unity`、`.prefab`、`.asset` 的 `m_Script`，並搜尋 C# 型別引用。數字是 YAML 直接引用數，包含測試、備份及 Recovery 資產。Prefab 展開後的實例數及 Play Mode 行為需另外驗證。

| 專案 | 直接引用 | 相關入口 |
| --- | ---: | --- |
| KFC | 145 | `DOFade` 76、`DOMove2D` 25、`DORotate2D` 21、`DOSpriteAnimation` 11、`DOColor` 7、`DOScale` 5。`KFC_RuntimeInitializeOnLoad.OnSceneUnloaded` 查找 `TweenBehaviour` 並逐一呼叫 `Kill()`，接著執行 `DOTween.KillAll(false)`。`A001` 的 UnityEvent 有 `TryPlayTween` 綁定及舊 generic 組件型別名稱。 |
| BonghuoVR | 16 | `DOFade` 9、`DOColor` 7，涵蓋 `Main.unity`、XR UI 和玩家 prefab。 |
| 同批其餘 10 個專案 | 0 | `Yu5h1LibTest` 已引用 Motion，可用來建立獨立的 Unity runner 驗證。 |

KFC 與 BonghuoVR 都以 `file:` 指向同一個 DOTweenExtension 來源。KFC 使用 Unity `6000.6.0f1`，BonghuoVR 使用 `6000.3.9f1`，目前 Motion package 宣告 `6000.6`。首輪以隔離的套件來源供 KFC 驗證，待兩端的套件解析、編譯及場景回歸通過，再決定共用發版所用的依賴版本。

## 責任與相容性

- **Motion package** 擁有引擎無關的 Unity `TweenBehaviour` 基底。共用層保留元件初始化、啟停、反向、倒回、等待、事件派送與釋放等流程，使用 Core 的 runner／handle 契約。目標物件的取值與寫入由各元件提供。
- **DOTweenExtension** 擁有 DOTween runner、更新時鐘與引擎設定映射。過渡期間保留 `Yu5h1Lib.TweenBehaviour` 舊型別與 `Kill()`，讓 KFC 的型別查找繼續找到已切換的元件。Motion 的基底使用不同的完整型別名，舊入口委派給它。原具體元件及仍有引用的 generic 入口保留 `.meta` GUID、序列化欄位與 UnityEvent 方法簽章，Editor 工具也跟著驗證。`EaseType`、`LoopType`、`UpdateType`、DOTween 預設值與資源回收留在引擎轉接層解讀。
- **素材相容** 同時查驗 GUID、原 namespace／assembly、Prefab overrides、序列化欄位、UnityEvent 目標及 C# 呼叫。KFC 資產已出現舊 `DOTweenBehaviour` 組件名與舊 generic 型別名稱。保留 GUID 後，仍要觸發事件及執行卸載清理，證明原本的接線有效。以原版實測分辨現存失效與遷移回歸。
- **生效語意** 先核對現行 `OnDisable` 的 `Rewind`／`DOTween.Pause(component)`、延遲及重入，以及 `UseUnscaledTime`、`updateType`、`isIndependentUpdate` 的實際優先順序。Motion 管理自己建立的 handle，舊 package 的目標群組與整體清理由 DOTweenExtension 映射，並核對直接使用 DOTween 的 tween 是否受影響。

## 遷移與驗收

1. **固定基準。** 記錄兩個專案、套件、Core DLL 與 DOTween 版本。以原版 KFC 記錄 UI 淡入、載入動畫、移動平台、傳送門及 `A001` 的事件與卸載行為。至少包含 `timeScale = 0`、停用後重啟、延遲中卸載與場景重載。用同一份素材對照改版。
2. **讓 Unity 跑通首輪契約。** 接入已驗收的 Core Tween，核對 DLL 實際載入路徑與公開入口。在 `Yu5h1LibTest` 以真實 DOTween runner 跑 14 個共享案例、123 步，核對值、狀態、事件順序與資源回收。由 Motion 核對 Core 及 DOTween 的證據。
3. **先換 KFC 的一個純量路徑。** 以 `StartSceneMenu` 中單週期 `DOFade` 為第一個代表案例，從舊元件入口切到 Motion 基底與 DOTween runner。沿用原 prefab 和欄位，比對自動播放、停用倒回與暫停時的表現。以 `A001` 核對已切換元件仍能被原型別查詢、觸發 UnityEvent，並在卸載時停止。可用原套件版本與對應的 lockfile 回復基準。
4. **依實際能力擴大。** `Loading.prefab` 的 `DOSpriteAnimation` 有無限循環、非縮放更新與離散影格。`movingPlatform.prefab` 的 `DOMove2D` 有 `Vector3`、`LoopCount = -1` 和固定更新，另須驗證本地端點轉世界座標及完成／倒回傳遞的速度。`Teleport Gate.prefab` 的 `DORotate2D` 有無限累加旋轉，需核對角度累加。先為各能力補足重播與素材基準，再切換對應元件。`DOColor`、`DOScale` 隨型別能力驗證後接續。
5. **驗證共用發版。** KFC 檢查 Build Settings 啟用的 `Start`、`A001`、`A002`、`A005`，再抽查 `A003`、`A004` 及具代表性的 prefabs。要求無新增 Missing Script、遺失欄位／覆寫、失效 UnityEvent 或場景卸載後殘留的 tween。BonghuoVR 需在其現用 Unity 版成功解析依賴，並通過 `Main`／XR UI 淡入與顏色的回歸。若 Motion 相依在 `6000.3` 無法通過，BonghuoVR 維持原可用版本，KFC 試遷移繼續使用隔離來源。確認舊型別、方法與組件名的消費端已遷移後，再評估收掉各相容入口。

首輪實作預估中型、系統層級影響。共用 MonoBehaviour、runner 和 Editor／資產接線跨套件。Core 參考 runner 提供既有基準，真實 DOTween、Unity Editor 載入及場景 Play Mode 提供消費端證據。完整元件遷移按第 4 步拆批估量，PrimeTween 另輪處理。
