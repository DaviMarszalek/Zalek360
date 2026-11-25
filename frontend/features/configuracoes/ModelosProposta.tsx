"use client";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AreaTexto, Aviso, Botao, Campo, Carregando, Cartao, Entrada, ErroCarregamento, Modal } from "@/components/ui";
import { propostaService } from "@/services";
import { erroDaApi } from "@/lib/api";
import type { ModeloProposta } from "@/types/api";

function Editor({ modelo, aoSalvar }: { modelo: ModeloProposta; aoSalvar: () => Promise<void> }) {
  const [assunto, setAssunto] = useState(modelo.assunto);
  const [mensagem, setMensagem] = useState(modelo.mensagem);
  const m = useMutation({ mutationFn: () => propostaService.salvarModelo(modelo.id, { assunto, mensagem, versao: modelo.versao }), onSuccess: aoSalvar });
  return <form className="space-y-4" onSubmit={e => { e.preventDefault(); m.mutate(); }}>
    {m.isError && <Aviso tipo="erro">{erroDaApi(m.error).message}</Aviso>}
    {modelo.canal === "Email" && <Campo rotulo="Assunto do modelo" htmlFor="modelo-assunto"><Entrada id="modelo-assunto" value={assunto} maxLength={200} onChange={e => setAssunto(e.target.value)} /></Campo>}
    <Campo rotulo="Texto do modelo" htmlFor="modelo-texto"><AreaTexto id="modelo-texto" className="min-h-[200px]" value={mensagem} maxLength={6000} onChange={e => setMensagem(e.target.value)} /></Campo>
    <p className="break-words text-xs text-texto-fraco">Campos disponíveis: {"{cliente}, {numero}, {valorTotal}, {validade}, {prazo}, {empresa}"}. Cada envio permite revisar o texto preenchido. Alterar o modelo não altera propostas já enviadas.</p>
    <div className="flex justify-end"><Botao type="submit" carregando={m.isPending}>Salvar modelo</Botao></div>
  </form>;
}

export function ModelosProposta() {
  const qc = useQueryClient();
  const q = useQuery({ queryKey: ["configuracoes", "modelos-proposta"], queryFn: propostaService.modelos });
  const [edicao, setEdicao] = useState<ModeloProposta | null>(null);
  return <>
    <Cartao titulo="Mensagens de proposta" className="mt-6">
      {q.isError ? <ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} /> : !q.data ? <Carregando /> : <div className="space-y-4">
        <p className="text-sm text-texto-suave">E-mail: {q.data.emailDisponivel ? "envio direto disponível" : "envio pelo aplicativo de e-mail; configuração do servidor pendente"}. As credenciais são configuradas no servidor e não são exibidas aqui.</p>
        {q.data.modelos.map(m => <div key={m.id} className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-borda p-3"><p className="font-medium">{m.canal === "Email" ? "E-mail" : "WhatsApp"}</p><Botao variante="secundaria" onClick={() => setEdicao(m)}>Editar modelo de {m.canal === "Email" ? "e-mail" : "WhatsApp"}</Botao></div>)}
      </div>}
    </Cartao>
    <Modal aberto={!!edicao} aoFechar={() => setEdicao(null)} titulo="Editar modelo de mensagem" descricao={edicao?.canal === "Email" ? "E-mail" : "WhatsApp"} largura="max-w-2xl">
      {edicao && <Editor key={`${edicao.id}-${edicao.versao}`} modelo={edicao} aoSalvar={async () => { await Promise.all([qc.invalidateQueries({ queryKey: ["configuracoes", "modelos-proposta"] }), qc.invalidateQueries({ queryKey: ["proposta"] })]); setEdicao(null); }} />}
    </Modal>
  </>;
}
