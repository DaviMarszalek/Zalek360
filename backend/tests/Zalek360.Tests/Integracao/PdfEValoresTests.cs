using System.Text;
using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Services;
using Zalek360.Domain.Common;
using Zalek360.Infrastructure.Documentos;
using Zalek360.Tests.Aplicacao;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Integracao;

/// <summary>Fase 7.3: PDF nunca falha por emoji/Unicode e valores com mais de 2 casas seguem uma regra única.</summary>
public sealed class PdfEValoresTests
{
    private const string Hostil = "Cliente 😀 Ação 👍🏽 👨‍👩‍👧 ❤️ ✓ ★ ⚠ → ∑ 漢字 עברית العربية Ελληνικά Кириллица ℃ ½   fim\r\nlinha\t2";

    private static async Task<byte[]> GerarPdf(Func<OrcamentoDocumentoDto, OrcamentoDocumentoDto> alterar)
    {
        var relogio = new RelogioFixo();
        var dto = OrcamentoDocumentoService.Montar(OrcamentoDocumentoTests.Orcamento(), OrcamentoDocumentoTests.Cliente(),
            new EmpresaOptions(), relogio, relogio.AgoraUtc);
        return await new GeradorDocumentoOrcamento().GerarAsync(alterar(dto), default);
    }

    [Fact]
    public async Task Pdf_real_com_emoji_e_caracteres_sem_glifo_e_gerado_sem_erro()
    {
        var bytes = await GerarPdf(d => d with
        {
            Cliente = Hostil, Empresa = "Zalek 😀", ObservacoesComerciais = Hostil, CondicaoPagamento = "PIX ✅",
            DadosCliente = d.DadosCliente.Select(c => c with { Valor = c.Valor + " 😀" }).ToArray(),
            Itens = d.Itens.Select(i => i with { Produto = "Camiseta 🕷️ Homem-Aranha", Descricao = Hostil,
                Personalizacao = i.Personalizacao.Select(c => c with { Valor = "Serigrafia 🎨 ✓" }).ToArray(), Artes = ["arte-😀.png"] }).ToArray()
        });
        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task Todos_os_caracteres_aceitos_existem_nas_fontes_embutidas()
    {
        // Renderiza de verdade cada caractere da lista de suportados: se a fonte não tiver o glifo, o QuestPDF lança exceção.
        var todos = new StringBuilder();
        for (var c = 0x20; c <= 0x2122; c++) if (TextoPdf.Suportado(c)) todos.Append((char)c);
        var texto = todos.ToString();
        Assert.Equal(texto, TextoPdf.Seguro(texto));
        var bytes = await GerarPdf(d => d with { ObservacoesComerciais = texto });
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Texto_seguro_descarta_emoji_troca_glifos_ausentes_e_preserva_portugues()
    {
        Assert.Equal("Ação São João — ç ã é ô “ok” R$ 1.234,56 € …", TextoPdf.Seguro("Ação São João — ç ã é ô “ok” R$ 1.234,56 € …"));
        Assert.Equal("Cliente  Ação", TextoPdf.Seguro("Cliente 😀 Ação"));
        Assert.Equal("Família ", TextoPdf.Seguro("Família 👨‍👩‍👧"));
        Assert.Equal("ok ", TextoPdf.Seguro("ok ❤️"));
        Assert.Equal("?? ?", TextoPdf.Seguro("漢字 Ω"));
        Assert.Equal("a b\nc d", TextoPdf.Seguro("a b\r\nc\td"));
        Assert.Equal("Ação", TextoPdf.Seguro("Ação")); // forma decomposta (NFD) vira NFC
    }

    [Fact]
    public void Calculo_arredonda_valores_unitarios_antes_de_multiplicar()
    {
        var r = OrcamentoService.Calcular(new CalculoRequest([new CalculoItemRequest(3, 10.555m, 0.005m)], 0.015m));
        Assert.Equal(31.68m, r.SubtotalProdutos);   // 3 × 10,56 (antes: 31,67 = arredondar(3 × 10,555))
        Assert.Equal(0.03m, r.ValorPersonalizacao); // 3 × 0,01
        Assert.Equal(0.02m, r.Desconto);
        Assert.Equal(31.69m, r.ValorTotal);
    }

    [Fact]
    public async Task Orcamento_gravado_tem_os_mesmos_valores_da_previa()
    {
        var c = new Cenario();
        var previa = OrcamentoService.Calcular(new CalculoRequest([new CalculoItemRequest(3, 10.555m, 0.005m)], 0.015m));
        var o = await c.Orcamentos().CriarAsync(Cenario.Requisicao(c.Metalurgica.Id, c.Polo.Id,
            Cenario.ItemReq(c.Polo.Id, quantidade: 3, unitario: 10.555m, personalizacao: 0.005m)) with { Desconto = 0.015m }, default);
        Assert.Equal(31.69m, previa.ValorTotal);
        Assert.Equal(previa.ValorTotal, o.Resumo.ValorTotal);
    }

    [Theory]
    [InlineData(1, 0.004, 0)]     // unitário vira 0,00: recusado (antes: gravado com total R$ 0,00)
    [InlineData(1000, 0.004, 4)]  // antes: validação aceitava e o banco recusava (HTTP 500)
    public async Task Unitario_que_arredonda_para_zero_e_recusado_na_validacao(int quantidade, decimal unitario, decimal desconto)
    {
        var c = new Cenario();
        var erro = await Assert.ThrowsAsync<ValidacaoException>(() => c.Orcamentos().CriarAsync(Cenario.Requisicao(c.Metalurgica.Id, c.Polo.Id,
            Cenario.ItemReq(c.Polo.Id, quantidade: quantidade, unitario: unitario, personalizacao: 0m)) with { Desconto = desconto }, default));
        Assert.Equal(400, erro.StatusCode);
        Assert.Contains(erro.Erros.Keys, k => k.EndsWith("valorUnitario"));
    }
}
