<#
.SYNOPSIS
  Sobe o ambiente de revisão do Zalek360 (frontend 3100, API 5100, PostgreSQL 55432) para a suíte Selenium.

.DESCRIPTION
  Na primeira vez (sem .env.revisao), cria o arquivo a partir de .env.revisao.example com a senha de
  demonstração e gera JWT_CHAVE e POSTGRES_PASSWORD aleatórias, sem exibi-las. Nunca altera o
  ambiente principal (projeto zalek360, portas 3000/5000/5432). Requer o Docker Desktop aberto.
#>
$ErrorActionPreference = 'Stop'
$raiz = Split-Path $PSScriptRoot -Parent
Set-Location $raiz

docker info --format '{{.ServerVersion}}' | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'O Docker não está respondendo. Abra o Docker Desktop e aguarde o Engine running.' }

if (-not (Test-Path .env.revisao)) {
    Write-Host 'Criando .env.revisao a partir do modelo (segredos gerados sem exibição)...'
    $conteudo = (Get-Content .env.revisao.example -Raw -Encoding UTF8) -replace '(?m)^SENHA_USUARIOS_DEMO=.*$', 'SENHA_USUARIOS_DEMO=Zalek@2026'
    [IO.File]::WriteAllText((Join-Path $raiz '.env.revisao'), $conteudo, (New-Object Text.UTF8Encoding $false))
    & (Join-Path $raiz 'infra\implantacao\preparar-segredos.ps1') -ArquivoEnv .env.revisao -GerarChaveJwt -GerarSenhaBanco
}

docker compose -p zalek360-revisao --env-file .env.revisao up --build -d --wait
if ($LASTEXITCODE -ne 0) { throw 'O ambiente de revisão não ficou saudável. Veja: docker compose -p zalek360-revisao logs' }
Write-Host 'Ambiente de revisão no ar: http://localhost:3100'
