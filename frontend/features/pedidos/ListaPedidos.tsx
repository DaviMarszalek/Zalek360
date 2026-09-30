"use client";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Info, Package, Search } from "lucide-react";
import { Abas, BadgePedido, Botao, CabecalhoPagina, Cartao, Entrada, ErroCarregamento, Esqueleto, EstadoVazio, Etiqueta, Paginacao, Selecao } from "@/components/ui";
import { FiltroPeriodo, intervaloDoPeriodo, Periodo } from "@/features/comum";
import { pedidosService } from "@/services";
import { useDebounce, useResponsaveis } from "@/hooks";
import { erroDaApi } from "@/lib/api";
import { cn } from "@/lib/utils";
import { rotuloSituacaoPedido, situacoesPedido } from "@/lib/status";
import { data, dataIsoDeInstante, diasEntre, hojeIso, moeda, numeroDocumento } from "@/utils/formatos";
import type { SituacaoPedido } from "@/types/api";

/** Prazo com destaque quando o pedido ainda não foi entregue e está atrasado ou vence em até 2 dias. */
function Prazo({ prazo, situacao }: { prazo: string; situacao: SituacaoPedido }) {
  const emAndamento = situacao !== "Entregue" && situacao !== "Cancelado";
  const dias = diasEntre(hojeIso(), prazo);
  const atrasado = emAndamento && dias < 0;
  const proximo = emAndamento && dias >= 0 && dias <= 2;
  return (
    <span className={cn("numero", atrasado ? "font-medium text-red-700" : proximo ? "font-medium text-amber-700" : "text-texto-suave")}>
      {data(prazo)}{atrasado && <span className="ml-1 text-xs">(atrasado)</span>}{proximo && <span className="ml-1 text-xs">({dias === 0 ? "hoje" : dias === 1 ? "amanhã" : `${dias} dias`})</span>}
    </span>
  );
}

