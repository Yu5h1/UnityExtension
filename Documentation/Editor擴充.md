# Editor 擴充

> 狀態：總覽盤點中；[EnhanceCollectionField](#enhancecollectionfield更新計畫) 方向已定、尚未實作。
>
> 與其他文件的關係：
> - Editor 擴充的寫法慣例與陷阱（給 agent 讀）：[editor-tooling](../.agents/skills/editor-tooling.md)，本文不重述
> - 執行狀態與下一步：[handoff.md](../handoff.md)

## 系統階層與關係

```text
Yu5h1Lib Editor 擴充
├─ Core Editor（Unity/Editor，編譯成 Yu5h1Lib.Editor.dll）
│  ├─ Editor<T>：所有自訂 Inspector 的基底，把陣列包成 ReorderableListEnhanced
│  ├─ ReorderableListEnhanced：過濾、size 欄位的 IMGUI 集合
│  └─ PropertyDrawer（TypeRestriction、ReadOnly、MinMax…）
├─ common Editor（Packages/common/Editor）
│  ├─ 右鍵選單：CollectionElementMenu 等
│  ├─ PropertyDrawer、Inspector、工具視窗
│  └─ EnhanceCollectionField：集合欄位強化（本次計畫，新增）
└─ Unity 預設 Inspector
   ├─ 有自訂 IMGUI Inspector → 集合是 ReorderableList
   └─ 沒有自訂 Inspector   → 集合是 UI Toolkit ListView
```

集合有兩種繪製方式，功能要同時支援兩者才算對所有集合生效。右鍵選單這類「只加行為」的功能不需要改繪製；要改預覽時，Unity 6 的 `PropertyAttribute.applyToCollection` 讓單一欄位可以接管自己的集合繪製，不必替整個元件換 Inspector。

## 擴充總覽

目前只列出已查證過的項目，其餘類別待盤點。

### 集合

| 擴充 | 位置 | 作用範圍 | 功能 |
|---|---|---|---|
| `ReorderableListEnhanced` | Core `Control/ReorderableListEnhanced.cs` | 繼承 `Editor<T>` 的 Inspector 裡所有陣列 | 過濾搜尋欄、header 上的 size 欄位、多選 |
| `CollectionElementMenu` | common `Editor/MenuItem/CollectionElementMenu.cs` | 所有集合欄位，兩種繪製方式皆可 | 右鍵 Copy／Cut／Paste Elements；在元素上貼上會插入到該位置，在集合標題上貼上會加到尾端 |

### 其他類別

PropertyDrawer、Inspector、右鍵選單、工具視窗、Editor 工具函式：尚未盤點。

## EnhanceCollectionField（更新計畫）

### 目標

集合欄位的強化功能：改善預覽、增加工具。以 attribute 掛在單一集合欄位上（array 與 `List<T>` 皆可），不需要改元件的 Inspector；沒有自訂 Inspector 的元件（UI Toolkit 預設 Inspector）也能使用。

`ReorderableListEnhanced` 目前有其他程式依賴，保持不動；`EnhanceCollectionField` 另外實作。

### 預覽與工具

- header 右上角排列為 `[搜尋欄] + - [count]`，不再有 footer。
- 參數 `bool searchbar`：開啟時在 `+ -` 左邊顯示搜尋欄。用法：`[EnhanceCollectionField(searchbar: true)]`。
- `-` 沿用 Unity 原生 footer 的行為：有選取時刪除選取的元素，沒有選取時刪除最後一個。
- 搜尋中停用拖曳排序，理由同 `ReorderableListEnhanced`：過濾後的順序不是真實 index。

### 做法

- attribute 開啟 `applyToCollection`，它的 PropertyDrawer 收到的是集合本身而不是每個元素。
- 先只實作 IMGUI 的 `OnGUI`／`GetPropertyHeight`。UI Toolkit 預設 Inspector 遇到只有 IMGUI 的 drawer 會自動包進 `IMGUIContainer`，一份實作兩邊都能用。
- 集合本體繼承 `ReorderableList`，這樣 `CollectionElementMenu` 讀得到它的多選。
- drawer 收到的 property 可能是 Inspector 迴圈的 iterator，建立 list 時要傳 `Copy()`；依「目標物件 + 屬性路徑」快取，`SerializedObject` 換掉時重建。
- attribute 放 common Runtime，drawer 放 common Editor。
- 和 `Editor<T>` 並存：`Editor<T>` 照舊把其他陣列包成 `ReorderableListEnhanced`，遇到掛了 `EnhanceCollectionField` 的欄位就跳過，交給 attribute 的 drawer。這是唯一要改 Core 的地方：`EditorAdvanced.TryPrepareList`。

### 驗收

- 同一個欄位在 UI Toolkit 預設 Inspector 和 `Editor<T>` Inspector 都顯示 `EnhanceCollectionField`，而且只包一層。
- header 右上角為 `[搜尋欄] + - [count]`；`searchbar` 關閉時沒有搜尋欄。
- 元素層級的 attribute（例如 `_bindings` 的 `[TypeRestriction]`）在 `EnhanceCollectionField` 裡仍然生效。
- `CollectionElementMenu` 的 Copy／Cut 在 `EnhanceCollectionField` 上能讀到多選數量。
- Undo 對新增、刪除、排序都有效。

### 實作前要實測

- `PropertyAttribute` 設定 `applyToCollection` 的建構子寫法：本機 6000.3.9f1 的 API 說明有這個屬性，但沒有列出建構子。
- header 若沿用 `EditorGUI.PropertyField(headRect, property, false)` 來保留原生右鍵選單，會不會又進到自己的 drawer 造成遞迴。

### 尚未決定

- **是否由 `EnhanceCollectionField` 取代 `Editor<T>` 的自動包裝**：等上面的驗收全部通過後再決定。取代的話，`Editor<T>` 不再自動包裝陣列，行為一致，但現有 Inspector 要逐一加上 attribute。
