#!/usr/bin/env bash
# Verificação OFFLINE do backend (para ambientes sem acesso ao NuGet).
#
#  1. Compila Domain, Application, Infrastructure, Api e Tests contra "stubs" — assemblies que reproduzem apenas as
#     ASSINATURAS públicas de EF Core 8, Npgsql, SQLite, BCrypt, IdentityModel, JwtBearer e Swashbuckle.
#     Detecta erros de tipos, DTOs, chamadas entre camadas, LINQ e Fluent API. NÃO executa o EF/Npgsql reais.
#  2. Executa os testes de Domínio e Aplicação (não dependem de pacotes) com um executor mínimo compatível com xUnit.
#
# Com acesso ao NuGet, prefira o caminho oficial:  ./scripts/testar.sh
# Não faz parte do produto e não é usado pelo Docker.
set -euo pipefail
AQUI="$(cd "$(dirname "$0")" && pwd)"
BACKEND="$(cd "$AQUI/../.." && pwd)"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
FEED="$TMP/feed-vazio"; mkdir -p "$FEED"

echo "==> Compilando stubs"
cp -r "$AQUI/stubs" "$TMP/stubs"
dotnet restore "$TMP/stubs" --source "$FEED" >/dev/null
dotnet build "$TMP/stubs" --no-restore -v q -nologo >/dev/null
STUBS="$TMP/stubs/bin/Debug/net8.0/Stubs.dll"

echo "==> Preparando cópia do código sem PackageReference"
cp -r "$BACKEND/src" "$BACKEND/tests" "$TMP/"
find "$TMP/src" "$TMP/tests" -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
python3 - "$TMP" "$STUBS" "$AQUI/executor/Xunit.cs" <<'PY'
import re, sys, pathlib
raiz, stubs, xunit = sys.argv[1:]
ref = f'<ItemGroup><Reference Include="Stubs"><HintPath>{stubs}</HintPath></Reference></ItemGroup>'
for csproj in pathlib.Path(raiz).rglob("*.csproj"):
    if "stubs" in csproj.parts: continue
    s = csproj.read_text()
    s = re.sub(r'<PackageReference[^>]*/>', '', s)
    s = re.sub(r'<PackageReference[^>]*>.*?</PackageReference>', '', s, flags=re.S)
    extra = ref
    if csproj.name == "Zalek360.Tests.csproj":
        extra += f'<ItemGroup><Compile Include="{xunit}" /></ItemGroup>'
    s = s.replace("</Project>", extra + "\n</Project>")
    csproj.write_text(s)
PY

falhou=0
for p in src/Zalek360.Domain src/Zalek360.Application src/Zalek360.Infrastructure src/Zalek360.Api tests/Zalek360.Tests; do
  nome=$(basename "$p")
  dotnet restore "$TMP/$p" --source "$FEED" >/dev/null 2>&1 || true
  saida=$(dotnet build "$TMP/$p" --no-restore -nologo 2>&1 || true)
  erros=$(echo "$saida" | grep -oE "[0-9]+ Error\(s\)" | tail -1)
  avisos=$(echo "$saida" | grep -oE "[0-9]+ Warning\(s\)" | tail -1)
  printf "    %-28s %s, %s\n" "$nome" "${erros:-? Error(s)}" "${avisos:-? Warning(s)}"
  if [ "${erros%% *}" != "0" ]; then falhou=1; echo "$saida" | grep -E " error " | sort -u | head -20; fi
done

echo "==> Executando testes de Domínio e Aplicação"
mkdir -p "$TMP/executor"
cp "$AQUI/executor/"*.cs "$TMP/executor/"
cat > "$TMP/executor/Executor.csproj" <<XML
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
  <ItemGroup>
    <Compile Include="*.cs" />
    <Compile Include="$BACKEND/tests/Zalek360.Tests/Suporte/*.cs" />
    <Compile Include="$BACKEND/tests/Zalek360.Tests/Dominio/*.cs" />
    <Compile Include="$BACKEND/tests/Zalek360.Tests/Aplicacao/*.cs" />
    <Using Include="Xunit" />
    <ProjectReference Include="$TMP/src/Zalek360.Application/Zalek360.Application.csproj" />
  </ItemGroup>
</Project>
XML
dotnet restore "$TMP/executor" --source "$FEED" >/dev/null
dotnet build "$TMP/executor" --no-restore -v q -nologo >/dev/null
dotnet "$TMP/executor/bin/Debug/net8.0/Executor.dll" || falhou=1

exit $falhou
