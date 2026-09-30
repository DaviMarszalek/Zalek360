using Zalek360.Domain.Enums;

namespace Zalek360.Application.Dtos;

public sealed record ItemOrcamentoRequest(
    Guid? Id,
    Guid? ProdutoId,
    string? Descricao,
    int? Quantidade,
    decimal? ValorUnitario,
    decimal? ValorPersonalizacaoUnitario,
    string? TipoPersonalizacao,
    string? CorPeca,
    string? CoresArte,
    string? Medidas,
    string? LocalAplicacao,
    string? ReferenciaArte,
    string? ObservacoesTecnicas,
    IReadOnlyList<Guid>? AnexoIds);

/// <summary>Os totais NÃO fazem parte do contrato de entrada: o backend sempre recalcula (evita overposting).</summary>
public sealed record OrcamentoRequest(
    Guid? ClienteId,
    Guid? ResponsavelId,
    DateOnly? Validade,
    int? PrazoEstimadoDiasUteis,
    string? CondicaoPagamento,
    string? ObservacoesComerciais,
    decimal? Desconto,
    IReadOnlyList<ItemOrcamentoRequest>? Itens,
    int? Versao);

public sealed record CalculoItemRequest(int? Quantidade, decimal? ValorUnitario, decimal? ValorPersonalizacaoUnitario);

public sealed record CalculoRequest(IReadOnlyList<CalculoItemRequest>? Itens, decimal? Desconto);

public sealed record CalculoItemDto(decimal SubtotalProduto, decimal TotalPersonalizacao, decimal ValorTotal);

public sealed record CalculoDto(
    IReadOnlyList<CalculoItemDto> Itens, int QuantidadeItens, int TotalUnidades, decimal SubtotalProdutos,
    decimal ValorPersonalizacao, decimal Desconto, decimal ValorTotal, bool DescontoExcedeValor);

public sealed record OrcamentoFiltro(
    string? Busca, SituacaoOrcamento? Situacao, Guid? ResponsavelId, Guid? ClienteId,
    DateOnly? De, DateOnly? Ate, int? Pagina, int? TamanhoPagina);

public sealed record OrcamentoListaItemDto(
    Guid Id, int Numero, Guid ClienteId, string Cliente, DateOnly Data, DateOnly Validade, int QuantidadeItens,
    decimal ValorTotal, Guid ResponsavelId, string Responsavel, SituacaoOrcamento Situacao);

public sealed record DecisaoDto(
    ResultadoDecisao Resultado, MeioContato? MeioContato, DateTime DataHora, string? Motivo, string? Observacao,
    string RegistradoPor, bool Automatica);

public sealed record PedidoVinculadoDto(Guid Id, int Numero, SituacaoPedido Situacao, DateTime CriadoEm, DateOnly PrazoEntrega);

public sealed record OrcamentoDetalheDto(
    Guid Id,
    int Numero,
    SituacaoOrcamento Situacao,
    ClienteCardDto Cliente,
    DateOnly DataOrcamento,
    DateTime CriadoEm,
    DateOnly Validade,
    int PrazoEstimadoDiasUteis,
    DateOnly PrevisaoEntrega,
    string CondicaoPagamento,
    string? ObservacoesComerciais,
    Guid ResponsavelId,
    string Responsavel,
    IReadOnlyList<ItemDetalheDto> Itens,
    ResumoFinanceiroDto Resumo,
    DecisaoDto? Decisao,
    PedidoVinculadoDto? Pedido,
    DateTime? AguardandoRetornoDesde,
    DateTime? DataUltimaEdicao,
    bool PodeSerEditado,
    int Versao);

public sealed record AguardandoRetornoRequest(MeioContato? MeioApresentacao, int? Versao);

public sealed record ContatoRequest(MeioContato? Tipo, DateTimeOffset? DataHora, string? Observacao);

public sealed record ContatoDto(Guid Id, MeioContato Tipo, DateTime DataHora, string Observacao, string RegistradoPor, DateTime CriadoEm);

public sealed record DecisaoRequest(
    ResultadoDecisao? Resultado, MeioContato? MeioContato, DateTimeOffset? DataHora, string? Motivo, string? Observacao, int? Versao);

public sealed record DecisaoResultadoDto(OrcamentoDetalheDto Orcamento, PedidoVinculadoDto? PedidoGerado);

/// <summary>Opções exibidas no formulário de orçamento (nada fica fixo no frontend).</summary>
public sealed record OpcoesOrcamentoDto(
    IReadOnlyList<string> ExtensoesPermitidas, long TamanhoMaximoBytes,
    IReadOnlyList<string> TiposPersonalizacao, IReadOnlyList<string> CondicoesPagamento);

public static class OpcoesOrcamento
{
    public static readonly string[] TiposPersonalizacao =
    {
        "Serigrafia (frente)", "Serigrafia (frente e costas)", "Serigrafia (1 lado)", "Bordado",
        "Sublimação", "Gravação a laser", "DTF", "Transfer"
    };

    public static readonly string[] CondicoesPagamento =
    {
        "50% na aprovação + 50% na entrega (PIX)", "50% na aprovação + 50% na entrega (boleto)",
        "100% na aprovação (PIX)", "À vista (PIX)", "30 dias (boleto)", "30/60 dias (boleto)"
    };
}
