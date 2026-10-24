# Motion System Refactor

> Task ID: motion-system

## 系統階層與關係

```text
Motion：Tween 行為契約與共享案例
└─ DotNet：共用 C# 框架、easing、參考 runner
   └─ Unity/Core：來源納入與 DLL 交付
      └─ UnityExtension：Core 接入 → DOTween runner → 實際案例重播
         └─ 後續：既有組件與場景遷移
```

## 目標與責任

首輪依 [Tween 目標 1](../../../../Motion/Documentation/ModuleSpec/Tween.md#goal)，完成純量單週期的 DOTween runner，與 Core 參考 runner 使用同一份案例。行為、命名、完成後的 handle 生命週期及六動詞由 Motion 規格擁有；本文件保留 Unity 的接入與遷移安排。

Core 框架由 [DotNet 卡片](http://127.0.0.1:8001/?scope=dotnet&pin=core-motion-contracts) 承接。Unity 以既有 DOTween 套件適配，不重寫 Core 契約或 easing；引擎型別以 `DG.Tweening.Tween` 全名避免與 `Yu5h1Lib.Tween` 混淆。進度與依賴只由 [motion-system 卡片](http://127.0.0.1:8001/?scope=unity-extension&pin=motion-system) 擁有。

## 接入與驗收

| 工作 | 交付與證據入口 |
|---|---|
| Core 接入 | [接入清單](../../checklists/item-motion-system-todo-1.checklist)：核對 linked sources、建置 DLL、實際載入路徑與公開入口 smoke test |
| DOTween 適配及重播 | [實作清單](../../checklists/item-motion-system-todo-3.checklist)：真實引擎執行共享案例，核對寫入、狀態與事件順序 |
| 驗收紀錄 | 由實作輪次建立 `Packages/Plugins/DOTween/Documentation/Motion/Tween.md`，持有版本、提交、DLL 雜湊、接入與重播歷史證據；逐項結果路由到清單 |
| 雙端目標核對 | [Motion 卡片](http://127.0.0.1:8001/?scope=motion&pin=motion-tween-shell) 核對兩端證據；Unity 不代替 Motion 宣告目標完成 |

Tween 接入不等待 Throw／Scatter 的整包交付。共享案例採可控制的時鐘；scaled／unscaled、宿主生命週期及未跑平台依來源規格明列限制。目標 1 不要求十一組件遷移、PrimeTween 或 loops，也不以本輪完成關閉整份 UNITYEXTENSION-DOTNET-1。

## 8. 既有現況（要遷移的東西）

**Source**:`Packages/Plugins/DOTween/Runtime/Component/`,asmdef `Yu5h1Lib.DOExtension`(rootNamespace `Yu5h1Lib`)。

`TweenBehaviour.cs` 三層繼承:
- `TweenBehaviour : BaseMonoBehaviour`(abstract;持 `Tweener tweener`、`abstract Tweener Create()`、`normalizedTime => tweener.ElapsedPercentage()`)
- `TweenBehaviour<TComponent, TValue1, TValue2, TPlugOptions>`(核心,DOTween 型別大量出現;`TweenerCore`、`startValue/endValue` 寫穿 live tween、`_PlayEvent/OnCompleteEvent/OnRewindEvent`、`OnInitializing` 設定 + `SetAutoKill(false)`、`OnEnable→PlayForward`、`OnDisable→Rewind + DOTween.Pause(component)`、`TryPlayTween`/coroutine + `IsWaiting`)
- `TweenBehaviour<TComponent, TValue, TPlugOptions>`(折疊 same-type)

**原遷移候選清單（遷移輪次須核對實際型別與場景引用）**:DOFade、DOMove2D、DORotate2D、DOScale、DOColor、DOAudioVolume、DOCounter、DOSpriteAnimation、DOTimeScale、TweenColorRenderer、TweenFloat、TweenLoadAsync(+中介 `DOTransform`)。

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

→ 全搬進 `MotionBehaviour`(behaviour 層),改用 `Tween.IHandle` 而非 `tweener`。中介 `DOTransform` 壓平。`TPlugOptions`/`TValue1,TValue2` 折疊(11 個 concrete 全 same-type)。

---

## 9. 組件遷移方向

在 runner 通過目標 1 後，另以現有組件與場景引用規劃遷移。保留 §8 的序列化欄位、事件與宿主生命週期需求；遷移前核對資產相容性、loop 能力及非純量型別，不能把純量案例通過當成這些能力已支援。

Core 參考 runner 已納入首輪。PrimeTween、WPF／WinForms 的平台時鐘接線與完整組件遷移各自另定範圍；不在本輪安裝新引擎或切換既有場景。

## 已退役的方向

原 §0–§7、§10–§13 的殼規範與階段順序由 Motion Tween 1.0.0 取代，git 保留原內容。純介面先行、參考 runner 延後、完成後 handle 失效與五動詞等舊敘述不再作為實作依據。保留 §8 的遷移素材，避免遺失既有組件相容性需求。
