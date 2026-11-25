# 09 — Revisão funcional completa do Zalek360

Data da revisão: 20/11/2026
Branch: `revisao/funcional`
Commit de origem: `f356299`

Esta etapa foi exclusivamente de revisão. Não foram alterados código ou banco principal. Foi criado um ambiente Docker separado chamado `zalek360-revisao`, com banco PostgreSQL próprio e portas próprias.

## Ambiente e testes executados

- Docker Compose: projeto `zalek360-revisao`, sem volumes ou rede compartilhados com o ambiente principal.
- Banco: PostgreSQL 16, database `zalek360_revisao`, porta host `55432`.
- Backend: porta isolada `5100`, aplicação iniciada e migrações aplicadas.
- Frontend: porta isolada `3100`, aplicação iniciada.
- Backend em PostgreSQL real: após a Fase 0, 99 testes aprovados, 0 falhas, 0 ignorados.
- Frontend: `npx tsc --noEmit` aprovado.
- Frontend: build de produção aprovado pelo Compose.
- Vitest: 4/4 em três execuções sequenciais após separar arquivos E2E.
- Docker Compose: imagens, rede, banco, backend e frontend construídos e iniciados.
- Playwright: instalado como dependência de desenvolvimento; Edge do sistema usado via `channel: "msedge"`.
- Playwright de login: 2/2 (Administrador e Gestor) contra o Compose isolado.

## Atualização — Fase 1 e R1 definitiva (24/11/2026)

Implementação na branch `revisao/fase-1-perfis`. Esta fase atualiza os itens R1, R4.8 e parte de R8; os demais itens da matriz permanecem como na revisão inicial.

- Gestor e Administrador: toda a operação diária, incluindo dashboard, clientes, orçamentos e pedidos. Somente Administrador: configurações, usuários e exclusão definitiva de produto nunca usado. Usar Gestor no dia a dia e Administrador somente para configurar. Custo/margem ficam na tela para ambos, nunca no PDF ou em mensagens ao cliente.
- A criação de orçamento define o responsável pelo usuário autenticado. Na edição, preserva o responsável original. Condição de pagamento aceita nulo; detalhes e PDF exibem “A combinar”.
- Formulário de cliente indica a aba inválida, a quantidade de erros e navega até a primeira aba com falha.
- Materiais de pedido e estoque alinham as quantidades em colunas numéricas à direita.
- Revalidação da R1 definitiva em 24/11/2026: backend **105/105** testes aprovados, **0 falhas e 0 ignorados**, executados em container .NET 8 com PostgreSQL real do ambiente `zalek360-revisao`. Responsável automático/condição nula e exportação auditada de PDF são testados para Gestor e Administrador.
- Frontend nesta revalidação: `npm ci` aprovado (0 vulnerabilidades), `npm run typecheck`, Vitest **5/5** em uma execução e `npm run build` aprovados, executados em sequência.
- Causa dos timeouts Vitest: `userEvent.type` simulava tecla por tecla em campos que recalculam/re-renderizam o formulário. Os testes de estado passaram a disparar `change` diretamente; o timeout não foi aumentado.
- Playwright Edge em `http://localhost:3100`: **4/4**, incluindo login/menu dos dois perfis, cadastro de cliente e criação/edição de orçamento pelo Gestor (responsável automático e “A combinar”), 403 em GET/POST usuários e erro da primeira aba inválida no cliente.
- `docker compose -p zalek360-revisao --env-file .env.revisao up --build -d --wait`: concluído; os serviços ficaram saudáveis.
- Migration `CondicaoPagamentoOpcional` preservada: comparação anterior com 0 divergências (18 tabelas, 211 colunas, 66 constraints e 65 índices). O comparador não foi reexecutado nesta revisão de permissões; nenhuma migration/modelo foi alterado.
- Na Fase 1, Configurações e tela de Usuários ficaram nas fases previstas; o 403 de Configurações só passou a ser testável na Fase 2 com seu endpoint real. A tela de Usuários permanece pendente.
- Ainda não verificados: cenário Homem-Aranha completo, validação visual em 360 px/1280 px e navegação/retorno de foco em todos os modais. Alinhamentos de quantidades foram preservados sem alteração de estilos.
- O comando .NET do `AGENTS.md` foi ajustado e validado no PowerShell 5.1: script pela entrada padrão, preservação de aspas e remoção de CRLF sem expor credenciais.

## Atualização — Fase 2: catálogo e opções (24/11/2026)

- Branch `revisao/fase-2-catalogo`, criada de `main` em `9fc55ff` após o merge autorizado do PR #3.
- Produtos passa a ser uma tela própria para os dois perfis, com busca, formulário único de dados/valores/composição, duplicação, inativação/reativação e exclusão definitiva por Administrador apenas quando nunca usado. Estoque oferece acesso à tela Produtos, sem um segundo editor de catálogo.
- Preço/custo são opcionais, não negativos e limitados a duas casas; registros antigos continuam com nulo. Valores e textos dos itens históricos não são reescritos.
- Categorias, personalizações, condições e motivos vêm de `opcoes_catalogo`. Configurações é exclusivo do Administrador; desativação preserva valores históricos. Categorias legadas são incorporadas de forma aditiva.
- Cadastro/edição de produto com composição é transacional. API verifica versão e nome único; exclusão é protegida também pelas FKs existentes.
- Migration via EF: `20261124220121_CatalogoProdutosEOpcoes`. Comparador na rede de revisão: 0 divergências, 19 tabelas, 221 colunas, 67 constraints, 67 índices, 2 sequências. Teste PostgreSQL verifica upgrade de produto/categoria legado sem inventar preço/custo.
- **Validação final da Fase 2:** backend **117 aprovados, 0 falhas, 0 ignorados** com PostgreSQL real; Vitest **5/5**; Edge **8/8**; `npm ci`, typecheck, build local e Compose aprovados, em sequência no ambiente `zalek360-revisao`.
- E2E cobre Gestor criando/editando/duplicando/inativando produto com custo e composição, 403 em Configurações/exclusão, Administrador gerenciando categoria e excluindo produto nunca usado, valores não informados, consulta atrasada durante exclusão, menus e regressões da Fase 1.
- Telas Produtos/Configurações e seus formulários verificados em **360 px e 1280 px**, sem rolagem horizontal; Escape dos modais confirmado. Capturas locais ignoradas pelo Git em `data/revisao-fase2`.
- Upgrade do Compose preservou contagens e hashes dos **8 produtos, 16 orçamentos e 7 pedidos** existentes, antes dos novos dados de E2E. O ambiente principal não foi acessado.
- Consulta de pesquisa anterior à exclusão é cancelada antes de atualizar o catálogo, para não reapresentar o item removido. Preço/custo nulos aparecem como “Não informado”, sem serem convertidos em zero.
- Pendências gerais mantidas: cenário Homem-Aranha completo, inspeção das demais rotas em tamanhos móveis e foco/teclado em todos os modais.
- Na entrega da Fase 2, preço automático, margem, produto no orçamento e miniaturas ficaram para a Fase 3 (atualização abaixo). Tela de Usuários e demais configurações administrativas não foram antecipadas.

