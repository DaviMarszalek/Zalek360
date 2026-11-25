<#
.SYNOPSIS
  Teste de restauração do Zalek360: restaura um backup em um banco TEMPORÁRIO e extrai os anexos em uma pasta,
  comparando com os dados atuais. Não altera o banco da aplicação nem os volumes.

.DESCRIPTION
  1. Cria o banco temporário <banco>_restauracao_AAAAMMDD_HHMMSS no PostgreSQL do projeto.
  2. Restaura o dump com pg_restore (--no-owner, --exit-on-error).
  3. Compara a quantidade de linhas de cada tabela e a última migration com o banco atual (consulta somente leitura).
  4. Remove o banco temporário (a menos que use -ManterBanco). Só remove nomes com o sufixo _restauracao_<data>.
  5. Com -Anexos e -PastaAnexos, extrai o pacote na pasta (que deve estar vazia ou não existir) e compara a
     quantidade de arquivos com o volume <projeto>_anexos (montado somente leitura).
  Para substituir os dados da aplicação por um backup, siga o procedimento manual do docs/12-backup-e-restauracao.md.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File infra/backup/restaurar.ps1 -Projeto zalek360 -Dump C:\Backups\Zalek360\banco-zalek360-20261125-200000.dump -Anexos C:\Backups\Zalek360\anexos-zalek360-20261125-200000.tar.gz -PastaAnexos C:\Temp\anexos-restaurados
#>
param(
    [string]$Projeto = 'zalek360',
    [Parameter(Mandatory = $true)][string]$Dump,
    [string]$Anexos,
    [string]$PastaAnexos,
    [switch]$ManterBanco,
    [string]$ImagemFerramentas = 'postgres:16-alpine'
)
$ErrorActionPreference = 'Stop'

function Get-ContainerDoServico([string]$Servico) {
    $nome = docker ps --filter "label=com.docker.compose.project=$Projeto" --filter "label=com.docker.compose.service=$Servico" --format '{{.Names}}'
    if ($LASTEXITCODE -ne 0 -or -not $nome) { throw "Serviço '$Servico' do projeto '$Projeto' não está em execução." }
    return ($nome | Select-Object -First 1)
}
function Invoke-SqlArquivo([string]$Container, [string]$Banco, [string]$Sql) {
    $local = [System.IO.Path]::GetTempFileName()
    $remoto = "/tmp/zalek360-restauracao-$([guid]::NewGuid().ToString('N')).sql"
    try {
        [System.IO.File]::WriteAllText($local, $Sql, (New-Object System.Text.UTF8Encoding $false))
        docker cp $local "${Container}:$remoto" | Out-Null
        $saida = docker exec $Container sh -c "psql -X -q -A -t -v ON_ERROR_STOP=1 -U `$POSTGRES_USER -d $Banco -f $remoto; c=`$?; rm -f $remoto; exit `$c"
        if ($LASTEXITCODE -ne 0) { throw "Consulta falhou no banco $Banco." }
        return $saida
    } finally { Remove-Item -LiteralPath $local -Force -ErrorAction SilentlyContinue }
}

$sqlContagens = @'
SET default_transaction_read_only = on;
SELECT table_name || '|' || (xpath('/row/c/text()', query_to_xml(format('select count(*) as c from %I.%I', table_schema, table_name), false, true, '')))[1]::text
FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' ORDER BY table_name;
SELECT '__ultima_migration|' || max("MigrationId") FROM "__EFMigrationsHistory";
'@

$arquivoDump = (Resolve-Path -LiteralPath $Dump).Path
$db = Get-ContainerDoServico 'db'
$bancoAtual = (docker exec $db printenv POSTGRES_DB).Trim()
$carimbo = Get-Date -Format 'yyyyMMdd_HHmmss'
$bancoTemp = "${bancoAtual}_restauracao_$carimbo"
if ($bancoTemp -notmatch '^[a-z0-9_]+_restauracao_\d{8}_\d{6}$') { throw "Nome de banco temporário inesperado: $bancoTemp" }
$remotoDump = "/tmp/zalek360-restauracao-$carimbo.dump"

