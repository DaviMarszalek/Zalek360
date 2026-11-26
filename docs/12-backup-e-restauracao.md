# 12 — Backup e restauração

**Para quem:** o responsável pelo computador onde roda o Zalek360.
**O que protege:** o banco PostgreSQL (clientes, orçamentos, pedidos, estoque, histórico, usuários) e as artes/referências anexadas, que ficam em volumes Docker separados (`<projeto>_dados-postgres` e `<projeto>_anexos`).
**Testado:** backup e restauração em banco temporário no ambiente de revisão em 25/11/2026 — 22 tabelas com contagens idênticas às do banco de origem, mesma última migration e 71/71 anexos.

> Nunca use `docker compose down -v` nem remova volumes. Os scripts abaixo não param serviços, não alteram dados e não criam nem removem volumes.

## 1. Fazer um backup

Na raiz do repositório, no PowerShell, com o sistema rodando:

```powershell
powershell -ExecutionPolicy Bypass -File infra/backup/backup.ps1 -Projeto zalek360 -Destino C:\Backups\Zalek360
```

Gera três arquivos com data e hora no nome:

| Arquivo | Conteúdo |
|---|---|
| `banco-zalek360-AAAAMMDD-HHMMSS.dump` | `pg_dump` em formato custom (feito dentro do container do banco, sem senha) |
| `anexos-zalek360-AAAAMMDD-HHMMSS.tar.gz` | todos os arquivos do volume de anexos (montado somente leitura) |
| `manifesto-zalek360-AAAAMMDD-HHMMSS.txt` | tamanhos, SHA-256, entradas do dump e quantidade de arquivos |

O script confere o dump com `pg_restore --list` e o pacote com `tar` antes de declarar sucesso. Leva segundos com o volume atual.

**Os backups contêm dados pessoais de clientes (CPF/CNPJ, telefone, endereço) e o hash das senhas.** Guarde-os em local com acesso restrito e criptografado (por exemplo, pasta pessoal sincronizada com a nuvem da empresa ou disco externo guardado).

## 2. Rotina recomendada

| Item | Recomendação (decisão provisória, ajuste à realidade da empresa) |
|---|---|
| Frequência | diário, no fim do expediente; e sempre antes de atualizar o sistema |
| Retenção | 7 diários + 4 semanais (um por semana) |
| Cópia externa | pelo menos uma cópia fora do computador (nuvem ou disco externo) |
| Teste de restauração | uma vez por mês e depois de qualquer atualização (seção 3) |

**Agendar no Windows** (Agendador de Tarefas, diário às 18h30, executado pelo usuário que roda o Docker):

```powershell
$raiz = (Get-Location).Path
$acao = "-NoProfile -ExecutionPolicy Bypass -File `"$raiz\infra\backup\backup.ps1`" -Projeto zalek360 -Destino C:\Backups\Zalek360"
schtasks /Create /TN "Zalek360 backup diario" /SC DAILY /ST 18:30 /TR "powershell.exe $acao" /F
```

O Docker Desktop precisa estar aberto no horário. Confira de vez em quando se os arquivos novos estão aparecendo em `C:\Backups\Zalek360`.

**Limpar backups antigos** (mantém os 14 conjuntos mais recentes; confira a lista antes de remover):

```powershell
Get-ChildItem C:\Backups\Zalek360 -File | Sort-Object LastWriteTime -Descending | Select-Object -Skip 42 | Remove-Item -WhatIf
```

Tire o `-WhatIf` só depois de conferir a lista (3 arquivos por conjunto × 14 = 42).

## 3. Testar a restauração (sem afetar o sistema)

```powershell
powershell -ExecutionPolicy Bypass -File infra/backup/restaurar.ps1 -Projeto zalek360 `
  -Dump C:\Backups\Zalek360\banco-zalek360-AAAAMMDD-HHMMSS.dump `
  -Anexos C:\Backups\Zalek360\anexos-zalek360-AAAAMMDD-HHMMSS.tar.gz `
  -PastaAnexos C:\Temp\zalek360-anexos-restaurados
```

