using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Zalek360.Domain.Entities;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Tests.Integracao;

public sealed class CatalogoPostgresTests
{
    [PostgresFact]
    public async Task Upgrade_preserva_produto_legado_e_disponibiliza_categoria_sem_inventar_precos()
    {
        var administracao = Environment.GetEnvironmentVariable("ZALEK_TEST_POSTGRES")!;
        var nome = "catalogo_test_" + Guid.NewGuid().ToString("N");
        await using var conexao = new NpgsqlConnection(administracao); await conexao.OpenAsync();
        await using (var criar = new NpgsqlCommand($"CREATE DATABASE {nome}", conexao) { CommandTimeout = 120 }) await criar.ExecuteNonQueryAsync();
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(administracao) { Database = nome }.ConnectionString;
            await using var db = new ZalekDbContext(new DbContextOptionsBuilder<ZalekDbContext>().UseNpgsql(cs).Options);
            var migrador = db.GetService<IMigrator>();
            await migrador.MigrateAsync("20261124033502_CondicaoPagamentoOpcional");
            var id = Guid.NewGuid(); var categoria = "Categoria histórica"; var produto = "Produto histórico";
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO produtos (id,nome,categoria,descricao_base,ativo) VALUES ({id},{produto},{categoria},NULL,true)");
            await migrador.MigrateAsync();
            var preservado = await db.Produtos.SingleAsync();
            Assert.Equal(id, preservado.Id); Assert.Equal(produto, preservado.Nome); Assert.Equal(categoria, preservado.Categoria);
            Assert.True(preservado.Ativo); Assert.Null(preservado.PrecoVenda); Assert.Null(preservado.Custo); Assert.Equal(1, preservado.Versao);
            Assert.True(await db.OpcoesCatalogo.AnyAsync(x => x.Tipo == TipoOpcaoCatalogo.CategoriaProduto && x.Nome == categoria && x.Ativo));
            Assert.Equal(8, await db.OpcoesCatalogo.CountAsync(x => x.Tipo == TipoOpcaoCatalogo.TipoPersonalizacao));
            Assert.Equal(6, await db.OpcoesCatalogo.CountAsync(x => x.Tipo == TipoOpcaoCatalogo.CondicaoPagamento));
            Assert.Equal(6, await db.OpcoesCatalogo.CountAsync(x => x.Tipo == TipoOpcaoCatalogo.MotivoDecisao));
        }
        finally
        {
            // Banco temporário com nome interno; não usa o banco da conexão de administração.
            await using var remover = new NpgsqlCommand($"DROP DATABASE {nome} WITH (FORCE)", conexao) { CommandTimeout = 120 };
            await remover.ExecuteNonQueryAsync();
        }
    }
}
