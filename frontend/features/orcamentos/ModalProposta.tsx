"use client";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AreaTexto, Aviso, Botao, Campo, Carregando, Entrada, ErroCarregamento, Modal, Selecao } from "@/components/ui";
import { useToast } from "@/components/ui/toast";
import { propostaService } from "@/services";
import { erroDaApi } from "@/lib/api";
import type { CanalProposta, OrcamentoDetalhe, PreparacaoProposta } from "@/types/api";

function FormularioProposta({ o, dados, aoFechar, aoOutro, aoOcupado }: { o: OrcamentoDetalhe; dados: PreparacaoProposta; aoFechar: () => void; aoOutro: () => void; aoOcupado: (valor: boolean) => void }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [canal, setCanal] = useState<CanalProposta>("Email");
  const [destino, setDestino] = useState(dados.email ?? "");
  const [telefone, setTelefone] = useState(dados.whatsApp ?? "");
  const [assunto, setAssunto] = useState(dados.assunto);
  const [mensagemEmail, setMensagemEmail] = useState(dados.mensagemEmail);
  const [mensagemWa, setMensagemWa] = useState(dados.mensagemWhatsApp);
  const [chave, setChave] = useState(() => crypto.randomUUID());
  const [tentou, setTentou] = useState(false);
  const [baixou, setBaixou] = useState(false);
  const [confirmou, setConfirmou] = useState(false);
  const [aviso, setAviso] = useState("");
  const mensagem = canal === "Email" ? mensagemEmail : mensagemWa;
  const numero = telefone.replace(/\D/g, "");
  const internacional = numero.length === 10 || numero.length === 11 ? `55${numero}` : numero;
  const telefoneValido = /^\d{12,15}$/.test(internacional) && !internacional.startsWith("0");
  const emailValido = /^[^\s@,;]+@[^\s@,;]+\.[^\s@,;]+$/.test(destino.trim());
  const mailto = `mailto:${encodeURIComponent(destino.trim())}?subject=${encodeURIComponent(assunto)}&body=${encodeURIComponent(mensagemEmail)}`;
  const whatsapp = `https://wa.me/${internacional}?text=${encodeURIComponent(mensagemWa)}`;

  // A gravação já foi confirmada pela API: aplica o detalhe retornado e fecha o modal na hora.
  // As demais consultas (análise interna, histórico, listas) são atualizadas em segundo plano,
  // sem prender o modal aberto enquanto recarregam.
  function concluir(detalhe: OrcamentoDetalhe | null, mensagem: string) {
    if (detalhe) qc.setQueryData(["orcamento", o.id], detalhe);
    for (const queryKey of [["orcamento", o.id], ["orcamentos"], ["dashboard"], ["cliente", o.cliente.id]]) void qc.invalidateQueries({ queryKey });
    // A preparação (["proposta", id]) usa gcTime 0 e é descartada quando o modal fecha.
    toast("sucesso", mensagem); aoFechar();
  }
  const enviar = useMutation({ mutationFn: () => propostaService.enviar(o.id, { chaveEnvio: chave, destinatario: destino.trim(), assunto, mensagem: mensagemEmail, versao: dados.versao }), retry: false,
    onMutate: () => { aoOcupado(true); setTentou(true); setAviso(""); }, onSettled: () => aoOcupado(false), onSuccess: r => concluir(r.orcamento, "Proposta aceita pelo servidor de e-mail e apresentação registrada.") });
  const consultar = useMutation({ mutationFn: () => propostaService.consultar(o.id, chave), retry: false, onSuccess: r => {
    if (r.situacao === "Enviado") concluir(null, "O envio foi confirmado e já está registrado.");
    else setAviso("O envio continua sem confirmação. Confira sua caixa de enviados antes de preparar outro envio.");
  } });
  const pdf = useMutation({ mutationFn: () => propostaService.pdf(o.id), onSuccess: arquivo => {
    const url = URL.createObjectURL(arquivo); const a = document.createElement("a");
    a.href = url; a.download = `Proposta-${String(o.numero).padStart(6, "0")}.pdf`; document.body.appendChild(a); a.click(); a.remove();
    window.setTimeout(() => URL.revokeObjectURL(url), 60_000); setBaixou(true); void qc.invalidateQueries({ queryKey: ["orcamento", o.id] });
  } });
  const confirmar = useMutation({ mutationFn: () => propostaService.confirmar(o.id, canal, dados.versao), retry: false, onMutate: () => aoOcupado(true), onSettled: () => aoOcupado(false), onSuccess: detalhe => concluir(detalhe, "Apresentação da proposta registrada.") });
  const pendente = enviar.isPending || confirmar.isPending;
  async function copiar() {
    try { await navigator.clipboard.writeText(mensagem); toast("sucesso", "Mensagem copiada."); } catch { setAviso("Não foi possível copiar. Selecione o texto da mensagem e copie manualmente."); }
  }
  const erro = enviar.error ?? consultar.error ?? pdf.error ?? confirmar.error;
  return <div className="space-y-4">
    {erro && <Aviso tipo="erro">{erroDaApi(erro).message}</Aviso>}
    {aviso && <Aviso>{aviso}</Aviso>}
    {dados.ultimoEnvio && <p className="break-words text-xs text-texto-fraco">Última tentativa de e-mail: {dados.ultimoEnvio.destinatario} · {dados.ultimoEnvio.situacao === "Enviado" ? "aceita pelo servidor" : "sem confirmação; confira antes de reenviar"}.</p>}
    <Campo rotulo="Canal de envio" htmlFor="proposta-canal"><Selecao id="proposta-canal" value={canal} disabled={pendente} onChange={e => { setCanal(e.target.value as CanalProposta); setConfirmou(false); }}><option value="Email">E-mail</option><option value="WhatsApp">WhatsApp</option></Selecao></Campo>
    {canal === "Email" ? <>
      <Campo rotulo="Destinatário" htmlFor="proposta-destino" ajuda="Confira o endereço antes de enviar."><Entrada id="proposta-destino" type="email" maxLength={254} value={destino} disabled={pendente || tentou} onChange={e => setDestino(e.target.value)} /></Campo>
      <Campo rotulo="Assunto" htmlFor="proposta-assunto"><Entrada id="proposta-assunto" maxLength={200} value={assunto} disabled={pendente || tentou} onChange={e => setAssunto(e.target.value)} /></Campo>
    </> : <Campo rotulo="WhatsApp do cliente" htmlFor="proposta-telefone" ajuda="DDD e número; para outro país, inclua o código internacional."><Entrada id="proposta-telefone" inputMode="tel" maxLength={25} value={telefone} disabled={pendente} onChange={e => setTelefone(e.target.value)} /></Campo>}
    <Campo rotulo="Mensagem ao cliente" htmlFor="proposta-mensagem" ajuda="Revise o texto. O PDF contém os valores e as condições da proposta."><AreaTexto id="proposta-mensagem" value={mensagem} maxLength={6000} className="min-h-[180px]" disabled={pendente || (canal === "Email" && tentou)} onChange={e => canal === "Email" ? setMensagemEmail(e.target.value) : setMensagemWa(e.target.value)} /></Campo>
    <div className="flex flex-wrap gap-2"><Botao variante="secundaria" onClick={copiar}>Copiar mensagem</Botao><Botao variante="secundaria" carregando={pdf.isPending} onClick={() => pdf.mutate()}>Baixar PDF da proposta</Botao></div>
    {canal === "Email" && dados.emailDisponivel ? <>
      <p className="text-sm text-texto-suave">O PDF será anexado automaticamente. A apresentação será registrada após a aceitação pelo servidor de e-mail.</p>
      <Botao carregando={enviar.isPending} disabled={!emailValido || !assunto.trim() || !mensagemEmail.trim() || tentou} onClick={() => enviar.mutate()}>Enviar e-mail com PDF</Botao>
      {tentou && !enviar.isPending && <div className="space-y-3"><Botao variante="secundaria" carregando={consultar.isPending} onClick={() => consultar.mutate()}>Consultar resultado</Botao><label className="flex items-start gap-2 text-sm"><input type="checkbox" checked={confirmou} onChange={e => setConfirmou(e.target.checked)} />Conferi minha caixa de enviados e quero preparar um novo envio.</label><Botao variante="secundaria" disabled={!confirmou || consultar.isPending} onClick={() => { setChave(crypto.randomUUID()); setTentou(false); setConfirmou(false); enviar.reset(); consultar.reset(); setAviso(""); }}>Preparar novo envio</Botao></div>}
    </> : <>
      <Aviso>{canal === "Email" ? "Envio direto indisponível. Abra seu aplicativo de e-mail e anexe o PDF baixado." : "O WhatsApp abrirá com o texto. Anexe manualmente o PDF baixado antes de enviar."} Abrir o aplicativo ou copiar a mensagem não registra a apresentação.</Aviso>
      {(canal === "Email" ? emailValido : telefoneValido) && mensagem.trim() ? <a href={canal === "Email" ? mailto : whatsapp} target={canal === "WhatsApp" ? "_blank" : undefined} rel="noreferrer" className="inline-flex rounded-md border border-borda bg-white px-3 py-2 text-sm font-medium text-primaria">{canal === "Email" ? "Abrir aplicativo de e-mail" : "Abrir WhatsApp"}</a> : <p className="text-sm text-texto-fraco">Informe um {canal === "Email" ? "e-mail" : "telefone"} válido e uma mensagem para abrir o aplicativo.</p>}
      <label className="flex items-start gap-2 text-sm"><input type="checkbox" checked={confirmou} onChange={e => setConfirmou(e.target.checked)} />Já enviei a mensagem e anexei o PDF ao cliente.</label>
      <Botao carregando={confirmar.isPending} disabled={!baixou || !confirmou} onClick={() => confirmar.mutate()}>Confirmar apresentação</Botao>
    </>}
    <div className="flex flex-wrap justify-between gap-2 border-t border-borda pt-4"><Botao variante="fantasma" disabled={pendente} onClick={aoOutro}>Registrar por outro meio</Botao><Botao variante="secundaria" disabled={pendente} onClick={aoFechar}>Fechar</Botao></div>
  </div>;
}

export function ModalProposta({ o, aoFechar, aoOutro }: { o: OrcamentoDetalhe; aoFechar: () => void; aoOutro: () => void }) {
  const [ocupado, setOcupado] = useState(false);
  const q = useQuery({ queryKey: ["proposta", o.id], queryFn: () => propostaService.preparar(o.id), retry: false, refetchOnWindowFocus: false, gcTime: 0 });
  return <Modal aberto aoFechar={() => { if (!ocupado) aoFechar(); }} titulo="Enviar proposta" descricao="Revise o destinatário e a mensagem antes de enviar." largura="max-w-2xl">
    {q.isError ? <ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} /> : !q.data ? <Carregando /> : <FormularioProposta o={o} dados={q.data} aoOcupado={setOcupado} aoFechar={aoFechar} aoOutro={aoOutro} />}
  </Modal>;
}
