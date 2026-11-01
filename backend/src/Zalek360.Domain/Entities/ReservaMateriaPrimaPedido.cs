namespace Zalek360.Domain.Entities;

public enum SituacaoReservaMaterial { Reservada, Consumida, Liberada }

public sealed class ReservaMateriaPrimaPedido : Zalek360.Domain.Common.Entidade
{
    public Guid PedidoId { get; private set; }
    public Guid MateriaPrimaId { get; private set; }
    public MateriaPrima MateriaPrima { get; private set; } = null!;
    public decimal Quantidade { get; private set; }
    public SituacaoReservaMaterial Situacao { get; private set; }
    public DateTime CriadaEm { get; private set; }
    private ReservaMateriaPrimaPedido() { }
    public static ReservaMateriaPrimaPedido Criar(Guid pedidoId, Guid materialId, decimal quantidade, DateTime agora) => new()
    { PedidoId = pedidoId, MateriaPrimaId = materialId, Quantidade = quantidade, Situacao = SituacaoReservaMaterial.Reservada, CriadaEm = agora };
    public void Ajustar(decimal quantidade) => Quantidade = quantidade;
    public void DefinirSituacao(SituacaoReservaMaterial situacao) => Situacao = situacao;
}
