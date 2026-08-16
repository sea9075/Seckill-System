import { useEffect, useState } from "react";
import { getHealth } from "./api/health";

function App() {
	const [status, setStatus] = useState("檢查中...");

	useEffect(() => {
		getHealth()
			.then((res) => {
				setStatus(`API 連線成功：${res.data.status}`)
			})
			.catch(() => setStatus("API 連線失敗，檢查後端是否啟動、CORS 是否設定正確"));
	}, []);

	return (
		<div>
			{status}
		</div>
	)
}

export default App;