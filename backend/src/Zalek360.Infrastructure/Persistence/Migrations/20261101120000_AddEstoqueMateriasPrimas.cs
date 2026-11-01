using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zalek360.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ZalekDbContext))]
[Migration("20261101120000_AddEstoqueMateriasPrimas")]
public partial class AddEstoqueMateriasPrimas : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable(name: "materias_primas", columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
            unidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            quantidade_disponivel = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
            estoque_minimo = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
            ativo = table.Column<bool>(type: "boolean", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("pk_materias_primas", x => x.id);
            table.CheckConstraint("ck_materias_primas_quantidades", "quantidade_disponivel >= 0 AND estoque_minimo >= 0");
        });
        m.CreateTable(name: "compras_materias_primas", columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            materia_prima_id = table.Column<Guid>(type: "uuid", nullable: false),
            quantidade = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
            fornecedor = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
            data_prevista = table.Column<DateOnly>(type: "date", nullable: false),
            situacao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
            criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("pk_compras_materias_primas", x => x.id);
            table.CheckConstraint("ck_compras_materias_primas_quantidade", "quantidade > 0");
            table.ForeignKey("fk_compras_materias_primas_materias_primas_materia_prima_id", x => x.materia_prima_id, "materias_primas", "id", onDelete: ReferentialAction.Restrict);
        });
        m.CreateIndex(name: "ix_materias_primas_nome", table: "materias_primas", column: "nome", unique: true);
        m.CreateIndex(name: "ix_compras_materias_primas_materia_prima_id_situacao_data_prevista", table: "compras_materias_primas", columns: new[] { "materia_prima_id", "situacao", "data_prevista" });

    }
    protected override void Down(MigrationBuilder m)
    {
        m.DropTable(name: "compras_materias_primas");
        m.DropTable(name: "materias_primas");
    }
}
