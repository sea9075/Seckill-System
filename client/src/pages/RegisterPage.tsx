import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { apiClient } from "../api/client";

export function RegisterPage() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const navigate = useNavigate();

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError(null);

        try {
            await apiClient.post("/api/auth/register", { email, password });
            navigate("/login");
        } catch {
            setError("註冊失敗，Email 可能已經被使用");
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <h2>註冊</h2>
            <input type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)} required />
            <input type="password" placeholder="密碼" value={password} onChange={(e) => setPassword(e.target.value)} required />
            {error && <p>{error}</p>}
            <button type="submit">註冊</button>
        </form>
    )
}