## Atualização — Fase 3: orçamento (24/11/2026)

- [PR #6 aberto](https://github.com/DaviMarszalek/Zalek360/pull/6), branch `revisao/fase-3-orcamento`, criada de `main` em `3c827a0` após o merge autorizado do PR #5. Plano aprovado pelo usuário; aguardar novo OK para merge.
- Preço aplicado na seleção do produto, com edição, aviso e restauração. Abrir edição não altera preço histórico; produto sem preço pede valor manual.
- Modal compartilha o cadastro do catálogo e seleciona o novo produto mantendo os dados do orçamento. Descrição/preço já preenchidos no item são preservados.
- Análise interna por API para os dois perfis: total com personalização após desconto menos custo dos produtos, sem despesas adicionais; percentual sobre a venda. Valores incompletos/custo ausente não geram margem fictícia; total zero não gera percentual.
- Custo registrado ao incluir/trocar o produto; edição do mesmo produto mantém o custo e nome históricos, inclusive quando ele foi inativado. Produto inativo não pode ser usado em item novo. Os DTOs da proposta e o gerador de PDF permanecem sem custo/margem.
- Disponibilidade atual por item, na ordem dos itens, incluindo reservas ativas do próprio pedido. Sem reserva ou bloqueio por esta consulta. Produtos sem composição e matérias-primas inativas são indicados.
- Miniaturas autenticadas no formulário/detalhe do orçamento e nas referências do pedido. Link e ícone mantidos para outros formatos; imagem indisponível tem fallback.
- Migration `20261124231443_CustoInternoNoItemOrcamento` adiciona uma coluna nullable, sem backfill. Teste PostgreSQL faz upgrade desde a Fase 2 preservando item e valores; custo histórico segue nulo mesmo com custo atual no catálogo.
- Backend: **124 aprovados, 0 falhas e 0 ignorados**, com PostgreSQL real. Comparador: **0 divergências**, 19 tabelas, 222 colunas, 67 constraints, 67 índices, 2 sequências.
- Vitest **8/8**, incluindo preservação dos campos/anexos, preço/edição/restauração e modal; `npm ci`, typecheck e build local aprovados.
- Compose construído e iniciado com `up --build -d --wait`, exclusivamente na revisão (3100/5100/55432). Contagens e hashes dos **14 produtos, 19 orçamentos, 28 itens e 7 pedidos** existentes foram preservados no upgrade, antes dos E2E.
- Edge **10/10**: mantém as regressões das Fases 1/2 e percorre, para cada perfil, seleção/restauração do preço, produto no modal com composição, margem, falta informativa, gravação, aprovação e miniatura no pedido. Confere a análise após reservar materiais para o próprio pedido e não registra erros JavaScript.
- Formulário/modal, detalhe do orçamento e pedido verificados em **360 px/1280 px**, sem rolagem horizontal da página. Capturas locais ficam ignoradas em `data/revisao-fase3`. A revisão geral de teclado de todos os modais permanece pendente.
- O envio por e-mail/WhatsApp, o cenário Homem-Aranha completo e o acabamento geral continuam nas fases previstas em `docs/10-contexto-e-proximos-passos.md`.

## Atualização — Fase 4: envio de proposta (24/11/2026)

- [PR #7 aberto](https://github.com/DaviMarszalek/Zalek360/pull/7), branch `revisao/fase-4-envio-proposta`, criada de `main` em `3abacc2`, após o merge autorizado do PR #6 e a autorização para iniciar esta fase. Aguardar novo OK para merge.
- Gestor e Administrador revisam destinatário, assunto e mensagem. Com SMTP, a aplicação anexa o PDF e registra apresentação/contato/exportação após aceitação pelo servidor. Aceitação não confirma entrega/leitura.
- Sem SMTP, disponibiliza `mailto`, cópia da mensagem e PDF para anexar manualmente. WhatsApp usa texto revisável e `wa.me`, sem link público do PDF. Cópia/download/abertura não apresentam a proposta; a confirmação externa é explícita. Outros meios de apresentação foram preservados.
- Administrador edita modelos persistidos com campos públicos permitidos: cliente, número, total, validade, prazo e empresa. Custos/margens não são oferecidos; campos desconhecidos são rejeitados. Segredos SMTP são configurados por `Email__*`, sem retorno à UI.
- Tentativa persistida antes do SMTP; chave confirmada retorna recibo sem reenviar. Confirmação incerta não gera retry automático. Bloqueio PostgreSQL e versão impedem envio concorrente da mesma versão; banco e SMTP não formam transação distribuída, por isso uma queda após aceitação pode exigir conferência manual.
- Migration EF `20261125001901_ModelosEEnviosProposta` cria somente modelos e recibos, com sementes aditivas. Comparador: **0 divergências**, 21 tabelas, 235 colunas, 71 constraints, 72 índices e 2 sequências.
- Backend **133 aprovados, 0 falhas, 0 ignorados**, com PostgreSQL real. Inclui upgrade desde a Fase 3, concorrência/duplicação, falha SMTP, permissões, modelo/cabeçalhos inválidos e transporte MIME com Unicode, destinatário, mensagem e anexo.
- `npm ci`, typecheck, Vitest **8/8**, build local e Compose padrão `up --build -d --wait` aprovados em sequência, apenas em `zalek360-revisao`.
- Upgrade preservou hashes e contagens dos **24 produtos, 24 orçamentos, 37 itens e 11 pedidos** existentes, antes dos E2E.
- Edge: **13/13** na suíte completa sem SMTP; execução adicional **3/3** com smtp4dev recebeu destinatário, assunto, texto revisado e anexo PDF reais. Repetir a chave de envio não criou outra mensagem. O envio para conta/provedor real da empresa não foi verificado.
- Nova tela/modal verificada em **360 px e 1280 px**, com confirmação alcançável pela rolagem interna e Escape, sem rolagem horizontal da página. Capturas em `data/revisao-fase4` ficam ignoradas pelo Git. A captura visual aguarda o toast expirar para não fechar o modal ao clicar fora.
- Revisão restaurada à configuração padrão, sem SMTP direto; capturador local permanece disponível em `http://localhost:5180`, sem relay externo. Configurar conta/provedor real e senha de aplicativo continua sob responsabilidade do ambiente seguro; nenhum segredo foi exibido ou versionado.

## Atualização — Fase 5: estoque mínimo e vendas (25/11/2026)

- [PR #8 aberto](https://github.com/DaviMarszalek/Zalek360/pull/8), branch `revisao/fase-5-estoque-vendas`, criada de `main` em `35ce777`, após o merge autorizado do PR #7 e a autorização da Fase 5. Aguardar novo OK para merge.
- Reposição usa `máximo(0, mínimo − saldo livre + pendências de reservas ativas)`. A sugestão bruta fica separada da falta líquida de compra, que desconta compras planejadas/pedidas a receber. Canceladas/recebidas e reservas liberadas/consumidas não são somadas novamente.
- Cartão **Repor estoque** no dashboard, lista dedicada e filtro **Abaixo do mínimo**. Apenas materiais ativos; abaixo do mínimo é estrito, sem incluir igualdade ou mínimo zero com saldo zero.
- Planejar compra do item preenche a falta líquida e permite revisão antes de salvar; na lista de reposição, a ação fica desabilitada quando compras previstas já cobrem a necessidade. Pedidos sem ficha permanecem na lista de pendências, sem quantidade de material inventada.
- Dashboard mostra quantidade e valor de pedidos **Entregue** no mês, pela data da entrega no fuso da empresa. Os dois atalhos de orçamento ficam agrupados sob **Orçamentos em aberto**, mantendo suas ações separadas.
- Pedidos oferece seletor de cliente com busca/teclado, status, responsável, período predefinido/personalizado e referência de data do pedido ou entrega. O link do dashboard usa o mesmo mês por entrega. Datas inclusivas, período invertido rejeitado e contagens/paginação respeitam os demais filtros.
- Backend **139 aprovados, 0 falhas, 0 ignorados**, incluindo PostgreSQL real, limites do mês, cliente/status/paginação, compras previstas e cancelamento. Os cenários criam clientes próprios para não depender dos pedidos do seed.
- `npm ci`, typecheck, Vitest **8/8**, build local e Compose padrão aprovados. Atualização preservou hashes/contagens de **30 produtos, 39 orçamentos, 54 itens, 13 pedidos e 11 matérias-primas**, antes dos E2E.
- Edge **15/15**: regressões das Fases 1–4 e cenários de reposição/compra/cancelamento/recebimento/entrega para ambos os perfis, incluindo sugestão bruta versus falta líquida, compra predefinida, ausência de nova reposição após recebimento e baixa única ao iniciar produção.
- Dashboard, reposição e pedidos verificados em **360 px/1280 px**, sem rolagem horizontal da página; tabelas mantêm rolagem interna. Seletor de cliente usado por teclado e período inválido validado. Capturas locais ignoradas em `data/revisao-fase5`.
- Sem migration ou alteração de modelo/esquema nesta fase. Comparador não reexecutado porque não houve migration. Permissões seguem `Operacao`; configuração administrativa e envio não foram alterados. A captura SMTP adicional continua sendo a validação da Fase 4.
- Permanecem pendentes o cenário Homem-Aranha completo e o acabamento geral da Fase 6; conta/provedor real de e-mail não verificados.

## Atualização — Fase 6: cenário completo e acabamento (25/11/2026)

- Branch `revisao/fase-6-cenario-acabamento`, criada de `main` em `2da907d`, após o merge autorizado do PR #8. O usuário autorizou executar e encerrar as fases restantes enquanto estiver ausente; esta é a última fase definida no plano vigente.
- Cenário Homem-Aranha completo para Gestor e Administrador no Edge: cadastro de camiseta preta GG e estampa, cliente, produto com dois materiais criado dentro do orçamento, imagem de referência, preço/custo internos, contato, PDF/WhatsApp com confirmação externa simulada, aprovação e reserva parcial, bloqueio de produção com falta, compra/recebimento integral, recálculo explícito da ficha, produção, entrega e segunda aprovação com cancelamento/liberação.
- O teste confere os saldos físicos/reservados, a baixa única, os movimentos e a soma de quantidade/valor entregue no dashboard. Nenhuma mensagem é enviada a clientes reais. O PNG de referência é uma fixture local; a confirmação externa usa somente os dados fictícios da revisão.
- Modais e menu móvel: foco inicial, Tab/Shift+Tab nos limites, Escape e retorno ao acionador; o menu fecha ao mudar para desktop. Quando a ação deixa de existir, o foco retorna ao título da página. A origem é mantida mesmo se uma consulta substituir a linha. O erro na aprovação abre somente seu diálogo, sem manter outro diálogo ativo/oculto para acessibilidade.
- Campos simples associam automaticamente rótulo, controle e mensagem de erro/ajuda. Abas aceitam setas, Home e End. Formulários de cliente/orçamento têm título e retorno explícitos; avisos acomodam a ação de tentar novamente em 360 px.
- Consultas principais, auxiliares, abas, detalhes, materiais, históricos, proposta e modelos foram revisados. Falha no histórico do orçamento agora oferece nova tentativa. Falhas de sessão não redirecionam indevidamente ao login; falha ao sair mantém a sessão e permite repetir. Gravação com erro mantém o formulário.
- Suíte adicional força carregamento/503/vazio/recuperação nas listas e 503/recuperação nas abas e consultas subordinadas; confere gravação e saída com falha. Clientes próprios por execução evitam dependência de histórico/contagens do seed.
- Rotas restantes e abas do cliente capturadas em 360/1280 px, complementando as inspeções das Fases 2–5. Não há rolagem horizontal da página; tabelas usam rolagem interna. Capturas ignoradas em `data/revisao-fase6`, com rolagem no topo e sem toasts cobrindo o conteúdo.
- `docs/08-mvp2.md` registra que o fluxo simples substitui OS por etapas, preservando a referência técnica histórica. Código legado de OS, modelo de persistência, migrations e dependências foram preservados.
- Backend **139 aprovados, 0 falhas e 0 ignorados**, com PostgreSQL real. `npm ci` sem vulnerabilidades, typecheck, Vitest **8/8**, build local e Compose padrão aprovados em sequência. Edge **24/24** na suíte completa.
- Captura SMTP adicional repetida: **3/3** no smtp4dev, com destinatário, mensagem e PDF recebidos e chave repetida sem duplicação. Revisão restaurada sem SMTP direto. Antes dos cenários, o preparo E2E aguarda GET `/api/auth/me` retornar 401 pelo proxy sem cookies; `--wait` sozinho não verifica HTTP da API. Relatórios Playwright foram excluídos do contexto Docker.
- Atualização do Compose preservou contagens/hashes de **46 produtos, 63 orçamentos, 82 itens, 29 pedidos e 25 matérias-primas** existentes antes dos E2E. Sem migration/modelo alterado; o comparador não foi reexecutado. Ambiente principal não foi acessado.

## Limitações da execução ponta a ponta

O cenário Homem-Aranha completo foi executado na Fase 6 para os dois perfis no ambiente PostgreSQL de revisão. Envio por conta/provedor SMTP real permanece não verificado. Na auditoria inicial, a tentativa manual com `curl.exe` não preservou as aspas JSON no PowerShell; o 500 observado não era evidência de falha de credenciais. O login com JSON válido foi confirmado e o teste de navegador passou para os dois perfis após as correções da Fase 0.

## Matriz funcional

Legenda: **Atende**, **Parcial** ou **Não atende**.

| Requisito | Situação | Evidência | Correção proposta | Tamanho | Risco |
|---|---|---|---|---|---|
| R1.1 Administrador acessa clientes | Atende | API de leitura/operação e menu; [ClientesController.cs](../backend/src/Zalek360.Api/Controllers/ClientesController.cs). Matriz de autorização aprovada. | Preservar. | P | Baixo |
| R1.2 Administrador acessa orçamentos | Atende | [OrcamentosController.cs](../backend/src/Zalek360.Api/Controllers/OrcamentosController.cs); operações de edição/apresentação/decisão permitidas a Gestor e Administrador. Teste API confirma responsável autenticado. | Preservar. | P | Baixo |
| R1.3 Administrador acessa pedidos/vendas e histórico | Atende | [PedidosController.cs](../backend/src/Zalek360.Api/Controllers/PedidosController.cs) e endpoints de consulta/histórico; alteração de status e cancelamento com motivo permitidos a Gestor e Administrador. | Preservar. | P | Baixo |
| R1.4 Administrador acessa dashboard, produtos, matérias-primas e estoque | Atende | Consultas e operações de estoque/catálogo acessíveis; dashboard para Gestor e Administrador. Matriz API cobre 200/403. | Preservar. | P | Baixo |
| R1.5 Gestor executa toda a operação diária | Atende nos fluxos existentes | Gestor opera dashboard, clientes, orçamentos (criação, edição, exportação, apresentação, contato e decisão), pedidos/status/materiais, catálogo, ficha técnica/composição, matérias-primas e compras; [EstoqueController.cs](../backend/src/Zalek360.Api/Controllers/EstoqueController.cs). | Preservar. | P | Baixo |
| R1.6 Gestor opera clientes, orçamentos e pedidos | Atende | Consultas retornam 200; mutações comerciais, status/cancelamento com motivo e materiais usam `Operacao`. API testa os contratos de validação/404/422 sem bloqueio de perfil, e E2E cria cliente e cria/edita orçamento. GET/POST usuários retornam 403 ao Gestor. | Preservar; matriz 403 de Configurações implementada na Fase 2. | P | Baixo |
| R1.7 Administrador pode operar conforme R1 | Atende | Política `Operacao` permite Gestor e Administrador; operações restritas por perfil usam `Administracao`. Testes de autorização cobrem ambos. | Preservar e manter auditoria das ações existentes. | P | Baixo |
| R1.8 Atendente e Operador retirados da interface | Atende | Menus e ações são filtrados por perfil; perfis legados continuam no enum e banco para compatibilidade. | Preservar dados e código legado. | P | Baixo |
| R1.9 Menu por perfil | Atende | [AppShell.tsx](../frontend/components/layout/AppShell.tsx) mostra o mesmo menu operacional completo aos dois perfis, sem badge Consulta. | Configurações é exclusivo do Administrador na Fase 2. Tela de Usuários permanece pendente e terá a mesma restrição. | P | Baixo |
| R1.10 Credenciais de demonstração Gestor | Atende em banco novo; legado preservado | Seed atualizado para `gestao@zalekpersonalizados.com.br`; E2E de Gestor aprovado em banco descartável novo. Bases antigas mantêm `gestor@zalekpersonalizados.com.br`, pois não são reseedadas. | Preservar compatibilidade e não renomear contas existentes automaticamente. | P | Baixo |
| R1.11 Credenciais de demonstração Administrador | Atende | Login de Administrador verificado pelo Playwright Edge no ambiente isolado. | Preservar. | P | Baixo |
| R1.12 Login válido e corpo inválido | Atende na Fase 0 | Login E2E Edge 2/2; JSON malformado retorna 400 no envelope padrão e tem teste de integração. A primeira tentativa manual usou JSON quebrado pelo `curl.exe` no PowerShell. | Preservar testes de contrato. | P | Baixo |
| R1.13 Compatibilidade de dados históricos | Atende | Perfis legados continuam no enum e banco; o auth atual os recusa sem apagar IDs/referências. | Preservar compatibilidade; não reclassificar dados sem migração aprovada. | P | Baixo |
| R2.1 Código único da matéria-prima | Não atende | A entidade e o DTO não possuem código de matéria-prima; a migração/documentação confirma código como fase futura. | Criar campo opcional ou migrar sem quebrar registros; exigir unicidade somente após backfill aprovado. | M | Alto |
| R2.2 Nome da matéria-prima | Atende | Cadastro permite nome obrigatório até 120 caracteres em `EstoqueService`. | Preservar. | P | Baixo |
| R2.3 Categoria da matéria-prima | Não atende | Não existe campo categoria no modelo físico atual. | Decidir se é obrigatório ou usar “Geral” temporariamente; não fixar valores no frontend. | M | Médio |
| R2.4 Unidade fixa "un" | Atende | [MateriaPrima.cs](../backend/src/Zalek360.Domain/Entities/MateriaPrima.cs) força `"un"` no criar/editar e a interface fixa `un`. | Preservar e remover qualquer valor legado divergente por migration ou decisão de dados. | P | Baixo |
| R2.5 Quantidades inteiras | Atende | Snapshot/modelo possuem check e migrations com integralidade de saldo, mínimo e reserva. | Preservar validação inteira. | P | Baixo |
| R2.6 Estoque mínimo | Atende | Cadastro/edição aceita mínimo e a API calcula `noMinimo`. | Preservar e usar o mesmo indicador no dashboard. | P | Médio |
| R2.7 Ativo/inativo | Atende | Status ativo existe, com ação de ativação/desativação e regras de uso. | Preservar. | P | Médio |
| R2.8 Entrada por compra/recebimento | Parcial | Compras planejadas/pedidas/recebidas existem. Recebimento parcial e custo não estão completos. | Implementar só o necessário para entrada simples e manter o custo em decisão futura. | M | Médio |
| R2.9 Ajuste com motivo | Parcial | Existe ajuste de saldo, mas o motivo informado detalhado está pendente; o motivo de movimento usa texto legado. | Adicionar motivo obrigatório, enum simples de ajustes e histórico. | M | Médio |
| R2.10 Saldo físico/reservado/disponível | Atende | Estoque mostra físico, reservado, livre, mínimo e a receber em [Estoque.tsx](../frontend/features/estoque/Estoque.tsx). | Preservar e padronizar rótulos em todas as telas. | P | Baixo |
| R2.11 Histórico de movimentos por item | Atende | Endpoint `materias-primas/{id}/movimentos` e interface mostram histórico. | Preservar; melhorar filtros por tipo/período se necessário. | P | Baixo |
| R3.1 Produto com nome, descrição e composição | Atende na Fase 2 | [FormularioProduto.tsx](../frontend/features/produtos/FormularioProduto.tsx) salva dados, valores e composição no mesmo fluxo; operação transacional na API. | Preservar. | M | Alto |
| R3.2 Cadastro de produto na tela de produtos | Atende na Fase 2 | Rota `/produtos`, menu para Gestor/Administrador, pesquisa, cadastro e edição; Estoque aponta para o catálogo. | Preservar. | M | Médio |
| R3.3 Produto criado dentro do orçamento | Atende na Fase 3 | Modal compartilha `FormularioProduto`, com preço/custo/composição e seleção no item. | Preservar dados preenchidos do orçamento. | M | Alto |
| R3.4 Produto criado entra no catálogo | Atende na Fase 3 | Cadastro usa a mesma API de produtos e invalida a consulta de catálogo, sem sair do orçamento. | Preservar. | M | Alto |
| R3.5 Duplicar produto | Atende na Fase 2 | API e interface duplicam descrição, preço/custo e composição; nome novo obrigatório, sem copiar vínculos históricos. | Preservar. | M | Médio |
| R3.6 Preço sugerido no produto | Atende na Fase 3 | Seleção aplica preço; alteração mostra aviso/restauração. Análise interna para Gestor/Administrador usa custo registrado no item, sem estimar legado pelo catálogo atual. | Preservar histórico; custo/margem nunca no PDF ou mensagens. | M | Alto |
| R4.1 Escolher cliente | Atende | Formulário possui busca e seleção de cliente em [FormularioOrcamento.tsx](../frontend/features/orcamentos/FormularioOrcamento.tsx). | Preservar. | P | Baixo |
| R4.2 Escolher produto existente | Atende | Formulário lista produtos existentes e permite quantidade/valores/personalização. | Preservar. | P | Baixo |
| R4.3 Criar novo produto no item | Atende na Fase 3 | Modal mantém cliente, outros itens, desconto, personalização, descrição/valor já preenchidos e anexos. | Preservar. | M | Alto |
| R4.4 Foto/referência por item | Atende na Fase 3 | `ReferenciaAnexo` mostra imagem autenticada no formulário/detalhe; mantém abertura do arquivo e fallback para falha. | Preservar. | M | Médio |
| R4.5 Miniatura no pedido | Atende na Fase 3 | Pedido mostra miniatura das imagens copiadas do orçamento; arquivos de outros formatos mantêm ícone/link autenticado. | Preservar. | P/M | Médio |
| R4.6 Disponibilidade das matérias-primas no orçamento | Atende na Fase 3 | Backend simula necessidade/saldo/falta por item sem duplicar saldo compartilhado; inclui reservas do próprio pedido. Não grava reservas nem bloqueia orçamento. | Preservar caráter informativo. | M | Alto |
| R4.7 Cálculo automático | Atende | O orçamento usa cálculo posterior no backend e mostra totais no resumo. | Preservar cálculo no backend; não duplicar regra no frontend. | P | Médio |
| R4.8 PDF | Atende | Pré-visualização GET e exportação POST preservadas. Fase 4 prepara PDF para apresentação sem selo de rascunho; mensagem/PDF sem custo ou margem. | Preservar. | P | Baixo |
| R4.9 Contato e decisão existentes | Atende na Fase 4 | SMTP registra após aceitação; envio externo exige confirmação. Fluxo de decisão/aprovação e apresentação por outros meios preservados. | Preservar. | P | Médio |
| R5.1 Aprovação gera pedido | Atende | `RegistrarDecisao` aprova em transação e gera pedido. | Preservar. | P | Alto |
| R5.2 Reserva limitada ao disponível | Atende | A reserva usa mínimo físico menos reservado e cria pendência pela diferença. | Preservar concorrência PostgreSQL. | P | Alto |
| R5.3 Pendência por falta | Atende | Pedido misto reserva o conhecido e gera pendência por saldo/ficha. | Preservar e mostrar visualmente no fluxo. | P | Alto |
| R5.4 Status Aberto → Em produção → Pronto → Entregue | Atende | Enum e transições manuais implementam o fluxo. | Preservar como fluxo simples. | P | Alto |
| R5.5 Baixa ao passar para Em produção | Parcial | Existe baixa transitória limitada às reservas próprias, mas a documentação a identifica como temporária. | Validar cenário real; substituir por consumo confirmado simples sem OS por etapas. | M | Alto |
| R5.6 Bloqueio de pendência ao iniciar | Atende | API retorna 409 se houver pendência/falta de controle. | Preservar e deixar mensagem e ação de reposição visíveis no frontend. | P | Alto |
| R5.7 Cancelamento libera reservas | Atende | Status cancelado libera reservas ainda ativas e registra movimento. | Preservar confirmação e motivo. | P | Alto |
| R5.8 Ordem de Serviço por etapas não é usada | Parcial | Não há OS ativa no fluxo atual; a documentação declara 2.2 futura. O código não deve ser removido sem aprovação. | Registrar no docs/08 na implementação aprovada que OS foi substituída; não apagar entidades/endpoints legados nesta fase. | P/M | Alto |
| R6.1 Alerta no dashboard com cartão “Repor estoque” | Atende na Fase 5 | Contagem derivada da lista de reposição da API, com link para a mesma visão em Estoque. | Preservar. | M | Médio |
| R6.2 Filtro “Abaixo do mínimo” no estoque | Atende na Fase 5 | Filtro usa comparação estrita do saldo livre com o mínimo, calculada na API, somente para ativos. | Preservar distinção de igualdade. | P | Médio |
| R6.3 Quantidade sugerida = mínimo - disponível + pendências | Atende na Fase 5 | API separa sugestão bruta e falta líquida descontando compras a receber; exclui reservas liberadas/consumidas. | Preservar; pedidos sem ficha exigem definição dos materiais. | M | Alto |
| R6.4 Ações de reposição visíveis | Atende na Fase 5 | Visão Repor estoque mostra saldo livre, mínimo, sugestão, a receber e falta comprar; ação preenche compra editável. | Preservar. | M | Médio |
| R7.1 Histórico por cliente | Atende | Cliente possui visão geral, orçamentos, pedidos e histórico consolidado em endpoints. | Melhorar apresentação de linha do tempo se houver ruído. | P | Médio |
| R7.2 Filtros de pedidos por período | Atende na Fase 5 | Períodos predefinidos/personalizados, pela data do pedido ou entrega, com datas inclusivas e validação. | Preservar. | P | Baixo |
| R7.3 Filtros por cliente | Atende na Fase 5 | Seletor com busca e teclado envia clienteId exato à API; compõe status, responsável e período. | Preservar. | P | Médio |
| R7.4 Filtros por status | Atende | Abas e enum permitem todos os estados. | Preservar. | P | Baixo |
| R7.5 Vendas do mês entregues em quantidade e valor | Atende na Fase 5 | Conta e soma pedidos Entregue pelo instante da entrega no mês local; link usa o mesmo intervalo. | Preservar cálculo no backend. | M | Alto |
| R7.6 Orçamentos em aberto | Atende na Fase 5 | Título agrupa elaboração e aguardando retorno, com contagens e próximos passos separados. | Preservar atalhos operacionais. | P | Baixo |
| R7.7 Pedidos em andamento | Atende | Conta Aberto, Em produção e Pronto no dashboard. | Preservar; adicionar filtro linkado a cada status se necessário. | P | Baixo |
| R7.8 Alertas de estoque no dashboard | Atende na Fase 5 | Cartão usa a mesma lista/contagem da reposição, sem fórmula no frontend. | Preservar. | M | Médio |
| R8.1 Textos portugueses e consistência | Atende | Perfis vigentes, títulos/retornos nos formulários e textos operacionais revisados; documentação do fluxo simples sincronizada. | Preservar. | P/M | Baixo |
| R8.2 Estados de carregamento | Atende | Indicadores de carregamento nas consultas e bloqueio de ações em andamento; E2E retém respostas para verificar os estados. | Preservar. | M | Baixo |
| R8.3 Estados vazios | Atende | Listas e abas com mensagens e ações; E2E verifica vazio após recuperação, incluindo 360 px. | Preservar. | P | Baixo |
| R8.4 Estados de erro | Atende | Consultas com nova tentativa; histórico do orçamento e auxiliares corrigidos; E2E força 503, recupera e preserva formulário/sessão. API mantém envelope padrão. | Preservar. | P/M | Baixo |
| R8.5 Ações com ícones/AcoesLinha | Atende | Componente `AcoesLinha` está presente e usado em várias listas; algumas ações ainda são botões. | Completar só nas listas relevantes, preservando clique de linha e teclado. | P/M | Baixo |
| R8.6 Responsividade 360 px | Atende | Edge verifica largura da página e captura rotas/abas; avisos, formulários e modais alcançáveis; tabelas com rolagem interna. | Preservar. | M | Baixo |
| R8.7 Responsividade 1280 px | Atende | Capturas e verificação de largura em 1280 px, complementadas pelas Fases 2–5. | Preservar. | P/M | Baixo |
| R8.8 Navegação por teclado/modais | Atende | Edge cobre variantes de diálogo e menu móvel: foco inicial, contenção, Escape e retorno. Abas e seletores usados por teclado. | Preservar. | M | Baixo |

## Evidências técnicas adicionais

- As políticas de autorização estão em [ConfiguracaoServicos.cs](../backend/src/Zalek360.Api/Configuracao/ConfiguracaoServicos.cs): `Operacao` permite todo o fluxo diário para Gestor e Administrador; `Administracao` restringe usuários ao Administrador. Configurações e exclusão definitiva de produto usam `Administracao` na Fase 2, com matriz de permissões testada.
- [AppShell.tsx](../frontend/components/layout/AppShell.tsx) oferece o mesmo menu operacional aos dois perfis. Atendente e Operador permanecem nos enums e dados históricos, mas não recebem acesso aos módulos.
- O responsável do orçamento é obtido por `IUsuarioAtual`; a API ignora o `ResponsavelId` recebido e preserva o responsável persistido ao editar.
- A coluna `condicao_pagamento` em orçamentos e pedidos agora permite nulo; a migration nova não transforma os valores existentes.
- O seed de banco novo contém `gestao@zalekpersonalizados.com.br`; bancos já preenchidos mantêm suas contas anteriores.
- Quantidades inteiras já estão presentes em migration/modelo; o cadastro fixa unidade `un`.
- O catálogo da Fase 2 permite dados/valores/composição, duplicação, inativação/reativação e exclusão por Administrador de produto nunca usado; preço automático e análise interna foram acrescentados na Fase 3.
- Não existe chamada de produto novo no formulário de orçamento.
- A disponibilidade das matérias-primas não é apresentada no cálculo do orçamento.
- O dashboard usa orçamentos aprovados no mês e não pedidos entregues em quantidade/valor.
- Não há “Repor estoque” no dashboard nem filtro “Abaixo do mínimo” no estoque.
- Os testes Vitest anteriores de item foram estabilizados na Fase 0 e a suíte atual passou 5/5; isso não substitui a revisão visual em tamanhos móveis.

## Bugs encontrados

### Resolvido na Fase 0

1. **JSON inválido causava 500 por colisão de chave**
   - Endpoint: `POST /api/auth/login` com corpo JSON truncado.
   - Raiz: chaves do ModelState normalizadas para `"corpo"` eram convertidas diretamente em dicionário e colidiam.
   - Correção: agrupar chaves após normalização e acumular mensagens distintas.
   - Teste: JSON malformado retorna 400 e envelope `{status, code, message, errors}`.

2. **Login válido confirmado**
   - JSON montado com `ConvertTo-Json` autenticou Administrador com HTTP 200.
   - Playwright Edge confirmou login dos dois perfis com banco novo.

### Alto — resolvidos na Fase 1

2. **Administrador não podia operar conforme R1**
   - As políticas agora permitem operações comerciais/de catálogo/estoque conforme R1; mutações comerciais e status de pedido são permitidos a Gestor e Administrador.
   - Validado pela matriz de autorização da API.

3. **Gestor estava limitado à consulta em módulos comerciais**
   - R1 definitiva permite criar/editar clientes e orçamentos, exportar/apresentar/registrar contatos e decisões, alterar status e materiais de pedido.
   - Também opera dashboard, catálogo, composição, matérias-primas e compras.

4. **Menu ocultava o dashboard e rotulava módulos como consulta para o Gestor**
   - Corrigido e verificado por Playwright Edge.

5. **Responsável selecionável e condição de pagamento obrigatória**
   - O responsável agora é definido pela API e mantido na edição; condição vazia é aceita e exibida como “A combinar”.

### Médio

7. **Duplicar produto — resolvido na Fase 2**
8. **Preço sugerido — resolvido na Fase 3, com aviso e restauração**
9. **Disponibilidade da ficha no orçamento — resolvido na Fase 3, como informação sem bloqueio**
10. **Vendas entregues e reposição no dashboard — resolvido na Fase 5**
11. **Filtro dedicado abaixo do mínimo — resolvido na Fase 5**
12. **Revisão de responsividade e teclado incompleta**
   - A cobertura E2E atual não verifica todas as rotas a 360/1280 px nem todos os modais.

## Fase 0 — resultado da execução

- Commit separado de documentação: `docs: registra a revisão funcional`.
- Backend em PostgreSQL descartável: 99 aprovados, 0 falhas, 0 ignorados.
- Vitest: 4/4 em três execuções sequenciais; timeout preservado.
- Playwright em Edge: 2/2 testes de login contra `http://localhost:3100`.
- `npm run typecheck`: aprovado.
- `docker compose -p zalek360-revisao up --build -d`: imagens e serviços PostgreSQL/API/Next.js construídos e iniciados.
- O volume foi recriado somente no projeto descartável `zalek360-revisao`; nenhum banco principal foi acessado.
- Seed de banco novo usa `gestao@zalekpersonalizados.com.br`; bancos já populados mantêm o usuário `gestor@zalekpersonalizados.com.br`.
- O Vitest foi limitado a `features/**/*.test.{ts,tsx}` para não tentar executar especificações Playwright sem contexto de navegador.
- Naquele momento, `npm audit` reportava 1 vulnerabilidade moderada em `next` 16.4.3; ela foi tratada separadamente no PR #2 com a atualização para 16.4.5.
- Pendências ao encerrar a Fase 0: cenário Homem-Aranha, responsividade visual, permissões finais R1, SMTP e fluxos operacionais de estoque ponta a ponta. R1 foi tratado nesta Fase 1; as demais pendências permanecem.

## Riscos de regressão

- Login: qualquer correção deve preservar cookie httpOnly, duração e perfil no `/auth/me`.
- Autorização: mudar políticas pode permitir operações indevidas se endpoints forem tratados no controller errado.
- Histórico: contas antigas, IDs e registros responsáveis não podem ser removidos ou reclassificados.
- Estoque: reservas, saldo físico e movimentos devem permanecer transacionais e nunca negativos.
- Pedido: cancelar após baixa ou em estados incorretos não pode estornar de forma duplicada.
- Orçamento: criação modal de produto não pode trocar ou limpar item, anexos, desconto ou personalização.
- Migração: código/categoria/preço sugerido não devem ser adicionados sem backfill e migration aprovada.
- Dados de demonstração: seed de banco já populado não deve sobrescrever usuários existentes.

## Decisões necessárias

1. **Código único de matéria-prima**
   - Definir se será gerado automaticamente ou informado manualmente; será necessário planejar o backfill dos itens existentes.

2. **Categoria de matéria-prima**
   - Definir se é obrigatória. As opções serão gerenciadas pelo Administrador conforme a regra geral.

3. **Preço de venda e custo do produto**
   - A inclusão do preço de venda e do custo, e seu uso para preencher o orçamento, já estão aprovados nas Fases 2 e 3 do plano.

4. **Fluxo simples de pedidos**
   - Decisão já tomada: o fluxo manual Aberto → Em produção → Pronto → Entregue substitui a OS por etapas. Não remover código legado de OS sem perguntar.

7. **Miniaturas de referência**
   - Miniaturas reais aprovadas e implementadas na Fase 3; PDF continua como download autenticado, sem link público.

## Plano de fases recomendado

Plano histórico da revisão inicial. A ordem vigente e as aprovações de cada fase estão em `docs/10-contexto-e-proximos-passos.md`.

### Fase 1 — Segurança e identidade (P/M)

- Corrigir login, duplicação no ModelState e credenciais demo.
- Revisar políticas por perfil.
- Ajustar menu por perfil.
- Garantir que Gestor opera pedidos, status/cancelamento com motivo e materiais; usuários e configurações ficam exclusivos do Administrador.
- Confirmar operações excepcionais do Administrador.
- Testes: login válido/inválido, permissões API, menu, 360 px/1280 px.

### Fase 2 — Catálogo e produto no orçamento (M)

- Disponibilizar cadastro claro de produto.
- Criar modal de novo produto no orçamento.
- Compartilhar TanStack Query e atualizar catálogo sem perder estado do orçamento.
- Implementar duplicação.
- Preço de venda/custo já aprovados para as Fases 2/3; seguir o plano vigente em `docs/10-contexto-e-proximos-passos.md`.
- Testes: nome único, cadastro direto, composição, estado do item, anexos e remoção.

### Fase 3 — Materiais e composição (M)

- Completar código/categoria de matéria-prima somente após decisão.
- Ajustar motivo obrigatório.
- Mostrar disponibilidade da composição no orçamento.
- Preservar unidade `un` e quantidades inteiras.
- Testes: cadastro, edição, movimentação, reserva e receita por unidade.

### Fase 4 — Fluxo pedido/estoque (M)

- Refinar bloqueio de pendência e mensagem de reposição.
- Garantir baixa simples ao entrar em produção.
- Cancelamento e liberação já existentes devem ser revalidados.
- Não introduzir OS por etapas.
- Testes: cenário Homem-Aranha completo em PostgreSQL.

### Fase 5 — Reposição e dashboard (M)

- Lista “Repor estoque”.
- Filtro abaixo do mínimo.
- Fórmula sugestão = mínimo - disponível + pendências dos pedidos.
- Dashboard com vendas entregues do mês e alertas.
- Testes: cálculos com pedidos, reservas e pendências.

### Fase 6 — Histórico, qualidade visual e acessibilidade (P/M)

- Histórico por cliente.
- Filtros de período/cliente/status em pedidos.
- Corrigir Vitest com timeout.
- Testar modal/foco/teclado.
- Validar 360 px e 1280 px com Playwright.

## Primeira entrega segura recomendada

Começar pela **Fase 1 — Segurança e identidade**. Ela corrige o bloqueio real de login, elimina o conflito atual de políticas e torna o resto da revisão testável com o perfil correto. As mudanças são pequenas e não exigem migration obrigatoriamente, desde que as contas e IDs legados sejam preservados.
