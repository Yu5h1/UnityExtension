# BackgroundLoad（背景讀取）

> Task ID: ux-loading-experience
>
> 狀態：規範草案（2026-10-29 建立）。規範版本：0.1.2-draft；設計資料 0.1.1-draft，案例 0.1.1-draft。

```text
內容在背景準備、使用者繼續操作的介面
├─ 背景讀取（Foundation）
│  ├─ 不擋操作、自主準備、就緒才替換
│  ├─ 非阻塞進度（已知比例／不確定／準備中）
│  └─ 未就緒替代畫面與非關鍵失敗降級
├─ AsyncOperation → 使用者等待中的作業狀態；本規範只引用 ASYNC-PROGRESS、ASYNC-STALE
├─ 宿主 → 決定讀哪些內容、先後順序、替代視覺與內建備用內容
└─ Motion Interpolation → 替換時的淡入（選用，不在本規範定曲線）
平台實作 → 讀取機制（例如 Addressables）與呈現技術；不在本文統一 API
```

背景讀取定義內容在背景準備時，畫面如何讓使用者繼續操作、看得到進度、在未就緒與失敗時顯示什麼。它不是作業狀態本身：使用者正在等的作業由 [AsyncOperation](AsyncOperation.md) 擁有（該草案明確不收預先載入）；本規範擁有「沒在等」的背景準備，以及使用者選到那份內容時畫面如何呈現。歸基礎層：任何會在背景讀進內容的位置（角色、頭像、場景）都共用。

## 目標

### 目標 1：非阻塞背景資源預備與自主內容準備（進行中，2026-10-29 開輪次）

- **為誰服務**：需要在背景讀進大型內容、又不能打斷使用者的介面。實際消費端是 VCP 的角色載入（Unity，uGUI）：`CharacterOption` 在 App 開啟時就讀上次選的角色、其餘依序預讀、讀完才切換；進度來源是 UnityExtension `AddressablesLoader` 的下載比例。方向由使用者 2026-10-24 選定（選項 A：背景資源預備／非阻塞優先），2026-10-29 併入進度呈現與未就緒替代畫面，同日定名與分層。
- **情境**：
  1. 背景讀取進行中，使用者照常操作，焦點不被搶走、不出現擋住畫面的遮罩（不擋操作 BGL-NOBLOCK）。
  2. 宿主宣告優先內容，App 開啟就開始讀；其餘依宣告順序接著讀（自主準備 BGL-PREPARE）。
  3. 讀完的內容只在它是目前選擇時才換上，連續選擇以最後一次為準（就緒才替換 BGL-SWAP）。
  4. 讀取中在角落顯示進度：有比例時顯示百分比，沒有時顯示不確定的等待，下載完但還不能用時顯示「準備中」（非阻塞進度 BGL-PROGRESS）。
  5. 選到還沒準備好的內容時，該位置改顯示替代視覺與進度，不讓先前的內容冒充所選的（未就緒替代畫面 BGL-PLACEHOLDER）。
  6. 選中的內容讀取失敗時，換上內建備用內容並以不擋操作的提示告知，流程繼續（非關鍵失敗降級 BGL-FALLBACK）。
