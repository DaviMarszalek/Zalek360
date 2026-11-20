using System.Text;
using System.Security.Cryptography;
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
            tabela = Limitar(tabela);
            entidade.SetTableName(tabela);

            foreach (var propriedade in entidade.GetProperties())
                if (propriedade.FindAnnotation(RelationalAnnotationNames.ColumnName) is null)
                    propriedade.SetColumnName(Limitar(SnakeCase(propriedade.Name)));
                else
                    propriedade.SetColumnName(Limitar(propriedade.GetColumnName()!));

            var pk = entidade.FindPrimaryKey();
            if (pk is not null)
            {
                pk.SetName(Limitar($"pk_{tabela}"));
                foreach (var p in pk.Properties) p.ValueGenerated = ValueGenerated.Never;
            }

            foreach (var fk in entidade.GetForeignKeys())
                fk.SetConstraintName(Limitar($"fk_{tabela}_{Colunas(fk.Properties)}"));

            foreach (var indice in entidade.GetIndexes())
                indice.SetDatabaseName(Limitar($"{(indice.IsUnique ? "ux" : "ix")}_{tabela}_{Colunas(indice.Properties)}"));

            foreach (var check in entidade.GetCheckConstraints())
                if (check.Name is { } nome) check.Name = Limitar(nome);
        }
    }

    /// <summary>PostgreSQL limita identificadores a 63 bytes. SHA-256 evita colisões por truncamento e é estável entre processos.</summary>
    public static string Limitar(string nome)
    {
        if (Encoding.UTF8.GetByteCount(nome) <= 63) return nome;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(nome)))[..8].ToLowerInvariant();
        var prefixo = new StringBuilder();
        var bytes = 0;
        foreach (var rune in nome.EnumerateRunes())
        {
            if (bytes + rune.Utf8SequenceLength > 54) break;
            prefixo.Append(rune.ToString());
            bytes += rune.Utf8SequenceLength;
        }
        return $"{prefixo}_{hash}";
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
