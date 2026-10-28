using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Services;
using Zalek360.Domain.Enums;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Aplicacao;

public class OrcamentoDocumentoTests
{
    public static ClienteDetalheDto Cliente(string nome = "João & Cia Ltda.") => new(
        Guid.NewGuid(), TipoPessoa.PessoaJuridica, Dados.CnpjAcademia, nome, null, null, nome,
        "Rafael Costa", "Compras", "4730001122", "47999887766", "cliente@example.com",
        new(null, null, null, null, null, null, null), Dados.Agora, 0, 0, 1);

    public static OrcamentoDetalheDto Orcamento(int dias = 15, SituacaoOrcamento situacao = SituacaoOrcamento.AguardandoRetorno,
        decimal desconto = 0) => new(Guid.NewGuid(), 124, situacao,
        new(Guid.NewGuid(), "João & Cia", TipoPessoa.PessoaJuridica, Dados.CnpjAcademia, null, null, null, null, null, null),
        Dados.Hoje, Dados.Agora, new(2026, 10, 2), dias, new(2030, 1, 1), "À vista (PIX)", null,
        Dados.UsuarioId, "Maria Silva",
        new[] { new ItemDetalheDto(Guid.NewGuid(), 1, Guid.NewGuid(), "Camisa Polo", "Polo azul", 3, 10.01m,
            2.03m, 30.03m, 6.09m, 36.12m, new("Bordado", "Azul", null, null, null, null, null), Array.Empty<AnexoDto>()) },
        new(1, 3, 30.03m, 6.09m, desconto, 36.12m - desconto), null, null, null, null, true, 1);

    private static OrcamentoDocumentoDto Montar(OrcamentoDetalheDto o, RelogioFixo? clock = null,
        EmpresaOptions? empresa = null) => OrcamentoDocumentoService.Montar(o, Cliente(), empresa ?? new(),
            clock ?? new(), (clock ?? new()).AgoraUtc);

    [Theory]
    [InlineData(1, "1 dia útil", "1 semana")]
    [InlineData(5, "5 dias úteis", "1 semana")]
    [InlineData(6, "6 dias úteis", "2 semanas")]
    [InlineData(15, "15 dias úteis", "3 semanas")]
    public void Prazo_tem_dias_e_semanas_com_plural_correto(int dias, string duracao, string semanas)
    {
        var d = Montar(Orcamento(dias));
        Assert.Contains(duracao, d.TextosPrazo[0]);
        Assert.Contains(semanas, d.TextosPrazo[0]);
    }

    [Fact]
    public void Previsoes_usam_calendario_comum_e_hoje_no_fuso_de_negocio()
    {
        // Sábado em UTC, mas sexta-feira 25/09 em São Paulo: um dia útil termina na segunda.
        var clock = new RelogioFixo { Agora = new(2026, 9, 26, 1, 0, 0, DateTimeKind.Utc) };
        var d = Montar(Orcamento(1), clock);
        Assert.Contains("hoje (25/09/2026): entrega até 28/09/2026", d.TextosPrazo[1]);
        Assert.Contains("(02/10/2026): entrega até 05/10/2026", d.TextosPrazo[2]);
        Assert.Equal("25/09/2026", d.Emissao);
        Assert.EndsWith("25/09/2026 22:00", d.GeradoEm);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(1.23, "R$ 1,23")]
    public void Valores_sao_os_persistidos_e_desconto_zero_e_omitido(double desconto, string? texto)
    {
        var o = Orcamento(desconto: (decimal)desconto);
        var d = Montar(o);
        Assert.Equal(texto, d.Desconto);
        Assert.Equal("R$ 30,03", d.SubtotalProdutos);
        Assert.Equal("R$ 6,09", d.Personalizacao);
        Assert.Equal("R$ 36,12", d.Itens[0].Total);
        Assert.Equal("R$ 10,01", d.Itens[0].ValorUnitario);
        // Valor propositalmente divergente prova que a projeção não recalcula o financeiro.
        Assert.Equal("R$ 7,89", Montar(o with { Resumo = o.Resumo with { ValorTotal = 7.89m } }).Total);
    }

