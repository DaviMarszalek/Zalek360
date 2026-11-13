using Microsoft.EntityFrameworkCore;
using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Services;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Persistence;
using Zalek360.Infrastructure.Queries;
using Zalek360.Infrastructure.Repositories;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Integracao;

public class EstoqueTransacionalTests
{
    internal static async Task<Guid> MaterialAsync(BancoSqlite banco, decimal saldo, bool comFicha = true)
    {
        await using var db = banco.Contexto();
        var material = MateriaPrima.Criar("Tecido de teste", "m", saldo, 0);
        db.MateriasPrimas.Add(material);
        if (comFicha) db.FichasTecnicasMateriasPrimas.Add(FichaTecnicaMaterial.Criar(banco.ProdutoId, material.Id, 2));
        await db.SaveChangesAsync();
        return material.Id;
    }

    internal static async Task<OrcamentoDetalheDto> AguardarAsync(BancoSqlite banco, params ItemOrcamentoRequest[] itens)
    {
        await using var db = banco.Contexto();
        var service = banco.Orcamentos(db);
        var o = await service.CriarAsync(Cenario.Requisicao(banco.ClienteId, banco.ProdutoId, itens), CancellationToken.None);
        return await service.MarcarAguardandoRetornoAsync(o.Id, new AguardandoRetornoRequest(MeioContato.Email, o.Versao), CancellationToken.None);
    }

    internal static async Task<DecisaoResultadoDto> AprovarAsync(BancoSqlite banco, int quantidade = 3)
    {
        var o = await AguardarAsync(banco, Cenario.ItemReq(banco.ProdutoId, quantidade));
        await using var db = banco.Contexto();
        return await banco.Orcamentos(db).RegistrarDecisaoAsync(o.Id, Decisao(o.Versao), CancellationToken.None);
    }

    internal static DecisaoRequest Decisao(int versao) =>
        new(ResultadoDecisao.Aprovado, MeioContato.Email, new DateTimeOffset(Dados.Agora), null, null, versao);

    internal static PedidoService Pedidos(BancoSqlite banco, ZalekDbContext db) =>
        new(new PedidoRepository(db), new PedidoQueries(db), db, banco.Relogio, new UsuarioAtualFake(), banco.Estoque(db));

    [Theory]
    [InlineData(10, 6, 0)]
    [InlineData(4, 4, 2)]
    [InlineData(0, 0, 6)]
    public async Task Aprovacao_reserva_ate_o_disponivel_e_mantem_a_pendencia(int fisico, int reservada, int pendente)
    {
        await using var banco = await BancoSqlite.CriarAsync();
        var id = await MaterialAsync(banco, fisico);
        var resultado = await AprovarAsync(banco);
        Assert.Equal(SituacaoOrcamento.Aprovado, resultado.Orcamento.Situacao);
        var reserva = Assert.Single(resultado.Materiais!.Reservas);
        Assert.Equal(6, reserva.Quantidade);
        Assert.Equal(reservada, reserva.QuantidadeReservada);
        Assert.Equal(pendente, reserva.QuantidadePendente);
        Assert.Equal(pendente > 0, resultado.Materiais.TemPendencia);
        await using var db = banco.Contexto();
        var material = await db.MateriasPrimas.SingleAsync(x => x.Id == id);
        Assert.Equal(fisico, material.QuantidadeDisponivel);
        Assert.Equal(reservada, material.SaldoReservado);
        Assert.True(material.SaldoDisponivel >= 0);
        var posicao = Assert.Single(await banco.Estoque(db).ListarMateriasAsync(CancellationToken.None));
        Assert.Equal(pendente, posicao.FaltaComprar);
        var movimentos = await db.MovimentosMateriasPrimas.ToListAsync();
        if (reservada == 0) Assert.Empty(movimentos);
        else
        {
            var movimento = Assert.Single(movimentos);
            Assert.Equal(TipoMovimentoMateriaPrima.Reserva, movimento.Tipo);
            Assert.Equal(Dados.UsuarioId, movimento.UsuarioId);
            Assert.Equal(reservada, movimento.SaldoReservadoApos);
        }
    }

