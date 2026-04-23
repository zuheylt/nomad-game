"use client";

import { useEffect, useState } from "react";
import type { NextPage } from "next";

const BACKEND = process.env.NEXT_PUBLIC_BACKEND_URL ?? "http://localhost:8080";
const GRID_MIN = -10;
const GRID_MAX = 10;

type WorldTile = {
  id: { x: number; y: number };
  ownerWallet: string | null;
  buildingType: string;
  resourceLevel: number;
};

function walletColor(wallet: string): string {
  let hash = 0;
  for (let i = 0; i < wallet.length; i++) hash = wallet.charCodeAt(i) + ((hash << 5) - hash);
  const h = Math.abs(hash) % 360;
  return `hsl(${h}, 70%, 45%)`;
}

const BUILDING_ICONS: Record<string, string> = {
  NONE: "",
  MINE: "⛏",
  FARM: "🌾",
  RAILGUN: "🔫",
  WORKSHOP: "🔧",
};

const WorldPage: NextPage = () => {
  const [tiles, setTiles] = useState<Map<string, WorldTile>>(new Map());
  const [selected, setSelected] = useState<WorldTile | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const data: WorldTile[] = await fetch(
          `${BACKEND}/api/world/tiles?x1=${GRID_MIN}&y1=${GRID_MIN}&x2=${GRID_MAX}&y2=${GRID_MAX}`
        ).then(r => r.json());
        const map = new Map<string, WorldTile>();
        data.forEach(t => map.set(`${t.id.x},${t.id.y}`, t));
        setTiles(map);
      } catch (_) {}
    };
    load();
    const id = setInterval(load, 5000);
    return () => clearInterval(id);
  }, []);

  const size = GRID_MAX - GRID_MIN + 1;

  return (
    <div className="min-h-screen bg-base-100 p-6">
      <h1 className="text-3xl font-bold text-center mb-2 text-primary">World Map</h1>
      <p className="text-center text-gray-400 mb-6 text-sm">Land ownership — refreshes every 5s</p>

      <div className="flex flex-col lg:flex-row gap-6 justify-center items-start">
        {/* Grid */}
        <div
          className="border border-base-300 rounded-xl overflow-hidden"
          style={{ display: "grid", gridTemplateColumns: `repeat(${size}, 28px)` }}
        >
          {Array.from({ length: size }, (_, row) =>
            Array.from({ length: size }, (_, col) => {
              const x = GRID_MIN + col;
              const y = GRID_MAX - row;
              const key = `${x},${y}`;
              const tile = tiles.get(key);
              const bg = tile?.ownerWallet ? walletColor(tile.ownerWallet) : "#1a1a2e";
              const icon = BUILDING_ICONS[tile?.buildingType ?? "NONE"] ?? "";
              return (
                <div
                  key={key}
                  title={`(${x},${y}) ${tile?.ownerWallet ?? "unclaimed"}`}
                  onClick={() => setSelected(tile ?? null)}
                  className="w-7 h-7 flex items-center justify-center text-xs cursor-pointer hover:opacity-80 border border-black/20"
                  style={{ backgroundColor: bg }}
                >
                  {icon}
                </div>
              );
            })
          )}
        </div>

        {/* Legend + Selected */}
        <div className="flex flex-col gap-4 min-w-52">
          <div className="bg-base-200 rounded-xl p-4 border border-base-300">
            <h3 className="font-semibold mb-3">Legend</h3>
            <div className="flex items-center gap-2 mb-1">
              <div className="w-5 h-5 rounded" style={{ backgroundColor: "#1a1a2e" }} />
              <span className="text-sm">Unclaimed</span>
            </div>
            <div className="flex items-center gap-2 mb-1">
              <div className="w-5 h-5 rounded bg-gradient-to-br from-purple-600 to-teal-500" />
              <span className="text-sm">Player-owned</span>
            </div>
          </div>

          {selected ? (
            <div className="bg-base-200 rounded-xl p-4 border border-base-300">
              <h3 className="font-semibold mb-2">Selected Tile</h3>
              <p className="text-sm">
                <span className="text-gray-400">Position: </span>({selected.id.x}, {selected.id.y})
              </p>
              <p className="text-sm mt-1">
                <span className="text-gray-400">Owner: </span>
                <span className="font-mono text-xs">
                  {selected.ownerWallet
                    ? `${selected.ownerWallet.slice(0, 6)}...${selected.ownerWallet.slice(-4)}`
                    : "Unclaimed"}
                </span>
              </p>
              <p className="text-sm mt-1">
                <span className="text-gray-400">Building: </span>
                {selected.buildingType || "NONE"}
              </p>
            </div>
          ) : (
            <div className="bg-base-200 rounded-xl p-4 border border-base-300 text-sm text-gray-500">
              Click a tile to inspect
            </div>
          )}

          <div className="bg-base-200 rounded-xl p-4 border border-base-300">
            <p className="text-sm text-gray-400">Total claimed</p>
            <p className="text-2xl font-bold text-primary">{tiles.size}</p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default WorldPage;