    [Fact]
    public void Opcionais_vazios_sao_omitidos_e_campos_publicos_sao_formatados()
    {
        var d = Montar(Orcamento());
        Assert.Equal(2, d.Itens[0].Personalizacao.Count);
        Assert.Empty(d.Itens[0].Artes);
        Assert.Contains(d.DadosCliente, c => c.Valor == "38.221.907/0001-53");
        Assert.Contains(d.DadosCliente, c => c.Valor == "(47) 99988-7766");
        Assert.Null(d.ObservacoesComerciais);
    }

    [Fact]
    public void Rascunho_e_marcado_e_aprovado_usa_data_real_do_pedido()
    {
        var rascunho = Montar(Orcamento(situacao: SituacaoOrcamento.EmElaboracao));
        Assert.True(rascunho.Rascunho);
        Assert.Contains("RASCUNHO", rascunho.Selo);
        var o = Orcamento(situacao: SituacaoOrcamento.Aprovado) with
        { Pedido = new(Guid.NewGuid(), 57, SituacaoPedido.Aberto, Dados.Agora, new(2026, 10, 30)) };
        var d = Montar(o);
        Assert.Equal("APROVADO", d.Selo);
        Assert.Contains("Pedido nº 000057", Assert.Single(d.TextosPrazo));
        Assert.Contains("30/10/2026", d.PrazoDestaque);
        Assert.DoesNotContain(d.TextosPrazo, t => t.Contains("se aprovada", StringComparison.OrdinalIgnoreCase));
        Assert.False(d.PodeAprovar);
    }

    [Theory]
    [InlineData(SituacaoOrcamento.Recusado, "Proposta recusada")]
    [InlineData(SituacaoOrcamento.Expirado, "Proposta expirada")]
    [InlineData(SituacaoOrcamento.Cancelado, "Proposta cancelada")]
    public void Encerrados_nao_prometem_entrega(SituacaoOrcamento situacao, string selo)
    {
        var d = Montar(Orcamento(situacao: situacao));
        Assert.Equal(selo, d.Selo);
        Assert.Equal("Sem previsão de entrega", d.PrazoDestaque);
        Assert.DoesNotContain(d.TextosPrazo, t => t.Contains("entrega até"));
        Assert.False(d.PodeAprovar);
    }

    [Fact]
    public void Validade_vencida_antes_da_rotina_nao_promete_entrega()
    {
        var d = Montar(Orcamento() with { Validade = Dados.Hoje.AddDays(-1) });
        Assert.Equal("Proposta com validade vencida", d.Selo);
        Assert.False(d.PodeAprovar);
        Assert.DoesNotContain(d.TextosPrazo, t => t.Contains("entrega até"));
        Assert.True(Montar(Orcamento() with { Validade = Dados.Hoje }).PodeAprovar);
    }

    [Theory]
    [InlineData("João / Ação: <Loja> ?*|\"", "Orcamento-000124-Joao-Acao-Loja.pdf")]
    [InlineData("***", "Orcamento-000124-Cliente.pdf")]
    [InlineData("  São José  ", "Orcamento-000124-Sao-Jose.pdf")]
    public void Nome_de_arquivo_e_portavel(string nome, string esperado) =>
        Assert.Equal(esperado, OrcamentoDocumentoService.NomeArquivo(124, nome));

    [Fact]
    public void Nome_de_arquivo_e_limitado_e_contatos_vem_da_configuracao()
    {
        Assert.True(OrcamentoDocumentoService.NomeArquivo(124, new string('a', 500)).Length <= 120);
        var d = Montar(Orcamento(), empresa: new() { NomeFantasia = "Empresa Teste", Email = "vendas@example.com", TextoRodape = "Obrigado!" });
        Assert.Equal("Empresa Teste", d.Empresa);
        Assert.Contains("vendas@example.com", d.ParaAprovar);
        Assert.Contains("Obrigado!", d.Rodape);
    }
}
