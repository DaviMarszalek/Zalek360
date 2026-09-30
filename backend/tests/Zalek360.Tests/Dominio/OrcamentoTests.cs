using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Domain.Services;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Dominio;

public class OrcamentoTests
{
    [Fact]
    public void Calcula_total_do_orcamento_123_do_prototipo()
    {
        var r = CalculadoraOrcamento.Calcular(new[]
        {
            new LinhaCalculo(150, 38m, 8m), new LinhaCalculo(150, 22m, 6m), new LinhaCalculo(60, 24m, 5m)
        }, 0m);
        Assert.Equal(12840m, r.ValorTotal);
        Assert.Equal(360, r.TotalUnidades);
        Assert.Equal(6900m, r.Linhas[0].ValorTotal);
    }

    [Fact]
    public void Aplica_desconto_no_total()
    {
        var r = CalculadoraOrcamento.Calcular(new[] { new LinhaCalculo(10, 20m, 5m) }, 30m);
        Assert.Equal(200m, r.SubtotalProdutos);
        Assert.Equal(50m, r.ValorPersonalizacao);
        Assert.Equal(220m, r.ValorTotal);
    }

    [Fact]
    public void Orcamento_nasce_em_elaboracao_com_total_calculado_pelo_sistema()
    {
        var o = Dados.Orcamento();
        Assert.Equal(SituacaoOrcamento.EmElaboracao, o.Situacao);
        Assert.Equal(6900m, o.ValorTotal);
        Assert.Contains(o.ExtrairHistoricoPendente(), e => e.Tipo == TipoEventoHistorico.OrcamentoCriado);
    }

    [Fact]
    public void Orcamento_sem_item_nao_e_criado()
    {
        var ex = Assert.Throws<DomainException>(() => Orcamento.Criar(1, Guid.NewGuid(), Dados.UsuarioId, Dados.Hoje,
            Dados.Comercial(), Array.Empty<DadosItemOrcamento>(), Dados.Agora, Dados.UsuarioId));
        Assert.Equal(CodigosErro.OrcamentoSemItens, ex.Codigo);
        Assert.Equal(Orcamento.MensagemSemItens, ex.Message);
    }

    [Fact]
    public void Desconto_maior_que_valor_dos_itens_e_rejeitado()
    {
        var erros = Orcamento.Validar(Dados.Hoje, Dados.Comercial(desconto: 999_999m), new[] { Dados.Item(Guid.NewGuid()) });
        Assert.True(erros.ContainsKey("desconto"));
    }

    [Fact]
    public void Registrar_contato_nao_altera_status()
    {
        var o = Dados.OrcamentoAguardando();
        o.RegistrarContato(MeioContato.WhatsApp, Dados.Agora, Dados.Hoje, "Cliente pediu mais prazo.", Dados.Agora, Dados.UsuarioId);
        Assert.Equal(SituacaoOrcamento.AguardandoRetorno, o.Situacao);
        Assert.Single(o.Contatos);
        Assert.Contains(o.ExtrairHistoricoPendente(), e => e.Tipo == TipoEventoHistorico.ContatoRegistrado);
    }

    [Fact]
    public void Decisao_exige_meio_de_contato_e_data_hora()
    {
        var o = Dados.OrcamentoAguardando();
        var ex = Assert.Throws<DomainException>(() =>
            o.RegistrarDecisao(ResultadoDecisao.Aprovado, null, null, null, null, null, Dados.Agora, Dados.UsuarioId));
        Assert.Equal(CodigosErro.DecisaoDadosObrigatorios, ex.Codigo);
        Assert.True(ex.Erros.ContainsKey("meioContato"));
        Assert.True(ex.Erros.ContainsKey("dataHora"));
        Assert.Equal(SituacaoOrcamento.AguardandoRetorno, o.Situacao);
    }

    [Fact]
    public void Aprovacao_exige_aguardando_retorno()
    {
        var o = Dados.Orcamento();
        var ex = Assert.Throws<DomainException>(() => o.RegistrarDecisao(ResultadoDecisao.Aprovado, MeioContato.WhatsApp,
            Dados.Agora, Dados.Hoje, null, null, Dados.Agora, Dados.UsuarioId));
        Assert.Equal(CodigosErro.OrcamentoTransicaoInvalida, ex.Codigo);
    }

    [Fact]
    public void Aprovacao_apos_a_validade_e_rejeitada()
    {
        var o = Dados.OrcamentoAguardando();
        var depois = Dados.Agora.AddDays(20);
        var ex = Assert.Throws<DomainException>(() => o.RegistrarDecisao(ResultadoDecisao.Aprovado, MeioContato.Ligacao,
            depois, Dados.Hoje.AddDays(20), null, null, depois, Dados.UsuarioId));
        Assert.Equal(CodigosErro.OrcamentoValidadeVencida, ex.Codigo);
    }

