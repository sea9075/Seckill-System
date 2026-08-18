import { createContext, useContext, useState, type ReactNode } from "react";

interface AuthContextValue {
    token: string | null;
    role: string | null;
    login: (token: string) => void;
    logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

// AuthService.GenerateJwtToken 用 ClaimTypes.Role 建立 Role claim
// System.IdentityModel.Tokens.Jwt 寫進 JWT payload 時，claim type 會照原樣寫成完整長網址
// 不會被縮短成 "role"，所以前端解析時要用這個完整字串當 JSON key 去讀，不能只寫 "role"
const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

function decodeRole(token: string): string | null {
    try {
        const payload = token.split(".")[1];
        const base64 = payload.replace(/-/g, "+").replace(/_/g, "/");
        const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
        const json = JSON.parse(atob(padded));
        console.log("JWT payload:", json); // 先印出完整內容，確認 role 的 key 到底叫什麼
        return json[ROLE_CLAIM] ?? json["role"] ?? null;
    } catch (e) {
        console.error("JWT 解析失敗", e); // 原本這裡是靜默吞掉，先印出來看是不是真的解析失敗
        return null;
    }
}

export function AuthProvider({ children }: { children: ReactNode }) {
    const [token, setToken] = useState<string | null>(localStorage.getItem("token"));
    const [role, setRole] = useState<string | null>(token ? decodeRole(token) : null);

    const login = (t: string) => {
        localStorage.setItem("token", t);
        setToken(t);
        setRole(decodeRole(t));
    };

    const logout = () => {
        localStorage.removeItem("token");
        setToken(null);
        setRole(null);
    };

    return <AuthContext.Provider value={{ token, role, login, logout }}>
        {children}
    </AuthContext.Provider>;
}

export function useAuth() {
    const ctx = useContext(AuthContext);
    if (!ctx) throw new Error("useAuth 必須在 AuthProvider 裡使用");
    return ctx;
}