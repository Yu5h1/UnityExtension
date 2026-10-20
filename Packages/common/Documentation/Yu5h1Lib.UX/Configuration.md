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
| 套件釘選 | 快照 `Packages/common/Tests/Editor/UX~/Configuration/Configuration.md`（位元組與 UX 相同，`.gitattributes` 設 `-text`）與 EditMode [`ConfigurationDesignTests`](../../Tests/Editor/ConfigurationDesignTests.cs)（核對 SHA-256 與主版本 1）；實作 commit `2ee96de` |
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

## 原生對照（planned，實作方第一階段讀碼）

實作方 2026-10-20 逐行讀 [`PreferencesEntry.cs`](../../Runtime/Data/PreferencesEntry.cs)、[`Preferences.cs`](../../Runtime/Component/Preferences.cs)、[`DataView.cs`](../../Runtime/Data/DataView.cs)、[`PlayerPreferences.cs`](../../Runtime/Component/PlayerPreferences.cs) 的結果。「現況」是讀碼推得，**沒有實測**（本輪 Unity Editor 沒開，無法探測 `JsonUtility` 的實際行為）；所有行為還沒驗，狀態一律 planned。分類：已成立＝原生行為符合規格；缺口＝需新增或修改；宿主明定＝已記錄的設計決定，不算缺口。缺口裡需要選設計的，已寫成[決策題](../../../../decisions/cross-platform-ui-parity.decisions)（共七題，全部待答）；答案出來前不實作。

