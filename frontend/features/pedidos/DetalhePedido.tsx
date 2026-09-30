"use client";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowRight, Ban, Check, FileImage, FileText, Info } from "lucide-react";
import { AreaTexto, Aviso, BadgePedido, Botao, CabecalhoPagina, Campo, Cartao, Carregando, ErroCarregamento, Etiqueta, Info2, Modal } from "@/components/ui";
import { useToast } from "@/components/ui/toast";
import { CartaoCliente, LinhaDoTempo, Voltar } from "@/features/comum";
import { anexosService, pedidosService } from "@/services";
import { erroCampo, erroDaApi } from "@/lib/api";
import { cn } from "@/lib/utils";
import { fluxoPedido, rotuloMeio, rotuloSituacaoPedido } from "@/lib/status";
import { data, dataHora, dataIsoDeInstante, diasEntre, hojeIso, hora, moeda, numeroDocumento, tamanhoArquivo } from "@/utils/formatos";
import type { ErroApi, PedidoDetalhe, SituacaoPedido } from "@/types/api";

function Etapas({ p }: { p: PedidoDetalhe }) {
  const indiceAtual = fluxoPedido.indexOf(p.situacao);
  const cancelado = p.situacao === "Cancelado";
  return (
    <ol className="flex flex-wrap items-center gap-y-2 text-[13px]" aria-label="Etapas do pedido">
      {fluxoPedido.map((s, i) => {
        const feito = !cancelado && i <= indiceAtual;
        const atual = !cancelado && i === indiceAtual;
        return (
          <li key={s} className="flex items-center">
            <span className={cn("flex items-center gap-2 rounded-full px-3 py-1",
              atual ? "bg-primaria text-white" : feito ? "bg-primaria-clara text-primaria" : "bg-slate-100 text-texto-fraco")}
              aria-current={atual ? "step" : undefined}>
              <span className={cn("numero flex h-4 w-4 items-center justify-center rounded-full text-[10px]", atual ? "bg-white/25" : "bg-white")}>
                {feito && !atual ? <Check className="h-3 w-3" /> : i + 1}
              </span>{rotuloSituacaoPedido[s]}
            </span>
            {i < fluxoPedido.length - 1 && <span className={cn("mx-1.5 h-px w-6", feito && i < indiceAtual ? "bg-primaria-borda" : "bg-borda")} />}
          </li>);
      })}
      {cancelado && <li className="ml-3"><span className="flex items-center gap-2 rounded-full bg-red-50 px-3 py-1 text-red-700"><Ban className="h-3.5 w-3.5" />Cancelado</span></li>}
    </ol>
  );
}

/** Faixa de origem (protótipo): todo pedido nasce de um orçamento aprovado e herda seus dados. */
function FaixaOrigem({ p }: { p: PedidoDetalhe }) {
  const o = p.origem;
  return (
    <div className="flex flex-wrap items-start justify-between gap-3 rounded-lg border border-primaria-borda bg-primaria-clara px-4 py-3 text-sm">
      <div>
        <p className="font-medium text-primaria">Originado do <Link href={`/orcamentos/${o.orcamentoId}`} className="numero underline-offset-2 hover:underline">Orçamento {numeroDocumento(o.orcamentoNumero)}</Link></p>
        <p className="mt-0.5 text-[13px] text-texto-suave">
          {o.aprovadoEm ? <>Aprovado em <span className="numero">{data(dataIsoDeInstante(o.aprovadoEm))}</span> às <span className="numero">{hora(o.aprovadoEm)}</span></> : "Aprovado"}
          {o.meioContato && <> via {rotuloMeio[o.meioContato]}</>}{o.decisaoRegistradaPor && <> · decisão registrada por {o.decisaoRegistradaPor}</>}
        </p>
      </div>
      <p className="max-w-sm text-[13px] text-texto-suave">Todo pedido nasce de um orçamento aprovado. Os dados abaixo foram copiados do orçamento de origem.</p>
    </div>
  );
}

