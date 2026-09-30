using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zalek360.Infrastructure.Persistence.Migrations;

/// <summary>
/// Migration inicial do MVP 1. Escrita manualmente (o ambiente de desenvolvimento desta entrega não tinha acesso
/// ao NuGet para executar "dotnet ef"), espelhando exatamente o mapeamento de Persistence/Configurations.
/// O VerificadorEsquema confere, na inicialização, se todas as colunas do modelo existem no banco.
/// Para regenerar com a ferramenta oficial (inclui o ModelSnapshot), veja backend/scripts/recriar-migration-inicial.sh.
/// </summary>
[DbContext(typeof(ZalekDbContext))]
[Migration("20260928120000_InitialCreate")]
public partial class InitialCreate : Migration
{
    private const string Uuid = "uuid";
    private const string Texto = "text";
    private const string Inteiro = "integer";
    private const string Booleano = "boolean";
    private const string Data = "date";
    private const string DataHora = "timestamp with time zone";
    private const string Dinheiro = "numeric(10,2)";

    private static string Varchar(int n) => $"character varying({n})";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateSequence<int>(name: "seq_orcamento_numero", startValue: 1L, incrementBy: 1);
        migrationBuilder.CreateSequence<int>(name: "seq_pedido_numero", startValue: 1L, incrementBy: 1);

        migrationBuilder.CreateTable(
            name: "usuarios",
            columns: table => new
            {
                id = table.Column<Guid>(type: Uuid, nullable: false),
                nome = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: false),
                email = table.Column<string>(type: Varchar(150), maxLength: 150, nullable: false),
                senha_hash = table.Column<string>(type: Varchar(255), maxLength: 255, nullable: false),
                perfil = table.Column<string>(type: Varchar(30), maxLength: 30, nullable: false),
                ativo = table.Column<bool>(type: Booleano, nullable: false),
                criado_em = table.Column<DateTime>(type: DataHora, nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_usuarios", x => x.id));

        migrationBuilder.CreateTable(
            name: "produtos",
            columns: table => new
            {
                id = table.Column<Guid>(type: Uuid, nullable: false),
                nome = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: false),
                categoria = table.Column<string>(type: Varchar(50), maxLength: 50, nullable: false),
                descricao_base = table.Column<string>(type: Texto, nullable: true),
                ativo = table.Column<bool>(type: Booleano, nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_produtos", x => x.id));

        migrationBuilder.CreateTable(
            name: "clientes",
            columns: table => new
            {
                id = table.Column<Guid>(type: Uuid, nullable: false),
                tipo_pessoa = table.Column<string>(type: Varchar(30), maxLength: 30, nullable: false),
                cpf_cnpj = table.Column<string>(type: Varchar(14), maxLength: 14, nullable: false),
                nome_razao_social = table.Column<string>(type: Varchar(150), maxLength: 150, nullable: false),
                nome_fantasia = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: true),
                contato_nome = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: true),
                contato_cargo = table.Column<string>(type: Varchar(60), maxLength: 60, nullable: true),
                telefone = table.Column<string>(type: Varchar(15), maxLength: 15, nullable: true),
                whatsapp = table.Column<string>(type: Varchar(15), maxLength: 15, nullable: true),
                email = table.Column<string>(type: Varchar(150), maxLength: 150, nullable: true),
                endereco_cep = table.Column<string>(type: Varchar(8), maxLength: 8, nullable: true),
                endereco_logradouro = table.Column<string>(type: Varchar(150), maxLength: 150, nullable: true),
                endereco_numero = table.Column<string>(type: Varchar(20), maxLength: 20, nullable: true),
                endereco_complemento = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: true),
                endereco_bairro = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: true),
                endereco_cidade = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: true),
                endereco_uf = table.Column<string>(type: Varchar(2), maxLength: 2, nullable: true),
                busca_normalizada = table.Column<string>(type: Varchar(1000), maxLength: 1000, nullable: false),
                versao = table.Column<int>(type: Inteiro, nullable: false),
                data_cadastro = table.Column<DateTime>(type: DataHora, nullable: false),
                criado_por_id = table.Column<Guid>(type: Uuid, nullable: true),
                atualizado_em = table.Column<DateTime>(type: DataHora, nullable: true),
                atualizado_por_id = table.Column<Guid>(type: Uuid, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_clientes", x => x.id);
                table.CheckConstraint("ck_clientes_cpf_cnpj_tamanho", "length(cpf_cnpj) IN (11, 14)");
            });

