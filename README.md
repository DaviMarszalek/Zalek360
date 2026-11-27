# Zalek360 — MVP 1 (ciclo comercial)

Sistema comercial **interno** da Zalek Personalizados. Cobre o cadastro de clientes, a elaboração de orçamentos com itens e personalização, o acompanhamento do retorno do cliente e a geração automática do pedido quando o orçamento é aprovado.

O cliente **não acessa o sistema**: WhatsApp, ligação, e-mail ou conversa presencial acontecem fora dele. O Gestor ou Administrador registra no Zalek360 os contatos e a decisão comunicada pelo cliente.

| Serviço | Endereço |
|---|---|
| Frontend (Next.js) | http://localhost:3000 (acessível na rede) |
| API (ASP.NET Core) | http://127.0.0.1:5000 (somente este computador) |
| Swagger | http://localhost:5000/swagger — **somente em Development** (`dotnet run`, seção 2) |
| Health check | http://127.0.0.1:5000/health |

**Acessos de demonstração:** criados pelo seed somente quando `EXECUTAR_SEED=true` e `SENHA_USUARIOS_DEMO` estão definidos e o banco está vazio (desenvolvimento e revisão). Contas: `admin@zalekpersonalizados.com.br` (Administrador) e `gestao@zalekpersonalizados.com.br` (Gestor), com a senha de demonstração `Zalek@2026` informada em `SENHA_USUARIOS_DEMO`.
As contas históricas Maria Silva, Carlos Souza e Fernanda Lima (perfil Atendente) também são criadas pelo seed. Perfis legados são preservados nos dados, mas não autenticam no modelo de acesso atual.
Em bases já populadas pelo seed anterior, a conta Gestor existente continua com `gestor@zalekpersonalizados.com.br`; o seed não renomeia nem cria contas quando já há usuários. Para trocar a senha de qualquer conta existente sem tela, use `backend/tools/redefinir-senha` (senha lida sem eco, nunca por argumento). Em ambiente com dados reais, **nunca** deixe a senha de demonstração: veja `docs/11-atualizar-ambiente-principal.md`.

---

## 1. Executar com Docker (recomendado)

Pré-requisito: Docker com Compose v2.

**O `.env` é obrigatório.** O Compose não tem chave JWT nem senha de banco padrão: sem `JWT_CHAVE` e `POSTGRES_PASSWORD`, qualquer comando `docker compose` para com a mensagem `required variable ... is missing a value`. Gere os segredos sem exibi-los:

```powershell
Copy-Item .env.example .env
powershell -ExecutionPolicy Bypass -File infra/implantacao/preparar-segredos.ps1 -ArquivoEnv .env -GerarChaveJwt -GerarSenhaBanco   # volume novo
docker compose up --build
```

Para um ambiente que **já tem banco** (como o principal), não use `-GerarSenhaBanco`: siga `docs/11-atualizar-ambiente-principal.md`, que troca a senha com `ALTER ROLE` na ordem segura.

Na primeira subida, a API:
1. confere a configuração: fora de Development, recusa chave JWT com menos de 32 caracteres ou publicada no repositório e senha de banco com menos de 16 caracteres ou publicada (mensagem `Configuração insegura`, sem valores);
2. espera o PostgreSQL aceitar conexões;
3. aplica as migrations pendentes;
4. confere se todas as colunas do modelo existem no banco;
5. grava os dados de demonstração **somente** com `EXECUTAR_SEED=true` e `SENHA_USUARIOS_DEMO` definidos (padrão: desligado).

Quando o log mostrar `Now listening on: http://[::]:8080`, abra http://localhost:3000.

- **Dados persistentes:** os volumes `dados-postgres` (banco) e `anexos` (artes enviadas) sobrevivem a `docker compose down`.
- **Recomeçar do zero:** `docker compose down -v` apaga os volumes (e todos os dados).
- **Portas:** o frontend é publicado na rede; API e PostgreSQL só em `127.0.0.1`. Se 3000, 5000 ou 5432 estiverem em uso, altere `FRONTEND_PORTA_HOST`, `BACKEND_PORTA_HOST` ou `POSTGRES_PORTA_HOST` no `.env`.
- **Cookie seguro:** `COOKIE_SEGURA` vale `true` por padrão; o login só se mantém em HTTPS ou `http://localhost`. Para acesso por `http://IP` na rede local sem HTTPS, configure HTTPS (`infra/nginx/`) ou use `COOKIE_SEGURA=false` conscientemente.
- **Backup e restauração:** `infra/backup/backup.ps1` e `infra/backup/restaurar.ps1` (teste em banco temporário); rotina em `docs/12-backup-e-restauracao.md`.
- **Atualização do ambiente principal:** `docs/11-atualizar-ambiente-principal.md`.

### Ambiente isolado de revisão e testes

O ambiente de revisão nunca compartilha containers, portas, banco ou volumes com o projeto principal. Use sempre o projeto Compose `zalek360-revisao` e o arquivo local ignorado `.env.revisao`; não execute `down -v` sem autorização explícita, mesmo nesse projeto.

Na primeira utilização, copie o exemplo, gere os segredos sem exibi-los e informe a senha de demonstração em `SENHA_USUARIOS_DEMO` (o seed da revisão cria as contas do `AGENTS.md` em banco novo):

```powershell
Copy-Item .env.revisao.example .env.revisao
powershell -ExecutionPolicy Bypass -File infra/implantacao/preparar-segredos.ps1 -ArquivoEnv .env.revisao -GerarChaveJwt -GerarSenhaBanco
```

