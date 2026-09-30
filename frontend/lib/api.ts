import axios, { AxiosError } from "axios";
import type { ErroApi } from "@/types/api";

/** Cliente HTTP único. O token viaja no cookie httpOnly (mesmo domínio via rewrite /api → backend). */
export const api = axios.create({
  baseURL: "/api",
  withCredentials: true,
  timeout: 60_000,
  headers: { Accept: "application/json" },
});

api.interceptors.response.use(
  (resposta) => resposta,
  (erro: AxiosError<ErroApi>) => {
    const url = erro.config?.url ?? "";
    if (erro.response?.status === 401 && typeof window !== "undefined" && !url.includes("/auth/login") && !url.includes("/auth/me")) {
      const destino = window.location.pathname + window.location.search;
      window.location.href = `/login?redirect=${encodeURIComponent(destino)}`;
    }
    return Promise.reject(erro);
  },
);

/** Normaliza qualquer falha para o formato de erro da API. */
export function erroDaApi(erro: unknown): ErroApi {
  if (axios.isAxiosError(erro)) {
    const dados = erro.response?.data as Partial<ErroApi> | undefined;
    if (dados && typeof dados === "object" && "code" in dados) {
      return { status: erro.response!.status, code: dados.code ?? "ERRO", message: dados.message ?? "Não foi possível concluir a operação.", errors: dados.errors ?? {}, details: dados.details, traceId: dados.traceId };
    }
    if (!erro.response) return { status: 0, code: "SEM_CONEXAO", message: "Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.", errors: {} };
    return { status: erro.response.status, code: "ERRO", message: "Não foi possível concluir a operação. Tente novamente.", errors: {} };
  }
  return { status: 0, code: "ERRO", message: "Ocorreu um erro inesperado.", errors: {} };
}

/** Primeira mensagem de um campo (aceita chaves como "itens[0].quantidade"). */
export function erroCampo(erro: ErroApi | null | undefined, campo: string): string | undefined {
  return erro?.errors?.[campo]?.[0];
}
