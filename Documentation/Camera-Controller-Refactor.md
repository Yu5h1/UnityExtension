# Camera Controller 重構分析

> Task ID: camera-controller
>
> 共用控制工作：[Camera Controller 任務卡](http://127.0.0.1:8001/?scope=unity-extension&pin=camera-controller)。
> Combat-Aesthetics 整合入口：[重構計畫](../../Combat-Aesthetics/Documentation/plans/Refactor-Plan.md#camera-controller-重構)。

## 系統階層與關係

```text
UnityExtension 共用攝影機能力
├─ InputSource：裝置操作轉為控制意圖
│  └─ 裝置讀取經由 InputHandler
├─ CameraController：控制方法、狀態與鏡頭更新
│  ├─ Follow：位置跟隨
│  └─ LookDirection：觀看方向
└─ 消費端
   ├─ MotionGen：SceneView／第三人稱操作
   ├─ VCP：角色觀看、offset 編輯與保存
   └─ Combat-Aesthetics：2D 跟隨、演出與 WhiteRoom 3D
```

裝置輸入與遊戲程式都使用同一個控制入口；遊戲程式可直接呼叫控制方法。共用能力由 UnityExtension 擁有，消費端維護自己的角色、UI 與演出政策。

## 問題與目標

既有攝影機分散在三個專案，重複處理 Orbit、輸入與狀態更新。目標是收斂共用控制方法，讓輸入來源可替換，並以獨立的 Follow 與 LookDirection 組合出 2D／3D 所需行為。

本文件整理先前程式盤點與已確認方向；具體 API、模組配置及遷移細節由任務卡中的契約與重用盤點工作收斂，不能將以下方向當成已存在的實作。

## 既有實作與消費需求

| 來源 | 既有能力 | 重構時需要保留的需求 |
| --- | --- | --- |
| MotionGen `Assets/Scripts/CameraSystem/CameraController.cs` 與其行為類別 | 模式切換、ICameraInput、SceneView、第三人稱 | 平移、旋轉、距離／FOV 調整與重設；將滑鼠按鍵、修飾鍵及 UI 判斷移出行為層。 |
| VCP `Assets/Scripts/Control/TouchController.cs` | Orbit、拖曳、雙指縮放與 offset 編輯 | 角色 offset 保存、OffsetChanged、DistanceNormal、depthChanged、DragEnd 與重設的既有消費者。 |
| [Core CameraController](../../Core/Runtime/Source/Control/CameraController.cs) | LookAt3rdPerson 的角度限制與 pivot 幾何計算 | 納入既有能力的重用評估；Core 變更須依既有授權規則處理。 |
| [Combat-Aesthetics CameraController](../../Combat-Aesthetics/Runtime/Core/CameraController.cs) | 偏向 2D 的跟隨、縮放、目標構圖與聚焦演出 | 保留演出與回復跟隨；將游標、點擊特效、Sorting Layer 遮罩及淡出交給各自擁有者。 |
| [WhiteRoomCamera](../../Combat-Aesthetics/Samples~/WhiteRoom/Runtime/WhiteRoomCamera.cs) | 3D 跟隨、Orbit、平滑與射線避障 | 接用共用控制；面板焦點及游標鎖定由 WhiteRoom 保留。 |

## 已確認的責任邊界

| 責任 | 方向 |
| --- | --- |
| CameraController | 提供可直接呼叫的控制方法，持有控制狀態，統一套用鏡頭結果；不讀取裝置按鍵。 |
| InputSource | 消費端將按鍵、拖曳、雙指、UI 焦點等轉為操作意圖；契約避免綁定 MouseDelta、滑鼠按鍵或 Shift／Alt 等裝置名稱。常用轉換可共用。 |
| Follow | 依目標與 offset 更新位置，可設定跟隨所需平滑；沒有操作輸入時，仍持續跟隨。 |
| LookDirection | 與位置跟隨分開，支援固定觀看方向及受控方向變更；Follow 不強制鏡頭 LookAt 跟隨目標。 |
| 消費端 | 維護角色選擇、offset 保存、演出協調、UI 焦點與游標鎖定政策。 |

輸入增量的單位與時間語意須在契約工作中定義，區分滑鼠位移、滾輪增量及搖桿速度。停止裝置操作不等於停止 Follow 或鏡頭求值。

## 2D 與 3D 案例

- 2D：Follow 更新角色所在平面的構圖位置，LookDirection 保持固定方向；正交縮放調整 orthographicSize。
- 3D Orbit：輸入意圖控制觀看方向與距離，配合跟隨目標定位。既有 Orbit 幾何應有單一維護來源。
- SceneView：保留場景平移、旋轉、焦點與投影調整需求，與 Orbit 共用可重用部分。
- 距離調整、orthographicSize 與 FOV 的控制語意分開定義；多目標構圖需同時考慮畫面寬高與比例。

## Combat-Aesthetics 附屬功能拆分

詳細工作由[攝影機附屬功能拆分卡](http://127.0.0.1:8001/?scope=combat-aesthetic&pin=camera-controller-extraction)維護：游標呈現、點擊特效、Sorting Layer 遮罩、淡出各自擁有狀態與更新責任。遮罩幾何與淡出時序分開處理。

遷移要保留 KFC 等既有消費端使用的 FoldUp、OverrideFadeMethod、cursorSprite、座標換算，以及 CameraAssist／CameraControllerAgent 的聚焦演出入口。必要的舊入口先委派給新擁有者，資產與呼叫端完成遷移後再評估退役。

## 遷移與驗收原則

1. 先收斂控制契約與既有計算的擁有者，再遷移消費端，避免新增另一套獨立 Orbit。
2. 以 MotionGen 與 VCP 驗證共用控制，以 Combat-Aesthetics 固定觀看方向的 2D 案例驗證 Follow／LookDirection 可獨立組合，再涵蓋 WhiteRoom 3D。
3. 分開驗證裝置輸入停止、UI 焦點、目標切換、重設與持續跟隨。
4. 檢查腳本 GUID、序列化欄位、UnityEvent 接線及既有公開入口的相容性；來源盤點不等於執行期驗證。

實作項目與驗收進度以既有兩張任務卡為準。本文件不指定新增套件、安裝依賴或直接修改 Core。
