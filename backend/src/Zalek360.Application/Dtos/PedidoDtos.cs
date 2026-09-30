using Zalek360.Domain.Enums;

namespace Zalek360.Application.Dtos;

public sealed record PedidoFiltro(
    string? Busca, SituacaoPedido? Situacao, Guid? ResponsavelId, DateOnly? De, DateOnly? Ate, int? Pagina, int? TamanhoPagina);

public sealed record PedidoListaItemDto(
    Guid Id, int Numero, Guid OrcamentoId, int OrcamentoNumero, Guid ClienteId, string Cliente, DateOnly Data,
    decimal ValorTotal, DateOnly PrazoEntrega, Guid ResponsavelId, string Responsavel, SituacaoPedido Situacao, DateTime CriadoEm);

public sealed record OrigemPedidoDto(
    Guid OrcamentoId, int OrcamentoNumero, DateOnly ValidadeOrcamento, DateTime? AprovadoEm, MeioContato? MeioContato, string? DecisaoRegistradaPor);

public sealed record PedidoDetalheDto(
    Guid Id,
    int Numero,
    SituacaoPedido Situacao,
    ClienteCardDto Cliente,
    OrigemPedidoDto Origem,
    DateOnly DataPedido,
    DateTime CriadoEm,
    DateOnly PrazoEntrega,
    int PrazoEstimadoDiasUteis,
    string CondicaoPagamento,
    string? ObservacoesComerciais,
    string? ObservacaoDecisao,
    Guid ResponsavelId,
    string Responsavel,
    IReadOnlyList<ItemDetalheDto> Itens,
    ResumoFinanceiroDto Resumo,
    SituacaoPedido? ProximaSituacao,
    bool PodeCancelar,
    DateTime? EntregueEm,
    DateTime? CanceladoEm,
    string? MotivoCancelamento,
    int Versao);

public sealed record AtualizarSituacaoPedidoRequest(SituacaoPedido? NovaSituacao, string? Observacao, int? Versao);
