using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Tests.Integracao;

public class ConvencaoNomesTests
{
    [Theory]
    [InlineData("ix_compras_materias_primas_materia_prima_id_situacao_data_prevista", "ix_compras_materias_primas_materia_prima_id_situacao_d_80534e86")]
    [InlineData("ix_reservas_materias_primas_pedido_pedido_id_materia_prima_id_situacao", "ix_reservas_materias_primas_pedido_pedido_id_materia_p_da9f39ed")]
    public void Hash_tem_resultado_conhecido_e_estavel(string entrada, string esperado)
    {
        Assert.Equal(esperado, ConvencaoNomes.Limitar(entrada));
        Assert.Equal(esperado, ConvencaoNomes.Limitar(entrada));
    }

    [Fact]
    public void Nomes_curto_e_unicode_respeitam_o_limite_em_bytes()
    {
        var curto = new string('a', 63);
        Assert.Equal(curto, ConvencaoNomes.Limitar(curto));
        Assert.Equal("pk_clientes", ConvencaoNomes.Limitar("pk_clientes"));
        var unicode = ConvencaoNomes.Limitar(new string('é', 40));
        Assert.True(Encoding.UTF8.GetByteCount(unicode) <= 63);
        Assert.DoesNotContain('\uFFFD', unicode);
    }

    [Fact]
    public void Todos_os_identificadores_do_modelo_cabem_e_so_dois_nomes_convencionais_mudam()
    {
        using var db = new ZalekDbContext(new DbContextOptionsBuilder<ZalekDbContext>()
            .UseNpgsql("Host=localhost;Database=nao_conectar;Username=teste;Password=teste").Options);
        var model = db.GetService<IDesignTimeModel>().Model;
        var identificadores = new List<string>();
        var alterados = new List<(string Antes, string Depois)>();
        foreach (var e in model.GetEntityTypes())
        {
            var tabela = e.GetTableName()!;
            var store = StoreObjectIdentifier.Table(tabela, e.GetSchema());
            identificadores.Add(tabela);
            identificadores.AddRange(e.GetProperties().Select(p => p.GetColumnName(store)!));
            identificadores.AddRange(e.GetKeys().Select(k => k.GetName()!));
            identificadores.AddRange(e.GetCheckConstraints().Select(c => c.Name!));
            foreach (var fk in e.GetForeignKeys())
            {
                var original = $"fk_{tabela}_{string.Join("_", fk.Properties.Select(p => p.GetColumnName(store)))}";
                var atual = fk.GetConstraintName()!;
                identificadores.Add(atual);
                if (original != atual) alterados.Add((original, atual));
            }
            foreach (var index in e.GetIndexes())
            {
                var original = $"{(index.IsUnique ? "ux" : "ix")}_{tabela}_{string.Join("_", index.Properties.Select(p => p.GetColumnName(store)))}";
                var atual = index.GetDatabaseName()!;
                identificadores.Add(atual);
                if (original != atual) alterados.Add((original, atual));
            }
        }
        identificadores.AddRange(model.GetSequences().Select(s => s.Name));
        Assert.All(identificadores, n => Assert.InRange(Encoding.UTF8.GetByteCount(n), 1, 63));
        Assert.Equal(2, alterados.Count);
        Assert.Contains(alterados, x => x.Depois == "ix_compras_materias_primas_materia_prima_id_situacao_d_80534e86");
        Assert.Contains(alterados, x => x.Depois == "ix_reservas_materias_primas_pedido_pedido_id_materia_p_da9f39ed");
        Assert.All(alterados, x => Assert.True(Encoding.UTF8.GetByteCount(x.Antes) > 63));
    }
}
