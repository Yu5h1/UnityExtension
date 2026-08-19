# 元件設計收斂

> 狀態：**規格已定案，尚未實作。** 阻塞用的兩個設計問題已於 2026-08-18 答完。使用者將併入其他重構工作執行。
> 起因：2026-08-17 為水柱 trail 找「材質 UV 捲動」既有方法時，發現材質控制元件有三代並存，職責重疊、成熟度不一。
> 本檔前半是 issue log 與決議紀錄，後半是實作規格。

---

## 現況地圖

```text
UnityExtension
└── 材質控制（三處，互不相識）
    ├── common/Runtime/Component/
    │   ├── AssetSequence<TComponent,TValue> (SO)      整顆資產循環切換 → 刪除
    │   │   └── MaterialSequence                       全庫唯一子類 → 刪除
    │   │       └── 使用端 → Addon/RendererAddon.cs
    │   └── MaterialController (MonoBehaviour)          子樹批次改材質參數
    │
    ├── common/Runtime/Data/Architecture/
    │   └── ParameterCollection<T> : ParameterObject<T[]>   已存在，五個具體子類
    │
    └── Animation/Runtime/Component/
        └── RendererMaterialController (MonoBehaviour) ──持有──> RendererMaterialResolver (SO)
                每幀 Process(Renderer)                          ├── TextureSheetResolver
                                                                ├── TextureSequenceResolver
                                                                └── TextureScrollResolver（2026-08-17 新增）
```

三者的軸：

| 元件 | 對象 | 觸發 | 改什麼 |
|---|---|---|---|
| `MaterialSequence` + `RendererAddon` | 單一 Renderer | 外部呼叫一次 | 換整顆 material |
| `MaterialController` | 自己 + 子樹全部 Renderer | 外部呼叫一次 | 材質的 property |
| `RendererMaterialController` + Resolver | 單一 Renderer | 每幀 | 材質的 texture / UV |

## 已確認的判斷（使用者，2026-08-17）

- **`MaterialSequence` 的用途正確**，有實際應用場合。但其承載機制與 `ParameterCollection<T>` 重複，見決議 3。
- **`MaterialController` 是設計一半的元件**，是收斂的主要對象。
- `RendererMaterialController` 名稱不好，且無獨立存在理由。

## 問題清單

標「→ 決議 N」者已由下方決議解決，標「→ 步驟 N」者已排入實作。

### `MaterialController`（主要對象）

1. **進不了 inspector**。`SetFloat(string, float)` 等全是兩參數，UnityEvent 面板只綁得了 0/1 參數的方法。宣稱的「批次」實際只對程式碼開放。→ 步驟 4
2. **事件半套**。`_floatChanged` / `_intChanged` / `_colorChanged` 是 C# `event UnityAction`（面板看不到），且 `SetVector` / `SetTexture` 沒有對應事件。→ 步驟 4
3. **不在 namespace 裡**（全域）。→ 步驟 4
4. **Get 系列回傳第一個命中的材質值**。多材質且值不同時回哪個是任意的。→ 步驟 4

### 命名衝突

5. **「Resolver」在庫內有兩種意思**：`IResolver<T>`（[AtomicComponents](AtomicComponents.md)，產出一個值）vs `RendererMaterialResolver`（每幀改材質，不產值）。兩者無繼承關係也無共同契約。→ 決議 6 / B1

### 既有 resolver 的規範偏離

6. **`TextureSheetResolver` / `TextureSequenceResolver` 帶 `[CreateAssetMenu]`**，違反 house convention（見 [introduction.md](../introduction.md)）。→ 步驟 3
7. **基底的 `fps` / `CurrentFrame()` 只對 frame-based resolver 有意義**。`TextureScrollResolver` 是連續型，面板上會露出無效欄位。→ 決議 5 / A1

### `MaterialSequence` / `AssetSequence`

8. **`MoveNext` 靠 `items.IndexOf(GetValue(component))` 反推位置**。材質不在陣列裡時 `IndexOf` 回 -1 → 下一格是 0，會靜靜跳回第一顆。→ 步驟 1（推導式游標保留，但此缺陷要顯式處理）
9. **`MaterialSequence.GetValue` 用 `renderer.sharedMaterial ?? renderer.material`**。`sharedMaterial` 為 null 時會走到 `renderer.material`，在 editor 下會實體化材質。→ 步驟 1（隨型別刪除消失）
10. ~~檔名與型別不符~~ — ✅ 已解（2026-08-18）：`RendererAide` → `RendererAddon`。

### 其他仍在用 `Aide` 的型別（→ 決議 6 / B2-B4，步驟 7）

11. `AudioSourceAide`（`common/Runtime/Component/`）+ `AudioSourceAideEditor`
12. `Collider2DAide`（`CombatAesthetic/Runtime/Physics/`）+ `Collider2DAideEditor`（其內部型別名為 `Collider2DAgentEditor`，與檔名和 attribute 也不一致）
13. `LayoutGroupAddon.cs` 內殘留字串 `"[LayoutGroupAide] ..."`；`Plugins/TMP/.../PopupPanel.prefab` 的 `m_EditorClassIdentifier` 仍寫 `Yu5h1Lib.UI.LayoutGroupAide`

### 斷裂的文件參照

