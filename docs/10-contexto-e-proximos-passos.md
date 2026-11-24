# Contexto e próximos passos do Zalek360

**Atualizado em:** 24/11/2026
**Base consultada:** `main` e `origin/main` em `4f1f095`; PRs consultados no GitHub na mesma data.

## A) Como o negócio funciona

- A Zalek Personalizados faz produtos personalizados; uma pessoa usa o sistema.
- A empresa compra matérias-primas, por exemplo, “Camiseta preta GG” e “Estampa Homem-Aranha”. Um produto é composto por matérias-primas: neste exemplo, 1 camiseta preta GG + 1 estampa Homem-Aranha. Essa composição é a ficha técnica.
- Fluxo: orçamento com foto de referência, usando produto existente ou criado no orçamento → aprovação → pedido/venda → baixa de matérias-primas do estoque.
- O estoque é de matérias-primas, com estoque mínimo e aviso de reposição.
- O cliente não acessa o sistema. O contato é feito externamente por WhatsApp ou e-mail.

## B) Perfis (R1)

- **Administrador:** acesso total, incluindo gestão das opções configuráveis.
- **Gestor:** operação de produtos, composição, matérias-primas e estoque; clientes, orçamentos e pedidos somente para consulta.
- **Atendente e Operador:** fora da interface e mantidos no código/dados por compatibilidade.

## C) Decisões tomadas e origem

- **Fluxo simples de pedido:** Aberto → Em produção → Pronto → Entregue, com transições manuais e baixa das matérias-primas reservadas ao passar para Em produção. A Ordem de Serviço por etapas foi descartada pelo usuário (R5). Não remover código legado de OS sem perguntar. `docs/08-mvp2.md` ainda descreve OS como fase futura; atualizar essa documentação na Fase 6.
- **Reservas:** na aprovação, reservar até o saldo disponível; a diferença vira pendência. Cancelamento libera apenas reservas ativas. Aprovado pelo usuário nos requisitos R5/Fase 2.0.
- **Estoque:** controlar matérias-primas; unidade fixa `un` e quantidades inteiras. Definido nos requisitos R2.
- **Responsável do orçamento:** sempre o usuário autenticado na criação; na edição, preservar o responsável original. Aprovado na Fase 1.
- **Condição de pagamento:** opcional; vazia é exibida como “A combinar”. Aprovado na Fase 1.
- **Opções gerenciáveis:** toda opção de domínio vinda do banco deve ser gerenciável pelo Administrador, incluindo produtos, tipos de personalização, condições de pagamento, motivos de decisão e categorias. Regra geral aprovada pelo usuário.
- **Produto:** terá preço de venda (sugere o valor do item do orçamento, editável, com aviso de alteração e opção de restaurar) e custo/preço pago. Custo e margem aparecem apenas na tela para Administrador, nunca no PDF enviado ao cliente. Aprovado nas Fases 2 e 3.
- **Remoção de produto:** excluir se nunca foi usado em orçamento/pedido; se já foi usado, inativar para preservar histórico e impedir novos usos. Aprovado para a Fase 2.
- **Envio da proposta:** requisitos da Fase 4 aprovados pelo usuário. E-mail com mensagem revisável, PDF e SMTP configurado por variáveis `Email__*`; sem SMTP, abrir `mailto`, baixar o PDF e registrar apresentação somente após confirmação. WhatsApp com mensagem para copiar e link `wa.me`; anexar o PDF manualmente, sem link público. Modelos editáveis pelo Administrador.
- **Estoque de produtos acabados/pronta-entrega:** fora do escopo atual; tratar somente se o usuário solicitar.

## D) Estado atual confirmado

### Git e PRs

