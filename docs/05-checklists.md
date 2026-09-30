# 05 — Checklists

## Legenda

| Marca | Significado |
|---|---|
| **[OK]** | Implementado **e verificado em execução** neste ambiente |
| **[IMPLEMENTADO]** | Código completo, compilado e revisado; as regras foram exercitadas pelos testes executados de domínio e de casos de uso. A **execução ponta a ponta** (API real com EF/Npgsql, ou tela contra a API real) ficou pendente, porque NuGet e Docker estavam indisponíveis. Validar com `./backend/scripts/testar.sh` e com o roteiro do README. |
| **[PARCIAL]** | Implementado com diferença ou lacuna documentada |
| **[NÃO]** | Não implementado no MVP 1 |

A marca [OK] só aparece quando houve execução real, como build, testes, DDL no PostgreSQL ou servidor Next.js respondendo. Uma camada de uma funcionalidade pode ter sido verificada sem que a funcionalidade inteira tenha sido testada ponta a ponta.

---

## 1. Casos de uso

| # | Item | Situação | Evidência |
|---|---|---|---|
| UC01 | Pesquisar cliente antes de cadastrar | [IMPLEMENTADO] | Busca normalizada por nome, documento, telefone e e-mail |
| UC01 | Cadastrar PF/PJ com validação de CPF/CNPJ (DV) | [IMPLEMENTADO] | 7 testes de domínio executados (DV, obrigatórios, normalização) |
| UC01 | A1 — bloquear CPF/CNPJ duplicado e sugerir o cliente existente | [IMPLEMENTADO] | Teste de caso de uso executado + índice único validado no PostgreSQL |
| UC01 | Editar cadastro com controle de versão | [IMPLEMENTADO] | Teste de conflito de versão executado |
| UC02 | Criar orçamento com itens, personalização e condições | [IMPLEMENTADO] | Testes de domínio e de caso de uso executados |
| UC02 | Cálculo automático no servidor (RN4) | [IMPLEMENTADO] | Teste com o #000123 do protótipo executado |
| UC02 | Anexar artes (PDF, PNG, JPG, AI, CDR, até 20 MB) | [IMPLEMENTADO] | Validação de tipo e assinatura no domínio; upload de 21 MB atravessou o proxy do frontend na simulação |
| UC02 | A1 — impedir orçamento sem item | [IMPLEMENTADO] | Testes executados (inclusive "não consome número") |
| UC03 | Apresentar ao cliente (Aguardando Retorno) | [IMPLEMENTADO] | Teste de validade vencida executado |
| UC03 | Registrar contato sem alterar status | [IMPLEMENTADO] | Testes de domínio e de caso de uso executados |
| UC03 | Registrar decisão com meio de contato e data/hora (RN7) | [IMPLEMENTADO] | Testes executados |
| UC03 | A1 — recusa com motivo | [IMPLEMENTADO] | Testes executados |
| UC03 | A2 — expiração automática | [IMPLEMENTADO] | Testes do domínio e da rotina executados; agendamento em segundo plano não executado |
| UC03 | A3 — editar enquanto Em Elaboração ou Aguardando Retorno | [IMPLEMENTADO] | Teste de recálculo com histórico executado |
| UC03 | Exportar orçamento (condição de entrada do UC03) | [NÃO] | Substituído por "Apresentar ao cliente"; PDF fica como próximo passo |
| UC04 | Converter automaticamente em pedido, copiando itens, personalização, artes e valores | [IMPLEMENTADO] | Testes de domínio e de caso de uso executados; teste de integração compilado, não executado |
| UC04 | Transação única com rollback em caso de falha | [IMPLEMENTADO] | Teste com fakes executado; teste de rollback real (SQLite) compilado, não executado |
| UC04 | Vínculo pedido ↔ orçamento (rastreabilidade) | [IMPLEMENTADO] | FK única `pedidos.orcamento_origem_id` validada no PostgreSQL |
| UC05 | Histórico de orçamentos e pedidos por cliente, com filtros | [IMPLEMENTADO] | Teste de integração compilado, não executado |
| UC05 | A1 — cliente sem histórico mostra mensagem informativa | [IMPLEMENTADO] | Estados vazios nas abas; cliente sem orçamentos no seed |
| RN9 | Pedido direto bloqueado (422) | [IMPLEMENTADO] | Teste executado; resposta 422 com corpo preservado verificada no proxy do frontend |
| — | Atualizar status do pedido (uma etapa por vez) e cancelar com motivo | [IMPLEMENTADO] | Testes de domínio executados |
| — | Dashboard por situação (RF 4.1.4.2) | [IMPLEMENTADO] | Sem teste automatizado |
| — | Login, logout e sessão | [IMPLEMENTADO] | Proteção de rotas do frontend verificada em execução (redireciona para `/login`); login real contra a API não executado |

## 2. Tecnologia e requisitos da entrega