    [Fact]
    public async Task Pedido_misto_reserva_o_produto_com_ficha_e_expoe_a_ficha_ausente()
    {
        await using var banco = await BancoSqlite.CriarAsync();
        await MaterialAsync(banco, 10);
        var semFicha = Produto.Criar("Produto sem ficha", "Teste", null);
        await using (var db = banco.Contexto()) { db.Produtos.Add(semFicha); await db.SaveChangesAsync(); }
        var o = await AguardarAsync(banco, Cenario.ItemReq(banco.ProdutoId, 3), Cenario.ItemReq(semFicha.Id, 2));
        await using var consulta = banco.Contexto();
        var r = await banco.Orcamentos(consulta).RegistrarDecisaoAsync(o.Id, Decisao(o.Versao), CancellationToken.None);
        Assert.Equal(6, Assert.Single(r.Materiais!.Reservas).QuantidadeReservada);
        Assert.Equal(semFicha.Id, Assert.Single(r.Materiais.PendenciasFicha).ProdutoId);
        Assert.True(r.Materiais.TemPendencia);
        var fila = await banco.Estoque(consulta).ListarPendenciasAsync(CancellationToken.None);
        Assert.Equal(r.PedidoGerado!.Id, Assert.Single(fila).PedidoId);
    }

    [Fact]
    public async Task Cancelamento_libera_somente_a_reserva_do_pedido_e_registra_movimento()
    {
        await using var banco = await BancoSqlite.CriarAsync();
        var materialId = await MaterialAsync(banco, 10);
        var p1 = (await AprovarAsync(banco)).PedidoGerado!;
        var p2 = (await AprovarAsync(banco, 2)).PedidoGerado!;
        await using (var db = banco.Contexto())
        {
            var p = await db.Pedidos.SingleAsync(x => x.Id == p1.Id);
            await Pedidos(banco, db).AtualizarSituacaoAsync(p.Id, new(SituacaoPedido.Cancelado, "Cancelado no teste", p.Versao), CancellationToken.None);
        }
        await using var consulta = banco.Contexto();
        var material = await consulta.MateriasPrimas.SingleAsync(x => x.Id == materialId);
        Assert.Equal(10, material.QuantidadeDisponivel);
        Assert.Equal(4, material.SaldoReservado);
        Assert.Equal(4, (await consulta.ReservasMateriasPrimasPedido.SingleAsync(x => x.PedidoId == p2.Id)).QuantidadeReservada);
        var movimento = await consulta.MovimentosMateriasPrimas.SingleAsync(x => x.Tipo == TipoMovimentoMateriaPrima.LiberacaoReserva);
        Assert.Equal(-6, movimento.Variacao);
        Assert.Equal(4, movimento.SaldoReservadoApos);
    }

