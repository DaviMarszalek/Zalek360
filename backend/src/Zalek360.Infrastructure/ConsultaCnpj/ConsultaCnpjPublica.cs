using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;

namespace Zalek360.Infrastructure.ConsultaCnpj;

/// <summary>
/// Item 2 — consulta em bases públicas gratuitas, sem chave:
/// dados cadastrais BrasilAPI → CNPJá (fallback); IEs CNPJá → CNPJ.ws (quando a CNPJá não traz nenhuma).
/// A CNPJá é chamada uma única vez por consulta (limite de 5/min por IP), em paralelo com a BrasilAPI.
/// Timeout de 5 s por provedor; cache em memória de 10 min por CNPJ, somente para resultados sem falha transitória.
/// </summary>
internal sealed class ConsultaCnpjPublica : IConsultaCnpj
{
    public static readonly TimeSpan TimeoutPadrao = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan DuracaoCache = TimeSpan.FromMinutes(10);

    public const string MensagemNaoEncontrado = "CNPJ não encontrado nas bases públicas. Preencha os dados manualmente.";
    public const string MensagemLimite = "Limite de consultas gratuitas atingido. Aguarde um minuto e tente de novo ou preencha os dados manualmente.";
    public const string MensagemFalha = "Não foi possível consultar o CNPJ agora. Preencha os dados manualmente.";
    public const string MensagemSemIe = "Não foi possível consultar as inscrições estaduais agora. Se houver, preencha a IE manualmente.";

    private readonly IHttpClientFactory _fabrica;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConsultaCnpjPublica> _logger;
    private readonly TimeSpan _timeout;

    public ConsultaCnpjPublica(IHttpClientFactory fabrica, IMemoryCache cache, ILogger<ConsultaCnpjPublica> logger)
        : this(fabrica, cache, logger, TimeoutPadrao) { }

    internal ConsultaCnpjPublica(IHttpClientFactory fabrica, IMemoryCache cache, ILogger<ConsultaCnpjPublica> logger, TimeSpan timeout)
    {
        _fabrica = fabrica;
        _cache = cache;
        _logger = logger;
        _timeout = timeout;
    }

    public async Task<ConsultaCnpjDto> ConsultarAsync(string cnpj, CancellationToken ct)
    {
        // Defesa em profundidade: a URL só é montada com 14 dígitos (a validação de DV fica na Application).
        if (cnpj is not { Length: 14 } || !cnpj.All(char.IsAsciiDigit))
            throw new ArgumentException("CNPJ deve ter exatamente 14 dígitos.", nameof(cnpj));

        var chave = $"consulta-cnpj:{cnpj}";
        if (_cache.TryGetValue(chave, out ConsultaCnpjDto? emCache) && emCache is not null) return emCache;

        var tarefaBrasil = ChamarAsync(ProvedorBrasilApi.Nome, ProvedorBrasilApi.Cliente, ProvedorBrasilApi.Caminho(cnpj), ProvedorBrasilApi.Mapear, ct);
        var tarefaCnpja = ChamarAsync(ProvedorCnpja.Nome, ProvedorCnpja.Cliente, ProvedorCnpja.Caminho(cnpj), ProvedorCnpja.Mapear, ct);
        await Task.WhenAll(tarefaBrasil, tarefaCnpja);
        var brasil = await tarefaBrasil;
        var cnpja = await tarefaCnpja;

        var fontes = new List<string>();
        DadosCadastraisCnpj? dados = null;
        if (brasil is { Sucesso: true, Dados: not null }) { dados = brasil.Dados; fontes.Add(ProvedorBrasilApi.Nome); }
        else if (cnpja is { Sucesso: true, Dados: not null }) { dados = cnpja.Dados; fontes.Add(ProvedorCnpja.Nome); }

        if (dados is null)
            return NaoEncontrado(brasil.Situacao == SituacaoProvedorCnpj.NaoEncontrado && cnpja.Situacao == SituacaoProvedorCnpj.NaoEncontrado
                ? MensagemNaoEncontrado
                : brasil.Situacao == SituacaoProvedorCnpj.Limite || cnpja.Situacao == SituacaoProvedorCnpj.Limite
                    ? MensagemLimite : MensagemFalha);

        var definitivo = brasil.Definitiva && cnpja.Definitiva;
        IReadOnlyList<InscricaoEstadualDto> inscricoes;
        string? mensagem = null;
        if (cnpja is { Sucesso: true, Inscricoes.Count: > 0 })
        {
            inscricoes = cnpja.Inscricoes!;
            if (!fontes.Contains(ProvedorCnpja.Nome)) fontes.Add(ProvedorCnpja.Nome);
        }
        else
        {
            var cnpjWs = await ChamarAsync(ProvedorCnpjWs.Nome, ProvedorCnpjWs.Cliente, ProvedorCnpjWs.Caminho(cnpj), ProvedorCnpjWs.Mapear, ct);
            definitivo &= cnpjWs.Definitiva;
            inscricoes = cnpjWs.Inscricoes ?? Array.Empty<InscricaoEstadualDto>();
            if (inscricoes.Count > 0) fontes.Add(ProvedorCnpjWs.Nome);
            // Nenhuma fonte de IE respondeu de forma definitiva: avisa para preencher manualmente.
            if (!cnpjWs.Definitiva && cnpja is not { Sucesso: true, Inscricoes: not null }) mensagem = MensagemSemIe;
        }

        var resultado = new ConsultaCnpjDto(true, string.Join(" + ", fontes), dados.RazaoSocial, dados.NomeFantasia,
            dados.Telefone, dados.Email, dados.Endereco, Ordenar(inscricoes), mensagem);
        if (definitivo) _cache.Set(chave, resultado, DuracaoCache);
        return resultado;
    }

