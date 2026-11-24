using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Zalek360.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogoProdutosEOpcoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "custo",
                table: "produtos",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "preco_venda",
                table: "produtos",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "versao",
                table: "produtos",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "opcoes_catalogo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nome_normalizado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    versao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opcoes_catalogo", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "opcoes_catalogo",
                columns: new[] { "id", "ativo", "nome", "nome_normalizado", "ordem", "tipo", "versao" },
                values: new object[,]
                {
                    { new Guid("c0000000-0000-0000-0000-000000000001"), true, "Serigrafia (frente)", "SERIGRAFIA (FRENTE)", 0, "TipoPersonalizacao", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000002"), true, "Serigrafia (frente e costas)", "SERIGRAFIA (FRENTE E COSTAS)", 1, "TipoPersonalizacao", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000003"), true, "Serigrafia (1 lado)", "SERIGRAFIA (1 LADO)", 2, "TipoPersonalizacao", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000004"), true, "Bordado", "BORDADO", 3, "TipoPersonalizacao", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000005"), true, "Sublimação", "SUBLIMAÇÃO", 4, "TipoPersonalizacao", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000006"), true, "Gravação a laser", "GRAVAÇÃO A LASER", 5, "TipoPersonalizacao", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000007"), true, "DTF", "DTF", 6, "TipoPersonalizacao", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000008"), true, "Transfer", "TRANSFER", 7, "TipoPersonalizacao", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000009"), true, "50% na aprovação + 50% na entrega (PIX)", "50% NA APROVAÇÃO + 50% NA ENTREGA (PIX)", 0, "CondicaoPagamento", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000010"), true, "50% na aprovação + 50% na entrega (boleto)", "50% NA APROVAÇÃO + 50% NA ENTREGA (BOLETO)", 1, "CondicaoPagamento", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000011"), true, "100% na aprovação (PIX)", "100% NA APROVAÇÃO (PIX)", 2, "CondicaoPagamento", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000012"), true, "À vista (PIX)", "À VISTA (PIX)", 3, "CondicaoPagamento", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000013"), true, "30 dias (boleto)", "30 DIAS (BOLETO)", 4, "CondicaoPagamento", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000014"), true, "30/60 dias (boleto)", "30/60 DIAS (BOLETO)", 5, "CondicaoPagamento", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000015"), true, "Preço", "PREÇO", 0, "MotivoDecisao", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000016"), true, "Prazo de entrega", "PRAZO DE ENTREGA", 1, "MotivoDecisao", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000017"), true, "Escolheu outro fornecedor", "ESCOLHEU OUTRO FORNECEDOR", 2, "MotivoDecisao", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000018"), true, "Desistiu da compra", "DESISTIU DA COMPRA", 3, "MotivoDecisao", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000019"), true, "Mudança no evento", "MUDANÇA NO EVENTO", 4, "MotivoDecisao", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000020"), true, "Outro", "OUTRO", 5, "MotivoDecisao", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000021"), true, "Camiseta", "CAMISETA", 0, "CategoriaProduto", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000022"), true, "Caneca", "CANECA", 1, "CategoriaProduto", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000023"), true, "Copo térmico", "COPO TÉRMICO", 2, "CategoriaProduto", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000024"), true, "Squeeze", "SQUEEZE", 3, "CategoriaProduto", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000025"), true, "Ecobag", "ECOBAG", 4, "CategoriaProduto", 1 },
                    { new Guid("c0000000-0000-0000-0000-000000000026"), true, "Boné", "BONÉ", 5, "CategoriaProduto", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "ux_opcoes_catalogo_tipo_nome_normalizado",
                table: "opcoes_catalogo",
                columns: new[] { "tipo", "nome_normalizado" },
                unique: true);
            // Cópia aditiva das categorias existentes; produtos e seus textos não são alterados.
            if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
                migrationBuilder.Sql("""
                    INSERT INTO opcoes_catalogo (id, tipo, nome, nome_normalizado, ativo, ordem, versao)
                    SELECT gen_random_uuid(), 'CategoriaProduto', MIN(btrim(categoria)), upper(btrim(categoria)), true, 100, 1
                    FROM produtos WHERE btrim(categoria) <> ''
                    GROUP BY upper(btrim(categoria))
                    ON CONFLICT (tipo, nome_normalizado) DO NOTHING;
                    """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opcoes_catalogo");

            migrationBuilder.DropColumn(
                name: "custo",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "preco_venda",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "versao",
                table: "produtos");
        }
    }
}
