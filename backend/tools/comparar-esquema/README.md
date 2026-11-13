# Comparação de esquema antes de migrations

Requer Docker/Compose, PowerShell 5.1+ e Node.js 18+. Não precisa de SDK .NET no host.

Na raiz do repositório:

```powershell
powershell -ExecutionPolicy Bypass -File backend/tools/comparar-esquema/comparar.ps1
```

O script cria o projeto isolado `estoque-check` (PostgreSQL na porta 15439 e API na 15009), com volumes novos. Recusa usar um projeto que já tenha recursos e nunca acessa `zalek360`. Para executar em paralelo, use `-Projeto estoque-check-outro -PortaBanco 15440 -PortaApi 15010`.

Banco A: migrations aplicadas pela API, sem seed. Banco B: SQL de `dotnet ef dbcontext script` do código atual. A API é parada antes da comparação. As consultas aos catálogos usam `BEGIN READ ONLY`; somente a preparação escreve nos bancos descartáveis.

Compara tabelas, colunas como conjunto (tipos, nulidade, defaults e demais atributos), PKs, FKs com ações de exclusão/atualização, índices (nome, colunas, unicidade, filtro), checks e definições das sequências. Ignora `__EFMigrationsHistory`, a posição física das colunas e o valor corrente das sequências. Ordem distinta em `clientes`, `orcamentos` e `pedidos` foi aceita no alinhamento inicial; continua registrada no resultado, sem tornar a comparação reprovada.

Retorna código diferente de zero se encontrar divergências. Evidências vão para uma pasta temporária exibida ao final, ou para `-Saida CAMINHO_NOVO`; não versionar esses arquivos. O comparador pode ser repetido sem Docker sobre os catálogos salvos:

```powershell
node backend/tools/comparar-esquema/comparar.mjs CAMINHO/banco-a.json CAMINHO/banco-b.json
```

Para listar os identificadores de dois scripts do modelo e ver o que mudou:

```powershell
node backend/tools/comparar-esquema/comparar.mjs antes.sql depois.sql --nomes-sql
```

O ambiente Docker é removido mesmo em caso de falha. `-ManterAmbiente` permite investigar e executar provas adicionais; nesse caso, remova o helper, o Compose e as imagens ao terminar:

```powershell
docker rm -f estoque-check-modelo
docker compose -p estoque-check down --volumes --remove-orphans
docker image rm estoque-check-sdk estoque-check-backend
```

Antes de gerar uma migration futura, rode a comparação na base anterior. Depois da alteração, revise a migration, aplique-a em banco descartável e repita a comparação.
