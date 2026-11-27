# Suíte Selenium — Zalek360

Suíte de testes automatizados de interface do Zalek360, feita com **Selenium WebDriver + Python + pytest**.
São 8 cenários e 9 execuções, porque o CT05 roda uma vez por perfil. O planejamento completo (cenários, objetivos, prioridades e justificativas) está em [PLANEJAMENTO.md](PLANEJAMENTO.md).

## O que é preciso

| Item | Versão usada | Observação |
|---|---|---|
| Windows 10/11 | — | Linux e macOS funcionam com os comandos equivalentes (veja abaixo) |
| Python | 3.14.5 | Use Python 3.10 ou mais novo |
| Microsoft Edge ou Google Chrome | atual | O Selenium Manager baixa o driver sozinho; não é preciso instalar chromedriver/msedgedriver |
| Docker Desktop | 29.x | Necessário para subir o Zalek360 localmente |

## Versão do sistema

A suíte depende da Fase 7.4 do Zalek360: o limite de tentativas de login (CT03) e a volta ao destino após o login (CT04). Ela precisa rodar a partir desta branch (`testes/selenium-avaliacao`) ou da `main` depois que a Fase 7.4 e esta suíte forem mescladas. Contra uma versão anterior, o CT03 falha.

Para gerar um pacote com apenas os arquivos versionados, sem `.env.revisao` nem `.venv`, execute na raiz do repositório:

```powershell
git archive --format=zip -o ..\Zalek360-selenium.zip HEAD
```

## Passo a passo (Windows, PowerShell)

Todos os comandos partem da **raiz do repositório**, a pasta que contém `docker-compose.yml`.

### 1. Subir o sistema testado

Com o Docker Desktop aberto e mostrando *Engine running*, execute:

```powershell
powershell -ExecutionPolicy Bypass -File .\testes-selenium\subir-ambiente.ps1
```

Na primeira vez, o script:

- cria o arquivo `.env.revisao` a partir do modelo `.env.revisao.example`;
- gera senhas aleatórias, sem exibi-las;
- sobe o ambiente de revisão.

A primeira subida compila o backend e o frontend e pode levar alguns minutos.
Quando terminar, o sistema estará em **http://localhost:3100**. Usuários de demonstração:

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador | `admin@zalekpersonalizados.com.br` | `Zalek@2026` |
| Gestor | `gestao@zalekpersonalizados.com.br` | `Zalek@2026` |

### 2. Executar os testes

```powershell
cd testes-selenium
powershell -ExecutionPolicy Bypass -File .\executar-testes.ps1
```

O script cria o ambiente virtual `.venv` (se ainda não existir), instala as versões fixadas em `requirements.txt` e roda o pytest.

Se preferir fazer manualmente, os comandos são os mesmos vistos em aula:

```powershell
cd testes-selenium
py -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
python -m pytest
```

No VS Code, selecione o interpretador da `.venv`: `Ctrl+Shift+P` → **Python: Select Interpreter** → `.venv`.

### 3. Ver o resultado

- **Terminal:** lista cada teste com `PASSED` ou `FAILED` e o total no final.
- **`relatorios/relatorio.html`:** relatório autocontido; basta abrir no navegador. Para cada teste mostra o cenário, a duração, a URL final e um screenshot da tela no fim do teste. No topo aparecem o navegador, a URL e a ferramenta usados.
- **`relatorios/junit.xml`:** o mesmo resultado em formato JUnit, gerado a cada execução e não versionado.
- **`relatorios/capturas/`:** screenshot de cada teste que falhar (criada somente quando há falha).

A execução que serve de evidência da entrega está versionada em `relatorios/relatorio.html` (Edge com janela visível, 9/9 aprovados). Uma nova execução sobrescreve esse arquivo.

## Opções úteis

| Comando | Efeito |
|---|---|
| `python -m pytest --headless` | Executa sem abrir a janela do navegador |
| `python -m pytest --navegador chrome` | Usa o Chrome (padrão: `edge`; também aceita `firefox`) |
| `python -m pytest --url http://outro-endereco:3100` | Aponta para outro endereço do frontend |
| `python -m pytest -m negativo` | Só os testes negativos (marcadores: `positivo`, `negativo`, `tabela_decisao`, `transicao_estado`, `valor_limite`, `particionamento`) |
| `python -m pytest -k ct05` | Só um cenário |

As mesmas opções podem ser passadas ao `executar-testes.ps1`, por exemplo `.\executar-testes.ps1 --headless -m positivo`.

Também é possível usar as variáveis de ambiente `ZALEK_URL`, `NAVEGADOR`, `HEADLESS=1` e `ZALEK_SENHA`.

## Estrutura

```
testes-selenium/
├── conftest.py          # opções, navegador (setup/teardown), pré-condição do ambiente, evidências no relatório
├── dados.py             # usuários de demonstração e geração de CPF/e-mail únicos por execução
├── paginas/             # Page Objects (seletores e ações de cada tela)
│   ├── base.py          #   esperas explícitas comuns
│   ├── login.py
│   ├── menu.py
│   └── cliente.py
├── testes/
│   ├── test_login.py    # CT01, CT02, CT03
│   ├── test_acesso.py   # CT04, CT05
│   └── test_clientes.py # CT06, CT07, CT08
├── relatorios/          # evidências da execução (HTML + JUnit)
├── PLANEJAMENTO.md
├── executar-testes.ps1
├── subir-ambiente.ps1
├── pytest.ini
└── requirements.txt
```

## Repetibilidade

- Cada execução gera CPF, e-mail e nomes novos, então a suíte pode ser rodada várias vezes seguidas sem limpar o banco.
- O teste de bloqueio (CT03) usa um e-mail inexistente diferente a cada vez, porque o limite de tentativas é por conta. Assim ele nunca bloqueia os usuários de demonstração.
- O CT02 erra a senha do Gestor uma vez e, no teardown, faz um login correto, que zera o contador de falhas da conta.
- O primeiro teste checa se o sistema está no ar. Se ele não responder, a execução para com a mensagem **BLOQUEADO** (código de saída 4), e não como falha.

## Linux / macOS

```bash
cp .env.revisao.example .env.revisao
# preencha no .env.revisao:
#   POSTGRES_PASSWORD  (16+ caracteres)
#   JWT_CHAVE          (32+ caracteres)
#   SENHA_USUARIOS_DEMO=Zalek@2026
docker compose -p zalek360-revisao --env-file .env.revisao up --build -d --wait
cd testes-selenium
python3 -m venv .venv && . .venv/bin/activate
python -m pip install -r requirements.txt
python -m pytest --navegador chrome
```

## Encerrar o ambiente

```powershell
docker compose -p zalek360-revisao --env-file .env.revisao stop
```

Use `stop`, que mantém os dados. Não use `down -v`.
