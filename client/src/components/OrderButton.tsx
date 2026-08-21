import { useState } from "react";
import { apiClient } from "../api/client";

export function OrderButton({ productId, activityId }: { productId?: number; activityId?: number }) {
    const [status, setStatus] = useState<"idle" | "loading" | "pending" | "success" | "error">("idle");

    const handleClick = async () => {
        setStatus("loading");

        try {
            const res = await apiClient.post("/api/orders", {
                productId, activityId, quantity: 1
            });

            setStatus(res.data.status === "Pending" ? "pending" : "success");
        } catch {
            setStatus("error");
        }
    };

    return (
        <button onClick={handleClick} disabled={status === "loading"}>
            {status === "loading" ? "處理中..." : activityId ? "搶購" : "購買"}
        </button>
    );
}