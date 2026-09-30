"use client";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, Ban, CheckCircle2, FileImage, FileText, Gavel, MessageSquarePlus, Package, Pencil, Plus, Send } from "lucide-react";
import { Abas, AreaTexto, Aviso, BadgeOrcamento, Botao, CabecalhoPagina, Campo, Cartao, Carregando, Entrada, ErroCarregamento, Info2, Modal, Selecao } from "@/components/ui";
import { useToast } from "@/components/ui/toast";
import { CartaoCliente, IconeMeio, LinhaDoTempo, Voltar } from "@/features/comum";
import { anexosService, orcamentosService } from "@/services";
import { erroCampo, erroDaApi } from "@/lib/api";
import { cn } from "@/lib/utils";
import { meiosContato, rotuloMeio, rotuloResultado, rotuloSituacaoOrcamento } from "@/lib/status";
import { agoraLocalInput, data, dataDeInstante, dataHora, diasEntre, hojeIso, localInputParaIso, moeda, numeroDocumento, relativo, tamanhoArquivo } from "@/utils/formatos";
import type { ErroApi, MeioContato, OrcamentoDetalhe, PedidoVinculado, ResultadoDecisao } from "@/types/api";

function Etapas({ o }: { o: OrcamentoDetalhe }) {
  const decidido = !["EmElaboracao", "AguardandoRetorno"].includes(o.situacao);
  const etapas = [
    { rotulo: "Em Elaboração", feito: true, atual: o.situacao === "EmElaboracao" },
    { rotulo: "Aguardando Retorno", feito: o.situacao !== "EmElaboracao" && o.aguardandoRetornoDesde !== null, atual: o.situacao === "AguardandoRetorno" },
    { rotulo: decidido ? rotuloSituacaoOrcamento[o.situacao] : "Decisão do cliente", feito: decidido, atual: decidido && o.situacao !== "Aprovado", falha: ["Recusado", "Expirado", "Cancelado"].includes(o.situacao) },
    { rotulo: o.pedido ? `Pedido ${numeroDocumento(o.pedido.numero)}` : "Pedido", feito: !!o.pedido, atual: !!o.pedido },
  ];
  return (
    <ol className="flex flex-wrap items-center gap-y-2 text-[13px]" aria-label="Etapas do orçamento">
      {etapas.map((e, i) => (
        <li key={i} className="flex items-center">
          <span className={cn("flex items-center gap-2 rounded-full px-3 py-1",
            e.falha ? "bg-red-50 text-red-700" : e.atual ? "bg-primaria text-white" : e.feito ? "bg-primaria-clara text-primaria" : "bg-slate-100 text-texto-fraco")}>
            <span className={cn("numero flex h-4 w-4 items-center justify-center rounded-full text-[10px]", e.atual && !e.falha ? "bg-white/25" : "bg-white")}>{i + 1}</span>{e.rotulo}
          </span>
          {i < etapas.length - 1 && <span className={cn("mx-1.5 h-px w-6", e.feito ? "bg-primaria-borda" : "bg-borda")} />}
        </li>))}
    </ol>
  );
}

