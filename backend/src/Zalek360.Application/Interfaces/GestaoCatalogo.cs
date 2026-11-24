using Zalek360.Application.Dtos;
using Zalek360.Domain.Entities;

namespace Zalek360.Application.Interfaces;

public interface IGestaoCatalogoRepository
{
    Task<IReadOnlyList<ProdutoDto>> ListarAsync(string? busca, bool incluirInativos, CancellationToken ct, Guid? id = null);
    Task<Produto?> ObterAsync(Guid id, CancellationToken ct);
    Task<bool> UsadoAsync(Guid id, CancellationToken ct);
    Task<bool> NomeExisteAsync(string nome, Guid? excetoId, CancellationToken ct);
    void Remover(Produto produto);
    Task<IReadOnlyList<OpcaoCatalogo>> OpcoesAsync(bool somenteAtivas, CancellationToken ct);
    Task<OpcaoCatalogo?> OpcaoAsync(Guid id, CancellationToken ct);
    void Adicionar(OpcaoCatalogo opcao);
}
