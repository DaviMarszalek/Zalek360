# Plano dos agentes — pacote de melhorias de cadastro, pedidos e estoque

**Início:** 28/11/2026. **Autorização:** o usuário autorizou executar o plano inteiro (Partes A–F do pedido) sem esperar, incluindo merges no `main`, respeitando os limites absolutos e os portões de qualidade.
**Como retomar:** ler `AGENTS.md`, este arquivo (seções 1 e 9) e conferir `git log`/PRs no GitHub antes de agir.

> Nome do arquivo: o pedido chamou este documento de `docs/11-plano-agentes.md`. Já existe `docs/11-atualizar-ambiente-principal.md`; os dois convivem e `docs/10` §G aponta para este plano.

## 1. Estado atual (checkpoint)

| Onda | Situação | Branch / PR |
|---|---|---|
| Pré-etapa (Parte C) | **Já concluída antes deste plano**: PR #3 (Fase 1, R1 definitiva) e PR #4 (AGENTS.md + docs/10) mesclados em 24/11/2026. Nada a refazer. | — |
| Onda 0 — plano, contratos, linha de base | concluída | `integracao/melhorias-cadastro-pedidos` (inclui `item/00-base-e2e-estavel`) |
| Onda 1 | em andamento: Banco (M1 busca, `item/06a-busca-modelo`), Pedidos (`item/03-alertas-pedidos`, `item/10-estoque-abas`), Clientes (`item/05-ie-cliente`, `item/02-cnpj`). Pesquisa entra após a M1 integrada. | — |
| Onda 2 | pendente | — |
| Onda 3 | pendente | — |

## 2. Reconciliação do pedido com o repositório

O pedido foi escrito antes das Fases 2–7. O que cada item encontra hoje:

| Item | Situação no código | Consequência |
|---|---|---|
| 1 Endereços/contatos | Novo. `Cliente` tem 1 endereço e 1 contato em colunas próprias. | Tabelas novas; colunas atuais viram espelho do principal. |
| 2 Consulta CNPJ | Novo. Só existe `GET /api/clientes/verificar-documento`. | Serviço novo no backend. |
| 3 Alertas pedidos | Parcial. `PendenciaFichaTecnicaPedido`, reservas parciais e falta por item já existem no detalhe; a lista não sinaliza. | Só consulta/DTO/tela. |
| 4 Dados da empresa | Parcial. PDF e proposta leem `EmpresaOptions` (`Empresa__*`). Configurações já existe (opções e modelos). | Tabela nova; `Empresa__*` vira fallback. |
| 5 IE do cliente | Novo na exibição: IE é gravada, mas não aparece no orçamento, PDF e pedido. | Sem banco. |
| 6 Pesquisa | Parcial. Clientes por `busca_normalizada`; orçamentos por número/cliente; pedidos por número/cliente. | Ampliar consultas e colunas normalizadas. |
| 7 NF emitida | Novo. Fluxo atual Aberto → Em produção → Pronto → Entregue (`Pedido.ProximaSituacao`). | Novo valor de enum (gravado como texto). |
| 8 Referência | Novo (docs/10 §F listava "código único" sem fase). | Colunas + sequências. |
| 9 Listas gerenciáveis | Parcial. `OpcaoCatalogo` com `TipoPersonalizacao`, `CondicaoPagamento`, `MotivoDecisao`, `CategoriaProduto`; Configurações (Admin) já gerencia. Cor da peça, cores da arte e local de aplicação são texto livre nos itens; matéria-prima não tem categoria. | Novos tipos de opção + colunas de referência. |
| 10 Estoque em abas | Parcial. `Estoque.tsx` em tela única com Repor estoque, compras, fichas e matérias-primas. Movimentos só por matéria-prima. | Tela em abas + consulta global de movimentos. |

## 3. Linha de base (antes de qualquer mudança, `main` em `b5504f7`)

- Backend: **162 aprovados, 0 falhas, 0 ignorados** (PostgreSQL real, ~2,5 min).
- Frontend: typecheck OK; Vitest **50/50** nas 3 execuções; build OK (`npm ci` + tudo ~6 min).
- E2E Edge: **22/24** (~7,7 min). Falhas pré-existentes, que passam isoladas:
  - `acabamento.spec.ts:118` — corrida no "Sair": `qc.clear()` refaz `/auth/me`, o 401 aciona o redirecionamento de sessão expirada e a URL fica `/login?redirect=...`. Corrigido em `item/00-base-e2e-estavel` (ver seção 8).
  - `catalogo.spec.ts:59` — opção recém-criada não aparece na lista sob carga. Em investigação.
