// STUBS DE ASSINATURA — somente para compilação offline (sem NuGet). Reproduzem a API pública do EF Core 8.
#nullable enable
using System.Collections;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore
{
    public enum DeleteBehavior { ClientSetNull, Restrict, SetNull, Cascade, ClientCascade, NoAction, ClientNoAction }
    public enum PropertyAccessMode { Field, FieldDuringConstruction, Property, PreferField, PreferFieldDuringConstruction, PreferProperty }

    public abstract class DbContextOptions { }
    public class DbContextOptions<TContext> : DbContextOptions where TContext : DbContext { }
    public class DbContextOptionsBuilder { public virtual DbContextOptions Options => throw null!; }
    public class DbContextOptionsBuilder<TContext> : DbContextOptionsBuilder where TContext : DbContext
    {
        public DbContextOptionsBuilder() { }
        public DbContextOptionsBuilder(DbContextOptions<TContext> options) { }
        public new virtual DbContextOptions<TContext> Options => throw null!;
    }

    public class DbContext : IDisposable, IAsyncDisposable
    {
        protected DbContext() { }
        public DbContext(DbContextOptions options) { }
        public virtual DatabaseFacade Database => throw null!;
        public virtual ChangeTracker ChangeTracker => throw null!;
        public virtual IModel Model => throw null!;
        public virtual DbSet<TEntity> Set<TEntity>() where TEntity : class => throw null!;
        protected internal virtual void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
        protected internal virtual void OnModelCreating(ModelBuilder modelBuilder) { }
        public virtual int SaveChanges() => 0;
        public virtual int SaveChanges(bool acceptAllChangesOnSuccess) => 0;
        public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public virtual Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public virtual EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class => throw null!;
        public virtual EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class => throw null!;
        public virtual EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class => throw null!;
        public virtual EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class => throw null!;
        public virtual void Dispose() { }
        public virtual ValueTask DisposeAsync() => default;
    }

    public abstract class DbSet<TEntity> : IQueryable<TEntity>, IAsyncEnumerable<TEntity>, IListSource where TEntity : class
    {
        public virtual EntityEntry<TEntity> Add(TEntity entity) => throw null!;
        public virtual ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default) => throw null!;
        public virtual void AddRange(params TEntity[] entities) { }
        public virtual void AddRange(IEnumerable<TEntity> entities) { }
        public virtual EntityEntry<TEntity> Remove(TEntity entity) => throw null!;
        public virtual void RemoveRange(params TEntity[] entities) { }
        public virtual void RemoveRange(IEnumerable<TEntity> entities) { }
        public virtual EntityEntry<TEntity> Update(TEntity entity) => throw null!;
        public virtual EntityEntry<TEntity> Attach(TEntity entity) => throw null!;
        public virtual TEntity? Find(params object?[]? keyValues) => throw null!;
        public virtual ValueTask<TEntity?> FindAsync(params object?[]? keyValues) => throw null!;
        public virtual ValueTask<TEntity?> FindAsync(object?[]? keyValues, CancellationToken cancellationToken) => throw null!;
        public virtual LocalView<TEntity> Local => throw null!;
        Type IQueryable.ElementType => throw null!;
        Expression IQueryable.Expression => throw null!;
        IQueryProvider IQueryable.Provider => throw null!;
        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator() => throw null!;
        IEnumerator IEnumerable.GetEnumerator() => throw null!;
        IAsyncEnumerator<TEntity> IAsyncEnumerable<TEntity>.GetAsyncEnumerator(CancellationToken cancellationToken) => throw null!;
        bool IListSource.ContainsListCollection => false;
        IList IListSource.GetList() => throw null!;
    }

    public class DbUpdateException : Exception
    {
        public DbUpdateException() { }
        public DbUpdateException(string message, Exception? innerException) : base(message, innerException) { }
    }
    public class DbUpdateConcurrencyException : DbUpdateException { }

    public interface IEntityTypeConfiguration<TEntity> where TEntity : class { void Configure(EntityTypeBuilder<TEntity> builder); }

    public class ModelBuilder
    {
        public virtual IMutableModel Model => throw null!;
        public virtual ModelBuilder ApplyConfigurationsFromAssembly(Assembly assembly, Func<Type, bool>? predicate = null) => this;
        public virtual ModelBuilder ApplyConfiguration<TEntity>(IEntityTypeConfiguration<TEntity> configuration) where TEntity : class => this;
        public virtual EntityTypeBuilder<TEntity> Entity<TEntity>() where TEntity : class => throw null!;
        public virtual ModelBuilder HasDefaultSchema(string? schema) => this;
    }

    public static class RelationalModelBuilderExtensions
    {
        public static SequenceBuilder HasSequence<T>(this ModelBuilder modelBuilder, string name, string? schema = null) => throw null!;
        public static SequenceBuilder HasSequence(this ModelBuilder modelBuilder, string name, string? schema = null) => throw null!;
    }

    public static class RelationalEntityTypeBuilderExtensions
    {
        public static EntityTypeBuilder<TEntity> ToTable<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, string? name) where TEntity : class => entityTypeBuilder;
        public static EntityTypeBuilder<TEntity> ToTable<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, Action<TableBuilder<TEntity>> buildAction) where TEntity : class => entityTypeBuilder;
        public static EntityTypeBuilder<TEntity> ToTable<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, string name, Action<TableBuilder<TEntity>> buildAction) where TEntity : class => entityTypeBuilder;
        public static EntityTypeBuilder<TEntity> ToTable<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, string name, string? schema) where TEntity : class => entityTypeBuilder;
    }

    public static class RelationalPropertyBuilderExtensions
    {
        public static PropertyBuilder<TProperty> HasColumnName<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, string? name) => propertyBuilder;
        public static PropertyBuilder<TProperty> HasColumnType<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, string? typeName) => propertyBuilder;
        public static PropertyBuilder<TProperty> HasDefaultValueSql<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, string? sql) => propertyBuilder;
        public static PropertyBuilder<TProperty> HasDefaultValue<TProperty>(this PropertyBuilder<TProperty> propertyBuilder, object? value) => propertyBuilder;
    }

    public static class RelationalIndexBuilderExtensions
    {
        public static IndexBuilder<TEntity> HasDatabaseName<TEntity>(this IndexBuilder<TEntity> indexBuilder, string? name) => indexBuilder;
        public static IndexBuilder<TEntity> HasFilter<TEntity>(this IndexBuilder<TEntity> indexBuilder, string? sql) => indexBuilder;
    }

    public static class RelationalKeyBuilderExtensions
    {
        public static KeyBuilder<TEntity> HasName<TEntity>(this KeyBuilder<TEntity> keyBuilder, string? name) => keyBuilder;
    }

    public static class RelationalForeignKeyBuilderExtensions
    {
        public static ReferenceCollectionBuilder<TP, TD> HasConstraintName<TP, TD>(this ReferenceCollectionBuilder<TP, TD> b, string? name) where TP : class where TD : class => b;
        public static ReferenceReferenceBuilder<TE, TR> HasConstraintName<TE, TR>(this ReferenceReferenceBuilder<TE, TR> b, string? name) where TE : class where TR : class => b;
    }

    // Metadados relacionais (extensões sobre as interfaces somente-leitura/mutáveis)
    public static class RelationalEntityTypeExtensions
    {
        public static string? GetTableName(this IReadOnlyEntityType entityType) => throw null!;
        public static string? GetSchema(this IReadOnlyEntityType entityType) => throw null!;
    }
    public static class RelationalPropertyExtensions
    {
        public static string GetColumnName(this IReadOnlyProperty property) => throw null!;
        public static string? GetColumnName(this IReadOnlyProperty property, in StoreObjectIdentifier storeObject) => throw null!;
        public static void SetColumnName(this IMutableProperty property, string? name) { }
    }
    public static class RelationalKeyExtensions { public static void SetName(this IMutableKey key, string? name) { } }
    public static class RelationalForeignKeyExtensions { public static void SetConstraintName(this IMutableForeignKey foreignKey, string? value) { } }
    public static class RelationalIndexExtensions { public static void SetDatabaseName(this IMutableIndex index, string? name) { } }

    public static class RelationalDatabaseFacadeExtensions
    {
        public static void Migrate(this DatabaseFacade databaseFacade) { }
        public static Task MigrateAsync(this DatabaseFacade databaseFacade, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public static Task<IEnumerable<string>> GetPendingMigrationsAsync(this DatabaseFacade databaseFacade, CancellationToken cancellationToken = default) => throw null!;
        public static Task<IEnumerable<string>> GetAppliedMigrationsAsync(this DatabaseFacade databaseFacade, CancellationToken cancellationToken = default) => throw null!;
        public static Task<int> ExecuteSqlRawAsync(this DatabaseFacade databaseFacade, string sql, CancellationToken cancellationToken = default) => throw null!;
        public static Task<int> ExecuteSqlRawAsync(this DatabaseFacade databaseFacade, string sql, params object[] parameters) => throw null!;
        public static Task<int> ExecuteSqlRawAsync(this DatabaseFacade databaseFacade, string sql, IEnumerable<object> parameters, CancellationToken cancellationToken = default) => throw null!;
        public static IQueryable<TResult> SqlQueryRaw<TResult>(this DatabaseFacade databaseFacade, string sql, params object[] parameters) => throw null!;
        public static bool IsRelational(this DatabaseFacade databaseFacade) => true;
    }

    public static class NpgsqlDatabaseFacadeExtensions { public static bool IsNpgsql(this DatabaseFacade database) => true; }

    public static class NpgsqlDbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder UseNpgsql(this DbContextOptionsBuilder optionsBuilder, string? connectionString,
            Action<Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.NpgsqlDbContextOptionsBuilder>? npgsqlOptionsAction = null) => optionsBuilder;
    }

    public static class RelationalQueryableExtensions
    {
        public static IQueryable<TEntity> AsSplitQuery<TEntity>(this IQueryable<TEntity> source) where TEntity : class => source;
        public static IQueryable<TEntity> AsSingleQuery<TEntity>(this IQueryable<TEntity> source) where TEntity : class => source;
    }

    public static class EntityFrameworkQueryableExtensions
    {
        public static IQueryable<TEntity> AsNoTracking<TEntity>(this IQueryable<TEntity> source) where TEntity : class => source;
        public static IQueryable<TEntity> AsTracking<TEntity>(this IQueryable<TEntity> source) where TEntity : class => source;
        public static IIncludableQueryable<TEntity, TProperty> Include<TEntity, TProperty>(this IQueryable<TEntity> source, Expression<Func<TEntity, TProperty>> navigationPropertyPath) where TEntity : class => throw null!;
        public static IQueryable<TEntity> Include<TEntity>(this IQueryable<TEntity> source, string navigationPropertyPath) where TEntity : class => source;
        public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(this IIncludableQueryable<TEntity, IEnumerable<TPreviousProperty>> source, Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class => throw null!;
        public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(this IIncludableQueryable<TEntity, TPreviousProperty> source, Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class => throw null!;

        public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => throw null!;
        public static Task<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => throw null!;
        public static Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken cancellationToken = default) where TKey : notnull => throw null!;
        public static Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, CancellationToken cancellationToken = default) where TKey : notnull => throw null!;
        public static Task<HashSet<TSource>> ToHashSetAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => throw null!;

        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => throw null!;
        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => throw null!;
        public static Task<TSource?> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => throw null!;
        public static Task<TSource?> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => throw null!;
        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => throw null!;
        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => throw null!;
        public static Task<TSource?> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => throw null!;
        public static Task<TSource?> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => throw null!;
        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => throw null!;
        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => throw null!;
        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => throw null!;
        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) => throw null!;
        public static Task<TSource> MaxAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) => throw null!;
        public static Task<TResult> MaxAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default) => throw null!;
        public static Task<decimal> SumAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default) => throw null!;
        public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default) => throw null!;
        public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default) => throw null!;
        public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default) => throw null!;
    }
}

