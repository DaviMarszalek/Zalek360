using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zalek360.Domain.Entities;

namespace Zalek360.Infrastructure.Persistence.Configurations;

// Mapeamento físico baseado no Dicionário de Dados (seção 15). Valores monetários: numeric(10,2) (decimal, nunca float/double).
// Enums persistidos como texto legível. Nomes de colunas em snake_case pela ConvencaoNomes; os nomes que o dicionário
// define de forma diferente do nome da propriedade são declarados aqui explicitamente.

internal static class Tipos
{
    public const int Precisao = 10;
    public const int Escala = 2;
    public const int Enum = 30;
}

internal sealed class UsuarioMap : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(150).IsRequired();
        b.Property(x => x.SenhaHash).HasMaxLength(255).IsRequired();
        b.Property(x => x.Perfil).HasConversion<string>().HasMaxLength(Tipos.Enum).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
    }
}

internal sealed class ProdutoMap : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> b)
    {
        b.ToTable("produtos");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        b.Property(x => x.Categoria).HasMaxLength(50).IsRequired();
        b.Property(x => x.DescricaoBase);
        b.HasIndex(x => x.Nome).IsUnique();
    }
}

internal sealed class ClienteMap : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("clientes");
        b.HasKey(x => x.Id);
        b.Property(x => x.TipoPessoa).HasConversion<string>().HasMaxLength(Tipos.Enum).IsRequired();
        b.Property(x => x.Documento).HasColumnName("cpf_cnpj").HasMaxLength(14).IsRequired();
        b.Property(x => x.NomeRazaoSocial).HasMaxLength(150).IsRequired();
        b.Property(x => x.NomeFantasia).HasMaxLength(100);
        b.Property(x => x.ContatoNome).HasMaxLength(100);
        b.Property(x => x.ContatoCargo).HasMaxLength(60);
        b.Property(x => x.Telefone).HasMaxLength(15);
        b.Property(x => x.WhatsApp).HasColumnName("whatsapp").HasMaxLength(15);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.EnderecoCep).HasMaxLength(8);
        b.Property(x => x.EnderecoLogradouro).HasMaxLength(150);
        b.Property(x => x.EnderecoNumero).HasMaxLength(20);
        b.Property(x => x.EnderecoComplemento).HasMaxLength(100);
        b.Property(x => x.EnderecoBairro).HasMaxLength(100);
        b.Property(x => x.EnderecoCidade).HasMaxLength(100);
        b.Property(x => x.EnderecoUf).HasMaxLength(2);
        b.Property(x => x.BuscaNormalizada).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Versao).IsConcurrencyToken();
        b.Property(x => x.CriadoEm).HasColumnName("data_cadastro");

        // RN2 — CPF/CNPJ único garantido pelo banco (além da validação da aplicação).
        b.HasIndex(x => x.Documento).IsUnique();
        b.HasIndex(x => x.EnderecoCidade);
        b.HasIndex(x => x.TipoPessoa);
        b.ToTable(t => t.HasCheckConstraint("ck_clientes_cpf_cnpj_tamanho", "length(cpf_cnpj) IN (11, 14)"));
    }
}

internal sealed class ObservacaoClienteMap : IEntityTypeConfiguration<ObservacaoCliente>
{
    public void Configure(EntityTypeBuilder<ObservacaoCliente> b)
    {
        b.ToTable("observacoes_cliente");
        b.HasKey(x => x.Id);
        b.Property(x => x.Texto).HasMaxLength(2000).IsRequired();
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Autor).WithMany().HasForeignKey(x => x.AutorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ClienteId, x.CriadoEm });
    }
}

internal sealed class OrcamentoMap : IEntityTypeConfiguration<Orcamento>
{
    public void Configure(EntityTypeBuilder<Orcamento> b)
    {
        b.ToTable("orcamentos", t =>
        {
            t.HasCheckConstraint("ck_orcamentos_valores", "desconto >= 0 AND valor_total >= 0");
            t.HasCheckConstraint("ck_orcamentos_validade", "validade >= data_criacao");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Numero).IsRequired();
        b.Property(x => x.DataOrcamento).HasColumnName("data_criacao");
        b.Property(x => x.PrazoEstimadoDiasUteis).HasColumnName("prazo_estimado");
        b.Property(x => x.CondicaoPagamento).HasMaxLength(100).IsRequired();
        b.Property(x => x.ObservacoesComerciais).HasMaxLength(2000);
        b.Property(x => x.Situacao).HasConversion<string>().HasMaxLength(Tipos.Enum).IsRequired();
        b.Property(x => x.SubtotalProdutos).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.ValorPersonalizacao).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.Desconto).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.ValorTotal).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.Versao).IsConcurrencyToken();

        b.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Responsavel).WithMany().HasForeignKey(x => x.ResponsavelId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Itens).WithOne().HasForeignKey(i => i.OrcamentoId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Contatos).WithOne().HasForeignKey(c => c.OrcamentoId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Decisao).WithOne().HasForeignKey<DecisaoOrcamento>(d => d.OrcamentoId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Contatos).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasIndex(x => x.Numero).IsUnique();
        b.HasIndex(x => new { x.ClienteId, x.DataOrcamento });
        b.HasIndex(x => new { x.Situacao, x.Validade });
        b.HasIndex(x => x.DataOrcamento);
        b.HasIndex(x => x.ResponsavelId);
    }
}

