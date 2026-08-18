import { useState, type FormEvent } from "react";
import { apiClient } from "../api/client";
import axios from "axios";

export function AdminPage() {
    return (
        <div>
            <h2>後台管理</h2>
            <CreateProductForm />
            <CreateActivityForm />
        </div>
    );
}

function CreateProductForm() {
    const [name, setName] = useState("");
    const [price, setPrice] = useState("");
    const [stock, setStock] = useState("");
    const [message, setMessage] = useState<string | null>(null);

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();

        setMessage(null);

        try {
            await apiClient.post("/api/products", {
                name, price: Number(price), stock: Number(stock)
            });
            setMessage("商品建立成功");
            setName("");
            setPrice("");
            setStock("");
        } catch (error) {
            const msg = axios.isAxiosError(error) ? error.response?.data?.message : null;
            setMessage(msg ?? "建立失敗");
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <h3>新增商品</h3>
            <input placeholder="名稱" value={name} onChange={(e) => setName(e.target.value)} required />
            <input type="number" placeholder="價格" value={price} onChange={(e) => setPrice(e.target.value)} required />
            <input type="number" placeholder="庫存" value={stock} onChange={(e) => setStock(e.target.value)} required />
            {message && <p>{message}</p>}
            <button type="submit">建立商品</button>
        </form>
    )
}

function CreateActivityForm() {
    const [productId, setProductId] = useState("");
    const [start, setStart] = useState("");
    const [end, setEnd] = useState("");
    const [stock, setStock] = useState("");
    const [isHighTraffic, setIsHighTraffic] = useState(false);
    const [message, setMessage] = useState<string | null>(null);

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setMessage(null);

        try {
            await apiClient.post("/api/seckill-activities", {
                productId: Number(productId),
                start: new Date(start).toISOString(),
                end: new Date(end).toISOString(),
                stock: Number(stock),
                isHighTraffic,
            });

            setMessage("活動建立成功");
            setProductId("");
            setStart("");
            setEnd("");
            setStock("");
            setIsHighTraffic(false);
        } catch (error) {
            const msg = axios.isAxiosError(error) ? error.response?.data?.message : null;
            setMessage(msg ?? "建立失敗");
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <h3>新增秒殺活動</h3>
            <input type="number" placeholder="商品 Id" value={productId} onChange={(e) => setProductId(e.target.value)} required />
            <label>
                開始時間
                <input type="datetime-local" value={start} onChange={(e) => setStart(e.target.value)} required />
            </label>
            <label>
                結束時間
                <input type="datetime-local" value={end} onChange={(e) => setEnd(e.target.value)} required />
            </label>
            <input type="number" placeholder="秒殺庫存" value={stock} onChange={(e) => setStock(e.target.value)} required />
            <label>
                <input type="checkbox" checked={isHighTraffic} onChange={(e) => setIsHighTraffic(e.target.checked)} />
                標記為高並發（走 Redis 流程）
            </label>
            {message && <p>{message}</p>}
            <button type="submit">建立活動</button>
        </form>
    );
}