# Zalek360 — MVP 1 (ciclo comercial)

Sistema comercial **interno** da Zalek Personalizados. Cobre o cadastro de clientes, a elaboração de orçamentos com itens e personalização, o acompanhamento do retorno do cliente e a geração automática do pedido quando o orçamento é aprovado.

O cliente **não acessa o sistema**: WhatsApp, ligação, e-mail ou conversa presencial acontecem fora dele. O atendente registra no Zalek360 os contatos e a decisão comunicada pelo cliente.

| Serviço | Endereço |
|---|---|
| Frontend (Next.js) | http://localhost:3000 |
| API (ASP.NET Core) | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| Health check | http://localhost:5000/health |

**Acesso de demonstração:** `maria.silva@zalekpersonalizados.com.br` / `Zalek@2026` (perfil Atendente).
Também existem `carlos.souza@…` e `fernanda.lima@…` (Atendentes) e `admin@zalekpersonalizados.com.br` (Administrador), todos com a mesma senha.

---

## 1. Executar com Docker (recomendado)

Pré-requisito: Docker com Compose v2.

```bash
docker compose up --build
```

Na primeira subida, a API:
1. espera o PostgreSQL aceitar conexões;
2. aplica a migration;
3. confere se todas as colunas do modelo existem no banco;
4. grava os dados de demonstração.

Quando o log mostrar `Now listening on: http://[::]:8080`, abra http://localhost:3000.

- **Dados persistentes:** os volumes `dados-postgres` (banco) e `anexos` (artes enviadas) sobrevivem a `docker compose down`.
- **Recomeçar do zero:** `docker compose down -v` apaga os volumes, e o seed é recriado na próxima subida.
- **Portas ocupadas:** se 3000, 5000 ou 5432 já estiverem em uso, copie `.env.example` para `.env` e altere `FRONTEND_PORTA_HOST`, `BACKEND_PORTA_HOST` ou `POSTGRES_PORTA_HOST`.
- **Fora da demonstração local:** defina `POSTGRES_PASSWORD` e `JWT_CHAVE` no `.env`. Gere a chave com `openssl rand -base64 48`; a API não inicia com chave de menos de 32 caracteres.

## 2. Executar localmente (sem Docker)

Pré-requisitos: .NET SDK 8, Node.js 22 e PostgreSQL 16.

**Banco**

```bash
sudo -u postgres psql -c "CREATE USER zalek360 WITH PASSWORD 'zalek360' CREATEDB;"
sudo -u postgres psql -c "CREATE DATABASE zalek360 OWNER zalek360;"
```

**Backend** (porta 5000, ambiente Development)

```bash
cd backend
dotnet run --project src/Zalek360.Api
```

`appsettings.Development.json` já traz uma chave JWT de desenvolvimento e o cookie sem exigência de HTTPS.
A connection string padrão é `Host=localhost;Port=5432;Database=zalek360;Username=zalek360;Password=zalek360`.

**Frontend** (porta 3000)

```bash
cd frontend
cp .env.example .env.local      # API_INTERNAL_URL=http://localhost:5000
npm install
npm run dev
```

O navegador chama sempre `/api/*` no próprio frontend, e o Next.js encaminha para `API_INTERNAL_URL`. Assim o cookie de sessão fica no mesmo domínio e o navegador não precisa de CORS.

## 3. Roteiro de demonstração (segue os protótipos)

Os dados de demonstração usam **datas relativas**: o "hoje" do protótipo (24/09/2026) corresponde ao dia em que o sistema é iniciado. Por isso validades, prazos e o indicador "vencem em 3 dias" do dashboard fazem sentido em qualquer data.