14. **`MaterialTextureSheetController` 型別已不存在**，但三處 XML `<see cref>` 仍指著它：`RendererMaterialController.cs`（兩處，含一段已失效的遷移指引）與 `TextureSheetResolver.cs`（一處）。該元件正是被 resolver 架構取代的舊版。→ 步驟 3

## 收斂方向

職責重新切分成 **取得 vs 操作**：

| 職責 | 歸屬 |
|---|---|
| 從 Renderer 取得材質（搜集策略：子樹、shared/instance、shader 過濾） | `RendererAddon` |
| 對一組 Material 做事（改參數、每幀驅動 resolver） | `MaterialController` |
| 承載一組材質資料 + 循環切換 | `MaterialArrayObject` |

- `MaterialController` 現有的 `includeChildren` / `useSharedMaterial` / `enableShaderFilter` 全是搜集策略，移往 `RendererAddon`。這三個欄位跟「改參數」黏在同一顆，就是問題 1-4 那種「設計一半」感受的來源。（`includeChildren` 後續由決議 8 直接刪除，不遷移。）
- **`RendererMaterialController` 取消**。它只是「單一 Renderer + 每幀驅動」，由 `MaterialController` 天然涵蓋。
- 自洽性檢查：碰 Renderer 的歸 Addon，不碰 Renderer 的歸 Controller。
- 副作用：**粒子 trail 材質不再是架構搆不到的槽位**，只是來源之一。

成本：`RendererMaterialController` 有 `[RequireComponent(typeof(Renderer))]`，丟上去就自動綁自己的 Renderer、零接線。改成材質來源後最少多一步接線。不以「來源留空則 fallback 到自身 Renderer」補回（決議 5 / A6）。

## 已定案決議（2026-08-18 ~ 08-19）

### 決議 1：instance 生命週期歸供給端

⚠️ **產生方式已由決議 7 取代**（不再呼叫 `renderer.materials`），但「誰產生誰銷毀」的原則不變。

誰產生 instance 誰負責銷毀。`RendererAddon` 產生 instance，自己在 `OnDestroy` 銷毀。`MaterialController` 是純借用者，完全不碰生命週期，也因此不需要知道材質從哪來。

由 `RendererAddon` 的 `useSharedMaterial` 旗標決定回傳哪一種。

### 決議 2：材質來源型別為 `IReadOnlyList<Material>`

- `MaterialController` 的來源欄位是 **`Object[]` + `[TypeRestriction(typeof(IReadOnlyList<Material>))]`**，可同時接多個來源。
- Resolver 簽名為 **`Drive(IReadOnlyList<Material>)`**。
- `RendererAddon` 與 `MaterialArrayObject` 各自實作 `IReadOnlyList<Material>`（三個成員轉發給內部 `Material[]`）。

**否決 `IEnumerator<Material>`**（曾為候選）：`IEnumerator<T>` 是帶可變位置的游標本身，不是可重複列舉的集合。掛在元件上會造成單一共用游標、多 consumer 互踩、強制 `IDisposable`、每幀不知誰負責 `Reset()`，並且每幀重建 enumerator 會裝箱配置。`IReadOnlyList<Material>` 走索引迴圈零配置，且 `renderer.materials` 回傳的 `Material[]` 原生滿足此介面。

**否決具名 `IMaterialSource`**：介面不該為了 Material 而存在。

**查證結果**（2026-08-18）：
- `TypeRestrictionDrawer` 已支援介面——`Validate` 走 `Type.IsAssignableFrom`，GameObject 分支走 `GetComponent(t)`，兩條對介面都成立。
- ⚠️ `Mode.Exact` 用 `t == objType`，介面永遠不可能 exact match，故介面只有 `Include` / `Exclude` 有意義。
- `Object[]` 不需改 drawer：Unity 會把 attribute drawer 套到每個 array element，現況即可逐格驗證。

### 決議 3：`AssetSequence` / `MaterialSequence` 刪除，改走 `ParameterCollection<Material>`

`AssetSequence<TComponent,TValue>` 全庫**只有 `MaterialSequence` 一個子類**，泛型化的目的（以後可換 component）從未兌現。而 `ParameterCollection<T> : ParameterObject<T[]>` 已存在且有五個具體子類（`IntegerArrayObject`、`StringArrayObject`、`ObjectCollection`、`ParameterCollectionObject`、`GenericPresetObjectCollection`），資料面完全重疊。

改為：

```csharp
public class MaterialArrayObject : ParameterCollection<Material> { /* + MoveNext + IReadOnlyList */ }
```

順帶取得 `ParameterCollection<T>` 既有的 `Random()` 與 `GetRandomElement(exclude)`。

同時解除先前「`MaterialSequence` 不該當 `MaterialController` 來源」的顧慮——該顧慮的實質是寫入安全性而非 is-a 不成立，決議 1 之後供給端只供給自己擁有的東西，asset 來源就是「供給 asset、不擁有 instance」，代價在拖進去那一刻可見。`MaterialArrayObject` 同時是切換資料與批次修改來源，沒有結構問題。

### 決議 4：`MoveNext(Renderer)` 放在 `MaterialArrayObject`，游標用推導

游標**不儲存**，沿用現行做法從 renderer 當前材質反推位置。因此 SO 保持無狀態，同一顆 asset 給多個 renderer 共用時各自從自身當前材質往下走，不會同進同退。

`RendererAddon.MoveNext(MaterialSequence)` 這層包裝隨之刪除。UnityEvent 兩種形狀都綁得到（皆為單參數），不影響決策。

