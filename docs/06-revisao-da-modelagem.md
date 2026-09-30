# 06 — Revisão da modelagem

Esta revisão compara o **dicionário de dados e o DER** (seções 14 e 15 do Documento do Projeto) com a migration implementada, tabela por tabela. O motivo de cada mudança está em `03-alteracoes-e-complementos-na-modelagem.md`.

Convenção geral:
- **ALTERADO:** tipos `datetime` passaram a `timestamp with time zone` (UTC).
- **ALTERADO:** enums são gravados como texto legível (`varchar(30)`).
- **ALTERADO:** campos `text` de livre digitação ganharam limite (`varchar(n)`), validado no domínio com mensagem por campo.
- **ADICIONADO:** auditoria (`criado_em`, `criado_por_id`, `atualizado_em`, `atualizado_por_id`) nas entidades principais.

---

## Cliente → `clientes`

| Marca | Atributos |
|---|---|
| MANTIDO | `id`, `tipo_pessoa`, `cpf_cnpj` (varchar 14, único), `nome_razao_social` (150), `nome_fantasia` (100), `telefone` (15), `whatsapp` (15), `email` (150), `data_cadastro`; regra "ao menos um meio de contato" |
| ALTERADO | `endereco_*` explicitado em `endereco_cep`, `endereco_logradouro`, `endereco_numero`, `endereco_complemento`, `endereco_bairro`, `endereco_cidade`, `endereco_uf`; `cpf_cnpj` gravado só com dígitos (check 11 ou 14) |
| ADICIONADO | `contato_nome`, `contato_cargo`, `busca_normalizada`, `versao`, auditoria |
| REMOVIDO | `observacoes_internas` → tabela `observacoes_cliente` |

## Observação do cliente → `observacoes_cliente` (**tabela ADICIONADA**)

`id`, `cliente_id` (FK, cascade), `texto` (2000), `criado_em`, `autor_id` (FK usuário).

## Orçamento → `orcamentos`

| Marca | Atributos |
|---|---|
| MANTIDO | `id`, `numero` (único), `cliente_id`, `responsavel_id`, `data_criacao` (date), `validade` (date, padrão 15 dias), `prazo_estimado` (dias úteis), `condicao_pagamento` (100), `situacao`, `subtotal_produtos`, `valor_personalizacao`, `valor_total` (numeric 10,2), `data_ultima_edicao` |
| ALTERADO | `situacao`: "Enviado" → `AguardandoRetorno`, com `Cancelado` explícito; `desconto` passa a obrigatório (padrão 0, check ≥ 0); `observacoes_comerciais` text → varchar(2000); check `validade >= data_criacao` |
| ADICIONADO | `aguardando_retorno_desde`, `versao`, auditoria; sequência `seq_orcamento_numero` |
| REMOVIDO | — |

## Item do orçamento → `itens_orcamento`

| Marca | Atributos |
|---|---|
| MANTIDO | `id`, `orcamento_id`, `produto_id`, `quantidade` (check > 0), `valor_unitario`, `valor_personalizacao_unit`, `tipo_personalizacao` (100), `cor_peca` (50), `cores_arte` (100), `medidas` (100), `local_aplicacao` (100) |
| ALTERADO | `referencia_arte` text → varchar(500); `observacoes_tecnicas` text → varchar(2000) |
| ADICIONADO | `ordem`, `produto_nome` (snapshot), `descricao` (500), `subtotal_produto`, `total_personalizacao`, `valor_total` |
| REMOVIDO | — |

## Arquivo anexo → `arquivos_anexos`

| Marca | Atributos |
|---|---|
| MANTIDO | `id`, `nome_arquivo` (255); limite de 20 MB (agora também validado no servidor) |
| ALTERADO | `item_orcamento_id` obrigatório → **opcional** (upload antecipado); `tamanho_kb` → `tamanho_bytes` (bigint, check > 0); `url` → `chave_armazenamento` (500, interna) |
| ADICIONADO | `item_pedido_id`, `anexo_origem_id`, `extensao` (10), `content_type` (100), `criado_em`, `criado_por_id` |
| REMOVIDO | `url` (URL pública), `tamanho_kb` |

## Contato do orçamento → `contatos_orcamento`

| Marca | Atributos |
|---|---|
| MANTIDO | `id`, `orcamento_id`, `tipo_contato` (WhatsApp / Ligação / E-mail / Presencial / Outro), `data_hora`, `observacao` (obrigatória) |
| ALTERADO | `registrado_por` → `registrado_por_id`; `observacao` text → varchar(2000) |
| ADICIONADO | `criado_em` |
| REMOVIDO | — |

## Decisão do orçamento → `decisoes_orcamento`

