# Configuration 驗收紀錄（Yu5h1Lib.Common）

> Task ID: common-configuration
>
> 狀態：planned（2026-10-20，第一輪：Configuration 1.3.1〈目標 2〉保存能力，需求 UX-UNITYEXTENSION-7）。依使用者指示「在 Yu5h1Lib.Common 引用 UX 的 Configuration」，流程見 [referenced-module-acceptance](../../../../../../.agents/skills/referenced-module-acceptance/SKILL.md)。驗收方先建立本紀錄與 checklist 第一輪，所有檢查都還沒跑；原生對照由實作方第一階段補完。

```text
UnityExtension
└─ Yu5h1Lib.Common（實作套件，Packages/common）
   └─ Configuration 保存能力（本紀錄）
      ├─ 體驗規範 ← Yu5h1Lib.UX ModuleSpec/Foundation/Configuration 1.3.1〈目標 2〉（CFG-01～CFG-08、CFG-P01、CFG-P05）
      ├─ 原生層 → 統一入口 Preferences（PreferencesEntry.cs）、host Preferences<T>、DataView、PlayerPrefsStorage
      ├─ 本輪不含 → 保存行為（目標 3）、宣告、Theme、生命週期的人工確認
      └─ 驗證專案 → Yu5h1LibTest（Unity 6000.6），自動檢查在分支的乾淨 worktree 執行
```

## 身分

| 項目 | 內容 |
|---|---|
| 實作套件 | `com.yu5h1.common`（`Packages/common`，Yu5h1Lib.Common）的 Preferences 層：[`PreferencesEntry.cs`](../../Runtime/Data/PreferencesEntry.cs)（統一入口 `Preferences`、`IStorage`、`PlayerPrefsStorage`）、[`Preferences.cs`](../../Runtime/Component/Preferences.cs)（host `Preferences<T>`）、[`DataView.cs`](../../Runtime/Data/DataView.cs) |
| UX 規範 | [UX ModuleSpec/Foundation/Configuration.md](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md) 1.3.1，SHA-256 `63ca52aa2637cdcd2e7d737f117804749d1b2495a6f9898cda01b4b0a1219d6e`（與 UX manifest 相同） |
| UX commit | `68a72d8`（1.3.1）；需求與分工在 `8b3b972` |
| UX 決策 | 第 90 題 `adopt`、第 91 題 `entry-wide`（解析失敗或版本未知的存檔：該 KEY 載入錯誤、不覆寫、阻止保存；統一入口的全部 KEY 都適用）、第 92 題 `split`（目標 2 只含保存能力） |
| DX 基準 | 不適用：Configuration 是基礎層的保存語意，沒有視覺組裝 |
| 套件釘選 | 規劃：快照 `Packages/common/Tests/Editor/UX~/Configuration/Configuration.md`（位元組與 UX 相同，`.gitattributes` 設 `-text`），EditMode `ConfigurationDesignTests` 核對雜湊；由實作方建立 |
| 環境 | Windows 11、Unity 6000.6.0f1 Editor、Yu5h1LibTest |

## 範圍

