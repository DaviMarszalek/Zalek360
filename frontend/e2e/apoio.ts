import { expect, type Locator, type Page } from "@playwright/test";
import { mkdir } from "node:fs/promises";
import path from "node:path";

export const png = Buffer.from("iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAHElEQVR4nGNQc7yDFX2dz4UVMYxqGNXANWw1AABgfnCQSrM/TAAAAABJRU5ErkJggg==", "base64");
export async function entrar(page: Page, admin = false) {
  await page.goto("/login");
  await page.getByLabel("E-mail").fill(admin ? "admin@zalekpersonalizados.com.br" : "gestao@zalekpersonalizados.com.br");
  await page.getByRole("textbox", { name: "Senha" }).fill("Zalek@2026");
  await page.getByRole("button", { name: "Entrar", exact: true }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
}
export function cpfNovo() {
  let cpf = String(Date.now()).slice(-9);
  for (let peso = 10; peso <= 11; peso++) {
    const resto = [...cpf].reduce((t, d, i) => t + Number(d) * (peso - i), 0) * 10 % 11;
    cpf += resto === 10 ? "0" : String(resto);
  }
  return cpf;
}
export async function capturar(page: Page, nome: string) {
  const pasta = path.resolve("../data/revisao-fase6"); await mkdir(pasta, { recursive: true });
  await expect(page.locator('[aria-live="polite"]').getByRole("button", { name: "Fechar", exact: true })).toHaveCount(0, { timeout: 7000 });
  for (const width of [360, 1280]) {
    await page.setViewportSize({ width, height: 900 });
    await page.evaluate(() => window.scrollTo(0, 0));
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await page.screenshot({ path: path.join(pasta, `${nome}-${width}.png`), fullPage: true });
  }
}
export async function tecladoModal(page: Page, dialogo: Locator) {
  await expect(dialogo).toBeVisible();
  await expect(dialogo.getByRole("status").filter({ hasText: /Carregando|Buscando/ })).toHaveCount(0);
  await expect.poll(() => dialogo.evaluate(el => el.contains(document.activeElement))).toBe(true);
  // Exercita os dois limites de tabulação, inclusive botões do rodapé.
  const focaveis = dialogo.locator('button:not([disabled]), input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled]), a[href]');
  const primeiro = focaveis.first(), ultimo = focaveis.last();
  await ultimo.focus(); await page.keyboard.press("Tab"); await expect(primeiro).toBeFocused();
  await primeiro.focus(); await page.keyboard.press("Shift+Tab"); await expect(ultimo).toBeFocused();
}
export async function conferirModal(page: Page, botao: Locator, titulo: string | RegExp) {
  await botao.click(); const dialogo = page.getByRole("dialog", { name: titulo });
  await tecladoModal(page, dialogo);
  await page.keyboard.press("Escape"); await expect(dialogo).toHaveCount(0);
  await expect(botao).toBeFocused();
}
export async function criarOrcamento(page: Page) {
  const cpf = cpfNovo();
  const cadastrado = await page.request.post("/api/clientes", { data: { tipoPessoa: "PessoaFisica", documento: cpf, nomeRazaoSocial: `Cliente revisão ${cpf}`, email: `revisao.${cpf}@example.test` } });
  expect(cadastrado.status()).toBe(201); const cliente = await cadastrado.json();
  const produtos = await (await page.request.get("/api/produtos")).json();
  const opcoes = await (await page.request.get("/api/produtos/opcoes")).json();
  const r = await page.request.post("/api/orcamentos", { data: { clienteId: cliente.id, validade: new Date(Date.now() + 15 * 86400000).toISOString().slice(0, 10), prazoEstimadoDiasUteis: 5, desconto: 0, itens: [{ produtoId: produtos[0].id, quantidade: 1, valorUnitario: 40, valorPersonalizacaoUnitario: 0, tipoPersonalizacao: opcoes.tiposPersonalizacao[0], corPeca: "Preta", anexoIds: [] }] } });
  expect(r.status()).toBe(201); return r.json();
}
