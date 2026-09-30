using System.Globalization;
using System.Text;

namespace Zalek360.Domain.Common;

/// <summary>Formatação pt-BR independente da cultura do servidor (usada nos textos do histórico).</summary>
public static class Formatos
{
    public static string Moeda(decimal valor)
    {
        var v = Dinheiro.Arredondar(valor);
        var texto = Math.Abs(v).ToString("#,##0.00", CultureInfo.InvariantCulture)
            .Replace(",", "_").Replace(".", ",").Replace("_", ".");
        return (v < 0 ? "-R$ " : "R$ ") + texto;
    }

    public static string Data(DateOnly data) => data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string NumeroDocumento(int numero) => "#" + numero.ToString("D6", CultureInfo.InvariantCulture);

    public static string Quantidade(int itens, int unidades) =>
        $"{itens} {(itens == 1 ? "item" : "itens")}, {unidades} {(unidades == 1 ? "unidade" : "unidades")}";
}

public static class Dinheiro
{
    public static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}

/// <summary>Normalização para busca: minúsculas e sem acentos, sem depender de ICU.</summary>
public static class TextoBusca
{
    private const string ComAcento = "áàâãäåéèêëíìîïóòôõöúùûüçñý";
    private const string SemAcento = "aaaaaaeeeeiiiiooooouuuucny";

    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var sb = new StringBuilder(texto.Length);
        foreach (var c in texto.Trim().ToLowerInvariant())
        {
            var i = ComAcento.IndexOf(c);
            sb.Append(i >= 0 ? SemAcento[i] : c);
        }
        return sb.ToString();
    }

    public static string SomenteDigitos(string? texto) =>
        texto is null ? string.Empty : new string(texto.Where(char.IsAsciiDigit).ToArray());
}

public static class TextoOpcional
{
    /// <summary>Trim + null para strings vazias.</summary>
    public static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
