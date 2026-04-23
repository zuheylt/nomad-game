"use client";

import { useEffect, useState } from "react";

export function AlienTimer({ alienArrivalTime }: { alienArrivalTime: number }) {
  const [secondsLeft, setSecondsLeft] = useState(0);

  useEffect(() => {
    const tick = () => {
      const diff = alienArrivalTime - Math.floor(Date.now() / 1000);
      setSecondsLeft(Math.max(0, diff));
    };
    tick();
    const id = setInterval(tick, 1000);
    return () => clearInterval(id);
  }, [alienArrivalTime]);

  const h = Math.floor(secondsLeft / 3600);
  const m = Math.floor((secondsLeft % 3600) / 60);
  const s = secondsLeft % 60;

  const isUrgent = secondsLeft < 3600;
  const arrived = secondsLeft === 0;

  return (
    <div className={`text-center p-6 rounded-2xl border-2 ${arrived ? "border-red-600 bg-red-950" : isUrgent ? "border-orange-500 bg-orange-950 animate-pulse" : "border-purple-700 bg-purple-950"}`}>
      <p className="text-xs uppercase tracking-widest text-gray-400 mb-1">Alien Arrival</p>
      {arrived ? (
        <p className="text-4xl font-bold text-red-400">ALIENS HAVE ARRIVED</p>
      ) : (
        <p className={`text-5xl font-mono font-bold ${isUrgent ? "text-orange-400" : "text-purple-300"}`}>
          {String(h).padStart(2, "0")}:{String(m).padStart(2, "0")}:{String(s).padStart(2, "0")}
        </p>
      )}
      <p className="text-xs text-gray-500 mt-2">Defend or perish</p>
    </div>
  );
}
