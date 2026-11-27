"""Configuração da suíte: opções de linha de comando, navegador (setup/teardown),
pré-condição do ambiente e evidências no relatório HTML."""
import html
import os
import re
import urllib.error
import urllib.request
from datetime import datetime
from pathlib import Path

import pytest
from selenium import webdriver

from dados import SENHA_DEMO, USUARIOS
from paginas.login import PaginaLogin

PASTA_CAPTURAS = Path(__file__).parent / "relatorios" / "capturas"


def pytest_addoption(parser):
    grupo = parser.getgroup("zalek360")
    grupo.addoption("--url", default=os.environ.get("ZALEK_URL", "http://localhost:3100"),
                    help="Endereço do frontend do Zalek360 (padrão: ambiente de revisão, porta 3100).")
    grupo.addoption("--navegador", default=os.environ.get("NAVEGADOR", "edge"),
                    choices=["edge", "chrome", "firefox"], help="Navegador usado nos testes.")
    grupo.addoption("--headless", action="store_true", default=os.environ.get("HEADLESS") == "1",
                    help="Executa sem abrir a janela do navegador.")


def pytest_configure(config):
    try:
        from pytest_metadata.plugin import metadata_key
    except ImportError:
        return
    meta = config.stash[metadata_key]
    meta["Sistema testado"] = "Zalek360"
    meta["URL"] = config.getoption("--url")
    meta["Navegador"] = config.getoption("--navegador") + (" (headless)" if config.getoption("--headless") else "")
    meta["Ferramenta"] = "Selenium WebDriver + Python + pytest"
    meta["Início da execução"] = datetime.now().strftime("%d/%m/%Y %H:%M:%S")


def pytest_html_report_title(report):
    report.title = "Zalek360 — suíte Selenium"


# ---------- pré-condição: o sistema precisa estar no ar ----------

@pytest.fixture(scope="session")
def url_base(pytestconfig) -> str:
    url = pytestconfig.getoption("--url").rstrip("/")
    try:
        urllib.request.urlopen(url + "/login", timeout=10)
    except (urllib.error.URLError, OSError) as erro:
        # Sem o sistema no ar os testes não podem ser concluídos: é bloqueio, não falha.
        pytest.exit(f"BLOQUEADO: o Zalek360 não respondeu em {url} ({erro}). "
                    "Suba o ambiente de revisão antes de executar a suíte (veja o README).", returncode=4)
    return url


# ---------- setup e teardown do navegador ----------

def _criar_driver(nome: str, headless: bool):
    if nome == "firefox":
        opcoes = webdriver.FirefoxOptions()
        if headless:
            opcoes.add_argument("-headless")
        driver = webdriver.Firefox(options=opcoes)
        driver.set_window_size(1366, 900)
        return driver

    opcoes = webdriver.EdgeOptions() if nome == "edge" else webdriver.ChromeOptions()
    if headless:
        opcoes.add_argument("--headless=new")
    opcoes.add_argument("--window-size=1366,900")
    # Evita que o gerenciador de senhas abra pop-ups por cima da página durante o login.
    opcoes.add_experimental_option("prefs", {
        "credentials_enable_service": False,
        "profile.password_manager_enabled": False,
        "profile.password_manager_leak_detection": False,
    })
    return webdriver.Edge(options=opcoes) if nome == "edge" else webdriver.Chrome(options=opcoes)


@pytest.fixture
def driver(pytestconfig, url_base):
    navegador = _criar_driver(pytestconfig.getoption("--navegador"), pytestconfig.getoption("--headless"))
    yield navegador
    navegador.quit()


@pytest.fixture
def entrar(driver, url_base):
    """Faz login com um dos perfis de demonstração e devolve a página de login usada."""
    def _entrar(perfil: str = "Gestor"):
        pagina = PaginaLogin(driver, url_base)
        pagina.entrar(USUARIOS[perfil], SENHA_DEMO)
        return pagina
    return _entrar


# ---------- evidências no relatório ----------

@pytest.hookimpl(hookwrapper=True)
def pytest_runtest_makereport(item, call):
    resultado = yield
    relatorio = resultado.get_result()
    doc = (item.function.__doc__ or "").strip().splitlines()
    relatorio.cenario = doc[0] if doc else ""
    if relatorio.when != "call":
        return
    driver = item.funcargs.get("driver")
    plugin_html = item.config.pluginmanager.getplugin("html")
    if driver is None or plugin_html is None:
        return
    try:
        imagem = driver.get_screenshot_as_base64()
    except Exception:  # o navegador pode ter caído junto com o teste
        return
    extras = getattr(relatorio, "extras", [])
    extras.append(plugin_html.extras.url(driver.current_url, name="URL no fim do teste"))
    extras.append(plugin_html.extras.png(imagem, name="Tela no fim do teste"))
    relatorio.extras = extras
    if relatorio.failed:
        PASTA_CAPTURAS.mkdir(parents=True, exist_ok=True)
        nome = re.sub(r"[^\w.-]+", "_", item.nodeid)
        driver.save_screenshot(str(PASTA_CAPTURAS / f"{nome}.png"))


def pytest_html_results_table_header(cells):
    cells.insert(2, "<th>Cenário</th>")


def pytest_html_results_table_row(report, cells):
    cells.insert(2, f"<td>{html.escape(getattr(report, 'cenario', ''))}</td>")
