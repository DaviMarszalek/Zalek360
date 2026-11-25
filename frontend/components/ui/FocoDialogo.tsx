"use client";
import { createContext, useContext, useEffect, useRef, type ReactNode } from "react";

type OrigemFoco = { elemento: HTMLElement; rotulo: string | null };
const ContextoFoco = createContext<{ current: OrigemFoco | null } | null>(null);

export function ProvedorFocoDialogo({ children }: { children: ReactNode }) {
  const origem = useRef<OrigemFoco | null>(null);
  useEffect(() => {
    function registrar(evento: FocusEvent) {
      const elemento = evento.target;
      if (elemento instanceof HTMLElement && elemento !== document.body && !elemento.closest('[role="dialog"]'))
        origem.current = { elemento, rotulo: elemento.getAttribute("aria-label") };
    }
    document.addEventListener("focusin", registrar);
    return () => document.removeEventListener("focusin", registrar);
  }, []);
  return <ContextoFoco.Provider value={origem}>{children}</ContextoFoco.Provider>;
}

export const useOrigemFocoDialogo = () => useContext(ContextoFoco);
