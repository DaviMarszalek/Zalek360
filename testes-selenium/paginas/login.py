from selenium.webdriver.common.by import By
from selenium.webdriver.support import expected_conditions as EC

from paginas.base import PaginaBase


class PaginaLogin(PaginaBase):
    caminho = "/login"

    EMAIL = (By.ID, "email")
    SENHA = (By.ID, "senha")
    ENTRAR = (By.CSS_SELECTOR, "form button[type='submit']")
    AVISO_ERRO = (By.CSS_SELECTOR, "form div[role='alert']")

    def preencher(self, email: str, senha: str):
        self.digitar(self.EMAIL, email)
        self.digitar(self.SENHA, senha)
        return self

    def enviar(self):
        self.clicavel(self.ENTRAR).click()

    def entrar(self, email: str, senha: str):
        """Login completo: só retorna depois que o sistema sair da tela de login."""
        self.abrir().preencher(email, senha).enviar()
        self.espera.until(lambda d: "/login" not in d.current_url)

    def tentar_e_ler_erro(self, email: str, senha: str) -> str:
        """Envia credenciais que devem ser recusadas e devolve o texto do aviso exibido.

        Se já houver um aviso na tela (tentativa anterior), espera ele sumir antes de
        ler o novo, para não confundir a mensagem antiga com a resposta atual.
        """
        anteriores = self.driver.find_elements(*self.AVISO_ERRO)
        self.preencher(email, senha).enviar()
        if anteriores:
            self.espera.until(EC.staleness_of(anteriores[0]))
        return self.visivel(self.AVISO_ERRO).text
