
"use client";
import { useMemo, useState } from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter, useSearchParams } from "next/navigation";
import { AlertTriangle, Archive, CalendarDays, CheckCircle2, ClipboardList, History, PackageCheck, PackagePlus, Pencil, Plus, RotateCcw, Search, Truck, XCircle } from "lucide-react";
import { AcoesLinha, Botao, CabecalhoPagina, Campo, Cartao, Entrada, ErroCarregamento, EstadoVazio, Modal, Selecao } from "@/components/ui";
import { estoqueService } from "@/services";
import { useToast } from "@/components/ui/toast";
import { erroDaApi } from "@/lib/api";
import { numeroDocumento } from "@/utils/formatos";
import type { CompraMateriaPrima, MateriaPrima, SituacaoCompraMateriaPrima } from "@/types/api";

type MaterialForm = { nome: string; quantidadeDisponivel: number; estoqueMinimo: number };
const materialVazio: MaterialForm = { nome: "", quantidadeDisponivel: 0, estoqueMinimo: 0 };
type CompraForm = { materiaPrimaId: string; quantidade: number; fornecedor: string; dataPrevista: string };
const dataHoje = () => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`; };
const numero = (n: number) => new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 0 }).format(n);
const dataPt = (d: string) => { const [a, m, dia] = d.slice(0, 10).split("-"); return `${dia}/${m}/${a}`; };

export function Estoque() {
  const router = useRouter();
  const params = useSearchParams();
  const [filtroEstoque, setFiltroEstoque] = useState(params.get("filtro") === "reposicao" ? "reposicao" : params.get("filtro") === "abaixo" ? "abaixo" : "todos");
  const qc = useQueryClient();
  const toast = useToast();
  const [busca, setBusca] = useState("");
  const [mostrarInativos, setMostrarInativos] = useState(false);
  const [modal, setModal] = useState<"material" | "compra" | null>(null);
  const [materialAtual, setMaterialAtual] = useState<MateriaPrima | null>(null);
  const [historicoMaterial, setHistoricoMaterial] = useState<MateriaPrima | null>(null);
  const [compraCancelar, setCompraCancelar] = useState<CompraMateriaPrima | null>(null);
  const [materialForm, setMaterialForm] = useState<MaterialForm>(materialVazio);
  const [compraForm, setCompraForm] = useState<CompraForm>({ materiaPrimaId: "", quantidade: 1, fornecedor: "", dataPrevista: dataHoje() });
  const [erro, setErro] = useState("");
  const pendencias = useQuery({ queryKey: ["estoque", "pendencias"], queryFn: estoqueService.pendencias });
  const materias = useQuery({ queryKey: ["estoque", "materias"], queryFn: estoqueService.materias });
  const compras = useQuery({ queryKey: ["estoque", "compras"], queryFn: estoqueService.compras });
  const movimentos = useQuery({ queryKey: ["estoque", "movimentos", historicoMaterial?.id], queryFn: () => estoqueService.movimentos(historicoMaterial!.id), enabled: !!historicoMaterial });
  const atualizar = async () => { await Promise.all([qc.invalidateQueries({ queryKey: ["estoque"] }), qc.invalidateQueries({ queryKey: ["dashboard"] })]); };
  const salvarMaterial = useMutation({ mutationFn: () => materialAtual ? estoqueService.atualizarMateria(materialAtual.id, materialForm) : estoqueService.criarMateria(materialForm), onSuccess: async () => { await atualizar(); setModal(null); setErro(""); } });
  const salvarCompra = useMutation({ mutationFn: () => estoqueService.criarCompra(compraForm), onSuccess: async () => { await atualizar(); setModal(null); setErro(""); } });
  const mudarCompra = useMutation({ mutationFn: ({ id, situacao }: { id: string; situacao: SituacaoCompraMateriaPrima }) => estoqueService.atualizarSituacaoCompra(id, situacao), onSuccess: async (_, variaveis) => {
    await atualizar();
    if (variaveis.situacao === "Cancelada") setCompraCancelar(null);
  }, onError: (e) => toast("erro", erroDaApi(e).message) });
  const mudarAtivo = useMutation({ mutationFn: ({ id, ativo }: { id: string; ativo: boolean }) => estoqueService.definirMateriaAtiva(id, ativo), onSuccess: atualizar, onError: (e) => toast("erro", erroDaApi(e).message) });
  const itens = useMemo(() => (materias.data ?? []).filter((m) => (mostrarInativos || m.ativo) && (filtroEstoque === "todos" || m.ativo && (filtroEstoque === "abaixo" ? m.abaixoDoMinimo : m.quantidadeSugerida > 0)) && `${m.nome} ${m.unidade}`.toLocaleLowerCase("pt-BR").includes(busca.toLocaleLowerCase("pt-BR"))), [materias.data, mostrarInativos, busca, filtroEstoque]);
  const ativos = materias.data?.filter((m) => m.ativo) ?? [];
  const baixos = ativos.filter((m) => m.abaixoDoMinimo);
  const repor = ativos.filter(m => m.quantidadeSugerida > 0);
  const comprasAbertas = (compras.data ?? []).filter((c) => c.aberta);

  function abrirMaterial(m?: MateriaPrima) { setMaterialAtual(m ?? null); setMaterialForm(m ? { nome: m.nome, quantidadeDisponivel: m.quantidadeDisponivel, estoqueMinimo: m.estoqueMinimo } : { ...materialVazio }); setErro(""); setModal("material"); }
  function abrirCompra(m?: MateriaPrima) { setCompraForm({ materiaPrimaId: m?.id ?? ativos[0]?.id ?? "", quantidade: Math.max(1, m?.faltaComprar ?? 1), fornecedor: "", dataPrevista: dataHoje() }); setErro(""); setModal("compra"); }
  async function submeter(e: React.FormEvent) { e.preventDefault(); setErro(""); try {
    if (modal === "material") await salvarMaterial.mutateAsync();
    else if (modal === "compra") await salvarCompra.mutateAsync();
  } catch (err) { setErro(erroDaApi(err).message); } }
  const carregando = salvarMaterial.isPending || salvarCompra.isPending;

  return <>
    <CabecalhoPagina titulo="Estoque de matéria-prima" subtitulo="Acompanhe o saldo, os níveis mínimos e as compras que estão a caminho."
      acoes={<><Botao variante="secundaria" icone={<Truck className="h-4 w-4" />} onClick={() => abrirCompra()}>Planejar compra</Botao><Botao icone={<Plus className="h-4 w-4" />} onClick={() => abrirMaterial()}>Nova matéria-prima</Botao></>} />

    <div className="mb-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
      <Cartao><p className="text-xs font-medium uppercase tracking-wide text-texto-fraco">Matérias-primas ativas</p><p className="mt-2 text-2xl font-semibold numero text-texto">{materias.data ? ativos.length : "—"}</p></Cartao>
      <Cartao><p className="flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-texto-fraco"><AlertTriangle className="h-3.5 w-3.5" />Abaixo do mínimo</p><p className={`mt-2 text-2xl font-semibold numero ${baixos.length ? "text-amber-700" : "text-texto"}`}>{materias.data ? baixos.length : "—"}</p></Cartao>
      <Cartao><p className="text-xs font-medium uppercase tracking-wide text-texto-fraco">Repor estoque</p><p className="numero mt-2 text-2xl font-semibold">{materias.data ? repor.length : "—"}</p><Botao tamanho="sm" variante="fantasma" onClick={() => setFiltroEstoque("reposicao")}>Ver lista de reposição</Botao></Cartao>
      <Cartao><p className="flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-texto-fraco"><Truck className="h-3.5 w-3.5" />Compras a caminho</p><p className="mt-2 text-2xl font-semibold numero text-texto">{comprasAbertas.length}</p></Cartao>
    </div>

    <Cartao titulo="Pendências de materiais" className="mb-5">
      {pendencias.isError ? <ErroCarregamento mensagem={erroDaApi(pendencias.error).message} aoTentar={() => pendencias.refetch()} />
        : pendencias.isLoading ? <p role="status" className="text-sm text-texto-fraco">Carregando pendências…</p>
          : !pendencias.data?.length ? <p className="text-sm text-texto-fraco">Nenhuma pendência de materiais.</p>
            : <div className="space-y-2">{pendencias.data.map((p) => <div key={p.pedidoId} className="flex flex-wrap items-center justify-between gap-2 text-sm"><Link href={`/pedidos/${p.pedidoId}`} className="font-medium text-primaria hover:underline">Pedido #{p.pedidoNumero}</Link><span className="text-amber-800">{p.descricao}</span><span className="numero text-texto-fraco">{dataPt(p.prazoEntrega)}</span></div>)}</div>}
    </Cartao>

    <Cartao titulo={filtroEstoque === "reposicao" ? "Repor estoque" : "Matérias-primas"} semPadding>
      <div className="flex flex-wrap items-center gap-3 border-b border-borda p-4">
        <div className="relative min-w-[230px] flex-1"><Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-texto-fraco" /><Entrada className="pl-9" placeholder="Buscar matéria-prima" value={busca} onChange={(e) => setBusca(e.target.value)} aria-label="Buscar matérias-primas" /></div>
        <Selecao aria-label="Filtrar estoque" className="w-auto max-w-full" value={filtroEstoque} onChange={e => setFiltroEstoque(e.target.value)}><option value="todos">Todas as matérias-primas</option><option value="abaixo">Abaixo do mínimo</option><option value="reposicao">Repor estoque</option></Selecao>
        <label className="flex items-center gap-2 text-sm text-texto-suave"><input type="checkbox" checked={mostrarInativos} onChange={(e) => setMostrarInativos(e.target.checked)} />Mostrar inativas</label>
      </div>
      {filtroEstoque === "reposicao" && <p className="border-b border-borda px-4 py-3 text-xs text-texto-suave">Sugestão: mínimo − saldo livre + pendências, limitada a zero. “Falta comprar” desconta as compras previstas a receber. Pedidos sem ficha técnica precisam de definição dos materiais na lista de pendências.</p>}
      {materias.isError ? <div className="p-4"><ErroCarregamento mensagem={erroDaApi(materias.error).message} aoTentar={() => materias.refetch()} /></div>
        : materias.isLoading ? <div role="status" className="p-8 text-center text-sm text-texto-fraco">Carregando estoque…</div>
          : itens.length === 0 ? <EstadoVazio icone={<ClipboardList className="h-5 w-5" />} titulo={filtroEstoque !== "todos" ? "Nenhuma matéria-prima neste filtro" : busca ? "Nenhum item encontrado" : "Nenhuma matéria-prima cadastrada"} descricao="Cadastre os materiais utilizados nas encomendas para acompanhar o que há disponível e o que precisa ser reposto." acao={<Botao icone={<Plus className="h-4 w-4" />} onClick={() => abrirMaterial()}>Cadastrar matéria-prima</Botao>} />
            : <div className="overflow-x-auto"><table className="tabela w-full min-w-[1080px] text-sm"><thead><tr><th>Matéria-prima</th><th className="text-right">Saldo físico</th><th className="text-right">Reservado</th><th className="text-right">Livre p/ pedidos</th><th className="text-right">Estoque mínimo</th><th className="text-right">Reposição sugerida</th><th className="text-right">A receber</th><th className="text-right">Falta comprar</th><th>Status</th><th className="w-36 text-right">Ações</th></tr></thead><tbody>
              {itens.map((m) => {
                const livre = m.saldoDisponivel;
                const falta = m.faltaComprar;
                const baixo = m.noMinimo;
                return <tr key={m.id} className={`group ${!m.ativo ? "opacity-60" : ""}`}>
                  <td><span className="font-medium text-texto">{m.nome}</span><span className="ml-2 text-xs text-texto-fraco">({m.unidade})</span></td>
                  <td className="numero text-right font-medium whitespace-nowrap">{numero(m.quantidadeDisponivel)} <span className="text-xs font-normal text-texto-fraco">{m.unidade}</span></td>
                  <td className="numero text-right text-texto-suave whitespace-nowrap">{numero(m.quantidadeReservada)} {m.unidade}{m.quantidadePendente > 0 && <p className="text-xs text-amber-700">Pendente: {numero(m.quantidadePendente)} {m.unidade}</p>}</td>
                  <td className={`numero text-right font-medium whitespace-nowrap ${livre < 0 ? "text-red-700" : "text-texto"}`}>{numero(livre)} {m.unidade}</td>
                  <td className="numero text-right text-texto-suave whitespace-nowrap">{numero(m.estoqueMinimo)} {m.unidade}</td>
                  <td className="numero text-right whitespace-nowrap">{numero(m.quantidadeSugerida)} {m.unidade}</td>
                  <td className="numero text-right text-texto-suave whitespace-nowrap">{numero(m.quantidadeAReceber)} {m.unidade}</td>
                  <td className={`numero text-right font-medium whitespace-nowrap ${falta > 0 ? "text-red-700" : "text-texto-suave"}`}>{numero(falta)} {m.unidade}</td>
                  <td><span className={`rounded-full px-2 py-1 text-xs ${!m.ativo ? "bg-slate-100 text-slate-600" : baixo ? "bg-amber-50 text-amber-800" : "bg-emerald-50 text-emerald-700"}`}>{!m.ativo ? "Inativa" : baixo && m.quantidadeAReceber > 0 ? "Reposição a caminho" : baixo ? "Atenção" : "Adequado"}</span></td>
                  <td><AcoesLinha acoes={[
                    { rotulo: `Ver movimentações de ${m.nome}`, icone: <History className="h-4 w-4" />, aoAcionar: () => setHistoricoMaterial(m) },
                    { rotulo: `Editar matéria-prima ${m.nome}`, icone: <Pencil className="h-4 w-4" />, aoAcionar: () => abrirMaterial(m) },
                    ...(m.ativo ? [
                      { rotulo: `Planejar compra de ${m.nome}`, icone: <PackagePlus className="h-4 w-4" />, aoAcionar: () => abrirCompra(m), desabilitada: filtroEstoque === "reposicao" && m.faltaComprar <= 0 },
                      { rotulo: `Desativar ${m.nome}`, icone: <Archive className="h-4 w-4" />, aoAcionar: () => mudarAtivo.mutate({ id: m.id, ativo: false }), desabilitada: mudarAtivo.isPending },
                    ] : [
                      { rotulo: `Reativar ${m.nome}`, icone: <RotateCcw className="h-4 w-4" />, aoAcionar: () => mudarAtivo.mutate({ id: m.id, ativo: true }), desabilitada: mudarAtivo.isPending },
                    ]),
                  ]} /></td>
                </tr>;
              })}
            </tbody></table></div>}
    </Cartao>

    <section className="mt-7"><Cartao titulo="Produtos e composição"><p className="mb-3 text-sm text-texto-suave">Cadastre produtos e defina os materiais por unidade na tela Produtos.</p><Link href="/produtos" className="text-sm font-medium text-primaria hover:underline">Abrir catálogo de produtos</Link></Cartao></section>

    <section className="mt-7">
      <div className="mb-3 flex items-center justify-between gap-3"><div><h2 className="text-base font-semibold text-texto">Compras previstas e pedidos</h2><p className="mt-0.5 text-sm text-texto-fraco">Os itens entram no saldo disponível quando forem marcados como recebidos.</p></div><Botao variante="secundaria" tamanho="sm" icone={<Plus className="h-3.5 w-3.5" />} onClick={() => abrirCompra()}>Adicionar compra</Botao></div>
      <Cartao semPadding>
        {compras.isError ? <div className="p-4"><ErroCarregamento mensagem={erroDaApi(compras.error).message} aoTentar={() => compras.refetch()} /></div>
          : compras.isLoading ? <div role="status" className="p-6 text-center text-sm text-texto-fraco">Carregando compras…</div>
            : comprasAbertas.length === 0 ? <EstadoVazio icone={<Truck className="h-5 w-5" />} titulo="Nenhuma compra a caminho" descricao="Planeje uma reposição para acompanhar quantidades e previsão de chegada." acao={<Botao variante="secundaria" onClick={() => abrirCompra()}>Planejar compra</Botao>} />
              : <div className="overflow-x-auto"><table className="tabela w-full min-w-[760px] text-sm"><thead><tr><th>Matéria-prima</th><th className="text-right">Quantidade</th><th>Fornecedor</th><th>Previsão</th><th>Situação</th><th className="text-right">Ações</th></tr></thead><tbody>
                {comprasAbertas.map((c) => <tr key={c.id} className="group"><td className="font-medium">{c.materiaPrimaNome}</td><td className="numero text-right whitespace-nowrap">{numero(c.quantidade)} {c.unidade}</td><td className="text-texto-suave">{c.fornecedor || "—"}</td><td className="numero text-texto-suave"><span className="inline-flex items-center gap-1"><CalendarDays className="h-3.5 w-3.5" />{dataPt(c.dataPrevista)}</span></td><td><span className={`rounded-full px-2 py-1 text-xs ${c.situacao === "Pedido" ? "bg-blue-50 text-blue-700" : "bg-slate-100 text-slate-700"}`}>{c.situacaoDescricao}</span></td><td><AcoesLinha acoes={c.situacoesPermitidas.map((opcao) => ({
                  rotulo: opcao.valor === "Cancelada" ? `Cancelar compra de ${c.materiaPrimaNome}` : opcao.valor === "Recebida" ? `Marcar compra de ${c.materiaPrimaNome} como recebida` : `Marcar compra de ${c.materiaPrimaNome} como pedido`,
                  icone: opcao.valor === "Cancelada" ? <XCircle className="h-4 w-4" /> : opcao.valor === "Recebida" ? <PackageCheck className="h-4 w-4" /> : <CheckCircle2 className="h-4 w-4" />,
                  aoAcionar: () => opcao.valor === "Cancelada" ? setCompraCancelar(c) : mudarCompra.mutate({ id: c.id, situacao: opcao.valor }),
                  desabilitada: mudarCompra.isPending,
                  perigo: opcao.valor === "Cancelada",
                }))} /></td></tr>)}
              </tbody></table></div>}
      </Cartao>
    </section>

    <Modal aberto={modal === "material" || modal === "compra"} aoFechar={() => { if (!carregando) setModal(null); }} titulo={modal === "compra" ? "Planejar compra de matéria-prima" : materialAtual ? "Editar matéria-prima" : "Nova matéria-prima"}
      descricao={modal === "compra" ? "Registre uma compra prevista ou já solicitada ao fornecedor." : "Cadastre o material, a unidade de controle e o nível mínimo desejado."}
      rodape={<><Botao variante="secundaria" disabled={carregando} onClick={() => setModal(null)}>Cancelar</Botao><Botao type="submit" form="form-estoque" carregando={carregando}>{modal === "compra" ? "Salvar compra" : "Salvar matéria-prima"}</Botao></>}>
      <form id="form-estoque" onSubmit={submeter} className="space-y-4">
        {erro && <p role="alert" className="rounded-md bg-red-50 p-3 text-sm text-red-700">{erro}</p>}
        {modal === "material" ? <>
          <Campo rotulo="Nome do material" obrigatorio><Entrada maxLength={120} required disabled={carregando} value={materialForm.nome} onChange={(e) => setMaterialForm({ ...materialForm, nome: e.target.value })} placeholder="Ex.: Camiseta branca, tinta azul-marinho" /></Campo>
          <div className="grid gap-4 sm:grid-cols-3">
            <Campo rotulo="Unidade"><p className="flex h-9 items-center rounded-md border border-borda bg-slate-50 px-3 text-sm text-texto">un</p></Campo>
            <Campo rotulo="Saldo físico"><Entrada type="number" min="0" step="1" disabled={carregando} value={materialForm.quantidadeDisponivel} onChange={(e) => setMaterialForm({ ...materialForm, quantidadeDisponivel: Number(e.target.value) })} /></Campo>
            <Campo rotulo="Estoque mínimo"><Entrada type="number" min="0" step="1" disabled={carregando} value={materialForm.estoqueMinimo} onChange={(e) => setMaterialForm({ ...materialForm, estoqueMinimo: Number(e.target.value) })} /></Campo>
          </div>
          <p className="text-xs text-texto-fraco">O saldo físico pode ser ajustado após conferência do estoque, respeitando as reservas existentes.</p>
        </> : <>
          <Campo rotulo="Matéria-prima" obrigatorio><Selecao required disabled={carregando} value={compraForm.materiaPrimaId} onChange={(e) => setCompraForm({ ...compraForm, materiaPrimaId: e.target.value })}><option value="">Selecione um material</option>{ativos.map((m) => <option key={m.id} value={m.id}>{m.nome} ({m.unidade})</option>)}</Selecao></Campo>
          <div className="grid gap-4 sm:grid-cols-2"><Campo rotulo="Quantidade" obrigatorio><Entrada type="number" min="1" step="1" required disabled={carregando} value={compraForm.quantidade} onChange={(e) => setCompraForm({ ...compraForm, quantidade: Number(e.target.value) })} /></Campo><Campo rotulo="Data prevista" obrigatorio><Entrada type="date" required disabled={carregando} value={compraForm.dataPrevista} onChange={(e) => setCompraForm({ ...compraForm, dataPrevista: e.target.value })} /></Campo></div>
          <Campo rotulo="Fornecedor"><Entrada maxLength={120} disabled={carregando} value={compraForm.fornecedor} onChange={(e) => setCompraForm({ ...compraForm, fornecedor: e.target.value })} placeholder="Opcional" /></Campo>
          <p className="text-xs text-texto-fraco">Após salvar, a compra aparece como planejada. Atualize para “Pedido realizado” ou “Recebida” conforme o andamento.</p>
        </>}
      </form>
    </Modal>
    <Modal aberto={!!historicoMaterial} aoFechar={() => setHistoricoMaterial(null)} titulo={`Movimentações · ${historicoMaterial?.nome ?? ""}`} descricao="Entradas, saídas para pedidos e ajustes manuais de saldo." largura="max-w-2xl">
      {movimentos.isError ? <ErroCarregamento mensagem={erroDaApi(movimentos.error).message} aoTentar={() => movimentos.refetch()} />
        : movimentos.isLoading ? <p role="status" className="text-sm text-texto-fraco">Carregando histórico…</p>
          : movimentos.data?.length ? <div className="overflow-x-auto"><table className="tabela w-full min-w-[600px] text-sm"><thead><tr><th>Data</th><th>Movimento</th><th>Quantidade</th><th>Origem</th><th className="w-16 text-right">Ações</th></tr></thead><tbody>{movimentos.data.map((x) => <tr key={x.id} className="group"><td className="numero text-texto-suave">{new Date(x.criadoEm).toLocaleString("pt-BR")}</td><td>{x.tipoDescricao}{x.observacao && <p className="text-xs text-texto-fraco">{x.observacao}</p>}</td><td className={`numero font-medium ${x.variacao < 0 ? "text-red-700" : "text-emerald-700"}`}>{x.variacao > 0 ? "+" : ""}{numero(x.variacao)} {historicoMaterial?.unidade}</td><td>{x.pedidoId ? <Link href={`/pedidos/${x.pedidoId}`} className="text-primaria hover:underline">Pedido #{x.pedidoNumero}</Link> : "—"}</td><td><AcoesLinha acoes={x.pedidoId ? [{
            rotulo: `Ver pedido ${x.pedidoNumero ? numeroDocumento(x.pedidoNumero) : ""}`,
            icone: <History className="h-4 w-4" />,
            aoAcionar: () => router.push(`/pedidos/${x.pedidoId}`),
          }] : []} /></td></tr>)}</tbody></table></div>
            : <p className="text-sm text-texto-fraco">Ainda não há movimentações para este material.</p>}
    </Modal>
    <Modal aberto={!!compraCancelar} aoFechar={() => setCompraCancelar(null)} titulo="Cancelar compra"
      descricao={compraCancelar ? `Confirma o cancelamento da compra de ${compraCancelar.materiaPrimaNome}?` : undefined}
      rodape={<><Botao variante="secundaria" disabled={mudarCompra.isPending} onClick={() => setCompraCancelar(null)}>Voltar</Botao>
        <Botao variante="perigo" carregando={mudarCompra.isPending} onClick={() => compraCancelar && mudarCompra.mutate({ id: compraCancelar.id, situacao: "Cancelada" })}>Confirmar cancelamento</Botao></>}>
      <p className="text-sm text-texto-suave">O cancelamento não altera o saldo físico da matéria-prima.</p>
    </Modal>
  </>;
}
