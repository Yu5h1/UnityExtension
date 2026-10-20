# Configuration 驗收紀錄（Yu5h1Lib.Common）

> Task ID: common-configuration
>
> 狀態：planned（2026-10-20，第一輪：Configuration 1.4.0〈目標 2〉等級 1「直接存」，需求 UX-UNITYEXTENSION-7）。依使用者指示「在 Yu5h1Lib.Common 引用 UX 的 Configuration」，流程見 [referenced-module-acceptance](../../../../../../.agents/skills/referenced-module-acceptance/SKILL.md)。本輪原本釘 1.3.1 的八條服務語意，2026-10-20 依 UX 第 93 題縮為等級 1，改釘 1.4.0；所有檢查都還沒跑。

```text
UnityExtension
└─ Yu5h1Lib.Common（實作套件，Packages/common）
   └─ Configuration 等級 1「直接存」（本紀錄）
      ├─ 體驗規範 ← Yu5h1Lib.UX ModuleSpec/Foundation/Configuration 1.4.0〈目標 2〉（CFG-01、CFG-02、CFG-08、CFG-07 原生等價、CFG-P01、CFG-P05）
      ├─ 原生層 → 統一入口 Preferences（PreferencesEntry.cs）、host Preferences<T>、DataView、PlayerPrefsStorage
      ├─ 本輪不含 → 等級 2「確認保存」（目標 3）、宣告、Theme、生命週期的人工確認
      └─ 驗證專案 → Yu5h1LibTest（Unity 6000.6），自動檢查在分支的乾淨 worktree 執行
```

## 身分