        migrationBuilder.CreateTable(
            name: "observacoes_cliente",
            columns: table => new
            {
                id = table.Column<Guid>(type: Uuid, nullable: false),
                cliente_id = table.Column<Guid>(type: Uuid, nullable: false),
                texto = table.Column<string>(type: Varchar(2000), maxLength: 2000, nullable: false),
                criado_em = table.Column<DateTime>(type: DataHora, nullable: false),
                autor_id = table.Column<Guid>(type: Uuid, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_observacoes_cliente", x => x.id);
                table.ForeignKey("fk_observacoes_cliente_cliente_id", x => x.cliente_id, "clientes", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_observacoes_cliente_autor_id", x => x.autor_id, "usuarios", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "orcamentos",
            columns: table => new
            {
                id = table.Column<Guid>(type: Uuid, nullable: false),
                numero = table.Column<int>(type: Inteiro, nullable: false),
                cliente_id = table.Column<Guid>(type: Uuid, nullable: false),
                responsavel_id = table.Column<Guid>(type: Uuid, nullable: false),
                data_criacao = table.Column<DateOnly>(type: Data, nullable: false),
                validade = table.Column<DateOnly>(type: Data, nullable: false),
                prazo_estimado = table.Column<int>(type: Inteiro, nullable: false),
                condicao_pagamento = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: false),
                observacoes_comerciais = table.Column<string>(type: Varchar(2000), maxLength: 2000, nullable: true),
                situacao = table.Column<string>(type: Varchar(30), maxLength: 30, nullable: false),
                subtotal_produtos = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                valor_personalizacao = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                desconto = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                valor_total = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                aguardando_retorno_desde = table.Column<DateTime>(type: DataHora, nullable: true),
                data_ultima_edicao = table.Column<DateTime>(type: DataHora, nullable: true),
                versao = table.Column<int>(type: Inteiro, nullable: false),
                criado_em = table.Column<DateTime>(type: DataHora, nullable: false),
                criado_por_id = table.Column<Guid>(type: Uuid, nullable: true),
                atualizado_em = table.Column<DateTime>(type: DataHora, nullable: true),
                atualizado_por_id = table.Column<Guid>(type: Uuid, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_orcamentos", x => x.id);
                table.ForeignKey("fk_orcamentos_cliente_id", x => x.cliente_id, "clientes", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_orcamentos_responsavel_id", x => x.responsavel_id, "usuarios", "id", onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_orcamentos_valores", "desconto >= 0 AND valor_total >= 0");
                table.CheckConstraint("ck_orcamentos_validade", "validade >= data_criacao");
            });

        migrationBuilder.CreateTable(
            name: "itens_orcamento",
            columns: table => new
            {
                id = table.Column<Guid>(type: Uuid, nullable: false),
                orcamento_id = table.Column<Guid>(type: Uuid, nullable: false),
                ordem = table.Column<int>(type: Inteiro, nullable: false),
                produto_id = table.Column<Guid>(type: Uuid, nullable: false),
                produto_nome = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: false),
                descricao = table.Column<string>(type: Varchar(500), maxLength: 500, nullable: true),
                quantidade = table.Column<int>(type: Inteiro, nullable: false),
                valor_unitario = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                valor_personalizacao_unit = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                subtotal_produto = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                total_personalizacao = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                valor_total = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                tipo_personalizacao = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: false),
                cor_peca = table.Column<string>(type: Varchar(50), maxLength: 50, nullable: false),
                cores_arte = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: true),
                medidas = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: true),
                local_aplicacao = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: true),
                referencia_arte = table.Column<string>(type: Varchar(500), maxLength: 500, nullable: true),
                observacoes_tecnicas = table.Column<string>(type: Varchar(2000), maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_itens_orcamento", x => x.id);
                table.ForeignKey("fk_itens_orcamento_orcamento_id", x => x.orcamento_id, "orcamentos", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_itens_orcamento_produto_id", x => x.produto_id, "produtos", "id", onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_itens_orcamento_quantidade", "quantidade > 0");
            });

        migrationBuilder.CreateTable(
            name: "contatos_orcamento",
            columns: table => new
            {
                id = table.Column<Guid>(type: Uuid, nullable: false),
                orcamento_id = table.Column<Guid>(type: Uuid, nullable: false),
                tipo_contato = table.Column<string>(type: Varchar(30), maxLength: 30, nullable: false),
                data_hora = table.Column<DateTime>(type: DataHora, nullable: false),
                observacao = table.Column<string>(type: Varchar(2000), maxLength: 2000, nullable: false),
                registrado_por_id = table.Column<Guid>(type: Uuid, nullable: false),
                criado_em = table.Column<DateTime>(type: DataHora, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_contatos_orcamento", x => x.id);
                table.ForeignKey("fk_contatos_orcamento_orcamento_id", x => x.orcamento_id, "orcamentos", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_contatos_orcamento_registrado_por_id", x => x.registrado_por_id, "usuarios", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "decisoes_orcamento",
            columns: table => new
            {
                id = table.Column<Guid>(type: Uuid, nullable: false),
                orcamento_id = table.Column<Guid>(type: Uuid, nullable: false),
                resultado = table.Column<string>(type: Varchar(30), maxLength: 30, nullable: false),
                meio_contato = table.Column<string>(type: Varchar(30), maxLength: 30, nullable: true),
                data_hora = table.Column<DateTime>(type: DataHora, nullable: false),
                motivo = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: true),
                observacao = table.Column<string>(type: Varchar(2000), maxLength: 2000, nullable: true),
                registrado_por_id = table.Column<Guid>(type: Uuid, nullable: true),
                automatica = table.Column<bool>(type: Booleano, nullable: false),
                criado_em = table.Column<DateTime>(type: DataHora, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_decisoes_orcamento", x => x.id);
                table.ForeignKey("fk_decisoes_orcamento_orcamento_id", x => x.orcamento_id, "orcamentos", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_decisoes_orcamento_registrado_por_id", x => x.registrado_por_id, "usuarios", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "pedidos",
            columns: table => new
            {
                id = table.Column<Guid>(type: Uuid, nullable: false),
                numero = table.Column<int>(type: Inteiro, nullable: false),
                orcamento_origem_id = table.Column<Guid>(type: Uuid, nullable: false),
                cliente_id = table.Column<Guid>(type: Uuid, nullable: false),
                responsavel_id = table.Column<Guid>(type: Uuid, nullable: false),
                situacao = table.Column<string>(type: Varchar(30), maxLength: 30, nullable: false),
                data_pedido = table.Column<DateOnly>(type: Data, nullable: false),
                prazo_estimado = table.Column<int>(type: Inteiro, nullable: false),
                prazo_entrega = table.Column<DateOnly>(type: Data, nullable: false),
                condicao_pagamento = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: false),
                observacoes_comerciais = table.Column<string>(type: Varchar(2000), maxLength: 2000, nullable: true),
                observacao_decisao = table.Column<string>(type: Varchar(2000), maxLength: 2000, nullable: true),
                subtotal_produtos = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                valor_personalizacao = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                desconto = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                valor_total = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                entregue_em = table.Column<DateTime>(type: DataHora, nullable: true),
                cancelado_em = table.Column<DateTime>(type: DataHora, nullable: true),
                motivo_cancelamento = table.Column<string>(type: Varchar(500), maxLength: 500, nullable: true),
                versao = table.Column<int>(type: Inteiro, nullable: false),
                data_criacao = table.Column<DateTime>(type: DataHora, nullable: false),
                criado_por_id = table.Column<Guid>(type: Uuid, nullable: true),
                atualizado_em = table.Column<DateTime>(type: DataHora, nullable: true),
                atualizado_por_id = table.Column<Guid>(type: Uuid, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_pedidos", x => x.id);
                table.ForeignKey("fk_pedidos_orcamento_origem_id", x => x.orcamento_origem_id, "orcamentos", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_pedidos_cliente_id", x => x.cliente_id, "clientes", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_pedidos_responsavel_id", x => x.responsavel_id, "usuarios", "id", onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_pedidos_valores", "desconto >= 0 AND valor_total >= 0");
            });

        migrationBuilder.CreateTable(
            name: "itens_pedido",
            columns: table => new
            {
                id = table.Column<Guid>(type: Uuid, nullable: false),
                pedido_id = table.Column<Guid>(type: Uuid, nullable: false),
                item_orcamento_origem_id = table.Column<Guid>(type: Uuid, nullable: false),
                ordem = table.Column<int>(type: Inteiro, nullable: false),
                produto_id = table.Column<Guid>(type: Uuid, nullable: false),
                produto_nome = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: false),
                descricao = table.Column<string>(type: Varchar(500), maxLength: 500, nullable: true),
                quantidade = table.Column<int>(type: Inteiro, nullable: false),
                valor_unitario = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                valor_personalizacao_unit = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                subtotal_produto = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                total_personalizacao = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                valor_total = table.Column<decimal>(type: Dinheiro, precision: 10, scale: 2, nullable: false),
                tipo_personalizacao = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: false),
                cor_peca = table.Column<string>(type: Varchar(50), maxLength: 50, nullable: false),
                cores_arte = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: true),
                medidas = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: true),
                local_aplicacao = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: true),
                referencia_arte = table.Column<string>(type: Varchar(500), maxLength: 500, nullable: true),
                observacoes_tecnicas = table.Column<string>(type: Varchar(2000), maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_itens_pedido", x => x.id);
                table.ForeignKey("fk_itens_pedido_pedido_id", x => x.pedido_id, "pedidos", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_itens_pedido_produto_id", x => x.produto_id, "produtos", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_itens_pedido_item_orcamento_origem_id", x => x.item_orcamento_origem_id, "itens_orcamento", "id", onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_itens_pedido_quantidade", "quantidade > 0");
            });

        migrationBuilder.CreateTable(
            name: "arquivos_anexos",
            columns: table => new
            {
                id = table.Column<Guid>(type: Uuid, nullable: false),
                item_orcamento_id = table.Column<Guid>(type: Uuid, nullable: true),
                item_pedido_id = table.Column<Guid>(type: Uuid, nullable: true),
                anexo_origem_id = table.Column<Guid>(type: Uuid, nullable: true),
                nome_arquivo = table.Column<string>(type: Varchar(255), maxLength: 255, nullable: false),
                extensao = table.Column<string>(type: Varchar(10), maxLength: 10, nullable: false),
                content_type = table.Column<string>(type: Varchar(100), maxLength: 100, nullable: false),
                tamanho_bytes = table.Column<long>(type: "bigint", nullable: false),
                chave_armazenamento = table.Column<string>(type: Varchar(500), maxLength: 500, nullable: false),
                criado_em = table.Column<DateTime>(type: DataHora, nullable: false),
                criado_por_id = table.Column<Guid>(type: Uuid, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_arquivos_anexos", x => x.id);
                table.ForeignKey("fk_arquivos_anexos_item_orcamento_id", x => x.item_orcamento_id, "itens_orcamento", "id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("fk_arquivos_anexos_item_pedido_id", x => x.item_pedido_id, "itens_pedido", "id", onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_arquivos_anexos_tamanho", "tamanho_bytes > 0");
            });

        migrationBuilder.CreateTable(
            name: "historico_eventos",
            columns: table => new
            {
                id = table.Column<Guid>(type: Uuid, nullable: false),
                escopo = table.Column<string>(type: Varchar(30), maxLength: 30, nullable: false),
                tipo = table.Column<string>(type: Varchar(40), maxLength: 40, nullable: false),
                cliente_id = table.Column<Guid>(type: Uuid, nullable: false),
                orcamento_id = table.Column<Guid>(type: Uuid, nullable: true),
                pedido_id = table.Column<Guid>(type: Uuid, nullable: true),
                titulo = table.Column<string>(type: Varchar(200), maxLength: 200, nullable: false),
                descricao = table.Column<string>(type: Varchar(4000), maxLength: 4000, nullable: true),
                situacao_anterior = table.Column<string>(type: Varchar(40), maxLength: 40, nullable: true),
                situacao_nova = table.Column<string>(type: Varchar(40), maxLength: 40, nullable: true),
                meio_contato = table.Column<string>(type: Varchar(30), maxLength: 30, nullable: true),
                ocorrido_em = table.Column<DateTime>(type: DataHora, nullable: false),
                registrado_em = table.Column<DateTime>(type: DataHora, nullable: false),
                usuario_id = table.Column<Guid>(type: Uuid, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_historico_eventos", x => x.id);
                table.ForeignKey("fk_historico_eventos_cliente_id", x => x.cliente_id, "clientes", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_historico_eventos_orcamento_id", x => x.orcamento_id, "orcamentos", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_historico_eventos_pedido_id", x => x.pedido_id, "pedidos", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_historico_eventos_usuario_id", x => x.usuario_id, "usuarios", "id", onDelete: ReferentialAction.Restrict);
            });

        // ---------------------------------------------------------------- índices (RNF 5.3.2)
        Indice(migrationBuilder, "usuarios", true, "email");
        Indice(migrationBuilder, "produtos", true, "nome");
        Indice(migrationBuilder, "clientes", true, "cpf_cnpj");
        Indice(migrationBuilder, "clientes", false, "endereco_cidade");
        Indice(migrationBuilder, "clientes", false, "tipo_pessoa");
        Indice(migrationBuilder, "observacoes_cliente", false, "cliente_id", "criado_em");
        Indice(migrationBuilder, "observacoes_cliente", false, "autor_id");
        Indice(migrationBuilder, "orcamentos", true, "numero");
        Indice(migrationBuilder, "orcamentos", false, "cliente_id", "data_criacao");
        Indice(migrationBuilder, "orcamentos", false, "situacao", "validade");
        Indice(migrationBuilder, "orcamentos", false, "data_criacao");
        Indice(migrationBuilder, "orcamentos", false, "responsavel_id");
        Indice(migrationBuilder, "itens_orcamento", false, "orcamento_id");
        Indice(migrationBuilder, "itens_orcamento", false, "produto_id");
        Indice(migrationBuilder, "contatos_orcamento", false, "orcamento_id", "data_hora");
        Indice(migrationBuilder, "contatos_orcamento", false, "registrado_por_id");
        Indice(migrationBuilder, "decisoes_orcamento", true, "orcamento_id");
        Indice(migrationBuilder, "decisoes_orcamento", false, "resultado", "data_hora");
        Indice(migrationBuilder, "decisoes_orcamento", false, "registrado_por_id");
        Indice(migrationBuilder, "pedidos", true, "numero");
        Indice(migrationBuilder, "pedidos", true, "orcamento_origem_id");
        Indice(migrationBuilder, "pedidos", false, "cliente_id", "data_pedido");
        Indice(migrationBuilder, "pedidos", false, "situacao");
        Indice(migrationBuilder, "pedidos", false, "data_pedido");
        Indice(migrationBuilder, "pedidos", false, "responsavel_id");
        Indice(migrationBuilder, "itens_pedido", false, "pedido_id");
        Indice(migrationBuilder, "itens_pedido", false, "produto_id");
        Indice(migrationBuilder, "itens_pedido", false, "item_orcamento_origem_id");
        Indice(migrationBuilder, "arquivos_anexos", false, "item_orcamento_id");
        Indice(migrationBuilder, "arquivos_anexos", false, "item_pedido_id");
        Indice(migrationBuilder, "arquivos_anexos", false, "chave_armazenamento");
        Indice(migrationBuilder, "historico_eventos", false, "cliente_id", "ocorrido_em");
        Indice(migrationBuilder, "historico_eventos", false, "orcamento_id", "ocorrido_em");
        Indice(migrationBuilder, "historico_eventos", false, "pedido_id", "ocorrido_em");
        Indice(migrationBuilder, "historico_eventos", false, "usuario_id");
    }

    private static void Indice(MigrationBuilder migrationBuilder, string tabela, bool unico, params string[] colunas) =>
        migrationBuilder.CreateIndex(
            name: $"{(unico ? "ux" : "ix")}_{tabela}_{string.Join("_", colunas)}",
            table: tabela,
            columns: colunas,
            unique: unico);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var tabela in new[]
                 {
                     "historico_eventos", "arquivos_anexos", "itens_pedido", "pedidos", "decisoes_orcamento",
                     "contatos_orcamento", "itens_orcamento", "orcamentos", "observacoes_cliente", "clientes",
                     "produtos", "usuarios"
                 })
            migrationBuilder.DropTable(name: tabela);

        migrationBuilder.DropSequence(name: "seq_pedido_numero");
        migrationBuilder.DropSequence(name: "seq_orcamento_numero");
    }
}
