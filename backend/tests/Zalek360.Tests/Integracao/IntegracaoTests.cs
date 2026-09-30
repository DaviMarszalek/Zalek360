using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Zalek360.Application.Dtos;
using Zalek360.Application.Services;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Persistence;
using Zalek360.Infrastructure.Queries;
using Zalek360.Infrastructure.Repositories;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Integracao;

/// <summary>Banco relacional real (SQLite em memória) com o mesmo modelo EF Core da aplicação.</summary>
public sealed class BancoSqlite : IAsyncDisposable
{
    private readonly SqliteConnection _conexao = new("DataSource=:memory:");
    public RelogioFixo Relogio { get; } = new();
    public NumeradorFake Numerador { get; } = new();
    public Guid ClienteId { get; private set; }
    public Guid ProdutoId { get; private set; }

    private BancoSqlite() { }

    public static async Task<BancoSqlite> CriarAsync()
    {
        var banco = new BancoSqlite();
        await banco._conexao.OpenAsync();
        await using var db = banco.Contexto();
        await db.Database.EnsureCreatedAsync();
        db.Usuarios.Add(Usuario.Criar("Maria Silva", "maria.silva@zalekpersonalizados.com.br", "hash", PerfilUsuario.Atendente, Dados.Agora, Dados.UsuarioId));
        var produto = Produto.Criar("Camisa Polo", "Camiseta", "Camisa polo piquet");
        var cliente = Cliente.Criar(Dados.ClientePj(), Dados.Agora, Dados.UsuarioId);
        db.Produtos.Add(produto);
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        banco.ClienteId = cliente.Id;
        banco.ProdutoId = produto.Id;
        return banco;
    }

    public ZalekDbContext Contexto() => new(new DbContextOptionsBuilder<ZalekDbContext>().UseSqlite(_conexao).Options);

    public OrcamentoService Orcamentos(ZalekDbContext db) => new(
        new OrcamentoRepository(db), new PedidoRepository(db), new ClienteRepository(db), new UsuarioRepository(db),
        new ProdutoRepository(db), new AnexoRepository(db), new OrcamentoQueries(db, Relogio), Numerador, db, Relogio, new UsuarioAtualFake());

    public async Task<OrcamentoDetalheDto> OrcamentoAguardandoAsync(params Guid[] anexos)
    {
        await using var db = Contexto();
        var s = Orcamentos(db);
        var o = await s.CriarAsync(Cenario.Requisicao(ClienteId, ProdutoId, Cenario.ItemReq(ProdutoId, anexos: anexos)), CancellationToken.None);
        return await s.MarcarAguardandoRetornoAsync(o.Id, new AguardandoRetornoRequest(MeioContato.Email, o.Versao), CancellationToken.None);
    }

    public ValueTask DisposeAsync() => _conexao.DisposeAsync();
}

public class AprovacaoTransacionalTests
{
    [Fact]
    public async Task Aprovacao_persiste_orcamento_pedido_itens_anexos_e_historico()
    {
        await using var banco = await BancoSqlite.CriarAsync();
        Guid arteId;
        await using (var db = banco.Contexto())
        {
            var arte = ArquivoAnexo.CriarPendente("logo.pdf", 1024, "2026/09/logo.pdf", Dados.Agora, Dados.UsuarioId);
            db.ArquivosAnexos.Add(arte);
            await db.SaveChangesAsync();
            arteId = arte.Id;
        }
        var o = await banco.OrcamentoAguardandoAsync(arteId);

        await using (var db = banco.Contexto())
            await banco.Orcamentos(db).RegistrarDecisaoAsync(o.Id, new DecisaoRequest(ResultadoDecisao.Aprovado, MeioContato.WhatsApp,
                new DateTimeOffset(Dados.Agora), null, "Aprovado.", o.Versao), CancellationToken.None);

        await using (var db = banco.Contexto())
        {
            var orcamento = await db.Orcamentos.Include(x => x.Decisao).SingleAsync(x => x.Id == o.Id);
            var pedido = await db.Pedidos.Include(p => p.Itens).ThenInclude(i => i.Anexos).SingleAsync();
            Assert.Equal(SituacaoOrcamento.Aprovado, orcamento.Situacao);
            Assert.Equal(MeioContato.WhatsApp, orcamento.Decisao!.MeioContato);
            Assert.Equal(o.Id, pedido.OrcamentoOrigemId);
            Assert.Equal(orcamento.ValorTotal, pedido.ValorTotal);
            var copia = Assert.Single(Assert.Single(pedido.Itens).Anexos);
            Assert.Equal(arteId, copia.AnexoOrigemId);
            Assert.True(await db.HistoricoEventos.AnyAsync(h => h.Tipo == TipoEventoHistorico.PedidoGerado && h.PedidoId == pedido.Id));
            Assert.True(await db.HistoricoEventos.AnyAsync(h => h.Tipo == TipoEventoHistorico.PedidoCriado && h.PedidoId == pedido.Id));
        }
    }

