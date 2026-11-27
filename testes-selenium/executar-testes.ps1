<#
.SYNOPSIS
  Cria o ambiente virtual (se preciso), instala as dependências fixadas e executa a suíte Selenium.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\executar-testes.ps1
  powershell -ExecutionPolicy Bypass -File .\executar-testes.ps1 --navegador chrome --headless
  powershell -ExecutionPolicy Bypass -File .\executar-testes.ps1 -m negativo
#>
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not (Test-Path .venv\Scripts\python.exe)) {
    Write-Host 'Criando ambiente virtual .venv...'
    py -m venv .venv
}
.\.venv\Scripts\python.exe -m pip install --disable-pip-version-check -q -r requirements.txt
.\.venv\Scripts\python.exe -m pytest @args
exit $LASTEXITCODE
