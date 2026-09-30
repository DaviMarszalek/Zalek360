# 01 — Análise inicial

## Objetivo do MVP 1

Entregar funcionando o **ciclo comercial** da Zalek Personalizados, descrito nas seções 4.1.1 a 4.1.4 do Documento do Projeto:
1. cadastrar o cliente;
2. montar o orçamento com itens e personalização;
3. apresentá-lo ao cliente e registrar os contatos e a decisão;
4. converter automaticamente o orçamento aprovado em pedido;
5. consultar o histórico.

As integrações da seção 4.1.5 (estoque, ordens de serviço e financeiro) ficam para o MVP 2.

O sistema é **interno**. O cliente é ator indireto: decide fora do sistema, por WhatsApp, ligação, e-mail ou presencialmente, e o atendente registra essa decisão informando o meio de contato e a data/hora.

## Fontes e ordem de prioridade

Quando as fontes divergiram, prevaleceu a de maior prioridade:

1. Prompt do projeto (requisitos da entrega).
2. Casos de uso UC01–UC05 e regras de negócio RN1–RN10.
3. Protótipos do MVP 1 (32 telas).
4. Requisitos do ambiente (RF/RNF).
5. Diagramas (componentes, classes, DER) e dicionário de dados.

O exemplo genérico do dicionário de dados (tabela com `cod_matricula`) é modelo de preenchimento do template acadêmico e **não** foi implementado.

## Escopo implementado

| Área | Conteúdo |
|---|---|
| Autenticação | Login por e-mail corporativo; JWT em cookie httpOnly (e Bearer para Swagger); senhas com BCrypt; perfis |
| Dashboard | Indicadores por situação; orçamentos recentes; aguardando retorno com dias para vencer; pedidos em andamento e concluídos no mês |
| Clientes | Pesquisa por nome, CPF/CNPJ, telefone ou e-mail; cadastro PF/PJ com validação de DV e de duplicidade; detalhe com abas Visão geral, Orçamentos, Pedidos, Observações e Histórico; edição |
| Orçamentos | Criação e edição com itens, personalização e anexos; cálculo no servidor; apresentação ao cliente; contatos; decisão (aprovar, recusar, cancelar); expiração automática |
| Pedidos | Geração automática e transacional na aprovação; lista e detalhe; atualização de status em uma etapa por vez; cancelamento com motivo; criação direta bloqueada |
| Transversal | Histórico unificado; concorrência otimista; erros padronizados; seed com os dados do protótipo; Docker Compose; testes |

## Conflitos entre as fontes e como foram resolvidos

