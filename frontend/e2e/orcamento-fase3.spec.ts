import { expect, test, type Locator } from "@playwright/test";
import { mkdir } from "node:fs/promises";
import path from "node:path";

// PNG colorido de 16 × 16; fixture de referência, sem dependência externa.
const png = Buffer.from("iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAHElEQVR4nGNQc7yDFX2dz4UVMYxqGNXANWw1AABgfnCQSrM/TAAAAABJRU5ErkJggg==", "base64");
const campo = (raiz: Locator, rotulo: string) => raiz.locator("label").filter({ hasText: rotulo }).locator("..");

for (const perfil of ["Gestor", "Administrador"]) {
  test(`${perfil}: preço, produto no orçamento, margem, materiais e referência até o pedido`, async ({ page }) => {
    test.setTimeout(90_000);
    const erros: string[] = [];
    page.on("pageerror", erro => erros.push(erro.message));
    await page.goto("/login");
    await page.getByLabel("E-mail").fill(perfil === "Gestor" ? "gestao@zalekpersonalizados.com.br" : "admin@zalekpersonalizados.com.br");
    await page.getByRole("textbox", { name: "Senha" }).fill("Zalek@2026");
    await page.getByRole("button", { name: "Entrar", exact: true }).click();
    await expect(page).toHaveURL(/\/dashboard$/);
    const nome = `Orçamento fase 3 ${perfil} ${Date.now()}`;
    const pasta = path.resolve("../data/revisao-fase3");
    await mkdir(pasta, { recursive: true });
    async function capturar(tela: string) {
      for (const fechar of await page.locator('[aria-live="polite"]').getByRole("button", { name: "Fechar" }).all()) await fechar.click();
      await page.evaluate(() => window.scrollTo(0, 0));
      for (const largura of [360, 1280]) {
        await page.setViewportSize({ width: largura, height: 900 });
        await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
        await page.screenshot({ path: path.join(pasta, `${perfil}-${tela}-${largura}.png`), fullPage: tela !== "modal-produto" });
      }
    }
    const m = await page.request.post("/api/estoque/materias-primas", { data: { nome: `Material ${nome}`, unidade: "un", quantidadeDisponivel: 3, estoqueMinimo: 0 } });
    expect(m.status()).toBe(201);
    const material = await m.json();
    const p = await page.request.post("/api/produtos", { data: { nome: `Base ${nome}`, categoria: "Caneca", precoVenda: 25, custo: 10, materiais: [] } });
    expect(p.status()).toBe(201);
    const produto = await p.json();
    const clientes = await (await page.request.get("/api/clientes?tamanhoPagina=20")).json();
    const cliente = clientes.itens[0];
    expect(cliente).toBeDefined();

    await page.goto(`/orcamentos/novo?clienteId=${cliente.id}`);
    await expect(page.getByRole("button", { name: "Trocar", exact: true })).toBeVisible();
    await campo(page.locator("main"), "Observações comerciais").getByRole("textbox").fill(nome);
    await page.getByRole("textbox", { name: "Desconto", exact: true }).fill("5,00");
    await page.getByRole("button", { name: "Adicionar item", exact: true }).click();
    const item = (n: number) => page.locator(".p-5").filter({ has: page.getByText(new RegExp(`^Item ${n}(?:$|R)`)) });
    const primeiro = item(1);
    await campo(primeiro, "Produto").getByRole("combobox").selectOption(produto.id);
    await expect(campo(primeiro, "Valor unitário (R$)").getByRole("textbox")).toHaveValue("25,00");
    await campo(primeiro, "Quantidade").getByRole("textbox").fill("1");
    await campo(primeiro, "Valor unitário (R$)").getByRole("textbox").fill("30,00");
    await expect(primeiro.getByText(/Valor diferente do preço cadastrado/)).toBeVisible();
    await primeiro.getByRole("button", { name: "Restaurar preço cadastrado" }).click();
    await expect(campo(primeiro, "Valor unitário (R$)").getByRole("textbox")).toHaveValue("25,00");
    await campo(primeiro, "Tipo de personalização").getByRole("combobox").selectOption({ index: 1 });
    await campo(primeiro, "Cor da peça").getByRole("textbox").fill("Azul");

    await page.getByRole("button", { name: "Adicionar item", exact: true }).click();
    const segundo = item(2);
    await campo(segundo, "Quantidade").getByRole("textbox").fill("2");
    await campo(segundo, "Descrição").getByRole("textbox").fill("Descrição mantida do orçamento");
    await campo(segundo, "Personalização por unidade (R$)").getByRole("textbox").fill("5,00");
    await campo(segundo, "Tipo de personalização").getByRole("combobox").selectOption({ index: 1 });
    await campo(segundo, "Cor da peça").getByRole("textbox").fill("Preta");
    await segundo.locator('input[type="file"]').setInputFiles({ name: "referencia.png", mimeType: "image/png", buffer: png });
    const previa = page.getByRole("img", { name: "Prévia de referencia.png" });
    await expect(previa).toBeVisible();
    await expect.poll(() => previa.evaluate((el: HTMLImageElement) => el.naturalWidth)).toBeGreaterThan(0);
    await segundo.getByRole("button", { name: "Novo produto neste item" }).click();
    const modal = page.getByRole("dialog", { name: "Novo produto para o orçamento" });
    await expect(modal).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(modal).toHaveCount(0);
    await expect(campo(segundo, "Descrição").getByRole("textbox")).toHaveValue("Descrição mantida do orçamento");
    await expect(previa).toBeVisible();
    await segundo.getByRole("button", { name: "Novo produto neste item" }).click();
    await modal.getByLabel("Nome do produto").fill(nome);
    await modal.getByLabel("Categoria", { exact: false }).selectOption("Caneca");
    await modal.getByLabel("Preço de venda (R$)").fill("40,00");
    await modal.getByLabel("Custo (R$)").fill("12,00");
    await modal.getByRole("button", { name: "Adicionar material" }).click();
    await modal.getByLabel("Material 1", { exact: true }).selectOption(material.id);
    await modal.getByLabel("Quantidade 1", { exact: true }).fill("2");
    await capturar("modal-produto");
    const criando = page.waitForResponse(r => r.url().endsWith("/api/produtos") && r.request().method() === "POST");
    await modal.getByRole("button", { name: "Salvar produto", exact: true }).click();
    const respostaProduto = await criando;
    expect(respostaProduto.status()).toBe(201);
    const novoProduto = await respostaProduto.json();
    await expect(modal).toHaveCount(0);
    await expect(campo(segundo, "Produto").getByRole("combobox")).toHaveValue(novoProduto.id);
    await expect(campo(segundo, "Descrição").getByRole("textbox")).toHaveValue("Descrição mantida do orçamento");
    await expect(campo(segundo, "Quantidade").getByRole("textbox")).toHaveValue("2");
    await expect(campo(segundo, "Cor da peça").getByRole("textbox")).toHaveValue("Preta");
    await expect(campo(segundo, "Valor unitário (R$)").getByRole("textbox")).toHaveValue("40,00");
    await expect(campo(primeiro, "Produto").getByRole("combobox")).toHaveValue(produto.id);
    await expect(page.getByRole("textbox", { name: "Desconto", exact: true })).toHaveValue("5,00");
    await expect(segundo.getByText(/Falta:/)).toBeVisible();
    await expect(page.getByText("Margem sobre a venda", { exact: true }).locator("..")).toContainText("69,09%");

    await capturar("formulario");
    const salvando = page.waitForResponse(r => r.url().endsWith("/api/orcamentos") && r.request().method() === "POST");
    await page.getByRole("button", { name: "Salvar orçamento", exact: true }).click();
    const resposta = await salvando;
    expect(resposta.status()).toBe(201);
    const orcamento = await resposta.json();
    expect(orcamento).toMatchObject({ observacoesComerciais: nome, condicaoPagamento: null, resumo: { valorTotal: 110, desconto: 5 } });
    expect(orcamento.itens[1]).toMatchObject({ descricao: "Descrição mantida do orçamento", quantidade: 2, valorUnitario: 40 });
    expect(orcamento.itens[1].anexos).toHaveLength(1);
    await expect(page).toHaveURL(new RegExp(`/orcamentos/${orcamento.id}$`));
    await expect(previa).toBeVisible();
    await expect.poll(() => previa.evaluate((el: HTMLImageElement) => el.naturalWidth)).toBeGreaterThan(0);
    await expect(page.getByText("Margem sobre a venda", { exact: true }).locator("..")).toContainText("69,09%");
    expect(await (await page.request.get(`/api/orcamentos/${orcamento.id}/analise-interna`)).json()).toMatchObject({ receita: 110, custoTotal: 34, margemValor: 76, margemPercentual: 69.09 });

    await capturar("orcamento");

    const apresentado = await page.request.post(`/api/orcamentos/${orcamento.id}/aguardando-retorno`, { data: { meioContato: "WhatsApp", versao: orcamento.versao } });
    expect(apresentado.status()).toBe(200);
    const aguardando = await apresentado.json();
    const decisao = await page.request.post(`/api/orcamentos/${orcamento.id}/decisao`, { data: { resultado: "Aprovado", meioContato: "WhatsApp", dataHora: new Date().toISOString(), versao: aguardando.versao } });
    expect(decisao.status()).toBe(200);
    const aprovado = await decisao.json();
    await page.goto(`/pedidos/${aprovado.pedidoGerado.id}`);
    await expect(previa).toBeVisible();
    await expect.poll(() => previa.evaluate((el: HTMLImageElement) => el.naturalWidth)).toBeGreaterThan(0);
    await capturar("pedido");
    // A reserva feita na aprovação não deve aparecer como falta duplicada na análise do próprio orçamento.
    const analise = await (await page.request.get(`/api/orcamentos/${orcamento.id}/analise-interna`)).json();
    expect(analise.itens[1].materiais[0]).toMatchObject({ necessario: 4, disponivel: 3, falta: 1 });
    expect(erros).toEqual([]);
  });
}
