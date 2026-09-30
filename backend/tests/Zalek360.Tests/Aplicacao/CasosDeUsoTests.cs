using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Services;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Aplicacao;

public class ClienteServiceTests
{
    [Fact]
    public async Task Cadastra_cliente_e_observacao_inicial()
    {
        var c = new Cenario();
        var d = Dados.ClientePj();
        var r = new ClienteRequest(d.TipoPessoa, "38.221.907/0001-53", d.NomeRazaoSocial, d.NomeFantasia, d.ContatoNome, d.ContatoCargo,
            "(47) 3000-1122", "(47) 99988-7766", d.Email, "89201-000", d.Logradouro, d.Numero, null, d.Bairro, d.Cidade, d.Uf,
            "Prefere contato pelo WhatsApp.", null);

        var criado = await c.Clientes().CriarAsync(r, CancellationToken.None);

        Assert.Equal("38221907000153", criado.Documento);
        Assert.Equal("47999887766", criado.WhatsApp);
        Assert.Single(c.Banco.Observacoes);
    }

    [Fact]
    public async Task Cpf_cnpj_duplicado_retorna_conflito_com_cliente_existente()
    {
        var c = new Cenario();
        var d = Dados.ClientePj(Dados.CnpjMetalurgica);
        var r = new ClienteRequest(d.TipoPessoa, "12.345.678/0001-95", "Outra Empresa", null, null, null, "4730001122", null,
            null, null, null, null, null, null, "Joinville", "SC", null, null);

        var ex = await Assert.ThrowsAsync<ConflitoException>(() => c.Clientes().CriarAsync(r, CancellationToken.None));
        Assert.Equal(CodigosErro.ClienteDocumentoDuplicado, ex.Codigo);
        Assert.Equal(409, ex.StatusCode);
        Assert.NotNull(ex.Detalhes);
        Assert.Single(c.Banco.Clientes);
    }

    [Fact]
    public async Task Verificacao_de_documento_informa_invalido_disponivel_e_existente()
    {
        var c = new Cenario();
        var s = c.Clientes();
        Assert.False((await s.VerificarDocumentoAsync("38.221.907/0001-40", TipoPessoa.PessoaJuridica, null, CancellationToken.None)).Valido);
        var livre = await s.VerificarDocumentoAsync("38.221.907/0001-53", TipoPessoa.PessoaJuridica, null, CancellationToken.None);
        Assert.True(livre.Valido && livre.Disponivel);
        var existente = await s.VerificarDocumentoAsync("12.345.678/0001-95", null, null, CancellationToken.None);
        Assert.False(existente.Disponivel);
        Assert.Equal(c.Metalurgica.Id, existente.ClienteExistente!.Id);
    }
}

public class OrcamentoServiceTests
{
    [Fact]
    public async Task Cria_orcamento_com_numero_sequencial_e_total_recalculado()
    {
        var c = new Cenario();
        var dto = await c.Orcamentos().CriarAsync(Cenario.Requisicao(c.Metalurgica.Id, c.Polo.Id, Cenario.ItemReq(c.Polo.Id)), CancellationToken.None);
        Assert.Equal(124, dto.Numero);
        Assert.Equal(SituacaoOrcamento.EmElaboracao, dto.Situacao);
        Assert.Equal(6900m, dto.Resumo.ValorTotal);
    }

    [Fact]
    public async Task Orcamento_sem_itens_retorna_erro_de_validacao_e_nao_consome_numero()
    {
        var c = new Cenario();
        var ex = await Assert.ThrowsAsync<ValidacaoException>(() =>
            c.Orcamentos().CriarAsync(Cenario.Requisicao(c.Metalurgica.Id, c.Polo.Id), CancellationToken.None));
        Assert.Equal(CodigosErro.OrcamentoSemItens, ex.Codigo);
        Assert.Equal(124, c.Numerador.ProximoOrcamento);
        Assert.Empty(c.Banco.Orcamentos);
    }

    [Fact]
    public async Task Orcamento_exige_cliente_cadastrado()
    {
        var c = new Cenario();
        var ex = await Assert.ThrowsAsync<ValidacaoException>(() =>
            c.Orcamentos().CriarAsync(Cenario.Requisicao(Guid.NewGuid(), c.Polo.Id, Cenario.ItemReq(c.Polo.Id)), CancellationToken.None));
        Assert.True(ex.Erros.ContainsKey("clienteId"));
    }

    [Fact]
    public void Calculo_de_pre_visualizacao_nao_depende_do_banco()
    {
        var r = OrcamentoService.Calcular(new CalculoRequest(new[] { new CalculoItemRequest(150, 38m, 8m) }, 100m));
        Assert.Equal(6800m, r.ValorTotal);
        Assert.False(r.DescontoExcedeValor);
    }

