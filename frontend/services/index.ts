import { api } from "@/lib/api";
import type {
  Anexo, Calculo, ClienteDetalhe, ClienteListaItem, ClienteRequisicao, ClienteVisaoGeral, Contato, Dashboard, DecisaoResultado,
  HistoricoEvento, ItemDetalhe, ListaComContagem, ListaFiltrada, LoginResposta, MeioContato, ObservacaoCliente, OpcoesOrcamento,
  OrcamentoDetalhe, OrcamentoListaItem, OrcamentoRequisicao, OrcamentoResumo, Pagina, PedidoDetalhe, PedidoListaItem, PedidoResumo,
  Produto, ResultadoDecisao, SituacaoOrcamento, SituacaoPedido, TipoPessoa, Usuario, VerificacaoDocumento,
} from "@/types/api";

type Params = Record<string, string | number | undefined | null>;
const limpar = (p: Params) => Object.fromEntries(Object.entries(p).filter(([, v]) => v !== undefined && v !== null && v !== ""));

export const authService = {
  login: (email: string, senha: string, manterConectado: boolean) =>
    api.post<LoginResposta>("/auth/login", { email, senha, manterConectado }).then((r) => r.data),
  logout: () => api.post("/auth/logout"),
  me: () => api.get<Usuario>("/auth/me").then((r) => r.data),
};

export const clientesService = {
  listar: (p: { busca?: string; tipo?: TipoPessoa | ""; cidade?: string; pagina?: number }) =>
    api.get<Pagina<ClienteListaItem>>("/clientes", { params: limpar({ ...p, tamanhoPagina: 20 }) }).then((r) => r.data),
  cidades: () => api.get<string[]>("/clientes/cidades").then((r) => r.data),
  verificarDocumento: (documento: string, tipo: TipoPessoa, excetoId?: string) =>
    api.get<VerificacaoDocumento>("/clientes/verificar-documento", { params: limpar({ documento, tipo, excetoId }) }).then((r) => r.data),
  criar: (dados: ClienteRequisicao) => api.post<ClienteDetalhe>("/clientes", dados).then((r) => r.data),
  atualizar: (id: string, dados: ClienteRequisicao) => api.put<ClienteDetalhe>(`/clientes/${id}`, dados).then((r) => r.data),
  obter: (id: string) => api.get<ClienteDetalhe>(`/clientes/${id}`).then((r) => r.data),
  visaoGeral: (id: string) => api.get<ClienteVisaoGeral>(`/clientes/${id}/visao-geral`).then((r) => r.data),
  orcamentos: (id: string, p: { de?: string; ate?: string; status?: SituacaoOrcamento | "" }) =>
    api.get<ListaFiltrada<OrcamentoResumo>>(`/clientes/${id}/orcamentos`, { params: limpar(p) }).then((r) => r.data),
  pedidos: (id: string, p: { de?: string; ate?: string; status?: SituacaoPedido | "" }) =>
    api.get<ListaFiltrada<PedidoResumo>>(`/clientes/${id}/pedidos`, { params: limpar(p) }).then((r) => r.data),
  historico: (id: string, p: { de?: string; ate?: string; tipo?: string; statusOrcamento?: string; statusPedido?: string }) =>
    api.get<HistoricoEvento[]>(`/clientes/${id}/historico`, { params: limpar(p) }).then((r) => r.data),
  observacoes: (id: string) => api.get<ObservacaoCliente[]>(`/clientes/${id}/observacoes`).then((r) => r.data),
  adicionarObservacao: (id: string, texto: string) => api.post<ObservacaoCliente>(`/clientes/${id}/observacoes`, { texto }).then((r) => r.data),
};