namespace Microsoft.EntityFrameworkCore.Query
{
    public interface IIncludableQueryable<out TEntity, out TProperty> : IQueryable<TEntity> { }
}

namespace Microsoft.EntityFrameworkCore.Storage
{
    public interface IDbContextTransaction : IDisposable, IAsyncDisposable
    {
        Guid TransactionId { get; }
        void Commit();
        Task CommitAsync(CancellationToken cancellationToken = default);
        void Rollback();
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    public class DatabaseFacade
    {
        public virtual string? ProviderName => throw null!;
        public virtual IDbContextTransaction? CurrentTransaction => throw null!;
        public virtual IDbContextTransaction BeginTransaction() => throw null!;
        public virtual Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw null!;
        public virtual bool CanConnect() => true;
        public virtual Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) => throw null!;
        public virtual bool EnsureCreated() => true;
        public virtual Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default) => throw null!;
        public virtual Task<bool> EnsureDeletedAsync(CancellationToken cancellationToken = default) => throw null!;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DbContextAttribute : Attribute { public DbContextAttribute(Type contextType) { } public Type ContextType => throw null!; }

    public abstract class ModelSnapshot { protected abstract void BuildModel(ModelBuilder modelBuilder); }
}

namespace Microsoft.EntityFrameworkCore.ChangeTracking
{
    public class ChangeTracker
    {
        public virtual IEnumerable<EntityEntry> Entries() => throw null!;
        public virtual IEnumerable<EntityEntry<TEntity>> Entries<TEntity>() where TEntity : class => throw null!;
        public virtual void Clear() { }
        public virtual bool HasChanges() => false;
    }
    public class EntityEntry { public virtual object Entity => throw null!; public EntityState State { get; set; } }
    public class EntityEntry<TEntity> : EntityEntry where TEntity : class { public new virtual TEntity Entity => throw null!; }
    public class LocalView<TEntity> : IEnumerable<TEntity> where TEntity : class
    {
        public IEnumerator<TEntity> GetEnumerator() => throw null!;
        IEnumerator IEnumerable.GetEnumerator() => throw null!;
    }
}

