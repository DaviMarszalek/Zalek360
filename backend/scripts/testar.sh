#!/usr/bin/env bash
# Executa todos os testes (domínio, casos de uso e integração com SQLite em memória).
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet test Zalek360.sln --logger "console;verbosity=normal" "$@"
