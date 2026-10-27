using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zalek360.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ZalekDbContext))]
[Migration("20261026120000_AddInscricaoEstadual")]
public partial class AddInscricaoEstadual : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "inscricao_estadual",
            table: "clientes",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "inscricao_estadual",
            table: "clientes");
    }
}