`RendererAddon` 中沒用到的 `current` 欄位與註解掉的 `MoveNext(_renderer, ref current)` 一併刪除——那是往「儲存游標」方向的試探，本決議不採。

### 決議 5：A 組七項（2026-08-18）

**A1 — `fps` 抽成可組合的 `FrameStepResolver`，不是中介基底。**

```text
common/Runtime/Resolver/
└── FrameStepResolver          [Serializable] plain class
                               持有 fps，Resolve(frameCount) → int frame

common/Runtime/Component/Driver/
├── TextureSheetDriver       持有 columns/rows + 一個 FrameStepResolver 欄位
├── TextureSequenceDriver    持有 textures + 一個 FrameStepResolver 欄位
└── TextureScrollDriver      連續型，不持有 FrameStepResolver
```

本項曾定為繼承版（`FrameStepResolver : MaterialDriver` 當中介抽象），同日改為組合。**繼承版被否決的理由**：中介基底會佔掉唯一的繼承槽，未來需要同時具備影格推進與其他共用行為的 resolver 會卡死；而且繼承版讓影格推進綁死在材質 driver 家族內，無法在別處使用。

**邊界**：`FrameStepResolver` 只知道 `fps` 與 frameCount，**不知道 grid、不知道材質**。`columns` / `rows` → col / row 的換算留在 `TextureSheetDriver`（那兩行住在真正擁有 `columns` / `rows` 的一邊）。曾考慮在其上放 `TryGetGridLocation(out column, out row)`，否決：精靈表概念一旦進去，它就不再是任何地方可用的通用元件。

**封裝形式**：plain `[Serializable]` class，比照 Core 既有的 `Repeater`。當欄位即內嵌序列化，不需 `[SerializeReference]`（已排除），也不需為每個 driver 多配一顆 SO 資產。

**位置**：先放 Unity 端 `common/Runtime/Resolver/`，直接用 `Time.time`。是否往 Core 搬（時間改由參數傳入以脫離 UnityEngine）留待日後重構，本輪不討論。

**不實作 `IResolver<int>`**：形狀相近但契約不合——`IResolver<int>.TryResolve(out int)` 不吃輸入，而 frameCount 是呼叫端給的（`columns * rows` vs `textures.Length`），不是自身狀態。詞彙相近不等於 is-a。

**A2 — 移除 `MaterialController` 的事件。** `_floatChanged` / `_intChanged` / `_colorChanged` 全刪，不補齊成 UnityEvent。目前零 consumer。

**A3 — 移除 `SetXxxForShader` 系列。** 搜集端（`RendererAddon`）已能過濾 shader，這組是同一件事的第二個入口。

**A4 — 移除 `Get` 系列與 `HasProperty`，改為曝露材質本身。** `MaterialController` 自身實作 `IReadOnlyList<Material>`，把多個來源攤平後對外開放。呼叫端要讀值、要 toggle，直接拿材質自己做，不經過語意可疑的「回傳第一個命中」轉發。`TestToggleOutline` / `TestSetOutlineWidth` 兩個 `[ContextMenu]` 測試方法一併刪除。

**A5 — `MoveNext` 找不到當前材質就從 0 開始。** 不是錯誤情形。`false` 只保留給真正的切換失敗：陣列為空或 null、renderer 為 null。
現行算式 `IndexOf(...) + 1` 在找不到時已經是 `-1 + 1 = 0`，所以這不是行為變更，是把隱性行為顯性化並補上回傳值。

**A6 — 來源欄位留空時不 fallback。** 留空就不動作，不自動綁自身 Renderer。比隱式綁定好除錯。

**A7 — `textureProperty` 更名為 `propertyName`。** 材質屬性可以是 float / color / vector / texture，原名把用途窄化了。
位置**維持在 `MaterialDriver`**，不隨 `fps` 一起下放：`fps` 有具體違反者（`TextureScrollDriver` 的面板上會出現無效欄位），`propertyName` 目前三個 driver 全都要用，沒有違反者。等第一個不需要它的 resolver 出現時再決定下放到哪一層。
`propertyId` 對外維持唯讀，改由 `propertyName` 的 setter 一併更新，避免兩者失同步。

### 決議 6：B 組六項（2026-08-18）

**B1 — `MaterialResolver` 更名 `MaterialDriver`，方法 `Process` 更名 `Drive`。** 不接受同字多義。

```text
MaterialResolver          → MaterialDriver          Drive(IReadOnlyList<Material>)
TextureSheetResolver      → TextureSheetDriver
TextureSequenceResolver   → TextureSequenceDriver
TextureScrollResolver     → TextureScrollDriver
Component/Resolver/       → Component/Driver/
```

`Driver` 在動畫與圖形領域即「每幀把值推進目標」的既定說法，比 `Processor` 有資訊量——後者幾乎任何類別都適用，說不出這族的特徵。`MaterialController` 持有 `MaterialDriver` 也讀得順。

改名後「Resolver」在庫內只剩「產出一個值」一義，`FrameStepResolver` 因此名正言順，**不改名**，維持在 `common/Runtime/Resolver/`。

成本：四個型別加一個資料夾。這些檔案本輪本來就要搬移與改簽名，且掛載點為零，改名是搭順風車而非額外一輪。

