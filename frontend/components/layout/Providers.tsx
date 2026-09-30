"use client";
import { useState } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ToastProvider } from "@/components/ui/toast";

export function Providers({ children }: { children: React.ReactNode }) {
  const [cliente] = useState(() => new QueryClient({
    defaultOptions: {
      queries: { staleTime: 20_000, refetchOnWindowFocus: false, retry: (n, e: unknown) => n < 1 && (e as { response?: { status?: number } })?.response?.status !== 404 },
    },
  }));
  return <QueryClientProvider client={cliente}><ToastProvider>{children}</ToastProvider></QueryClientProvider>;
}
