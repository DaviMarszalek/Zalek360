using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zalek360.Domain.Entities;

namespace Zalek360.Infrastructure.Persistence.Configurations;

internal sealed class OpcoesCatalogoMap : IEntityTypeConfiguration<OpcaoCatalogo>
{
    public void Configure(EntityTypeBuilder<OpcaoCatalogo> b)
    {
        b.ToTable("opcoes_catalogo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.Property(x => x.NomeNormalizado).HasMaxLength(100).IsRequired();
        b.Property(x => x.Versao).IsConcurrencyToken();
        b.HasIndex(x => new { x.Tipo, x.NomeNormalizado }).IsUnique();
        // Dados iniciais da migration. As consultas passam a usar apenas o banco.
        var grupos = new (TipoOpcaoCatalogo Tipo, string[] Nomes)[]
        {
            (TipoOpcaoCatalogo.TipoPersonalizacao, ["Serigrafia (frente)", "Serigrafia (frente e costas)", "Serigrafia (1 lado)", "Bordado", "Sublimação", "Gravação a laser", "DTF", "Transfer"]),
            (TipoOpcaoCatalogo.CondicaoPagamento, ["50% na aprovação + 50% na entrega (PIX)", "50% na aprovação + 50% na entrega (boleto)", "100% na aprovação (PIX)", "À vista (PIX)", "30 dias (boleto)", "30/60 dias (boleto)"]),
            (TipoOpcaoCatalogo.MotivoDecisao, ["Preço", "Prazo de entrega", "Escolheu outro fornecedor", "Desistiu da compra", "Mudança no evento", "Outro"]),
            (TipoOpcaoCatalogo.CategoriaProduto, ["Camiseta", "Caneca", "Copo térmico", "Squeeze", "Ecobag", "Boné"])
        };
        var indice = 1;
        foreach (var (tipo, nomes) in grupos)
            for (var ordem = 0; ordem < nomes.Length; ordem++)
                b.HasData(OpcaoCatalogo.Criar(tipo, nomes[ordem], ordem, Guid.Parse($"c0000000-0000-0000-0000-{indice++:D12}")));
    }
}
