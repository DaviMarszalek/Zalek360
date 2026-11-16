"use client";

import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

export interface AcaoLinha {
  rotulo: string;
  icone: ReactNode;
  aoAcionar: () => void;
  desabilitada?: boolean;
  perigo?: boolean;
}

export function AcoesLinha({ acoes, className }: { acoes: AcaoLinha[]; className?: string }) {
  if (acoes.length === 0) return null;

  return (
    <div className={cn("acoes-linha", className)}>
      {acoes.map((acao) => (
        <button
          key={acao.rotulo}
          type="button"
          title={acao.rotulo}
          aria-label={acao.rotulo}
          disabled={acao.desabilitada}
          onClick={(evento) => {
            evento.stopPropagation();
            acao.aoAcionar();
          }}
          className={cn(
            "inline-flex h-8 w-8 items-center justify-center rounded-md text-texto-suave transition-colors hover:bg-slate-100 hover:text-primaria disabled:cursor-not-allowed disabled:opacity-50",
            acao.perigo && "text-red-700 hover:bg-red-50 hover:text-red-800",
          )}
        >
          {acao.icone}
        </button>
      ))}
    </div>
  );
}
