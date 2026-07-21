# Motion System Refactor

> Audience: Claude（跨 session 持續記憶）。使用者是驗證者。本檔是單一真相。
> 取代並合併 `Tween_Refactor.md` + 舊 `Motion System Refactor Plan.md`（作廢）。
> **狀態：📐 架構設計中。2026-06-09 大幅收斂 → 殼優先 + `Motion` 巢狀架構。尚未寫 code。**

---

## 0. 一句話（2026-06-09 改版）

**殼優先。`Yu5h1Lib.Animation.Motion` 是統一靜態 facade + 注入點:巢狀 `IHandle`/`ISettings`/`IRunner` + 靜態 `Interpolate`。第三方引擎(DOTween / PrimeTween / 自製)退化成可抽換的 `IRunner`。組件只認殼,換引擎零改動。**

> ⚠️ 與舊版差異:舊版「Phase 1 = Interpolation 數學層先做」。**現已推翻** —— Interpolation 只服務「無引擎兜底 runner」,是配角。第一刀是**行為殼**(用已有的 DOTween 驗證),不是數學。

動機不變:tween 出現多版本(DOTween 老、PrimeTween 新且 alloc-free),舊專案綁死 `DOTween.To(...)` 在 core logic。使用者多專案實際只用 DOTween + 自家 `TweenBehaviour`;未來要上 PrimeTween,**但「為 PrimeTween 再做一套組件」不合理** → 故需殼。

---

## 1. 命名與架構風格 ★（使用者偏好,通用）

**使用者偏好:單一架構容器類 + 巢狀型別 + 短名。不要一堆平行同字首的扁平名**(`IMotionHandle` / `MotionSettings` / `MotionRunner` / `MotionBehaviour` 這種 —— 顯得不像一個架構,只是一堆並列檔)。

```csharp
namespace Yu5h1Lib.Animation
{
    public static class Motion          // 架構容器 + 統一靜態 facade
    {
        public interface IHandle { ... }
        public interface IHandle<T> : IHandle { ... }
        public interface ISettings { ... }
        public interface IRunner { ... }
        public enum LoopMode { Restart, Yoyo, Incremental }

        public static IRunner Default { get; set; }              // 注入點,bootstrap 設 DOTween runner
        public static T Interpolate<T>(T from, T to, float t);   // 統一靜態通用方法
        // 其他通用靜態方法
    }
}
```

→ 引用時:`Motion.IHandle`、`Motion.ISettings`、`Motion.Interpolate(...)`。讀起來是「一個 Motion 架構」,不是散落的 `Motion*` 檔。

---

## 2. 殼定案 —— `Motion.IHandle`（只有動詞 + 狀態 + 事件）

```csharp
public interface IHandle
{
    bool  IsAlive       { get; }   // Stop 後 false（回收語意,見 §5）
    bool  IsPlaying     { get; }
    float NormalizedTime{ get; }   // 0..1,含 loop 進度

    event System.Action Played, Completed, StepCompleted, Rewound;

    void Play();           // forward
    void PlayBackwards();  // PrimeTween 自己組:current→From 起新 tween
    void Pause();
    void Stop();           // 結束 + 標記可回收
    void Rewind();         // 立即 snap 回 From（不播反向）
}

public interface IHandle<T> : IHandle
{
    T From { get; set; }   // 對應現有 _startValue / ChangeStartValue
    T To   { get; set; }   // 對應現有 _endValue
}
```