namespace Microsoft.EntityFrameworkCore
{
    public enum EntityState { Detached, Unchanged, Deleted, Modified, Added }
}

namespace Microsoft.EntityFrameworkCore.Metadata
{
    public enum ValueGenerated { Never, OnAdd, OnUpdate, OnUpdateSometimes, OnAddOrUpdate }

    public static class RelationalAnnotationNames
    {
        public const string Prefix = "Relational:";
        public const string ColumnName = Prefix + "ColumnName";
        public const string TableName = Prefix + "TableName";
    }

    public readonly struct StoreObjectIdentifier
    {
        public static StoreObjectIdentifier Table(string name, string? schema = null) => default;
        public string Name => throw null!;
    }

    public interface IAnnotation { string Name { get; } object? Value { get; } }
    public interface IReadOnlyAnnotatable { IAnnotation? FindAnnotation(string name); }
    public interface IMutableAnnotatable : IReadOnlyAnnotatable { }

    public interface IReadOnlyProperty : IReadOnlyAnnotatable { string Name { get; } Type ClrType { get; } bool IsNullable { get; } ValueGenerated ValueGenerated { get; } }
    public interface IProperty : IReadOnlyProperty { }
    public interface IMutableProperty : IReadOnlyProperty, IMutableAnnotatable { new ValueGenerated ValueGenerated { get; set; } new bool IsNullable { get; set; } }

