# 04 — Relatório final (MVP 1)

## 1. Resumo

O MVP 1 do Zalek360 foi implementado ponta a ponta, como sistema interno da Zalek Personalizados:
- API ASP.NET Core 8 em Clean Architecture;
- persistência EF Core + PostgreSQL;
- interface Next.js 16 fiel aos 32 protótipos;
- execução por `docker compose up --build`.

O fluxo completo está coberto por código e testes:
- pesquisar e cadastrar o cliente;
- criar o orçamento com personalização e artes;
- apresentar ao cliente e registrar contatos e decisão;
- gerar o pedido automaticamente na aprovação, em transação;
- acompanhar o pedido e consultar o histórico.

O ambiente de construção **não tinha acesso ao NuGet nem ao Docker**. A verificação foi feita por:
- compilação real das camadas sem pacotes externos;
- compilação das demais camadas contra stubs de assinatura;
- execução de 41 testes;
- execução da DDL em PostgreSQL 16;
- build e simulação da imagem do frontend.

A execução da API real e do compose completo fica como primeira ação na máquina de vocês (item 15).

## 2. Escopo entregue × escopo do MVP 1

| Requisito | Situação |
|---|---|
| RF 4.1.1.1–4.1.1.2 (cadastro PF/PJ, sem duplicidade) | Implementado |
| RF 4.1.2.1–4.1.2.7 (orçamento, personalização, cálculo, prazo e pagamento, ciclo, edição, consulta por cliente) | Implementado |
| RF 4.1.3.1–4.1.3.5 (conversão automática, sem pedido direto, situação inicial, cancelamento, sem exclusão) | Implementado. Situação inicial "Aberto" (ver item 16) |
| RF 4.1.4.1–4.1.4.2 (histórico por cliente, painel por situação) | Implementado |
| RF 4.1.5.x (estoque, OS, financeiro) | Fora do MVP 1, por definição do documento |
| RNF 5.1.1–5.1.3, 5.2.1–5.2.2, 5.3.2, 5.4.1–5.4.3 | Implementados (HTTPS via proxy reverso de exemplo) |

## 3. Arquitetura e organização

- **Domain:** entidades com comportamento, sem dependências.
  - Agregados `Cliente`, `Orcamento` e `Pedido`.
  - Serviços de domínio `CalculadoraOrcamento` e `CalendarioDiasUteis`.
  - Validações com lista de erros por campo.
- **Application:** um serviço por módulo (`AuthService`, `ClienteService`, `OrcamentoService`, `PedidoService`, `AnexoService`, `ExpiracaoOrcamentoService`), DTOs e interfaces de repositório, consulta e infraestrutura.
  - Consultas de leitura (CQRS leve) retornam DTOs prontos para as telas.
- **Infrastructure:** tudo que depende de tecnologia.
  - `ZalekDbContext`, mapeamentos e convenção de nomes, migration, seed e inicializador;
  - repositórios e consultas;
  - JWT, BCrypt, numerador por sequência, armazenamento local de anexos;
  - rotinas em segundo plano (expiração e limpeza de anexos pendentes).
- **Api:** controllers finos, autenticação por cookie ou Bearer, políticas por perfil, middleware de erros e Swagger.
- **Frontend:**
  - `app/` (rotas), `features/` (telas por módulo), `components/` (UI e layout);
  - `services/` (Axios), `hooks/` (TanStack Query), `lib/`, `types/`, `utils/`;
  - nenhum dado de negócio fixo no código: listas, opções, motivos e responsáveis vêm da API.

## 4. Tecnologias e versões

| Camada | Tecnologia |
|---|---|
| Backend | .NET 8, ASP.NET Core Web API, EF Core 8.0.10, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.10, BCrypt.Net-Next 4.0.3, System.IdentityModel.Tokens.Jwt 7.1.2, JwtBearer 8.0.10, Swashbuckle 6.6.2 |
| Testes | xUnit 2.9.2, Microsoft.NET.Test.Sdk 17.11.1, EF Core Sqlite 8.0.10, coverlet |
| Banco | PostgreSQL 16 |
| Frontend | Next.js 16.3 (App Router, output standalone), React 19, TypeScript 5.9, Tailwind CSS 3.4, TanStack Query 5, Axios 1.20, Radix UI Dialog, lucide-react, IBM Plex Sans/Mono (@fontsource) |
| Infra | Docker (imagens oficiais `dotnet/sdk:8.0`, `dotnet/aspnet:8.0`, `node:22-bookworm-slim`, `postgres:16-alpine`), Docker Compose, exemplo Nginx |

