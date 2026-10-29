# Addressables 套件：內容按需下載與載入

> Task ID: addressables-package

首輪先做讀取處理器與代理，用 Resources 讀取器驗證，再接 Addressables 讀取器、以 VCP 的角色用 local group（StreamingAssets）驗證（[首輪範圍](#首輪範圍讀取處理器代理與角色載入)）；之後才做遠端內容測試，Git LFS 素材分離與 Windows 頭像原型排在遠端測試之後（2026-10-21 定案）。本文是討論稿；單一遠端流程與初始角色優先是使用者指定方向，套件位置已定（[套件位置與依賴](#套件位置與依賴)），元件名稱及下方標示的建議仍待收斂。進度由同目錄的 [handoff.md](handoff.md) 記錄。

## 系統階層與關係

```text
Yu5h1Lib / UnityExtension
└─ com.yu5h1.addressables：內容按需下載與載入
   ├─ 應用流程：決定現在與下一頁需要什麼
   ├─ 使用許可：由應用提供可使用的角色／場景清單
   ├─ 載入協調：排程、共用請求、進度、重試與釋放
   ├─ 畫面呈現：頭像暗幕、等待提示與可操作狀態
   └─ Unity Addressables：下載內容包、快取、載入資源
      └─ HTTP 主機（R2、GitHub Pages 等，由消費端決定）：提供遠端內容

獨立素材 repo（大型檔案用 Git LFS）
   → 固定版本取入 Unity 建置專案
   → Addressables 建置 → R2 發布
   → 開發中的 App 與正式 App 使用相同載入流程
```

## 目的與固定方向

大型角色貼圖、模型及場景美術從應用專案 repo 分離，保留可重現的素材版本。使用者開啟 App 後，只下載目前頁面與下一步實際需要的資源，畫面持續回應操作。

開發與正式版本共用一套 Addressables 遠端流程。開發只改用測試發布位置，正式版本使用正式發布位置；程式、狀態與測試方法一致。日常驗證使用 `Use Existing Build` 或實際 Player，不把 `Use Asset Database` 當成本計畫的驗收路徑。

這裡的「流式」指按需下載、提早準備、逐項開放使用。一般 Addressables 以內容包為下載單位，不能承諾同一張貼圖下載 30% 就能顯示 30%；需要的 bundle 與相依資源備妥後才能載入使用。

第一輪以 VCP 的 WebGL Player 驗證（2026-10-21 定案）。Windows Player 與手機是後續平台驗證；各平台的網路、快取與非同步限制不同，結果不能互相沿用。

## 術語對照

| 名稱 | 本計畫中的意思 |
|---|---|
| Git LFS | 保存大型原始素材的版本；只給開發與建置端使用 |
| Address／Key | 程式用來找資源的名字，例如 `characters/knight/content` |
| Catalog | 記錄資源名字與下載位置的目錄 |
| Bundle（內容包） | Unity 建置後實際下載的一包資源 |
| 預載 | 在使用前，先下載到裝置快取；需要立即顯示的小圖還要載入記憶體 |
| 快取 | 已下載的內容留在裝置，下次相同版本可以重用 |
| Ready（可使用） | 下載成功且資源已載入／完成必要準備；不是只有下載到 100% |

## 素材與專案如何分離

採用一個獨立素材 repo，透過 submodule 固定在應用 Unity 專案的 `Assets/RemoteContentSource`。大型二進位素材由該 repo 的 LFS 管理，Prefab、場景、Material 與 `.meta` 同樣留在素材 repo 版本管理。主 repo 只記錄 submodule 的 commit 與自身程式、設定。

這個目錄是提案中的新整合位置，不代表目前已有對應 repo 或資料夾。素材根目錄的 `.meta` 由主 repo 保留，內部素材的 `.meta` 由素材 repo 保留；搬移時維持 GUID。Addressables 群組設定由應用建置專案管理，以素材 GUID 引用素材。

Git LFS 第一階段操作順序：

1. 選定測試專案及有使用權的 A、B、C 頭像，先確認對應 `.meta`，記錄 GUID。
2. 在獨立素材 repo 安裝／啟用 LFS，為實際大型圖片副檔名設定追蹤；提交 `.gitattributes`，`.meta` 保持一般 Git 文字檔。
3. 提交素材及 `.meta` 並推送，確認遠端 LFS 本體已上傳，不能只看 Git commit 成功。
4. 主專案以 submodule 固定素材 commit。素材 repo 先提交／推送，主 repo 再提交新的指標；主 repo 不直接追蹤其中的大型素材。
5. 用另一個乾淨 checkout 初始化 submodule，並在素材 repo 取得 LFS 本體，確認圖片不是指標文字、GUID 未變、Unity 引用正常。保存這輪驗證紀錄後再開始遠端建置。

LFS 階段只證明開發輸入可重現，不能算完成 App 的遠端下載測試；玩家永遠不透過 LFS 取素材。

內容中的 MonoBehaviour 所需程式必須隨 Player 編譯；素材 repo 固定版本須與程式相容。第一次先讓建置與驗證機取得完整素材，證實遠端載入；不在此階段加入「完全不下載原始素材也能編輯專案」的另一條流程。

```text
修改素材 → 素材 repo commit / push（含 LFS）
        → 主 repo 更新素材 commit 指標
        → Unity 重新建置 Addressables
        → 上傳測試 R2
        → 開發 App 從 HTTP 下載並驗證
        → 同一份已驗證發布產物推廣到正式環境
```

打包網址若依環境不同，推廣前必須確認 catalog 的位置與內部 URL；不能把帶有測試 URL 的目錄直接當正式目錄使用。首次流程手動完成，再將相同步驟做成 GitHub Actions。Actions 是自動化，不是玩家素材主機。

## 最小實例與內容切分

首輪原型使用三個角色 A、B、C：A 為初始角色，帳號可用 A、B，不可用 C。先用頭像作為實際下載資源，不要求先做模型、動畫或場景系統。A 的頭像在首頁準備階段先下載並載入；B 在首頁結束動作提出需求後下載；C 不下載。模型與兩個場景是後續延伸驗證。

| 內容 | 下載／載入時機 | 分包原則 |
|---|---|---|
| 啟動頁、基本字型、錯誤與重試 UI | 隨 App 安裝 | 最小啟動殼，避免等待畫面依賴自己的下載 |
| 主畫面 | 啟動後優先下載並載入 | 獨立於角色大型內容 |
| A 的頭像 | 許可取得後，在首頁準備階段優先下載並載入 | 與 B、C 獨立分包 |
| B 的頭像 | 首頁結束動作提出選單需求後，背景下載 | 首輪以此驗證非阻塞進度 |
| A、B 的模型、高解析貼圖、動畫 | 後續階段加入；依使用需求下載 | 各角色有獨立內容邊界，與頭像分開 |
| C 的素材 | 不排入下載 | 原型以本機通用鎖定占位圖顯示 |
| 下一場景 | 流程確定且具有許可後，先下載依賴 | 各場景分包，進入時才載入場景 |
| 共用 shader／材質等 | 由被請求內容的相依性帶入 | 只放真正共用內容，避免混入 C 的專屬資源 |

初版按角色與場景建立明確群組，避免全部角色 `Pack Together`。頭像圖集也不能把未授權角色圖片打進會預載的同一張圖集。Label 是查詢分類，並不自動保證隔離；最終以 Build Layout 與實際下載請求證明分包結果。[1]

原型的許可資料由測試帳號資料提供，正式應用由其可信任的帳號服務提供。先完成許可判斷，再產生下載清單；不要先下載全部再把卡片藏起來。這項篩選節省下載量，並不是檔案存取保護。若要求未授權者連 URL 都不能取得內容，需另外設計伺服器端授權；本計畫第一輪不實作 DRM 或付費系統。

## App 的單一流程

```text
App 開啟
  → 顯示內建 Loading 殼（離線也能顯示錯誤與重試）
  → 初始化 Addressables、確定相容 catalog、取得使用許可
  → 下載／載入首頁自訂清單 + 初始角色 A 頭像
  → 顯示首頁
  → 首頁結束 UI 動作，例如按「開始」或動畫結束
  → 資源需求元件提出「角色選單需要的內容」
  → 篩選許可：A、B 可用；C 排除
  → 檢查本次 catalog 對應內容是否已可用
      ├─ 已可用：直接回報，不重播假的下載動畫
      └─ 缺少內容：加入共用非同步下載工作
  → 開啟選單：A 立即顯示，B 以占位卡片呈現下載進度
  → B 下載完成 → 載入 Sprite → 換成真正頭像 → 可選取
```

首頁可以設定自己的必需素材清單；啟動流程等待這些素材及 A 頭像準備完成，才切入首頁。Loading 殼本身內建；若要有可下載的 Loading 美術，也必須先用內建殼顯示其下載狀態，避免循環依賴。

首頁結束動作只發出一次資源需求，應用決定頁面何時切換。建議首輪直接打開角色選單，讓 A 可用、B 背景完成，以驗證遊戲畫面內的進度。其他場合若下一步必須等素材，則等待同一份需求的 Ready 通知再繼續，不另寫一個「阻塞版下載器」。重複點擊應合併內容工作，且每次導頁操作最多繼續一次。

初始角色是帳號給出的角色 ID，不把 A 寫死在下載系統。若許可查詢失敗，保留 Loading／重試，不自動下載所有角色；若初始角色不在許可清單，交由應用選擇合法預設角色或提示錯誤。

先以少量並行工作驗證：目前頁面必需內容優先，明確選取其次，推測下一步最後。尚未開始的工作可調整順序；第一輪不承諾搶占已在下載的請求。

## 可在不同場合觸發的元件

首輪定案的元件是讀取處理器與代理（[首輪範圍](#首輪範圍讀取處理器代理與角色載入)）。本節的「遠端資源需求元件」是 2026-09-09 為頭像原型寫的提案，做頭像原型時再和代理對齊。

討論名稱採 `RemoteContentHandler`，中文稱「遠端資源需求元件」。按鈕、頁面開啟、動畫完成或程式呼叫，都可進入同一個「檢查並準備」入口。元件負責提交需求與轉發通知，下載協調服務負責工作合併及 handle 生命週期。

| 責任 | 建議擁有者 | 不應混入的責任 |
|---|---|---|
| 這個 UI 動作需要哪些資源 | 應用流程及需求清單 | CDN 路徑與下載實作 |
| 使用者能使用哪些角色 | 應用的許可來源 | 以快取存在推定擁有權 |
| 觸發檢查與訂閱結果 | RemoteContentHandler | 每個元件各自刷新 catalog、另建下載器 |
| 確定 catalog、查詢快取、共用下載與載入 | 同一個遠端資源服務 | 首頁順序、角色 ID、商店規則 |
| 暗幕、進度條、世界中的視覺變化 | 進度呈現元件 | 判斷快取和擁有權 |

以下是責任草圖，不是已定案的 C# API：

```text
遠端資源需求元件
  輸入：內容清單、需求目標（下載備妥／載入可用）
  操作：檢查並準備、重試、解除這個使用者的需求
  輸出：狀態變更、下載進度、目標達成、失敗

檢查並準備
  → 向應用取得已通過許可的內容清單
  → 等待共用服務完成 catalog 初始化
  → 加入既有工作，或建立缺少的內容工作
  → 查詢本版本缺少的下載量
  → 需要時下載，依需求目標決定是否接著載入
  → 通知呈現元件，成功才觸發後續動作
```

「下載備妥」與「載入可用」是同一條工作的不同完成目標：前者適合背景預載模型，後者適合立即顯示頭像。回傳狀態要明確區分 Downloaded 與 Ready，不能用沒有語意的完成事件讓呼叫者猜。

你提出的「更新器」建議命名為「進度呈現器」，避免和素材版本更新混淆。首輪採狀態／進度通知接到呈現元件；同一工作可同時被 Loading 頁和頭像卡片觀察。若既有 Yu5h1Lib 綁定足以完成，就重用，不先增加另一套 UI 框架或 Update 輪詢。

目前可見 `Packages/common/Runtime/Event/MonoEventHandler.cs` 已有 Enabled／Disabled 事件入口，可作整合調查起點；這只證明已有觸發機制，不代表已確認所有事件型別適合本次進度資料。實作前盤點現有事件與 UI 綁定，再決定如何接通知。

## 本機已有素材如何判斷

不把 `bool 本地是否有常駐的下載資源` 存成可編輯的真相。這個值會因 catalog 換版、快取清除或載入釋放而失效。元件應取得帶有內容與 catalog 識別的即時狀態：

| 狀態 | 含義 |
|---|---|
| 尚未檢查 | 初始化或查詢尚未完成，不能當成缺檔或已存在 |
| 需要下載 | 本次 catalog 所指內容或相依包不完整 |
| 已下載 | 本次需要的 bundle 可用，可能在安裝內容或磁碟快取中 |
| 已載入 | 此使用者持有可顯示的資源，例如 Sprite |
| 無許可／失敗 | 另列原因，不用 false 混成一種結果 |

「帳號有使用許可」「磁碟有相容內容」「記憶體中可用」是三件不同的事。磁碟快取跨啟動可重用，但不是永久常駐保證；記憶體保留則由使用者的持有期間決定。

共用服務在 App 安全入口確認 catalog，工作使用固定的 catalog 世代；Handler 只檢查這個世代的需求，不每點一次按鈕就刷新遠端目錄。更新後重新解析 key 與缺少的下載量，不用自建的角色版號 bool 取代 Addressables 的內容識別。`GetDownloadSizeAsync` 成功且 key 有效時回報零，代表此需求不用再下載，不代表已取得 Sprite。[2][6]

若卡片和另一頁同時使用 B，服務共用 B 的工作；各自解除需求，最後一個使用者離開才釋放載入結果。當請求仍在下載、使用者已離開時，完成回呼不可再操作被銷毀的 UI，也不能留下沒有擁有者的 handle。

## 套件位置與依賴

已定（2026-10-21）：

| 項目 | 設定 |
|---|---|
| 位置 | `Packages/Addressables`（骨架 2026-10-21 建立：package.json、README、Runtime asmdef `Yu5h1Lib.Addressables`、固定 GUID 的 `.meta`） |
| 套件 | `com.yu5h1.addressables`，顯示名稱 `Yu5h1Lib.Addressables` |
| 相依 | `com.yu5h1.common`、`com.unity.addressables` 2.8.1 以上（直接相依，不用 versionDefines）。2.8.1 是最舊支援的 Unity 6000.3.9f1 隨附的版本；6000.6.0f1 隨附 2.11.2 |
| Namespace | `Yu5h1Lib.AddressableAssets`。不用 `Yu5h1Lib.Addressables`：它會在套件內遮蔽 `UnityEngine.AddressableAssets.Addressables` 這個型別（[UnityExtension 命名規則](../../.agents/introduction.md)） |

```text
應用頁面與角色資料
  → com.yu5h1.addressables
      → Unity Addressables → HTTP（R2、GitHub Pages 等，由消費端決定）

套件內部
  Runtime：讀取處理器、代理、Addressables 讀取器、狀態通知與資源持有（工作物件與讀取器基底在 common）
  Editor：必要的內容設定檢查、建置後發佈（實例證明需要後才加入）
  UI 呈現：接收 Runtime 通知，依賴單向流向 Runtime
```

分類理由：

- UnityExtension 是 Unity 的擴充，套件以它擴充的 Unity 功能命名（InputSystem、Timeline、URP、UI、UIToolkit）。
  套件是大分類，不是一個功能一個套件。
- 以程式處理的對象分類，不以傳輸方式分類。Addressables 的內容可以在本機也可以在遠端，所以不放進網路類套件；
  `AsyncTaskObject`、`TaskEvent` 這類非同步工作的通用模式也不只用在網路上，留在 common。
- 網路通訊（從 VCP `ChatRequest` 抽出的 API 請求、WebSocket）是另一個大分類，暫名 Network，
  要抽 ChatRequest 時才建立，不先建空套件。本套件不依賴它：下載與改寫網址由 Addressables 自己處理。
- 套件不叫 Cloudflare 或 CDN：執行期只透過 HTTP 取得內容，不綁定主機。

讀取器（2026-10-21 定案）：

- 讀取器是基底類別，讀取處理器可以替換讀取器。
- **基底與 Resources 讀取器在 common**：它們不需要 Addressables。Resources 讀取器用來先驗證讀取處理器與代理，
  之後也負責載入內建在本體的備援內容（例如角色讀取失敗時的測試角色）。Resources 的內容會打包進本體，
  所以它不能用來把角色從本體分出去。
- **Addressables 讀取器在本套件**。不另寫 StreamingAssets 讀取器：StreamingAssets 只能放原始檔，角色要先打包成
  AssetBundle，這正是 Addressables local group 做的事。本機或遠端由 Addressables 的 profile（load path）決定，
  執行期可用 `InternalIdTransformFunc` 改寫網址，讀取器的程式不分 local 或 url。

UGUI 呈現可放同一套件的獨立 assembly，避免下載 Runtime 反過來依賴 UI；assembly 名稱在建立套件時訂定。

「程式 package」與「下載 bundle」分開：前者隨 App 編譯，後者由 Addressables 建置後放 R2，再交 Unity 快取管理。第一輪不另設玩家下載資料夾或自行搬動快取檔案。大型原始素材仍在獨立 LFS repo，不放進這個工具套件。

## 第一個消費端：VCP（WebGL）

VCP 把角色改為執行期下載（[VCP 流式資源下載](file:///W:/UnityProject/Virtual-Chat-Partner/Documentation/流式資源下載.md)，
Task ID `streaming-assets`），在本套件內開發、以消費端身分引用。VCP 的需求帶來以下對所有消費端都成立的事實與設計：

- **WebGL 的 StreamingAssets 不在 `.data` 內**：它放在 `Build/` 旁，執行期由瀏覽器以 HTTP 另外抓取。
  local group 建到 StreamingAssets，在 WebGL 上就已經是本體之外的執行期下載；頁面與內容放在同一站台時沒有跨網域與 CORS 問題。
- **場景序列化的邊界**：場景（inline SO、UnityEvent）引用不到執行期才出現的物件。場景事件一律指向常駐場景的代理，
  由代理在程式中轉發給載入的實例；讀取處理器代理就是這個代理。
- **非同步 API**：核心 API 用 `Awaitable`＋`IProgress<float>`＋`CancellationToken`，不依賴 UniTask；
  先不做可替換的非同步處理器，留的路見 [非同步工作](../非同步工作.md#已決定)。
  本機 Unity 專案都是 6000.x（2026-10-21 檢查）。WebGL 沒有執行緒，不用 `Awaitable.BackgroundThreadAsync`；
  `Awaitable` 實例會回收重用，同一個實例不能 await 兩次。場景一側仍是事件，UnityEvent 不能 await。
- **進度與事件**：讀取請求是 common 的 `AsyncProgressObject`（`AsyncTaskObject` 加 `Progress<float>`），事件設計由
  [非同步工作](../非同步工作.md) 擁有。
- **UX 的邊界**：載入體驗（進度呈現、失敗與重試、能否取消、未就緒的佔位）屬於 Yu5h1Lib.UX，平台中立；
  本套件是它的 Unity 實作。使用者 2026-10-21 對載入體驗的要求（擋住與不擋住操作兩種呈現、預設不擋、失敗處理）
  交給 UX 定目標，見 UX 決策第 98 題。

## 首輪範圍：讀取處理器、代理與角色載入

目標：把角色當作執行期讀取的資源，和遊戲本體資源分開；用非同步處理解決大型檔案讀取太久造成的等待與流量負擔。
2026-10-21 定案的元件、順序與範圍：

- **讀取處理器**：singleton，可替換讀取器（[讀取器](#套件位置與依賴)）。
- **讀取處理器代理**：場景裡的一般 GameObject，不是 singleton。它提供通知讀取的方法（例如 `Prepare(path)`），
  持有讀取請求（`AsyncProgressObject[]`）；通知讀取本身就是一個請求。載入的內容要不要釋放，由代理定義。
- **分批執行**：不需要特別的技術。初始化、按鈕、捲動、圖片進入畫面等任何事件都能呼叫代理，所以讀取時機可以自由設定。
  例如啟動時一定讀取第一號角色：在代理同一個 GameObject 上加 `MonoEventHandler`，在它的 Start 事件呼叫代理，代理再通知處理器讀取。
- **替換讀取器是讀取處理器上的 MonoBehaviour 欄位**（2026-10-23 使用者定）：讀取器基底是 MonoBehaviour，處理器以序列化欄位引用一個讀取器；
  換讀取器就是換欄位裡的元件。欄位是空的時候用 Resources 讀取器。依據：處理器沿用 `SingletonBehaviour<T>`，它先找場景裡的實例，
  再從 Resources 裡同名的 prefab 產生，最後才新建；所以處理器與讀取器可以放在場景或 Resources 的 prefab 上設定，新建出來的處理器沒有欄位值，要有預設。
- **初始化器是基底類別，消費端繼承**（2026-10-23 使用者定）：套件提供初始化器基底（MonoBehaviour，抽象方法收到載入的實例與 key），
  VCP 繼承它、在裡面添加自己的元件並套用角色專屬資料；代理以欄位引用初始化器。用基底類別而不是 C# 介面：
  Unity 的 Inspector 不能序列化介面型別的欄位。初始化器放在代理上而不是處理器上：處理器是全域唯一，之後也會讀取角色以外的內容（場景、音效），
  放在處理器上會讓每一種內容都套用角色的初始化；代理是場景裡、按內容種類放的物件，與〈第一個消費端〉的「場景事件一律指向代理」一致。
- **角色內容含大頭貼**（2026-10-27 使用者）：角色的下載內容除了模型 prefab 與 Animator Override Controller，還有大頭貼圖片。
  套件以圖片代理讀取 Sprite（和讀取角色的代理共用讀取、取消與釋放規則），由場景把讀到的圖片接到 UI；
  Yu5h1LibTest 的範例角色已加上大頭貼（見 [loader-handler](../../.agents/skills/loader-handler.md#scene-wiring-example)）。
- **角色專屬資料**（2026-10-23 定案）：prefab 與 override controller 以外的資料（口型 blendshape 對照、表情覆寫、鏡頭偏移、選單用 `DataView`）
  放在主程式內一份以 key 對應的角色資料，由消費端的初始化器套用。角色只交 prefab 與 override controller，這些資料要有來處；
  選單資料在下載前就要能顯示。
- **驗證場所**（2026-10-23 定案）：VCP 還在等美術交貨，先在 Yu5h1LibTest 用一個測試角色驗證讀取處理器、代理與初始化器，VCP 交貨後再接 Cat。
  交貨前在 VCP 安裝 Addressables、把 Cat 移進群組，會改變交貨建置的內容。

順序：

1. **讀取處理器與代理**，用 Resources 讀取器驗證。只依賴 [非同步工作 § 首輪](../非同步工作.md#首輪讀取角色)（工作物件與預設判斷），不等 ChatRequest 優化。
   實作：common 的 `AssetLoader`／`ResourcesLoader` 與 `MonoEventHandler` 的 Start 事件，本套件的 `LoaderHandler`、`LoaderAgent`、`InstanceLoad`、`InstanceInitializer`；
   Yu5h1LibTest 測試資料夾 `Assets/AddressablesTest`、場景 `Assets/AddressablesTest/AddressablesTest.unity`。用法見 [loader-handler](../../.agents/skills/loader-handler.md)，進度與驗收見 [loader-handler 驗收清單](../../checklists/loader-handler.checklist)。
2. **Addressables 讀取器**，用 local group（建到 StreamingAssets）驗證。
   實作：本套件的 `AddressablesLoader`；Yu5h1LibTest 的 local group `AddressablesTestCharacters`（address `characters/test-character`，Use Existing Build）與測試用 IMGUI 監看。用法見 [loader-handler](../../.agents/skills/loader-handler.md)，進度與驗收見 [addressables-loader 驗收清單](../../checklists/addressables-loader.checklist)。
3. **角色只準備 prefab 與 Animator Override Controller**。其餘元件（VCP 的 AudioSource、vcpAnimationController、
   Character、uLipSync 等）不放進 prefab，由消費端的初始化流程在載入後自動添加。套件只提供掛接點，不認識消費端的元件；
   非同步表演用 [非同步工作](../非同步工作.md) 的事件物件掛接。
   實作：第 1 步的初始化器基底與讀取請求的事件就是掛接點；Yu5h1LibTest 的測試角色只有 prefab＋override controller，
   `TestCharacterInitializer` 在載入後添加元件，監看畫面接讀取請求的事件。VCP 的初始化器屬於 VCP（`streaming-assets`）。
4. 以上完成後，才開始遠端內容測試，依序：
   - **本機遠端**（2026-10-24 使用者同意）：另一個 port 的 LocalWebService（`127.0.0.1:8002`）只提供 Yu5h1LibTest 的
     `ServerData`，remote group 的 load path 指向它，Editor 以 Use Existing Build 走真的 HTTP。證明下載、快取後不重下，
     以及伺服器停止時已快取的照常載入、未快取的失敗；證明不了 https、R2 的快取規則與真實延遲。Unity 預設擋 http，
     測試專案要允許。catalog 仍在本體，遠端 catalog 屬於內容更新。實作：Yu5h1LibTest 的 `RemoteTestSetup` 與 PlayMode 測試，
     見 [loader-handler](../../.agents/skills/loader-handler.md#remote-content-on-this-machine)。
   - **WebGL**：頁面與內容同站，以及不同 port 的跨來源（CORS）。LocalWebService 每個回應都是 `no-store`，
     WebGL 的 bundle 快取靠瀏覽器，暖快取要伺服器允許快取才測得準（LocalWebService 以 `--cache-max-age` 開啟）。
     實作：本機的 WebGL 探測（`Tools/agent/unity_addressables_webgl.py`）與 GitHub Pages 用的 `WebGLTestBuild`，
     見 [loader-handler](../../.agents/skills/loader-handler.md#webgl-probe)。
   - **R2**，之後才接 [學習與實作順序](#學習與實作順序) 的 LFS 與 Windows 頭像原型。

非必要內容（例如角色）讀取失敗時，用內建的測試角色代替並提示「此角色異常」，流程繼續；測試角色由 Resources 讀取器載入。
畫面怎麼呈現由 UX 的載入體驗決定。

## 頭像暗幕與進度通知

首輪暗幕代表「頭像下載」進度：A 已在首頁準備完畢，立即顯示；B 尚未下載時底下是通用占位圖，暗幕隨下載退去，下載並載入 Sprite 成功後才換上 B 頭像。不能一邊下載同一張尚不可讀取的圖，一邊顯示其局部內容。

後續若要讓真正頭像底圖一直可見、暗幕代表模型／高解析素材，先預載小頭像，再對大型內容訂閱相同進度通知。這是更換需求內容與呈現資料，仍共用同一個 Handler 和下載工作流程。

| 狀態 | 畫面行為 |
|---|---|
| 無使用許可 | 通用鎖定圖；不訂閱角色下載 |
| 排隊中 | 占位圖覆蓋暗幕，顯示等待 |
| 下載中 | 暗幕隨實際下載比例縮小，可顯示下載百分比 |
| 已下載、準備中 | 暗幕已退去，顯示「準備中」；首輪自動載入 Sprite，仍未開放選取 |
| 可使用 | 移除等待提示，開放操作 |
| 失敗 | 顯示重試；不把失敗偽裝成完成 |

UGUI 原型使用頭像上方的一張半透明黑色 `Image`，設為 `Filled`，選擇水平或圓形填滿；暗幕比例為 `1 - 下載比例`。這裡的切片揭露效果使用 Filled，`Image.Type.Sliced` 是九宮格縮放，功能不同。

下載比例來自 `GetDownloadStatus()` 的已下載位元組／總位元組，不把 `PercentComplete` 的子工作比例當作網路下載比例。先完成初始化與大小查詢，未知大小顯示準備中；總量為零時檢查成功與載入狀態，不做除零運算。[2]

已實作（2026-10-29，`AddressablesLoader`）：讀取請求的 Progress 就是下載比例，階段不另開事件，由現有事件順序推得——
Begin 之後 Progress < 1 是下載中，Progress 到 1 而 End 還沒來是「已下載、準備中」，True／False 是可使用與失敗。
不必下載時（已快取，或 Editor、PC 上非 http 的本機路徑）一開始就回報 1；WebGL 的 local group 從 StreamingAssets 以 http 抓取，算下載。
排隊中、無使用許可與多角色總進度還沒做。

通知至少攜帶內容 ID、階段、已下載量、總下載量與錯誤。UI 只顯示狀態，不直接啟動第二次下載。相同內容有多個訂閱者時，共用一個工作；卡片重用或關閉時解除訂閱，避免舊角色的通知更新新角色。

多角色的總進度須按去重後的 bundle 位元組統計，不能平均各卡片百分比或重複加總共用依賴。初版先驗證每角色進度，再加入全頁總進度。

## 下載、記憶體與場景生命週期

用 `GetDownloadSizeAsync` 查詢剩餘下載量，`DownloadDependenciesAsync` 提前把內容下載到快取。需要顯示時才 `LoadAssetAsync`／`InstantiateAsync`，場景則使用 `LoadSceneAsync`。下載到快取不等於已把頭像、模型或場景載入記憶體。[2][3]

每個操作 handle 要有明確擁有者。UI 離開只解除自己的需求；仍有其他需求者時保留共用工作。載入資源不再使用時 Release，實例使用對應的 ReleaseInstance，場景使用 UnloadSceneAsync；釋放記憶體與刪除磁碟快取分開處理。[3]

場景背景預載先使用下載依賴，真正切換時才載入。不要長時間以 `activateOnLoad=false` 停在等待啟用，這可能阻塞 Unity 的其他非同步操作。[4]

第一輪不承諾取消按鈕能立即中止底層 HTTP，也不承諾半包斷點續傳。先支援撤回尚未開始的需求、解除訂閱和失敗重試；成功快取的包能重用，未完成的包可能重下。許可撤銷後停止新增該內容需求、禁止操作，既有共用工作依使用者數清理。

## 發布、更新與故障處理

R2 使用自訂網域及適合 bundle 的快取規則。以平台、相容版本與發布版本組織內容；有雜湊的 bundle 不覆寫，catalog／hash 避免長時間陳舊快取。[5]

先上傳並檢查全部 bundle，再發布 catalog 與 hash／發布入口；保留舊版需要的內容，避免已安裝 App 下載到不存在的檔案。更新版本只在安全入口切換，避免角色使用中突然換目錄。保存素材 commit、程式 commit、Unity／Addressables 版本、平台、catalog、內容狀態檔及 Build Layout。[6]

冷快取斷網時保留啟動 UI 與重試；有可用 catalog 及完整快取時可以使用已備妥內容，仍須符合應用的許可有效性政策。HTTP 錯誤、磁碟不足、載入失敗分開記錄，重試採有上限的退避，避免無限下載循環。

## 學習與實作順序

本表排在 [首輪範圍](#首輪範圍讀取處理器代理與角色載入) 與 VCP 的同站遠端測試之後。

| 階段 | 實際工作 | 必須留下的證據 |
|---|---|---|
| 1. Git LFS 素材分離 | 建立測試素材 repo，以三個角色頭像練習 LFS；保留 .meta，主專案固定素材 commit | 乾淨 checkout 能取回素材本體與原 GUID；主 repo 不含大型素材本體。此階段尚未驗證遠端下載 |
| 2. 角色選單遠端閉環 | 建置上傳 R2；首頁先載 A，首頁結束動作請求 B；占位暗幕、Sprite 載入、選取開放 | Editor 與 Player 皆走 HTTP；C 沒有下載；暖快取不重下、重複觸發不重複工作 |
| 3. 場景流程 | 下一場景先下載依賴，切換才載入；關閉舊場景 | 未下載內容不會假稱 Ready；反覆切換後引用與記憶體能穩定 |
| 4. 更新與復原 | 改一個角色貼圖、發布新內容；測試斷網、失敗、暖快取與舊版 App | 只下載變更及必要依賴；舊版仍可取得自身相容內容 |
| 5. 收斂 | 以角色、頭像與場景驗證套件的排程、狀態通知與生命週期；整理操作指南 | 第二例共用核心，僅修改內容清單與呈現；新專案可按指南重做 |

實作規模：整體為大型、跨素材建置與 Runtime 的新能力；本次交付僅為文件。程式修改數量待第一階段盤點後估算，不預先承諾工期。驗證需要真實 HTTP 與 Windows Player；純單元測試不足以證明下載和快取。

第一階段先確定測試 Unity 專案與素材 repo；第二階段前記錄 Unity／Addressables 版本與測試 R2 位置。這些是實作環境設定，不增加第二套載入方案。先以測試素材跑通，不遷移既有大型專案或改寫 Git 歷史。

## Yu5h1Lib 最終收斂成果

應用提供內容 ID、使用許可與下一步需求；共用能力負責下載協調、狀態、重試和持有／釋放。角色選單負責暗幕及按鈕，場景流程負責切換時機。核心不內建角色 ID、商店規則或特定遊戲頁面。

最後形成三份能直接使用的成果：一個角色與場景的可執行範例、一組經兩例驗證的共用能力，以及一份從素材提交到遠端發布和除錯的操作指南。先盤點既有 Yu5h1Lib 事件與 UI 綁定能力，再決定接合方式；本計畫不宣稱已確認庫內完全沒有相關實作，也不先定死公開型別名稱。

方法收斂的完成條件是：換一批角色或下一個場景，透過資料設定就能重用；開發與正式環境走相同 Runtime 路徑；遇到下載失敗，能由紀錄判斷是許可、目錄、網路、快取或載入問題。

## 驗收清單

- 新機依固定 commit 取得程式與素材後，能重建同一內容版本的輸入；主 repo 不保存大型原始素材本體。
- 開發 Editor 與 Player 都驗證實際 HTTP 下載；不靠 Asset Database 隱藏分包或路徑問題。
- 首頁必需素材與初始角色 A 頭像先準備；首頁結束動作能提出 B 的需求，C 被許可篩選排除。
- 選單 A 立即可見；B 占位暗幕表示頭像下載，100% 下載與 Sprite Ready 分開。
- 同一 Handler 可從按鈕與另一種生命週期／動畫事件觸發；無需素材或已可用時直接繼續一次，失敗不繼續。
- Build Layout 和網路請求皆證明 C 的專屬素材沒有因共用包／圖集被下載。
- 同內容多個需求不重複排程；切頁後不收到過期 UI 更新。
- 暖快取不重下相同內容；未知大小、零下載量、離線與失敗都能正確呈現。
- 場景與角色走同一套下載狀態；反覆進出能釋放不再使用的載入資源。
- 新內容與舊 App 相容路徑有紀錄，更新失敗可回到仍完整存在的相容發布。
- 操作指南可讓另一個 Unity 專案重現範例；尚未實測的限制清楚列出。

## 待討論決策

以下是本討論稿的建議，不是已核定的公開 API；回答回填本節相應設計，再開始實作。

### 首輪（VCP）

兩題已於 2026-10-23 照建議定案，移入 [首輪範圍](#首輪範圍讀取處理器代理與角色載入)（角色專屬資料、驗證場所）。

### 頭像原型（遠端測試之後）

| 決策 | 建議 | 影響 |
|---|---|---|
| 首輪暗幕代表什麼 | 先用 B 頭像本身；下載時顯示占位圖。之後再加入小圖先到、大圖後到 | 決定首輪素材數量；若要求真頭像漸揭露，須先備妥另一張預覽圖 |
| 首頁結束後是否等所有角色 | 先開選單，A 可用、B 背景載入 | 可以直接驗證畫面不中斷及遊戲內進度；必需素材仍須等待 |

實作環境資訊另外於開始時取得：測試專案位置、素材 repo 的公開／私有性與實際初始角色。這些尚未建立，本次不操作帳號、建立 repo 或上傳素材。

## 官方參考

查核日期：2026-09-09。引用不同版本文件說明穩定概念；實作時以測試專案固定的套件版本核對 API，引用版本不代表要求升級。

1. [Unity：素材與依賴如何打包](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/PackingGroupsAsBundles.html)。
2. [Unity：預下載、下載大小與位元組進度](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/DownloadDependenciesAsync.html)。
3. [Unity：非同步操作與 handle 生命週期](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/AddressableAssetsAsyncOperationHandle.html)。
4. [Unity：場景載入與延後啟用限制](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/LoadingScenes.html)。
5. [Cloudflare：R2 公開網域與快取](https://developers.cloudflare.com/r2/buckets/public-buckets/)。
6. [Unity：內容更新與內容狀態檔](https://docs.unity3d.com/Packages/com.unity.addressables@3.0/manual/ContentUpdateWorkflow.html)。
