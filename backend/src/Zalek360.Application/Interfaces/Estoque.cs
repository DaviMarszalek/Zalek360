using Zalek360.Domain.Entities;

namespace Zalek360.Application.Interfaces;

public interface IEstoqueRepository
{
    Task<IReadOnlyList<MateriaPrima>> ListarMateriasAsync(CancellationToken ct);
    Task<MateriaPrima?> ObterMateriaAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<CompraMateriaPrima>> ListarComprasAsync(CancellationToken ct);
    Task<CompraMateriaPrima?> ObterCompraAsync(Guid id, CancellationToken ct);
    Task<bool> NomeMateriaExisteAsync(string nome, Guid? excetoId, CancellationToken ct);
    Task<IReadOnlyList<FichaTecnicaMaterial>> ListarFichasAsync(CancellationToken ct);
    Task<IReadOnlyList<FichaTecnicaMaterial>> ListarFichasPorProdutosAsync(IReadOnlyCollection<Guid> produtoIds, CancellationToken ct);
    Task<IReadOnlyList<FichaTecnicaMaterial>> ListarFichasPorProdutoAsync(Guid produtoId, CancellationToken ct);
    void RemoverFichas(IEnumerable<FichaTecnicaMaterial> fichas);
    void Adicionar(FichaTecnicaMaterial ficha);
    Task<IReadOnlyList<ReservaMateriaPrimaPedido>> ListarReservasPedidoAsync(Guid pedidoId, CancellationToken ct);
    Task<IReadOnlyList<ReservaMateriaPrimaPedido>> ListarReservasAsync(CancellationToken ct);
    Task<IReadOnlyList<MovimentoMateriaPrima>> ListarMovimentosAsync(Guid materiaPrimaId, CancellationToken ct);
    void Adicionar(ReservaMateriaPrimaPedido reserva);
    void Adicionar(MovimentoMateriaPrima movimento);
    void Adicionar(MateriaPrima materia);
    void Adicionar(CompraMateriaPrima compra);
}