- Backup do banco da revisão antes das migrations: `C:\dev\zalek360-agentes\backups\banco-zalek360-revisao-20261128-132714.dump` (22 tabelas com dados) e anexos (81 arquivos). Fora do repositório.

## 4. Regras para todos os agentes (vão em cada briefing)

1. Trabalhar **somente** no worktree `C:\dev\zalek360-agentes\<agente>` e nas branches `item/NN-*` indicadas.
2. **Nunca** executar `docker compose` (o `docker-compose.yml` declara `name: zalek360`: sem `-p`, atinge o projeto principal). Nunca `docker system/volume/network prune`. Só `docker run`/`docker rm`/`docker network create|rm`/`docker volume rm` com nomes exatos `zalek360-agente-<agente>-*`.
3. Portas proibidas: 3000, 5000, 5432, 3100, 5100, 55432, 5180. Cada agente usa a sua (tabela da seção 6).
4. Nunca ler, copiar ou exibir `.env`, `.env.revisao` ou seus `*.anterior/*.novo`. Bancos dos agentes usam credenciais descartáveis.
5. Migrations, `ZalekDbContextModelSnapshot`, `Mapeamentos`/`*Map.cs`, `ZalekDbContext`, entidades, enums e seed: **somente o Agente Banco**. Os demais pedem mudanças de modelo ao orquestrador.
6. `frontend/types/api.ts` e `frontend/services/index.ts`: só **acrescentar** o que o contrato define. `AppShell`, `Program.cs`, `ConfiguracaoServicos.cs`, `DependencyInjection.cs` e políticas: orquestrador (o agente pode propor a linha exata no relatório).
7. Commits pequenos em português, terminando com `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Sem push: o orquestrador integra.
8. Antes de entregar: testes backend com o PostgreSQL próprio (0 falhas, 0 ignorados), `npm run typecheck` e `npm test` no worktree. E2E, build completo e Compose: só orquestrador/QA no `zalek360-revisao`.
9. Nada de chamadas reais a provedores externos em testes.

### Testes backend no worktree do agente

```powershell
docker network create zalek360-agente-<a>-net
docker run -d --name zalek360-agente-<a>-db --network zalek360-agente-<a>-net --network-alias db `
  -e POSTGRES_USER=agente -e POSTGRES_PASSWORD=agente-descartavel-123 -e POSTGRES_DB=postgres -p <porta>:5432 postgres:16-alpine
docker run --rm --network zalek360-agente-<a>-net -e POSTGRES_USER=agente -e POSTGRES_PASSWORD=agente-descartavel-123 `
  --mount "type=bind,source=C:\dev\zalek360-agentes\<a>\backend,target=/src" `
  --mount type=volume,source=zalek360-agente-<a>-nuget,target=/root/.nuget/packages -w /src `
  mcr.microsoft.com/dotnet/sdk:8.0 sh -c "sed 's/\r$//' scripts/testar-revisao.sh > /tmp/t.sh && sh /tmp/t.sh"
```

## 5. Contratos (definidos antes dos agentes)

Convenções: tabelas/colunas em `snake_case` (convenção existente), enums gravados como **texto** (máx. 30), novos valores de enum sempre **acrescentados** sem renumerar. Erros no envelope `{status, code, message, errors}`. Normalização de busca: `TextoBusca.Normalizar` (trim, `ToLowerInvariant`, mapa fixo `áàâãäåéèêëíìîïóòôõöúùûüçñý → aaaaaaeeeeiiiiooooouuuucny`). Em SQL, a expressão equivalente obrigatória é:

```sql
-- zalek_normalizar(x): traduz as maiúsculas acentuadas antes do lower (o banco pode ter ctype C)
translate(lower(translate(btrim(x), 'ÁÀÂÃÄÅÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇÑÝ', 'AAAAAAEEEEIIIIOOOOOUUUUCNY')),
          'áàâãäåéèêëíìîïóòôõöúùûüçñý', 'aaaaaaeeeeiiiiooooouuuucny')
