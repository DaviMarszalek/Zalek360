# 02 — Matriz de rastreabilidade

## Como ler esta matriz

- **Tela:** número da página no PDF *Zalek360 — Protótipos MVP 1* (T01–T32) e rota no frontend.
- **Teste:** classe e método em `backend/tests/Zalek360.Tests`:
  - **D** = domínio (`Dominio/`);
  - **A** = casos de uso (`Aplicacao/CasosDeUsoTests`);
  - **I** = integração EF + SQLite (`Integracao/IntegracaoTests`).

  Os testes D e A foram executados (41/41). Os testes I estão compilados, mas não foram executados neste ambiente.
- As regras usam a numeração consolidada de `01-analise-inicial.md`.

## Mapa das telas do protótipo

| Tela | Conteúdo | Rota / componente |
|---|---|---|
| T01 | Login | `/login` · `features/auth/TelaLogin` |
| T02 | Dashboard | `/dashboard` · `features/dashboard/Dashboard` |
| T03 | Clientes — lista e pesquisa | `/clientes` · `ListaClientes` |
| T04 | Clientes — nenhum encontrado | `/clientes?busca=…` · `ListaClientes` (estado vazio) |
| T05 | Novo cliente — formulário | `/clientes/novo` · `FormularioCliente` |
| T06 | Novo cliente — validação | idem (erros por campo) |
| T07 | Novo cliente — preenchido válido | idem |
| T08 | Novo cliente — CNPJ já cadastrado | idem (verificação ao sair do campo + 409 no salvar) |
| T09 | Cliente cadastrado com sucesso | `/clientes/{id}` · `DetalheCliente`, aberto após o salvar com notificação de sucesso (ou direto no novo orçamento, se escolhido) |
| T10 | Orçamentos — lista | `/orcamentos` · `ListaOrcamentos` |
| T11 | Novo orçamento — seleção de cliente | `/orcamentos/novo` · `FormularioOrcamento` |
| T12 | Novo orçamento — sem itens | idem |
| T13 | Novo orçamento — erro "adicione pelo menos um item" | idem |
| T14 | Novo orçamento — itens, personalização e anexos | idem |
| T15 | Orçamento #000124 — Em Elaboração (salvo) | `/orcamentos/{id}` · `DetalheOrcamento` |
| T16 | Aguardando Retorno com contatos | idem |
| T17 | Modal Registrar contato | idem · `ModalContato` |
| T18 | Modal Registrar decisão | idem · `ModalDecisao` |
| T19 | Modal decisão — erro de validação | idem |
| T20 | Recusado | idem (faixa por status) |
| T21 | Expirado (#000116) | idem |
| T22 | Modal "Aprovar orçamento e gerar pedido?" | idem (confirmação) |
| T23 | Aprovado + pedido gerado | idem · modal de sucesso |
| T24 | Aprovado (detalhe) | idem |
| T25 | Pedido #000057 — detalhe | `/pedidos/{id}` · `DetalhePedido` |
| T26 | Pedidos — lista | `/pedidos` · `ListaPedidos` |
| T27 | Cliente — Visão geral | `/clientes/{id}` (aba Visão geral) |
| T28 | Cliente — aba Orçamentos | `/clientes/{id}?aba=orcamentos` |
| T29 | Cliente — aba Pedidos | `/clientes/{id}?aba=pedidos` |
| T30 | Cliente — aba Observações | `/clientes/{id}?aba=observacoes` |
| T31 | Cliente com histórico — Visão geral (Metalúrgica) | `/clientes/{id}` |
| T32 | Cliente — Orçamentos filtrados por período e status | `/clientes/{id}?aba=orcamentos` |

## Matriz

| UC / fluxo | Tela | Endpoint | Entidade / tabela | Regra | Teste |
|---|---|---|---|---|---|
| **Pré-condição** — autenticação (RNF 5.4.1) | T01 | `POST /api/auth/login` · `GET /api/auth/me` · `POST /api/auth/logout` | `Usuario` / `usuarios` | BCrypt (RNF 5.4.3); perfis comerciais (RNF 5.4.2) | — (cobertura manual pelo roteiro do README) |
| **UC01** fluxo principal — pesquisar antes de cadastrar | T03, T04 | `GET /api/clientes?busca=` | `Cliente` / `clientes.busca_normalizada` | RN2 (evitar duplicidade) | — |
| **UC01** passos 1–4 — cadastrar PF/PJ | T05, T07, T09 | `POST /api/clientes` | `Cliente`, `ObservacaoCliente`, `HistoricoEvento` / `clientes`, `observacoes_cliente`, `historico_eventos` | RN2; ao menos um meio de contato (dicionário) | D `ClienteTests.Cadastra_cliente_pj_valido_com_documento_normalizado`, `Cadastra_cliente_pf_valido`, `Exige_tipo_de_pessoa`, `Exige_documento_nome_e_ao_menos_um_meio_de_contato`; A `Cadastra_cliente_e_observacao_inicial` |
| **UC01** — validação | T06 | `POST /api/clientes` → 400 `CLIENTE_DADOS_INVALIDOS` | `Cliente` | RN2 (DV) | D `Rejeita_cnpj_com_digito_verificador_invalido`, `Rejeita_cpf_com_digitos_repetidos`, `Criar_com_dados_invalidos_lanca_excecao_de_validacao` |
| **UC01 A1** — CPF/CNPJ já cadastrado | T08 | `GET /api/clientes/verificar-documento` · `POST /api/clientes` → 409 `CLIENTE_CPF_CNPJ_DUPLICADO` | `clientes` (índice único `ux_clientes_cpf_cnpj`) | RN2 | A `Cpf_cnpj_duplicado_retorna_conflito_com_cliente_existente`, `Verificacao_de_documento_informa_invalido_disponivel_e_existente` |
| UC01 — editar cadastro | T27 ("Editar cadastro") | `PUT /api/clientes/{id}` (com `versao`) | `Cliente` | RN2; concorrência otimista | A `Versao_desatualizada_retorna_conflito` (mecanismo compartilhado) |
| **UC02** passos 1–4 — criar orçamento | T11, T12, T14, T15 | `POST /api/orcamentos` · `GET /api/produtos` · `GET /api/produtos/opcoes` · `GET /api/usuarios/responsaveis` | `Orcamento`, `ItemOrcamento` / `orcamentos`, `itens_orcamento`; sequência `seq_orcamento_numero` | RN1, RN3, RN4, RN5 (Em Elaboração) | D `Orcamento_nasce_em_elaboracao_com_total_calculado_pelo_sistema`, `Calcula_total_do_orcamento_123_do_prototipo`, `Aplica_desconto_no_total`; A `Cria_orcamento_com_numero_sequencial_e_total_recalculado`, `Orcamento_exige_cliente_cadastrado` |
| UC02 passo 2 — cálculo automático | T14 (resumo) | `POST /api/orcamentos/calculo` | — (serviço de domínio `CalculadoraOrcamento`) | RN4 | A `Calculo_de_pre_visualizacao_nao_depende_do_banco`; D `Desconto_maior_que_valor_dos_itens_e_rejeitado` |
| UC02 — artes de referência (RF 4.1.2.2) | T14 | `POST /api/anexos` · `DELETE /api/anexos/{id}` · `GET /api/anexos/{id}/download` | `ArquivoAnexo` / `arquivos_anexos` | tipos PDF/PNG/JPG/AI/CDR; até 20 MB; assinatura do conteúdo | I `Aprovacao_persiste_orcamento_pedido_itens_anexos_e_historico` (anexos copiados) |
| **UC02 A1** — orçamento sem item | T13 | `POST /api/orcamentos` → 400 `ORCAMENTO_SEM_ITENS` | `Orcamento` | RN3 | D `Orcamento_sem_item_nao_e_criado`; A `Orcamento_sem_itens_retorna_erro_de_validacao_e_nao_consome_numero` |
| **UC03** passo 1 — apresentar ao cliente | T15 → T16 | `POST /api/orcamentos/{id}/aguardando-retorno` | `Orcamento`, `HistoricoEvento` | RN5 | D `Nao_marca_aguardando_retorno_com_validade_vencida` |
| UC03 passo 2 — registrar contato | T16, T17 | `POST /api/orcamentos/{id}/contatos` · `GET …/contatos` | `ContatoOrcamento` / `contatos_orcamento` | RN7 (contato não altera status) | D `Registrar_contato_nao_altera_status`; A `Contato_registrado_nao_altera_status` |
| **UC03 A3** — editar enquanto aberto | T16 (Editar) | `PUT /api/orcamentos/{id}` | `Orcamento`, `ItemOrcamento`, `HistoricoEvento` | RN6, RN4 | D `Alterar_quantidade_recalcula_e_registra_no_historico`, `Nao_aceita_segunda_decisao_nem_edicao_apos_decidido` |
| UC03 passos 3–4 — decisão Aprovado | T18, T22, T23, T24 | `POST /api/orcamentos/{id}/decisao` | `DecisaoOrcamento` / `decisoes_orcamento` | RN7, RN8; aprovação só dentro da validade | D `Decisao_exige_meio_de_contato_e_data_hora`, `Aprovacao_exige_aguardando_retorno`, `Aprovacao_apos_a_validade_e_rejeitada` |
| UC03 — erro na decisão | T19 | `POST …/decisao` → 400 `DECISAO_DADOS_OBRIGATORIOS` | `DecisaoOrcamento` | RN7 | D `Decisao_exige_meio_de_contato_e_data_hora` |
| **UC03 A1** — recusa | T20 | `POST …/decisao` (`Recusado`, motivo) · `GET /api/orcamentos/motivos-decisao` | `DecisaoOrcamento` | RN5 | D `Recusado_nao_gera_pedido`; A `Recusado_e_cancelado_nao_geram_pedido` |
| **UC03 A2** — expiração automática | T21 | — (rotina `ExpiracaoOrcamentosWorker`) | `Orcamento`, `DecisaoOrcamento` (automática, sem usuário) | RN5 | D `Expira_automaticamente_apenas_quando_aguardando_e_vencido`; A `Rotina_de_expiracao_expira_somente_vencidos` |
| RF 4.1.3.4 — cancelar orçamento | T15 ("Cancelar orçamento"), T18 | `POST …/decisao` (`Cancelado`) | `DecisaoOrcamento` | RN10 | D `Cancelado_pode_ocorrer_em_elaboracao_e_nao_gera_pedido` |
| **UC04** passos 1–4 — converter em pedido | T22, T23, T25 | `POST /api/orcamentos/{id}/decisao` (Aprovado) → `pedidoGerado` | `Pedido`, `ItemPedido`, `ArquivoAnexo` (cópia com `anexo_origem_id`) / `pedidos`, `itens_pedido`; sequência `seq_pedido_numero` | RN8, RN9; RNF 5.2.1 (transação) | D `Pedido_herda_cliente_itens_personalizacao_valores_e_vinculo`, `Prazo_de_entrega_conta_dias_uteis_a_partir_da_aprovacao`, `Geracao_do_pedido_registra_historico_nos_dois_lados`; A `Aprovacao_gera_pedido_na_mesma_transacao`; I `Aprovacao_persiste_orcamento_pedido_itens_anexos_e_historico` |
| UC04 — falha na conversão | T19 (padrão de erro) | `POST …/decisao` → erro sem efeitos | transação única | RNF 5.2.1 | A `Falha_ao_gerar_pedido_nao_confirma_a_transacao`; I `Falha_na_geracao_do_pedido_desfaz_a_aprovacao` |
| RN9 — criação direta bloqueada | T26 (aviso "não há criação manual") · `/pedidos/novo` | `POST /api/pedidos` → 422 `PEDIDO_CRIACAO_DIRETA_NAO_PERMITIDA` | `Pedido` (fábrica única `CriarAPartirDe`) | RN9 | A `Pedido_nao_pode_ser_criado_diretamente` |
| Acompanhar pedido | T25, T26 | `GET /api/pedidos` · `GET /api/pedidos/{id}` · `GET …/historico` · `PATCH /api/pedidos/{id}/status` | `Pedido`, `HistoricoEvento` | Uma etapa por vez; cancelamento com motivo (RN10) | D `Status_evolui_um_passo_por_vez`, `Cancelamento_exige_motivo` |
| **UC05** — histórico por cliente | T27–T32 | `GET /api/clientes/{id}/visao-geral` · `/orcamentos?de&ate&status` · `/pedidos?de&ate&status` · `/historico?tipo&de&ate` | `HistoricoEvento` / `historico_eventos`; `orcamentos`, `pedidos` | RF 4.1.2.7, RF 4.1.4.1 | I `Historico_do_cliente_filtra_por_tipo_e_pertence_ao_cliente` |
| **UC05 A1** — cliente sem histórico | T27 (estados vazios) | idem (listas vazias, 200) | — | — | — (cliente "Transportes Rota Litoral" no seed, sem orçamentos) |
| UC05 — observações internas | T30 | `GET/POST /api/clientes/{id}/observacoes` | `ObservacaoCliente` / `observacoes_cliente` | visível só para a equipe | A `Cadastra_cliente_e_observacao_inicial` |
| RF 4.1.4.2 — painel | T02 | `GET /api/dashboard` | consultas agregadas | — | — (validação visual pelo roteiro) |
| Concorrência (transversal) | T16, T25 (edição e decisão) | todas as alterações recebem `versao` → 409 `CONFLITO_CONCORRENCIA` | coluna `versao` em `clientes`, `orcamentos`, `pedidos` | RNF 5.2.1 | A `Versao_desatualizada_retorna_conflito` |

## Cobertura resumida

| Caso de uso | Telas | Testes automatizados |
|---|---|---|
| UC01 Cadastrar Cliente | T03–T09 | 7 D + 3 A |
| UC02 Criar Orçamento | T11–T15 | 5 D + 4 A |
| UC03 Enviar e Decidir Orçamento | T15–T24 | 10 D + 4 A |
| UC04 Converter Orçamento em Pedido | T22–T25 | 3 D + 2 A + 2 I |
| UC05 Consultar Histórico | T27–T32 | 1 I |
| Pedidos (status, cancelamento, bloqueio) | T25, T26 | 2 D + 1 A |

Cada teste foi contado no caso de uso que ele exercita principalmente, e o total fecha em 27 D + 14 A + 3 I = 44. As lacunas de teste automatizado são o login, o dashboard e as consultas do UC05. Elas estão listadas como próximo passo no relatório final.
