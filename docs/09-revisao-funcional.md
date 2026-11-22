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

## Limitações da execução ponta a ponta

O cenário Homem-Aranha completo permanece pendente para a fase prevista. Na auditoria inicial, a tentativa manual com `curl.exe` não preservou as aspas JSON no PowerShell; o 500 observado não era evidência de falha de credenciais. O login com JSON válido foi confirmado e o teste de navegador passou para os dois perfis após as correções da Fase 0.

## Matriz funcional

Legenda: **Atende**, **Parcial** ou **Não atende**.

| Requisito | Situação | Evidência | Correção proposta | Tamanho | Risco |
|---|---|---|---|---|---|
| R1.1 Administrador acessa clientes | Parcial | Backend: clientes em `Politicas.Consulta`; [ClientesController.cs](../backend/src/Zalek360.Api/Controllers/ClientesController.cs) permite leitura aos dois perfis, mas operações são restritas a Gestor. | Alterar operações administrativas permitidas por R1 e manter consulta compartilhada. Validar o menu do Administrador. | P | Médio |
| R1.2 Administrador acessa orçamentos | Parcial | [OrcamentosController.cs](../backend/src/Zalek360.Api/Controllers/OrcamentosController.cs) usa `Operacao`, cuja política aceita apenas Gestor. | Autorizar o Administrador nas operações excepcionais e manter rastreamento de usuário. | P/M | Médio |
| R1.3 Administrador acessa pedidos/vendas e histórico | Parcial | [PedidosController.cs](../backend/src/Zalek360.Api/Controllers/PedidosController.cs) usa consulta compartilhada, mas mudanças de status usam `Operacao`, exclusiva do Gestor. | Permitir operações administrativas excepcionais ao Administrador, quando R1 autorizar, com confirmação e log. | P/M | Médio |
| R1.4 Administrador acessa dashboard, produtos, matérias-primas e estoque | Parcial | Dashboard e consultas são restritas por autenticação/políticas compartilhadas; operações de estoque estão em `Operacao`, exclusiva do Gestor. | Confirmar que o Administrador pode listar tudo e definir quais operações excepcionais poderá executar. | P | Médio |
| R1.5 Gestor executa cadastros e estoque | Atende | A política `Operacao` permite Gestor em clientes, orçamentos, pedidos, produtos e estoque. | Preservar. | P | Baixo |
| R1.6 Gestor pode consultar pedidos, mas não operar | Não atende | [PedidosController.cs](../backend/src/Zalek360.Api/Controllers/PedidosController.cs) concede ao Gestor tanto consulta quanto status. | Separar consulta administrativa/operacional: Gestor consulta pedidos e não altera status; Administrador pode operação excepcional. Isto depende da leitura final da R1 e da decisão de fluxo simples. | P/M | Alto |
| R1.7 Administrador pode operar exceções rastreadas | Não atende | Nenhum fluxo administrativo excepcional de negócio está implementado além da criação de Gestor. | Implementar apenas ações excepcionais necessárias, confirmar mudança antes de executar e registrar ator/motivo. | M | Alto |
| R1.8 Atendente e Operador retirados da interface | Parcial | O tipo ainda contém esses perfis em [api.ts](../frontend/types/api.ts) e no enum/backend. A interface não mostra opções de menu específicas desses perfis, mas o menu não é filtrado por perfil. | Manter compatibilidade no código/banco; remover menu/actions por perfil desnecessários e não criar novos usuários desses perfis. | P | Médio |
| R1.9 Menu por perfil | Não atende | [AppShell.tsx](../frontend/components/layout/AppShell.tsx) monta o mesmo menu para todos os usuários autenticados. | Gestor: Estoque/Produtos e consulta de pedidos; sem clientes/orçamentos/admin. Administrador: todos os módulos e administração. | M | Médio |
| R1.10 Credenciais de demonstração Gestor | Atende em banco novo; legado preservado | Seed atualizado para `gestao@zalekpersonalizados.com.br`; E2E de Gestor aprovado em banco descartável novo. Bases antigas mantêm `gestor@zalekpersonalizados.com.br`, pois não são reseedadas. | Preservar compatibilidade e não renomear contas existentes automaticamente. | P | Baixo |
| R1.11 Credenciais de demonstração Administrador | Parcial | O seed usa `admin@zalekpersonalizados.com.br`; a senha configurada é `Zalek@2026`. | Confirmar criação idempotente e indicar claramente no ambiente. | P | Médio |
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
| R3.1 Produto com nome, descrição e composição | Parcial | Produto contém nome/categoria/descrição; composição é configurada por ficha técnica em [Estoque.tsx](../frontend/features/estoque/Estoque.tsx). | Integrar composição ao fluxo de produtos sem duplicar conceitos. | M | Alto |
| R3.2 Cadastro de produto na tela de produtos | Parcial | Existe modal "Cadastrar produto" dentro da tela de estoque/fichas e API `/api/produtos`. | Criar uma tela/rota clara de produtos e manter o cadastro atual como reutilizável. | M | Médio |
| R3.3 Produto criado dentro do orçamento | Não atende | O formulário de orçamento lista produtos existentes, mas não possui modal "Novo produto". | Criar modal no próprio formulário, criar via API, atualizar lista e selecionar sem limpar a descrição/anexos já preenchidos. | M | Alto |
| R3.4 Produto criado entra no catálogo | Não atende | Não há criação a partir do orçamento. O cadastro existente em estoque cria catálogo, mas não retorna ao fluxo do orçamento. | Compartilhar o mesmo serviço/API e atualizar TanStack Query após criação. | M | Alto |
| R3.5 Duplicar produto | Não atende | Não foi encontrada ação ou endpoint de duplicação. | Adicionar “Duplicar produto”, copiando descrição e composição, mas não anexos históricos. Exigir novo nome único. | M | Médio |
| R3.6 Preço sugerido no produto | Não atende | Produto não possui preço; orçamento usa valor unitário informado manualmente. | **Decisão necessária:** confirmar se o preço sugerido será opcional e reaplicado em novos itens, mas editável no orçamento. | M | Alto |
| R4.1 Escolher cliente | Atende | Formulário possui busca e seleção de cliente em [FormularioOrcamento.tsx](../frontend/features/orcamentos/FormularioOrcamento.tsx). | Preservar. | P | Baixo |
| R4.2 Escolher produto existente | Atende | Formulário lista produtos existentes e permite quantidade/valores/personalização. | Preservar. | P | Baixo |
| R4.3 Criar novo produto no item | Não atende | Não existe modal ou ação no formulário. | Criar modal compartilhado; manter cliente, itens, desconto e anexos. | M | Alto |
| R4.4 Foto/referência por item | Parcial | O orçamento permite anexar arquivos por item e o pedido renderiza os anexos copiados. | Verificar miniatura real no navegador e padronizar abertura/thumbnail, principalmente em celular. | M | Médio |
| R4.5 Miniatura no pedido | Parcial | [DetalhePedido.tsx](../frontend/features/pedidos/DetalhePedido.tsx) mostra anexos como links com ícone; não há garantia visual de miniatura. | Exibir miniatura apenas para imagem e manter link seguro para PDF/outros arquivos. | P/M | Médio |
| R4.6 Disponibilidade das matérias-primas no orçamento | Não atende | O cálculo do orçamento não consulta posição disponível; não há indicação disponível/falta X. | Consultar posição pelo backend no preview/cálculo e usar erro/aviso informativo sem bloquear. | M | Alto |
| R4.7 Cálculo automático | Atende | O orçamento usa cálculo posterior no backend e mostra totais no resumo. | Preservar cálculo no backend; não duplicar regra no frontend. | P | Médio |
| R4.8 PDF | Atende | Endpoints de pré-visualização/exportação e testes de PDF existem. | Preservar e validar anexos/miniaturas no documento. | P | Médio |
| R4.9 Contato e decisão existentes | Atende | Fluxo mantém elaboração, apresentação, aguardando retorno, decisão e aprovação. | Preservar; padronizar comunicação em português. | P | Médio |
| R5.1 Aprovação gera pedido | Atende | `RegistrarDecisao` aprova em transação e gera pedido. | Preservar. | P | Alto |
| R5.2 Reserva limitada ao disponível | Atende | A reserva usa mínimo físico menos reservado e cria pendência pela diferença. | Preservar concorrência PostgreSQL. | P | Alto |
| R5.3 Pendência por falta | Atende | Pedido misto reserva o conhecido e gera pendência por saldo/ficha. | Preservar e mostrar visualmente no fluxo. | P | Alto |
| R5.4 Status Aberto → Em produção → Pronto → Entregue | Atende | Enum e transições manuais implementam o fluxo. | Preservar como fluxo simples. | P | Alto |
| R5.5 Baixa ao passar para Em produção | Parcial | Existe baixa transitória limitada às reservas próprias, mas a documentação a identifica como temporária. | Validar cenário real; substituir por consumo confirmado simples sem OS por etapas. | M | Alto |
| R5.6 Bloqueio de pendência ao iniciar | Atende | API retorna 409 se houver pendência/falta de controle. | Preservar e deixar mensagem e ação de reposição visíveis no frontend. | P | Alto |
| R5.7 Cancelamento libera reservas | Atende | Status cancelado libera reservas ainda ativas e registra movimento. | Preservar confirmação e motivo. | P | Alto |
| R5.8 Ordem de Serviço por etapas não é usada | Parcial | Não há OS ativa no fluxo atual; a documentação declara 2.2 futura. O código não deve ser removido sem aprovação. | Registrar no docs/08 na implementação aprovada que OS foi substituída; não apagar entidades/endpoints legados nesta fase. | P/M | Alto |
| R6.1 Alerta no dashboard com cartão “Repor estoque” | Não atende | Dashboard não consulta estoque mínimo nem renderiza cartão de reposição. | Adicionar indicador/card derivado da API sem adicionar valor fixo no frontend. | M | Médio |
| R6.2 Filtro “Abaixo do mínimo” no estoque | Não atende | Estoque possui indicador, busca e mostrar inativos, mas não filtro abaixo do mínimo. | Adicionar toggle/filtro “Abaixo do mínimo” sobre os dados já calculados pela API. | P | Médio |
| R6.3 Quantidade sugerida = mínimo - disponível + pendências | Parcial | Existe `faltaComprar`, mas a fórmula precisa da composição da pendência e não está refletida em uma lista específica de reposição. | Expor lista com quantidade sugerida calculada pelo backend e incluir pendências do pedido. | M | Alto |
| R6.4 Ações de reposição visíveis | Parcial | A tela permite planejar compra por matéria-prima. | Criar uma visão única “Repor estoque” mostrando ordem sugerida e falta. | M | Médio |
| R7.1 Histórico por cliente | Atende | Cliente possui visão geral, orçamentos, pedidos e histórico consolidado em endpoints. | Melhorar apresentação de linha do tempo se houver ruído. | P | Médio |
| R7.2 Filtros de pedidos por período | Atende | Lista possui período, status, responsável e busca. | Adicionar filtro explícito de cliente ao lado dos demais, se não vier pela busca. | P | Baixo |
| R7.3 Filtros por cliente | Parcial | Busca aceita nome/CPF/CNPJ e endpoint aceita `clienteId`, mas a UI não oferece seletor de cliente claro. | Adicionar seletor de cliente na lista de pedidos. | P | Médio |
| R7.4 Filtros por status | Atende | Abas e enum permitem todos os estados. | Preservar. | P | Baixo |
| R7.5 Vendas do mês entregues em quantidade e valor | Não atende | Dashboard só conta pedidos concluídos e usa a lista de orçamentos aprovados no mês como `valorAprovadoNoMes`. | Trocar por pedidos `Entregue` no período e somar valor entregue do pedido no backend. | M | Alto |
| R7.6 Orçamentos em aberto | Parcial | Dashboard conta elaboração e aguardando retorno separadamente. | Padronizar como “Orçamentos em aberto” ou manter os dois indicadores se forem usados como próximos passos. | P | Baixo |
| R7.7 Pedidos em andamento | Atende | Conta Aberto, Em produção e Pronto no dashboard. | Preservar; adicionar filtro linkado a cada status se necessário. | P | Baixo |
| R7.8 Alertas de estoque no dashboard | Não atende | Não há alerta de estoque mínimo no dashboard. | Reusar lista/contagem calculada pelo backend. | M | Médio |
| R8.1 Textos portugueses e consistência | Parcial | Maioria em português, mas ainda há “colaboradores”, perfis legados e termos técnicos como “Aguardando retorno”. | Padronizar Gestor/Administrador e revisar termos operacionais. | P/M | Médio |
| R8.2 Estados de carregamento | Parcial | Muitas consultas têm loading/erro/empty. Login não exibiu o corpo correto em 500 no teste HTTP. | Padronizar envelopes e estados; corrigir login primeiro. | M | Alto |
| R8.3 Estados vazios | Atende | Telas principais possuem estados vazios ou mensagens. | Preservar e revisar 360 px. | P | Baixo |
| R8.4 Estados de erro | Parcial | Há tratamento em muitos componentes, mas a API não serializa corretamente o erro de binding do login. | Corrigir normalização de erros e teste. | P/M | Alto |
| R8.5 Ações com ícones/AcoesLinha | Atende | Componente `AcoesLinha` está presente e usado em várias listas; algumas ações ainda são botões. | Completar só nas listas relevantes, preservando clique de linha e teclado. | P/M | Baixo |
| R8.6 Responsividade 360 px | Não verificável | Não foi possível executar navegador/Playwright. A tabela de estoque usa `min-width` dentro de overflow horizontal; pode haver contenção em mobile. | Validar visualmente Playwright 360 px em fase posterior. | M | Alto |
| R8.7 Responsividade 1280 px | Não verificável | Layout tem breakpoints lg/xl e max-width 1280; sem execução visual. | Validar 1280 px em Playwright. | P/M | Médio |
| R8.8 Navegação por teclado/modais | Parcial | Há modais Radix e alguns handlers de teclado em tabelas. O comportamento completo não foi exercitado no navegador. | Testar foco inicial, Escape, foco preso e retorno do foco em Playwright/Testing Library. | M | Médio |

