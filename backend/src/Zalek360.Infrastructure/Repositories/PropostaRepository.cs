using Microsoft.EntityFrameworkCore;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Entities;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Infrastructure.Repositories;

internal sealed class PropostaRepository(ZalekDbContext db) : IPropostaRepository
{
    public async Task<IReadOnlyList<ModeloMensagemProposta>> ModelosAsync(CancellationToken ct) => await db.ModelosMensagemProposta.OrderBy(x => x.Canal).ToListAsync(ct);
    public Task<ModeloMensagemProposta?> ModeloAsync(Guid id, CancellationToken ct) => db.ModelosMensagemProposta.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<EnvioProposta?> EnvioAsync(Guid chave, CancellationToken ct) => db.EnviosProposta.FirstOrDefaultAsync(x => x.Id == chave, ct);
    public Task<EnvioProposta?> UltimoEnvioAsync(Guid id, CancellationToken ct) => db.EnviosProposta.AsNoTracking().Where(x => x.OrcamentoId == id).OrderByDescending(x => x.CriadoEm).FirstOrDefaultAsync(ct);
    public void Adicionar(EnvioProposta envio) => db.EnviosProposta.Add(envio);
    public async Task BloquearOrcamentoAsync(Guid id, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("O bloqueio da proposta exige uma transação.");
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM orcamentos WHERE id = {id} FOR UPDATE", ct);
    }
}