```

Toda migration que gravar texto normalizado usa essa expressão e há teste de paridade C# × SQL.

### Item 1 — Endereços e contatos

- `TipoEnderecoCliente { Principal = 1, Entrega = 2, Cobranca = 3, Outro = 4 }`.
- Tabela `clientes_enderecos`: `id`, `cliente_id` (FK cascade), `tipo` (texto), `apelido` (60, null), `cep` (8), `logradouro` (150), `numero` (20), `complemento` (100), `bairro` (100), `cidade` (100), `uf` (2), `principal` (bool), `ordem` (int). Índice único parcial `(cliente_id) WHERE principal`.
- Tabela `clientes_contatos`: `id`, `cliente_id` (FK cascade), `nome` (100, null), `cargo` (60, null), `telefone` (11, null), `whatsapp` (11, null), `email` (150, null), `principal`, `ordem`. Índice único parcial `(cliente_id) WHERE principal`.
- As colunas atuais de endereço/contato em `clientes` **continuam** e passam a ser **espelho do principal**, mantido pelo domínio. Quem já lê essas colunas continua funcionando.
- Migration: para cada cliente, cria 1 endereço principal (tipo `Principal`) se houver qualquer campo de endereço, e 1 contato principal com `contato_nome`, `contato_cargo`, `telefone`, `whatsapp`, `email`. Nada é apagado.
- Regras: exatamente um principal em cada lista quando a lista não estiver vazia; remover o principal promove o próximo por `ordem`; ao menos um meio de contato (telefone, WhatsApp ou e-mail) em algum contato; validações de tamanho/formato atuais aplicadas por item (erros com chave `enderecos[i].campo` / `contatos[i].campo`).
- API (`ClienteRequest`): acrescenta `Enderecos: EnderecoClienteRequest[]?` e `Contatos: ContatoClienteRequest[]?`. **Compatibilidade:** se as listas vierem `null`, os campos planos atuais montam o principal (testes e clientes antigos continuam válidos); se vierem, os campos planos são ignorados.
  - `EnderecoClienteRequest(Guid? Id, TipoEnderecoCliente? Tipo, string? Apelido, string? Cep, string? Logradouro, string? Numero, string? Complemento, string? Bairro, string? Cidade, string? Uf, bool Principal)`
  - `ContatoClienteRequest(Guid? Id, string? Nome, string? Cargo, string? Telefone, string? WhatsApp, string? Email, bool Principal)`
- `ClienteDetalheDto` acrescenta `Enderecos: EnderecoClienteDto[]` e `Contatos: ContatoClienteDto[]` (com `Id`, campos, `Principal`, `Ordem`); campos planos atuais = principal.
- Orçamento: `orcamentos.endereco_entrega_id` e `orcamentos.contato_cliente_id` (FK nulas, `ON DELETE SET NULL`). `null` = principal. `OrcamentoRequest` acrescenta `EnderecoEntregaId?` e `ContatoClienteId?` (devem pertencer ao cliente → 400). `OrcamentoDetalheDto` acrescenta `EnderecoEntrega: EnderecoClienteDto?` e `ContatoCliente: ContatoClienteDto?` já resolvidos (selecionado ou principal). PDF e mensagens usam os resolvidos.
- Busca de clientes considera todos os endereços e contatos (via `busca_normalizada`, item 6).

### Item 2 — Consulta de CNPJ

- `GET /api/clientes/cnpj/{cnpj}` (`Operacao`). CNPJ com 14 dígitos e DV válido; senão 400 `CNPJ_INVALIDO`.
- Resposta sempre 200 quando o CNPJ é válido: `ConsultaCnpjDto(bool Encontrado, string? Fonte, string? RazaoSocial, string? NomeFantasia, string? Telefone, string? Email, EnderecoDto? Endereco, IReadOnlyList<InscricaoEstadualDto> InscricoesEstaduais, string? Mensagem)`; `InscricaoEstadualDto(string Numero, string Uf, bool Ativa)`. Falha, timeout ou limite → `Encontrado=false` e `Mensagem` amigável (cadastro manual, sem erro).
- Interface `IConsultaCnpj` (Application) e implementação na Infrastructure: `HttpClient` nomeado, hosts fixos, timeout 5 s por provedor, cache em memória de 10 min por CNPJ, fallback BrasilAPI → CNPJá pública para dados; CNPJá pública → CNPJ.ws para IE. Endpoints e formatos confirmados na documentação pública antes de implementar (nunca com o CNPJ da empresa).
- Testes só com `HttpMessageHandler` simulado: sucesso, sem IE, várias IEs, timeout, limite (429).
- Tela: botão "Buscar dados do CNPJ" e busca ao sair do campo (PJ, CNPJ válido); painel "Dados encontrados" com "Aplicar", nunca sobrescreve campo preenchido sem confirmação; escolha da IE (prioriza a UF do endereço); aviso de dados públicos possivelmente desatualizados.

### Item 3 — Alertas na lista de pedidos

- `PedidoListaItemDto` acrescenta `Alertas: AlertasPedidoDto(bool SemFichaTecnica, bool FaltaMaterial, IReadOnlyList<string> MateriaisEmFalta, bool PrazoVencido)`, calculados na mesma consulta (sem N+1), só para pedidos não entregues/cancelados.
  - `SemFichaTecnica`: existe pendência de ficha técnica do pedido.
  - `FaltaMaterial`: alguma reserva com quantidade pendente (mesma regra do detalhe/materiais); `MateriaisEmFalta` = nomes com a quantidade faltante.
  - `PrazoVencido`: prazo de entrega < hoje (fuso do sistema).
- `PedidoFiltro` acrescenta `ComPendencia: bool?`. `IndicadoresDto` (dashboard) acrescenta `PedidosComPendencia: int`.

### Item 4 — Dados da empresa

- Tabela `dados_empresa` (linha única): `id`, `nome_fantasia` (100), `razao_social` (150), `cnpj` (14), `inscricao_estadual` (20), `cep`, `logradouro`, `numero`, `complemento`, `bairro`, `cidade`, `uf`, `telefone`, `whatsapp`, `email` (150), `site` (150), `logo_anexo_id` (FK `arquivos_anexos`, null), `versao`, `atualizado_em`, `atualizado_por`. Todos opcionais.
- `GET/PUT /api/configuracoes/empresa` (`Administracao`) e `GET /api/empresa` (`Operacao`, só dados públicos + `Configurado: bool`). Logo pelo fluxo de anexos existente.
- Precedência: valor do banco → `Empresa__*` (fallback/valor inicial, documentado) → vazio. PDF nunca quebra sem dados; Administrador vê "Configure os dados da empresa" no detalhe do orçamento.
- Seed de demonstração: apenas valores de exemplo neutros.

### Item 5 — IE do cliente

- Resumos de cliente no orçamento, pedido e documento do PDF acrescentam `InscricaoEstadual` (PJ). Exibida junto do CNPJ. Sem banco.

### Item 6 — Pesquisa universal

- Colunas `busca_normalizada` (texto, default `''`) em `produtos`, `materias_primas`; em `itens_orcamento` a coluna `produto_nome_normalizado`. `clientes.busca_normalizada` passa a incluir fantasia, razão social, IE, cidades, logradouros e todos os contatos. Preenchidas pelo domínio ao salvar; backfill na migration com a expressão SQL da seção 5.
- Índices: GIN `pg_trgm` nessas colunas se a extensão for aceita pelo comparador; senão, sem índice e registrado (volume pequeno).
- Clientes: nome, razão, fantasia, CPF/CNPJ com/sem máscara, telefones, e-mails, cidade, IE.
- Produtos: nome, referência, descrição, matérias-primas da composição.
- Orçamentos: número (`124`, `000124`, `#000124`), cliente, produtos dos itens, status (rótulo), data (`25/11/2026`; `25/11` = esse dia/mês em qualquer ano) e valor.
- **Regra de valor (decidida pelo usuário):** termo numérico encontra orçamentos com valor total **igual** (`50` → R$ 50,00; `50,5`/`50.5` → R$ 50,50; aceita `R$` e `1.234,56`) **e** números de orçamento que **contenham** o termo.