Com o banco de revisão já existente, troque a senha com `-ContainerBanco zalek360-revisao-db-1 -TrocarSenhaBanco` no lugar de `-GerarSenhaBanco`.

Suba o ambiente com:

```powershell
docker compose -p zalek360-revisao --env-file .env.revisao up --build -d --wait
```

O ambiente usa frontend `http://localhost:3100`, API `http://localhost:5100` e PostgreSQL `localhost:55432`. O banco de teste chama-se `zalek360_revisao`; seus dados ficam nos volumes próprios do projeto. Somente com autorização explícita, para remover o ambiente descartável e seus dados:

```powershell
docker compose -p zalek360-revisao --env-file .env.revisao down -v
```

Rode os testes de navegador apontando para esse ambiente:

```powershell
Set-Location frontend
$env:E2E_BASE_URL = "http://localhost:3100"
npm run e2e
```

Nunca use o nome do projeto `zalek360` para testes ou limpeza: ele identifica o ambiente principal nas portas 3000/5000/5432.

### Perfis e orçamentos

- **Administrador:** tudo o que o Gestor faz, mais os exclusivos: configurações (opções gerenciáveis, modelos de mensagem, dados da empresa e e-mail), usuários (criar, desativar e redefinir senha) e exclusão definitiva de produto nunca usado. Funcionalidades ainda não disponíveis seguem as fases de `docs/10-contexto-e-proximos-passos.md`.
- **Gestor:** operação diária completa: dashboard, cadastrar/editar clientes, criar/editar/exportar/apresentar orçamentos, registrar contato e decisão, alterar status/cancelar pedidos com motivo, materiais do pedido, produtos/composição, matérias-primas, compras, entradas e ajustes de estoque com motivo. A tela Produtos inclui cadastro/edição, valores, composição, duplicação e inativação. Ajustes detalhados de estoque permanecem nas fases previstas.
- **Uso recomendado:** entrar como Gestor no dia a dia e como Administrador somente para configurar.
- **Custo e margem:** visíveis na tela para Gestor e Administrador, nunca no PDF ou em mensagens ao cliente. Ocultar do Gestor é uma opção futura, somente se o usuário pedir quando houver funcionário.
- **Atendente** e **Operador** permanecem apenas por compatibilidade com dados históricos e não aparecem na interface.
- Contas de demonstração em banco novo: `admin@zalekpersonalizados.com.br` e `gestao@zalekpersonalizados.com.br`, senha `Zalek@2026`. Bancos já populados mantêm a conta legada `gestor@zalekpersonalizados.com.br`; o seed não renomeia nem substitui usuários existentes.
- O responsável de um orçamento é sempre o usuário autenticado. A condição de pagamento é opcional; quando não informada, é apresentada como **A combinar**.

## 2. Executar localmente (sem Docker)

Pré-requisitos: .NET SDK 8, Node.js 22 e PostgreSQL 16.

**Banco**

```bash
sudo -u postgres psql -c "CREATE USER zalek360 WITH PASSWORD 'zalek360' CREATEDB;"
sudo -u postgres psql -c "CREATE DATABASE zalek360 OWNER zalek360;"
```

**Backend** (porta 5000, ambiente Development)

```bash
cd backend
dotnet run --project src/Zalek360.Api
```

`appsettings.Development.json` já traz uma chave JWT de desenvolvimento e o cookie sem exigência de HTTPS.
A connection string padrão é `Host=localhost;Port=5432;Database=zalek360;Username=zalek360;Password=zalek360`.

**Frontend** (porta 3000)

```bash
cd frontend
cp .env.example .env.local      # API_INTERNAL_URL=http://localhost:5000
npm install
npm run dev
```

O navegador chama sempre `/api/*` no próprio frontend, e o Next.js encaminha para `API_INTERNAL_URL`. Assim o cookie de sessão fica no mesmo domínio e o navegador não precisa de CORS.

**Testes E2E** usam Playwright e o Microsoft Edge instalado (`channel: msedge`). Com o ambiente isolado já iniciado:

```powershell
cd frontend
$env:E2E_BASE_URL = "http://localhost:3100"
npm run e2e
```

O comando não inicia o Compose. Antes de executar os cenários, aguarda a API responder pelo proxy do frontend. A suíte cria e modifica dados de teste; use exclusivamente o ambiente descartável `zalek360-revisao` em `http://localhost:3100`.

## 3. Roteiro de demonstração (segue os protótipos)

Os dados de demonstração usam **datas relativas**: o "hoje" do protótipo (24/09/2026) corresponde ao dia em que o sistema é iniciado. Por isso validades, prazos e o indicador "vencem em 3 dias" do dashboard fazem sentido em qualquer data.

1. **Login** como Gestor (`gestao@zalekpersonalizados.com.br`). Use Administrador somente para configurar. O **Dashboard** mostra orçamentos por situação, os aguardando retorno e os pedidos em andamento.
2. **Clientes → pesquisar "Academia Movimento"**: nenhum resultado. O cliente é propositalmente ausente do seed, para demonstrar o fluxo "pesquise antes de cadastrar".
3. **Novo cliente (PJ) com CNPJ `38.221.907/0001-53`.**
   - Salve sem preencher os obrigatórios para ver a validação campo a campo.
   - Troque para `12.345.678/0001-95` para ver o bloqueio de CNPJ duplicado. Esse CNPJ é da Metalúrgica Horizonte, e a tela oferece o link para o cadastro existente.
   - Volte ao CNPJ da Academia e salve.
