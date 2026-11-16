
"use client";
import { useMemo, useState } from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { AlertTriangle, Archive, CalendarDays, CheckCircle2, ClipboardList, History, PackageCheck, PackagePlus, Pencil, Plus, RotateCcw, Search, Trash2, Truck, XCircle } from "lucide-react";
import { AcoesLinha, Botao, CabecalhoPagina, Campo, Cartao, Entrada, ErroCarregamento, EstadoVazio, Modal, Selecao } from "@/components/ui";
import { catalogoService, estoqueService } from "@/services";
import { useUsuarioAtual } from "@/hooks";
import { useToast } from "@/components/ui/toast";
import { erroDaApi } from "@/lib/api";
import { numeroDocumento } from "@/utils/formatos";
import type { CompraMateriaPrima, MateriaPrima, ProdutoFichaTecnica, SituacaoCompraMateriaPrima } from "@/types/api";

type MaterialForm = { nome: string; quantidadeDisponivel: number; estoqueMinimo: number };
const materialVazio: MaterialForm = { nome: "", quantidadeDisponivel: 0, estoqueMinimo: 0 };
type CompraForm = { materiaPrimaId: string; quantidade: number; fornecedor: string; dataPrevista: string };
type FichaLinha = { materiaPrimaId: string; quantidadePorUnidade: number };
type ProdutoForm = { nome: string; categoria: string; descricaoBase: string };
const dataHoje = () => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`; };
const numero = (n: number) => new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 0 }).format(n);
const dataPt = (d: string) => { const [a, m, dia] = d.slice(0, 10).split("-"); return `${dia}/${m}/${a}`; };

export function Estoque() {
  const router = useRouter();
  const qc = useQueryClient();
  const toast = useToast();
  const { data: usuario } = useUsuarioAtual();
  const podeGerenciarFichas = usuario?.perfil === "Gestor" || usuario?.perfil === "Administrador";
  const [busca, setBusca] = useState("");
  const [mostrarInativos, setMostrarInativos] = useState(false);
  const [modal, setModal] = useState<"material" | "compra" | "ficha" | "produtos" | "produto-novo" | null>(null);
  const [materialAtual, setMaterialAtual] = useState<MateriaPrima | null>(null);
  const [historicoMaterial, setHistoricoMaterial] = useState<MateriaPrima | null>(null);
  const [compraCancelar, setCompraCancelar] = useState<CompraMateriaPrima | null>(null);
  const [materialForm, setMaterialForm] = useState<MaterialForm>(materialVazio);
  const [compraForm, setCompraForm] = useState<CompraForm>({ materiaPrimaId: "", quantidade: 1, fornecedor: "", dataPrevista: dataHoje() });
  const [produtoForm, setProdutoForm] = useState<ProdutoForm>({ nome: "", categoria: "", descricaoBase: "" });
  const [produtoFicha, setProdutoFicha] = useState<ProdutoFichaTecnica | null>(null);
  const [fichaLinhas, setFichaLinhas] = useState<FichaLinha[]>([]);
  const [erro, setErro] = useState("");
  const pendencias = useQuery({ queryKey: ["estoque", "pendencias"], queryFn: estoqueService.pendencias });
  const materias = useQuery({ queryKey: ["estoque", "materias"], queryFn: estoqueService.materias });
  const compras = useQuery({ queryKey: ["estoque", "compras"], queryFn: estoqueService.compras });
  const fichas = useQuery({ queryKey: ["estoque", "fichas"], queryFn: estoqueService.fichas, enabled: podeGerenciarFichas });
  const movimentos = useQuery({ queryKey: ["estoque", "movimentos", historicoMaterial?.id], queryFn: () => estoqueService.movimentos(historicoMaterial!.id), enabled: !!historicoMaterial });
  const atualizar = async () => { await qc.invalidateQueries({ queryKey: ["estoque"] }); };
  const salvarMaterial = useMutation({ mutationFn: () => materialAtual ? estoqueService.atualizarMateria(materialAtual.id, materialForm) : estoqueService.criarMateria(materialForm), onSuccess: async () => { await atualizar(); setModal(null); setErro(""); } });
  const salvarCompra = useMutation({ mutationFn: () => estoqueService.criarCompra(compraForm), onSuccess: async () => { await atualizar(); setModal(null); setErro(""); } });
  const salvarFicha = useMutation({ mutationFn: () => estoqueService.salvarFicha(produtoFicha!.produtoId, fichaLinhas), onSuccess: async () => { await qc.invalidateQueries({ queryKey: ["estoque", "fichas"] }); setModal(null); setErro(""); } });
  const salvarProduto = useMutation({ mutationFn: () => catalogoService.criarProduto(produtoForm), onSuccess: async (produto) => {
    await Promise.all([
      qc.invalidateQueries({ queryKey: ["catalogo", "produtos"] }),
      qc.invalidateQueries({ queryKey: ["estoque", "fichas"] }),
    ]);
    toast("sucesso", `Produto ${produto.nome} cadastrado.`);
    setErro("");
    setModal("produtos");
  } });
  const mudarCompra = useMutation({ mutationFn: ({ id, situacao }: { id: string; situacao: SituacaoCompraMateriaPrima }) => estoqueService.atualizarSituacaoCompra(id, situacao), onSuccess: async (_, variaveis) => {
    await atualizar();
    if (variaveis.situacao === "Cancelada") setCompraCancelar(null);
  }, onError: (e) => toast("erro", erroDaApi(e).message) });
  const mudarAtivo = useMutation({ mutationFn: ({ id, ativo }: { id: string; ativo: boolean }) => estoqueService.definirMateriaAtiva(id, ativo), onSuccess: atualizar, onError: (e) => toast("erro", erroDaApi(e).message) });
  const itens = useMemo(() => (materias.data ?? []).filter((m) => (mostrarInativos || m.ativo) && `${m.nome} ${m.unidade}`.toLocaleLowerCase("pt-BR").includes(busca.toLocaleLowerCase("pt-BR"))), [materias.data, mostrarInativos, busca]);
  const ativos = materias.data?.filter((m) => m.ativo) ?? [];
  const baixos = ativos.filter((m) => m.noMinimo);
  const comprasAbertas = (compras.data ?? []).filter((c) => c.aberta);

  function abrirMaterial(m?: MateriaPrima) { setMaterialAtual(m ?? null); setMaterialForm(m ? { nome: m.nome, quantidadeDisponivel: m.quantidadeDisponivel, estoqueMinimo: m.estoqueMinimo } : { ...materialVazio }); setErro(""); setModal("material"); }
  function abrirCompra(m?: MateriaPrima) { setCompraForm({ materiaPrimaId: m?.id ?? ativos[0]?.id ?? "", quantidade: 1, fornecedor: "", dataPrevista: dataHoje() }); setErro(""); setModal("compra"); }
  function abrirFicha(p: ProdutoFichaTecnica) { setProdutoFicha(p); setFichaLinhas(p.materiais.map((x) => ({ materiaPrimaId: x.materiaPrimaId, quantidadePorUnidade: x.quantidadePorUnidade }))); setErro(""); setModal("ficha"); }
  async function submeter(e: React.FormEvent) { e.preventDefault(); setErro(""); try {
    if (modal === "material") await salvarMaterial.mutateAsync();
    else if (modal === "compra") await salvarCompra.mutateAsync();
    else if (modal === "ficha") await salvarFicha.mutateAsync();
    else if (modal === "produto-novo") await salvarProduto.mutateAsync();
  } catch (err) { setErro(erroDaApi(err).message); } }
  const carregando = salvarMaterial.isPending || salvarCompra.isPending || salvarFicha.isPending || salvarProduto.isPending;

  return <>
    <CabecalhoPagina titulo="Estoque de matéria-prima" subtitulo="Acompanhe o saldo, os níveis mínimos e as compras que estão a caminho."
      acoes={<><Botao variante="secundaria" icone={<Truck className="h-4 w-4" />} onClick={() => abrirCompra()}>Planejar compra</Botao><Botao icone={<Plus className="h-4 w-4" />} onClick={() => abrirMaterial()}>Nova matéria-prima</Botao></>} />

    <div className="mb-5 grid gap-3 sm:grid-cols-3">
      <Cartao><p className="text-xs font-medium uppercase tracking-wide text-texto-fraco">Matérias-primas ativas</p><p className="mt-2 text-2xl font-semibold numero text-texto">{ativos.length}</p></Cartao>
      <Cartao><p className="flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-texto-fraco"><AlertTriangle className="h-3.5 w-3.5" />No mínimo ou abaixo</p><p className={`mt-2 text-2xl font-semibold numero ${baixos.length ? "text-amber-700" : "text-texto"}`}>{baixos.length}</p></Cartao>
      <Cartao><p className="flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-texto-fraco"><Truck className="h-3.5 w-3.5" />Compras a caminho</p><p className="mt-2 text-2xl font-semibold numero text-texto">{comprasAbertas.length}</p></Cartao>
    </div>

    <Cartao titulo="Pendências de materiais" className="mb-5">
      {pendencias.isError ? <ErroCarregamento mensagem={erroDaApi(pendencias.error).message} aoTentar={() => pendencias.refetch()} />
        : pendencias.isLoading ? <p className="text-sm text-texto-fraco">Carregando pendências…</p>
          : !pendencias.data?.length ? <p className="text-sm text-texto-fraco">Nenhuma pendência de materiais.</p>
            : <div className="space-y-2">{pendencias.data.map((p) => <div key={p.pedidoId} className="flex flex-wrap items-center justify-between gap-2 text-sm"><Link href={`/pedidos/${p.pedidoId}`} className="font-medium text-primaria hover:underline">Pedido #{p.pedidoNumero}</Link><span className="text-amber-800">{p.descricao}</span><span className="numero text-texto-fraco">{dataPt(p.prazoEntrega)}</span></div>)}</div>}
    </Cartao>

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
                const livre = m.saldoDisponivel;
                const falta = m.faltaComprar;
                const baixo = m.noMinimo;
                return <tr key={m.id} className={`group ${!m.ativo ? "opacity-60" : ""}`}>
                  <td><span className="font-medium text-texto">{m.nome}</span><span className="ml-2 text-xs text-texto-fraco">({m.unidade})</span></td>
                  <td className="numero font-medium">{numero(m.quantidadeDisponivel)} <span className="text-xs font-normal text-texto-fraco">{m.unidade}</span></td>
                  <td className="numero text-texto-suave">{numero(m.quantidadeReservada)} {m.unidade}{m.quantidadePendente > 0 && <p className="text-xs text-amber-700">Pendente: {numero(m.quantidadePendente)} {m.unidade}</p>}</td>
                  <td className={`numero font-medium ${livre < 0 ? "text-red-700" : "text-texto"}`}>{numero(livre)} {m.unidade}</td>
                  <td className="numero text-texto-suave">{numero(m.estoqueMinimo)} {m.unidade}</td>
                  <td className="numero text-texto-suave">{numero(m.quantidadeAReceber)} {m.unidade}</td>
                  <td className={`numero font-medium ${falta > 0 ? "text-red-700" : "text-texto-suave"}`}>{numero(falta)} {m.unidade}</td>
                  <td><span className={`rounded-full px-2 py-1 text-xs ${!m.ativo ? "bg-slate-100 text-slate-600" : baixo ? "bg-amber-50 text-amber-800" : "bg-emerald-50 text-emerald-700"}`}>{!m.ativo ? "Inativa" : baixo && m.quantidadeAReceber > 0 ? "Reposição a caminho" : baixo ? "Atenção" : "Adequado"}</span></td>
                  <td><AcoesLinha acoes={[
                    { rotulo: `Ver movimentações de ${m.nome}`, icone: <History className="h-4 w-4" />, aoAcionar: () => setHistoricoMaterial(m) },
                    { rotulo: `Editar matéria-prima ${m.nome}`, icone: <Pencil className="h-4 w-4" />, aoAcionar: () => abrirMaterial(m) },
                    ...(m.ativo ? [
                      { rotulo: `Planejar compra de ${m.nome}`, icone: <PackagePlus className="h-4 w-4" />, aoAcionar: () => abrirCompra(m) },
                      { rotulo: `Desativar ${m.nome}`, icone: <Archive className="h-4 w-4" />, aoAcionar: () => mudarAtivo.mutate({ id: m.id, ativo: false }), desabilitada: mudarAtivo.isPending },
                    ] : [
                      { rotulo: `Reativar ${m.nome}`, icone: <RotateCcw className="h-4 w-4" />, aoAcionar: () => mudarAtivo.mutate({ id: m.id, ativo: true }), desabilitada: mudarAtivo.isPending },
                    ]),
                  ]} /></td>
                </tr>;
              })}
            </tbody></table></div>}
    </Cartao>

    <section className="mt-7">
      <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
        <div><h2 className="text-base font-semibold text-texto">Fichas técnicas</h2><p className="mt-0.5 text-sm text-texto-fraco">Defina os materiais consumidos por unidade de produto.</p></div>
        {podeGerenciarFichas && <Botao variante="secundaria" icone={<ClipboardList className="h-4 w-4" />} onClick={() => setModal("produtos")}>Produtos personalizados</Botao>}
      </div>
      <Cartao><p className="text-sm text-texto-suave">Consulte o catálogo e configure a ficha técnica de cada produto personalizado.</p></Cartao>
    </section>

    <section className="mt-7">
      <div className="mb-3 flex items-center justify-between gap-3"><div><h2 className="text-base font-semibold text-texto">Compras previstas e pedidos</h2><p className="mt-0.5 text-sm text-texto-fraco">Os itens entram no saldo disponível quando forem marcados como recebidos.</p></div><Botao variante="secundaria" tamanho="sm" icone={<Plus className="h-3.5 w-3.5" />} onClick={() => abrirCompra()}>Adicionar compra</Botao></div>
      <Cartao semPadding>
        {compras.isError ? <div className="p-4"><ErroCarregamento mensagem={erroDaApi(compras.error).message} aoTentar={() => compras.refetch()} /></div>
          : compras.isLoading ? <div className="p-6 text-center text-sm text-texto-fraco">Carregando compras…</div>
            : comprasAbertas.length === 0 ? <EstadoVazio icone={<Truck className="h-5 w-5" />} titulo="Nenhuma compra a caminho" descricao="Planeje uma reposição para acompanhar quantidades e previsão de chegada." acao={<Botao variante="secundaria" onClick={() => abrirCompra()}>Planejar compra</Botao>} />
              : <div className="overflow-x-auto"><table className="tabela w-full min-w-[760px] text-sm"><thead><tr><th>Matéria-prima</th><th>Quantidade</th><th>Fornecedor</th><th>Previsão</th><th>Situação</th><th className="text-right">Ações</th></tr></thead><tbody>
                {comprasAbertas.map((c) => <tr key={c.id} className="group"><td className="font-medium">{c.materiaPrimaNome}</td><td className="numero">{numero(c.quantidade)} {c.unidade}</td><td className="text-texto-suave">{c.fornecedor || "—"}</td><td className="numero text-texto-suave"><span className="inline-flex items-center gap-1"><CalendarDays className="h-3.5 w-3.5" />{dataPt(c.dataPrevista)}</span></td><td><span className={`rounded-full px-2 py-1 text-xs ${c.situacao === "Pedido" ? "bg-blue-50 text-blue-700" : "bg-slate-100 text-slate-700"}`}>{c.situacaoDescricao}</span></td><td><AcoesLinha acoes={c.situacoesPermitidas.map((opcao) => ({
                  rotulo: opcao.valor === "Cancelada" ? `Cancelar compra de ${c.materiaPrimaNome}` : opcao.valor === "Recebida" ? `Marcar compra de ${c.materiaPrimaNome} como recebida` : `Marcar compra de ${c.materiaPrimaNome} como pedido`,
                  icone: opcao.valor === "Cancelada" ? <XCircle className="h-4 w-4" /> : opcao.valor === "Recebida" ? <PackageCheck className="h-4 w-4" /> : <CheckCircle2 className="h-4 w-4" />,
                  aoAcionar: () => opcao.valor === "Cancelada" ? setCompraCancelar(c) : mudarCompra.mutate({ id: c.id, situacao: opcao.valor }),
                  desabilitada: mudarCompra.isPending,
                  perigo: opcao.valor === "Cancelada",
                }))} /></td></tr>)}
              </tbody></table></div>}
      </Cartao>
    </section>

    <Modal aberto={modal === "produtos"} aoFechar={() => setModal(null)} titulo="Produtos personalizados"
      descricao="Veja quais produtos já têm ficha técnica e configure os que ainda estão pendentes." largura="max-w-4xl"
      rodape={<Botao variante="secundaria" onClick={() => setModal(null)}>Fechar</Botao>}>
      <div className="mb-4 flex justify-end">
        <Botao icone={<Plus className="h-4 w-4" />} onClick={() => { setProdutoForm({ nome: "", categoria: "", descricaoBase: "" }); setErro(""); setModal("produto-novo"); }}>Cadastrar produto</Botao>
      </div>
      {fichas.isError ? <ErroCarregamento mensagem={erroDaApi(fichas.error).message} aoTentar={() => fichas.refetch()} />
        : fichas.isLoading ? <p className="py-8 text-center text-sm text-texto-fraco">Carregando produtos…</p>
          : fichas.data?.length === 0 ? <EstadoVazio titulo="Nenhum produto cadastrado" descricao="Cadastre um produto personalizado para incluí-lo no catálogo dos orçamentos." />
            : <div className="overflow-x-auto"><table className="tabela w-full min-w-[680px] text-sm">
              <thead><tr><th>Produto</th><th>Categoria</th><th>Ficha técnica</th><th className="w-16 text-right">Ações</th></tr></thead>
              <tbody>{fichas.data?.map((produto) => <tr key={produto.produtoId} className="group cursor-pointer hover:bg-slate-50/60" tabIndex={0}
                onClick={() => abrirFicha(produto)}
                onKeyDown={(evento) => {
                  if (evento.target !== evento.currentTarget) return;
                  if (evento.key === "Enter" || evento.key === " ") { evento.preventDefault(); abrirFicha(produto); }
                }}>
                <td><span className="font-medium">{produto.produtoNome}</span>{produto.descricaoBase && <p className="mt-0.5 max-w-sm truncate text-xs text-texto-fraco">{produto.descricaoBase}</p>}</td>
                <td className="text-texto-suave">{produto.categoria}</td>
                <td><span className={`rounded-full px-2 py-1 text-xs ${produto.temFicha ? "bg-emerald-50 text-emerald-700" : "bg-amber-50 text-amber-800"}`}>
                  {produto.temFicha ? "Com ficha técnica" : "Pendente"}
                </span></td>
                <td><AcoesLinha acoes={[{
                  rotulo: `${produto.temFicha ? "Editar" : "Configurar"} ficha técnica de ${produto.produtoNome}`,
                  icone: produto.temFicha ? <Pencil className="h-4 w-4" /> : <Plus className="h-4 w-4" />,
                  aoAcionar: () => abrirFicha(produto),
                }]} /></td>
              </tr>)}</tbody>
            </table></div>}
    </Modal>

    <Modal aberto={modal === "produto-novo"} aoFechar={() => { if (!salvarProduto.isPending) setModal("produtos"); }} titulo="Cadastrar produto personalizado"
      descricao="O novo produto ficará disponível para seleção nos orçamentos." largura="max-w-xl"
      rodape={<><Botao variante="secundaria" disabled={salvarProduto.isPending} onClick={() => setModal("produtos")}>Voltar</Botao>
        <Botao type="submit" form="form-produto" carregando={salvarProduto.isPending}>Cadastrar produto</Botao></>}>
      <form id="form-produto" onSubmit={submeter} className="space-y-4">
        {erro && <p role="alert" className="rounded-md bg-red-50 p-3 text-sm text-red-700">{erro}</p>}
        <Campo rotulo="Nome do produto" obrigatorio><Entrada required disabled={salvarProduto.isPending} value={produtoForm.nome} onChange={(e) => setProdutoForm({ ...produtoForm, nome: e.target.value })} /></Campo>
        <Campo rotulo="Categoria" obrigatorio><Entrada required disabled={salvarProduto.isPending} value={produtoForm.categoria} onChange={(e) => setProdutoForm({ ...produtoForm, categoria: e.target.value })} /></Campo>
        <Campo rotulo="Descrição base" ajuda="Descrição sugerida para os itens de orçamento; pode ser editada em cada orçamento.">
          <Entrada disabled={salvarProduto.isPending} value={produtoForm.descricaoBase} onChange={(e) => setProdutoForm({ ...produtoForm, descricaoBase: e.target.value })} />
        </Campo>
      </form>
    </Modal>

    <Modal aberto={modal === "material" || modal === "compra" || modal === "ficha"} aoFechar={() => { if (!carregando) setModal(null); }} titulo={modal === "compra" ? "Planejar compra de matéria-prima" : modal === "ficha" ? `Ficha técnica · ${produtoFicha?.produtoNome ?? "Produto"}` : materialAtual ? "Editar matéria-prima" : "Nova matéria-prima"}
      descricao={modal === "compra" ? "Registre uma compra prevista ou já solicitada ao fornecedor." : modal === "ficha" ? "Consumo padrão por unidade. O consumo variável pode ser ajustado no próprio pedido." : "Cadastre o material, a unidade de controle e o nível mínimo desejado."}
      rodape={<><Botao variante="secundaria" disabled={carregando} onClick={() => setModal(null)}>Cancelar</Botao><Botao type="submit" form="form-estoque" carregando={carregando}>{modal === "compra" ? "Salvar compra" : modal === "ficha" ? "Salvar ficha" : "Salvar matéria-prima"}</Botao></>}>
      <form id="form-estoque" onSubmit={submeter} className="space-y-4">
        {erro && <p role="alert" className="rounded-md bg-red-50 p-3 text-sm text-red-700">{erro}</p>}
        {modal === "ficha" ? <>
          {ativos.length === 0 ? <p className="text-sm text-texto-fraco">Cadastre pelo menos uma matéria-prima ativa para montar esta ficha.</p> : <>
            {fichaLinhas.map((linha, i) => <div key={i} className="grid grid-cols-[1fr_130px_36px] items-end gap-2"><Campo rotulo={i === 0 ? "Matéria-prima" : undefined}><Selecao required disabled={salvarFicha.isPending} value={linha.materiaPrimaId} onChange={(e) => setFichaLinhas(fichaLinhas.map((x, j) => j === i ? { ...x, materiaPrimaId: e.target.value } : x))}><option value="">Selecione</option>{ativos.map((m) => <option key={m.id} value={m.id}>{m.nome} ({m.unidade})</option>)}</Selecao></Campo><Campo rotulo={i === 0 ? "Consumo por unidade" : undefined}><Entrada type="number" min="1" step="1" required disabled={salvarFicha.isPending} value={linha.quantidadePorUnidade} onChange={(e) => setFichaLinhas(fichaLinhas.map((x, j) => j === i ? { ...x, quantidadePorUnidade: Number(e.target.value) } : x))} /></Campo><Botao variante="fantasma" tamanho="sm" aria-label="Remover material da ficha" disabled={salvarFicha.isPending} onClick={() => setFichaLinhas(fichaLinhas.filter((_, j) => j !== i))} icone={<Trash2 className="h-4 w-4" />} /></div>)}
            <Botao variante="secundaria" tamanho="sm" onClick={() => setFichaLinhas([...fichaLinhas, { materiaPrimaId: "", quantidadePorUnidade: 1 }])}>Adicionar matéria-prima</Botao>
            <p className="text-xs text-texto-fraco">Deixe a lista vazia para remover a ficha técnica deste produto.</p>
          </>}
        </> : modal === "material" ? <>
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
        : movimentos.isLoading ? <p className="text-sm text-texto-fraco">Carregando histórico…</p>
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
