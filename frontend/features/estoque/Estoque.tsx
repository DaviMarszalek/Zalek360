
"use client";
import { useMemo, useState } from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, Archive, CalendarDays, ClipboardList, History, PackagePlus, Pencil, Plus, RotateCcw, Search, Trash2, Truck } from "lucide-react";
import { Botao, CabecalhoPagina, Campo, Cartao, Entrada, ErroCarregamento, EstadoVazio, Modal, Selecao } from "@/components/ui";
import { catalogoService, estoqueService } from "@/services";
import { erroDaApi } from "@/lib/api";
import type { MateriaPrima, Produto, SituacaoCompraMateriaPrima } from "@/types/api";

type MaterialForm = { nome: string; unidade: string; quantidadeDisponivel: number; estoqueMinimo: number };
const materialVazio: MaterialForm = { nome: "", unidade: "un", quantidadeDisponivel: 0, estoqueMinimo: 0 };
type CompraForm = { materiaPrimaId: string; quantidade: number; fornecedor: string; dataPrevista: string };
type FichaLinha = { materiaPrimaId: string; quantidadePorUnidade: number };
const dataHoje = () => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`; };
const numero = (n: number) => new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 3 }).format(n);
const dataPt = (d: string) => { const [a, m, dia] = d.slice(0, 10).split("-"); return `${dia}/${m}/${a}`; };

export function Estoque() {
  const qc = useQueryClient();
  const [busca, setBusca] = useState("");
  const [mostrarInativos, setMostrarInativos] = useState(false);
  const [modal, setModal] = useState<"material" | "compra" | "ficha" | null>(null);
  const [materialAtual, setMaterialAtual] = useState<MateriaPrima | null>(null);
  const [historicoMaterial, setHistoricoMaterial] = useState<MateriaPrima | null>(null);
  const [materialForm, setMaterialForm] = useState<MaterialForm>(materialVazio);
  const [compraForm, setCompraForm] = useState<CompraForm>({ materiaPrimaId: "", quantidade: 1, fornecedor: "", dataPrevista: dataHoje() });
  const [produtoFicha, setProdutoFicha] = useState<Produto | null>(null);
  const [fichaLinhas, setFichaLinhas] = useState<FichaLinha[]>([]);
  const [erro, setErro] = useState("");
  const materias = useQuery({ queryKey: ["estoque", "materias"], queryFn: estoqueService.materias });
  const compras = useQuery({ queryKey: ["estoque", "compras"], queryFn: estoqueService.compras });
  const produtos = useQuery({ queryKey: ["catalogo", "produtos"], queryFn: catalogoService.produtos });
  const fichas = useQuery({ queryKey: ["estoque", "fichas"], queryFn: estoqueService.fichas });
  const movimentos = useQuery({ queryKey: ["estoque", "movimentos", historicoMaterial?.id], queryFn: () => estoqueService.movimentos(historicoMaterial!.id), enabled: !!historicoMaterial });
  const atualizar = async () => { await Promise.all([qc.invalidateQueries({ queryKey: ["estoque", "materias"] }), qc.invalidateQueries({ queryKey: ["estoque", "compras"] })]); };
  const salvarMaterial = useMutation({ mutationFn: () => materialAtual ? estoqueService.atualizarMateria(materialAtual.id, materialForm) : estoqueService.criarMateria(materialForm), onSuccess: async () => { await atualizar(); setModal(null); setErro(""); } });
  const salvarCompra = useMutation({ mutationFn: () => estoqueService.criarCompra(compraForm), onSuccess: async () => { await atualizar(); setModal(null); setErro(""); } });
  const salvarFicha = useMutation({ mutationFn: () => estoqueService.salvarFicha(produtoFicha!.id, fichaLinhas), onSuccess: async () => { await qc.invalidateQueries({ queryKey: ["estoque", "fichas"] }); setModal(null); setErro(""); } });
  const mudarCompra = useMutation({ mutationFn: ({ id, situacao }: { id: string; situacao: SituacaoCompraMateriaPrima }) => estoqueService.atualizarSituacaoCompra(id, situacao), onSuccess: atualizar });
  const mudarAtivo = useMutation({ mutationFn: ({ id, ativo }: { id: string; ativo: boolean }) => estoqueService.definirMateriaAtiva(id, ativo), onSuccess: atualizar });
  const itens = useMemo(() => (materias.data ?? []).filter((m) => (mostrarInativos || m.ativo) && `${m.nome} ${m.unidade}`.toLocaleLowerCase("pt-BR").includes(busca.toLocaleLowerCase("pt-BR"))), [materias.data, mostrarInativos, busca]);
  const ativos = materias.data?.filter((m) => m.ativo) ?? [];
  const baixos = ativos.filter((m) => m.quantidadeDisponivel - m.quantidadeReservada <= m.estoqueMinimo);
  const comprasAbertas = (compras.data ?? []).filter((c) => c.situacao === "Planejada" || c.situacao === "Pedido");

  function abrirMaterial(m?: MateriaPrima) { setMaterialAtual(m ?? null); setMaterialForm(m ? { nome: m.nome, unidade: m.unidade, quantidadeDisponivel: m.quantidadeDisponivel, estoqueMinimo: m.estoqueMinimo } : { ...materialVazio }); setErro(""); setModal("material"); }
  function abrirCompra(m?: MateriaPrima) { setCompraForm({ materiaPrimaId: m?.id ?? ativos[0]?.id ?? "", quantidade: 1, fornecedor: "", dataPrevista: dataHoje() }); setErro(""); setModal("compra"); }
  function abrirFicha(p: Produto) { const existentes = fichas.data?.find((f) => f.produtoId === p.id)?.materiais ?? []; setProdutoFicha(p); setFichaLinhas(existentes.map((x) => ({ materiaPrimaId: x.materiaPrimaId, quantidadePorUnidade: x.quantidadePorUnidade }))); setErro(""); setModal("ficha"); }
  async function submeter(e: React.FormEvent) { e.preventDefault(); setErro(""); try { if (modal === "material") await salvarMaterial.mutateAsync(); else if (modal === "compra") await salvarCompra.mutateAsync(); else await salvarFicha.mutateAsync(); } catch (err) { setErro(erroDaApi(err).message); } }
  const carregando = salvarMaterial.isPending || salvarCompra.isPending || salvarFicha.isPending;

  return <>
    <CabecalhoPagina titulo="Estoque de matéria-prima" subtitulo="Acompanhe o saldo, os níveis mínimos e as compras que estão a caminho."
      acoes={<><Botao variante="secundaria" icone={<Truck className="h-4 w-4" />} onClick={() => abrirCompra()}>Planejar compra</Botao><Botao icone={<Plus className="h-4 w-4" />} onClick={() => abrirMaterial()}>Nova matéria-prima</Botao></>} />

    <div className="mb-5 grid gap-3 sm:grid-cols-3">
      <Cartao><p className="text-xs font-medium uppercase tracking-wide text-texto-fraco">Matérias-primas ativas</p><p className="mt-2 text-2xl font-semibold numero text-texto">{ativos.length}</p></Cartao>
      <Cartao><p className="flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-texto-fraco"><AlertTriangle className="h-3.5 w-3.5" />No mínimo ou abaixo</p><p className={`mt-2 text-2xl font-semibold numero ${baixos.length ? "text-amber-700" : "text-texto"}`}>{baixos.length}</p></Cartao>
      <Cartao><p className="flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-texto-fraco"><Truck className="h-3.5 w-3.5" />Compras a caminho</p><p className="mt-2 text-2xl font-semibold numero text-texto">{comprasAbertas.length}</p></Cartao>
    </div>

    <Cartao semPadding>
      <div className="flex flex-wrap items-center gap-3 border-b border-borda p-4">
        <div className="relative min-w-[230px] flex-1"><Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-texto-fraco" /><Entrada className="pl-9" placeholder="Buscar matéria-prima" value={busca} onChange={(e) => setBusca(e.target.value)} aria-label="Buscar matérias-primas" /></div>
        <label className="flex items-center gap-2 text-sm text-texto-suave"><input type="checkbox" checked={mostrarInativos} onChange={(e) => setMostrarInativos(e.target.checked)} />Mostrar inativas</label>
      </div>
      {materias.isError ? <div className="p-4"><ErroCarregamento mensagem={erroDaApi(materias.error).message} aoTentar={() => materias.refetch()} /></div>
        : materias.isLoading ? <div className="p-8 text-center text-sm text-texto-fraco">Carregando estoque…</div>
          : itens.length === 0 ? <EstadoVazio icone={<ClipboardList className="h-5 w-5" />} titulo={busca ? "Nenhum item encontrado" : "Nenhuma matéria-prima cadastrada"} descricao="Cadastre os materiais utilizados nas encomendas para acompanhar o que há disponível e o que precisa ser reposto." acao={<Botao icone={<Plus className="h-4 w-4" />} onClick={() => abrirMaterial()}>Cadastrar matéria-prima</Botao>} />
            : <div className="overflow-x-auto"><table className="tabela w-full min-w-[960px] text-sm"><thead><tr><th>Matéria-prima</th><th>Saldo físico</th><th>Reservado</th><th>Livre p/ pedidos</th><th>Estoque mínimo</th><th>A receber</th><th>Falta comprar</th><th>Status</th><th className="w-36 text-right">Ações</th></tr></thead><tbody>
              {itens.map((m) => {
                const livre = m.quantidadeDisponivel - m.quantidadeReservada;
                const falta = Math.max(0, m.estoqueMinimo - livre - m.quantidadeAReceber);
                const baixo = livre <= m.estoqueMinimo;
                return <tr key={m.id} className={!m.ativo ? "opacity-60" : ""}>
                  <td><span className="font-medium text-texto">{m.nome}</span><span className="ml-2 text-xs text-texto-fraco">({m.unidade})</span></td>
                  <td className="numero font-medium">{numero(m.quantidadeDisponivel)} <span className="text-xs font-normal text-texto-fraco">{m.unidade}</span></td>
                  <td className="numero text-texto-suave">{numero(m.quantidadeReservada)} {m.unidade}</td>
                  <td className={`numero font-medium ${livre < 0 ? "text-red-700" : "text-texto"}`}>{numero(livre)} {m.unidade}</td>
                  <td className="numero text-texto-suave">{numero(m.estoqueMinimo)} {m.unidade}</td>
                  <td className="numero text-texto-suave">{numero(m.quantidadeAReceber)} {m.unidade}</td>
                  <td className={`numero font-medium ${falta > 0 ? "text-red-700" : "text-texto-suave"}`}>{numero(falta)} {m.unidade}</td>
                  <td><span className={`rounded-full px-2 py-1 text-xs ${!m.ativo ? "bg-slate-100 text-slate-600" : baixo ? "bg-amber-50 text-amber-800" : "bg-emerald-50 text-emerald-700"}`}>{!m.ativo ? "Inativa" : baixo && m.quantidadeAReceber > 0 ? "Reposição a caminho" : baixo ? "Atenção" : "Adequado"}</span></td>
                  <td><div className="flex justify-end gap-1"><Botao variante="fantasma" tamanho="sm" aria-label={`Histórico de ${m.nome}`} onClick={() => setHistoricoMaterial(m)} icone={<History className="h-4 w-4" />} /><Botao variante="fantasma" tamanho="sm" aria-label={`Editar ${m.nome}`} onClick={() => abrirMaterial(m)} icone={<Pencil className="h-4 w-4" />} />{m.ativo && <Botao variante="fantasma" tamanho="sm" aria-label={`Planejar compra de ${m.nome}`} onClick={() => abrirCompra(m)} icone={<PackagePlus className="h-4 w-4" />} />}{m.ativo ? <Botao variante="fantasma" tamanho="sm" aria-label={`Desativar ${m.nome}`} onClick={() => mudarAtivo.mutate({ id: m.id, ativo: false })} icone={<Archive className="h-4 w-4" />} /> : <Botao variante="fantasma" tamanho="sm" aria-label={`Reativar ${m.nome}`} onClick={() => mudarAtivo.mutate({ id: m.id, ativo: true })} icone={<RotateCcw className="h-4 w-4" />} />}</div></td>
                </tr>;
              })}
            </tbody></table></div>}
    </Cartao>

    <section className="mt-7">
      <div className="mb-3"><h2 className="text-base font-semibold text-texto">Fichas técnicas por produto</h2><p className="mt-0.5 text-sm text-texto-fraco">Informe quanto de cada matéria-prima é usado para produzir uma unidade. O sistema multiplica pelo total do pedido.</p></div>
      <Cartao semPadding>
        {produtos.isError || fichas.isError ? <div className="p-4"><ErroCarregamento mensagem={erroDaApi(produtos.error ?? fichas.error).message} aoTentar={() => { produtos.refetch(); fichas.refetch(); }} /></div>
          : !produtos.data || !fichas.data ? <div className="p-6 text-center text-sm text-texto-fraco">Carregando fichas técnicas…</div>
            : produtos.data.length === 0 ? <EstadoVazio titulo="Nenhum produto cadastrado" descricao="Cadastre produtos no catálogo comercial antes de definir o consumo de materiais." />
              : <div className="overflow-x-auto"><table className="tabela w-full min-w-[640px] text-sm"><thead><tr><th>Produto do pedido</th><th>Materiais vinculados</th><th className="text-right">Ficha técnica</th></tr></thead><tbody>
                {produtos.data.map((p) => { const f = fichas.data.find((x) => x.produtoId === p.id); return <tr key={p.id}><td className="font-medium">{p.nome}</td><td className="text-texto-suave">{f?.materiais.length ? f.materiais.map((x) => `${x.materiaPrimaNome}: ${numero(x.quantidadePorUnidade)} ${x.unidade}/un.`).join(" · ") : <span className="text-amber-700">Ainda sem materiais</span>}</td><td className="text-right"><Botao variante="secundaria" tamanho="sm" onClick={() => abrirFicha(p)}>{f?.materiais.length ? "Editar ficha" : "Configurar"}</Botao></td></tr>; })}
              </tbody></table></div>}
      </Cartao>
    </section>

    <section className="mt-7">
      <div className="mb-3 flex items-center justify-between gap-3"><div><h2 className="text-base font-semibold text-texto">Compras previstas e pedidos</h2><p className="mt-0.5 text-sm text-texto-fraco">Os itens entram no saldo disponível quando forem marcados como recebidos.</p></div><Botao variante="secundaria" tamanho="sm" icone={<Plus className="h-3.5 w-3.5" />} onClick={() => abrirCompra()}>Adicionar compra</Botao></div>
      <Cartao semPadding>
        {compras.isError ? <div className="p-4"><ErroCarregamento mensagem={erroDaApi(compras.error).message} aoTentar={() => compras.refetch()} /></div>
          : compras.isLoading ? <div className="p-6 text-center text-sm text-texto-fraco">Carregando compras…</div>
            : comprasAbertas.length === 0 ? <EstadoVazio icone={<Truck className="h-5 w-5" />} titulo="Nenhuma compra a caminho" descricao="Planeje uma reposição para acompanhar quantidades e previsão de chegada." acao={<Botao variante="secundaria" onClick={() => abrirCompra()}>Planejar compra</Botao>} />
              : <div className="overflow-x-auto"><table className="tabela w-full min-w-[760px] text-sm"><thead><tr><th>Matéria-prima</th><th>Quantidade</th><th>Fornecedor</th><th>Previsão</th><th>Situação</th><th className="text-right">Atualizar</th></tr></thead><tbody>
                {comprasAbertas.map((c) => <tr key={c.id}><td className="font-medium">{c.materiaPrimaNome}</td><td className="numero">{numero(c.quantidade)} {c.unidade}</td><td className="text-texto-suave">{c.fornecedor || "—"}</td><td className="numero text-texto-suave"><span className="inline-flex items-center gap-1"><CalendarDays className="h-3.5 w-3.5" />{dataPt(c.dataPrevista)}</span></td><td><span className={`rounded-full px-2 py-1 text-xs ${c.situacao === "Pedido" ? "bg-blue-50 text-blue-700" : "bg-slate-100 text-slate-700"}`}>{c.situacao === "Pedido" ? "Pedido realizado" : "Planejada"}</span></td><td><div className="flex justify-end gap-2"><Selecao className="h-8 w-36 text-xs" aria-label={`Atualizar situação da compra de ${c.materiaPrimaNome}`} value={c.situacao} onChange={(e) => mudarCompra.mutate({ id: c.id, situacao: e.target.value as SituacaoCompraMateriaPrima })}><option value="Planejada" disabled>Planejada</option><option value="Pedido">Pedido realizado</option><option value="Recebida">Recebida</option><option value="Cancelada">Cancelar</option></Selecao></div></td></tr>)}
              </tbody></table></div>}
      </Cartao>
    </section>

    <Modal aberto={modal !== null} aoFechar={() => setModal(null)} titulo={modal === "compra" ? "Planejar compra de matéria-prima" : modal === "ficha" ? `Ficha técnica · ${produtoFicha?.nome ?? "Produto"}` : materialAtual ? "Editar matéria-prima" : "Nova matéria-prima"}
      descricao={modal === "compra" ? "Registre uma compra prevista ou já solicitada ao fornecedor." : modal === "ficha" ? "Consumo padrão por unidade. O consumo variável pode ser ajustado no próprio pedido." : "Cadastre o material, a unidade de controle e o nível mínimo desejado."}
      rodape={<><Botao variante="secundaria" onClick={() => setModal(null)}>Cancelar</Botao><Botao type="submit" form="form-estoque" carregando={carregando}>{modal === "compra" ? "Salvar compra" : modal === "ficha" ? "Salvar ficha" : "Salvar matéria-prima"}</Botao></>}>
      <form id="form-estoque" onSubmit={submeter} className="space-y-4">
        {erro && <p role="alert" className="rounded-md bg-red-50 p-3 text-sm text-red-700">{erro}</p>}
        {modal === "ficha" ? <>
          {ativos.length === 0 ? <p className="text-sm text-texto-fraco">Cadastre pelo menos uma matéria-prima ativa para montar esta ficha.</p> : <>
            {fichaLinhas.map((linha, i) => <div key={i} className="grid grid-cols-[1fr_130px_36px] items-end gap-2"><Campo rotulo={i === 0 ? "Matéria-prima" : undefined}><Selecao required value={linha.materiaPrimaId} onChange={(e) => setFichaLinhas(fichaLinhas.map((x, j) => j === i ? { ...x, materiaPrimaId: e.target.value } : x))}><option value="">Selecione</option>{ativos.map((m) => <option key={m.id} value={m.id}>{m.nome} ({m.unidade})</option>)}</Selecao></Campo><Campo rotulo={i === 0 ? "Consumo por unidade" : undefined}><Entrada type="number" min="0.001" step="0.001" required value={linha.quantidadePorUnidade} onChange={(e) => setFichaLinhas(fichaLinhas.map((x, j) => j === i ? { ...x, quantidadePorUnidade: Number(e.target.value) } : x))} /></Campo><Botao variante="fantasma" tamanho="sm" aria-label="Remover material da ficha" onClick={() => setFichaLinhas(fichaLinhas.filter((_, j) => j !== i))} icone={<Trash2 className="h-4 w-4" />} /></div>)}
            <Botao variante="secundaria" tamanho="sm" onClick={() => setFichaLinhas([...fichaLinhas, { materiaPrimaId: "", quantidadePorUnidade: 1 }])}>Adicionar matéria-prima</Botao>
            <p className="text-xs text-texto-fraco">Deixe a lista vazia para remover a ficha técnica deste produto.</p>
          </>}
        </> : modal === "material" ? <>
          <Campo rotulo="Nome do material" obrigatorio><Entrada maxLength={120} required value={materialForm.nome} onChange={(e) => setMaterialForm({ ...materialForm, nome: e.target.value })} placeholder="Ex.: Camiseta branca, tinta azul-marinho" /></Campo>
          <div className="grid gap-4 sm:grid-cols-3"><Campo rotulo="Unidade" obrigatorio ajuda="Ex.: un, kg, m, L, rolo"><Entrada maxLength={20} required value={materialForm.unidade} onChange={(e) => setMaterialForm({ ...materialForm, unidade: e.target.value })} /></Campo><Campo rotulo="Saldo disponível"><Entrada type="number" min="0" step="0.001" value={materialForm.quantidadeDisponivel} onChange={(e) => setMaterialForm({ ...materialForm, quantidadeDisponivel: Number(e.target.value) })} /></Campo><Campo rotulo="Estoque mínimo"><Entrada type="number" min="0" step="0.001" value={materialForm.estoqueMinimo} onChange={(e) => setMaterialForm({ ...materialForm, estoqueMinimo: Number(e.target.value) })} /></Campo></div>
          <p className="text-xs text-texto-fraco">O saldo disponível pode ser ajustado após conferência física do estoque.</p>
        </> : <>
          <Campo rotulo="Matéria-prima" obrigatorio><Selecao required value={compraForm.materiaPrimaId} onChange={(e) => setCompraForm({ ...compraForm, materiaPrimaId: e.target.value })}><option value="">Selecione um material</option>{ativos.map((m) => <option key={m.id} value={m.id}>{m.nome} ({m.unidade})</option>)}</Selecao></Campo>
          <div className="grid gap-4 sm:grid-cols-2"><Campo rotulo="Quantidade" obrigatorio><Entrada type="number" min="0.001" step="0.001" required value={compraForm.quantidade} onChange={(e) => setCompraForm({ ...compraForm, quantidade: Number(e.target.value) })} /></Campo><Campo rotulo="Data prevista" obrigatorio><Entrada type="date" required value={compraForm.dataPrevista} onChange={(e) => setCompraForm({ ...compraForm, dataPrevista: e.target.value })} /></Campo></div>
          <Campo rotulo="Fornecedor"><Entrada maxLength={120} value={compraForm.fornecedor} onChange={(e) => setCompraForm({ ...compraForm, fornecedor: e.target.value })} placeholder="Opcional" /></Campo>
          <p className="text-xs text-texto-fraco">Após salvar, a compra aparece como planejada. Atualize para “Pedido realizado” ou “Recebida” conforme o andamento.</p>
        </>}
      </form>
    </Modal>
    <Modal aberto={!!historicoMaterial} aoFechar={() => setHistoricoMaterial(null)} titulo={`Movimentações · ${historicoMaterial?.nome ?? ""}`} descricao="Entradas, saídas para pedidos e ajustes manuais de saldo." largura="max-w-2xl">
      {movimentos.isError ? <ErroCarregamento mensagem={erroDaApi(movimentos.error).message} aoTentar={() => movimentos.refetch()} />
        : movimentos.isLoading ? <p className="text-sm text-texto-fraco">Carregando histórico…</p>
          : movimentos.data?.length ? <div className="overflow-x-auto"><table className="tabela w-full min-w-[540px] text-sm"><thead><tr><th>Data</th><th>Movimento</th><th>Quantidade</th><th>Origem</th></tr></thead><tbody>{movimentos.data.map((x) => <tr key={x.id}><td className="numero text-texto-suave">{new Date(x.criadoEm).toLocaleString("pt-BR")}</td><td>{x.tipo === "EntradaCompra" ? "Entrada de compra" : x.tipo === "SaidaPedido" ? "Consumo de pedido" : "Ajuste"}{x.observacao && <p className="text-xs text-texto-fraco">{x.observacao}</p>}</td><td className={`numero font-medium ${x.variacao < 0 ? "text-red-700" : "text-emerald-700"}`}>{x.variacao > 0 ? "+" : ""}{numero(x.variacao)} {historicoMaterial?.unidade}</td><td>{x.pedidoId ? <Link href={`/pedidos/${x.pedidoId}`} className="text-primaria hover:underline">Pedido #{x.pedidoNumero}</Link> : "—"}</td></tr>)}</tbody></table></div>
            : <p className="text-sm text-texto-fraco">Ainda não há movimentações para este material.</p>}
    </Modal>
  </>;
}