4. **Novo orçamento** para a Academia.
   - Tente salvar sem itens: o sistema bloqueia (RN3).
   - Adicione "Camiseta Dry Fit", 100 un., R$ 32,00 + R$ 10,00 de personalização, "Serigrafia (frente) + DTF (costas)", e anexe as artes. Os totais são calculados pela API.
   - Salve: nasce o **#000124 — Em Elaboração**.
5. **Apresentar ao cliente**: o orçamento passa a Aguardando Retorno.
   - Revise destinatário, mensagem e PDF em **Apresentar ao cliente**. E-mail direto exige SMTP configurado; no fluxo externo de WhatsApp/e-mail, baixe o PDF, envie-o manualmente e confirme a apresentação. Também é possível registrar por outro meio.
6. **Registrar contato** (WhatsApp) e depois ajuste a quantidade para 80. O contato **não** altera o status, e as duas ações entram no histórico.
7. **Registrar decisão → Aprovado**, informando o meio de contato e a data/hora. Depois confirme **"Aprovar orçamento e gerar pedido?"**.
   - Na mesma transação, o orçamento vira Aprovado e o **pedido #000057 — Aberto** é criado.
   - O pedido herda itens, personalização, artes, valores e condições. O prazo de entrega é contado em dias úteis a partir da aprovação.
8. **Pedidos → #000057**: na Fase 2.0, configure fichas e saldo em **Estoque** antes de iniciar produção; ausência de ficha ou saldo mantém a aprovação e gera pendência. Pedidos legados com controle_materiais=false conservam o fluxo manual. No detalhe: faixa de origem com link para o orçamento, **Atualizar status** (Aberto → Em produção → Pronto → Entregue, uma etapa por vez) e **Cancelar pedido** (com motivo obrigatório).
9. **Cliente Metalúrgica Horizonte**: veja as abas Visão geral, Orçamentos, Pedidos, Observações e Histórico (6 orçamentos e 3 pedidos, como no protótipo). Filtre Orçamentos por período e por "Aprovado".
10. Outros exemplos:
    - #000116 **Expirado**: expiração automática, sem ação do Gestor;
    - um orçamento **Recusado**, com motivo;
    - `/pedidos/novo`: a criação manual é bloqueada. `POST /api/pedidos` retorna 422 `PEDIDO_CRIACAO_DIRETA_NAO_PERMITIDA`.

> **CPF/CNPJ do protótipo:** vários documentos das telas têm dígito verificador inválido. Por exemplo, o protótipo mostra `38.221.907/0001-64` para a Academia, mas o DV correto é `-53`. Como o sistema valida o DV (RN2), o seed usa os mesmos números-base com o DV corrigido. A tabela completa está em [`docs/03-alteracoes-e-complementos-na-modelagem.md`](docs/03-alteracoes-e-complementos-na-modelagem.md).

## 4. Arquitetura

```
backend/
  src/Zalek360.Domain          Entidades, regras (RN1–RN10), cálculo, calendário de dias úteis — sem dependências
  src/Zalek360.Application     Casos de uso (serviços), DTOs, interfaces de repositório/consulta, validação
  src/Zalek360.Infrastructure  EF Core + Npgsql, migration, seed, consultas, JWT, BCrypt, armazenamento de anexos, rotinas
  src/Zalek360.Api             Controllers REST, autenticação (cookie httpOnly + Bearer), erros padronizados, Swagger
  tests/Zalek360.Tests         xUnit: domínio, casos de uso (fakes) e integração (EF + SQLite em memória)
  scripts/                     testar.sh, recriar-migration-inicial.sh
  tools/verificacao-offline/   Verificação sem NuGet (ver seção 7)
frontend/
  app/                         Rotas (App Router): login, dashboard, clientes, orcamentos, pedidos
  features/                    Telas por módulo (auth, dashboard, clientes, orcamentos, pedidos, comum)
  components/ui, layout        Componentes de interface e casca (menu lateral, cabeçalho)
  services/, hooks/, lib/      Axios centralizado, TanStack Query, tratamento de erro, status
  types/, utils/               Tipos da API e formatação pt-BR (moeda, datas, CPF/CNPJ, telefone)
docs/                          Análise, rastreabilidade, modelagem, relatório, checklists, insumos para diagramas
infra/nginx/                   Exemplo de proxy reverso com HTTPS
```

**Decisões centrais:**
- **Cálculo sempre no backend.** A tela de orçamento mostra os totais retornados por `POST /api/orcamentos/calculo`, e o valor gravado é recalculado no servidor.
- **Pedido só nasce de orçamento aprovado.** `Pedido.CriarAPartirDe` é o único ponto de criação. A aprovação e a geração do pedido ocorrem numa transação: se a geração falhar, nada é gravado e o orçamento continua Aguardando Retorno.
- **Histórico unificado** (`historico_eventos`) por cliente, orçamento e pedido, gravado na mesma transação da alteração.
- **Concorrência otimista** (`versao`): editar um registro alterado por outra pessoa retorna 409 com mensagem clara.
- **Erros no formato** `{ "status", "code", "message", "errors" }`, com mensagens em português e `errors` por campo.

## 5. API

Documentação interativa em `/swagger`, **somente com `ASPNETCORE_ENVIRONMENT=Development`** (execução local, seção 2); nos containers ela não é publicada. Para autenticar lá, chame `POST /api/auth/login` (o cookie é definido automaticamente) ou cole o `accessToken` retornado em **Authorize**.