| 行為 | Unity 入口／host 的對應點 | 現況（讀碼） | 分類與去向 | 狀態 |
|---|---|---|---|---|
| [預設與載入（CFG-01）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#行為編號與共同案例) | 建立＝host 建立（`Awake`，只建立 `defaultSetting`，沒有 I/O）；load＝`Preferences<T>.current` 第一次存取（`Start` 的 `BindAll` 觸發）→ `GetEntry` → `IStorage.TryLoad` | 首次存取才讀，建立與訂閱前沒有 I/O。無存檔：view 為空、併入 `defaultSetting`，**不寫盤**（`loaded && merged` 才寫）。有存檔缺欄位：補預設並寫一次，是[宿主明定的新增欄位遷移（D6）](../../../../Documentation/偏好設定.md#so-設定來源d6)。有綁定控件時，`BindPort` 對沒有預設的欄位用控件值補並立即寫盤（設計決策「補上的預設值即落盤」）。`defaultSetting` 與存檔都是字串 DataView，沒有 decode | 已成立：首次讀、有／無存檔、缺欄位遷移（宿主明定）。缺口：「無效 defaults 拒絕建立」無對應點 → [決策題 decode 驗證](../../../../decisions/cross-platform-ui-parity.decisions)。無存檔且有綁定控件時首次綁定會寫盤，與案例「零 write」的前提不同，驗收用不含綁定的 host 驗案例，綁定路徑另記 | planned |
| [載入失敗不覆寫（CFG-02）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#行為編號與共同案例) | `IStorage.TryLoad` → `DataView.TryParseFromJson` | `PlayerPrefsStorage.TryLoad` 把「沒有 KEY」與「有 KEY 但解析失敗」都回 false；入口建立空 view 並併入預設，**下一次變更即覆寫原存檔**（E3 現行寫法即此）。存檔沒有 version 欄位。`TryParseFromJson`：空字串回 false（等同無存檔）；`JsonUtility` 沒丟例外且原文含「entries」就算成功（即使解出空資料，或「entries」只是值的內容）；否則走以逗號切割的舊路徑，切出一筆以上就算成功。破損 JSON、`null`、其他物件格式是否被誤判成功，推導有可能，**待實測**；`null` 若讓 `FromJson` 回 null，後面的 `CopyFrom` 會丟 NullReferenceException 穿出入口 | 缺口（第 91 題 entry-wide，統一入口全部 KEY）：載入錯誤狀態、保留原始字串、阻止保存、Reload 恢復；需先定義「損毀」與版本欄位 → [決策題 存檔格式](../../../../decisions/cross-platform-ui-parity.decisions)、[決策題 載入錯誤與恢復](../../../../decisions/cross-platform-ui-parity.decisions) | planned |
| [保存提交順序（CFG-03）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#行為編號與共同案例) | 控件寫入或 `Preferences.Set` → DataView setter → `Changed` → 入口訂閱者 `Preferences.Save(KEY)` → `IStorage.Save` | 值先寫進共用 DataView，`Changed` 才觸發存檔；已接受的值與被寫入的是同一個物件，沒有 saving 階段；`GetView` 把同一個可變物件交給每個呼叫端，沒有獨立快照 | 缺口 → [決策題 已接受值與寫入](../../../../decisions/cross-platform-ui-parity.decisions) | planned |
| [保存失敗與重試（CFG-04）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#行為編號與共同案例) | `IStorage.Save`、`PlayerPrefsStorage.Save`、host `SaveToPlayerPrefs` | `IStorage.Save` 回傳 void；`PlayerPrefs.Save` 不回報失敗；`SetString` 若丟例外（容量不足等）會穿過 setter 傳給寫入端，值已改。沒有命令回傳值、沒有重試命令（下一次任何變更才會再寫）。沒有 decode，「無效值不寫入」無對應點 | 缺口 → [決策題 失敗偵測](../../../../decisions/cross-platform-ui-parity.decisions)、[決策題 已接受值與寫入](../../../../decisions/cross-platform-ui-parity.decisions)、[決策題 decode 驗證](../../../../decisions/cross-platform-ui-parity.decisions) | planned |
| [已保存未套用（CFG-05）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#行為編號與共同案例) | 套用＝host `changed`（UnityEvent）與消費端對控件事件的訂閱 | 沒有 apply 階段、apply-error 狀態與 retryApply；`changed` 在入口存檔之後觸發（入口先訂閱 DataView.Changed），但套用成敗不回報。UnityEvent 監聽者丟例外是否中斷其他監聽者，待實測 | 缺口 → [決策題 套用階段](../../../../decisions/cross-platform-ui-parity.decisions) | planned |
| [單一進行中命令（CFG-06）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#行為編號與共同案例) | 同上各命令 | backend 同步，沒有可觀察的進行中；入口沒有命令鎖，`Changed` 觀察者可以在存檔期間重入 `Preferences.Set`／`Save`。依完成條件以原生等價檢查記錄（在 storage 與觀察者回呼內重入命令），不能略過 | 缺口（新命令的重入拒絕）→ 併入 [決策題 已接受值與寫入](../../../../decisions/cross-platform-ui-parity.decisions) 的命令定義 | planned |
| [卸載與延遲完成（CFG-07）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#行為編號與共同案例) | host `OnDestroy`（`UnbindAll`、取消 `Changed` 訂閱）；入口的 KEY | 入口靜態、比 host 活得久；host 註冊的 `HostStorage` 在 host 銷毀後仍留在靜態表裡並持有已銷毀的 host，之後 `Preferences.Save(KEY)` 還會呼叫它的 `SaveToPlayerPrefs`；沒有 disposed 狀態。「服務實例」是 host 還是 KEY 未定 | 缺口 → [決策題 服務實例](../../../../decisions/cross-platform-ui-parity.decisions)；同步 backend 的延遲完成以原生等價檢查記錄 | planned |
| [通知與隔離（CFG-08）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#行為編號與共同案例) | 每 KEY 一個 `Entry` 與 DataView；通知＝`DataView.Changed`、host `changed` | 不同 KEY 各自一份 view，且存檔時只寫該 KEY（已成立，待測）。入口是 `Changed` 的第一個訂閱者，所以存檔先於其他觀察者；直接 C# 訂閱者丟例外會中斷後面的訂閱者並傳給寫入端（值與存檔已完成）；host `changed` 是 UnityEvent，待實測。取消訂閱可行（`-=`）。「不同 listener 得到獨立快照」不成立：全部拿到同一個物件 | 部分成立；觀察者例外隔離與獨立快照屬缺口 → 併入 [決策題 已接受值與寫入](../../../../decisions/cross-platform-ui-parity.decisions)（快照）；例外隔離於實作時決定並測試 | planned |
| [實際 backend（CFG-P01）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#平台驗收與已知界線) | `PlayerPrefsStorage`、真實 `PlayerPrefs` | 提交點＝`PlayerPrefs.Save()`（`SetString` 只進記憶體）；無記錄表示＝`HasKey` 為 false。「重啟」在 EditMode 需要清空入口靜態狀態的等價入口，目前只有 `SubsystemRegistration` 時的私有 `ResetState`；`ClearPlayerPrefs` 只刪 PlayerPrefs 的 KEY，不重設入口裡已載入的 Entry | 缺口（重啟入口）→ [決策題 服務實例](../../../../decisions/cross-platform-ui-parity.decisions) 的 `Unload`；ClearPlayerPrefs 一併見 [決策題 載入錯誤與恢復](../../../../decisions/cross-platform-ui-parity.decisions) | planned |
| [匯入與版本固定（CFG-P05）](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md#平台驗收與已知界線) | `Tests/Editor/UX~/Configuration/Configuration.md`、`ConfigurationDesignTests` | 已建立：快照位元組與 UX 相同，`.gitattributes` 設 `-text`，EditMode 測試核對 SHA-256 與主版本 | 已成立（實作 commit `2ee96de`）；測試尚未在 Unity 執行（本輪 Editor 未開），結果由驗收方在乾淨 worktree 記錄 | planned |

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
