import { useEffect, useState } from "react";

export function CountdownTimer({ targetTime }: { targetTime: string }) {
    const [remaining, setRemaining] = useState(() => new Date(targetTime).getTime() - Date.now());

    useEffect(() => {
        const timer = setInterval(() => {
            setRemaining(new Date(targetTime).getTime() - Date.now());
        }, 1000);

        return () => clearInterval(timer);
    }, [targetTime]);

    if (remaining <= 0) return <span>已結束</span>

    const totalSeconds = Math.floor(remaining / 1000);
    const h = Math.floor(totalSeconds / 3600);
    const m = Math.floor((totalSeconds % 3600) / 60);
    const s = totalSeconds % 60;

    return <span>{`${h}:${String(m).padStart(2, "0")}:${String(s).padStart(2, "0")}`}</span>;
}