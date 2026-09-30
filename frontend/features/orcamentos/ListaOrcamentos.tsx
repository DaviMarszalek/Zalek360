"use client";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { FileText, Plus, Search } from "lucide-react";
import { Abas, BadgeOrcamento, Botao, CabecalhoPagina, Cartao, Entrada, ErroCarregamento, Esqueleto, EstadoVazio, Paginacao, Selecao } from "@/components/ui";
import { FiltroPeriodo, intervaloDoPeriodo, Periodo } from "@/features/comum";
import { orcamentosService } from "@/services";
import { useDebounce, useResponsaveis } from "@/hooks";
import { erroDaApi } from "@/lib/api";
import { rotuloSituacaoOrcamento, situacoesOrcamento } from "@/lib/status";
import { data, moeda, numeroDocumento } from "@/utils/formatos";
import type { SituacaoOrcamento } from "@/types/api";

export function ListaOrcamentos() {
  const params = useSearchParams();
  const router = useRouter();
  const [status, setStatus] = useState<SituacaoOrcamento | "">((params.get("status") as SituacaoOrcamento) ?? "");
  const [periodo, setPeriodo] = useState<Periodo>((params.get("periodo") as Periodo) ?? (params.get("status") ? "todos" : "30"));
  const [busca, setBusca] = useState("");
  const [responsavelId, setResponsavelId] = useState("");
  const [pagina, setPagina] = useState(1);
  const termo = useDebounce(busca.trim());
  const responsaveis = useResponsaveis();
  const q = useQuery({
    queryKey: ["orcamentos", status, periodo, termo, responsavelId, pagina],
    queryFn: () => orcamentosService.listar({ status, busca: termo, responsavelId, pagina, ...intervaloDoPeriodo(periodo) }),
    placeholderData: keepPreviousData,
  });
  const cont = q.data?.contagemPorSituacao;
  const filtrando = !!(termo || responsavelId || periodo !== "30" || status);
  const reiniciar = () => setPagina(1);

  return (
    <>
      <CabecalhoPagina titulo="Orçamentos" subtitulo="Propostas comerciais — do rascunho à decisão do cliente."
        acoes={<Link href="/orcamentos/novo"><Botao icone={<Plus className="h-4 w-4" />}>Novo orçamento</Botao></Link>} />
      <Cartao semPadding>
        <div className="px-4 pt-2">
          <Abas<SituacaoOrcamento | ""> ativa={status} aoMudar={(s) => { setStatus(s); reiniciar(); }}
            abas={[{ id: "", rotulo: "Todos", contagem: cont?.Todos }, ...situacoesOrcamento.map((s) => ({ id: s, rotulo: rotuloSituacaoOrcamento[s], contagem: cont?.[s] }))]} />
        </div>
        <div className="flex flex-wrap gap-3 border-b border-borda p-4">
          <div className="relative min-w-[240px] flex-1">
            <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-texto-fraco" />
            <Entrada className="pl-9" placeholder="Número (#000123), cliente ou CPF/CNPJ" value={busca} onChange={(e) => { setBusca(e.target.value); reiniciar(); }} aria-label="Pesquisar orçamentos" />
          </div>
          <Selecao className="w-auto min-w-[170px]" value={responsavelId} onChange={(e) => { setResponsavelId(e.target.value); reiniciar(); }} aria-label="Responsável">
            <option value="">Todos os responsáveis</option>{responsaveis.data?.map((u) => <option key={u.id} value={u.id}>{u.nome}</option>)}
          </Selecao>
          <FiltroPeriodo valor={periodo} aoMudar={(p) => { setPeriodo(p); reiniciar(); }} />
          {filtrando && <Botao variante="fantasma" onClick={() => { setBusca(""); setResponsavelId(""); setPeriodo("todos"); setStatus(""); reiniciar(); }}>Limpar filtros</Botao>}
        </div>
        {q.isError ? <div className="p-4"><ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} /></div>
          : !q.data ? <div className="space-y-2 p-4">{Array.from({ length: 6 }).map((_, i) => <Esqueleto key={i} className="h-11" />)}</div>
            : q.data.itens.length === 0 ? <EstadoVazio icone={<FileText className="h-5 w-5" />} titulo="Nenhum orçamento encontrado"
                descricao={filtrando ? "Ajuste os filtros ou amplie o período." : "Crie o primeiro orçamento para um cliente cadastrado."}
                acao={filtrando ? <Botao variante="secundaria" onClick={() => { setBusca(""); setResponsavelId(""); setPeriodo("todos"); setStatus(""); }}>Limpar filtros</Botao>
                  : <Link href="/orcamentos/novo"><Botao icone={<Plus className="h-4 w-4" />}>Novo orçamento</Botao></Link>} />
              : <>
                <div className="overflow-x-auto"><table className="tabela w-full min-w-[900px] text-sm">
                  <thead><tr><th>Número</th><th>Cliente</th><th>Data</th><th>Validade</th><th>Itens</th><th className="text-right">Valor</th><th>Responsável</th><th>Status</th></tr></thead>
                  <tbody>{q.data.itens.map((o) => (
                    <tr key={o.id} className="cursor-pointer hover:bg-slate-50/60" onClick={() => router.push(`/orcamentos/${o.id}`)}>
                      <td><Link href={`/orcamentos/${o.id}`} className="numero font-medium text-primaria hover:underline" onClick={(e) => e.stopPropagation()}>{numeroDocumento(o.numero)}</Link></td>
                      <td className="max-w-[240px] truncate">{o.cliente}</td>
                      <td className="numero text-texto-suave">{data(o.data)}</td>
                      <td className="numero text-texto-suave">{data(o.validade)}</td>
                      <td className="text-texto-suave">{o.quantidadeItens}</td>
                      <td className="numero text-right">{moeda(o.valorTotal)}</td>
                      <td className="text-texto-suave">{o.responsavel}</td>
                      <td><BadgeOrcamento situacao={o.situacao} /></td>
                    </tr>))}</tbody></table></div>
                <Paginacao pagina={q.data.pagina} totalPaginas={q.data.totalPaginas} total={q.data.total} exibindo={q.data.itens.length} nome={["orçamento", "orçamentos"]} aoMudar={setPagina} />
              </>}
      </Cartao>
    </>
  );
}
