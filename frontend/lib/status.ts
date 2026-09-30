import type { MeioContato, ResultadoDecisao, SituacaoOrcamento, SituacaoPedido, TipoPessoa } from "@/types/api";

export const rotuloSituacaoOrcamento: Record<SituacaoOrcamento, string> = {
  EmElaboracao: "Em Elaboração", AguardandoRetorno: "Aguardando Retorno", Aprovado: "Aprovado",
  Recusado: "Recusado", Expirado: "Expirado", Cancelado: "Cancelado",
};

export const estiloSituacaoOrcamento: Record<SituacaoOrcamento, string> = {
  EmElaboracao: "bg-blue-50 text-blue-700 border-blue-200",
  AguardandoRetorno: "bg-amber-50 text-amber-800 border-amber-200",
  Aprovado: "bg-emerald-50 text-emerald-700 border-emerald-200",
  Recusado: "bg-red-50 text-red-700 border-red-200",
  Expirado: "bg-slate-100 text-slate-600 border-slate-200",
  Cancelado: "bg-white text-red-700 border-red-300",
};

export const rotuloSituacaoPedido: Record<SituacaoPedido, string> = {
  Aberto: "Aberto", EmProducao: "Em produção", Pronto: "Pronto", Entregue: "Entregue", Cancelado: "Cancelado",
};

export const estiloSituacaoPedido: Record<SituacaoPedido, string> = {
  Aberto: "bg-slate-100 text-slate-700 border-slate-200",
  EmProducao: "bg-blue-50 text-blue-700 border-blue-200",
  Pronto: "bg-emerald-50 text-emerald-700 border-emerald-200",
  Entregue: "bg-slate-900 text-white border-slate-900",
  Cancelado: "bg-white text-red-700 border-red-300",
};

export const fluxoPedido: SituacaoPedido[] = ["Aberto", "EmProducao", "Pronto", "Entregue"];

export const rotuloMeio: Record<MeioContato, string> = {
  WhatsApp: "WhatsApp", Ligacao: "Ligação", Email: "E-mail", Presencial: "Presencial", Outro: "Outro",
};
export const meiosContato: MeioContato[] = ["WhatsApp", "Ligacao", "Email", "Presencial", "Outro"];

export const rotuloResultado: Record<ResultadoDecisao, string> = {
  Aprovado: "Aprovado", Recusado: "Recusado", Cancelado: "Cancelado", Expirado: "Expirado",
};

export const rotuloTipoPessoa: Record<TipoPessoa, string> = { PessoaFisica: "Pessoa física", PessoaJuridica: "Pessoa jurídica" };
export const siglaTipoPessoa: Record<TipoPessoa, string> = { PessoaFisica: "PF", PessoaJuridica: "PJ" };

export const situacoesOrcamento = Object.keys(rotuloSituacaoOrcamento) as SituacaoOrcamento[];
export const situacoesPedido = Object.keys(rotuloSituacaoPedido) as SituacaoPedido[];
