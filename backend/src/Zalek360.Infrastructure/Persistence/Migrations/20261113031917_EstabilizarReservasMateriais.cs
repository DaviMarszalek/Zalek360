using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zalek360.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EstabilizarReservasMateriais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "quantidade_reservada",
                table: "reservas_materias_primas_pedido",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "versao",
                table: "reservas_materias_primas_pedido",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "controle_materiais",
                table: "pedidos",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "saldo_fisico_apos",
                table: "movimentos_materias_primas",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "saldo_reservado_apos",
                table: "movimentos_materias_primas",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "usuario_id",
                table: "movimentos_materias_primas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "saldo_reservado",
                table: "materias_primas",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "pendencias_ficha_tecnica_pedido",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pedido_id = table.Column<Guid>(type: "uuid", nullable: false),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantidade_produto = table.Column<int>(type: "integer", nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolvida_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pendencias_ficha_tecnica_pedido", x => x.id);
                    table.CheckConstraint("ck_pendencias_ficha_quantidade", "quantidade_produto > 0");
                    table.ForeignKey(
                        name: "fk_pendencias_ficha_tecnica_pedido_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pendencias_ficha_tecnica_pedido_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Reconciliacao anterior aos checks e ao indice unico parcial.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE mvp20_reservas_originais ON COMMIT DROP AS
                SELECT r.*,
                       first_value(id) OVER (PARTITION BY pedido_id, materia_prima_id ORDER BY criada_em, id) AS principal,
                       sum(quantidade) OVER (PARTITION BY pedido_id, materia_prima_id) AS total
                FROM reservas_materias_primas_pedido r WHERE situacao = 'Reservada';
                CREATE TEMP TABLE mvp20_ajustes (
                    materia_prima_id uuid, pedido_id uuid, variacao numeric(12,3)
                ) ON COMMIT DROP;
                UPDATE reservas_materias_primas_pedido
                SET quantidade_reservada = CASE WHEN situacao = 'Reservada' THEN quantidade ELSE 0 END;
                INSERT INTO mvp20_ajustes
                SELECT materia_prima_id, pedido_id, -quantidade
                FROM mvp20_reservas_originais WHERE id <> principal;
                UPDATE reservas_materias_primas_pedido r
                SET quantidade = o.total, quantidade_reservada = o.total
                FROM mvp20_reservas_originais o WHERE r.id = o.id AND o.id = o.principal;
                UPDATE reservas_materias_primas_pedido r
                SET situacao = 'Liberada', quantidade_reservada = 0, versao = r.versao + 1
                FROM mvp20_reservas_originais o WHERE r.id = o.id AND o.id <> o.principal;
                WITH prioridade AS (
                    SELECT r.id, r.quantidade_reservada AS antes,
                           LEAST(r.quantidade, GREATEST(0,
                               m.quantidade_disponivel - COALESCE(
                                   sum(r.quantidade) OVER (
                                       PARTITION BY r.materia_prima_id
                                       ORDER BY p.prazo_entrega, COALESCE(d.data_hora, p.data_criacao), r.criada_em, r.id
                                       ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING
                                   ), 0)
                           )) AS depois
                    FROM reservas_materias_primas_pedido r
                    JOIN materias_primas m ON m.id = r.materia_prima_id
                    JOIN pedidos p ON p.id = r.pedido_id
                    LEFT JOIN decisoes_orcamento d ON d.orcamento_id = p.orcamento_origem_id
                    WHERE r.situacao = 'Reservada'
                ), ajustadas AS (
                    UPDATE reservas_materias_primas_pedido r
                    SET quantidade_reservada = p.depois, versao = r.versao + 1
                    FROM prioridade p WHERE r.id = p.id AND p.antes > p.depois
                    RETURNING r.materia_prima_id, r.pedido_id, p.depois - p.antes AS variacao
                )
                INSERT INTO mvp20_ajustes SELECT * FROM ajustadas;
                UPDATE materias_primas m SET saldo_reservado = COALESCE((
                    SELECT sum(r.quantidade_reservada) FROM reservas_materias_primas_pedido r
                    WHERE r.materia_prima_id = m.id AND r.situacao = 'Reservada'
                ), 0);
                INSERT INTO movimentos_materias_primas
                    (id, materia_prima_id, pedido_id, variacao, tipo, observacao, criado_em,
                     saldo_fisico_apos, saldo_reservado_apos, usuario_id)
                SELECT gen_random_uuid(), a.materia_prima_id, a.pedido_id, a.variacao,
                       'LiberacaoReserva', 'Reconciliação MVP 2 — Fase 2.0', CURRENT_TIMESTAMP,
                       m.quantidade_disponivel, m.saldo_reservado, NULL
                FROM mvp20_ajustes a JOIN materias_primas m ON m.id = a.materia_prima_id
                WHERE a.variacao <> 0;
                UPDATE pedidos p SET controle_materiais = EXISTS (
                    SELECT 1 FROM reservas_materias_primas_pedido r WHERE r.pedido_id = p.id
                );
                ALTER TABLE materias_primas ALTER COLUMN saldo_reservado DROP DEFAULT;
                ALTER TABLE reservas_materias_primas_pedido ALTER COLUMN quantidade_reservada DROP DEFAULT;
                ALTER TABLE reservas_materias_primas_pedido ALTER COLUMN versao DROP DEFAULT;
                """);


            migrationBuilder.CreateIndex(
                name: "ux_reservas_materias_primas_pedido_pedido_id_materia_prima_id",
                table: "reservas_materias_primas_pedido",
                columns: new[] { "pedido_id", "materia_prima_id" },
                unique: true,
                filter: "situacao = 'Reservada'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reservas_materias_primas_reservada",
                table: "reservas_materias_primas_pedido",
                sql: "CAST(quantidade_reservada AS NUMERIC) >= 0 AND CAST(quantidade_reservada AS NUMERIC) <= CAST(quantidade AS NUMERIC)");

            migrationBuilder.CreateIndex(
                name: "ix_movimentos_materias_primas_usuario_id",
                table: "movimentos_materias_primas",
                column: "usuario_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_materias_primas_reservado",
                table: "materias_primas",
                sql: "CAST(saldo_reservado AS NUMERIC) >= 0 AND CAST(saldo_reservado AS NUMERIC) <= CAST(quantidade_disponivel AS NUMERIC)");

            migrationBuilder.CreateIndex(
                name: "ix_pendencias_ficha_tecnica_pedido_produto_id",
                table: "pendencias_ficha_tecnica_pedido",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "ux_pendencias_ficha_tecnica_pedido_pedido_id_produto_id",
                table: "pendencias_ficha_tecnica_pedido",
                columns: new[] { "pedido_id", "produto_id" },
                unique: true,
                filter: "resolvida_em IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_movimentos_materias_primas_usuario_id",
                table: "movimentos_materias_primas",
                column: "usuario_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_movimentos_materias_primas_usuario_id",
                table: "movimentos_materias_primas");

            migrationBuilder.DropTable(
                name: "pendencias_ficha_tecnica_pedido");

            migrationBuilder.DropIndex(
                name: "ux_reservas_materias_primas_pedido_pedido_id_materia_prima_id",
                table: "reservas_materias_primas_pedido");

            migrationBuilder.DropCheckConstraint(
                name: "ck_reservas_materias_primas_reservada",
                table: "reservas_materias_primas_pedido");

            migrationBuilder.DropIndex(
                name: "ix_movimentos_materias_primas_usuario_id",
                table: "movimentos_materias_primas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_materias_primas_reservado",
                table: "materias_primas");

            migrationBuilder.DropColumn(
                name: "quantidade_reservada",
                table: "reservas_materias_primas_pedido");

            migrationBuilder.DropColumn(
                name: "versao",
                table: "reservas_materias_primas_pedido");

            migrationBuilder.DropColumn(
                name: "controle_materiais",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "saldo_fisico_apos",
                table: "movimentos_materias_primas");

            migrationBuilder.DropColumn(
                name: "saldo_reservado_apos",
                table: "movimentos_materias_primas");

            migrationBuilder.DropColumn(
                name: "usuario_id",
                table: "movimentos_materias_primas");

            migrationBuilder.DropColumn(
                name: "saldo_reservado",
                table: "materias_primas");
        }
    }
}
