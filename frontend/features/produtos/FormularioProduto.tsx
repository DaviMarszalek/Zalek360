"use client";
import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Plus, Trash2 } from "lucide-react";
import { AreaTexto, Aviso, Botao, Campo, Carregando, Entrada, ErroCarregamento, Selecao } from "@/components/ui";
import { catalogoService, estoqueService } from "@/services";
import { erroCampo, erroDaApi } from "@/lib/api";
import type { Produto } from "@/types/api";

export function FormularioProduto({ produto, aoSalvar }: { produto?: Produto; aoSalvar: (produto: Produto) => Promise<void> }) {
  const [nome, setNome] = useState(produto?.nome ?? "");
  const [categoria, setCategoria] = useState(produto?.categoria ?? "");
  const [descricao, setDescricao] = useState(produto?.descricaoBase ?? "");
  const [preco, setPreco] = useState(produto?.precoVenda?.toString().replace(".", ",") ?? "");
  const [custo, setCusto] = useState(produto?.custo?.toString().replace(".", ",") ?? "");
  const [linhas, setLinhas] = useState(() => (produto?.materiais ?? []).map(x => ({ materiaPrimaId: x.materiaPrimaId, quantidadePorUnidade: x.quantidadePorUnidade })));
  const opcoes = useQuery({ queryKey: ["catalogo", "opcoes-ativas"], queryFn: catalogoService.opcoesAtivas });
  const materias = useQuery({ queryKey: ["estoque", "materias"], queryFn: estoqueService.materias });
  const [erroLocal, setErroLocal] = useState("");
  const salvar = useMutation({ mutationFn: async () => {
    function valor(texto: string) {
      if (!texto.trim()) return null;
      if (!/^\d+(?:[.,]\d{1,2})?$/.test(texto.trim())) throw new Error("Informe preço e custo como valores não negativos com até duas casas decimais.");
      return Number(texto.replace(",", "."));
    }
    const dados = { nome, categoria, descricaoBase: descricao, precoVenda: valor(preco), custo: valor(custo), materiais: linhas, versao: produto?.versao };
    return produto ? catalogoService.atualizarProduto(produto.id, dados) : catalogoService.criarProduto(dados);
  }, onSuccess: aoSalvar });
  const erro = salvar.isError ? erroDaApi(salvar.error) : null;
  const e = (campo: string) => erroCampo(erro, campo);
  if (opcoes.isPending || materias.isPending) return <Carregando texto="Carregando categorias e matérias-primas…" />;
  if (opcoes.isError || materias.isError) return <ErroCarregamento mensagem="Não foi possível carregar categorias e matérias-primas." aoTentar={() => { opcoes.refetch(); materias.refetch(); }} />;
  const categorias = [...new Set([...(opcoes.data ?? []).filter(x => x.tipo === "CategoriaProduto").map(x => x.nome), ...(produto?.categoria ? [produto.categoria] : [])])];
  return <form className="space-y-5" onSubmit={async ev => { ev.preventDefault(); setErroLocal(""); try { await salvar.mutateAsync(); } catch (err) { if (err instanceof Error && !("isAxiosError" in err)) setErroLocal(err.message); } }}>
    {(erro || erroLocal) && <Aviso tipo="erro">{erroLocal || erro?.message}</Aviso>}
    <div className="grid gap-4 sm:grid-cols-2">
      <Campo rotulo="Nome do produto" htmlFor="produto-nome" obrigatorio erro={e("nome")}><Entrada id="produto-nome" value={nome} maxLength={100} onChange={ev => setNome(ev.target.value)} /></Campo>
      <Campo rotulo="Categoria" htmlFor="produto-categoria" obrigatorio erro={e("categoria")}><Selecao id="produto-categoria" value={categoria} onChange={ev => setCategoria(ev.target.value)}><option value="">Selecione</option>{categorias.map(x => <option key={x}>{x}</option>)}</Selecao></Campo>
      <Campo rotulo="Preço de venda (R$)" htmlFor="produto-preco" erro={e("precoVenda")} ajuda="Opcional. Não altera os valores dos orçamentos existentes."><Entrada id="produto-preco" className="numero text-right" inputMode="decimal" value={preco} onChange={ev => setPreco(ev.target.value)} /></Campo>
      <Campo rotulo="Custo (R$)" htmlFor="produto-custo" erro={e("custo")} ajuda="Informação interna; não é enviada ao cliente."><Entrada id="produto-custo" className="numero text-right" inputMode="decimal" value={custo} onChange={ev => setCusto(ev.target.value)} /></Campo>
      <Campo rotulo="Descrição" htmlFor="produto-descricao" erro={e("descricaoBase")} className="sm:col-span-2"><AreaTexto id="produto-descricao" value={descricao} maxLength={500} onChange={ev => setDescricao(ev.target.value)} /></Campo>
    </div>
    <section className="space-y-3 rounded-lg border border-borda p-4" aria-label="Composição do produto">
      <div className="flex items-center justify-between gap-2"><h3 className="font-semibold">Composição por unidade</h3><Botao tamanho="sm" variante="secundaria" icone={<Plus className="h-4 w-4" />} onClick={() => setLinhas(l => [...l, { materiaPrimaId: "", quantidadePorUnidade: 1 }])}>Adicionar material</Botao></div>
      {e("materiais") && <Aviso tipo="erro">{e("materiais")}</Aviso>}
      {linhas.length === 0 && <p className="text-sm text-texto-fraco">Nenhum material definido. Você pode completar a composição depois.</p>}
      {linhas.map((linha, i) => <div key={i} className="grid grid-cols-[minmax(0,1fr)_80px_36px] items-end gap-2">
        <Campo rotulo={`Material ${i + 1}`} htmlFor={`material-${i}`}><Selecao id={`material-${i}`} value={linha.materiaPrimaId} onChange={ev => setLinhas(l => l.map((x, j) => j === i ? { ...x, materiaPrimaId: ev.target.value } : x))}><option value="">Selecione</option>{materias.data?.filter(m => m.ativo || m.id === linha.materiaPrimaId).map(m => <option key={m.id} value={m.id}>{m.nome}{!m.ativo ? " (inativa)" : ""}</option>)}</Selecao></Campo>
        <Campo rotulo="Quantidade" htmlFor={`quantidade-${i}`}><Entrada id={`quantidade-${i}`} aria-label={`Quantidade ${i + 1}`} className="numero text-right" type="number" min={1} step={1} value={linha.quantidadePorUnidade} onChange={ev => setLinhas(l => l.map((x, j) => j === i ? { ...x, quantidadePorUnidade: Number(ev.target.value) } : x))} /></Campo>
        <Botao variante="fantasma" className="w-9 px-0" aria-label={`Remover material ${i + 1}`} onClick={() => setLinhas(l => l.filter((_, j) => j !== i))}><Trash2 className="h-4 w-4" /></Botao>
      </div>)}
    </section>
    <div className="flex justify-end"><Botao type="submit" carregando={salvar.isPending} disabled={opcoes.isPending || materias.isPending}>Salvar produto</Botao></div>
  </form>;
}
