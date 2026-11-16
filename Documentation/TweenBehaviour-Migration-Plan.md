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

- **Motion** 擁有引擎無關的 Unity `TweenBehaviour` 基底、淡入與位移等補間元件及共用 Inspector。共用層保留元件初始化、啟停、反向、倒回、等待、事件派送與釋放等流程，使用 Core 的 runner／handle 契約。目標物件的取值與寫入由各元件提供。
- **基本 Tween** 隨 Motion 交付。只安裝 Motion 的 Unity 專案也能直接播放單一數值、單週期的補間，以 `TweenFade` 為首輪實例。由 Unity 時鐘驅動既有 Core 參考 runner。KFC 需要循環和其他進階能力的案例在過渡期使用 DOTween 後端。
- **Motion Tween Adapter** 負責 runner、更新時鐘、引擎設定和資源回收。DOTween 的 `EaseType`、`LoopType`、`UpdateType` 與預設值由此處解讀。需要隔離引擎依賴時，在 Motion 內使用獨立 asmdef。
- **Animation** 擁有 KFC 的 Sprite 逐格播放元件及影格選擇。需要補間時，Animation 共用 Motion 的數值推進。遷移先依下節評估既有播放方式。
- **舊入口** 過渡期間保留 `Yu5h1Lib.TweenBehaviour` 舊型別與 `Kill()`，讓 KFC 的型別查找繼續找到已切換的元件。Motion 的基底使用不同的完整型別名，舊入口委派給它。原具體元件及仍有引用的 generic 入口保留 `.meta` GUID、序列化欄位與 UnityEvent 方法簽章，Editor 工具也跟著驗證。
- **素材相容** 同時查驗 GUID、原 namespace／assembly、Prefab overrides、序列化欄位、UnityEvent 目標及 C# 呼叫。KFC 資產已出現舊 `DOTweenBehaviour` 組件名與舊 generic 型別名稱。保留 GUID 後，仍要觸發事件及執行卸載清理，證明原本的接線有效。以原版實測分辨現存失效與遷移回歸。
- **生效語意** 先核對現行 `OnDisable` 的 `Rewind`／`DOTween.Pause(component)`、延遲及重入，以及 `UseUnscaledTime`、`updateType`、`isIndependentUpdate` 的實際優先順序。Motion 管理自己建立的 handle，舊入口的目標群組與整體清理由後端轉接層映射，並核對直接使用 DOTween 的 tween 是否受影響。

## Sprite 逐格播放遷移

貼圖 driver 已從 Animation 移到 `common`，由 `MaterialController` 每幀驅動。`TextureSequenceDriver` 逐格更換整張材質貼圖，BonghuoVR 的捕魚測試場景有五張貼圖、12 FPS 的案例。`TextureSheetDriver` 逐格更新材質 UV，`Sulfuric Fire Line` 有 5×5、25 FPS 的案例。兩個 driver 共用 `FrameStepResolver` 計算影格。KFC 的 `DOSpriteAnimation` 直接指定 `Image.sprite` 或 `SpriteRenderer.sprite`。 [材質整合紀錄](../.agents/plans/Component_Convergence.md) · [現行 Sprite 元件](../Packages/Plugins/DOTween/Runtime/Component/DOSpriteAnimation.cs)

遷移前用同一組 KFC 素材比較兩種做法。

- **`AnimationClip`**。沿用 Unity 的 Sprite 物件曲線。KFC 的 `Boss_jump.anim` 已使用 `m_Sprite` 曲線。以載入圖示確認相同方式能驅動 `Image.sprite` 並在暫停時循環，以齒輪按鈕確認 `SetSprite(int)` 的既有 UnityEvent 仍能指定影格。
- **共用 `FrameStepResolver`**。由 Animation 播放元件使用共用影格計算，再寫入 Sprite。現行 resolver 用全域 `Time.time` 計算固定 FPS 循環。評估共用時要驗證能以各元件的非縮放經過時間保留播放起點與啟停行為，並提供 `SetSprite(int)` 讓事件直接指定影格。 [FrameStepResolver](../Packages/common/Runtime/Resolver/FrameStepResolver.cs)

