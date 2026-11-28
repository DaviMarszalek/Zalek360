from selenium.webdriver.common.by import By

from paginas.base import PaginaBase


class MenuPrincipal(PaginaBase):
    NAV = (By.CSS_SELECTOR, "nav[aria-label='Menu principal']")
    SAIR = (By.XPATH, "//button[normalize-space()='Sair']")

    def esperar_carregar(self):
        self.visivel(self.NAV)
        return self

    def rotulos(self) -> list[str]:
        nav = self.visivel(self.NAV)
        return [link.text.strip() for link in nav.find_elements(By.TAG_NAME, "a") if link.text.strip()]

    def sair(self):
        self.clicavel(self.SAIR).click()
        self.esperar_url_termina("/login")
