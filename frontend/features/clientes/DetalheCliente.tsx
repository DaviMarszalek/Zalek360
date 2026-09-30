"use client";
import Link from "next/link";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { FileText, Package, Pencil, Plus, StickyNote } from "lucide-react";
import { Abas, AreaTexto, BadgeOrcamento, BadgePedido, Botao, CabecalhoPagina, Cartao, Carregando, ErroCarregamento, EstadoVazio, Etiqueta, Info2, Selecao } from "@/components/ui";
import { useToast } from "@/components/ui/toast";
import { CartaoCliente, FiltroPeriodo, intervaloDoPeriodo, LinhaDoTempo, Periodo, Voltar } from "@/features/comum";
import { clientesService } from "@/services";
import { erroDaApi } from "@/lib/api";
import { rotuloSituacaoOrcamento, rotuloSituacaoPedido, rotuloTipoPessoa, situacoesOrcamento, situacoesPedido } from "@/lib/status";
import { data, dataDeInstante, dataHora, documento, moeda, numeroDocumento } from "@/utils/formatos";
import type { ClienteDetalhe, SituacaoOrcamento, SituacaoPedido } from "@/types/api";

type Aba = "visao-geral" | "orcamentos" | "pedidos" | "observacoes" | "historico";

export function DetalheCliente() {
  const { id } = useParams<{ id: string }>();
  const params = useSearchParams();
  const router = useRouter();
  const aba = (params.get("aba") as Aba) ?? "visao-geral";
  const q = useQuery({ queryKey: ["cliente", id], queryFn: () => clientesService.obter(id) });

  if (q.isError) return <ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} />;
  if (!q.data) return <Carregando />;
  const c = q.data;
  const mudarAba = (a: Aba) => router.replace(`/clientes/${id}${a === "visao-geral" ? "" : `?aba=${a}`}`, { scroll: false });

  return (
    <>
      <CabecalhoPagina voltar={<Voltar href="/clientes">Clientes</Voltar>}
        titulo={<span className="flex flex-wrap items-center gap-2">{c.nomeExibicao}<Etiqueta>{rotuloTipoPessoa[c.tipoPessoa]}</Etiqueta></span>}
        subtitulo={<span className="numero">{documento(c.documento)}{c.nomeFantasia && c.nomeFantasia !== c.nomeRazaoSocial && <span className="font-sans"> · {c.nomeRazaoSocial}</span>}</span>}
        acoes={<>
          <Link href={`/clientes/${id}/editar`}><Botao variante="secundaria" icone={<Pencil className="h-4 w-4" />}>Editar</Botao></Link>
          <Link href={`/orcamentos/novo?clienteId=${id}`}><Botao icone={<Plus className="h-4 w-4" />}>Novo orçamento</Botao></Link>
        </>} />
      <div className="grid gap-6 lg:grid-cols-[300px_1fr]">
        <div className="space-y-4">
          <Cartao titulo="Dados do cliente">
            <CartaoCliente cliente={{ id: c.id, nome: c.nomeExibicao, tipoPessoa: c.tipoPessoa, documento: c.documento, contatoNome: c.contatoNome, contatoCargo: c.contatoCargo,
              telefone: c.telefone, whatsApp: c.whatsApp, email: c.email, cidade: c.endereco.cidade ? `${c.endereco.cidade}${c.endereco.uf ? `/${c.endereco.uf}` : ""}` : null }} />
            {c.endereco.logradouro && <p className="mt-3 border-t border-borda pt-3 text-[13px] text-texto-suave">
              {c.endereco.logradouro}{c.endereco.numero && `, ${c.endereco.numero}`}{c.endereco.complemento && ` · ${c.endereco.complemento}`}<br />
              {c.endereco.bairro}{c.endereco.cep && <span className="numero"> · CEP {c.endereco.cep.replace(/^(\d{5})(\d{3})$/, "$1-$2")}</span>}</p>}
            <p className="mt-3 border-t border-borda pt-3 text-xs text-texto-fraco">Cliente desde {dataDeInstante(c.criadoEm)}</p>
          </Cartao>
        </div>
        <div className="min-w-0">
          <Abas<Aba> ativa={aba} aoMudar={mudarAba} abas={[
            { id: "visao-geral", rotulo: "Visão geral" }, { id: "orcamentos", rotulo: "Orçamentos", contagem: c.totalOrcamentos },
            { id: "pedidos", rotulo: "Pedidos", contagem: c.totalPedidos }, { id: "observacoes", rotulo: "Observações" }, { id: "historico", rotulo: "Histórico" }]} />
          <div className="mt-5">
            {aba === "visao-geral" && <VisaoGeral id={id} />}
            {aba === "orcamentos" && <AbaOrcamentos cliente={c} />}
            {aba === "pedidos" && <AbaPedidos id={id} />}
            {aba === "observacoes" && <AbaObservacoes id={id} />}
            {aba === "historico" && <AbaHistorico id={id} />}
          </div>
        </div>
      </div>
    </>
  );
}

