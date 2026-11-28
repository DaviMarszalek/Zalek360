using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Application.Services;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.ConsultaCnpj;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Tests.Integracao;

/// <summary>
/// Simula os três provedores públicos por host. Nenhuma chamada real: host sem resposta configurada falha o teste.
/// </summary>
public sealed class ProvedoresCnpjFalsos : HttpMessageHandler
{
    private readonly object _trava = new();
    private readonly List<Uri> _chamadas = new();
    public Dictionary<string, Func<CancellationToken, Task<HttpResponseMessage>>> Respostas { get; } = new();

    public IReadOnlyList<Uri> Chamadas { get { lock (_trava) return _chamadas.ToList(); } }
    public int ChamadasA(string host) => Chamadas.Count(u => u.Host == host);

    public void Json(string host, string json, HttpStatusCode status = HttpStatusCode.OK) =>
        Respostas[host] = _ => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    public void Status(string host, HttpStatusCode status) =>
        Respostas[host] = _ => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent("{}") });

    public void Demora(string host) => Respostas[host] = async ct =>
    {
        await Task.Delay(Timeout.Infinite, ct);
        throw new InvalidOperationException("inalcançável");
    };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        lock (_trava) _chamadas.Add(request.RequestUri!);
        return Respostas.TryGetValue(request.RequestUri!.Host, out var resposta)
            ? resposta(ct)
            : throw new InvalidOperationException($"Chamada inesperada a {request.RequestUri}");
    }

    public static readonly string[] Clientes = { ProvedorBrasilApi.Cliente, ProvedorCnpja.Cliente, ProvedorCnpjWs.Cliente };

    /// <summary>Troca somente o handler primário dos HttpClients nomeados (hosts e cabeçalhos continuam os de produção).</summary>
    public void Substituir(IServiceCollection services)
    {
        foreach (var nome in Clientes) services.AddHttpClient(nome).ConfigurePrimaryHttpMessageHandler(() => this);
    }
}

public class ConsultaCnpjTests
{
    // CNPJ de exemplo da própria documentação da BrasilAPI (Open Knowledge Brasil).
    private const string Cnpj = "19131243000197";
    private const string BrasilApi = "brasilapi.com.br";
    private const string Cnpja = "open.cnpja.com";
    private const string CnpjWs = "publica.cnpj.ws";

    private const string JsonBrasilApi = """
        {"cnpj":"19131243000197","razao_social":"OPEN KNOWLEDGE BRASIL","nome_fantasia":"REDE PELO CONHECIMENTO LIVRE",
         "ddd_telefone_1":"(11) 2385-1939","ddd_telefone_2":"","email":null,"cep":"01311-902",
         "descricao_tipo_de_logradouro":"AVENIDA","logradouro":"PAULISTA","numero":"37","complemento":"ANDAR 4",
         "bairro":"BELA VISTA","municipio":"SAO PAULO","uf":"sp"}
        """;

    private static string JsonCnpja(string registros = ""","registrations":[{"number":"123456789119","state":"SP","enabled":true}]""") => $$"""
        {"taxId":"19131243000197","alias":"Rede pelo Conhecimento Livre","company":{"name":"Open Knowledge Brasil"},
         "phones":[{"area":"11","number":"23851939"}],"emails":[{"address":"contato@example.org"}],
         "address":{"street":"Avenida Paulista","number":"37","details":"Andar 4","district":"Bela Vista","city":"São Paulo","state":"SP","zip":"01311902"}{{registros}}}
        """;

    private const string JsonCnpjWsVarias = """
        {"razao_social":"OPEN KNOWLEDGE BRASIL","estabelecimento":{"inscricoes_estaduais":[
          {"inscricao_estadual":"84295760","ativo":false,"estado":{"sigla":"RJ"}},
          {"inscricao_estadual":"113717433112","ativo":true,"estado":{"sigla":"sp"}},
          {"inscricao_estadual":"84347353","ativo":false,"estado":{"sigla":"RJ"}},
          {"inscricao_estadual":"","ativo":true,"estado":{"sigla":"MG"}}]}}
        """;