| Recurso | Endpoints |
|---|---|
| Autenticação | `POST /api/auth/login` (5 falhas seguidas na mesma conta → 429 por 15 min) · `POST /api/auth/logout` · `GET /api/auth/me` |
| Clientes | `GET/POST /api/clientes` · `GET /api/clientes/cidades` · `GET /api/clientes/verificar-documento` · `GET/PUT /api/clientes/{id}` · `GET /api/clientes/{id}/visao-geral` · `/orcamentos` · `/pedidos` · `/historico` · `GET/POST /api/clientes/{id}/observacoes` |
| Orçamentos | `GET/POST /api/orcamentos` · `POST /api/orcamentos/calculo` · `GET/PUT /api/orcamentos/{id}` · `/itens` · `/historico?categoria=` · `GET/POST /api/orcamentos/{id}/contatos` · `POST /api/orcamentos/{id}/aguardando-retorno` · `POST /api/orcamentos/{id}/decisao` · `GET /api/orcamentos/motivos-decisao` |
| PDF do orçamento | `POST /api/orcamentos/{id}/exportacoes?inline=true` exporta e registra histórico · `GET /api/orcamentos/{id}/exportar-pdf?inline=true` pré-visualiza sem gravar. Omitir `inline` baixa o arquivo |
| Pedidos | `GET /api/pedidos` · `GET /api/pedidos/{id}` · `/historico` · `PATCH /api/pedidos/{id}/status` · `POST /api/pedidos` → **sempre 422** |
| Anexos | `POST /api/anexos` (multipart `arquivo`; PDF, PNG, JPG, AI, CDR; até 20 MB) · `GET /api/anexos/{id}/download?inline=` · `DELETE /api/anexos/{id}` (só anexos ainda não vinculados) |
| Estoque | GET/POST /api/estoque/materias-primas; PUT …/materias-primas/{id}; PATCH …/materias-primas/{id}/status; GET …/materias-primas/{id}/movimentos; GET /api/estoque/fichas-tecnicas; PUT …/fichas-tecnicas/{produtoId}; GET/POST /api/estoque/compras; PATCH …/compras/{id}/situacao; GET /api/estoque/pendencias |
| Materiais do pedido | GET/PUT /api/pedidos/{id}/materiais; POST …/materiais/ficha-tecnica |
| Apoio | `GET /api/dashboard` · `GET /api/produtos` · `GET /api/produtos/opcoes` · `GET /api/usuarios/responsaveis` · `GET/POST /api/usuarios` (Administrador; POST cria conta Gestor) · `GET /health` |

Não existe exclusão de orçamentos, pedidos ou clientes (RN10): apenas cancelamento, com o histórico preservado.

### Catálogo de produtos e opções — Fase 2

Abra **Produtos** para pesquisar, cadastrar, editar, duplicar, inativar ou reativar um produto. O formulário reúne descrição, categoria, preço de venda, custo e matérias-primas por unidade. Preço e custo são opcionais; vazios nos produtos antigos significam não informados. Editar o catálogo não altera valores de orçamentos/pedidos existentes.

Gestor e Administrador operam o catálogo. Somente Administrador pode excluir definitivamente um produto nunca usado; produtos usados só podem ser inativados. As cópias incluem valores/composição e exigem novo nome, sem vínculos históricos.

**Configurações**, exclusiva do Administrador, gerencia categorias de produtos, tipos de personalização, condições de pagamento e motivos de decisão. Desativar uma opção impede novas seleções e preserva os textos históricos. Categorias existentes são incorporadas pela migration; as opções anteriores são semeadas uma vez no banco. Nenhuma lista de negócio fica fixa no frontend.

API do catálogo: `GET /api/produtos/catalogo?busca=...&incluirInativos=true`, `GET/PUT /api/produtos/{id}`, `POST /api/produtos`, `POST /api/produtos/{id}/duplicar`, `PATCH /api/produtos/{id}/status` e `DELETE /api/produtos/{id}?versao=...` (Administrador). Edição/status/exclusão verificam versão. `GET /api/catalogo/opcoes` oferece opções ativas à operação; `GET/POST /api/configuracoes/opcoes` e `PUT /api/configuracoes/opcoes/{id}` são administrativos.

### Orçamento — Fase 3

**Valores:** o campo aceita vírgula ou ponto decimal ("40,50" ou "40.50"); valores com mais de 2 casas são arredondados para 2 casas (ponto médio afastando de zero) no backend, igual na prévia, na gravação e no produto. O PDF remove emoji e troca por `?` caracteres sem glifo nas fontes (o cadastro não muda).

Ao selecionar um produto, o orçamento aplica seu preço de venda. É possível alterar o valor; um aviso mostra a diferença e **Restaurar preço cadastrado** reaplica a sugestão. Produto sem preço exige valor manual. Abrir uma edição preserva os preços registrados.

**Novo produto neste item** abre o mesmo formulário de catálogo, incluindo custo e composição. Ao salvar, seleciona o produto sem perder cliente, outros itens, desconto, personalização ou anexos; descrição e preço já preenchidos no item são mantidos.

A **Análise interna** mostra custo e margem para Gestor e Administrador. Margem estimada = total com personalização, após desconto, menos custo dos produtos; percentual sobre o total da venda. Não inclui despesas adicionais. Custo ausente não vira zero: a margem fica não calculada. O custo conhecido é registrado no item ao incluir/trocar o produto; mudanças posteriores no catálogo não reescrevem esse histórico. Orçamentos antigos continuam sem custo informado.

