using Microsoft.EntityFrameworkCore;
using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Infrastructure.Queries;

internal sealed class ClienteQueries : IClienteQueries
{
    private readonly ZalekDbContext _db;
    private readonly IClock _clock;

    public ClienteQueries(ZalekDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PaginaResultado<ClienteListaItemDto>> ListarAsync(ClienteFiltro filtro, CancellationToken ct)
    {
        var (pagina, tamanho) = Paginacao.Normalizar(filtro.Pagina, filtro.TamanhoPagina);
        var consulta = _db.Clientes.AsNoTracking();

        // Busca por nome, razão social, nome fantasia, CPF/CNPJ, telefone, WhatsApp e e-mail (coluna normalizada).
        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termo = TextoBusca.Normalizar(filtro.Busca);
            var digitos = TextoBusca.SomenteDigitos(filtro.Busca);
            consulta = digitos.Length >= 3
                ? consulta.Where(c => c.BuscaNormalizada.Contains(termo) || c.BuscaNormalizada.Contains(digitos))
                : consulta.Where(c => c.BuscaNormalizada.Contains(termo));
        }
        if (filtro.Tipo is { } tipo) consulta = consulta.Where(c => c.TipoPessoa == tipo);
        if (!string.IsNullOrWhiteSpace(filtro.Cidade))
        {
            var partes = filtro.Cidade.Split('/', 2, StringSplitOptions.TrimEntries);
            var cidade = partes[0];
            consulta = consulta.Where(c => c.EnderecoCidade == cidade);
            if (partes.Length == 2 && partes[1].Length > 0)
            {
                var uf = partes[1].ToUpperInvariant();
                consulta = consulta.Where(c => c.EnderecoUf == uf);
            }
        }

        var total = await consulta.CountAsync(ct);
        var clientes = await consulta
            .OrderBy(c => c.NomeFantasia ?? c.NomeRazaoSocial)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(ct);

        var ids = clientes.Select(c => c.Id).ToList();
        var orcamentos = await _db.Orcamentos.AsNoTracking()
            .Where(o => ids.Contains(o.ClienteId))
            .Select(o => new { o.ClienteId, o.Id, o.Numero, o.DataOrcamento })
            .ToListAsync(ct);
        var ultimos = orcamentos
            .GroupBy(o => o.ClienteId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.DataOrcamento).ThenByDescending(o => o.Numero).First());

        var itens = clientes.Select(c => new ClienteListaItemDto(
            c.Id, c.TipoPessoa, c.NomeExibicao, c.TipoPessoa == TipoPessoa.PessoaJuridica ? c.NomeRazaoSocial : null,
            c.Documento, c.Telefone, c.WhatsApp, c.Email, c.CidadeUf,
            ultimos.TryGetValue(c.Id, out var u) ? new UltimoOrcamentoResumoDto(u.Id, u.Numero, u.DataOrcamento) : null)).ToList();