/** Avisos de situação que exigem atenção (o protótipo mostra o fluxo; aqui ficam cancelamento, entrega e atraso). */
function AvisoSituacao({ p }: { p: PedidoDetalhe }) {
  if (p.situacao === "Cancelado")
    return <Aviso tipo="erro" className="bg-white" titulo={`Pedido cancelado em ${p.canceladoEm ? dataHora(p.canceladoEm) : ""}`}>Motivo: {p.motivoCancelamento}</Aviso>;
  if (p.situacao === "Entregue")
    return <Aviso tipo="sucesso" titulo={`Pedido entregue em ${p.entregueEm ? dataHora(p.entregueEm) : ""}`}>Fluxo concluído. O pedido não pode mais mudar de status.</Aviso>;
  const dias = diasEntre(hojeIso(), p.prazoEntrega);
  if (dias < 0)
    return <Aviso tipo="erro" titulo={`Prazo de entrega vencido em ${data(p.prazoEntrega)}`}>O pedido está {rotuloSituacaoPedido[p.situacao].toLowerCase()} e passou {Math.abs(dias)} {Math.abs(dias) === 1 ? "dia" : "dias"} do prazo combinado com o cliente.</Aviso>;
  return null;
}

const Herdado = () => <Etiqueta>Herdado do orçamento</Etiqueta>;

function ModalStatus({ p, alvo, aoFechar }: { p: PedidoDetalhe; alvo: SituacaoPedido | null; aoFechar: () => void }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [obs, setObs] = useState("");
  const [erro, setErro] = useState<ErroApi | null>(null);
  const cancelando = alvo === "Cancelado";
  const fechar = () => { setObs(""); setErro(null); aoFechar(); };
  const m = useMutation({
    mutationFn: () => pedidosService.atualizarStatus(p.id, alvo!, obs, p.versao),
    onSuccess: (d) => {
      qc.setQueryData(["pedido", p.id], d);
      for (const k of [["pedido", p.id], ["pedidos"], ["dashboard"], ["cliente", p.cliente.id]]) qc.invalidateQueries({ queryKey: k });
      toast("sucesso", cancelando ? `Pedido ${numeroDocumento(p.numero)} cancelado.` : `Pedido ${numeroDocumento(p.numero)} agora está ${rotuloSituacaoPedido[d.situacao]}.`);
      fechar();
    },
    onError: (e) => setErro(erroDaApi(e)),
  });
  return (
    <Modal aberto={!!alvo} aoFechar={fechar}
      titulo={cancelando ? `Cancelar pedido ${numeroDocumento(p.numero)}` : `Atualizar status do pedido ${numeroDocumento(p.numero)}`}
      descricao={cancelando ? "O cancelamento é definitivo e fica registrado no histórico do pedido e do cliente."
        : `${rotuloSituacaoPedido[p.situacao]} → ${alvo ? rotuloSituacaoPedido[alvo] : ""}. O status avança uma etapa por vez.`}
      rodape={<><Botao variante="secundaria" onClick={fechar}>Voltar</Botao>
        <Botao variante={cancelando ? "perigo" : "primaria"} carregando={m.isPending} onClick={() => m.mutate()}
          icone={cancelando ? <Ban className="h-4 w-4" /> : <ArrowRight className="h-4 w-4" />}>{cancelando ? "Cancelar pedido" : `Mover para ${alvo ? rotuloSituacaoPedido[alvo] : ""}`}</Botao></>}>
      <div className="space-y-4">
        {erro && !Object.keys(erro.errors).length && <Aviso tipo="erro">{erro.message}</Aviso>}
        <Campo rotulo={cancelando ? "Motivo do cancelamento" : "Observação (opcional)"} obrigatorio={cancelando} erro={erroCampo(erro, "observacao") ?? erroCampo(erro, "novaSituacao")}>
          <AreaTexto value={obs} maxLength={500} onChange={(e) => setObs(e.target.value)} invalido={!!erroCampo(erro, "observacao")}
            placeholder={cancelando ? "Ex.: cliente desistiu da compra após a aprovação." : "Ex.: arte liberada pela produção."} />
        </Campo>
      </div>
    </Modal>
  );
}