    public interface IReadOnlyKey : IReadOnlyAnnotatable { IReadOnlyList<IReadOnlyProperty> Properties { get; } }
    public interface IMutableKey : IReadOnlyKey, IMutableAnnotatable { new IReadOnlyList<IMutableProperty> Properties { get; } }

    public interface IReadOnlyForeignKey : IReadOnlyAnnotatable { IReadOnlyList<IReadOnlyProperty> Properties { get; } DeleteBehavior DeleteBehavior { get; } }
    public interface IMutableForeignKey : IReadOnlyForeignKey, IMutableAnnotatable { new IReadOnlyList<IMutableProperty> Properties { get; } }

    public interface IReadOnlyIndex : IReadOnlyAnnotatable { IReadOnlyList<IReadOnlyProperty> Properties { get; } bool IsUnique { get; } }
    public interface IMutableIndex : IReadOnlyIndex, IMutableAnnotatable { new IReadOnlyList<IMutableProperty> Properties { get; } new bool IsUnique { get; set; } }

    public interface IReadOnlyEntityType : IReadOnlyAnnotatable { string Name { get; } Type ClrType { get; } }
    public interface IEntityType : IReadOnlyEntityType
    {
        IEnumerable<IProperty> GetProperties();
    }
    public interface IMutableEntityType : IReadOnlyEntityType, IMutableAnnotatable
    {
        IEnumerable<IMutableProperty> GetProperties();
        IMutableKey? FindPrimaryKey();
        IEnumerable<IMutableForeignKey> GetForeignKeys();
        IEnumerable<IMutableIndex> GetIndexes();
    }

