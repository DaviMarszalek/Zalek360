using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Entities;

namespace Zalek360.Tests.Suporte;

/// <summary>Catálogo sem fichas dos testes comerciais originais. Operações não exercitadas falham explicitamente.</summary>
public sealed class EstoqueSemFichaFake : IEstoqueRepository
{
    public Task<IReadOnlyList<FichaTecnicaMaterial>> ListarFichasPorProdutosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<FichaTecnicaMaterial>>(Array.Empty<FichaTecnicaMaterial>());
    public Task<IReadOnlyList<MateriaPrima>> ListarMateriasAsync(CancellationToken ct) => throw new NotSupportedException();
    public Task<MateriaPrima?> ObterMateriaAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<CompraMateriaPrima>> ListarComprasAsync(CancellationToken ct) => throw new NotSupportedException();
    public Task<CompraMateriaPrima?> ObterCompraAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    public Task<bool> NomeMateriaExisteAsync(string nome, Guid? excetoId, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<FichaTecnicaMaterial>> ListarFichasAsync(CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<FichaTecnicaMaterial>> ListarFichasPorProdutoAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<ReservaMateriaPrimaPedido>> ListarReservasPedidoAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<ReservaMateriaPrimaPedido>> ListarReservasAsync(CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<MovimentoMateriaPrima>> ListarMovimentosAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    public void RemoverFichas(IEnumerable<FichaTecnicaMaterial> fichas) => throw new NotSupportedException();
    public void Adicionar(FichaTecnicaMaterial ficha) => throw new NotSupportedException();
    public void Adicionar(ReservaMateriaPrimaPedido reserva) => throw new NotSupportedException();
    public void Adicionar(MovimentoMateriaPrima movimento) => throw new NotSupportedException();
    public void Adicionar(MateriaPrima materia) => throw new NotSupportedException();
    public void Adicionar(CompraMateriaPrima compra) => throw new NotSupportedException();
}

public sealed class ConsultasEstoqueFake : ICatalogoQueries, IPedidoQueries
{
    public Task<IReadOnlyList<ProdutoDto>> ListarProdutosAsync(CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<UsuarioDto>> ListarResponsaveisAsync(CancellationToken ct) => throw new NotSupportedException();
    public Task<ListaComContagemDto<PedidoListaItemDto>> ListarAsync(PedidoFiltro filtro, CancellationToken ct) => throw new NotSupportedException();
    public Task<PedidoDetalheDto> ObterDetalheAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<HistoricoEventoDto>> ObterHistoricoAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
}
