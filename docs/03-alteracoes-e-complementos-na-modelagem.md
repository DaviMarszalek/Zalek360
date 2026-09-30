# 03 — ALTERAÇÕES E COMPLEMENTOS NA MODELAGEM

Cada item compara a modelagem do Documento do Projeto (DER, dicionário de dados, seção 15, e diagrama de classes, seção 13) com o que foi implementado na migration `20260928120000_InitialCreate`.

Convenções físicas aplicadas em todas as tabelas:
- nomes em português, em `snake_case`;
- chaves UUID geradas pela aplicação;
- dinheiro em `numeric(10,2)`;
- datas de negócio em `date`, instantes em `timestamp with time zone` (UTC);
- enums gravados como texto legível;
- nomes determinísticos de restrições: `pk_`, `fk_`, `ix_`, `ux_`, `ck_`.

---

### 1. Situações do orçamento

| | |
|---|---|
| **Estrutura original** | RN5 / RF 4.1.2.5: Em Elaboração → Enviado ao Cliente → Aprovado / Recusado / Expirado. O dicionário já cita "Aguardando Retorno" e "Cancelado". |
| **Alteração** | Enum `SituacaoOrcamento` = `EmElaboracao`, `AguardandoRetorno`, `Aprovado`, `Recusado`, `Expirado`, `Cancelado`. Nova coluna `aguardando_retorno_desde` (timestamptz, nula). |
| **Motivo** | Protótipo e dicionário; o envio acontece fora do sistema. O cancelamento vem do RF 4.1.3.4. A data de apresentação alimenta "aguardando desde…" no detalhe e no dashboard. |
| **Impacto** | Transições validadas no agregado: aprovar ou recusar só a partir de Aguardando Retorno; cancelar a partir de Em Elaboração ou Aguardando Retorno; expirar automaticamente. Um estado final não aceita outra decisão nem edição. |
| **Tabelas** | `orcamentos` (`situacao` varchar(30); `aguardando_retorno_desde`) |

### 2. Situações e ciclo do pedido

| | |
|---|---|
| **Estrutura original** | RF 4.1.3.3: situação inicial "Registrado"; avanço só no MVP 2. Dicionário: "Aberto/Registrado (MVP1) · Em Produção/Pronto/Entregue (MVP2+)". |
| **Alteração** | Enum `SituacaoPedido` = `Aberto`, `EmProducao`, `Pronto`, `Entregue`, `Cancelado`. Atualização manual, uma etapa por vez. Cancelamento com motivo obrigatório a partir de Aberto, Em produção ou Pronto. Novas colunas `entregue_em`, `cancelado_em`, `motivo_cancelamento`. |
| **Motivo** | Protótipo: telas 25 e 26 ("Atualizar status", "Cancelar pedido", etapas 1–4) e dashboard (tela 02: pedidos em produção e entregues no mês). RF 4.1.3.4 (cancelar pedido registrado). |
| **Impacto** | `PATCH /api/pedidos/{id}/status` com validação de transição e registro no histórico. No MVP 2, a integração com a Ordem de Serviço (RF 4.3.4.2) poderá acionar as mesmas transições automaticamente. |
| **Tabelas** | `pedidos` |

### 3. Pedido como cópia completa do orçamento

| | |
|---|---|
| **Estrutura original** | `pedidos`: id, numero, orcamento_origem_id, cliente_id, situacao, prazo_entrega, valor_total, data_criacao. |
| **Alteração** | Novas colunas `responsavel_id`, `data_pedido`, `prazo_estimado`, `condicao_pagamento`, `observacoes_comerciais`, `observacao_decisao`, `subtotal_produtos`, `valor_personalizacao`, `desconto`, `versao` e auditoria (`criado_por_id`, `atualizado_em`, `atualizado_por_id`). |
| **Motivo** | O RF 4.1.3.1 manda copiar "itens, quantidades, valores, prazos e especificações". A tela 25 exibe resumo financeiro, condições, responsável e observação da decisão. O pedido precisa ser um retrato imutável do que foi aprovado, e não uma referência que mudaria se o orçamento mudasse. |
| **Impacto** | A consulta do pedido não depende de juntar dados com o orçamento. `prazo_entrega` é calculado em dias úteis a partir da aprovação. |
| **Tabelas** | `pedidos` |

### 4. Itens: snapshot, ordem e totais persistidos

| | |
|---|---|
| **Estrutura original** | `itens_orcamento` e `itens_pedido` com produto, quantidade, valores unitários e campos de personalização. |
| **Alteração** | Nos dois: `ordem`, `produto_nome` (nome do produto no momento do orçamento), `descricao`, e os totais calculados `subtotal_produto`, `total_personalizacao`, `valor_total`. Em `itens_pedido`: `item_orcamento_origem_id` (FK para o item de origem). Checks `quantidade > 0`. |
| **Motivo** | Preservar a ordem e o nome exibidos ao cliente, mesmo se o catálogo mudar. RN4 com total gravado e auditável. Rastreabilidade item a item (RNF 5.2.2). |
| **Impacto** | O cálculo é feito uma única vez no domínio (`CalculadoraOrcamento`) e gravado; as telas apenas exibem. |
| **Tabelas** | `itens_orcamento`, `itens_pedido` |

