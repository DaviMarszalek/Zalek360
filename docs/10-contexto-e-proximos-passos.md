# Contexto e próximos passos do Zalek360

**Atualizado em:** 24/11/2026
**Base consultada:** `main`/`origin/main` em `9fc55ff`, após o merge autorizado do PR #3 em 24/11/2026. Fase 2 na branch `revisao/fase-2-catalogo`.

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

- **Fluxo simples de pedido:** Aberto → Em produção → Pronto → Entregue, com transições manuais e baixa das matérias-primas reservadas ao passar para Em produção. A Ordem de Serviço por etapas foi descartada pelo usuário (R5). Não remover código legado de OS sem perguntar. `docs/08-mvp2.md` ainda descreve OS como fase futura; atualizar essa documentação na Fase 6.
- **Reservas:** na aprovação, reservar até o saldo disponível; a diferença vira pendência. Cancelamento libera apenas reservas ativas. Aprovado pelo usuário nos requisitos R5/Fase 2.0.
- **Estoque:** controlar matérias-primas; unidade fixa `un` e quantidades inteiras. Definido nos requisitos R2.
- **Responsável do orçamento:** sempre o usuário autenticado na criação; na edição, preservar o responsável original. Aprovado na Fase 1.
- **Condição de pagamento:** opcional; vazia é exibida como “A combinar”. Aprovado na Fase 1.
- **Opções gerenciáveis:** toda opção de domínio vinda do banco deve ser gerenciável pelo Administrador, incluindo produtos, tipos de personalização, condições de pagamento, motivos de decisão e categorias. Regra geral aprovada pelo usuário.
- **Produto:** possui preço de venda e custo/preço pago opcionais no catálogo (Fase 2). Na Fase 3, o preço sugerirá o valor do item do orçamento, editável com aviso de alteração e opção de restaurar. Custo e margem aparecem apenas na tela para Gestor e Administrador, nunca no PDF ou em mensagens ao cliente. Aprovado nas Fases 2 e 3.
- **Remoção de produto:** somente Administrador pode excluir definitivamente se nunca foi usado em orçamento/pedido; se já foi usado, inativar para preservar histórico e impedir novos usos. Aprovado para a Fase 2.
- **Envio da proposta:** requisitos da Fase 4 aprovados pelo usuário. E-mail com mensagem revisável, PDF e SMTP configurado por variáveis `Email__*`; sem SMTP, abrir `mailto`, baixar o PDF e registrar apresentação somente após confirmação. WhatsApp com mensagem para copiar e link `wa.me`; anexar o PDF manualmente, sem link público. Modelos editáveis pelo Administrador.
- **Estoque de produtos acabados/pronta-entrega:** fora do escopo atual; tratar somente se o usuário solicitar.

## D) Estado atual confirmado

### Git e PRs

