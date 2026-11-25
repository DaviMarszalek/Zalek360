using Microsoft.EntityFrameworkCore;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Application.Services;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Infrastructure.Queries;

internal sealed class DashboardQueries : IDashboardQueries
{
    private static readonly string[] Meses =
    {
        "janeiro", "fevereiro", "março", "abril", "maio", "junho",
        "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"
    };

    private readonly ZalekDbContext _db;
    private readonly IClock _clock;

    private readonly EstoqueService _estoque;
    public DashboardQueries(ZalekDbContext db, IClock clock, EstoqueService estoque)
    {
        _db = db;
        _clock = clock; _estoque = estoque;
    }

    public async Task<DashboardDto> ObterAsync(CancellationToken ct)
    {
        var hoje = _clock.HojeLocal;
        var inicioMes = new DateOnly(hoje.Year, hoje.Month, 1);
        var inicioMesUtc = _clock.InicioDoDiaLocalEmUtc(inicioMes);
        var inicioProximoMesUtc = _clock.InicioDoDiaLocalEmUtc(inicioMes.AddMonths(1));
        var limiteVencimento = hoje.AddDays(3);

        var orcamentos = _db.Orcamentos.AsNoTracking();
        var emElaboracao = await orcamentos.CountAsync(o => o.Situacao == SituacaoOrcamento.EmElaboracao, ct);
        var criadosHoje = await orcamentos.CountAsync(o => o.Situacao == SituacaoOrcamento.EmElaboracao && o.DataOrcamento == hoje, ct);
        var aguardando = await orcamentos.CountAsync(o => o.Situacao == SituacaoOrcamento.AguardandoRetorno, ct);
        var vencendo = await orcamentos.CountAsync(o => o.Situacao == SituacaoOrcamento.AguardandoRetorno
                                                         && o.Validade >= hoje && o.Validade <= limiteVencimento, ct);
        // Soma feita em memória: portável entre provedores e com poucos registros por mês.
        var valoresAprovados = await orcamentos
            .Where(o => o.Situacao == SituacaoOrcamento.Aprovado && o.Decisao != null
                        && o.Decisao.DataHora >= inicioMesUtc && o.Decisao.DataHora < inicioProximoMesUtc)
            .Select(o => o.ValorTotal)
            .ToListAsync(ct);

        var pedidos = _db.Pedidos.AsNoTracking();
        var emAndamento = await pedidos.CountAsync(p => p.Situacao == SituacaoPedido.Aberto
                                                        || p.Situacao == SituacaoPedido.EmProducao
                                                        || p.Situacao == SituacaoPedido.Pronto, ct);
        var entregues = await pedidos.Where(p => p.Situacao == SituacaoPedido.Entregue
            && p.EntregueEm >= inicioMesUtc && p.EntregueEm < inicioProximoMesUtc).Select(p => p.ValorTotal).ToListAsync(ct);
        var reposicao = await _estoque.ListarReposicaoAsync(ct);

        var recentes = await OrcamentoQueries.Linhas(orcamentos.OrderByDescending(o => o.Numero).Take(6), ct);

        var aguardandoLinhas = await orcamentos
            .Where(o => o.Situacao == SituacaoOrcamento.AguardandoRetorno)
            .OrderBy(o => o.Validade).ThenBy(o => o.Numero)
            .Take(3)
            .Select(o => new
            {
                o.Id, o.Numero, o.ValorTotal, o.AguardandoRetornoDesde, o.Validade,
                ClienteTipo = o.Cliente!.TipoPessoa, ClienteRazao = o.Cliente.NomeRazaoSocial, ClienteFantasia = o.Cliente.NomeFantasia
            })
            .ToListAsync(ct);
        var listaAguardando = aguardandoLinhas.Select(o => new AguardandoRetornoItemDto(
            o.Id, o.Numero, Projecoes.NomeCliente(o.ClienteTipo, o.ClienteRazao, o.ClienteFantasia), o.ValorTotal,
            o.AguardandoRetornoDesde, o.Validade, o.Validade.DayNumber - hoje.DayNumber)).ToList();

        return new DashboardDto(
            new IndicadoresDto(emElaboracao, criadosHoje, aguardando, vencendo, valoresAprovados.Count, valoresAprovados.Sum(),
                emAndamento, entregues.Count, Meses[hoje.Month - 1], entregues.Sum(), inicioMes, inicioMes.AddMonths(1).AddDays(-1), reposicao.Count),
            recentes, listaAguardando, aguardando);
    }
}