    public interface IReadOnlyModel : IReadOnlyAnnotatable { }
    public interface IModel : IReadOnlyModel { IEnumerable<IEntityType> GetEntityTypes(); IEntityType? FindEntityType(Type type); }
    public interface IMutableModel : IReadOnlyModel, IMutableAnnotatable { IEnumerable<IMutableEntityType> GetEntityTypes(); }
}

namespace Microsoft.EntityFrameworkCore.Metadata.Builders
{
    public class EntityTypeBuilder<TEntity> where TEntity : class
    {
        public virtual KeyBuilder<TEntity> HasKey(Expression<Func<TEntity, object?>> keyExpression) => throw null!;
        public virtual KeyBuilder<TEntity> HasKey(params string[] propertyNames) => throw null!;
        public virtual PropertyBuilder<TProperty> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression) => throw null!;
        public virtual PropertyBuilder<TProperty> Property<TProperty>(string propertyName) => throw null!;
        public virtual IndexBuilder<TEntity> HasIndex(Expression<Func<TEntity, object?>> indexExpression) => throw null!;
        public virtual IndexBuilder<TEntity> HasIndex(Expression<Func<TEntity, object?>> indexExpression, string name) => throw null!;
        public virtual EntityTypeBuilder<TEntity> Ignore(Expression<Func<TEntity, object?>> propertyExpression) => this;
        public virtual EntityTypeBuilder<TEntity> Ignore(string propertyName) => this;
        public virtual ReferenceNavigationBuilder<TEntity, TRelatedEntity> HasOne<TRelatedEntity>(Expression<Func<TEntity, TRelatedEntity?>>? navigationExpression = null) where TRelatedEntity : class => throw null!;
        public virtual ReferenceNavigationBuilder<TEntity, TRelatedEntity> HasOne<TRelatedEntity>(string? navigationName) where TRelatedEntity : class => throw null!;
        public virtual CollectionNavigationBuilder<TEntity, TRelatedEntity> HasMany<TRelatedEntity>(Expression<Func<TEntity, IEnumerable<TRelatedEntity>?>>? navigationExpression = null) where TRelatedEntity : class => throw null!;
        public virtual NavigationBuilder<TEntity, TNavigation> Navigation<TNavigation>(Expression<Func<TEntity, TNavigation?>> navigationExpression) where TNavigation : class => throw null!;
        public virtual NavigationBuilder<TEntity, TNavigation> Navigation<TNavigation>(Expression<Func<TEntity, IEnumerable<TNavigation>?>> navigationExpression) where TNavigation : class => throw null!;
        public virtual EntityTypeBuilder<TEntity> HasData(params TEntity[] data) => this;
    }

    public class TableBuilder<TEntity> where TEntity : class
    {
        public virtual CheckConstraintBuilder HasCheckConstraint(string name, string? sql) => throw null!;
        public virtual TableBuilder<TEntity> HasComment(string? comment) => this;
    }
    public class CheckConstraintBuilder { public virtual CheckConstraintBuilder HasName(string name) => this; }
    public class SequenceBuilder
    {
        public virtual SequenceBuilder StartsAt(long startValue) => this;
        public virtual SequenceBuilder IncrementsBy(int increment) => this;
        public virtual SequenceBuilder HasMin(long minimum) => this;
        public virtual SequenceBuilder HasMax(long maximum) => this;
        public virtual SequenceBuilder IsCyclic(bool cyclic = true) => this;
    }

    public class KeyBuilder<TEntity> { }

    public class PropertyBuilder<TProperty>
    {
        public virtual PropertyBuilder<TProperty> HasMaxLength(int maxLength) => this;
        public virtual PropertyBuilder<TProperty> IsRequired(bool required = true) => this;
        public virtual PropertyBuilder<TProperty> HasPrecision(int precision, int scale) => this;
        public virtual PropertyBuilder<TProperty> HasPrecision(int precision) => this;
        public virtual PropertyBuilder<TProperty> HasConversion<TConversion>() => this;
        public virtual PropertyBuilder<TProperty> HasConversion<TProvider>(Expression<Func<TProperty, TProvider>> convertToProviderExpression, Expression<Func<TProvider, TProperty>> convertFromProviderExpression) => this;
        public virtual PropertyBuilder<TProperty> IsConcurrencyToken(bool concurrencyToken = true) => this;
        public virtual PropertyBuilder<TProperty> ValueGeneratedNever() => this;
        public virtual PropertyBuilder<TProperty> IsUnicode(bool unicode = true) => this;
        public virtual PropertyBuilder<TProperty> HasField(string fieldName) => this;
        public virtual PropertyBuilder<TProperty> UsePropertyAccessMode(PropertyAccessMode propertyAccessMode) => this;
    }

    public class IndexBuilder<T> { public virtual IndexBuilder<T> IsUnique(bool unique = true) => this; }

    public class NavigationBuilder<TSource, TTarget> where TSource : class where TTarget : class
    {
        public virtual NavigationBuilder<TSource, TTarget> UsePropertyAccessMode(PropertyAccessMode propertyAccessMode) => this;
        public virtual NavigationBuilder<TSource, TTarget> HasField(string? fieldName) => this;
        public virtual NavigationBuilder<TSource, TTarget> AutoInclude(bool autoInclude = true) => this;
    }

    public class ReferenceNavigationBuilder<TEntity, TRelatedEntity> where TEntity : class where TRelatedEntity : class
    {
        public virtual ReferenceCollectionBuilder<TRelatedEntity, TEntity> WithMany(Expression<Func<TRelatedEntity, IEnumerable<TEntity>?>>? navigationExpression = null) => throw null!;
        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> WithOne(Expression<Func<TRelatedEntity, TEntity?>>? navigationExpression = null) => throw null!;
    }

    public class CollectionNavigationBuilder<TEntity, TRelatedEntity> where TEntity : class where TRelatedEntity : class
    {
        public virtual ReferenceCollectionBuilder<TEntity, TRelatedEntity> WithOne(Expression<Func<TRelatedEntity, TEntity?>>? navigationExpression = null) => throw null!;
    }

    public class ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> where TPrincipalEntity : class where TDependentEntity : class
    {
        public virtual ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> HasForeignKey(Expression<Func<TDependentEntity, object?>> foreignKeyExpression) => this;
        public virtual ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> HasForeignKey(params string[] foreignKeyPropertyNames) => this;
        public virtual ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> HasPrincipalKey(Expression<Func<TPrincipalEntity, object?>> keyExpression) => this;
        public virtual ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> OnDelete(DeleteBehavior deleteBehavior) => this;
        public virtual ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> IsRequired(bool required = true) => this;
    }

    public class ReferenceReferenceBuilder<TEntity, TRelatedEntity> where TEntity : class where TRelatedEntity : class
    {
        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasForeignKey<TDependentEntity>(Expression<Func<TDependentEntity, object?>> foreignKeyExpression) where TDependentEntity : class => this;
        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> HasPrincipalKey<TPrincipalEntity>(Expression<Func<TPrincipalEntity, object?>> keyExpression) where TPrincipalEntity : class => this;
        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> OnDelete(DeleteBehavior deleteBehavior) => this;
        public virtual ReferenceReferenceBuilder<TEntity, TRelatedEntity> IsRequired(bool required = true) => this;
    }
}

