using Zalek360.Application.Dtos;

namespace Zalek360.Application.Interfaces;

// Lado de leitura (queries): projeções diretas para DTOs, implementadas na Infraestrutura (EF Core, AsNoTracking).
// As consultas lançam NaoEncontradoException quando o recurso não existe.

public interface IClienteQueries
{
    Task<PaginaResultado<ClienteListaItemDto>> ListarAsync(ClienteFiltro filtro, CancellationToken ct);
    Task<IReadOnlyList<string>> ListarCidadesAsync(CancellationToken ct);
    Task<ClienteDetalheDto> ObterDetalheAsync(Guid id, CancellationToken ct);
    Task<ClienteExistenteDto?> ObterPorDocumentoAsync(string documentoDigitos, Guid? excetoId, CancellationToken ct);
    Task<ClienteVisaoGeralDto> ObterVisaoGeralAsync(Guid id, CancellationToken ct);
    Task<ListaFiltradaDto<OrcamentoResumoDto>> ListarOrcamentosAsync(Guid clienteId, FiltroOrcamentosCliente filtro, CancellationToken ct);
    Task<ListaFiltradaDto<PedidoResumoDto>> ListarPedidosAsync(Guid clienteId, FiltroPedidosCliente filtro, CancellationToken ct);
    Task<IReadOnlyList<HistoricoEventoDto>> ObterHistoricoAsync(Guid clienteId, FiltroHistoricoCliente filtro, CancellationToken ct);
    Task<IReadOnlyList<ObservacaoClienteDto>> ListarObservacoesAsync(Guid clienteId, CancellationToken ct);
}

public interface IOrcamentoQueries
{
    Task<ListaComContagemDto<OrcamentoListaItemDto>> ListarAsync(OrcamentoFiltro filtro, CancellationToken ct);
    Task<OrcamentoDetalheDto> ObterDetalheAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ItemDetalheDto>> ListarItensAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ContatoDto>> ListarContatosAsync(Guid id, CancellationToken ct);
    /// <summary>Categoria: "alteracoes", "contatos" ou nulo (todos).</summary>
    Task<IReadOnlyList<HistoricoEventoDto>> ObterHistoricoAsync(Guid id, string? categoria, CancellationToken ct);
}

public interface IPedidoQueries
{
    Task<ListaComContagemDto<PedidoListaItemDto>> ListarAsync(PedidoFiltro filtro, CancellationToken ct);
    Task<PedidoDetalheDto> ObterDetalheAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<HistoricoEventoDto>> ObterHistoricoAsync(Guid id, CancellationToken ct);
}

public interface IDashboardQueries
{
    Task<DashboardDto> ObterAsync(CancellationToken ct);
}

public interface ICatalogoQueries
{
    Task<IReadOnlyList<ProdutoDto>> ListarProdutosAsync(CancellationToken ct);
    Task<IReadOnlyList<UsuarioDto>> ListarResponsaveisAsync(CancellationToken ct);
}