**B2 / B3 / B4 — `Aide` 殘留一併清除。**
- `AudioSourceAide` + `AudioSourceAideEditor` → Addon
- `Collider2DAide` + `Collider2DAideEditor` → Addon；其內部型別 `Collider2DAgentEditor` 同時帶了 Aide 與 Agent 兩種淘汰用詞，一併對齊
- `LayoutGroupAddon.cs` 內的殘留字串 `"[LayoutGroupAide] ..."` 清掉；`PopupPanel.prefab` 的 `m_EditorClassIdentifier` 不動，Unity 下次存檔會自行重寫

這三項與材質收斂無關，可單獨執行，見步驟 7。

**B5 — `Mode.Exact` 對介面無效，只寫進 summary，不加檢查。**

`TypeRestrictionDrawer` 的 `Mode.Exact` 用 `t == objType` 比對，`objType` 是拖入物件的實際型別，介面型別永遠不等於類別型別，因此 `Exact` 配介面會把任何拖入的物件清成 null，看起來像 bug 而非設定錯誤。`Include` / `Exclude` 走 `IsAssignableFrom`，對介面正常。

在 `TypeRestrictionAttribute` 的 `<summary>` 註明此限制即可。加執行期檢查需改 `Unity/Runtime/Base/Source/Attribute/`，屬 Core，成本不值。
⚠️ 即使只加 summary 也動到 Core，**需使用者授權**；且 `Unity/UnityExtension/Runtime/Attribute/` 下有同名檔案，要確認兩者是連結來源還是各自一份。

**B6 — `TypeRestriction` 的 array 聚合 drawer 不做。** 泛型 drawer 成本高；若對象限定 SO 還可直接畫 SO inspector，但此欄位的對象不一定是 SO。逐格驗證現況已可用。

### 決議 7：instance 自行建立，`useSharedMaterial` 是唯一旗標（2026-08-19）

決議 1 說「誰產生 instance 誰銷毀」，但 `renderer.materials` 的 getter **隱式**產生 instance，使「誰產生」不可觀測——別的腳本碰一下 `.material` 拿到的是同一份，擁有權無法判定。要安全就得加 `owned` 記錄 + `destroyInstancedMaterialsOnDestroy` opt-out，共三個欄位。

改為 `RendererAddon` **自行 `new Material(source)` 再寫回**：

```csharp
[SerializeField] private Renderer _renderer;
[SerializeField] private bool useSharedMaterial;

/// <summary>Instances created and owned by this addon. Null while sharing.</summary>
private Material[] materials;

public void RefreshMaterials()
{
    Release();
    if (_renderer == null || useSharedMaterial || !Application.isPlaying) return;

    var sources = _renderer.sharedMaterials;
    materials = new Material[sources.Length];
    for (int i = 0; i < sources.Length; i++)
        materials[i] = sources[i] == null ? null : new Material(sources[i]);
    _renderer.sharedMaterials = materials;
}

private void Release()
{
    if (materials == null) return;
    foreach (var m in materials)
        if (m != null) Destroy(m);
    materials = null;
}

private void OnDestroy() => Release();
```

自己造的東西全世界只有自己有，歧義消失 → 不需要 opt-out 旗標。`materials != null` 本身即擁有權記錄 → 不需要 `owned`。**欄位收斂成 `useSharedMaterial` 一個。**

**Renderer 機制查證（2026-08-19）**

- Renderer 只有**一個**材質陣列 `m_Materials`（即場景 YAML 中所見那個）。**沒有 source / instance 兩套儲存。**
- `materials` getter 的行為 = 把 `m_Materials` 每一格複製成新 Material → **寫回 `m_Materials`** → 回傳。因此自動實體化之後，`sharedMaterials` 回傳的是 instance，原始 asset 參照已從該 renderer 上消失。
- 故 `renderer.sharedMaterials = 自己複製的一份` 與 `renderer.materials` getter 等價。本決議不是繞過 Unity，是把隱式寫入攤開成明確的一行。
- ⚠️ Unity 官方立場：`renderer.materials` 產生的 instance **由呼叫者負責銷毀**。唯一的自動回收是 `Resources.UnloadUnusedAssets()`，通常只在載入場景時跑。
- ⚠️ **GC 不回收 `UnityEngine.Object`。** C# GC 只收 managed wrapper，native 那份不歸它管。「沒有 ref 就會被 GC 掉」不成立。

**實作約束**

1. 銷毀迴圈跑 cache 的 `materials`，**絕不跑 `_renderer.sharedMaterials`**。`materials` 裡永遠只有自造 instance，所以旗標中途被改的最壞情況是漏刪（留到換場景被 `UnloadUnusedAssets` 收），不可能刪到 asset。`useSharedMaterial` 那個判斷是免費雙保險，不是主要安全機制。
2. `!Application.isPlaying` 必須擋。edit mode 下建立並寫回會永久污染場景，且 `Destroy()` 在 edit mode 不可用。
3. 陣列長度嚴格照 `sources.Length`，null 格保留 null。長度對應 submesh，少一格該 submesh 就不畫；`ParticleSystemRenderer` 的 trail 佔 index 1（見開放技術問題），長度掉到 1 即失去 trail。
4. shader filter **不得**篩掉陣列成員。filter 只決定哪幾格要實體化（其餘沿用 shared），或只影響對外 `IReadOnlyList` 視圖，不影響寫回 renderer 的那個陣列。舊 `MaterialController.RefreshMaterials()` 的 `continue` 寫法在此會破壞陣列。
5. `RefreshMaterials()` 放 `Awake` 而非 `Start`，搶在其他腳本碰 `.material` 之前。若他人搶先，`sharedMaterials` 拿到的已是他的 instance，被本 addon 擠掉後會洩漏——那是他造成的，但現象會被觀察到。

