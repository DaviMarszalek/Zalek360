using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Application.Services;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Documentos;
using Zalek360.Infrastructure.Options;
using Zalek360.Infrastructure.Persistence;
using Zalek360.Infrastructure.Persistence.Seed;
using Zalek360.Tests.Aplicacao;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Integracao;

public class DocumentoPdfTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pdf_real_com_anexo_ausente_ou_ilegivel_nao_falha(bool ilegivel)
    {
        var temporario = Path.GetTempFileName();
        try
        {
            if (ilegivel) await File.WriteAllTextAsync(temporario, "não é uma imagem");
            else File.Delete(temporario);
            var o = OrcamentoDocumentoTests.Orcamento();
            o = o with { Itens = o.Itens.Select(i => i with { Anexos = new[] {
                new AnexoDto(Guid.NewGuid(), "arte-ausente.png", ".png", "image/png", 100, true),
                new AnexoDto(Guid.NewGuid(), "arte-ilegivel.jpg", ".jpg", "image/jpeg", 100, true)
            } }).ToArray() };
            var relogio = new RelogioFixo();
            var dto = OrcamentoDocumentoService.Montar(o, OrcamentoDocumentoTests.Cliente(),
                new EmpresaOptions { CaminhoLogo = temporario }, relogio, relogio.AgoraUtc);
            var bytes = await new GeradorDocumentoOrcamento().GerarAsync(dto, default);
            Assert.True(bytes.Length > 1000);
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
            Assert.Equal(2, dto.Itens[0].Artes.Count);
        }
        finally { if (File.Exists(temporario)) File.Delete(temporario); }
    }
}

/// <summary>Pipeline HTTP e autenticação reais, banco relacional isolado e os dados reais do seed.</summary>
public sealed class ApiPdfFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conexao = new("DataSource=:memory:");
    private readonly IGeradorDocumentoOrcamento? _gerador;
    public RelogioFixo Relogio { get; } = new() { Agora = DateTime.UtcNow };

    public ApiPdfFactory(IGeradorDocumentoOrcamento? gerador = null)
    {
        _gerador = gerador;
        _conexao.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Jwt:Chave", "chave-isolada-teste-pdf-zalek360-mais-32-caracteres");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ZalekDbContext>>();
            services.RemoveAll<ZalekDbContext>();
            services.AddDbContext<ZalekDbContext>(o => o.UseSqlite(_conexao));
            services.PostConfigure<BancoOptions>(o =>
            {
                o.AplicarMigrationsNaInicializacao = false; o.VerificarEsquema = false; o.ExecutarSeed = false;
            });
            services.PostConfigure<RotinasOptions>(o => { o.ExpiracaoHabilitada = false; o.LimpezaAnexosHabilitada = false; });
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Relogio);
            if (_gerador is not null)
            {
                services.RemoveAll<IGeradorDocumentoOrcamento>();
                services.AddSingleton(_gerador);
            }
        });
    }

    public async Task PrepararSeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZalekDbContext>();
        await db.Database.EnsureCreatedAsync();
        await new DbSeeder(db, scope.ServiceProvider.GetRequiredService<IClock>(),
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>(), NullLogger.Instance).ExecutarAsync("Zalek@2026", default);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _conexao.Dispose();
    }
}

