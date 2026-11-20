using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    public async Task Gestor_opera_e_administrador_consulta_e_provisiona_conta()
    {
        await using var factory = new ApiPdfFactory();
        await factory.PrepararSeedAsync();

        using var administrador = ClienteAutenticado(factory, PerfilUsuario.Administrador);
        using var gestor = ClienteAutenticado(factory, PerfilUsuario.Gestor);

        Assert.Equal(HttpStatusCode.OK, (await administrador.GetAsync("/api/produtos")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await administrador.PostAsJsonAsync("/api/produtos",
            new ProdutoRequest("Produto sem permissão administrativa", "Teste", null))).StatusCode);

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
        Assert.DoesNotContain(responsaveis, u => u.Perfil != PerfilUsuario.Gestor);

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
    public async Task Gestor_e_administrador_compartilham_consulta_de_fichas_com_escrita_restrita_ao_gestor()
    {
        await using var factory = new ApiPdfFactory();
        await factory.PrepararSeedAsync();
        using var administrador = ClienteAutenticado(factory, PerfilUsuario.Administrador);
        using var gestor = ClienteAutenticado(factory, PerfilUsuario.Gestor);

        Assert.Equal(HttpStatusCode.OK, (await administrador.GetAsync("/api/estoque/fichas-tecnicas")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await gestor.GetAsync("/api/estoque/fichas-tecnicas")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await administrador.PutAsJsonAsync(
            $"/api/estoque/fichas-tecnicas/{Guid.NewGuid()}",
            new { produtoId = Guid.NewGuid(), materiais = Array.Empty<object>() })).StatusCode);
    }

    [Fact]
    public async Task Administrador_consulta_os_modulos_e_nao_executa_operacoes_de_negocio()
    {
        await using var factory = new ApiPdfFactory();
        await factory.PrepararSeedAsync();
        using var administrador = ClienteAutenticado(factory, PerfilUsuario.Administrador);

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
            Assert.Equal(HttpStatusCode.OK, (await administrador.GetAsync(caminho)).StatusCode);

        var id = Guid.NewGuid();
        var operacoes = new (HttpMethod Metodo, string Caminho)[]
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
            (HttpMethod.Put, $"/api/estoque/fichas-tecnicas/{id}"),
            (HttpMethod.Post, "/api/estoque/compras"),
            (HttpMethod.Patch, $"/api/estoque/compras/{id}/situacao"),
            (HttpMethod.Post, $"/api/pedidos/{id}/materiais/ficha-tecnica"),
            (HttpMethod.Put, $"/api/pedidos/{id}/materiais"),
            (HttpMethod.Post, "/api/produtos")
        };

        foreach (var (metodo, caminho) in operacoes)
        {
            using var requisicao = new HttpRequestMessage(metodo, caminho);
            if (caminho == "/api/anexos")
                requisicao.Content = new MultipartFormDataContent();
            else if (metodo != HttpMethod.Delete &&
                     caminho != "/api/orcamentos/" + id + "/exportacoes")
                requisicao.Content = JsonContent.Create(new { });
            Assert.Equal(HttpStatusCode.Forbidden, (await administrador.SendAsync(requisicao)).StatusCode);
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

    private static JsonSerializerOptions CriarOpcoesJson()
    {
        var opcoes = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        opcoes.Converters.Add(new JsonStringEnumConverter());
        return opcoes;
    }
}
