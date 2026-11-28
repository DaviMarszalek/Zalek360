using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;

namespace Zalek360.Application.Services;

/// <summary>Item 2 — valida o CNPJ antes de qualquer chamada externa e delega a consulta às bases públicas.</summary>
public sealed class ConsultaCnpjService(IConsultaCnpj consulta)
{
    public const string Mensagem = "CNPJ inválido. Confira os 14 dígitos.";

    public Task<ConsultaCnpjDto> ConsultarAsync(string? cnpj, CancellationToken ct)
    {
        var digitos = Normalizar(cnpj) ?? throw new ValidacaoException(CodigosErro.CnpjInvalido, Mensagem,
            new Dictionary<string, string[]> { ["cnpj"] = new[] { "Informe um CNPJ válido com 14 dígitos." } });
        return consulta.ConsultarAsync(digitos, ct);
    }

    /// <summary>
    /// Aceita o CNPJ com ou sem máscara (inclusive com a barra codificada como %2F na URL).
    /// Devolve os 14 dígitos quando o DV é válido; caso contrário, null. Letras ou outros símbolos invalidam.
    /// </summary>
    public static string? Normalizar(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj) || cnpj.Length > 40) return null;
        var texto = Uri.UnescapeDataString(cnpj).Trim();
        if (texto.Any(c => !char.IsAsciiDigit(c) && c is not ('.' or '/' or '-' or ' '))) return null;
        var digitos = DocumentoFiscal.Normalizar(texto);
        return DocumentoFiscal.CnpjValido(digitos) ? digitos : null;
    }
}
