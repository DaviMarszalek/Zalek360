<#
.SYNOPSIS
  Cria ou atualiza os segredos de um arquivo de ambiente do Docker Compose (.env, .env.revisao) sem exibi-los.

.DESCRIPTION
  -GerarChaveJwt     Grava JWT_CHAVE aleatória (64 caracteres) se estiver ausente ou vazia. Com -Substituir, troca a atual.
  -TrocarSenhaBanco  Gera uma senha nova (43 caracteres), executa ALTER ROLE CURRENT_USER dentro do container do banco
                     (-ContainerBanco) e só depois grava POSTGRES_PASSWORD no arquivo. Use em banco que já existe.
  -GerarSenhaBanco   Grava POSTGRES_PASSWORD nova SEM alterar o banco. Somente para volume novo, antes da primeira subida.

  Nenhum valor é impresso, passado por argumento de linha de comando ou registrado em log:
  - a senha nova chega ao psql por variável de ambiente (docker exec -e NOVA_SENHA_BANCO) e por \getenv;
  - o arquivo novo é gravado como <arquivo>.novo antes do ALTER ROLE e substitui o atual somente se o ALTER der certo;
  - o arquivo anterior é guardado como <arquivo>.anterior (ignorado pelo Git).
  Grava UTF-8 sem BOM. Requer Docker e o container do banco em execução para -TrocarSenhaBanco.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File infra/implantacao/preparar-segredos.ps1 -ArquivoEnv .env -ContainerBanco zalek360-db-1 -GerarChaveJwt -TrocarSenhaBanco
#>
param(
    [Parameter(Mandatory = $true)][string]$ArquivoEnv,
    [string]$ContainerBanco,
    [switch]$GerarChaveJwt,
    [switch]$TrocarSenhaBanco,
    [switch]$GerarSenhaBanco,
    [switch]$Substituir
)
$ErrorActionPreference = 'Stop'

if (-not ($GerarChaveJwt -or $TrocarSenhaBanco -or $GerarSenhaBanco)) { throw 'Informe ao menos uma ação: -GerarChaveJwt, -TrocarSenhaBanco ou -GerarSenhaBanco.' }
if ($TrocarSenhaBanco -and $GerarSenhaBanco) { throw 'Use -TrocarSenhaBanco (banco existente) OU -GerarSenhaBanco (volume novo), não os dois.' }
if ($TrocarSenhaBanco -and -not $ContainerBanco) { throw 'Informe -ContainerBanco (ex.: zalek360-db-1) para trocar a senha do banco.' }

function New-SegredoAleatorio([int]$Bytes) {
    $dados = New-Object byte[] $Bytes
    $gerador = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $gerador.GetBytes($dados) } finally { $gerador.Dispose() }
    return ([Convert]::ToBase64String($dados)).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}
function Get-ValorEnv([System.Collections.Generic.List[string]]$Linhas, [string]$Nome) {
    foreach ($linha in $Linhas) { if ($linha -match "^\s*$([regex]::Escape($Nome))=(.*)$") { return $Matches[1].Trim() } }
    return $null
}
function Set-ValorEnv([System.Collections.Generic.List[string]]$Linhas, [string]$Nome, [string]$Valor) {
    for ($i = 0; $i -lt $Linhas.Count; $i++) {
        if ($Linhas[$i] -match "^\s*$([regex]::Escape($Nome))=") { $Linhas[$i] = "$Nome=$Valor"; return }
    }
    $Linhas.Add("$Nome=$Valor")
}
function Write-ArquivoSemBom([string]$Caminho, [System.Collections.Generic.List[string]]$Linhas) {
    [System.IO.File]::WriteAllText($Caminho, (($Linhas -join "`n") + "`n"), (New-Object System.Text.UTF8Encoding $false))
}

$caminho = [System.IO.Path]::GetFullPath((Join-Path (Get-Location).Path $ArquivoEnv))
$linhas = New-Object 'System.Collections.Generic.List[string]'
if (Test-Path -LiteralPath $caminho) {
    foreach ($l in [System.IO.File]::ReadAllLines($caminho, [System.Text.Encoding]::UTF8)) { $linhas.Add($l.TrimStart([char]0xFEFF)) }
} else {
    $linhas.Add('# Gerado por infra/implantacao/preparar-segredos.ps1. Nunca versione este arquivo.')
}
$mensagens = New-Object 'System.Collections.Generic.List[string]'