    [Fact]
    public async Task Falha_na_geracao_do_pedido_desfaz_a_aprovacao()
    {
        await using var banco = await BancoSqlite.CriarAsync();
        var o = await banco.OrcamentoAguardandoAsync();
        banco.Numerador.FalharAoNumerarPedido = true;

        await using (var db = banco.Contexto())
            await Assert.ThrowsAsync<InvalidOperationException>(() => banco.Orcamentos(db).RegistrarDecisaoAsync(o.Id,
                new DecisaoRequest(ResultadoDecisao.Aprovado, MeioContato.WhatsApp, new DateTimeOffset(Dados.Agora), null, null, o.Versao),
                CancellationToken.None));

        await using (var db = banco.Contexto())
        {
            var orcamento = await db.Orcamentos.Include(x => x.Decisao).SingleAsync(x => x.Id == o.Id);
            Assert.Equal(SituacaoOrcamento.AguardandoRetorno, orcamento.Situacao);
            Assert.Null(orcamento.Decisao);
            Assert.False(await db.Pedidos.AnyAsync());
            Assert.False(await db.DecisoesOrcamento.AnyAsync());
            Assert.False(await db.HistoricoEventos.AnyAsync(h => h.Tipo == TipoEventoHistorico.DecisaoRegistrada));
        }
    }
}

public class HistoricoConsultasTests
{
    [Fact]
    public async Task Historico_do_cliente_filtra_por_tipo_e_pertence_ao_cliente()
    {
        await using var banco = await BancoSqlite.CriarAsync();
        var o = await banco.OrcamentoAguardandoAsync();
        await using (var db = banco.Contexto())
            await banco.Orcamentos(db).RegistrarContatoAsync(o.Id,
                new ContatoRequest(MeioContato.Ligacao, new DateTimeOffset(Dados.Agora), "Cliente confirmou as cores."), CancellationToken.None);

        await using var consulta = banco.Contexto();
        var queries = new ClienteQueries(consulta, banco.Relogio);
        var todos = await queries.ObterHistoricoAsync(banco.ClienteId, new FiltroHistoricoCliente(null, null, null, null, null, null), CancellationToken.None);
        var contatos = await queries.ObterHistoricoAsync(banco.ClienteId, new FiltroHistoricoCliente(null, null, "contatos", null, null, null), CancellationToken.None);
        var outroCliente = await consulta.HistoricoEventos.CountAsync(h => h.ClienteId != banco.ClienteId);

        Assert.True(todos.Count >= 4); // cadastro, criação, aguardando retorno e contato
        Assert.All(contatos, e => Assert.Equal(TipoEventoHistorico.ContatoRegistrado, e.Tipo));
        Assert.Single(contatos);
        Assert.Equal(0, outroCliente);

        var linhaDoTempo = await new OrcamentoQueries(consulta, banco.Relogio).ObterHistoricoAsync(o.Id, "contatos", CancellationToken.None);
        Assert.Single(linhaDoTempo);
        Assert.Equal(SituacaoOrcamento.AguardandoRetorno, (await consulta.Orcamentos.SingleAsync()).Situacao);
    }
}
