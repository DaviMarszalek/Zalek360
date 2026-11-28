import re

import pytest

from dados import cpf_com_digito_invalido, cpf_valido, sufixo_unico
from paginas.cliente import DetalheCliente, FormularioCliente

UUID = r"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}"


@pytest.fixture
def formulario(entrar, driver, url_base):
    entrar("Gestor")
    return FormularioCliente(driver, url_base).abrir().pessoa_fisica()


@pytest.mark.positivo
def test_ct06_cadastrar_cliente_pessoa_fisica(formulario, driver, url_base):
    nome = f"Cliente Selenium {sufixo_unico()}"
    formulario.informar_cpf(cpf_valido()).informar_nome(nome)
    formulario.informar_email(f"selenium.{sufixo_unico()}@exemplo.test")

    formulario.salvar()

    detalhe = DetalheCliente(driver, url_base)
    detalhe.espera.until(lambda d: re.search(rf"/clientes/{UUID}$", d.current_url))
    assert nome in detalhe.titulo()


@pytest.mark.negativo
def test_ct07_cliente_sem_meio_de_contato(formulario):
    formulario.informar_cpf(cpf_valido()).informar_nome(f"Cliente sem contato {sufixo_unico()}")

    formulario.salvar()

    assert "Contato: Informe ao menos um meio de contato." in formulario.aviso_erro()
    assert formulario.aba_selecionada("Contato")
    assert formulario.caminho_atual() == "/clientes/novo"


@pytest.mark.negativo
@pytest.mark.particionamento
def test_ct08_cpf_com_digito_verificador_invalido(formulario):
    formulario.informar_cpf(cpf_com_digito_invalido()).informar_nome(f"Cliente CPF inválido {sufixo_unico()}")
    formulario.informar_email(f"selenium.{sufixo_unico()}@exemplo.test")

    formulario.salvar()

    assert "Identificação: CPF inválido" in formulario.aviso_erro()
    assert formulario.aba_selecionada("Identificação")
    assert formulario.caminho_atual() == "/clientes/novo"