if ($GerarChaveJwt) {
    if ((Get-ValorEnv $linhas 'JWT_CHAVE') -and -not $Substituir) { $mensagens.Add('JWT_CHAVE mantida (já definida; use -Substituir para trocar).') }
    else { Set-ValorEnv $linhas 'JWT_CHAVE' (New-SegredoAleatorio 48); $mensagens.Add('JWT_CHAVE gerada.') }
}
if ($GerarSenhaBanco) {
    if ((Get-ValorEnv $linhas 'POSTGRES_PASSWORD') -and -not $Substituir) { $mensagens.Add('POSTGRES_PASSWORD mantida (já definida; use -Substituir para trocar).') }
    else { Set-ValorEnv $linhas 'POSTGRES_PASSWORD' (New-SegredoAleatorio 32); $mensagens.Add('POSTGRES_PASSWORD gerada (sem alterar banco existente).') }
}

$novaSenhaBanco = $null
if ($TrocarSenhaBanco) {
    $ativo = docker ps --filter "name=^$ContainerBanco$" --format '{{.Names}}'
    if ($LASTEXITCODE -ne 0 -or $ativo -ne $ContainerBanco) { throw "O container $ContainerBanco não está em execução. Nada foi alterado." }
    # Usuário e banco precisam continuar iguais aos do volume existente: lê do container quando faltarem no arquivo.
    foreach ($nome in 'POSTGRES_USER', 'POSTGRES_DB') {
        if (-not (Get-ValorEnv $linhas $nome)) {
            $valor = docker exec $ContainerBanco printenv $nome
            if ($LASTEXITCODE -ne 0 -or -not $valor) { throw "Não foi possível ler $nome do container. Nada foi alterado." }
            Set-ValorEnv $linhas $nome $valor.Trim()
        }
    }
    $novaSenhaBanco = New-SegredoAleatorio 32
    Set-ValorEnv $linhas 'POSTGRES_PASSWORD' $novaSenhaBanco
}

# Grava primeiro o arquivo novo; ele só substitui o atual depois do ALTER ROLE.
$novo = "$caminho.novo"
Write-ArquivoSemBom $novo $linhas

if ($TrocarSenhaBanco) {
    $sql = [System.IO.Path]::GetTempFileName()
    try {
        [System.IO.File]::WriteAllText($sql, "\set ON_ERROR_STOP on`n\getenv nova NOVA_SENHA_BANCO`nALTER ROLE CURRENT_USER WITH PASSWORD :'nova';`n", (New-Object System.Text.UTF8Encoding $false))
        docker cp $sql "${ContainerBanco}:/tmp/zalek360-alterar-senha.sql" | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Falha ao copiar o script SQL para o container.' }
        $env:NOVA_SENHA_BANCO = $novaSenhaBanco
        try {
            docker exec -e NOVA_SENHA_BANCO $ContainerBanco sh -c 'psql -X -q -v ON_ERROR_STOP=1 -U $POSTGRES_USER -d $POSTGRES_DB -f /tmp/zalek360-alterar-senha.sql; c=$?; rm -f /tmp/zalek360-alterar-senha.sql; exit $c'
            $codigo = $LASTEXITCODE
        } finally { Remove-Item Env:NOVA_SENHA_BANCO -ErrorAction SilentlyContinue }
        if ($codigo -ne 0) { throw 'O ALTER ROLE falhou.' }
        $mensagens.Add('Senha do PostgreSQL alterada no banco.')
    } catch {
        Remove-Item -LiteralPath $novo -Force -ErrorAction SilentlyContinue
        throw "Falha ao trocar a senha do banco; o arquivo de ambiente NÃO foi modificado. Detalhe: $($_.Exception.Message)"
    } finally {
        Remove-Item -LiteralPath $sql -Force -ErrorAction SilentlyContinue
    }
}

try {
    if (Test-Path -LiteralPath $caminho) { Copy-Item -LiteralPath $caminho -Destination "$caminho.anterior" -Force }
    Move-Item -LiteralPath $novo -Destination $caminho -Force
} catch {
    throw "O arquivo $novo contém os valores novos, mas não substituiu $caminho. Renomeie-o manualmente para $ArquivoEnv antes de recriar a API."
}
$mensagens.Add("Arquivo $ArquivoEnv atualizado (anterior guardado como $ArquivoEnv.anterior, se existia).")
$mensagens | ForEach-Object { Write-Output $_ }
if ($TrocarSenhaBanco) { Write-Output 'Próximo passo: recrie a API (docker compose ... up -d --build --no-deps backend frontend).' }
