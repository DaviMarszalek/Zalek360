# Contexto e próximos passos do Zalek360

**Atualizado em:** 25/11/2026
**Base de partida da Fase 6:** `main`/`origin/main` em `2da907d`, após o merge autorizado do PR #8 em 25/11/2026. Entrega na branch `revisao/fase-6-cenario-acabamento`. O usuário autorizou executar e encerrar as fases restantes após validação, enquanto estiver ausente; conferir o estado atual no GitHub ao retomar.

## A) Como o negócio funciona

- A Zalek Personalizados faz produtos personalizados; uma pessoa usa o sistema.
- A empresa compra matérias-primas, por exemplo, “Camiseta preta GG” e “Estampa Homem-Aranha”. Um produto é composto por matérias-primas: neste exemplo, 1 camiseta preta GG + 1 estampa Homem-Aranha. Essa composição é a ficha técnica.
- Fluxo: orçamento com foto de referência, usando produto existente ou criado no orçamento → aprovação → pedido/venda → baixa de matérias-primas do estoque.
- O estoque é de matérias-primas, com estoque mínimo e aviso de reposição.
- O cliente não acessa o sistema. O contato é feito externamente por WhatsApp ou e-mail.

## B) Perfis (R1 definitiva — redefinida em 24/11/2026)

- **Gestor — operação diária:** dashboard; clientes (cadastrar e editar); orçamentos (criar, editar, exportar, apresentar, registrar contato e decisão); pedidos (status, cancelar com motivo e materiais); produtos (cadastrar, editar, composição, duplicar e inativar); matérias-primas, compras, entradas e ajustes de estoque com motivo. Vê custo e margem.
- **Administrador:** tudo o que o Gestor faz, mais os exclusivos: configurações (opções gerenciáveis, modelos de mensagem, dados da empresa e e-mail), usuários (criar, desativar e redefinir senha) e exclusão definitiva de produto nunca usado.
- **Atendente e Operador:** fora da interface e da autenticação, mantidos no código/dados por compatibilidade.
- **Uso recomendado:** entrar como Gestor no dia a dia e como Administrador somente para configurar.
- **Custo e margem:** visíveis somente na tela para Gestor e Administrador, nunca no PDF ou em mensagens ao cliente. Uma opção para esconder do Gestor só será considerada se o usuário pedir, quando houver funcionário.
- **API e interface:** `Operacao` permite Gestor e Administrador; `Administracao` permite só Administrador. Menu operacional igual para ambos; Configurações é exclusivo do Administrador nesta Fase 2. A tela de Usuários permanece pendente e será exclusiva do Administrador.
- Esta definição substitui a restrição antiga do Gestor a consultas comerciais. Define as permissões das funcionalidades futuras, sem antecipar sua implementação.

## C) Decisões tomadas e origem

- **Fluxo simples de pedido:** Aberto → Em produção → Pronto → Entregue, com transições manuais e baixa das matérias-primas reservadas ao passar para Em produção. A Ordem de Serviço por etapas foi descartada pelo usuário (R5). Não remover código legado de OS sem perguntar. `docs/08-mvp2.md` foi atualizado na Fase 6 para registrar esta substituição; nenhum código legado foi removido.
- **Reservas:** na aprovação, reservar até o saldo disponível; a diferença vira pendência. Cancelamento libera apenas reservas ativas. Aprovado pelo usuário nos requisitos R5/Fase 2.0.
- **Estoque:** controlar matérias-primas; unidade fixa `un` e quantidades inteiras. Definido nos requisitos R2.
- **Responsável do orçamento:** sempre o usuário autenticado na criação; na edição, preservar o responsável original. Aprovado na Fase 1.
- **Condição de pagamento:** opcional; vazia é exibida como “A combinar”. Aprovado na Fase 1.
- **Opções gerenciáveis:** toda opção de domínio vinda do banco deve ser gerenciável pelo Administrador, incluindo produtos, tipos de personalização, condições de pagamento, motivos de decisão e categorias. Regra geral aprovada pelo usuário.
- **Produto:** possui preço de venda e custo/preço pago opcionais no catálogo (Fase 2). Na Fase 3, o preço sugere o valor do item do orçamento, editável com aviso de alteração e opção de restaurar. O custo conhecido é registrado ao incluir/trocar o produto no item e preservado na edição do mesmo produto; custos antigos ausentes continuam não informados. A margem estimada usa total com personalização após desconto menos custo dos produtos, sem despesas adicionais, e percentual sobre a venda. Custo e margem aparecem apenas na tela para Gestor e Administrador, nunca no PDF ou em mensagens ao cliente. Aprovado nas Fases 2 e 3.
- **Remoção de produto:** somente Administrador pode excluir definitivamente se nunca foi usado em orçamento/pedido; se já foi usado, inativar para preservar histórico e impedir novos usos. Aprovado para a Fase 2.
- **Envio da proposta:** requisitos da Fase 4 aprovados pelo usuário. E-mail com mensagem revisável, PDF e SMTP configurado por variáveis `Email__*`; sem SMTP, abrir `mailto`, baixar o PDF e registrar apresentação somente após confirmação. WhatsApp com mensagem para copiar e link `wa.me`; anexar o PDF manualmente, sem link público. Modelos editáveis pelo Administrador.
- **Estoque de produtos acabados/pronta-entrega:** fora do escopo atual; tratar somente se o usuário solicitar.