相容基準以 KFC 素材接線為準。`Loading.prefab` 配置六張 Sprite、一秒一輪、無限循環；在 `Time.timeScale = 0` 時仍須按原順序播放，停用及重新啟用的行為比對原版。`gearbutton.prefab`、`gearButtonDoor.prefab` 的 UnityEvent 呼叫 `SetSprite(0/1)` 後須顯示指定影格。素材接線已靜態核對，播放行為留待 Play Mode 驗證。

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
2. **讓 Unity 跑通首輪契約。** 接入已驗收的 Core Tween，核對 DLL 實際載入路徑與公開入口。在只裝 Motion 與其正常相依的 Unity 專案，驗證 `TweenFade` 可直接從起始透明度播到終值，並核對停用與重新啟用的行為。在 `Yu5h1LibTest` 以真實 DOTween runner 跑 14 個共享案例、123 步，核對值、狀態、事件順序與資源回收。由 Motion 核對 Core 及 DOTween 的證據。
3. **先換 KFC 的一個純量路徑。** 以 `StartSceneMenu` 中單週期 `DOFade` 為第一個代表案例，從舊元件入口切到 Motion 基底與 DOTween runner。沿用原 prefab 和欄位，比對自動播放、停用倒回與暫停時的表現。以 `A001` 核對已切換元件仍能被原型別查詢、觸發 UnityEvent，並在卸載時停止。可用原套件版本與對應的 lockfile 回復基準。
4. **依實際能力擴大。** `movingPlatform.prefab` 的 `DOMove2D` 有 `Vector3`、`LoopCount = -1` 和固定更新，另須驗證本地端點轉世界座標及完成／倒回傳遞的速度。`Teleport Gate.prefab` 的 `DORotate2D` 有無限累加旋轉，需核對角度累加。先為各能力補足重播與素材基準，再切換對應元件。`DOColor`、`DOScale` 隨型別能力驗證後接續。Animation 依 [Sprite 逐格播放遷移](#sprite-逐格播放遷移) 比較既有播放方式並驗證 KFC 接線。
5. **分開驗證換殼、改名與套件合併。** KFC 的第一個換殼案例通過後，用同一組 prefab／場景驗證元件改名。保留腳本 `.meta` GUID，核對 Inspector、Prefab overrides、`TryPlayTween` 的 UnityEvent 及卸載用的型別查找。再驗證 Motion 交付的補間元件、Inspector 和轉接模組，以及 Animation 交付的逐格播放元件。檢查兩個消費端的 manifest、lockfile 和 asmdef 引用。同一專案每次只載入一份提供舊腳本 GUID 的套件，切換版本時同步更換套件來源及相依。
6. **驗證共用發版並退役舊套件。** KFC 檢查 Build Settings 啟用的 `Start`、`A001`、`A002`、`A005`，再抽查 `A003`、`A004` 及具代表性的 prefabs。要求無新增 Missing Script、遺失欄位／覆寫、失效 UnityEvent 或場景卸載後殘留的 tween。BonghuoVR 需在其現用 Unity 版成功解析依賴，並通過 `Main`／XR UI 淡入與顏色的回歸。若 Motion 相依在 `6000.3` 無法通過，BonghuoVR 維持原可用版本，KFC 試遷移繼續使用隔離來源。確認消費端已切到 Motion 且無存續的舊套件依賴後，再退役 `com.yu5h1.dotweenextension`。

首輪實作預估中型、系統層級影響。共用 MonoBehaviour、runner 和 Editor／資產接線在過渡期跨越 Motion 與舊套件。Core 參考 runner 提供既有基準，真實 DOTween、Unity Editor 載入及場景 Play Mode 提供消費端證據。完整元件遷移按第 4 步拆批估量，PrimeTween 另輪處理。
