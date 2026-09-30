using Microsoft.EntityFrameworkCore;
using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Infrastructure.Queries;

internal sealed class PedidoQueries : IPedidoQueries
{
    private readonly ZalekDbContext _db;

    public PedidoQueries(ZalekDbContext db) => _db = db;

    public async Task<ListaComContagemDto<PedidoListaItemDto>> ListarAsync(PedidoFiltro filtro, CancellationToken ct)
    {
        var (pagina, tamanho) = Paginacao.Normalizar(filtro.Pagina, filtro.TamanhoPagina);
        var consulta = _db.Pedidos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var numero = Projecoes.NumeroDaBusca(filtro.Busca);
            var termo = TextoBusca.Normalizar(filtro.Busca);
            var digitos = TextoBusca.SomenteDigitos(filtro.Busca);
            consulta = numero is { } n
                // "#000057" encontra o pedido; "#000124" encontra o pedido gerado pelo orçamento 124.
                ? consulta.Where(p => p.Numero == n || p.OrcamentoOrigem!.Numero == n || p.Cliente!.BuscaNormalizada.Contains(digitos))
                : consulta.Where(p => p.Cliente!.BuscaNormalizada.Contains(termo));
        }
        if (filtro.ResponsavelId is { } responsavel) consulta = consulta.Where(p => p.ResponsavelId == responsavel);
        if (filtro.De is { } de) consulta = consulta.Where(p => p.DataPedido >= de);
        if (filtro.Ate is { } ate) consulta = consulta.Where(p => p.DataPedido <= ate);

        var contagens = await consulta.GroupBy(p => p.Situacao).Select(g => new { g.Key, Total = g.Count() }).ToListAsync(ct);
        var contagemPorSituacao = Enum.GetValues<SituacaoPedido>().ToDictionary(
            s => s.ToString(), s => contagens.FirstOrDefault(c => c.Key == s)?.Total ?? 0);
        contagemPorSituacao["Todos"] = contagens.Sum(c => c.Total);

        if (filtro.Situacao is { } situacao) consulta = consulta.Where(p => p.Situacao == situacao);
        var total = await consulta.CountAsync(ct);
        var linhas = await consulta
            .OrderByDescending(p => p.Numero)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(p => new
            {
                p.Id, p.Numero, p.OrcamentoOrigemId, OrcamentoNumero = p.OrcamentoOrigem!.Numero, p.ClienteId,
                ClienteTipo = p.Cliente!.TipoPessoa, ClienteRazao = p.Cliente.NomeRazaoSocial, ClienteFantasia = p.Cliente.NomeFantasia,
                p.DataPedido, p.ValorTotal, p.PrazoEntrega, p.ResponsavelId, Responsavel = p.Responsavel!.Nome, p.Situacao, p.CriadoEm
            })
            .ToListAsync(ct);

        var itens = linhas.Select(p => new PedidoListaItemDto(
            p.Id, p.Numero, p.OrcamentoOrigemId, p.OrcamentoNumero, p.ClienteId,
            Projecoes.NomeCliente(p.ClienteTipo, p.ClienteRazao, p.ClienteFantasia), p.DataPedido, p.ValorTotal, p.PrazoEntrega,
            p.ResponsavelId, p.Responsavel, p.Situacao, p.CriadoEm)).ToList();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanho));
        return new ListaComContagemDto<PedidoListaItemDto>(itens, total, pagina, tamanho, totalPaginas, contagemPorSituacao);
    }

    public async Task<PedidoDetalheDto> ObterDetalheAsync(Guid id, CancellationToken ct)
    {
        var p = await _db.Pedidos.AsNoTracking()
                    .Include(x => x.Cliente)
                    .Include(x => x.Responsavel)
                    .Include(x => x.Itens).ThenInclude(i => i.Anexos)
                    .Include(x => x.OrcamentoOrigem).ThenInclude(o => o!.Decisao).ThenInclude(d => d!.RegistradoPor)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NaoEncontradoException("Pedido");

        var origem = p.OrcamentoOrigem!;
        var decisao = origem.Decisao;
        var itens = p.Itens.OrderBy(i => i.Ordem).Select(Projecoes.Item).ToList();
        return new PedidoDetalheDto(
            p.Id, p.Numero, p.Situacao, Projecoes.Card(p.Cliente!),
            new OrigemPedidoDto(origem.Id, origem.Numero, origem.Validade, decisao?.DataHora, decisao?.MeioContato, decisao?.RegistradoPor?.Nome),
            p.DataPedido, p.CriadoEm, p.PrazoEntrega, p.PrazoEstimadoDiasUteis, p.CondicaoPagamento, p.ObservacoesComerciais,
            p.ObservacaoDecisao, p.ResponsavelId, p.Responsavel?.Nome ?? "—", itens,
            new ResumoFinanceiroDto(itens.Count, itens.Sum(i => i.Quantidade), p.SubtotalProdutos, p.ValorPersonalizacao, p.Desconto, p.ValorTotal),
            p.ProximaSituacao, p.PodeCancelar, p.EntregueEm, p.CanceladoEm, p.MotivoCancelamento, p.Versao);
    }

    public async Task<IReadOnlyList<HistoricoEventoDto>> ObterHistoricoAsync(Guid id, CancellationToken ct)
    {
        if (!await _db.Pedidos.AnyAsync(p => p.Id == id, ct)) throw new NaoEncontradoException("Pedido");
        var consulta = _db.HistoricoEventos.AsNoTracking()
            .Where(h => h.Escopo == EscopoHistorico.Pedido && h.PedidoId == id)
            .OrderBy(h => h.OcorridoEm).ThenBy(h => h.RegistradoEm);
        return await Projecoes.HistoricoAsync(_db, consulta, crescente: true, ct);
    }
}