- **不在本目標內**：必要內容讀不到時的阻塞等待（含取消與重試）；多份內容的合計進度（須以去重後的位元組計算，見 [Addressables 套件計畫〈頭像暗幕與進度通知〉](../../../../Unity/UnityExtension/Documentation/Addressables/plan.md#頭像暗幕與進度通知)）；排隊、使用許可等狀態；讀取機制、快取與 API；淡入曲線。
- **完成條件**：VCP 以 uGUI 實作情境 1～6，依本規範的中性案例與人工檢查驗收並記在 VCP 的驗收紀錄；外觀（指示、替代視覺、提示）由使用者在 VCP 確認（第 65 題）。
- **前置條件**：
  - AsyncOperation 0.1.0-draft 沒有目標。依使用者 2026-10-29 的選擇，本目標只引用其中兩條：已知與未知進度（ASYNC-PROGRESS）、舊結果不改新畫面（ASYNC-STALE），由 VCP 這輪一併驗收；AsyncOperation 其他條文不在本目標。
  - VisualFoundation 的色彩角色在 uGUI 尚無結果（UI Toolkit 目標 1 已達成）。指示、替代視覺與提示在 VCP 的色彩是本目標的受阻情境，回報 UX，不由實作端自訂共用色。
  - Motion Interpolation 1.x 已交付；淡入只選用。
- **能力盤點**：角落進度指示、替代視覺、內容替換、失敗提示。目前都只有 VCP 一個消費端，依停止規則留在本規範內；第二個消費端（例如 Addressables 頭像暗幕）出現時再評估是否拆出。

## 來源盤點

靜態閱讀加上一次遠端測試；來源內容雜湊記於 [設計資料](BackgroundLoad/background-load-design.json)。所列是規則的依據，不是本草案在任何平台通過的結果。

| 來源 | 目前可觀察行為／限制 | 用途 |
|---|---|---|
| VCP `Assets/Scripts/CharacterOption.cs`：`Awake`、`Start`、`OnSelected`、`PreloadOthers` | 場景載入就讀上次選的角色，找不到用第一個；選到未讀完的角色時舊角色留在畫面，讀完才切換，最後一次選擇為準；第一次切換後其餘角色一個接一個讀；讀取失敗只記 log | BGL-PREPARE、BGL-SWAP 的來源；舊角色留在畫面是被條件 6 取代的行為 |
| VCP `Assets/Scripts/CharacterContent.cs` | 角色外殼持有 key，同一角色重複讀取會等同一個讀取 | 同一內容共用一次讀取 |
| UnityExtension `AddressablesLoader.cs` | 進度是已下載位元組除以需要下載的總量；不必下載時一開始就是 1；到 1 之後還在從 bundle 載入 | BGL-PROGRESS 的「準備中」依據 |
| UnityExtension `ContentAgent.cs`（`PrepareAsync`） | 同 key 等目前讀取、另一 key 先取消；讀取成功才取代目前內容，失敗保留原內容 | BGL-SWAP 與 ASYNC-STALE 的對照 |
| Yu5h1LibTest `RemoteLoadTests.cs`（2026-10-29 通過） | 281,576 bytes 的下載回報 0 → 0.35 → 0.47 → 0.70 → 0.93 → 1，之後維持 1 直到載入結束；已快取時回報 0、1、1 | 進度單調、準備中確實存在 |
| VCP report `streaming-assets`（使用者 2026-10-29） | 右下角 Loading… 百分比；選到未讀完的角色時顯示替代視覺與百分比、不讓舊角色留在畫面；非必要內容失敗用測試角色代替並提示「此角色異常」 | 條件 4～6 的使用者要求 |

## 狀態語意

一個**內容位置**（例如角色台）顯示一份內容。每份內容有讀取狀態：

| 狀態 | 意義 |
|---|---|
| 未讀 notLoaded | 還沒開始讀 |
| 下載中 downloading | 讀取中、比例 < 1，或比例未知 |
| 準備中 preparing | 下載比例已到 1，內容還不能用 |
| 就緒 ready | 內容可以顯示 |
| 失敗 failed | 本次讀取沒有成功 |

位置顯示的是下列之一：某份就緒的內容、替代視覺（placeholder）、內建備用內容（fallback）、空。名稱是案例詞彙，不規定平台列舉命名。

## 行為

| 行為 ID／短名 | 前提、操作、可觀察結果與邊界 | 依據 | 案例 |
|---|---|---|---|
| BGL-NOBLOCK 不擋操作 | 任何背景讀取進行中、開始或結束時，輸入焦點不移動、不出現擋住操作的遮罩或對話框，宿主的其他操作維持可用。宿主自己的阻塞作業（例如聊天請求的 Loading 面板）不屬本規範 | 使用者 2026-10-24 條件 1 | nonblocking-during-load |
| BGL-PREPARE 自主準備 | 宿主宣告一份優先內容（`preparePolicy.first`）時，不等使用者操作就開始讀；優先內容讀完或使用者第一次選擇之後，其餘內容依宣告順序一次一份接著讀（`preparePolicy.concurrency = 1`）。同一份內容正在讀時再要求，等同一次讀取，不重讀 | VCP `Awake`、`PreloadOthers`、`CharacterContent` | prepare-first-then-rest、same-content-shares-load |
| BGL-SWAP 就緒才替換 | 只有目前選擇的內容就緒時才換上位置；不是目前選擇的內容就緒，位置不變。連續選擇以最後一次為準，較早選擇的讀取結果（成功或失敗）不改位置（引用 ASYNC-STALE） | VCP `OnSelected` 的 `_pending`；ContentAgent | background-ready-no-swap、latest-selection-wins |
| BGL-PROGRESS 非阻塞進度 | 指示跟隨一份讀取：目前選擇的內容尚未就緒時跟隨它，否則跟隨正在背景讀的那份；沒有讀取時隱藏。下載中且比例已知時顯示無條件捨去的整數百分比（比例 1 才會顯示 100%）；比例未知顯示不確定的等待，不用假百分比（引用 ASYNC-PROGRESS）；準備中顯示「準備中」，不顯示 100% 當作可用。讀取開始到第一次回報比例之前視為比例未知。比例在同一次讀取內不倒退。指示位置見 `indicator.placement`，不搶焦點（BGL-NOBLOCK） | AddressablesLoader；RemoteLoadTests；使用者 2026-10-29 | progress-known-percent、progress-unknown、progress-preparing、indicator-hides-when-done、indicator-follows-selected |
| BGL-PLACEHOLDER 未就緒替代畫面 | 選到未就緒的內容時，位置立即改顯示替代視覺與該讀取的進度（同 BGL-PROGRESS 的三種表達），先前的內容離開位置；就緒後換上所選內容。等待中改選另一份已就緒的內容時，位置立即顯示它，替代視覺消失，原讀取晚到的結果不改位置。替代視覺由宿主提供 | 使用者 2026-10-29 條件 6；取代 VCP 舊行為（舊角色留在畫面） | select-not-ready-shows-placeholder、placeholder-replaced-when-ready、reselect-ready-during-wait |
| BGL-FALLBACK 非關鍵失敗降級 | 目前選擇的內容讀取失敗時，位置顯示宿主的內建備用內容（宿主沒有內建內容時，替代視覺本身就是備用內容），並出現不擋操作的提示說明這份內容異常；不出現阻斷式對話框，其他操作維持可用。之後再選同一份內容會重新讀取。未被選擇的背景讀取失敗不改位置、不出提示，只留紀錄。提示保留到下一次選擇或離開，不設自動消失秒數（同 AsyncOperation 的 ASYNC-FEEDBACK） | 使用者 2026-10-21／10-29；Addressables 套件計畫首輪 | selected-fail-fallback、background-fail-silent |

## 設計資料與案例解讀

[設計資料](BackgroundLoad/background-load-design.json) 用 schemaVersion 1 與具名 tokens；`indicator.placement` 的 `bottom-end` 來自使用者 2026-10-29 的「右下角」，是本目標的預設，宿主可覆寫；提示保留到下一次操作（`fallbackNotice.retention`）。

[案例](BackgroundLoad/background-load-cases.json) 沿用 schemaVersion 1 的 cases／initial／steps／action／expect 結構，共 15 個。動作是測試語意：`select`、`progress`（帶 `ratio`，或 `null` 表示未知）、`ready`、`fail`、`appStart`、`request`。expect 只比較列出的欄位；`slot` 是位置顯示的內容（內容鍵、`placeholder`、`fallback` 或 `null`），`indicator` 的 `text` 是 `"34%"` 這類整數百分比、`indeterminate` 或 `preparing`，隱藏時 `visible: false`；`loadsStarted` 是依序開始讀的內容鍵陣列，精確比較。沒有時鐘。

焦點、遮罩、指示位置與外觀要在真實介面檢查：人工檢查 BGL-M1（讀取中照常打字與捲動、焦點不動）、BGL-M2（指示在設定的角落、不擋操作、外觀由使用者確認）、BGL-M3（替代視覺與失敗提示的外觀由使用者確認）。

## 引用

| 規範 | 本規範引用的部分 | 不引用 |
|---|---|---|
| AsyncOperation 0.1.0-draft | ASYNC-PROGRESS（已知／未知進度、終點不代替成功）、ASYNC-STALE（舊結果不改新畫面） | 其他條文；作業狀態四態只在使用者等待時適用，本規範的讀取狀態另列於上 |
| VisualFoundation | 指示、替代視覺與提示使用既有色彩角色（目前 uGUI 受阻） | 不新增 token |
| Motion Interpolation | 替換時的淡入選用 | 曲線與時長 |

## 待決

- 一般介面（非角色台）是否也要在「使用者沒選、只是預讀」時顯示指示：VCP 要；第二個消費端出現時再核對。

## 版本與交付

- 0.1.2-draft（2026-10-29）：案例 0.1.1-draft 修正 progress-preparing、placeholder-replaced-when-ready 與 BGL-PROGRESS 的矛盾——原本有三份內容，選中的就緒後其餘接著背景讀，指示依規則應跟隨背景讀取而不是隱藏；改為只有一份內容。由 UnityExtension `ContentSlot` 跑案例時發現，條文不變。
- 0.1.1-draft（2026-10-29）：使用者決定失敗提示保留到下一次操作；宿主沒有內建內容時以替代視覺作備用內容（VCP 本體沒有內建角色）。案例位元組不變。
- 0.1.0-draft（2026-10-29）：首版草案，目標 1 進行中。未登錄任何平台結果；來源基準未執行——條件 4～6 在來源（VCP）還沒有實作，基準即 VCP 本目標的驗收。
- 實作端以 spec-adoption 引用：VCP 釘選本版的規範、設計資料與案例雜湊（[manifest](../manifest.json)）。