    private static (IConsultaCnpj Consulta, ServiceProvider Provedor) Criar(ProvedoresCnpjFalsos falsos, TimeSpan? timeout = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AdicionarConsultaCnpj();
        falsos.Substituir(services);
        var sp = services.BuildServiceProvider();
        IConsultaCnpj consulta = timeout is null
            ? sp.GetRequiredService<IConsultaCnpj>()
            : new ConsultaCnpjPublica(sp.GetRequiredService<IHttpClientFactory>(), sp.GetRequiredService<IMemoryCache>(),
                NullLogger<ConsultaCnpjPublica>.Instance, timeout.Value);
        return (consulta, sp);
    }

    [Fact]
    public async Task Sucesso_completo_usa_brasilapi_para_dados_e_cnpja_para_ie()
    {
        var falsos = new ProvedoresCnpjFalsos();
        falsos.Json(BrasilApi, JsonBrasilApi);
        falsos.Json(Cnpja, JsonCnpja());
        var (consulta, sp) = Criar(falsos);
        using var _ = sp;

        var r = await consulta.ConsultarAsync(Cnpj, default);

        Assert.True(r.Encontrado);
        Assert.Equal("BrasilAPI + CNPJá", r.Fonte);
        Assert.Equal("OPEN KNOWLEDGE BRASIL", r.RazaoSocial);
        Assert.Equal("REDE PELO CONHECIMENTO LIVRE", r.NomeFantasia);
        Assert.Equal("1123851939", r.Telefone);
        Assert.Null(r.Email);
        Assert.Equal(new EnderecoDto("01311902", "AVENIDA PAULISTA", "37", "ANDAR 4", "BELA VISTA", "SAO PAULO", "SP"), r.Endereco);
        Assert.Equal(new InscricaoEstadualDto("123456789119", "SP", true), Assert.Single(r.InscricoesEstaduais));
        Assert.Null(r.Mensagem);
        Assert.Equal(new[] { new Uri($"https://brasilapi.com.br/api/cnpj/v1/{Cnpj}"), new Uri($"https://open.cnpja.com/office/{Cnpj}") }
            .OrderBy(u => u.ToString()), falsos.Chamadas.OrderBy(u => u.ToString()));
    }

    [Fact]
    public async Task Sucesso_sem_ie_consulta_cnpjws_e_devolve_lista_vazia()
    {
        var falsos = new ProvedoresCnpjFalsos();
        falsos.Json(BrasilApi, JsonBrasilApi);
        falsos.Json(Cnpja, JsonCnpja(""","registrations":[]"""));
        falsos.Json(CnpjWs, """{"razao_social":"OPEN KNOWLEDGE BRASIL","estabelecimento":{"inscricoes_estaduais":[]}}""");
        var (consulta, sp) = Criar(falsos);
        using var _ = sp;

        var r = await consulta.ConsultarAsync(Cnpj, default);

        Assert.True(r.Encontrado);
        Assert.Equal("BrasilAPI", r.Fonte);
        Assert.Empty(r.InscricoesEstaduais);
        Assert.Null(r.Mensagem);
        Assert.Equal(new Uri($"https://publica.cnpj.ws/cnpj/{Cnpj}"), Assert.Single(falsos.Chamadas, u => u.Host == CnpjWs));
    }

    [Fact]
    public async Task Varias_ies_sao_todas_devolvidas_com_as_ativas_primeiro()
    {
        var falsos = new ProvedoresCnpjFalsos();
        falsos.Json(BrasilApi, JsonBrasilApi);
        falsos.Json(Cnpja, JsonCnpja("")); // sem o campo "registrations": cai para a CNPJ.ws
        falsos.Json(CnpjWs, JsonCnpjWsVarias);
        var (consulta, sp) = Criar(falsos);
        using var _ = sp;

        var r = await consulta.ConsultarAsync(Cnpj, default);

        Assert.Equal("BrasilAPI + CNPJ.ws", r.Fonte);
        Assert.Equal(new[]
        {
            new InscricaoEstadualDto("113717433112", "SP", true),
            new InscricaoEstadualDto("84295760", "RJ", false),
            new InscricaoEstadualDto("84347353", "RJ", false)
        }, r.InscricoesEstaduais);
    }