function VisaoGeral({ id }: { id: string }) {
  const q = useQuery({ queryKey: ["cliente", id, "visao-geral"], queryFn: () => clientesService.visaoGeral(id) });
  if (q.isError) return <ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} />;
  if (!q.data) return <Carregando />;
  const v = q.data;
  return (
    <div className="space-y-5">
      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        {[["Orçamentos", v.totalOrcamentos], ["Aprovados", v.orcamentosAprovados], ["Pedidos", v.totalPedidos], ["Total em pedidos", moeda(v.totalEmPedidos)]].map(([r, n]) => (
          <div key={r as string} className="cartao p-4"><p className="text-xs text-texto-fraco">{r}</p><p className="numero mt-1.5 text-lg font-semibold">{n}</p></div>))}
      </div>
      <div className="grid gap-4 md:grid-cols-2">
        <Cartao titulo="Último orçamento">
          {v.ultimoOrcamento ? <Link href={`/orcamentos/${v.ultimoOrcamento.id}`} className="block">
            <div className="flex items-center justify-between"><span className="numero font-medium text-primaria">{numeroDocumento(v.ultimoOrcamento.numero)}</span><BadgeOrcamento situacao={v.ultimoOrcamento.situacao} /></div>
            <p className="numero mt-2 text-lg font-semibold">{moeda(v.ultimoOrcamento.valorTotal)}</p>
            <p className="mt-1 text-xs text-texto-fraco">{data(v.ultimoOrcamento.data)} · {v.ultimoOrcamento.quantidadeItens} {v.ultimoOrcamento.quantidadeItens === 1 ? "item" : "itens"} · {v.ultimoOrcamento.responsavel}</p>
          </Link> : <p className="text-sm text-texto-fraco">Nenhum orçamento ainda.</p>}
        </Cartao>
        <Cartao titulo="Último pedido">
          {v.ultimoPedido ? <Link href={`/pedidos/${v.ultimoPedido.id}`} className="block">
            <div className="flex items-center justify-between"><span className="numero font-medium text-primaria">{numeroDocumento(v.ultimoPedido.numero)}</span><BadgePedido situacao={v.ultimoPedido.situacao} /></div>
            <p className="numero mt-2 text-lg font-semibold">{moeda(v.ultimoPedido.valorTotal)}</p>
            <p className="mt-1 text-xs text-texto-fraco">{data(v.ultimoPedido.data)} · entrega prevista {data(v.ultimoPedido.prazoEntrega)}</p>
          </Link> : <p className="text-sm text-texto-fraco">Nenhum pedido ainda.</p>}
        </Cartao>
      </div>
      <Cartao titulo="Últimas interações"><LinhaDoTempo eventos={v.ultimasInteracoes} mostrarDocumento vazio="Nenhum contato ou decisão registrados." /></Cartao>
    </div>
  );
}

