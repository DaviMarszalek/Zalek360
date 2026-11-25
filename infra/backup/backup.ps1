<#
.SYNOPSIS
  Backup do Zalek360: banco PostgreSQL (pg_dump, formato custom) e arquivos anexados (volume de anexos).

.DESCRIPTION
  Gera em -Destino:
    banco-<projeto>-AAAAMMDD-HHMMSS.dump        pg_dump -Fc do banco da aplicação (feito dentro do container do banco)
    anexos-<projeto>-AAAAMMDD-HHMMSS.tar.gz     conteúdo do volume <projeto>_anexos, montado SOMENTE LEITURA
    manifesto-<projeto>-AAAAMMDD-HHMMSS.txt     tamanhos, SHA-256, entradas do dump e quantidade de arquivos
  Confere o dump com "pg_restore --list" e o pacote de anexos com "tar -tzf" antes de declarar sucesso.
  Não para serviços, não altera dados, não cria nem remove volumes. Nenhuma senha é usada ou exibida
  (o pg_dump roda dentro do container do banco, pelo socket local).

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File infra/backup/backup.ps1 -Projeto zalek360 -Destino C:\Backups\Zalek360
#>
param(
    [string]$Projeto = 'zalek360',
    [Parameter(Mandatory = $true)][string]$Destino,
    [string]$ImagemFerramentas = 'postgres:16-alpine'
)
$ErrorActionPreference = 'Stop'

function Get-ContainerDoServico([string]$Servico) {
    $nome = docker ps --filter "label=com.docker.compose.project=$Projeto" --filter "label=com.docker.compose.service=$Servico" --format '{{.Names}}'
    if ($LASTEXITCODE -ne 0 -or -not $nome) { throw "Serviço '$Servico' do projeto '$Projeto' não está em execução." }
    return ($nome | Select-Object -First 1)
}

$destinoCompleto = [System.IO.Path]::GetFullPath($Destino)
New-Item -ItemType Directory -Force -Path $destinoCompleto | Out-Null
$carimbo = Get-Date -Format 'yyyyMMdd-HHmmss'
$arqDump = Join-Path $destinoCompleto "banco-$Projeto-$carimbo.dump"
$arqAnexos = Join-Path $destinoCompleto "anexos-$Projeto-$carimbo.tar.gz"
$arqManifesto = Join-Path $destinoCompleto "manifesto-$Projeto-$carimbo.txt"
$volumeAnexos = "${Projeto}_anexos"

# 1. Banco: pg_dump dentro do container (sem senha, socket local), copiado para o destino.
$db = Get-ContainerDoServico 'db'
$tmp = "/tmp/zalek360-backup-$carimbo.dump"
docker exec $db sh -c "pg_dump -U `$POSTGRES_USER -d `$POSTGRES_DB -Fc -f $tmp"
if ($LASTEXITCODE -ne 0) { throw 'pg_dump falhou.' }
try {
    docker cp "${db}:$tmp" $arqDump | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao copiar o dump do container.' }
    $entradas = docker exec $db sh -c "pg_restore --list $tmp | grep -c ';'"
    if ($LASTEXITCODE -ne 0 -or [int]$entradas -le 0) { throw 'O dump não pôde ser lido pelo pg_restore.' }
    $tabelas = docker exec $db sh -c "pg_restore --list $tmp | grep -c ' TABLE DATA '"
} finally {
    docker exec $db rm -f $tmp | Out-Null
}

# 2. Anexos: volume montado somente leitura num container descartável.
$existeVolume = docker volume ls --filter "name=^$volumeAnexos$" --format '{{.Name}}'
if ($existeVolume -ne $volumeAnexos) { throw "Volume $volumeAnexos não encontrado." }
$nomeAnexos = Split-Path $arqAnexos -Leaf
docker run --rm -v "${volumeAnexos}:/dados:ro" -v "${destinoCompleto}:/saida" $ImagemFerramentas sh -c "tar czf /saida/$nomeAnexos -C /dados . && tar tzf /saida/$nomeAnexos | grep -vc '/$'" | Tee-Object -Variable quantidadeArquivos | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Falha ao empacotar os anexos.' }

# 3. Manifesto.
$linhas = @(
    "Backup Zalek360 - projeto $Projeto - $carimbo",
    ("banco:  {0}  {1} bytes  sha256={2}  entradas={3}  tabelas_com_dados={4}" -f (Split-Path $arqDump -Leaf), (Get-Item $arqDump).Length, (Get-FileHash $arqDump -Algorithm SHA256).Hash, $entradas, $tabelas),
    ("anexos: {0}  {1} bytes  sha256={2}  arquivos={3}" -f $nomeAnexos, (Get-Item $arqAnexos).Length, (Get-FileHash $arqAnexos -Algorithm SHA256).Hash, ($quantidadeArquivos | Select-Object -Last 1))
)
[System.IO.File]::WriteAllLines($arqManifesto, $linhas, (New-Object System.Text.UTF8Encoding $false))
$linhas | ForEach-Object { Write-Output $_ }
Write-Output "Backup concluído em $destinoCompleto. Copie os três arquivos para outro disco ou nuvem."