function Faixa({ o }: { o: OrcamentoDetalhe }) {
  const d = o.decisao;
  const vence = diasEntre(hojeIso(), o.validade);
  switch (o.situacao) {
    case "EmElaboracao": return <Aviso tipo="info" titulo="Orçamento em elaboração.">Ao enviar a proposta ao cliente (WhatsApp, e-mail, ligação ou presencialmente), use “Apresentar ao cliente” para passar a Aguardando Retorno.</Aviso>;
    case "AguardandoRetorno": return <Aviso tipo="alerta" titulo={`Aguardando retorno do cliente ${o.aguardandoRetornoDesde ? `desde ${dataDeInstante(o.aguardandoRetornoDesde)} (${relativo(o.aguardandoRetornoDesde)})` : ""}`}>
      Válido até {data(o.validade)} — {vence < 0 ? "vencido" : vence === 0 ? "vence hoje" : vence === 1 ? "vence amanhã" : `vence em ${vence} dias`}. Registre os contatos feitos e, quando o cliente responder, a decisão.</Aviso>;
    case "Aprovado": return <Aviso tipo="sucesso" titulo={`Aprovado em ${d ? dataHora(d.dataHora) : ""}${d?.meioContato ? ` via ${rotuloMeio[d.meioContato]}` : ""}`}
      acao={o.pedido && <Link href={`/pedidos/${o.pedido.id}`}><Botao tamanho="sm" variante="sucesso" icone={<Package className="h-4 w-4" />}>Ver pedido {numeroDocumento(o.pedido.numero)}</Botao></Link>}>
      Pedido {o.pedido && numeroDocumento(o.pedido.numero)} gerado automaticamente com os itens, personalização, artes e valores deste orçamento. Decisão registrada por {d?.registradoPor}.</Aviso>;
    case "Recusado": return <Aviso tipo="erro" titulo={`Recusado pelo cliente em ${d ? dataHora(d.dataHora) : ""}${d?.meioContato ? ` via ${rotuloMeio[d.meioContato]}` : ""}`}>
      {d?.motivo && <>Motivo: {d.motivo}. </>}{d?.observacao}{" "}Nenhum pedido foi gerado.</Aviso>;
    case "Expirado": return <Aviso tipo="info" className="border-slate-200 bg-slate-50 text-slate-800" titulo={`Expirado automaticamente em ${d ? dataDeInstante(d.dataHora) : ""}`}>
      A validade terminou em {data(o.validade)} sem decisão do cliente. Para retomar a negociação, crie um novo orçamento com condições atualizadas.</Aviso>;
    case "Cancelado": return <Aviso tipo="erro" className="bg-white" titulo={`Cancelado em ${d ? dataHora(d.dataHora) : ""}`}>{d?.motivo && <>Motivo: {d.motivo}. </>}{d?.observacao} Nenhum pedido foi gerado.</Aviso>;
  }
}

function SeletorMeio({ valor, aoMudar, erro }: { valor: MeioContato | null; aoMudar: (m: MeioContato) => void; erro?: string }) {
  return (
    <div>
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-5" role="radiogroup">
        {meiosContato.map((m) => (
          <button key={m} type="button" role="radio" aria-checked={valor === m} onClick={() => aoMudar(m)}
            className={cn("flex items-center justify-center gap-1.5 rounded-md border px-2 py-2 text-[13px] font-medium transition-colors",
              valor === m ? "border-primaria bg-primaria-clara text-primaria" : erro ? "border-red-300" : "border-borda hover:bg-slate-50")}>
            <IconeMeio meio={m} />{rotuloMeio[m]}
          </button>))}
      </div>
      {erro && <p className="mt-1 text-xs text-red-600">{erro}</p>}
    </div>
  );
}

function ModalContato({ o, aberto, aoFechar }: { o: OrcamentoDetalhe; aberto: boolean; aoFechar: () => void }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [tipo, setTipo] = useState<MeioContato | null>(null);
  const [quando, setQuando] = useState(agoraLocalInput());
  const [obs, setObs] = useState("");
  const [erro, setErro] = useState<ErroApi | null>(null);
  const m = useMutation({
    mutationFn: () => orcamentosService.registrarContato(o.id, { tipo, dataHora: localInputParaIso(quando), observacao: obs }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["orcamento", o.id] }); toast("sucesso", "Contato registrado. O status do orçamento não foi alterado."); setTipo(null); setObs(""); setErro(null); aoFechar(); },
    onError: (e) => setErro(erroDaApi(e)),
  });
  return (
    <Modal aberto={aberto} aoFechar={aoFechar} titulo="Registrar contato" descricao={`Orçamento ${numeroDocumento(o.numero)} · ${o.cliente.nome}`}
      rodape={<><Botao variante="secundaria" onClick={aoFechar}>Cancelar</Botao><Botao carregando={m.isPending} onClick={() => m.mutate()}>Registrar contato</Botao></>}>
      <div className="space-y-4">
        {erro && !Object.keys(erro.errors).length && <Aviso tipo="erro">{erro.message}</Aviso>}
        <Campo rotulo="Tipo de contato" obrigatorio><SeletorMeio valor={tipo} aoMudar={setTipo} erro={erroCampo(erro, "tipo")} /></Campo>
        <Campo rotulo="Data e hora do contato" obrigatorio erro={erroCampo(erro, "dataHora")}><Entrada type="datetime-local" className="numero" value={quando} max={agoraLocalInput()} onChange={(e) => setQuando(e.target.value)} invalido={!!erroCampo(erro, "dataHora")} /></Campo>
        <Campo rotulo="O que foi conversado" obrigatorio erro={erroCampo(erro, "observacao")}><AreaTexto value={obs} maxLength={2000} onChange={(e) => setObs(e.target.value)} invalido={!!erroCampo(erro, "observacao")} placeholder="Ex.: cliente pediu para trocar a cor dos bonés para preto." /></Campo>
        <p className="text-xs text-texto-fraco">Registrar um contato não altera o status do orçamento.</p>
      </div>
    </Modal>
  );
}

