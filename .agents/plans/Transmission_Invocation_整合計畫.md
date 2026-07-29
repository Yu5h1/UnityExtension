# Transmission × Invocation 整合計畫

## 計畫目標

建立 Transmission 與 Invocation 的清楚整合邊界：Transmission 負責尋找接收器並傳遞帶具名引數的訊息，Invocation 負責解析資料、匹配動作並執行。透過一致的名稱合約，讓觸發端與接收端能獨立序列化，並使行為相同的互動目標共用執行設定。

## 解決問題

- 明確劃分訊息投遞、資料解析與動作執行的責任，避免兩套系統彼此耦合。
- 解決 prefab 與場景物件直接互相引用所造成的序列化限制。
- 讓不同互動目標只需提供各自資料，不必重複建立相同行為設定。
- 以名稱合約連結 `ArgumentSet`、`ActionGroup`、`Argument` 與 `Action`，避免依賴索引或跨 prefab 參照。
