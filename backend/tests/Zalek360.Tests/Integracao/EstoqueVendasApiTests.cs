using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Persistence;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Integracao;

public sealed class EstoqueVendasApiTests
{
    private static async Task<T> Ler<T>(HttpResponseMessage r)
    {
        Assert.True(r.IsSuccessStatusCode, $"HTTP {(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}");
        return (await r.Content.ReadFromJsonAsync<T>(PropostaApiTests.Json))!;
    }
    internal static Pedido Pedido(Orcamento o, int numero, DateOnly data, DateTime agora, Guid usuario)
    {
        o.MarcarAguardandoRetorno(MeioContato.Email, data, agora, usuario);
        o.RegistrarDecisao(ResultadoDecisao.Aprovado, MeioContato.Email, agora, data, null, null, agora, usuario);
        return Zalek360.Domain.Entities.Pedido.CriarAPartirDe(o, numero, data, agora, usuario);
    }

    [Theory]
    [InlineData("gestao@zalekpersonalizados.com.br")]
    [InlineData("admin@zalekpersonalizados.com.br")]
    public async Task Reposicao_distingue_necessidade_compra_prevista_limite_minimo_e_pendencias(string usuario)
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync(); using var c = await PropostaApiTests.Login(f, usuario);
        var m = await Ler<MateriaPrimaDto>(await c.PostAsJsonAsync("/api/estoque/materias-primas", new MateriaPrimaRequest("Material fase 5", "un", 3, 5)));
        var igual = await Ler<MateriaPrimaDto>(await c.PostAsJsonAsync("/api/estoque/materias-primas", new MateriaPrimaRequest("No mínimo fase 5", "un", 5, 5)));
        var zero = await Ler<MateriaPrimaDto>(await c.PostAsJsonAsync("/api/estoque/materias-primas", new MateriaPrimaRequest("Mínimo zero fase 5", "un", 0, 0)));
        var inativa = await Ler<MateriaPrimaDto>(await c.PostAsJsonAsync("/api/estoque/materias-primas", new MateriaPrimaRequest("Inativa fase 5", "un", 0, 5)));
        await c.PatchAsJsonAsync($"/api/estoque/materias-primas/{inativa.Id}/status", new { ativo = false });
        Guid pedidoId;
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZalekDbContext>();
            var u = await db.Usuarios.FirstAsync(); var cliente = await db.Clientes.FirstAsync(); var produto = await db.Produtos.FirstAsync();
            var o = Orcamento.Criar(990, cliente.Id, u.Id, f.Relogio.HojeLocal, Dados.Comercial(f.Relogio.HojeLocal.AddDays(15)), [Dados.Item(produto.Id, 4, 40, 0)], f.Relogio.AgoraUtc, u.Id);
            var p = Pedido(o, 990, f.Relogio.HojeLocal, f.Relogio.AgoraUtc, u.Id); db.AddRange(o, p);
            var materia = await db.MateriasPrimas.SingleAsync(x => x.Id == m.Id); materia.AlterarReserva(3);
            db.ReservasMateriasPrimasPedido.Add(ReservaMateriaPrimaPedido.Criar(p.Id, m.Id, 8, f.Relogio.AgoraUtc, 3));
            var liberada = ReservaMateriaPrimaPedido.Criar(p.Id, m.Id, 100, f.Relogio.AgoraUtc, 0); liberada.DefinirSituacao(SituacaoReservaMaterial.Liberada); db.Add(liberada);
            await db.SaveChangesAsync(); pedidoId = p.Id;
        }
        var compra = await Ler<CompraMateriaPrimaDto>(await c.PostAsJsonAsync("/api/estoque/compras", new CompraMateriaPrimaRequest(m.Id, 4, null, f.Relogio.HojeLocal.AddDays(3))));
        var lista = await Ler<List<MateriaPrimaDto>>(await c.GetAsync("/api/estoque/reposicao"));
        var reposicao = Assert.Single(lista, x => x.Id == m.Id);
        Assert.Equal(0, reposicao.SaldoDisponivel); Assert.Equal(5, reposicao.QuantidadePendente); Assert.Equal(10, reposicao.QuantidadeSugerida); Assert.Equal(4, reposicao.QuantidadeAReceber); Assert.Equal(6, reposicao.FaltaComprar);
        Assert.DoesNotContain(lista, x => x.Id == igual.Id || x.Id == zero.Id || x.Id == inativa.Id);
        var abaixo = await Ler<List<MateriaPrimaDto>>(await c.GetAsync("/api/estoque/materias-primas?abaixoDoMinimo=true"));
        Assert.Contains(abaixo, x => x.Id == m.Id); Assert.DoesNotContain(abaixo, x => x.Id == igual.Id || x.Id == zero.Id || x.Id == inativa.Id);
        var dashboard = await Ler<DashboardDto>(await c.GetAsync("/api/dashboard")); Assert.Equal(lista.Count, dashboard.Indicadores.MateriasParaReposicao);
        await c.PatchAsJsonAsync($"/api/estoque/compras/{compra.Id}/situacao", new SituacaoCompraRequest(SituacaoCompraMateriaPrima.Cancelada));
        var semCompra = Assert.Single(await Ler<List<MateriaPrimaDto>>(await c.GetAsync("/api/estoque/reposicao")), x => x.Id == m.Id); Assert.Equal(10, semCompra.FaltaComprar);
        var pedido = await Ler<PedidoDetalheDto>(await c.GetAsync($"/api/pedidos/{pedidoId}"));
        await Ler<PedidoDetalheDto>(await c.PatchAsJsonAsync($"/api/pedidos/{pedidoId}/status", new AtualizarSituacaoPedidoRequest(SituacaoPedido.Cancelado, "Cancelamento de teste", pedido.Versao)));
        var cancelado = Assert.Single(await Ler<List<MateriaPrimaDto>>(await c.GetAsync("/api/estoque/reposicao")), x => x.Id == m.Id);
        Assert.Equal(0, cancelado.QuantidadePendente); Assert.Equal(3, cancelado.SaldoDisponivel); Assert.Equal(2, cancelado.QuantidadeSugerida);
    }

    private static string Cpf()
    {
        var cpf = Random.Shared.Next(100000000, 999999999).ToString();
        for (var peso = 10; peso <= 11; peso++)
        {
            var soma = cpf.Select((digito, i) => (digito - '0') * (peso - i)).Sum();
            var resto = soma * 10 % 11; cpf += resto == 10 ? "0" : resto.ToString();
        }
        return cpf;
    }

    internal static async Task VerificarVendas(ApiPdfFactory f)
    {
        var inicioMes = new DateOnly(f.Relogio.HojeLocal.Year, f.Relogio.HojeLocal.Month, 1);
        var proximoMes = inicioMes.AddMonths(1);
        var inicioUtc = f.Relogio.InicioDoDiaLocalEmUtc(inicioMes);
        var fimUtc = f.Relogio.InicioDoDiaLocalEmUtc(proximoMes);
        await f.PrepararSeedAsync(); using var c = await PropostaApiTests.Login(f);
        var ids = new List<Guid>(); Guid clienteId;
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZalekDbContext>();
            var u = await db.Usuarios.FirstAsync(); var clientes = new[] { Cliente.Criar(Dados.ClientePf(Cpf()), f.Relogio.AgoraUtc, u.Id), Cliente.Criar(Dados.ClientePf(Cpf()), f.Relogio.AgoraUtc, u.Id) }; db.AddRange(clientes); clienteId = clientes[0].Id; var produto = await db.Produtos.FirstAsync();
            var datas = new[] { inicioUtc.AddTicks(-1), inicioUtc, fimUtc.AddTicks(-1), fimUtc };
            for (var i = 0; i < 6; i++)
            {
                var data = inicioMes.AddMonths(-1).AddDays(9); var agora = f.Relogio.InicioDoDiaLocalEmUtc(data).AddHours(10);
                var o = Orcamento.Criar(1000 + i, i == 5 ? clientes[1].Id : clienteId, u.Id, data, Dados.Comercial(data.AddDays(15)), [Dados.Item(produto.Id, 1, 100 + i, 0)], agora, u.Id);
                var p = Pedido(o, 1000 + i, data, agora, u.Id);
                if (i < 4 || i == 5)
                {
                    var entrega = i == 5 ? inicioUtc.AddDays(14).AddHours(12) : datas[i];
                    p.AtualizarSituacao(SituacaoPedido.EmProducao, null, entrega.AddMinutes(-2), u.Id); p.AtualizarSituacao(SituacaoPedido.Pronto, null, entrega.AddMinutes(-1), u.Id); p.AtualizarSituacao(SituacaoPedido.Entregue, null, entrega, u.Id);
                }
                else p.AtualizarSituacao(SituacaoPedido.Cancelado, "Teste", agora, u.Id);
                db.AddRange(o, p); ids.Add(p.Id);
            }
            await db.SaveChangesAsync();
        }
        var filtro = $"/api/pedidos?clienteId={clienteId}&dataReferencia=Entrega&de={inicioMes:yyyy-MM-dd}&ate={proximoMes.AddDays(-1):yyyy-MM-dd}";
        var lista = await Ler<ListaComContagemDto<PedidoListaItemDto>>(await c.GetAsync(filtro));
        Assert.Equal(2, lista.Total); Assert.Equal(2, lista.ContagemPorSituacao["Entregue"]); Assert.Equal(0, lista.ContagemPorSituacao["Cancelado"]);
        Assert.Equal(new[] { ids[1], ids[2] }.Order(), lista.Itens.Select(x => x.Id).Order());
        var porCriacao = await Ler<ListaComContagemDto<PedidoListaItemDto>>(await c.GetAsync(filtro.Replace("Entrega", "Criacao"))); Assert.DoesNotContain(porCriacao.Itens, x => ids.Contains(x.Id));
        Assert.Equal(0, (await Ler<ListaComContagemDto<PedidoListaItemDto>>(await c.GetAsync(filtro + "&status=Cancelado"))).Total);
        Assert.Equal(0, (await Ler<ListaComContagemDto<PedidoListaItemDto>>(await c.GetAsync(filtro.Replace(clienteId.ToString(), Guid.NewGuid().ToString())))).Total);
        var primeira = await Ler<ListaComContagemDto<PedidoListaItemDto>>(await c.GetAsync(filtro + "&pagina=1&tamanhoPagina=1")); var segunda = await Ler<ListaComContagemDto<PedidoListaItemDto>>(await c.GetAsync(filtro + "&pagina=2&tamanhoPagina=1")); Assert.NotEqual(primeira.Itens[0].Id, segunda.Itens[0].Id); Assert.Equal(2, primeira.TotalPaginas);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/pedidos?de=2026-12-01&ate=2026-11-01")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/pedidos?dataReferencia=Entrega&ate=9999-12-31")).StatusCode);
        var dashboard = await Ler<DashboardDto>(await c.GetAsync("/api/dashboard"));
        using var escopo = f.Services.CreateScope(); var banco = escopo.ServiceProvider.GetRequiredService<ZalekDbContext>();
        var inicio = inicioUtc; var fim = fimUtc;
        var valores = await banco.Pedidos.Where(x => x.Situacao == SituacaoPedido.Entregue && x.EntregueEm >= inicio && x.EntregueEm < fim).Select(x => x.ValorTotal).ToListAsync();
        Assert.Equal(valores.Count, dashboard.Indicadores.PedidosConcluidosNoMes); Assert.Equal(valores.Sum(), dashboard.Indicadores.ValorEntregueNoMes);
        Assert.Equal(inicioMes, dashboard.Indicadores.InicioMes); Assert.Equal(proximoMes.AddDays(-1), dashboard.Indicadores.FimMes);
    }

    [Fact] public async Task Vendas_e_filtros_respeitam_data_de_entrega_fuso_cliente_status_e_paginacao() { await using var f = new ApiPdfFactory(); await VerificarVendas(f); }

    [PostgresFact]
    public async Task Filtros_e_vendas_funcionam_em_PostgreSQL_real()
    {
        var admin = Environment.GetEnvironmentVariable("ZALEK_TEST_POSTGRES")!; var nome = "vendas_test_" + Guid.NewGuid().ToString("N");
        await using var conexao = new NpgsqlConnection(admin); await conexao.OpenAsync();
        await using (var cmd = new NpgsqlCommand($"CREATE DATABASE {nome}", conexao)) await cmd.ExecuteNonQueryAsync();
        try
        {
            var cs = new NpgsqlConnectionStringBuilder(admin) { Database = nome }.ConnectionString;
            await using (var db = new ZalekDbContext(new DbContextOptionsBuilder<ZalekDbContext>().UseNpgsql(cs).Options)) await db.Database.MigrateAsync();
            await using var f = new ApiPdfFactory(postgres: cs); await VerificarVendas(f);
        }
        finally { await using var cmd = new NpgsqlCommand($"DROP DATABASE {nome} WITH (FORCE)", conexao); await cmd.ExecuteNonQueryAsync(); }
    }

    [Theory, InlineData(PerfilUsuario.Atendente), InlineData(PerfilUsuario.Operador)]
    public async Task Reposicao_restrita_a_operacao(PerfilUsuario perfil)
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync(); using var c = f.CreateClient(); Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/estoque/reposicao")).StatusCode);
        using var scope = f.Services.CreateScope(); var u = Usuario.Criar("Legado", "legado@example.test", "hash", perfil, f.Relogio.AgoraUtc);
        c.DefaultRequestHeaders.Authorization = new("Bearer", scope.ServiceProvider.GetRequiredService<ITokenService>().Gerar(u, false).Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/estoque/reposicao")).StatusCode);
    }
}
