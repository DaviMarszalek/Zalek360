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
    await expect(page.getByRole("navigation", { name: "Menu principal" })).toBeVisible();
  });
}
