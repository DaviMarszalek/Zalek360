param(
    [ValidatePattern('^estoque-check(?:-[a-z0-9-]+)?$')][string]$Projeto = 'estoque-check',
    [int]$PortaBanco = 15439,
    [int]$PortaApi = 15009,
    [string]$Saida = (Join-Path ([IO.Path]::GetTempPath()) ('zalek-schema-' + [guid]::NewGuid().ToString('N'))),
    [switch]$ManterAmbiente
)
$ErrorActionPreference = 'Stop'
$raiz = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$compose = Join-Path $raiz 'docker-compose.yml'
$helper = "$Projeto-modelo"
$imagemSdk = "$Projeto-sdk"
$ambienteCriado = $false
$sucesso = $false
function Invoke-Docker {
    # Funcao simples: parametros nativos como -p e -d nao podem virar parametros comuns do PowerShell.
    $argumentosRecebidos = @($args)
    if ($MyInvocation.ExpectingInput) { $input | & docker @argumentosRecebidos }
    else { & docker @argumentosRecebidos }
    if ($LASTEXITCODE -ne 0) { throw "Docker falhou: $($argumentosRecebidos -join ' ')" }
}
$variaveis = @{
    POSTGRES_PORTA_HOST = "$PortaBanco"; BACKEND_PORTA_HOST = "$PortaApi"; FRONTEND_PORTA_HOST = '13009'
    POSTGRES_DB = 'zalek360'; POSTGRES_USER = 'zalek360'; POSTGRES_PASSWORD = 'zalek360'
    EXECUTAR_SEED = 'false'
}
$anteriores = @{}
foreach ($chave in $variaveis.Keys) { $anteriores[$chave] = [Environment]::GetEnvironmentVariable($chave) }
$encodingAnterior = $OutputEncoding
try {
    $containers = @(Invoke-Docker ps -aq --filter "label=com.docker.compose.project=$Projeto")
    $volumes = @(Invoke-Docker volume ls -q --filter "label=com.docker.compose.project=$Projeto")
    $redes = @(Invoke-Docker network ls -q --filter "label=com.docker.compose.project=$Projeto")
    $imagens = @(Invoke-Docker images -q "$Projeto-*")
    if ($containers.Count -or $volumes.Count -or $redes.Count -or $imagens.Count) {
        throw "O projeto $Projeto ja tem recursos. Use outro nome estoque-check-...; nada foi removido."
    }
    foreach ($chave in $variaveis.Keys) { [Environment]::SetEnvironmentVariable($chave, $variaveis[$chave]) }
    $OutputEncoding = [Text.UTF8Encoding]::new($false)
    $config = Invoke-Docker compose -f $compose -p $Projeto config --format json | ConvertFrom-Json
    if ($config.name -ne $Projeto) { throw 'Projeto efetivo inesperado; nenhuma criacao foi iniciada.' }
    New-Item -ItemType Directory -Path $Saida -ErrorAction Stop | Out-Null
    $ambienteCriado = $true
    Invoke-Docker compose -f $compose -p $Projeto up -d --build db backend
    $pronto = $false
    for ($tentativa = 0; $tentativa -lt 30; $tentativa++) {
        try { $null = Invoke-RestMethod "http://localhost:$PortaApi/health" -TimeoutSec 2; $pronto = $true; break }
        catch { Start-Sleep -Seconds 1 }
    }
    if (!$pronto) {
        Invoke-Docker compose -f $compose -p $Projeto logs --no-color backend
        throw 'API nao iniciou'
    }
    Invoke-Docker compose -f $compose -p $Projeto stop backend
    Invoke-Docker build --target build -t $imagemSdk (Join-Path $raiz 'backend')
    Invoke-Docker run -d --name $helper --label "com.docker.compose.project=$Projeto" -e 'ConnectionStrings__Zalek360=Host=127.0.0.1;Port=1;Database=isolado;Username=isolado;Password=isolado' -e 'Jwt__Chave=chave-temporaria-isolada-comparacao-esquema-2026' -e Banco__AplicarMigrationsNaInicializacao=false -e Banco__ExecutarSeed=false -e Banco__VerificarEsquema=false $imagemSdk sleep infinity
    Invoke-Docker exec $helper dotnet tool install --tool-path /tmp/ef dotnet-ef --version 8.0.10
    Invoke-Docker exec $helper /tmp/ef/dotnet-ef dbcontext script --project src/Zalek360.Infrastructure --startup-project src/Zalek360.Api --configuration Release --no-build --output /tmp/modelo.sql
    Invoke-Docker cp ($helper + ':/tmp/modelo.sql') (Join-Path $Saida 'modelo.sql')
    'CREATE DATABASE modelo_atual;' | Invoke-Docker compose -f $compose -p $Projeto exec -T db psql -U zalek360 -d postgres -v ON_ERROR_STOP=1
    Get-Content -Raw -Encoding UTF8 (Join-Path $Saida 'modelo.sql') | Invoke-Docker compose -f $compose -p $Projeto exec -T db psql -U zalek360 -d modelo_atual -v ON_ERROR_STOP=1
    foreach ($banco in @('zalek360', 'modelo_atual')) {
        $nome = if ($banco -eq 'zalek360') { 'banco-a.json' } else { 'banco-b.json' }
        $catalogo = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'catalogo.sql') | Invoke-Docker compose -f $compose -p $Projeto exec -T db psql -q -A -t -U zalek360 -d $banco -v ON_ERROR_STOP=1
        $catalogo | Set-Content -Encoding UTF8 (Join-Path $Saida $nome)
    }
    $resultado = & node (Join-Path $PSScriptRoot 'comparar.mjs') (Join-Path $Saida 'banco-a.json') (Join-Path $Saida 'banco-b.json')
    $codigoComparacao = $LASTEXITCODE
    $resultado | Set-Content -Encoding UTF8 (Join-Path $Saida 'comparacao.json')
    $resultado
    if ($codigoComparacao -ne 0) { throw "Divergencias no esquema. Evidencias: $Saida" }
    $sucesso = $true
    Write-Output "Esquemas equivalentes. Evidencias: $Saida"
}
finally {
    if ($ambienteCriado -and !$ManterAmbiente) {
        & docker rm -f $helper 2>$null | Out-Null
        & docker compose -f $compose -p $Projeto down --volumes --remove-orphans
        & docker image rm "$Projeto-backend" $imagemSdk 2>$null | Out-Null
    }
    foreach ($chave in $anteriores.Keys) { [Environment]::SetEnvironmentVariable($chave, $anteriores[$chave]) }
    $OutputEncoding = $encodingAnterior
    if ($ambienteCriado -and $ManterAmbiente) { Write-Output "Ambiente mantido: $Projeto. Remova-o ao terminar, conforme README." }
}
if (!$sucesso) { exit 1 }

