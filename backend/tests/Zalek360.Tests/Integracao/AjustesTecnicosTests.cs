using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zalek360.Api.Seguranca;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Tests.Integracao;

/// <summary>Fase 7.4: enum fora da lista, limite de tentativas de login e histórico imutável.</summary>
public sealed class AjustesTecnicosTests
{
    private const string SenhaDemo = "Zalek@2026";

    private static async Task<HttpResponseMessage> Login(HttpClient c, string email, string senha) =>
        await c.PostAsJsonAsync("/api/auth/login", new { email, senha, manterConectado = false });

    private static async Task<string?> Codigo(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    [Fact]
    public async Task Enum_fora_da_lista_responde_400_no_envelope_e_inteiro_valido_continua_aceito()
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync();
        using var c = f.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await Login(c, "gestao@zalekpersonalizados.com.br", SenhaDemo)).StatusCode);
        var url = $"/api/orcamentos/{Guid.NewGuid()}/decisao";
        var invalido = await c.PostAsJsonAsync(url, new { resultado = 99, meioContato = "WhatsApp", dataHora = DateTimeOffset.UtcNow, versao = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
        Assert.Equal("VALIDACAO", await Codigo(invalido));
        var textoInvalido = await c.PostAsJsonAsync(url, new { resultado = "Inexistente", meioContato = "WhatsApp", dataHora = DateTimeOffset.UtcNow, versao = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, textoInvalido.StatusCode);
        // Valor válido (inteiro ou texto) passa pelo binding; o orçamento inexistente gera 404.
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync(url, new { resultado = 1, meioContato = "WhatsApp", dataHora = DateTimeOffset.UtcNow, versao = 1 })).StatusCode);
    }

    [Fact]
    public async Task Login_bloqueia_a_conta_apos_cinco_falhas_sem_afetar_outra_conta()
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync();
        using var c = f.CreateClient();
        const string admin = "admin@zalekpersonalizados.com.br";
        for (var i = 0; i < LimitadorTentativasLogin.MaximoFalhas; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(c, admin, "senha-errada-" + i)).StatusCode);
        var bloqueado = await Login(c, " ADMIN@zalekpersonalizados.com.br ", SenhaDemo);
        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueado.StatusCode);
        Assert.Equal("MUITAS_TENTATIVAS", await Codigo(bloqueado));
        Assert.Equal(HttpStatusCode.OK, (await Login(c, "gestao@zalekpersonalizados.com.br", SenhaDemo)).StatusCode);
    }

    private sealed class RelogioManual : TimeProvider
    {
        public DateTimeOffset Agora { get; set; } = new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    [Fact]
    public void Limitador_libera_apos_a_janela_e_zera_no_sucesso()
    {
        var relogio = new RelogioManual();
        var limitador = new LimitadorTentativasLogin(relogio);
        for (var i = 0; i < LimitadorTentativasLogin.MaximoFalhas - 1; i++) limitador.RegistrarFalha("a@x.test");
        Assert.Null(limitador.Bloqueio("a@x.test"));
        limitador.RegistrarFalha("a@x.test");
        Assert.NotNull(limitador.Bloqueio("a@x.test"));
        relogio.Agora += LimitadorTentativasLogin.Janela;
        Assert.Null(limitador.Bloqueio("a@x.test"));
        for (var i = 0; i < LimitadorTentativasLogin.MaximoFalhas - 1; i++) limitador.RegistrarFalha("b@x.test");
        limitador.Limpar("b@x.test");
        limitador.RegistrarFalha("b@x.test");
        Assert.Null(limitador.Bloqueio("b@x.test"));
        Assert.Equal("a***@empresa.com.br", LimitadorTentativasLogin.Mascarar("ana.paula@empresa.com.br"));
        Assert.Equal("***", LimitadorTentativasLogin.Mascarar("sem-arroba"));
    }

    [Fact]
    public async Task Historico_nao_pode_ser_alterado_nem_excluido()
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync();
        using var escopo = f.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<ZalekDbContext>();
        var evento = await db.HistoricoEventos.FirstAsync();
        db.HistoricoEventos.Remove(evento);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.Entry(evento).State = EntityState.Modified;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}