### 決議 8：`includeChildren` 刪除，一個 addon 管一顆 Renderer（2026-08-19）

決議 7 的寫回是 per-renderer 的。多 renderer 攤平成單一 `Material[]` 之後無法反推「第 n 格屬於哪個 renderer 的第幾格」，得改成 `Material[][]` 再加一層扁平化視圖，把兩個維度混在一起。

改為讓 `RendererAddon` 名副其實：持有單一 `_renderer`，只管自己那顆。多 renderer 的情境由掛多個 addon 解決——`MaterialController` 的 `Object[]` 多來源欄位（決議 2）本來就能同時接。

`includeChildren` 欄位**直接刪除，不遷移**。

### 決議 9：`RendererAddon` 泛型化，特化 renderer addon 由它繼承（2026-08-19）

```csharp
public abstract class RendererAddon<TRenderer> : ComponentController<TRenderer>, IReadOnlyList<Material>
    where TRenderer : Renderer
public class RendererAddon : RendererAddon<Renderer> { }
public class LineRendererAddon : RendererAddon<LineRenderer>, IColor
```

起因：`LineRendererAddon` 拖進 `MaterialController.sources` 被 `TypeRestrictionDrawer` 擋下——它沒實作 `IReadOnlyList<Material>`。
兩條路是「同一顆 GameObject 再掛一個 `RendererAddon`」或「讓特化 addon 繼承材質供給」。**使用者選後者。**

agent 當時建議前者，理由是材質供給與線段幾何是兩件事，繼承會讓不需要動材質的線也背上複製與回收機制。
使用者的取捨是：一顆 Renderer 上有兩個都在 `GetComponent` 同一顆 renderer 的 addon 更難解釋，寧可付那份成本。**此項不再重開。**

**衍生約束（實作已套用）**

- 基底的 `OnDestroy` 必須是 `protected virtual`，且子類覆寫時要呼叫 base。Unity 只把訊息派送到最衍生的宣告，
  子類若自行宣告 `private void OnDestroy()` 會把基底那個藏起來，`Release()` 從此不執行，材質副本全漏——
  無編譯警告、無 runtime 錯誤，只在 profiler 上看得出來。此約束寫進基底 `<summary>`。
- `RefreshMaterials()` 同樣改 `virtual`。
- 完整 `<summary>` 掛在泛型基底上；非泛型 `RendererAddon` 另給一行，因為它才是 inspector 與 IntelliSense 先遇到的那個。

**`useSharedMaterial` 預設維持 false，子類不得翻轉。** 代價是每個 `LineRendererAddon` 在 Awake 複製一份材質，
即使那條線只用漸層與點位——一個 LineRenderer 一顆材質。反方向會讓寫入直接落在專案資產上，正是驗收條件 8 要抓的失敗；
為了省一顆材質，讓同一個旗標在 `RendererAddon` 上代表安全、在特化子類上代表危險，代價遠高於那顆材質。

真的量大到有感時，解法是把複製延後到第一次讀清單，不是換預設值。**但本輪不做**：延後會擴大「別人搶先碰 `.material`」的窗口，
決議 7 約束 5 特意選 Awake 就是為了關掉它。

## 未決清單

A 組七項見決議 5，B 組六項見決議 6，皆已於 2026-08-18 拍板。
C 組兩項於 2026-08-19 結案：C1 見決議 7 / 8，C2 見開放技術問題。**本清單已清空。**

### C. 缺資訊（已結案）

| # | 議題 | 缺什麼 |
|---|---|---|
| ~~C1~~ | Unity 自動實體化（`renderer.material` / `materials`）與決議 1 的手動管理如何相處 | 已解決（2026-08-19）。不共存——改為自行建立 instance，見決議 7；`includeChildren` 一併刪除，見決議 8 |
| ~~C2~~ | `ParticleSystemRenderer` 的 trail 材質是否出現在 `sharedMaterials` 陣列中 | 已解決（2026-08-19）。見下方開放技術問題 |

## 開放技術問題

- **`ParticleSystemRenderer` 的 trail 材質是否出現在 `sharedMaterials` / `materials` 陣列中？**
  **已驗證：是。**（2026-08-19）trail 材質就在陣列裡，index 1；index 0 是粒子材質。

  驗證方式不需要啟動 Unity——`m_Materials` 是序列化欄位，直接讀場景 YAML 即可。
  對照組在 `Unified-Solver/Runtime/Test/test solver mesh.unity`：

  | GameObject | `TrailModule.enabled` | `m_Materials` 長度 |
  |---|---|---|
  | fileID 1612126555 | 0 | 1 |
  | fileID 1978910203 | 1 | 2 |

  **影響：不需要第二種供給端。** 決議 2 的 `Object[]` 多來源欄位本來就能接，
  而 `RendererAddon` 的 `IReadOnlyList<Material>` 直接含 trail，`psr.trailMaterial` 不需要特例化。

  殘留小未知（不阻塞）：runtime 用腳本切 `ps.trails.enabled` 時陣列長度會不會跟著變。
  `m_Materials` 是序列化欄位，較可能的行為是長度由編輯期決定。若如此，
  切換 trail 後需重呼 `RefreshMaterials()`——這是使用約定，不影響架構。