## D) Estado atual confirmado

### Git e PRs

- Base da Fase 6: `2da907d`, merge do PR #8 aprovado pelo usuário em 25/11/2026. A entrega da Fase 6 está descrita ao final desta lista.
- PRs #1 (Fase 0), #2 (Next.js), #3 (Fase 1/R1 definitiva), #4 (regras/contexto), #5 (Fase 2/catálogo), #6 (Fase 3/orçamento), #7 (Fase 4/envio) e #8 (Fase 5/estoque e vendas): **MESCLADOS**.
- A Fase 1 teve 105 testes backend aprovados/0 ignorados, Vitest 5/5, Edge 4/4 e typecheck/build/Compose aprovados.
- Fase 2 — [PR #5 mesclado](https://github.com/DaviMarszalek/Zalek360/pull/5), com autorização explícita. Branch da entrega: `revisao/fase-2-catalogo`.
- Implementado na Fase 2: tela Produtos com pesquisa, cadastro/edição, composição, preço/custo opcionais, duplicação, inativação/reativação e exclusão definitiva por Administrador apenas quando nunca usado; Configurações para categorias, tipos de personalização, condições de pagamento e motivos de decisão.
- Migration `20261124220121_CatalogoProdutosEOpcoes` gerada via EF sobre o snapshot existente. Preço/custo dos produtos antigos permanecem nulos; categorias legadas são copiadas para opções sem alterar produtos/históricos. A semeadura das opções ocorre na migration, inclusive em bancos já existentes.
- Comparador da Fase 2 executado no ambiente de revisão: **0 divergências**, 19 tabelas, 221 colunas, 67 constraints, 67 índices e 2 sequências. Os bancos temporários foram removidos sem apagar volumes.
- **Validação final da Fase 2:** backend **117 aprovados, 0 falhas, 0 ignorados** com PostgreSQL real; Vitest **5/5**; Edge **8/8**; `npm ci`, typecheck, build local e Compose aprovados, em sequência.
- Produtos/Configurações e formulários verificados em 360 px/1280 px, sem rolagem horizontal, com Escape nos modais. Dados/capturas locais ficam ignorados em `data/revisao-fase2`.
- Antes dos E2E, o upgrade preservou contagens e hashes dos 8 produtos, 16 orçamentos e 7 pedidos existentes na revisão. Testes adicionais incluem consulta atrasada durante exclusão, 403 em configurações e apresentação correta de valores nulos.
- Fase 3 — [PR #6 mesclado](https://github.com/DaviMarszalek/Zalek360/pull/6), autorizada e implementada na branch `revisao/fase-3-orcamento`: preço automático com aviso/restauração; análise interna de custo/margem para Gestor e Administrador; cadastro de produto em modal mantendo os dados do orçamento; miniaturas de referência no orçamento/pedido; necessidade/saldo/falta de materiais por item, informativos.
- Migration via EF `20261124231443_CustoInternoNoItemOrcamento`: somente custo nullable no item, sem backfill. Histórico de nome/preço/custo preservado; catálogo não altera itens existentes.
- Validação Fase 3: backend **124 aprovados, 0 falhas, 0 ignorados**, PostgreSQL real; Vitest **8/8**; Edge **10/10**; `npm ci`, typecheck, build local e Compose aprovados, em sequência. Comparador: **0 divergências**, 19 tabelas, 222 colunas, 67 constraints, 67 índices, 2 sequências.
- Upgrade da Fase 3 preservou contagens/hashes de **14 produtos, 19 orçamentos, 28 itens e 7 pedidos**, antes dos dados novos dos E2E. Ambiente principal não foi acessado.
- E2E da Fase 3 executa os dois perfis até o pedido, verifica margem/materiais e carregamento real das miniaturas. Formulário/modal, detalhe e pedido verificados em 360 px/1280 px, sem rolagem horizontal da página; capturas ignoradas em `data/revisao-fase3`.
- Fase 4 — [PR #7 mesclado](https://github.com/DaviMarszalek/Zalek360/pull/7), autorizada e implementada na branch `revisao/fase-4-envio-proposta`: revisão do destinatário/mensagem, SMTP com PDF, fallback `mailto`, WhatsApp sem link público e confirmação externa explícita. Apresentação por outros meios preservada.
- Modelos persistidos editáveis só pelo Administrador; dados públicos permitidos por lista explícita. Configuração SMTP por `Email__*`, sem expor credenciais na interface. Tentativa persistida antes do SMTP, sem reenvio automático quando a confirmação é incerta; bloqueio do orçamento evita envios concorrentes na mesma versão.
- Migration EF `20261125001901_ModelosEEnviosProposta`: duas tabelas novas e sementes de modelos, sem alterar dados comerciais existentes. Comparador: **0 divergências**, 21 tabelas, 235 colunas, 71 constraints, 72 índices, 2 sequências.
- Validação Fase 4: backend **133 aprovados, 0 falhas, 0 ignorados**, PostgreSQL real; Vitest **8/8**; Edge **13/13** na suíte completa sem SMTP e **3/3** na captura SMTP adicional. `npm ci`, typecheck, build local e Compose padrão aprovados, em sequência.
- Capturador smtp4dev confirmou destinatário, assunto, texto revisado e PDF anexado, com uma mensagem por chave. Testes cobrem upgrade, concorrência, erro SMTP, campos públicos e MIME Unicode. Conta/provedor real não foi testado.
- Upgrade preservou hashes/contagens de **24 produtos, 24 orçamentos, 37 itens e 11 pedidos** existentes, antes dos E2E. Modal validado em 360/1280 px com rolagem interna até a confirmação, sem rolagem horizontal da página; capturas ignoradas em `data/revisao-fase4`.
- Ambiente de revisão restaurado sem SMTP direto. Capturador local em `http://localhost:5180`, sem relay externo; o override `docker-compose.email-teste.yml` é exclusivo da revisão. Ambiente principal e segredos preservados.
- Fase 5 — [PR #8 mesclado](https://github.com/DaviMarszalek/Zalek360/pull/8), autorizada e implementada na branch `revisao/fase-5-estoque-vendas`: cartão/lista de reposição e filtro abaixo do mínimo; vendas entregues no mês em quantidade/valor; filtros de pedidos por cliente, status e período com referência de data do pedido/entrega.
- Sugestão bruta = máximo de zero e mínimo − saldo livre + pendências; falta líquida desconta compras previstas. Ambas ficam visíveis; a compra é sugerida e revisável, sem criação automática. Materiais inativos são excluídos das duas visões de reposição.
- Validação Fase 5: backend **139 aprovados, 0 falhas, 0 ignorados**, PostgreSQL real; Vitest **8/8**; Edge **15/15**. `npm ci`, typecheck, build local e Compose padrão aprovados. Sem migration ou alteração de esquema; comparador não foi reexecutado.
- Atualização preservou hashes/contagens de **30 produtos, 39 orçamentos, 54 itens, 13 pedidos e 11 matérias-primas**, antes dos E2E.
- Dashboard, reposição e pedidos verificados em 360/1280 px, sem rolagem horizontal da página; tabelas usam rolagem interna. E2E dos dois perfis cobre compra prevista, cancelamento, recebimento, baixa única ao iniciar produção e vendas/filtros. Capturas ignoradas em `data/revisao-fase5`.
- Fase 5 mesclada com OK explícito; o usuário depois autorizou executar a Fase 6 e mesclar seu PR após a validação, sem aguardar uma nova resposta.
- Fase 6 — cenário Homem-Aranha completo para Gestor e Administrador: materiais/composição, cliente, orçamento com imagem, proposta/PDF, contato, aprovação com reserva parcial, bloqueio por falta, compra/recebimento, recálculo da ficha, baixa única, entrega/vendas e cancelamento/liberação.
- Acabamento R8: títulos e retorno dos formulários, rótulos associados aos controles, foco inicial/contenção/Escape/retorno dos modais, abas por teclado e menu móvel acessível, incluindo fechamento ao mudar para desktop. Falha de aprovação não abre dois diálogos; falhas de consultas, histórico, sessão e saída têm retorno e nova tentativa, sem descartar o formulário.
- Validação Fase 6: backend **139 aprovados, 0 falhas, 0 ignorados**, com PostgreSQL real; Vitest **8/8**; Edge **24/24**. `npm ci`, typecheck, build local e Compose padrão aprovados em sequência. Sem migration/modelo/dependência alterados; comparador não reexecutado.
- Captura SMTP repetida na Fase 6: **3/3**, incluindo PDF real e ausência de reenvio com chave repetida. Ambiente restaurado sem SMTP direto. Os E2E aguardam uma resposta 401 em GET `/api/auth/me` sem cookies pelo proxy antes das ações; `--wait` do Compose não garante HTTP pronto para a API. Relatórios Playwright ficam fora do contexto de build.
- Atualização preservou hashes/contagens de **46 produtos, 63 orçamentos, 82 itens, 29 pedidos e 25 matérias-primas**, antes dos dados novos dos E2E. Capturas complementares de rotas/abas em 360/1280 px, sem rolagem horizontal da página; artefatos ignorados em `data/revisao-fase6`. Ambiente principal preservado.
- `docs/08-mvp2.md` atualizado: fluxo simples substitui OS por etapas. Código legado de OS preservado. Esta é a última fase definida; os itens sem fase ficam na seção F.
- Referências: [PR #3](https://github.com/DaviMarszalek/Zalek360/pull/3), [PR #4](https://github.com/DaviMarszalek/Zalek360/pull/4) e matriz em `docs/09-revisao-funcional.md`.

### Ambiente

- No momento da consulta, Docker Desktop estava ativo. O projeto isolado `zalek360-revisao` estava saudável, com PostgreSQL, API e frontend nas portas 55432, 5100 e 3100.
- Na consulta anterior, o projeto principal `zalek360` estava ativo nas portas 5432, 5000 e 3000. Não foi acessado nem alterado nesta revalidação. Nunca interromper nem limpar esse projeto.
- `.env.revisao` está ignorado nesta branch, sem conteúdo exibido ou versionado. O `.gitignore` atualizado e `.env.revisao.example` já estão em `main` após o PR #3. Nunca adicionar `.env.revisao` ao Git.

## E) Fases da sequência aprovada

As Fases 2–5 estão mescladas. A Fase 6 foi implementada e validada, concluindo a sequência definida. Em 25/11/2026, o usuário autorizou iniciar e executar todas as fases seguintes, incluindo os merges após validação. Essa autorização não define requisitos ausentes nem habilita ações no ambiente principal.

1. **Fase 3 — Orçamento:** preço automático do produto com aviso e restauração; margem interna para Gestor e Administrador; modal “Novo produto” sem perder dados do orçamento; miniatura de referência no orçamento e pedido; disponibilidade/falta de matérias-primas por item, informativa e sem bloquear. Mesclada no PR #6.
2. **Fase 4 — Envio da proposta:** e-mail SMTP com PDF, mensagem editável antes do envio e captura por smtp4dev; fallback `mailto` sem SMTP; WhatsApp com mensagem pronta e `wa.me`, sem link público do PDF. Mesclada no PR #7.
3. **Fase 5 — Estoque mínimo e vendas:** filtro abaixo do mínimo, cartão/lista Repor estoque, sugestão bruta e falta líquida, dashboard de entregues e filtros por período/cliente/status. Mesclada no PR #8.
4. **Fase 6 — Cenário completo e acabamento:** cenário Homem-Aranha completo e R8 implementados/validados; `docs/08-mvp2.md` atualizado com o fluxo simples. Última entrega do plano.

A Fase 6 usa branch própria criada do `main` atualizado. Plano apresentado e execução autorizada. Não há Fase 7 definida; pendências sem fase ou com decisão de negócio ausente não devem ser implementadas por inferência.

## F) Pendências e não verificados

### Não verificado e dependência externa

- Envio por conta/provedor real da empresa. Captura SMTP local validada; falta a configuração segura de conta habilitada e parâmetros do provedor. Não solicitar segredos em commits, mensagens ou documentação.

### Funcionalidades pendentes, sem fase de implementação definida

- Matérias-primas: código único e categoria, recebimento parcial/custos e motivo de ajuste obrigatório/detalhado. Código depende de decidir geração automática/manual e planejar os registros existentes; categoria depende de decidir obrigatoriedade.
- Complementação automática de reservas por prioridade. Hoje o recebimento exige reaplicar explicitamente a ficha do pedido; o cenário completo verifica esse fluxo.
- Tela de Usuários, desativação e redefinição de senha. GET/POST `/api/usuarios` existentes continuam exclusivos do Administrador.
- Configuração dos dados da empresa/e-mail permanece no servidor; a interface administrativa completa desses dados não integra as fases entregues. Segredos SMTP não devem ser expostos pela UI.
- Produtos acabados/pronta-entrega, Financeiro e OS por etapas continuam fora do escopo vigente. Não remover código legado de OS sem perguntar.
- Cenário completo, reposição, vendas e acabamento R8 estão concluídos. Não há Fase 7 definida; novas funcionalidades exigem plano e decisões próprios.

### Notas de sincronização da documentação

- A matriz R1 e a nota R3.6 em `docs/09-revisao-funcional.md` foram atualizadas nesta branch: preço de venda/custo e margem interna para Gestor e Administrador já estão aprovados para as Fases 2/3.
- Configurações de opções foram implementadas na Fase 2, com GET/POST/PUT exclusivos do Administrador e testes 403 para os outros perfis. A tela de Usuários continua pendente; GET/POST `/api/usuarios` permanecem exclusivos do Administrador. Modelos de mensagem e status de envio foram acrescentados na Fase 4; dados da empresa continuam por configuração do servidor. Segredos SMTP ficam somente no ambiente local/seguro.
- `docs/08-mvp2.md` registra o fluxo simples como vigente e o descarte do antigo plano de OS por etapas. A referência histórica de estabilização foi preservada. Não remover código legado de OS sem perguntar.
- Para usar SMTP com destinatários reais após a Fase 4, o usuário precisa providenciar uma conta de e-mail habilitada para SMTP e senha de aplicativo, além dos parâmetros do provedor. Nunca solicitar que esses segredos sejam commitados; configurar somente no ambiente local/seguro.

## G) Como retomar na próxima sessão

1. Ler `AGENTS.md` e este arquivo; conferir estado real de Git/PRs antes de agir.
2. A Fase 6 conclui a sequência vigente. Consultar o PR da branch `revisao/fase-6-cenario-acabamento` e atualizar `main` local por fast-forward após confirmar o merge, sem repetir alterações já entregues.
3. Usar somente `zalek360-revisao` e `.env.revisao`, nunca exibir nem versionar seu conteúdo. Não tocar no projeto principal.
4. As pendências da seção F não têm fase definida; discutir decisões/plano antes de implementá-las. Não inventar requisitos nem configurar SMTP externo sem os dados seguros do responsável.