namespace Microsoft.EntityFrameworkCore.Migrations
{
    public enum ReferentialAction { NoAction, Restrict, Cascade, SetNull, SetDefault }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MigrationAttribute : Attribute { public MigrationAttribute(string id) { } public string Id => throw null!; }

    public abstract class Migration
    {
        protected abstract void Up(MigrationBuilder migrationBuilder);
        protected virtual void Down(MigrationBuilder migrationBuilder) { }
        protected virtual void BuildTargetModel(ModelBuilder modelBuilder) { }
    }

    public class MigrationBuilder
    {
        public MigrationBuilder(string? activeProvider) { }
        public virtual string? ActiveProvider => throw null!;
        public virtual Operations.Builders.OperationBuilder<Operations.CreateSequenceOperation> CreateSequence<T>(string name, string? schema = null, long startValue = 1L, int incrementBy = 1, long? minValue = null, long? maxValue = null, bool cyclic = false) => throw null!;
        public virtual Operations.Builders.OperationBuilder<Operations.CreateSequenceOperation> CreateSequence(string name, string? schema = null, long startValue = 1L, int incrementBy = 1, long? minValue = null, long? maxValue = null, bool cyclic = false) => throw null!;
        public virtual Operations.Builders.CreateTableBuilder<TColumns> CreateTable<TColumns>(string name, Func<Operations.Builders.ColumnsBuilder, TColumns> columns, string? schema = null, Action<Operations.Builders.CreateTableBuilder<TColumns>>? constraints = null, string? comment = null) => throw null!;
        public virtual Operations.Builders.OperationBuilder<Operations.CreateIndexOperation> CreateIndex(string name, string table, string column, string? schema = null, bool unique = false, string? filter = null, bool descending = false) => throw null!;
        public virtual Operations.Builders.OperationBuilder<Operations.CreateIndexOperation> CreateIndex(string name, string table, string[] columns, string? schema = null, bool unique = false, string? filter = null, bool[]? descending = null) => throw null!;
        public virtual Operations.Builders.OperationBuilder<Operations.DropTableOperation> DropTable(string name, string? schema = null) => throw null!;
        public virtual Operations.Builders.OperationBuilder<Operations.DropSequenceOperation> DropSequence(string name, string? schema = null) => throw null!;
        public virtual Operations.Builders.OperationBuilder<Operations.SqlOperation> Sql(string sql, bool suppressTransaction = false) => throw null!;
    }
}