---

# 實作規格（定案 2026-08-18）

任務：收斂 `RendererAddon` 與 `MaterialController`，吸收 `RendererMaterialController` 與其 driver 家族，並讓材質資料改走既有的 `ParameterCollection<T>`。

## 目標形狀

全部落在 `common`。`Animation` 在 common 之下，材質控制不屬於 Animation，沒有東西需要留在那邊。

```text
common/Runtime/Component/
├── Addon/RendererAddon.cs      持有單一 Renderer、自建材質 instance、負責其生命週期
│                               : IReadOnlyList<Material>
├── MaterialController.cs       綁材質來源、改參數、每幀驅動 driver
├── MaterialDriver.cs         abstract SO 契約：Drive(IReadOnlyList<Material>)
└── Resolver/
    ├── TextureSheetDriver.cs     持有 columns/rows + FrameStepResolver 欄位
    ├── TextureSequenceDriver.cs  持有 textures + FrameStepResolver 欄位
    └── TextureScrollDriver.cs    連續型，無 FrameStepResolver

common/Runtime/Resolver/
└── FrameStepResolver.cs        [Serializable] plain class：fps → frame index

common/Runtime/Data/Architecture/Object/
└── MaterialArrayObject.cs      : ParameterCollection<Material>, IReadOnlyList<Material>
                                + MoveNext(Renderer)

已刪除：AssetSequence.cs、MaterialSequence.cs、RendererMaterialController.cs
Animation/Runtime/Component/    材質相關檔案全部移出
```

## 作法列項

### 1. 資料層改走 `ParameterCollection<Material>`

1.1 新增 `MaterialArrayObject : ParameterCollection<Material>`，置於 `common/Runtime/Data/Architecture/Object/`（與 `IntegerArrayObject` / `StringArrayObject` 同層）。
1.2 實作 `IReadOnlyList<Material>`，三個成員轉發給 `value`。
1.3 實作 `MoveNext(Renderer)`：從 `renderer.sharedMaterial` 在 `value` 中反推索引，`Repeat` 繞回後寫回 `renderer.sharedMaterial`。
1.4 `MoveNext` 回傳 `bool`：找不到當前材質即從索引 0 開始，`false` 只給陣列空/null 或 renderer 為 null（決議 5 / A5）。
1.5 刪除 `AssetSequence.cs` 與 `MaterialSequence.cs`。
1.6 補 XML `<summary>`（英文）。

### 2. 搜集職責移入 `RendererAddon`

2.1 把 `useSharedMaterial` / `enableShaderFilter` / `shaderNameFilters` 三個序列化欄位從 `MaterialController` 移過來。`includeChildren` **直接刪除不遷移**（決議 8）。不新增 `owned` 或 `destroyInstancedMaterialsOnDestroy`（決議 7）。
2.2 移入 `RefreshMaterials()` 與 `PassesShaderFilter()`，改寫成決議 7 的形式：`Release()` → 讀 `_renderer.sharedMaterials` → 逐格 `new Material(source)` → 寫回 `_renderer.sharedMaterials`，結果存為內部 `Material[] materials`。在 `Awake` 呼叫。shader filter 不得篩掉陣列成員（決議 7 約束 4）。
2.3 `CleanupMaterials()` 改名 `Release()`：迴圈跑 cache 的 `materials`，**不跑 `_renderer.sharedMaterials`**（決議 7 約束 1）。`OnDestroy` 與 `RefreshMaterials()` 開頭各呼叫一次。
2.4 實作 `IReadOnlyList<Material>`，三個成員轉發給內部 `Material[]`。
2.5 刪除 `MoveNext(MaterialSequence)` 包裝、沒用到的 `current` 欄位、以及註解掉的 `MoveNext(_renderer, ref current)`（決議 4）。
2.6 補 XML `<summary>`（英文），目前沒有。

### 3. driver 搬入 common 並改簽名

3.1 `Animation/Runtime/Component/RendererMaterialResolver.cs` → `common/Runtime/Component/MaterialDriver.cs`，型別更名 **`MaterialDriver`**。名稱裡的 `Renderer` 在吃材質之後不成立。
3.2 `Drive(Renderer)` → `Drive(IReadOnlyList<Material>)`。
3.3 新增 `FrameStepResolver`（`common/Runtime/Resolver/`，plain `[Serializable]` class），把 `fps` 與影格算式從 `MaterialDriver` 移出（決議 5 / A1）。
3.3b `textureProperty` 更名 `propertyName`，`propertyId` 改由其 setter 一併更新並維持對外唯讀（決議 5 / A7）。
3.4 `Animation/Runtime/Component/Driver/` 三個檔搬到 `common/Runtime/Component/Driver/`，改為走傳入的材質清單而非 `renderer.material`，以索引迴圈避免配置。前兩個各持有一個 `FrameStepResolver` 欄位，grid 換算留在 `TextureSheetDriver` 自己；`TextureScrollDriver` 不持有：
- `TextureSheetDriver` — 每顆材質套同一組 scale/offset
- `TextureSequenceDriver` — 每顆材質套同一張貼圖
- `TextureScrollDriver` — 每顆材質套同一個 offset

