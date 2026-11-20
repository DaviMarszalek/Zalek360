using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Zalek360.Application.Dtos;
using Zalek360.Application.Services;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Persistence;
using Zalek360.Infrastructure.Queries;
using Zalek360.Infrastructure.Repositories;
using Zalek360.Infrastructure.Services;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Integracao;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ZALEK_TEST_POSTGRES")))
            Skip = "Defina ZALEK_TEST_POSTGRES para um PostgreSQL descartável; cada teste cria e remove seu próprio banco.";
    }
}

public class EstoquePostgresTests
{
    private sealed class Banco : IAsyncDisposable
    {
        public string Conexao { get; }
        private readonly string _administracao;
        private readonly string _nome = "estoque_test_" + Guid.NewGuid().ToString("N");
        private Banco(string admin)
        {
            _administracao = admin;
            var cs = new NpgsqlConnectionStringBuilder(admin) { Database = _nome };
            Conexao = cs.ConnectionString;
        }
        public static async Task<Banco> CriarAsync(string? migration = null)
        {
            var banco = new Banco(Environment.GetEnvironmentVariable("ZALEK_TEST_POSTGRES")!);
            await using var conn = new NpgsqlConnection(banco._administracao);
            await conn.OpenAsync();
            await using var comando = new NpgsqlCommand($"""CREATE DATABASE "{banco._nome}" """, conn);
            await comando.ExecuteNonQueryAsync();
            await using var db = banco.Contexto();
            await db.GetService<IMigrator>().MigrateAsync(migration);
            return banco;
        }
        public ZalekDbContext Contexto(DbCommandInterceptor? interceptor = null)
        {
            var opcoes = new DbContextOptionsBuilder<ZalekDbContext>().UseNpgsql(Conexao);
            if (interceptor is not null) opcoes.AddInterceptors(interceptor);
            return new(opcoes.Options);
        }
        public async ValueTask DisposeAsync()
        {
            await using var conn = new NpgsqlConnection(_administracao);
            await conn.OpenAsync();
            // Nome gerado internamente, com prefixo exclusivo do teste; nunca usa o banco da conexão de administração.
            await using var comando = new NpgsqlCommand($"""DROP DATABASE IF EXISTS "{_nome}" WITH (FORCE)""", conn);
            await comando.ExecuteNonQueryAsync();
        }
    }

