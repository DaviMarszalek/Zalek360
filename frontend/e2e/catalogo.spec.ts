import { expect, test, type Page } from "@playwright/test";

async function entrar(page: Page, email: string) {
  await page.goto("/login");
  await page.getByLabel("E-mail").fill(email);
  await page.getByRole("textbox", { name: "Senha" }).fill("Zalek@2026");
  await page.getByRole("button", { name: "Entrar", exact: true }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
}

test("Gestor gerencia produto com valores e composição e não acessa Configurações", async ({ page }) => {
  await entrar(page, "gestao@zalekpersonalizados.com.br");
  const nome = `Produto E2E ${Date.now()}`;
  const material = await page.request.post("/api/estoque/materias-primas", { data: { nome: `Material ${nome}`, unidade: "un", quantidadeDisponivel: 10, estoqueMinimo: 0 } });
  expect(material.status()).toBe(201);
  const materia = await material.json();
  await page.getByRole("navigation", { name: "Menu principal" }).getByRole("link", { name: "Produtos", exact: true }).click();
  await page.getByRole("button", { name: "Novo produto", exact: true }).click();
  await page.getByLabel("Nome do produto").fill(nome);
  await page.getByLabel("Categoria", { exact: false }).selectOption("Caneca");
  await page.getByLabel("Preço de venda (R$)").fill("59,90");
  await page.getByLabel("Custo (R$)").fill("21,35");
  await page.getByRole("button", { name: "Adicionar material", exact: true }).click();
  await page.getByLabel("Material 1", { exact: true }).selectOption(materia.id);
  await page.getByLabel("Quantidade 1", { exact: true }).fill("2");
  const criacao = page.waitForResponse(r => r.url().endsWith("/api/produtos") && r.request().method() === "POST");
  await page.getByRole("button", { name: "Salvar produto", exact: true }).click();
  const resposta = await criacao; expect(resposta.status()).toBe(201);
  const produto = await resposta.json();
  expect(produto).toMatchObject({ precoVenda: 59.9, custo: 21.35 });
  expect(produto.materiais).toHaveLength(1);
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await page.getByRole("textbox", { name: "Pesquisar produtos" }).fill(nome);
  const linha = page.getByRole("article").filter({ has: page.getByRole("heading", { name: nome, exact: true }) });
  await expect(linha).toBeVisible();
  await linha.getByRole("button", { name: `Editar ${nome}`, exact: true }).click();
  await page.getByLabel("Preço de venda (R$)").fill("65,00");
  await page.getByLabel("Quantidade 1", { exact: true }).fill("3");
  await page.getByRole("button", { name: "Salvar produto", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  expect(await (await page.request.get(`/api/produtos/${produto.id}`)).json()).toMatchObject({ precoVenda: 65, custo: 21.35 });
  await linha.getByRole("button", { name: `Duplicar ${nome}`, exact: true }).click();
  await page.getByLabel("Nome da cópia").fill(`Cópia ${nome}`);
  await page.getByRole("button", { name: "Confirmar", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(page.getByRole("heading", { name: `Cópia ${nome}`, exact: true })).toBeVisible();
  await linha.getByRole("button", { name: `Inativar ${nome}`, exact: true }).click();
  await page.getByRole("button", { name: "Confirmar", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(linha).toHaveCount(0);
  await page.getByLabel("Mostrar inativos").check();
  await expect(page.getByRole("button", { name: `Reativar ${nome}`, exact: true })).toBeVisible();
  expect((await page.request.delete(`/api/produtos/${produto.id}?versao=${produto.versao}`)).status()).toBe(403);
  expect((await page.request.get("/api/configuracoes/opcoes")).status()).toBe(403);
  await page.goto("/configuracoes");
  await expect(page.getByText("Somente o Administrador pode acessar as configurações.")).toBeVisible();
});

test("Administrador configura opção usada no catálogo e exclui produto nunca usado", async ({ page }) => {
  await entrar(page, "admin@zalekpersonalizados.com.br");
  const categoria = `Categoria E2E ${Date.now()}`;
  const nome = `Produto admin ${Date.now()}`;
  await page.getByRole("link", { name: "Configurações", exact: true }).click();
  await page.getByRole("button", { name: "Nova opção", exact: true }).click();
  await page.getByLabel("Nome da opção").fill(categoria);
  await page.getByRole("button", { name: "Salvar opção", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(page.getByText(categoria, { exact: true })).toBeVisible();
  await page.getByRole("link", { name: "Produtos", exact: true }).click();
  await page.getByRole("button", { name: "Novo produto", exact: true }).click();
  await page.getByLabel("Nome do produto").fill(nome);
  await page.getByLabel("Categoria", { exact: false }).selectOption(categoria);
  await page.getByRole("button", { name: "Salvar produto", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  const produtoSemValores = page.getByRole("article").filter({ has: page.getByRole("heading", { name: nome, exact: true }) });
  await expect(produtoSemValores.getByText("Venda: Não informado", { exact: true })).toBeVisible();
  await expect(produtoSemValores.getByText("Custo: Não informado", { exact: true })).toBeVisible();
  await produtoSemValores.getByRole("button", { name: `Editar ${nome}`, exact: true }).click();
  await page.getByLabel("Preço de venda (R$)").fill("20,00");
  await page.getByLabel("Custo (R$)").fill("9,00");
  await page.getByRole("button", { name: "Salvar produto", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  // Reproduz uma pesquisa iniciada antes da exclusão cuja resposta chega atrasada.
  let liberarConsulta!: () => void;
  let consultaPronta!: () => void;
  let consultaFinalizada!: () => void;
  const liberada = new Promise<void>(resolve => { liberarConsulta = resolve; });
  const pronta = new Promise<void>(resolve => { consultaPronta = resolve; });
  const finalizada = new Promise<void>(resolve => { consultaFinalizada = resolve; });
  let capturada = false;
  await page.route("**/api/produtos/catalogo?*", async route => {
    if (!capturada && new URL(route.request().url()).searchParams.get("busca") === nome) {
      capturada = true;
      const resposta = await route.fetch();
      consultaPronta();
      await liberada;
      await route.fulfill({ response: resposta }).catch(() => { /* Consulta cancelada após a exclusão. */ });
      consultaFinalizada();
    } else await route.continue();
  });
  try {
    await page.getByRole("textbox", { name: "Pesquisar produtos" }).fill(nome);
    await page.getByRole("button", { name: `Excluir ${nome}`, exact: true }).click();
    await pronta;
    const exclusao = page.waitForResponse(r => r.request().method() === "DELETE" && r.url().includes("/api/produtos/"));
    await page.getByRole("button", { name: "Confirmar", exact: true }).click();
    expect((await exclusao).status()).toBe(204);
    await expect(page.getByRole("dialog")).toHaveCount(0);
    await expect(page.getByRole("heading", { name: nome, exact: true })).toHaveCount(0);
  } finally { liberarConsulta(); }
  await finalizada;
  await expect(page.getByRole("heading", { name: nome, exact: true })).toHaveCount(0);
  await page.unroute("**/api/produtos/catalogo?*");
  await page.getByRole("link", { name: "Configurações", exact: true }).click();
  await page.getByRole("button", { name: `Editar ${categoria}`, exact: true }).click();
  await page.getByLabel("Opção ativa").uncheck();
  await page.getByRole("button", { name: "Salvar opção", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  const ativas = await (await page.request.get("/api/catalogo/opcoes")).json();
  expect(ativas.some((x: { nome: string }) => x.nome === categoria)).toBe(false);
});

for (const largura of [360, 1280]) {
  test(`Catálogo e composição cabem em ${largura}px`, async ({ page }) => {
    await page.setViewportSize({ width: largura, height: 900 });
    await entrar(page, "admin@zalekpersonalizados.com.br");
    await page.goto("/produtos");
    await expect(page.getByRole("heading", { name: "Produtos", exact: true })).toBeVisible();
    await expect(page.getByRole("article").first()).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
    await page.screenshot({ path: `../data/revisao-fase2/produtos-${largura}.png`, fullPage: true });
    await page.getByRole("button", { name: "Novo produto", exact: true }).click();
    await page.getByRole("button", { name: "Adicionar material", exact: true }).click();
    const dialogo = page.getByRole("dialog");
    await expect(dialogo.getByLabel("Material 1", { exact: true })).toBeVisible();
    expect(await dialogo.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true);
    await page.screenshot({ path: `../data/revisao-fase2/composicao-${largura}.png` });
    await page.keyboard.press("Escape");
    await expect(dialogo).toHaveCount(0);
    await page.goto("/configuracoes");
    await expect(page.getByLabel("Grupo de opções")).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
    await page.screenshot({ path: `../data/revisao-fase2/configuracoes-${largura}.png`, fullPage: true });
    await page.getByRole("button", { name: "Nova opção", exact: true }).click();
    await expect(page.getByLabel("Nome da opção")).toBeVisible();
    expect(await page.getByRole("dialog").evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true);
    await page.keyboard.press("Escape");
    await expect(page.getByRole("dialog")).toHaveCount(0);
  });
}
