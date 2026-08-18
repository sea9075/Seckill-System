import { useEffect, useState } from "react";
import { apiClient } from "../api/client";
import { CountdownTimer } from "../components/CountdownTimer";
import { OrderButton } from "../components/OrderButton";

interface ProductDto {
    id: number;
    name: string;
    price: number;
    stock: number;
}

interface SeckillActivityDto {
    id: number;
    productId: number;
    startTime: string;
    endTime: string;
    seckillStock: number;
    isHighTraffic: boolean;
}

export function ProductListPage() {
    const [products, setProducts] = useState<ProductDto[]>([]);
    const [activities, setActivities] = useState<SeckillActivityDto[]>([]);

    useEffect(() => {
        apiClient.get<ProductDto[]>("/api/products")
            .then((res) => setProducts(res.data))
            .catch((error) => console.log(error.response));
        apiClient.get<SeckillActivityDto[]>("/api/seckill-activities")
            .then((res) => setActivities(res.data))
            .catch((error) => console.log(error.response));
    }, []);

    return (
        <div>
            <h2>秒殺活動</h2>
            {activities.map((a) => (
                <div key={a.id}>
                    <span>商品 #{a.productId} (剩 {a.seckillStock} 件)</span>
                    <CountdownTimer targetTime={a.endTime} />
                    <OrderButton activityId={a.id} />
                </div>
            ))}

            <h2>一般商品</h2>
            {products.map((p) => (
                <div key={p.id}>
                    <span>{p.name} - ${p.price} (庫存 {p.stock})</span>
                    <OrderButton productId={p.id} />
                </div>
            ))}
        </div>
    );
}