internal sealed class ItemOrcamentoMap : IEntityTypeConfiguration<ItemOrcamento>
{
    public void Configure(EntityTypeBuilder<ItemOrcamento> b)
    {
        b.ToTable("itens_orcamento", t => t.HasCheckConstraint("ck_itens_orcamento_quantidade", "quantidade > 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.ProdutoNome).HasMaxLength(100).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(500);
        b.Property(x => x.ValorUnitario).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.ValorPersonalizacaoUnitario).HasColumnName("valor_personalizacao_unit").HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.SubtotalProduto).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.TotalPersonalizacao).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.ValorTotal).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.TipoPersonalizacao).HasMaxLength(100).IsRequired();
        b.Property(x => x.CorPeca).HasMaxLength(50).IsRequired();
        b.Property(x => x.CoresArte).HasMaxLength(100);
        b.Property(x => x.Medidas).HasMaxLength(100);
        b.Property(x => x.LocalAplicacao).HasMaxLength(100);
        b.Property(x => x.ReferenciaArte).HasMaxLength(500);
        b.Property(x => x.ObservacoesTecnicas).HasMaxLength(2000);

        b.HasOne<Produto>().WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        // Item removido na edição: anexos voltam a ser "pendentes" (FK nula) e são limpos pela rotina de limpeza.
        b.HasMany(x => x.Anexos).WithOne().HasForeignKey(a => a.ItemOrcamentoId).OnDelete(DeleteBehavior.SetNull);
        b.Navigation(x => x.Anexos).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasIndex(x => x.OrcamentoId);
        b.HasIndex(x => x.ProdutoId);
    }
}

internal sealed class ArquivoAnexoMap : IEntityTypeConfiguration<ArquivoAnexo>
{
    public void Configure(EntityTypeBuilder<ArquivoAnexo> b)
    {
        b.ToTable("arquivos_anexos", t => t.HasCheckConstraint("ck_arquivos_anexos_tamanho", "tamanho_bytes > 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.NomeOriginal).HasColumnName("nome_arquivo").HasMaxLength(255).IsRequired();
        b.Property(x => x.Extensao).HasMaxLength(10).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        b.Property(x => x.ChaveArmazenamento).HasMaxLength(500).IsRequired();
        b.HasIndex(x => x.ItemOrcamentoId);
        b.HasIndex(x => x.ItemPedidoId);
        b.HasIndex(x => x.ChaveArmazenamento);
    }
}

internal sealed class ContatoOrcamentoMap : IEntityTypeConfiguration<ContatoOrcamento>
{
    public void Configure(EntityTypeBuilder<ContatoOrcamento> b)
    {
        b.ToTable("contatos_orcamento");
        b.HasKey(x => x.Id);
        b.Property(x => x.Tipo).HasColumnName("tipo_contato").HasConversion<string>().HasMaxLength(Tipos.Enum).IsRequired();
        b.Property(x => x.Observacao).HasMaxLength(2000).IsRequired();
        b.HasOne(x => x.RegistradoPor).WithMany().HasForeignKey(x => x.RegistradoPorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.OrcamentoId, x.DataHora });
        b.HasIndex(x => x.RegistradoPorId);
    }
}

internal sealed class DecisaoOrcamentoMap : IEntityTypeConfiguration<DecisaoOrcamento>
{
    public void Configure(EntityTypeBuilder<DecisaoOrcamento> b)
    {
        b.ToTable("decisoes_orcamento");
        b.HasKey(x => x.Id);
        b.Property(x => x.Resultado).HasConversion<string>().HasMaxLength(Tipos.Enum).IsRequired();
        b.Property(x => x.MeioContato).HasConversion<string>().HasMaxLength(Tipos.Enum);
        b.Property(x => x.Motivo).HasMaxLength(100);
        b.Property(x => x.Observacao).HasMaxLength(2000);
        b.HasOne(x => x.RegistradoPor).WithMany().HasForeignKey(x => x.RegistradoPorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.OrcamentoId).IsUnique();
        b.HasIndex(x => new { x.Resultado, x.DataHora });
        b.HasIndex(x => x.RegistradoPorId);
    }
}

internal sealed class PedidoMap : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> b)
    {
        b.ToTable("pedidos", t => t.HasCheckConstraint("ck_pedidos_valores", "desconto >= 0 AND valor_total >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Situacao).HasConversion<string>().HasMaxLength(Tipos.Enum).IsRequired();
        b.Property(x => x.PrazoEstimadoDiasUteis).HasColumnName("prazo_estimado");
        b.Property(x => x.CondicaoPagamento).HasMaxLength(100).IsRequired();
        b.Property(x => x.ObservacoesComerciais).HasMaxLength(2000);
        b.Property(x => x.ObservacaoDecisao).HasMaxLength(2000);
        b.Property(x => x.MotivoCancelamento).HasMaxLength(500);
        b.Property(x => x.SubtotalProdutos).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.ValorPersonalizacao).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.Desconto).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.ValorTotal).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.Versao).IsConcurrencyToken();
        b.Property(x => x.CriadoEm).HasColumnName("data_criacao");

        // RN9 — todo pedido nasce de um orçamento (FK obrigatória e única: um orçamento gera no máximo um pedido).
        b.HasOne(x => x.OrcamentoOrigem).WithOne(o => o.PedidoGerado)
            .HasForeignKey<Pedido>(x => x.OrcamentoOrigemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Responsavel).WithMany().HasForeignKey(x => x.ResponsavelId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Itens).WithOne().HasForeignKey(i => i.PedidoId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasIndex(x => x.Numero).IsUnique();
        b.HasIndex(x => x.OrcamentoOrigemId).IsUnique();
        b.HasIndex(x => new { x.ClienteId, x.DataPedido });
        b.HasIndex(x => x.Situacao);
        b.HasIndex(x => x.DataPedido);
        b.HasIndex(x => x.ResponsavelId);
    }
}

