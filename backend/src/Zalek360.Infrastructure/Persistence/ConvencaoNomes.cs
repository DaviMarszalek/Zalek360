using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Zalek360.Infrastructure.Persistence;

/// <summary>
/// Convenção física (RNF 5.1.1 + dicionário de dados): tabelas e colunas em português, snake_case.
/// Colunas com nome explícito (HasColumnName) são preservadas. PK/FK/índices recebem nomes determinísticos:
/// pk_{tabela}, fk_{tabela}_{colunas}, ix_/ux_{tabela}_{colunas}. Chaves UUID são geradas no domínio.
/// </summary>
internal static class ConvencaoNomes
{
    public static void Aplicar(ModelBuilder modelBuilder)
    {
        foreach (var entidade in modelBuilder.Model.GetEntityTypes())
        {
            var tabela = entidade.GetTableName();
            if (tabela is null) continue;

            foreach (var propriedade in entidade.GetProperties())
                if (propriedade.FindAnnotation(RelationalAnnotationNames.ColumnName) is null)
                    propriedade.SetColumnName(SnakeCase(propriedade.Name));

            var pk = entidade.FindPrimaryKey();
            if (pk is not null)
            {
                pk.SetName($"pk_{tabela}");
                foreach (var p in pk.Properties) p.ValueGenerated = ValueGenerated.Never;
            }

            foreach (var fk in entidade.GetForeignKeys())
                fk.SetConstraintName($"fk_{tabela}_{Colunas(fk.Properties)}");

            foreach (var indice in entidade.GetIndexes())
                indice.SetDatabaseName($"{(indice.IsUnique ? "ux" : "ix")}_{tabela}_{Colunas(indice.Properties)}");
        }
    }

    private static string Colunas(IEnumerable<IMutableProperty> propriedades) =>
        string.Join("_", propriedades.Select(p => p.GetColumnName()));

    public static string SnakeCase(string nome)
    {
        var sb = new StringBuilder(nome.Length + 8);
        for (var i = 0; i < nome.Length; i++)
        {
            var c = nome[i];
            if (char.IsUpper(c))
            {
                var anteriorMinusculo = i > 0 && (char.IsLower(nome[i - 1]) || char.IsDigit(nome[i - 1]));
                var fimDeSigla = i > 0 && char.IsUpper(nome[i - 1]) && i + 1 < nome.Length && char.IsLower(nome[i + 1]);
                if (anteriorMinusculo || fimDeSigla) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }
}
