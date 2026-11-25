# 11 — Atualizar o ambiente principal com segurança

**Para quem:** o responsável pelo computador onde roda o ambiente principal `zalek360` (portas 3000/5000/5432).
**Quando:** antes do próximo `docker compose up --build` do ambiente principal com o código da Fase 7.1 ou posterior.
**Duração:** cerca de 20 a 30 minutos, com o sistema indisponível por alguns minutos nos passos 4 e 5.
**Testado:** procedimento completo executado no ambiente de revisão (`zalek360-revisao`) em 25/11/2026, com os comandos abaixo e os nomes de container da revisão.

## Por que é preciso

A partir da Fase 7.1:
- o `docker-compose.yml` **não tem mais** chave JWT nem senha de banco padrão. Sem `JWT_CHAVE` e `POSTGRES_PASSWORD` no `.env`, **qualquer** comando `docker compose` desse projeto para com a mensagem `required variable ... is missing a value: Defina ... no .env`;
- fora de Development, a API **não sobe** com chave JWT curta ou já publicada no repositório, nem com senha de banco com menos de 16 caracteres ou publicada. O log mostra `Configuração insegura; a API não será iniciada.` e o motivo, sem valores;
- o PostgreSQL e a API passam a ser publicados só em `127.0.0.1` (o frontend continua acessível na rede, porta 3000);
- o Swagger só existe em Development; o cookie de sessão usa `Secure` por padrão; o seed de demonstração fica desligado por padrão.

O ambiente principal foi criado sem `.env`, ou seja, com a chave JWT e a senha de banco que estavam publicadas no repositório (que é **público** no GitHub). Quem conhece esses valores consegue forjar um login de Administrador. Este guia troca tudo sem perder dados.

> **Regras durante todo o procedimento**
> - Nunca use `docker compose down -v` nem remova volumes: o banco e as artes estão neles.
> - Não cole senhas ou chaves no chat, em e-mail, em commits ou em documentos. Os scripts geram e gravam os valores sem exibi-los.
> - Use o **PowerShell**, na pasta raiz do repositório (onde está o `docker-compose.yml`).

## 0. Conferir o ponto de partida

```powershell
docker ps --filter "label=com.docker.compose.project=zalek360" --format "{{.Names}}  {{.Status}}"
git status
```

Esperado: `zalek360-db-1`, `zalek360-backend-1` e `zalek360-frontend-1` em execução e o Git sem alterações locais.

## 1. Backup antes de qualquer mudança

Com os containers atuais ainda rodando:

```powershell
$destino = "C:\Backups\Zalek360"
New-Item -ItemType Directory -Force $destino | Out-Null
$data = Get-Date -Format yyyyMMdd-HHmmss
docker exec zalek360-db-1 sh -c 'pg_dump -U $POSTGRES_USER -d $POSTGRES_DB -Fc -f /tmp/zalek360.dump'
docker cp zalek360-db-1:/tmp/zalek360.dump "$destino\banco-$data.dump"
docker exec zalek360-db-1 rm /tmp/zalek360.dump
docker exec zalek360-backend-1 tar czf /tmp/anexos.tgz -C /app/data uploads
docker cp zalek360-backend-1:/tmp/anexos.tgz "$destino\anexos-$data.tar.gz"
docker exec zalek360-backend-1 rm /tmp/anexos.tgz
Get-ChildItem $destino | Select-Object Name, Length
```

Confira que os dois arquivos existem e não estão vazios, e copie a pasta para **outro disco ou nuvem** antes de seguir. (A Fase 7.2 acrescenta scripts de backup e restauração testados; quando estiverem no `main`, o `docs/12-backup-e-restauracao.md` passa a ser a referência.)

## 2. Atualizar o código

```powershell
git switch main
git pull
```

A partir daqui, os comandos `docker compose` do projeto exigem o `.env` (passo 3). Os containers atuais continuam rodando até serem recriados.