本輪只驗〈目標 2〉的能力層（[能力與行為的分工](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#能力與行為的分工)），驗的是命令的狀態與回傳值，不需要 UI：

- 八條服務語意（[行為編號與共同案例](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#行為編號與共同案例)）：預設與載入（CFG-01）、載入失敗不覆寫（CFG-02）、保存提交順序（CFG-03）、保存失敗與重試（CFG-04）、已保存未套用（CFG-05）、單一進行中命令（CFG-06）、卸載與延遲完成（CFG-07）、通知與隔離（CFG-08）。
- 平台檢查（[平台驗收與已知界線](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#平台驗收與已知界線)）：實際 backend（CFG-P01：真實 PlayerPrefs 保存、重啟、重讀、KEY 隔離）、匯入與版本固定（CFG-P05：本輪只有規格檔一個釘選對象；manifest 全檔、artifact 雜湊與具體預設 JSON 屬宣告，不在本輪）。

不在本輪：

- 保存行為——草稿及模式（CFG-12）與保存失敗、已保存未套用的顯示半邊——是〈目標 3〉，由各 UI 套件另開輪次。
- 宣告與宣告產生欄位（CFG-09～CFG-11、CFG-14、CFG-P03）、Theme 持久化（CFG-13）。
- 生命週期（CFG-P02：Play 停止／重開後值仍在）：模組輪通過後，依第 89 題在 Yu5h1LibTest demo 的 uGUI 與 UI Toolkit 兩頁各開一份人工 checklist，不進本輪。

## 分工

依 referenced-module-acceptance〈Divide implementation and acceptance〉與 UX-UNITYEXTENSION-7〈分工〉：

| 角色 | 做什麼 | 不做什麼 |
|---|---|---|
| 驗收方（Opus 驗收） | 在共用 checkout 的 `adopt-ux-optionpicker` 線提交本紀錄與 checklist；開實作分支 `feat/ux-unityextension-7-configuration`；之後審查、在分支的乾淨 worktree 跑自動檢查並記入主線 checklist；開人工 checklist；全過後合併、結案 | 不寫實作 |
| 實作方（Sonnet 實作） | 在自己的 worktree 於實作分支引用：第一階段只做釘選、原生對照與缺口清單，缺口依第 80 題寫成 UnityExtension 決策題後暫停；答完才實作與測試；PlayMode 前後依 [introduction〈Adopting a UX module in a package〉](../../../../.agents/introduction.md#adopting-a-ux-module-in-a-package) 改指與還原 Yu5h1LibTest 的 manifest | 不勾 checklist、不改紀錄結果、不合併、不 tag、不切換共用 checkout |

## 原生對照（planned，由實作方第一階段補完）

驗收方建立紀錄時讀原生程式碼看到的現況，列為起點；Unity 入口欄的最終對應、缺口與決策由實作方補上。「現況」不是結果，所有行為都還沒驗。

| 行為 | Unity 入口／host 的對應點 | 現況（2026-10-20 讀碼） | 狀態 |
|---|---|---|---|
| 預設與載入（CFG-01） | 建立＝host 建立或第一次 `Preferences.GetView(KEY)`；load＝`Preferences<T>.current` 第一次存取（`GetView(KEY, out loaded)` → `IStorage.TryLoad`）；預設＝`defaultSetting` | 首次存取才讀取，建立時沒有 I/O；有存檔但缺欄位時補 `defaultSetting` 並寫一次（宿主明定的新增欄位遷移，[偏好設定 D6](../../../../Documentation/偏好設定.md#so-設定來源d6)）；`defaultSetting` 是字串 DataView，沒有 decode，「無效 defaults 拒絕建立」沒有對應點 | planned |
| 載入失敗不覆寫（CFG-02） | `IStorage.TryLoad` → `DataView.TryParseFromJson` | 解析失敗與沒有存檔同樣建立空 DataView，下一次變更即覆寫原存檔（[偏好設定 E3](../../../../Documentation/偏好設定.md#已知問題)）；存檔沒有 version 欄位；非 `entries` 格式走寬鬆拆字串解析，破損 JSON 可能被當成功。第 91 題要求改為該 KEY 載入錯誤、阻止保存、保留原始字串 | planned |
| 保存提交順序（CFG-03） | save＝寫入共用 DataView（`Preferences.Set`、綁定的 port）→ `Changed` → `Preferences.Save(KEY)` → `IStorage.Save` | 值先寫進共用 DataView 才存檔，已接受的值與寫入沒有分離 | planned |
| 保存失敗與重試（CFG-04） | `IStorage.Save` | 回傳 `void`，`PlayerPrefs.Save` 不回報失敗，沒有命令回傳值 | planned |
| 已保存未套用（CFG-05） | 套用＝host `changed` 事件與消費端的訂閱 | 沒有 apply 階段與 apply-error 狀態，沒有 retryApply | planned |
| 單一進行中命令（CFG-06） | 同上各命令 | backend 同步，命令期間沒有可觀察的「進行中」；依完成條件以原生等價檢查記錄（例如在 storage 或觀察者回呼內重入命令），不能略過 | planned |
| 卸載與延遲完成（CFG-07） | host `OnDestroy`（`UnbindAll`、取消 `Changed` 訂閱） | 共用 entry 是靜態的，host 銷毀後仍可 `Preferences.Set`／`Save`；沒有 disposed 狀態。同步 backend 下的「延遲完成」同樣以原生等價檢查記錄 | planned |
| 通知與隔離（CFG-08） | 每 KEY 一個 `Entry` 與 DataView；通知＝`DataView.Changed`、host `changed`（UnityEvent） | 不同 KEY 各自一份 view；觀察者拋例外時是否影響存檔與其他觀察者未查 | planned |
| 實際 backend（CFG-P01） | `PlayerPrefsStorage` | 重啟在 EditMode 需要清空靜態 entry 的等價入口（目前只有 `SubsystemRegistration` 時的私有 `ResetState`） | planned |
| 匯入與版本固定（CFG-P05） | `Tests/Editor/UX~/Configuration/` 快照與 `ConfigurationDesignTests` | 尚未建立 | planned |

## 驗收對照

每項檢查的結果記在 [checklist](../../../../checklists/common-configuration.checklist)，本文不重複。

| 行為 | 檢查 | 結果所在 |
|---|---|---|
| 匯入與版本固定（CFG-P05） | 快照雜湊、`-text`、竄改後拒絕 | checklist 第 1 項 |
| 預設與載入（CFG-01）～通知與隔離（CFG-08） | 共同案例 CFG-C01～CFG-C08 經 adapter 走真實統一入口與 host | checklist 第 2～9 項 |
| 實際 backend（CFG-P01） | 真實 PlayerPrefs 保存、重啟、重讀、KEY 隔離 | checklist 第 10 項 |
| 生命週期（CFG-P02） | Play 停止／重開後值仍在（uGUI、UI Toolkit 各一份人工 checklist） | 模組輪通過後開立 |

## 已知偏差

無（尚未驗收）。

## 輪次

| 規範版本 | UX commit | UnityExtension commit | checklist commit | 結論 |
|---|---|---|---|---|
| Configuration 1.3.1〈目標 2〉 | `68a72d8` | 待實作 | 待記錄 | planned |

## 結論

### 第一輪（Configuration 1.3.1〈目標 2〉保存能力）

狀態：planned（2026-10-20）。紀錄與 checklist 已建立，所有檢查 not run；實作分支 `feat/ux-unityextension-7-configuration`。
