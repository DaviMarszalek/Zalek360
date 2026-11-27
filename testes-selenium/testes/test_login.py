import pytest

from dados import SENHA_DEMO, USUARIOS, email_inexistente
from paginas.login import PaginaLogin
from paginas.menu import MenuPrincipal

MENSAGEM_CREDENCIAL_INVALIDA = "E-mail ou senha inválidos."
MENSAGEM_BLOQUEIO = "Muitas tentativas de login sem sucesso"


@pytest.mark.positivo
def test_ct01_login_valido_do_gestor(driver, url_base):
    PaginaLogin(driver, url_base).entrar(USUARIOS["Gestor"], SENHA_DEMO)

    menu = MenuPrincipal(driver, url_base)
    menu.esperar_url_termina("/dashboard")
    rotulos = menu.esperar_carregar().rotulos()
    for modulo in ["Dashboard", "Clientes", "Orçamentos", "Pedidos", "Produtos", "Estoque"]:
        assert modulo in rotulos, f"Módulo '{modulo}' ausente no menu: {rotulos}"


@pytest.fixture
def zerar_falhas_do_gestor(driver, url_base):
    yield
    driver.delete_all_cookies()
    PaginaLogin(driver, url_base).entrar(USUARIOS["Gestor"], SENHA_DEMO)


@pytest.mark.negativo
def test_ct02_login_com_senha_incorreta(driver, url_base, zerar_falhas_do_gestor):
    pagina = PaginaLogin(driver, url_base).abrir()

    mensagem = pagina.tentar_e_ler_erro(USUARIOS["Gestor"], "SenhaErrada@123")

    assert mensagem == MENSAGEM_CREDENCIAL_INVALIDA
    assert pagina.caminho_atual().startswith("/login")
    assert driver.find_elements(*MenuPrincipal.NAV) == [], "O menu interno não deveria aparecer sem login"


@pytest.mark.negativo
@pytest.mark.valor_limite
@pytest.mark.transicao_estado
def test_ct03_bloqueio_apos_cinco_falhas(driver, url_base):
    email = email_inexistente()
    pagina = PaginaLogin(driver, url_base).abrir()

    for tentativa in range(1, 6):
        mensagem = pagina.tentar_e_ler_erro(email, "SenhaErrada@123")
        assert mensagem == MENSAGEM_CREDENCIAL_INVALIDA, f"Tentativa {tentativa}: '{mensagem}'"

    mensagem = pagina.tentar_e_ler_erro(email, "SenhaErrada@123")
    assert MENSAGEM_BLOQUEIO in mensagem, f"6ª tentativa deveria estar bloqueada: '{mensagem}'"
    assert pagina.caminho_atual().startswith("/login")
