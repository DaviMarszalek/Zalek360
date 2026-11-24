using Zalek360.Application.Dtos;

namespace Zalek360.Application.Interfaces;

public interface IAnaliseOrcamentoQueries
{
    Task<OrcamentoAnaliseRegistrado?> ObterAsync(Guid id, CancellationToken ct);
}
