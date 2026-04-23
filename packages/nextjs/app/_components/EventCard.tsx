"use client";

const EVENT_COLORS: Record<string, string> = {
  TILE_CLAIMED: "badge-success",
  CURRENCY_CREATED: "badge-info",
  DEATH: "badge-error",
  TREASON_ASSIGNED: "badge-warning",
  TREASON_COMPLETED: "badge-warning",
};

const EVENT_ICONS: Record<string, string> = {
  TILE_CLAIMED: "🏕",
  CURRENCY_CREATED: "💰",
  DEATH: "💀",
  TREASON_ASSIGNED: "🗡",
  TREASON_COMPLETED: "🤑",
};

type GameEvent = {
  id: number;
  txHash: string;
  eventType: string;
  payload: string;
  timestamp: string;
};

const EXPLORER = "https://testnet.monadexplorer.com/tx/";

export function EventCard({ event }: { event: GameEvent }) {
  const badgeClass = EVENT_COLORS[event.eventType] ?? "badge-neutral";
  const icon = EVENT_ICONS[event.eventType] ?? "📡";
  const time = new Date(event.timestamp).toLocaleTimeString();

  return (
    <div className="flex items-start gap-3 bg-base-200 rounded-xl px-4 py-3 border border-base-300">
      <span className="text-2xl">{icon}</span>
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <span className={`badge badge-sm ${badgeClass}`}>{event.eventType}</span>
          <span className="text-xs text-gray-500">{time}</span>
        </div>
        {event.txHash && (
          <a
            href={`${EXPLORER}${event.txHash}`}
            target="_blank"
            rel="noopener noreferrer"
            className="text-xs text-blue-400 hover:underline font-mono truncate block mt-0.5"
          >
            {event.txHash.slice(0, 20)}...{event.txHash.slice(-8)}
          </a>
        )}
      </div>
    </div>
  );
}