    [Fact]
    public async Task Varias_ies_da_cnpja_dispensam_a_cnpjws()
    {
        var falsos = new ProvedoresCnpjFalsos();
        falsos.Json(BrasilApi, JsonBrasilApi);
        falsos.Json(Cnpja, JsonCnpja("""
            ,"registrations":[{"number":"111","state":"rj","enabled":false},{"number":"222","state":"SP","enabled":true},{"number":"333","state":"MG"}]
            """));
        var (consulta, sp) = Criar(falsos);
        using var _ = sp;

        var r = await consulta.ConsultarAsync(Cnpj, default);

        Assert.Equal(new[] { "222/SP/True", "111/RJ/False", "333/MG/False" }, r.InscricoesEstaduais.Select(i => $"{i.Numero}/{i.Uf}/{i.Ativa}"));
        Assert.Equal(0, falsos.ChamadasA(CnpjWs));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Timeout_no_primeiro_provedor_usa_o_segundo(bool timeoutReal)
    {
        var falsos = new ProvedoresCnpjFalsos();
        if (timeoutReal) falsos.Demora(BrasilApi);
        else falsos.Respostas[BrasilApi] = _ => throw new TaskCanceledException("HttpClient.Timeout");
        falsos.Json(Cnpja, JsonCnpja());
        var (consulta, sp) = Criar(falsos, TimeSpan.FromSeconds(1.5));
        using var _ = sp;

        var r = await consulta.ConsultarAsync(Cnpj, default);

        Assert.True(r.Encontrado);
        Assert.Equal("CNPJá", r.Fonte);
        Assert.Equal("Open Knowledge Brasil", r.RazaoSocial);
        Assert.Equal("Rede pelo Conhecimento Livre", r.NomeFantasia);
        Assert.Equal("1123851939", r.Telefone);
        Assert.Equal("contato@example.org", r.Email);
        Assert.Equal(new EnderecoDto("01311902", "Avenida Paulista", "37", "Andar 4", "Bela Vista", "São Paulo", "SP"), r.Endereco);
        Assert.Single(r.InscricoesEstaduais);
        Assert.Equal(1, falsos.ChamadasA(Cnpja)); // CNPJá chamada uma única vez (dados + IE)

        await consulta.ConsultarAsync(Cnpj, default);
        Assert.Equal(2, falsos.ChamadasA(BrasilApi)); // resultado obtido após timeout não vai para o cache
    }

    [Fact]
    public async Task Todos_os_provedores_falham_responde_nao_encontrado_com_mensagem_amigavel()
    {
        var falsos = new ProvedoresCnpjFalsos();
        falsos.Status(BrasilApi, HttpStatusCode.InternalServerError);
        falsos.Respostas[Cnpja] = _ => throw new HttpRequestException("conexão recusada");
        var (consulta, sp) = Criar(falsos);
        using var _ = sp;

        var r = await consulta.ConsultarAsync(Cnpj, default);

        Assert.False(r.Encontrado);
        Assert.Equal(ConsultaCnpjPublica.MensagemFalha, r.Mensagem);
        Assert.Null(r.Fonte);
        Assert.Null(r.RazaoSocial);
        Assert.Empty(r.InscricoesEstaduais);
        Assert.Equal(0, falsos.ChamadasA(CnpjWs));
        await consulta.ConsultarAsync(Cnpj, default);
        Assert.Equal(2, falsos.ChamadasA(BrasilApi)); // falha não é guardada no cache
    }

    [Fact]
    public async Task Json_inesperado_conta_como_falha()
    {
        var falsos = new ProvedoresCnpjFalsos();
        falsos.Json(BrasilApi, "<html>erro</html>");
        falsos.Json(Cnpja, """{"company":null}""");
        var (consulta, sp) = Criar(falsos);
        using var _ = sp;

        var r = await consulta.ConsultarAsync(Cnpj, default);

        Assert.False(r.Encontrado);
        Assert.Equal(ConsultaCnpjPublica.MensagemFalha, r.Mensagem);
    }

    [Fact]
    public async Task Limite_429_responde_nao_encontrado_com_mensagem_de_limite()
    {
        var falsos = new ProvedoresCnpjFalsos();
        falsos.Status(BrasilApi, HttpStatusCode.TooManyRequests);
        falsos.Status(Cnpja, HttpStatusCode.TooManyRequests);
        var (consulta, sp) = Criar(falsos);
        using var _ = sp;

        var r = await consulta.ConsultarAsync(Cnpj, default);

        Assert.False(r.Encontrado);
        Assert.Equal(ConsultaCnpjPublica.MensagemLimite, r.Mensagem);
    }

    [Fact]
    public async Task Cnpj_inexistente_em_todas_as_bases_informa_nao_encontrado()
    {
        var falsos = new ProvedoresCnpjFalsos();
        falsos.Status(BrasilApi, HttpStatusCode.NotFound);
        falsos.Status(Cnpja, HttpStatusCode.NotFound);
        var (consulta, sp) = Criar(falsos);
        using var _ = sp;

        var r = await consulta.ConsultarAsync(Cnpj, default);

        Assert.False(r.Encontrado);
        Assert.Equal(ConsultaCnpjPublica.MensagemNaoEncontrado, r.Mensagem);
    }

    [Fact]
    public async Task Dados_encontrados_mas_ie_indisponivel_avisa_e_nao_guarda_no_cache()
    {
        var falsos = new ProvedoresCnpjFalsos();
        falsos.Json(BrasilApi, JsonBrasilApi);
        falsos.Status(Cnpja, HttpStatusCode.TooManyRequests);
        falsos.Status(CnpjWs, HttpStatusCode.TooManyRequests);
        var (consulta, sp) = Criar(falsos);
        using var _ = sp;

        var r = await consulta.ConsultarAsync(Cnpj, default);

        Assert.True(r.Encontrado);
        Assert.Equal("BrasilAPI", r.Fonte);
        Assert.Empty(r.InscricoesEstaduais);
        Assert.Equal(ConsultaCnpjPublica.MensagemSemIe, r.Mensagem);
        await consulta.ConsultarAsync(Cnpj, default);
        Assert.Equal(2, falsos.ChamadasA(BrasilApi));
    }

    [Fact]
    public async Task Cache_segunda_consulta_do_mesmo_cnpj_nao_vai_a_rede()
    {
        var falsos = new ProvedoresCnpjFalsos();
        falsos.Json(BrasilApi, JsonBrasilApi);
        falsos.Json(Cnpja, JsonCnpja());
        var (consulta, sp) = Criar(falsos);
        using var _ = sp;

        var primeira = await consulta.ConsultarAsync(Cnpj, default);
        var chamadas = falsos.Chamadas.Count;
        var segunda = await consulta.ConsultarAsync(Cnpj, default);

        Assert.Equal(2, chamadas);
        Assert.Equal(chamadas, falsos.Chamadas.Count);
        Assert.Same(primeira, segunda);
    }

    [Fact]
    public async Task Cancelamento_pelo_chamador_nao_vira_fallback()
    {
        var falsos = new ProvedoresCnpjFalsos();
        falsos.Demora(BrasilApi);
        falsos.Demora(Cnpja);
        var (consulta, sp) = Criar(falsos);
        using var _ = sp;
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consulta.ConsultarAsync(Cnpj, cts.Token));
    }

