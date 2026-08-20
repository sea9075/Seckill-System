import http from 'k6/http';
import { check } from 'k6';
import { SharedArray } from 'k6/data';

// 從 tokens.json 讀出所有測試帳號的 JWT，每個 VU（虛擬使用者）對應一個帳號
const tokens = new SharedArray('tokens', function () {
  return JSON.parse(open('./tokens.json'));
});

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8081';
const PRODUCT_ID = __ENV.PRODUCT_ID || '1';

export const options = {
  scenarios: {
    // per-vu-iterations：每個 VU 只送出「一次」請求，且所有 VU 幾乎同時開始，
    // 這樣才能模擬「同一秒瘋狂搶購」的秒殺場景，而不是一般網站的持續性負載測試
    flash_sale_rush: {
      executor: 'per-vu-iterations',
      vus: tokens.length,
      iterations: 1,
      maxDuration: '30s',
    },
  },
};

export default function () {
  const token = tokens[__VU - 1];

  const res = http.post(
    `${BASE_URL}/api/orders`,
    JSON.stringify({ productId: Number(__ENV.PRODUCT_ID || 1), quantity: 1 }),
    {
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
    }
  );

  check(res, {
    '搶購成功 (200)': (r) => r.status === 200,
    '搶購失敗但錯誤碼正確 (400/409)': (r) => r.status === 400 || r.status === 409,
    '非預期錯誤 (5xx)': (r) => r.status >= 500,
  });
}