### Item 7 — Nota fiscal emitida

- `SituacaoPedido.NotaFiscalEmitida = 6` (texto `NotaFiscalEmitida`). Fluxo: Aberto → Em produção → Pronto → **Nota fiscal emitida** → Entregue. Etapa obrigatória: Pronto não vai direto para Entregue. Cancelamento continua permitido em Nota fiscal emitida (igual a Pronto).
- Colunas `pedidos.numero_nota_fiscal` (20, null) e `pedidos.data_emissao_nota_fiscal` (date, null). `AtualizarSituacaoPedidoRequest` acrescenta `NumeroNotaFiscal?` e `DataEmissaoNotaFiscal?`, aceitos só na transição para NF; registrados no histórico. O sistema não emite nota.
- Rótulo: "Nota fiscal emitida". Telas, abas/filtros, dashboard e testes atualizados.

### Item 8 — Referência

- `produtos.referencia` e `materias_primas.referencia` (20, único, obrigatório após a migration). Sequências `seq_referencia_produto`/`seq_referencia_materia_prima`. Formato gerado `PRD-0001`/`MP-0001`; editável (maiúsculas, `[A-Z0-9-]`, 1–20, único → 409 `REFERENCIA_DUPLICADA`).
- Migration gera para os existentes por data de criação e ajusta as sequências.
- Snapshot `itens_orcamento.produto_referencia` e `itens_pedido.produto_referencia` (null; backfill pelo produto atual).
- Requests de produto e matéria-prima acrescentam `Referencia?` (vazio = gerar). DTOs, selects, orçamento, pedido e PDF exibem.

