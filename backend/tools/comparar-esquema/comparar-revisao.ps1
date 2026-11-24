param(
    [string]$Saida = (Join-Path ([IO.Path]::GetTempPath()) ('zalek-schema-' + [guid]::NewGuid().ToString('N')))
)
$ErrorActionPreference = 'Stop'
$raiz = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$sufixo = [guid]::NewGuid().ToString('N')
$bancoA = "schema_test_a_$sufixo"
$bancoB = "schema_test_b_$sufixo"
$encodingAnterior = $OutputEncoding
function Invoke-PsqlRevisao([string]$Banco, [string]$Sql) {
    $resultado = $Sql | & docker compose -p zalek360-revisao --env-file .env.revisao exec -T db sh -c 'exec psql -U $POSTGRES_USER -d $1 -v ON_ERROR_STOP=1 -q -A -t' sh $Banco
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao executar SQL nos bancos temporários da revisão.' }
    return $resultado
}
Push-Location $raiz
try {
    $OutputEncoding = [Text.UTF8Encoding]::new($false)
    $configRevisao = & docker compose -p zalek360-revisao --env-file .env.revisao config --format json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $configRevisao.name -ne 'zalek360-revisao' -or [int]$configRevisao.services.db.ports[0].published -ne 55432) { throw 'Configuração de revisão inválida.' }
    Remove-Variable configRevisao
    New-Item -ItemType Directory -Path $Saida -ErrorAction Stop | Out-Null
    Invoke-PsqlRevisao postgres "CREATE DATABASE $bancoA; CREATE DATABASE $bancoB;" | Out-Null
    $scriptModelo = @'
set -e
export ConnectionStrings__Zalek360="Host=db;Port=5432;Database=$ZALEK_SCHEMA_BANCO_A;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"
dotnet tool install --tool-path /tmp/ef dotnet-ef --version 8.0.10
/tmp/ef/dotnet-ef database update --project src/Zalek360.Infrastructure --startup-project src/Zalek360.Api
/tmp/ef/dotnet-ef dbcontext script --project src/Zalek360.Infrastructure --startup-project src/Zalek360.Api --no-build --output /saida/modelo.sql
'@
    $scriptModelo | & docker run --rm -i --network zalek360-revisao_default --env-file .env.revisao --env "ZALEK_SCHEMA_BANCO_A=$bancoA" --env Banco__AplicarMigrationsNaInicializacao=false --env Banco__ExecutarSeed=false --env Banco__VerificarEsquema=false --mount "type=bind,source=$raiz\backend,target=/src" --mount "type=bind,source=$Saida,target=/saida" --mount type=volume,source=zalek360-nuget-cache,target=/root/.nuget/packages -w /src mcr.microsoft.com/dotnet/sdk:8.0 sh -c "sed 's/\r$//' | sh"
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao preparar migrations e SQL do modelo.' }
    Invoke-PsqlRevisao $bancoB (Get-Content -Raw -Encoding UTF8 (Join-Path $Saida 'modelo.sql')) | Out-Null
    $catalogo = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'catalogo.sql')
    Invoke-PsqlRevisao $bancoA $catalogo | Set-Content -Encoding UTF8 (Join-Path $Saida 'banco-a.json')
    Invoke-PsqlRevisao $bancoB $catalogo | Set-Content -Encoding UTF8 (Join-Path $Saida 'banco-b.json')
    $resultado = & node (Join-Path $PSScriptRoot 'comparar.mjs') (Join-Path $Saida 'banco-a.json') (Join-Path $Saida 'banco-b.json')
    if ($LASTEXITCODE -ne 0) { throw "Divergências no esquema. Evidências: $Saida" }
    $resultado | Set-Content -Encoding UTF8 (Join-Path $Saida 'comparacao.json')
    $comparacao = ($resultado -join "`n") | ConvertFrom-Json
    [pscustomobject]@{ divergencias = $comparacao.divergencias; totais = $comparacao.totais } | ConvertTo-Json -Depth 6
    Write-Output "Esquemas equivalentes. Evidências: $Saida"
}
finally {
    # Nomes criados nesta execução; nenhum volume ou banco da aplicação é removido.
    foreach ($bancoTemporario in @($bancoA, $bancoB)) {
        if ($bancoTemporario -notmatch '^schema_test_[ab]_[a-f0-9]{32}$') { throw 'Nome temporário inválido.' }
        Invoke-PsqlRevisao postgres "DROP DATABASE IF EXISTS $bancoTemporario WITH (FORCE);" | Out-Null
    }
    $OutputEncoding = $encodingAnterior
    Pop-Location
}