- `main`/`origin/main`: `9fc55ff`, merge do PR #3 aprovado pelo usuário em 24/11/2026.
- PRs #1 (Fase 0), #2 (Next.js), #3 (Fase 1/R1 definitiva) e #4 (regras/contexto): **MESCLADOS**.
- A Fase 1 teve 105 testes backend aprovados/0 ignorados, Vitest 5/5, Edge 4/4 e typecheck/build/Compose aprovados.
- Fase 2 — [PR #5 aberto](https://github.com/DaviMarszalek/Zalek360/pull/5), branch `revisao/fase-2-catalogo`, criada do `main` atualizado. Plano aprovado pelo usuário; a entrega aguarda revisão e OK explícito para merge em PR próprio.
- Implementado nesta fase: tela Produtos com pesquisa, cadastro/edição, composição, preço/custo opcionais, duplicação, inativação/reativação e exclusão definitiva por Administrador apenas quando nunca usado; Configurações para categorias, tipos de personalização, condições de pagamento e motivos de decisão.
- Migration `20261124220121_CatalogoProdutosEOpcoes` gerada via EF sobre o snapshot existente. Preço/custo dos produtos antigos permanecem nulos; categorias legadas são copiadas para opções sem alterar produtos/históricos. A semeadura das opções ocorre na migration, inclusive em bancos já existentes.
- Comparador executado no ambiente de revisão: **0 divergências**, 19 tabelas, 221 colunas, 67 constraints, 67 índices e 2 sequências. Os bancos temporários foram removidos sem apagar volumes.
- **Validação final da Fase 2:** backend **117 aprovados, 0 falhas, 0 ignorados** com PostgreSQL real; Vitest **5/5**; Edge **8/8**; `npm ci`, typecheck, build local e Compose aprovados, em sequência.
- Produtos/Configurações e formulários verificados em 360 px/1280 px, sem rolagem horizontal, com Escape nos modais. Dados/capturas locais ficam ignorados em `data/revisao-fase2`.
- Antes dos E2E, o upgrade preservou contagens e hashes dos 8 produtos, 16 orçamentos e 7 pedidos existentes na revisão. Testes adicionais incluem consulta atrasada durante exclusão, 403 em configurações e apresentação correta de valores nulos.
- Fase 3 e merge da Fase 2 ainda exigem autorização explícita.
- Referências: [PR #3](https://github.com/DaviMarszalek/Zalek360/pull/3), [PR #4](https://github.com/DaviMarszalek/Zalek360/pull/4) e matriz em `docs/09-revisao-funcional.md`.

### Ambiente

- No momento da consulta, Docker Desktop estava ativo. O projeto isolado `zalek360-revisao` estava saudável, com PostgreSQL, API e frontend nas portas 55432, 5100 e 3100.
- Na consulta anterior, o projeto principal `zalek360` estava ativo nas portas 5432, 5000 e 3000. Não foi acessado nem alterado nesta revalidação. Nunca interromper nem limpar esse projeto.
- `.env.revisao` está ignorado nesta branch, sem conteúdo exibido ou versionado. O `.gitignore` atualizado e `.env.revisao.example` já estão em `main` após o PR #3. Nunca adicionar `.env.revisao` ao Git.

## E) Próximas fases, na ordem aprovada

A **Fase 2 — Catálogo de produtos** está nesta branch: produtos com composição/valores no mesmo formulário, ciclo de vida do catálogo e opções gerenciáveis persistidas. A edição não altera os valores dos itens já registrados; preço automático e margem no orçamento ficam na Fase 3.

1. **Fase 3 — Orçamento:** preço automático do produto com aviso e restauração; margem interna para Gestor e Administrador; modal “Novo produto” sem perder dados do orçamento; miniatura de referência no orçamento e pedido; disponibilidade/falta de matérias-primas por item, informativa e sem bloquear.
2. **Fase 4 — Envio da proposta:** e-mail SMTP com PDF, mensagem editável antes do envio e teste com smtp4dev/MailHog; fallback `mailto` quando SMTP não estiver configurado; WhatsApp com mensagem pronta e `wa.me`, sem link público do PDF.
3. **Fase 5 — Estoque mínimo e vendas:** filtro abaixo do mínimo, cartão e lista “Repor estoque” com sugestão `mínimo − disponível + pendências`; dashboard de pedidos entregues no mês e filtros por período, cliente e status.
4. **Fase 6 — Cenário completo e acabamento:** E2E do cenário Homem-Aranha completo; documentar em `docs/08-mvp2.md` que o fluxo simples substitui OS por etapas; concluir pendências R8.

Criar branch nova da fase seguinte a partir do `main` atualizado somente depois que o usuário aprovar o merge do PR da fase atual. Apresentar plano curto e aguardar “pode seguir” antes de codar.

## F) Pendências e não verificados

### Validação ainda não executada

- Cenário Homem-Aranha completo de ponta a ponta.
- Inspeção visual das demais rotas em 360 px e 1280 px. Produtos, Configurações e seus formulários já foram verificados na Fase 2.
- Navegação por teclado em todos os modais: foco inicial, Escape, contenção e retorno do foco.
- Revisão de estados de carregamento/erro/vazio em todas as consultas e ações.
- Fase 4: envio real de e-mail via servidor SMTP de teste, incluindo destinatário, assunto e PDF anexado.

### Funcionalidades ainda pendentes na matriz R1–R8

- Matérias-primas: código único e categoria; recebimento parcial; motivo de ajuste obrigatório e detalhado.
- Catálogo/orçamento: cadastro a partir do orçamento, aplicação automática do preço com aviso/restauração e margem interna continuam na Fase 3. A tela Produtos, duplicação, composição/valores e remoção com preservação do histórico foram implementadas na Fase 2.
- Orçamento/pedido: miniaturas verificadas no navegador e disponibilidade de matérias-primas por item.
- Estoque/pedidos: validar em fluxo real a baixa ao iniciar produção e atualizar documentação da decisão de não usar OS por etapas.
- Reposição: cartão/lista “Repor estoque”, filtro abaixo do mínimo e quantidade sugerida com pendências.
- Dashboard/vendas: valor e quantidade de pedidos entregues no mês, rótulo consolidado de orçamentos em aberto e filtros de pedidos por período/cliente/status.
- Acabamento R8: consistência de textos, estados de carregamento e erro, responsividade e teclado.

### Notas de sincronização da documentação

- A matriz R1 e a nota R3.6 em `docs/09-revisao-funcional.md` foram atualizadas nesta branch: preço de venda/custo e margem interna para Gestor e Administrador já estão aprovados para as Fases 2/3.
- Configurações de opções foram implementadas nesta Fase 2, com GET/POST/PUT exclusivos do Administrador e testes 403 para os outros perfis. A tela de Usuários continua pendente; GET/POST `/api/usuarios` permanecem exclusivos do Administrador. Modelos de mensagem, dados da empresa e e-mail pela interface ficam nas fases correspondentes, sem alteração de segredos nesta entrega.
- `docs/08-mvp2.md` ainda descreve OS como fase futura. A decisão de substituir OS por fluxo simples foi aprovada; registrar a decisão na Fase 6. Não remover código legado de OS sem perguntar.
- Para implementar SMTP na Fase 4, o usuário precisa providenciar uma conta de e-mail habilitada para SMTP e senha de aplicativo, além dos parâmetros do provedor. Nunca solicitar que esses segredos sejam commitados; configurar somente no ambiente local/seguro.

## G) Como retomar na próxima sessão

1. Ler `AGENTS.md` e este arquivo.
2. Conferir `git status` e a branch antes de trocar de contexto. Retomar `revisao/fase-2-catalogo` enquanto seu PR estiver aberto; atualizar `origin` e integrar `origin/main` por merge, preservando alterações locais.
3. Abrir Docker Desktop e confirmar “Engine running”. Não tocar no projeto principal.
4. Conferir o estado do PR #5 (Fase 2) no GitHub. Não mesclar nenhum PR sem OK do usuário.
5. Usar o ambiente de revisão com `docker compose -p zalek360-revisao --env-file .env.revisao ...`; nunca exibir ou adicionar o `.env.revisao`.
6. Aguardar aprovação para mesclar a Fase 2 e para iniciar a Fase 3, em branch nova do `main` atualizado e com plano curto aprovado.
