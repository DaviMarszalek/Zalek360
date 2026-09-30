using Zalek360.Domain.Common;

namespace Zalek360.Domain.Entities;

/// <summary>Item do pedido: cópia fiel do item do orçamento aprovado (RF 4.1.3.1), com vínculo ao item de origem.</summary>
public class ItemPedido : Entidade
{
    private readonly List<ArquivoAnexo> _anexos = new();

    public Guid PedidoId { get; private set; }
    public Guid ItemOrcamentoOrigemId { get; private set; }
    public int Ordem { get; private set; }
    public Guid ProdutoId { get; private set; }
    public string ProdutoNome { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public int Quantidade { get; private set; }
    public decimal ValorUnitario { get; private set; }
    public decimal ValorPersonalizacaoUnitario { get; private set; }
    public decimal SubtotalProduto { get; private set; }
    public decimal TotalPersonalizacao { get; private set; }
    public decimal ValorTotal { get; private set; }
    public string TipoPersonalizacao { get; private set; } = string.Empty;
    public string CorPeca { get; private set; } = string.Empty;
    public string? CoresArte { get; private set; }
    public string? Medidas { get; private set; }
    public string? LocalAplicacao { get; private set; }
    public string? ReferenciaArte { get; private set; }
    public string? ObservacoesTecnicas { get; private set; }

    public IReadOnlyCollection<ArquivoAnexo> Anexos => _anexos;

    private ItemPedido() { }

    internal static ItemPedido CopiarDe(Guid pedidoId, ItemOrcamento origem, DateTime agoraUtc)
    {
        var item = new ItemPedido
        {
            PedidoId = pedidoId,
            ItemOrcamentoOrigemId = origem.Id,
            Ordem = origem.Ordem,
            ProdutoId = origem.ProdutoId,
            ProdutoNome = origem.ProdutoNome,
            Descricao = origem.Descricao,
            Quantidade = origem.Quantidade,
            ValorUnitario = origem.ValorUnitario,
            ValorPersonalizacaoUnitario = origem.ValorPersonalizacaoUnitario,
            SubtotalProduto = origem.SubtotalProduto,
            TotalPersonalizacao = origem.TotalPersonalizacao,
            ValorTotal = origem.ValorTotal,
            TipoPersonalizacao = origem.TipoPersonalizacao,
            CorPeca = origem.CorPeca,
            CoresArte = origem.CoresArte,
            Medidas = origem.Medidas,
            LocalAplicacao = origem.LocalAplicacao,
            ReferenciaArte = origem.ReferenciaArte,
            ObservacoesTecnicas = origem.ObservacoesTecnicas
        };
        foreach (var anexo in origem.Anexos)
            item._anexos.Add(anexo.CopiarParaItemPedido(item.Id, agoraUtc));
        return item;
    }
}
