using System.Text.Json;
using Zalek360.Application.Dtos;
using Zalek360.Domain.Common;

namespace Zalek360.Infrastructure.ConsultaCnpj;

internal enum SituacaoProvedorCnpj { Sucesso, NaoEncontrado, Limite, Falha }

internal sealed record DadosCadastraisCnpj(
    string? RazaoSocial, string? NomeFantasia, string? Telefone, string? Email, EnderecoDto? Endereco);

/// <summary>
/// Resposta normalizada de um provedor. <see cref="Inscricoes"/> nulo = o provedor não informou IEs
/// (campo ausente); lista vazia = informou que não há.
/// </summary>
internal sealed record RespostaProvedorCnpj(
    SituacaoProvedorCnpj Situacao, DadosCadastraisCnpj? Dados = null, IReadOnlyList<InscricaoEstadualDto>? Inscricoes = null)
{
    public bool Sucesso => Situacao == SituacaoProvedorCnpj.Sucesso;
    /// <summary>Resposta definitiva (não depende de nova tentativa): sucesso ou não encontrado.</summary>
    public bool Definitiva => Situacao is SituacaoProvedorCnpj.Sucesso or SituacaoProvedorCnpj.NaoEncontrado;

    public static readonly RespostaProvedorCnpj NaoEncontrado = new(SituacaoProvedorCnpj.NaoEncontrado);
    public static readonly RespostaProvedorCnpj Limite = new(SituacaoProvedorCnpj.Limite);
    public static readonly RespostaProvedorCnpj Falha = new(SituacaoProvedorCnpj.Falha);
}

/// <summary>Leitura tolerante do JSON dos provedores: ignora ausentes, nulos, tipos inesperados e textos vazios.</summary>
internal static class JsonCnpj
{
    public static JsonElement? Objeto(JsonElement e, string nome) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(nome, out var v) && v.ValueKind == JsonValueKind.Object ? v : null;

    public static IEnumerable<JsonElement> Lista(JsonElement e, string nome) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(nome, out var v) && v.ValueKind == JsonValueKind.Array
            ? v.EnumerateArray().ToArray() : Array.Empty<JsonElement>();

    public static bool TemLista(JsonElement e, string nome) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(nome, out var v) && v.ValueKind == JsonValueKind.Array;

    public static string? Texto(JsonElement? e, string nome)
    {
        if (e is not { ValueKind: JsonValueKind.Object } o || !o.TryGetProperty(nome, out var v)) return null;
        var texto = v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number => v.GetRawText(),
            _ => null
        };
        return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    }

    public static bool Booleano(JsonElement e, string nome) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(nome, out var v) && v.ValueKind == JsonValueKind.True;

    public static string? Digitos(string? valor)
    {
        var d = TextoBusca.SomenteDigitos(valor);
        return d.Length == 0 ? null : d;
    }

    public static string? Uf(string? valor)
    {
        var uf = valor?.Trim().ToUpperInvariant();
        return uf is { Length: 2 } && uf.All(char.IsAsciiLetterUpper) ? uf : null;
    }

    /// <summary>"AVENIDA" + "PAULISTA" → "AVENIDA PAULISTA", sem repetir o tipo se o logradouro já o tiver.</summary>
    public static string? Logradouro(string? tipo, string? logradouro)
    {
        if (logradouro is null) return null;
        if (tipo is null || logradouro.StartsWith(tipo + " ", StringComparison.OrdinalIgnoreCase)) return logradouro;
        return $"{tipo} {logradouro}";
    }

    public static EnderecoDto? Endereco(string? cep, string? logradouro, string? numero, string? complemento,
        string? bairro, string? cidade, string? uf)
    {
        var endereco = new EnderecoDto(Digitos(cep), logradouro, numero, complemento, bairro, cidade, Uf(uf));
        return endereco is { Cep: null, Logradouro: null, Numero: null, Complemento: null, Bairro: null, Cidade: null, Uf: null }
            ? null : endereco;
    }

    public static InscricaoEstadualDto? Inscricao(string? numero, string? uf, bool ativa) =>
        numero is null ? null : new InscricaoEstadualDto(numero, Uf(uf) ?? "", ativa);
}
