// Contrato da API (espelha os DTOs da camada Application). Datas "AAAA-MM-DD" = DateOnly; ISO com "Z" = DateTime UTC.

export type TipoPessoa = "PessoaFisica" | "PessoaJuridica";
export type PerfilUsuario = "Atendente" | "Gestor" | "Operador" | "Administrador";
export type SituacaoOrcamento = "EmElaboracao" | "AguardandoRetorno" | "Aprovado" | "Recusado" | "Expirado" | "Cancelado";
export type SituacaoPedido = "Aberto" | "EmProducao" | "Pronto" | "Entregue" | "Cancelado";
export type MeioContato = "WhatsApp" | "Ligacao" | "Email" | "Presencial" | "Outro";
export type ResultadoDecisao = "Aprovado" | "Recusado" | "Cancelado" | "Expirado";
export type CategoriaHistorico = "alteracao" | "contato" | "decisao" | "pedido";

export interface ErroApi {
  status: number;
  code: string;
  message: string;
  errors: Record<string, string[]>;
  details?: unknown;
  traceId?: string;
}

export interface Pagina<T> { itens: T[]; total: number; pagina: number; tamanhoPagina: number; totalPaginas: number }
export interface ListaComContagem<T> extends Pagina<T> { contagemPorSituacao: Record<string, number> }
export interface ListaFiltrada<T> { itens: T[]; total: number; totalSemFiltro: number }

export interface Usuario { id: string; nome: string; email: string; perfil: PerfilUsuario; iniciais: string }
export interface LoginResposta { usuario: Usuario; accessToken: string; expiraEm: string; manterConectado: boolean }

export interface Produto { id: string; nome: string; categoria: string; descricaoBase: string | null }
export interface OpcoesOrcamento { extensoesPermitidas: string[]; tamanhoMaximoBytes: number; tiposPersonalizacao: string[]; condicoesPagamento: string[] }
export interface Anexo { id: string; nome: string; extensao: string; contentType: string; tamanhoBytes: number; ehImagem: boolean }

export interface ClienteCard {
  id: string; nome: string; tipoPessoa: TipoPessoa; documento: string; contatoNome: string | null; contatoCargo: string | null;
  telefone: string | null; whatsApp: string | null; email: string | null; cidade: string | null;
}

export interface ClienteListaItem {
  id: string; tipoPessoa: TipoPessoa; nome: string; razaoSocial: string | null; documento: string; telefone: string | null;
  whatsApp: string | null; email: string | null; cidade: string | null;
  ultimoOrcamento: { id: string; numero: number; data: string } | null;
}

export interface Endereco { cep: string | null; logradouro: string | null; numero: string | null; complemento: string | null; bairro: string | null; cidade: string | null; uf: string | null }

export interface ClienteDetalhe {
  id: string; tipoPessoa: TipoPessoa; documento: string; nomeRazaoSocial: string; nomeFantasia: string | null; nomeExibicao: string;
  contatoNome: string | null; contatoCargo: string | null; telefone: string | null; whatsApp: string | null; email: string | null;
  endereco: Endereco; criadoEm: string; totalOrcamentos: number; totalPedidos: number; versao: number;
}

export interface ClienteRequisicao {
  tipoPessoa: TipoPessoa | null; documento: string; nomeRazaoSocial: string; nomeFantasia: string; contatoNome: string; contatoCargo: string;
  telefone: string; whatsApp: string; email: string; cep: string; logradouro: string; numero: string; complemento: string; bairro: string;
  cidade: string; uf: string; observacaoInicial?: string; versao?: number;
}

export interface ClienteExistente { id: string; nome: string; tipoPessoa: TipoPessoa; documento: string; cidade: string | null; ultimoOrcamentoNumero: number | null }
export interface VerificacaoDocumento { valido: boolean; disponivel: boolean; mensagem: string | null; clienteExistente: ClienteExistente | null }

export interface OrcamentoResumo { id: string; numero: number; data: string; validade: string; situacao: SituacaoOrcamento; valorTotal: number; quantidadeItens: number; totalUnidades: number; responsavel: string }
export interface PedidoResumo { id: string; numero: number; data: string; prazoEntrega: string; situacao: SituacaoPedido; valorTotal: number; orcamentoId: string; orcamentoNumero: number; responsavel: string }

export interface HistoricoEvento {
  id: string; tipo: string; categoria: CategoriaHistorico; titulo: string; descricao: string | null; meioContato: MeioContato | null;
  ocorridoEm: string; autor: string; automatico: boolean; orcamentoId: string | null; orcamentoNumero: number | null;
  pedidoId: string | null; pedidoNumero: number | null; situacaoNova: string | null;
}

export interface ClienteVisaoGeral {
  totalOrcamentos: number; orcamentosAprovados: number; totalPedidos: number; totalEmPedidos: number;
  ultimoOrcamento: OrcamentoResumo | null; ultimoPedido: PedidoResumo | null; ultimasInteracoes: HistoricoEvento[];
}

export interface ObservacaoCliente { id: string; texto: string; criadoEm: string; autor: string; autorIniciais: string }