### 5. Arquivos anexos (artes)

| | |
|---|---|
| **Estrutura original** | `arquivos_anexos`: id, item_orcamento_id (obrigatório), nome_arquivo, tamanho_kb, url. |
| **Alteração** | `item_orcamento_id` passa a **opcional**. Novas colunas `item_pedido_id` e `anexo_origem_id`. `url` foi substituída por `chave_armazenamento` (interna) e `tamanho_kb` por `tamanho_bytes` (bigint, check `> 0`). Novas colunas `extensao`, `content_type`, `criado_em`, `criado_por_id`. |
| **Motivo** | O arquivo é enviado **antes** de o orçamento ser salvo (tela 14: "Clique para anexar ou arraste o arquivo"; a interface mostra o progresso do envio) e fica "pendente" até ser vinculado. Na aprovação, o pedido recebe cópias dos registros, com o mesmo arquivo físico e vínculo à origem. Sem URL pública: o download exige autenticação. O tipo real é conferido pela assinatura do conteúdo. |
| **Impacto** | `POST /api/anexos` devolve o id, e o orçamento vincula por `anexoIds`. Uma rotina remove os pendentes com mais de 24 h. Tipos permitidos: PDF, PNG, JPG/JPEG, AI e CDR, com até 20 MB. |
| **Tabelas** | `arquivos_anexos` |

### 6. Observações internas do cliente

| | |
|---|---|
| **Estrutura original** | `clientes.observacoes_internas` (text). |
| **Alteração** | Nova tabela `observacoes_cliente` (id, cliente_id, texto, criado_em, autor_id). A coluna foi removida de `clientes`. |
| **Motivo** | A tela 30 mostra uma lista de observações, cada uma com autor e data, e um campo "Nova observação". |
| **Impacto** | O cadastro aceita uma observação inicial opcional, e a aba Observações lista e adiciona novas. |
| **Tabelas** | `observacoes_cliente` (nova), `clientes` |

### 7. Cliente: contato responsável, busca e endereço

| | |
|---|---|
| **Estrutura original** | Nome/razão social, fantasia, telefone, whatsapp, email, `endereco_*`, data_cadastro. |
| **Alteração** | Novas colunas `contato_nome` e `contato_cargo`. O endereço foi explicitado em `endereco_cep`, `endereco_logradouro`, `endereco_numero`, `endereco_complemento`, `endereco_bairro`, `endereco_cidade`, `endereco_uf`. Novas colunas `busca_normalizada` (sem acentos, minúsculas, com documento e telefones), `versao` e auditoria. Check de tamanho do CPF/CNPJ (11 ou 14 dígitos, gravados sem máscara). |
| **Motivo** | O protótipo exibe "Rafael Costa · Sócio-proprietário" (telas 09 e 27). A pesquisa por nome, documento ou telefone (tela 03) precisa ser rápida e sem acentos. A concorrência otimista evita sobrescrever a edição de outro atendente. |
| **Impacto** | Índices por cidade e por tipo de pessoa; índice único em `cpf_cnpj` (RN2). |
| **Tabelas** | `clientes` |

### 8. Decisão e contato do orçamento

| | |
|---|---|
| **Estrutura original** | Decisão: meio_contato e data_hora obrigatórios; registrado_por nulo quando Expirado. Contato: tipo, data_hora, observação, registrado_por. |
| **Alteração** | `decisoes_orcamento.meio_contato` passa a **nulo** somente na expiração automática. Nova coluna `automatica` (boolean). Chaves estrangeiras como `registrado_por_id`. Nova coluna `criado_em` em ambos. Índice único em `decisoes_orcamento.orcamento_id` (1:1). |
| **Motivo** | A expiração não tem meio de contato nem usuário. A RN7 continua exigindo meio e data/hora em toda decisão **manual**, e o domínio valida isso. `criado_em` separa quando a decisão aconteceu de quando foi registrada. |
| **Impacto** | Motivos sugeridos: Preço, Prazo de entrega, Escolheu outro fornecedor, Desistiu da compra, Mudança no evento, Outro. Data/hora não pode estar no futuro nem ser anterior à data do orçamento. |
| **Tabelas** | `decisoes_orcamento`, `contatos_orcamento` |

### 9. Histórico unificado

| | |
|---|---|
| **Estrutura original** | Não existe. O histórico seria derivado de contatos e decisões. |
| **Alteração** | Nova tabela `historico_eventos`: escopo, tipo, cliente_id, orcamento_id, pedido_id, título, descrição, situação anterior e nova, meio_contato, ocorrido_em, registrado_em, usuario_id. |
| **Motivo** | RNF 5.2.2 (toda transição de status rastreável). UC05 e as linhas do tempo das telas 16, 25 e 27 misturam alterações do sistema, contatos e decisões. |
| **Impacto** | Os eventos são gerados pelos agregados e gravados no mesmo `SaveChanges` da alteração, portanto na mesma transação. Índices por (cliente, data), (orçamento, data) e (pedido, data). |
| **Tabelas** | `historico_eventos` (nova) |

