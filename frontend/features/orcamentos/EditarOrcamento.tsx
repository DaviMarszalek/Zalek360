"use client";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Lock } from "lucide-react";
import { BadgeOrcamento, Botao, CabecalhoPagina, Cartao, Carregando, ErroCarregamento } from "@/components/ui";
import { Voltar } from "@/features/comum";
import { orcamentosService } from "@/services";
import { erroDaApi } from "@/lib/api";
import { numeroDocumento } from "@/utils/formatos";
import { FormularioOrcamento } from "./FormularioOrcamento";

/** Edição só é permitida em Em Elaboração / Aguardando Retorno; nos demais status mostra o bloqueio (o backend também rejeita). */
export function EditarOrcamento() {
  const { id } = useParams<{ id: string }>();
  const q = useQuery({ queryKey: ["orcamento", id, "edicao"], queryFn: () => orcamentosService.obter(id), gcTime: 0 });
  if (q.isError) return <ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} />;
  if (!q.data) return <Carregando />;
  const o = q.data;
  if (!o.podeSerEditado)
    return (
      <>
        <CabecalhoPagina voltar={<Voltar href={`/orcamentos/${o.id}`}>Orçamento {numeroDocumento(o.numero)}</Voltar>}
          titulo={<span className="flex items-center gap-3">Editar orçamento <span className="numero">{numeroDocumento(o.numero)}</span><BadgeOrcamento situacao={o.situacao} /></span>} />
        <Cartao className="max-w-2xl">
          <div className="flex gap-4 text-sm">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-slate-100 text-texto-suave"><Lock className="h-5 w-5" /></div>
            <div>
              <p className="text-base font-semibold">Este orçamento não pode mais ser editado.</p>
              <p className="mt-1 text-texto-suave">Somente orçamentos Em Elaboração ou Aguardando Retorno aceitam alterações. Orçamentos decididos preservam as condições que o cliente recebeu.</p>
              <div className="mt-4 flex flex-wrap gap-2">
                <Link href={`/orcamentos/${o.id}`}><Botao variante="secundaria">Voltar ao orçamento</Botao></Link>
                <Link href={`/orcamentos/novo?clienteId=${o.cliente.id}`}><Botao>Novo orçamento para o cliente</Botao></Link>
              </div>
            </div>
          </div>
        </Cartao>
      </>
    );
  return <FormularioOrcamento key={o.versao} orcamento={o} />;
}