export interface Personalizacao { tipoPersonalizacao: string; corPeca: string; coresArte: string | null; medidas: string | null; localAplicacao: string | null; referenciaArte: string | null; observacoesTecnicas: string | null }

export interface ItemDetalhe {
  id: string; ordem: number; produtoId: string; produtoNome: string; descricao: string | null; quantidade: number; valorUnitario: number;
  valorPersonalizacaoUnitario: number; subtotalProduto: number; totalPersonalizacao: number; valorTotal: number;
  personalizacao: Personalizacao; anexos: Anexo[];
}

export interface ResumoFinanceiro { quantidadeItens: number; totalUnidades: number; subtotalProdutos: number; valorPersonalizacao: number; desconto: number; valorTotal: number }

export interface OrcamentoListaItem { id: string; numero: number; clienteId: string; cliente: string; data: string; validade: string; quantidadeItens: number; valorTotal: number; responsavelId: string; responsavel: string; situacao: SituacaoOrcamento }

export interface Decisao { resultado: ResultadoDecisao; meioContato: MeioContato | null; dataHora: string; motivo: string | null; observacao: string | null; registradoPor: string; automatica: boolean }
export interface PedidoVinculado { id: string; numero: number; situacao: SituacaoPedido; criadoEm: string; prazoEntrega: string }

export interface OrcamentoDetalhe {
  id: string; numero: number; situacao: SituacaoOrcamento; cliente: ClienteCard; dataOrcamento: string; criadoEm: string; validade: string;
  prazoEstimadoDiasUteis: number; previsaoEntrega: string; condicaoPagamento: string; observacoesComerciais: string | null;
  responsavelId: string; responsavel: string; itens: ItemDetalhe[]; resumo: ResumoFinanceiro; decisao: Decisao | null;
  pedido: PedidoVinculado | null; aguardandoRetornoDesde: string | null; dataUltimaEdicao: string | null; podeSerEditado: boolean; versao: number;
}

export interface ItemOrcamentoRequisicao {
  id?: string | null; produtoId: string | null; descricao: string; quantidade: number | null; valorUnitario: number | null;
  valorPersonalizacaoUnitario: number | null; tipoPersonalizacao: string; corPeca: string; coresArte: string; medidas: string;
  localAplicacao: string; referenciaArte: string; observacoesTecnicas: string; anexoIds: string[];
}

export interface OrcamentoRequisicao {
  clienteId: string | null; responsavelId: string | null; validade: string | null; prazoEstimadoDiasUteis: number | null;
  condicaoPagamento: string; observacoesComerciais: string; desconto: number | null; itens: ItemOrcamentoRequisicao[]; versao?: number;
}

export interface Calculo {
  itens: { subtotalProduto: number; totalPersonalizacao: number; valorTotal: number }[];
  quantidadeItens: number; totalUnidades: number; subtotalProdutos: number; valorPersonalizacao: number; desconto: number;
  valorTotal: number; descontoExcedeValor: boolean;
}

export interface Contato { id: string; tipo: MeioContato; dataHora: string; observacao: string; registradoPor: string; criadoEm: string }
export interface DecisaoResultado { orcamento: OrcamentoDetalhe; pedidoGerado: PedidoVinculado | null }

export interface PedidoListaItem {
  id: string; numero: number; orcamentoId: string; orcamentoNumero: number; clienteId: string; cliente: string; data: string;
  valorTotal: number; prazoEntrega: string; responsavelId: string; responsavel: string; situacao: SituacaoPedido; criadoEm: string;
}

export interface PedidoDetalhe {
  id: string; numero: number; situacao: SituacaoPedido; cliente: ClienteCard;
  origem: { orcamentoId: string; orcamentoNumero: number; validadeOrcamento: string; aprovadoEm: string | null; meioContato: MeioContato | null; decisaoRegistradaPor: string | null };
  dataPedido: string; criadoEm: string; prazoEntrega: string; prazoEstimadoDiasUteis: number; condicaoPagamento: string;
  observacoesComerciais: string | null; observacaoDecisao: string | null; responsavelId: string; responsavel: string;
  itens: ItemDetalhe[]; resumo: ResumoFinanceiro; proximaSituacao: SituacaoPedido | null; podeCancelar: boolean;
  entregueEm: string | null; canceladoEm: string | null; motivoCancelamento: string | null; versao: number;
}

export interface Dashboard {
  indicadores: {
    orcamentosEmElaboracao: number; orcamentosCriadosHoje: number; aguardandoRetorno: number; aguardandoVencemEm3Dias: number;
    aprovadosNoMes: number; valorAprovadoNoMes: number; pedidosEmAndamento: number; pedidosConcluidosNoMes: number; mesReferencia: string;
  };
  orcamentosRecentes: OrcamentoListaItem[];
  aguardandoRetorno: { id: string; numero: number; cliente: string; valorTotal: number; apresentadoEm: string | null; validade: string; diasParaVencer: number }[];
  totalAguardandoRetorno: number;
}
