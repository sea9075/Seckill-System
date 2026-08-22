import http from 'k6/http';
import { check } from 'k6';
import { SharedArray } from 'k6/data';

// 只需要一個帳號的 token，用同一個人連續狂發請求來測限流
const tokens = new SharedArray('tokens', function () {
    return JSON.parse(open('./tokens.json'));
});

const BASE_URL = __ENV.BASE_URL || "http://localhost:8081";
const ACTIVITY_ID = __ENV.ACTIVITY_ID || '1';
const token = tokens[0];

export const options = {
    scenarios: {
        single_user_burst: {
            executor: 'shared-iterations',
            vus: 1,          // 只有一個「人」
            iterations: 10,  // 這個人連續打 10 次
            maxDuration: '10s',
        },
    },
};

export default function () {
    const res = http.post(
        `${BASE_URL}/api/orders`,
        JSON.stringify({ activityId: Number(ACTIVITY_ID), quantity: 1 }),
        { headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` } }
    );

    check(res, {
        '搶購成功 (200)': (r) => r.status === 200,
        '庫存判斷正常拒絕 (400/409)': (r) => r.status === 400 || r.status === 409,
        '被限流擋下 (429)': (r) => r.status === 429,
        '非預期錯誤 (5xx)': (r) => r.status >= 500,
    });
}