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
| Step 3（Redis 原子扣減） | Redis Lua Script 原子檢查+扣減 | 秒殺活動（`SeckillOrderService`） | 50 | 10 | 10 | 40 | 0 | 否 | 庫存剩 0，剛好 10 筆 `Confirmed` 訂單；平均延遲 275ms（比 Step 2 快 41%） |

## 結果解讀

### 基準線：確認超賣重現

50 個並發請求全部拿到 200，但因為「讀取庫存 → 記憶體扣減 → 寫回」這個流程沒有鎖保護，多個請求在同一時間窗內讀到同一份舊庫存值，各自算出相同的目標值寫回，彼此互相覆蓋（lost update）。最終庫存只反映了其中一部分的扣減效果（這次是降到 5），但 50 筆訂單全部被建立成 `Confirmed`。**訂單數（50）遠超過原始庫存（10）**，確認超賣重現——這是超賣的判斷依據，不是看庫存欄位有沒有變成負數（這個特定寫法的 bug 結構上不會讓庫存變負，只會讓訂單數失控超過庫存）。

### Step 2：樂觀鎖解決一般商品的超賣

針對一般商品下單流程套用 MSSQL RowVersion 樂觀鎖後，50 個並發請求中只有 10 個成功（剛好等於初始庫存），其餘 40 個因為併發衝突被偵測到、正確回應 400/409，沒有任何 5xx 系統錯誤。DB 驗證庫存精準歸零、訂單數精準等於 10，一般商品流程的超賣問題確認解決。

> **注意範圍**：這個 Step 目前只驗證了「一般商品」流程（`ProductOrderService`）。秒殺活動流程（`SeckillOrderService`）尚未套用任何併發保護，會在 `4-3` 改用 Redis 原子操作解決（不沿用 `4-2` 的樂觀鎖做法，原因見 `4-3` 文件開頭的說明），屆時會補上秒殺活動流程的「Step 3」對比數據。

---

*原始 k6 JSON 匯出檔案存放於 `loadtests/k6/results/`（`baseline.json`、`step2-optimistic-lock.json`），供之後 Phase 9 撰寫作品集 README 時引用。*