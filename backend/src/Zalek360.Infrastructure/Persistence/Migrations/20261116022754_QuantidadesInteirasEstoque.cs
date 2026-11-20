using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zalek360.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QuantidadesInteirasEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_reservas_materias_primas_quantidade",
                table: "reservas_materias_primas_pedido");

            migrationBuilder.DropCheckConstraint(
                name: "ck_reservas_materias_primas_reservada",
                table: "reservas_materias_primas_pedido");

            migrationBuilder.DropCheckConstraint(
                name: "ck_movimentos_materias_primas_variacao",
                table: "movimentos_materias_primas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_materias_primas_quantidades",
                table: "materias_primas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fichas_tecnicas_quantidade",
                table: "fichas_tecnicas_materias_primas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_materias_primas_quantidade",
                table: "compras_materias_primas");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reservas_materias_primas_quantidade",
                table: "reservas_materias_primas_pedido",
                sql: "quantidade > 0 AND quantidade = CAST(quantidade AS INTEGER)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reservas_materias_primas_reservada",
                table: "reservas_materias_primas_pedido",
                sql: "CAST(quantidade_reservada AS NUMERIC) >= 0 AND CAST(quantidade_reservada AS NUMERIC) <= CAST(quantidade AS NUMERIC) AND quantidade_reservada = CAST(quantidade_reservada AS INTEGER)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_movimentos_materias_primas_saldos_inteiros",
                table: "movimentos_materias_primas",
                sql: "(saldo_fisico_apos IS NULL OR saldo_fisico_apos = CAST(saldo_fisico_apos AS INTEGER)) AND (saldo_reservado_apos IS NULL OR saldo_reservado_apos = CAST(saldo_reservado_apos AS INTEGER))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_movimentos_materias_primas_variacao",
                table: "movimentos_materias_primas",
                sql: "variacao <> 0 AND variacao = CAST(variacao AS INTEGER)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_materias_primas_quantidades",
                table: "materias_primas",
                sql: "quantidade_disponivel >= 0 AND estoque_minimo >= 0 AND quantidade_disponivel = CAST(quantidade_disponivel AS INTEGER) AND estoque_minimo = CAST(estoque_minimo AS INTEGER) AND saldo_reservado = CAST(saldo_reservado AS INTEGER)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fichas_tecnicas_quantidade",
                table: "fichas_tecnicas_materias_primas",
                sql: "quantidade_por_unidade > 0 AND quantidade_por_unidade = CAST(quantidade_por_unidade AS INTEGER)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_materias_primas_quantidade",
                table: "compras_materias_primas",
                sql: "quantidade > 0 AND quantidade = CAST(quantidade AS INTEGER)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_reservas_materias_primas_quantidade",
                table: "reservas_materias_primas_pedido");

            migrationBuilder.DropCheckConstraint(
                name: "ck_reservas_materias_primas_reservada",
                table: "reservas_materias_primas_pedido");

            migrationBuilder.DropCheckConstraint(
                name: "ck_movimentos_materias_primas_saldos_inteiros",
                table: "movimentos_materias_primas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_movimentos_materias_primas_variacao",
                table: "movimentos_materias_primas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_materias_primas_quantidades",
                table: "materias_primas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fichas_tecnicas_quantidade",
                table: "fichas_tecnicas_materias_primas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compras_materias_primas_quantidade",
                table: "compras_materias_primas");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reservas_materias_primas_quantidade",
                table: "reservas_materias_primas_pedido",
                sql: "quantidade > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reservas_materias_primas_reservada",
                table: "reservas_materias_primas_pedido",
                sql: "CAST(quantidade_reservada AS NUMERIC) >= 0 AND CAST(quantidade_reservada AS NUMERIC) <= CAST(quantidade AS NUMERIC)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_movimentos_materias_primas_variacao",
                table: "movimentos_materias_primas",
                sql: "variacao <> 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_materias_primas_quantidades",
                table: "materias_primas",
                sql: "quantidade_disponivel >= 0 AND estoque_minimo >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_fichas_tecnicas_quantidade",
                table: "fichas_tecnicas_materias_primas",
                sql: "quantidade_por_unidade > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_compras_materias_primas_quantidade",
                table: "compras_materias_primas",
                sql: "quantidade > 0");
        }
    }
}
