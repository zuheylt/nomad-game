"use client";

import type { NextPage } from "next";
import { useReadContract } from "wagmi";
import { monadTestnet } from "~~/scaffold.config";

const CURRENCY_FACTORY_ABI = [
  {
    name: "getAllCurrencies",
    type: "function",
    stateMutability: "view",
    inputs: [],
    outputs: [{ type: "address[]", name: "" }],
  },
  {
    name: "currencyCount",
    type: "function",
    stateMutability: "view",
    inputs: [],
    outputs: [{ type: "uint256", name: "" }],
  },
] as const;

const PLAYER_CURRENCY_ABI = [
  { name: "name", type: "function", stateMutability: "view", inputs: [], outputs: [{ type: "string" }] },
  { name: "symbol", type: "function", stateMutability: "view", inputs: [], outputs: [{ type: "string" }] },
  { name: "creator", type: "function", stateMutability: "view", inputs: [], outputs: [{ type: "address" }] },
  { name: "totalSupply", type: "function", stateMutability: "view", inputs: [], outputs: [{ type: "uint256" }] },
] as const;

const FACTORY_ADDRESS = (process.env.NEXT_PUBLIC_CURRENCY_FACTORY_ADDRESS ?? "") as `0x${string}`;

function CurrencyRow({ address }: { address: `0x${string}` }) {
  const { data: name } = useReadContract({ address, abi: PLAYER_CURRENCY_ABI, functionName: "name", chainId: monadTestnet.id });
  const { data: symbol } = useReadContract({ address, abi: PLAYER_CURRENCY_ABI, functionName: "symbol", chainId: monadTestnet.id });
  const { data: creator } = useReadContract({ address, abi: PLAYER_CURRENCY_ABI, functionName: "creator", chainId: monadTestnet.id });
  const { data: supply } = useReadContract({ address, abi: PLAYER_CURRENCY_ABI, functionName: "totalSupply", chainId: monadTestnet.id });

  const EXPLORER = "https://testnet.monadexplorer.com/token/";

  return (
    <tr className="hover:bg-base-200 transition-colors">
      <td className="py-3 px-4 font-semibold">{name ?? "..."}</td>
      <td className="py-3 px-4">
        <span className="badge badge-outline">{symbol ?? "..."}</span>
      </td>
      <td className="py-3 px-4 font-mono text-xs text-gray-400">
        {creator ? `${(creator as string).slice(0, 6)}...${(creator as string).slice(-4)}` : "..."}
      </td>
      <td className="py-3 px-4 text-sm">
        {supply ? Number(BigInt(supply as bigint) / 10n ** 18n).toLocaleString() : "..."}
      </td>
      <td className="py-3 px-4">
        <a
          href={`${EXPLORER}${address}`}
          target="_blank"
          rel="noopener noreferrer"
          className="btn btn-xs btn-outline"
        >
          Explorer ↗
        </a>
      </td>
    </tr>
  );
}

const EconomyPage: NextPage = () => {
  const { data: currencies } = useReadContract({
    address: FACTORY_ADDRESS,
    abi: CURRENCY_FACTORY_ABI,
    functionName: "getAllCurrencies",
    chainId: monadTestnet.id,
  });

  const list = (currencies as `0x${string}`[] | undefined) ?? [];

  return (
    <div className="min-h-screen bg-base-100 p-6">
      <h1 className="text-3xl font-bold text-center mb-2 text-primary">Economy</h1>
      <p className="text-center text-gray-400 mb-8 text-sm">Player-created currencies on Monad L1</p>

      <div className="max-w-4xl mx-auto">
        <div className="bg-base-200 rounded-xl p-4 border border-base-300 mb-6 flex gap-6">
          <div>
            <p className="text-xs text-gray-400 uppercase tracking-widest">Currencies</p>
            <p className="text-3xl font-bold text-primary">{list.length}</p>
          </div>
          <div className="border-l border-base-300 pl-6">
            <p className="text-xs text-gray-400 uppercase tracking-widest">AMM Pricing</p>
            <p className="text-sm text-gray-500 mt-1">Dynamic (supply/demand)</p>
          </div>
          <div className="border-l border-base-300 pl-6">
            <p className="text-xs text-gray-400 uppercase tracking-widest">Default Currency</p>
            <p className="text-sm text-yellow-400 mt-1">None — players decide</p>
          </div>
        </div>

        {list.length === 0 ? (
          <div className="text-center text-gray-500 py-16">
            <p className="text-4xl mb-4">💰</p>
            <p>No currencies created yet.</p>
            <p className="text-sm mt-2">Deploy the contracts and use the game to create player currencies.</p>
            {!FACTORY_ADDRESS && (
              <p className="text-xs text-red-400 mt-4">
                NEXT_PUBLIC_CURRENCY_FACTORY_ADDRESS is not set in .env.local
              </p>
            )}
          </div>
        ) : (
          <div className="overflow-x-auto rounded-xl border border-base-300">
            <table className="table w-full">
              <thead>
                <tr className="bg-base-300">
                  <th className="px-4 py-3 text-left">Name</th>
                  <th className="px-4 py-3 text-left">Symbol</th>
                  <th className="px-4 py-3 text-left">Creator</th>
                  <th className="px-4 py-3 text-left">Total Supply</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {list.map(addr => (
                  <CurrencyRow key={addr} address={addr} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default EconomyPage;
