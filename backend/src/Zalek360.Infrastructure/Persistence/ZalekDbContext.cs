using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;

namespace Zalek360.Infrastructure.Persistence;

public static class Sequencias
{
    public const string Orcamento = "seq_orcamento_numero";
    public const string Pedido = "seq_pedido_numero";
}

public sealed class ZalekDbContext : DbContext, IUnitOfWork
{
    public ZalekDbContext(DbContextOptions<ZalekDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ObservacaoCliente> ObservacoesCliente => Set<ObservacaoCliente>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<ItemOrcamento> ItensOrcamento => Set<ItemOrcamento>();
    public DbSet<ArquivoAnexo> ArquivosAnexos => Set<ArquivoAnexo>();
    public DbSet<ContatoOrcamento> ContatosOrcamento => Set<ContatoOrcamento>();
    public DbSet<DecisaoOrcamento> DecisoesOrcamento => Set<DecisaoOrcamento>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();
    public DbSet<HistoricoEvento> HistoricoEventos => Set<HistoricoEvento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZalekDbContext).Assembly);

        // Sequências só existem no PostgreSQL (os testes usam SQLite em memória com numerador próprio).
        if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            modelBuilder.HasSequence<int>(Sequencias.Orcamento).StartsAt(1).IncrementsBy(1);
            modelBuilder.HasSequence<int>(Sequencias.Pedido).StartsAt(1).IncrementsBy(1);
        }

        ConvencaoNomes.Aplicar(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ColetarHistoricoPendente();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ColetarHistoricoPendente();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Eventos de histórico produzidos pelos agregados são gravados na mesma operação (mesma transação).</summary>
    private void ColetarHistoricoPendente()
    {
        var agregados = ChangeTracker.Entries()
            .Select(e => e.Entity)
            .OfType<IPossuiHistorico>()
            .ToList();
        foreach (var agregado in agregados)
            foreach (var evento in agregado.ExtrairHistoricoPendente())
                HistoricoEventos.Add(evento);
    }

    public async Task<ITransacao> IniciarTransacaoAsync(CancellationToken ct)
    {
        if (Database.CurrentTransaction is not null) return new TransacaoAninhada();
        var transacao = await Database.BeginTransactionAsync(ct);
        return new TransacaoEf(transacao);
    }

    private sealed class TransacaoEf : ITransacao
    {
        private readonly IDbContextTransaction _transacao;
        public TransacaoEf(IDbContextTransaction transacao) => _transacao = transacao;
        public Task ConfirmarAsync(CancellationToken ct) => _transacao.CommitAsync(ct);
        /// <summary>Sem ConfirmarAsync, o descarte desfaz tudo (ROLLBACK).</summary>
        public ValueTask DisposeAsync() => _transacao.DisposeAsync();
    }

    private sealed class TransacaoAninhada : ITransacao
    {
        public Task ConfirmarAsync(CancellationToken ct) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