1. **Login** com Maria Silva. O **Dashboard** mostra orçamentos por situação, os aguardando retorno e os pedidos em andamento.
2. **Clientes → pesquisar "Academia Movimento"**: nenhum resultado. O cliente é propositalmente ausente do seed, para demonstrar o fluxo "pesquise antes de cadastrar".
3. **Novo cliente (PJ) com CNPJ `38.221.907/0001-53`.**
   - Salve sem preencher os obrigatórios para ver a validação campo a campo.
   - Troque para `12.345.678/0001-95` para ver o bloqueio de CNPJ duplicado. Esse CNPJ é da Metalúrgica Horizonte, e a tela oferece o link para o cadastro existente.
   - Volte ao CNPJ da Academia e salve.
4. **Novo orçamento** para a Academia.
   - Tente salvar sem itens: o sistema bloqueia (RN3).
   - Adicione "Camiseta Dry Fit", 100 un., R$ 32,00 + R$ 10,00 de personalização, "Serigrafia (frente) + DTF (costas)", e anexe as artes. Os totais são calculados pela API.
   - Salve: nasce o **#000124 — Em Elaboração**.
5. **Apresentar ao cliente**: o orçamento passa a Aguardando Retorno.
6. **Registrar contato** (WhatsApp) e depois ajuste a quantidade para 80. O contato **não** altera o status, e as duas ações entram no histórico.
7. **Registrar decisão → Aprovado**, informando o meio de contato e a data/hora. Depois confirme **"Aprovar orçamento e gerar pedido?"**.
   - Na mesma transação, o orçamento vira Aprovado e o **pedido #000057 — Aberto** é criado.
   - O pedido herda itens, personalização, artes, valores e condições. O prazo de entrega é contado em dias úteis a partir da aprovação.
8. **Pedidos → #000057**: faixa de origem com link para o orçamento, **Atualizar status** (Aberto → Em produção → Pronto → Entregue, uma etapa por vez) e **Cancelar pedido** (com motivo obrigatório).
9. **Cliente Metalúrgica Horizonte**: veja as abas Visão geral, Orçamentos, Pedidos, Observações e Histórico (6 orçamentos e 3 pedidos, como no protótipo). Filtre Orçamentos por período e por "Aprovado".
10. Outros exemplos:
    - #000116 **Expirado**: expiração automática, sem ação do atendente;
    - um orçamento **Recusado**, com motivo;
    - `/pedidos/novo`: a criação manual é bloqueada. `POST /api/pedidos` retorna 422 `PEDIDO_CRIACAO_DIRETA_NAO_PERMITIDA`.

> **CPF/CNPJ do protótipo:** vários documentos das telas têm dígito verificador inválido. Por exemplo, o protótipo mostra `38.221.907/0001-64` para a Academia, mas o DV correto é `-53`. Como o sistema valida o DV (RN2), o seed usa os mesmos números-base com o DV corrigido. A tabela completa está em [`docs/03-alteracoes-e-complementos-na-modelagem.md`](docs/03-alteracoes-e-complementos-na-modelagem.md).

## 4. Arquitetura

```
backend/
  src/Zalek360.Domain          Entidades, regras (RN1–RN10), cálculo, calendário de dias úteis — sem dependências
  src/Zalek360.Application     Casos de uso (serviços), DTOs, interfaces de repositório/consulta, validação
  src/Zalek360.Infrastructure  EF Core + Npgsql, migration, seed, consultas, JWT, BCrypt, armazenamento de anexos, rotinas
  src/Zalek360.Api             Controllers REST, autenticação (cookie httpOnly + Bearer), erros padronizados, Swagger
  tests/Zalek360.Tests         xUnit: domínio, casos de uso (fakes) e integração (EF + SQLite em memória)
  scripts/                     testar.sh, recriar-migration-inicial.sh
  tools/verificacao-offline/   Verificação sem NuGet (ver seção 7)
frontend/
  app/                         Rotas (App Router): login, dashboard, clientes, orcamentos, pedidos
  features/                    Telas por módulo (auth, dashboard, clientes, orcamentos, pedidos, comum)
  components/ui, layout        Componentes de interface e casca (menu lateral, cabeçalho)
  services/, hooks/, lib/      Axios centralizado, TanStack Query, tratamento de erro, status
  types/, utils/               Tipos da API e formatação pt-BR (moeda, datas, CPF/CNPJ, telefone)
docs/                          Análise, rastreabilidade, modelagem, relatório, checklists, insumos para diagramas
infra/nginx/                   Exemplo de proxy reverso com HTTPS
```

