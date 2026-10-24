# AppsMenu 驗收紀錄（Yu5h1Lib.UIToolkit）

> Task ID: appsmenu-library-adoption

本文件分別記錄已完成的 AppsMenu sample 來源對照，以及依 Yu5h1Lib.UX 建立共用模組的採用紀錄。體驗要求以 UX 規範為準；sample 可驗收不代表後來新增的全部 UX 要求已實作。Web 是另一個實作端，它的結果不代表 Unity。

## 2026-10-24：完成的 sample 與共用規範差異

本次使用者指定 `Samples~/AppsMenu` 已完成、可以驗收；本次不重做 sample，也不以尚未建立 Core 模型否定 sample 的完成。以下是依 referenced-module-acceptance 的來源盤點，尚非新的執行驗收輪。下方〈身分〉至〈結論與下一步〉保留 2026-10-07 的 UX 1.0.1 採用規劃及當時證據，不能作為現行 sample 是否可驗收的判斷。

### 來源與檢查邊界

- 實作：UnityExtension `5ca4152c901690db5bca93414940b54b4aae6dba` 的 [AppsMenuDemo.cs](../../Samples~/AppsMenu/AppsMenuDemo.cs)，SHA-256 `ffa41b40b5ce9eaf8bbac8ff98f6a5f6c8d845cd576196d3a8c49d26f7d13728`；[AppsMenuDemo.uss](../../Samples~/AppsMenu/Resources/AppsMenuDemo.uss)，`c2043325e97f15930c5117c156517f1a75c5abf49ccf208f7d6b2db260c35d18`。
- 對照來源：UX `9d54a67e886f1e8f75331b678927efa0ec690bcf`，[AppsMenu](../../../../../../UX/Documentation/ModuleSpec/AppsMenu.md) 5.1.3；規格 SHA-256 `b9cb28d5670b5e1fa8224f7bc225a84c585e504fb01a71bf2d0d76e66dff6666`。這是差異盤點基準，未替換舊輪釘選、未匯入 Unity 快照。
- 設計 6.0.0：`f4548c5749450092f7987cc9aaa3fb9897492d4228fd59758304bfbebd2f0a7a`；案例 1.0.0（11 項）：`4075be6d7f6d86a479a0bf5299b9573d4c09797976af5e798c9c17103edf84a1`；設定宣告 1.4.0：`1731f32d4387abde03785cd7b32f16b210aa4f4c370dab116f3780016dab96b7`。均以 UX 原檔對照。
- 公開來源入口：[AppsMenuDemo.unity](../../Samples~/AppsMenu/AppsMenuDemo.unity)／`AppsMenuDemo` 元件，執行期自行建立 UI。已讀取 Yu5h1LibTest 的 6000.6.0f1 版本與 package manifest；已確認 `Assets/Samples/Yu5h1Lib.UIToolkit/0.1.0/Apps Menu` junction 指回本 sample。沒有修改 host、場景或連結。
- 最初盤點只有原碼、文件及路徑核對，沒有執行 Unity／11 項模型案例／P01～P12。工具清單沒有 unityMCP，未啟動 Editor 或接管使用者畫面；實際互動、數值與外觀結果均不能由靜態閱讀代替。驗收進度與阻塞由 [AppsMenu 卡片](http://127.0.0.1:8001/?scope=unity-extension&pin=appsmenu-library-adoption) 擁有。
- DX 1.0.0 本次只用來檢查後續採用的入口需求；不追溯加進舊輪，也未開新 DX 輪。原 sample 的最小組裝已有場景與元件；可重用產品資料／動作政策入口屬共用模組的交付工作。

來源基準只能證明規格是否符合它所抽取的來源，不證明其他平台已採用，也不證明其他消費端需要全部行為。本次來源未執行，不能宣稱來源基準通過；使用者所說的「完成、可驗收」也不是人工驗收結果。

UX 本次補正已交付：使用者授權保留 ChatPanel 並合入 AppsMenu 的 manifest 版本與規格雜湊。來源 UX `4dcc807dff138d89b12f6980d9e840bec9c8bf53`，AppsMenu 5.1.4（SHA-256 `b5a10b67af7c96189f4d03b5a61daab8c1d027d93344fc84794dbde269021522`）只澄清設定／佈局分開保存及入場 size 為縮放後尺寸，設計與案例雜湊不變。以上 5.1.3 仍是本次盤點起始基準；兩處文字核對見 Requirements.UX 的 UNITYEXTENSION-UX-2，沒有把新版本寫成平台驗收通過。

### 規範交付驗收與編譯預檢（2026-10-24）

規範交付驗收：UX `4dcc807` 的 manifest 相對父 commit 只修改 AppsMenu 版本與規格雜湊。獨立審查以 Git blob 核對 AppsMenu 四份交付檔均符合 manifest，設計、案例與設定資料沒有變更；ChatPanel 三份交付檔、版本及 manifest 映射均與父 commit 相同。UNITYEXTENSION-UX-2 的規格澄清條件成立；這不代表平台行為通過。

組裝入口預檢命令（UnityExtension 根目錄）：`python Tools/agent/unity_compile.py Packages/UIToolkit/Samples~/AppsMenu/Yu5h1Lib.UIToolkit.Samples.AppsMenu.asmdef --reference-project W:/UnityProject/Yu5h1LibTest --reference-project W:/UnityProject/BonghuoVR`。對象仍為上列 `5ca4152` 已提交原碼，sample SHA-256 未變；執行前後未修改程式、場景、host manifest 或 junction。

- 首輪沙箱預檢回傳 `dependency Yu5h1Lib.Common failed`，工具未輸出底層 dotnet 診斷；另行讀取 `dotnet --info` 顯示 Service Control Manager access denied。未把這次環境失敗判為 sample 程式缺陷。
- 經核准在沙箱外以同命令重跑：6000.3.9f1 與 6000.6.0f1 均 `ok, 0 error(s), 0 warning(s)`，exit code 0。6000.3 的 sample 與 UIToolkit 沒有對應 generated csproj，使用通用參考；6000.6 取自 Yu5h1LibTest generated csproj。這是無須啟動 Editor 的定向編譯，不是乾淨 host 上的執行驗收。
- 本次沒有執行 APPS 中性模型案例、平台輸入、layout 量測或外觀確認；亦未建立替代模型來假裝測到 sample。公開語意 adapter 與非干擾的執行入口仍需準備。正式平台輪須依乾淨 checkout／host 重指規則另行執行；編譯不計為任何完整情境通過。

### 哪些是 UX 缺口，哪些是 Unity 未採用

| 元件／能力 | UX 規範缺口 | Unity UI Toolkit 的實際情況與本 sample 關係 |
|---|---|---|
| 格線與停靠 [GridDock](../../../../../../UX/Documentation/ModuleSpec/Collections/GridDock.md) | 0.1.0-draft，尚無已確認目標與發布案例 | 有 `GroupDragLayout`，不是完全沒有格線；sample 使用 free 散佈。通用 GridDock 尚無採用紀錄。四邊停靠、其他 app 釘選屬 AppsMenu 下一目標，不阻擋現有 sample；目標 1 的格線仍依 AppsMenu 自有條文 |
| 集合換位 [CollectionSwitchPosition](../../../../../../UX/Documentation/ModuleSpec/Collections/CollectionSwitchPosition.md) | 0.1.0-draft；把手與長按比較原型尚未收斂成正式契約 | sample 已有 `SlotUnder`、`Reorder`、`MarkSlot` 的面板換位；沒有正式共用集合採用紀錄。不能把已有面板排序說成未實作，也不能要求先完成 swap／跨集合釘選才驗 sample |
| 捲動視窗 [ScrollView](../../../../../../UX/Documentation/ModuleSpec/Collections/ScrollView.md) | 0.1.0-draft，溢位、拖出與排序的手勢仲裁尚待確認 | sample 的面板是換行容器，未接原生 ScrollView；現行 AppsMenu 目標 1 未要求溢位捲動，不是本輪硬前置 |
| [Toggle](../../../../../../UX/Documentation/ModuleSpec/Controls/Toggle.md) 名稱與狀態 | 1.0.2 已發布；目標 1 明列 Unity 為範圍外，尚無 Unity 採用目標 | 尚無本套件的 Toggle 採用紀錄；sample 沒有排列切換 app。這是共用能力採用與目標範圍問題，不是 Toggle 沒有規則 |
| [Icon](../../../../../../UX/Documentation/ModuleSpec/Foundation/Icon.md) 圖示來源與著色 | 1.0.1 已發布；三種來源的目標 1 只含 Web，Unity 烘焙／VectorImage 尚未納入目標 | [Button 紀錄](Button.md) 仍列 Icon blocked；sample 用數字 Label，沒有採用三來源圖示契約。不能由「圖示」一詞推定數字 demo 必須新增 SVG 管線 |
| [Input](Input.md) | 基礎規則已發布 3.0.2；不能稱整份 UX 缺失 | 已有 `HoldManipulator` 採用 3.0.1 的紀錄。sample 使用舊 `Gesture.LongPress`，未整合該操作者；既有 Input 輪未涵蓋全部速度取樣與拖曳交易，不能外推為全通過 |
| [Button](Button.md)／[VisualFoundation](VisualFoundation.md) | 已有正式規則；舊的狀態配色缺口 UNITYEXTENSION-UX-1 已結案 | 原生 Button＋USS、色票與狀態已有 Unity 證據；sample 使用自有 VisualElement 與固定色值，尚未接主題。不是缺少整個 Button／色票元件；AppsMenu 圖示尺寸也不能改套 Button 尺寸來驗 |
| [Configuration](../../../../../../UX/Documentation/ModuleSpec/Foundation/Configuration.md) | 1.4.2 已發布，等級 1 在 Common 已有證據；AppsMenu 自己仍明列草稿／保存／套用流程，不能逕用等級 1 消除此要求 | sample 只有 Inspector 設定，未接設定保存。兩處歧義已由 UX 5.1.4 交付澄清，見 [Requirements.UX](../../../../Documentation/Requirements.UX.md#unityextension-ux-2--appsmenu-設定檢查與佈局保存說法矛盾及入場尺寸未明) 的規格矛盾 |
| 入場與吸收包絡 | Motion [Interpolation](../../../../../../Motion/Documentation/ModuleSpec/Interpolation.md) 1.1.0 已發布 ENT-01～07／ENV-01；剩 UX 核對與改引用，不再是等發布 | sample 已用 `Transition.Entrance`／`Opening`；缺的是目前 AppsMenu 參數、吸收包絡及組合驗收，不是 Unity 沒有入場元件 |
| 搖晃／彈簧、sorted Scatter | Motion 的 UX-MOTION-4／6 仍未交付正式對應；AppsMenu 已提供暫定公式 | Unity 有 `Reaction.Shake`／`Spring` 與 `GroupDragLayout`，可驗來源；不能說 Unity 尚無這些動態。跨平台共用入口與數值交付另依 [Requirements.DotNet](../../../../Documentation/Requirements.DotNet.md)／[Requirements.Motion](../../../../Documentation/Requirements.Motion.md) |

### 現行行為到 sample 的映射

下表為 2026-10-24 靜態對照，描述「已存在的來源能力」及「對 5.1.3 的差異」，不記任何執行 pass。檢查處方以 [AppsMenu 的行為／平台檢查表](../../../../../../UX/Documentation/ModuleSpec/AppsMenu.md#驗收案例與覆蓋) 為準。

| 行為（檢查） | sample 入口與差異 |
|---|---|
| 資料與隔離 APPS-DATA（P01） | `BuildApps` 有項目及前 5 項桌面預設；沒有公開 configure、快照與政策驗證入口 |
| 動作 APPS-ACTION（P02） | `BuildIcon` 長按切 mark，`OnIconUp` 計 click；產品啟動／可覆寫 action 通知尚未交付 |
| 通知重入 APPS-REENTRY（P09） | 沒有對外通知／命令模型，不具中性重入檢查入口 |
| 拖曳交易 APPS-DRAG（P01／P03） | `OnIconMove`、背景 `DragGroup` 已存在；沒有符合契約的完整 rollback 交易 |
| 放置 APPS-DROP（P03） | `DropTarget`、`Reorder`、拉回桌面已存在；外側拒絕／自訂政策與原子提交需另驗／接入 |
| 實體輸入 APPS-INPUT（P02／P03） | 舊 LongPress，app-0 容許量為 0，圖示移動即拖；不符合共用門檻與拖後 click 抑制規則 |
| 面板 APPS-PANEL（P03） | `OpenPanel`／`ClosePanel` 可用；沒有完整鍵盤命令、可讀名稱與焦點回歸處理 |
| 吸收 APPS-VACUUM（P04） | `OnAppsButtonMove`／`TickVacuum` 已存在；恰 8px 即成立，捕獲距離未取 min(radius,size×.6) |
| 包絡 APPS-RAMP（P04） | 固定半徑 170／吸力 5，尚無隨時間增長包絡 |
| 釋放 APPS-RELEASE（P05） | `EndVacuum` 以桌面全空才釋放；與規格「本次零捕獲才釋放」不同，是已完成 sample 與後定體驗的差異 |
| 速度 APPS-VELOCITY（P05） | `Sample`／`Launch` 有平滑速度與甩飛；沒有 100ms 首末視窗及 releaseForce／releaseFan 分支 |
| 甩飛接住 APPS-CATCH（P05） | `TickFlight`／`catchArmed` 已有離開後再接住與收納，不應再列成「規格沒有的行為」 |
| 收納搖晃 APPS-SHAKE（P03／P05） | `Stash` 已搖 Apps 按鈕；固定播放，尚未接受擊方向／速度／reducedMotion 政策 |
| 生命週期 APPS-LIFECYCLE（P06） | `OnDisable`／capture lost 有清理；尚無完整取消還原與失焦／resize／第二指標組合處理 |
| 設定 APPS-CONFIG（P07） | Inspector 欄位可調；沒有宣告匯入、保存、草稿及失敗重試入口 |
| 入場 APPS-OPENING（P08） | `StartOpening` 已有排程與跳過；總長仍 1.4s，現行 UX 為 .666s；曲線固定 linear |
| 排列 APPS-ARRANGE（P10） | `GroupDragLayout` 有基礎能力；sample 未提供排／不排切換及現行釘選列定位 |
| 騰空 APPS-REFLOW（P10） | `Apply` 有位置彈簧、面板有 slot 提示；沒有完整 sorted 候選格與面板項目彈簧騰空 |
| 佈局保存 APPS-LAYOUT（P11） | 沒有佈局匯出／匯入與持久化入口 |
| 排列切換 app APPS-FUNCTION（P12） | 沒有 arrange-toggle 資料或功能 app 入口 |

這些差異不能用修改已完成 sample、降低 UX 要求或勾選未跑檢查來消除。來源互動驗收可先獨立進行；共用模組實作仍依原有 Core 架構決策另行採用。下一次執行驗收須用固定 commit 的乾淨 checkout、真實公開入口與所需 host，不能把此表算作 7 個情境已通過。

```text
UnityExtension
└─ Yu5h1Lib.UIToolkit（實作套件）
   └─ AppsMenu（本紀錄）
      ├─ 體驗規範 ← Yu5h1Lib.UX ModuleSpec/AppsMenu（APPS-*、P01–P08、設計資料、11 項中性案例）
      ├─ 數值服務 ← Motion：Throw、Scatter（含 Attraction），經 Packages/Motion 原生接入
      │    └─ Unity 端數值驗收：../Motion/Throw.md、Scatter.md、Attraction.md
      └─ 驗證專案 → Yu5h1LibTest（Unity 6000.6）
```

## 身分

| 項目 | 內容 |
|---|---|
| 實作套件 | `com.yu5h1.uitoolkit`（`Packages/UIToolkit`）。目前只有既有範例 `Samples~/AppsMenu/AppsMenuDemo.cs`，尚無依規格實作的模組 |
| UX 規範 | [UX ModuleSpec/AppsMenu.md](../../../../../../UX/Documentation/ModuleSpec/AppsMenu.md)，規範 1.0.1；UX `077c254`，乾淨；SHA-256 `9a36f2c5…5571` |
| 設計資料與案例 | UX `ModuleSpec/AppsMenu/apps-design.json` 2.1.0 `fad0f43b…c585`；`apps-cases.json` schema 1／1.0.0，11 項，`4075be6d…84a1`。UX 未提供 manifest，以上為 UX commit 內的檔案雜湊；Unity 尚未匯入副本 |
| 規範引用的其他契約 | 設定宣告與保存格式：Web [ModuleSpec/Configuration.md](../../../../../../Web/Documentation/ModuleSpec/Configuration.md)（UX 暫引用）；主題色票：Web 色票契約 1.0.0 |
| 歷史基準 | 2026-10-07 前以 Web `f7abccf` 的規格 1.0.0 為來源，SHA-256 `658fd825…cece`（`apps-delivery.json` 記錄）；設計資料與案例雜湊與上列相同。UX 1.0.1 只改擁有者，APPS-*、P01–P08 與案例 ID 不變，因此下方實作對照沿用，但所有驗收對 UX 1.0.1 視為未驗證 |
| Motion | Motion 規格包 `8b53e5f`（Throw、Scatter、Attraction 1.0.0）；Unity 端數值驗收見 [Motion/](../Motion/) 三份紀錄 |
| 驗收範圍 | APPS-DATA～APPS-OPENING 全部、P01–P08、11 項中性案例。平台：Windows Editor 先行；Windows Player、Android 列入但未排程 |
| 紀錄狀態 | **planned**：已採用 UX 1.0.1 作為對照基準；完成既有範例對照，尚未實作、尚未執行任何 APPS 驗收 |

Unity 端的選擇（模組位置、參數匯入、案例來源、鍵盤、入場、物理範圍）在 [決策檔](../../../../decisions/cross-platform-ui-parity.decisions)。

## 實作對照

決策 `appsmenu-approach` 要求先逐條比對既有範例與規格，再決定改既有範例或另建模組。下表是 2026-10-07 對 `AppsMenuDemo.cs` 的靜態閱讀結果，不是執行證據。

| 來源 ID | 既有範例的對應 | 共用依賴 | 對照 |
|---|---|---|---|
| APPS-DATA | 狀態散在元件欄位（`apps`、`onDesktop`、`marked`）；ID 自動產生 `app-{i}`，前 5 項在桌面 | — | **缺少**：沒有模型、快照、政策注入與輸入驗證；初始位置由 `GroupDragLayout` 散佈，不是規格的格狀公式 |
| APPS-ACTION | `Gesture.LongPress` 400ms 切換 mark；放手計數為 click | `Gesture.LongPress` | **部分**：mark 相同；沒有 activate 動作通知與 activate／mark／none 政策 |
| APPS-DRAG | 桌面圖示跟手（`Resolve`）；背景拖曳整組移動 | `GroupDragLayout.DragGroup` | **部分**：沒有取消還原；背景拖曳放手帶慣性，規格未提供群組慣性 |
| APPS-DROP | `DropTarget`：Apps 按鈕 → 已開面板 → 桌面；面板內依游標下的項目排序；拉回桌面後關閉面板 | — | **相同**（判斷順序、排序、拉回關閉）；**缺少**元件外拒絕與自訂 drop 政策 |
| APPS-INPUT | 長按 400ms、移動容許 6px；`drop-ready` 外框，手勢結束清除 | `Gesture.LongPress` | **不同**：app-0 容許量為 0（規格無此例外）；圖示一移動就拖曳，沒有 >6px 門檻；拖曳後放手仍算 click |
| APPS-PANEL | 短按 Apps 開關；背景按下關閉；面板吃掉事件，不啟動背景拖曳 | `Reaction.Spring` | **缺少**：Escape 與鍵盤操作（Enter／Space、Shift+Enter、Alt+左右）；面板開合用彈簧動畫，規格未提供 |
| APPS-VACUUM | 拖動超過 8px 才吸收；按鈕跟手；捕獲距離圖示尺寸 × 0.6 | `GroupDragLayout.Pull`（C#） | **不同**：恰好 8px 時已啟動（規格要求 >8）；捕獲距離未取 min(尺寸×0.6, 半徑)；吸收用 C# 而非 Motion Scatter |
| APPS-RAMP | 固定半徑 170、力道 5 | — | **缺少**：沒有隨時間增長的範圍與吸力包絡 |
| APPS-RELEASE | 放手時桌面已清空才釋放全部，從放手點靜態散佈 | `GroupDragLayout` | **不同**：規格是本次零捕獲才釋放、以 Throw 往外拋；origin 取放手點相同 |
| APPS-VELOCITY | 最後取樣距放手 <0.1s 且平滑速度 >180 才甩飛 | `PointerVelocity`、`Reaction.Throw`（C#） | **不同**：不是 100ms 視窗首末取樣；沒有 Apps 的 releaseForce 與徑向最低速度；甩飛用 C# 而非 Motion Throw |
| APPS-LIFECYCLE | capture 遺失時清除手勢；背景按下停止飛行；`OnDisable` 停止動畫 | — | **缺少**：取消不還原位置；沒有失焦、隱藏、尺寸變更、第二指標、捲動的處理 |
| APPS-CONFIG | 參數只在 Inspector | — | **缺少**：沒有 JSON 宣告、保存、套用與失敗重試 |
| APPS-OPENING | `Transition.Opening`：總長 1.4s、間隔 0.075、收攏 0.35、逾時 3s、按下跳過 | `Transition.Entrance` | **待比對**：UX〈入場規範〉沿用這套 Unity 入場，數值與回饋基數尚未逐項核對 |

範例另有規格沒有的行為：飛行撞擊障礙物、甩進 Apps 自動收納、收納時按鈕抖動、背景群組慣性、面板彈簧。規格說明這些不是本版復刻條件。

## 驗收對照與結果

每項的預期以 UX 規範 1.0.1 為準。目前沒有任何依規範的實作，所以全部未執行。

| 來源 ID | 中性案例／補充檢查 | 環境 | 結果 | 證據／差異 |
|---|---|---|---|---|
| APPS-DATA | second-host-policies；P01 | Windows Editor | 未執行 | — |
| APPS-ACTION | hold-marks-click-activates、second-host-policies；P02 | Windows Editor | 未執行 | — |
| APPS-DRAG | group-follows-pointer、cancel-rolls-back、rejected-drop-rolls-back；P01、P03 | Windows Editor | 未執行 | — |
| APPS-DROP | stash-in-apps、stash-before-panel-slot、pull-back-to-desktop、panel-reorder；P03 | Windows Editor | 未執行 | — |
| APPS-INPUT | P02、P03（實體輸入） | Windows Editor；Android 觸控 | 未執行 | — |
| APPS-PANEL | panel-open-close；P03 | Windows Editor | 未執行 | — |
| APPS-VACUUM、APPS-RAMP | P04 | Windows Editor | 未執行 | 依賴 Motion Scatter 接入 |
| APPS-RELEASE、APPS-VELOCITY | P05 | Windows Editor | 未執行 | 依賴 Motion Throw、Scatter 接入 |
| APPS-LIFECYCLE | dispose-cancels-and-ignores；P06 | Windows Editor | 未執行 | — |
| APPS-CONFIG | P07 | Windows Editor | 未執行 | 依 Configuration 規格；Unity 端與 Preferences 的對應待討論 |
| APPS-OPENING | P08 | Windows Editor | 未執行 | — |

11 項中性案例要透過 Unity adapter 執行；adapter 尚未建立。手感、觸控與視覺屬人工檢查，執行時列入 `checklists/`，本文件只連結結果。

## 結論與下一步

目前沒有任何 APPS 項目可宣稱相容。對照結果：既有範例的組裝方式可沿用（單一手勢擁有者、指標捕獲、`Gesture.LongPress`、`ElementTarget`、`Transition.Opening`），但缺少模型與政策層，吸收、釋放、取消、鍵盤與設定都與規格不同，數值也還沒改走 Motion。

尚未決定：依上表選擇改既有範例或另建模組（決策 `appsmenu-approach` 的後續），以及 `Samples~/AppsMenu` 之後改為模組範例或退役。

**2026-10-07 暫停，先打基礎。** 依決策 `layering-doc-owner`、`core-contracts`、`ui-model-location`：AppsMenu 的操作模型（狀態、命令、行為規則、手勢判斷）放 DotNet Core，甩飛與吸塵器經 Core 的物理互動入口，Unity 只寫畫面適配。前置需求：

- [UNITYEXTENSION-DOTNET-2](../../../../Documentation/Requirements.DotNet.md#unityextension-dotnet-2--在-core-建立-appsmenu-的操作模型)：Core 的 AppsMenu 操作模型，11 項中性案例在模型上執行。
- [UNITYEXTENSION-DOTNET-1](../../../../Documentation/Requirements.DotNet.md#unityextension-dotnet-1--依-motion-規格在-core-建立動態合約)：Throw、Scatter 的單一入口。

前置完成後的順序：

1. 依上表決定改既有範例或另建模組；UIToolkit 的畫面與控制接上 Core 模型與物理互動入口。
2. 從 UX 匯入設計資料與案例並核對雜湊，記錄採用的 UX commit（[UnityExtension agent knowledge〈Adopting a UX module in a package〉](../../../../.agents/introduction.md#adopting-a-ux-module-in-a-package)）。
3. 逐項執行上表的 Unity 端檢查（輸入、畫面、生命週期、平台），結果記在本紀錄，供 UX 的 [UX-UNITYEXTENSION-1](../../../../../../UX/Documentation/Requirements.UnityExtension.md) 與 Web 的 [WEB-UNITYEXTENSION-2](../../../../../../Web/Documentation/Requirements.UnityExtension.md) 查核；兩者的狀態由提出方更新。

UGUI 版等有產品需要再做（決策 `ugui-appsmenu-now`），屆時另建 `Packages/UI/Documentation/Yu5h1Lib.UX/AppsMenu.md`。

UX 規範、設計資料、案例或 Motion 規格包改版時，找出受影響的 APPS-ID 重新驗收；舊結果只對應它記錄的版本。規範含糊或缺漏時，在 UnityExtension 的 `Documentation/Requirements.UX.md` 提出，不自行補規則。
