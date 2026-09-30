#!/usr/bin/env bash
# Regenera a migration inicial com a ferramenta oficial do EF Core (dotnet-ef), produzindo também o ModelSnapshot.
#
# Por que existe: a migration 20260928120000_InitialCreate foi escrita manualmente (o ambiente desta entrega não
# tinha acesso ao NuGet). Ela foi validada contra o mapeamento EF e executada em PostgreSQL 16, mas não tem
# ModelSnapshot — e o snapshot é necessário para gerar as PRÓXIMAS migrations com "dotnet ef migrations add".
#
# ATENÇÃO: use em banco NOVO. Um banco criado pela migration manual já registrou "20260928120000_InitialCreate"
# em __EFMigrationsHistory; a migration regenerada terá outro id e o EF tentaria recriar as tabelas.
# Com Docker:  docker compose down -v   (apaga o volume do banco e dos anexos de demonstração)
set -euo pipefail
cd "$(dirname "$0")/.."

if ! command -v dotnet-ef >/dev/null 2>&1 && [ ! -x "$HOME/.dotnet/tools/dotnet-ef" ]; then
  dotnet tool install --global dotnet-ef --version 8.0.10
fi
export PATH="$PATH:$HOME/.dotnet/tools"

MIGRATIONS=src/Zalek360.Infrastructure/Persistence/Migrations
BACKUP=$(mktemp -d)
cp "$MIGRATIONS"/*.cs "$BACKUP"/
echo "Backup da migration manual em: $BACKUP"
rm -f "$MIGRATIONS"/*.cs

dotnet ef migrations add InitialCreate \
  --project src/Zalek360.Infrastructure \
  --startup-project src/Zalek360.Api \
  --output-dir Persistence/Migrations

echo
echo "Migration regenerada em $MIGRATIONS."
echo "Confira o SQL gerado antes de aplicar:"
echo "  dotnet ef migrations script --project src/Zalek360.Infrastructure --startup-project src/Zalek360.Api"