Cada item mostra a necessidade de matérias-primas, o saldo disponível e a falta, distribuindo saldo na ordem dos itens e considerando reservas do próprio pedido. É uma consulta da posição atual, sem reservar materiais ou bloquear orçamento por falta. As referências de imagem têm miniaturas no formulário, no detalhe e no pedido, com acesso autenticado; outros formatos mantêm links.

Análise interna usa `POST /api/orcamentos/analise-interna` (prévia) e `GET /api/orcamentos/{id}/analise-interna` (registrado), ambos sob `Operacao`. Custo e margem não integram o DTO da proposta nem o PDF ou mensagens ao cliente. O envio da proposta está descrito na Fase 4 abaixo.

### Envio da proposta — Fase 4

Gestor e Administrador usam **Apresentar ao cliente** (ou **Enviar proposta** quando já aguarda retorno). Escolhem e-mail/WhatsApp, conferem o destinatário e revisam a mensagem. **Registrar por outro meio** preserva a apresentação por telefone/presencial.

- **E-mail com SMTP:** **Enviar e-mail com PDF** anexa automaticamente o PDF da proposta. A aceitação pelo servidor registra contato/exportação e passa a Aguardando Retorno. Aceitação SMTP não confirma entrega ou leitura pelo destinatário.
- **Sem SMTP:** baixar o PDF, abrir o aplicativo de e-mail por `mailto` e anexar o arquivo manualmente. Abrir o aplicativo/copiar texto/baixar PDF não registra apresentação. Marcar “Já enviei...” e **Confirmar apresentação** somente depois do envio.
- **WhatsApp:** copiar o texto ou abrir `wa.me`, anexar o PDF manualmente e confirmar depois de enviar. Nenhum link público do PDF é criado.
- O PDF preparado para apresentação não leva o selo de rascunho, sem alterar o status por esta preparação. Custos e margens continuam fora do documento e dos modelos.

**Configurações → Mensagens de proposta**, exclusiva do Administrador, edita os modelos. Campos disponíveis: `{cliente}`, `{numero}`, `{valorTotal}`, `{validade}`, `{prazo}` e `{empresa}`. A API rejeita campos desconhecidos, incluindo custo/margem. Modelos não alteram mensagens já enviadas.