O shadcn/ui era preferível, mas a CLI não pôde ser usada no ambiente. Os componentes (`components/ui`) foram escritos no mesmo padrão: Tailwind + Radix, com variantes e acessibilidade de teclado no modal.

## 5. Banco de dados

Doze tabelas:
- `usuarios`, `produtos`, `clientes`, `observacoes_cliente`;
- `orcamentos`, `itens_orcamento`, `arquivos_anexos`, `contatos_orcamento`, `decisoes_orcamento`;
- `pedidos`, `itens_pedido`, `historico_eventos`.

A estrutura inclui 2 sequências, 47 índices (7 únicos), 22 chaves estrangeiras e 7 checks. Dinheiro é `numeric(10,2)`, e datas e horas são gravadas em UTC.

As diferenças em relação ao DER e ao dicionário estão em `03-alteracoes-e-complementos-na-modelagem.md` e `06-revisao-da-modelagem.md`.

## 6. API

- **Endpoints:** REST em `/api`; a lista completa está no README e no Swagger.
- **Autenticação:**
  - `POST /api/auth/login` grava o JWT num cookie httpOnly `zalek360_token` e também o devolve no corpo, para uso no Swagger;
  - todas as demais rotas exigem autenticação por política de fallback.
- **Formato de erro, em todas as falhas:**

```json
{ "status": 409, "code": "CLIENTE_CPF_CNPJ_DUPLICADO", "message": "Já existe um cliente cadastrado com este CPF/CNPJ.",
  "errors": { "documento": ["Este CPF/CNPJ já está cadastrado."] } }
```

- **Status HTTP:**
  - validação → 400;
  - não autenticado → 401;
  - perfil sem permissão → 403;
  - não encontrado → 404;
  - duplicidade, transição inválida ou versão desatualizada → 409;
  - regra de negócio → 422 (ex.: `PEDIDO_CRIACAO_DIRETA_NAO_PERMITIDA`);
  - arquivo grande → 413;
  - erro inesperado → 500, com `traceId` para suporte.
- **Códigos de erro:** 31 no total, definidos em `Domain/Common/CodigosErro.cs`.
- **Mensagens de validação do ASP.NET** (por exemplo, JSON malformado) são traduzidas para o mesmo formato.

## 7. Telas

Todas as 32 telas do protótipo têm correspondente funcional; o mapa está em `02-matriz-rastreabilidade.md`.

- **Identidade visual:**
  - menu lateral escuro (`#0A1120`) com item ativo em `#172033`;
  - azul primário `#1D4ED8`, fundo `#F5F7FA`, bordas `#E3E7EE`;
  - IBM Plex Sans para texto e IBM Plex Mono para números e códigos;
  - menu apenas com Dashboard, Clientes, Orçamentos e Pedidos;
  - rodapé "Zalek Personalizados · Ambiente interno · colaboradores" e botão Sair.
- **Estados:** carregamento (esqueletos), vazio com ação sugerida, erro com "Tentar novamente", validação por campo e notificações de sucesso.
- **Responsividade:** desktop em primeiro lugar, com tabelas roláveis e grades que se reorganizam em telas menores.
- **PWA preparado:** `manifest.webmanifest`, ícone e metadados. O service worker e o modo offline não foram incluídos.
- **Adições justificadas:**
  - aba **Histórico** no cliente;
  - campo **Cargo do contato**;
  - botão **Cancelar orçamento** em Em Elaboração;
  - modal **Apresentar ao cliente** (meio opcional);
  - bloqueio de troca do cliente na edição do orçamento;
  - período padrão "Últimos 30 dias" nas listas de orçamentos e pedidos;
  - selo "Novo" no pedido criado hoje;
  - tela explicativa em `/pedidos/novo`.

## 8. Regras de negócio

As RN1–RN10 estão implementadas no **domínio**, e não apenas na tela. A numeração consolidada e o local de cada garantia estão em `01-analise-inicial.md`. Exemplos:
- o CPF/CNPJ é validado (DV) no domínio e protegido por índice único;
- o total é sempre recalculado no servidor;
- o pedido só pode ser construído por `Pedido.CriarAPartirDe(orcamentoAprovado)`.

## 9. Casos de uso

UC01 a UC05 estão implementados, com fluxos principais e alternativos:
- UC01 A1: documento duplicado;
- UC02 A1: orçamento sem item;
- UC03 A1, A2 e A3: recusa, expiração e edição;
- UC05 A1: cliente sem histórico.

