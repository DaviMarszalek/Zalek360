using System.Text.Json;
using Zalek360.Application.Dtos;

namespace Zalek360.Infrastructure.ConsultaCnpj;

/// <summary>
/// BrasilAPI — <c>GET https://brasilapi.com.br/api/cnpj/v1/{cnpj}</c> (dados cadastrais; não traz IE).
/// Formato confirmado no OpenAPI público (github.com/BrasilAPI/BrasilAPI, pages/docs/doc/cnpj.json).
/// </summary>
internal static class ProvedorBrasilApi
{
    public const string Nome = "BrasilAPI";
    public const string Cliente = "cnpj-brasilapi";
    public static readonly Uri Base = new("https://brasilapi.com.br/");
    public static string Caminho(string cnpj) => $"api/cnpj/v1/{cnpj}";

    public static RespostaProvedorCnpj Mapear(JsonElement r)
    {
        var razao = JsonCnpj.Texto(r, "razao_social");
        if (razao is null) return RespostaProvedorCnpj.Falha;
        var telefone = JsonCnpj.Digitos(JsonCnpj.Texto(r, "ddd_telefone_1")) ?? JsonCnpj.Digitos(JsonCnpj.Texto(r, "ddd_telefone_2"));
        var endereco = JsonCnpj.Endereco(JsonCnpj.Texto(r, "cep"),
            JsonCnpj.Logradouro(JsonCnpj.Texto(r, "descricao_tipo_de_logradouro"), JsonCnpj.Texto(r, "logradouro")),
            JsonCnpj.Texto(r, "numero"), JsonCnpj.Texto(r, "complemento"), JsonCnpj.Texto(r, "bairro"),
            JsonCnpj.Texto(r, "municipio"), JsonCnpj.Texto(r, "uf"));
        return new(SituacaoProvedorCnpj.Sucesso,
            new DadosCadastraisCnpj(razao, JsonCnpj.Texto(r, "nome_fantasia"), telefone, JsonCnpj.Texto(r, "email"), endereco));
    }
}

/// <summary>
/// CNPJá API pública — <c>GET https://open.cnpja.com/office/{cnpj}</c> (5 consultas/min por IP).
/// Dados cadastrais (fallback) e IEs em <c>registrations</c>. Formato NÃO confirmado na documentação oficial
/// (cnpja.com respondeu 429 a todas as tentativas); baseado no esquema "office" do SDK cnpja_ex e de proxies públicos:
/// <c>company.name, alias, phones[].area/number, emails[].address, address.street/number/details/district/city/state/zip,
/// registrations[].number/state/enabled</c>.
/// </summary>
internal static class ProvedorCnpja
{
    public const string Nome = "CNPJá";
    public const string Cliente = "cnpj-cnpja";
    public static readonly Uri Base = new("https://open.cnpja.com/");
    public static string Caminho(string cnpj) => $"office/{cnpj}";

    public static RespostaProvedorCnpj Mapear(JsonElement r)
    {
        var razao = JsonCnpj.Texto(JsonCnpj.Objeto(r, "company"), "name");
        if (razao is null) return RespostaProvedorCnpj.Falha;
        var telefone = JsonCnpj.Lista(r, "phones")
            .Select(p => JsonCnpj.Digitos($"{JsonCnpj.Texto(p, "area")}{JsonCnpj.Texto(p, "number")}"))
            .FirstOrDefault(t => t is not null);
        var email = JsonCnpj.Lista(r, "emails").Select(e => JsonCnpj.Texto(e, "address")).FirstOrDefault(e => e is not null);
        var a = JsonCnpj.Objeto(r, "address");
        var endereco = JsonCnpj.Endereco(JsonCnpj.Texto(a, "zip"), JsonCnpj.Texto(a, "street"), JsonCnpj.Texto(a, "number"),
            JsonCnpj.Texto(a, "details"), JsonCnpj.Texto(a, "district"), JsonCnpj.Texto(a, "city"), JsonCnpj.Texto(a, "state"));
        IReadOnlyList<InscricaoEstadualDto>? inscricoes = JsonCnpj.TemLista(r, "registrations")
            ? JsonCnpj.Lista(r, "registrations")
                .Select(i => JsonCnpj.Inscricao(JsonCnpj.Texto(i, "number"), JsonCnpj.Texto(i, "state"), JsonCnpj.Booleano(i, "enabled")))
                .OfType<InscricaoEstadualDto>().ToList()
            : null;
        return new(SituacaoProvedorCnpj.Sucesso,
            new DadosCadastraisCnpj(razao, JsonCnpj.Texto(r, "alias"), telefone, email, endereco), inscricoes);
    }
}

/// <summary>
/// CNPJ.ws API pública — <c>GET https://publica.cnpj.ws/cnpj/{cnpj}</c> (3 consultas/min). Usada só para IEs:
/// <c>estabelecimento.inscricoes_estaduais[].inscricao_estadual/ativo/estado.sigla</c>.
/// Formato confirmado em docs.cnpj.ws/referencia-de-api/api-publica/consultando-cnpj.
/// </summary>
internal static class ProvedorCnpjWs
{
    public const string Nome = "CNPJ.ws";
    public const string Cliente = "cnpj-cnpjws";
    public static readonly Uri Base = new("https://publica.cnpj.ws/");
    public static string Caminho(string cnpj) => $"cnpj/{cnpj}";

    public static RespostaProvedorCnpj Mapear(JsonElement r)
    {
        if (JsonCnpj.Objeto(r, "estabelecimento") is not { } estabelecimento) return RespostaProvedorCnpj.Falha;
        var inscricoes = JsonCnpj.Lista(estabelecimento, "inscricoes_estaduais")
            .Select(i => JsonCnpj.Inscricao(JsonCnpj.Texto(i, "inscricao_estadual"),
                JsonCnpj.Texto(JsonCnpj.Objeto(i, "estado"), "sigla"), JsonCnpj.Booleano(i, "ativo")))
            .OfType<InscricaoEstadualDto>().ToList();
        return new(SituacaoProvedorCnpj.Sucesso, Inscricoes: inscricoes);
    }
}
