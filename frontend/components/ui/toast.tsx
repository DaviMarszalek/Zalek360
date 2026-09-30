"use client";
import * as React from "react";
import { CheckCircle2, AlertCircle, X } from "lucide-react";
import { cn } from "@/lib/utils";

type Toast = { id: number; tipo: "sucesso" | "erro"; mensagem: string };
const Contexto = React.createContext<(tipo: Toast["tipo"], mensagem: string) => void>(() => {});

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [itens, setItens] = React.useState<Toast[]>([]);
  const notificar = React.useCallback((tipo: Toast["tipo"], mensagem: string) => {
    const id = Date.now() + Math.random();
    setItens((l) => [...l, { id, tipo, mensagem }]);
    setTimeout(() => setItens((l) => l.filter((t) => t.id !== id)), 5000);
  }, []);
  return (
    <Contexto.Provider value={notificar}>
      {children}
      <div className="pointer-events-none fixed bottom-4 right-4 z-[60] flex w-[360px] max-w-[calc(100vw-2rem)] flex-col gap-2" aria-live="polite">
        {itens.map((t) => (
          <div key={t.id} className={cn("pointer-events-auto flex items-start gap-2.5 rounded-lg border bg-white px-4 py-3 text-sm shadow-modal",
            t.tipo === "sucesso" ? "border-emerald-200" : "border-red-200")}>
            {t.tipo === "sucesso" ? <CheckCircle2 className="mt-0.5 h-4 w-4 text-emerald-600" /> : <AlertCircle className="mt-0.5 h-4 w-4 text-red-600" />}
            <p className="flex-1 text-texto">{t.mensagem}</p>
            <button onClick={() => setItens((l) => l.filter((x) => x.id !== t.id))} aria-label="Fechar"><X className="h-4 w-4 text-texto-fraco" /></button>
          </div>
        ))}
      </div>
    </Contexto.Provider>
  );
}

export const useToast = () => React.useContext(Contexto);