| # | Conflito | Decisão | Base |
|---|---|---|---|
| 1 | O RF 4.1.2.5, o UC03 e a RN5 chamam a segunda situação do orçamento de **"Enviado ao Cliente"**. O protótipo, o diagrama de classes e o dicionário usam **"Aguardando Retorno"**. | **Aguardando Retorno** | Protótipo + dicionário; descreve melhor um sistema em que o envio ocorre fora dele |
| 2 | O RF 4.1.3.3 diz que o pedido inicia como **"Registrado"** e que o avanço de status depende do MVP 2. O protótipo mostra o pedido **"Aberto"** e o botão **"Atualizar status"**, na sequência Aberto → Em produção → Pronto → Entregue. O dicionário aceita "Aberto/Registrado". | **Aberto**, com atualização manual (uma etapa por vez) já no MVP 1 | Protótipo (telas 25, 26 e 02) e dicionário |
| 3 | **Numeração das RNs** diferente entre a seção 7, o UC03 e o diagrama de classes. Por exemplo, "edição" é RN7 na seção 7 e RN6 no UC03, e "decisão com meio e data/hora" é RN7 no UC03 e RN10 no diagrama de classes. | Numeração consolidada (tabela abaixo), usada no código, nos testes e nesta documentação | Ver `07-insumos-diagramas-der-dicionario.md` |
| 4 | O UC04 (RN9) menciona "opção de pedido sem orçamento", e o RF 4.1.3.2 cita uma exceção para "produtos prontos para venda, sem especificação". Os requisitos da entrega exigem que todo pedido nasça de orçamento aprovado. | Criação direta **sempre rejeitada**: `POST /api/pedidos` → 422 `PEDIDO_CRIACAO_DIRETA_NAO_PERMITIDA`, "Pedido deve ser originado de um orçamento aprovado." A exceção fica registrada como evolução. | Prompt (prioridade 1) |
| 5 | O UC03 tem como condição de entrada "Atendente aciona **Exportar Orçamento**". | A ação implementada é **"Apresentar ao cliente"**, que registra o envio feito fora do sistema, com meio opcional, e muda o status. A exportação em PDF **não** foi implementada no MVP 1. | Protótipo; sistema interno |
| 6 | A RN5 não lista **Cancelado**, mas o RF 4.1.3.4 permite cancelar o orçamento antes da conversão, e o dicionário inclui Cancelado. | Cancelado incluído; permitido a partir de Em Elaboração e de Aguardando Retorno | RF 4.1.3.4 + dicionário |
| 7 | O UC05 lista **Cliente** como ator. | Apenas o atendente acessa; o cliente continua sem acesso ao sistema | Prompt |
| 8 | Vários **CPF/CNPJ do protótipo têm dígito verificador inválido**. | O seed usa os mesmos números-base com o DV corrigido, porque a validação de DV faz parte da RN2 | RN2 |
| 9 | O dicionário guarda anexos por `url` e `tamanho_kb`. | Guarda `chave_armazenamento` (interna) e `tamanho_bytes`. O download passa por endpoint autenticado, sem URL pública. | RNF 5.4 (segurança) |
| 10 | O dicionário tem `clientes.observacoes_internas` como um único texto. O protótipo mostra uma lista de observações com autor e data. | Tabela `observacoes_cliente` | Protótipo (tela 30) |

## Regras de negócio (numeração consolidada)

| RN | Regra | Onde é garantida |
|---|---|---|
| RN1 | Todo orçamento pertence a um cliente já cadastrado | `Orcamento.Criar` + FK `orcamentos.cliente_id` |
| RN2 | CPF/CNPJ válido (DV) e único | `DocumentoFiscal`, `ClienteService`, índice único `ux_clientes_cpf_cnpj` |
| RN3 | Orçamento tem ao menos um item com especificação de personalização | `Orcamento` (validação de itens) |
| RN4 | Totais calculados automaticamente pelo sistema | `CalculadoraOrcamento` (usada no domínio e em `POST /calculo`) |
| RN5 | Situações: Em Elaboração (inicial), Aguardando Retorno, Aprovado, Recusado, Expirado, Cancelado | `SituacaoOrcamento` + transições no agregado |
| RN6 | Edição somente em Em Elaboração ou Aguardando Retorno | `Orcamento.PodeSerEditado` + UI bloqueada |
| RN7 | Toda decisão manual exige meio de contato e data/hora; contato não altera status | `Orcamento.RegistrarDecisao` / `RegistrarContato` |
| RN8 | A aprovação converte automaticamente em pedido, copiando itens, valores, personalização e artes | `OrcamentoService.RegistrarDecisaoAsync` (transação) + `Pedido.CriarAPartirDe` |
| RN9 | Não existe criação direta de pedido | Fábrica única + `POST /api/pedidos` → 422 |
| RN10 | Orçamentos e pedidos nunca são excluídos, apenas cancelados, com histórico preservado | Ausência de DELETE + `historico_eventos` |

## Premissas adotadas

- **Validade padrão:** 15 dias. **Prazo estimado:** em dias úteis (segunda a sexta), contado da data da aprovação.
- **Fuso de negócio:** America/Sao_Paulo. Datas e horas são gravadas em UTC.
- A aprovação só é aceita até o fim do dia da validade.
- **Expiração:** um orçamento Aguardando Retorno com validade vencida passa a Expirado a partir de 00:00 do dia seguinte, por rotina em segundo plano que roda de hora em hora e também na inicialização.
- **Perfis:** Atendente, Gestor e Administrador acessam o módulo comercial. Operador (produção, MVP 2) não acessa.
- Números de orçamento e de pedido vêm de sequências próprias no PostgreSQL. Uma tentativa inválida não consome número.