    private sealed class DisputaMaterial : DbCommandInterceptor
    {
        private int _chegadas;
        private readonly TaskCompletionSource _ambas = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Chegadas => _chegadas;
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken ct = default)
        {
            if (command.CommandText.Contains("FROM materias_primas") && command.CommandText.Contains("FOR UPDATE"))
            {
                if (Interlocked.Increment(ref _chegadas) == 2) _ambas.TrySetResult();
                await _ambas.Task.WaitAsync(TimeSpan.FromSeconds(20), ct);
            }
            return result;
        }
    }

    private static OrcamentoService Orcamentos(ZalekDbContext db)
    {
        var clock = new RelogioFixo();
        var pedidos = new PedidoRepository(db);
        var produtos = new ProdutoRepository(db);
        var estoque = new EstoqueService(new EstoqueRepository(db), db, clock, produtos, new CatalogoQueries(db),
            pedidos, new PedidoQueries(db), new UsuarioAtualFake());
        return new(new OrcamentoRepository(db), pedidos, new ClienteRepository(db), new UsuarioRepository(db), produtos,
            new AnexoRepository(db), new OrcamentoQueries(db, clock), new NumeradorPostgres(db), db, clock, new UsuarioAtualFake(), estoque);
    }

    [PostgresFact]
    public async Task Duas_aprovacoes_disputam_o_mesmo_saldo_sem_dupla_reserva()
    {
        await using var banco = await Banco.CriarAsync();
        Guid clienteId, produtoId, materialId;
        await using (var db = banco.Contexto())
        {
            var cliente = Cliente.Criar(Dados.ClientePj(), Dados.Agora, Dados.UsuarioId);
            var produto = Produto.Criar("Produto concorrente", "Teste", null);
            var material = MateriaPrima.Criar("Material concorrente", "un", 10, 0);
            db.Usuarios.Add(Usuario.Criar("Atendente", "teste@example.com", "hash", PerfilUsuario.Atendente, Dados.Agora, Dados.UsuarioId));
            db.Clientes.Add(cliente); db.Produtos.Add(produto); db.MateriasPrimas.Add(material);
            db.FichasTecnicasMateriasPrimas.Add(FichaTecnicaMaterial.Criar(produto.Id, material.Id, 2));
            await db.SaveChangesAsync();
            clienteId = cliente.Id; produtoId = produto.Id; materialId = material.Id;
        }
        var orcamentos = new List<OrcamentoDetalheDto>();
        for (var i = 0; i < 2; i++)
        {
            await using var db = banco.Contexto();
            var s = Orcamentos(db);
            var o = await s.CriarAsync(Cenario.Requisicao(clienteId, produtoId, Cenario.ItemReq(produtoId, 4)), CancellationToken.None);
            orcamentos.Add(await s.MarcarAguardandoRetornoAsync(o.Id, new(MeioContato.Email, o.Versao), CancellationToken.None));
        }
        var disputa = new DisputaMaterial();
        await using var db1 = banco.Contexto(disputa);
        await using var db2 = banco.Contexto(disputa);
        var resultados = await Task.WhenAll(
            Orcamentos(db1).RegistrarDecisaoAsync(orcamentos[0].Id, EstoqueTransacionalTests.Decisao(orcamentos[0].Versao), CancellationToken.None),
            Orcamentos(db2).RegistrarDecisaoAsync(orcamentos[1].Id, EstoqueTransacionalTests.Decisao(orcamentos[1].Versao), CancellationToken.None));
        Assert.Equal(2, disputa.Chegadas);
        Assert.All(resultados, r => Assert.Equal(SituacaoOrcamento.Aprovado, r.Orcamento.Situacao));
        await using var consulta = banco.Contexto();
        var m = await consulta.MateriasPrimas.SingleAsync(x => x.Id == materialId);
        var reservas = await consulta.ReservasMateriasPrimasPedido.ToListAsync();
        Assert.Equal(10, m.QuantidadeDisponivel);
        Assert.Equal(10, m.SaldoReservado);
        Assert.Equal(0, m.SaldoDisponivel);
        Assert.Equal(new decimal[] { 2, 8 }, reservas.Select(x => x.QuantidadeReservada).Order().ToArray());
        Assert.Equal(6, reservas.Sum(x => x.QuantidadePendente));
        Assert.All(await consulta.Pedidos.ToListAsync(), p => Assert.True(p.ControleMateriais));
    }

    [PostgresFact]
    public async Task Backfill_consolida_duplicadas_prioriza_prazo_e_preserva_pedidos_legados()
    {
        await using var banco = await Banco.CriarAsync("20261112120000_AlinharEsquemaComModelo");
        var usuario = Guid.NewGuid(); var cliente = Guid.NewGuid(); var material = Guid.NewGuid();
        var pedidos = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        var orcamentos = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        await using (var db = banco.Contexto())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO usuarios(id,nome,email,senha_hash,perfil,ativo,criado_em)
                VALUES({usuario},'Usuario teste','backfill@example.com','hash','Atendente',true,'2026-09-24Z');
                INSERT INTO clientes(id,tipo_pessoa,cpf_cnpj,nome_razao_social,busca_normalizada,versao,data_cadastro)
                VALUES({cliente},'PessoaJuridica','12345678000195','Cliente teste','cliente teste',0,'2026-09-24Z');
                INSERT INTO materias_primas(id,nome,unidade,quantidade_disponivel,estoque_minimo,ativo,versao)
                VALUES({material},'Material backfill','un',10,0,true,3);
                """);
            for (var i = 0; i < 3; i++)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO orcamentos(id,numero,cliente_id,responsavel_id,data_criacao,validade,prazo_estimado,condicao_pagamento,situacao,subtotal_produtos,valor_personalizacao,desconto,valor_total,versao,criado_em)
                    VALUES({orcamentos[i]},{i + 1},{cliente},{usuario},'2026-09-24','2026-10-01',5,'PIX','Aprovado',10,0,0,10,1,'2026-09-24Z');
                    INSERT INTO pedidos(id,numero,orcamento_origem_id,cliente_id,responsavel_id,situacao,data_pedido,prazo_estimado,prazo_entrega,condicao_pagamento,subtotal_produtos,valor_personalizacao,desconto,valor_total,versao,data_criacao)
                    VALUES({pedidos[i]},{i + 1},{orcamentos[i]},{cliente},{usuario},'Aberto','2026-09-25',5,{new DateOnly(2026, 10, 1 + i)},'PIX',10,0,0,10,1,'2026-09-25Z');
                    """);
            }
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO reservas_materias_primas_pedido(id,pedido_id,materia_prima_id,quantidade,situacao,criada_em)
                VALUES({Guid.NewGuid()},{pedidos[0]},{material},4,'Reservada','2026-09-25T10:00:00Z'),
                      ({Guid.NewGuid()},{pedidos[0]},{material},3,'Reservada','2026-09-25T11:00:00Z'),
                      ({Guid.NewGuid()},{pedidos[1]},{material},8,'Reservada','2026-09-25T12:00:00Z');
                """);
            await db.Database.MigrateAsync();
        }
        await using var consulta = banco.Contexto();
        var ativas = await consulta.ReservasMateriasPrimasPedido.Where(x => x.Situacao == SituacaoReservaMaterial.Reservada).ToListAsync();
        Assert.Equal(2, ativas.Count);
        Assert.Equal(7, ativas.Single(x => x.PedidoId == pedidos[0]).Quantidade);
        Assert.Equal(7, ativas.Single(x => x.PedidoId == pedidos[0]).QuantidadeReservada);
        Assert.Equal(8, ativas.Single(x => x.PedidoId == pedidos[1]).Quantidade);
        Assert.Equal(3, ativas.Single(x => x.PedidoId == pedidos[1]).QuantidadeReservada);
        Assert.Equal(5, ativas.Single(x => x.PedidoId == pedidos[1]).QuantidadePendente);
        Assert.Equal(1, await consulta.ReservasMateriasPrimasPedido.CountAsync(x => x.Situacao == SituacaoReservaMaterial.Liberada));
        Assert.Equal(10, (await consulta.MateriasPrimas.SingleAsync()).SaldoReservado);
        Assert.True((await consulta.Pedidos.SingleAsync(x => x.Id == pedidos[0])).ControleMateriais);
        Assert.False((await consulta.Pedidos.SingleAsync(x => x.Id == pedidos[2])).ControleMateriais);
        var movimentos = await consulta.MovimentosMateriasPrimas.ToListAsync();
        Assert.Equal(2, movimentos.Count);
        Assert.All(movimentos, x => {
            Assert.Equal(TipoMovimentoMateriaPrima.LiberacaoReserva, x.Tipo);
            Assert.Null(x.UsuarioId);
            Assert.Equal("Reconciliação MVP 2 — Fase 2.0", x.Observacao);
        });
        // A constraint parcial também precisa rejeitar a duplicação diretamente no banco.
        await using var duplicacao = banco.Contexto();
        duplicacao.ReservasMateriasPrimasPedido.Add(ReservaMateriaPrimaPedido.Criar(pedidos[0], material, 1, Dados.Agora, 0));
        await Assert.ThrowsAsync<DbUpdateException>(() => duplicacao.SaveChangesAsync());
    }
}
