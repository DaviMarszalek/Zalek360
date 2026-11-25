namespace Zalek360.Application.Dtos;

public sealed record ItemAnaliseRequest(Guid? Id, Guid? ProdutoId, int? Quantidade, decimal? ValorUnitario, decimal? ValorPersonalizacaoUnitario);
public sealed record AnaliseOrcamentoRequest(Guid? OrcamentoId, IReadOnlyList<ItemAnaliseRequest>? Itens, decimal? Desconto);
public sealed record ItemAnaliseRegistrado(Guid Id, Guid ProdutoId, int Quantidade, decimal ValorUnitario, decimal ValorPersonalizacaoUnitario, decimal? CustoUnitarioInterno);
public sealed record OrcamentoAnaliseRegistrado(decimal Desconto, Guid? PedidoId, IReadOnlyList<ItemAnaliseRegistrado> Itens);
public sealed record MaterialDisponibilidadeDto(Guid Id, string Nome, string Unidade, decimal Necessario, decimal Disponivel, decimal Falta, bool Ativo);
public sealed record ItemAnaliseDto(int Indice, Guid? ItemId, Guid? ProdutoId, decimal? CustoUnitario, decimal? CustoTotal,
    bool SemFicha, IReadOnlyList<MaterialDisponibilidadeDto> Materiais);
public sealed record AnaliseOrcamentoDto(decimal Receita, decimal? CustoTotal, decimal? MargemValor, decimal? MargemPercentual,
    bool Completa, IReadOnlyList<ItemAnaliseDto> Itens);