### Item 9 — Listas gerenciáveis

- `TipoOpcaoCatalogo` acrescenta `CorPeca`, `CorArte`, `LocalAplicacao`, `CategoriaMateriaPrima` (`CategoriaProduto` já existe).
- Colunas de referência (FK `opcoes_catalogo`, null, `ON DELETE RESTRICT`): `itens_orcamento.cor_peca_opcao_id`, `cor_arte_opcao_id`, `local_aplicacao_opcao_id`; o mesmo em `itens_pedido`; `materias_primas.categoria` (50, null) e `materias_primas.categoria_opcao_id`.
- Itens continuam guardando o **texto** (`cor_peca`, `cores_arte`, `local_aplicacao`) no momento do uso, além da referência.
- `ItemOrcamentoRequest` acrescenta `CorPecaOpcaoId?`, `CorArteOpcaoId?`, `LocalAplicacaoOpcaoId?`; com id, o texto passa a ser o nome da opção ativa; sem id (compatibilidade), o texto é aceito e associado à opção ativa de mesmo nome normalizado, se existir.
- `POST /api/catalogo/opcoes` (`Operacao`) para "Cadastrar nova opção" no orçamento, limitado a `CorPeca`, `CorArte`, `LocalAplicacao`. Configurações (Admin) lista os novos grupos; opções usadas só são inativadas.
- Migration: agrupamento **conservador** dos textos existentes por chave = normalização da seção 5 com espaços colapsados (variações como "Preto"/"preta" ficam separadas); nome da opção = grafia mais frequente (empate: ordem alfabética). Relatório do mapeamento na seção 7 e consulta somente leitura para o usuário rodar no banco principal antes de atualizar.

### Item 10 — Estoque em abas

- Abas `?aba=materias-primas | produtos | compras | movimentacoes | repor` (padrão `materias-primas`). Carregamento, vazio e erro por aba.
- `GET /api/estoque/movimentos?pagina=&tamanhoPagina=&materiaPrimaId=` (`Operacao`), paginado, mais recentes primeiro (`PaginaDto<MovimentoMateriaPrimaDto>`).

## 6. Organização

| Agente | Worktree | Porta do banco | Itens |
|---|---|---|---|
| Banco e Domínio | `C:\dev\zalek360-agentes\banco` | 56101 | modelo de 1, 4, 7, 8, 9 e colunas/índices de 6 |
| Pedidos e Estoque | `C:\dev\zalek360-agentes\pedidos` | 56102 | 3, 10 (onda 1); 7 telas/transições (onda 2) |
| Pesquisa | `C:\dev\zalek360-agentes\pesquisa` | 56103 | 6 |
| Clientes e CNPJ | `C:\dev\zalek360-agentes\clientes` | 56104 | 5, 2 backend (onda 1); 1 e 2 telas (onda 2) |
| Configurações e Catálogo | `C:\dev\zalek360-agentes\catalogo` | 56105 | 4, 8 telas, 9 telas (onda 2) |
| QA | `C:\dev\zalek360-agentes\qa` | 56106 | revisão, e2e de aceite, matriz 200/403, regressão |

