using Microsoft.EntityFrameworkCore;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Infrastructure.Repositories;

internal sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly ZalekDbContext _db;
    public UsuarioRepository(ZalekDbContext db) => _db = db;

    public Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct) =>
        _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == emailNormalizado, ct);

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
}

internal sealed class ClienteRepository : IClienteRepository
{
    private readonly ZalekDbContext _db;
    public ClienteRepository(ZalekDbContext db) => _db = db;

    public Task<Cliente?> ObterParaAlteracaoAsync(Guid id, CancellationToken ct) =>
        _db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExisteAsync(Guid id, CancellationToken ct) => _db.Clientes.AnyAsync(c => c.Id == id, ct);

    public void Adicionar(Cliente cliente) => _db.Clientes.Add(cliente);

    public void AdicionarObservacao(ObservacaoCliente observacao) => _db.ObservacoesCliente.Add(observacao);
}

internal sealed class ProdutoRepository : IProdutoRepository
{
    private readonly ZalekDbContext _db;
    public ProdutoRepository(ZalekDbContext db) => _db = db;

    public async Task<IReadOnlyList<Produto>> ObterPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        ids.Count == 0 ? Array.Empty<Produto>() : await _db.Produtos.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync(ct);
}

internal sealed class OrcamentoRepository : IOrcamentoRepository
{
    private readonly ZalekDbContext _db;
    public OrcamentoRepository(ZalekDbContext db) => _db = db;

    public Task<Orcamento?> ObterParaAlteracaoAsync(Guid id, CancellationToken ct) =>
        _db.Orcamentos
            .Include(o => o.Itens).ThenInclude(i => i.Anexos)
            .Include(o => o.Decisao)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Orcamento>> ListarParaExpiracaoAsync(DateOnly hojeLocal, CancellationToken ct) =>
        await _db.Orcamentos
            .Include(o => o.Decisao)
            .Where(o => o.Situacao == SituacaoOrcamento.AguardandoRetorno && o.Validade < hojeLocal)
            .ToListAsync(ct);

    public void Adicionar(Orcamento orcamento) => _db.Orcamentos.Add(orcamento);
}

internal sealed class PedidoRepository : IPedidoRepository
{
    private readonly ZalekDbContext _db;
    public PedidoRepository(ZalekDbContext db) => _db = db;

    public Task<Pedido?> ObterParaAlteracaoAsync(Guid id, CancellationToken ct) =>
        _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Adicionar(Pedido pedido) => _db.Pedidos.Add(pedido);
}

internal sealed class AnexoRepository : IAnexoRepository
{
    private readonly ZalekDbContext _db;
    public AnexoRepository(ZalekDbContext db) => _db = db;

    public Task<ArquivoAnexo?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        _db.ArquivosAnexos.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<ArquivoAnexo>> ObterPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        ids.Count == 0 ? Array.Empty<ArquivoAnexo>() : await _db.ArquivosAnexos.Where(a => ids.Contains(a.Id)).ToListAsync(ct);

    public async Task<IReadOnlyList<ArquivoAnexo>> ListarPendentesCriadosAntesDeAsync(DateTime limiteUtc, CancellationToken ct) =>
        await _db.ArquivosAnexos
            .Where(a => a.ItemOrcamentoId == null && a.ItemPedidoId == null && a.CriadoEm < limiteUtc)
            .Take(500)
            .ToListAsync(ct);

    public Task<bool> ChaveUsadaPorOutroAnexoAsync(string chave, Guid excetoId, CancellationToken ct) =>
        _db.ArquivosAnexos.AnyAsync(a => a.ChaveArmazenamento == chave && a.Id != excetoId, ct);

    public void Adicionar(ArquivoAnexo anexo) => _db.ArquivosAnexos.Add(anexo);

    public void Remover(ArquivoAnexo anexo) => _db.ArquivosAnexos.Remove(anexo);
}
