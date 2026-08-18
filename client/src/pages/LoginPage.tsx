import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { apiClient } from "../api/client";
import { useAuth } from "../context/AuthContext";

export function LoginPage() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const { login } = useAuth();
    const navigate = useNavigate();

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError(null);

        try {
            const res = await apiClient.post<{ token: string }>(
                "/api/auth/login",
                { email, password }
            );
            login(res.data.token);
            navigate("/");
        } catch {
            setError("帳號或密碼錯誤");
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <h2>登入</h2>
            <input type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)} required />
            <input type="password" placeholder="密碼" value={password} onChange={(e) => setPassword(e.target.value)} required />
            {error && <p>{error}</p>}
            <button type="submit">登入</button>
        </form>
    )
}