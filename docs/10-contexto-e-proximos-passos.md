# Contexto e próximos passos do Zalek360

**Atualizado em:** 24/11/2026
**Base consultada:** `origin/main` em `b59eb19` (PR #4 mesclado), integrado à branch `revisao/fase-1-perfis` pelo merge `09b1061`; PRs #3 e #4 consultados em 24/11/2026.

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
- **API e interface:** `Operacao` permite Gestor e Administrador; `Administracao` permite só Administrador. Menu operacional igual para ambos; Configurações e Usuários serão exclusivos do Administrador quando suas telas forem implementadas.
- Esta definição substitui a restrição antiga do Gestor a consultas comerciais. Define as permissões das funcionalidades futuras, sem antecipar sua implementação.

## C) Decisões tomadas e origem

- **Fluxo simples de pedido:** Aberto → Em produção → Pronto → Entregue, com transições manuais e baixa das matérias-primas reservadas ao passar para Em produção. A Ordem de Serviço por etapas foi descartada pelo usuário (R5). Não remover código legado de OS sem perguntar. `docs/08-mvp2.md` ainda descreve OS como fase futura; atualizar essa documentação na Fase 6.
- **Reservas:** na aprovação, reservar até o saldo disponível; a diferença vira pendência. Cancelamento libera apenas reservas ativas. Aprovado pelo usuário nos requisitos R5/Fase 2.0.
- **Estoque:** controlar matérias-primas; unidade fixa `un` e quantidades inteiras. Definido nos requisitos R2.
- **Responsável do orçamento:** sempre o usuário autenticado na criação; na edição, preservar o responsável original. Aprovado na Fase 1.
- **Condição de pagamento:** opcional; vazia é exibida como “A combinar”. Aprovado na Fase 1.
- **Opções gerenciáveis:** toda opção de domínio vinda do banco deve ser gerenciável pelo Administrador, incluindo produtos, tipos de personalização, condições de pagamento, motivos de decisão e categorias. Regra geral aprovada pelo usuário.
- **Produto:** terá preço de venda (sugere o valor do item do orçamento, editável, com aviso de alteração e opção de restaurar) e custo/preço pago. Custo e margem aparecem apenas na tela para Gestor e Administrador, nunca no PDF ou em mensagens ao cliente. Aprovado nas Fases 2 e 3.
- **Remoção de produto:** somente Administrador pode excluir definitivamente se nunca foi usado em orçamento/pedido; se já foi usado, inativar para preservar histórico e impedir novos usos. Aprovado para a Fase 2.
- **Envio da proposta:** requisitos da Fase 4 aprovados pelo usuário. E-mail com mensagem revisável, PDF e SMTP configurado por variáveis `Email__*`; sem SMTP, abrir `mailto`, baixar o PDF e registrar apresentação somente após confirmação. WhatsApp com mensagem para copiar e link `wa.me`; anexar o PDF manualmente, sem link público. Modelos editáveis pelo Administrador.
- **Estoque de produtos acabados/pronta-entrega:** fora do escopo atual; tratar somente se o usuário solicitar.

## D) Estado atual confirmado

### Git e PRs

- `origin/main`: `b59eb19`, merge do PR #4. Após `git fetch origin`, `git merge origin/main` confirmou a branch atualizada.
- PR #1 — Fase 0, mesclado. Incluiu JSON inválido com 400 no envelope padrão, Playwright e seed `gestao@` para bancos novos.
- PR #2 — atualização de segurança do Next.js, mesclado. Next atualizado para 16.4.5.
- PR #3 — Fase 1, **ABERTO**, branch `revisao/fase-1-perfis`, base `main`; aguarda OK explícito do usuário para mesclar. Mantém as correções de responsável automático, condição opcional “A combinar”, erros por aba e alinhamento, com permissões atualizadas à R1 definitiva.
- PR #4 — regras permanentes e contexto, **MESCLADO** em 24/11/2026, commit `b59eb19`. Integrado ao PR #3 pelo merge `09b1061`, sem conflitos e sem rebase.
- Revalidação da R1 definitiva em 24/11/2026: backend **105 aprovados, 0 falhas, 0 ignorados**, com PostgreSQL real; Vitest **5/5**; Playwright Edge **4/4**; `npm ci` (0 vulnerabilidades), typecheck, build e Compose aprovados. Execuções sequenciais no projeto `zalek360-revisao`.
- Responsável automático, condição opcional “A combinar”, erros por aba e alinhamentos do PR #3 preservados. Migration/modelo sem alterações nesta revisão; comparador não reexecutado (resultado anterior: 0 divergências).
- Os commits desta revisão atualizam API/testes, interface/E2E e documentação, sem iniciar fases futuras. O PR #3 permanece aberto para revisão desses resultados e aguarda o OK explícito do usuário.
- Nenhum merge do PR #3 nem início da Fase 2 está autorizado.
- Referências: [PR #3](https://github.com/DaviMarszalek/Zalek360/pull/3), [PR #4](https://github.com/DaviMarszalek/Zalek360/pull/4) e matriz em `docs/09-revisao-funcional.md`.

### Ambiente

- No momento da consulta, Docker Desktop estava ativo. O projeto isolado `zalek360-revisao` estava saudável, com PostgreSQL, API e frontend nas portas 55432, 5100 e 3100.
- Na consulta anterior, o projeto principal `zalek360` estava ativo nas portas 5432, 5000 e 3000. Não foi acessado nem alterado nesta revalidação. Nunca interromper nem limpar esse projeto.
- `.env.revisao` está ignorado nesta branch, sem conteúdo exibido ou versionado. O `.gitignore` atualizado e `.env.revisao.example` pertencem ao PR #3. Nunca adicionar `.env.revisao` ao Git.

## E) Próximas fases, na ordem aprovada

1. **Fase 2 — Catálogo de produtos:** tela Produtos com pesquisa; adicionar, editar, duplicar, excluir/inativar; preço de venda, custo e composição na mesma tela. Criar Configurações para as opções gerenciáveis. Migrar `TiposPersonalizacao`, `CondicoesPagamento` e `MotivosSugeridos` do código para o banco e semear os valores atuais.
2. **Fase 3 — Orçamento:** preço automático do produto com aviso e restauração; margem interna para Gestor e Administrador; modal “Novo produto” sem perder dados do orçamento; miniatura de referência no orçamento e pedido; disponibilidade/falta de matérias-primas por item, informativa e sem bloquear.
3. **Fase 4 — Envio da proposta:** e-mail SMTP com PDF, mensagem editável antes do envio e teste com smtp4dev/MailHog; fallback `mailto` quando SMTP não estiver configurado; WhatsApp com mensagem pronta e `wa.me`, sem link público do PDF.
4. **Fase 5 — Estoque mínimo e vendas:** filtro abaixo do mínimo, cartão e lista “Repor estoque” com sugestão `mínimo − disponível + pendências`; dashboard de pedidos entregues no mês e filtros por período, cliente e status.
5. **Fase 6 — Cenário completo e acabamento:** E2E do cenário Homem-Aranha completo; documentar em `docs/08-mvp2.md` que o fluxo simples substitui OS por etapas; concluir pendências R8.

Criar branch nova da fase seguinte a partir do `main` atualizado somente depois que o usuário aprovar o merge do PR da fase atual. Apresentar plano curto e aguardar “pode seguir” antes de codar.

## F) Pendências e não verificados

### Validação ainda não executada

- Cenário Homem-Aranha completo de ponta a ponta.
- Inspeção visual em 360 px e 1280 px.
- Navegação por teclado em todos os modais: foco inicial, Escape, contenção e retorno do foco.
- Revisão de estados de carregamento/erro/vazio em todas as consultas e ações.
- Fase 4: envio real de e-mail via servidor SMTP de teste, incluindo destinatário, assunto e PDF anexado.

### Funcionalidades ainda pendentes na matriz R1–R8

- Matérias-primas: código único e categoria; recebimento parcial; motivo de ajuste obrigatório e detalhado.
- Catálogo: tela dedicada de produtos, composição integrada, cadastro a partir do orçamento, duplicação, preço/custo/margem e remoção com preservação do histórico.
- Orçamento/pedido: miniaturas verificadas no navegador e disponibilidade de matérias-primas por item.
- Estoque/pedidos: validar em fluxo real a baixa ao iniciar produção e atualizar documentação da decisão de não usar OS por etapas.
- Reposição: cartão/lista “Repor estoque”, filtro abaixo do mínimo e quantidade sugerida com pendências.
- Dashboard/vendas: valor e quantidade de pedidos entregues no mês, rótulo consolidado de orçamentos em aberto e filtros de pedidos por período/cliente/status.
- Acabamento R8: consistência de textos, estados de carregamento e erro, responsividade e teclado.

### Notas de sincronização da documentação

- A matriz R1 e a nota R3.6 em `docs/09-revisao-funcional.md` foram atualizadas nesta branch: preço de venda/custo e margem interna para Gestor e Administrador já estão aprovados para as Fases 2/3.
- Configurações e a tela de Usuários ainda não existem. O usuário confirmou em 24/11/2026 mantê-las nas fases previstas; o teste 403 de Configurações fica pendente até existir endpoint. A API GET/POST `/api/usuarios` já é exclusiva do Administrador.
- `docs/08-mvp2.md` ainda descreve OS como fase futura. A decisão de substituir OS por fluxo simples foi aprovada; registrar a decisão na Fase 6. Não remover código legado de OS sem perguntar.
- Para implementar SMTP na Fase 4, o usuário precisa providenciar uma conta de e-mail habilitada para SMTP e senha de aplicativo, além dos parâmetros do provedor. Nunca solicitar que esses segredos sejam commitados; configurar somente no ambiente local/seguro.

## G) Como retomar na próxima sessão

1. Ler `AGENTS.md` e este arquivo.
2. Conferir `git status` e a branch antes de trocar de contexto. Enquanto o PR #3 estiver aberto, retomar `revisao/fase-1-perfis`; atualizar `origin` e integrar `origin/main` por merge, preservando alterações locais.
3. Abrir Docker Desktop e confirmar “Engine running”. Não tocar no projeto principal.
4. Conferir o estado dos PRs #3 e desta documentação no GitHub. Não mesclar nenhum PR sem OK do usuário.
5. Usar o ambiente de revisão com `docker compose -p zalek360-revisao --env-file .env.revisao ...`; nunca exibir ou adicionar o `.env.revisao`.
6. Após o merge aprovado do PR #3, iniciar a Fase 2 somente em branch nova a partir do `main` atualizado, com plano curto e aguardando “pode seguir”.
