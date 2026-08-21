# 秒殺系統壓力測試結果記錄

這份文件記錄 Phase 4 每個 Step 改造前後的壓測數據對比。測試腳本統一使用 [k6](https://k6.io/)，執行方式與各項參數見對應的 `4-X` 文件；`seckill-rush.js` 打的是秒殺活動流程（`SeckillOrderService`），`product-rush.js` 打的是一般商品流程（`ProductOrderService`）。

## 測試方式

- **Executor**：`per-vu-iterations`，每個 VU（虛擬使用者）只送出 1 次請求，且所有 VU 幾乎同時開始——模擬「同一瞬間搶購尖峰」，不是持續性負載測試
- **VUs**：50（對應 `tokens.json` 裡 50 個測試帳號）
- 每次測試前都會重置對應的庫存與訂單資料（見各 Step 文件的重置語法），確保數字之間可以直接比較

## 結果對比

| Step | 改造內容 | 測試對象 | VUs | 初始庫存 | 200（成功） | 400/409（正常拒絕） | 5xx（系統錯誤） | 超賣？ | 最終庫存 / 訂單數 |
|---|---|---|---|---|---|---|---|---|---|
| 基準線（Phase 3） | 無，僅 DB Transaction + 基本檢查 | 秒殺活動（`SeckillOrderService`） | 50 | 10 | 50 | 0 | 0 | 是 | 庫存剩 5，但有 50 筆 `Confirmed` 訂單，遠超過庫存 10 |
| Step 2（樂觀鎖） | MSSQL RowVersion + `DbUpdateConcurrencyException` 重試 | 一般商品（`ProductOrderService`） | 50 | 10 | 10 | 40 | 0 | 否 | 庫存剩 0，剛好 10 筆 `Confirmed` 訂單，與庫存數一致 |
| Step 3（Redis 原子扣減） | Redis Lua Script 原子檢查＋扣減 | 秒殺活動（`SeckillOrderService`） | 50 | 10 | 10 | 40 | 0 | 否 | Redis 庫存剩 0，剛好 10 筆 `Confirmed` 訂單；平均延遲 275ms（比 Step 2 快 41%） |
| Step 5（Redis Stream + Worker 非同步） | 扣庫存＋`XADD` 同一支 Lua Script 原子完成，Worker 非同步消費寫入 MSSQL | 秒殺活動（`SeckillOrderService`） | 50 | 10 | 10 | 40 | 0 | 否 | 訂單數精準等於 10；成功路徑平均延遲降到 87.7ms（不再等 MSSQL 寫入） |

## 結果解讀

### 基準線：確認超賣重現

50 個並發請求全部拿到 200，但因為「讀取庫存 → 記憶體扣減 → 寫回」這個流程沒有鎖保護，多個請求在同一時間窗內讀到同一份舊庫存值，各自算出相同的目標值寫回，彼此互相覆蓋（lost update）。最終庫存只反映了其中一部分的扣減效果（這次是降到 5），但 50 筆訂單全部被建立成 `Confirmed`。**訂單數（50）遠超過原始庫存（10）**，確認超賣重現——這是超賣的判斷依據，不是看庫存欄位有沒有變成負數（這個特定寫法的 bug 結構上不會讓庫存變負，只會讓訂單數失控超過庫存）。

### Step 2：樂觀鎖解決一般商品的超賣

針對一般商品下單流程套用 MSSQL RowVersion 樂觀鎖後，50 個並發請求中只有 10 個成功（剛好等於初始庫存），其餘 40 個因為併發衝突被偵測到、正確回應 400/409，沒有任何 5xx 系統錯誤。DB 驗證庫存精準歸零、訂單數精準等於 10，一般商品流程的超賣問題確認解決。

> **注意範圍**：這個 Step 只解決了「一般商品」流程（`ProductOrderService`）的超賣。秒殺活動流程（`SeckillOrderService`）尚未套用任何併發保護，改用 Redis 原子操作解決（見下方 Step 3），不沿用這裡的樂觀鎖做法（原因見 `4-3` 文件開頭的說明）。

### Step 3：Redis 原子扣減解決秒殺活動的超賣

秒殺活動流程改用 Redis Lua Script，把「檢查庫存夠不夠」跟「扣減庫存」包在同一支腳本裡原子執行，靠 Redis 單執行緒模型保證不會有 Phase 3 那種「讀取」和「扣減」中間留空隙的競態條件。50 個並發請求中只有 10 個成功，精準等於初始庫存，其餘 40 個正確回應 400/409。跟 Step 2（MSSQL 樂觀鎖）比較，平均回應延遲從 470ms 降到 275ms（快 41%），因為庫存檢查的成本從「一次 MSSQL round trip + 樂觀鎖重試」變成「一次 Redis round trip」。

### Step 5：Redis Stream + Worker 非同步下單，驗證削峰填谷

把「扣庫存成功後同步寫入 MSSQL 才回應」改成「扣庫存 + 推進 Redis Stream 在同一支 Lua Script 內原子完成，立刻回應使用者」，訂單的實際寫入交給獨立的 `Seckill.Worker` 非同步處理。

驗證方式：測試前先關閉 Worker（`docker compose stop worker`），重跑壓測後查詢 `Orders` 表為空（0 筆），`redis-cli XLEN seckill:orders:stream` 顯示 10（10 個扣庫存成功的請求，訊息全部堆積在 Stream 裡，沒有 Consumer 在處理）。重新啟動 Worker（`docker compose start worker`）後，`Orders` 表在數秒內補齊到 10 筆 `Confirmed` 訂單，`XPENDING` 也歸零，確認訊息被完整消化——這證明了「請求瞬間湧入、但資料庫寫入速度由 Worker 自己控制」的削峰填谷效果。

延遲方面，整體 `http_req_duration` 平均（278ms）跟 Step 3 相近，是因為 50 個請求裡有 40 個（80%）是被快速拒絕的 400/409，兩個版本這部分耗時相近，拉平了整體平均、掩蓋了真正的差異。真正該比較的是**成功路徑**的延遲：這次 10 個成功請求（200）的平均延遲只有 **87.7ms**，因為成功回應不再需要等待 MSSQL 寫入完成，只需要一次 Redis round trip 就能回應使用者。

---

*原始 k6 JSON 匯出檔案存放於 `loadtests/k6/results/`（`baseline.json`、`step2-optimistic-lock.json`、`step3-redis-decrement.json`、`step5-async-worker.json`），供之後 Phase 9 撰寫作品集 README 時引用。*