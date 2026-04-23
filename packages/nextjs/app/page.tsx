"use client";

import { useEffect, useState } from "react";
import type { NextPage } from "next";
import { AlienTimer } from "~~/app/_components/AlienTimer";
import { EventCard } from "~~/app/_components/EventCard";

const BACKEND = process.env.NEXT_PUBLIC_BACKEND_URL ?? "http://localhost:8080";

type GameStatus = {
  alienArrivalTime: number;
  secondsUntilAliens: number;
  prizePoolWei: number;
  playerCount: number;
};

type GameEvent = {
  id: number;
  txHash: string;
  eventType: string;
  payload: string;
  timestamp: string;
};

const Home: NextPage = () => {
  const [status, setStatus] = useState<GameStatus | null>(null);
  const [events, setEvents] = useState<GameEvent[]>([]);

  useEffect(() => {
    const fetchAll = async () => {
      try {
        const [s, e] = await Promise.all([
          fetch(`${BACKEND}/api/game/status`).then(r => r.json()),
          fetch(`${BACKEND}/api/events?limit=20`).then(r => r.json()),
        ]);
        setStatus(s);
        setEvents(e);
      } catch (_) {}
    };
    fetchAll();
    const id = setInterval(fetchAll, 3000);
    return () => clearInterval(id);
  }, []);

  const prizePoolMon = status ? (status.prizePoolWei / 1e18).toFixed(4) : "–";

  return (
    <div className="min-h-screen bg-base-100 p-6">
      <h1 className="text-4xl font-bold text-center mb-2 text-primary">Monad Republic</h1>
      <p className="text-center text-gray-400 mb-8 text-sm">Eco × EVE × Foxhole — on Monad L1</p>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8 max-w-5xl mx-auto">
        {/* Alien Countdown */}
        <div className="md:col-span-2">
          {status ? (
            <AlienTimer alienArrivalTime={status.alienArrivalTime} />
          ) : (
            <div className="skeleton h-36 rounded-2xl" />
          )}
        </div>

        {/* Stats */}
        <div className="grid grid-rows-2 gap-4">
          <div className="bg-base-200 rounded-2xl p-5 border border-base-300 flex flex-col justify-center items-center">
            <p className="text-xs uppercase tracking-widest text-gray-400">Prize Pool</p>
            <p className="text-3xl font-bold text-yellow-400 mt-1">{prizePoolMon} MON</p>
          </div>
          <div className="bg-base-200 rounded-2xl p-5 border border-base-300 flex flex-col justify-center items-center">
            <p className="text-xs uppercase tracking-widest text-gray-400">Players Online</p>
            <p className="text-3xl font-bold text-green-400 mt-1">{status?.playerCount ?? "–"}</p>
          </div>
        </div>
      </div>

      {/* Live Event Feed */}
      <div className="max-w-5xl mx-auto">
        <div className="flex items-center gap-2 mb-4">
          <span className="inline-block w-2 h-2 rounded-full bg-green-500 animate-pulse" />
          <h2 className="font-semibold text-lg">Live On-Chain Events</h2>
          <span className="text-xs text-gray-500 ml-auto">updates every 3s</span>
        </div>

        {events.length === 0 ? (
          <div className="text-center text-gray-500 py-12">
            No events yet — start playing to see on-chain activity here.
          </div>
        ) : (
          <div className="flex flex-col gap-2">
            {events.map(e => (
              <EventCard key={e.id} event={e} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default Home;