public class EndpointPdfTests
{
    [Fact]
    public async Task Falha_na_geracao_nao_registra_exportacao_e_perfil_operador_nao_tem_acesso()
    {
        await using var factory = new ApiPdfFactory(new GeradorComFalha());
        await factory.PrepararSeedAsync();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZalekDbContext>();
        var o = await db.Orcamentos.SingleAsync(o => o.Numero == 123);
        var antes = await db.HistoricoEventos.CountAsync();
        var usuario = await db.Usuarios.FirstAsync();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var operador = Zalek360.Domain.Entities.Usuario.Criar("Operador", "operador@example.com", "hash",
            PerfilUsuario.Operador, factory.Relogio.AgoraUtc);
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Gerar(operador, false).Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/orcamentos/{o.Id}/exportacoes", null)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Gerar(usuario, false).Token);
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.PostAsync($"/api/orcamentos/{o.Id}/exportacoes", null)).StatusCode);
        Assert.Equal(antes, await db.HistoricoEventos.CountAsync());
    }

    private sealed class GeradorComFalha : IGeradorDocumentoOrcamento
    {
        public Task<byte[]> GerarAsync(OrcamentoDocumentoDto documento, CancellationToken ct) =>
            throw new IOException("Falha simulada no renderizador.");
    }

    [Fact]
    public async Task Endpoint_autentica_exporta_seed_e_grava_historico_somente_no_post()
    {
        await using var factory = new ApiPdfFactory();
        await factory.PrepararSeedAsync();
        using var client = factory.CreateClient();
        var inexistente = Guid.NewGuid();
        var semLogin = await client.GetAsync($"/api/orcamentos/{inexistente}/exportar-pdf");
        Assert.Equal(HttpStatusCode.Unauthorized, semLogin.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync($"/api/orcamentos/{inexistente}/exportacoes", null)).StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "maria.silva@zalekpersonalizados.com.br", senha = "Zalek@2026", manterConectado = false });
        login.EnsureSuccessStatusCode();
        var json = await login.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
        foreach (var post in new[] { false, true })
        {
            var r = post ? await client.PostAsync($"/api/orcamentos/{inexistente}/exportacoes", null)
                : await client.GetAsync($"/api/orcamentos/{inexistente}/exportar-pdf?inline=true");
            Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
            var erro = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal(404, erro.GetProperty("status").GetInt32());
            Assert.True(erro.TryGetProperty("code", out _));
        }
        Guid id, clienteId;
        int antes, versao;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZalekDbContext>();
            var o = await db.Orcamentos.SingleAsync(o => o.Numero == 123);
            id = o.Id; clienteId = o.ClienteId; versao = o.Versao;
            antes = await db.HistoricoEventos.CountAsync();
        }
        foreach (var inline in new[] { false, true })
        {
            var resposta = await client.GetAsync($"/api/orcamentos/{id}/exportar-pdf?inline={inline}");
            await ConferirPdf(resposta, inline);
        }
        using (var scope = factory.Services.CreateScope())
            Assert.Equal(antes, await scope.ServiceProvider.GetRequiredService<ZalekDbContext>().HistoricoEventos.CountAsync());
        var exportado = await client.PostAsync($"/api/orcamentos/{id}/exportacoes?inline=true", null);
        await ConferirPdf(exportado, true);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZalekDbContext>();
            Assert.Equal(antes + 1, await db.HistoricoEventos.CountAsync());
            var evento = await db.HistoricoEventos.SingleAsync(h => h.Tipo == TipoEventoHistorico.OrcamentoExportadoPdf);
            Assert.Equal(clienteId, evento.ClienteId);
            Assert.Equal(id, evento.OrcamentoId);
            Assert.NotNull(evento.UsuarioId);
            Assert.Equal(factory.Relogio.AgoraUtc, evento.OcorridoEm);
            Assert.Equal(versao, (await db.Orcamentos.SingleAsync(o => o.Id == id)).Versao);
            var queries = scope.ServiceProvider.GetRequiredService<IClienteQueries>();
            var historico = await queries.ObterHistoricoAsync(clienteId, new(null, null, null, null, null, null), default);
            Assert.Contains(historico, e => e.Tipo == TipoEventoHistorico.OrcamentoExportadoPdf);
        }
        await client.GetAsync($"/api/orcamentos/{id}/exportar-pdf?inline=true");
        using (var scope = factory.Services.CreateScope())
            Assert.Equal(antes + 1, await scope.ServiceProvider.GetRequiredService<ZalekDbContext>().HistoricoEventos.CountAsync());
    }

    private static async Task ConferirPdf(HttpResponseMessage resposta, bool inline)
    {
        resposta.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", resposta.Content.Headers.ContentType?.MediaType);
        Assert.Equal(inline ? "inline" : "attachment", resposta.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Contains("Orcamento-000123-", resposta.Content.Headers.ContentDisposition?.FileName);
        Assert.True(resposta.Headers.CacheControl?.NoStore);
        var bytes = await resposta.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
