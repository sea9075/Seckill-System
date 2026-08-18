import { useAuth } from "./context/AuthContext";
import { Link, Routes, Route } from "react-router-dom";
import { ProductListPage } from "./pages/ProductListPage";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";
import { MyOrdersPage } from "./pages/MyOrdersPage";
import { AdminPage } from "./pages/AdminPage";

function App() {
	const { token, logout, role } = useAuth();

	return (
		<div>
			<nav>
				<Link to="/">商品列表</Link>
				{token ? (
					<>
						<Link to="/orders">我的訂單</Link>
						{role === "Admin" && <Link to="/admin">後台管理</Link>}
						<button onClick={logout}>登出</button>
					</>
				) : (
					<>
						<Link to="/login">登入</Link>
						<Link to="/register">註冊</Link>
					</>
				)}
			</nav>

			<Routes>
				<Route path="/" element={<ProductListPage />} />
				<Route path="/login" element={<LoginPage />} />
				<Route path="/register" element={<RegisterPage />} />
				<Route path="/orders" element={<MyOrdersPage />} />
				<Route path="/admin" element={<AdminPage />} />
			</Routes>
		</div>
	)
}

export default App;