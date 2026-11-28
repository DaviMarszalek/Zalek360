# Planejamento da automação — Zalek360

**Sistema testado:** Zalek360, sistema interno de orçamentos, pedidos e estoque da Zalek Personalizados (Next.js + API ASP.NET Core + PostgreSQL).
**Ferramenta principal:** Selenium WebDriver + Python + pytest (opção A).
**Ambiente:** ambiente de revisão em Docker Compose, frontend em `http://localhost:3100`, banco descartável com os usuários de demonstração.

## 1. Exploração manual (fluxos principais)

Login → Dashboard → Clientes → Orçamentos → Pedidos → Produtos → Estoque. Dois perfis entram no sistema:

- **Gestor:** toda a operação diária.
- **Administrador:** o mesmo, mais Configurações e Usuários.

Regras observadas que orientaram os cenários:

| Regra | Origem |
|---|---|
| Rotas internas exigem sessão; sem ela o sistema redireciona ao login e, após entrar, volta ao destino pedido. | Proxy do frontend |
| Após **5 falhas** de login na mesma conta (janela de 15 min), a conta fica bloqueada temporariamente; login correto zera o contador. | Limitador de tentativas da API |
| Apenas o Administrador vê "Configurações". | Regra de perfis R1 |
| Cliente exige CPF/CNPJ válido (dígitos verificadores) e ao menos um meio de contato (WhatsApp, telefone ou e-mail). | Regra de domínio do cliente |

## 2. Cenários

| ID | Funcionalidade | Objetivo | Resultado esperado | Prioridade | Automatizar? | Justificativa |
|---|---|---|---|---|---|---|
| CT01 | Login | Confirmar que o Gestor entra com credenciais válidas (positivo) | Vai para `/dashboard` e o menu mostra Dashboard, Clientes, Orçamentos, Pedidos, Produtos e Estoque | Alta | Sim | Porta de entrada de todo o sistema; roda em toda versão (regressão) e é rápido e estável |
| CT02 | Login | Verificar a recusa de senha incorreta (negativo) | Aviso "E-mail ou senha inválidos.", continua em `/login`, sem menu interno | Alta | Sim | Risco de segurança; resultado objetivo e determinístico |
| CT03 | Login / segurança | Verificar o limite de 5 falhas (valor limite + transição de estado) | 1ª a 5ª tentativas: mensagem comum; 6ª: "Muitas tentativas de login sem sucesso…" | Alta | Sim | Manualmente é repetitivo e fácil de errar a contagem. Usa um e-mail inexistente e único por execução, porque o limite é por conta, então não bloqueia os usuários reais nem a repetição da suíte |
| CT04 | Controle de acesso | Garantir que rota protegida exige login e preserva o destino (transição de estado) | `/clientes` sem sessão → `/login?redirect=%2Fclientes`; após login → `/clientes` | Alta | Sim | Falha aqui expõe dados ou quebra o fluxo; estados bem definidos (sem sessão → com sessão) |
| CT05 | Controle de acesso | Menu conforme o perfil (tabela de decisão) | Administrador vê "Configurações"; Gestor não. Ambos veem "Clientes" | Alta | Sim | Regra combinatória perfil × permissão; parametrização evita duplicar código |
| CT06 | Cadastro de cliente | Cadastrar cliente PF com dados válidos (positivo) | Cliente salvo; abre `/clientes/{id}` com o nome no título | Alta | Sim | Fluxo de negócio mais usado no dia a dia; CPF gerado a cada execução permite repetir |
| CT07 | Cadastro de cliente | Recusar cliente sem meio de contato (negativo) | Aviso "Contato: Informe ao menos um meio de contato.", aba Contato selecionada, continua em `/clientes/novo` | Média | Sim | Regra de negócio obrigatória; verificação objetiva pela mensagem e pela aba ativa |
| CT08 | Cadastro de cliente | Recusar CPF com dígito verificador inválido (particionamento: classe "11 dígitos, DV errado") | Aviso "Identificação: CPF inválido…", aba Identificação selecionada, sem salvar | Média | Sim | Classe de equivalência distinta do CPF válido (CT06) e do vazio; protege a qualidade do cadastro |
| CT09 | Proposta em PDF | Conferir que custo e margem não aparecem no PDF enviado ao cliente | PDF sem custo nem margem | Alta | **Não** | O conteúdo do PDF fica fora do DOM do navegador, onde o Selenium atua; a regra já é coberta por testes automatizados do backend (xUnit) |
| CT10 | Envio de proposta por e-mail | Enviar a proposta ao cliente por SMTP | E-mail entregue com o PDF anexo | Média | **Não** | Depende de servidor SMTP externo e gera efeito fora do sistema (e-mail real). Melhor validado manualmente com o capturador smtp4dev do ambiente de revisão |

## 3. Cenários implementados

São 8 cenários automatizados (CT01–CT08), que geram **9 execuções**, porque o CT05 roda uma vez por perfil:

| Tipo | Cenários |
|---|---|
| Positivos | CT01, CT05 (Administrador), CT06 |
| Negativos | CT02, CT03, CT04, CT07, CT08 |
| Tabela de decisão | CT05 |
| Transição de estados | CT03 (conta liberada → bloqueada) e CT04 (sem sessão → com sessão) |
| Técnicas da Aula 2 | CT03 (valor limite: 5ª × 6ª tentativa) e CT08 (particionamento de equivalência) |

Cada teste tem pelo menos uma *assertion* objetiva: URL, texto exato da mensagem, aba selecionada ou presença/ausência de item no menu.

### Tabela de decisão do CT05

| Condição / ação | R1 | R2 |
|---|---|---|
| Perfil é Administrador? | Sim | Não (Gestor) |
| Vê "Configurações" no menu? | Sim | Não |
| Vê "Clientes" no menu? | Sim | Sim |

### Transições de estado cobertas

```
CT04: [sem sessão] --acessa /clientes--> [tela de login c/ redirect] --login válido--> [em /clientes]
CT03: [conta liberada] --falhas 1..5--> [conta liberada, 5 falhas] --6ª tentativa--> [bloqueada (429)]
```

### Valor limite do CT03

| Tentativa | Esperado |
|---|---|
| 5ª (último valor permitido) | "E-mail ou senha inválidos." |
| 6ª (primeiro valor acima do limite) | "Muitas tentativas de login sem sucesso…" |

## 4. Decisões técnicas

- **Page Objects** (`paginas/`) separam os seletores dos testes; se a tela mudar, só o page object muda.
- **Esperas explícitas** (`WebDriverWait`) em toda sincronização; nenhum `sleep`.
- **Setup/teardown:** um navegador novo por teste (fixture `driver`, com `quit()` no fim). O CT02 tem um teardown extra que faz login correto e zera o contador de falhas do Gestor.
- **Dados únicos por execução:** CPF com dígito válido, e-mails e nomes com sufixo de tempo. A suíte pode ser repetida sem limpar o banco.
- **Pré-condição do ambiente:** se o sistema não responder, a execução é interrompida como **BLOQUEADA** (código 4), e não como falha.
