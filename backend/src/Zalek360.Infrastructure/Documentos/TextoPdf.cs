using System.Globalization;
using System.Text;
using Zalek360.Application.Dtos;

namespace Zalek360.Infrastructure.Documentos;

/// <summary>
/// Garante que todo texto do PDF use somente caracteres cobertos pelas fontes embutidas (IBM Plex Sans e Lato).
/// Desde o QuestPDF 2026.9, um glifo ausente lança exceção ao gerar o documento; sem este filtro, um emoji no nome
/// do cliente ou numa observação impedia o PDF (HTTP 500). Regra: normaliza em NFC, mantém a lista explícita de
/// caracteres suportados, descarta emoji, seletores de variação, ZWJ e controles, e troca qualquer outro caractere
/// não suportado por "?". O cadastro original não é alterado; só a cópia renderizada.
/// </summary>
public static class TextoPdf
{
    public const char Substituto = '?';

    public static bool Suportado(int c) =>
        c == '\n'
        || c is >= 0x20 and <= 0x7E          // ASCII imprimível
        || c is >= 0xA0 and <= 0xFF && c != 0xAD // Latin-1 (sem hífen suave)
        || c is >= 0x100 and <= 0x17F         // Latin Extended-A
        || c is >= 0x2013 and <= 0x2014       // – —
        || c is >= 0x2018 and <= 0x201A       // ‘ ’ ‚
        || c is >= 0x201C and <= 0x201E       // “ ” „
        || c is 0x2020 or 0x2021 or 0x2022 or 0x2026 or 0x2030 or 0x2039 or 0x203A // † ‡ • … ‰ ‹ ›
        || c is 0x20AC or 0x2122;             // € ™

    private static bool Descartavel(Rune r)
    {
        if (r.Value is 0xFE0E or 0xFE0F or 0x200D) return true;          // seletores de variação e ZWJ (emoji)
        if (r.Value >= 0x1F000) return true;                               // emoji e pictogramas (planos suplementares)
        return Rune.GetUnicodeCategory(r) is UnicodeCategory.OtherSymbol or UnicodeCategory.Format
            or UnicodeCategory.Control or UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark
            or UnicodeCategory.PrivateUse or UnicodeCategory.Surrogate or UnicodeCategory.OtherNotAssigned;
    }

    public static string Seguro(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        var normalizado = texto.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\t', ' ').Normalize(NormalizationForm.FormC);
        var sb = new StringBuilder(normalizado.Length);
        foreach (var r in normalizado.EnumerateRunes())
        {
            if (Suportado(r.Value)) sb.Append((char)r.Value);
            else if (Rune.GetUnicodeCategory(r) == UnicodeCategory.SpaceSeparator) sb.Append(' '); // ex.: U+202F da formatação pt-BR
            else if (!Descartavel(r)) sb.Append(Substituto);
        }
        return sb.ToString();
    }

    public static string? SeguroOuNulo(string? texto) => texto is null ? null : Seguro(texto);

    private static IReadOnlyList<string> Lista(IReadOnlyList<string> itens) => itens.Select(Seguro).ToArray();
    private static IReadOnlyList<CampoDocumentoDto> Campos(IReadOnlyList<CampoDocumentoDto> itens) =>
        itens.Select(c => new CampoDocumentoDto(Seguro(c.Rotulo), Seguro(c.Valor))).ToArray();

    /// <summary>Cópia do documento com todos os textos seguros para as fontes embutidas.</summary>
    public static OrcamentoDocumentoDto Sanitizar(OrcamentoDocumentoDto d) => d with
    {
        Numero = Seguro(d.Numero), Emissao = Seguro(d.Emissao), GeradoEm = Seguro(d.GeradoEm), Empresa = Seguro(d.Empresa),
        ContatosEmpresa = Lista(d.ContatosEmpresa), Rodape = Lista(d.Rodape), Cliente = Seguro(d.Cliente),
        DadosCliente = Campos(d.DadosCliente), Responsavel = Seguro(d.Responsavel), Selo = SeguroOuNulo(d.Selo),
        Validade = Seguro(d.Validade), PrazoDestaque = Seguro(d.PrazoDestaque), TextosPrazo = Lista(d.TextosPrazo),
        NotaPrazo = Seguro(d.NotaPrazo), CondicaoPagamento = Seguro(d.CondicaoPagamento),
        ObservacoesComerciais = SeguroOuNulo(d.ObservacoesComerciais),
        Itens = d.Itens.Select(i => i with
        {
            Numero = Seguro(i.Numero), Produto = Seguro(i.Produto), Descricao = SeguroOuNulo(i.Descricao),
            Quantidade = Seguro(i.Quantidade), ValorUnitario = Seguro(i.ValorUnitario),
            PersonalizacaoUnitaria = Seguro(i.PersonalizacaoUnitaria), Total = Seguro(i.Total),
            Personalizacao = Campos(i.Personalizacao), Artes = Lista(i.Artes)
        }).ToArray(),
        SubtotalProdutos = Seguro(d.SubtotalProdutos), Personalizacao = Seguro(d.Personalizacao), Desconto = SeguroOuNulo(d.Desconto),
        Total = Seguro(d.Total), PassosAprovacao = Lista(d.PassosAprovacao), ParaAprovar = Seguro(d.ParaAprovar)
    };
}