## 3. Criar o `.env` e trocar a senha do banco (ordem segura)

```powershell
powershell -ExecutionPolicy Bypass -File infra/implantacao/preparar-segredos.ps1 -ArquivoEnv .env -ContainerBanco zalek360-db-1 -GerarChaveJwt -TrocarSenhaBanco
```

O script, nesta ordem:
1. gera uma chave JWT nova (64 caracteres aleatórios) e uma senha nova para o PostgreSQL (43 caracteres aleatórios);
2. lê o usuário e o nome do banco do container atual, para continuarem iguais aos do volume existente;
3. grava o arquivo novo como `.env.novo`;
4. executa `ALTER ROLE CURRENT_USER WITH PASSWORD ...` **dentro do container do banco**, com a senha passada por variável de ambiente e `\getenv` do `psql` — nunca por argumento de linha de comando;
5. somente se o `ALTER ROLE` der certo, substitui o `.env` (UTF-8 sem BOM) e guarda o anterior como `.env.anterior`, se existia. Se falhar, o `.env` não é alterado.

Saída esperada, sem valores:

```
JWT_CHAVE gerada.
Senha do PostgreSQL alterada no banco.
Arquivo .env atualizado (anterior guardado como .env.anterior, se existia).
Próximo passo: recrie a API (docker compose ... up -d --build --no-deps backend frontend).
```

> A partir deste momento a API em execução ainda usa a senha antiga do banco nas conexões já abertas e vai falhar nas novas. Faça o passo 4 **em seguida**.

Depois, abra o `.env` num editor só para conferir as opções **não secretas** (não é preciso ler as linhas de segredo). Se quiser, copie do `.env.example` as linhas de `Empresa__*` e `Email__*`.

| Variável | Recomendação | Observação |
|---|---|---|
| `COOKIE_SEGURA` | `true` (padrão; pode omitir) | Veja o aviso abaixo. |
| `EXECUTAR_SEED` | `false` (padrão; pode omitir) | Nunca `true` em banco com dados reais. |
| `Empresa__*`, `Email__*` | conforme a empresa | Como no `.env.example`. |

> **Aviso sobre o cookie seguro (`COOKIE_SEGURA`)**
> Com `true` (padrão), o navegador só mantém o login em conexões **HTTPS** ou em `http://localhost`.
> Se alguém acessa o sistema por `http://IP-do-computador:3000` (outro computador da rede, sem HTTPS), o login **deixa de se manter**.
> Nesse caso: configure HTTPS (exemplo em `infra/nginx/zalek360.conf`) **ou** grave `COOKIE_SEGURA=false` no `.env` sabendo que o cookie de sessão passa a trafegar sem criptografia na rede local.

## 4. Recriar somente a API e o frontend

```powershell
docker compose up -d --build --no-deps backend frontend
```

O `--no-deps` não mexe no banco neste passo. Se aparecer `ports are not available ... bind: Normalmente é permitida apenas uma utilização`, o container antigo ainda estava liberando a porta: rode o mesmo comando de novo sem `--build` (`docker compose up -d --no-deps backend frontend`).

Conferência:

```powershell
curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:5000/health        # 200
curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:3000/api/auth/me   # 401 (sem login)
docker compose logs backend --since 5m | Select-String "Configuração insegura|password authentication failed"   # nada
```

- `Configuração insegura` no log: a mensagem diz qual variável corrigir no `.env`.
- `password authentication failed`: o `.env` e o banco ficaram diferentes; veja "Se algo der errado".

**Todos precisam entrar de novo:** a chave JWT mudou e as sessões antigas deixam de valer (esperado).

## 5. Publicar o banco só em 127.0.0.1

```powershell
docker compose up -d --wait
docker compose restart backend
docker compose port db 5432       # esperado: 127.0.0.1:5432
docker compose port backend 8080  # esperado: 127.0.0.1:5000
```

