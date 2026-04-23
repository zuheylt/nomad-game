"use client";

import { useState } from "react";
import type { NextPage } from "next";
import { useReadContract, useWriteContract, useWaitForTransactionReceipt } from "wagmi";
import { parseUnits } from "viem";
import { monadTestnet } from "~~/scaffold.config";

const FACTORY_ABI = [
  {
    name: "getAllCurrencies",
    type: "function",
    stateMutability: "view",
    inputs: [],
    outputs: [{ type: "address[]" }],
  },
  {
    name: "createCurrency",
    type: "function",
    stateMutability: "nonpayable",
    inputs: [
      { name: "name",          type: "string"  },
      { name: "symbol",        type: "string"  },
      { name: "initialSupply", type: "uint256" },
      { name: "description",   type: "string"  },
    ],
    outputs: [{ name: "tokenAddress", type: "address" }],
  },
] as const;

const TOKEN_ABI = [
  { name: "name",        type: "function", stateMutability: "view", inputs: [], outputs: [{ type: "string"  }] },
  { name: "symbol",      type: "function", stateMutability: "view", inputs: [], outputs: [{ type: "string"  }] },
  { name: "creator",     type: "function", stateMutability: "view", inputs: [], outputs: [{ type: "address" }] },
  { name: "totalSupply", type: "function", stateMutability: "view", inputs: [], outputs: [{ type: "uint256" }] },
] as const;

const FACTORY = (process.env.NEXT_PUBLIC_CURRENCY_FACTORY_ADDRESS ?? "") as `0x${string}`;
const EXPLORER = "https://testnet.monadexplorer.com";

function CurrencyRow({ address }: { address: `0x${string}` }) {
  const { data: name }   = useReadContract({ address, abi: TOKEN_ABI, functionName: "name",        chainId: monadTestnet.id });
  const { data: symbol } = useReadContract({ address, abi: TOKEN_ABI, functionName: "symbol",      chainId: monadTestnet.id });
  const { data: creator} = useReadContract({ address, abi: TOKEN_ABI, functionName: "creator",     chainId: monadTestnet.id });
  const { data: supply } = useReadContract({ address, abi: TOKEN_ABI, functionName: "totalSupply", chainId: monadTestnet.id });

  return (
    <tr className="hover:bg-base-200 transition-colors">
      <td className="py-3 px-4 font-semibold">{name ?? "..."}</td>
      <td className="py-3 px-4"><span className="badge badge-outline">{symbol ?? "..."}</span></td>
      <td className="py-3 px-4 font-mono text-xs text-gray-400">
        {creator ? `${(creator as string).slice(0,6)}...${(creator as string).slice(-4)}` : "..."}
      </td>
      <td className="py-3 px-4 text-sm">
        {supply != null ? Number(BigInt(supply as bigint) / 10n ** 18n).toLocaleString() : "..."}
      </td>
      <td className="py-3 px-4 flex gap-2">
        <a href={`${EXPLORER}/token/${address}`} target="_blank" rel="noopener noreferrer"
           className="btn btn-xs btn-outline">Token ↗</a>
      </td>
    </tr>
  );
}

function CreateCurrencyForm({ onCreated }: { onCreated: () => void }) {
  const [name,        setName]        = useState("");
  const [symbol,      setSymbol]      = useState("");
  const [supply,      setSupply]      = useState("1000000");
  const [description, setDescription] = useState("");
  const [error,       setError]       = useState("");

  const { writeContract, data: txHash, isPending } = useWriteContract();
  const { isLoading: isConfirming, isSuccess } = useWaitForTransactionReceipt({ hash: txHash });

  if (isSuccess) {
    onCreated();
  }

  function submit(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    if (!name || !symbol || !supply) { setError("Name, symbol, and supply are required."); return; }
    try {
      writeContract({
        address: FACTORY,
        abi: FACTORY_ABI,
        functionName: "createCurrency",
        args: [name, symbol, parseUnits(supply, 18), description],
        chainId: monadTestnet.id,
      });
    } catch (err: any) {
      setError(err.message ?? "Unknown error");
    }
  }

  return (
    <div className="bg-base-200 rounded-xl p-6 border border-base-300 mb-6">
      <h2 className="text-lg font-bold mb-4">💰 Create a Player Currency</h2>
      <form onSubmit={submit} className="grid grid-cols-2 gap-3">
        <div className="form-control">
          <label className="label"><span className="label-text">Currency Name</span></label>
          <input className="input input-bordered" placeholder="Nomad Gold" value={name}
                 onChange={e => setName(e.target.value)} />
        </div>
        <div className="form-control">
          <label className="label"><span className="label-text">Symbol</span></label>
          <input className="input input-bordered" placeholder="NGD" value={symbol}
                 onChange={e => setSymbol(e.target.value.toUpperCase())} maxLength={8} />
        </div>
        <div className="form-control">
          <label className="label"><span className="label-text">Initial Supply</span></label>
          <input className="input input-bordered" type="number" min="1" value={supply}
                 onChange={e => setSupply(e.target.value)} />
        </div>
        <div className="form-control">
          <label className="label"><span className="label-text">Description</span></label>
          <input className="input input-bordered" placeholder="The currency of the north" value={description}
                 onChange={e => setDescription(e.target.value)} />
        </div>

        {error && <p className="col-span-2 text-red-400 text-sm">{error}</p>}

        {txHash && (
          <div className="col-span-2 text-xs text-gray-400 break-all">
            TX: <a href={`${EXPLORER}/tx/${txHash}`} target="_blank" rel="noopener noreferrer"
                   className="text-primary underline">{txHash}</a>
            {isConfirming && <span className="ml-2 text-yellow-400">⏳ confirming...</span>}
            {isSuccess    && <span className="ml-2 text-green-400">✓ confirmed!</span>}
          </div>
        )}

        <div className="col-span-2">
          <button type="submit" className="btn btn-primary w-full"
                  disabled={isPending || isConfirming}>
            {isPending ? "Waiting for wallet..." : isConfirming ? "Confirming..." : "Create Currency on Monad"}
          </button>
        </div>
      </form>
    </div>
  );
}

const EconomyPage: NextPage = () => {
  const { data: currencies, refetch } = useReadContract({
    address: FACTORY,
    abi: FACTORY_ABI,
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
            <p className="text-xs text-gray-400 uppercase tracking-widest">Chain</p>
            <p className="text-sm text-purple-400 mt-1">Monad Testnet</p>
          </div>
          <div className="border-l border-base-300 pl-6">
            <p className="text-xs text-gray-400 uppercase tracking-widest">Factory</p>
            <a href={`${EXPLORER}/address/${FACTORY}`} target="_blank" rel="noopener noreferrer"
               className="text-xs font-mono text-gray-400 hover:text-primary mt-1 block">
              {FACTORY ? `${FACTORY.slice(0,6)}...${FACTORY.slice(-4)}` : "not set"}
            </a>
          </div>
        </div>

        <CreateCurrencyForm onCreated={() => setTimeout(() => refetch(), 3000)} />

        {list.length === 0 ? (
          <div className="text-center text-gray-500 py-12">
            <p className="text-4xl mb-4">💰</p>
            <p>No currencies yet — create the first one above.</p>
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
                {list.map(addr => <CurrencyRow key={addr} address={addr} />)}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default EconomyPage;