    [Fact]
    public void Recusado_nao_gera_pedido()
    {
        var o = Dados.OrcamentoAguardando();
        o.RegistrarDecisao(ResultadoDecisao.Recusado, MeioContato.Ligacao, Dados.Agora, Dados.Hoje, "Preço", null, Dados.Agora, Dados.UsuarioId);
        Assert.Equal(SituacaoOrcamento.Recusado, o.Situacao);
        var ex = Assert.Throws<DomainException>(() => Pedido.CriarAPartirDe(o, 57, Dados.Hoje, Dados.Agora, Dados.UsuarioId));
        Assert.Equal(CodigosErro.PedidoOrigemInvalida, ex.Codigo);
    }

    [Fact]
    public void Cancelado_pode_ocorrer_em_elaboracao_e_nao_gera_pedido()
    {
        var o = Dados.Orcamento();
        o.RegistrarDecisao(ResultadoDecisao.Cancelado, MeioContato.WhatsApp, Dados.Agora, Dados.Hoje, "Desistiu da compra", null, Dados.Agora, Dados.UsuarioId);
        Assert.Equal(SituacaoOrcamento.Cancelado, o.Situacao);
        Assert.Throws<DomainException>(() => Pedido.CriarAPartirDe(o, 57, Dados.Hoje, Dados.Agora, Dados.UsuarioId));
    }

    [Fact]
    public void Nao_aceita_segunda_decisao_nem_edicao_apos_decidido()
    {
        var o = Dados.OrcamentoAguardando();
        o.RegistrarDecisao(ResultadoDecisao.Recusado, MeioContato.Email, Dados.Agora, Dados.Hoje, null, null, Dados.Agora, Dados.UsuarioId);
        var segunda = Assert.Throws<DomainException>(() => o.RegistrarDecisao(ResultadoDecisao.Aprovado, MeioContato.Email,
            Dados.Agora, Dados.Hoje, null, null, Dados.Agora, Dados.UsuarioId));
        Assert.Equal(CodigosErro.OrcamentoJaDecidido, segunda.Codigo);
        var edicao = Assert.Throws<DomainException>(() => o.Atualizar(Dados.UsuarioId, Dados.Comercial(), new[] { Dados.Item(Guid.NewGuid()) },
            Dados.Agora, Dados.UsuarioId));
        Assert.Equal(CodigosErro.OrcamentoNaoEditavel, edicao.Codigo);
    }

    [Fact]
    public void Alterar_quantidade_recalcula_e_registra_no_historico()
    {
        var produto = Guid.NewGuid();
        var o = Dados.Orcamento(null, Dados.Item(produto, quantidade: 120));
        o.ExtrairHistoricoPendente();
        var item = o.Itens.Single();
        o.Atualizar(Dados.UsuarioId, Dados.Comercial(), new[] { Dados.Item(produto, quantidade: 150, id: item.Id) }, Dados.Agora.AddHours(1), Dados.UsuarioId);

        Assert.Equal(150, o.Itens.Single().Quantidade);
        Assert.Equal(6900m, o.ValorTotal);
        Assert.Contains(o.ExtrairHistoricoPendente(), e => e.Titulo == "Quantidade alterada de 120 para 150");
    }

    [Fact]
    public void Expira_automaticamente_apenas_quando_aguardando_e_vencido()
    {
        var o = Dados.OrcamentoAguardando();
        Assert.False(o.DeveExpirar(o.Validade));
        var diaSeguinte = o.Validade.AddDays(1);
        Assert.True(o.ExpirarAutomaticamente(diaSeguinte, Dados.Agora.AddDays(16), Dados.Agora.AddDays(16)));
        Assert.Equal(SituacaoOrcamento.Expirado, o.Situacao);
        Assert.True(o.Decisao!.Automatica);

        var emElaboracao = Dados.Orcamento();
        Assert.False(emElaboracao.ExpirarAutomaticamente(diaSeguinte, Dados.Agora, Dados.Agora));
    }

    [Fact]
    public void Nao_marca_aguardando_retorno_com_validade_vencida()
    {
        var o = Dados.Orcamento();
        var ex = Assert.Throws<DomainException>(() =>
            o.MarcarAguardandoRetorno(MeioContato.Email, o.Validade.AddDays(1), Dados.Agora, Dados.UsuarioId));
        Assert.Equal(CodigosErro.OrcamentoValidadeVencida, ex.Codigo);
    }
}
