using Zalek360.Domain.Common;
using Zalek360.Domain.Enums;
using Zalek360.Domain.Services;

namespace Zalek360.Domain.Entities;

/// <summary>
/// UC04 — Pedido. Não existe criação manual: a única forma de criar é <see cref="CriarAPartirDe"/>,
/// a partir de um orçamento Aprovado (RN8/RN9). Pedidos nunca são excluídos, apenas cancelados (RN10).
/// </summary>
public class Pedido : RaizAgregado
{
    private readonly List<ItemPedido> _itens = new();

    public int Numero { get; private set; }
    public Guid OrcamentoOrigemId { get; private set; }
    public Orcamento? OrcamentoOrigem { get; private set; }
    public Guid ClienteId { get; private set; }
    public Cliente? Cliente { get; private set; }
    public Guid ResponsavelId { get; private set; }
    public Usuario? Responsavel { get; private set; }
    public SituacaoPedido Situacao { get; private set; }
    public DateOnly DataPedido { get; private set; }
    public int PrazoEstimadoDiasUteis { get; private set; }
    public DateOnly PrazoEntrega { get; private set; }
    public string CondicaoPagamento { get; private set; } = string.Empty;
    public string? ObservacoesComerciais { get; private set; }
    public string? ObservacaoDecisao { get; private set; }
    public decimal SubtotalProdutos { get; private set; }
    public decimal ValorPersonalizacao { get; private set; }
    public decimal Desconto { get; private set; }
    public decimal ValorTotal { get; private set; }
    public DateTime? EntregueEm { get; private set; }
    public DateTime? CanceladoEm { get; private set; }
    public string? MotivoCancelamento { get; private set; }

    public IReadOnlyCollection<ItemPedido> Itens => _itens;

    private Pedido() { }

    public string NumeroFormatado => Formatos.NumeroDocumento(Numero);

    /// <summary>Fluxo previsto: Aberto → Em produção → Pronto → Entregue.</summary>
    public SituacaoPedido? ProximaSituacao => Situacao switch
    {
        SituacaoPedido.Aberto => SituacaoPedido.EmProducao,
        SituacaoPedido.EmProducao => SituacaoPedido.Pronto,
        SituacaoPedido.Pronto => SituacaoPedido.Entregue,
        _ => null
    };

    public bool PodeCancelar => Situacao is SituacaoPedido.Aberto or SituacaoPedido.EmProducao or SituacaoPedido.Pronto;

    /// <summary>Fábrica única: herda cliente, itens, personalização, artes, valores, prazo, condições e observações.</summary>
    public static Pedido CriarAPartirDe(Orcamento orcamento, int numero, DateOnly dataAprovacaoLocal, DateTime agoraUtc, Guid? usuarioId)
    {
        if (orcamento.Situacao != SituacaoOrcamento.Aprovado || orcamento.Decisao?.Resultado != ResultadoDecisao.Aprovado)
            throw new DomainException(CodigosErro.PedidoOrigemInvalida,
                "Pedido deve ser originado de um orçamento aprovado.", CategoriaErro.Conflito);

        var pedido = new Pedido
        {
            Numero = numero,
            OrcamentoOrigemId = orcamento.Id,
            ClienteId = orcamento.ClienteId,
            ResponsavelId = orcamento.ResponsavelId,
            Situacao = SituacaoPedido.Aberto,
            DataPedido = dataAprovacaoLocal,
            PrazoEstimadoDiasUteis = orcamento.PrazoEstimadoDiasUteis,
            PrazoEntrega = CalendarioDiasUteis.Adicionar(dataAprovacaoLocal, orcamento.PrazoEstimadoDiasUteis),
            CondicaoPagamento = orcamento.CondicaoPagamento,
            ObservacoesComerciais = orcamento.ObservacoesComerciais,
            ObservacaoDecisao = orcamento.Decisao.Observacao,
            SubtotalProdutos = orcamento.SubtotalProdutos,
            ValorPersonalizacao = orcamento.ValorPersonalizacao,
            Desconto = orcamento.Desconto,
            ValorTotal = orcamento.ValorTotal
        };
        foreach (var item in orcamento.Itens.OrderBy(i => i.Ordem))
            pedido._itens.Add(ItemPedido.CopiarDe(pedido.Id, item, agoraUtc));
        pedido.MarcarCriacao(agoraUtc, usuarioId);

        var meio = orcamento.Decisao.MeioContato is { } m ? $", aprovado via {Rotulos.De(m)}" : string.Empty;
        var momento = orcamento.Decisao.DataHora;
        pedido.RegistrarHistorico(pedido.Evento(TipoEventoHistorico.PedidoCriado, "Pedido criado automaticamente",
            $"A partir do orçamento {orcamento.NumeroFormatado}{meio}", momento, agoraUtc, null));
        pedido.RegistrarHistorico(pedido.Evento(TipoEventoHistorico.PedidoStatusInicial, $"Status inicial: {Rotulos.De(SituacaoPedido.Aberto)}",
            "Aguardando início da produção", momento, agoraUtc, null, nova: Rotulos.De(SituacaoPedido.Aberto)));

        orcamento.RegistrarPedidoGerado(numero, pedido.Id, agoraUtc);
        return pedido;
    }