3.5 移除 `TextureSheetDriver` / `TextureSequenceDriver` 的 `[CreateAssetMenu]`，對齊 house convention。
3.6 清掉三處指向已不存在型別 `MaterialTextureSheetController` 的 `<see cref>`（問題 14），含 `RendererMaterialController` 那段已失效的遷移指引。

### 4. `MaterialController` 吸收驅動職責

4.1 新增來源欄位 `Object[]` + `[TypeRestriction(typeof(IReadOnlyList<Material>))]`（決議 2）。
4.2 新增 `MaterialDriver` 欄位，**必須加 `[Inline]`**（house convention），加 `Update()` 每幀對每個來源呼叫 `Drive`。
4.3 既有 `SetFloat` / `SetColor` / `SetVector` / `SetTexture` 改為對來源清單操作，移除 `cachedMaterials` 與 `MaterialData`（`shaderName` 快取隨 shader 過濾一起搬到 `RendererAddon`）。
4.4 移除 `SetXxxForShader` 系列（決議 5 / A3）。
4.5 移除 `_floatChanged` / `_intChanged` / `_colorChanged` 三個事件（決議 5 / A2）。
4.6 加上 `namespace Yu5h1Lib`。
4.7 移除 `Get` 系列、`HasProperty`、兩個 `[ContextMenu]` 測試方法；改為讓 `MaterialController` 自身實作 `IReadOnlyList<Material>`，把多來源攤平後對外開放（決議 5 / A4）。
4.8 補 XML `<summary>`（英文）。

### 5. 刪除與資料遷移

5.1 刪除 `RendererMaterialController.cs`。
5.2 序列化遷移：`Packages/Unified-Solver/Runtime/Test/test solver mesh.unity` 掛著 2 個 `RendererMaterialController` 與 1 個 `MaterialController`，改掛 `RendererAddon` + `MaterialController` 並重接 driver 引用。
5.3 舊 `MaterialController` 移除欄位後殘留的序列化資料一併清掉。

⚠️ 5.2-5.3 不可逆且需人工核對，比照 Timer 重構前例由**使用者自行處理**。

**遷移範圍已掃描完畢**（2026-08-18）：`MaterialSequence`、`RendererAddon`、`MaterialController`、`RendererMaterialController`、`RendererMaterialResolver` 與三個 driver，在 Yu5h1Lib 內除上述測試場景外**無任何 `.asset` / `.unity` / `.prefab` 掛載點**，在 `W:\UnityProject\Assets` 內**零掛載點**。

### 6. 驗證

6.1 使用者自行 rebuild 與 Unity 編譯（不由 agent 執行）。
6.2 跑 trail 探針：對開啟 Trails 的 ParticleSystem 讀 `psr.sharedMaterials.Length`。
6.3 端到端：水管水柱的 trail 材質 UV 捲動。

### 7. `Aide` 清理（獨立於材質收斂，可單獨執行）

7.1 `AudioSourceAide` → `AudioSourceAddon`，連同 `AudioSourceAideEditor`。
7.2 `Collider2DAide` → `Collider2DAddon`，連同 `Collider2DAideEditor`；其內部型別 `Collider2DAgentEditor` 一併對齊檔名與 attribute。
7.3 清掉 `LayoutGroupAddon.cs` 內的殘留字串 `"[LayoutGroupAide] ..."`。
7.4 `PopupPanel.prefab` 的 `m_EditorClassIdentifier` 不動。

## 驗收條件

1. `RendererMaterialController`、`AssetSequence`、`MaterialSequence` 三個型別都不存在；`Animation` 套件內沒有材質控制相關檔案。
2. `MaterialController` 位於 `Yu5h1Lib` namespace，來源欄位可同時接受多個 `IReadOnlyList<Material>` 實作者，並每幀驅動 driver。
3. `RendererAddon` 持有單一 `Renderer`，依 `useSharedMaterial` 與 shader 過濾產出材質；`useSharedMaterial` 為 false 時 instance 由 addon 自行建立、寫回 renderer，並在 `OnDestroy` 銷毀。序列化欄位中不存在 `includeChildren`、`owned` 或任何 opt-out 旗標。
4. `MaterialArrayObject : ParameterCollection<Material>` 可在 inspector 建立，`MoveNext(Renderer)` 循環行為與舊 `MaterialSequence` 一致，且當前材質不在陣列中時行為是明確定義的。
5. 三個 texture driver 位於 common，簽名吃 `IReadOnlyList<Material>`，皆無 `[CreateAssetMenu]`。
6. `TextureScrollDriver` 的 inspector 上不再出現 `fps`；兩個 frame-based driver 各以內嵌的 `FrameStepResolver` 呈現。`FrameStepResolver` 不引用任何材質或 grid 型別。
7. `MaterialController` 不再有事件、`SetXxxForShader`、`Get` 系列與 `HasProperty`，並可被當成 `IReadOnlyList<Material>` 讀取。
8. **進入播放模式操作後退出，專案內的 material 資產內容未被改動。** 這條直接驗 asset 誤寫風險。
9. `test solver mesh.unity` 的三個掛載點行為與改動前一致。
10. 粒子 trail 材質可經 `MaterialController` 驅動 UV 捲動（結果取決於 6.2 探針）。

## 不在範圍

