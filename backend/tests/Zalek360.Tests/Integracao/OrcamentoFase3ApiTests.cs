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
using Zalek360.Infrastructure.Documentos;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Tests.Integracao;

public sealed class OrcamentoFase3ApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private sealed class Numerador : INumeradorDocumentos
    {
        private int _orcamento = 123, _pedido = 56;
        public Task<int> ProximoNumeroOrcamentoAsync(CancellationToken ct) => Task.FromResult(Interlocked.Increment(ref _orcamento));
        public Task<int> ProximoNumeroPedidoAsync(CancellationToken ct) => Task.FromResult(Interlocked.Increment(ref _pedido));
    }
    private sealed class PdfInspecionado : IGeradorDocumentoOrcamento
    {
        public string Documento { get; private set; } = "";
        public Task<byte[]> GerarAsync(OrcamentoDocumentoDto documento, CancellationToken ct)
        { Documento = JsonSerializer.Serialize(documento, Json); return new GeradorDocumentoOrcamento().GerarAsync(documento, ct); }
    }
    private static async Task<HttpClient> Login(ApiPdfFactory f, string email)
    {
        var c = f.CreateClient();
        var r = await c.PostAsJsonAsync("/api/auth/login", new { email, senha = "Zalek@2026", manterConectado = false });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return c;
    }
    private static async Task<T> Ler<T>(HttpResponseMessage r)
    {
        Assert.True(r.IsSuccessStatusCode, $"HTTP {(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}");
        return (await r.Content.ReadFromJsonAsync<T>(Json))!;
    }
    private static ItemOrcamentoRequest Item(Guid produto, int qtd = 2, decimal valor = 40, Guid? id = null) =>
        new(id, produto, "Referência de teste", qtd, valor, 5, "DTF", "Preta", null, null, null, null, null, []);

    [Theory]
    [InlineData("gestao@zalekpersonalizados.com.br")]
    [InlineData("admin@zalekpersonalizados.com.br")]
    public async Task Analise_estima_margem_sem_reservar_e_congela_custo_sem_vazar_no_pdf(string email)
    {
        var pdf = new PdfInspecionado();
        await using var f = new ApiPdfFactory(pdf, new Numerador()); await f.PrepararSeedAsync();
        using var c = await Login(f, email);
        var m = await Ler<MateriaPrimaDto>(await c.PostAsJsonAsync("/api/estoque/materias-primas", new MateriaPrimaRequest("Material para orçamento", "un", 3, 0)));
        var pr = new ProdutoRequest("Produto para orçamento", "Camiseta", null, 40, 12, [new(m.Id, 2)]);
        var p = await Ler<ProdutoDto>(await c.PostAsJsonAsync("/api/produtos", pr));
        var previa = new AnaliseOrcamentoRequest(null, [new(null, p.Id, 2, 40, 5)], 10);
        var a = await Ler<AnaliseOrcamentoDto>(await c.PostAsJsonAsync("/api/orcamentos/analise-interna", previa));
        Assert.True(a.Completa); Assert.Equal(80, a.Receita); Assert.Equal(24, a.CustoTotal); Assert.Equal(56, a.MargemValor); Assert.Equal(70, a.MargemPercentual);
        var material = Assert.Single(Assert.Single(a.Itens).Materiais);
        Assert.Equal(4, material.Necessario); Assert.Equal(3, material.Disponivel); Assert.Equal(1, material.Falta);
        var saldo = Assert.Single(await Ler<List<MateriaPrimaDto>>(await c.GetAsync("/api/estoque/materias-primas")), x => x.Id == m.Id);
        Assert.Equal(0, saldo.QuantidadeReservada); Assert.Equal(3, saldo.QuantidadeDisponivel);
        Guid cliente;
        using (var scope = f.Services.CreateScope()) cliente = await scope.ServiceProvider.GetRequiredService<ZalekDbContext>().Clientes.Select(x => x.Id).FirstAsync();
        var r = new OrcamentoRequest(cliente, null, f.Relogio.HojeLocal.AddDays(10), 5, null, null, 10, [Item(p.Id)], null);
        var o = await Ler<OrcamentoDetalheDto>(await c.PostAsJsonAsync("/api/orcamentos", r));
        var publico = await (await c.GetAsync($"/api/orcamentos/{o.Id}")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("custo", publico, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("margem", publico, StringComparison.OrdinalIgnoreCase);
        var exportacao = await c.PostAsync($"/api/orcamentos/{o.Id}/exportacoes", null);
        Assert.Equal(HttpStatusCode.OK, exportacao.StatusCode);
        Assert.Equal("application/pdf", exportacao.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("custo", pdf.Documento, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("margem", pdf.Documento, StringComparison.OrdinalIgnoreCase);

        var alterado = await Ler<ProdutoDto>(await c.PutAsJsonAsync($"/api/produtos/{p.Id}", pr with { Nome = "Nome novo no catálogo", PrecoVenda = 60, Custo = 500, Versao = p.Versao }));
        await Ler<ProdutoDto>(await c.PatchAsJsonAsync($"/api/produtos/{p.Id}/status", new SituacaoProdutoRequest(false, alterado.Versao)));
        var gravado = await Ler<AnaliseOrcamentoDto>(await c.GetAsync($"/api/orcamentos/{o.Id}/analise-interna"));
        Assert.Equal(24, gravado.CustoTotal);
        var editado = await Ler<OrcamentoDetalheDto>(await c.PutAsJsonAsync($"/api/orcamentos/{o.Id}", r with { Itens = [Item(p.Id, 3, 40, o.Itens[0].Id)], Versao = o.Versao }));
        Assert.Equal(p.Nome, editado.Itens[0].ProdutoNome); Assert.Equal(40, editado.Itens[0].ValorUnitario);
        var congelado = await Ler<AnaliseOrcamentoDto>(await c.GetAsync($"/api/orcamentos/{o.Id}/analise-interna"));
        Assert.Equal(36, congelado.CustoTotal); Assert.Equal(89, congelado.MargemValor);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/orcamentos", r)).StatusCode);
    }

    [Fact]
    public async Task Materiais_compartilhados_nao_reutilizam_saldo_e_custo_ausente_nao_vira_zero()
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync(); using var c = await Login(f, "gestao@zalekpersonalizados.com.br");
        var m = await Ler<MateriaPrimaDto>(await c.PostAsJsonAsync("/api/estoque/materias-primas", new MateriaPrimaRequest("Material compartilhado", "un", 5, 0)));
        var p = await Ler<ProdutoDto>(await c.PostAsJsonAsync("/api/produtos", new ProdutoRequest("Produto com material", "Caneca", null, 40, 0, [new(m.Id, 2)])));
        var req = new AnaliseOrcamentoRequest(null, [new(null, p.Id, 2, 40, 0), new(null, p.Id, 2, 40, 0)], 0);
        var a = await Ler<AnaliseOrcamentoDto>(await c.PostAsJsonAsync("/api/orcamentos/analise-interna", req));
        Assert.Equal(0, a.CustoTotal); Assert.Equal(100, a.MargemPercentual);
        Assert.Equal(0, a.Itens[0].Materiais[0].Falta); Assert.Equal(1, a.Itens[1].Materiais[0].Disponivel); Assert.Equal(3, a.Itens[1].Materiais[0].Falta);
        var semCusto = await Ler<ProdutoDto>(await c.PostAsJsonAsync("/api/produtos", new ProdutoRequest("Produto sem custo", "Caneca", null, 40)));
        a = await Ler<AnaliseOrcamentoDto>(await c.PostAsJsonAsync("/api/orcamentos/analise-interna", req with { Itens = [new(null, semCusto.Id, 1, 40, 0)] }));
        Assert.Null(a.CustoTotal); Assert.Null(a.MargemValor); Assert.Null(a.MargemPercentual); Assert.True(a.Itens[0].SemFicha);
        a = await Ler<AnaliseOrcamentoDto>(await c.PostAsJsonAsync("/api/orcamentos/analise-interna", req with { Desconto = 160 }));
        Assert.Equal(0, a.Receita); Assert.Null(a.MargemPercentual);
    }

    [Fact]
    public async Task Legado_sem_custo_continua_desconhecido_e_previa_rejeita_item_alheio_ou_valor_fora_do_limite()
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync(); using var c = await Login(f, "gestao@zalekpersonalizados.com.br");
        Guid id, produto;
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZalekDbContext>();
            var item = await db.ItensOrcamento.FirstAsync(); id = item.OrcamentoId; produto = item.ProdutoId;
            var p = await db.Produtos.SingleAsync(x => x.Id == produto); p.Atualizar(p.Nome, p.Categoria, p.DescricaoBase, 99, 33); await db.SaveChangesAsync();
        }
        var a = await Ler<AnaliseOrcamentoDto>(await c.GetAsync($"/api/orcamentos/{id}/analise-interna"));
        Assert.Null(a.CustoTotal); Assert.All(a.Itens, i => Assert.Null(i.CustoUnitario));
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/orcamentos/analise-interna", new AnaliseOrcamentoRequest(id, [new(Guid.NewGuid(), produto, 1, 20, 0)], 0))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/orcamentos/analise-interna", new AnaliseOrcamentoRequest(null, [new(null, produto, int.MaxValue, 20, 0)], 0))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/orcamentos/{Guid.NewGuid()}/analise-interna")).StatusCode);
    }

    [Theory]
    [InlineData(PerfilUsuario.Atendente)]
    [InlineData(PerfilUsuario.Operador)]
    public async Task Analise_interna_bloqueia_anonimo_e_perfis_legados(PerfilUsuario perfil)
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync(); using var c = f.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/orcamentos/analise-interna", new { })).StatusCode);
        using var scope = f.Services.CreateScope(); var u = Usuario.Criar("Legado", "legado@example.com", "hash", perfil, f.Relogio.AgoraUtc);
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", scope.ServiceProvider.GetRequiredService<ITokenService>().Gerar(u, false).Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/orcamentos/analise-interna", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync($"/api/orcamentos/{Guid.NewGuid()}/analise-interna")).StatusCode);
    }
}
