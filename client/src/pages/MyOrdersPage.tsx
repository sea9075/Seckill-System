import { useEffect, useState } from "react";
import { apiClient } from "../api/client";

interface OrderDto {
    id: string;
    productId: number;
    activityId: number | null;
    status: string;
    createdAt: string;
}

export function MyOrdersPage() {
    const [orders, setOrders] = useState<OrderDto[]>([]);

    useEffect(() => {
        apiClient.get<OrderDto[]>("/api/orders")
            .then((res) => {
                setOrders(res.data);
            })
            .catch((error) => {
                console.log(error);
            })
    }, []);

    return (
        <table>
            <thead>
                <tr>
                    <th>訂單編號</th>
                    <th>商品</th>
                    <th>狀態</th>
                    <th>時間</th>
                </tr>
            </thead>
            <tbody>
                {orders.map((o) => (
                    <tr key={o.id}>
                        <td>{o.id}</td>
                        <td>{o.activityId ? `秒殺活動 #${o.activityId}` : `商品 #${o.productId}`}</td>
                        <td>{o.status}</td>
                        <td>{new Date(o.createdAt).toLocaleString()}</td>
                    </tr>
                ))}
            </tbody>
        </table>
    );
}