export const orcamentosService = {
  listar: (p: { busca?: string; status?: SituacaoOrcamento | ""; responsavelId?: string; clienteId?: string; de?: string; ate?: string; pagina?: number }) =>
    api.get<ListaComContagem<OrcamentoListaItem>>("/orcamentos", { params: limpar({ ...p, tamanhoPagina: 20 }) }).then((r) => r.data),
  obter: (id: string) => api.get<OrcamentoDetalhe>(`/orcamentos/${id}`).then((r) => r.data),
  criar: (dados: OrcamentoRequisicao) => api.post<OrcamentoDetalhe>("/orcamentos", dados).then((r) => r.data),
  atualizar: (id: string, dados: OrcamentoRequisicao) => api.put<OrcamentoDetalhe>(`/orcamentos/${id}`, dados).then((r) => r.data),
  calcular: (itens: { quantidade: number | null; valorUnitario: number | null; valorPersonalizacaoUnitario: number | null }[], desconto: number | null) =>
    api.post<Calculo>("/orcamentos/calculo", { itens, desconto }).then((r) => r.data),
  itens: (id: string) => api.get<ItemDetalhe[]>(`/orcamentos/${id}/itens`).then((r) => r.data),
  historico: (id: string, categoria?: string) =>
    api.get<HistoricoEvento[]>(`/orcamentos/${id}/historico`, { params: limpar({ categoria }) }).then((r) => r.data),
  contatos: (id: string) => api.get<Contato[]>(`/orcamentos/${id}/contatos`).then((r) => r.data),
  registrarContato: (id: string, dados: { tipo: MeioContato | null; dataHora: string | null; observacao: string }) =>
    api.post<Contato>(`/orcamentos/${id}/contatos`, dados).then((r) => r.data),
  marcarAguardando: (id: string, meioApresentacao: MeioContato | null, versao: number) =>
    api.post<OrcamentoDetalhe>(`/orcamentos/${id}/aguardando-retorno`, { meioApresentacao, versao }).then((r) => r.data),
  registrarDecisao: (id: string, dados: { resultado: ResultadoDecisao | null; meioContato: MeioContato | null; dataHora: string | null; motivo: string; observacao: string; versao: number }) =>
    api.post<DecisaoResultado>(`/orcamentos/${id}/decisao`, dados).then((r) => r.data),
  motivos: () => api.get<string[]>("/orcamentos/motivos-decisao").then((r) => r.data),
};

export const pedidosService = {
  listar: (p: { busca?: string; status?: SituacaoPedido | ""; responsavelId?: string; de?: string; ate?: string; pagina?: number }) =>
    api.get<ListaComContagem<PedidoListaItem>>("/pedidos", { params: limpar({ ...p, tamanhoPagina: 20 }) }).then((r) => r.data),
  obter: (id: string) => api.get<PedidoDetalhe>(`/pedidos/${id}`).then((r) => r.data),
  historico: (id: string) => api.get<HistoricoEvento[]>(`/pedidos/${id}/historico`).then((r) => r.data),
  atualizarStatus: (id: string, novaSituacao: SituacaoPedido, observacao: string, versao: number) =>
    api.patch<PedidoDetalhe>(`/pedidos/${id}/status`, { novaSituacao, observacao, versao }).then((r) => r.data),
};

export const anexosService = {
  enviar: (arquivo: File, aoProgredir?: (percentual: number) => void) => {
    const form = new FormData();
    form.append("arquivo", arquivo);
    return api.post<Anexo>("/anexos", form, {
      headers: { "Content-Type": "multipart/form-data" },
      timeout: 300_000,
      onUploadProgress: (e) => aoProgredir?.(e.total ? Math.round((e.loaded / e.total) * 100) : 0),
    }).then((r) => r.data);
  },
  remover: (id: string) => api.delete(`/anexos/${id}`),
  url: (id: string, inline = false) => `/api/anexos/${id}/download${inline ? "?inline=true" : ""}`,
};

export const dashboardService = { obter: () => api.get<Dashboard>("/dashboard").then((r) => r.data) };

export const catalogoService = {
  produtos: () => api.get<Produto[]>("/produtos").then((r) => r.data),
  opcoes: () => api.get<OpcoesOrcamento>("/produtos/opcoes").then((r) => r.data),
  responsaveis: () => api.get<Usuario[]>("/usuarios/responsaveis").then((r) => r.data),
};
