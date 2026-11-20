using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zalek360.Infrastructure.Persistence.Migrations;

/// <summary>Alinha os objetos existentes ao modelo, preservando as cinco migrations publicadas e todos os dados.</summary>
[DbContext(typeof(ZalekDbContext))]
[Migration("20261112120000_AlinharEsquemaComModelo")]
public partial class AlinharEsquemaComModelo : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.Sql("ALTER TABLE IF EXISTS public.materias_primas ALTER COLUMN versao DROP DEFAULT;");
        RenomearFk(m, "compras_materias_primas", "fk_compras_materias_primas_materias_primas_materia_prima_id", "fk_compras_materias_primas_materia_prima_id");
        RenomearFk(m, "fichas_tecnicas_materias_primas", "fk_fichas_tecnicas_materias_primas_produtos_produto_id", "fk_fichas_tecnicas_materias_primas_produto_id");
        RenomearFk(m, "fichas_tecnicas_materias_primas", "fk_fichas_tecnicas_materias_primas_materias_primas_materia_prim", "fk_fichas_tecnicas_materias_primas_materia_prima_id");
        RenomearFk(m, "movimentos_materias_primas", "fk_movimentos_materias_primas_materias_primas_materia_prima_id", "fk_movimentos_materias_primas_materia_prima_id");
        RenomearFk(m, "movimentos_materias_primas", "fk_movimentos_materias_primas_pedidos_pedido_id", "fk_movimentos_materias_primas_pedido_id");
        RenomearFk(m, "reservas_materias_primas_pedido", "fk_reservas_materias_primas_pedido_pedidos_pedido_id", "fk_reservas_materias_primas_pedido_pedido_id");
        RenomearFk(m, "reservas_materias_primas_pedido", "fk_reservas_materias_primas_pedido_materias_primas_materia_prim", "fk_reservas_materias_primas_pedido_materia_prima_id");
        RenomearIndice(m, "ix_materias_primas_nome", "ux_materias_primas_nome");
        RenomearIndice(m, "ix_fichas_tecnicas_materias_primas_produto_id_materia_prima_id", "ux_fichas_tecnicas_materias_primas_produto_id_materia_prima_id");
        RenomearIndice(m, "ix_compras_materias_primas_materia_prima_id_situacao_data_previ", "ix_compras_materias_primas_materia_prima_id_situacao_d_80534e86");
        RenomearIndice(m, "ix_reservas_materias_primas_pedido_pedido_id_materia_prima_id_s", "ix_reservas_materias_primas_pedido_pedido_id_materia_p_da9f39ed");
        m.Sql("""
            CREATE INDEX IF NOT EXISTS ix_fichas_tecnicas_materias_primas_materia_prima_id
                ON public.fichas_tecnicas_materias_primas (materia_prima_id);
            CREATE INDEX IF NOT EXISTS ix_reservas_materias_primas_pedido_materia_prima_id
                ON public.reservas_materias_primas_pedido (materia_prima_id);
            """);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("""
            DROP INDEX IF EXISTS public.ix_reservas_materias_primas_pedido_materia_prima_id;
            DROP INDEX IF EXISTS public.ix_fichas_tecnicas_materias_primas_materia_prima_id;
            """);
        RenomearIndice(m, "ix_reservas_materias_primas_pedido_pedido_id_materia_p_da9f39ed", "ix_reservas_materias_primas_pedido_pedido_id_materia_prima_id_s");
        RenomearIndice(m, "ix_compras_materias_primas_materia_prima_id_situacao_d_80534e86", "ix_compras_materias_primas_materia_prima_id_situacao_data_previ");
        RenomearIndice(m, "ux_fichas_tecnicas_materias_primas_produto_id_materia_prima_id", "ix_fichas_tecnicas_materias_primas_produto_id_materia_prima_id");
        RenomearIndice(m, "ux_materias_primas_nome", "ix_materias_primas_nome");
        RenomearFk(m, "reservas_materias_primas_pedido", "fk_reservas_materias_primas_pedido_materia_prima_id", "fk_reservas_materias_primas_pedido_materias_primas_materia_prim");
        RenomearFk(m, "reservas_materias_primas_pedido", "fk_reservas_materias_primas_pedido_pedido_id", "fk_reservas_materias_primas_pedido_pedidos_pedido_id");
        RenomearFk(m, "movimentos_materias_primas", "fk_movimentos_materias_primas_pedido_id", "fk_movimentos_materias_primas_pedidos_pedido_id");
        RenomearFk(m, "movimentos_materias_primas", "fk_movimentos_materias_primas_materia_prima_id", "fk_movimentos_materias_primas_materias_primas_materia_prima_id");
        RenomearFk(m, "fichas_tecnicas_materias_primas", "fk_fichas_tecnicas_materias_primas_materia_prima_id", "fk_fichas_tecnicas_materias_primas_materias_primas_materia_prim");
        RenomearFk(m, "fichas_tecnicas_materias_primas", "fk_fichas_tecnicas_materias_primas_produto_id", "fk_fichas_tecnicas_materias_primas_produtos_produto_id");
        RenomearFk(m, "compras_materias_primas", "fk_compras_materias_primas_materia_prima_id", "fk_compras_materias_primas_materias_primas_materia_prima_id");
        m.Sql("ALTER TABLE IF EXISTS public.materias_primas ALTER COLUMN versao SET DEFAULT 0;");
    }

    // Os nomes são constantes desta migration. IF EXISTS permite repetir Up/Down sem alterar dados.
    private static void RenomearIndice(MigrationBuilder m, string origem, string destino) =>
        m.Sql($"""ALTER INDEX IF EXISTS public."{origem}" RENAME TO "{destino}";""");

    private static void RenomearFk(MigrationBuilder m, string tabela, string origem, string destino) =>
        m.Sql($"""
            DO $renomear$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conrelid = 'public."{tabela}"'::regclass AND conname = '{origem}'
                ) THEN
                    ALTER TABLE public."{tabela}" RENAME CONSTRAINT "{origem}" TO "{destino}";
                END IF;
            END $renomear$;
            """);
}

