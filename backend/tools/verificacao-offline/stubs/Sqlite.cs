#nullable enable
using System.Data.Common;
namespace Microsoft.Data.Sqlite
{
    public class SqliteConnection : DbConnection
    {
        public SqliteConnection(string? connectionString) { }
        public override string ConnectionString { get; set; } = "";
        public override string Database => "";
        public override string DataSource => "";
        public override string ServerVersion => "";
        public override System.Data.ConnectionState State => default;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) => throw null!;
        protected override DbCommand CreateDbCommand() => throw null!;
    }
}
namespace Microsoft.EntityFrameworkCore
{
    public static class SqliteDbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder<TContext> UseSqlite<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, DbConnection connection, Action<object>? sqliteOptionsAction = null) where TContext : DbContext => optionsBuilder;
        public static DbContextOptionsBuilder<TContext> UseSqlite<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, string? connectionString, Action<object>? sqliteOptionsAction = null) where TContext : DbContext => optionsBuilder;
    }
}
