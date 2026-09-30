using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Domain.Services;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Dominio;

public class PedidoTests
{
    private static (Orcamento Orcamento, Pedido Pedido, ArquivoAnexo Arte) Aprovar()
    {
        var arte = ArquivoAnexo.CriarPendente("logo-academia.pdf", 1_200_000, "2026/09/abc.pdf", Dados.Agora, Dados.UsuarioId);
        var o = Dados.OrcamentoAguardando(null, Dados.Item(Guid.NewGuid(), anexos: arte), Dados.Item(Guid.NewGuid(), 60, 24m, 5m));
        o.RegistrarDecisao(ResultadoDecisao.Aprovado, MeioContato.WhatsApp, Dados.Agora, Dados.Hoje, null, "Aprovado pelo Rafael.", Dados.Agora, Dados.UsuarioId);
        var p = Pedido.CriarAPartirDe(o, 57, Dados.Hoje, Dados.Agora, Dados.UsuarioId);
        return (o, p, arte);
    }

    [Fact]
    public void Pedido_herda_cliente_itens_personalizacao_valores_e_vinculo()
    {
        var (o, p, arte) = Aprovar();

        Assert.Equal(o.Id, p.OrcamentoOrigemId);
        Assert.Equal(o.ClienteId, p.ClienteId);
        Assert.Equal(SituacaoPedido.Aberto, p.Situacao);
        Assert.Equal(o.ValorTotal, p.ValorTotal);
        Assert.Equal(o.Desconto, p.Desconto);
        Assert.Equal(o.CondicaoPagamento, p.CondicaoPagamento);
        Assert.Equal("Aprovado pelo Rafael.", p.ObservacaoDecisao);
        Assert.Equal(2, p.Itens.Count);

        var itemOrigem = o.Itens.OrderBy(i => i.Ordem).First();
        var itemPedido = p.Itens.OrderBy(i => i.Ordem).First();
        Assert.Equal(itemOrigem.Id, itemPedido.ItemOrcamentoOrigemId);
        Assert.Equal(itemOrigem.TipoPersonalizacao, itemPedido.TipoPersonalizacao);
        Assert.Equal(itemOrigem.CorPeca, itemPedido.CorPeca);
        Assert.Equal(itemOrigem.ValorTotal, itemPedido.ValorTotal);

        var copia = Assert.Single(itemPedido.Anexos);
        Assert.Equal(arte.Id, copia.AnexoOrigemId);
        Assert.Equal(arte.ChaveArmazenamento, copia.ChaveArmazenamento);
        Assert.NotEqual(arte.Id, copia.Id);
    }

    [Fact]
    public void Prazo_de_entrega_conta_dias_uteis_a_partir_da_aprovacao()
    {
        var (_, p, _) = Aprovar();
        // 24/09/2026 (quinta) + 15 dias úteis = 15/10/2026 (como no protótipo).
        Assert.Equal(new DateOnly(2026, 10, 15), p.PrazoEntrega);
        Assert.Equal(new DateOnly(2026, 9, 28), CalendarioDiasUteis.Adicionar(new DateOnly(2026, 9, 25), 1));
    }

    [Fact]
    public void Geracao_do_pedido_registra_historico_nos_dois_lados()
    {
        var (o, p, _) = Aprovar();
        Assert.Contains(o.ExtrairHistoricoPendente(), e => e.Tipo == TipoEventoHistorico.PedidoGerado && e.PedidoId == p.Id);
        var eventosPedido = p.ExtrairHistoricoPendente();
        Assert.Contains(eventosPedido, e => e.Tipo == TipoEventoHistorico.PedidoCriado);
        Assert.Contains(eventosPedido, e => e.Tipo == TipoEventoHistorico.PedidoStatusInicial);
    }

    [Fact]
    public void Status_evolui_um_passo_por_vez()
    {
        var (_, p, _) = Aprovar();
        var pulo = Assert.Throws<DomainException>(() => p.AtualizarSituacao(SituacaoPedido.Entregue, null, Dados.Agora, Dados.UsuarioId));
        Assert.Equal(CodigosErro.PedidoTransicaoInvalida, pulo.Codigo);
        p.AtualizarSituacao(SituacaoPedido.EmProducao, null, Dados.Agora, Dados.UsuarioId);
        p.AtualizarSituacao(SituacaoPedido.Pronto, null, Dados.Agora, Dados.UsuarioId);
        p.AtualizarSituacao(SituacaoPedido.Entregue, null, Dados.Agora, Dados.UsuarioId);
        Assert.Equal(SituacaoPedido.Entregue, p.Situacao);
        Assert.NotNull(p.EntregueEm);
        Assert.False(p.PodeCancelar);
    }

    [Fact]
    public void Cancelamento_exige_motivo()
    {
        var (_, p, _) = Aprovar();
        var ex = Assert.Throws<DomainException>(() => p.AtualizarSituacao(SituacaoPedido.Cancelado, "  ", Dados.Agora, Dados.UsuarioId));
        Assert.Equal(CodigosErro.PedidoCancelamentoSemMotivo, ex.Codigo);
        p.AtualizarSituacao(SituacaoPedido.Cancelado, "Cliente desistiu.", Dados.Agora, Dados.UsuarioId);
        Assert.Equal("Cliente desistiu.", p.MotivoCancelamento);
    }
}
