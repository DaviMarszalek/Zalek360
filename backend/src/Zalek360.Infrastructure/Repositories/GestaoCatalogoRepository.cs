using Microsoft.EntityFrameworkCore;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Entities;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Infrastructure.Repositories;

internal sealed class GestaoCatalogoRepository(ZalekDbContext db) : IGestaoCatalogoRepository
{
    public async Task<IReadOnlyList<ProdutoDto>> ListarAsync(string? busca, bool incluirInativos, CancellationToken ct, Guid? id = null)
    {
        var q = db.Produtos.AsNoTracking().Where(p => incluirInativos || p.Ativo);
        if (id.HasValue) q = q.Where(p => p.Id == id.Value);
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            q = q.Where(p => p.Nome.ToLower().Contains(termo) || p.Categoria.ToLower().Contains(termo));
        }
        var produtos = await q.OrderBy(p => p.Nome).ToListAsync(ct);
        var ids = produtos.Select(p => p.Id).ToList();
        var usados = (await db.ItensOrcamento.Where(i => ids.Contains(i.ProdutoId)).Select(i => i.ProdutoId).Distinct().ToListAsync(ct))
            .Concat(await db.ItensPedido.Where(i => ids.Contains(i.ProdutoId)).Select(i => i.ProdutoId).Distinct().ToListAsync(ct)).ToHashSet();
        var fichas = await (from f in db.FichasTecnicasMateriasPrimas.AsNoTracking()
                            join m in db.MateriasPrimas on f.MateriaPrimaId equals m.Id
                            where ids.Contains(f.ProdutoId)
                            select new { f.ProdutoId, Linha = new FichaTecnicaLinhaDto(m.Id, m.Nome, m.Unidade, f.QuantidadePorUnidade) }).ToListAsync(ct);
        var porProduto = fichas.ToLookup(f => f.ProdutoId, f => f.Linha);
        return produtos.Select(p => new ProdutoDto(p.Id, p.Nome, p.Categoria, p.DescricaoBase, p.PrecoVenda, p.Custo,
            p.Ativo, usados.Contains(p.Id), p.Versao, porProduto[p.Id].ToList())).ToList();
    }
    public Task<Produto?> ObterAsync(Guid id, CancellationToken ct) => db.Produtos.FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task<bool> UsadoAsync(Guid id, CancellationToken ct) =>
        await db.ItensOrcamento.AnyAsync(x => x.ProdutoId == id, ct) || await db.ItensPedido.AnyAsync(x => x.ProdutoId == id, ct);
    public async Task<bool> NomeExisteAsync(string nome, Guid? excetoId, CancellationToken ct)
    {
        // Serializa cadastros/renomeações concorrentes do mesmo nome sem reescrever dados legados.
        var normalizado = nome.Trim().ToLowerInvariant();
        if (db.Database.IsNpgsql() && db.Database.CurrentTransaction is not null)
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({normalizado}, 0))", ct);
        return await db.Produtos.AnyAsync(x => x.Id != excetoId && x.Nome.ToLower() == normalizado, ct);
    }
    public void Remover(Produto p) => db.Produtos.Remove(p);
    public async Task<IReadOnlyList<OpcaoCatalogo>> OpcoesAsync(bool somenteAtivas, CancellationToken ct) =>
        await db.OpcoesCatalogo.AsNoTracking().Where(x => !somenteAtivas || x.Ativo).OrderBy(x => x.Tipo).ThenBy(x => x.Ordem).ThenBy(x => x.Nome).ToListAsync(ct);
    public Task<OpcaoCatalogo?> OpcaoAsync(Guid id, CancellationToken ct) => db.OpcoesCatalogo.FirstOrDefaultAsync(x => x.Id == id, ct);
    public void Adicionar(OpcaoCatalogo x) => db.OpcoesCatalogo.Add(x);
}