    /// <summary>Evolui o status conforme o fluxo previsto, um passo por vez; cancelamento exige motivo.</summary>
    public void AtualizarSituacao(SituacaoPedido? nova, string? observacao, DateTime agoraUtc, Guid usuarioId)
    {
        if (nova is null)
            throw new DomainException(CodigosErro.PedidoTransicaoInvalida, "Selecione o novo status do pedido.", CategoriaErro.Validacao,
                new Dictionary<string, string[]> { ["novaSituacao"] = new[] { "Selecione o novo status do pedido." } });

        var texto = TextoOpcional.Limpar(observacao);
        if (texto?.Length > 500)
            throw new DomainException(CodigosErro.PedidoTransicaoInvalida, "Use no máximo 500 caracteres.", CategoriaErro.Validacao,
                new Dictionary<string, string[]> { ["observacao"] = new[] { "Use no máximo 500 caracteres." } });

        var anterior = Situacao;
        if (nova == SituacaoPedido.Cancelado)
        {
            if (!PodeCancelar)
                throw new DomainException(CodigosErro.PedidoTransicaoInvalida,
                    $"Um pedido {Rotulos.De(Situacao)} não pode ser cancelado.", CategoriaErro.Conflito);
            if (texto is null)
                throw new DomainException(CodigosErro.PedidoCancelamentoSemMotivo, "Informe o motivo do cancelamento.", CategoriaErro.Validacao,
                    new Dictionary<string, string[]> { ["observacao"] = new[] { "Informe o motivo do cancelamento." } });
            Situacao = SituacaoPedido.Cancelado;
            CanceladoEm = agoraUtc;
            MotivoCancelamento = texto;
            RegistrarHistorico(Evento(TipoEventoHistorico.PedidoCancelado, "Pedido cancelado", $"Motivo: {texto}",
                agoraUtc, agoraUtc, usuarioId, Rotulos.De(anterior), Rotulos.De(Situacao)));
        }
        else
        {
            if (ProximaSituacao != nova)
                throw new DomainException(CodigosErro.PedidoTransicaoInvalida,
                    ProximaSituacao is { } proxima
                        ? $"O pedido está {Rotulos.De(Situacao)}; o próximo status permitido é {Rotulos.De(proxima)}."
                        : $"Um pedido {Rotulos.De(Situacao)} não pode mudar de status.",
                    CategoriaErro.Conflito);
            Situacao = nova.Value;
            if (Situacao == SituacaoPedido.Entregue) EntregueEm = agoraUtc;
            RegistrarHistorico(Evento(TipoEventoHistorico.PedidoStatusAlterado, $"Status alterado para {Rotulos.De(Situacao)}",
                texto ?? $"De {Rotulos.De(anterior)} para {Rotulos.De(Situacao)}",
                agoraUtc, agoraUtc, usuarioId, Rotulos.De(anterior), Rotulos.De(Situacao)));
        }
        MarcarAlteracao(agoraUtc, usuarioId);
        IncrementarVersao();
    }

    private HistoricoEvento Evento(TipoEventoHistorico tipo, string titulo, string? descricao, DateTime ocorridoEm, DateTime registradoEm,
        Guid? usuarioId, string? anterior = null, string? nova = null) =>
        HistoricoEvento.Criar(EscopoHistorico.Pedido, tipo, ClienteId, OrcamentoOrigemId, Id, titulo, descricao,
            ocorridoEm, registradoEm, usuarioId, anterior, nova);
}