$resultado = New-Object 'System.Collections.Generic.List[string]'
$falhou = $false
docker cp $arquivoDump "${db}:$remotoDump" | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Falha ao copiar o dump para o container do banco.' }
try {
    docker exec $db sh -c "createdb -U `$POSTGRES_USER $bancoTemp"
    if ($LASTEXITCODE -ne 0) { throw "Falha ao criar o banco temporário $bancoTemp." }
    try {
        $inicio = Get-Date
        docker exec $db sh -c "pg_restore -U `$POSTGRES_USER -d $bancoTemp --no-owner --exit-on-error $remotoDump"
        if ($LASTEXITCODE -ne 0) { throw 'pg_restore falhou.' }
        $resultado.Add(("Restauração em {0} concluída em {1:N0} s." -f $bancoTemp, ((Get-Date) - $inicio).TotalSeconds))

        $restaurado = @{}; foreach ($l in (Invoke-SqlArquivo $db $bancoTemp $sqlContagens)) { if ($l -match '^(.+)\|(.*)$') { $restaurado[$Matches[1]] = $Matches[2] } }
        $atual = @{}; foreach ($l in (Invoke-SqlArquivo $db $bancoAtual $sqlContagens)) { if ($l -match '^(.+)\|(.*)$') { $atual[$Matches[1]] = $Matches[2] } }
        $resultado.Add('Tabela | restaurado | atual')
        foreach ($t in (@($restaurado.Keys) + @($atual.Keys) | Sort-Object -Unique)) {
            $r = $restaurado[$t]; $a = $atual[$t]
            $marca = if ($r -eq $a) { '' } else { '  <- diferente (dados alterados depois do backup?)' }
            $resultado.Add("$t | $r | $a$marca")
        }
        if (-not $restaurado.ContainsKey('__ultima_migration') -or [int]($restaurado['usuarios']) -le 0) { $falhou = $true; $resultado.Add('FALHA: banco restaurado sem migrations ou sem usuários.') }
    } finally {
        if ($ManterBanco) { $resultado.Add("Banco temporário mantido: $bancoTemp (remova com dropdb quando terminar).") }
        else {
            docker exec $db sh -c "dropdb -U `$POSTGRES_USER --if-exists --force $bancoTemp"
            $resultado.Add("Banco temporário $bancoTemp removido.")
        }
    }
} finally {
    docker exec $db rm -f $remotoDump | Out-Null
}

if ($Anexos) {
    if (-not $PastaAnexos) { throw 'Informe -PastaAnexos para extrair os anexos.' }
    $pasta = [System.IO.Path]::GetFullPath($PastaAnexos)
    if ((Test-Path -LiteralPath $pasta) -and (Get-ChildItem -LiteralPath $pasta -Force | Select-Object -First 1)) { throw "A pasta $pasta não está vazia." }
    New-Item -ItemType Directory -Force -Path $pasta | Out-Null
    $arquivoAnexos = (Resolve-Path -LiteralPath $Anexos).Path
    $dirPacote = Split-Path $arquivoAnexos -Parent; $nomePacote = Split-Path $arquivoAnexos -Leaf
    $extraidos = docker run --rm -v "${dirPacote}:/pacote:ro" -v "${pasta}:/saida" $ImagemFerramentas sh -c "tar xzf /pacote/$nomePacote -C /saida && find /saida -type f | wc -l"
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao extrair os anexos.' }
    $noVolume = docker run --rm -v "${Projeto}_anexos:/dados:ro" $ImagemFerramentas sh -c 'find /dados -type f | wc -l'
    $resultado.Add("Anexos extraídos em ${pasta}: $($extraidos.Trim()) arquivo(s); volume atual: $($noVolume.Trim()).")
    if ([int]$extraidos -ne [int]$noVolume) { $resultado.Add('  <- quantidade diferente (anexos enviados ou removidos depois do backup?)') }
}

$resultado | ForEach-Object { Write-Output $_ }
if ($falhou) { exit 1 }
Write-Output 'Teste de restauração concluído.'