Ver `02-matriz-rastreabilidade.md` e `05-checklists.md`.

## 10. Transações, concorrência e integridade (RNF 5.2)

- **Aprovação + pedido em transação única**, na ordem:
  1. registra a decisão;
  2. grava;
  3. obtém o número do pedido na sequência;
  4. cria o pedido copiando itens, personalização e anexos;
  5. grava;
  6. confirma.

  Qualquer falha faz o descarte da transação executar ROLLBACK, e o orçamento continua Aguardando Retorno. A tela de erro diz isso explicitamente.
- **Sem retentativa automática de execução** (`EnableRetryOnFailure` desligado), para não repetir parcialmente uma operação com transação explícita.
- **Um pedido por orçamento:** índice único em `pedidos.orcamento_origem_id`.
- **Concorrência otimista** por `versao` nas alterações de cliente, orçamento e pedido.
- **Histórico** gravado no mesmo `SaveChanges` da alteração que o originou.

## 11. Segurança (RNF 5.4)

- **Senhas:** BCrypt com custo 11. A mensagem de login é genérica ("E-mail ou senha inválidos").
- **JWT:** HS256; emissor, audiência e expiração validados.
  - Validade de 10 h, ou 7 dias com "Manter conectado".
  - A chave precisa ter pelo menos 32 caracteres; a API não inicia sem ela.
- **Cookie:** `httpOnly`, `SameSite=Lax`. `Secure` é configurável e deve ficar ativo com HTTPS.
  - O frontend encaminha `/api` no mesmo domínio, então não há CORS aberto.
- **Perfis:** a política "Comercial" libera Atendente, Gestor e Administrador.
- **Anexos:**
  - lista de extensões permitidas e conferência da assinatura do conteúdo;
  - limite de 20 MB;
  - armazenamento fora da pasta pública, com proteção contra *path traversal*;
  - download apenas autenticado.
- **Cabeçalhos:** `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy`.
- **Containers:** executam como usuário não-root.

## 12. Testes