        return new PaginaResultado<ClienteListaItemDto>(itens, total, pagina, tamanho);
    }

    public async Task<IReadOnlyList<string>> ListarCidadesAsync(CancellationToken ct)
    {
        var cidades = await _db.Clientes.AsNoTracking()
            .Where(c => c.EnderecoCidade != null)
            .Select(c => new { c.EnderecoCidade, c.EnderecoUf })
            .Distinct()
            .ToListAsync(ct);
        return cidades
            .Select(c => c.EnderecoUf is null ? c.EnderecoCidade! : $"{c.EnderecoCidade}/{c.EnderecoUf}")
            .OrderBy(c => c, StringComparer.CurrentCulture)
            .ToList();
    }

    public async Task<ClienteDetalheDto> ObterDetalheAsync(Guid id, CancellationToken ct)
    {
        var c = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NaoEncontradoException("Cliente");
        var totalOrcamentos = await _db.Orcamentos.CountAsync(o => o.ClienteId == id, ct);
        var totalPedidos = await _db.Pedidos.CountAsync(p => p.ClienteId == id, ct);
        return new ClienteDetalheDto(
            c.Id, c.TipoPessoa, c.Documento, c.NomeRazaoSocial, c.NomeFantasia, c.NomeExibicao, c.ContatoNome, c.ContatoCargo,
            c.Telefone, c.WhatsApp, c.Email,
            new EnderecoDto(c.EnderecoCep, c.EnderecoLogradouro, c.EnderecoNumero, c.EnderecoComplemento, c.EnderecoBairro, c.EnderecoCidade, c.EnderecoUf),
            c.CriadoEm, totalOrcamentos, totalPedidos, c.Versao);
    }

    public async Task<ClienteExistenteDto?> ObterPorDocumentoAsync(string documentoDigitos, Guid? excetoId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(documentoDigitos)) return null;
        var c = await _db.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Documento == documentoDigitos && (excetoId == null || x.Id != excetoId), ct);
        if (c is null) return null;
        var numeros = await _db.Orcamentos.AsNoTracking().Where(o => o.ClienteId == c.Id).Select(o => o.Numero).ToListAsync(ct);
        return new ClienteExistenteDto(c.Id, c.NomeExibicao, c.TipoPessoa, c.Documento, c.CidadeUf,
            numeros.Count == 0 ? null : numeros.Max());
    }

    public async Task<ClienteVisaoGeralDto> ObterVisaoGeralAsync(Guid id, CancellationToken ct)
    {
        if (!await _db.Clientes.AnyAsync(c => c.Id == id, ct)) throw new NaoEncontradoException("Cliente");

        var orcamentos = await ResumosOrcamentos(_db.Orcamentos.AsNoTracking().Where(o => o.ClienteId == id), ct);
        var pedidos = await ResumosPedidos(_db.Pedidos.AsNoTracking().Where(p => p.ClienteId == id), ct);
        var interacoes = await Projecoes.HistoricoAsync(_db,
            _db.HistoricoEventos.AsNoTracking()
                .Where(h => h.ClienteId == id && (h.Tipo == TipoEventoHistorico.ContatoRegistrado || h.Tipo == TipoEventoHistorico.DecisaoRegistrada))
                .OrderByDescending(h => h.OcorridoEm).ThenByDescending(h => h.RegistradoEm)
                .Take(5), crescente: false, ct);

        return new ClienteVisaoGeralDto(
            orcamentos.Count,
            orcamentos.Count(o => o.Situacao == SituacaoOrcamento.Aprovado),
            pedidos.Count,
            pedidos.Where(p => p.Situacao != SituacaoPedido.Cancelado).Sum(p => p.ValorTotal),
            orcamentos.FirstOrDefault(),
            pedidos.FirstOrDefault(),
            interacoes);
    }

    public async Task<ListaFiltradaDto<OrcamentoResumoDto>> ListarOrcamentosAsync(Guid clienteId, FiltroOrcamentosCliente filtro, CancellationToken ct)
    {
        if (!await _db.Clientes.AnyAsync(c => c.Id == clienteId, ct)) throw new NaoEncontradoException("Cliente");
        var baseConsulta = _db.Orcamentos.AsNoTracking().Where(o => o.ClienteId == clienteId);
        var totalSemFiltro = await baseConsulta.CountAsync(ct);
        var consulta = baseConsulta;
        if (filtro.De is { } de) consulta = consulta.Where(o => o.DataOrcamento >= de);
        if (filtro.Ate is { } ate) consulta = consulta.Where(o => o.DataOrcamento <= ate);
        if (filtro.Situacao is { } s) consulta = consulta.Where(o => o.Situacao == s);
        var itens = await ResumosOrcamentos(consulta, ct);
        return new ListaFiltradaDto<OrcamentoResumoDto>(itens, itens.Count, totalSemFiltro);
    }

    public async Task<ListaFiltradaDto<PedidoResumoDto>> ListarPedidosAsync(Guid clienteId, FiltroPedidosCliente filtro, CancellationToken ct)
    {
        if (!await _db.Clientes.AnyAsync(c => c.Id == clienteId, ct)) throw new NaoEncontradoException("Cliente");
        var baseConsulta = _db.Pedidos.AsNoTracking().Where(p => p.ClienteId == clienteId);
        var totalSemFiltro = await baseConsulta.CountAsync(ct);
        var consulta = baseConsulta;
        if (filtro.De is { } de) consulta = consulta.Where(p => p.DataPedido >= de);
        if (filtro.Ate is { } ate) consulta = consulta.Where(p => p.DataPedido <= ate);
        if (filtro.Situacao is { } s) consulta = consulta.Where(p => p.Situacao == s);
        var itens = await ResumosPedidos(consulta, ct);
        return new ListaFiltradaDto<PedidoResumoDto>(itens, itens.Count, totalSemFiltro);
    }

    /// <summary>UC05 — histórico consolidado (orçamentos, pedidos, contatos e decisões), somente leitura.</summary>
    public async Task<IReadOnlyList<HistoricoEventoDto>> ObterHistoricoAsync(Guid clienteId, FiltroHistoricoCliente filtro, CancellationToken ct)
    {
        if (!await _db.Clientes.AnyAsync(c => c.Id == clienteId, ct)) throw new NaoEncontradoException("Cliente");
        var consulta = _db.HistoricoEventos.AsNoTracking().Where(h => h.ClienteId == clienteId);

        if (filtro.De is { } de)
        {
            var inicio = de.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-12);
            consulta = consulta.Where(h => h.OcorridoEm >= inicio);
        }
        if (filtro.Ate is { } ate)
        {
            var fim = ate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(12);
            consulta = consulta.Where(h => h.OcorridoEm < fim);
        }

        consulta = filtro.Tipo?.ToLowerInvariant() switch
        {
            "orcamentos" => consulta.Where(h => h.Escopo == EscopoHistorico.Orcamento
                && h.Tipo != TipoEventoHistorico.ContatoRegistrado && h.Tipo != TipoEventoHistorico.DecisaoRegistrada
                && h.Tipo != TipoEventoHistorico.OrcamentoExpirado),
            "pedidos" => consulta.Where(h => h.Escopo == EscopoHistorico.Pedido || h.Tipo == TipoEventoHistorico.PedidoGerado),
            "contatos" => consulta.Where(h => h.Tipo == TipoEventoHistorico.ContatoRegistrado),
            "decisoes" => consulta.Where(h => h.Tipo == TipoEventoHistorico.DecisaoRegistrada || h.Tipo == TipoEventoHistorico.OrcamentoExpirado),
            _ => consulta
        };

        if (filtro.SituacaoOrcamento is { } so)
            consulta = consulta.Where(h => h.OrcamentoId != null && _db.Orcamentos.Any(o => o.Id == h.OrcamentoId && o.Situacao == so));
        if (filtro.SituacaoPedido is { } sp)
            consulta = consulta.Where(h => h.PedidoId != null && _db.Pedidos.Any(p => p.Id == h.PedidoId && p.Situacao == sp));

        var limite = Math.Clamp(filtro.Limite ?? 200, 1, 500);
        var eventos = await Projecoes.HistoricoAsync(_db,
            consulta.OrderByDescending(h => h.OcorridoEm).ThenByDescending(h => h.RegistradoEm).Take(limite), crescente: false, ct);

        // Filtro de período no fuso de negócio (a pré-filtragem SQL usa margem de 12h para acomodar o fuso).
        return eventos.Where(e =>
        {
            var data = _clock.ParaDataLocal(e.OcorridoEm);
            return (filtro.De is null || data >= filtro.De) && (filtro.Ate is null || data <= filtro.Ate);
        }).ToList();
    }

    public async Task<IReadOnlyList<ObservacaoClienteDto>> ListarObservacoesAsync(Guid clienteId, CancellationToken ct)
    {
        var linhas = await _db.ObservacoesCliente.AsNoTracking()
            .Where(o => o.ClienteId == clienteId)
            .OrderBy(o => o.CriadoEm)
            .Select(o => new { o.Id, o.Texto, o.CriadoEm, Autor = o.Autor != null ? o.Autor.Nome : "—" })
            .ToListAsync(ct);
        return linhas.Select(o => new ObservacaoClienteDto(o.Id, o.Texto, o.CriadoEm, o.Autor, Usuario.CalcularIniciais(o.Autor))).ToList();
    }

    private static async Task<List<OrcamentoResumoDto>> ResumosOrcamentos(IQueryable<Orcamento> consulta, CancellationToken ct)
    {
        var linhas = await consulta
            .OrderByDescending(o => o.DataOrcamento).ThenByDescending(o => o.Numero)
            .Select(o => new
            {
                o.Id, o.Numero, o.DataOrcamento, o.Validade, o.Situacao, o.ValorTotal,
                Itens = o.Itens.Count(),
                Unidades = o.Itens.Sum(i => (int?)i.Quantidade) ?? 0,
                Responsavel = o.Responsavel != null ? o.Responsavel.Nome : "—"
            })
            .ToListAsync(ct);
        return linhas.Select(o => new OrcamentoResumoDto(o.Id, o.Numero, o.DataOrcamento, o.Validade, o.Situacao, o.ValorTotal,
            o.Itens, o.Unidades, o.Responsavel)).ToList();
    }

    private static async Task<List<PedidoResumoDto>> ResumosPedidos(IQueryable<Pedido> consulta, CancellationToken ct)
    {
        var linhas = await consulta
            .OrderByDescending(p => p.DataPedido).ThenByDescending(p => p.Numero)
            .Select(p => new
            {
                p.Id, p.Numero, p.DataPedido, p.PrazoEntrega, p.Situacao, p.ValorTotal, p.OrcamentoOrigemId,
                OrcamentoNumero = p.OrcamentoOrigem != null ? p.OrcamentoOrigem.Numero : 0,
                Responsavel = p.Responsavel != null ? p.Responsavel.Nome : "—"
            })
            .ToListAsync(ct);
        return linhas.Select(p => new PedidoResumoDto(p.Id, p.Numero, p.DataPedido, p.PrazoEntrega, p.Situacao, p.ValorTotal,
            p.OrcamentoOrigemId, p.OrcamentoNumero, p.Responsavel)).ToList();
    }
}
