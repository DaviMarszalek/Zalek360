namespace Zalek360.Domain.Entities;

public enum SituacaoCompraMateriaPrima { Planejada, Pedido, Recebida, Cancelada }

public sealed class CompraMateriaPrima : Zalek360.Domain.Common.Entidade
{
    public Guid MateriaPrimaId { get; private set; }
    public MateriaPrima MateriaPrima { get; private set; } = null!;
    public decimal Quantidade { get; private set; }
    public string? Fornecedor { get; private set; }
    public DateOnly DataPrevista { get; private set; }
    public SituacaoCompraMateriaPrima Situacao { get; private set; } = SituacaoCompraMateriaPrima.Planejada;
    public DateTime CriadaEm { get; private set; }
    private CompraMateriaPrima() { }
    public static CompraMateriaPrima Criar(Guid materiaPrimaId, decimal quantidade, string? fornecedor, DateOnly prevista, DateTime agora) => new()
    { MateriaPrimaId = materiaPrimaId, Quantidade = quantidade, Fornecedor = string.IsNullOrWhiteSpace(fornecedor) ? null : fornecedor.Trim(), DataPrevista = prevista, CriadaEm = agora };
    public void DefinirSituacao(SituacaoCompraMateriaPrima situacao) => Situacao = situacao;
}
