# Yu5h1's UnityExtension 計畫

## 簡述

Yu5h1's UnityExtension 是從 Yu5h1 開發視角出發的 Unity 應用擴充方案。它沉澱可重用的系統、工具與開發流程，目標是用高效、低耗、使用體驗友善的方式完成各類 Unity 專案，補足 Unity 原生流程在資料組織、工作流、擴充性與應用層支援上的不足。

## 1. 目標

UnityExtension 的目標不是建立單一大型框架，而是整理一套能支援多種 Unity 專案的 solution：

- 將常用系統拆成清楚、可重用、可維護的 package。
- 讓資料、工具、Runtime、Editor 與 UX 能互相配合，而不是散落在專案內硬寫。
- 用 Yu5h1 視角補完 Unity 開發中不夠順手、不夠一致、不夠可重用的部分。
- 讓不同應用專案可以專注在自己的領域價值，而不是重複處理基礎系統與工具流程。

## 2. 設計原則

- 先解決真實開發問題，再決定是否需要抽象名詞或設計模式。
- 通用能力放在 UnityExtension；具體專案只保留自己的應用邏輯與表現需求。
- Runtime 能力、Editor 工具、資料設計與使用體驗要一起考慮。
- Package 邊界要以用途與依賴方向為準，不以資料夾現況硬切。
- 設計要能支援快速開發，也要能支援後續整理、重用與擴充。
- Package 以它擴充的 Unity 功能命名（InputSystem、Timeline、URP、UI、UIToolkit、Addressables），是大分類，不是一個功能一個套件；
  依程式處理的對象分類，不依傳輸方式。只有必要而且無法改成可選的依賴才另開套件（2026-10-21，理由見
  [Addressables 套件計畫〈套件位置與依賴〉](Documentation/Addressables/plan.md#套件位置與依賴)）。
- 資料驅動的組裝只是外層：核心能力要能直接用程式碼呼叫與測試，不能只能從 Inspector 觸發；場景一側用 inline SO 與
  UnityEvent 組裝。例如非同步工作的 `Awaitable` 只在處理器與工作物件內部，對場景只露出事件（2026-10-22）。
- 持續檢查 Inspector 接線：類別或欄位改名、遷移之後，用 UnityEvent 監控工具（Tools／UnityEvent Viewer、batchmode
  `UnityEventReport.Run`，見 [UnityEvent監控](Documentation/UnityEvent監控.md)）找出失效接線。邏輯散在接線裡是資料驅動的代價，
  這個工具是它的補救；VCP ChatScene 曾掃出 226 筆接線中 24 筆失效（2026-10-20）。

## 3. 文件分層

根目錄 `plan.md` 只保留整體方向與決策原則。

細部系統、應用領域與開發者操作方式，放在 `Documentation` 下作為指南，例如：

- package 拆分指南
- 資料流程與工具使用指南
- Runtime system 使用指南
- Editor tool 操作指南
- 具體應用案例整理

這樣根計畫保持清楚，細節則能依系統逐步補齊。

## 4. 近期工作

- 依 [Recycler 重構計畫](Documentation/Recycler_Refactor.md#後續實作與驗收)，先驗證 Core 正確性與穩態配置，再完成 Unity 接合及舊 Pool 遷移；以目標 runtime 的量測決定重用範圍。
- 盤點現有 UnityExtension packages 的責任與依賴方向。
- 從 CombatAesthetic 等實際專案中辨識可回收的通用能力。
- 將通用能力規劃到對應 package，而不是留在單一應用專案。
- 為重要系統補 `Documentation` 開發者指南，讓使用方式與設計意圖能被理解。
- 非同步讀取角色：先做 [非同步工作 § 首輪](Documentation/非同步工作.md#首輪讀取角色)，再做
  [Addressables 套件](Documentation/Addressables/plan.md#首輪範圍讀取處理器代理與角色載入) 的讀取處理器；第一個消費端是 VCP。

## 5. 驗證原則

後續整理 package、文件或工具時，用以下方向檢查是否符合 UnityExtension 的目標：

- 是否讓常用能力更容易重用，而不是每個專案重新硬寫。
- 是否讓系統邊界與依賴方向更容易理解。
- 是否改善 Editor/Runtime 的使用體驗。
- 是否讓新專案更快組合出需要的能力。
- 是否讓舊專案能逐步把通用能力回收成 package。


