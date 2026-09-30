using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zalek360.Application.Interfaces;
using Zalek360.Infrastructure.Options;
using Zalek360.Infrastructure.Persistence.Seed;

namespace Zalek360.Infrastructure.Persistence;

/// <summary>Aplica migrations, confere o esquema, executa o seed (idempotente) e ajusta as sequências.</summary>
public static class InicializadorBanco
{
    public static async Task InicializarAsync(IServiceProvider servicos, CancellationToken ct = default)
    {
        await using var escopo = servicos.CreateAsyncScope();
        var sp = escopo.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Zalek360.Banco");
        var opcoes = sp.GetRequiredService<IOptions<BancoOptions>>().Value;
        var db = sp.GetRequiredService<ZalekDbContext>();

        await AguardarConexaoAsync(db, logger, ct);

        if (opcoes.AplicarMigrationsNaInicializacao)
        {
            var pendentes = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
            if (pendentes.Count > 0)
                logger.LogInformation("Aplicando migrations: {Migrations}", string.Join(", ", pendentes));
            await db.Database.MigrateAsync(ct);
        }

        if (opcoes.VerificarEsquema) await VerificarEsquemaAsync(db, logger, ct);

        if (opcoes.ExecutarSeed)
        {
            var seeder = new DbSeeder(db, sp.GetRequiredService<IClock>(), sp.GetRequiredService<IPasswordHasher>(), logger);
            await seeder.ExecutarAsync(opcoes.SenhaUsuariosDemo, ct);
        }

        await AjustarSequenciasAsync(db, ct);
    }

    private static async Task AguardarConexaoAsync(ZalekDbContext db, ILogger logger, CancellationToken ct)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                if (await db.Database.CanConnectAsync(ct)) return;
            }
            catch (Exception ex) when (tentativa < 30)
            {
                logger.LogDebug(ex, "Banco ainda indisponível.");
            }
            if (tentativa >= 30)
            {
                // Pode ser apenas o banco ainda não criado: MigrateAsync cria o banco se tiver permissão.
                logger.LogWarning("Não foi possível confirmar a conexão com o banco após {Tentativas} tentativas.", tentativa);
                return;
            }
            logger.LogInformation("Aguardando o PostgreSQL ficar disponível (tentativa {Tentativa}/30)...", tentativa);
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }

    /// <summary>Após o seed/importações, garante que a próxima numeração continue do maior número existente.</summary>
    private static async Task AjustarSequenciasAsync(ZalekDbContext db, CancellationToken ct)
    {
        if (!db.Database.IsNpgsql()) return;
        await db.Database.ExecuteSqlRawAsync(
            "SELECT setval('" + Sequencias.Orcamento + "', GREATEST((SELECT COALESCE(MAX(numero), 0) FROM orcamentos), 1), (SELECT COUNT(*) > 0 FROM orcamentos));", ct);
        await db.Database.ExecuteSqlRawAsync(
            "SELECT setval('" + Sequencias.Pedido + "', GREATEST((SELECT COALESCE(MAX(numero), 0) FROM pedidos), 1), (SELECT COUNT(*) > 0 FROM pedidos));", ct);
    }

    private sealed class ColunaExistente
    {
        public string Tabela { get; set; } = string.Empty;
        public string Coluna { get; set; } = string.Empty;
    }

    /// <summary>
    /// Proteção para a migration escrita manualmente: compara as colunas esperadas pelo modelo EF com o information_schema.
    /// </summary>
    private static async Task VerificarEsquemaAsync(ZalekDbContext db, ILogger logger, CancellationToken ct)
    {
        if (!db.Database.IsNpgsql()) return;
        var existentes = await db.Database
            .SqlQueryRaw<ColunaExistente>("SELECT table_name AS \"Tabela\", column_name AS \"Coluna\" FROM information_schema.columns WHERE table_schema = current_schema()")
            .ToListAsync(ct);
        var conjunto = existentes.Select(c => $"{c.Tabela}.{c.Coluna}").ToHashSet(StringComparer.Ordinal);
        var faltando = new List<string>();
        foreach (var entidade in db.Model.GetEntityTypes())
        {
            var tabela = entidade.GetTableName();
            if (tabela is null) continue;
            var objeto = StoreObjectIdentifier.Table(tabela, entidade.GetSchema());
            foreach (var propriedade in entidade.GetProperties())
            {
                var coluna = propriedade.GetColumnName(objeto);
                if (coluna is not null && !conjunto.Contains($"{tabela}.{coluna}")) faltando.Add($"{tabela}.{coluna}");
            }
        }
        if (faltando.Count > 0)
            throw new InvalidOperationException(
                "O esquema do banco não corresponde ao modelo da aplicação. Colunas ausentes: " + string.Join(", ", faltando));
        logger.LogInformation("Esquema do banco verificado: {Total} colunas conferidas.", conjunto.Count);
    }
}
