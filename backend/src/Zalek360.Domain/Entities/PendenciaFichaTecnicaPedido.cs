namespace Zalek360.Domain.Entities;

/// <summary>Uma falta de ficha nao possui materia-prima para referenciar: fica vinculada ao produto do pedido.</summary>
public sealed class PendenciaFichaTecnicaPedido : Zalek360.Domain.Common.Entidade
{
    public Guid PedidoId { get; private set; }
    public Guid ProdutoId { get; private set; }
    public int QuantidadeProduto { get; private set; }
    public DateTime CriadaEm { get; private set; }
    public DateTime? ResolvidaEm { get; private set; }
    private PendenciaFichaTecnicaPedido() { }
    public static PendenciaFichaTecnicaPedido Criar(Guid pedidoId, Guid produtoId, int quantidade, DateTime agora) => new()
    { PedidoId = pedidoId, ProdutoId = produtoId, QuantidadeProduto = quantidade, CriadaEm = agora };
    public void Resolver(DateTime agora) => ResolvidaEm = agora;
}
