using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Integracao;

/// <summary>
/// Matriz 200/403 conforme R1 definitiva (24/11/2026):
/// - Operacao (Gestor + Administrador): todo o fluxo diario.
/// - Administracao (so Administrador): GET/POST /api/usuarios.
/// </summary>
public sealed class ControleAcessoApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = CriarOpcoesJson();

    [Fact]
    public async Task Administrador_e_gestor_operam_catalogo_e_apenas_administrador_gerencia_usuarios()
    {
        await using var factory = new ApiPdfFactory();
        await factory.PrepararSeedAsync();

        using var administrador = ClienteAutenticado(factory, PerfilUsuario.Administrador);
        using var gestor = ClienteAutenticado(factory, PerfilUsuario.Gestor);

        Assert.Equal(HttpStatusCode.OK, (await administrador.GetAsync("/api/produtos")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await administrador.PostAsJsonAsync("/api/produtos",
            new ProdutoRequest("Produto do Administrador", "Camiseta", null))).StatusCode);

        var criado = await administrador.PostAsJsonAsync("/api/usuarios",
            new CriarGestorRequest("Novo Gestor", "novo.gestor@example.com", "SenhaForte-2026!"));
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var usuario = await criado.Content.ReadFromJsonAsync<UsuarioAcessoDto>(JsonOptions);
        Assert.NotNull(usuario);
        Assert.Equal(PerfilUsuario.Gestor, usuario.Perfil);
        Assert.Equal("novo.gestor@example.com", usuario.Email);

        using var clienteNovoGestor = factory.CreateClient();
        var loginGestor = await clienteNovoGestor.PostAsJsonAsync("/api/auth/login",
            new { email = usuario.Email, senha = "SenhaForte-2026!", manterConectado = false });
        Assert.Equal(HttpStatusCode.OK, loginGestor.StatusCode);

        var usuarios = await administrador.GetFromJsonAsync<List<UsuarioAcessoDto>>("/api/usuarios", JsonOptions);
        Assert.NotNull(usuarios);
        Assert.Contains(usuarios, u => u.Id == usuario.Id);

        var duplicado = await administrador.PostAsJsonAsync("/api/usuarios",
            new CriarGestorRequest("Outro Nome", " NOVO.GESTOR@example.com ", "SenhaForte-2026!"));
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);

        var senhaInvalida = await administrador.PostAsJsonAsync("/api/usuarios",
            new CriarGestorRequest("Outro Gestor", "outro.gestor@example.com", "curta"));
        Assert.Equal(HttpStatusCode.BadRequest, senhaInvalida.StatusCode);

        var responsaveis = await gestor.GetFromJsonAsync<List<UsuarioDto>>("/api/usuarios/responsaveis", JsonOptions);
        Assert.NotNull(responsaveis);
        Assert.Contains(responsaveis, u => u.Perfil == PerfilUsuario.Gestor);
        Assert.Contains(responsaveis, u => u.Perfil == PerfilUsuario.Administrador);
        Assert.DoesNotContain(responsaveis, u => u.Perfil is not (PerfilUsuario.Gestor or PerfilUsuario.Administrador));

        Assert.Equal(HttpStatusCode.Created, (await gestor.PostAsJsonAsync("/api/produtos",
            new ProdutoRequest("Produto do Gestor", "Camiseta", null))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await gestor.GetAsync("/api/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await gestor.PostAsJsonAsync("/api/usuarios",
            new CriarGestorRequest("Gestor indevido", "indevido@example.com", "SenhaForte-2026!"))).StatusCode);
    }

    [Theory]
    [InlineData(PerfilUsuario.Atendente)]
    [InlineData(PerfilUsuario.Operador)]
    public async Task Perfis_legados_preservam_dados_mas_nao_autenticam_no_novo_contrato(PerfilUsuario perfil)
    {
        await using var factory = new ApiPdfFactory();
        await factory.PrepararSeedAsync();
        using var cliente = factory.CreateClient();

        var login = await cliente.PostAsJsonAsync("/api/auth/login",
            new { email = "maria.silva@zalekpersonalizados.com.br", senha = "Zalek@2026", manterConectado = false });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        var erro = await login.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("CREDENCIAIS_INVALIDAS", erro.GetProperty("code").GetString());

        using var clienteLegado = ClienteAutenticado(factory, perfil);
        Assert.Equal(HttpStatusCode.Forbidden, (await clienteLegado.GetAsync("/api/produtos")).StatusCode);
    }

    [Fact]
    public async Task Gestor_e_administrador_consultam_e_gerenciam_fichas_tecnicas()
    {
        await using var factory = new ApiPdfFactory();
        await factory.PrepararSeedAsync();
        using var administrador = ClienteAutenticado(factory, PerfilUsuario.Administrador);
        using var gestor = ClienteAutenticado(factory, PerfilUsuario.Gestor);

        Assert.Equal(HttpStatusCode.OK, (await administrador.GetAsync("/api/estoque/fichas-tecnicas")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await gestor.GetAsync("/api/estoque/fichas-tecnicas")).StatusCode);

        Assert.NotEqual(HttpStatusCode.Forbidden, (await administrador.PutAsJsonAsync(
            $"/api/estoque/fichas-tecnicas/{Guid.NewGuid()}",
            new { produtoId = Guid.NewGuid(), materiais = Array.Empty<object>() })).StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, (await gestor.PutAsJsonAsync(
            $"/api/estoque/fichas-tecnicas/{Guid.NewGuid()}",
            new { produtoId = Guid.NewGuid(), materiais = Array.Empty<object>() })).StatusCode);
    }

    [Theory]
    [InlineData("admin@zalekpersonalizados.com.br", PerfilUsuario.Administrador)]
    [InlineData("gestao@zalekpersonalizados.com.br", PerfilUsuario.Gestor)]
    public async Task Orcamento_define_responsavel_autenticado_e_preserva_condicao_nula_na_edicao(string email, PerfilUsuario perfil)
    {
        await using var factory = new ApiPdfFactory(numerador: new NumeradorTeste());
        await factory.PrepararSeedAsync();
        Guid clienteId, produtoId, gestorId, administradorId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Zalek360.Infrastructure.Persistence.ZalekDbContext>();
            clienteId = await db.Clientes.Select(c => c.Id).FirstAsync();
            produtoId = await db.Produtos.Select(p => p.Id).FirstAsync();
            gestorId = await db.Usuarios.Where(u => u.Perfil == PerfilUsuario.Gestor).Select(u => u.Id).FirstAsync();
            administradorId = await db.Usuarios.Where(u => u.Perfil == PerfilUsuario.Administrador).Select(u => u.Id).FirstAsync();
        }

        using var cliente = factory.CreateClient();
        var login = await cliente.PostAsJsonAsync("/api/auth/login",
            new { email, senha = "Zalek@2026", manterConectado = false });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = await login.Content.ReadFromJsonAsync<JsonElement>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());

        var request = new OrcamentoRequest(clienteId, perfil == PerfilUsuario.Administrador ? gestorId : administradorId, factory.Relogio.HojeLocal.AddDays(10), 10, null, null, 0,
            new[] { new ItemOrcamentoRequest(null, produtoId, null, 1, 25, 0, "Serigrafia", "Preta", null, null, null, null, null, Array.Empty<Guid>()) },
            null);
        var criadoResponse = await cliente.PostAsJsonAsync("/api/orcamentos", request);
        Assert.True(criadoResponse.StatusCode == HttpStatusCode.Created,
            $"Esperado 201, recebido {(int)criadoResponse.StatusCode}: {await criadoResponse.Content.ReadAsStringAsync()}");
        var criado = await criadoResponse.Content.ReadFromJsonAsync<OrcamentoDetalheDto>(JsonOptions);
        Assert.NotNull(criado);
        var responsavelEsperado = perfil == PerfilUsuario.Administrador ? administradorId : gestorId;
        Assert.Equal(responsavelEsperado, criado.ResponsavelId);
        Assert.Null(criado.CondicaoPagamento);

        var atualizadoResponse = await cliente.PutAsJsonAsync($"/api/orcamentos/{criado.Id}",
            request with { ResponsavelId = perfil == PerfilUsuario.Administrador ? gestorId : administradorId, ObservacoesComerciais = "Atualizado", Versao = criado.Versao });
        Assert.Equal(HttpStatusCode.OK, atualizadoResponse.StatusCode);
        var atualizado = await atualizadoResponse.Content.ReadFromJsonAsync<OrcamentoDetalheDto>(JsonOptions);
        Assert.NotNull(atualizado);
        Assert.Equal(responsavelEsperado, atualizado.ResponsavelId);
        Assert.Null(atualizado.CondicaoPagamento);
    }

    [Fact]
    public async Task Gestor_e_administrador_operam_fluxo_diario_e_apenas_administrador_acessa_usuarios()
    {
        await using var factory = new ApiPdfFactory();
        await factory.PrepararSeedAsync();
        using var administrador = ClienteAutenticado(factory, PerfilUsuario.Administrador);
        using var gestor = ClienteAutenticado(factory, PerfilUsuario.Gestor);

        // Ambos consultam todos os modulos operacionais, incluindo dashboard
        var consultas = new[]
        {
            "/api/clientes",
            "/api/orcamentos",
            "/api/pedidos",
            "/api/dashboard",
            "/api/produtos",
            "/api/estoque/materias-primas",
            "/api/estoque/compras",
            "/api/estoque/fichas-tecnicas"
        };
        foreach (var caminho in consultas)
        {
            Assert.Equal(HttpStatusCode.OK, (await administrador.GetAsync(caminho)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await gestor.GetAsync(caminho)).StatusCode);
        }

        var id = Guid.NewGuid();

        // IDs inexistentes e corpos inválidos devem chegar à regra de negócio/validação,
        // sem bloqueio de perfil. Conferir o status exato também evita aceitar 500/405.
        var operacoesFluxoDiario = new (HttpMethod Metodo, string Caminho)[]
        {
            (HttpMethod.Post, "/api/anexos"),
            (HttpMethod.Delete, $"/api/anexos/{id}"),
            (HttpMethod.Post, "/api/clientes"),
            (HttpMethod.Put, $"/api/clientes/{id}"),
            (HttpMethod.Post, $"/api/clientes/{id}/observacoes"),
            (HttpMethod.Post, "/api/orcamentos"),
            (HttpMethod.Post, $"/api/orcamentos/{id}/exportacoes"),
            (HttpMethod.Put, $"/api/orcamentos/{id}"),
            (HttpMethod.Post, $"/api/orcamentos/{id}/contatos"),
            (HttpMethod.Post, $"/api/orcamentos/{id}/aguardando-retorno"),
            (HttpMethod.Post, $"/api/orcamentos/{id}/decisao"),
            (HttpMethod.Post, "/api/pedidos"),
            (HttpMethod.Patch, $"/api/pedidos/{id}/status"),
            (HttpMethod.Post, $"/api/pedidos/{id}/materiais/ficha-tecnica"),
            (HttpMethod.Put, $"/api/pedidos/{id}/materiais"),
            (HttpMethod.Post, "/api/estoque/materias-primas"),
            (HttpMethod.Put, $"/api/estoque/materias-primas/{id}"),
            (HttpMethod.Patch, $"/api/estoque/materias-primas/{id}/status"),
            (HttpMethod.Put, $"/api/estoque/fichas-tecnicas/{id}"),
            (HttpMethod.Post, "/api/estoque/compras"),
            (HttpMethod.Patch, $"/api/estoque/compras/{id}/situacao"),
            (HttpMethod.Post, "/api/produtos")
        };

        foreach (var (metodo, caminho) in operacoesFluxoDiario)
        {
            foreach (var (httpClient, perfil) in new[] { (administrador, "Administrador"), (gestor, "Gestor") })
            {
                using var requisicao = new HttpRequestMessage(metodo, caminho);
                if (caminho == "/api/anexos")
                    requisicao.Content = new MultipartFormDataContent();
                else if (metodo != HttpMethod.Delete &&
                         caminho != $"/api/orcamentos/{id}/exportacoes" &&
                         caminho != $"/api/pedidos/{id}/materiais/ficha-tecnica")
                    requisicao.Content = caminho == $"/api/pedidos/{id}/materiais"
                        ? JsonContent.Create(Array.Empty<object>())
                        : JsonContent.Create(new { });
                var resposta = await httpClient.SendAsync(requisicao);
                var esperado = caminho.Contains(id.ToString()) && !caminho.Contains("/fichas-tecnicas/")
                        ? HttpStatusCode.NotFound
                        : caminho == "/api/pedidos" ? HttpStatusCode.UnprocessableEntity : HttpStatusCode.BadRequest;
                Assert.True(resposta.StatusCode == esperado,
                    $"{metodo} {caminho} respondeu {(int)resposta.StatusCode} para {perfil}, esperado {(int)esperado}.");
            }
        }

        // Exclusivos do Administrador: GET e POST /api/usuarios retornam 403 para Gestor
        Assert.Equal(HttpStatusCode.Forbidden, (await gestor.GetAsync("/api/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await gestor.PostAsJsonAsync("/api/usuarios",
            new CriarGestorRequest("Indevido", "indevido@example.com", "SenhaForte-2026!"))).StatusCode);

        // Administrador acessa usuarios normalmente
        Assert.Equal(HttpStatusCode.OK, (await administrador.GetAsync("/api/usuarios")).StatusCode);
    }

    private static HttpClient ClienteAutenticado(ApiPdfFactory factory, PerfilUsuario perfil)
    {
        var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var usuario = Usuario.Criar("Usuário de teste", $"{perfil.ToString().ToLowerInvariant()}@example.com", "hash",
            perfil, factory.Relogio.AgoraUtc);
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>().Gerar(usuario, false).Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed class NumeradorTeste : INumeradorDocumentos
    {
        public Task<int> ProximoNumeroOrcamentoAsync(CancellationToken ct) => Task.FromResult(124);
        public Task<int> ProximoNumeroPedidoAsync(CancellationToken ct) => Task.FromResult(57);
    }

    private static JsonSerializerOptions CriarOpcoesJson()
    {
        var opcoes = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        opcoes.Converters.Add(new JsonStringEnumConverter());
        return opcoes;
    }
}