| 項目 | 內容 |
|---|---|
| 實作套件 | `com.yu5h1.common`（`Packages/common`，Yu5h1Lib.Common）的 Preferences 層：[`PreferencesEntry.cs`](../../Runtime/Data/PreferencesEntry.cs)（統一入口 `Preferences`、`IStorage`、`PlayerPrefsStorage`）、[`Preferences.cs`](../../Runtime/Component/Preferences.cs)（host `Preferences<T>`）、[`DataView.cs`](../../Runtime/Data/DataView.cs) |
| UX 規範 | [UX ModuleSpec/Foundation/Configuration.md](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md) 1.4.0，SHA-256 `4ee43951f43da3bd10a27c02754aebe0a445bdfc74fd4a60a20b8fea971d61d2`（與 UX manifest 相同） |
| UX commit | `bf62034`（1.4.0 與縮範圍後的 UX-UNITYEXTENSION-7） |
| 保存等級 | 等級 1「直接存」（[保存等級](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#保存等級)）：同步、本地、低代價的設定，改了就存，沒有草稿、確認、重試與套用階段 |
| UX 決策 | 第 90 題 `adopt`、第 91 題 `entry-wide`（統一入口的全部 KEY 都適用載入失敗不覆寫）、第 92 題 `split`、第 93 題 `two-levels`（保存分兩級，依失敗模型與代價分級，不依平台） |
| DX 基準 | 不適用：Configuration 是基礎層的保存語意，沒有視覺組裝 |
| 套件釘選 | 快照 `Packages/common/Tests/Editor/UX~/Configuration/Configuration.md`（位元組與 UX 相同，`.gitattributes` 設 `-text`），EditMode `ConfigurationDesignTests` 核對雜湊。實作分支 `2ee96de` 釘的是 1.3.1，需由實作方改為 1.4.0 |
| 環境 | Windows 11、Unity 6000.6.0f1 Editor、Yu5h1LibTest |

## 範圍

本輪驗〈[目標 2](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#目標-2等級-1直接存在-unity-原生-preferences-上成立進行中2026-10-20-點名)〉的四條情境與兩項平台檢查：

1. 預設與載入（CFG-01）：沒有紀錄時用 host 的 `defaultSetting`，有紀錄時還原；有存檔但缺欄位時補預設並寫一次，是宿主明定的新增欄位遷移。
2. 載入失敗不覆寫（CFG-02）：存檔讀不懂或版本未知時印 Error、用預設值繼續、不覆寫原存檔、該 KEY 不寫盤直到明確重設；修好後重新讀取即恢復；統一入口的全部 KEY 都適用。
3. 通知與隔離（CFG-08）：改了就存，不同 KEY 互不覆寫。
4. 卸載與延遲完成（CFG-07）的原生等價：host 元件銷毀後入口不再呼叫它；入口可釋放單一 KEY，釋放後下次存取重新讀取，等於重開。
5. 實際 backend（CFG-P01）：真實 PlayerPrefs 的保存、重啟、重讀與 KEY 隔離。
6. 匯入與版本固定（CFG-P05）：本輪只有規格檔一個釘選對象；manifest 全檔、artifact 雜湊與具體預設 JSON 屬宣告，不在本輪。

不在本輪：

- 保存提交順序（CFG-03）、保存失敗與重試（CFG-04）、已保存未套用（CFG-05）、單一進行中命令（CFG-06）、草稿及模式（CFG-12）屬等級 2，在〈目標 3〉，等 Unity 出現需要它的消費端。
- 宣告與宣告產生欄位（CFG-09～CFG-11、CFG-14、CFG-P03）、Theme 持久化（CFG-13）。
- 生命週期（CFG-P02：Play 停止／重開後值仍在）：模組輪通過後，依第 89 題在 Yu5h1LibTest demo 的 uGUI 與 UI Toolkit 兩頁各開一份人工 checklist，不進本輪。

## 分工

依 referenced-module-acceptance〈Divide implementation and acceptance〉與 UX-UNITYEXTENSION-7〈分工〉：

| 角色 | 做什麼 | 不做什麼 |
|---|---|---|
| 驗收方（Opus 驗收） | 在共用 checkout 的 `adopt-ux-optionpicker` 線提交本紀錄與 checklist；開實作分支 `feat/ux-unityextension-7-configuration`；之後審查、在分支的乾淨 worktree 跑自動檢查並記入主線 checklist；開人工 checklist；全過後合併、結案 | 不寫實作 |
| 實作方（Sonnet 實作） | 在自己的 worktree 於實作分支引用：第一階段做釘選、原生對照與缺口決策題（已完成，分支 `5a1b47b`）；第二階段依下方決策實作與測試；PlayMode 前後依 [introduction〈Adopting a UX module in a package〉](../../../../.agents/introduction.md#adopting-a-ux-module-in-a-package) 改指與還原 Yu5h1LibTest 的 manifest | 不勾 checklist、不改紀錄結果、不合併、不 tag、不切換共用 checkout |

## 決策

實作方第一階段在 [cross-platform-ui-parity.decisions](../../../../decisions/cross-platform-ui-parity.decisions) 出了七題，使用者 2026-10-20 都已作答；其中幾題的答案是討論反饋，結論由 UX 流程審查與使用者討論後寫進 UX 1.4.0〈目標 2〉。

| 決策 | 結論 | 影響的情境 |
|---|---|---|
| `config-storage-format` | 有效：存檔加版本欄位、解析改嚴格、舊存檔當版本 0 | 載入失敗不覆寫（CFG-02） |
| `config-load-error-recovery` | 有效：印 Error、用預設值繼續、不寫盤直到明確重設；加查詢錯誤與重新讀取的入口 | 載入失敗不覆寫（CFG-02） |
| `config-service-subject` | 有效：服務＝入口裡的 KEY；host 元件銷毀時自行登出；入口加 `Unload` 釋放單一 KEY | 卸載與延遲完成（CFG-07）原生等價、實際 backend（CFG-P01）的重啟 |
| `config-accepted-value-split` | 撤回：已接受值與寫入的分離只有等級 2 需要（第 93 題） | — |
| `config-save-failure-detect` | 撤回：寫入失敗的偵測與重試屬等級 2 | — |
| `config-apply-shape` | 撤回：套用階段屬等級 2 | — |
| `config-decode-hook` | 撤回：值的驗證（decode）屬等級 2 | — |

決策檔只有 pending／decided 兩種狀態，撤回的四題保持原樣，撤回與原因只記在本表。

## 原生對照（planned）

起點是實作方第一階段的讀碼結果（分支 `5a1b47b`），本輪縮為等級 1。「現況」是讀碼推得，沒有實測；所有行為還沒驗。

| 行為 | Unity 入口／host 的對應點 | 現況（讀碼） | 本輪要成立的事 | 狀態 |
|---|---|---|---|---|
| [預設與載入（CFG-01）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#行為編號與共同案例) | 建立＝host 建立（`Awake`，沒有 I/O）；load＝`Preferences<T>.current` 第一次存取 → `GetEntry` → `IStorage.TryLoad` | 首次存取才讀；沒有存檔時併入 `defaultSetting`、不寫盤；有存檔缺欄位時補預設並寫一次（[宿主明定的新增欄位遷移 D6](../../../../Documentation/偏好設定.md#so-設定來源d6)）。有綁定控件時，`BindPort` 對沒有預設的欄位用控件值補並立即寫盤 | 有／無存檔、缺欄位遷移照現況成立並測試；綁定路徑的首次寫盤另記，案例用不含綁定的 host 驗 | planned |
| [載入失敗不覆寫（CFG-02）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#行為編號與共同案例) | `IStorage.TryLoad` → `DataView.TryParseFromJson` | 「沒有 KEY」與「解析失敗」都回 false，入口建立空 view，下一次變更即覆寫原存檔（[偏好設定 E3](../../../../Documentation/偏好設定.md#已知問題)）；存檔沒有版本欄位；`TryParseFromJson` 的舊格式路徑以逗號切割，破損 JSON 或 `null` 可能被誤判成功，`null` 還可能在 `CopyFrom` 丟 NullReferenceException | 依 `config-storage-format` 與 `config-load-error-recovery`：版本欄位、嚴格解析、舊存檔當版本 0；載入錯誤時印 Error、用預設值、保留原始字串、該 KEY 不寫盤直到明確重設；可查詢錯誤、可重新讀取；全部 KEY 都適用；E3 改寫為新行為 | planned |
| [通知與隔離（CFG-08）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#行為編號與共同案例) | 每 KEY 一個 `Entry` 與 DataView；通知＝`DataView.Changed`、host `changed`（UnityEvent） | 不同 KEY 各自一份 view，存檔只寫該 KEY；入口是 `Changed` 的第一個訂閱者，存檔先於其他觀察者；取消訂閱可行 | 改了就存、不同 KEY 互不覆寫、取消訂閱後不再通知 | planned |
| [卸載與延遲完成（CFG-07）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#行為編號與共同案例)的原生等價 | host `OnDestroy`；入口的 KEY | 入口靜態、比 host 活得久；host 註冊的 `HostStorage` 在 host 銷毀後仍留在靜態表裡並持有已銷毀的 host，之後 `Preferences.Save(KEY)` 還會呼叫它；沒有釋放單一 KEY 的入口 | 依 `config-service-subject`：host 銷毀時登出，入口不再呼叫它；`Unload(key)` 釋放單一 KEY，下次存取重新讀取 | planned |
| [實際 backend（CFG-P01）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#平台驗收與已知界線) | `PlayerPrefsStorage`、真實 `PlayerPrefs` | 提交點＝`PlayerPrefs.Save()`；無記錄＝`HasKey` 為 false；EditMode 的「重啟」需要清空入口狀態的入口，`ClearPlayerPrefs` 不重設已載入的 Entry | 以 `Unload` 作為重啟的等價；保存、重啟、重讀、KEY 隔離成立；沒有存檔與載入錯誤可分辨 | planned |
| [匯入與版本固定（CFG-P05）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#平台驗收與已知界線) | `Tests/Editor/UX~/Configuration/Configuration.md`、`ConfigurationDesignTests` | 分支 `2ee96de` 已建立快照與測試，但釘的是 1.3.1 | 改釘 1.4.0 的位元組與雜湊 | planned |

## 驗收對照

每項檢查的結果記在 [checklist](../../../../checklists/common-configuration.checklist)，本文不重複。

| 行為 | 檢查 | 結果所在 |
|---|---|---|
| 匯入與版本固定（CFG-P05） | 快照雜湊、`-text`、竄改後拒絕、乾淨 checkout | checklist 第 1 項 |
| 預設與載入（CFG-01） | 共同案例 CFG-C01 經 adapter 走真實入口與 host；缺欄位補預設 | checklist 第 2 項 |
| 載入失敗不覆寫（CFG-02） | 破損、`null`、未知版本、讀取失敗；沒有 host 的 KEY；舊存檔當版本 0 | checklist 第 3 項 |
| 通知與隔離（CFG-08） | 改了就存、KEY 隔離、取消訂閱 | checklist 第 4 項 |
| 卸載與延遲完成（CFG-07）原生等價 | host 銷毀後入口不再呼叫它、`Unload` 後重新讀取 | checklist 第 5 項 |
| 實際 backend（CFG-P01） | 真實 PlayerPrefs 保存、重啟、重讀、KEY 隔離 | checklist 第 6 項 |
| 生命週期（CFG-P02） | Play 停止／重開後值仍在（uGUI、UI Toolkit 各一份人工 checklist） | 模組輪通過後開立 |

## 已知偏差

無（尚未驗收）。

## 輪次

| 規範版本 | UX commit | UnityExtension commit | checklist commit | 結論 |
|---|---|---|---|---|
| Configuration 1.4.0〈目標 2〉等級 1 | `bf62034` | 待實作 | 待記錄 | planned |

## 結論

### 第一輪（Configuration 1.4.0〈目標 2〉等級 1「直接存」）

狀態：planned（2026-10-20）。紀錄與 checklist 已建立，所有檢查 not run；實作分支 `feat/ux-unityextension-7-configuration`。範圍原為 1.3.1 的八條服務語意（主線 `8df460f`），同日依第 93 題縮為等級 1。