## Evidências técnicas adicionais

- A autorização centralizada está em [ConfiguracaoServicos.cs](../backend/src/Zalek360.Api/Configuracao/ConfiguracaoServicos.cs): consulta aceita Gestor e Administrador; operação aceita apenas Gestor.
- O problema do commit de controle de acesso permanece no backend: o Administrador não pode operar orçamentos, pedidos, clientes, estoque ou produtos, mesmo que R1 o qualifique para consulta completa e operações excepcionais. [DemaisControllers.cs](../backend/src/Zalek360.Api/Controllers/DemaisControllers.cs), [OrcamentosController.cs](../backend/src/Zalek360.Api/Controllers/OrcamentosController.cs), [PedidosController.cs](../backend/src/Zalek360.Api/Controllers/PedidosController.cs) e [EstoqueController.cs](../backend/src/Zalek360.Api/Controllers/EstoqueController.cs) confirmam o comportamento.
- O menu não usa perfil em [AppShell.tsx](../frontend/components/layout/AppShell.tsx).
- O Gestor é o único perfil listado como responsável ativo em [CatalogoQueries.cs](../backend/src/Zalek360.Infrastructure/Queries/CatalogoQueries.cs). Isso impede operações de orçamentos pelo Administrador se também forem usadas como responsáveis.
- O seed contém três Atendentes e não possui a conta `gestao@zalekpersonalizados.com.br` em [DbSeeder.cs](../backend/src/Zalek360.Infrastructure/Persistence/Seed/DbSeeder.cs).
- Quantidades inteiras já estão presentes em migration/modelo; o cadastro fixa unidade `un`.
- O catálogo permite criar produto em estoque, mas não duplicar e não mostra preço sugerido.
- Não existe chamada de produto novo no formulário de orçamento.
- A disponibilidade das matérias-primas não é apresentada no cálculo do orçamento.
- O dashboard usa orçamentos aprovados no mês e não pedidos entregues em quantidade/valor.
- Não há “Repor estoque” no dashboard nem filtro “Abaixo do mínimo” no estoque.
- Existem testes Vitest para o item do orçamento, mas duas falharam por timeout. Os testes não provam correção visual ou ausência de falha real.

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