| Item | Situação | Evidência |
|---|---|---|
| C#/.NET 8 + ASP.NET Core Web API | [IMPLEMENTADO] | Api compilada contra stubs; não executada |
| Clean Architecture (Domain / Application / Infrastructure / Api / tests) | [OK] | Estrutura e dependências entre projetos conferidas; Domain e Application compilados de verdade |
| EF Core + migrations | [PARCIAL] | Migration escrita manualmente, sem `ModelSnapshot`. DDL equivalente executada no PostgreSQL 16 [OK]; aplicação pelo EF não executada |
| PostgreSQL real, `decimal` para dinheiro | [OK] | DDL executada: `numeric(10,2)`, 12 tabelas, 47 índices, 22 FKs, 7 checks |
| Swagger | [IMPLEMENTADO] | Configurado com Bearer e comentários XML (geração do XML compilada) |
| JWT | [IMPLEMENTADO] | Emissão e validação configuradas; cookie repassado pelo proxy verificado na simulação |
| BCrypt | [IMPLEMENTADO] | Custo 11 |
| Injeção de dependência, DTOs e validação | [OK] | Compilado; validação exercitada pelos 41 testes executados |
| Erro padronizado `{status, code, message, errors}` | [IMPLEMENTADO] | Middleware e fábrica de erros de modelo compilados |
| Next.js + React + TypeScript + Tailwind | [OK] | `tsc` e `next build` sem erros; servidor standalone respondendo |
| shadcn/ui | [PARCIAL] | CLI indisponível; componentes próprios no mesmo padrão (Tailwind + Radix) |
| TanStack Query + Axios centralizado | [OK] | Build; tratamento de 401 → `/login` implementado |
| Estrutura do frontend (app, components, features, hooks, lib, services, types, utils) | [OK] | Presente |
| Nada fixo no frontend (listas e opções vindas da API) | [OK] | Produtos, opções, motivos, responsáveis e cidades vêm de endpoints |
| Docker (`docker compose up --build`) | [IMPLEMENTADO] | Compose e Dockerfiles escritos; imagem do frontend simulada etapa por etapa [OK]; compose não executado (sem Docker) |
| `.env.example` do frontend e do backend (e da raiz) | [OK] | Presentes |
| Usuário demo `maria.silva@zalekpersonalizados.com.br` | [IMPLEMENTADO] | Seed |
| Menu Dashboard, Clientes, Orçamentos e Pedidos + rodapé + Sair | [OK] | Build do layout |
| Identidade visual fiel | [IMPLEMENTADO] | Tokens de cor e tipografia do protótipo; sem comparação visual lado a lado neste ambiente |
| Responsivo (desktop-first) | [IMPLEMENTADO] | Grades e tabelas roláveis; não testado em dispositivos |
| PWA preparado | [PARCIAL] | Manifest e ícone servidos [OK]; sem service worker |
| Seed com clientes do protótipo | [IMPLEMENTADO] | DVs corrigidos e documentados |
| Testes (cliente, orçamento, contatos, decisão, aprovação com rollback, histórico) | [PARCIAL] | 44 escritos; 41 executados e aprovados; 3 de integração compilados, não executados |
| README (frontend :3000, backend :5000, Swagger :5000/swagger) | [OK] | `README.md` |

## 3. Telas (protótipos T01–T32)

Todas as telas foram compiladas no build de produção. Nenhuma foi exercitada contra a API real neste ambiente, por isso a marca é [IMPLEMENTADO].

| Tela | Situação | Observação |
|---|---|---|
| T01 Login | [IMPLEMENTADO] | Página servida pelo Next (200) [OK]; proteção de rotas [OK] |
| T02 Dashboard | [IMPLEMENTADO] | Indicadores reais do banco (diferem dos números ilustrativos do protótipo) |
| T03 Clientes — lista e pesquisa | [IMPLEMENTADO] | |
| T04 Clientes — nenhum encontrado | [IMPLEMENTADO] | Oferece cadastrar com o termo pesquisado |
| T05 Novo cliente | [IMPLEMENTADO] | Campo "Cargo do contato" adicionado |
| T06 Novo cliente — validação | [IMPLEMENTADO] | Erros por campo vindos da API |
| T07 Novo cliente — válido | [IMPLEMENTADO] | |
| T08 Novo cliente — CNPJ duplicado | [IMPLEMENTADO] | Verificação ao sair do campo + link para o cliente existente |
| T09 Cliente cadastrado | [IMPLEMENTADO] | Notificação + detalhe (ou direto para o novo orçamento) |
| T10 Orçamentos — lista | [PARCIAL] | Filtro de status só por abas (sem o seletor duplicado do protótipo) |
| T11 Novo orçamento — cliente | [IMPLEMENTADO] | |
| T12 Novo orçamento — sem itens | [IMPLEMENTADO] | |
| T13 Novo orçamento — erro sem item | [IMPLEMENTADO] | |
| T14 Novo orçamento — itens, personalização e anexos | [IMPLEMENTADO] | Progresso de upload |
| T15 Em Elaboração | [IMPLEMENTADO] | + "Cancelar orçamento" e "Apresentar ao cliente" |
| T16 Aguardando Retorno | [IMPLEMENTADO] | |
| T17 Registrar contato | [IMPLEMENTADO] | |
| T18 Registrar decisão | [IMPLEMENTADO] | |
| T19 Decisão — erro | [IMPLEMENTADO] | |
| T20 Recusado | [IMPLEMENTADO] | |
| T21 Expirado | [IMPLEMENTADO] | |
| T22 Aprovar e gerar pedido | [IMPLEMENTADO] | |
| T23 Aprovado + pedido gerado | [IMPLEMENTADO] | Modal de sucesso; modal de falha informa que nada foi gravado |
| T24 Aprovado | [IMPLEMENTADO] | |
| T25 Pedido — detalhe | [IMPLEMENTADO] | Faixa de origem, etapas, "Atualizar status", "Cancelar pedido" |
| T26 Pedidos — lista | [PARCIAL] | Mesma observação de T10 |
| T27 Cliente — Visão geral | [IMPLEMENTADO] | + aba Histórico (adição) |
| T28 Cliente — Orçamentos | [IMPLEMENTADO] | Filtros por período e status |
| T29 Cliente — Pedidos | [IMPLEMENTADO] | |
| T30 Cliente — Observações | [IMPLEMENTADO] | |
| T31 Cliente com histórico | [IMPLEMENTADO] | Metalúrgica: 6 orçamentos e 3 pedidos no seed, como no protótipo |
| T32 Orçamentos filtrados | [IMPLEMENTADO] | |