function ModalApresentar({ o, aberto, aoFechar }: { o: OrcamentoDetalhe; aberto: boolean; aoFechar: () => void }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [meio, setMeio] = useState<MeioContato | null>(null);
  const [erro, setErro] = useState<ErroApi | null>(null);
  const m = useMutation({
    mutationFn: () => orcamentosService.marcarAguardando(o.id, meio, o.versao),
    onSuccess: (d) => { qc.setQueryData(["orcamento", o.id], d); qc.invalidateQueries({ queryKey: ["orcamento", o.id] }); qc.invalidateQueries({ queryKey: ["orcamentos"] }); toast("sucesso", "Orçamento aguardando retorno do cliente."); aoFechar(); },
    onError: (e) => setErro(erroDaApi(e)),
  });
  return (
    <Modal aberto={aberto} aoFechar={aoFechar} titulo="Apresentar proposta ao cliente" descricao="O orçamento passará para Aguardando Retorno."
      rodape={<><Botao variante="secundaria" onClick={aoFechar}>Cancelar</Botao><Botao carregando={m.isPending} onClick={() => m.mutate()} icone={<Send className="h-4 w-4" />}>Marcar como apresentado</Botao></>}>
      <div className="space-y-4">
        {erro && <Aviso tipo="erro">{erro.message}</Aviso>}
        <Campo rotulo="Como a proposta foi enviada? (opcional)"><SeletorMeio valor={meio} aoMudar={setMeio} /></Campo>
        <p className="text-xs text-texto-fraco">O envio acontece fora do sistema. Esta ação apenas registra que o cliente recebeu a proposta, válida até {data(o.validade)}.</p>
      </div>
    </Modal>
  );
}

