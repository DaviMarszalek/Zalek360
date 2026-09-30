using Microsoft.EntityFrameworkCore;
using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Domain.Services;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Infrastructure.Queries;

internal sealed class OrcamentoQueries : IOrcamentoQueries
{
    private readonly ZalekDbContext _db;
    private readonly IClock _clock;

    public OrcamentoQueries(ZalekDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<ListaComContagemDto<OrcamentoListaItemDto>> ListarAsync(OrcamentoFiltro filtro, CancellationToken ct)
    {
        var (pagina, tamanho) = Paginacao.Normalizar(filtro.Pagina, filtro.TamanhoPagina);
        var consulta = AplicarFiltros(_db.Orcamentos.AsNoTracking(), filtro);

        // Contagem por situação respeitando os demais filtros (abas da lista).
        var contagens = await consulta.GroupBy(o => o.Situacao).Select(g => new { g.Key, Total = g.Count() }).ToListAsync(ct);
        var contagemPorSituacao = Enum.GetValues<SituacaoOrcamento>().ToDictionary(
            s => s.ToString(), s => contagens.FirstOrDefault(c => c.Key == s)?.Total ?? 0);
        contagemPorSituacao["Todos"] = contagens.Sum(c => c.Total);

        if (filtro.Situacao is { } situacao) consulta = consulta.Where(o => o.Situacao == situacao);
        var total = await consulta.CountAsync(ct);
        var itens = await Linhas(consulta.OrderByDescending(o => o.Numero).Skip((pagina - 1) * tamanho).Take(tamanho), ct);

        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanho));
        return new ListaComContagemDto<OrcamentoListaItemDto>(itens, total, pagina, tamanho, totalPaginas, contagemPorSituacao);
    }

    internal static async Task<List<OrcamentoListaItemDto>> Linhas(IQueryable<Orcamento> consulta, CancellationToken ct)
    {
        var linhas = await consulta
            .Select(o => new
            {
                o.Id, o.Numero, o.ClienteId, o.DataOrcamento, o.Validade, o.ValorTotal, o.ResponsavelId, o.Situacao,
                Itens = o.Itens.Count(),
                ClienteTipo = o.Cliente!.TipoPessoa,
                ClienteRazao = o.Cliente.NomeRazaoSocial,
                ClienteFantasia = o.Cliente.NomeFantasia,
                Responsavel = o.Responsavel!.Nome
            })
            .ToListAsync(ct);
        return linhas.Select(o => new OrcamentoListaItemDto(
            o.Id, o.Numero, o.ClienteId, Projecoes.NomeCliente(o.ClienteTipo, o.ClienteRazao, o.ClienteFantasia),
            o.DataOrcamento, o.Validade, o.Itens, o.ValorTotal, o.ResponsavelId, o.Responsavel, o.Situacao)).ToList();
    }

    private static IQueryable<Orcamento> AplicarFiltros(IQueryable<Orcamento> consulta, OrcamentoFiltro filtro)
    {
        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var numero = Projecoes.NumeroDaBusca(filtro.Busca);
            var termo = TextoBusca.Normalizar(filtro.Busca);
            var digitos = TextoBusca.SomenteDigitos(filtro.Busca);
            if (numero is { } n)
                consulta = consulta.Where(o => o.Numero == n || o.Cliente!.BuscaNormalizada.Contains(digitos));
            else
                consulta = consulta.Where(o => o.Cliente!.BuscaNormalizada.Contains(termo));
        }
        if (filtro.ResponsavelId is { } responsavel) consulta = consulta.Where(o => o.ResponsavelId == responsavel);
        if (filtro.ClienteId is { } cliente) consulta = consulta.Where(o => o.ClienteId == cliente);
        if (filtro.De is { } de) consulta = consulta.Where(o => o.DataOrcamento >= de);
        if (filtro.Ate is { } ate) consulta = consulta.Where(o => o.DataOrcamento <= ate);
        return consulta;
    }

    public async Task<OrcamentoDetalheDto> ObterDetalheAsync(Guid id, CancellationToken ct)
    {
        var o = await _db.Orcamentos.AsNoTracking()
                    .Include(x => x.Cliente)
                    .Include(x => x.Responsavel)
                    .Include(x => x.Itens).ThenInclude(i => i.Anexos)
                    .Include(x => x.Decisao).ThenInclude(d => d!.RegistradoPor)
                    .Include(x => x.PedidoGerado)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NaoEncontradoException("Orçamento");

        var itens = o.Itens.OrderBy(i => i.Ordem).Select(Projecoes.Item).ToList();
        var pedido = o.PedidoGerado is { } p
            ? new PedidoVinculadoDto(p.Id, p.Numero, p.Situacao, p.CriadoEm, p.PrazoEntrega)
            : null;
        var previsao = pedido?.PrazoEntrega ?? CalendarioDiasUteis.Adicionar(_clock.HojeLocal, o.PrazoEstimadoDiasUteis);
        var decisao = o.Decisao is { } d
            ? new DecisaoDto(d.Resultado, d.MeioContato, d.DataHora, d.Motivo, d.Observacao,
                d.Automatica ? "Sistema" : d.RegistradoPor?.Nome ?? "—", d.Automatica)
            : null;

        return new OrcamentoDetalheDto(
            o.Id, o.Numero, o.Situacao, Projecoes.Card(o.Cliente!), o.DataOrcamento, o.CriadoEm, o.Validade,
            o.PrazoEstimadoDiasUteis, previsao, o.CondicaoPagamento, o.ObservacoesComerciais, o.ResponsavelId,
            o.Responsavel?.Nome ?? "—", itens,
            new ResumoFinanceiroDto(itens.Count, itens.Sum(i => i.Quantidade), o.SubtotalProdutos, o.ValorPersonalizacao, o.Desconto, o.ValorTotal),
            decisao, pedido, o.AguardandoRetornoDesde, o.DataUltimaEdicao, o.PodeSerEditado, o.Versao);
    }

    public async Task<IReadOnlyList<ItemDetalheDto>> ListarItensAsync(Guid id, CancellationToken ct)
    {
        if (!await _db.Orcamentos.AnyAsync(o => o.Id == id, ct)) throw new NaoEncontradoException("Orçamento");
        var itens = await _db.ItensOrcamento.AsNoTracking()
            .Include(i => i.Anexos)
            .Where(i => i.OrcamentoId == id)
            .OrderBy(i => i.Ordem)
            .ToListAsync(ct);
        return itens.Select(Projecoes.Item).ToList();
    }

    public async Task<IReadOnlyList<ContatoDto>> ListarContatosAsync(Guid id, CancellationToken ct)
    {
        if (!await _db.Orcamentos.AnyAsync(o => o.Id == id, ct)) throw new NaoEncontradoException("Orçamento");
        var linhas = await _db.ContatosOrcamento.AsNoTracking()
            .Where(c => c.OrcamentoId == id)
            .OrderByDescending(c => c.DataHora).ThenByDescending(c => c.CriadoEm)
            .Select(c => new { c.Id, c.Tipo, c.DataHora, c.Observacao, c.CriadoEm, Autor = c.RegistradoPor != null ? c.RegistradoPor.Nome : "—" })
            .ToListAsync(ct);
        return linhas.Select(c => new ContatoDto(c.Id, c.Tipo, c.DataHora, c.Observacao, c.Autor, c.CriadoEm)).ToList();
    }

    public async Task<IReadOnlyList<HistoricoEventoDto>> ObterHistoricoAsync(Guid id, string? categoria, CancellationToken ct)
    {
        if (!await _db.Orcamentos.AnyAsync(o => o.Id == id, ct)) throw new NaoEncontradoException("Orçamento");
        var consulta = _db.HistoricoEventos.AsNoTracking()
            .Where(h => h.Escopo == EscopoHistorico.Orcamento && h.OrcamentoId == id);
        consulta = categoria?.ToLowerInvariant() switch
        {
            "contatos" => consulta.Where(h => h.Tipo == TipoEventoHistorico.ContatoRegistrado),
            "alteracoes" => consulta.Where(h => h.Tipo != TipoEventoHistorico.ContatoRegistrado),
            _ => consulta
        };
        // Linha do tempo em ordem cronológica, como no protótipo (criação → ... → decisão → pedido gerado).
        return await Projecoes.HistoricoAsync(_db, consulta.OrderBy(h => h.OcorridoEm).ThenBy(h => h.RegistradoEm), crescente: true, ct);
    }
}
