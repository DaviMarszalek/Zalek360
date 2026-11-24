"use client";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Archive, Copy, Pencil, Plus, RotateCcw, Trash2 } from "lucide-react";
import { AcoesLinha, Aviso, Botao, CabecalhoPagina, Campo, Carregando, Cartao, Entrada, ErroCarregamento, EstadoVazio, Modal } from "@/components/ui";
import { useToast } from "@/components/ui/toast";
import { useDebounce, useUsuarioAtual } from "@/hooks";
import { catalogoService } from "@/services";
import { erroDaApi } from "@/lib/api";
import { moeda } from "@/utils/formatos";
import { FormularioProduto } from "./FormularioProduto";
import type { Produto } from "@/types/api";

const valorInformado = (valor: number | null | undefined) => valor == null ? "Não informado" : moeda(valor);

export function Produtos() {
  const qc = useQueryClient();
  const toast = useToast();
  const { data: usuario } = useUsuarioAtual();
  const [busca, setBusca] = useState("");
  const termo = useDebounce(busca);
  const [inativos, setInativos] = useState(false);
  const q = useQuery({ queryKey: ["catalogo", "gestao", termo, inativos], queryFn: ({ signal }) => catalogoService.listarProdutos(termo, inativos, signal) });
  const [edicao, setEdicao] = useState<Produto | "novo" | null>(null);
  const [confirmacao, setConfirmacao] = useState<{ produto: Produto; acao: "duplicar" | "situacao" | "excluir" } | null>(null);
  const [nomeCopia, setNomeCopia] = useState("");
  const atualizar = async () => {
    // Uma pesquisa inicial em andamento pode conter o produto anterior à alteração.
    await qc.cancelQueries({ queryKey: ["catalogo", "gestao"] });
    await Promise.all([qc.invalidateQueries({ queryKey: ["catalogo"] }), qc.invalidateQueries({ queryKey: ["estoque", "fichas"] })]);
  };
  const executar = useMutation({ mutationFn: async () => {
    if (!confirmacao) return;
    const { produto: p, acao } = confirmacao;
    if (acao === "duplicar") await catalogoService.duplicarProduto(p.id, nomeCopia);
    else if (acao === "excluir") await catalogoService.excluirProduto(p.id, p.versao);
    else await catalogoService.situacaoProduto(p.id, !p.ativo, p.versao);
  }, onSuccess: async () => { await atualizar(); setConfirmacao(null); toast("sucesso", "Catálogo atualizado."); } });
  function confirmar(produto: Produto, acao: "duplicar" | "situacao" | "excluir") { executar.reset(); setNomeCopia(""); setConfirmacao({ produto, acao }); }
  return <>
    <CabecalhoPagina titulo="Produtos" subtitulo="Organize o catálogo, os valores internos e a composição de cada produto." acoes={<Botao icone={<Plus className="h-4 w-4" />} onClick={() => setEdicao("novo")}>Novo produto</Botao>} />
    <Cartao semPadding>
      <div className="flex flex-wrap items-center gap-3 border-b border-borda p-4"><Entrada className="min-w-0 flex-1" aria-label="Pesquisar produtos" placeholder="Pesquisar por nome ou categoria" value={busca} onChange={ev => setBusca(ev.target.value)} /><label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={inativos} onChange={ev => setInativos(ev.target.checked)} />Mostrar inativos</label></div>
      {q.isError ? <div className="p-5"><ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} /></div> : q.isPending ? <Carregando /> : !q.data.length ? <EstadoVazio titulo="Nenhum produto encontrado" descricao="Cadastre um produto ou ajuste a pesquisa." /> :
        <div className="divide-y divide-borda">{q.data.map(p => <article key={p.id} className="group flex flex-wrap items-center justify-between gap-4 p-5">
          <div className="min-w-0 w-full sm:flex-1"><h2 className="break-words font-semibold">{p.nome} {!p.ativo && <span className="text-xs font-normal text-texto-fraco">Inativo</span>}</h2><p className="text-sm text-texto-fraco">{p.categoria} · {p.materiais?.length ? `${p.materiais.length} ${p.materiais.length === 1 ? "material" : "materiais"} na composição` : "Sem composição"}</p><p className="mt-1 flex flex-wrap gap-x-4 gap-y-1 text-sm"><span className="numero">Venda: {valorInformado(p.precoVenda)}</span><span className="numero text-texto-fraco">Custo: {valorInformado(p.custo)}</span></p>{p.usado && <p className="mt-1 text-xs text-texto-fraco">Já usado em orçamento ou pedido; histórico preservado.</p>}</div>
          <AcoesLinha className="ml-auto" acoes={[
            { rotulo: `Editar ${p.nome}`, icone: <Pencil className="h-4 w-4" />, aoAcionar: () => setEdicao(p) },
            { rotulo: `Duplicar ${p.nome}`, icone: <Copy className="h-4 w-4" />, aoAcionar: () => confirmar(p, "duplicar") },
            { rotulo: `${p.ativo ? "Inativar" : "Reativar"} ${p.nome}`, icone: p.ativo ? <Archive className="h-4 w-4" /> : <RotateCcw className="h-4 w-4" />, aoAcionar: () => confirmar(p, "situacao") },
            ...(usuario?.perfil === "Administrador" && !p.usado ? [{ rotulo: `Excluir ${p.nome}`, icone: <Trash2 className="h-4 w-4" />, aoAcionar: () => confirmar(p, "excluir"), perigo: true }] : []),
          ]} />
        </article>)}</div>}
    </Cartao>
    <Modal aberto={!!edicao} aoFechar={() => setEdicao(null)} titulo={edicao === "novo" ? "Novo produto" : "Editar produto"} largura="max-w-3xl">
      {edicao && <FormularioProduto key={edicao === "novo" ? "novo" : `${edicao.id}-${edicao.versao}`} produto={edicao === "novo" ? undefined : edicao} aoSalvar={async () => { await atualizar(); setEdicao(null); toast("sucesso", "Produto salvo."); }} />}
    </Modal>
    <Modal aberto={!!confirmacao} aoFechar={() => { if (!executar.isPending) setConfirmacao(null); }} titulo={confirmacao?.acao === "duplicar" ? "Duplicar produto" : confirmacao?.acao === "excluir" ? "Excluir produto definitivamente" : confirmacao?.produto.ativo ? "Inativar produto" : "Reativar produto"}
      rodape={<><Botao variante="secundaria" disabled={executar.isPending} onClick={() => setConfirmacao(null)}>Cancelar</Botao><Botao variante={confirmacao?.acao === "excluir" ? "perigo" : "primaria"} carregando={executar.isPending} onClick={() => executar.mutate()}>Confirmar</Botao></>}>
      {executar.isError && <Aviso tipo="erro" className="mb-3">{erroDaApi(executar.error).message}</Aviso>}
      <p className="mb-3 break-words text-sm">{confirmacao?.produto.nome}</p>
      {confirmacao?.acao === "duplicar" ? <><p className="mb-3 text-sm text-texto-fraco">A cópia terá os mesmos valores e composição, sem vínculos com orçamentos e pedidos.</p><Campo rotulo="Nome da cópia" htmlFor="nome-copia" erro={executar.isError ? erroDaApi(executar.error).errors.nome?.[0] : undefined}><Entrada id="nome-copia" value={nomeCopia} maxLength={100} onChange={ev => setNomeCopia(ev.target.value)} /></Campo></> : <p className="text-sm text-texto-fraco">{confirmacao?.acao === "excluir" ? "Esta exclusão é definitiva e só é permitida para produtos nunca usados." : confirmacao?.produto.ativo ? "O histórico será mantido. O produto deixará de aparecer para novos orçamentos." : "O produto voltará a aparecer para novos orçamentos."}</p>}
    </Modal>
  </>;
}
