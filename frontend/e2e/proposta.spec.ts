import { expect, test, type Page } from "@playwright/test";
import { mkdir } from "node:fs/promises";
import path from "node:path";

async function entrar(page: Page, admin = false) {
  await page.goto("/login");
  await page.getByLabel("E-mail").fill(admin ? "admin@zalekpersonalizados.com.br" : "gestao@zalekpersonalizados.com.br");
  await page.getByRole("textbox", { name: "Senha" }).fill("Zalek@2026");
  await page.getByRole("button", { name: "Entrar", exact: true }).click();
  await expect(page).toHaveURL(/\/dashboard$/);
}

async function criar(page: Page) {
  const clientes = await (await page.request.get("/api/clientes")).json();
  const produtos = await (await page.request.get("/api/produtos")).json();
  const opcoes = await (await page.request.get("/api/produtos/opcoes")).json();
  const validade = new Date(Date.now() + 15 * 86400000).toISOString().slice(0, 10);
  const resposta = await page.request.post("/api/orcamentos", { data: { clienteId: clientes.itens[0].id, validade, prazoEstimadoDiasUteis: 10, desconto: 0, itens: [{ produtoId: produtos[0].id, quantidade: 2, valorUnitario: 40, valorPersonalizacaoUnitario: 5, tipoPersonalizacao: opcoes.tiposPersonalizacao[0], corPeca: "Preta", anexoIds: [] }] } });
  expect(resposta.status()).toBe(201); return resposta.json();
}

for (const admin of [false, true]) {
  test(`${admin ? "Administrador" : "Gestor"} revisa proposta e confirma cada canal sem envio automático`, async ({ page }) => {
    test.setTimeout(90_000);
    const erros: string[] = []; page.on("pageerror", e => erros.push(e.message));
    await entrar(page, admin);
    const o = await criar(page);
    const preparado = await (await page.request.get(`/api/orcamentos/${o.id}/proposta`)).json();
    const smtp = process.env.E2E_SMTP_TESTE === "true";
    expect(preparado.emailDisponivel).toBe(smtp);
    await page.goto(`/orcamentos/${o.id}`);
    await page.getByRole("button", { name: "Apresentar ao cliente", exact: true }).click();
    const modal = page.getByRole("dialog", { name: "Enviar proposta", exact: true });
    const marcador = `RevisaoE2E${Date.now()}${admin ? "A" : "G"}`;
    const destinatario = `${marcador.toLowerCase()}@example.test`;
    const texto = `Texto revisado E2E ${marcador}. Aguardo sua resposta.`;
    await modal.getByLabel("Destinatário", { exact: true }).fill(destinatario);
    await modal.getByLabel("Assunto", { exact: true }).fill(marcador);
    await modal.getByLabel("Mensagem ao cliente", { exact: true }).fill(texto);
    if (!smtp) {
      const link = modal.getByRole("link", { name: "Abrir aplicativo de e-mail" });
      await expect(link).toHaveAttribute("href", `mailto:${encodeURIComponent(destinatario)}?subject=${encodeURIComponent(marcador)}&body=${encodeURIComponent(texto)}`);
      await expect(modal.getByRole("button", { name: "Confirmar apresentação" })).toBeDisabled();
      const baixando = page.waitForEvent("download");
      await modal.getByRole("button", { name: "Baixar PDF da proposta" }).click();
      expect((await baixando).suggestedFilename()).toMatch(/\.pdf$/);
      expect((await (await page.request.get(`/api/orcamentos/${o.id}`)).json()).situacao).toBe("EmElaboracao");
      await modal.getByRole("checkbox", { name: "Já enviei a mensagem e anexei o PDF ao cliente." }).check();
      await modal.getByRole("button", { name: "Confirmar apresentação" }).click();
      await expect(modal).toHaveCount(0);
      expect((await (await page.request.get(`/api/orcamentos/${o.id}`)).json()).situacao).toBe("AguardandoRetorno");
    } else {
      const enviando = page.waitForResponse(r => r.url().endsWith(`/api/orcamentos/${o.id}/proposta/email`) && r.request().method() === "POST");
      await modal.getByRole("button", { name: "Enviar e-mail com PDF" }).click();
      const resposta = await enviando; expect(resposta.status()).toBe(200);
      const resultado = await resposta.json(); expect(resultado.envio.situacao).toBe("Enviado");
      await expect(modal).toHaveCount(0);
      expect(resultado.orcamento.situacao).toBe("AguardandoRetorno");
      // Caixa SMTP local, sem relay. Verifica destinatário, assunto, texto e PDF realmente recebidos.
      const urlCaixa = `http://localhost:5180/api/Messages?searchTerms=${encodeURIComponent(marcador)}`;
      await expect.poll(async () => (await (await page.request.get(urlCaixa)).json()).results.length).toBe(1);
      const recebido = (await (await page.request.get(urlCaixa)).json()).results[0];
      expect(recebido.subject).toBe(marcador); expect(JSON.stringify(recebido.to)).toContain(destinatario);
      const eml = await (await page.request.get(`http://localhost:5180/api/Messages/${recebido.id}/download`)).text();
      expect(eml).toContain(texto);
      const parte = eml.split(/\r?\n--/).find(p => /Content-Type: application\/pdf/i.test(p));
      expect(parte).toBeDefined();
      const base64 = parte!.split(/\r?\n\r?\n/).slice(1).join("\n").replace(/\s/g, "");
      expect(Buffer.from(base64, "base64").subarray(0, 5).toString()).toBe("%PDF-");
      // Repetir a mesma chave pela API retorna o recibo, sem outra mensagem.
      const requisicao = resposta.request().postDataJSON();
      expect((await page.request.post(`/api/orcamentos/${o.id}/proposta/email`, { data: requisicao })).status()).toBe(200);
      expect((await (await page.request.get(urlCaixa)).json()).results).toHaveLength(1);
    }

    await page.getByRole("button", { name: "Enviar proposta", exact: true }).click();
    await modal.getByLabel("Canal de envio").selectOption("WhatsApp");
    await modal.getByLabel("WhatsApp do cliente").fill("47999998888");
    await modal.getByLabel("Mensagem ao cliente", { exact: true }).fill(texto);
    await expect(modal.getByRole("link", { name: "Abrir WhatsApp" })).toHaveAttribute("href", `https://wa.me/5547999998888?text=${encodeURIComponent(texto)}`);
    expect(decodeURIComponent((await modal.getByRole("link", { name: "Abrir WhatsApp" }).getAttribute("href"))!)).not.toMatch(/api\/|localhost|custo|margem/i);
    const antes = await (await page.request.get(`/api/orcamentos/${o.id}/contatos`)).json();
    // Abrir e fechar o modal/editar a mensagem não cria contato ou apresentação.
    await page.keyboard.press("Escape"); await expect(modal).toHaveCount(0);
    expect(await (await page.request.get(`/api/orcamentos/${o.id}/contatos`)).json()).toHaveLength(antes.length);
    await page.getByRole("button", { name: "Enviar proposta", exact: true }).click();
    await modal.getByLabel("Canal de envio").selectOption("WhatsApp");
    const baixando = page.waitForEvent("download"); await modal.getByRole("button", { name: "Baixar PDF da proposta" }).click(); await baixando;
    const pasta = path.resolve("../data/revisao-fase4"); await mkdir(pasta, { recursive: true });
    await expect(page.locator('[aria-live="polite"]').getByRole("button", { name: "Fechar" })).toHaveCount(0, { timeout: 7000 });
    for (const largura of [360, 1280]) {
      await page.setViewportSize({ width: largura, height: 900 });
      await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
      await modal.screenshot({ path: path.join(pasta, `${admin ? "Admin" : "Gestor"}-${smtp ? "smtp" : "manual"}-${largura}.png`) });
      await modal.getByRole("button", { name: "Confirmar apresentação" }).scrollIntoViewIfNeeded();
      await expect(modal.getByRole("button", { name: "Confirmar apresentação" })).toBeInViewport();
    }
    await modal.getByRole("checkbox", { name: "Já enviei a mensagem e anexei o PDF ao cliente." }).check();
    await modal.getByRole("button", { name: "Confirmar apresentação" }).click();
    await expect(modal).toHaveCount(0);
    const contatos = await (await page.request.get(`/api/orcamentos/${o.id}/contatos`)).json();
    expect(contatos).toHaveLength(antes.length + 1); expect(contatos.some((c: { tipo: string }) => c.tipo === "WhatsApp")).toBe(true);
    expect(erros).toEqual([]);
  });
}