### Alto

2. **Administrador não pode operar conforme R1**
   - API centraliza operações em `Politicas.Operacao`, que aceita somente Gestor.
   - Impacto: R1 não é atendido em orçamentos, pedidos, produtos, estoque e operações excepcionais.

3. **Gestor não está limitado a pedidos em consulta**
   - O Gestor recebe `Operacao` em clientes, orçamentos e atualização de status de pedido.
   - Impacto: contraria a divisão funcional solicitada.

4. **Menu não muda por perfil**
   - Mesmo menu para Gestor e Administrador.
   - Impacto: atalhos administrativos/clientes/orçamentos podem ficar visíveis ao Gestor.

5. **Produto novo não pode ser criado no orçamento**
   - Fluxo exige sair da tela e perder contexto.

### Médio

7. **Duplicar produto não existe**
8. **Preço sugerido não existe**
9. **Disponibilidade da ficha não aparece no orçamento**
10. **Dashboard não possui vendas entregues nem reposição**
11. **Estoque não possui filtro dedicado abaixo do mínimo**
12. **Timeouts Vitest sob carga concorrente**
   - Dois testes atingiram 5 segundos enquanto build e testes rodavam em paralelo.
   - Sem carga concorrente, a suíte passou três vezes consecutivas 4/4; timeout não foi aumentado.

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
- `npm audit` reportou 1 vulnerabilidade moderada preexistente em `next` (16.4.3), com correção disponível. A atualização não foi incluída na Fase 0 por estar fora do escopo de bloqueantes e precisa ser tratada em fase/dependência própria.
- Ainda não verificados: cenário Homem-Aranha, responsividade visual, permissões finais R1, SMTP e fluxos operacionais de estoque ponta a ponta.

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