    /// <summary>Ativas primeiro (mantendo a ordem do provedor), sem repetir número/UF.</summary>
    private static IReadOnlyList<InscricaoEstadualDto> Ordenar(IReadOnlyList<InscricaoEstadualDto> inscricoes) =>
        inscricoes.DistinctBy(i => (i.Numero, i.Uf)).OrderByDescending(i => i.Ativa).ToList();

    private static ConsultaCnpjDto NaoEncontrado(string mensagem) =>
        new(false, null, null, null, null, null, null, Array.Empty<InscricaoEstadualDto>(), mensagem);

    private async Task<RespostaProvedorCnpj> ChamarAsync(string provedor, string cliente, string caminho,
        Func<JsonElement, RespostaProvedorCnpj> mapear, CancellationToken ct)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(_timeout);
        try
        {
            var http = _fabrica.CreateClient(cliente);
            using var resposta = await http.GetAsync(caminho, HttpCompletionOption.ResponseHeadersRead, limite.Token);
            if (resposta.StatusCode == HttpStatusCode.NotFound) return RespostaProvedorCnpj.NaoEncontrado;
            if (resposta.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Consulta de CNPJ: limite de requisições em {Provedor}.", provedor);
                return RespostaProvedorCnpj.Limite;
            }
            if (!resposta.IsSuccessStatusCode)
            {
                _logger.LogWarning("Consulta de CNPJ: {Provedor} respondeu {Status}.", provedor, (int)resposta.StatusCode);
                return RespostaProvedorCnpj.Falha;
            }
            await using var corpo = await resposta.Content.ReadAsStreamAsync(limite.Token);
            using var json = await JsonDocument.ParseAsync(corpo, cancellationToken: limite.Token);
            return json.RootElement.ValueKind == JsonValueKind.Object ? mapear(json.RootElement) : RespostaProvedorCnpj.Falha;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Consulta de CNPJ: tempo esgotado em {Provedor}.", provedor);
            return RespostaProvedorCnpj.Falha;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or IOException)
        {
            _logger.LogWarning(ex, "Consulta de CNPJ: falha em {Provedor}.", provedor);
            return RespostaProvedorCnpj.Falha;
        }
    }
}

public static class ConsultaCnpjRegistro
{
    /// <summary>HttpClients nomeados com hosts fixos, cache em memória e a implementação de <see cref="IConsultaCnpj"/>.</summary>
    public static IServiceCollection AdicionarConsultaCnpj(this IServiceCollection services)
    {
        services.AddMemoryCache();
        foreach (var (nome, endereco) in new[]
                 {
                     (ProvedorBrasilApi.Cliente, ProvedorBrasilApi.Base),
                     (ProvedorCnpja.Cliente, ProvedorCnpja.Base),
                     (ProvedorCnpjWs.Cliente, ProvedorCnpjWs.Base)
                 })
            services.AddHttpClient(nome, c =>
            {
                c.BaseAddress = endereco;
                c.Timeout = TimeSpan.FromSeconds(10); // salvaguarda; o limite efetivo é de 5 s por provedor
                c.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                c.DefaultRequestHeaders.UserAgent.ParseAdd("Zalek360/1.0");
            });
        services.AddSingleton<IConsultaCnpj, ConsultaCnpjPublica>();
        return services;
    }
}