- `main` e `origin/main` estavam iguais em `4f1f095` após `git switch main` e `git pull`.
- PR #1 — Fase 0, merged. Head `2a71090f3f96eafec14af7cdb5a729848342d44c`. Incluiu o tratamento de JSON inválido (400 no envelope padrão), Playwright e seed `gestao@` para bancos novos.
- PR #2 — atualização de segurança do Next.js, merged. Head `1856cf70420b2f818cbbd68fe42ea8758a4266a6`. Next atualizado para 16.4.5; o PR reportou `npm audit` sem vulnerabilidades.
- PR #3 — Fase 1, **OPEN**, aguardando o OK do usuário; não mesclar. Head `1639c2f2e5feb5d9d05c28e657caf0fd41befacc`, branch `revisao/fase-1-perfis`, base `main`. Commits da branch: `806f519` (ambiente de revisão), `63d9ff2` (backend) e `1639c2f` (interface/documentação).
- Validações reportadas no PR #3: PostgreSQL real, 102 aprovados, 0 falhas, 0 ignorados; Vitest 5/5 em três execuções; Playwright Edge 3/3; typecheck, build, Compose e comparador de esquema aprovados (0 divergências).
- Branches locais/remotas verificadas:
  - `main`: `4f1f095`, acompanha `origin/main`.
  - `revisao/fase-1-perfis`: local e remota, 3 commits à frente da base; PR #3 aberto.
  - `feature/mvp2-fase-2.0`: local/remota em `1956360`; sem commits exclusivos em relação ao `main` consultado.
  - `ajustes/interface-estoque-orcamento`: local apenas, `c62fcf1`, ancestral do `main`; sem mudanças exclusivas.
  - `checkpoint/pre-evolucao-zalek360-20261116`: local apenas, `9fbdd09`, ancestral do `main`; sem mudanças exclusivas.
  - `chore/next-seguranca`: local em `1856cf7`; referência remota removida após PR #2; commit está no `main`.
  - `revisao/funcional`: local em `2a71090`; referência remota removida após PR #1; commit está no `main`.
  - Branch de contexto `docs/contexto-agentes`: criada a partir de `main` em `4f1f095`; commit inicial `e15e85e`; PR #4 aberto para `main`: https://github.com/DaviMarszalek/Zalek360/pull/4.
- Commits anteriores de referência: Fase 2.0 `f846219` e `4584a96`; ações acessíveis nas listas `49f2767`; PDF `ec6c8a5`/`f71306a`; preenchimento de endereço por CEP `c1cd34d`.
- O relatório da revisão funcional está em `docs/09-revisao-funcional.md`; a versão com resultados da Fase 1 está na branch do PR #3. A cópia em `main` só será atualizada quando o PR #3 for mesclado.

### Ambiente

- No momento da consulta, Docker Desktop estava ativo. O projeto isolado `zalek360-revisao` estava saudável, com PostgreSQL, API e frontend nas portas 55432, 5100 e 3100.
- O projeto principal `zalek360` também estava ativo nas portas 5432, 5000 e 3000. Não foi alterado. Nunca interromper nem limpar esse projeto.
- O arquivo local `.env.revisao` apareceu como **não rastreado** ao voltar para `main`; seu conteúdo não foi lido, copiado ou incluído. O `.gitignore` atualizado e `.env.revisao.example` estão no PR #3, ainda não mesclado no momento desta consulta. Nunca adicionar `.env.revisao` ao Git.

## E) Próximas fases, na ordem aprovada

1. **Fase 2 — Catálogo de produtos:** tela Produtos com pesquisa; adicionar, editar, duplicar, excluir/inativar; preço de venda, custo e composição na mesma tela. Criar Configurações para as opções gerenciáveis. Migrar `TiposPersonalizacao`, `CondicoesPagamento` e `MotivosSugeridos` do código para o banco e semear os valores atuais.
2. **Fase 3 — Orçamento:** preço automático do produto com aviso e restauração; margem interna somente para Administrador; modal “Novo produto” sem perder dados do orçamento; miniatura de referência no orçamento e pedido; disponibilidade/falta de matérias-primas por item, informativa e sem bloquear.
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

- A matriz em `docs/09-revisao-funcional.md` na branch do PR #3 ainda traz em R3.6 uma pergunta antiga sobre preço sugerido. O usuário aprovou preço de venda e custo nas Fases 2/3; atualizar essa nota junto à matriz após o PR #3 ser mesclado, sem alterar o PR enquanto aguarda.
- `docs/08-mvp2.md` ainda descreve OS como fase futura. A decisão de substituir OS por fluxo simples foi aprovada; registrar a decisão na Fase 6. Não remover código legado de OS sem perguntar.
- Para implementar SMTP na Fase 4, o usuário precisa providenciar uma conta de e-mail habilitada para SMTP e senha de aplicativo, além dos parâmetros do provedor. Nunca solicitar que esses segredos sejam commitados; configurar somente no ambiente local/seguro.

## G) Como retomar na próxima sessão

1. Ler `AGENTS.md` e este arquivo.
2. Executar `git switch main; git pull` e confirmar `git log --oneline -3`.
3. Abrir Docker Desktop e confirmar “Engine running”. Não tocar no projeto principal.
4. Conferir o estado dos PRs #3 e desta documentação no GitHub. Não mesclar nenhum PR sem OK do usuário.
5. Usar o ambiente de revisão com `docker compose -p zalek360-revisao --env-file .env.revisao ...`; nunca exibir ou adicionar o `.env.revisao`.
6. Após o merge aprovado do PR #3, iniciar a Fase 2 somente em branch nova a partir do `main` atualizado, com plano curto e aguardando “pode seguir”.
