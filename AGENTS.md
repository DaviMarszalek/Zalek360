# Regras permanentes para agentes

## Projeto e versões

- Zalek360 é o sistema interno da Zalek Personalizados.
- Backend: ASP.NET Core 8 / .NET 8, com Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Api`) e testes xUnit.
- Banco: PostgreSQL 16 (`postgres:16-alpine` no Compose).
- Frontend: Next.js 16.4.5, React 19.3, Tailwind CSS 3.4 e TanStack Query 5.104. As versões foram conferidas em `frontend/package.json` e nos projetos backend em 24/11/2026.
- Execução local de validação: Docker Compose.

## Git e aprovações

- Nunca faça push direto para `main`.
- Use uma branch por fase/tarefa e commits pequenos, com mensagens em português.
- Abra PR para `main` e aguarde o OK explícito do usuário antes de mesclar ou iniciar a próxima fase.
- Nunca force push, use `reset --hard`, apague branches remotas ou reescreva histórico publicado.
- Antes de alterar código, apresente um plano curto e aguarde o “pode seguir”.

## Ambientes e dados

- O ambiente principal `zalek360` (portas 3000/5000/5432) e seu banco nunca devem ser iniciados para testes, modificados, limpos ou removidos.
- Toda validação usa exclusivamente:

  ```powershell
  docker compose -p zalek360-revisao --env-file .env.revisao up --build -d --wait
  ```

  Portas de revisão: frontend 3100, API 5100 e PostgreSQL 55432. Confirme que o Docker Desktop está aberto e com o Engine running.
- `.env.revisao` contém configuração local e nunca deve ser commitado, exibido ou copiado para logs. Use `.env.revisao.example` como modelo quando disponível. O `.gitignore` de `main` pode ainda não conter a regra até o PR #3 ser mesclado; confira `git status` e nunca adicione esse arquivo.
- Não execute `docker compose down -v` sem autorização explícita. Não compartilhe volumes, redes ou bancos entre o ambiente principal e o de revisão.

## Banco e migrations

- Migrations novas somente via `dotnet ef migrations add`, usando o `ModelSnapshot` existente.
- Nunca edite migrations aplicadas.
- Confira toda migration com `backend/tools/comparar-esquema`.
- Preserve dados existentes; não faça conversões ou backfills destrutivos sem aprovação e plano de dados.

## Testes e builds por entrega

- Backend: testes com PostgreSQL real, `ZALEK_TEST_POSTGRES` configurado e zero testes ignorados.
- Frontend: `npm ci`, `npm run typecheck`, `npm test` (Vitest), `npm run build` e `npm run e2e`.
- E2E fica em `frontend/e2e`, usa o Microsoft Edge instalado (`channel: "msedge"`) e aponta para `http://localhost:3100`.
- Compose deve ser construído e iniciado no ambiente de revisão. Informe os números reais dos testes e tudo o que não foi verificado.
- O repositório está no OneDrive; builds e testes podem ficar lentos. Não rode builds/testes pesados em paralelo.
- Não use ferramentas automáticas de upgrade de dependências. Para dependências JavaScript, trabalhe diretamente com npm e restaure pelo lockfile com `npm ci`.

### Testes .NET no Windows

O SDK .NET não está instalado no Windows. Rode os testes em container .NET 8, pela raiz do repositório, com o ambiente de revisão ativo. O comando passa as credenciais localmente ao container sem escrever seus valores no comando/documentação:

```powershell
docker run --rm `
  --network zalek360-revisao_default `
  --env-file .env.revisao `
  --mount "type=bind,source=$($PWD.Path)\backend,target=/src" `
  --mount type=volume,source=zalek360-nuget-cache,target=/root/.nuget/packages `
  -w /src `
  mcr.microsoft.com/dotnet/sdk:8.0 `
  sh -lc 'export ZALEK_TEST_POSTGRES="Host=db;Port=5432;Database=postgres;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"; dotnet test Zalek360.sln --nologo'
```

O teste PostgreSQL cria e remove bancos de teste com prefixo próprio dentro do ambiente descartável. Sem `ZALEK_TEST_POSTGRES`, os testes PostgreSQL são ignorados; não aceite uma validação com ignorados.

## Convenções e armadilhas

- No PowerShell 5.1, `curl.exe` pode remover aspas internas de JSON. Para requests JSON, prefira `Invoke-RestMethod` com `ConvertTo-Json` ou `curl.exe --data-binary "@arquivo.json"`.
- Erros da API seguem `{status, code, message, errors}`.
- Siga os padrões existentes: nomes em português, UI em `frontend/components/ui`, serviços em `frontend/services`, hooks com TanStack Query e nenhum valor de negócio fixo no frontend.
- Usuários de demonstração em banco novo: `admin@zalekpersonalizados.com.br` (Administrador) e `gestao@zalekpersonalizados.com.br` (Gestor), senha `Zalek@2026`. Bancos antigos podem ainda ter `gestor@zalekpersonalizados.com.br`. Perfis Atendente e Operador são mantidos por compatibilidade, mas não devem aparecer na interface nem autenticar.
- Nunca inclua segredos, senhas reais, conteúdo de `.env` ou `.env.revisao` em commits, documentação, mensagens ou relatórios. A senha de demonstração acima é a exceção aprovada pelo usuário.

## Documentação e escopo

- Documentação viva: `README.md`, `docs/08-mvp2.md`, `docs/09-revisao-funcional.md` (matriz R1–R8) e `docs/10-contexto-e-proximos-passos.md`.
- Atualize a documentação diretamente relacionada ao trabalho de cada fase.
- Respeite as decisões de negócio e as pendências descritas em `docs/10-contexto-e-proximos-passos.md`; não implemente fases futuras sem autorização.
