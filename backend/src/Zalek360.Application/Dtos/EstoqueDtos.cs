using Zalek360.Domain.Entities;

namespace Zalek360.Application.Dtos;

public sealed record MateriaPrimaDto(Guid Id, string Nome, string Unidade, decimal QuantidadeDisponivel, decimal EstoqueMinimo,
    decimal QuantidadeAReceber, decimal QuantidadeReservada, bool Ativo);
public sealed record MateriaPrimaRequest(string? Nome, string? Unidade, decimal QuantidadeDisponivel, decimal EstoqueMinimo);
public sealed record CompraMateriaPrimaDto(Guid Id, Guid MateriaPrimaId, string MateriaPrimaNome, string Unidade, decimal Quantidade,
    string? Fornecedor, DateOnly DataPrevista, SituacaoCompraMateriaPrima Situacao, DateTime CriadaEm);
public sealed record CompraMateriaPrimaRequest(Guid MateriaPrimaId, decimal Quantidade, string? Fornecedor, DateOnly DataPrevista);
public sealed record SituacaoCompraRequest(SituacaoCompraMateriaPrima Situacao);
public sealed record FichaTecnicaLinhaDto(Guid MateriaPrimaId, string MateriaPrimaNome, string Unidade, decimal QuantidadePorUnidade);
public sealed record FichaTecnicaDto(Guid ProdutoId, string ProdutoNome, IReadOnlyList<FichaTecnicaLinhaDto> Materiais);
public sealed record FichaTecnicaLinhaRequest(Guid MateriaPrimaId, decimal QuantidadePorUnidade);
public sealed record FichaTecnicaRequest(Guid ProdutoId, IReadOnlyList<FichaTecnicaLinhaRequest> Materiais);
public sealed record ReservaPedidoDto(Guid Id, Guid MateriaPrimaId, string MateriaPrimaNome, string Unidade, decimal Quantidade,
    string Situacao);
public sealed record ReservaPedidoRequest(Guid MateriaPrimaId, decimal Quantidade);
public sealed record MovimentoMateriaPrimaDto(Guid Id, Guid MateriaPrimaId, string MateriaPrimaNome, Guid? PedidoId,
    int? PedidoNumero, decimal Variacao, TipoMovimentoMateriaPrima Tipo, string? Observacao, DateTime CriadoEm);
