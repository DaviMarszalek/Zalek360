using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zalek360.Domain.Entities;

namespace Zalek360.Infrastructure.Persistence.Configurations;

internal sealed class ModeloMensagemPropostaMap : IEntityTypeConfiguration<ModeloMensagemProposta>
{
    public void Configure(EntityTypeBuilder<ModeloMensagemProposta> b)
    {
        b.ToTable("modelos_mensagem_proposta"); b.HasKey(x => x.Id);
        b.Property(x => x.Canal).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Assunto).HasMaxLength(200).IsRequired();
        b.Property(x => x.Mensagem).HasMaxLength(6000).IsRequired();
        b.Property(x => x.Versao).IsConcurrencyToken(); b.HasIndex(x => x.Canal).IsUnique();
        b.HasData(
            ModeloMensagemProposta.Criar(Guid.Parse("d0000000-0000-0000-0000-000000000001"), CanalProposta.Email,
                "Proposta {numero} — {empresa}", "Olá, {cliente}!\n\nSegue em anexo a proposta {numero}, no valor de {valorTotal}, válida até {validade}. Prazo estimado: {prazo} dias úteis após a aprovação.\n\nFicamos à disposição.\n{empresa}"),
            ModeloMensagemProposta.Criar(Guid.Parse("d0000000-0000-0000-0000-000000000002"), CanalProposta.WhatsApp,
                "", "Olá, {cliente}! Segue a proposta {numero}, no valor de {valorTotal}, válida até {validade}. Prazo estimado: {prazo} dias úteis após a aprovação.\n{empresa}"));
    }
}

internal sealed class EnvioPropostaMap : IEntityTypeConfiguration<EnvioProposta>
{
    public void Configure(EntityTypeBuilder<EnvioProposta> b)
    {
        b.ToTable("envios_proposta"); b.HasKey(x => x.Id);
        b.Property(x => x.Destinatario).HasMaxLength(254).IsRequired();
        b.Property(x => x.Assunto).HasMaxLength(200).IsRequired();
        b.Property(x => x.Situacao).HasConversion<string>().HasMaxLength(30);
        b.HasOne<Orcamento>().WithMany().HasForeignKey(x => x.OrcamentoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.OrcamentoId, x.CriadoEm });
    }
}