    [Fact]
    public async Task Baixa_transitoria_consume_somente_a_propria_reserva()
    {
        await using var banco = await BancoSqlite.CriarAsync();
        var materialId = await MaterialAsync(banco, 10);
        var p1 = (await AprovarAsync(banco)).PedidoGerado!;
        var p2 = (await AprovarAsync(banco, 2)).PedidoGerado!;
        await using (var db = banco.Contexto())
        {
            var p = await db.Pedidos.SingleAsync(x => x.Id == p1.Id);
            await Pedidos(banco, db).AtualizarSituacaoAsync(p.Id, new(SituacaoPedido.EmProducao, null, p.Versao), CancellationToken.None);
        }
        await using var consulta = banco.Contexto();
        var material = await consulta.MateriasPrimas.SingleAsync(x => x.Id == materialId);
        Assert.Equal(4, material.QuantidadeDisponivel);
        Assert.Equal(4, material.SaldoReservado);
        Assert.Equal(0, material.SaldoDisponivel);
        Assert.Equal(4, (await consulta.ReservasMateriasPrimasPedido.SingleAsync(x => x.PedidoId == p2.Id)).QuantidadeReservada);
        Assert.Equal(-6, (await consulta.MovimentosMateriasPrimas.SingleAsync(x => x.Tipo == TipoMovimentoMateriaPrima.SaidaPedido)).Variacao);
        Assert.False((await banco.Estoque(consulta).ListarReservasPedidoAsync(p1.Id, CancellationToken.None)).TemPendencia);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Pendencia_de_saldo_ou_ficha_bloqueia_producao_com_rollback_do_status(bool temFicha)
    {
        await using var banco = await BancoSqlite.CriarAsync();
        await MaterialAsync(banco, 1, temFicha);
        var id = (await AprovarAsync(banco)).PedidoGerado!.Id;
        await using (var db = banco.Contexto())
        {
            var p = await db.Pedidos.SingleAsync(x => x.Id == id);
            var erro = await Assert.ThrowsAsync<ConflitoException>(() => Pedidos(banco, db).AtualizarSituacaoAsync(id,
                new(SituacaoPedido.EmProducao, null, p.Versao), CancellationToken.None));
            Assert.Equal(409, erro.StatusCode);
        }
        await using var consulta = banco.Contexto();
        Assert.Equal(SituacaoPedido.Aberto, (await consulta.Pedidos.SingleAsync()).Situacao);
        Assert.False(await consulta.MovimentosMateriasPrimas.AnyAsync(x => x.Tipo == TipoMovimentoMateriaPrima.SaidaPedido));
    }

    [Fact]
    public async Task Falha_na_gravacao_da_reserva_desfaz_toda_a_aprovacao()
    {
        await using var banco = await BancoSqlite.CriarAsync();
        await MaterialAsync(banco, 10);
        var o = await AguardarAsync(banco, Cenario.ItemReq(banco.ProdutoId, 3));
        await using (var db = banco.Contexto())
        {
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER falha_reserva BEFORE INSERT ON reservas_materias_primas_pedido BEGIN SELECT RAISE(ABORT, 'falha-reserva'); END;");
            await Assert.ThrowsAsync<DbUpdateException>(() => banco.Orcamentos(db).RegistrarDecisaoAsync(o.Id, Decisao(o.Versao), CancellationToken.None));
        }
        await using var consulta = banco.Contexto();
        var original = await consulta.Orcamentos.Include(x => x.Decisao).SingleAsync();
        Assert.Equal(SituacaoOrcamento.AguardandoRetorno, original.Situacao);
        Assert.Null(original.Decisao);
        Assert.False(await consulta.Pedidos.AnyAsync());
        Assert.False(await consulta.ReservasMateriasPrimasPedido.AnyAsync());
        Assert.Equal(0, (await consulta.MateriasPrimas.SingleAsync()).SaldoReservado);
        Assert.False(await consulta.MovimentosMateriasPrimas.AnyAsync());
        Assert.False(await consulta.HistoricoEventos.AnyAsync(x => x.Tipo == TipoEventoHistorico.DecisaoRegistrada));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Legado_mantem_fluxo_manual_ou_ativa_controle_explicitamente(bool ativar)
    {
        await using var banco = await BancoSqlite.CriarAsync();
        await MaterialAsync(banco, 10, false);
        var id = (await AprovarAsync(banco)).PedidoGerado!.Id;
        await using (var db = banco.Contexto())
        {
            // Simula um pedido anterior à migration, sem reservas/ficha.
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM pendencias_ficha_tecnica_pedido WHERE pedido_id={id}");
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE pedidos SET controle_materiais=0 WHERE id={id}");
            if (ativar)
            {
                var mat = await db.MateriasPrimas.SingleAsync();
                db.FichasTecnicasMateriasPrimas.Add(FichaTecnicaMaterial.Criar(banco.ProdutoId, mat.Id, 2));
                await db.SaveChangesAsync();
                var r = await banco.Estoque(db).AplicarFichaPedidoAsync(id, CancellationToken.None);
                Assert.True(r.ControleMateriais);
                Assert.Equal(6, Assert.Single(r.Reservas).QuantidadeReservada);
                Assert.False(r.TemPendencia);
            }
            else
            {
                var p = await db.Pedidos.SingleAsync(x => x.Id == id);
                await Pedidos(banco, db).AtualizarSituacaoAsync(id, new(SituacaoPedido.EmProducao, null, p.Versao), CancellationToken.None);
            }
        }
        await using var consulta = banco.Contexto();
        Assert.Equal(ativar, (await consulta.Pedidos.SingleAsync()).ControleMateriais);
        Assert.Equal(ativar, await consulta.HistoricoEventos.AnyAsync(x => x.Tipo == TipoEventoHistorico.ControleMateriaisAtivado));
        Assert.False(await consulta.MovimentosMateriasPrimas.AnyAsync(x => x.Tipo == TipoMovimentoMateriaPrima.SaidaPedido));
    }

    [Fact]
    public async Task Ajuste_nao_invade_reservas_e_recebimento_nao_duplica_entrada()
    {
        await using var banco = await BancoSqlite.CriarAsync();
        var id = await MaterialAsync(banco, 6);
        await AprovarAsync(banco);
        await using (var db = banco.Contexto())
            await Assert.ThrowsAsync<Zalek360.Domain.Common.DomainException>(() => banco.Estoque(db).AtualizarMateriaAsync(id,
                new("Tecido de teste", "m", 5, 0), CancellationToken.None));
        Guid compraId;
        await using (var db = banco.Contexto())
        {
            var s = banco.Estoque(db);
            compraId = (await s.CriarCompraAsync(new(id, 4, "Fornecedor", Dados.Hoje), CancellationToken.None)).Id;
            await s.AtualizarSituacaoCompraAsync(compraId, SituacaoCompraMateriaPrima.Recebida, CancellationToken.None);
        }
        await using (var db = banco.Contexto())
            await Assert.ThrowsAsync<ConflitoException>(() => banco.Estoque(db).AtualizarSituacaoCompraAsync(compraId, SituacaoCompraMateriaPrima.Recebida, CancellationToken.None));
        await using var consulta = banco.Contexto();
        var m = await consulta.MateriasPrimas.SingleAsync();
        Assert.Equal(10, m.QuantidadeDisponivel);
        Assert.Equal(6, m.SaldoReservado);
        Assert.Single(await consulta.MovimentosMateriasPrimas.Where(x => x.Tipo == TipoMovimentoMateriaPrima.EntradaCompra).ToListAsync());
    }

    [Fact]
    public async Task Movimentos_nao_podem_ser_alterados_ou_excluidos()
    {
        await using var banco = await BancoSqlite.CriarAsync();
        await MaterialAsync(banco, 10);
        await AprovarAsync(banco);
        await using var db = banco.Contexto();
        var movimento = await db.MovimentosMateriasPrimas.SingleAsync();
        db.MovimentosMateriasPrimas.Remove(movimento);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Ajustar_consumo_preserva_a_reserva_dos_outros_pedidos()
    {
        await using var banco = await BancoSqlite.CriarAsync();
        var material = await MaterialAsync(banco, 10);
        var p1 = (await AprovarAsync(banco)).PedidoGerado!;
        var p2 = (await AprovarAsync(banco, 2)).PedidoGerado!;
        await using (var db = banco.Contexto())
        {
            var r = await banco.Estoque(db).AtualizarReservasPedidoAsync(p1.Id, new[] { new ReservaPedidoRequest(material, 8) }, CancellationToken.None);
            Assert.Equal(6, Assert.Single(r.Reservas).QuantidadeReservada);
            Assert.Equal(2, Assert.Single(r.Reservas).QuantidadePendente);
        }
        await using (var db = banco.Contexto())
            await banco.Estoque(db).AtualizarReservasPedidoAsync(p1.Id, new[] { new ReservaPedidoRequest(material, 2) }, CancellationToken.None);
        await using var consulta = banco.Contexto();
        Assert.Equal(4, (await consulta.ReservasMateriasPrimasPedido.SingleAsync(x => x.PedidoId == p2.Id)).QuantidadeReservada);
        Assert.Equal(6, (await consulta.MateriasPrimas.SingleAsync()).SaldoReservado);
        Assert.Equal(4, (await consulta.MateriasPrimas.SingleAsync()).SaldoDisponivel);
    }

    [Fact]
    public async Task Aplicar_ficha_corrigida_resolve_a_pendencia_sem_ficha()
    {
        await using var banco = await BancoSqlite.CriarAsync();
        var material = await MaterialAsync(banco, 10, false);
        var id = (await AprovarAsync(banco)).PedidoGerado!.Id;
        await using var db = banco.Contexto();
        var service = banco.Estoque(db);
        await service.SalvarFichaAsync(new(banco.ProdutoId, new[] { new FichaTecnicaLinhaRequest(material, 2) }), CancellationToken.None);
        var r = await service.AplicarFichaPedidoAsync(id, CancellationToken.None);
        Assert.Empty(r.PendenciasFicha);
        Assert.False(r.TemPendencia);
        Assert.Equal(6, Assert.Single(r.Reservas).QuantidadeReservada);
    }

    [Fact]
    public async Task Compra_cancelada_nao_movimenta_e_material_inativo_nao_recebe()
    {
        await using var banco = await BancoSqlite.CriarAsync();
        var material = await MaterialAsync(banco, 6, false);
        Guid aberta;
        await using (var db = banco.Contexto())
        {
            var service = banco.Estoque(db);
            var compra = await service.CriarCompraAsync(new(material, 4, null, Dados.Hoje), CancellationToken.None);
            var cancelada = await service.AtualizarSituacaoCompraAsync(compra.Id, SituacaoCompraMateriaPrima.Cancelada, CancellationToken.None);
            Assert.False(cancelada.Aberta);
            Assert.Empty(cancelada.SituacoesPermitidas);
            aberta = (await service.CriarCompraAsync(new(material, 4, null, Dados.Hoje), CancellationToken.None)).Id;
            await service.DefinirMateriaAtivaAsync(material, false, CancellationToken.None);
        }
        await using (var db = banco.Contexto())
            await Assert.ThrowsAsync<ConflitoException>(() => banco.Estoque(db).AtualizarSituacaoCompraAsync(aberta, SituacaoCompraMateriaPrima.Recebida, CancellationToken.None));
        await using var consulta = banco.Contexto();
        Assert.Equal(6, (await consulta.MateriasPrimas.SingleAsync()).QuantidadeDisponivel);
        Assert.False(await consulta.MovimentosMateriasPrimas.AnyAsync());
    }

    [Fact]
    public async Task Ficha_pode_ser_editada_e_nao_aceita_material_duplicado()
    {
        await using var banco = await BancoSqlite.CriarAsync();
        var material = await MaterialAsync(banco, 10);
        await using (var db = banco.Contexto())
        {
            var service = banco.Estoque(db);
            await service.SalvarFichaAsync(new(banco.ProdutoId, new[] { new FichaTecnicaLinhaRequest(material, 3) }), CancellationToken.None);
            await Assert.ThrowsAsync<ValidacaoException>(() => service.SalvarFichaAsync(new(banco.ProdutoId,
                new[] { new FichaTecnicaLinhaRequest(material, 1), new FichaTecnicaLinhaRequest(material, 1) }), CancellationToken.None));
        }
        var r = await AprovarAsync(banco);
        Assert.Equal(9, Assert.Single(r.Materiais!.Reservas).QuantidadeReservada);
    }
}