function AbaOrcamentos({ cliente }: { cliente: ClienteDetalhe }) {
  const [periodo, setPeriodo] = useState<Periodo>("todos");
  const [status, setStatus] = useState<SituacaoOrcamento | "">("");
  const q = useQuery({ queryKey: ["cliente", cliente.id, "orcamentos", periodo, status], queryFn: () => clientesService.orcamentos(cliente.id, { ...intervaloDoPeriodo(periodo), status }) });
  return (
    <Cartao semPadding>
      <div className="flex flex-wrap gap-3 border-b border-borda p-4">
        <FiltroPeriodo valor={periodo} aoMudar={setPeriodo} />
        <Selecao className="w-auto min-w-[180px]" value={status} onChange={(e) => setStatus(e.target.value as SituacaoOrcamento | "")} aria-label="Status">
          <option value="">Todos os status</option>{situacoesOrcamento.map((s) => <option key={s} value={s}>{rotuloSituacaoOrcamento[s]}</option>)}
        </Selecao>
      </div>
      {q.isError ? <div className="p-4"><ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} /></div>
        : !q.data ? <Carregando /> : q.data.totalSemFiltro === 0
          ? <EstadoVazio icone={<FileText className="h-5 w-5" />} titulo="Este cliente ainda não possui orçamentos" descricao="Crie o primeiro orçamento para registrar produtos, personalização e valores."
              acao={<Link href={`/orcamentos/novo?clienteId=${cliente.id}`}><Botao icone={<Plus className="h-4 w-4" />}>Criar orçamento</Botao></Link>} />
          : q.data.itens.length === 0 ? <EstadoVazio titulo="Nenhum orçamento com esses filtros" acao={<Botao variante="secundaria" onClick={() => { setPeriodo("todos"); setStatus(""); }}>Limpar filtros</Botao>} />
            : <div className="overflow-x-auto"><table className="tabela w-full min-w-[640px] text-sm">
              <thead><tr><th>Número</th><th>Data</th><th>Validade</th><th>Itens</th><th className="text-right">Valor</th><th>Status</th></tr></thead>
              <tbody>{q.data.itens.map((o) => (
                <tr key={o.id} className="hover:bg-slate-50/60">
                  <td><Link href={`/orcamentos/${o.id}`} className="numero font-medium text-primaria hover:underline">{numeroDocumento(o.numero)}</Link></td>
                  <td className="numero text-texto-suave">{data(o.data)}</td><td className="numero text-texto-suave">{data(o.validade)}</td>
                  <td className="text-texto-suave">{o.quantidadeItens} · {o.totalUnidades} un.</td>
                  <td className="numero text-right">{moeda(o.valorTotal)}</td><td><BadgeOrcamento situacao={o.situacao} /></td>
                </tr>))}</tbody></table></div>}
    </Cartao>
  );
}

function AbaPedidos({ id }: { id: string }) {
  const [periodo, setPeriodo] = useState<Periodo>("todos");
  const [status, setStatus] = useState<SituacaoPedido | "">("");
  const q = useQuery({ queryKey: ["cliente", id, "pedidos", periodo, status], queryFn: () => clientesService.pedidos(id, { ...intervaloDoPeriodo(periodo), status }) });
  return (
    <Cartao semPadding>
      <div className="flex flex-wrap gap-3 border-b border-borda p-4">
        <FiltroPeriodo valor={periodo} aoMudar={setPeriodo} />
        <Selecao className="w-auto min-w-[170px]" value={status} onChange={(e) => setStatus(e.target.value as SituacaoPedido | "")} aria-label="Status">
          <option value="">Todos os status</option>{situacoesPedido.map((s) => <option key={s} value={s}>{rotuloSituacaoPedido[s]}</option>)}
        </Selecao>
      </div>
      {q.isError ? <div className="p-4"><ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} /></div>
        : !q.data ? <Carregando /> : q.data.totalSemFiltro === 0
          ? <EstadoVazio icone={<Package className="h-5 w-5" />} titulo="Nenhum pedido para este cliente" descricao="Pedidos são gerados automaticamente quando um orçamento é aprovado." />
          : q.data.itens.length === 0 ? <EstadoVazio titulo="Nenhum pedido com esses filtros" />
            : <div className="overflow-x-auto"><table className="tabela w-full min-w-[640px] text-sm">
              <thead><tr><th>Pedido</th><th>Orçamento</th><th>Data</th><th>Entrega prevista</th><th className="text-right">Valor</th><th>Status</th></tr></thead>
              <tbody>{q.data.itens.map((p) => (
                <tr key={p.id} className="hover:bg-slate-50/60">
                  <td><Link href={`/pedidos/${p.id}`} className="numero font-medium text-primaria hover:underline">{numeroDocumento(p.numero)}</Link></td>
                  <td><Link href={`/orcamentos/${p.orcamentoId}`} className="numero text-texto-suave hover:underline">{numeroDocumento(p.orcamentoNumero)}</Link></td>
                  <td className="numero text-texto-suave">{data(p.data)}</td><td className="numero text-texto-suave">{data(p.prazoEntrega)}</td>
                  <td className="numero text-right">{moeda(p.valorTotal)}</td><td><BadgePedido situacao={p.situacao} /></td>
                </tr>))}</tbody></table></div>}
    </Cartao>
  );
}

