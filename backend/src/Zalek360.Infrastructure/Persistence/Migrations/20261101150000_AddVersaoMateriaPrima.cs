using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zalek360.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ZalekDbContext))]
[Migration("20261101150000_AddVersaoMateriaPrima")]
public partial class AddVersaoMateriaPrima : Migration
{
    protected override void Up(MigrationBuilder m) => m.AddColumn<int>(name: "versao", table: "materias_primas", type: "integer", nullable: false, defaultValue: 0);
    protected override void Down(MigrationBuilder m) => m.DropColumn(name: "versao", table: "materias_primas");
}
