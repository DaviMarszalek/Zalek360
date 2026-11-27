"""Comportamento comum aos page objects: navegação e esperas explícitas."""
from selenium.webdriver.remote.webdriver import WebDriver
from selenium.webdriver.remote.webelement import WebElement
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.support.ui import WebDriverWait

ESPERA_PADRAO = 15


class PaginaBase:
    caminho = "/"

    def __init__(self, driver: WebDriver, url_base: str):
        self.driver = driver
        self.url_base = url_base.rstrip("/")
        self.espera = WebDriverWait(driver, ESPERA_PADRAO)

    def abrir(self):
        self.driver.get(self.url_base + self.caminho)
        return self

    def visivel(self, localizador) -> WebElement:
        return self.espera.until(EC.visibility_of_element_located(localizador))

    def clicavel(self, localizador) -> WebElement:
        return self.espera.until(EC.element_to_be_clickable(localizador))

    def digitar(self, localizador, texto: str):
        campo = self.visivel(localizador)
        campo.clear()
        campo.send_keys(texto)
        return campo

    def esperar_url_contem(self, trecho: str):
        self.espera.until(EC.url_contains(trecho))

    def esperar_url_termina(self, final: str):
        self.espera.until(lambda d: d.current_url.split("?")[0].rstrip("/").endswith(final))

    def caminho_atual(self) -> str:
        return self.driver.current_url.removeprefix(self.url_base)
