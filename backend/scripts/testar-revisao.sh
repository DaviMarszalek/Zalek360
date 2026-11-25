#!/bin/sh
# Testes do backend com PostgreSQL real, dentro do container .NET 8 na rede do ambiente de revisão.
# Uso (ver AGENTS.md): docker run ... --env-file .env.revisao ... sh -c "sed 's/\r$//' scripts/testar-revisao.sh > /tmp/t.sh && sh /tmp/t.sh"
# - Monta ZALEK_TEST_POSTGRES a partir de POSTGRES_USER/POSTGRES_PASSWORD do --env-file, sem imprimir os valores.
# - Não depende da entrada padrão (o PowerShell 5.1 pode prefixar BOM e quebrar o primeiro comando).
# - Falha se algum teste falhar, se o total não for encontrado ou se houver QUALQUER teste ignorado.
set -u
: "${POSTGRES_USER:?Defina POSTGRES_USER no --env-file do ambiente de revisão.}"
: "${POSTGRES_PASSWORD:?Defina POSTGRES_PASSWORD no --env-file do ambiente de revisão.}"
export ZALEK_TEST_POSTGRES="Host=db;Port=5432;Database=postgres;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"

saida=/tmp/zalek360-testes.log
codigo_arquivo=/tmp/zalek360-testes.codigo
{ dotnet test Zalek360.sln --nologo --logger "console;verbosity=normal" "$@"; echo $? > "$codigo_arquivo"; } 2>&1 | tee "$saida"
codigo=$(cat "$codigo_arquivo")

total=$(grep -E 'Total tests:' "$saida" | tail -1 | grep -oE '[0-9]+' | tail -1)
ignorados=$(grep -E '^[[:space:]]*Skipped:' "$saida" | tail -1 | grep -oE '[0-9]+' | tail -1)

if [ "$codigo" -ne 0 ]; then echo "VALIDAÇÃO REPROVADA: dotnet test terminou com código $codigo."; exit "$codigo"; fi
if [ -z "$total" ]; then echo "VALIDAÇÃO REPROVADA: total de testes não encontrado na saída."; exit 2; fi
if [ "${ignorados:-0}" -ne 0 ]; then echo "VALIDAÇÃO REPROVADA: $ignorados teste(s) ignorado(s). Confira ZALEK_TEST_POSTGRES."; exit 3; fi
echo "VALIDAÇÃO APROVADA: $total testes, 0 falhas, 0 ignorados."