SMTP usa [MailKit 5.0.0](https://www.nuget.org/packages/MailKit/5.0.0), com configuração `Email__Host`, `Email__Porta` (587 por padrão), `Email__Seguranca` (`StartTls` ou `SslOnConnect` para 465), `Email__Usuario`, `Email__Senha`, `Email__Remetente` e `Email__NomeRemetente`. Host/remetente ausentes ou configuração incompleta desabilitam envio direto; a operação continua pelo aplicativo de e-mail. Segredos ficam somente no ambiente local/seguro; não são retornados à interface. `None` é destinado ao servidor isolado de testes.

Cada tentativa tem uma chave persistida antes de chamar SMTP. Repetir uma chave confirmada retorna o recibo sem reenviar. Falhas/queda do processo/confirmação incerta não geram reenvio automático: consultar o resultado e conferir a caixa de enviados antes de preparar outra tentativa. Banco e SMTP não formam uma transação única; uma queda após a aceitação pode deixar a tentativa sem confirmação. O orçamento é bloqueado durante o envio; uma segunda tentativa para a mesma versão recebe conflito.

API operacional: `GET /api/orcamentos/{id}/proposta`, `POST .../proposta/email`, `GET .../proposta/email/{chave}`, `POST .../proposta/pdf` e `POST .../proposta/confirmacao`. Modelos usam `GET /api/configuracoes/modelos-proposta` e `PUT /api/configuracoes/modelos-proposta/{id}`, sob `Administracao`.

Para capturar e-mails sem conta externa, use somente a revisão:

```powershell
docker compose -p zalek360-revisao --env-file .env.revisao -f docker-compose.yml -f docker-compose.email-teste.yml --profile teste-email up --build -d --wait
```

O [smtp4dev](https://github.com/rnwood/smtp4dev) fica em `http://localhost:5180`, acessível apenas localmente, sem relay configurado; SMTP é interno à rede da revisão. Os E2E usam destinatários `example.test`; o override fixa remetente `propostas@example.test`, autenticação vazia e conexão ao capturador. Não use o override no ambiente principal. A validação padrão continua usando o comando de revisão do `AGENTS.md`; a captura SMTP é uma verificação adicional.

### Estoque mínimo e vendas — Fase 5

O dashboard mostra **Repor estoque** e **Vendas entregues no mês** (quantidade e valor dos pedidos pela data de entrega, no fuso da empresa). O cartão de vendas abre os mesmos limites de mês nos filtros de pedidos. **Orçamentos em aberto** agrupa os atalhos de elaboração e aguardando retorno; as contagens continuam separadas para orientar a próxima ação.

Em **Estoque**, o filtro oferece todas as matérias-primas, **Abaixo do mínimo** e **Repor estoque**. Abaixo do mínimo significa saldo livre estritamente menor que o mínimo; materiais inativos ficam fora das duas visões de reposição. A sugestão é calculada na API:

- **Reposição sugerida:** `máximo(0, mínimo − saldo livre + pendências de reservas ativas)`.
- **Falta comprar:** `máximo(0, reposição sugerida − compras previstas a receber)`.

Compras planejadas/pedidas aparecem separadamente em **A receber**. Compras canceladas/recebidas não são descontadas novamente; recebimento já aumenta o saldo físico. Reservas liberadas/consumidas não compõem pendências. Pedidos sem ficha aparecem em **Pendências de materiais**; não se inventa quantidade de material para eles.

**Planejar compra** no item preenche a falta líquida, editável antes de salvar. Na visão de reposição, a ação fica desabilitada quando as compras previstas já cobrem a necessidade. A lista não cria compras automaticamente.

**Pedidos** tem busca/seletor de cliente, status, responsável e período predefinido ou personalizado. A referência pode ser **Data do pedido** ou **Data da entrega**; esta última inclui somente pedidos entregues. As datas inicial/final são inclusivas e a API converte as datas de entrega pelo fuso da empresa. Período invertido é rejeitado.

API: `GET /api/estoque/reposicao`, `GET /api/estoque/materias-primas?abaixoDoMinimo=true` e `GET /api/pedidos?clienteId=...&dataReferencia=Entrega&de=AAAA-MM-DD&ate=AAAA-MM-DD`, todos sob `Operacao`. Esta fase altera consultas/contratos e interface, sem migration ou mudança de esquema.

### Cenário completo e acabamento — Fase 6

O cenário Homem-Aranha percorre cadastro de cliente/materiais, produto com composição dentro do orçamento, referência, proposta, aprovação com falta, compra, recebimento, produção, entrega e cancelamento de outra reserva. A baixa ocorre uma vez ao iniciar produção; após receber material, recarregue a ficha no pedido para completar a reserva.

Modais e menu móvel oferecem foco inicial, contenção, Escape e retorno; abas aceitam setas, Home e End. Falhas de consultas mostram nova tentativa, e os formulários mantêm os dados quando a gravação falha. A decisão de usar o fluxo simples no lugar de OS por etapas está em [docs/08-mvp2.md](docs/08-mvp2.md). Pendências sem fase definida estão em [docs/10-contexto-e-proximos-passos.md](docs/10-contexto-e-proximos-passos.md).

## 6. Testes

```bash
cd backend && ./scripts/testar.sh      # equivalente a: dotnet test Zalek360.sln
```

Na validação da Fase 6, **139 testes backend passaram, com 0 falhas e 0 ignorados** em PostgreSQL real; Vitest **8/8** e Edge **24/24** na suíte completa. A captura SMTP adicional foi repetida na Fase 6 (**3/3**); conta/provedor real não foram verificados. `npm ci`, typecheck, build e Compose foram aprovados. Os detalhes estão em [docs/09-revisao-funcional.md](docs/09-revisao-funcional.md). No Windows, execute os testes em container .NET 8 com PostgreSQL real conforme [AGENTS.md](AGENTS.md); não aceite testes ignorados. A cobertura original abaixo é histórica, complementada por autenticação, contrato HTTP/PDF, estoque, catálogo, permissões e upgrade de banco.

- **27 de domínio:** cliente, CPF/CNPJ, cálculo, ciclo do orçamento, expiração e pedido.
- **14 de casos de uso**, com repositórios em memória:
  - duplicidade de documento;
  - numeração sem consumo em caso de erro;
  - contato sem mudança de status;
  - aprovação e geração do pedido na mesma transação;
  - não confirmação da transação quando a geração do pedido falha;
  - conflito de versão;
  - bloqueio de pedido direto;
  - rotina de expiração.
- **3 de integração com EF Core + SQLite em memória:**
  - a aprovação persiste pedido, itens, anexos copiados e histórico;
  - uma falha na numeração do pedido desfaz a aprovação no banco (rollback real);
  - o histórico do cliente filtra por tipo.

### Suíte Selenium (Python + pytest)

A pasta [testes-selenium/](testes-selenium/README.md) tem uma suíte independente de testes de interface: login, bloqueio por tentativas, controle de acesso por perfil e cadastro de clientes. Ela roda contra o ambiente de revisão (porta 3100) e gera um relatório HTML. A suíte não substitui o E2E Playwright exigido em cada entrega.

## 7. O que foi verificado nesta entrega (transparência)

**Registro histórico da entrega inicial.** A verificação atual da exportação, realizada em 28/10/2026 com NuGet e Docker, está na seção 10. Os stubs offline originais não cobrem QuestPDF nem WebApplicationFactory e não validam a entrega atual.

O ambiente em que o projeto foi construído **não tinha acesso ao NuGet** (HTTP 403) **nem ao Docker**. Por isso:

| Verificação | Resultado |
|---|---|
| Domain e Application compilados com o SDK .NET 8 | 0 erros, 0 avisos |
| Infrastructure, Api e Tests compilados contra *stubs* de assinatura (`backend/tools/verificacao-offline/verificar.sh`) | 0 erros, 0 avisos nos 5 projetos |
| Testes de domínio e de casos de uso executados | **41 de 41 aprovados** |
| Testes de integração (EF + SQLite) | Compilados; **não executados** (precisam do pacote EF Sqlite) |
| Migration traduzida para SQL e executada em PostgreSQL 16 | 12 tabelas, 47 índices, 22 FKs, 7 checks; sequências e unicidade conferidas |
| Entidades × mapeamento EF × migration (script de conferência) | 0 divergências em 12 tabelas |
| Frontend: `tsc --noEmit` e `next build` (Next 16, output standalone) | Sem erros; 15 rotas; `proxy.ts` reconhecido |
| Imagem do frontend simulada etapa por etapa (`npm ci` limpo, build com `API_INTERNAL_URL=http://backend:8080`, execução do standalone) contra um backend simulado | Cookie de sessão repassado; erros 422 preservados; upload de 21 MB íntegro |
| `docker compose up --build` completo | **Não executado aqui** (sem Docker) |
| API real rodando com EF Core/Npgsql contra PostgreSQL | **Não executada aqui** (sem NuGet) |

**Primeira ação recomendada numa máquina com internet:**

```bash
cd backend && ./scripts/testar.sh
docker compose up --build
```

Depois, siga o roteiro da seção 3. Os stubs verificam tipos e assinaturas, mas não o comportamento do EF Core em execução. Por isso fiz também uma auditoria manual, descrita no relatório final:
- datas sempre em UTC;
- consultas traduzíveis para SQL;
- construtores e campos exigidos pelo EF;
- chaves Guid geradas no domínio.

## 8. Migrations

As cinco migrations publicadas foram preservadas. A linha atual acrescenta 20261112120000_AlinharEsquemaComModelo (manual aprovada) e 20261113031917_EstabilizarReservasMateriais (gerada por EF e revisada com backfill). O ZalekDbContextModelSnapshot foi adotado sem recriar a migration inicial.

Após a Fase 4, o banco tem 21 tabelas de negócio, 235 colunas e duas sequências. A migration `20261125001901_ModelosEEnviosProposta` cria modelos com sementes de e-mail/WhatsApp e recibos de tentativas, sem alterar tabelas/dados comerciais existentes. Na Fase 3, havia 19 tabelas e 222 colunas. A migration `20261124231443_CustoInternoNoItemOrcamento`, gerada via EF, adiciona somente `itens_orcamento.custo_unitario_interno` opcional, sem preencher custos antigos. Na Fase 2 de catálogo, havia 221 colunas. A migration `20261124220121_CatalogoProdutosEOpcoes`, gerada por EF com o snapshot existente, adiciona preço/custo opcionais, versão do produto e opções gerenciáveis; os produtos antigos permanecem com valores não informados e suas categorias são incorporadas sem reescrever o histórico. A API aplica migrations e verifica a presença das colunas; o comparador verifica também tipos, defaults, PKs, FKs, índices e checks.

Antes de qualquer migration futura:

~~~powershell
powershell -ExecutionPolicy Bypass -File backend/tools/comparar-esquema/comparar-revisao.ps1
~~~

O script usa somente o PostgreSQL do projeto `zalek360-revisao`, cria dois bancos temporários para migrations/modelo, consulta os catálogos em READ ONLY e remove apenas esses bancos. Não para serviços nem remove volumes. Colunas são comparadas como conjunto; a ordem física é ignorada por decisão aprovada. Não regenerar migrations publicadas nem usar recriar-migration-inicial.sh nesta linha de evolução.


## 9. Documentação

| Documento | Conteúdo |
|---|---|
| [`docs/01-analise-inicial.md`](docs/01-analise-inicial.md) | Leitura dos requisitos, prioridades e conflitos entre fontes |
| [`docs/02-matriz-rastreabilidade.md`](docs/02-matriz-rastreabilidade.md) | UC → Tela → Endpoint → Entidade → Regra → Teste |
| [`docs/03-alteracoes-e-complementos-na-modelagem.md`](docs/03-alteracoes-e-complementos-na-modelagem.md) | Estrutura original, alteração, motivo, impacto e tabelas |
| [`docs/04-relatorio-final.md`](docs/04-relatorio-final.md) | Relatório final em 17 itens |
| [`docs/05-checklists.md`](docs/05-checklists.md) | Checklists de casos de uso, tecnologia e telas |
| [`docs/06-revisao-da-modelagem.md`](docs/06-revisao-da-modelagem.md) | MANTIDO / ALTERADO / ADICIONADO / REMOVIDO |
| [`docs/07-insumos-diagramas-der-dicionario.md`](docs/07-insumos-diagramas-der-dicionario.md) | O que atualizar nos diagramas, no DER e no dicionário de dados |
| [`docs/08-mvp2.md`](docs/08-mvp2.md) | Fase 2.0: decisões, regras, dicionário físico, provas e limites |

## 10. Exportar orçamento em PDF

Abra **Orçamentos → detalhe → Exportar PDF** para baixar a **Proposta Comercial**. **Abrir em nova aba** solicita o mesmo documento com `inline=true`; ambos registram “Orçamento exportado em PDF”, com usuário e data/hora, no histórico do orçamento e do cliente. O modal após **Apresentar ao cliente** oferece o atalho de exportação. O sistema permanece interno: envie o arquivo ao cliente fora dele.

O PDF A4 inclui identificação do cliente, responsável, três destaques (total, validade e prazo), itens e detalhes de personalização, nomes das artes, resumo financeiro, pagamento, instruções de aprovação e paginação. Os valores são os persistidos; o frontend não calcula o conteúdo. As artes são listadas por nome, sem miniaturas, para manter a exportação independente da leitura dos anexos.

O prazo informa dias úteis e semanas (arredondamento para cima), a previsão se aprovado na emissão e se aprovado no último dia da validade. O calendário é o mesmo de `Pedido.CriarAPartirDe`: segunda a sexta, sem feriados, com datas em `America/Sao_Paulo`. Um aprovado com pedido mostra o número e a data real do pedido. Rascunhos têm marca no cabeçalho e rodapé; recusados, expirados, cancelados e propostas vencidas não apresentam previsões hipotéticas nem instruções para aprovar.

**Configuração pública da empresa:** use a seção `Empresa` de `backend/src/Zalek360.Api/appsettings.json` ou as variáveis abaixo, presentes nos dois `.env.example` e no Compose. O padrão neutro é “Zalek Personalizados”; os demais dados ficam vazios e são omitidos. Nome obrigatório, limites de tamanho, CNPJ e e-mail são validados na inicialização.

| Variável | Conteúdo |
|---|---|
| `Empresa__NomeFantasia` | Nome exibido no cabeçalho |
| `Empresa__RazaoSocial` / `Empresa__Cnpj` | Identificação fiscal no rodapé |
| `Empresa__Telefone` / `Empresa__WhatsApp` / `Empresa__Email` | Contatos para aprovar e falar com a empresa |
| `Empresa__Site` / `Empresa__Endereco` | Site e endereço público |
| `Empresa__CaminhoLogo` | Arquivo local opcional, até 2 MB; caminho dentro do contêiner, ex.: `/app/data/empresa/logo.png` |
| `Empresa__TextoRodape` | Texto adicional opcional, até 500 caracteres |

O Compose monta o volume `identidade` em `/app/data/empresa` somente para leitura pela API. Disponibilize o logo nesse volume ou acrescente um bind mount de um arquivo próprio. Logo ausente, sem permissão ou inválido usa o nome da empresa em texto. Na execução local, variáveis precisam estar no ambiente do processo (o `dotnet run` não carrega `.env` automaticamente).

**Implementação e licença:** QuestPDF está fixado em `2026.9.1`, configurado com `LicenseType.Community`. Empresas que excedam o limite da Community precisam de licença paga; verifique a elegibilidade na [licença oficial QuestPDF](https://www.questpdf.com/license/community.html) antes do uso comercial. As fontes IBM Plex Sans Regular, Medium e Bold são recursos embutidos com licença OFL em `backend/src/Zalek360.Infrastructure/Documentos/Fontes/OFL.txt`, com fallback Lato incluído pelo QuestPDF. O registro usa `FontManager.RegisterFontFromStream`, nome atual de `RegisterFont` na versão adotada. A imagem Debian instala `libfontconfig1`.

**Verificação em 28/10/2026:** 65/65 testes reais aprovados via SDK .NET 8 em Docker; `npx tsc --noEmit` e `npm run build` aprovados; `docker compose -p zalek360-pdf-validacao up --build -d --wait` executado do zero em portas separadas, com PostgreSQL, API e frontend. O SDK .NET não está instalado no Windows; o comando equivalente executado foi:

```powershell
docker run --rm --mount "type=bind,source=${PWD}/backend,target=/src" --mount type=volume,source=zalek360-nuget-cache,target=/root/.nuget/packages -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test Zalek360.sln --nologo
```

Os testes de endpoint cobrem 401, 403, 404 padronizado, `application/pdf`, `Content-Disposition`, evento compartilhado com o cliente, GET sem gravação e falha de renderização sem evento. Os testes reais de PDF cobrem logo ausente/inválido e artes ausentes/ilegíveis. A verificação de navegador opcional está em `backend/tools/verificacao-pdf/verificar-interface.cjs`; execute somente contra uma instância de demonstração isolada, pois ela anexa uma arte e apresenta o orçamento #000123.

**Aceite no navegador e no leitor PDF:** login, download com nome do servidor, nova aba, erro JSON recebido como blob, atalho após apresentação e histórico do cliente verificados no Chrome headless contra o Compose isolado. Os PDFs das seis situações foram lidos com PyMuPDF: proposta de três itens com arte em **2 páginas**, total **R$ 12.840,00** igual ao resumo da API, fontes IBM Plex incorporadas, paginação correta e ausência dos textos internos do histórico. Um orçamento de 12 itens/5 páginas confirmou a repetição do cabeçalho e blocos de item inteiros. PDFs e imagens de revisão ficam em `data/validacao-pdf/` (artefatos locais ignorados pelo Git). A inspeção independente pode ser repetida com `python backend/tools/verificacao-pdf/inspecionar_pdf.py data/validacao-pdf` após instalar `pymupdf`.

Após o aceite, `docker compose up --build -d --wait` também atualizou a instância principal de desenvolvimento (portas 3000/5000), preservando os volumes existentes. Login e GET PDF pelo proxy em `http://localhost:3000` retornaram 200, `application/pdf`, `Content-Disposition: inline` com nome saneado e bytes `%PDF`.

## 11. MVP 2 — Fase 2.0

Estoque do Luiz estabilizado: reserva limitada ao saldo livre, pendências por saldo/ficha, bloqueio concorrente por material, baixa transitória somente da reserva do próprio pedido e liberação no cancelamento. A aprovação nunca é bloqueada por saldo ou ficha. A fila em /estoque e o bloco Materiais do pedido mostram as pendências.

pedidos.controle_materiais identifica o legado explicitamente: existentes sem reserva ficam false; existentes com reservas ficam true; novas aprovações nascem true. Aplicar ficha em legado Aberto ativa o controle e registra histórico.

**Verificado em 13/11/2026:** 88/88 testes reais (zero ignorados, PostgreSQL configurado), TypeScript, build, Compose completo do zero, sete migrations × modelo sem divergências funcionais, upgrade com contagens/hashes preservados e roteiro manual HTTP de saldo suficiente/insuficiente, ficha ausente, início e cancelamento. Navegação visual desta fase: NÃO EXECUTADA.

Dicionário físico, decisões D1–D9, RN 11–28, situação e limites estão em [docs/08-mvp2.md](docs/08-mvp2.md). Complemento automático, perfis específicos e seed de estoque são 2.1; OS/consumo real são 2.2. As fases seguintes não foram implementadas.