    private static async Task<(Cenario C, OrcamentoDetalheDto Orcamento)> Aguardando()
    {
        var c = new Cenario();
        var s = c.Orcamentos();
        var o = await s.CriarAsync(Cenario.Requisicao(c.Metalurgica.Id, c.Polo.Id, Cenario.ItemReq(c.Polo.Id)), CancellationToken.None);
        o = await s.MarcarAguardandoRetornoAsync(o.Id, new AguardandoRetornoRequest(MeioContato.WhatsApp, o.Versao), CancellationToken.None);
        return (c, o);
    }

    [Fact]
    public async Task Contato_registrado_nao_altera_status()
    {
        var (c, o) = await Aguardando();
        await c.Orcamentos().RegistrarContatoAsync(o.Id,
            new ContatoRequest(MeioContato.Ligacao, new DateTimeOffset(Dados.Agora), "Cliente pediu mais prazo."), CancellationToken.None);
        Assert.Equal(SituacaoOrcamento.AguardandoRetorno, c.Banco.Orcamentos.Single().Situacao);
        Assert.Contains(c.Banco.Historico, h => h.Tipo == TipoEventoHistorico.ContatoRegistrado);
    }

    [Fact]
    public async Task Aprovacao_gera_pedido_na_mesma_transacao()
    {
        var (c, o) = await Aguardando();
        var r = await c.Orcamentos().RegistrarDecisaoAsync(o.Id,
            new DecisaoRequest(ResultadoDecisao.Aprovado, MeioContato.WhatsApp, new DateTimeOffset(Dados.Agora), null, "Aprovado.", o.Versao),
            CancellationToken.None);

        Assert.Equal(SituacaoOrcamento.Aprovado, r.Orcamento.Situacao);
        Assert.NotNull(r.PedidoGerado);
        Assert.Equal(57, r.PedidoGerado!.Numero);
        var pedido = Assert.Single(c.Banco.Pedidos);
        Assert.Equal(o.Id, pedido.OrcamentoOrigemId);
        Assert.True(Assert.Single(c.Uow.Transacoes).Confirmada);
        Assert.Contains(c.Banco.Historico, h => h.Tipo == TipoEventoHistorico.PedidoGerado);
    }

    [Fact]
    public async Task Falha_ao_gerar_pedido_nao_confirma_a_transacao()
    {
        var (c, o) = await Aguardando();
        c.Numerador.FalharAoNumerarPedido = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Orcamentos().RegistrarDecisaoAsync(o.Id,
            new DecisaoRequest(ResultadoDecisao.Aprovado, MeioContato.WhatsApp, new DateTimeOffset(Dados.Agora), null, null, o.Versao),
            CancellationToken.None));

        var transacao = Assert.Single(c.Uow.Transacoes);
        Assert.False(transacao.Confirmada);
        Assert.True(transacao.Descartada); // descartar sem confirmar = ROLLBACK no banco real
        Assert.Empty(c.Banco.Pedidos);
    }

    [Fact]
    public async Task Recusado_e_cancelado_nao_geram_pedido()
    {
        var (c, o) = await Aguardando();
        var r = await c.Orcamentos().RegistrarDecisaoAsync(o.Id,
            new DecisaoRequest(ResultadoDecisao.Recusado, MeioContato.Ligacao, new DateTimeOffset(Dados.Agora), "Preço", null, o.Versao),
            CancellationToken.None);
        Assert.Equal(SituacaoOrcamento.Recusado, r.Orcamento.Situacao);
        Assert.Null(r.PedidoGerado);
        Assert.Empty(c.Banco.Pedidos);
    }

    [Fact]
    public async Task Versao_desatualizada_retorna_conflito()
    {
        var (c, o) = await Aguardando();
        var ex = await Assert.ThrowsAsync<ConflitoException>(() => c.Orcamentos().RegistrarDecisaoAsync(o.Id,
            new DecisaoRequest(ResultadoDecisao.Aprovado, MeioContato.WhatsApp, new DateTimeOffset(Dados.Agora), null, null, o.Versao - 1),
            CancellationToken.None));
        Assert.Equal(CodigosErro.ConflitoConcorrencia, ex.Codigo);
    }

    [Fact]
    public async Task Pedido_nao_pode_ser_criado_diretamente()
    {
        var ex = Assert.Throws<DomainException>(PedidoService.RejeitarCriacaoDireta);
        Assert.Equal("Pedido deve ser originado de um orçamento aprovado.", ex.Message);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Rotina_de_expiracao_expira_somente_vencidos()
    {
        var (c, o) = await Aguardando();
        c.Relogio.Avancar(TimeSpan.FromDays(10));
        Assert.Equal(0, await c.Expiracao().ExpirarVencidosAsync(CancellationToken.None));
        c.Relogio.Avancar(TimeSpan.FromDays(6));
        Assert.Equal(1, await c.Expiracao().ExpirarVencidosAsync(CancellationToken.None));
        Assert.Equal(SituacaoOrcamento.Expirado, c.Banco.Orcamentos.Single(x => x.Id == o.Id).Situacao);
    }
}
