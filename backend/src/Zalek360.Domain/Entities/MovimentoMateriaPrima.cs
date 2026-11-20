namespace Zalek360.Domain.Entities;

public enum TipoMovimentoMateriaPrima { EntradaCompra, SaidaPedido, Ajuste, Reserva, LiberacaoReserva }

public sealed class MovimentoMateriaPrima : Zalek360.Domain.Common.Entidade
{
    public Guid MateriaPrimaId { get; private set; }
    public MateriaPrima MateriaPrima { get; private set; } = null!;
    public Guid? PedidoId { get; private set; }
    public Pedido? Pedido { get; private set; }
    public decimal Variacao { get; private set; }
    public TipoMovimentoMateriaPrima Tipo { get; private set; }
    public string? Observacao { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public decimal? SaldoFisicoApos { get; private set; }
    public decimal? SaldoReservadoApos { get; private set; }
    public Guid? UsuarioId { get; private set; }
    private MovimentoMateriaPrima() { }
    public static MovimentoMateriaPrima Criar(Guid materialId, Guid? pedidoId, decimal variacao, TipoMovimentoMateriaPrima tipo, string? observacao, DateTime agora,
        decimal? saldoFisicoApos = null, decimal? saldoReservadoApos = null, Guid? usuarioId = null) => new()
    { MateriaPrimaId = materialId, PedidoId = pedidoId, Variacao = variacao, Tipo = tipo, Observacao = observacao, CriadoEm = agora,
        SaldoFisicoApos = saldoFisicoApos, SaldoReservadoApos = saldoReservadoApos, UsuarioId = usuarioId };
}
