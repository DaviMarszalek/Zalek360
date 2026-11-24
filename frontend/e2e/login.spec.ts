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

    if (usuario.perfil === "Administrador") {
      await expect(page).toHaveURL(/\/dashboard$/);
      await expect(page.getByRole("navigation", { name: "Menu principal" })).toBeVisible();
      await expect(page.getByRole("link", { name: "Dashboard" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Novo cliente" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Novo orçamento" })).toBeVisible();
    } else {
      await expect(page).toHaveURL(/\/estoque$/);
      const menu = page.getByRole("navigation", { name: "Menu principal" });
      await expect(menu.getByRole("link", { name: "Dashboard" })).toHaveCount(0);
      for (const modulo of ["Clientes", "Orçamentos", "Pedidos"])
        await expect(menu.getByRole("link", { name: new RegExp(`${modulo}.*Consulta`) })).toBeVisible();
      await expect(page.getByRole("button", { name: "Nova matéria-prima" })).toBeVisible();
      await expect(page.getByRole("button", { name: "Planejar compra" }).first()).toBeVisible();
      await expect(page.getByRole("button", { name: "Produtos personalizados" })).toBeVisible();
      await page.goto("/clientes");
      await expect(page.getByRole("button", { name: "Novo cliente" })).toHaveCount(0);
      await page.goto("/orcamentos");
      await expect(page.getByRole("button", { name: "Novo orçamento" })).toHaveCount(0);
      await page.goto("/pedidos");
      await expect(page.getByRole("button", { name: "Novo pedido" })).toHaveCount(0);
    }
  });
}

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
