from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys

from paginas.base import PaginaBase


class FormularioCliente(PaginaBase):
    caminho = "/clientes/novo"

    PESSOA_FISICA = (By.XPATH, "//button[@role='radio'][contains(., 'Pessoa física')]")
    DOCUMENTO = (By.ID, "documento")
    ERRO_DOCUMENTO = (By.ID, "documento-mensagem")
    NOME = (By.ID, "nome")
    EMAIL = (By.ID, "email")
    SALVAR = (By.XPATH, "//button[@type='submit'][normalize-space()='Salvar cliente']")
    AVISO_ERRO = (By.CSS_SELECTOR, "form > div[role='alert']")

    @staticmethod
    def aba(rotulo: str):
        return (By.XPATH, f"//button[@role='tab'][starts-with(normalize-space(.), '{rotulo}')]")

    def abrir(self):
        super().abrir()
        self.visivel(self.DOCUMENTO)
        return self

    def pessoa_fisica(self):
        self.clicavel(self.PESSOA_FISICA).click()
        self.espera.until(lambda d: d.find_element(*self.PESSOA_FISICA).get_attribute("aria-checked") == "true")
        return self

    def informar_cpf(self, cpf: str):
        self.digitar(self.DOCUMENTO, cpf).send_keys(Keys.TAB)
        return self

    def informar_nome(self, nome: str):
        self.digitar(self.NOME, nome)
        return self

    def ir_para_aba(self, rotulo: str):
        self.clicavel(self.aba(rotulo)).click()
        return self

    def aba_selecionada(self, rotulo: str) -> bool:
        return self.driver.find_element(*self.aba(rotulo)).get_attribute("aria-selected") == "true"

    def informar_email(self, email: str):
        self.ir_para_aba("Contato")
        self.digitar(self.EMAIL, email)
        return self

    def salvar(self):
        self.clicavel(self.SALVAR).click()

    def erro_documento(self) -> str:
        return self.visivel(self.ERRO_DOCUMENTO).text

    def aviso_erro(self) -> str:
        return self.visivel(self.AVISO_ERRO).text


class DetalheCliente(PaginaBase):
    TITULO = (By.TAG_NAME, "h1")

    def titulo(self) -> str:
        return self.visivel(self.TITULO).text
