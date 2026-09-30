"use client";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { FileImage, FileText, Loader2, PackagePlus, Paperclip, Plus, Search, Trash2, UserPlus, X } from "lucide-react";
import { Aviso, AreaTexto, Botao, Campo, Cartao, Entrada, EstadoVazio, Selecao } from "@/components/ui";
import { useToast } from "@/components/ui/toast";
import { CartaoCliente } from "@/features/comum";
import { anexosService, clientesService, orcamentosService } from "@/services";
import { useDebounce, useOpcoesOrcamento, useProdutos, useResponsaveis, useUsuarioAtual } from "@/hooks";
import { erroCampo, erroDaApi } from "@/lib/api";
import { cn } from "@/lib/utils";
import { data, documento, hojeIso, moeda, numeroDocumento, paraNumero, paraTextoDecimal, somarDias, tamanhoArquivo } from "@/utils/formatos";
import type { Anexo, ClienteCard, ErroApi, OrcamentoDetalhe, OrcamentoRequisicao } from "@/types/api";

interface AnexoForm { chave: string; anexo?: Anexo; nome: string; tamanho: number; progresso: number; erro?: string }
interface ItemForm {
  chave: string; id?: string; produtoId: string; descricao: string; quantidade: string; valorUnitario: string; valorPersonalizacao: string;
  tipoPersonalizacao: string; corPeca: string; coresArte: string; medidas: string; localAplicacao: string; referenciaArte: string;
  observacoesTecnicas: string; anexos: AnexoForm[];
}

let seq = 0;
const novaChave = () => `k${++seq}`;
const itemVazio = (): ItemForm => ({ chave: novaChave(), produtoId: "", descricao: "", quantidade: "", valorUnitario: "", valorPersonalizacao: "",
  tipoPersonalizacao: "", corPeca: "", coresArte: "", medidas: "", localAplicacao: "", referenciaArte: "", observacoesTecnicas: "", anexos: [] });

function SeletorCliente({ cliente, aoSelecionar, erro }: { cliente: ClienteCard | null; aoSelecionar: (c: ClienteCard | null) => void; erro?: string }) {
  const [termo, setTermo] = useState("");
  const [aberto, setAberto] = useState(false);
  const busca = useDebounce(termo.trim(), 250);
  const q = useQuery({ queryKey: ["clientes", "seletor", busca], queryFn: () => clientesService.listar({ busca }), enabled: aberto });
  if (cliente) return (
    <div className="flex items-start justify-between gap-4 rounded-lg border border-borda bg-slate-50/50 p-4">
      <CartaoCliente cliente={cliente} />
      <Botao variante="fantasma" tamanho="sm" onClick={() => aoSelecionar(null)}>Trocar</Botao>
    </div>
  );
  return (
    <div className="relative">
      <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-texto-fraco" />
      <Entrada className="pl-9" placeholder="Pesquisar cliente cadastrado por nome, CPF/CNPJ ou telefone" value={termo} invalido={!!erro}
        onChange={(e) => { setTermo(e.target.value); setAberto(true); }} onFocus={() => setAberto(true)} onBlur={() => setTimeout(() => setAberto(false), 180)} aria-label="Cliente" />
      {erro && <p className="mt-1 text-xs text-red-600">{erro}</p>}
      {aberto && (
        <div className="absolute z-20 mt-1 max-h-80 w-full overflow-y-auto rounded-lg border border-borda bg-white shadow-modal">
          {!q.data ? <p className="flex items-center gap-2 px-4 py-3 text-sm text-texto-fraco"><Loader2 className="h-4 w-4 animate-spin" />Buscando...</p>
            : q.data.itens.length === 0 ? <div className="px-4 py-4 text-sm"><p className="text-texto-suave">Nenhum cliente encontrado{busca && ` para “${busca}”`}.</p>
              <Link href={`/clientes/novo${busca ? `?nome=${encodeURIComponent(busca)}` : ""}`} className="mt-2 inline-flex items-center gap-1.5 font-medium text-primaria hover:underline"><UserPlus className="h-4 w-4" />Cadastrar novo cliente</Link></div>
              : q.data.itens.map((c) => (
                <button key={c.id} type="button" className="flex w-full items-center justify-between gap-3 border-b border-borda px-4 py-2.5 text-left last:border-0 hover:bg-slate-50"
                  onMouseDown={(e) => e.preventDefault()} onClick={() => { aoSelecionar({ id: c.id, nome: c.nome, tipoPessoa: c.tipoPessoa, documento: c.documento, contatoNome: null, contatoCargo: null, telefone: c.telefone, whatsApp: c.whatsApp, email: c.email, cidade: c.cidade }); setAberto(false); setTermo(""); }}>
                  <span><span className="block text-sm font-medium">{c.nome}</span><span className="numero text-xs text-texto-fraco">{documento(c.documento)}{c.cidade && ` · ${c.cidade}`}</span></span>
                  {c.ultimoOrcamento && <span className="numero text-xs text-texto-fraco">{numeroDocumento(c.ultimoOrcamento.numero)}</span>}
                </button>))}
        </div>)}
    </div>
  );
}

