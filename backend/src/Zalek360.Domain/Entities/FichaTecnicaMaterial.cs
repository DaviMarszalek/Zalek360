namespace Zalek360.Domain.Entities;

public sealed class FichaTecnicaMaterial : Zalek360.Domain.Common.Entidade
{
    public Guid ProdutoId { get; private set; }
    public Guid MateriaPrimaId { get; private set; }
    public decimal QuantidadePorUnidade { get; private set; }
    private FichaTecnicaMaterial() { }
    public static FichaTecnicaMaterial Criar(Guid produtoId, Guid materiaPrimaId, decimal quantidade) => new()
    { ProdutoId = produtoId, MateriaPrimaId = materiaPrimaId, QuantidadePorUnidade = quantidade };
    public void AtualizarQuantidade(decimal quantidade) => QuantidadePorUnidade = quantidade;
}
