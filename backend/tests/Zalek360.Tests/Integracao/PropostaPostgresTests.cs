using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Tests.Integracao;

public sealed class PropostaPostgresTests
{
    [PostgresFact]
    public async Task Upgrade_aditivo_semeia_modelos_e_envios_concorrentes_nao_disparam_duas_mensagens()
    {
        var nome = "proposta_test_" + Guid.NewGuid().ToString("N");
        var admin = Environment.GetEnvironmentVariable("ZALEK_TEST_POSTGRES")!;
        await using var conexao = new NpgsqlConnection(admin); await conexao.OpenAsync();
        await using (var cmd = new NpgsqlCommand($"CREATE DATABASE {nome}", conexao)) await cmd.ExecuteNonQueryAsync();
        var liberar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var entrou = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var sender = new EmailPropostaTeste { Aguardar = () => { entrou.TrySetResult(); return liberar.Task.WaitAsync(TimeSpan.FromSeconds(20)); } };
            var cs = new NpgsqlConnectionStringBuilder(admin) { Database = nome }.ConnectionString;
            await using (var anterior = new ZalekDbContext(new DbContextOptionsBuilder<ZalekDbContext>().UseNpgsql(cs).Options))
                await anterior.GetService<IMigrator>().MigrateAsync("20261124231443_CustoInternoNoItemOrcamento");
            await using var f = new ApiPdfFactory(email: sender, postgres: cs);
            await f.PrepararSeedAsync();
            using var c = await PropostaApiTests.Login(f);
            var o = await PropostaApiTests.Criar(f, c);
            using (var scope = f.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ZalekDbContext>();
                await db.Database.MigrateAsync();
                Assert.Equal(2, await db.ModelosMensagemProposta.CountAsync());
                var item = await db.ItensOrcamento.FirstAsync(x => x.OrcamentoId == o.Id);
                Assert.Equal(12m, item.CustoUnitarioInterno); Assert.Equal(40m, item.ValorUnitario);
            }
            var url = $"/api/orcamentos/{o.Id}/proposta/email";
            var pedido = PropostaApiTests.Pedido(o);
            var primeiro = c.PostAsJsonAsync(url, pedido);
            await entrou.Task.WaitAsync(TimeSpan.FromSeconds(15));
            var segundo = c.PostAsJsonAsync(url, PropostaApiTests.Pedido(o));
            var bloqueou = false;
            for (var i = 0; i < 100 && !bloqueou; i++)
            {
                await using var cmd = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE datname=@nome AND wait_event_type='Lock')", conexao);
                cmd.Parameters.AddWithValue("nome", nome); bloqueou = (bool)(await cmd.ExecuteScalarAsync())!;
                if (!bloqueou) await Task.Delay(50);
            }
            liberar.TrySetResult();
            Assert.True(bloqueou, "A segunda tentativa deve aguardar o bloqueio do orçamento antes de chamar SMTP.");
            Assert.Equal(HttpStatusCode.OK, (await primeiro).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await segundo).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync(url, pedido)).StatusCode);
            Assert.Single(sender.Envios);
        }
        finally
        {
            liberar.TrySetResult();
            await using var remover = new NpgsqlCommand($"DROP DATABASE {nome} WITH (FORCE)", conexao); await remover.ExecuteNonQueryAsync();
        }
    }
}
