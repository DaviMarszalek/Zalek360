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
            new ProdutoRequest("Produto do Administrador", "Teste", null))).StatusCode);

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
            new ProdutoRequest("Produto do Gestor", "Teste", null))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await gestor.GetAsync("/api/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await gestor.PostAsJsonAsync("/api/usuarios",
            new CriarGestorRequest("Gestor indevido", "indevido@example.com", "SenhaForte-2026!"))).StatusCode);
    }

    [Fact]
    public async Task Perfis_legados_preservam_dados_mas_nao_autenticam_no_novo_contrato()
    {
        await using var factory = new ApiPdfFactory();
        await factory.PrepararSeedAsync();
        using var cliente = factory.CreateClient();

        var login = await cliente.PostAsJsonAsync("/api/auth/login",
            new { email = "maria.silva@zalekpersonalizados.com.br", senha = "Zalek@2026", manterConectado = false });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        var erro = await login.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("CREDENCIAIS_INVALIDAS", erro.GetProperty("code").GetString());

        using var clienteLegado = ClienteAutenticado(factory, PerfilUsuario.Atendente);
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

    [Fact]
    public async Task Orcamento_define_responsavel_autenticado_e_preserva_condicao_nula_na_edicao()
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
            new { email = "admin@zalekpersonalizados.com.br", senha = "Zalek@2026", manterConectado = false });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = await login.Content.ReadFromJsonAsync<JsonElement>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());

        var request = new OrcamentoRequest(clienteId, gestorId, factory.Relogio.HojeLocal.AddDays(10), 10, null, null, 0,
            new[] { new ItemOrcamentoRequest(null, produtoId, null, 1, 25, 0, "Serigrafia", "Preta", null, null, null, null, null, Array.Empty<Guid>()) },
            null);
        var criadoResponse = await cliente.PostAsJsonAsync("/api/orcamentos", request);
        Assert.True(criadoResponse.StatusCode == HttpStatusCode.Created,
            $"Esperado 201, recebido {(int)criadoResponse.StatusCode}: {await criadoResponse.Content.ReadAsStringAsync()}");
        var criado = await criadoResponse.Content.ReadFromJsonAsync<OrcamentoDetalheDto>(JsonOptions);
        Assert.NotNull(criado);
        Assert.Equal(administradorId, criado.ResponsavelId);
        Assert.Null(criado.CondicaoPagamento);

        var atualizadoResponse = await cliente.PutAsJsonAsync($"/api/orcamentos/{criado.Id}",
            request with { ResponsavelId = gestorId, ObservacoesComerciais = "Atualizado", Versao = criado.Versao });
        Assert.Equal(HttpStatusCode.OK, atualizadoResponse.StatusCode);
        var atualizado = await atualizadoResponse.Content.ReadFromJsonAsync<OrcamentoDetalheDto>(JsonOptions);
        Assert.NotNull(atualizado);
        Assert.Equal(administradorId, atualizado.ResponsavelId);
        Assert.Null(atualizado.CondicaoPagamento);
    }

    [Fact]
    public async Task Gestor_consulta_modulos_comerciais_mas_so_operar_catalogo_e_estoque()
    {
        await using var factory = new ApiPdfFactory();
        await factory.PrepararSeedAsync();
        using var administrador = ClienteAutenticado(factory, PerfilUsuario.Administrador);
        using var gestor = ClienteAutenticado(factory, PerfilUsuario.Gestor);

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
            if (caminho == "/api/dashboard")
                Assert.Equal(HttpStatusCode.Forbidden, (await gestor.GetAsync(caminho)).StatusCode);
            else
                Assert.Equal(HttpStatusCode.OK, (await gestor.GetAsync(caminho)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Created, (await gestor.PostAsJsonAsync("/api/estoque/materias-primas",
            new MateriaPrimaRequest("Material cadastrado pelo gestor", "un", 0, 5))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await gestor.PostAsJsonAsync("/api/produtos",
            new ProdutoRequest("Produto cadastrado pelo gestor", "Teste", null))).StatusCode);

        var id = Guid.NewGuid();
        var operacoesAdmin = new (HttpMethod Metodo, string Caminho)[]
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
            (HttpMethod.Patch, $"/api/pedidos/{id}/status"),
            (HttpMethod.Post, "/api/estoque/materias-primas"),
            (HttpMethod.Put, $"/api/estoque/materias-primas/{id}"),
            (HttpMethod.Patch, $"/api/estoque/materias-primas/{id}/status"),
            (HttpMethod.Post, "/api/estoque/compras"),
            (HttpMethod.Patch, $"/api/estoque/compras/{id}/situacao"),
            (HttpMethod.Post, $"/api/pedidos/{id}/materiais/ficha-tecnica"),
            (HttpMethod.Put, $"/api/pedidos/{id}/materiais"),
            (HttpMethod.Post, "/api/produtos")
        };

        foreach (var (metodo, caminho) in operacoesAdmin)
        {
            using var requisicao = new HttpRequestMessage(metodo, caminho);
            if (caminho == "/api/anexos")
                requisicao.Content = new MultipartFormDataContent();
            else if (metodo != HttpMethod.Delete &&
                     caminho != "/api/orcamentos/" + id + "/exportacoes")
                requisicao.Content = JsonContent.Create(new { });
            Assert.NotEqual(HttpStatusCode.Forbidden, (await administrador.SendAsync(requisicao)).StatusCode);
        }

        var operacoesGestor = new (HttpMethod Metodo, string Caminho)[]
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
            (HttpMethod.Get, "/api/usuarios"),
            (HttpMethod.Post, "/api/usuarios")
        };
        foreach (var (metodo, caminho) in operacoesGestor)
        {
            using var requisicao = new HttpRequestMessage(metodo, caminho);
            if (caminho == "/api/anexos")
                requisicao.Content = new MultipartFormDataContent();
            else if (metodo != HttpMethod.Delete && metodo != HttpMethod.Get &&
                     caminho != "/api/pedidos/" + id + "/materiais/ficha-tecnica" &&
                     caminho != "/api/pedidos")
                requisicao.Content = caminho == $"/api/pedidos/{id}/materiais"
                    ? JsonContent.Create(Array.Empty<object>())
                    : JsonContent.Create(new { });
            var resposta = await gestor.SendAsync(requisicao);
            Assert.True(resposta.StatusCode == HttpStatusCode.Forbidden,
                $"{metodo} {caminho} respondeu {(int)resposta.StatusCode}, esperado 403.");
        }
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