function ModalDecisao({ o, aberto, aoFechar, somenteCancelar, aoAprovado }: { o: OrcamentoDetalhe; aberto: boolean; aoFechar: () => void; somenteCancelar?: boolean; aoAprovado: (p: PedidoVinculado) => void }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [resultado, setResultado] = useState<ResultadoDecisao | null>(somenteCancelar ? "Cancelado" : null);
  const [meio, setMeio] = useState<MeioContato | null>(null);
  const [quando, setQuando] = useState(agoraLocalInput());
  const [motivo, setMotivo] = useState("");
  const [obs, setObs] = useState("");
  const [erro, setErro] = useState<ErroApi | null>(null);
  const [confirmar, setConfirmar] = useState(false);
  const [falhaAprovacao, setFalhaAprovacao] = useState<ErroApi | null>(null);
  const motivos = useQuery({ queryKey: ["motivos-decisao"], queryFn: orcamentosService.motivos, staleTime: Infinity });

  const m = useMutation({
    mutationFn: () => orcamentosService.registrarDecisao(o.id, { resultado, meioContato: meio, dataHora: localInputParaIso(quando), motivo, observacao: obs, versao: o.versao }),
    onSuccess: (r) => {
      qc.setQueryData(["orcamento", o.id], r.orcamento);
      for (const k of [["orcamento", o.id], ["orcamentos"], ["pedidos"], ["dashboard"], ["cliente", o.cliente.id]]) qc.invalidateQueries({ queryKey: k });
      setConfirmar(false);
      aoFechar();
      if (r.pedidoGerado) aoAprovado(r.pedidoGerado);
      else toast("sucesso", `Decisão registrada: ${rotuloResultado[r.orcamento.decisao?.resultado ?? "Recusado"]}.`);
    },
    onError: (e) => {
      const api = erroDaApi(e);
      if (confirmar) { setConfirmar(false); if (!Object.keys(api.errors).length) { setFalhaAprovacao(api); return; } }
      setErro(api);
    },
  });

  function avancar() {
    if (resultado === "Aprovado") {
      const faltando: Record<string, string[]> = {};
      if (!meio) faltando.meioContato = ["Informe o meio de contato utilizado pelo cliente."];
      if (!quando) faltando.dataHora = ["Informe a data e a hora da decisão."];
      if (Object.keys(faltando).length) { setErro({ status: 400, code: "DECISAO_DADOS_OBRIGATORIOS", message: "Toda decisão precisa de meio de contato e data/hora.", errors: faltando }); return; }
      setErro(null);
      setConfirmar(true);
    } else m.mutate();
  }

  const opcoesResultado: ResultadoDecisao[] = somenteCancelar ? ["Cancelado"] : ["Aprovado", "Recusado", "Cancelado"];
  return (
    <>
      <Modal aberto={aberto && !confirmar} aoFechar={aoFechar} titulo={somenteCancelar ? "Cancelar orçamento" : "Registrar decisão do cliente"} largura="max-w-xl"
        descricao={`Orçamento ${numeroDocumento(o.numero)} · ${o.cliente.nome} · ${moeda(o.resumo.valorTotal)}`}
        rodape={<><Botao variante="secundaria" onClick={aoFechar}>Voltar</Botao>
          <Botao variante={resultado === "Aprovado" ? "sucesso" : resultado ? "perigo" : "primaria"} disabled={!resultado} carregando={m.isPending} onClick={avancar}>
            {resultado === "Aprovado" ? "Continuar" : resultado === "Recusado" ? "Registrar recusa" : resultado === "Cancelado" ? "Cancelar orçamento" : "Registrar decisão"}</Botao></>}>
        <div className="space-y-4">
          {erro && <Aviso tipo="erro" titulo={erro.message} />}
          {!somenteCancelar && <Campo rotulo="Resultado da negociação" obrigatorio erro={erroCampo(erro, "resultado")}>
            <div className="grid grid-cols-3 gap-2" role="radiogroup">
              {opcoesResultado.map((r) => (
                <button key={r} type="button" role="radio" aria-checked={resultado === r} onClick={() => setResultado(r)}
                  className={cn("flex flex-col items-center gap-1 rounded-lg border px-3 py-3 text-sm font-medium transition-colors",
                    resultado === r ? (r === "Aprovado" ? "border-emerald-500 bg-emerald-50 text-emerald-800" : "border-red-400 bg-red-50 text-red-800") : "border-borda hover:bg-slate-50")}>
                  {r === "Aprovado" ? <CheckCircle2 className="h-5 w-5" /> : <Ban className="h-5 w-5" />}{rotuloResultado[r]}
                </button>))}
            </div>
          </Campo>}
          <Campo rotulo="Meio de contato utilizado pelo cliente" obrigatorio><SeletorMeio valor={meio} aoMudar={setMeio} erro={erroCampo(erro, "meioContato")} /></Campo>
          <Campo rotulo="Data e hora da decisão" obrigatorio erro={erroCampo(erro, "dataHora")}><Entrada type="datetime-local" className="numero" value={quando} max={agoraLocalInput()} onChange={(e) => setQuando(e.target.value)} invalido={!!erroCampo(erro, "dataHora")} /></Campo>
          {(resultado === "Recusado" || resultado === "Cancelado") && <Campo rotulo="Motivo" erro={erroCampo(erro, "motivo")}>
            <Selecao value={motivo} onChange={(e) => setMotivo(e.target.value)}><option value="">Selecione (opcional)</option>{motivos.data?.map((x) => <option key={x}>{x}</option>)}</Selecao>
          </Campo>}
          <Campo rotulo="Observação" erro={erroCampo(erro, "observacao")}><AreaTexto value={obs} maxLength={2000} onChange={(e) => setObs(e.target.value)} placeholder="Ex.: aprovado pelo Rafael por WhatsApp, pediu entrega na unidade Centro." /></Campo>
          {resultado === "Aprovado" && <Aviso tipo="info">Ao confirmar, o pedido será gerado automaticamente a partir deste orçamento.</Aviso>}
        </div>
      </Modal>

      <Modal aberto={confirmar} aoFechar={() => setConfirmar(false)} titulo="Aprovar orçamento e gerar pedido" largura="max-w-lg"
        rodape={<><Botao variante="secundaria" onClick={() => setConfirmar(false)} disabled={m.isPending}>Voltar</Botao><Botao variante="sucesso" carregando={m.isPending} onClick={() => m.mutate()} icone={<CheckCircle2 className="h-4 w-4" />}>Aprovar e gerar pedido</Botao></>}>
        <div className="space-y-4 text-sm">
          <p>Confira os dados. Esta ação não pode ser desfeita: o orçamento ficará <b>Aprovado</b> e não poderá mais ser editado.</p>
          <dl className="grid grid-cols-2 gap-3 rounded-lg border border-borda bg-slate-50/60 p-4">
            <Info2 rotulo="Cliente">{o.cliente.nome}</Info2><Info2 rotulo="Valor total"><span className="numero font-semibold">{moeda(o.resumo.valorTotal)}</span></Info2>
            <Info2 rotulo="Itens">{o.resumo.quantidadeItens} · {o.resumo.totalUnidades} unidades</Info2><Info2 rotulo="Prazo">{o.prazoEstimadoDiasUteis} dias úteis</Info2>
            <Info2 rotulo="Aprovado via">{meio ? rotuloMeio[meio] : "—"}</Info2><Info2 rotulo="Em">{quando.replace("T", " ").split(" ").map((p, i) => i === 0 ? data(p) : p).join(" ")}</Info2>
          </dl>
          <p className="text-xs text-texto-fraco">O pedido herdará cliente, itens, personalização, artes anexadas, valores, prazo, condição de pagamento e observações.</p>
        </div>
      </Modal>

      <Modal aberto={!!falhaAprovacao} aoFechar={() => setFalhaAprovacao(null)} titulo="Não foi possível aprovar o orçamento"
        rodape={<><Botao variante="secundaria" onClick={() => setFalhaAprovacao(null)}>Fechar</Botao><Botao onClick={() => { setFalhaAprovacao(null); setConfirmar(true); }}>Tentar novamente</Botao></>}>
        <div className="flex gap-3 text-sm"><AlertTriangle className="h-5 w-5 shrink-0 text-red-600" />
          <div><p className="font-medium">{falhaAprovacao?.message}</p><p className="mt-1 text-texto-suave">Nenhuma alteração foi salva: o orçamento continua Aguardando Retorno e nenhum pedido foi criado.</p>
            {falhaAprovacao?.traceId && <p className="numero mt-2 text-xs text-texto-fraco">Código para o suporte: {falhaAprovacao.traceId}</p>}</div></div>
      </Modal>
    </>
  );
}