**Decisões centrais:**
- **Cálculo sempre no backend.** A tela de orçamento mostra os totais retornados por `POST /api/orcamentos/calculo`, e o valor gravado é recalculado no servidor.
- **Pedido só nasce de orçamento aprovado.** `Pedido.CriarAPartirDe` é o único ponto de criação. A aprovação e a geração do pedido ocorrem numa transação: se a geração falhar, nada é gravado e o orçamento continua Aguardando Retorno.
- **Histórico unificado** (`historico_eventos`) por cliente, orçamento e pedido, gravado na mesma transação da alteração.
- **Concorrência otimista** (`versao`): editar um registro alterado por outra pessoa retorna 409 com mensagem clara.
- **Erros no formato** `{ "status", "code", "message", "errors" }`, com mensagens em português e `errors` por campo.

## 5. API

Documentação interativa em `/swagger`. Para autenticar lá, chame `POST /api/auth/login` (o cookie é definido automaticamente) ou cole o `accessToken` retornado em **Authorize**.

| Recurso | Endpoints |
|---|---|
| Autenticação | `POST /api/auth/login` · `POST /api/auth/logout` · `GET /api/auth/me` |
| Clientes | `GET/POST /api/clientes` · `GET /api/clientes/cidades` · `GET /api/clientes/verificar-documento` · `GET/PUT /api/clientes/{id}` · `GET /api/clientes/{id}/visao-geral` · `/orcamentos` · `/pedidos` · `/historico` · `GET/POST /api/clientes/{id}/observacoes` |
| Orçamentos | `GET/POST /api/orcamentos` · `POST /api/orcamentos/calculo` · `GET/PUT /api/orcamentos/{id}` · `/itens` · `/historico?categoria=` · `GET/POST /api/orcamentos/{id}/contatos` · `POST /api/orcamentos/{id}/aguardando-retorno` · `POST /api/orcamentos/{id}/decisao` · `GET /api/orcamentos/motivos-decisao` |
| Pedidos | `GET /api/pedidos` · `GET /api/pedidos/{id}` · `/historico` · `PATCH /api/pedidos/{id}/status` · `POST /api/pedidos` → **sempre 422** |
| Anexos | `POST /api/anexos` (multipart `arquivo`; PDF, PNG, JPG, AI, CDR; até 20 MB) · `GET /api/anexos/{id}/download?inline=` · `DELETE /api/anexos/{id}` (só anexos ainda não vinculados) |
| Apoio | `GET /api/dashboard` · `GET /api/produtos` · `GET /api/produtos/opcoes` · `GET /api/usuarios/responsaveis` · `GET /health` |

Não existe exclusão de orçamentos, pedidos ou clientes (RN10): apenas cancelamento, com o histórico preservado.

## 6. Testes

```bash
cd backend && ./scripts/testar.sh      # equivalente a: dotnet test Zalek360.sln
```

São 44 testes xUnit:
- **27 de domínio:** cliente, CPF/CNPJ, cálculo, ciclo do orçamento, expiração e pedido.
- **14 de casos de uso**, com repositórios em memória:
  - duplicidade de documento;
  - numeração sem consumo em caso de erro;
  - contato sem mudança de status;
  - aprovação e geração do pedido na mesma transação;
  - não confirmação da transação quando a geração do pedido falha;
  - conflito de versão;
  - bloqueio de pedido direto;
  - rotina de expiração.
- **3 de integração com EF Core + SQLite em memória:**
  - a aprovação persiste pedido, itens, anexos copiados e histórico;
  - uma falha na numeração do pedido desfaz a aprovação no banco (rollback real);
  - o histórico do cliente filtra por tipo.

