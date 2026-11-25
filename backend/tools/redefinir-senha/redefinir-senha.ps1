<#
.SYNOPSIS
  Redefine a senha de um usuário do Zalek360 pelo e-mail, executando a ferramenta dentro do container da API.

.DESCRIPTION
  Roda "dotnet Zalek360.Api.dll redefinir-senha <e-mail>" no serviço backend do projeto Compose informado.
  A senha nova é pedida duas vezes, SEM ECO, no próprio terminal; ela nunca é passada por argumento,
  gravada em arquivo ou registrada em log. Usa o mesmo hash BCrypt e a mesma conexão da aplicação.
  Execute na raiz do repositório (pasta do docker-compose.yml).

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File backend/tools/redefinir-senha/redefinir-senha.ps1 -Email admin@zalekpersonalizados.com.br

.EXAMPLE
  # Ambiente de revisão
  powershell -ExecutionPolicy Bypass -File backend/tools/redefinir-senha/redefinir-senha.ps1 -Email pessoa@example.test -Projeto zalek360-revisao -ArquivoEnv .env.revisao
#>
param(
    [Parameter(Mandatory = $true)][string]$Email,
    [string]$Projeto = 'zalek360',
    [string]$ArquivoEnv = '.env'
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $ArquivoEnv)) { throw "Arquivo $ArquivoEnv não encontrado. Execute na raiz do repositório." }
docker compose -p $Projeto --env-file $ArquivoEnv exec backend dotnet Zalek360.Api.dll redefinir-senha $Email
exit $LASTEXITCODE
