using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;

namespace Zalek360.Infrastructure.Documentos;

/// <summary>Somente apresentação. Nenhuma regra comercial, cálculo financeiro ou previsão pertence ao renderer.</summary>
public sealed class GeradorDocumentoOrcamento : IGeradorDocumentoOrcamento
{
    private const string Azul = "#1D4ED8";
    private const string Cinza = "#5B6472";
    private static readonly Lazy<bool> Fontes = new(() =>
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var assembly = typeof(GeradorDocumentoOrcamento).Assembly;
        foreach (var nome in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".ttf", StringComparison.Ordinal)))
        {
            using var fonte = assembly.GetManifestResourceStream(nome)!;
            FontManager.RegisterFontFromStream(fonte);
        }
        return true;
    });

    public Task<byte[]> GerarAsync(OrcamentoDocumentoDto d, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _ = Fontes.Value;
        var logo = LerLogo(d.CaminhoLogo);
        var documento = Document.Create(doc => doc.Page(p =>
        {
            p.Size(PageSizes.A4);
            p.Margin(32);
            p.DefaultTextStyle(t => t.FontFamily("IBM Plex Sans", "Lato").FontSize(9).FontColor("#172033"));
            p.Header().PaddingBottom(12).Column(c =>
            {
                c.Item().Row(r =>
                {
                    if (logo is not null) r.ConstantItem(130).Height(44).Image(logo).FitArea();
                    else r.RelativeItem().Text(d.Empresa).FontSize(19).Bold().FontColor(Azul);
                    r.RelativeItem().AlignRight().Column(t =>
                    {
                        t.Item().Text(RotulosDocumento.Titulo).FontSize(19).Bold();
                        t.Item().Text($"{RotulosDocumento.Numero} {d.Numero} · {d.Emissao}").FontColor(Cinza);
                    });
                });
                if (d.Selo is not null) c.Item().PaddingTop(6).Background("#EFF6FF").Padding(6).Text(d.Selo).SemiBold().FontColor(Azul);
            });
            p.Content().Column(c =>
            {
                c.Spacing(10);
                c.Item().Background("#EFF6FF").Padding(10).Row(r =>
                {
                    Destaque(r.RelativeItem(), RotulosDocumento.Total, d.Total);
                    Destaque(r.RelativeItem(), RotulosDocumento.Validade, d.Validade);
                    Destaque(r.RelativeItem(1.5f), RotulosDocumento.Prazo, d.PrazoDestaque);
                });
                c.Item().EnsureSpace(70).Row(r =>
                {
                    r.RelativeItem(1.6f).Column(cc =>
                    {
                        cc.Item().Text(RotulosDocumento.Cliente).SemiBold().FontColor(Azul);
                        cc.Item().Text(d.Cliente).Bold();
                        foreach (var campo in d.DadosCliente) cc.Item().Text($"{campo.Rotulo}: {campo.Valor}").FontSize(8);
                    });
                    r.RelativeItem().Column(cc =>
                    {
                        cc.Item().Text(RotulosDocumento.Responsavel).SemiBold().FontColor(Azul);
                        cc.Item().Text(d.Responsavel);
                        foreach (var contato in d.ContatosEmpresa) cc.Item().Text(contato).FontSize(8);
                    });
                });
                c.Item().Text(RotulosDocumento.Itens).SemiBold().FontColor(Azul);
                c.Item().Table(t =>
                {
                    t.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(22); cols.RelativeColumn(4); cols.ConstantColumn(34);
                        cols.RelativeColumn(1.25f); cols.RelativeColumn(1.25f); cols.RelativeColumn(1.4f);
                    });
                    t.Header(h =>
                    {
                        foreach (var rotulo in RotulosDocumento.Colunas)
                            h.Cell().Background(Azul).Padding(5).Text(rotulo).FontSize(8).Bold().FontColor(Colors.White);
                    });
                    foreach (var i in d.Itens)
                    {
                        // Uma célula por item mantém os detalhes juntos se couberem e permite textos extensos.
                        t.Cell().ColumnSpan(6).PreventPageBreak().BorderBottom(0.5f).BorderColor("#DCE2EA").PaddingVertical(7).Column(cc =>
                        {
                            cc.Spacing(3);
                            cc.Item().Table(linha =>
                            {
                                linha.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(22); cols.RelativeColumn(4); cols.ConstantColumn(34);
                                    cols.RelativeColumn(1.25f); cols.RelativeColumn(1.25f); cols.RelativeColumn(1.4f);
                                });
                                linha.Cell().PaddingHorizontal(4).Text(i.Numero);
                                linha.Cell().PaddingHorizontal(4).Column(prod =>
                                {
                                    prod.Item().Text(i.Produto).SemiBold();
                                    if (!string.IsNullOrWhiteSpace(i.Descricao)) prod.Item().Text(i.Descricao).FontSize(8).FontColor(Cinza);
                                });
                                foreach (var valor in new[] { i.Quantidade, i.ValorUnitario, i.PersonalizacaoUnitaria, i.Total })
                                    linha.Cell().PaddingHorizontal(4).AlignRight().Text(valor).FontSize(8);
                            });
                            if (i.Personalizacao.Count > 0)
                            {
                                cc.Item().PaddingLeft(26).Text(RotulosDocumento.Personalizacao).FontSize(8).SemiBold();
                                foreach (var campo in i.Personalizacao)
                                    cc.Item().PaddingLeft(26).Text($"{campo.Rotulo}: {campo.Valor}").FontSize(8).FontColor(Cinza);
                            }
                            if (i.Artes.Count > 0)
                                cc.Item().PaddingLeft(26).Text($"{RotulosDocumento.Artes}: {string.Join("; ", i.Artes)}").FontSize(8).FontColor(Cinza);
                        });
                    }
                });
                c.Item().EnsureSpace(85).AlignRight().Width(260).Column(cc =>
                {
                    cc.Item().Text(RotulosDocumento.Resumo).SemiBold().FontColor(Azul);
                    Valor(cc, RotulosDocumento.Produtos, d.SubtotalProdutos);
                    Valor(cc, RotulosDocumento.Personalizacao, d.Personalizacao);
                    if (d.Desconto is not null) Valor(cc, RotulosDocumento.Desconto, d.Desconto);
                    cc.Item().PaddingTop(4).BorderTop(1).BorderColor(Azul).PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text(RotulosDocumento.Total).Bold();
                        r.RelativeItem().AlignRight().Text(d.Total).FontSize(15).Bold().FontColor(Azul);
                    });
                });
                c.Item().EnsureSpace(50).Column(cc =>
                {
                    cc.Item().Text(RotulosDocumento.Condicoes).SemiBold().FontColor(Azul);
                    cc.Item().Text($"{RotulosDocumento.Pagamento}: {d.CondicaoPagamento}");
                    cc.Item().Text($"{RotulosDocumento.ValidadeCondicoes}: {d.Validade}");
                    if (!string.IsNullOrWhiteSpace(d.ObservacoesComerciais)) cc.Item().Text(d.ObservacoesComerciais);
                });
                c.Item().PreventPageBreak().Background("#EFF6FF").Padding(10).Column(cc =>
                {
                    cc.Spacing(4);
                    cc.Item().Text(RotulosDocumento.Prazo).Bold().FontColor(Azul);
                    foreach (var texto in d.TextosPrazo) cc.Item().Text(texto);
                    cc.Item().Text(d.NotaPrazo).FontSize(8).FontColor(Cinza);
                });
                if (d.PodeAprovar)
                {
                    c.Item().PreventPageBreak().Column(cc =>
                    {
                        cc.Item().Text(RotulosDocumento.AposAprovacao).SemiBold().FontColor(Azul);
                        foreach (var passo in d.PassosAprovacao) cc.Item().Text(passo);
                        cc.Item().PaddingTop(6).Text(RotulosDocumento.ParaAprovar).SemiBold().FontColor(Azul);
                        cc.Item().Text(d.ParaAprovar);
                    });
                }
            });
            p.Footer().PaddingTop(10).BorderTop(0.5f).BorderColor("#DCE2EA").PaddingTop(5).Column(c =>
            {
                if (d.Rascunho) c.Item().Text(d.Selo).FontSize(8).Bold().FontColor(Azul);
                foreach (var linha in d.Rodape) c.Item().Text(linha).FontSize(7).FontColor(Cinza);
                c.Item().Row(r =>
                {
                    r.RelativeItem().Text(d.GeradoEm).FontSize(7).FontColor(Cinza);
                    r.ConstantItem(85).AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(7).FontColor(Cinza));
                        t.Span(RotulosDocumento.Pagina); t.CurrentPageNumber(); t.Span(RotulosDocumento.DePaginas); t.TotalPages();
                    });
                });
            });
        }));
        var bytes = documento.GeneratePdf();
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(bytes);
    }

    private static void Destaque(IContainer c, string titulo, string valor) => c.Column(cc =>
    {
        cc.Item().Text(titulo).FontSize(7).SemiBold().FontColor(Cinza);
        cc.Item().PaddingTop(3).Text(valor).FontSize(11).Bold().FontColor(Azul);
    });

    private static void Valor(ColumnDescriptor c, string rotulo, string valor) => c.Item().Row(r =>
    {
        r.RelativeItem().Text(rotulo).FontColor(Cinza);
        r.RelativeItem().AlignRight().Text(valor);
    });

    private static Image? LerLogo(string? caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho)) return null;
        try
        {
            // Limite local; valida a imagem antes de compor o documento, com fallback textual.
            var info = new FileInfo(caminho);
            if (!info.Exists || info.Length > 2 * 1024 * 1024) return null;
            return Image.FromBinaryData(File.ReadAllBytes(caminho));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return null;
        }
    }
}