## 7. O que foi verificado nesta entrega (transparência)

O ambiente em que o projeto foi construído **não tinha acesso ao NuGet** (HTTP 403) **nem ao Docker**. Por isso:

| Verificação | Resultado |
|---|---|
| Domain e Application compilados com o SDK .NET 8 | 0 erros, 0 avisos |
| Infrastructure, Api e Tests compilados contra *stubs* de assinatura (`backend/tools/verificacao-offline/verificar.sh`) | 0 erros, 0 avisos nos 5 projetos |
| Testes de domínio e de casos de uso executados | **41 de 41 aprovados** |
| Testes de integração (EF + SQLite) | Compilados; **não executados** (precisam do pacote EF Sqlite) |
| Migration traduzida para SQL e executada em PostgreSQL 16 | 12 tabelas, 47 índices, 22 FKs, 7 checks; sequências e unicidade conferidas |
| Entidades × mapeamento EF × migration (script de conferência) | 0 divergências em 12 tabelas |
| Frontend: `tsc --noEmit` e `next build` (Next 16, output standalone) | Sem erros; 15 rotas; `proxy.ts` reconhecido |
| Imagem do frontend simulada etapa por etapa (`npm ci` limpo, build com `API_INTERNAL_URL=http://backend:8080`, execução do standalone) contra um backend simulado | Cookie de sessão repassado; erros 422 preservados; upload de 21 MB íntegro |
| `docker compose up --build` completo | **Não executado aqui** (sem Docker) |
| API real rodando com EF Core/Npgsql contra PostgreSQL | **Não executada aqui** (sem NuGet) |

**Primeira ação recomendada numa máquina com internet:**

```bash
cd backend && ./scripts/testar.sh
docker compose up --build
```

Depois, siga o roteiro da seção 3. Os stubs verificam tipos e assinaturas, mas não o comportamento do EF Core em execução. Por isso fiz também uma auditoria manual, descrita no relatório final:
- datas sempre em UTC;
- consultas traduzíveis para SQL;
- construtores e campos exigidos pelo EF;
- chaves Guid geradas no domínio.

## 8. Migration

A migration inicial `20260928120000_InitialCreate` foi **escrita manualmente**, espelhando o mapeamento EF, porque o `dotnet ef` não pôde ser instalado. Ela é aplicada automaticamente na inicialização. A API também compara as colunas do modelo com o `information_schema` e se recusa a subir se faltar alguma coluna.

Para regenerá-la com a ferramenta oficial (o que também gera o `ModelSnapshot`, necessário para as próximas migrations), use um **banco novo**:

```bash
docker compose down -v
cd backend && ./scripts/recriar-migration-inicial.sh
```

## 9. Documentação

| Documento | Conteúdo |
|---|---|
| [`docs/01-analise-inicial.md`](docs/01-analise-inicial.md) | Leitura dos requisitos, prioridades e conflitos entre fontes |
| [`docs/02-matriz-rastreabilidade.md`](docs/02-matriz-rastreabilidade.md) | UC → Tela → Endpoint → Entidade → Regra → Teste |
| [`docs/03-alteracoes-e-complementos-na-modelagem.md`](docs/03-alteracoes-e-complementos-na-modelagem.md) | Estrutura original, alteração, motivo, impacto e tabelas |
| [`docs/04-relatorio-final.md`](docs/04-relatorio-final.md) | Relatório final em 17 itens |
| [`docs/05-checklists.md`](docs/05-checklists.md) | Checklists de casos de uso, tecnologia e telas |
| [`docs/06-revisao-da-modelagem.md`](docs/06-revisao-da-modelagem.md) | MANTIDO / ALTERADO / ADICIONADO / REMOVIDO |
| [`docs/07-insumos-diagramas-der-dicionario.md`](docs/07-insumos-diagramas-der-dicionario.md) | O que atualizar nos diagramas, no DER e no dicionário de dados |
