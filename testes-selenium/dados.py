import os
import random
import time

SENHA_DEMO = os.environ.get("ZALEK_SENHA", "Zalek@2026")

USUARIOS = {
    "Administrador": "admin@zalekpersonalizados.com.br",
    "Gestor": "gestao@zalekpersonalizados.com.br",
}


def sufixo_unico() -> str:
    return f"{int(time.time() * 1000)}{random.randint(100, 999)}"


def cpf_valido() -> str:
    base = [random.randint(0, 9) for _ in range(9)]
    while len(set(base)) == 1:
        base = [random.randint(0, 9) for _ in range(9)]
    for peso_inicial in (10, 11):
        soma = sum(d * p for d, p in zip(base, range(peso_inicial, 1, -1)))
        resto = soma * 10 % 11
        base.append(0 if resto == 10 else resto)
    return "".join(map(str, base))


def cpf_com_digito_invalido() -> str:
    cpf = cpf_valido()
    ultimo = (int(cpf[-1]) + 1) % 10
    return cpf[:-1] + str(ultimo)


def email_inexistente() -> str:
    return f"selenium.{sufixo_unico()}@exemplo.test"