- Máximo de 3 agentes ao mesmo tempo. Uma branch por item (`item/NN-nome`), a partir da integração, para que um item bloqueado fique de fora sem travar a onda.
- O orquestrador integra por merge (sem rebase/force), roda os portões no `zalek360-revisao` e abre um PR por onda da integração para o `main`.

## 7. Mapeamento das listas (item 9)

Preenchido após a migration no banco da revisão.

## 8. Decisões tomadas sem o usuário

| # | Decisão | Por quê | Como desfazer |
|---|---|---|---|
| D1 | Parte C não refeita: PR #3 e #4 já mesclados e R1 já vigente. | Evitar retrabalho. | — |
| D2 | Branches remotas mescladas **não** são apagadas. | O pedido manda apagar, mas o AGENTS.md proíbe apagar branches remotas; prevalece o mais conservador. | Apagar manualmente no GitHub. |
| D3 | Este documento usa o nome pedido (`docs/11-plano-agentes.md`), convivendo com `docs/11-atualizar-ambiente-principal.md`. | O pedido usa o nome para retomada. | Renomear e ajustar links. |
| D4 | Colunas atuais de endereço/contato do cliente ficam como espelho do principal. | Migration só aditiva; consumidores atuais seguem funcionando; reversível. | Migration futura que remova as colunas. |
| D5 | Endereço/contato do orçamento por FK `SET NULL`: removido o endereço, o orçamento volta a usar o principal. | Simples e sem perda de cadastro. | Snapshot de endereço no orçamento. |
| D6 | NF emitida: etapa obrigatória; número e data opcionais; cancelamento permitido como em Pronto. O usuário pode querer "pular com motivo" depois. | Decisão padrão do pedido. | Permitir transição Pronto → Entregue com motivo. |
| D7 | Consulta de CNPJ responde 200 com `Encontrado=false` em falha/limite. | Pedido: falha vira cadastro manual, sem erro. | — |
| D8 | Agrupamento das listas colapsa espaços, sem removê-los. | "Ignorando espaços" lido do modo que junta menos. | Reagrupar manualmente em Configurações. |
| D9 | Correção da corrida do "Sair" (pré-existente) incluída antes da Onda 1. | E2E da linha de base vermelho bloquearia todos os merges. | Reverter `item/00-base-e2e-estavel`. |
| D11 | Consulta de CNPJ chama BrasilAPI e CNPJá em paralelo (a CNPJá serve a dados e IE) e usa a CNPJ.ws quando a CNPJá não traz IE; cache só de resultado definitivo. Formato da CNPJá pública **não confirmado** (site responde 429 a acessos automatizados); BrasilAPI e CNPJ.ws confirmadas na documentação oficial. | Menor tempo no pior caso; se a CNPJá estiver em outro formato, os dados vêm da BrasilAPI e a IE da CNPJ.ws. | Ordem sequencial em `ConsultaCnpjPublica`. |
| D12 | CNPJ alfanumérico (Receita, 2026) ainda recebe 400: o validador existente só aceita 14 dígitos. | Fora do contrato; exige decisão. | Ampliar `DocumentoFiscal`. |
| D10 | `testes-selenium/` fica sob responsabilidade do QA: o item 1 muda o formulário de cliente. | Manter a suíte verde; o zip do professor já foi entregue e não muda. | — |

## 9. Diário de checkpoints

- 28/11/2026 — Onda 0 iniciada. Integração criada a partir de `main` `b5504f7`. Linha de base registrada (seção 3). Backup da revisão feito.
- 28/11/2026 — `item/00-base-e2e-estavel` integrado: E2E **24/24** após a correção (a falha de `catalogo.spec.ts:59` não se repetiu; segue em observação). Onda 0 concluída. Worktrees `banco`, `pedidos`, `clientes` criados; agentes da Onda 1 iniciados.
- 28/11/2026 — Agente Clientes concluiu: `item/05-ie-cliente` (169 testes backend, Vitest 55/55) e `item/02-cnpj` (194 testes, 32 novos, todos com provedores simulados). Ambos integrados sem conflito (`d62a6d9`).
- Migrations em linha: cada migration do Agente Banco é integrada antes da próxima começar (evita conflito de `ModelSnapshot` entre branches).