São 44 testes xUnit: 27 de domínio, 14 de casos de uso e 3 de integração com EF Core + SQLite em memória.
- **Pontos exigidos que estão cobertos:**
  - cliente (DV, obrigatórios, duplicidade);
  - orçamento (sem itens, cálculo do #000123 do protótipo, desconto);
  - contatos (não alteram status);
  - decisão (meio e data obrigatórios, validade, recusa, cancelamento, expiração);
  - aprovação com geração do pedido e **rollback** quando a geração falha, em duas camadas: com fakes e com banco real SQLite;
  - histórico;
  - bloqueio de pedido direto;
  - concorrência.
- **Executados neste ambiente:** 41 de 41 (domínio + casos de uso).
- **Integração:** compilada, não executada (depende do pacote EF Sqlite).

## 13. Dados de demonstração

- **Usuários:**
  - Maria Silva, Carlos Souza e Fernanda Lima (Atendentes);
  - Administrador;
  - senha `Zalek@2026`, configurável.
- **Clientes:** 7 do protótipo, com CPF/CNPJ de DV corrigido. A Academia Movimento é deixada de fora de propósito, para o roteiro "pesquisar → não encontrado → cadastrar".
- **Orçamentos:** 15 (#000098–#000123), cobrindo as seis situações (7 aprovados, 1 recusado, 1 cancelado, 1 expirado, os demais em elaboração ou aguardando retorno), com itens, personalização, contatos, decisões e histórico.
- **Pedidos:** 7 (#000044 e #000051–#000056), em Em produção, Pronto, Entregue e Cancelado. A situação Aberto aparece com o #000057, gerado ao aprovar o #000124 no roteiro, como no protótipo.
- **Próximos números:** #000124 e #000057, como no protótipo.
- **Datas relativas** ao dia da execução.
- **Diferença em relação ao protótipo:** as telas mostram totais de uma operação já madura (por exemplo, 57 pedidos, 45 entregues, R$ 86.420,00 aprovados no mês), mas o seed contém apenas os registros que aparecem nas telas. Os indicadores exibem os valores reais do banco, sem números fixos no código.

## 14. Execução e configuração

- **Docker:** `docker compose up --build` sobe banco, API e frontend, com healthcheck do banco e volumes para dados e anexos. Funciona sem `.env`, usando padrões de demonstração.
- **Local:** `dotnet run` e `npm run dev`; ver README.
- **Configuração** por variáveis de ambiente, com `.env.example` na raiz, no backend e no frontend: banco, JWT, cookie, fuso, seed, rotinas, Swagger e portas.
- **Inicialização da API:**
  1. espera o banco (até 30 tentativas);
  2. aplica a migration;
  3. confere o esquema;
  4. grava o seed (uma única vez);
  5. ajusta as sequências.

## 15. Verificações realizadas e não realizadas

| Item | Como | Resultado |
|---|---|---|
| Domain + Application | `dotnet build` (SDK 8.0.131) | 0 erros / 0 avisos |
| Infrastructure + Api + Tests | Compilação contra stubs de assinatura (`backend/tools/verificacao-offline/verificar.sh`) | 0 erros / 0 avisos |
| Testes de domínio e de casos de uso | Executor compatível com xUnit (offline) | 41/41 |
| Modelo EF × migration | Script de conferência (colunas, tipos, nulidade, tamanhos) | 0 divergências; 3/3 erros plantados detectados |
| DDL | Migration traduzida para SQL e executada no PostgreSQL 16 | 12 tabelas, 47 índices, 22 FKs, 7 checks; sequência e unicidade conferidas |
| Riscos de execução do EF/Npgsql | Auditoria manual | Datas UTC, consultas traduzíveis, construtores e campos de apoio, `ValueGenerated.Never` nas chaves Guid: sem pendências |
| Frontend | `tsc --noEmit` + `next build` | Sem erros; 15 rotas; proxy reconhecido |
| Imagem do frontend | Etapas do Dockerfile reproduzidas + backend simulado | Cookie, erros 422 e upload de 21 MB íntegros |
| Testes de integração (SQLite) | — | **Não executados** (sem NuGet) |
| API real com EF/Npgsql + PostgreSQL | — | **Não executada** (sem NuGet) |
| `docker compose up --build` | — | **Não executado** (sem Docker) |

## 16. Limitações conhecidas e divergências em relação ao documento

1. **Execução ponta a ponta da API e do compose não realizada neste ambiente** (item 15). É a principal pendência de verificação.
2. **A migration inicial foi escrita à mão**, sem `ModelSnapshot`. Funciona para criar o banco. Antes da próxima mudança de esquema, regenere com `scripts/recriar-migration-inicial.sh` em banco novo.
3. **Pedido inicia "Aberto"**, com avanço manual já no MVP 1 (protótipo). O RF 4.1.3.3 previa "Registrado" e avanço só no MVP 2.
4. **"Enviado ao Cliente" virou "Aguardando Retorno"** (protótipo e dicionário).
5. **Exportar orçamento em PDF** (UC03, condição de entrada) **não implementado.** A apresentação ao cliente é registrada pela ação "Apresentar ao cliente".
6. **Exceção do RF 4.1.3.2** (pedido de produto pronto, sem orçamento) **não implementada.** Todo pedido nasce de orçamento, como exigido na entrega.
7. **Sem limitação de tentativas de login** (*rate limiting*). Não há telas de gestão de usuários nem de troca de senha: os usuários vêm do seed.
8. **Anexos em disco local** (volume Docker). Adequado a uma instância; várias instâncias exigem armazenamento compartilhado (ex.: S3).
9. **PWA apenas preparado** (manifest e ícone), sem service worker.
10. **Sem testes automatizados de frontend** nem das consultas de leitura (dashboard e listas); essas partes são cobertas pelo roteiro manual.
11. Os **indicadores do protótipo** (57 pedidos etc.) não são reproduzidos pelo seed (item 13).
12. **Filtro "Status" das listas:** o protótipo mostra um seletor além das abas; a implementação usa só as abas com contagem, que aplicam o mesmo filtro.

## 17. Próximos passos recomendados

1. Em máquina com internet:
   - `./backend/scripts/testar.sh` (44 testes, incluindo integração);
   - `docker compose up --build`;
   - roteiro do README, seção 3.
2. Regenerar a migration com `dotnet ef`, para ter o `ModelSnapshot`.
3. Testes de integração da API com `WebApplicationFactory` + PostgreSQL (Testcontainers) e testes de interface (Playwright) do roteiro.
4. Exportação do orçamento em PDF e envio por link ou WhatsApp.
5. *Rate limiting* no login, gestão de usuários e troca de senha.
6. MVP 2: integrar a aprovação com a reserva de estoque (RF 4.1.5.1) e a geração da OS (RF 4.1.5.2), usando as mesmas transições de status do pedido.
