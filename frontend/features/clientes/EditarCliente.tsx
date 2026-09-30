"use client";
import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Carregando, ErroCarregamento } from "@/components/ui";
import { clientesService } from "@/services";
import { erroDaApi } from "@/lib/api";
import { FormularioCliente } from "./FormularioCliente";

/** Carrega o cliente atual (com a versão para controle de concorrência) e abre o formulário em modo edição. */
export function EditarCliente() {
  const { id } = useParams<{ id: string }>();
  const q = useQuery({ queryKey: ["cliente", id, "edicao"], queryFn: () => clientesService.obter(id), gcTime: 0 });
  if (q.isError) return <ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} />;
  if (!q.data) return <Carregando />;
  return <FormularioCliente key={q.data.versao} cliente={q.data} />;
}