export function DetalhePedido() {
  const { id } = useParams<{ id: string }>();
  const q = useQuery({ queryKey: ["pedido", id], queryFn: () => pedidosService.obter(id) });
  const historico = useQuery({ queryKey: ["pedido", id, "historico"], queryFn: () => pedidosService.historico(id), enabled: !!q.data });
  const [alvo, setAlvo] = useState<SituacaoPedido | null>(null);

  if (q.isError) return <ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} />;
  if (!q.data) return <Carregando />;
  const p = q.data;
  const anexos = p.itens.flatMap((i) => i.anexos.map((a) => ({ ...a, item: i.ordem })));

  return (
    <>
      <CabecalhoPagina voltar={<Voltar href="/pedidos">Pedidos</Voltar>}
        titulo={<span className="flex flex-wrap items-center gap-3">Pedido <span className="numero">{numeroDocumento(p.numero)}</span><BadgePedido situacao={p.situacao} /></span>}
        subtitulo={<>{p.cliente.nome} · Criado em <span className="numero">{data(dataIsoDeInstante(p.criadoEm))}</span> às <span className="numero">{hora(p.criadoEm)}</span> · Prazo de entrega: <span className="numero">{data(p.prazoEntrega)}</span> · Responsável: {p.responsavel}</>}
        acoes={<>
          {p.podeCancelar && <Botao variante="secundaria" className="text-red-700" icone={<Ban className="h-4 w-4" />} onClick={() => setAlvo("Cancelado")}>Cancelar pedido</Botao>}
          {p.proximaSituacao && <Botao icone={<ArrowRight className="h-4 w-4" />} onClick={() => setAlvo(p.proximaSituacao)}>Atualizar status</Botao>}
        </>} />

      <div className="mb-5 space-y-4"><FaixaOrigem p={p} /><Etapas p={p} /><AvisoSituacao p={p} /></div>

      <div className="grid gap-6 xl:grid-cols-[1fr_340px]">
        <div className="min-w-0 space-y-5">
          <Cartao titulo={<span className="flex items-center gap-2">Itens <Herdado /></span>}
            acao={<span className="text-[13px] text-texto-fraco">{p.resumo.quantidadeItens} {p.resumo.quantidadeItens === 1 ? "item" : "itens"} · {p.resumo.totalUnidades} unidades</span>} semPadding>
            <div className="overflow-x-auto"><table className="tabela w-full min-w-[640px] text-sm">
              <thead><tr><th>Produto</th><th className="text-right">Qtd.</th><th className="text-right">Valor unit.</th><th className="text-right">Personalização / un.</th><th className="text-right">Total</th></tr></thead>
              <tbody>{p.itens.map((i) => (
                <tr key={i.id}>
                  <td><p className="font-medium">{i.produtoNome}</p>{i.descricao && <p className="text-[13px] text-texto-fraco">{i.descricao}</p>}</td>
                  <td className="numero text-right">{i.quantidade}</td>
                  <td className="numero text-right">{moeda(i.valorUnitario)}</td>
                  <td className="numero text-right">{moeda(i.valorPersonalizacaoUnitario)}</td>
                  <td className="numero text-right font-medium">{moeda(i.valorTotal)}</td>
                </tr>))}</tbody>
            </table></div>
            <div className="divide-y divide-borda border-t border-borda">{p.itens.map((i) => (
              <div key={i.id} className="p-5">
                <p className="mb-2 text-[13px] font-medium text-texto-suave">Personalização · Item {i.ordem}</p>
                <dl className="grid grid-cols-2 gap-x-4 gap-y-2 rounded-lg bg-slate-50/80 p-3 text-[13px] md:grid-cols-3">
                  <Info2 rotulo="Tipo de personalização">{i.personalizacao.tipoPersonalizacao}</Info2><Info2 rotulo="Cor da peça">{i.personalizacao.corPeca}</Info2>
                  <Info2 rotulo="Cores da arte">{i.personalizacao.coresArte ?? "—"}</Info2>
                  <Info2 rotulo="Medidas">{i.personalizacao.medidas ?? "—"}</Info2>
                  <Info2 rotulo="Local de aplicação">{i.personalizacao.localAplicacao ?? "—"}</Info2>
                  <Info2 rotulo="Referência da arte">{i.personalizacao.referenciaArte ?? "—"}</Info2>
                  {i.personalizacao.observacoesTecnicas && <Info2 rotulo="Observações técnicas" className="col-span-2 md:col-span-3">{i.personalizacao.observacoesTecnicas}</Info2>}
                </dl>
              </div>))}</div>
          </Cartao>

          <Cartao titulo={<span className="flex items-center gap-2">Artes e referências anexadas <Herdado /></span>}>
            {anexos.length === 0 ? <p className="text-sm text-texto-fraco">Nenhum arquivo foi anexado ao orçamento de origem.</p>
              : <div className="flex flex-wrap gap-2">{anexos.map((a) => (
                <a key={a.id} href={anexosService.url(a.id, true)} target="_blank" rel="noreferrer" className="inline-flex items-center gap-2 rounded-md border border-borda bg-white px-2.5 py-1.5 text-[13px] hover:border-primaria">
                  {a.ehImagem ? <FileImage className="h-4 w-4 text-texto-fraco" /> : <FileText className="h-4 w-4 text-texto-fraco" />}{a.nome}
                  <span className="numero text-xs text-texto-fraco">{tamanhoArquivo(a.tamanhoBytes)}{p.itens.length > 1 && ` · item ${a.item}`}</span>
                </a>))}</div>}
          </Cartao>

          <Cartao titulo="Histórico do pedido">
            {historico.isError ? <ErroCarregamento mensagem={erroDaApi(historico.error).message} aoTentar={() => historico.refetch()} />
              : !historico.data ? <Carregando /> : <LinhaDoTempo eventos={historico.data} />}
          </Cartao>
        </div>

        <aside className="space-y-5">
          <Cartao titulo="Cliente"><CartaoCliente cliente={p.cliente} /></Cartao>
          <Cartao titulo={<span className="flex items-center gap-2">Resumo financeiro <Herdado /></span>}>
            <dl className="space-y-2 text-sm">
              <div className="flex justify-between"><dt className="text-texto-fraco">Subtotal dos produtos</dt><dd className="numero">{moeda(p.resumo.subtotalProdutos)}</dd></div>
              <div className="flex justify-between"><dt className="text-texto-fraco">Personalização</dt><dd className="numero">{moeda(p.resumo.valorPersonalizacao)}</dd></div>
              <div className="flex justify-between"><dt className="text-texto-fraco">Desconto</dt><dd className="numero">{p.resumo.desconto ? `− ${moeda(p.resumo.desconto)}` : moeda(0)}</dd></div>
              <div className="flex items-baseline justify-between border-t border-borda pt-3"><dt className="font-medium">Total</dt><dd className="numero text-xl font-semibold">{moeda(p.resumo.valorTotal)}</dd></div>
            </dl>
          </Cartao>
          <Cartao titulo={<span className="flex items-center gap-2">Condições comerciais <Herdado /></span>}>
            <dl className="grid grid-cols-2 gap-4 text-sm">
              <Info2 rotulo="Validade do orçamento"><span className="numero">{data(p.origem.validadeOrcamento)}</span></Info2>
              <Info2 rotulo="Responsável">{p.responsavel}</Info2>
              <Info2 rotulo="Prazo estimado" className="col-span-2">{p.prazoEstimadoDiasUteis} dias úteis após a aprovação · entrega prevista <span className="numero">{data(p.prazoEntrega)}</span></Info2>
              <Info2 rotulo="Condição de pagamento" className="col-span-2">{p.condicaoPagamento}</Info2>
              {p.observacoesComerciais && <Info2 rotulo="Observações comerciais" className="col-span-2"><span className="whitespace-pre-line">{p.observacoesComerciais}</span></Info2>}
            </dl>
          </Cartao>
          {p.observacaoDecisao && <Cartao titulo="Observação da decisão"><p className="text-sm text-texto-suave">“{p.observacaoDecisao}”</p></Cartao>}
        </aside>
      </div>

      <ModalStatus key={`${p.versao}-${alvo}`} p={p} alvo={alvo} aoFechar={() => setAlvo(null)} />
    </>
  );
}

/** Tela exibida quando alguém tenta abrir /pedidos/novo: pedido não pode ser criado manualmente (RN9). */
export function PedidoManualBloqueado() {
  return (
    <>
      <CabecalhoPagina voltar={<Voltar href="/pedidos">Pedidos</Voltar>} titulo="Novo pedido" />
      <Cartao className="max-w-2xl">
        <div className="flex gap-4">
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-amber-50 text-amber-700"><Info className="h-5 w-5" /></div>
          <div className="text-sm">
            <p className="text-base font-semibold">Pedido deve ser originado de um orçamento aprovado.</p>
            <p className="mt-2 text-texto-suave">No Zalek360 não existe cadastro manual de pedido. Abra um orçamento que esteja <b>Aguardando Retorno</b> e registre a aprovação do cliente — o pedido é gerado automaticamente com os itens, personalização, artes e valores do orçamento.</p>
            <div className="mt-4 flex flex-wrap gap-2">
              <Link href="/orcamentos?status=AguardandoRetorno"><Botao>Ver orçamentos aguardando retorno</Botao></Link>
              <Link href="/pedidos"><Botao variante="secundaria">Voltar para pedidos</Botao></Link>
            </div>
          </div>
        </div>
      </Cartao>
    </>
  );
}