| Marca | Atributos |
|---|---|
| MANTIDO | `id`, `orcamento_id` (único, 1:1), `resultado` (Aprovado / Recusado / Cancelado / Expirado), `data_hora`, `motivo` (100), `observacao`, `registrado_por` (nulo na expiração) |
| ALTERADO | `meio_contato` obrigatório → nulo **somente** na expiração automática (RN7 continua obrigatória nas decisões manuais); `registrado_por` → `registrado_por_id`; `observacao` text → varchar(2000) |
| ADICIONADO | `automatica` (boolean), `criado_em` |
| REMOVIDO | — |

## Pedido → `pedidos`

| Marca | Atributos |
|---|---|
| MANTIDO | `id`, `numero` (único), `orcamento_origem_id` (FK única, RN9), `cliente_id` (herdado, desnormalizado), `prazo_entrega`, `valor_total`, `data_criacao` |
| ALTERADO | `situacao`: "Aberto/Registrado (MVP1) · Em Produção/Pronto/Entregue (MVP2+)" → `Aberto`, `EmProducao`, `Pronto`, `Entregue`, `Cancelado`, com atualização manual já no MVP 1 |
| ADICIONADO | `responsavel_id`, `data_pedido`, `prazo_estimado`, `condicao_pagamento`, `observacoes_comerciais`, `observacao_decisao`, `subtotal_produtos`, `valor_personalizacao`, `desconto`, `entregue_em`, `cancelado_em`, `motivo_cancelamento`, `versao`, auditoria; sequência `seq_pedido_numero` |
| REMOVIDO | — |

## Item do pedido → `itens_pedido`

| Marca | Atributos |
|---|---|
| MANTIDO | Mesma estrutura do item do orçamento, copiada na conversão (RF 4.1.3.1) |
| ALTERADO | Mesmos limites de texto do item do orçamento |
| ADICIONADO | `item_orcamento_origem_id` (FK), `ordem`, `produto_nome`, `descricao`, `subtotal_produto`, `total_personalizacao`, `valor_total` |
| REMOVIDO | — |

## Produto → `produtos`

| Marca | Atributos |
|---|---|
| MANTIDO | `id`, `nome` (100, agora único), `categoria` (50), `descricao_base` (text) |
| ADICIONADO | `ativo` |

## Usuário → `usuarios`

| Marca | Atributos |
|---|---|
| MANTIDO | `id`, `nome` (100), `email` (150, único), `senha_hash` (255, BCrypt), `perfil` |
| ALTERADO | `perfil`: Atendente / Gestor / Operador **+ Administrador** |
| ADICIONADO | `ativo`, `criado_em` |

## Histórico → `historico_eventos` (**tabela ADICIONADA**)

`id`, `escopo` (Cliente / Orçamento / Pedido), `tipo` (40), `cliente_id` (obrigatório), `orcamento_id`, `pedido_id`, `titulo` (200), `descricao` (4000), `situacao_anterior`, `situacao_nova`, `meio_contato`, `ocorrido_em`, `registrado_em`, `usuario_id` (nulo para eventos automáticos).

---

## Diagrama de classes (seção 13)

| Classe / método no documento | Implementação | Marca |
|---|---|---|
| `Orcamento.calcularValorTotal()` (RN4) | `CalculadoraOrcamento.Calcular` + recálculo interno do agregado a cada alteração | ALTERADO (extraído para serviço de domínio reutilizado por `POST /calculo`) |
| `Orcamento.podeSerEditado()` (RN6) | `Orcamento.PodeSerEditado` | MANTIDO |
| `Orcamento.registrarDecisao(resultado, meioContato, dataHora)` | `Orcamento.RegistrarDecisao(…, motivo, observacao, …)` | MANTIDO (com motivo e observação) |
| `Orcamento.gerarPedido()` | `Pedido.CriarAPartirDe(orcamento, numero, …)`, chamado pelo serviço na mesma transação | ALTERADO (uma única fábrica, no `Pedido`) |
| `Cliente.validarCpfCnpjUnico()` (RN2) | `DocumentoFiscal` valida o DV na entidade; a unicidade é verificada no serviço (repositório) e garantida pelo índice único | ALTERADO (a unicidade depende do banco, não da entidade) |
| `ItemOrcamento.calcularValorTotalItem()` | Totais do item calculados pela `CalculadoraOrcamento` e gravados | MANTIDO |
| `Pedido.criarAPartirDe(orcamento)` (RN9) | `Pedido.CriarAPartirDe` | MANTIDO |
| `ContatoOrcamento` imutável | Sem métodos de alteração | MANTIDO |
| — | `Orcamento.RegistrarContato`, `MarcarAguardandoRetorno`, `ExpirarAutomaticamente`, `Atualizar`; `Pedido.AtualizarSituacao` | ADICIONADO |