- **五動詞夠**(使用者確認):`Play / PlayBackwards / Pause / Stop / Rewind`。
- 事件**過去式無 on 前綴**(`Played/Completed/StepCompleted/Rewound`),`OnXxx` 留給 behaviour 內部 handler(memory: feedback_serialized_event_naming）。
- 「正在播就不重播」這類守衛(現有 `TryPlayTween`)是 **behaviour 層**邏輯,不入殼。

---

## 3. `Motion.ISettings` + 管道分離

```csharp
public interface ISettings        // 使用者選 interface（非 struct）
{
    float    Duration { get; }
    float    Delay    { get; }
    int      LoopCount{ get; }     // 0 = 不循環
    LoopMode LoopMode { get; }
    int      Ease     { get; }     // 佔位;easing 形態待定（見 §11）
    bool     UseUnscaledTime { get; }
}
```

- **interface 而非 struct 的好處**:behaviour 可直接 `implements ISettings`,用序列化欄位當屬性,`Create(this, ...)` 不複製。貼合「用 behaviour 去想」。
- **張力**:純資料 struct 對序列化 / 未來 ECS 友善,interface 不行。ECS 是 Q-J 之後 scope,需要時再加 struct 實作。
- **from/to 不放 settings**:settings 要非泛型,from/to 是 `T`。現有 `TweenBehaviour` 也是 timing 一組 / `_startValue,_endValue` 另一組。維持分開。
- **管道(getter/apply)不放 settings**:它是 delegate、per-instance。

---

## 4. `Motion.IRunner` + 注入 + lazy-init

```csharp
public interface IRunner
{
    IHandle<T> Create<T>(ISettings settings, T from, T to,
                         System.Func<T> getter, System.Action<T> apply);
    void PauseByTarget(object target);  // DOTween.Pause(target) / PrimeTween.PauseAll(onTarget)
    void StopByTarget(object target);
}
// Motion.Default : IRunner —— bootstrap 由 DOTween package [RuntimeInitializeOnLoadMethod] 設定
```

- 使用者原想的 `IHandle.Init(setting)` → **改成 `runner.Create(settings, from, to, getter, apply)`**。「執行前確認所有設定」= 帶齊 config 去建立 handle 那一刻。避免半初始化狀態。
- **lazy**:behaviour 首次 Play 才 `Create`(見 §5 `EnsureHandle`)。
- `settings` 帶資料、`getter/apply` 帶管道、`from/to` 帶端點,一次到位。

---

## 5. 鎖定的決策（locked, 2026-06-09）

1. **delegate,不用 interface 做型別 lerp**:管道 `Func<T> getter` / `Action<T> apply`;型別內插也是 `Func<T,T,float,T>`(註冊進 registry,`Motion.Interpolate<T>` dispatch)。無 `IInterpolator<T>`。
2. **Stop = 結束 + 標記可回收**;「停了還要重播」用 Pause。
3. **可運行時改的只有 `From/To`**。timing(duration/ease/loop)= **create-time 固化**。要改 timing → **重建 handle**。
   - ★ **「重建」= runtime 從池拿一個新 handle(回收再用),用當前 settings 重新 `Create`。不是重新設計程式碼,也不是改跑到一半的物件。**
4. **AutoKill 下沉**:DOTweenRunner 內部 `SetAutoKill(false)` 保住 tweener 重播,殼與 behaviour 都看不到。其他引擎專屬配套(池策略…)同理藏在各自 runner。
5. **Interpolate / 型別 lerp 只服務「無引擎 runner」**(自製 / Timer 兜底)。DOTween / PrimeTween 內部自算,完全不碰 registry。
6. **事件命名**:`Played/Completed/StepCompleted/Rewound`。

---

## 6. 兩個注入層級（別混為一談）

| 注入點 | 注入什麼 | 誰用到 |
|--------|---------|--------|
| **Provider 注入（主角）** | DOTween / PrimeTween / 自製引擎 整個 `IRunner` | 11 個行為組件 |
| **Interpolator 注入（配角）** | 「`T` 怎麼 lerp」`Func<T,T,float,T>` | **只有無引擎 runner**(經 `Motion.Interpolate`) |

統一組件、讓 DOTween↔PrimeTween 可抽換的是 **Runner + Handle**,不是 interpolator。換引擎 = 換 `Motion.Default`,組件零改動。

---

## 7. Provider 模型差異（DOTween vs PrimeTween,殼設計依據）

**根因**:現有 `TweenBehaviour` 不只「用 DOTween」,是**架構綁死 DOTween 的「持久、可重播、可即時改的 tweener 物件」模型**(create-once + `SetAutoKill(false)` + 對同物件反覆 `PlayForward/PlayBackwards` + 即時改 `startValue/endValue`)。PrimeTween 相反:struct、一次性、fire-and-forget、alloc-free。

**解法**:讓「持久的東西」= **你的 `IHandle`**(殼),引擎 tween 降為 handle 內部「用完可丟、可重建」的暫時資源。各引擎池子藏在各自 runner,不穿過殼。

| 操作 | DOTweenRunner | PrimeTweenRunner | 自製/Timer runner |
|------|---------------|------------------|-------------------|
| handle 持有 | 持久 Tweener(天生吻合) | spec + 當前 Tween struct | spec + 計時來源 |
| Play | `tweener.PlayForward()` | 開新 `Tween.Custom(current→To)` | 啟動,每 tick 算值 |
| PlayBackwards | `tweener.PlayBackwards()` | 開新 `current→From`,完成 fire Rewound | 反向遞減 t |
| Pause/Resume | `Pause()` / `Play()` | `tween.IsPaused = true/false` | 暫停 |
| Stop | 停 + 還池(`AutoKill(false)` 內部管) | `tween.Stop()` + 還池 | 停 + 還池 |
| Rewind | `tweener.Rewind()` | 直接 set value 回 From | snap t=0 寫回 |
| ByTarget | `DOTween.Pause/Kill(target)` | `Tween.PauseAll/StopAll(onTarget)` | 自建 target→handles 表 |
| 回收 | DOTween 內建池 | PrimeTween 內建池(struct id+version) | 自製池(未實作) |

`IsAlive` 語意以 PrimeTween 為準(completed 後 false);DOTweenRunner 自 wrap。

---

## 8. 既有現況（要遷移的東西）

**Source**:`Packages/Plugins/DOTween/Runtime/Component/`,asmdef `Yu5h1Lib.DOTweenAddon`(rootNamespace `Yu5h1Lib`)。

`TweenBehaviour.cs` 三層繼承:
- `TweenBehaviour : BaseMonoBehaviour`(abstract;持 `Tweener tweener`、`abstract Tweener Create()`、`normalizedTime => tweener.ElapsedPercentage()`)
- `TweenBehaviour<TComponent, TValue1, TValue2, TPlugOptions>`(核心,DOTween 型別大量出現;`TweenerCore`、`startValue/endValue` 寫穿 live tween、`_PlayEvent/OnCompleteEvent/OnRewindEvent`、`OnInitializing` 設定 + `SetAutoKill(false)`、`OnEnable→PlayForward`、`OnDisable→Rewind + DOTween.Pause(component)`、`TryPlayTween`/coroutine + `IsWaiting`)
- `TweenBehaviour<TComponent, TValue, TPlugOptions>`(折疊 same-type)

**11 個 concrete**:DOFade、DOMove2D、DORotate2D、DOScale、DOColor、DOAudioVolume、DOCounter、DOSpriteAnimation、DOTimeScale、TweenColorRenderer、TweenFloat、TweenLoadAsync(+中介 `DOTransform`)。

### 8.1 DOTween 滲透點（殼要抽象掉）
- Base:`Tweener`、`TweenerCore<T1,T2,TPlugOptions>`、`Ease`/`LoopType`/`UpdateType` 欄位、`where TPlugOptions:struct,IPlugOptions`
- 建立:`DOTween.To(get,set,end,d)`、`g.DOFade`/`transform.DOMove` 捷徑、`.SetTarget`
- 控制:`PlayForward/PlayBackwards/Rewind/Pause/Kill/SetDelay`
- 設定:`SetEase/SetLoops/SetUpdate/SetAutoKill/ChangeStartValue`
- 狀態:`IsPlaying/IsComplete/ElapsedPercentage`、`startValue/endValue/changeValue`
- 回調:`onPlay/onComplete/onStepComplete/onRewind`(可變事件)
- Static:`DOTween.KillAll/Clear`(`Application.wantsToQuit`)、`DOTween.Pause(component)`(`OnDisable`)

### 8.2 要保留的 Yu5h1Lib 自家邏輯（搬進 behaviour 層,改認殼）
- 欄位:Delay/Duration/LoopCount/LoopType/playOnEnable/RewindOnDisable/UseUnscaledTime/isIndependentUpdate/IsWaiting/isBackwards
- 事件:Play/Complete/Rewind 三個 UnityEvent(去 on 前綴)
- Lifecycle:OnInitializing 設參數 + 預設 Pause;OnEnable playOnEnable→PlayForward;OnDisable RewindOnDisable→Rewind;OnDestroy Kill
- `OverrideGetComponent()` 鉤子(DOFade 在 CanvasGroup/Image/SpriteRenderer/MeshRenderer 間挑;MaterialPropertyBlock 處理)
- `ChangeStartValue` 旗標 + `_startValue/_endValue` + `ContextMenuItem("Reset")`
- `TryPlayTween()`/coroutine 等待 + `IsWaiting` 守衛
- ContextMenu:Rewind / PlayForward / PlayBackwards

→ 全搬進 `MotionBehaviour`(behaviour 層),改用 `Motion.IHandle` 而非 `tweener`。中介 `DOTransform` 壓平。`TPlugOptions`/`TValue1,TValue2` 折疊(11 個 concrete 全 same-type)。

---

## 9. 遷移階段（2026-06-09 改版,殼優先）

| Phase | 工作 | 依賴 | 風險 |
|-------|------|------|------|
| **A（先做,本次唯一目標）** | **抽象架構 `Motion`**:`Yu5h1Lib.Animation` 純介面 `IHandle/IHandle<T>/ISettings/IRunner` + `LoopMode` + `Motion.Default` + `Motion.Interpolate` 佔位。**只定契約,不接引擎** | 無(Core/DotNet) | 介面遺漏 → 用 §7/§8 對照表檢查 |
| B | **DOTweenRunner**(DOTween package):把現有 `TweenBehaviour` 的 DOTween 用法包成 `IRunner`/`IHandle<T>`,內部管 AutoKill+池 | DOTween(已有) | enum↔Ease 對應、live 端點 |
| C | **`MotionBehaviour`** base(animation package)+ 逐一遷移 11 concrete,每個跑 scene 驗 forward/backward/rewind/loop/event | A,B | scene reference 破壞 |
| D（之後）| **自製 tween 引擎 / Timer runner**(可能完全自製一套)+ `Motion.Interpolate` registry(型別 lerp delegate 註冊) | Timer 重構 | Timer 現況未定 |
| E（之後）| **PrimeTweenRunner** | 裝 PrimeTween | PrimeTween API 細節 |
| F（之後）| WPF / WinForms runner | 各平台 | — |

**A 階段不碰**:Core 數學、interpolation registry、Timer、PrimeTween、behaviour。純抽象殼。

---

## 10. 放置

| 物件 | 位置 | namespace |
|------|------|-----------|
| 抽象架構 `Motion`(IHandle/ISettings/IRunner/Interpolate) | **Core** `DotNet/Source/...`(待定子資料夾) | `Yu5h1Lib.Animation` |
| `DOTweenRunner` | `Packages/Plugins/DOTween/Runtime` | `Yu5h1Lib`(DOTweenAddon) |
| `MotionBehaviour` + 11 concrete | **animation package**(未到,先完善架構) | `Yu5h1Lib` |

---

## 11. 待確認 / 之後

- **easing 形態**:`ISettings.Ease` 現用 `int` 佔位。之後決定 `IEasing` 介面 / `enum Ease` / AnimationCurve adapter(原 Q-B,傾向「Core 定 easing 數學 + Unity 加 enum/curve adapter」,但與「Core 不做數學」有張力,延後)
- **ISettings interface vs struct**:ECS / 序列化 settings 單獨存時可能要 struct 實作(Q-J 之後)
- **回收(池)語意**:DOTween 內建池 / PrimeTween 內建池 / 自製 runner 池;`Stop → 還池`時機;自製 runner 的池尚未實作
- **PrimeTween**:版本?專案是否已裝?(Q-F)→ 決定 E 階段能否照實際 API 驗
- **Timer / 自製引擎**:Timer 尚未重構,使用者「可能完全做一套自己的 tween 系統」→ D 階段範圍未定;需要時 Read 現有 Timer
- **`BaseMonoBehaviour.OnInitializing` / `initialized` 契約**(CLAUDE.md 列待確認)
- **`Motion.Interpolate` registry 機制**:手動 bootstrap 註冊 `Func<T,T,float,T>`(已知 ~6 型別),非反射掃描(已定方向,實作待 D)

---

## 12. 不在本次 scope

- Sequence / Parallel / Blend(將來)
- Timeline / Graph-based motion
- Tween path(DOPath)
- ECS / Job 實際整合(只保留資料層相容傾向)
- Editor PropertyDrawer 美化（C 之後）

---

## 13. 核心哲學（釘住）

- ✔ **`Motion` = 統一架構容器**:巢狀短名,不散落平行扁平名
- ✔ **殼 = 動詞**(IHandle);**設定 = 資料**(ISettings);**引擎 = 可抽換 provider**(IRunner)
- ✔ **組件只認殼**,換 DOTween/PrimeTween/自製 → 零改動
- ✔ **殼優先**:用已有 DOTween 驗證抽象;Interpolation / Timer / 自製引擎是後面的配角
- ★ 殼設計**參考 DOTween + PrimeTween 兩模型**(§7),實作先 DOTween,第二 provider 落地前算 provisional
