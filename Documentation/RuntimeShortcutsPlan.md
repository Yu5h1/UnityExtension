# Unity 執行時快捷鍵

> Task ID: unity-runtime-shortcuts

## 系統階層與關係

```text
UnityExtension
├─ Common → 快捷鍵指令、作用域與共用編輯 API
├─ InputSystem → 鍵位匹配、錄製與使用者覆寫
├─ UI／UIToolkit → 讀取並編輯同一份狀態
└─ 宿主專案 → 登錄指令、決定何時可用、保存設定
```

UI、UIToolkit 和 InputSystem 依賴 Common。宿主建立一個 `ShortcutService`，接上 Input System，並把同一個實例交給兩種 UI。`ShortcutService` 是提案名稱，公開介面在第一個實作案例驗證後定案。

## 問題與目標

Unity 執行時已有 Input System，部分操作仍直接讀固定鍵位，例如 VCP 主選單用 Enter 提交、Esc 返回。uGUI 和 UI Toolkit 需要同一個 API，才能查詢目前鍵位、讓使用者改鍵，並依當前畫面派送指令。用 ScrollView demo 的「切換循環」驗證：改鍵後新鍵立即生效，舊鍵停止作用，兩種 UI 讀到同一份綁定，離開 demo 後快捷鍵失效。 [VCP 主選單](file:///W:/UnityProject/Virtual-Chat-Partner/Assets/Scripts/UI/MainMenu.cs#L176)

## 責任邊界

| 所在層級 | 責任 |
|---|---|
| `Packages/common` | 提供穩定指令 ID、顯示名稱、作用域、有效鍵位、可編輯狀態與變更通知。提供錄製、取消、解除與還原操作，統一判定衝突和鎖定。公開 API 使用共用資料型別。 |
| `Packages/InputSystem` | 將指令與實際使用的 `InputAction` 對應。Input System 負責匹配按鍵、捕捉輸入與套用綁定覆寫。編輯與派送使用同一組 action 實例。 |
| uGUI、UI Toolkit | 顯示同一份指令和狀態，呼叫共用 API。各自接上焦點、編輯視窗和關閉時的錄製取消。 |
| 宿主 | 定義指令的行為與生效條件，透過既有設定機制載入與保存使用者覆寫。 |

[`InputHandler`](../Packages/common/Runtime/Input/InputHandler.cs) 持續提供逐幀的鍵盤、滑鼠與觸控查詢。`GetKeyDown(KeyCode.Return)` 在 Input System 後端直接查鍵盤狀態，綁定覆寫對它無效。改成可編輯的指令時，將原本的回呼接到新指令，移除同一操作的固定鍵監聽。攝影機連續輸入與既有 uGUI 輸入橋接沿用 `InputHandler`。 [Input System 後端](../Packages/InputSystem/Runtime/Backend/InputSystemBackend.cs)

## 設計與驗收

1. **共用 API 與 Input System 接線**。用穩定指令 ID 查詢和修改綁定。由 Input System 保存實際覆寫，預設鍵位保留在原始綁定。驗證改鍵、取消、解除、還原與存讀後復原。錄製時暫停快捷鍵派送，結束或關閉時釋放錄製並恢復派送。 [Unity Input Bindings](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/ActionBindings.html#interactive-rebinding)
2. **ScrollView demo 試行**。Toggle 點擊與快捷鍵共用切換循環操作，同一次輸入只執行一次。驗證 demo 開啟時生效，離開後失效，方向切換與鍵盤編輯仍可用。
3. **uGUI 與 UI Toolkit 共同驗證**。兩種介面讀取同一服務，一邊改鍵後另一邊即時反映。驗證同作用域衝突、不同作用域可共用鍵位，以及被鎖定的項目能顯示原因、拒絕修改。確認錄製鍵位時不會觸發原指令。
4. **VCP 單一指令試遷移**。挑一項原本寫死的操作，驗證改鍵後既有回呼只執行一次，並檢查文字輸入焦點、彈窗與原有操作的互動。

Web 與 Unity 對齊指令 ID、作用域、衝突與鎖定語意。Web 鍵碼與 Unity 控制路徑的轉換，以及鍵位檔互匯，留待有實際需求時定義。 [ShortcutEditor UX 規格](../../../UX/Documentation/ModuleSpec/Modules/ShortcutEditor.md)

## 尚未決定

- VCP 要用哪個既有指令做試遷移。等 ScrollView 與雙 UI 驗證完成後，再依文字焦點與彈窗影響挑選。