function AbaObservacoes({ id }: { id: string }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [texto, setTexto] = useState("");
  const q = useQuery({ queryKey: ["cliente", id, "observacoes"], queryFn: () => clientesService.observacoes(id) });
  const adicionar = useMutation({
    mutationFn: () => clientesService.adicionarObservacao(id, texto),
    onSuccess: () => { setTexto(""); qc.invalidateQueries({ queryKey: ["cliente", id, "observacoes"] }); toast("sucesso", "Observação adicionada."); },
    onError: (e) => toast("erro", erroDaApi(e).errors.texto?.[0] ?? erroDaApi(e).message),
  });
  return (
    <div className="space-y-4">
      <Cartao titulo="Nova observação interna">
        <AreaTexto value={texto} onChange={(e) => setTexto(e.target.value)} maxLength={2000} placeholder="Ex.: prefere receber a prova da arte pelo WhatsApp." />
        <div className="mt-3 flex justify-end"><Botao disabled={!texto.trim()} carregando={adicionar.isPending} onClick={() => adicionar.mutate()}>Adicionar observação</Botao></div>
      </Cartao>
      {!q.data ? <Carregando /> : q.data.length === 0
        ? <Cartao><EstadoVazio icone={<StickyNote className="h-5 w-5" />} titulo="Nenhuma observação registrada" descricao="Registre preferências e cuidados combinados com o cliente." /></Cartao>
        : <Cartao semPadding><ul className="divide-y divide-borda">{[...q.data].reverse().map((o) => (
          <li key={o.id} className="flex gap-3 px-5 py-4">
            <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-slate-100 text-xs font-semibold text-texto-suave">{o.autorIniciais}</div>
            <div><p className="whitespace-pre-line text-sm">{o.texto}</p><p className="mt-1 text-xs text-texto-fraco">{o.autor} · {dataHora(o.criadoEm)}</p></div>
          </li>))}</ul></Cartao>}
    </div>
  );
}

function AbaHistorico({ id }: { id: string }) {
  const [periodo, setPeriodo] = useState<Periodo>("todos");
  const [tipo, setTipo] = useState("");
  const [statusOrcamento, setStatusOrcamento] = useState("");
  const q = useQuery({ queryKey: ["cliente", id, "historico", periodo, tipo, statusOrcamento],
    queryFn: () => clientesService.historico(id, { ...intervaloDoPeriodo(periodo), tipo, statusOrcamento }) });
  return (
    <Cartao semPadding>
      <div className="flex flex-wrap gap-3 border-b border-borda p-4">
        <FiltroPeriodo valor={periodo} aoMudar={setPeriodo} />
        <Selecao className="w-auto min-w-[170px]" value={tipo} onChange={(e) => setTipo(e.target.value)} aria-label="Tipo de evento">
          <option value="">Todos os eventos</option><option value="orcamentos">Orçamentos</option><option value="contatos">Contatos</option>
          <option value="decisoes">Decisões</option><option value="pedidos">Pedidos</option>
        </Selecao>
        <Selecao className="w-auto min-w-[200px]" value={statusOrcamento} onChange={(e) => setStatusOrcamento(e.target.value)} aria-label="Status do orçamento">
          <option value="">Qualquer status de orçamento</option>{situacoesOrcamento.map((s) => <option key={s} value={s}>{rotuloSituacaoOrcamento[s]}</option>)}
        </Selecao>
      </div>
      <div className="p-5">
        {q.isError ? <ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} />
          : !q.data ? <Carregando /> : <LinhaDoTempo eventos={q.data} mostrarDocumento vazio="Nenhum evento encontrado para esses filtros." />}
      </div>
      <p className="border-t border-borda px-5 py-3 text-xs text-texto-fraco">Histórico somente leitura: registros de contatos, decisões e mudanças de status não podem ser alterados.</p>
    </Cartao>
  );
}

export { Info2 };
