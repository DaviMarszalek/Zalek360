import { expect, test } from "@playwright/test";

const usuarios = [
  { perfil: "Administrador", email: "admin@zalekpersonalizados.com.br" },
  { perfil: "Gestor", email: "gestao@zalekpersonalizados.com.br" },
];

for (const usuario of usuarios) {
  test(`login de ${usuario.perfil}`, async ({ page }) => {
    await page.goto("/login");
    await page.getByLabel("E-mail").fill(usuario.email);
    await page.getByRole("textbox", { name: "Senha" }).fill("Zalek@2026");
    await page.getByRole("button", { name: "Entrar" }).click();

    await expect(page).toHaveURL(/\/dashboard$/);
    const menu = page.getByRole("navigation", { name: "Menu principal" });
    await expect(menu).toBeVisible();

    for (const modulo of ["Dashboard", "Clientes", "Orçamentos", "Pedidos", "Estoque"])
      await expect(menu.getByRole("link", { name: modulo })).toBeVisible();

    await expect(menu.locator("text=Consulta")).toHaveCount(0);

    await expect(page.getByRole("button", { name: "Novo cliente" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Novo orçamento" })).toBeVisible();
  });
}

test("Gestor cadastra cliente, cria e edita orçamento completo e recebe 403 em usuários", async ({ page }) => {
  await page.goto("/login");
  await page.getByLabel("E-mail").fill("gestao@zalekpersonalizados.com.br");
  await page.getByRole("textbox", { name: "Senha" }).fill("Zalek@2026");
  await page.getByRole("button", { name: "Entrar" }).click();
  await expect(page).toHaveURL(/\/dashboard$/);

  const menu = page.getByRole("navigation", { name: "Menu principal" });
  for (const modulo of ["Configurações", "Usuários"])
    await expect(menu.getByRole("link", { name: modulo })).toHaveCount(0);

  // CPF exclusivo por execução, com dígitos verificadores válidos.
  let cpf = String(Date.now()).slice(-9);
  for (let peso = 10; peso <= 11; peso++) {
    const soma = [...cpf].reduce((total, digito, i) => total + Number(digito) * (peso - i), 0);
    const resto = (soma * 10) % 11;
    cpf += resto === 10 ? "0" : String(resto);
  }
  const nome = `Cliente E2E Gestor ${cpf}`;
  await page.goto("/clientes/novo");
  await page.getByRole("radio", { name: "Pessoa física" }).click();
  await page.getByLabel("CPF", { exact: false }).fill(cpf);
  await page.getByLabel("Nome completo").fill(nome);
  await page.getByRole("tab", { name: "Contato", exact: true }).click();
  await page.getByLabel("E-mail", { exact: true }).fill(`e2e.${cpf}@example.com`);
  const clienteSalvo = page.waitForResponse(r => r.url().endsWith("/api/clientes") && r.request().method() === "POST");
  await page.getByRole("button", { name: "Salvar cliente", exact: true }).click();
  const respostaCliente = await clienteSalvo;
  expect(respostaCliente.status()).toBe(201);
  const cliente = await respostaCliente.json();
  await expect(page).toHaveURL(new RegExp(`/clientes/${cliente.id}$`));
  await expect(page.getByText("Acesso somente para consulta")).toHaveCount(0);

  await page.goto("/orcamentos/novo");
  await page.getByRole("textbox", { name: "Cliente", exact: true }).fill(nome);
  await page.getByRole("button", { name: new RegExp(nome) }).click();
  await page.getByRole("button", { name: "Adicionar item" }).click();
  // Os campos do orçamento seguem o componente Campo existente.
  const campo = (rotulo: string) => page.locator("label").filter({ hasText: rotulo }).locator("..");
  const produtos = await page.request.get("/api/produtos");
  expect(produtos.status()).toBe(200);
  const [produto] = await produtos.json();
  expect(produto).toBeDefined();
  await campo("Produto").getByRole("combobox").selectOption(produto.id);
  await campo("Quantidade").getByRole("textbox").fill("2");
  await campo("Valor unitário (R$)").getByRole("textbox").fill("25,00");
  await campo("Tipo de personalização").getByRole("combobox").selectOption({ index: 1 });
  await campo("Cor da peça").getByRole("textbox").fill("Preta");
  await campo("Condição de pagamento").getByRole("combobox").selectOption("");
  const orcamentoSalvo = page.waitForResponse(r => r.url().endsWith("/api/orcamentos") && r.request().method() === "POST");
  await page.getByRole("button", { name: "Salvar orçamento", exact: true }).click();
  const respostaOrcamento = await orcamentoSalvo;
  expect(respostaOrcamento.status()).toBe(201);
  const orcamento = await respostaOrcamento.json();
  const usuario = await (await page.request.get("/api/auth/me")).json();
  expect(orcamento.responsavelId).toBe(usuario.id);
  expect(orcamento.condicaoPagamento).toBeNull();
  expect(orcamento.resumo.valorTotal).toBe(50);
  await expect(page).toHaveURL(new RegExp(`/orcamentos/${orcamento.id}$`));
  await expect(page.getByText("A combinar", { exact: true })).toBeVisible();
  await expect(page.getByText("Acesso somente para consulta")).toHaveCount(0);

  await page.goto(`/orcamentos/${orcamento.id}/editar`);
  await campo("Observações comerciais").getByRole("textbox").fill("Editado pelo Gestor no E2E");
  const orcamentoEditado = page.waitForResponse(r => r.url().endsWith(`/api/orcamentos/${orcamento.id}`) && r.request().method() === "PUT");
  await page.getByRole("button", { name: "Salvar alterações", exact: true }).click();
  const respostaEdicao = await orcamentoEditado;
  expect(respostaEdicao.status()).toBe(200);
  expect((await respostaEdicao.json()).responsavelId).toBe(usuario.id);
  await expect(page).toHaveURL(new RegExp(`/orcamentos/${orcamento.id}$`));

  for (const resposta of [await page.request.get("/api/usuarios"), await page.request.post("/api/usuarios", { data: {} })]) {
    expect(resposta.status()).toBe(403);
    expect(await resposta.json()).toMatchObject({ status: 403, code: "ACESSO_NEGADO" });
  }
});

test("Administrador vê o erro da primeira aba inválida no cadastro de cliente", async ({ page }) => {
  await page.goto("/login");
  await page.getByLabel("E-mail").fill("admin@zalekpersonalizados.com.br");
  await page.getByRole("textbox", { name: "Senha" }).fill("Zalek@2026");
  await page.getByRole("button", { name: "Entrar" }).click();
  await expect(page).toHaveURL(/\/dashboard$/);

  await page.goto("/clientes/novo");
  await page.getByLabel("CNPJ").fill("38.221.907/0001-53");
  await page.getByLabel("Razão social").fill("Cliente sem contato de teste");
  await page.getByRole("button", { name: "Salvar cliente", exact: true }).click();

  await expect(page.getByRole("alert").filter({ hasText: "Contato: Informe ao menos um meio de contato." }))
    .toBeVisible();
  const abaContato = page.getByRole("tab", { name: /Contato/ });
  await expect(abaContato).toHaveAttribute("aria-selected", "true");
  await expect(abaContato).toContainText("1");
});