export function ListaPedidos() {
  const params = useSearchParams();
  const router = useRouter();
  const [status, setStatus] = useState<SituacaoPedido | "">((params.get("status") as SituacaoPedido) ?? "");
  // Protótipo: período padrão "Últimos 30 dias"; ao chegar filtrando por status (ex.: dashboard), mostra todo o período.
  const [periodo, setPeriodo] = useState<Periodo>((params.get("periodo") as Periodo) ?? (params.get("status") ? "todos" : "30"));
  const [busca, setBusca] = useState("");
  const [responsavelId, setResponsavelId] = useState("");
  const [pagina, setPagina] = useState(1);
  const termo = useDebounce(busca.trim());
  const responsaveis = useResponsaveis();
  const q = useQuery({
    queryKey: ["pedidos", status, periodo, termo, responsavelId, pagina],
    queryFn: () => pedidosService.listar({ status, busca: termo, responsavelId, pagina, ...intervaloDoPeriodo(periodo) }),
    placeholderData: keepPreviousData,
  });
  const cont = q.data?.contagemPorSituacao;
  const filtrando = !!(termo || responsavelId || periodo !== "30" || status);
  const reiniciar = () => setPagina(1);
  const limpar = () => { setBusca(""); setResponsavelId(""); setPeriodo("todos"); setStatus(""); reiniciar(); };

  return (
    <>
      <CabecalhoPagina titulo="Pedidos" subtitulo="Acompanhe os pedidos originados de orçamentos aprovados." />
      <div className="mb-4 flex flex-wrap items-center gap-3 rounded-lg border border-primaria-borda bg-primaria-clara px-4 py-3 text-sm text-primaria">
        <Info className="h-4 w-4 shrink-0" />
        <p className="min-w-[240px] flex-1"><b>Pedidos são gerados automaticamente a partir de orçamentos aprovados.</b>{" "}
          Não há criação manual. Para gerar um pedido, abra o orçamento e registre a decisão do cliente como Aprovado.</p>
        <Link href="/orcamentos"><Botao tamanho="sm" variante="secundaria">Ir para orçamentos</Botao></Link>
      </div>
      <Cartao semPadding>
        <div className="px-4 pt-2">
          <Abas<SituacaoPedido | ""> ativa={status} aoMudar={(s) => { setStatus(s); reiniciar(); }}
            abas={[{ id: "", rotulo: "Todos", contagem: cont?.Todos }, ...situacoesPedido.map((s) => ({ id: s, rotulo: rotuloSituacaoPedido[s], contagem: cont?.[s] }))]} />
        </div>
        <div className="flex flex-wrap gap-3 border-b border-borda p-4">
          <div className="relative min-w-[240px] flex-1">
            <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-texto-fraco" />
            <Entrada className="pl-9" placeholder="Número do pedido ou orçamento, cliente ou CPF/CNPJ" value={busca}
              onChange={(e) => { setBusca(e.target.value); reiniciar(); }} aria-label="Pesquisar pedidos" />
          </div>
          <Selecao className="w-auto min-w-[170px]" value={responsavelId} onChange={(e) => { setResponsavelId(e.target.value); reiniciar(); }} aria-label="Responsável">
            <option value="">Todos os responsáveis</option>{responsaveis.data?.map((u) => <option key={u.id} value={u.id}>{u.nome}</option>)}
          </Selecao>
          <FiltroPeriodo valor={periodo} aoMudar={(p) => { setPeriodo(p); reiniciar(); }} />
          {filtrando && <Botao variante="fantasma" onClick={limpar}>Limpar filtros</Botao>}
        </div>
        {q.isError ? <div className="p-4"><ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} /></div>
          : !q.data ? <div className="space-y-2 p-4">{Array.from({ length: 6 }).map((_, i) => <Esqueleto key={i} className="h-11" />)}</div>
            : q.data.itens.length === 0 ? <EstadoVazio icone={<Package className="h-5 w-5" />} titulo="Nenhum pedido encontrado"
                descricao={filtrando ? "Ajuste os filtros ou amplie o período." : "Os pedidos aparecem aqui assim que um orçamento é aprovado pelo cliente."}
                acao={filtrando ? <Botao variante="secundaria" onClick={limpar}>Limpar filtros</Botao>
                  : <Link href="/orcamentos?status=AguardandoRetorno"><Botao variante="secundaria">Ver orçamentos aguardando retorno</Botao></Link>} />
              : <>
                <div className="overflow-x-auto"><table className="tabela w-full min-w-[920px] text-sm">
                  <thead><tr><th>Nº pedido</th><th>Nº orçamento</th><th>Cliente</th><th>Data</th><th className="text-right">Valor</th><th>Prazo</th><th>Responsável</th><th>Status</th></tr></thead>
                  <tbody>{q.data.itens.map((p) => (
                    <tr key={p.id} className="cursor-pointer hover:bg-slate-50/60" onClick={() => router.push(`/pedidos/${p.id}`)}>
                      <td className="whitespace-nowrap"><Link href={`/pedidos/${p.id}`} className="numero font-medium text-primaria hover:underline" onClick={(e) => e.stopPropagation()}>{numeroDocumento(p.numero)}</Link>
                        {dataIsoDeInstante(p.criadoEm) === hojeIso() && <Etiqueta className="ml-2 border-emerald-200 bg-emerald-50 text-emerald-700">Novo</Etiqueta>}</td>
                      <td><Link href={`/orcamentos/${p.orcamentoId}`} className="numero text-texto-suave hover:text-primaria hover:underline" onClick={(e) => e.stopPropagation()}>{numeroDocumento(p.orcamentoNumero)}</Link></td>
                      <td className="max-w-[240px] truncate">{p.cliente}</td>
                      <td className="numero text-texto-suave">{data(p.data)}</td>
                      <td className="numero text-right">{moeda(p.valorTotal)}</td>
                      <td><Prazo prazo={p.prazoEntrega} situacao={p.situacao} /></td>
                      <td className="text-texto-suave">{p.responsavel}</td>
                      <td><BadgePedido situacao={p.situacao} /></td>
                    </tr>))}</tbody></table></div>
                <Paginacao pagina={q.data.pagina} totalPaginas={q.data.totalPaginas} total={q.data.total} exibindo={q.data.itens.length} nome={["pedido", "pedidos"]} aoMudar={setPagina} />
              </>}
      </Cartao>
    </>
  );
}
