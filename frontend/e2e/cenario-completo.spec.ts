import type { MateriaPrima } from "../types/api";
import { expect, test } from "@playwright/test";
import { capturar, cpfNovo, entrar, png, tecladoModal } from "./apoio";

for (const admin of [false, true]) {
  test(`${admin ? "Administrador" : "Gestor"}: Homem-Aranha da compra à entrega, com falta e cancelamento`, async ({ page }) => {
    test.setTimeout(150_000);
    const erros: string[] = []; page.on("pageerror", e => erros.push(e.message));
    await entrar(page, admin);
    const perfil = admin ? "Admin" : "Gestor", marca = `${perfil} ${Date.now()}`;
    const antes = (await (await page.request.get("/api/dashboard")).json()).indicadores;
    // Estoque próprio da execução; duas matérias-primas, uma com falta inicial.
    const materiais: MateriaPrima[] = [];
    for (const [nome, saldo] of [["Camiseta preta GG", 5], ["Estampa Homem-Aranha", 1]] as const) {
      await page.goto("/estoque"); await page.getByRole("button", { name: "Nova matéria-prima", exact: true }).click();
      const modal = page.getByRole("dialog", { name: "Nova matéria-prima", exact: true });
      await modal.getByLabel("Nome do material").fill(`${nome} ${marca}`);
      await modal.getByLabel("Saldo físico").fill(String(saldo)); await modal.getByLabel("Estoque mínimo").fill("2");
      const salvo = page.waitForResponse(r => r.url().endsWith("/api/estoque/materias-primas") && r.request().method() === "POST");
      await modal.getByRole("button", { name: "Salvar matéria-prima" }).click();
      const r = await salvo; expect(r.status()).toBe(201); materiais.push(await r.json());
      await expect(modal).toHaveCount(0);
    }
    await page.goto("/clientes/novo");
    await page.getByRole("radio", { name: "Pessoa física" }).click();
    await page.getByLabel("CPF", { exact: false }).fill(cpfNovo());
    await page.getByLabel("Nome completo").fill(`Cliente Homem-Aranha ${marca}`);
    await page.getByRole("tab", { name: "Contato", exact: true }).click();
    await page.getByLabel("E-mail", { exact: true }).fill(`homemaranha.${Date.now()}@example.test`);
    await capturar(page, `${perfil}-cliente-novo`);
    const clienteSalvo = page.waitForResponse(r => r.url().endsWith("/api/clientes") && r.request().method() === "POST");
    await page.getByRole("button", { name: "Salvar cliente", exact: true }).click();
    const cliente = await (await clienteSalvo).json();
    await expect(page).toHaveURL(new RegExp(`/clientes/${cliente.id}$`));
    await capturar(page, `${perfil}-cliente`);

    await page.goto(`/orcamentos/novo?clienteId=${cliente.id}`);
    await expect(page.getByRole("button", { name: "Trocar", exact: true })).toBeVisible();
    await page.getByRole("button", { name: "Adicionar item", exact: true }).click();
    await page.getByRole("button", { name: "Novo produto neste item" }).click();
    const produtoModal = page.getByRole("dialog", { name: "Novo produto para o orçamento" });
    await produtoModal.getByLabel("Nome do produto").fill(`Camiseta Homem-Aranha ${marca}`);
    await produtoModal.getByLabel("Categoria", { exact: false }).selectOption("Camiseta");
    await produtoModal.getByLabel("Preço de venda (R$)").fill("80,00");
    await produtoModal.getByLabel("Custo (R$)").fill("30,00");
    for (let i = 0; i < 2; i++) {
      await produtoModal.getByRole("button", { name: "Adicionar material" }).click();
      await produtoModal.getByLabel(`Material ${i + 1}`, { exact: true }).selectOption(materiais[i].id);
      await produtoModal.getByLabel(`Quantidade ${i + 1}`, { exact: true }).fill("1");
    }
    await tecladoModal(page, produtoModal);
    await produtoModal.getByRole("button", { name: "Salvar produto", exact: true }).click();
    await expect(produtoModal).toHaveCount(0);
    await page.getByLabel("Quantidade", { exact: false }).fill("2");
    await page.getByLabel("Tipo de personalização").selectOption({ index: 1 });
    await page.getByLabel("Cor da peça").fill("Preta");
    await page.locator('input[type="file"]').setInputFiles({ name: "homem-aranha.png", mimeType: "image/png", buffer: png });
    await expect(page.getByRole("img", { name: "Prévia de homem-aranha.png" })).toBeVisible();
    const salvando = page.waitForResponse(r => r.url().endsWith("/api/orcamentos") && r.request().method() === "POST");
    await page.getByRole("button", { name: "Salvar orçamento", exact: true }).click();
    const resposta = await salvando; expect(resposta.status()).toBe(201); const o = await resposta.json();
    expect(o.resumo.valorTotal).toBe(160); expect(o.condicaoPagamento).toBeNull();
    await expect(page).toHaveURL(new RegExp(`/orcamentos/${o.id}$`));
    await page.getByRole("button", { name: "Registrar contato", exact: true }).click();
    let modal = page.getByRole("dialog", { name: "Registrar contato", exact: true });
    await modal.getByRole("radio", { name: "WhatsApp" }).click();
    await modal.getByLabel("O que foi conversado").fill("Cliente confirmou a referência Homem-Aranha e pediu duas camisetas GG.");
    await modal.getByRole("button", { name: "Registrar contato", exact: true }).click(); await expect(modal).toHaveCount(0);
    await page.getByRole("button", { name: "Apresentar ao cliente", exact: true }).click();
    modal = page.getByRole("dialog", { name: "Enviar proposta", exact: true });
    await modal.getByLabel("Canal de envio").selectOption("WhatsApp");
    await modal.getByLabel("WhatsApp do cliente").fill("47999998888");
    await expect(modal.getByLabel("Mensagem ao cliente")).not.toHaveValue(/custo|margem/i);
    const baixando = page.waitForEvent("download"); await modal.getByRole("button", { name: "Baixar PDF da proposta" }).click();
    expect((await baixando).suggestedFilename()).toMatch(/\.pdf$/);
    await modal.getByRole("checkbox", { name: "Já enviei a mensagem e anexei o PDF ao cliente." }).check();
    await modal.getByRole("button", { name: "Confirmar apresentação" }).click(); await expect(modal).toHaveCount(0);
    await page.getByRole("button", { name: "Registrar decisão", exact: true }).click();
    modal = page.getByRole("dialog", { name: "Registrar decisão do cliente", exact: true });
    await modal.getByRole("radio", { name: "Aprovado", exact: true }).click();
    await modal.getByRole("radio", { name: "WhatsApp" }).click();
    await modal.getByRole("button", { name: "Continuar", exact: true }).click();
    modal = page.getByRole("dialog", { name: "Aprovar orçamento e gerar pedido" }); await tecladoModal(page, modal);
    const aprovando = page.waitForResponse(r => r.url().endsWith(`/api/orcamentos/${o.id}/decisao`) && r.request().method() === "POST");
    await modal.getByRole("button", { name: "Aprovar e gerar pedido" }).click();
    const aprovada = await aprovando; expect(aprovada.status()).toBe(200); const pedido = (await aprovada.json()).pedidoGerado;
    await page.goto(`/pedidos/${pedido.id}`);
    const reservas = await (await page.request.get(`/api/pedidos/${pedido.id}/materiais`)).json();
    expect(reservas.temPendencia).toBe(true);
    async function saldo() { return (await (await page.request.get("/api/estoque/materias-primas")).json()).filter((m: { id: string }) => materiais.some(x => x.id === m.id)); }
    expect(await saldo()).toEqual(expect.arrayContaining([expect.objectContaining({ id: materiais[0].id, quantidadeDisponivel: 5, quantidadeReservada: 2 }), expect.objectContaining({ id: materiais[1].id, quantidadeDisponivel: 1, quantidadeReservada: 1 })]));
    await page.getByRole("button", { name: "Atualizar status", exact: true }).click();
    modal = page.getByRole("dialog", { name: /Atualizar status do pedido/ });
    await modal.getByRole("button", { name: "Mover para Em produção" }).click();
    await expect(modal.getByRole("alert")).toBeVisible();
    expect((await (await page.request.get(`/api/pedidos/${pedido.id}`)).json()).situacao).toBe("Aberto");
    await page.keyboard.press("Escape");
    await page.goto("/estoque?filtro=reposicao");
    await page.getByRole("button", { name: `Planejar compra de ${materiais[1].nome}`, exact: true }).click();
    modal = page.getByRole("dialog", { name: "Planejar compra de matéria-prima" });
    await expect(modal.getByLabel("Quantidade")).toHaveValue("3");
    const comprando = page.waitForResponse(r => r.url().endsWith("/api/estoque/compras") && r.request().method() === "POST");
    await modal.getByRole("button", { name: "Salvar compra" }).click(); const compra = await (await comprando).json();
    await expect(modal).toHaveCount(0);
    const linhaCompra = page.getByRole("row").filter({ hasText: materiais[1].nome }).filter({ hasText: "Planejada" });
    await linhaCompra.getByRole("button", { name: `Marcar compra de ${materiais[1].nome} como recebida`, exact: true }).click();
    await expect(linhaCompra).toHaveCount(0);
    // Recebimento atual exige recarregar a ficha do pedido para completar a reserva.
    await page.goto(`/pedidos/${pedido.id}`);
    await page.getByRole("button", { name: "Carregar ficha técnica" }).click();
    await expect(page.getByText("Há pendências de materiais ou ficha técnica.", { exact: false })).toHaveCount(0);
    for (const status of ["Em produção", "Pronto", "Entregue"]) {
      await page.getByRole("button", { name: "Atualizar status", exact: true }).click();
      modal = page.getByRole("dialog", { name: /Atualizar status do pedido/ });
      // Espera a resposta da API (baixa de estoque sob lock) antes de verificar o fechamento do modal.
      const atualizando = page.waitForResponse(r => r.url().endsWith(`/api/pedidos/${pedido.id}/status`) && r.request().method() === "PATCH");
      await modal.getByRole("button", { name: `Mover para ${status}`, exact: true }).click();
      expect((await atualizando).status()).toBe(200); await expect(modal).toHaveCount(0);
      expect(await saldo()).toEqual(expect.arrayContaining([expect.objectContaining({ id: materiais[0].id, quantidadeDisponivel: 3, quantidadeReservada: 0 }), expect.objectContaining({ id: materiais[1].id, quantidadeDisponivel: 2, quantidadeReservada: 0 })]));
    }
    await expect(page.getByText("Fluxo concluído.", { exact: false })).toBeVisible();
    await capturar(page, `${perfil}-homem-aranha-entregue`);
    const depois = (await (await page.request.get("/api/dashboard")).json()).indicadores;
    expect(depois.pedidosConcluidosNoMes).toBe(antes.pedidosConcluidosNoMes + 1); expect(depois.valorEntregueNoMes).toBe(antes.valorEntregueNoMes + 160);
    // Uma segunda aprovação reserva; cancelamento não estorna o consumo da venda anterior.
    const copia = await page.request.post("/api/orcamentos", { data: { clienteId: cliente.id, validade: o.validade, prazoEstimadoDiasUteis: 5, desconto: 0, itens: [{ produtoId: o.itens[0].produtoId, quantidade: 1, valorUnitario: 80, valorPersonalizacaoUnitario: 0, tipoPersonalizacao: o.itens[0].personalizacao.tipoPersonalizacao, corPeca: "Preta", anexoIds: [] }] } });
    expect(copia.status()).toBe(201); const segundo = await copia.json();
    const apres = await (await page.request.post(`/api/orcamentos/${segundo.id}/aguardando-retorno`, { data: { meioApresentacao: "WhatsApp", versao: segundo.versao } })).json();
    const cancelavel = await (await page.request.post(`/api/orcamentos/${segundo.id}/decisao`, { data: { resultado: "Aprovado", meioContato: "WhatsApp", dataHora: new Date().toISOString(), versao: apres.versao } })).json();
    await page.goto(`/pedidos/${cancelavel.pedidoGerado.id}`); await page.getByRole("button", { name: "Cancelar pedido", exact: true }).click();
    modal = page.getByRole("dialog", { name: /Cancelar pedido/ }); await modal.getByLabel("Motivo do cancelamento").fill("Cliente desistiu da segunda camiseta.");
    await modal.getByRole("button", { name: "Cancelar pedido", exact: true }).click(); await expect(modal).toHaveCount(0);
    expect((await saldo()).every((m: { quantidadeReservada: number }) => m.quantidadeReservada === 0)).toBe(true);
    const movimentos = await (await page.request.get(`/api/estoque/materias-primas/${materiais[1].id}/movimentos`)).json();
    expect(movimentos.filter((m: { tipo: string }) => m.tipo === "SaidaPedido")).toHaveLength(1);
    expect(erros).toEqual([]);
  });
}