test("Administrador edita modelos; Gestor recebe 403 e campos internos são rejeitados", async ({ page }) => {
  await entrar(page, true);
  const resposta = await page.request.get("/api/configuracoes/modelos-proposta"); expect(resposta.status()).toBe(200);
  const original = (await resposta.json()).modelos.find((m: { canal: string }) => m.canal === "Email");
  await page.goto("/configuracoes");
  await page.getByRole("button", { name: "Editar modelo de e-mail" }).click();
  const modal = page.getByRole("dialog", { name: "Editar modelo de mensagem" });
  await modal.getByLabel("Texto do modelo").fill("Valor interno {custo}");
  await modal.getByRole("button", { name: "Salvar modelo" }).click();
  await expect(modal.getByRole("alert")).toContainText("Use somente os campos disponíveis");
  await modal.getByLabel("Assunto do modelo").fill("Proposta {numero}");
  await modal.getByLabel("Texto do modelo").fill("Olá {cliente}, total {valorTotal}. {empresa}");
  await modal.getByRole("button", { name: "Salvar modelo" }).click(); await expect(modal).toHaveCount(0);
  const o = await criar(page); const previa = await (await page.request.get(`/api/orcamentos/${o.id}/proposta`)).json();
  expect(previa.assunto).toBe(`Proposta ${String(o.numero).padStart(6, "0")}`); expect(previa.mensagemEmail).not.toContain("{");
  const atual = (await (await page.request.get("/api/configuracoes/modelos-proposta")).json()).modelos.find((m: { id: string }) => m.id === original.id);
  expect((await page.request.put(`/api/configuracoes/modelos-proposta/${original.id}`, { data: { assunto: original.assunto, mensagem: original.mensagem, versao: atual.versao } })).status()).toBe(200);
  await page.request.post("/api/auth/logout"); await entrar(page);
  expect((await page.request.get("/api/configuracoes/modelos-proposta")).status()).toBe(403);
  expect((await page.request.put(`/api/configuracoes/modelos-proposta/${original.id}`, { data: {} })).status()).toBe(403);
});
