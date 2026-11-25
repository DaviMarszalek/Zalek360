using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Zalek360.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ModelosEEnviosProposta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "envios_proposta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    orcamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destinatario = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    assunto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    situacao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    concluido_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_envios_proposta", x => x.id);
                    table.ForeignKey(
                        name: "fk_envios_proposta_orcamento_id",
                        column: x => x.orcamento_id,
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_envios_proposta_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "modelos_mensagem_proposta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    canal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assunto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    mensagem = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: false),
                    versao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modelos_mensagem_proposta", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "modelos_mensagem_proposta",
                columns: new[] { "id", "assunto", "canal", "mensagem", "versao" },
                values: new object[,]
                {
                    { new Guid("d0000000-0000-0000-0000-000000000001"), "Proposta {numero} — {empresa}", "Email", "Olá, {cliente}!\n\nSegue em anexo a proposta {numero}, no valor de {valorTotal}, válida até {validade}. Prazo estimado: {prazo} dias úteis após a aprovação.\n\nFicamos à disposição.\n{empresa}", 1 },
                    { new Guid("d0000000-0000-0000-0000-000000000002"), "", "WhatsApp", "Olá, {cliente}! Segue a proposta {numero}, no valor de {valorTotal}, válida até {validade}. Prazo estimado: {prazo} dias úteis após a aprovação.\n{empresa}", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_envios_proposta_orcamento_id_criado_em",
                table: "envios_proposta",
                columns: new[] { "orcamento_id", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_envios_proposta_usuario_id",
                table: "envios_proposta",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_modelos_mensagem_proposta_canal",
                table: "modelos_mensagem_proposta",
                column: "canal",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "envios_proposta");

            migrationBuilder.DropTable(
                name: "modelos_mensagem_proposta");
        }
    }
}
