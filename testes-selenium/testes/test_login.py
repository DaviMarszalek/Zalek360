"""Autenticação: login válido, credencial incorreta e bloqueio por tentativas."""
import pytest

from dados import SENHA_DEMO, USUARIOS, email_inexistente
from paginas.login import PaginaLogin
from paginas.menu import MenuPrincipal

MENSAGEM_CREDENCIAL_INVALIDA = "E-mail ou senha inválidos."
MENSAGEM_BLOQUEIO = "Muitas tentativas de login sem sucesso"


@pytest.mark.positivo
def test_ct01_login_valido_do_gestor(driver, url_base):
    """CT01 — Gestor com credenciais válidas entra e vê os módulos do dia a dia."""
    PaginaLogin(driver, url_base).entrar(USUARIOS["Gestor"], SENHA_DEMO)

    menu = MenuPrincipal(driver, url_base)
    menu.esperar_url_termina("/dashboard")
    rotulos = menu.esperar_carregar().rotulos()
    for modulo in ["Dashboard", "Clientes", "Orçamentos", "Pedidos", "Produtos", "Estoque"]:
        assert modulo in rotulos, f"Módulo '{modulo}' ausente no menu: {rotulos}"


@pytest.fixture
def zerar_falhas_do_gestor(driver, url_base):
    """Teardown: um login correto zera o contador de falhas da conta do Gestor,
    para que repetir a suíte nunca acumule falhas até o bloqueio."""
    yield
    driver.delete_all_cookies()
    PaginaLogin(driver, url_base).entrar(USUARIOS["Gestor"], SENHA_DEMO)


@pytest.mark.negativo
def test_ct02_login_com_senha_incorreta(driver, url_base, zerar_falhas_do_gestor):
    """CT02 — Senha incorreta é recusada com mensagem e o usuário continua no login."""
    pagina = PaginaLogin(driver, url_base).abrir()

    mensagem = pagina.tentar_e_ler_erro(USUARIOS["Gestor"], "SenhaErrada@123")

    assert mensagem == MENSAGEM_CREDENCIAL_INVALIDA
    assert pagina.caminho_atual().startswith("/login")
    assert driver.find_elements(*MenuPrincipal.NAV) == [], "O menu interno não deveria aparecer sem login"


@pytest.mark.negativo
@pytest.mark.valor_limite
@pytest.mark.transicao_estado
def test_ct03_bloqueio_apos_cinco_falhas(driver, url_base):
    """CT03 — Até a 5ª falha a mensagem é comum; a 6ª tentativa já encontra a conta bloqueada."""
    # Conta inexistente e única por execução: o limite é por e-mail, então o teste
    # não bloqueia os usuários de demonstração e pode ser repetido.
    email = email_inexistente()
    pagina = PaginaLogin(driver, url_base).abrir()

    for tentativa in range(1, 6):
        mensagem = pagina.tentar_e_ler_erro(email, "SenhaErrada@123")
        assert mensagem == MENSAGEM_CREDENCIAL_INVALIDA, f"Tentativa {tentativa}: '{mensagem}'"

    mensagem = pagina.tentar_e_ler_erro(email, "SenhaErrada@123")
    assert MENSAGEM_BLOQUEIO in mensagem, f"6ª tentativa deveria estar bloqueada: '{mensagem}'"
    assert pagina.caminho_atual().startswith("/login")