1. **Preço sugerido no produto**
   - Proposta: campo opcional no produto, preenchido no novo item do orçamento e sempre editável.
   - Precisa de aprovação antes da implementação.

2. **Limites exatos do Gestor em pedidos**
   - R1 diz que pedidos são apenas consulta. Confirmar se o Gestor pode consultar materiais/pendências e histórico, mas não alterar status.

3. **Operações excepcionais do Administrador**
   - Definir quais operações poderão ser feitas pelo Administrador além de consultar tudo.
   - Administrar contas é já permitido; o restante não deve vir de permissões ampliadas sem confirmação.

4. **Categoria de matéria-prima**
   - Campo obrigatório ou opcional?
   - Não existe hoje no modelo.

5. **Código de matéria-prima**
   - Gerar automaticamente ou exigir cadastro manual único?
   - Impacta backfill de dados existentes.

6. **Ordem de Serviço**
   - Confirmar a substituição definitiva por fluxo simples.
   - Não remover código legado/OS enquanto não houver aprovação explícita.

7. **Miniaturas de referência**
   - Confirmar se imagens devem ter miniatura real no orçamento e pedido; PDF deve continuar como link seguro.

## Plano de fases recomendado

### Fase 1 — Segurança e identidade (P/M)

- Corrigir login, duplicação no ModelState e credenciais demo.
- Revisar políticas por perfil.
- Ajustar menu por perfil.
- Garantir que Gestor não altera pedidos.
- Confirmar operações excepcionais do Administrador.
- Testes: login válido/inválido, permissões API, menu, 360 px/1280 px.

### Fase 2 — Catálogo e produto no orçamento (M)

- Disponibilizar cadastro claro de produto.
- Criar modal de novo produto no orçamento.
- Compartilhar TanStack Query e atualizar catálogo sem perder estado do orçamento.
- Implementar duplicação.
- Não adicionar preço sugerido sem a decisão aprovada.
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