- 未決 C1 的 Unity 自動實體化處理方案。
- `TypeRestriction` 的 array 聚合 drawer（決議 6 / B6）。
- `Aide` 清理雖已決定要做（決議 6 / B2-B4），但與材質收斂無關，列為步驟 7 可單獨執行。

## 工作量規模

```
Volume:    改寫 383 行(MaterialController)
           刪除 35(RendererMaterialController) + 21(AssetSequence) + 9(MaterialSequence) 行
           搬移 49(resolver 基底) + 89(三個 driver) 行
           擴充 RendererAddon 20 行；新增 MaterialArrayObject 約 30 行、FrameStepResolver 約 20 行
           MaterialController 因 A2/A3/A4 三刀淨減，383 行預期剩約三分之一
           觸及 10 個 .cs 檔
           程式碼消費者 0 個
           序列化掛載點 3 個，全在 Unified-Solver 測試場景；W:\UnityProject 零掛載點
Precedent: SO 策略 + MonoBehaviour 殼、ParameterCollection<T> 皆為庫內既有模式 = settled port
           「材質 instance 生命週期轉移」無前例，但決議 1 / 7 已定調 = 風險已收斂
Proof:     需 Unity 編譯 + 測試場景手動驗證 + trail 需編輯器實測
```

## 風險

| 風險 | 出現時機 | 緩解 |
|---|---|---|
| resolver 每幀寫入專案 material 資產 | 拖 asset 來源進 `MaterialController` 時 | 決議 1 讓供給端擁有；驗收條件 6 專門攔這個 |
| Unity 自動實體化與手動管理打架 | 步驟 2.3 | 見未決 C1，實作時若撞到即停手回報 |
| 場景引用斷裂 | 步驟 5 | 範圍已掃描，僅測試場景 3 處；使用者手動遷移，遷移前先備份 |

## 執行方式

使用者將於其他重構工作中一併執行，本檔不單獨排程。

依 [agent-work-route](../../../../.agents/skills/agent-work-route/SKILL.md)，規格已定案，下一步是 Execute。是否轉成 `implementation-checklist.md` 由使用者決定——UnityExtension 目前沒有該檔，尚未 opt in。

## 已完成

- 2026-08-19：步驟 1-4 與 5.1 全部落地（agent），未編譯驗證。
  - 新增 `MaterialArrayObject`（`Data/Architecture/Object/`），刪除 `AssetSequence` / `MaterialSequence`。
  - `RendererAddon` 改寫成決議 7 / 8 的形狀，並改繼承 `ComponentController<Renderer>`（見下方偏離）。
  - `RendererMaterialResolver` → `common/Runtime/Component/MaterialDriver.cs`（git mv 連同 `.meta`，GUID 保留），
    三個 resolver → `common/Runtime/Component/Driver/Texture*Driver.cs`，皆 `Drive(IReadOnlyList<Material>)`、無 `[CreateAssetMenu]`。
  - 新增 `FrameStepResolver`（`common/Runtime/Resolver/`）。
  - `MaterialController` 改寫：`Object[]` 多來源 + `[Inline] MaterialDriver` + 每幀 `Drive`，
    移除事件、`SetXxxForShader`、`Get` 系列、`HasProperty`、兩個測試 `[ContextMenu]`，加上 `namespace Yu5h1Lib`。
  - 刪除 `RendererMaterialController.cs`；`Animation` 套件內已無材質控制檔案。

  **兩處對規格的偏離**（實作時判斷，需使用者確認）：
  1. `RendererAddon` 改繼承 `ComponentController<TRenderer>` 而非自持 `_renderer` 欄位。
     `Renderer` 由所在 GameObject 決定，不是設定；且 `OnInitializing` 在 `Awake` 執行，正好滿足決議 7 約束 5。
     與 `TransformAddon` / `ParticleSystemAddon` 一致。
     **已由使用者確認並進一步泛型化，見決議 9。**
  2. 決議 7 的 `materials` 一個欄位拆成三個 runtime 私有欄位 `sources` / `instances` / `materials`。
     `sources` 保留原始 asset，否則第二次 `RefreshMaterials()` 會拿已被替換的 instance 當來源複製，
     且 `Release()` 無法把 renderer 還原（addon 單獨移除時會留下指向已銷毀材質的 renderer）。
     `materials` 是套用 shader filter 後的對外視圖，`instances` 是全長度的擁有權記錄——
     決議 7 約束 3 要求寫回陣列保持長度，約束 4 要求 filter 不得動到該陣列，兩者無法用同一個陣列同時滿足。
     **序列化欄位仍只有 `useSharedMaterial`**，驗收條件 3 成立；銷毀迴圈仍只跑 `instances`。

- 2026-08-18：型別 `RendererAide` → `RendererAddon`。
- 2026-08-17：新增 `TextureScrollDriver`（連續 UV 捲動）。未動既有元件，未編譯驗證。

## 關聯

- [AtomicComponents.md](AtomicComponents.md) — `IResolver<T>` 家族，命名衝突的另一邊
- [Yu5h1lib-Unity-ScriptableObject-Architecture.md](Yu5h1lib-Unity-ScriptableObject-Architecture.md) — `ParameterObject` / `ParameterCollection<T>` 所屬架構
- [introduction.md](../introduction.md) — house convention（`[CreateAssetMenu]` 禁令、`[Inline]`、namespace）
- [agent-work-route](../../../../.agents/skills/agent-work-route/SKILL.md) — plan / spec / checklist 的階段界線