function ListaAnexos({ anexos, aoRemover }: { anexos: AnexoForm[]; aoRemover: (chave: string) => void }) {
  if (anexos.length === 0) return null;
  return (
    <ul className="mt-2 space-y-1.5">
      {anexos.map((a) => (
        <li key={a.chave} className={cn("flex items-center gap-3 rounded-md border px-3 py-2 text-sm", a.erro ? "border-red-200 bg-red-50" : "border-borda bg-white")}>
          {a.anexo?.ehImagem ? <FileImage className="h-4 w-4 text-texto-fraco" /> : <FileText className="h-4 w-4 text-texto-fraco" />}
          <div className="min-w-0 flex-1">
            {a.anexo ? <a href={anexosService.url(a.anexo.id, true)} target="_blank" rel="noreferrer" className="block truncate hover:text-primaria">{a.nome}</a> : <p className="truncate">{a.nome}</p>}
            {a.erro ? <p className="text-xs text-red-700">{a.erro}</p> : !a.anexo ? (
              <div className="mt-1 h-1 overflow-hidden rounded bg-slate-100"><div className="h-full bg-primaria transition-all" style={{ width: `${a.progresso}%` }} /></div>
            ) : <p className="numero text-xs text-texto-fraco">{tamanhoArquivo(a.tamanho)}</p>}
          </div>
          <button type="button" onClick={() => aoRemover(a.chave)} className="rounded p-1 text-texto-fraco hover:bg-slate-100" aria-label={`Remover ${a.nome}`}><X className="h-4 w-4" /></button>
        </li>))}
    </ul>
  );
}

