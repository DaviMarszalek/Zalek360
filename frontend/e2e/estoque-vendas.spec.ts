import { expect, test, type Page } from "@playwright/test";
import { mkdir } from "node:fs/promises";
import path from "node:path";

for (const admin of [false, true]) {
  test(`${admin ? "Administrador" : "Gestor"}: reposição, compras previstas, vendas entregues e filtros`, async ({ page }) => {
    test.setTimeout(90_000);
    const erros: string[] = []; page.on("pageerror", e => erros.push(e.message));
    await page.goto("/login");
    await page.getByLabel("E-mail").fill(admin ? "admin@zalekpersonalizados.com.br" : "gestao@zalekpersonalizados.com.br");
    await page.getByRole("textbox", { name: "Senha" }).fill("Zalek@2026");
    await page.getByRole("button", { name: "Entrar", exact: true }).click();
    await expect(page).toHaveURL(/\/dashboard$/);
    const nome = `Material Fase5 ${Date.now()}`;
    const antes = (await (await page.request.get("/api/dashboard")).json()).indicadores;
    const m = await page.request.post("/api/estoque/materias-primas", { data: { nome, unidade: "un", quantidadeDisponivel: 3, estoqueMinimo: 5 } });
    expect(m.status()).toBe(201); const material = await m.json();
    const eq = await page.request.post("/api/estoque/materias-primas", { data: { nome: `${nome} igual`, unidade: "un", quantidadeDisponivel: 5, estoqueMinimo: 5 } });
    expect(eq.status()).toBe(201);
    const pr = await page.request.post("/api/produtos", { data: { nome: `Produto ${nome}`, categoria: "Caneca", precoVenda: 40, custo: 12, materiais: [{ materiaPrimaId: material.id, quantidadePorUnidade: 2 }] } });
    expect(pr.status()).toBe(201); const produto = await pr.json();
    const clientes = (await (await page.request.get("/api/clientes")).json()).itens;
    const opcoes = await (await page.request.get("/api/produtos/opcoes")).json();
    async function pedido(clienteId: string, quantidade: number) {
      const r = await page.request.post("/api/orcamentos", { data: { clienteId, validade: antes.fimMes, prazoEstimadoDiasUteis: 5, desconto: 0, itens: [{ produtoId: produto.id, quantidade, valorUnitario: 40, valorPersonalizacaoUnitario: 0, tipoPersonalizacao: opcoes.tiposPersonalizacao[0], corPeca: "Preta", anexoIds: [] }] } });
      expect(r.status()).toBe(201); const o = await r.json();
      const aguardando = await page.request.post(`/api/orcamentos/${o.id}/aguardando-retorno`, { data: { meioApresentacao: "Email", versao: o.versao } });
      expect(aguardando.status()).toBe(200);
      const aprovado = await page.request.post(`/api/orcamentos/${o.id}/decisao`, { data: { resultado: "Aprovado", meioContato: "Email", dataHora: new Date().toISOString(), versao: (await aguardando.json()).versao } });
      expect(aprovado.status()).toBe(200); return (await aprovado.json()).pedidoGerado;
    }
    const p = await pedido(clientes[0].id, 4);
    async function reposicao() {
      return (await (await page.request.get("/api/estoque/reposicao")).json()).find((x: { id: string }) => x.id === material.id);
    }
    expect(await reposicao()).toMatchObject({ saldoDisponivel: 0, quantidadePendente: 5, quantidadeSugerida: 10, faltaComprar: 10 });
    await page.goto("/dashboard");
    await page.getByRole("link", { name: /Repor estoque/ }).click();
    await expect(page).toHaveURL(/filtro=reposicao/);
    await expect(page.getByRole("heading", { name: "Repor estoque", exact: true })).toBeVisible();
    await page.getByRole("textbox", { name: "Buscar matérias-primas" }).fill(nome);
    const linha = page.getByRole("row").filter({ has: page.getByText(nome, { exact: true }) });
    await expect(linha).toBeVisible();
    await expect(linha.getByRole("cell").nth(5)).toContainText("10 un");
    await page.getByRole("combobox", { name: "Filtrar estoque" }).selectOption("abaixo");
    await expect(page.getByText(`${nome} igual`, { exact: true })).toHaveCount(0);
    await page.getByRole("combobox", { name: "Filtrar estoque" }).selectOption("todos");
    await expect(page.getByText(`${nome} igual`, { exact: true })).toBeVisible();
    await page.getByRole("combobox", { name: "Filtrar estoque" }).selectOption("reposicao");
    await linha.getByRole("button", { name: `Planejar compra de ${nome}`, exact: true }).click();
    const modal = page.getByRole("dialog", { name: "Planejar compra de matéria-prima" });
    const quantidade = modal.locator("label").filter({ hasText: "Quantidade" }).locator("..").getByRole("spinbutton");
    await expect(quantidade).toHaveValue("10"); await quantidade.fill("4");
    const comprando = page.waitForResponse(r => r.url().endsWith("/api/estoque/compras") && r.request().method() === "POST");
    await modal.getByRole("button", { name: "Salvar compra" }).click();
    const respostaCompra = await comprando; expect(respostaCompra.status()).toBe(201); const compra = await respostaCompra.json();
    await expect(modal).toHaveCount(0);
    expect(await reposicao()).toMatchObject({ quantidadeSugerida: 10, quantidadeAReceber: 4, faltaComprar: 6 });
    await expect(linha.getByRole("cell").nth(6)).toContainText("4 un");
    await expect(linha.getByRole("cell").nth(7)).toContainText("6 un");
    const pasta = path.resolve("../data/revisao-fase5"); await mkdir(pasta, { recursive: true });
    async function capturar(tela: string) {
      await expect(page.locator('[aria-live="polite"]').getByRole("button", { name: "Fechar" })).toHaveCount(0, { timeout: 7000 });
      await page.evaluate(() => window.scrollTo(0, 0));
      for (const largura of [360, 1280]) {
        await page.setViewportSize({ width: largura, height: 900 });
        await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
        await page.screenshot({ path: path.join(pasta, `${admin ? "Admin" : "Gestor"}-${tela}-${largura}.png`), fullPage: true });
      }
    }
    await capturar("reposicao");
    const atual = await (await page.request.get(`/api/pedidos/${p.id}`)).json();
    expect((await page.request.patch(`/api/pedidos/${p.id}/status`, { data: { novaSituacao: "Cancelado", observacao: "Cancelamento E2E", versao: atual.versao } })).status()).toBe(200);
    expect(await reposicao()).toMatchObject({ saldoDisponivel: 3, quantidadePendente: 0, quantidadeSugerida: 2, faltaComprar: 0 });
    expect((await page.request.patch(`/api/estoque/compras/${compra.id}/situacao`, { data: { situacao: "Recebida" } })).status()).toBe(200);
    expect(await reposicao()).toBeUndefined();

    const venda = await pedido(clientes[0].id, 1);
    for (const novaSituacao of ["EmProducao", "Pronto", "Entregue"]) {
      const versao = (await (await page.request.get(`/api/pedidos/${venda.id}`)).json()).versao;
      expect((await page.request.patch(`/api/pedidos/${venda.id}/status`, { data: { novaSituacao, versao } })).status()).toBe(200);
      const saldo = (await (await page.request.get("/api/estoque/materias-primas")).json()).find((x: { id: string }) => x.id === material.id);
      expect(saldo).toMatchObject({ quantidadeDisponivel: 5, quantidadeReservada: 0 });
    }
    const outro = await pedido(clientes[1].id, 1);
    const depois = (await (await page.request.get("/api/dashboard")).json()).indicadores;
    expect(depois.pedidosConcluidosNoMes).toBe(antes.pedidosConcluidosNoMes + 1);
    expect(depois.valorEntregueNoMes).toBe(antes.valorEntregueNoMes + 40);
    await page.goto("/dashboard"); await capturar("dashboard");
    await expect(page.getByRole("heading", { name: "Orçamentos em aberto" })).toBeVisible();
    await page.getByRole("link", { name: /Vendas entregues em/ }).click();
    await expect(page.getByRole("combobox", { name: "Data de referência" })).toHaveValue("Entrega");
    await expect(page.getByLabel("Data inicial", { exact: true })).toHaveValue(antes.inicioMes);
    await expect(page.getByLabel("Data final", { exact: true })).toHaveValue(antes.fimMes);
    const filtroCliente = page.getByRole("combobox", { name: "Filtrar por cliente" });
    await filtroCliente.fill(clientes[0].nome);
    await expect(page.getByRole("option").filter({ hasText: clientes[0].nome }).first()).toBeVisible();
    await filtroCliente.press("ArrowDown"); await filtroCliente.press("Enter");
    const numero = (n: number) => `#${String(n).padStart(6, "0")}`;
    await expect(page.getByRole("link", { name: numero(venda.numero), exact: true })).toBeVisible();
    await expect(page.getByRole("link", { name: numero(outro.numero), exact: true })).toHaveCount(0);
    await capturar("pedidos");
    await page.getByLabel("Data inicial", { exact: true }).fill("2026-12-01");
    await page.getByLabel("Data final", { exact: true }).fill("2026-11-01");
    await expect(page.getByRole("alert").filter({ hasText: "data final deve ser igual ou posterior" })).toBeVisible();
    await page.getByRole("button", { name: "Limpar filtros", exact: true }).first().click();
    await expect(filtroCliente).toHaveValue("");
    await expect(page.getByRole("combobox", { name: "Data de referência" })).toHaveValue("Criacao");
    expect(erros).toEqual([]);
  });
}
