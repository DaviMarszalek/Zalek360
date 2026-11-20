using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Persistence;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Integracao;

public sealed class ProdutosPersonalizadosApiTests
{
    [Fact]
    public async Task Api_cadastra_produto_unico_e_lista_status_da_ficha_vinculada()
    {
        await using var factory = new ApiPdfFactory();
        await factory.PrepararSeedAsync();
        using var client = ClienteAutenticado(factory, PerfilUsuario.Gestor);

        var respostaCriacao = await client.PostAsJsonAsync("/api/produtos",
            new ProdutoRequest("Caneca de teste", "Caneca", "Porcelana branca"));
        Assert.Equal(HttpStatusCode.Created, respostaCriacao.StatusCode);
        var produto = await respostaCriacao.Content.ReadFromJsonAsync<ProdutoDto>();
        Assert.NotNull(produto);
        Assert.Equal("Caneca de teste", produto.Nome);
        Assert.Equal("Caneca", produto.Categoria);

        var listaInicial = await client.GetFromJsonAsync<List<System.Text.Json.JsonElement>>("/api/estoque/fichas-tecnicas");
        Assert.NotNull(listaInicial);
        var produtoPendente = Assert.Single(listaInicial, x => x.GetProperty("produtoId").GetGuid() == produto.Id);
        Assert.False(produtoPendente.GetProperty("temFicha").GetBoolean());

        var duplicado = await client.PostAsJsonAsync("/api/produtos",
            new ProdutoRequest("  caneca DE TESTE ", "Caneca", null));
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
        var erroDuplicado = await duplicado.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("REGISTRO_DUPLICADO", erroDuplicado.GetProperty("code").GetString());
        Assert.True(erroDuplicado.GetProperty("errors").TryGetProperty("nome", out _));

        var invalido = await client.PostAsJsonAsync("/api/produtos", new ProdutoRequest("", "", new string('x', 501)));
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
        var erroValidacao = await invalido.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(400, erroValidacao.GetProperty("status").GetInt32());
        Assert.True(erroValidacao.GetProperty("errors").TryGetProperty("nome", out _));
        Assert.True(erroValidacao.GetProperty("errors").TryGetProperty("categoria", out _));
        Assert.True(erroValidacao.GetProperty("errors").TryGetProperty("descricaoBase", out _));

        Guid materiaId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZalekDbContext>();
            var materia = MateriaPrima.Criar("Material da ficha API", "m", 12, 0);
            db.MateriasPrimas.Add(materia);
            await db.SaveChangesAsync();
            materiaId = materia.Id;
        }

        var fichaSalva = await client.PutAsJsonAsync($"/api/estoque/fichas-tecnicas/{produto.Id}",
            new { produtoId = produto.Id, materiais = new[] { new { materiaPrimaId = materiaId, quantidadePorUnidade = 2 } } });
        Assert.Equal(HttpStatusCode.OK, fichaSalva.StatusCode);

        var lista = await client.GetFromJsonAsync<List<System.Text.Json.JsonElement>>("/api/estoque/fichas-tecnicas");
        Assert.NotNull(lista);
        var produtoListado = Assert.Single(lista, x => x.GetProperty("produtoId").GetGuid() == produto.Id);
        Assert.True(produtoListado.GetProperty("temFicha").GetBoolean());
        var linha = Assert.Single(produtoListado.GetProperty("materiais").EnumerateArray());
        Assert.Equal("un", linha.GetProperty("unidade").GetString());
        Assert.Equal(2m, linha.GetProperty("quantidadePorUnidade").GetDecimal());

        Assert.Contains(lista, x => !x.GetProperty("temFicha").GetBoolean());

        client.DefaultRequestHeaders.Authorization = Token(factory, PerfilUsuario.Atendente);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/estoque/fichas-tecnicas")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/estoque/fichas-tecnicas/{produto.Id}",
            new { produtoId = produto.Id, materiais = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/produtos",
            new ProdutoRequest("Produto sem permissão", "Teste", null))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/produtos")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/pedidos/{Guid.NewGuid()}/materiais")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/pedidos/{Guid.NewGuid()}/materiais",
            Array.Empty<ReservaPedidoRequest>())).StatusCode);

        client.DefaultRequestHeaders.Authorization = Token(factory, PerfilUsuario.Gestor);
        var catalogo = await client.GetFromJsonAsync<List<ProdutoDto>>("/api/produtos");
        Assert.Contains(catalogo!, x => x.Id == produto.Id);
    }

    [Theory]
    [InlineData("m")]
    [InlineData("kg")]
    public async Task Api_rejeita_unidade_de_materia_prima_diferente_de_un(string unidade)
    {
        await using var factory = new ApiPdfFactory();
        await factory.PrepararSeedAsync();
        using var client = ClienteAutenticado(factory, PerfilUsuario.Gestor);

        var resposta = await client.PostAsJsonAsync("/api/estoque/materias-primas",
            new { nome = "Material com unidade inválida", unidade, quantidadeDisponivel = 2, estoqueMinimo = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var erro = await resposta.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(erro.GetProperty("errors").TryGetProperty("unidade", out _));
    }

    [Fact]
    public async Task Api_aceita_unidade_omitida_grava_un_e_rejeita_quantidades_fracionarias()
    {
        await using var factory = new ApiPdfFactory();
        await factory.PrepararSeedAsync();
        using var client = ClienteAutenticado(factory, PerfilUsuario.Gestor);

        var criado = await client.PostAsJsonAsync("/api/estoque/materias-primas",
            new { nome = "Material por unidade", quantidadeDisponivel = 0, estoqueMinimo = 0 });
        Assert.True(criado.StatusCode == HttpStatusCode.Created,
            $"Esperado HTTP 201, recebido {(int)criado.StatusCode}: {await criado.Content.ReadAsStringAsync()}");
        var material = await criado.Content.ReadFromJsonAsync<MateriaPrimaDto>();
        Assert.NotNull(material);
        Assert.Equal("un", material.Unidade);
        Assert.Equal(0m, material.QuantidadeDisponivel);

        var fracionario = await client.PostAsJsonAsync("/api/estoque/materias-primas",
            new { nome = "Material fracionário", quantidadeDisponivel = 1.5m, estoqueMinimo = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, fracionario.StatusCode);
        var erro = await fracionario.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(erro.GetProperty("errors").TryGetProperty("quantidadeDisponivel", out _));
    }

    private static HttpClient ClienteAutenticado(ApiPdfFactory factory, PerfilUsuario perfil)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Token(factory, perfil);
        return client;
    }

    private static AuthenticationHeaderValue Token(ApiPdfFactory factory, PerfilUsuario perfil)
    {
        using var scope = factory.Services.CreateScope();
        var usuario = Usuario.Criar("Usuário de teste", $"{perfil.ToString().ToLowerInvariant()}@example.com", "hash",
            perfil, factory.Relogio.AgoraUtc);
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>().Gerar(usuario, false).Token;
        return new AuthenticationHeaderValue("Bearer", token);
    }
}