export function FormularioOrcamento({ orcamento }: { orcamento?: OrcamentoDetalhe }) {
  const router = useRouter();
  const params = useSearchParams();
  const qc = useQueryClient();
  const toast = useToast();
  const edicao = !!orcamento;
  const hoje = hojeIso();
  const { data: usuario } = useUsuarioAtual();
  const produtos = useProdutos();
  const opcoes = useOpcoesOrcamento();
  const responsaveis = useResponsaveis();
  const clienteParam = params.get("clienteId");
  const clienteInicial = useQuery({ queryKey: ["cliente", clienteParam], queryFn: () => clientesService.obter(clienteParam!), enabled: !edicao && !!clienteParam });

  const [cliente, setCliente] = useState<ClienteCard | null>(orcamento?.cliente ?? null);
  const [responsavelId, setResponsavelId] = useState(orcamento?.responsavelId ?? "");
  const [validade, setValidade] = useState(orcamento?.validade ?? somarDias(hoje, 15));
  const [prazo, setPrazo] = useState(String(orcamento?.prazoEstimadoDiasUteis ?? 15));
  const [condicao, setCondicao] = useState(orcamento?.condicaoPagamento ?? "");
  const [observacoes, setObservacoes] = useState(orcamento?.observacoesComerciais ?? "");
  const [desconto, setDesconto] = useState(orcamento ? paraTextoDecimal(orcamento.resumo.desconto) : "");
  const [itens, setItens] = useState<ItemForm[]>(() => orcamento?.itens.map((i) => ({
    chave: novaChave(), id: i.id, produtoId: i.produtoId, descricao: i.descricao ?? "", quantidade: String(i.quantidade),
    valorUnitario: paraTextoDecimal(i.valorUnitario), valorPersonalizacao: paraTextoDecimal(i.valorPersonalizacaoUnitario),
    tipoPersonalizacao: i.personalizacao.tipoPersonalizacao, corPeca: i.personalizacao.corPeca, coresArte: i.personalizacao.coresArte ?? "",
    medidas: i.personalizacao.medidas ?? "", localAplicacao: i.personalizacao.localAplicacao ?? "", referenciaArte: i.personalizacao.referenciaArte ?? "",
    observacoesTecnicas: i.personalizacao.observacoesTecnicas ?? "",
    anexos: i.anexos.map((a) => ({ chave: novaChave(), anexo: a, nome: a.nome, tamanho: a.tamanhoBytes, progresso: 100 })),
  })) ?? []);
  const [erro, setErro] = useState<ErroApi | null>(null);
  const topo = useRef<HTMLDivElement>(null);

  useEffect(() => { if (!responsavelId && usuario) setResponsavelId(usuario.id); }, [usuario, responsavelId]);
  useEffect(() => {
    const c = clienteInicial.data;
    if (c && !cliente) setCliente({ id: c.id, nome: c.nomeExibicao, tipoPessoa: c.tipoPessoa, documento: c.documento, contatoNome: c.contatoNome, contatoCargo: c.contatoCargo,
      telefone: c.telefone, whatsApp: c.whatsApp, email: c.email, cidade: c.endereco.cidade ? `${c.endereco.cidade}/${c.endereco.uf ?? ""}` : null });
  }, [clienteInicial.data, cliente]);
  useEffect(() => { if (!condicao && opcoes.data) setCondicao(opcoes.data.condicoesPagamento[0] ?? ""); }, [opcoes.data, condicao]);

  const linhas = useMemo(() => itens.map((i) => ({ quantidade: paraNumero(i.quantidade), valorUnitario: paraNumero(i.valorUnitario), valorPersonalizacaoUnitario: paraNumero(i.valorPersonalizacao) })), [itens]);
  const chaveCalculo = useDebounce(JSON.stringify([linhas, paraNumero(desconto)]), 300);
  const calculo = useQuery({ queryKey: ["orcamento", "calculo", chaveCalculo], queryFn: () => orcamentosService.calcular(linhas, paraNumero(desconto)), placeholderData: (p) => p });

  const atualizarItem = (chave: string, patch: Partial<ItemForm>) => setItens((l) => l.map((i) => (i.chave === chave ? { ...i, ...patch } : i)));
  const removerItem = (chave: string) => setItens((l) => l.filter((i) => i.chave !== chave));

  async function enviarArquivos(chaveItem: string, arquivos: FileList | null) {
    if (!arquivos) return;
    const max = opcoes.data?.tamanhoMaximoBytes ?? 20 * 1024 * 1024;
    const permitidas = opcoes.data?.extensoesPermitidas ?? [".pdf", ".png", ".jpg", ".jpeg", ".ai", ".cdr"];
    for (const arquivo of Array.from(arquivos)) {
      const chave = novaChave();
      const ext = "." + (arquivo.name.split(".").pop() ?? "").toLowerCase();
      const base: AnexoForm = { chave, nome: arquivo.name, tamanho: arquivo.size, progresso: 0 };
      const erroLocal = !permitidas.includes(ext) ? `Formato não permitido. Use ${permitidas.join(", ").toUpperCase()}.` : arquivo.size > max ? "O arquivo excede 20 MB." : undefined;
      setItens((l) => l.map((i) => (i.chave === chaveItem ? { ...i, anexos: [...i.anexos, { ...base, erro: erroLocal }] } : i)));
      if (erroLocal) continue;
      const atualizar = (patch: Partial<AnexoForm>) => setItens((l) => l.map((i) => (i.chave === chaveItem ? { ...i, anexos: i.anexos.map((a) => (a.chave === chave ? { ...a, ...patch } : a)) } : i)));
      try {
        const anexo = await anexosService.enviar(arquivo, (p) => atualizar({ progresso: p }));
        atualizar({ anexo, progresso: 100 });
      } catch (e) { atualizar({ erro: erroDaApi(e).message }); }
    }
  }

  function removerAnexo(chaveItem: string, chaveAnexo: string) {
    const item = itens.find((i) => i.chave === chaveItem);
    const anexo = item?.anexos.find((a) => a.chave === chaveAnexo)?.anexo;
    const jaSalvo = !!orcamento?.itens.some((i) => i.anexos.some((a) => a.id === anexo?.id));
    if (anexo && !jaSalvo) anexosService.remover(anexo.id).catch(() => undefined);
    setItens((l) => l.map((i) => (i.chave === chaveItem ? { ...i, anexos: i.anexos.filter((a) => a.chave !== chaveAnexo) } : i)));
  }

  const enviando = itens.some((i) => i.anexos.some((a) => !a.anexo && !a.erro));
  const salvar = useMutation({
    mutationFn: () => {
      const corpo: OrcamentoRequisicao = {
        clienteId: cliente?.id ?? null, responsavelId: responsavelId || null, validade: validade || null, prazoEstimadoDiasUteis: paraNumero(prazo),
        condicaoPagamento: condicao, observacoesComerciais: observacoes, desconto: paraNumero(desconto) ?? 0, versao: orcamento?.versao,
        itens: itens.map((i) => ({ id: i.id ?? null, produtoId: i.produtoId || null, descricao: i.descricao, quantidade: paraNumero(i.quantidade),
          valorUnitario: paraNumero(i.valorUnitario), valorPersonalizacaoUnitario: paraNumero(i.valorPersonalizacao) ?? 0,
          tipoPersonalizacao: i.tipoPersonalizacao, corPeca: i.corPeca, coresArte: i.coresArte, medidas: i.medidas, localAplicacao: i.localAplicacao,
          referenciaArte: i.referenciaArte, observacoesTecnicas: i.observacoesTecnicas, anexoIds: i.anexos.filter((a) => a.anexo).map((a) => a.anexo!.id) })),
      };
      return edicao ? orcamentosService.atualizar(orcamento!.id, corpo) : orcamentosService.criar(corpo);
    },
    onSuccess: (o) => {
      qc.invalidateQueries({ queryKey: ["orcamentos"] });
      qc.invalidateQueries({ queryKey: ["orcamento", o.id] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast("sucesso", edicao ? `Orçamento ${numeroDocumento(o.numero)} atualizado.` : `Orçamento ${numeroDocumento(o.numero)} criado em Em Elaboração.`);
      router.push(`/orcamentos/${o.id}`);
    },
    onError: (e) => { setErro(erroDaApi(e)); topo.current?.scrollIntoView({ behavior: "smooth" }); },
  });

  const e = (c: string) => erroCampo(erro, c);
  const previsao = paraNumero(prazo);
  const c = calculo.data;

  return (
    <div ref={topo} className="grid gap-6 xl:grid-cols-[1fr_340px]">
      <div className="min-w-0 space-y-5">
        {erro && (erro.code === "ORCAMENTO_SEM_ITENS"
          ? <Aviso tipo="erro" titulo="Adicione pelo menos um item ao orçamento antes de salvar.">O orçamento precisa de ao menos um produto com quantidade e valor.</Aviso>
          : <Aviso tipo="erro" titulo={Object.keys(erro.errors).length ? "Corrija os campos destacados para salvar." : erro.message}>{Object.keys(erro.errors).length > 0 && erro.message !== "Corrija os campos destacados." ? erro.message : null}</Aviso>)}

        <Cartao titulo="Cliente">
          {edicao ? <CartaoCliente cliente={orcamento!.cliente} /> : <SeletorCliente cliente={cliente} aoSelecionar={setCliente} erro={e("clienteId")} />}
          {!edicao && !cliente && <p className="mt-2 text-xs text-texto-fraco">O orçamento só pode ser criado para um cliente já cadastrado.</p>}
        </Cartao>

        <Cartao titulo="Dados do orçamento">
          <div className="grid gap-4 md:grid-cols-3">
            <Campo rotulo="Responsável" obrigatorio erro={e("responsavelId")}>
              <Selecao value={responsavelId} onChange={(ev) => setResponsavelId(ev.target.value)} invalido={!!e("responsavelId")}>
                <option value="">Selecione</option>{responsaveis.data?.map((u) => <option key={u.id} value={u.id}>{u.nome}</option>)}
              </Selecao>
            </Campo>
            <Campo rotulo="Data do orçamento"><Entrada className="numero" value={data(orcamento?.dataOrcamento ?? hoje)} disabled /></Campo>
            <Campo rotulo="Validade" obrigatorio erro={e("validade")}><Entrada type="date" className="numero" value={validade} min={orcamento?.dataOrcamento ?? hoje} onChange={(ev) => setValidade(ev.target.value)} invalido={!!e("validade")} /></Campo>
            <Campo rotulo="Prazo estimado (dias úteis)" obrigatorio erro={e("prazoEstimadoDiasUteis")} ajuda={previsao ? `Contados a partir da aprovação` : undefined}>
              <Entrada className="numero" inputMode="numeric" value={prazo} onChange={(ev) => setPrazo(ev.target.value.replace(/\D/g, "").slice(0, 3))} invalido={!!e("prazoEstimadoDiasUteis")} />
            </Campo>
            <Campo rotulo="Condição de pagamento" obrigatorio erro={e("condicaoPagamento")} className="md:col-span-2">
              <Selecao value={condicao} onChange={(ev) => setCondicao(ev.target.value)} invalido={!!e("condicaoPagamento")}>
                <option value="">Selecione</option>
                {[...new Set([...(opcoes.data?.condicoesPagamento ?? []), ...(condicao ? [condicao] : [])])].map((o) => <option key={o}>{o}</option>)}
              </Selecao>
            </Campo>
            <Campo rotulo="Observações comerciais" erro={e("observacoesComerciais")} className="md:col-span-3">
              <AreaTexto value={observacoes} onChange={(ev) => setObservacoes(ev.target.value)} maxLength={2000} placeholder="Informações que acompanham a proposta: evento, prazos combinados, frete..." />
            </Campo>
          </div>
        </Cartao>

        <Cartao titulo={`Itens do orçamento${itens.length ? ` (${itens.length})` : ""}`} semPadding
          acao={itens.length > 0 && <Botao variante="secundaria" tamanho="sm" icone={<Plus className="h-4 w-4" />} onClick={() => setItens((l) => [...l, itemVazio()])}>Adicionar item</Botao>}>
          {itens.length === 0
            ? <EstadoVazio icone={<PackagePlus className="h-5 w-5" />} titulo="Nenhum item adicionado"
                descricao={<span className={cn(erro?.code === "ORCAMENTO_SEM_ITENS" && "font-medium text-red-600")}>Adicione pelo menos um produto com quantidade, valores e especificação de personalização.</span>}
                acao={<Botao icone={<Plus className="h-4 w-4" />} onClick={() => setItens([itemVazio()])}>Adicionar item</Botao>} />
            : <div className="divide-y divide-borda">{itens.map((item, idx) => {
              const p = `itens[${idx}].`;
              const totalItem = c?.itens[idx]?.valorTotal;
              return (
                <div key={item.chave} className="p-5">
                  <div className="mb-4 flex items-center justify-between">
                    <p className="text-sm font-semibold">Item {idx + 1}{totalItem !== undefined && <span className="numero ml-2 font-normal text-texto-fraco">{moeda(totalItem)}</span>}</p>
                    <Botao variante="fantasma" tamanho="sm" icone={<Trash2 className="h-4 w-4" />} onClick={() => removerItem(item.chave)}>Remover</Botao>
                  </div>
                  {e(p + "id") && <Aviso tipo="erro" className="mb-3">{e(p + "id")}</Aviso>}
                  <div className="grid gap-4 md:grid-cols-4">
                    <Campo rotulo="Produto" obrigatorio erro={e(p + "produtoId")} className="md:col-span-2">
                      <Selecao value={item.produtoId} invalido={!!e(p + "produtoId")} onChange={(ev) => {
                        const prod = produtos.data?.find((x) => x.id === ev.target.value);
                        atualizarItem(item.chave, { produtoId: ev.target.value, descricao: item.descricao || prod?.descricaoBase || "" });
                      }}>
                        <option value="">Selecione o produto</option>{produtos.data?.map((x) => <option key={x.id} value={x.id}>{x.nome}</option>)}
                      </Selecao>
                    </Campo>
                    <Campo rotulo="Quantidade" obrigatorio erro={e(p + "quantidade")}>
                      <Entrada className="numero" inputMode="numeric" value={item.quantidade} invalido={!!e(p + "quantidade")} onChange={(ev) => atualizarItem(item.chave, { quantidade: ev.target.value.replace(/\D/g, "").slice(0, 7) })} />
                    </Campo>
                    <div className="hidden md:block" />
                    <Campo rotulo="Valor unitário (R$)" obrigatorio erro={e(p + "valorUnitario")}>
                      <Entrada className="numero" inputMode="decimal" placeholder="0,00" value={item.valorUnitario} invalido={!!e(p + "valorUnitario")}
                        onChange={(ev) => atualizarItem(item.chave, { valorUnitario: ev.target.value })} onBlur={() => atualizarItem(item.chave, { valorUnitario: paraTextoDecimal(paraNumero(item.valorUnitario)) })} />
                    </Campo>
                    <Campo rotulo="Personalização por unidade (R$)" erro={e(p + "valorPersonalizacaoUnitario")}>
                      <Entrada className="numero" inputMode="decimal" placeholder="0,00" value={item.valorPersonalizacao}
                        onChange={(ev) => atualizarItem(item.chave, { valorPersonalizacao: ev.target.value })} onBlur={() => atualizarItem(item.chave, { valorPersonalizacao: paraTextoDecimal(paraNumero(item.valorPersonalizacao)) })} />
                    </Campo>
                    <Campo rotulo="Descrição" erro={e(p + "descricao")} className="md:col-span-2">
                      <Entrada value={item.descricao} maxLength={500} onChange={(ev) => atualizarItem(item.chave, { descricao: ev.target.value })} />
                    </Campo>
                  </div>
                  <div className="mt-5 rounded-lg border border-borda bg-slate-50/60 p-4">
                    <p className="mb-3 text-[13px] font-semibold text-texto-suave">Especificação da personalização</p>
                    <div className="grid gap-4 md:grid-cols-3">
                      <Campo rotulo="Tipo de personalização" obrigatorio erro={e(p + "tipoPersonalizacao")}>
                        <Selecao value={item.tipoPersonalizacao} invalido={!!e(p + "tipoPersonalizacao")} onChange={(ev) => atualizarItem(item.chave, { tipoPersonalizacao: ev.target.value })}>
                          <option value="">Selecione</option>
                          {[...new Set([...(opcoes.data?.tiposPersonalizacao ?? []), ...(item.tipoPersonalizacao ? [item.tipoPersonalizacao] : [])])].map((t) => <option key={t}>{t}</option>)}
                        </Selecao>
                      </Campo>
                      <Campo rotulo="Cor da peça" obrigatorio erro={e(p + "corPeca")}><Entrada value={item.corPeca} maxLength={50} invalido={!!e(p + "corPeca")} onChange={(ev) => atualizarItem(item.chave, { corPeca: ev.target.value })} /></Campo>
                      <Campo rotulo="Cores da arte" erro={e(p + "coresArte")}><Entrada value={item.coresArte} maxLength={100} placeholder="Ex.: 2 cores (branco e laranja)" onChange={(ev) => atualizarItem(item.chave, { coresArte: ev.target.value })} /></Campo>
                      <Campo rotulo="Medidas" erro={e(p + "medidas")}><Entrada value={item.medidas} maxLength={100} placeholder="Ex.: 9 × 5 cm" onChange={(ev) => atualizarItem(item.chave, { medidas: ev.target.value })} /></Campo>
                      <Campo rotulo="Local de aplicação" erro={e(p + "localAplicacao")}><Entrada value={item.localAplicacao} maxLength={100} placeholder="Ex.: peito esquerdo" onChange={(ev) => atualizarItem(item.chave, { localAplicacao: ev.target.value })} /></Campo>
                      <Campo rotulo="Referência da arte" erro={e(p + "referenciaArte")}><Entrada value={item.referenciaArte} maxLength={500} onChange={(ev) => atualizarItem(item.chave, { referenciaArte: ev.target.value })} /></Campo>
                      <Campo rotulo="Observações técnicas" erro={e(p + "observacoesTecnicas")} className="md:col-span-3">
                        <AreaTexto value={item.observacoesTecnicas} maxLength={2000} className="min-h-[60px]" placeholder="Grade de tamanhos, acabamento, cuidados de produção..." onChange={(ev) => atualizarItem(item.chave, { observacoesTecnicas: ev.target.value })} />
                      </Campo>
                    </div>
                    <div className="mt-4">
                      <label className={cn("flex cursor-pointer items-center justify-center gap-2 rounded-lg border border-dashed border-borda-forte bg-white px-4 py-3 text-sm text-texto-suave hover:border-primaria hover:text-primaria")}>
                        <Paperclip className="h-4 w-4" />Anexar arte ou referência<span className="text-xs text-texto-fraco">PDF, PNG, JPG, AI ou CDR · até 20 MB</span>
                        <input type="file" multiple className="sr-only" accept={(opcoes.data?.extensoesPermitidas ?? [".pdf", ".png", ".jpg", ".jpeg", ".ai", ".cdr"]).join(",")}
                          onChange={(ev) => { enviarArquivos(item.chave, ev.target.files); ev.target.value = ""; }} />
                      </label>
                      {e(p + "anexos") && <p className="mt-1 text-xs text-red-600">{e(p + "anexos")}</p>}
                      <ListaAnexos anexos={item.anexos} aoRemover={(k) => removerAnexo(item.chave, k)} />
                    </div>
                  </div>
                </div>);
            })}</div>}
        </Cartao>
      </div>

      <aside className="xl:sticky xl:top-6 xl:self-start">
        <Cartao titulo="Resumo">
          <dl className="space-y-2.5 text-sm">
            <div className="flex justify-between"><dt className="text-texto-fraco">Itens</dt><dd className="numero">{c ? `${c.quantidadeItens} · ${c.totalUnidades} un.` : "—"}</dd></div>
            <div className="flex justify-between"><dt className="text-texto-fraco">Subtotal dos produtos</dt><dd className="numero">{moeda(c?.subtotalProdutos)}</dd></div>
            <div className="flex justify-between"><dt className="text-texto-fraco">Personalização</dt><dd className="numero">{moeda(c?.valorPersonalizacao)}</dd></div>
            <div className="flex items-center justify-between gap-3"><dt className="text-texto-fraco">Desconto (R$)</dt>
              <dd><Entrada className="numero h-8 w-28 text-right" inputMode="decimal" placeholder="0,00" value={desconto} invalido={!!e("desconto") || c?.descontoExcedeValor}
                onChange={(ev) => setDesconto(ev.target.value)} onBlur={() => setDesconto(paraTextoDecimal(paraNumero(desconto)))} aria-label="Desconto" /></dd></div>
            {(e("desconto") || c?.descontoExcedeValor) && <p className="text-xs text-red-600">{e("desconto") ?? "O desconto não pode ser maior que o valor dos itens."}</p>}
            <div className="flex items-baseline justify-between border-t border-borda pt-3"><dt className="font-medium">Total</dt>
              <dd className="numero text-xl font-semibold">{calculo.isFetching && !c ? <Loader2 className="h-4 w-4 animate-spin" /> : moeda(c?.valorTotal)}</dd></div>
          </dl>
          <p className="mt-3 text-xs text-texto-fraco">Valores calculados pelo sistema. {previsao ? `Entrega em ${previsao} dias úteis após a aprovação.` : ""}</p>
          <div className="mt-5 flex flex-col gap-2">
            <Botao onClick={() => salvar.mutate()} carregando={salvar.isPending} disabled={enviando}>{enviando ? "Aguardando envio dos arquivos..." : edicao ? "Salvar alterações" : "Salvar orçamento"}</Botao>
            <Botao variante="secundaria" onClick={() => router.back()}>Cancelar</Botao>
          </div>
          {!edicao && <p className="mt-3 text-center text-xs text-texto-fraco">O orçamento será criado com status <b>Em Elaboração</b>.</p>}
        </Cartao>
      </aside>
    </div>
  );
}
