# TweenBehaviour 殼化與 KFC 驗證計畫

> 套件歸屬與命名方向已定。本文規劃 Unity 元件、舊入口相容性及消費端驗證。[Motion System Refactor](../.agents/plans/Motion_System_Refactor.md) 追蹤 Core 接入與雙 runner 首輪。[Tween 1.0.0](../../../Motion/Documentation/ModuleSpec/Tween.md#goal) 定義共用播放契約。

## 系統階層與關係

```text
Unity 運動與動畫
├─ Motion（com.yu5h1.motion）
│  ├─ Tween 元件與共用生命週期 → Core Tween 契約
│  └─ Motion Tween Adapter → 專案採用的 Tween 引擎
├─ Animation：動畫資產、動畫工具與 Sprite 逐格播放
└─ Tween 遷移驗證：KFC 主回歸，BonghuoVR 發版相容性檢查
```

Unity 的補間與運動能力集中交付於 `com.yu5h1.motion`。元件透過 `Tween.IHandle` 操作補間，Motion Tween Adapter 是套件內部的引擎轉接模組，首個後端採 DOTween。既有 `com.yu5h1.dotweenextension` 在遷移期間承接原場景與首輪 runner 驗證。

## 消費端盤點

2026-11-15 在 `W:/UnityProject` 找到 12 個有 `Packages/manifest.json` 的專案。用 DOTweenExtension `Runtime/Component` 的腳本 GUID 比對各專案 `Assets`／`Packages` 中 `.unity`、`.prefab`、`.asset` 的 `m_Script`，並搜尋 C# 型別引用。數字是 YAML 直接引用數，包含測試、備份及 Recovery 資產。Prefab 展開後的實例數及 Play Mode 行為需另外驗證。

| 專案 | 直接引用 | 相關入口 |
| --- | ---: | --- |
| KFC | 145 | `DOFade` 76、`DOMove2D` 25、`DORotate2D` 21、`DOSpriteAnimation` 11、`DOColor` 7、`DOScale` 5。`KFC_RuntimeInitializeOnLoad.OnSceneUnloaded` 查找 `TweenBehaviour` 並逐一呼叫 `Kill()`，接著執行 `DOTween.KillAll(false)`。`A001` 的 UnityEvent 有 `TryPlayTween` 綁定及舊 generic 組件型別名稱。 |
| BonghuoVR | 16 | `DOFade` 9、`DOColor` 7，涵蓋 `Main.unity`、XR UI 和玩家 prefab。 |
| 同批其餘 10 個專案 | 0 | `Yu5h1LibTest` 已引用 Motion，可用來建立獨立的 Unity runner 驗證。 |

KFC 與 BonghuoVR 都以 `file:` 指向同一個 DOTweenExtension 來源。KFC 使用 Unity `6000.6.0f1`，BonghuoVR 使用 `6000.3.9f1`，目前 Motion package 宣告 `6000.6`。首輪以隔離的套件來源供 KFC 驗證，待兩端的套件解析、編譯及場景回歸通過，再決定共用發版所用的依賴版本。

## 責任與相容性

- **Motion** 擁有引擎無關的 Unity `TweenBehaviour` 基底、淡入與位移等補間元件及共用 Inspector。殼的公開範圍以各 runner 都須提供的操作與狀態為準，沿用 Core 的建立、播放、暫停、反播、停止、倒回、完成與事件契約。目標物件的取值與寫入由各元件提供。共用的 Inspector 欄位與 UnityEvent 接線讓同一元件能切換 runner。UnityEvent 只用於場景操作與通知，例如按鈕呼叫 `Pause()` 或補間完成後通知場景流程。
- **計時與播放**。`Timer.ISource` 只提供 scaled／unscaled 時間。runner 依時間推進各 handle 的播放位置，處理反播、暫停、取樣，以及後端支援的連續循環。暫停保留當前位置，反播沿同一條時間軸倒退。循環的週期推進、往返方向、累加端點及跨週期剩餘時間由 runner 處理。
- **基本 Tween** 隨 Motion 交付。只安裝 Motion 的 Unity 專案也能直接播放，以 `TweenFade` 為首輪實例。Unity 接入層用 `Timer.ISource` 提供的時間驅動既有 Core `ReferenceRunner`，首輪維持單一數值、單週期，包含暫停與反播。
- **Motion Tween Adapter** 沿用 DOTween 執行 KFC 的往返、累加、固定更新等進階行為。DOTween 的設定、循環與資源回收由後端管理，轉接層將行為映射到共用契約。需要隔離引擎依賴時，在 Motion 內使用獨立 asmdef。首輪供應商為內建 runner 與 DOTween，PrimeTween 留待出現實際採用需求後評估。
- **Animation** 擁有 KFC 的 Sprite 逐格播放元件及影格選擇。需要補間時，Animation 共用 Motion 的數值推進。遷移先依 [Sprite 逐格播放遷移](#sprite-逐格播放遷移) 評估既有播放方式。
- **舊入口** 過渡期間保留 `Yu5h1Lib.TweenBehaviour` 舊型別與 `Kill()`，讓 KFC 的型別查找繼續找到已切換的元件。Motion 的基底使用不同的完整型別名，舊入口委派給它。原具體元件及仍有引用的 generic 入口保留 `.meta` GUID、序列化欄位與 UnityEvent 方法簽章，Editor 工具也跟著驗證。
- **素材相容** 同時查驗 GUID、原 namespace／assembly、Prefab overrides、序列化欄位、UnityEvent 目標及 C# 呼叫。KFC 資產已出現舊 `DOTweenBehaviour` 組件名與舊 generic 型別名稱。保留 GUID 後，仍要觸發事件及執行卸載清理，證明原本的接線有效。以原版實測分辨現存失效與遷移回歸。
- **生效語意** 先核對現行 `OnDisable` 的 `Rewind`／`DOTween.Pause(component)`、延遲及重入，以及 `UseUnscaledTime`、`updateType`、`isIndependentUpdate` 的實際優先順序。Motion 管理自己建立的 handle，舊入口的目標群組與整體清理由後端轉接層映射，並核對直接使用 DOTween 的 tween 是否受影響。

## Timer 與 Repeater 的遷移前調查

先盤點 Timer 的消費端，再把使用 repeat 或 loop 的呼叫端依原有接線逐個改用 `Repeater`。這項工作保留在 Timer 的重構路線。Tween 首輪透過 `Timer.ISource` 取時間，基本 Tween 的單週期播放可單獨驗證。 [Timer 整合原案](../.agents/plans/AtomicComponents.md) · [Timer 實作](../../Core/Runtime/Source/Timer.cs)

- **Inspector 接線**。Timer 完成後，`_Completed` 的 UnityEvent 呼叫所指定的 `RepeaterObject.Resolve()`。`RepeaterObject` 包裝 Core 的 `Repeater`，負責有限或無限次數。`_resolving` 每成功計次就觸發，可接下一次計時。`_resolved` 在最後一次成功計次時緊接著觸發，表示可用的重啟次數已用完。最後一輪真正跑完仍由 Timer 的完成事件判斷。現行 `RepeaterObject` 提供這兩個事件，舊 Timer 另有 `_FinalRepetition`。 [Repeater](../../../DotNet/Source/Resolvers/Repeater.cs) · [事件順序](../../../DotNet/Source/Resolvers/Resolver.cs) · [UnityEvent 橋接](../Packages/common/Runtime/Resolver/ResolverObject.cs)
- **遷移驗收**。核對 `RepeatCount = 0`、`2`、`-1`、最後一次重複與整段完成的事件順序，以及再次啟動時的重置。舊 `RepeatCount = 2` 代表首次計時加兩次重啟，最後一次重啟前舊 Timer 先送出 `_FinalRepetition`，接著送出 `_Repeated`。序列開始時重置計數，獨立序列各保有自己的計數狀態。驗證最後一輪確實跑完，並核對原有 `_FinalRepetition`、`_Repeated`、`_Completed` 的消費端。

2026-11-16 以工作區的 Unity 原始碼及 `W:/UnityProject` 下 12 個有 UPM manifest 的專案做靜態盤點，比對 C# 使用、腳本 GUID、序列化 `RepeatCount` 和 prefab override。以下是有明確重複設定的遷移案例。

| 消費端 | 盤點結果 |
| --- | --- |
| MotionGen `Assets/Test/test.unity` | Timer 設定 `RepeatCount = 2`，有 `_Repeated`、`_FinalRepetition`、`_Completed` 三組事件，先用來核對有限重複及事件順序。 |
| KFC `Assets/Scenes/A005.unity` | `TimerEventHandler` 裡的 Timer 每 5 秒無限重複，`_Repeated` 呼叫 `Caster.Cast()`。 |
| Virtual-Chat-Partner | `vcp_AudioClient.unity` 有每 15 秒觸發 `TryPlay` 的無限 Timer。`testWebGL.unity` 內嵌無限重複的 `ThinkingTimer`，`SequentialChatRequestData` 也在 C# 設定它每秒重複，使用 `repeatCounter` 更新等待提示。驗證 Inspector 路徑及程式呼叫路徑。 |
| BonghuoVR | 找到 12 筆 Timer 元件的直接 YAML 引用，其中 11 筆具有完整設定，`RepeatCount` 都是 0。有單次完成事件與讀取 `_Completed` 的 Editor 工具，供相容性回歸。 |

共用程式的消費端另有 `RouteTracer`、`FadeSpriteRenderer`、`TimerEventHandler`、`UI_TextPerformance`、`UI_DialogBase`。KFC 的程式呼叫包括 `FX_SpriteRendererReceiver`、`LineDrawerEnhance`、`KFC_CameraController` 和 `RouteTracerEditor`。Virtual-Chat-Partner 另有眨眼、表情和請求流程，BonghuoVR 另有捕撈及其 Editor 接線工具。實作前用此盤點覆核載入後的 Prefab 實例、執行時配置與直接程式呼叫。

**Yu5h1Lib 的使用方式**。場景與 prefab 用 ScriptableObject 設定及 UnityEvent 組合開始、完成等事件，調整接線由 Inspector 完成。執行期密集呼叫可從 C# 直接用 `Repeater.TryResolve()` 靜默計次，Tween 每幀取樣則留在 runner。兩種入口共用各自的核心邏輯。效能以同時運作數、每幀實際回呼量、CPU 時間與 GC 配置量評估。 [UnityEvent Inspector 接線](https://docs.unity3d.com/ja/current/Manual/unity-events.html)

**物件回收**。`ScrollView` 已透過 `Recycler<RectTransform>` 借還可見項目。Motion 若量出自有物件的配置問題，可評估共用 `Recycler` 並核對歸還時的狀態重置及舊 handle 失效。DOTween 回收已終止且啟用回收的 tween，PrimeTween 也重用底層物件並以 handle 的 ID 辨識有效性。各後端維護所擁有的資源與循環，所以上層場景仍可採資料驅動接線。 [ScrollView](../Packages/UI/Runtime/Module/Viewing/ScrollView.cs) · [DOTween 回收](https://dotween.demigiant.com/support.php) · [PrimeTween 底層重用](https://github.com/KyryloKuzyk/PrimeTween/discussions/201)

## Sprite 逐格播放遷移

貼圖 driver 已從 Animation 移到 `common`，由 `MaterialController` 每幀驅動。`TextureSequenceDriver` 逐格更換整張材質貼圖，BonghuoVR 的捕魚測試場景有五張貼圖、12 FPS 的案例。`TextureSheetDriver` 逐格更新材質 UV，`Sulfuric Fire Line` 有 5×5、25 FPS 的案例。兩個 driver 共用 `FrameStepResolver` 計算影格。KFC 的 `DOSpriteAnimation` 直接指定 `Image.sprite` 或 `SpriteRenderer.sprite`。 [材質整合紀錄](../.agents/plans/Component_Convergence.md) · [現行 Sprite 元件](../Packages/Plugins/DOTween/Runtime/Component/DOSpriteAnimation.cs)

遷移前用同一組 KFC 素材比較兩種做法。

- **`AnimationClip`**。沿用 Unity 的 Sprite 物件曲線。KFC 的 `Boss_jump.anim` 已使用 `m_Sprite` 曲線。以載入圖示確認相同方式能驅動 `Image.sprite` 並在暫停時循環，以齒輪按鈕確認 `SetSprite(int)` 的既有 UnityEvent 仍能指定影格。
- **共用 `FrameStepResolver`**。由 Animation 播放元件使用共用影格計算，再寫入 Sprite。現行 resolver 用全域 `Time.time` 計算固定 FPS 循環。評估共用時要驗證能以各元件的非縮放經過時間保留播放起點與啟停行為，並提供 `SetSprite(int)` 讓事件直接指定影格。 [FrameStepResolver](../Packages/common/Runtime/Resolver/FrameStepResolver.cs)

### KFC 消費端與驗收條件

2026-11-17 的腳本 GUID 盤點找到 11 筆 `DOSpriteAnimation` 直接引用，全在 KFC，包含備份場景。下表的輪長、自動播放與時鐘設定來自 Unity 隔離副本讀回。播放與事件效果以完整 KFC 專案的原版 Play Mode 作為比對基準。

| 使用點 | 原有設定與接線 | 遷移驗收條件 |
| --- | --- | --- |
| `Assets/Scenes/Start.unity` 的 `Artwork/底圖/流星` | `SpriteRenderer`，6 張 Sprite，啟用後每 2 秒循環。 | 啟用後自動播放，6 張影格依原順序每 2 秒循環。暫停及恢復時核對縮放時間的行為。 |
| `Assets/Scenes/A001.unity` 的 `OldMan` | 7 張 Sprite，啟用後每秒循環。 | 以原 `OldMan.prefab` 的場景實例核對繼承與覆寫。確認啟用後自動播放，7 張影格依原順序每秒循環。 |
| `Assets/Scenes/A001.unity` 的 `繪製提示1_結果` | `SpriteRenderer`，10 張 Sprite，啟用後每 3 秒循環。 | 讓原場景流程顯示與隱藏提示。確認顯示時 10 張影格依原順序每 3 秒循環，重新顯示時與原版的起始影格及啟停行為一致。 |
| `Assets/Resources/UI/Loading.prefab` 的 `Loading/Loading.../Image` | `Image`，6 張 Sprite，每秒循環。`UI_Manager` 從 Resources 載入，使用非縮放時間。 | 走原載入流程，確認在 `Time.timeScale = 0` 時，6 張影格仍依原順序每秒循環。停用後停止更新，重新啟用時依原版的起始影格播放。 |
| `Assets/StaticResources/Gear/gearbutton.prefab` | 兩張 Sprite，啟用時不自動播放，由 UnityEvent 呼叫 `SetSprite(0/1)`。 | 保留 `SetSprite(int)` 的方法簽章、目標與原參數。觸發兩個原事件，確認立即顯示對應影格，並維持到下一次指定。 |
| `Assets/StaticResources/Gear/gearButtonDoor.prefab` 的 `button` | 兩張 Sprite，啟用時不自動播放，保留 `SetSprite(0/1)` 事件，在 `A001`、`A002` 都有實例。 | 在 `A001` 與 `A002` 各觸發原按鈕流程，確認 `SetSprite(0/1)` 仍命中原目標且顯示對應影格。核對 Prefab 實例覆寫與同一流程中的其他 UnityEvent。 |

`A001 backup.unity` 與 `A001 00.unity` 也有直接引用。若仍有使用，沿用對應使用點的驗收條件。

## 對外命名

UPM 交付名稱為 **Yu5h1Lib.Motion**，ID 為 `com.yu5h1.motion`。**Motion Tween Adapter** 是套件內部模組名，需要獨立組件時使用 `Yu5h1Lib.Motion.TweenAdapter`。第三方依賴及後端實作說明以 `DOTween` 指明實際引擎。

對外元件統一採 **`Tween` + 動作**，與 `TweenBehaviour`、`TweenFloat` 和 `TweenColorRenderer` 一致。舊 `DO` 名稱供遷移相容用途。

| 現有元件 | 遷移後名稱 |
| --- | --- |
| `DOFade`／`DOColor`／`DOScale` | `TweenFade`／`TweenColor`／`TweenScale` |
| `DOMove2D`／`DORotate2D` | `TweenMove2D`／`TweenRotate2D` |
| `DOAudioVolume`／`DOTimeScale` | `TweenAudioVolume`／`TweenTimeScale` |

`DOCounter` 若仍有繼承端，對應 `TweenCounter`。`DOTransform` 依[原重構計畫 §8.2](../.agents/plans/Motion_System_Refactor.md)評估折疊。先盤點外部 C# 繼承與序列化引用，再確認兩個中介型別的去向。

## 遷移與驗收

1. **固定基準。** 記錄兩個專案、套件、Core DLL 與 DOTween 版本。以原版 KFC 記錄 UI 淡入、載入動畫、移動平台、傳送門及 `A001` 的事件與卸載行為。至少包含 `timeScale = 0`、停用後重啟、延遲中卸載與場景重載。用同一份素材對照改版。
2. **讓 Unity 跑通首輪契約。** 接入已驗收的 Core Tween，核對 DLL 實際載入路徑與公開入口。在只裝 Motion 與其正常相依的 Unity 專案，驗證 `TweenFade` 可直接從起始透明度播到終值，中途暫停後從原位置續播，反播沿原時間軸回到起點。另核對停用與重新啟用的行為。在 `Yu5h1LibTest` 以真實 DOTween runner 跑 14 個共享案例、123 步，核對值、狀態、事件順序與資源回收。由 Motion 核對 Core 及 DOTween 的證據。
3. **先換 KFC 的一個純量路徑。** 以 `StartSceneMenu` 中單週期 `DOFade` 為第一個代表案例，從舊元件入口切到 Motion 基底與 DOTween runner。沿用原 prefab 和欄位，比對自動播放、停用倒回與暫停時的表現。以 `A001` 核對已切換元件仍能被原型別查詢、觸發 UnityEvent，並在卸載時停止。可用原套件版本與對應的 lockfile 回復基準。
4. **依實際能力擴大。** `movingPlatform.prefab` 的 `DOMove2D` 有 `Vector3`、`LoopCount = -1` 和固定更新，另須驗證本地端點轉世界座標及完成／倒回傳遞的速度。`Teleport Gate.prefab` 的 `DORotate2D` 有無限累加旋轉，需核對角度累加。先為各能力補足重播與素材基準，再切換對應元件。`DOColor`、`DOScale` 隨型別能力驗證後接續。Animation 依 [Sprite 逐格播放遷移](#sprite-逐格播放遷移) 比較既有播放方式並驗證 KFC 接線。
5. **分開驗證換殼、改名與套件合併。** KFC 的第一個換殼案例通過後，用同一組 prefab／場景驗證元件改名。保留腳本 `.meta` GUID，核對 Inspector、Prefab overrides、`TryPlayTween` 的 UnityEvent 及卸載用的型別查找。再驗證 Motion 交付的補間元件、Inspector 和轉接模組，以及 Animation 交付的逐格播放元件。檢查兩個消費端的 manifest、lockfile 和 asmdef 引用。同一專案每次只載入一份提供舊腳本 GUID 的套件，切換版本時同步更換套件來源及相依。
6. **驗證共用發版並退役舊擴充套件。** KFC 檢查 Build Settings 啟用的 `Start`、`A001`、`A002`、`A005`，再抽查 `A003`、`A004` 及具代表性的 prefabs。要求無新增 Missing Script、遺失欄位／覆寫、失效 UnityEvent 或場景卸載後殘留的 tween。BonghuoVR 需在其現用 Unity 版成功解析依賴，並通過 `Main`／XR UI 淡入與顏色的回歸。若 Motion 相依在 `6000.3` 無法通過，BonghuoVR 維持原可用版本，KFC 試遷移繼續使用隔離來源。確認消費端已切到 Motion 且無存續的舊套件依賴後，再退役 `com.yu5h1.dotweenextension`。KFC 的進階案例繼續透過 Motion 使用 DOTween。
7. **依 KFC 驗證決定 DOTween 引擎的去留。** 內建 runner 通過相同的往返、累加旋轉、固定／非縮放更新及事件回歸後，再盤點 KFC 直接呼叫 DOTween 的程式是否已遷移，並評估移除第三方引擎。舊擴充套件的退役依第 6 步驗證。

首輪實作預估中型、系統層級影響。共用 MonoBehaviour、runner 和 Editor／資產接線在過渡期跨越 Motion 與舊套件。Core 參考 runner 提供既有基準，真實 DOTween、Unity Editor 載入及場景 Play Mode 提供消費端證據。完整元件遷移按第 4 步拆批估量。