    [Theory]
    [InlineData("19131243000198")]        // DV errado
    [InlineData("1913124300019")]         // 13 dígitos
    [InlineData("191312430001970")]       // 15 dígitos
    [InlineData("00000000000000")]
    [InlineData("19.131.243/0001-9A")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Cnpj_invalido_retorna_erro_de_validacao_sem_chamar_a_rede(string? cnpj)
    {
        var falsos = new ProvedoresCnpjFalsos();
        var (consulta, sp) = Criar(falsos);
        using var _ = sp;

        var erro = await Assert.ThrowsAsync<ValidacaoException>(() => new ConsultaCnpjService(consulta).ConsultarAsync(cnpj, default));

        Assert.Equal(CodigosErro.CnpjInvalido, erro.Codigo);
        Assert.Equal(400, erro.StatusCode);
        Assert.Empty(falsos.Chamadas);
    }

    [Theory]
    [InlineData("19131243000197")]
    [InlineData("19.131.243/0001-97")]
    [InlineData("19.131.243%2F0001-97")]
    [InlineData(" 19131243/0001-97 ")]
    public void Cnpj_com_ou_sem_mascara_e_normalizado(string cnpj) =>
        Assert.Equal(Cnpj, ConsultaCnpjService.Normalizar(cnpj));
}

public class ConsultaCnpjApiTests
{
    private const string Cnpj = "19131243000197";