export function DetalheOrcamento() {
  const { id } = useParams<{ id: string }>();
  const q = useQuery({ queryKey: ["orcamento", id], queryFn: () => orcamentosService.obter(id) });
  const [aba, setAba] = useState<"" | "alteracoes" | "contatos">("");
  const historico = useQuery({ queryKey: ["orcamento", id, "historico", aba], queryFn: () => orcamentosService.historico(id, aba || undefined), enabled: !!q.data });
  const [modal, setModal] = useState<"contato" | "apresentar" | "decisao" | "cancelar" | null>(null);
  const [pedidoGerado, setPedidoGerado] = useState<PedidoVinculado | null>(null);

  if (q.isError) return <ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} />;
  if (!q.data) return <Carregando />;
  const o = q.data;
  const aberto = o.podeSerEditado;

  return (
    <>
      <CabecalhoPagina voltar={<Voltar href="/orcamentos">Orçamentos</Voltar>}
        titulo={<span className="flex flex-wrap items-center gap-3">Orçamento <span className="numero">{numeroDocumento(o.numero)}</span><BadgeOrcamento situacao={o.situacao} /></span>}
        subtitulo={<>{o.cliente.nome} · criado em {dataDeInstante(o.criadoEm)} · responsável {o.responsavel}{o.dataUltimaEdicao && ` · editado em ${dataHora(o.dataUltimaEdicao)}`}</>}
        acoes={<>
          {aberto && <Link href={`/orcamentos/${o.id}/editar`}><Botao variante="secundaria" icone={<Pencil className="h-4 w-4" />}>Editar</Botao></Link>}
          {aberto && <Botao variante="secundaria" icone={<MessageSquarePlus className="h-4 w-4" />} onClick={() => setModal("contato")}>Registrar contato</Botao>}
          {o.situacao === "EmElaboracao" && <Botao variante="fantasma" className="text-red-700" onClick={() => setModal("cancelar")}>Cancelar orçamento</Botao>}
          {o.situacao === "EmElaboracao" && <Botao icone={<Send className="h-4 w-4" />} onClick={() => setModal("apresentar")}>Apresentar ao cliente</Botao>}
          {o.situacao === "AguardandoRetorno" && <Botao icone={<Gavel className="h-4 w-4" />} onClick={() => setModal("decisao")}>Registrar decisão</Botao>}
          {o.pedido && <Link href={`/pedidos/${o.pedido.id}`}><Botao icone={<Package className="h-4 w-4" />}>Ver pedido {numeroDocumento(o.pedido.numero)}</Botao></Link>}
          {["Recusado", "Expirado", "Cancelado"].includes(o.situacao) && <Link href={`/orcamentos/novo?clienteId=${o.cliente.id}`}><Botao icone={<Plus className="h-4 w-4" />}>Novo orçamento para o cliente</Botao></Link>}
        </>} />

      <div className="mb-5 space-y-4"><Etapas o={o} /><Faixa o={o} /></div>

      <div className="grid gap-6 xl:grid-cols-[1fr_340px]">
        <div className="min-w-0 space-y-5">
          <Cartao titulo={`Itens (${o.itens.length})`} semPadding>
            <ul className="divide-y divide-borda">{o.itens.map((i) => (
              <li key={i.id} className="p-5">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div><p className="font-medium">{i.ordem}. {i.produtoNome}</p>{i.descricao && <p className="mt-0.5 text-[13px] text-texto-fraco">{i.descricao}</p>}</div>
                  <div className="text-right"><p className="numero font-semibold">{moeda(i.valorTotal)}</p>
                    <p className="numero text-xs text-texto-fraco">{i.quantidade} × ({moeda(i.valorUnitario)} + {moeda(i.valorPersonalizacaoUnitario)})</p></div>
                </div>
                <dl className="mt-3 grid grid-cols-2 gap-x-4 gap-y-2 rounded-lg bg-slate-50/80 p-3 text-[13px] md:grid-cols-3">
                  <Info2 rotulo="Personalização">{i.personalizacao.tipoPersonalizacao}</Info2><Info2 rotulo="Cor da peça">{i.personalizacao.corPeca}</Info2>
                  {i.personalizacao.coresArte && <Info2 rotulo="Cores da arte">{i.personalizacao.coresArte}</Info2>}
                  {i.personalizacao.medidas && <Info2 rotulo="Medidas">{i.personalizacao.medidas}</Info2>}
                  {i.personalizacao.localAplicacao && <Info2 rotulo="Local de aplicação">{i.personalizacao.localAplicacao}</Info2>}
                  {i.personalizacao.referenciaArte && <Info2 rotulo="Referência da arte">{i.personalizacao.referenciaArte}</Info2>}
                  {i.personalizacao.observacoesTecnicas && <Info2 rotulo="Observações técnicas" className="col-span-2 md:col-span-3">{i.personalizacao.observacoesTecnicas}</Info2>}
                </dl>
                {i.anexos.length > 0 && <div className="mt-3 flex flex-wrap gap-2">{i.anexos.map((a) => (
                  <a key={a.id} href={anexosService.url(a.id, true)} target="_blank" rel="noreferrer" className="inline-flex items-center gap-2 rounded-md border border-borda bg-white px-2.5 py-1.5 text-[13px] hover:border-primaria">
                    {a.ehImagem ? <FileImage className="h-4 w-4 text-texto-fraco" /> : <FileText className="h-4 w-4 text-texto-fraco" />}{a.nome}<span className="numero text-xs text-texto-fraco">{tamanhoArquivo(a.tamanhoBytes)}</span>
                  </a>))}</div>}
              </li>))}</ul>
          </Cartao>

          <Cartao titulo="Histórico do orçamento">
            <div className="-mt-2 mb-4"><Abas ativa={aba} aoMudar={setAba} abas={[{ id: "", rotulo: "Tudo" }, { id: "alteracoes", rotulo: "Alterações e decisões" }, { id: "contatos", rotulo: "Contatos" }]} /></div>
            {!historico.data ? <Carregando /> : <LinhaDoTempo eventos={historico.data} vazio={aba === "contatos" ? "Nenhum contato registrado ainda." : undefined} />}
          </Cartao>
        </div>

        <aside className="space-y-5">
          <Cartao titulo="Cliente"><CartaoCliente cliente={o.cliente} /></Cartao>
          <Cartao titulo="Condições">
            <dl className="grid grid-cols-2 gap-4 text-sm">
              <Info2 rotulo="Data"><span className="numero">{data(o.dataOrcamento)}</span></Info2>
              <Info2 rotulo="Validade"><span className="numero">{data(o.validade)}</span></Info2>
              <Info2 rotulo="Prazo estimado">{o.prazoEstimadoDiasUteis} dias úteis</Info2>
              <Info2 rotulo={o.pedido ? "Entrega prevista" : "Previsão se aprovado hoje"}><span className="numero">{data(o.previsaoEntrega)}</span></Info2>
              <Info2 rotulo="Condição de pagamento" className="col-span-2">{o.condicaoPagamento}</Info2>
              {o.observacoesComerciais && <Info2 rotulo="Observações comerciais" className="col-span-2"><span className="whitespace-pre-line">{o.observacoesComerciais}</span></Info2>}
            </dl>
          </Cartao>
          <Cartao titulo="Resumo financeiro">
            <dl className="space-y-2 text-sm">
              <div className="flex justify-between"><dt className="text-texto-fraco">Subtotal dos produtos</dt><dd className="numero">{moeda(o.resumo.subtotalProdutos)}</dd></div>
              <div className="flex justify-between"><dt className="text-texto-fraco">Personalização</dt><dd className="numero">{moeda(o.resumo.valorPersonalizacao)}</dd></div>
              <div className="flex justify-between"><dt className="text-texto-fraco">Desconto</dt><dd className="numero">{o.resumo.desconto ? `− ${moeda(o.resumo.desconto)}` : moeda(0)}</dd></div>
              <div className="flex items-baseline justify-between border-t border-borda pt-3"><dt className="font-medium">Total</dt><dd className="numero text-xl font-semibold">{moeda(o.resumo.valorTotal)}</dd></div>
            </dl>
            <p className="mt-2 text-xs text-texto-fraco">{o.resumo.quantidadeItens} {o.resumo.quantidadeItens === 1 ? "item" : "itens"} · {o.resumo.totalUnidades} unidades</p>
          </Cartao>
        </aside>
      </div>

      <ModalContato o={o} aberto={modal === "contato"} aoFechar={() => setModal(null)} />
      <ModalApresentar o={o} aberto={modal === "apresentar"} aoFechar={() => setModal(null)} />
      <ModalDecisao key={`d-${o.versao}`} o={o} aberto={modal === "decisao"} aoFechar={() => setModal(null)} aoAprovado={setPedidoGerado} />
      <ModalDecisao key={`c-${o.versao}`} o={o} aberto={modal === "cancelar"} aoFechar={() => setModal(null)} somenteCancelar aoAprovado={setPedidoGerado} />

      <Modal aberto={!!pedidoGerado} aoFechar={() => setPedidoGerado(null)} titulo="Pedido gerado com sucesso"
        rodape={<><Botao variante="secundaria" onClick={() => setPedidoGerado(null)}>Voltar ao orçamento</Botao>{pedidoGerado && <Link href={`/pedidos/${pedidoGerado.id}`}><Botao icone={<Package className="h-4 w-4" />}>Ver pedido {numeroDocumento(pedidoGerado.numero)}</Botao></Link>}</>}>
        {pedidoGerado && <div className="flex gap-4">
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-emerald-50 text-emerald-600"><CheckCircle2 className="h-6 w-6" /></div>
          <div className="text-sm"><p>O orçamento <b className="numero">{numeroDocumento(o.numero)}</b> foi aprovado e o pedido <b className="numero">{numeroDocumento(pedidoGerado.numero)}</b> foi criado com status <b>Aberto</b>.</p>
            <p className="mt-2 text-texto-suave">Entrega prevista para <span className="numero font-medium">{data(pedidoGerado.prazoEntrega)}</span>. Itens, personalização, artes e valores foram copiados do orçamento.</p></div>
        </div>}
      </Modal>
    </>
  );
}
