"use client";
import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { authService, catalogoService } from "@/services";

export function useDebounce<T>(valor: T, atraso = 350): T {
  const [atual, setAtual] = useState(valor);
  useEffect(() => {
    const t = setTimeout(() => setAtual(valor), atraso);
    return () => clearTimeout(t);
  }, [valor, atraso]);
  return atual;
}

export const useUsuarioAtual = () =>
  useQuery({ queryKey: ["auth", "me"], queryFn: authService.me, staleTime: 5 * 60_000, retry: false });

export const useProdutos = () => useQuery({ queryKey: ["catalogo", "produtos"], queryFn: catalogoService.produtos, staleTime: 10 * 60_000 });
export const useOpcoesOrcamento = () => useQuery({ queryKey: ["catalogo", "opcoes"], queryFn: catalogoService.opcoes, staleTime: 30 * 60_000 });
export const useResponsaveis = () => useQuery({ queryKey: ["catalogo", "responsaveis"], queryFn: catalogoService.responsaveis, staleTime: 10 * 60_000 });
