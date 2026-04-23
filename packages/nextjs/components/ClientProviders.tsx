"use client";

import dynamic from "next/dynamic";

const ScaffoldEthAppWithProviders = dynamic(
  () => import("~~/components/ScaffoldEthAppWithProviders").then(m => m.ScaffoldEthAppWithProviders),
  { ssr: false }
);

export function ClientProviders({ children }: { children: React.ReactNode }) {
  return <ScaffoldEthAppWithProviders>{children}</ScaffoldEthAppWithProviders>;
}
