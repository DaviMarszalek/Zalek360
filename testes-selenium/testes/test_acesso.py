import pytest

from dados import SENHA_DEMO, USUARIOS
from paginas.login import PaginaLogin
from paginas.menu import MenuPrincipal


@pytest.mark.negativo
@pytest.mark.transicao_estado
def test_ct04_rota_protegida_exige_login_e_retorna_ao_destino(driver, url_base):
    driver.get(url_base + "/clientes")

    login = PaginaLogin(driver, url_base)
    login.esperar_url_contem("/login")
    assert "redirect=%2Fclientes" in driver.current_url, driver.current_url

    login.preencher(USUARIOS["Gestor"], SENHA_DEMO).enviar()
    login.esperar_url_termina("/clientes")
    assert login.caminho_atual().split("?")[0] == "/clientes"


@pytest.mark.tabela_decisao
@pytest.mark.parametrize("perfil, ve_configuracoes", [
    ("Administrador", True),
    ("Gestor", False),
])
def test_ct05_menu_conforme_perfil(entrar, driver, url_base, perfil, ve_configuracoes):
    entrar(perfil)
    rotulos = MenuPrincipal(driver, url_base).esperar_carregar().rotulos()

    assert ("Configurações" in rotulos) is ve_configuracoes, rotulos
    assert "Clientes" in rotulos