O primeiro comando recria o container do banco **com o mesmo volume** (alguns segundos fora do ar). O `restart backend` descarta as conexões antigas do pool: sem ele, as primeiras requisições depois do reinício do banco podem responder erro 500 (`57P01: terminating connection due to administrator command` no log), o que foi observado no teste da revisão.

## 6. Trocar a senha dos usuários de demonstração

As contas do seed antigo usam a senha de demonstração publicada no repositório. Redefina cada uma que existir no ambiente principal (`admin@zalekpersonalizados.com.br` e `gestor@...` ou `gestao@...`):

```powershell
powershell -ExecutionPolicy Bypass -File backend/tools/redefinir-senha/redefinir-senha.ps1 -Email admin@zalekpersonalizados.com.br
powershell -ExecutionPolicy Bypass -File backend/tools/redefinir-senha/redefinir-senha.ps1 -Email gestor@zalekpersonalizados.com.br
```

- A senha nova é pedida **duas vezes, sem eco** (mínimo de 12 caracteres). Ela não passa por argumento, log ou arquivo.
- Se o e-mail não existir, aparece `Nenhum usuário com esse e-mail. Nada foi alterado.`; tente o outro endereço do Gestor.
- As contas legadas Atendente/Operador não entram no sistema; não é preciso redefini-las.
- Detalhes: `backend/tools/redefinir-senha/README.md`.

Depois, entre no sistema com cada conta e a senha nova.

## 7. Conferência final

- [ ] `http://localhost:3000` abre o login e o login funciona com a senha nova.
- [ ] Dashboard, clientes, orçamentos e pedidos mostram os dados de antes.
- [ ] Gerar o PDF de um orçamento funciona.
- [ ] `http://localhost:5000/swagger/index.html` não abre (401 sem login; 404 com login): o Swagger só existe em Development.
- [ ] `docker compose port db 5432` mostra `127.0.0.1:5432`.
- [ ] O backup do passo 1 está copiado fora do computador.
- [ ] `git status` não mostra `.env`, `.env.anterior` nem `.env.novo` (são ignorados).
- [ ] Opcional: apague o `.env.anterior` quando tudo estiver conferido (ele contém os valores antigos).

## Se algo der errado

- **API não conecta ao banco (`password authentication failed`):** o `ALTER ROLE` e o `.env` ficaram diferentes. Rode o passo 3 de novo apenas com `-TrocarSenhaBanco` (gera outra senha, altera o banco e regrava o `.env`) e repita o passo 4.
- **Existe `.env.novo` e nenhuma mensagem de sucesso:** o `ALTER ROLE` pode ter sido aplicado sem a troca do arquivo. Renomeie `.env.novo` para `.env` e repita o passo 4.
- **Voltar o código:** `git switch --detach <commit anterior>` e `docker compose up -d --build`. O `.env` novo continua válido para o código antigo (mesmas variáveis). Não apague o `.env`: a senha antiga do banco não vale mais.
- **Restaurar dados:** restaure primeiro num banco temporário para conferir. Com os arquivos do passo 1: `docker cp banco-AAAAMMDD-HHMMSS.dump zalek360-db-1:/tmp/b.dump` e `docker exec zalek360-db-1 sh -c 'createdb -U $POSTGRES_USER zalek360_restauracao && pg_restore -U $POSTGRES_USER -d zalek360_restauracao --no-owner /tmp/b.dump'`. Para substituir o banco principal, siga o `docs/12-backup-e-restauracao.md` (Fase 7.2).

## Observações de segurança

- Os scripts nunca imprimem os valores. O `ALTER ROLE` envia a senha ao PostgreSQL dentro do container; a imagem `postgres:16-alpine` não registra comandos por padrão (`log_statement = none`). Não ative registro de comandos durante a troca.
- Para invalidar todas as sessões no futuro (por exemplo, ao desligar um colaborador), gere outra chave: `preparar-segredos.ps1 -ArquivoEnv .env -GerarChaveJwt -Substituir` e recrie a API (passo 4).