internal sealed class ItemPedidoMap : IEntityTypeConfiguration<ItemPedido>
{
    public void Configure(EntityTypeBuilder<ItemPedido> b)
    {
        b.ToTable("itens_pedido", t => t.HasCheckConstraint("ck_itens_pedido_quantidade", "quantidade > 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.ProdutoNome).HasMaxLength(100).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(500);
        b.Property(x => x.ValorUnitario).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.ValorPersonalizacaoUnitario).HasColumnName("valor_personalizacao_unit").HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.SubtotalProduto).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.TotalPersonalizacao).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.ValorTotal).HasPrecision(Tipos.Precisao, Tipos.Escala);
        b.Property(x => x.TipoPersonalizacao).HasMaxLength(100).IsRequired();
        b.Property(x => x.CorPeca).HasMaxLength(50).IsRequired();
        b.Property(x => x.CoresArte).HasMaxLength(100);
        b.Property(x => x.Medidas).HasMaxLength(100);
        b.Property(x => x.LocalAplicacao).HasMaxLength(100);
        b.Property(x => x.ReferenciaArte).HasMaxLength(500);
        b.Property(x => x.ObservacoesTecnicas).HasMaxLength(2000);

        b.HasOne<Produto>().WithMany().HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ItemOrcamento>().WithMany().HasForeignKey(x => x.ItemOrcamentoOrigemId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Anexos).WithOne().HasForeignKey(a => a.ItemPedidoId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Anexos).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasIndex(x => x.PedidoId);
        b.HasIndex(x => x.ProdutoId);
        b.HasIndex(x => x.ItemOrcamentoOrigemId);
    }
}

internal sealed class HistoricoEventoMap : IEntityTypeConfiguration<HistoricoEvento>
{
    public void Configure(EntityTypeBuilder<HistoricoEvento> b)
    {
        b.ToTable("historico_eventos");
        b.HasKey(x => x.Id);
        b.Property(x => x.Escopo).HasConversion<string>().HasMaxLength(Tipos.Enum).IsRequired();
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(40).IsRequired();
        b.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(4000);
        b.Property(x => x.SituacaoAnterior).HasMaxLength(40);
        b.Property(x => x.SituacaoNova).HasMaxLength(40);
        b.Property(x => x.MeioContato).HasConversion<string>().HasMaxLength(Tipos.Enum);

        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Orcamento>().WithMany().HasForeignKey(x => x.OrcamentoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Pedido>().WithMany().HasForeignKey(x => x.PedidoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.ClienteId, x.OcorridoEm });
        b.HasIndex(x => new { x.OrcamentoId, x.OcorridoEm });
        b.HasIndex(x => new { x.PedidoId, x.OcorridoEm });
        b.HasIndex(x => x.UsuarioId);
    }
}
