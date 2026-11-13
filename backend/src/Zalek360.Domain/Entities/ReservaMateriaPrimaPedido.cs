namespace Zalek360.Domain.Entities;

public enum SituacaoReservaMaterial { Reservada, Consumida, Liberada }

public sealed class ReservaMateriaPrimaPedido : Zalek360.Domain.Common.Entidade
{
    public Guid PedidoId { get; private set; }
    public Guid MateriaPrimaId { get; private set; }
    public MateriaPrima MateriaPrima { get; private set; } = null!;
    public decimal Quantidade { get; private set; }
    public decimal QuantidadeReservada { get; private set; }
    public decimal QuantidadePendente => Situacao == SituacaoReservaMaterial.Reservada ? Quantidade - QuantidadeReservada : 0;
    public int Versao { get; private set; }
    public SituacaoReservaMaterial Situacao { get; private set; }
    public DateTime CriadaEm { get; private set; }
    private ReservaMateriaPrimaPedido() { }
    public static ReservaMateriaPrimaPedido Criar(Guid pedidoId, Guid materialId, decimal quantidade, DateTime agora, decimal quantidadeReservada) => new()
    { PedidoId = pedidoId, MateriaPrimaId = materialId, Quantidade = quantidade, QuantidadeReservada = quantidadeReservada, Situacao = SituacaoReservaMaterial.Reservada, CriadaEm = agora };
    public void Ajustar(decimal quantidade, decimal reservada)
    {
        if (quantidade <= 0 || reservada < 0 || reservada > quantidade) throw new ArgumentOutOfRangeException(nameof(quantidade));
        Quantidade = quantidade; QuantidadeReservada = reservada; Versao++;
    }
    public void DefinirSituacao(SituacaoReservaMaterial situacao)
    {
        Situacao = situacao;
        if (situacao != SituacaoReservaMaterial.Reservada) QuantidadeReservada = 0;
        Versao++;
    }
}
