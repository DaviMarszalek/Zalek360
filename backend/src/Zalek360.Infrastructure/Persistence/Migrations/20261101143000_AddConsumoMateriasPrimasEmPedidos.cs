using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zalek360.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ZalekDbContext))]
[Migration("20261101143000_AddConsumoMateriasPrimasEmPedidos")]
public partial class AddConsumoMateriasPrimasEmPedidos : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable(name: "fichas_tecnicas_materias_primas", columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            produto_id = table.Column<Guid>(type: "uuid", nullable: false),
            materia_prima_id = table.Column<Guid>(type: "uuid", nullable: false),
            quantidade_por_unidade = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("pk_fichas_tecnicas_materias_primas", x => x.id);
            table.CheckConstraint("ck_fichas_tecnicas_quantidade", "quantidade_por_unidade > 0");
            table.ForeignKey("fk_fichas_tecnicas_materias_primas_produtos_produto_id", x => x.produto_id, "produtos", "id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("fk_fichas_tecnicas_materias_primas_materias_primas_materia_prima_id", x => x.materia_prima_id, "materias_primas", "id", onDelete: ReferentialAction.Restrict);
        });
        m.CreateTable(name: "reservas_materias_primas_pedido", columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            pedido_id = table.Column<Guid>(type: "uuid", nullable: false),
            materia_prima_id = table.Column<Guid>(type: "uuid", nullable: false),
            quantidade = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
            situacao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
            criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("pk_reservas_materias_primas_pedido", x => x.id);
            table.CheckConstraint("ck_reservas_materias_primas_quantidade", "quantidade > 0");
            table.ForeignKey("fk_reservas_materias_primas_pedido_pedidos_pedido_id", x => x.pedido_id, "pedidos", "id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("fk_reservas_materias_primas_pedido_materias_primas_materia_prima_id", x => x.materia_prima_id, "materias_primas", "id", onDelete: ReferentialAction.Restrict);
        });
        m.CreateTable(name: "movimentos_materias_primas", columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            materia_prima_id = table.Column<Guid>(type: "uuid", nullable: false),
            pedido_id = table.Column<Guid>(type: "uuid", nullable: true),
            variacao = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
            tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
            observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
            criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("pk_movimentos_materias_primas", x => x.id);
            table.CheckConstraint("ck_movimentos_materias_primas_variacao", "variacao <> 0");
            table.ForeignKey("fk_movimentos_materias_primas_materias_primas_materia_prima_id", x => x.materia_prima_id, "materias_primas", "id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("fk_movimentos_materias_primas_pedidos_pedido_id", x => x.pedido_id, "pedidos", "id", onDelete: ReferentialAction.Restrict);
        });
        m.CreateIndex(name: "ix_fichas_tecnicas_materias_primas_produto_id_materia_prima_id", table: "fichas_tecnicas_materias_primas", columns: new[] { "produto_id", "materia_prima_id" }, unique: true);
        m.CreateIndex(name: "ix_reservas_materias_primas_pedido_pedido_id_materia_prima_id_situacao", table: "reservas_materias_primas_pedido", columns: new[] { "pedido_id", "materia_prima_id", "situacao" });
        m.CreateIndex(name: "ix_movimentos_materias_primas_materia_prima_id_criado_em", table: "movimentos_materias_primas", columns: new[] { "materia_prima_id", "criado_em" });
        m.CreateIndex(name: "ix_movimentos_materias_primas_pedido_id", table: "movimentos_materias_primas", column: "pedido_id");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropTable(name: "movimentos_materias_primas");
        m.DropTable(name: "reservas_materias_primas_pedido");
        m.DropTable(name: "fichas_tecnicas_materias_primas");
    }
}
