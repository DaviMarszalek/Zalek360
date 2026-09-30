using Microsoft.EntityFrameworkCore;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Application.Services;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Infrastructure.Queries;

internal sealed class CatalogoQueries : ICatalogoQueries
{
    private readonly ZalekDbContext _db;

    public CatalogoQueries(ZalekDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProdutoDto>> ListarProdutosAsync(CancellationToken ct) =>
        await _db.Produtos.AsNoTracking()
            .Where(p => p.Ativo)
            .OrderBy(p => p.Nome)
            .Select(p => new ProdutoDto(p.Id, p.Nome, p.Categoria, p.DescricaoBase))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<UsuarioDto>> ListarResponsaveisAsync(CancellationToken ct)
    {
        var usuarios = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Ativo && (u.Perfil == PerfilUsuario.Atendente || u.Perfil == PerfilUsuario.Gestor))
            .OrderBy(u => u.Nome)
            .ToListAsync(ct);
        return usuarios.Select(AuthService.Mapear).ToList();
    }
}
