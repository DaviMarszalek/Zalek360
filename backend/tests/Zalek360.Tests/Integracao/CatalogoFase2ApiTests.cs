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
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Tests.Integracao;

public sealed class CatalogoFase2ApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private static HttpClient Cliente(ApiPdfFactory f, PerfilUsuario perfil)
    {
        var c = f.CreateClient();
        using var scope = f.Services.CreateScope();
        var u = scope.ServiceProvider.GetRequiredService<ZalekDbContext>().Usuarios.FirstOrDefault(x => x.Perfil == perfil)
            ?? Usuario.Criar("Teste", "teste@example.com", "hash", perfil, f.Relogio.AgoraUtc);
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", scope.ServiceProvider.GetRequiredService<ITokenService>().Gerar(u, false).Token);
        return c;
    }
    private static async Task<T> Ler<T>(HttpResponseMessage r)
    {
        Assert.True(r.IsSuccessStatusCode, $"HTTP {(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}");
        return (await r.Content.ReadFromJsonAsync<T>(Json))!;
    }

    [Theory]
    [InlineData(PerfilUsuario.Gestor)]
    [InlineData(PerfilUsuario.Administrador)]
    public async Task Catalogo_salva_composicao_duplica_controla_versao_e_inativa_sem_perder_dados(PerfilUsuario perfil)
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync();
        using var c = Cliente(f, perfil);
        var materia = await Ler<MateriaPrimaDto>(await c.PostAsJsonAsync("/api/estoque/materias-primas", new MateriaPrimaRequest("Material do catálogo", "un", 10, 0)));
        var request = new ProdutoRequest("Produto completo", "Caneca", "Descrição", 49.90m, 17.35m, [new(materia.Id, 2)]);
        var p = await Ler<ProdutoDto>(await c.PostAsJsonAsync("/api/produtos", request));
        Assert.Equal(49.90m, p.PrecoVenda); Assert.Equal(17.35m, p.Custo);
        Assert.Equal(2, Assert.Single(p.Materiais!).QuantidadePorUnidade);
        Assert.False(p.Usado);

        var copia = await Ler<ProdutoDto>(await c.PostAsJsonAsync($"/api/produtos/{p.Id}/duplicar", new DuplicarProdutoRequest("Cópia do produto")));
        Assert.NotEqual(p.Id, copia.Id); Assert.Equal(p.Custo, copia.Custo); Assert.Equal(p.PrecoVenda, copia.PrecoVenda);
        Assert.Equal(p.Materiais![0], Assert.Single(copia.Materiais!)); Assert.False(copia.Usado);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/produtos/{p.Id}/duplicar", new DuplicarProdutoRequest(" cópia DO produto "))).StatusCode);

        var atualizado = await Ler<ProdutoDto>(await c.PutAsJsonAsync($"/api/produtos/{p.Id}", request with { PrecoVenda = 55, Versao = p.Versao }));
        Assert.Equal(55, atualizado.PrecoVenda);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/produtos/{p.Id}", request with { Versao = p.Versao })).StatusCode);
        var invalido = request with { PrecoVenda = 99, Versao = atualizado.Versao, Materiais = [new(Guid.NewGuid(), 1)] };
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync($"/api/produtos/{p.Id}", invalido)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync($"/api/produtos/{p.Id}",
            invalido with { Materiais = [new(materia.Id, 1000000000)] })).StatusCode);
        var preservado = await Ler<ProdutoDto>(await c.GetAsync($"/api/produtos/{p.Id}"));
        Assert.Equal(55, preservado.PrecoVenda); Assert.Equal(atualizado.Versao, preservado.Versao);
        Assert.Single(preservado.Materiais!);

        var inativo = await Ler<ProdutoDto>(await c.PatchAsJsonAsync($"/api/produtos/{p.Id}/status", new SituacaoProdutoRequest(false, preservado.Versao)));
        Assert.False(inativo.Ativo);
        Assert.DoesNotContain(await Ler<List<ProdutoDto>>(await c.GetAsync("/api/produtos")), x => x.Id == p.Id);
        Assert.Contains(await Ler<List<ProdutoDto>>(await c.GetAsync("/api/produtos/catalogo?incluirInativos=true&busca=PRODUTO COMPLETO")), x => x.Id == p.Id && !x.Ativo);
        Assert.Equal(p.Custo, inativo.Custo); Assert.Single(inativo.Materiais!);
    }

    [Fact]
    public async Task Exclusao_exige_administrador_e_bloqueia_produto_usado_preservando_historico()
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync();
        using var gestor = Cliente(f, PerfilUsuario.Gestor); using var admin = Cliente(f, PerfilUsuario.Administrador);
        var p = await Ler<ProdutoDto>(await gestor.PostAsJsonAsync("/api/produtos", new ProdutoRequest("Nunca usado", "Caneca", null)));
        Assert.Equal(HttpStatusCode.Forbidden, (await gestor.DeleteAsync($"/api/produtos/{p.Id}?versao={p.Versao}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/produtos/{p.Id}?versao={p.Versao}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/produtos/{p.Id}")).StatusCode);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ZalekDbContext>();
        var id = await db.ItensOrcamento.Select(x => x.ProdutoId).FirstAsync();
        var usado = await Ler<ProdutoDto>(await admin.GetAsync($"/api/produtos/{id}"));
        var eventos = await db.HistoricoEventos.CountAsync(); var itens = await db.ItensOrcamento.CountAsync();
        Assert.True(usado.Usado);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/produtos/{id}?versao={usado.Versao}")).StatusCode);
        Assert.Equal(eventos, await db.HistoricoEventos.CountAsync()); Assert.Equal(itens, await db.ItensOrcamento.CountAsync());
    }

    [Theory]
    [InlineData(PerfilUsuario.Gestor)]
    [InlineData(PerfilUsuario.Atendente)]
    [InlineData(PerfilUsuario.Operador)]
    public async Task Configuracoes_exclusivas_do_administrador(PerfilUsuario perfil)
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync(); using var c = Cliente(f, perfil);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/configuracoes/opcoes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/configuracoes/opcoes", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PutAsJsonAsync($"/api/configuracoes/opcoes/{Guid.NewGuid()}", new { })).StatusCode);
    }

    [Fact]
    public async Task Opcoes_sao_persistidas_validam_duplicidade_e_desativam_sem_reescrever_historico()
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync(); using var admin = Cliente(f, PerfilUsuario.Administrador); using var gestor = Cliente(f, PerfilUsuario.Gestor);
        var r = new OpcaoCatalogoRequest(TipoOpcaoCatalogo.TipoPersonalizacao, "Nova técnica", true, 10);
        var o = await Ler<OpcaoCatalogoDto>(await admin.PostAsJsonAsync("/api/configuracoes/opcoes", r));
        Assert.Contains("Nova técnica", (await Ler<OpcoesOrcamentoDto>(await gestor.GetAsync("/api/produtos/opcoes"))).TiposPersonalizacao);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/configuracoes/opcoes", r with { Nome = " nova TÉCNICA " })).StatusCode);
        var desativada = await Ler<OpcaoCatalogoDto>(await admin.PutAsJsonAsync($"/api/configuracoes/opcoes/{o.Id}", r with { Ativo = false, Versao = o.Versao }));
        Assert.False(desativada.Ativo);
        Assert.DoesNotContain("Nova técnica", (await Ler<OpcoesOrcamentoDto>(await gestor.GetAsync("/api/produtos/opcoes"))).TiposPersonalizacao);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/configuracoes/opcoes/{o.Id}", r with { Versao = o.Versao })).StatusCode);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ZalekDbContext>();
        Assert.True(await db.OpcoesCatalogo.AnyAsync(x => x.Id == o.Id && !x.Ativo));
        Assert.Contains("Preço", await Ler<List<string>>(await gestor.GetAsync("/api/orcamentos/motivos-decisao")));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(10, -1)]
    [InlineData(-0.005, 1)]
    public async Task Preco_e_custo_invalidos_nao_sao_persistidos(decimal preco, decimal custo)
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync(); using var c = Cliente(f, PerfilUsuario.Gestor);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/produtos", new ProdutoRequest("Inválido", "Caneca", null, preco, custo))).StatusCode);
        Assert.Empty(await Ler<List<ProdutoDto>>(await c.GetAsync("/api/produtos/catalogo?busca=Inválido")));
    }

    /// <summary>Fase 7.3: regra única de dinheiro — mais de 2 casas são arredondadas (ponto médio afastando de zero).</summary>
    [Fact]
    public async Task Preco_e_custo_com_mais_de_duas_casas_sao_arredondados()
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync(); using var c = Cliente(f, PerfilUsuario.Gestor);
        var criado = await Ler<ProdutoDto>(await c.PostAsJsonAsync("/api/produtos", new ProdutoRequest("Arredondado", "Caneca", null, 10.555m, 2.344m)));
        Assert.Equal(10.56m, criado.PrecoVenda);
        Assert.Equal(2.34m, criado.Custo);
    }

    [Fact]
    public async Task Gestor_usa_apenas_categorias_ativas_e_edicao_preserva_categoria_historica()
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync();
        using var admin = Cliente(f, PerfilUsuario.Administrador); using var gestor = Cliente(f, PerfilUsuario.Gestor);
        var request = new ProdutoRequest("Produto categoria", "Categoria configurada", null);
        Assert.Equal(HttpStatusCode.BadRequest, (await gestor.PostAsJsonAsync("/api/produtos", request)).StatusCode);
        var categoria = await Ler<OpcaoCatalogoDto>(await admin.PostAsJsonAsync("/api/configuracoes/opcoes", new OpcaoCatalogoRequest(TipoOpcaoCatalogo.CategoriaProduto, request.Categoria)));
        var produto = await Ler<ProdutoDto>(await gestor.PostAsJsonAsync("/api/produtos", request));
        await Ler<OpcaoCatalogoDto>(await admin.PutAsJsonAsync($"/api/configuracoes/opcoes/{categoria.Id}", new OpcaoCatalogoRequest(categoria.Tipo, categoria.Nome, false, categoria.Ordem, categoria.Versao)));
        Assert.Equal(HttpStatusCode.BadRequest, (await gestor.PostAsJsonAsync("/api/produtos", request with { Nome = "Novo produto" })).StatusCode);
        var atualizado = await Ler<ProdutoDto>(await gestor.PutAsJsonAsync($"/api/produtos/{produto.Id}", request with { DescricaoBase = "Preservado", Versao = produto.Versao }));
        Assert.Equal(categoria.Nome, atualizado.Categoria);
    }
}