O script:
1. cria um banco **temporário** `<banco>_restauracao_AAAAMMDD_HHMMSS` no mesmo PostgreSQL e restaura o dump nele;
2. mostra, tabela por tabela, a quantidade de linhas restauradas e a atual (diferenças são esperadas se houve uso do sistema depois do backup);
3. confere a última migration aplicada;
4. remove o banco temporário (use `-ManterBanco` para inspecioná-lo antes);
5. extrai os anexos na pasta indicada (que precisa estar vazia) e compara a quantidade de arquivos com o volume atual.

Depois de conferir, apague a pasta de anexos restaurados.

## 4. Restaurar de verdade (emergência)

Use somente se o banco ou os anexos atuais estiverem perdidos ou corrompidos. **Faça antes um backup do estado atual (seção 1), mesmo que pareça inútil.** O procedimento abaixo não apaga o banco atual: ele é renomeado e fica guardado.

1. Pare a API (o banco continua rodando):
   ```powershell
   docker compose stop backend
   ```
2. Restaure o dump num banco novo e troque os nomes (substitua o arquivo e o carimbo):
   ```powershell
   docker cp C:\Backups\Zalek360\banco-zalek360-AAAAMMDD-HHMMSS.dump zalek360-db-1:/tmp/restaurar.dump
   docker exec zalek360-db-1 sh -c 'createdb -U $POSTGRES_USER zalek360_restaurado && pg_restore -U $POSTGRES_USER -d zalek360_restaurado --no-owner --exit-on-error /tmp/restaurar.dump && rm /tmp/restaurar.dump'
   docker exec zalek360-db-1 sh -c 'psql -U $POSTGRES_USER -d postgres -v ON_ERROR_STOP=1 -c ''ALTER DATABASE zalek360 RENAME TO zalek360_antes_da_restauracao'' -c ''ALTER DATABASE zalek360_restaurado RENAME TO zalek360'''
   ```
   (Se o seu banco não se chama `zalek360`, use o valor de `POSTGRES_DB` do `.env`. As aspas simples dobradas são intencionais: o PowerShell 5.1 corrompe aspas duplas passadas a programas externos.)
3. Anexos, se também precisarem voltar: extraia o pacote **por cima** do volume (arquivos atuais com o mesmo nome são substituídos; os demais permanecem):
   ```powershell
   docker run --rm -v zalek360_anexos:/dados -v C:\Backups\Zalek360:/pacote:ro postgres:16-alpine tar xzf /pacote/anexos-zalek360-AAAAMMDD-HHMMSS.tar.gz -C /dados
   ```
4. Suba a API e confira login, clientes, orçamentos e um PDF:
   ```powershell
   docker compose start backend
   ```
5. Quando tudo estiver conferido (dias depois), o banco antigo pode ser removido: `docker exec zalek360-db-1 sh -c 'dropdb -U $POSTGRES_USER zalek360_antes_da_restauracao'`.

> Os passos da seção 4 foram escritos com os mesmos comandos testados nas seções 1 e 3, mas a troca de nomes do banco principal **não foi executada** no teste (o banco temporário foi restaurado, conferido e removido). Faça um ensaio em um computador de teste antes de precisar.

## 5. O que o backup não cobre

- O arquivo `.env` (segredos): guarde uma cópia em local seguro separado (gerenciador de senhas). Sem ele, a API não conecta ao banco restaurado; nesse caso, troque a senha do banco com `infra/implantacao/preparar-segredos.ps1 -TrocarSenhaBanco` (docs/11).
- O volume `identidade` (logo da empresa), se usado: copie o arquivo de logo junto com os backups.
- O código: está no GitHub; anote o commit em uso (`git log -1 --oneline`) junto com o backup.
