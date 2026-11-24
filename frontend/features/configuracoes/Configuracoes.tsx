"use client";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil, Plus } from "lucide-react";
import { AcoesLinha, Aviso, Botao, CabecalhoPagina, Campo, Carregando, Cartao, Entrada, ErroCarregamento, Modal, Selecao } from "@/components/ui";
import { useUsuarioAtual } from "@/hooks";
import { configuracoesService } from "@/services";
import { erroDaApi } from "@/lib/api";
import type { OpcaoCatalogo, OpcaoCatalogoRequisicao, TipoOpcaoCatalogo } from "@/types/api";

const tipos: Record<TipoOpcaoCatalogo, string> = { CategoriaProduto: "Categorias de produtos", TipoPersonalizacao: "Tipos de personalização", CondicaoPagamento: "Condições de pagamento", MotivoDecisao: "Motivos de decisão" };

function FormularioOpcao({ opcao, tipo, aoSalvar }: { opcao?: OpcaoCatalogo; tipo: TipoOpcaoCatalogo; aoSalvar: () => Promise<void> }) {
  const [nome, setNome] = useState(opcao?.nome ?? "");
  const [ordem, setOrdem] = useState(opcao?.ordem ?? 0);
  const [ativo, setAtivo] = useState(opcao?.ativo ?? true);
  const salvar = useMutation({ mutationFn: () => {
    const dados: OpcaoCatalogoRequisicao = { nome, ordem, ativo, tipo, versao: opcao?.versao };
    return opcao ? configuracoesService.atualizarOpcao(opcao.id, dados) : configuracoesService.criarOpcao(dados);
  }, onSuccess: aoSalvar });
  const erro = salvar.isError ? erroDaApi(salvar.error) : null;
  return <form className="space-y-4" onSubmit={ev => { ev.preventDefault(); salvar.mutate(); }}>
    {erro && <Aviso tipo="erro">{erro.message}</Aviso>}
    <Campo rotulo="Nome da opção" htmlFor="opcao-nome" erro={erro?.errors.nome?.[0]}><Entrada id="opcao-nome" value={nome} maxLength={tipo === "CategoriaProduto" ? 50 : 100} onChange={ev => setNome(ev.target.value)} /></Campo>
    <Campo rotulo="Ordem de exibição" htmlFor="opcao-ordem" erro={erro?.errors.ordem?.[0]}><Entrada id="opcao-ordem" type="number" min={0} max={9999} step={1} value={ordem} onChange={ev => setOrdem(Number(ev.target.value))} /></Campo>
    <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={ativo} onChange={ev => setAtivo(ev.target.checked)} />Opção ativa</label>
    <p className="text-sm text-texto-fraco">Desativar retira a opção das novas seleções. Os valores já registrados em produtos, orçamentos e pedidos são preservados.</p>
    <div className="flex justify-end"><Botao type="submit" carregando={salvar.isPending}>Salvar opção</Botao></div>
  </form>;
}

export function Configuracoes() {
  const qc = useQueryClient();
  const { data: usuario, isPending } = useUsuarioAtual();
  const admin = usuario?.perfil === "Administrador";
  const q = useQuery({ queryKey: ["configuracoes", "opcoes"], queryFn: configuracoesService.opcoes, enabled: admin });
  const [tipo, setTipo] = useState<TipoOpcaoCatalogo>("CategoriaProduto");
  const [edicao, setEdicao] = useState<OpcaoCatalogo | "nova" | null>(null);
  if (isPending) return <Carregando />;
  if (!admin) return <Aviso tipo="erro" titulo="Acesso restrito">Somente o Administrador pode acessar as configurações.</Aviso>;
  return <>
    <CabecalhoPagina titulo="Configurações" subtitulo="Gerencie as opções usadas no catálogo e no fluxo comercial." acoes={<Botao icone={<Plus className="h-4 w-4" />} onClick={() => setEdicao("nova")}>Nova opção</Botao>} />
    <Cartao>
      <Campo rotulo="Grupo de opções" htmlFor="grupo-opcoes"><Selecao id="grupo-opcoes" value={tipo} onChange={ev => setTipo(ev.target.value as TipoOpcaoCatalogo)}>{Object.entries(tipos).map(([id, rotulo]) => <option key={id} value={id}>{rotulo}</option>)}</Selecao></Campo>
      {q.isError ? <ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} /> : q.isPending ? <Carregando /> : <div className="mt-5 divide-y divide-borda">
        {!q.data.some(x => x.tipo === tipo) && <p className="text-sm text-texto-fraco">Nenhuma opção cadastrada neste grupo.</p>}
        {q.data.filter(x => x.tipo === tipo).map(x => <div key={x.id} className="group flex items-center justify-between gap-3 py-3"><div className="min-w-0"><p className="break-words font-medium">{x.nome}</p><p className="text-xs text-texto-fraco">Ordem {x.ordem} · {x.ativo ? "Ativa" : "Inativa"}</p></div><AcoesLinha acoes={[{ rotulo: `Editar ${x.nome}`, icone: <Pencil className="h-4 w-4" />, aoAcionar: () => setEdicao(x) }]} /></div>)}
      </div>}
    </Cartao>
    <Modal aberto={!!edicao} aoFechar={() => setEdicao(null)} titulo={edicao === "nova" ? "Nova opção" : "Editar opção"} descricao={tipos[tipo]}>
      {edicao && <FormularioOpcao key={edicao === "nova" ? `nova-${tipo}` : `${edicao.id}-${edicao.versao}`} opcao={edicao === "nova" ? undefined : edicao} tipo={tipo} aoSalvar={async () => { await Promise.all([qc.invalidateQueries({ queryKey: ["configuracoes"] }), qc.invalidateQueries({ queryKey: ["catalogo"] }), qc.invalidateQueries({ queryKey: ["motivos-decisao"] })]); setEdicao(null); }} />}
    </Modal>
  </>;
}
