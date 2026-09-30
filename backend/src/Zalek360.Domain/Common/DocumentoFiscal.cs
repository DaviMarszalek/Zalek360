using Zalek360.Domain.Enums;

namespace Zalek360.Domain.Common;

/// <summary>Validação de CPF/CNPJ com dígitos verificadores. Documentos são persistidos somente com dígitos.</summary>
public static class DocumentoFiscal
{
    public static string Normalizar(string? documento) => TextoBusca.SomenteDigitos(documento);

    public static bool Valido(TipoPessoa tipo, string digitos) =>
        tipo == TipoPessoa.PessoaFisica ? CpfValido(digitos) : CnpjValido(digitos);

    public static bool CpfValido(string digitos)
    {
        if (digitos.Length != 11 || digitos.Distinct().Count() == 1) return false;
        var d = digitos.Select(c => c - '0').ToArray();
        for (var n = 9; n <= 10; n++)
        {
            var soma = 0;
            for (var i = 0; i < n; i++) soma += d[i] * (n + 1 - i);
            var dv = soma * 10 % 11 % 10;
            if (dv != d[n]) return false;
        }
        return true;
    }

    public static bool CnpjValido(string digitos)
    {
        if (digitos.Length != 14 || digitos.Distinct().Count() == 1) return false;
        var d = digitos.Select(c => c - '0').ToArray();
        int[] pesos1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] pesos2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        foreach (var (pesos, n) in new[] { (pesos1, 12), (pesos2, 13) })
        {
            var soma = 0;
            for (var i = 0; i < n; i++) soma += d[i] * pesos[i];
            var resto = soma % 11;
            var dv = resto < 2 ? 0 : 11 - resto;
            if (dv != d[n]) return false;
        }
        return true;
    }

    public static string Formatar(string digitos) => digitos.Length switch
    {
        11 => $"{digitos[..3]}.{digitos[3..6]}.{digitos[6..9]}-{digitos[9..]}",
        14 => $"{digitos[..2]}.{digitos[2..5]}.{digitos[5..8]}/{digitos[8..12]}-{digitos[12..]}",
        _ => digitos
    };

    public static string Rotulo(TipoPessoa tipo) => tipo == TipoPessoa.PessoaFisica ? "CPF" : "CNPJ";
}