### 10. Numeração sequencial

| | |
|---|---|
| **Estrutura original** | `numero` int sequencial (#000124, #000057). |
| **Alteração** | Sequências `seq_orcamento_numero` e `seq_pedido_numero` no PostgreSQL. Índice único em `numero`. Na inicialização, `setval` ajusta as sequências ao maior número existente. |
| **Motivo** | Numeração sem colisão entre atendentes simultâneos (RNF 5.5.2). O número só é obtido depois que os dados são validados, então uma tentativa inválida não consome número. |
| **Impacto** | Com o seed, o próximo orçamento é o #000124 e o próximo pedido é o #000057, como no protótipo. |
| **Tabelas** | `orcamentos`, `pedidos` |

### 11. Usuário e produto

| | |
|---|---|
| **Estrutura original** | Usuário: perfil Atendente / Gestor / Operador. Produto: nome, categoria, descrição. |
| **Alteração** | Novo perfil `Administrador`. Novas colunas `ativo` em ambos e `criado_em` em usuários. Índice único em `usuarios.email` e `produtos.nome`. |
| **Motivo** | Existência de um usuário administrativo no seed. Inativar sem excluir (RN10, por extensão). Login por e-mail único. |
| **Impacto** | Política "Comercial" = Atendente, Gestor e Administrador. Operador fica reservado para a produção no MVP 2. |
| **Tabelas** | `usuarios`, `produtos` |

### 12. Concorrência e auditoria

| | |
|---|---|
| **Estrutura original** | Apenas `data_ultima_edicao` em orçamentos. |
| **Alteração** | `versao` (inteiro, *concurrency token*) em clientes, orçamentos e pedidos. Novas colunas `criado_em`, `criado_por_id`, `atualizado_em`, `atualizado_por_id` nas entidades principais. `data_ultima_edicao` foi mantida (RF 4.1.2.6). |
| **Motivo** | RNF 5.2.1. Duas pessoas editando ou decidindo o mesmo orçamento não podem sobrescrever uma à outra sem aviso. |
| **Impacto** | Toda alteração envia a `versao` lida; se estiver desatualizada, a API responde 409 `CONFLITO_CONCORRENCIA` e a tela pede para recarregar. |
| **Tabelas** | `clientes`, `orcamentos`, `pedidos` |

---

## Restrições do banco (resumo)

- **Índices únicos:**
  - `usuarios.email`, `produtos.nome`, `clientes.cpf_cnpj`;
  - `orcamentos.numero`, `decisoes_orcamento.orcamento_id`;
  - `pedidos.numero`, `pedidos.orcamento_origem_id` (um pedido por orçamento, RN9).
- **Checks:**
  - `ck_clientes_cpf_cnpj_tamanho`, `ck_orcamentos_valores`, `ck_orcamentos_validade`;
  - `ck_itens_orcamento_quantidade`, `ck_pedidos_valores`, `ck_itens_pedido_quantidade`;
  - `ck_arquivos_anexos_tamanho`.
- **Chaves estrangeiras:** 22 no total.
  - `Cascade` apenas de pais para filhos da mesma composição (orçamento → itens, contatos e decisão; pedido → itens; cliente → observações);
  - `SetNull` do item de orçamento para os anexos;
  - `Restrict` em todas as demais, para que nada histórico possa ser apagado indiretamente.

## Dados de demonstração: CPF/CNPJ corrigidos

A RN2 exige documento válido. Os números do protótipo têm dígito verificador inválido, por isso o seed mantém a base e corrige o DV:

| Cliente | Protótipo | Seed |
|---|---|---|
| Academia Movimento | 38.221.907/0001-64 | 38.221.907/0001-53 (**não** incluída no seed: é cadastrada no roteiro de demonstração) |
| Metalúrgica Horizonte Ltda. | 12.345.678/0001-90 | 12.345.678/0001-95 |
| Construtora Vale Sul | 45.908.112/0001-37 | 45.908.112/0001-83 |
| Colégio Aurora | 07.231.554/0001-82 | 07.231.554/0001-49 |
| Clínica Bem Viver | 29.117.340/0001-06 | 29.117.340/0001-89 |
| Transportes Rota Litoral | 33.604.219/0001-55 | 33.604.219/0001-59 |
| João Martins (PF) | 318.442.960-05 | 318.442.960-16 |
| Ana Paula (PF) | 027.815.330-41 | 027.815.330-50 |

O seed também usa **datas relativas**: o dia 24/09/2026 do protótipo corresponde ao dia em que o banco é populado. Assim, validades, prazos, "aguardando desde…" e a expiração do #000116 continuam coerentes em qualquer data de execução.
