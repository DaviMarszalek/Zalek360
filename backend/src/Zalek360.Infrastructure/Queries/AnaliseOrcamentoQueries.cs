using Microsoft.EntityFrameworkCore;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Infrastructure.Queries;

internal sealed class AnaliseOrcamentoQueries(ZalekDbContext db) : IAnaliseOrcamentoQueries
{
    public async Task<OrcamentoAnaliseRegistrado?> ObterAsync(Guid id, CancellationToken ct)
    {
        var o = await db.Orcamentos.AsNoTracking().Include(x => x.Itens).Include(x => x.PedidoGerado).AsSplitQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
        return o is null ? null : new(o.Desconto, o.PedidoGerado?.Id, o.Itens.OrderBy(x => x.Ordem)
            .Select(x => new ItemAnaliseRegistrado(x.Id, x.ProdutoId, x.Quantidade, x.ValorUnitario, x.ValorPersonalizacaoUnitario, x.CustoUnitarioInterno)).ToList());
    }
}