namespace Microsoft.EntityFrameworkCore.Migrations.Operations
{
    public abstract class MigrationOperation { }
    public class AddColumnOperation : MigrationOperation { }
    public class AddPrimaryKeyOperation : MigrationOperation { }
    public class AddForeignKeyOperation : MigrationOperation { }
    public class AddCheckConstraintOperation : MigrationOperation { }
    public class AddUniqueConstraintOperation : MigrationOperation { }
    public class CreateTableOperation : MigrationOperation { }
    public class CreateIndexOperation : MigrationOperation { }
    public class CreateSequenceOperation : MigrationOperation { }
    public class DropTableOperation : MigrationOperation { }
    public class DropSequenceOperation : MigrationOperation { }
    public class SqlOperation : MigrationOperation { }
}

namespace Microsoft.EntityFrameworkCore.Migrations.Operations.Builders
{
    public class OperationBuilder<TOperation> where TOperation : MigrationOperation { }

    public class ColumnsBuilder
    {
        public virtual OperationBuilder<AddColumnOperation> Column<T>(string? type = null, bool? unicode = null, int? maxLength = null,
            bool rowVersion = false, string? name = null, bool nullable = false, object? defaultValue = null, string? defaultValueSql = null,
            string? computedColumnSql = null, bool? fixedLength = null, string? comment = null, string? collation = null,
            int? precision = null, int? scale = null, bool? stored = null) => throw null!;
    }

    public class CreateTableBuilder<TColumns> : OperationBuilder<CreateTableOperation>
    {
        public virtual OperationBuilder<AddPrimaryKeyOperation> PrimaryKey(string name, Expression<Func<TColumns, object>> columns) => throw null!;
        public virtual OperationBuilder<AddForeignKeyOperation> ForeignKey(string name, Expression<Func<TColumns, object>> column, string principalTable,
            string? principalColumn = null, string? principalSchema = null, ReferentialAction onUpdate = ReferentialAction.NoAction,
            ReferentialAction onDelete = ReferentialAction.NoAction) => throw null!;
        public virtual OperationBuilder<AddForeignKeyOperation> ForeignKey(string name, Expression<Func<TColumns, object>> columns, string principalTable,
            string[] principalColumns, string? principalSchema = null, ReferentialAction onUpdate = ReferentialAction.NoAction,
            ReferentialAction onDelete = ReferentialAction.NoAction) => throw null!;
        public virtual OperationBuilder<AddUniqueConstraintOperation> UniqueConstraint(string name, Expression<Func<TColumns, object>> columns) => throw null!;
        public virtual OperationBuilder<AddCheckConstraintOperation> CheckConstraint(string name, string sql) => throw null!;
    }
}

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure
{
    public class NpgsqlDbContextOptionsBuilder
    {
        public virtual NpgsqlDbContextOptionsBuilder EnableRetryOnFailure(int maxRetryCount) => this;
        public virtual NpgsqlDbContextOptionsBuilder MigrationsAssembly(string? assemblyName) => this;
        public virtual NpgsqlDbContextOptionsBuilder CommandTimeout(int? commandTimeout) => this;
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    using Microsoft.EntityFrameworkCore;

    public static class EntityFrameworkServiceCollectionExtensions
    {
        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder>? optionsAction = null,
            ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped) where TContext : DbContext => serviceCollection;
        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection serviceCollection, Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction,
            ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped) where TContext : DbContext => serviceCollection;
    }
}
