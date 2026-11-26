# Relatório de revisão do MVP — Zalek360

- **Commit revisado:** `5faa20ec84283008ed47eaf52c58d3aa39df9c98` (merge do PR #9), na branch `main`, limpo e igual a `origin/main`.
- **Data:** 25/11/2026.
- **Modo:** somente revisão. Nenhum código, teste, migration, configuração ou documento do repositório foi alterado.
- **Evidências:** em `revisao-mvp/evidencias/`; logs em `revisao-mvp/logs/`; scripts em `revisao-mvp/scripts/`. As anotações feitas durante o trabalho estão em `revisao-mvp/NOTAS-PARCIAIS.md`.

---

## 1. Resumo executivo

MVP sólido para o porte. Backend 139/139 sem ignorados; autorização 35/35; comparador de esquema sem divergências; fluxos BDD conferidos ao vivo. O que impede o uso real é a configuração de implantação, não o negócio.
**Prontidão: NÃO PRONTO hoje.** Fica **PRONTO COM RESSALVAS** após a fase "Agora" do roadmap (1 crítico + 3 altos; cerca de 2 a 4 dias).

**Riscos principais:**
1. C1 (Crítico): chave JWT padrão versionada no compose permite forjar token de Administrador (leitura e escrita provadas na revisão).
2. A1 (Alto): padrões de produção inseguros no compose (senha do banco, porta 5432 exposta, seed com senha conhecida, Swagger, cookie sem `Secure`).
3. A2 (Alto): sem backup nem restauração documentados ou testados (banco e anexos em volumes separados).
4. A3 (Alto): emoji ou glifo fora da fonte faz o PDF retornar 500 e trava proposta e e-mail daquele cliente.
5. M1/M2 (Médio): "40.50" vira 4050 na tela; unitário com 3+ casas grava R$ 0,00 ou gera 500.

**Forças principais:**
1. Autorização na própria API; token adulterado, expirado e `alg=none` → 401; perfis legados não autenticam.
2. Estoque transacional (`FOR UPDATE`, reserva parcial, sem dupla reserva, baixa única, liberação no cancelamento).
3. Cálculo financeiro só no backend; custo e margem fora do PDF, da proposta e das mensagens.
4. Migrations nunca editadas, 0 divergências e histórico unificado.
5. Upload seguro e envelope de erro padronizado, sem stack trace.

---

## 2. Escopo e método

**Ambiente:** somente `zalek360-revisao` (frontend 3100, API 5100, PostgreSQL 55432), com Docker Desktop 29.8.1 e o Engine running.
- O projeto principal `zalek360` não foi iniciado, parado, consultado nem acessado.
- Divulgação: o primeiro `docker ps`, sem filtro, listou os containers do projeto principal. Foi só uma listagem, sem acesso.

**Executado, em sequência:**
1. Leitura obrigatória: `AGENTS.md`, `README.md` e `docs/08`, `09` e `10`.
2. Mapeamento estático do backend, frontend, modelo e testes. Duas partes foram feitas com subagentes de leitura; os achados citados aqui foram conferidos no código, como indicado em cada caso.
3. Varredura de segredos no histórico do git, registrando apenas commit e arquivo.
4. Consulta aos avisos de segurança do Next e do React, enviando só nomes de pacote e versões.
5. Snapshot de contagens e de identidade/versão de 2.334 linhas, para provar que os dados preexistentes não mudaram.
6. Subida padrão do ambiente: `docker compose -p zalek360-revisao --env-file .env.revisao up --build -d --wait`.
7. Testes backend em container .NET 8 com PostgreSQL real.
8. Frontend: `npm ci`, `typecheck`, Vitest, `build` e `e2e` (Edge).
9. Comparador de esquema (exceção aprovada ao item 3.4).
10. Matriz de autorização, exploratório da API (68 cenários) e da interface (Edge headless).
11. Carga smoke.
12. E2E com o capturador smtp4dev mais bordas de SMTP, seguidos da restauração do ambiente padrão.

**Origem das verificações:**
- **VERIFICADO** significa que eu executei, ou que li o trecho e conferi a linha com `grep -n`.
- Achados de desempenho e de arquitetura vindos do subagente sem conferência linha a linha aparecem como "(leitura por subagente)".
- O mapeamento teste → BDD da seção 5 foi conferido pelo **nome do método** (`grep -n`). O corpo do teste só foi lido onde indicado.

**Não executado:** ver a seção 11.

**Fontes externas consultadas:**
- github.com/vercel/next.js/security (páginas 1–2 e avisos individuais) e github.com/facebook/react/security (os links estão na seção 4, achado B8).
- `npm view next time` e `npm view react-server-dom-turbopack time` no registry público.
- Os documentos OWASP, ANPD, Microsoft Learn e Playwright citados no prompt **não foram lidos nesta revisão**. As referências a eles são apenas por nome de categoria, sem citação de conteúdo.

---

## 3. Pontos fortes (preservar)

| Tema | Evidência |
|---|---|
| Autorização na API, não no frontend | Todos os controllers têm `[Authorize(Policy=Operacao\|Administracao)]` na classe; o fallback exige autenticação (`ConfiguracaoServicos.cs:119-127`). Matriz ao vivo 35/35 (`evidencias/matriz-autorizacao.md`). O `proxy.ts` do Next só redireciona para o login. |
| Perfis legados fora da autenticação | `AuthService.cs:35,45,50-51`. Login da Atendente retorna 401 ao vivo. |
| Login sem enumeração de usuários | Hash fictício e mensagem única (`AuthService.cs:13,35-36`). BCrypt com custo 11 (`ServicosInfraestrutura.cs:34`). Senha mínima de 12 caracteres e máxima de 72 bytes (`UsuarioService.cs:14-15,54-59`). |
| Cookie de sessão | `HttpOnly`, `SameSite=Lax`, `Secure` configurável (`AuthController.cs:33-43`). O token não é usado pelo frontend nem guardado em storage. |
| Cálculo no backend | Totais vêm de `POST /orcamentos/calculo`; `Dinheiro.Arredondar` AwayFromZero é a única regra (`Domain/Common/Formatos.cs:27`). Nenhuma conta financeira no frontend. |
| Custo e margem internos | Ausentes de `OrcamentoDocumentoDto`, `PropostaDtos` e `GeradorDocumentoOrcamento.cs`. Os modelos de mensagem aceitam só uma lista fixa de campos (`PropostaService.cs:15,24-25,39-42`). Ao vivo: o detalhe e a proposta não trazem custo ou margem (D14, D16, U6b). |
| Estoque e concorrência | Bloqueio por `FOR UPDATE` parametrizado e ordenado (`Repositorios.cs:21-23,136`). Ao vivo: 60 disponíveis disputados por dois pedidos de 50 resultaram em 50 + 10 reservados; reserva parcial 50/30; uma única `SaidaPedido`; liberação apenas das reservas ativas. |
| Idempotência e concorrência otimista | Duplo clique em aprovar gerou 200/409 e um único pedido. Versão desatualizada retorna 409. Chave de envio de e-mail não reenvia (smtp4dev: 1 mensagem). |
| Migrations | 12 migrations com um único commit cada (nunca editadas). Comparador: 0 divergências (21 tabelas, 235 colunas, 71 constraints, 72 índices, 2 sequências). |
| Upload | Allowlist, magic bytes (`AnexoService.cs:38-42`), chave aleatória no servidor, sem SVG/HTML, `nosniff` e `filename*` codificado. Anexo pendente visível só para o autor. Ao vivo: H1–H6 conformes. |
| Erros | Envelope `{status, code, message, errors}` sem stack trace (`TratamentoErrosMiddleware.cs`). JSON malformado retorna 400 no envelope. |
| E-mail | Rejeita CRLF e múltiplos destinatários; Unicode chega intacto (S1–S6). Tentativa persistida antes do SMTP. |
| Mass assignment | Os requests não têm os campos sensíveis: `OrcamentoRequest` não tem situação nem totais, `ItemOrcamentoRequest` não tem custo (o custo vem do produto) e `CriarGestorRequest` não tem perfil (sempre Gestor). O `responsavelId` enviado é ignorado (D12 ao vivo). A situação do pedido só muda pela máquina de estados. Fonte: assinaturas em `Application/Dtos/*.cs`. |
| SQL | Sem SQL cru com entrada do usuário: tudo parametrizado ou constante. |
| Frontend | Sem `dangerouslySetInnerHTML`, `eval` ou token em storage; 0 `any` com `strict`. Sem rolagem horizontal em 360 e 1280 px nas 8 rotas. Modal com foco inicial, Tab preso, Escape e retorno do foco. |
| Containers | O backend roda como `app` e o frontend como `node` (não root). |
| Logs | Sem dados pessoais nos logs; parâmetros SQL mascarados (`'?'`). |
| Dependências | `npm ci`: 0 vulnerabilidades. Projetos .NET de produção sem pacotes vulneráveis. |

---

## 4. Achados

### 4.1 Tabela

Legenda: **V** = VERIFICADO; **I** = INFERÊNCIA. Esforço P/M/G. **Dec.** = depende de decisão sua. **Bloq.** = bloqueante para o uso real.

| ID | Título | Sev. | Categoria | V/I | Evidência | Esforço | Dec. | Bloq. |
|---|---|---|---|---|---|---|---|---|
| C1 | Chave JWT padrão versionada permite forjar token de Administrador | **Crítica** | segurança | V | `docker-compose.yml:34`; `DependencyInjection.cs:22`; `evidencias/S1-jwt-forjado.txt` | P | não | **sim** |
| A1 | Padrões inseguros de produção no compose | **Alta** | segurança/operação | V (leitura) | `docker-compose.yml:14,15-16,32,34,36,55-59` | P | não | **sim** |
| A2 | Sem backup e restauração documentados ou testados | **Alta** | dados/operação | I | ausência em README/docs/infra; volumes `dados-postgres` e `anexos` | M | sim | **sim** (com dados reais) |
| A3 | PDF retorna 500 com emoji ou glifo fora da fonte | **Alta** | negócio | V | log `DocumentDrawingException ... Missing glyphs: U+1F600`; `exploratorio-api.md` D15 | P/M | não | **sim** |
| M1 | "40.50" vira 4050 no orçamento | Média | negócio/produto | V | `utils/formatos.ts:103-108`; `ui/valor-com-ponto.png` | P | não | recomendado |
| M2 | Unitário com mais de 2 casas: gravado como R$ 0,00 ou erro 500 | Média | negócio | V | `Orcamento.cs:92-97`, `ItemOrcamento.cs:65,137`; D7 = 500, D8 = total 0 | P | não | recomendado |
| M3 | Sem limitação de tentativas no login | Média | segurança | V (leitura) | `Program.cs` sem `AddRateLimiter` | P | não | sim, se exposto à internet |
| M4 | Sessão não revogável: usuário desativado ou com perfil alterado segue operando até o token expirar (até 7 dias) | Média | segurança | V (leitura) | `ConfiguracaoServicos.cs:119-127`; `AuthService.cs:42-48`; `appsettings.json:9-10` | M | não | antes do ajudante |
| M5 | Open redirect após o login (`/\host`) | Média | segurança | V | `TelaLogin.tsx:31`; `exploratorio-ui.md` U1 | P | não | recomendado |
| M6 | Sem CI rodando testes nos PRs | Média | testes/operação | V | `.github/` inexistente | M | não | não |
| M7 | E2E instável em `proposta.spec.ts:100` (modal não fecha) | Média | testes | V | falhou na suíte E2E completa (Admin) e na execução SMTP de `proposta.spec.ts` (Gestor); passou nas 2 execuções isoladas | M | não | não |
| CS | CSRF: exposição baixa (SameSite=Lax + JSON obrigatório); 3 POSTs sem corpo dependem só do SameSite; 415 fora do envelope | Baixa | segurança | V | `evidencias/sonda-csrf-escrita.md`; `AuthController.cs:33-43` | P | não | não |
| M8 | Dashboard e listas vão degradar com o volume | Média | desempenho | V/I | `EstoqueService.cs:25-32` carrega todas as reservas; listas sem paginação; smoke: dashboard p95 329 ms | M | não | não |
| B1 | `?periodo=` inválido derruba `/pedidos` | Baixa | qualidade | V | `ListaPedidos.tsx:36`; `features/comum/index.tsx:23`; U3 | P | não | não |
| B2 | Enum fora da faixa retorna 500 | Baixa | qualidade | V | `Rotulos.cs:52`; D21 | P | não | não |
| B3 | Decisão manual "Expirado" é aceita | Baixa | negócio | V | `Orcamento.cs:317`; D26 | P | sim | não |
| B4 | Logs de segurança pobres (login falho sem conta/IP, 403 não registrado) | Baixa | segurança | V (leitura) | `TratamentoErrosMiddleware.cs:48-50`; `OnForbidden` sem log | P | não | não |
| B5 | Sem CSP, HSTS e Permissions-Policy no Next | Baixa | segurança | V (leitura) | `next.config.ts:23-25` | P | não | não |
| B6 | `ForwardedHeaders` confia em qualquer proxy | Baixa | segurança | V (leitura) | `Program.cs:20-25` | P | não | não |
| B7 | `historico_eventos` sem proteção contra UPDATE/DELETE | Baixa | dados | V (leitura) | `ZalekDbContext.cs:70-71` protege só movimentos | P | não | não |
| B8 | Dependências: next 16.4.6 disponível; React canary embutido indeterminado; SQLite vulnerável só nos testes | Baixa | segurança | V | `npm view`; `logs/dotnet-vulneraveis.log` | P | não | não |
| B9 | Imagens Docker com tag móvel | Baixa | operação | V | compose e Dockerfiles | P | não | não |
| B10 | Comando de teste .NET pode passar com 6 ignorados sem acusar | Baixa | testes/documentação | V | `logs/backend-testes-1.log`, `-2.log` | P | não | não |
| B11 | Testes frontend: Vitest em 1 arquivo; E2E poluem dados e dependem do seed | Baixa | testes | V (leitura) | `FormularioOrcamento.test.tsx`; `catalogo.spec:61-66`; `proposta.spec:114-123` | M | não | não |
| B12 | WhatsApp: número estrangeiro com 11 dígitos recebe o prefixo 55 | Baixa | produto | V (leitura) | `ModalProposta.tsx:26` | P | sim | não |
| B13 | Miniaturas usam a imagem original (até 20 MB) | Baixa | desempenho | V (leitura) | `DemaisControllers.cs:39-50` | M | não | não |
| B14 | Deriva de documentação (README, docs/09, Swagger) | Baixa | documentação | V | ver a seção 8 | P | não | não |
| B15 | Ref local de agente contém `.env.revisao` commitado | Baixa | segurança | V | commit `ea5077a` só em `refs/agents/...` | P | sim | não |
| Q1 | Licença QuestPDF Community depende da receita anual | [CONFIRMAR] | operação | V (leitura) | `GeradorDocumentoOrcamento.cs:17` | — | sim | a confirmar |

### 4.2 Detalhamento (por severidade)

#### C1 — Chave JWT padrão versionada permite forjar token de Administrador (Crítica, VERIFICADO)

**Evidência:**
- `docker-compose.yml:34` define `Jwt__Chave: ${JWT_CHAVE:-<valor versionado>}` com 62 caracteres.
- A validação de inicialização só exige 32 ou mais caracteres (`Infrastructure/DependencyInjection.cs:22`).
- As políticas conferem apenas a claim `role` (`ConfiguracaoServicos.cs:122-127`); só `/api/auth/me` consulta o banco.
- `.env.revisao` não define `JWT_CHAVE` (verificado pelos nomes das variáveis, sem valores), então a revisão usa o padrão.

**Teste ao vivo** (`scripts/forjar-jwt.mjs` → `evidencias/S1-jwt-forjado.txt`), na porta 5100 apenas:
- Token HS256 assinado com o padrão, `sub` inexistente e `role=Administrador`: **200** em `GET /api/usuarios` (5 itens), `/api/configuracoes/opcoes` e `/api/clientes`.
- Controle com chave errada: 401.
- Escrita, sem efeito (`scripts/sonda-csrf-escrita.mjs` → `evidencias/sonda-csrf-escrita.md`): `POST /api/usuarios`, `/api/clientes` e `/api/configuracoes/opcoes` com corpo `{}` e o token forjado retornaram **400 de validação**. Ou seja, passaram pela autorização e só pararam por falta de dados.

**Impacto:** qualquer implantação feita com o compose sem `JWT_CHAVE` permite a quem conhece o repositório ler e alterar CPF/CNPJ, telefones e endereços de clientes, custos e margens, e criar usuários. A escrita foi provada até a autorização; nenhum dado foi gravado. Não há `.env` na raiz. O ambiente principal provavelmente usa o mesmo padrão (INFERÊNCIA; não acessado; [CONFIRMAR]).

**Correção sugerida:**
- Trocar por `${JWT_CHAVE:?defina JWT_CHAVE}` para que o compose falhe sem a variável.
- **Efeito colateral:** o comando de revisão do `AGENTS.md` passa a falhar enquanto `.env.revisao` e `.env.revisao.example` não tiverem `JWT_CHAVE`. Atualize os dois e a documentação no mesmo PR.
- Na API, recusar na inicialização qualquer chave igual ao padrão conhecido ou à chave de Development.
- Gerar uma chave nova para cada ambiente.
- Depois disso, todos os tokens antigos deixam de valer.

**Esforço:** P. **Bloqueante:** sim.

#### A1 — Padrões inseguros de produção no compose (Alta, VERIFICADO por leitura)

O compose é o caminho de implantação documentado e sobe com `ASPNETCORE_ENVIRONMENT: Production` (`:32`), mas com estes padrões:
- `POSTGRES_PASSWORD` padrão versionado (`:14`) e porta `5432` publicada em `0.0.0.0` (`:15-16`). O banco fica acessível na rede com senha conhecida.
- `EXECUTAR_SEED:-true` e `SENHA_USUARIOS_DEMO` padrão (`:55-56`). Num banco novo de produção, o seed (`DbSeeder.cs:42,54-59`) cria Administrador e Gestor com a senha de demonstração conhecida, além de clientes, orçamentos e pedidos fictícios misturados aos reais.
- `SWAGGER_HABILITADO:-true` (`:57`): Swagger exposto (200 em `/swagger/index.html` na revisão).
- `COOKIE_SEGURA:-false` (`:36`): cookie sem `Secure`.
- Porta da API (`5000`) publicada diretamente (`:58-59`). O nginx de exemplo (`infra/nginx/zalek360.conf`) não impede o acesso direto.

**Impacto:**
- Rede local: Alta.
- Exposto à internet: Crítica.

**Correção sugerida:**
- Um arquivo de produção (`docker-compose.prod.yml` ou `.env.producao.example`) sem padrões de segredo, com seed e Swagger desligados, `COOKIE_SEGURA=true` e o banco sem porta publicada (ou ligado a 127.0.0.1).
- Um checklist de go-live (seção 7).

**Esforço:** P. **Bloqueante:** sim.

#### A2 — Sem backup e restauração (Alta, INFERÊNCIA)

**Evidência:** não há menção a backup, `pg_dump` ou restauração no README, em `docs/` ou em `infra/`. O README só descreve os volumes `dados-postgres` e `anexos` e alerta que `down -v` apaga os dados. As artes ficam no volume `anexos`, fora do banco.

**Impacto:** perda total de clientes, orçamentos, histórico e artes em caso de falha de disco, `down -v` acidental ou corrupção.

**Correção sugerida:**
- `pg_dump` diário mais cópia do volume `anexos`, com retenção (por exemplo, 7 diários e 4 semanais) e cópia fora da máquina.
- Teste de restauração documentado e repetido periodicamente.
- Considerar a ANPD e a LGPD para a retenção.

**Esforço:** M. **Depende de decisão** (onde hospedar e guardar). **Bloqueante:** sim, com dados reais. [CONFIRMAR] se já existe algo fora do repositório.

#### A3 — PDF retorna 500 com emoji ou glifo fora da fonte (Alta, VERIFICADO)

**Evidência:** um cliente cadastrado com "😀" no nome fez `GET /api/orcamentos/{id}/exportar-pdf` retornar 500. O log do backend mostra:

> `QuestPDF.Drawing.Exceptions.DocumentDrawingException: The text "...😀 Ação" contains 1 glyph(s) that are not available... Since version 2026.9.0, this check is always performed`

As fontes registradas são apenas IBM Plex Sans e Lato.

**Impacto:**
- Afeta todos os orçamentos do cliente e qualquer texto do item ou das observações com emoji (comum ao copiar do WhatsApp) ou com símbolos fora da fonte.
- A falha é na geração: sem PDF não há proposta pelo modal nem e-mail com anexo, e o fluxo diário trava para aquele cliente.
- Os testes existentes cobrem Unicode no MIME do e-mail, mas não emoji no PDF.

**Correção sugerida** (escolha sua sobre o comportamento):
- registrar uma fonte de fallback com emoji e símbolos (por exemplo, Noto Emoji/Noto Sans Symbols, com licença OFL); e/ou
- substituir glifos ausentes antes de renderizar; e/ou
- validar ou avisar no cadastro.

Incluir um teste de PDF com emoji. **Esforço:** P/M. **Bloqueante:** recomendado.

#### M1 — "40.50" vira 4050 no orçamento (Média, VERIFICADO)

**Evidência:**
- `paraNumero` remove todos os pontos (`frontend/utils/formatos.ts:103-108`).
- O formulário de produto aceita ponto como decimal (`FormularioProduto.tsx:23-24`); os dois formulários discordam.
- Ao vivo (`evidencias/ui/valor-com-ponto.png`): valor unitário "40.50" com 2 unidades exibiu **Total R$ 8.100,00**.

**Impacto:** quem digita o decimal com ponto (teclado numérico) grava um valor 100 vezes maior. A prévia do total e o aviso "Valor diferente do preço cadastrado" ajudam, mas não impedem salvar.

**Correção sugerida:** uma única função de conversão de valores, que aceite vírgula ou ponto como separador decimal quando houver no máximo 2 casas e rejeite formatos ambíguos. Incluir teste Vitest. **Esforço:** P.

#### M2 — Unitário com mais de 2 casas decimais (Média, VERIFICADO)

**Evidência:** `Orcamento.Validar` usa o unitário cru (`Orcamento.cs:92-97`; `ItemOrcamento.cs:65` aceita qualquer escala). A gravação arredonda (`ItemOrcamento.cs:137`) e recalcula (`Orcamento.cs:403-411`). Ao vivo:
- 1000 × 0,004 com desconto 4 → **500** (`ck_orcamentos_valores` violado; log do backend).
- 1 × 0,004 → **201 com unitário R$ 0,00 e total R$ 0,00**.

**Impacto:** casos raros, mas que gravam um orçamento sem valor ou causam erro 500 em vez de mensagem.

**Correção sugerida:** rejeitar mais de 2 casas na API (como já é feito no produto) ou validar com o valor já arredondado; exigir unitário ≥ 0,01; mapear violações de CHECK para 422. **Esforço:** P.

#### M3 — Sem limitação de tentativas no login (Média, VERIFICADO por leitura)

**Evidência:** não há `AddRateLimiter` nem bloqueio progressivo. A senha de demonstração, com 10 caracteres, é conhecida.

**Impacto:** força bruta viável se a aplicação ficar exposta.

**Correção sugerida:** `AddRateLimiter` por IP e por e-mail no `POST /api/auth/login` (o ASP.NET Core 8 tem o middleware nativo), com resposta 429 no envelope padrão. Antes disso, corrigir B6 para que o IP considerado seja o real. **Esforço:** P.

#### M4 — Sessão não revogável (Média, VERIFICADO por leitura)

**Evidência:** as políticas usam só as claims do JWT. Desativar o usuário ou mudar o perfil não invalida tokens já emitidos: 600 minutos, ou 7 dias com "manter conectado" (`appsettings.json:9-10`). O logout só apaga o cookie (`AuthController.cs:48-62`).

**Impacto:** quando houver um ajudante e ele for desligado, continua operando até o token expirar.

**Correção sugerida:** um carimbo de segurança (`versao_sessao`) no usuário, conferido a cada requisição com cache curto, ou validação de "ativo e perfil" no `OnTokenValidated`. Combinar com a tela de Usuários (seção F do docs/10). **Esforço:** M.

#### M5 — Open redirect após o login (Média, VERIFICADO)

**Evidência:** `TelaLogin.tsx:31` aceita destinos que começam com "/" e não com "//". Ao vivo, `/login?redirect=/\example.com/` levou para `https://example.com/` depois do login.

**Impacto:** um link legítimo do sistema pode levar a uma página falsa ("sessão expirada, digite a senha").

**Correção sugerida:** aceitar só caminhos internos conhecidos, ou normalizar com `new URL(destino, location.origin)` e exigir a mesma origem. **Esforço:** P.

#### M6 — Sem CI (Média, VERIFICADO)

**Evidência:** `.github/` não existe. A validação depende de execução manual, que nesta revisão mostrou armadilhas (B10).

**Correção sugerida:** GitHub Actions com backend (serviço PostgreSQL e `ZALEK_TEST_POSTGRES`, falhando se houver ignorados), typecheck, Vitest e build. O E2E pode ficar manual no início. **Esforço:** M.

#### M7 — E2E instável em `proposta.spec.ts:100` (Média, VERIFICADO)

**Evidência:**
- Na suíte E2E completa (24 execuções, sem SMTP), falhou o caso do Administrador.
- Na execução de `proposta.spec.ts` com captura SMTP (3 execuções), falhou o caso do Gestor, na mesma asserção ("modal não fechou após Confirmar apresentação").
- Isolados, os dois passaram.
- Contextos: `evidencias/e2e-falha-proposta-*.md`.
- O docs/10 registra 24/24 e 3/3.

**Impacto:** é o fluxo de apresentação da proposta. Pode ser só um problema de sincronização do teste, mas também pode haver uma condição de corrida real na interface (por exemplo, a análise interna "Atualizando..." re-renderizando o modal). Não deu para concluir sem alterar o código.

**Correção sugerida:** investigar com trace do Playwright; esperar a resposta de `POST .../proposta/confirmacao` antes de verificar o modal. **Esforço:** M.

#### M8 — Desempenho com volume (Média, VERIFICADO/INFERÊNCIA)

**Evidência:**
- `ListarReposicaoAsync`, usado no dashboard e depois de cada ajuste, carrega todas as matérias-primas, compras e **todas as reservas históricas** em memória (`EstoqueService.cs:25-32`; `DashboardQueries.cs:55`).
- Compras, matérias, fichas, produtos, pendências e os orçamentos e pedidos por cliente não têm paginação; os movimentos têm `Take(100)`.
- Smoke com a massa pequena da revisão: dashboard p95 de 329 ms e PDF p95 de 393 ms, acima da referência de 300 ms. As listagens ficaram com p95 de 135 a 181 ms.

**Impacto:** degradação gradual em 1 a 2 anos de uso (INFERÊNCIA).

**Correção sugerida:** filtrar reservas ativas no banco, agregar via SQL, paginar compras e movimentos. **Esforço:** M.

#### CS — Avaliação de CSRF (Baixa, VERIFICADO)

**Contexto:** a sessão é um cookie, que o navegador envia sozinho. Não há token antiforgery e o CORS fica vazio por padrão (`appsettings.json:50`). O navegador sempre fala com `/api/*` na mesma origem, via proxy do Next.

**Proteções observadas:**
- O cookie tem `SameSite=Lax` explícito (`AuthController.cs:33-43`). Navegadores atuais não enviam o cookie em POST, PUT, PATCH ou DELETE vindos de outro site.
- Ao vivo, com cookie válido, POST com `text/plain`, `application/x-www-form-urlencoded` ou `multipart/form-data` em `/api/clientes` retorna **415**. Os endpoints com corpo exigem JSON, que um formulário HTML de outro site não consegue enviar.

**Pontos que dependem só do SameSite:**
- POSTs sem corpo que alteram estado e aceitam qualquer content-type: `POST /api/pedidos/{id}/materiais/ficha-tecnica`, `POST /api/orcamentos/{id}/exportacoes` e `POST /api/orcamentos/{id}/proposta/pdf`. Com id aleatório, retornaram 404, ou seja, foram processados.
- O upload `POST /api/anexos` aceita multipart por natureza.

**Exposição por cenário:**
- Rede local ou um único domínio, com navegadores atuais: baixa.
- Domínio compartilhado com subdomínios que outra pessoa controle (same-site): média. Nesse caso, considerar um header antiforgery customizado (por exemplo, `X-Requested-With` exigido pela API).

**Observação:** o 415 sai sem o envelope `{status, code, message, errors}`.

**Esforço:** P.

#### Baixas (resumo)

- **B1** `/pedidos?periodo=abc` derruba a página ("This page couldn't load", em inglês). Não há `app/error.tsx`. Validar o parâmetro e adicionar um error boundary.
- **B2** `resultado: 99` retorna 500 (`ArgumentOutOfRangeException`, `Rotulos.cs:52`). Validar `Enum.IsDefined`, ou impedir que o `JsonStringEnumConverter` aceite inteiros.
- **B3** A decisão manual "Expirado" é aceita (D26). Decidir se deve ficar só automática.
- **B4** Login falho registrado só como "Requisição rejeitada 401", sem conta mascarada nem IP; o 403 não é registrado; das ações administrativas, só a criação de Gestor é registrada.
- **B5** Sem CSP, HSTS e Permissions-Policy (`next.config.ts:23-25`). Não há vetores de XSS conhecidos; é defesa em profundidade. Lembrar `connect-src` para viacep.com.br.
- **B6** `ForwardedHeaders` com `KnownNetworks` e `KnownProxies` vazios (`Program.cs:20-25`): qualquer cliente forja `X-Forwarded-For` e `X-Forwarded-Proto`.
- **B7** `historico_eventos` só é protegido por setters privados. Estender a guarda do `SaveChanges` (`ZalekDbContext.cs:70-71`), ou criar um trigger, como nos movimentos.
- **B8** Dependências:
  - Next 16.4.5 está fora das faixas de todos os avisos lidos: os 7 Moderate de 03/11/2026 têm a correção justamente em 16.4.5; os críticos de out/2026 foram corrigidos em 16.4.1.
  - A 16.4.6 foi publicada em 09/11/2026.
  - O Next embute o React `19.3.0-canary-278794d7-20261002`. Os avisos GHSA-xqc2-xf4c-x3h3 (Critical) e GHSA-h3gv-c5rc-p453 (High) afetam `react-server-dom-*` 19.3.0 (corrigido em 19.3.1); os equivalentes do Next (GHSA-673q-mjm6-r5xr, GHSA-p3m5-59mp-jv5r) foram corrigidos em 16.4.1. O canary embutido fica **indeterminado**.
  - O projeto de testes .NET usa `SQLitePCLRaw.lib.e_sqlite3` 2.1.6 (High, GHSA-2m69-gcr7-jv3q), fora da imagem de produção.
  - Links: https://github.com/vercel/next.js/security/advisories/GHSA-2464-fhr4-w9xw · https://github.com/vercel/next.js/security/advisories/GHSA-vcvr-r3jv-pc5j · https://github.com/react/react/security/advisories/GHSA-xqc2-xf4c-x3h3 · https://github.com/react/react/security/advisories/GHSA-h3gv-c5rc-p453 · https://github.com/react/react/security/advisories/GHSA-wx67-qw84-cm4g · https://github.com/advisories/GHSA-2m69-gcr7-jv3q
  - Os demais IDs consultados estão em `NOTAS-PARCIAIS.md`.
- **B9** Imagens `postgres:16-alpine`, `node:22-bookworm-slim`, `dotnet/sdk:8.0`, `aspnet:8.0` e `rnwood/smtp4dev:v3` sem digest. Fixar versão de patch ou digest.
- **B10** O comando de testes do `AGENTS.md`, executado pelo PowerShell desta ferramenta, enviou um BOM no stdin (`sh: 1: ﻿export: not found`). O resultado foi 133 aprovados e 6 ignorados, com exit 0 e "Test Run Successful". A causa é o ambiente, não o projeto, mas nada acusa os ignorados. Sugestão: no modo de validação, fazer `[PostgresFact]` falhar sem a variável, ou checar "Skipped: 0".
- **B11** Testes frontend:
  - Vitest com um único arquivo (8 testes); sem testes de `utils/formatos` (onde está o M1) nem de `lib/api`.
  - O E2E cria uma categoria permanente a cada execução (`catalogo.spec:61-66`; `opcoes_catalogo` passou de 41 para 42).
  - O E2E altera o modelo global de e-mail sem `finally` (`proposta.spec:114-123`).
  - Depende do seed (`clientes[0]`, categorias "Camiseta" e "Caneca") e exige deltas exatos no dashboard (`estoque-vendas:87-88`).
  - Tem cerca de 5% de seletores CSS, XPath ou `nth`.
  - Não há `waitForTimeout`, um ponto positivo.
- **B12** WhatsApp: um número com 10 ou 11 dígitos recebe 55. Um estrangeiro de 11 dígitos (+1) vira +55. Números sem DDD são corretamente recusados.
- **B13** Miniaturas usam a imagem original, até 20 MB (`DemaisControllers.cs:39-50`): lento no celular.
- **B14** Deriva de documentação: ver a seção 8.
- **B15** `.env.revisao`, com POSTGRES_DB/USER/PASSWORD e portas, foi commitado em `ea5077a` (24/11/2026) apenas na ref local `refs/agents/b1df5a70-.../checkpoints/turn/14`. Nenhuma branch local ou remota contém o commit, e o remoto não tem `refs/agents`. Risco: só um push de todas as refs o publicaria. Sugestão (sua decisão): apagar a ref local de checkpoint.

#### Questionamentos de decisão (não são bugs)

- **Cancelar após Em produção não estorna o consumo** (`Pedido.cs:53`; G3). É regra documentada. Sugiro registrar a perda e oferecer um ajuste de estoque com motivo, quando houver (seção F).
- **Seed com 3 Atendentes ativos** em banco novo: não autenticam, mas aparecem como contas ativas no banco. Avaliar se devem ser criadas inativas.

---

## 5. Resultados dos testes

| Suíte | Aprovados | Falhas | Ignorados | Duração | docs/10 (Fase 6) |
|---|---|---|---|---|---|
| Backend (execução 3, Git Bash: mesmo container, rede, env-file e volumes) | **139** | 0 | **0** | 28 s | 139/0/0 ✔ |
| Backend (execuções 1 e 2, PowerShell da ferramenta) | 133 | 0 | 6 | 1,9 min / 38 s | descartadas: BOM no stdin (B10) |
| `npm ci` | — | — | — | 212 s | 0 vulnerabilidades ✔ |
| `npm run typecheck` | ok | — | — | 4 s | ✔ |
| Vitest | **8** | 0 | 0 | 7 s | 8/8 ✔ |
| `npm run build` | ok | — | — | 21 s | ✔ |
| E2E Edge (suíte completa) | **23** | **1** | 0 | 4,5 min | 24/24 ✘ (instabilidade, M7) |
| E2E isolado (proposta Admin) | 1 | 0 | 0 | 17 s | — |
| E2E captura SMTP (`E2E_SMTP_TESTE=true`) | **2** | **1** | 0 | 59 s | 3/3 ✘ (instabilidade, M7) |
| E2E SMTP isolado (proposta Gestor) | 1 | 0 | 0 | 33 s | — |
| Bordas SMTP (script) | 6/6 | 0 | — | — | — |

- **Comparador de esquema:** 0 divergências. Tabelas 21/21, colunas 235/235, constraints 71/71, índices 72/72, sequências 2/2. Os bancos temporários foram removidos.
- **Matriz de autorização** (`evidencias/matriz-autorizacao.md`): 35/35 conformes em 35 endpoints × 6 sujeitos (sem cookie, Gestor, Admin, role adulterada, expirado, `alg=none`). O login da Atendente retorna 401. Não há conta Operador no banco de revisão, então não foi testada.
- **Exploratório da API** (`evidencias/exploratorio-api.md`): 63/68 conformes. O G2 foi reconferido: era erro de nome de campo no meu script. As divergências são D7, D8, D15, D21 e D26.
- **Exploratório da interface** (`evidencias/exploratorio-ui.md`): divergências U1 (open redirect), U3 (`/pedidos?periodo=abc`) e U5 (decimal com ponto). Os demais itens estão conformes.
- **Smoke** (`evidencias/smoke-carga.md`): 5 usuários virtuais, 90 s, 2.621 requisições, 0 erros. A massa é pequena (cerca de 50 clientes, 210 orçamentos e 130 pedidos) e não representa o volume futuro.

| Endpoint | p50 | p95 | ≤ 300 ms |
|---|---|---|---|
| clientes | 24 | 154 | sim |
| orçamentos | 25 | 138 | sim |
| pedidos | 26 | 135 | sim |
| estoque | 30 | 181 | sim |
| dashboard | 57 | 329 | não |
| PDF | 56 | 393 | não |

### Checklist BDD e regras (adaptado: sem OS; Gestor no lugar de "atendente")

| Cenário | Ao vivo | Teste automatizado | Status |
|---|---|---|---|
| BDD-1 Cadastro com duplicidade (com e sem máscara) | A2, A6: 409 | `CasosDeUsoTests:30` (fake) | ✔ |
| BDD-2 Orçamento sem itens | D1: 400 | `OrcamentoTests:42`, `CasosDeUsoTests:104` | ✔ |
| BDD-3 Decisão sem meio de contato | D20: 400 | `OrcamentoTests:68` | ✔ |
| BDD-4 Conversão em pedido (na mesma transação) | D22: um pedido | `IntegracaoTests:65`, `CasosDeUsoTests:151` | ✔ |
| BDD-5 Histórico vazio | não exercitado ao vivo | não localizado | ⚠ sem teste |
| BDD-6 Edição bloqueada após aprovação | D24: 409 | `OrcamentoTests:118` | ✔ |
| BDD-7 Contato sem mudar a situação | D19 | `OrcamentoTests:58` | ✔ |
| BDD-8 Reserva parcial (50 para 80 → 50 + 30; disponível ≥ 0) | D23 | `EstoqueTransacionalTests:47-78` | ✔ |
| BDD-9 Aprovações simultâneas (60; 2 × 50 → ≤ 60) | F1: 50 + 10 | `EstoquePostgresTests:97-136` (PostgreSQL) | ✔ |
| BDD-10 Produto sem ficha técnica | não ao vivo | `EstoqueTransacionalTests:81,144` | ✔ (teste) |
| BDD-11 Cancelamento libera reservas | G2 (reconferido) | `EstoqueTransacionalTests:98` | ✔ |
| Pendência bloqueia Em produção | E2: 409 | testes de estoque + E2E cenário completo | ✔ |
| Baixa única e transições inválidas | E7–E14 | `PedidoTests`, E2E | ✔ |
| Expiração na data-limite (UTC × America/Sao_Paulo) | não ao vivo | `Orcamento.cs:351` (`Validade < hojeLocal`), testes de domínio de expiração | ✔ por leitura |
| Custo e margem fora do PDF e das mensagens | D14, D16, U6b | `OrcamentoFase3ApiTests:50-76`, `PropostaApiTests:119` | ✔ |

---

## 6. Rastreabilidade (requisito → código → teste → status)

| Requisito | Código principal | Teste | Status |
|---|---|---|---|
| R1 Perfis e políticas (Operacao/Administracao) | `ConfiguracaoServicos.cs:119-127`; atributos dos controllers; `AuthService.cs:35,45,50-51` | `ControleAcessoApiTests`, `CatalogoFase2ApiTests`, `PropostaApiTests:137`, `EstoqueVendasApiTests:143`, `login.spec` + matriz ao vivo | Atende (ressalvas C1 e M4) |
| R1 Responsável automático, preservado na edição | `OrcamentoService.cs:72,94` | `ControleAcessoApiTests` (responsável) + D12 | Atende |
| R1 Custo e margem só na tela | `AnaliseOrcamentoService.cs`, `AnaliseInterna.tsx`; DTOs da proposta e do PDF sem custo | `OrcamentoFase3ApiTests`, `PropostaApiTests` + D14/D16/U6 | Atende |
| R2 Matéria-prima (`un`, inteiros, mínimo, ativo) | `MateriaPrima.cs`, `EstoqueService.cs:337-342`, CHECKs `Mapeamentos.cs:54-131` | testes de estoque + B3–B6 | Atende (código e categoria pendentes, seção F) |
| R2 Compra e recebimento | `EstoqueService` (compras), lock da compra `Repositorios.cs:37` | testes de reposição + E3–E5 | Parcial (recebimento parcial e custo pendentes, seção F) |
| R2 Ajuste com motivo | PUT matéria-prima (motivo legado) | — | Parcial (seção F) |
| R3 Produto, composição, duplicar, inativar, excluir nunca usado | `ProdutoService.cs:35-60`, `DemaisControllers.cs:109-110` | `CatalogoFase2ApiTests`, `ProdutosPersonalizadosApiTests`, `catalogo.spec` | Atende |
| R4 Orçamento (itens, foto, cálculo, PDF, proposta) | `Orcamento.cs`, `CalculadoraOrcamento.cs`, `GeradorDocumentoOrcamento.cs`, `PropostaService.cs` | `OrcamentoTests`, `OrcamentoDocumentoTests`, `DocumentoPdfTests`, `PropostaApiTests`, `EmailMimeTests`, E2E | Atende, com defeitos A3, M1 e M2 |
| R5 Aprovação → pedido, reservas, pendência, fluxo simples, baixa, cancelamento | `OrcamentoService.cs:124-158`, `EstoqueService.cs:190-312`, `Pedido.cs:45-57` | `EstoqueTransacionalTests`, `EstoquePostgresTests`, `PedidoTests`, `cenario-completo.spec` + E/F/G | Atende |
| R5.8 OS por etapas não usada | nenhuma entidade, enum ou endpoint de OS no código atual (só os perfis legados no enum) | — | Atende (decisão registrada) |
| R6 Reposição (sugestão bruta e falta líquida) | `EstoqueService` reposição | `EstoqueVendasApiTests`, `estoque-vendas.spec` | Atende (M8: desempenho) |
| R7 Histórico, filtros, vendas entregues | `PedidoQueries`, `DashboardQueries`, `historico_eventos` | `EstoqueVendasApiTests` (PostgreSQL), `IntegracaoTests:124` | Atende (BDD-5 sem teste) |
| R8 Português, estados, 360/1280, teclado | `features/*`, `components/ui` | `acabamento.spec`, U2/U4/U6c/U7 | Atende (B1: tela de erro em inglês) |
| Expiração automática | `Rotinas.cs:24-43`, `Orcamento.cs:351`, `RelogioComFusoHorario.cs` | testes de domínio e de casos de uso de expiração | Atende (B3) |
| Numeração | sequências PostgreSQL (`ServicosInfraestrutura.cs:27`) | `CasosDeUsoTests` (sem consumo em erro) | Atende |
| Imutabilidade de movimentos e histórico | `ZalekDbContext.cs:70-71` | — | Movimentos: atende; histórico: B7 |

---

## 7. Avaliação de produto

**Rapidez do fluxo diário** (INFERÊNCIA, contada pelos componentes e pelos E2E, sem cronômetro):

- **Criar e enviar um orçamento típico:** cerca de 15 interações em 2 telas, 1 modal e o WhatsApp externo.
  - Criação: Orçamentos → Novo → buscar e selecionar o cliente → produto (o preço vem sozinho) → quantidade → tipo de personalização → cor da peça → Salvar.
  - Envio: Apresentar → canal → Baixar PDF → Abrir WhatsApp → (anexar o PDF manualmente) → marcar "Já enviei" → Confirmar.
- **Do aprovado ao Entregue:** cerca de 12 a 14 interações.
  - Registrar decisão (resultado, meio, data) → confirmar "Aprovar e gerar pedido?".
  - No pedido: 3 × Atualizar status. Se faltar material: Estoque → planejar compra → receber → voltar ao pedido → Carregar ficha técnica.
- É razoável para uma pessoa. O passo manual de anexar o PDF no WhatsApp é inevitável sem link público, por decisão sua.

**Pontos de atrito observados:**
- Separador decimal (M1).
- Emoji no nome bloqueia o PDF (A3).
- Depois de receber uma compra, é preciso lembrar de "Carregar ficha técnica" no pedido (complementação automática pendente, seção F).
- Tela de erro em inglês com URL inválida (B1).
- Os campos de data e hora dos modais de contato e decisão guardam a hora em que a página foi aberta (`DetalheOrcamento.tsx:82,137`, INFERÊNCIA de leitura).
- Depois de um erro 4xx, o modal de envio fica travado até marcar "Preparar novo envio".

**Lacunas que bloqueiam ou arriscam o uso real:**

| Lacuna | Situação hoje | Risco |
|---|---|---|
| Tela de Usuários / redefinir senha | Não existe. Hoje só via Swagger (`POST /api/usuarios`, que cria Gestor). Não há como redefinir senha nem desativar conta sem acesso direto ao banco | Alto quando houver ajudante ou senha esquecida |
| Dados da empresa e SMTP | Só por variáveis do servidor (`Empresa__*`, `Email__*`) | Médio (depende de quem administra o servidor) |
| E-mail por provedor real | Não testado; só o capturador | Médio |
| Backup | Inexistente no repositório (A2) | Alto |
| Hospedagem e HTTPS | Só um nginx de exemplo; sem runbook | Alto se exposto |
| Importação de clientes existentes | Não existe; cadastro manual com validação de DV e duplicidade | Médio (trabalho manual inicial) |
| Seção F do docs/10 | Código e categoria de matéria-prima, recebimento parcial, motivo de ajuste, complementação automática | Baixo a médio |

**Checklist de go-live (proposta):**
1. `JWT_CHAVE` nova e forte; `POSTGRES_PASSWORD` forte; `EXECUTAR_SEED=false`; `SWAGGER_HABILITADO=false`; `COOKIE_SEGURA=true`.
2. Banco sem porta publicada; API sem porta publicada (só o frontend atrás do proxy TLS).
3. HTTPS com certificado válido; HSTS.
4. Backup automático de banco e anexos, com restauração testada e documentada.
5. Banco de produção novo, sem dados fictícios; contas criadas manualmente com senhas fortes.
6. `Empresa__*` preenchidos; SMTP com conta própria e senha de aplicativo (opcional); envio de teste para um endereço da empresa.
7. Corrigir C1, A1, A3, M1 e M5 antes do primeiro cliente real.
8. Plano de atualização: `git pull` → backup → `up --build` → conferir `/health` e o login.
9. Confirmar a elegibilidade da licença QuestPDF Community (Q1).

**Métricas sugeridas** (dá para extrair das tabelas existentes):
- orçamentos criados e enviados por semana;
- taxa de aprovação (aprovados ÷ decididos);
- tempo médio até a decisão;
- motivos de recusa;
- pedidos atrasados (prazo de entrega vencido sem Entregue);
- rupturas (pedidos com pendência de material) e o tempo para resolvê-las;
- valor entregue no mês.

**Coleta de feedback:** uma nota semanal de 5 minutos ("o que travou, o que demorou, o que digitei duas vezes") nas primeiras 4 semanas, mais a revisão das métricas acima.

**Preparo para um segundo usuário:** tela de Usuários (criar, desativar, redefinir) mais revogação de sessão (M4), rate limit (M3) e logs de segurança (B4). Decidir antes se o ajudante verá custo e margem (decisão registrada como futura).

**Sugestões de produto (decisão sua, não são defeitos):**
- aviso de "lembrete de recarregar a ficha" no pedido com pendência quando chega uma compra;
- importação CSV de clientes;
- link no dashboard para "pedidos atrasados".

---

## 8. Deriva de documentação

**Documento V4 × sistema atual** (com base no resumo do prompt; o V4 não está no repositório):
- **OS por etapas:** descartada; vale o fluxo simples Aberto → Em produção → Pronto → Entregue.
- **4 perfis:** viraram 2 ativos (Gestor e Administrador); Atendente e Operador são só legados.
- **Hospedagem Azure App Service + Vercel + Nginx:** hoje há só Docker Compose e um nginx de exemplo.
- **PWA:** não existe.
- **Meta de 300 ms:** atendida nas listagens com massa pequena; dashboard e PDF ficaram acima.
- **Disponibilidade de 99%:** sem medição nem monitoramento.
- **Novidades para a V5:**
  - catálogo com preço e custo;
  - análise interna de margem;
  - envio de proposta por SMTP, `mailto` e WhatsApp com confirmação explícita;
  - reposição (sugestão bruta e falta líquida);
  - vendas entregues no mês;
  - opções gerenciáveis;
  - modelos de mensagem.
- **BDD-12:** remover.

**Documentos do repositório × código:**
- O título do `README.md` ainda diz "MVP 1 (ciclo comercial)"; a descrição do Swagger também diz "MVP 1" (`ConfiguracaoServicos.cs:143`).
- `README.md` §4 e §6 descrevem os testes como "integração (EF + SQLite em memória)" e contam 27/14/3. Hoje há testes WebApplicationFactory e PostgreSQL real; o total atual (139) aparece só em outra frase.
- `docs/09-revisao-funcional.md`, bloco "Evidências técnicas adicionais" (linhas 206-209), está desatualizado e contradiz a própria matriz. Ainda afirma que:
  - "Não existe chamada de produto novo no formulário";
  - "A disponibilidade... não é apresentada";
  - "O dashboard usa orçamentos aprovados no mês";
  - "Não há Repor estoque".
- `docs/10`: registra E2E 24/24 e SMTP 3/3. Nesta revisão houve instabilidade recorrente (M7).
- `AGENTS.md`: o comando de testes .NET pode passar com testes ignorados em hosts PowerShell que injetam BOM no stdin (B10). Sugiro mencionar a checagem "Skipped: 0".
- Não há documentação de deploy em produção, atualização de versão, rotação de chave nem backup e restauração [CONFIRMAR se existe fora do repositório].
- O log do seed diz "4 usuários", mas cria 5 (`DbSeeder.cs:269`).

---

## 9. Roadmap priorizado

Cada fase corresponde a uma branch e um PR, seguindo o fluxo do `AGENTS.md`.

**Agora (antes do uso real):**
1. **Fase S1 — Configuração segura de implantação (P):**
   - C1: `${JWT_CHAVE:?}` e rejeição da chave padrão na inicialização;
   - A1: compose ou env de produção sem padrões, seed e Swagger desligados, banco sem porta publicada, `COOKIE_SEGURA=true`;
   - M5: open redirect;
   - documentar a rotação.
2. **Fase S2 — Backup e implantação (M, depende de decisão):**
   - A2: script de backup de banco e anexos com restauração testada;
   - runbook de deploy, atualização e restauração;
   - checklist de go-live.
3. **Fase S3 — Correções de valores e PDF (P/M):**
   - A3: fonte de fallback e teste com emoji;
   - M1: conversão decimal única, com teste Vitest;
   - M2: validação de casas decimais e mapeamento de CHECK para 422;
   - B2: validação de enum.

**Próximas 2 a 4 semanas:**
4. **Fase S4 — Contas e sessões (M):**
   - tela de Usuários (seção F; decisão sua);
   - M4: revogação por versão de sessão;
   - M3: rate limit no login (com B6);
   - B4: logs de segurança.
5. **Fase S5 — Qualidade contínua (M):**
   - M6: CI (backend com PostgreSQL e ignorados = falha; typecheck, Vitest, build);
   - M7: estabilizar o E2E da proposta;
   - B11: isolar os dados do E2E;
   - B10.
6. **Fase S6 — Desempenho e cabeçalhos (M):**
   - M8: reposição filtrada no banco e paginação;
   - B13: miniaturas;
   - B5: CSP e HSTS;
   - B8: next 16.4.6 via npm, com lockfile;
   - B9: imagens com versão fixa.

**Depois:**
7. Itens da seção F, conforme as suas decisões (código e categoria, recebimento parcial, motivo de ajuste, complementação automática, interface de dados da empresa e SMTP).
8. B7 (imutabilidade do histórico), B1, B3, B12, documentação V5 e métricas no dashboard.

---

## 10. Perguntas em aberto (só você pode responder)

1. O sistema já roda em produção com dados reais? Onde está hospedado? Está exposto à internet ou só na rede local? Há HTTPS?
2. O ambiente principal `zalek360` (3000/5000/5432) guarda dados reais da empresa? Ele foi subido sem `.env`, isto é, com a chave JWT e a senha do banco padrão? Se sim, a correção de C1 e A1 é urgente lá também.
3. Já existe algum backup fora do repositório? Onde devem ficar as cópias, e com qual retenção?
4. Licença QuestPDF: a receita anual da empresa está dentro do limite da Community? (Q1; não é uma conclusão jurídica.)
5. Emoji em nomes e textos: prefere suportar no PDF (fonte de fallback) ou impedir ou avisar no cadastro?
6. A decisão manual "Expirado" deve continuar permitida?
7. Quando entra o ajudante? Ele verá custo e margem?
8. Haverá importação dos clientes atuais? De que fonte (planilha, WhatsApp)?
9. Qual provedor de e-mail será usado?
10. A ref local `refs/agents/...` com o `.env.revisao` commitado pode ser apagada (B15)?

---

## 11. Não verificado (e por quê)

- **Ambiente principal:** proibido acessar. A exposição real de C1 e A1 lá é [CONFIRMAR].
- **Envio por provedor SMTP real:** proibido; só o capturador.
- **Expiração ao vivo na virada da data:** exigiria manipular o relógio. Verificado por leitura (`Orcamento.cs:351`; `RelogioComFusoHorario.cs`) e pelos testes existentes.
- **Conteúdo textual do PDF** (ausência de custo e margem por extração de texto): não instalei ferramentas novas. Verificado pelo código, pelos DTOs e pelos testes `OrcamentoFase3ApiTests` e `PropostaApiTests`.
- **Login de conta Operador:** não há conta Operador no banco de revisão. Atendente testada (401); Operador coberto pelo código e pelos testes (`Reposicao_restrita_a_operacao(perfil: Operador)`).
- **Token expirado "natural":** usei um token expirado assinado com a chave padrão (só possível por causa do C1). O vencimento real ao longo do tempo não foi esperado.
- **Navegação por teclado em todos os modais:** amostra no modal da proposta (U6c) mais os E2E existentes (`acabamento.spec`).
- **Celular real e PWA:** só viewport de 360 px no Edge.
- **Cobertura de código:** não medida.
- **Avisos do Next mais antigos** (páginas 3 a 8 da lista de segurança) e o React canary embutido: não lidos ou indeterminado (B8).
- **Backup e restauração:** não existem no repositório para testar.
- **Multi-instância** (expiração e `MigrateAsync` sem lock): fora do uso atual; só leitura.
- **Documento V4:** fora do repositório; usei o resumo do prompt.
- **Orientação OWASP, ANPD e Playwright:** os documentos externos não foram lidos nesta revisão.
- **`EXPLAIN` das consultas principais:** não executado. Com a massa da revisão (centenas de linhas), o planejador escolhe varredura sequencial e o plano não é representativo. Os índices existem nas colunas de filtro (`Mapeamentos.cs:81,117,142,238-241,353-355`, leitura por subagente).
- **Fluxo essencial como Administrador:** coberto pela suíte E2E existente (cenário completo e catálogo, nos dois perfis) e por U9 (Configurações). Meus scripts exploratórios rodaram o fluxo completo como Gestor.
- **Estados de carregamento e vazio, e erros por aba:** só o estado de erro foi simulado por mim (U4). Carregamento, vazio e erro por aba dependem dos E2E existentes (`acabamento.spec`, `login.spec`), que passaram.
- **Peso do bundle:** medido. 1,2 MB de chunks estáticos sem compressão; o maior chunk tem 229 KB (`.next/static/chunks`). Sem análise por rota.

---

## 12. Dados criados na revisão e arquivos gerados

**Dados criados por mim** no banco `zalek360_revisao`, todos com prefixo `REVISAO-`.

**Dados preexistentes:** nas tabelas rastreadas (clientes, orçamentos, pedidos, produtos, matérias-primas, usuários, `historico_eventos` e movimentos), as 2.334 linhas de referência estão idênticas antes e depois (`evidencias/linhas-antes.txt` × `linhas-depois.txt`; `linhas-alteradas.txt` vazio). Duas tabelas ficaram de fora do snapshot:
- `modelos_mensagem_proposta`: o E2E existente edita e restaura o modelo de e-mail (a versão muda);
- `opcoes_catalogo`: o E2E acrescentou 1 categoria.

- **Prefixo `REVISAO-11251943-`:**
  - 2 clientes: PF com emoji no nome e PJ, com CPF/CNPJ fictícios gerados.
  - 4 matérias-primas: Camiseta preta GG, Estampa, Unidade kg, Disputa 60.
  - 2 produtos: Camiseta Homem-Aranha, Produto disputa.
  - Orçamentos #338 (total R$ 0,00 do M2), #339 (fluxo principal), #340 (decisão manual Expirado), #341 e #342 (disputa).
  - Pedidos #192 (Entregue), #193 e #194 (Cancelados).
  - 1 compra recebida; 1 anexo PNG.
- **Interface:** orçamento #343 (em elaboração, não alterado).
- **Prefixo `REVISAO-SMTP-333276-`:** 1 cliente, orçamento #347 e 1 e-mail no smtp4dev (destinatário `destino@example.test`).
- **Pelas suítes existentes** (E2E completo e SMTP), com os dados próprios delas: 1 categoria nova (`opcoes_catalogo` 41 → 42), além de clientes, orçamentos, pedidos e matérias de teste.
- **Contagens antes e depois:** `evidencias/contagens-antes.txt` e `contagens-depois.txt`. Por exemplo, clientes 52 → 61, orçamentos 208 → 239, pedidos 128 → 145, usuários 5 → 5.
- **Bancos temporários** dos testes e do comparador: criados e removidos (0 restantes).

**Arquivos gerados** (todos em `revisao-mvp/`):
- `RELATORIO-REVISAO-MVP.md` (este relatório) e `NOTAS-PARCIAIS.md`.
- `scripts/`:
  - `api.mjs`: cliente HTTP com trava para 5100/3100;
  - `forjar-jwt.mjs`, `matriz-autorizacao.mjs`, `exploratorio-api.mjs`, `exploratorio-ui.mjs`, `smoke-carga.mjs`, `smtp-bordas.mjs`, `reconferir-reservas.mjs`, `verificar-smtp.mjs`, `formatos.mjs`, `sonda-csrf-escrita.mjs` e `corrigir-linhas-relatorio.mjs` (só edita este relatório);
  - `contagens.sql`, `hashes.sql`, `linhas.sql`.
- `evidencias/`:
  - matriz, exploratórios, smoke, bordas SMTP, estado SMTP e prova do C1;
  - contagens, hashes e linhas;
  - `esquema/` (saída do comparador);
  - contextos das falhas E2E (senha de demonstração mascarada);
  - `ui/` com 24 capturas.
- `logs/`: compose, testes backend (3 execuções), frontend (ci, typecheck, vitest, build, e2e e isolados), comparador e `dotnet list package --vulnerable`.

**Efeitos colaterais fora de `revisao-mvp/`** (todos ignorados pelo Git):
- `frontend/node_modules` reinstalado (`npm ci`);
- `frontend/.next`, `frontend/test-results/`, `frontend/tsconfig.tsbuildinfo`;
- `backend/**/bin|obj`;
- capturas dos E2E em `data/revisao-fase4|5|6`.

**Incidente corrigido:** a primeira execução do script de interface gravou as capturas em uma pasta irmã do repositório com `%20` no nome (caminho com `%20`). Os arquivos foram movidos para `evidencias/ui/`, a árvore vazia criada por engano foi removida e o script foi corrigido.

**Segredos:** nenhum valor de `.env.revisao` foi exibido ou gravado. Os tokens ficaram numa pasta temporária fora do repositório. A varredura de `revisao-mvp/` por JWT e pela senha de demonstração está limpa.

**Estado final do ambiente:** projeto `zalek360-revisao` no estado padrão (sem override de e-mail; `emailDisponivel=false` às 19:54Z), db saudável, backend e frontend no ar e smtp4dev mantido ligado, conforme pedido. Nenhum volume removido.

---

## 13. Status da Fase 7 (25/11/2026, 17:14)

- **Fase 7 não iniciada.** Hora conferida às 17:14 com `Get-Date`. Faltavam 66 minutos para o limite de 18:20, abaixo dos 75 exigidos para começar uma fase. Decisão provisória: aplicar a regra como foi escrita. Nenhuma branch, PR ou alteração versionada foi criada.
- **Todos os achados seguem pendentes**, inclusive C1 e A1. O `docs/11-atualizar-ambiente-principal.md` não foi escrito.
- **B15 resolvido:** `git ls-remote origin` reconfirmou que o GitHub não tem refs `agents`. A ref local `refs/agents/b1df5a70-.../checkpoints/turn/14` foi apagada com `git update-ref -d`. Nenhuma ref ou commit alcançável toca mais `.env.revisao`. O objeto `ea5077a` continua no repositório local, inalcançável, porque `gc` estava proibido.
- **Armadilhas a tratar no planejamento da 7.1** (a confirmar no código quando a fase começar):
  - `POSTGRES_PASSWORD` só vale na primeira inicialização do volume. Trocar a senha de um banco existente exige `ALTER USER` antes de mudar o `.env`.
  - Não há endpoint para trocar a senha de usuários existentes.
  - Revisão e produção rodam como `Production`. Seed só em revisão e cookie `Secure` exigem um critério novo para distinguir os dois.
  - Segredos obrigatórios quebram o comando de revisão até o `.env.revisao` local ter `JWT_CHAVE`.
  - O E2E instável (M7) pode bloquear o critério "tudo verde" de qualquer merge. Convém tratá-lo primeiro.

---

## 14. Acompanhamento das correções (Fase 7)

Atualizado a cada PR da Fase 7. Status: **corrigido** (mesclado em `main`), **em PR**, **pendente**.

| Achado | Status | Onde |
|---|---|---|
| M7 — E2E instável em `proposta.spec.ts:100` | corrigido | PR #10 (Fase 7.0): modal fecha com o detalhe da API; testes esperam a resposta da API |
| B10 — comando de teste .NET aceitava ignorados | corrigido | PR #10: `backend/scripts/testar-revisao.sh` + `AGENTS.md` |
| B11 (parcial) — E2E alterava o modelo global sem `finally` | corrigido | PR #10 |
| Instabilidade nova: `DROP DATABASE` dos testes PostgreSQL estourando 30 s | corrigido | PR #10: tempo limite de 120 s |
| B15 — ref local com `.env.revisao` | corrigido | ref apagada em 25/11/2026 (sem `gc`) |
| C1 — chave JWT padrão versionada | corrigido no código (PR #11); **ambiente principal pendente do docs/11** | Fase 7.1: segredos obrigatórios; API recusa chaves publicadas ou curtas fora de Development |
| A1 — padrões inseguros do compose | corrigido no código (PR #11); ambiente principal pendente do docs/11 | Fase 7.1: senha do banco obrigatória; banco e API em 127.0.0.1; Swagger só em Development; cookie Secure e seed desligado por padrão |
| Senhas de demonstração em ambiente real | ferramenta pronta (PR #11); troca pendente no ambiente principal | Fase 7.1: `backend/tools/redefinir-senha` e `docs/11` |
| A2 — sem backup e restauração | corrigido (PR #12) | Fase 7.2: `infra/backup/*.ps1` e `docs/12`; restauração testada em banco temporário |
| A3 — PDF 500 com emoji | em PR | Fase 7.3: `TextoPdf` filtra caracteres sem glifo |
| M1 — "40.50" vira 4050 | em PR | Fase 7.3: `paraNumero` com ponto decimal + Vitest |
| M2 — mais de 2 casas grava zero ou 500 | em PR | Fase 7.3: arredondamento único na entrada; CHECK → 422 |
| Demais Médios/Baixos | pendente | após a Fase 7.3 |