    [Theory]
    [InlineData("19131243000197")]
    [InlineData("19.131.243/0001-97")]
    [InlineData("19.131.243%2F0001-97")]
    public async Task Gestor_consulta_cnpj_com_ou_sem_mascara(string caminho)
    {
        var falsos = new ProvedoresCnpjFalsos();
        falsos.Json("brasilapi.com.br", """{"razao_social":"OPEN KNOWLEDGE BRASIL","uf":"SP"}""");
        falsos.Json("open.cnpja.com", """{"company":{"name":"Open Knowledge Brasil"},"registrations":[{"number":"123","state":"SP","enabled":true}]}""");
        await using var api = new ApiPdfFactory();
        await using var factory = api.WithWebHostBuilder(b => b.ConfigureTestServices(falsos.Substituir));
        using var cliente = await AutenticadoAsync(factory, PerfilUsuario.Gestor);

        var resposta = await cliente.GetAsync($"/api/clientes/cnpj/{caminho}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("encontrado").GetBoolean());
        Assert.Equal("OPEN KNOWLEDGE BRASIL", json.RootElement.GetProperty("razaoSocial").GetString());
        var ie = Assert.Single(json.RootElement.GetProperty("inscricoesEstaduais").EnumerateArray());
        Assert.Equal("123", ie.GetProperty("numero").GetString());
        Assert.Contains(new Uri($"https://brasilapi.com.br/api/cnpj/v1/{Cnpj}"), falsos.Chamadas);
    }

    [Theory]
    [InlineData("19131243000198")]
    [InlineData("19.131.243%2F0001-9X")]
    [InlineData("1913124300019")]
    public async Task Cnpj_invalido_retorna_400_no_envelope_padrao_sem_consultar_provedores(string caminho)
    {
        var falsos = new ProvedoresCnpjFalsos();
        await using var api = new ApiPdfFactory();
        await using var factory = api.WithWebHostBuilder(b => b.ConfigureTestServices(falsos.Substituir));
        using var cliente = await AutenticadoAsync(factory, PerfilUsuario.Administrador);

        var resposta = await cliente.GetAsync($"/api/clientes/cnpj/{caminho}");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(CodigosErro.CnpjInvalido, json.RootElement.GetProperty("code").GetString());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("cnpj", out _));
        Assert.Empty(falsos.Chamadas);
    }

    [Fact]
    public async Task Sem_autenticacao_retorna_401()
    {
        var falsos = new ProvedoresCnpjFalsos();
        await using var api = new ApiPdfFactory();
        await using var factory = api.WithWebHostBuilder(b => b.ConfigureTestServices(falsos.Substituir));
        using var cliente = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync($"/api/clientes/cnpj/{Cnpj}")).StatusCode);
        Assert.Empty(falsos.Chamadas);
    }

    private static async Task<HttpClient> AutenticadoAsync(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory, PerfilUsuario perfil)
    {
        var cliente = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ZalekDbContext>().Database.EnsureCreatedAsync();
        var usuario = Usuario.Criar("Usuário de teste", $"{perfil.ToString().ToLowerInvariant()}@example.com", "hash", perfil, DateTime.UtcNow);
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>().Gerar(usuario, false).Token;
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}
