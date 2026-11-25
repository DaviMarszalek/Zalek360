using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Persistence;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Integracao;

public sealed class OrcamentoCustoPostgresTests
{
    [PostgresFact]
    public async Task Upgrade_preserva_item_antigo_sem_inventar_custo_historico()
    {
        var administracao = Environment.GetEnvironmentVariable("ZALEK_TEST_POSTGRES")!;
        var nome = "orcamento_test_" + Guid.NewGuid().ToString("N");
        await using var conexao = new NpgsqlConnection(administracao);
        await conexao.OpenAsync();
        await using (var criar = new NpgsqlCommand($"CREATE DATABASE {nome}", conexao) { CommandTimeout = 120 }) await criar.ExecuteNonQueryAsync();
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(administracao) { Database = nome }.ConnectionString;
            await using var db = new ZalekDbContext(new DbContextOptionsBuilder<ZalekDbContext>().UseNpgsql(cs).Options);
            var migrador = db.GetService<IMigrator>();
            await migrador.MigrateAsync("20261124220121_CatalogoProdutosEOpcoes");
            var usuario = Usuario.Criar("Gestor", "teste@example.com", "hash-teste", PerfilUsuario.Gestor, Dados.Agora);
            var cliente = Cliente.Criar(Dados.ClientePj(), Dados.Agora, usuario.Id);
            var produto = Produto.Criar("Nome atual", "Caneca", null);
            produto.Atualizar(produto.Nome, produto.Categoria, null, 60m, 33m);
            db.AddRange(usuario, cliente, produto);
            await db.SaveChangesAsync();
            var id = Guid.NewGuid(); var itemId = Guid.NewGuid();
            // O modelo anterior ainda não contém a coluna de custo interno.
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO orcamentos (id,numero,cliente_id,responsavel_id,data_criacao,validade,prazo_estimado,situacao,subtotal_produtos,valor_personalizacao,desconto,valor_total,versao,criado_em) VALUES ({id},1,{cliente.Id},{usuario.Id},{Dados.Hoje},{Dados.Hoje.AddDays(15)},15,'EmElaboracao',80,10,5,85,1,{Dados.Agora})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO itens_orcamento (id,orcamento_id,ordem,produto_id,produto_nome,quantidade,valor_unitario,valor_personalizacao_unit,subtotal_produto,total_personalizacao,valor_total,tipo_personalizacao,cor_peca) VALUES ({itemId},{id},1,{produto.Id},'Nome histórico',2,40,5,80,10,90,'Bordado','Azul')");
            await migrador.MigrateAsync();
            var orcamento = await db.Orcamentos.Include(x => x.Itens).SingleAsync();
            Assert.Equal(id, orcamento.Id);
            Assert.Equal(85m, orcamento.ValorTotal);
            var item = Assert.Single(orcamento.Itens);
            Assert.Equal(itemId, item.Id);
            Assert.Equal("Nome histórico", item.ProdutoNome);
            Assert.Equal(40m, item.ValorUnitario);
            Assert.Equal(2, item.Quantidade);
            Assert.Null(item.CustoUnitarioInterno);
            Assert.Equal(33m, (await db.Produtos.SingleAsync()).Custo);
        }
        finally
        {
            await using var remover = new NpgsqlCommand($"DROP DATABASE {nome} WITH (FORCE)", conexao) { CommandTimeout = 120 };
            await remover.ExecuteNonQueryAsync();
        }
    }
}
