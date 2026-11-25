import { expect, test, type Page } from "@playwright/test";
import { capturar, conferirModal, criarOrcamento, entrar, tecladoModal } from "./apoio";

const falha = { status: 503, code: "REVISAO_INDISPONIVEL", message: "Serviço temporariamente indisponível na verificação.", errors: {} };

async function fixture(page: Page) {
  const nome = `Acabamento ${Date.now()}`;
  const r = await page.request.post("/api/estoque/materias-primas", { data: { nome, unidade: "un", quantidadeDisponivel: 5, estoqueMinimo: 2 } });
  expect(r.status()).toBe(201); const material = await r.json();
  const p = await page.request.post("/api/produtos", { data: { nome, categoria: "Camiseta", materiais: [] } });
  expect(p.status()).toBe(201); const produto = await p.json();
  const c = await page.request.post("/api/estoque/compras", { data: { materiaPrimaId: material.id, quantidade: 2, dataPrevista: new Date().toISOString().slice(0, 10) } });
  expect(c.status()).toBe(201);
  return { nome, material, produto };
}

test("R8: teclado e retorno do foco em catálogo, estoque e configurações", async ({ page }) => {
  test.setTimeout(120_000); await entrar(page, true);
  const { nome, produto } = await fixture(page);
  await page.goto("/produtos");
  const pesquisa = page.waitForResponse(r => new URL(r.url()).pathname === "/api/produtos/catalogo" && new URL(r.url()).searchParams.get("busca") === nome);
  await page.getByRole("textbox", { name: "Pesquisar produtos" }).fill(nome); await pesquisa;
  await conferirModal(page, page.getByRole("button", { name: "Novo produto", exact: true }), "Novo produto");
  for (const [acao, titulo] of [["Editar", "Editar produto"], ["Duplicar", "Duplicar produto"], ["Inativar", "Inativar produto"], ["Excluir", "Excluir produto definitivamente"]])
    await conferirModal(page, page.getByRole("button", { name: `${acao} ${nome}`, exact: true }), titulo);
  expect((await page.request.patch(`/api/produtos/${produto.id}/status`, { data: { ativo: false, versao: produto.versao } })).status()).toBe(200);
  await page.reload(); await page.getByRole("checkbox", { name: "Mostrar inativos" }).check();
  const inativos = page.waitForResponse(r => new URL(r.url()).pathname === "/api/produtos/catalogo" && new URL(r.url()).searchParams.get("busca") === nome);
  await page.getByRole("textbox", { name: "Pesquisar produtos" }).fill(nome); await inativos;
  await conferirModal(page, page.getByRole("button", { name: `Reativar ${nome}`, exact: true }), "Reativar produto");
  await page.goto("/estoque"); await page.getByRole("textbox", { name: "Buscar matérias-primas" }).fill(nome);
  await conferirModal(page, page.getByRole("button", { name: "Nova matéria-prima", exact: true }), "Nova matéria-prima");
  await conferirModal(page, page.getByRole("button", { name: `Editar matéria-prima ${nome}`, exact: true }), "Editar matéria-prima");
  await conferirModal(page, page.getByRole("button", { name: `Planejar compra de ${nome}`, exact: true }), "Planejar compra de matéria-prima");
  await conferirModal(page, page.getByRole("button", { name: `Ver movimentações de ${nome}`, exact: true }), `Movimentações · ${nome}`);
  await conferirModal(page, page.getByRole("button", { name: `Cancelar compra de ${nome}`, exact: true }), "Cancelar compra");
  await page.goto("/configuracoes");
  await conferirModal(page, page.getByRole("button", { name: "Nova opção", exact: true }), "Nova opção");
  await conferirModal(page, page.getByRole("button", { name: /^Editar / }).filter({ hasNotText: "modelo" }).first(), "Editar opção");
  for (const canal of ["e-mail", "WhatsApp"]) await conferirModal(page, page.getByRole("button", { name: `Editar modelo de ${canal}`, exact: true }), "Editar modelo de mensagem");
  await capturar(page, "configuracoes");
});

test("R8: teclado nos diálogos de proposta, decisão, confirmações e pedido", async ({ page }) => {
  test.setTimeout(120_000); await entrar(page); const o = await criarOrcamento(page);
  await page.goto(`/orcamentos/${o.id}`);
  await conferirModal(page, page.getByRole("button", { name: "Registrar contato", exact: true }), "Registrar contato");
  await conferirModal(page, page.getByRole("button", { name: "Cancelar orçamento", exact: true }), "Cancelar orçamento");
  await conferirModal(page, page.getByRole("button", { name: "Apresentar ao cliente", exact: true }), "Enviar proposta");
  await page.getByRole("button", { name: "Apresentar ao cliente", exact: true }).click();
  await page.getByRole("dialog", { name: "Enviar proposta", exact: true }).getByRole("button", { name: "Registrar por outro meio" }).click();
  let modal = page.getByRole("dialog", { name: "Apresentar proposta ao cliente", exact: true }); await tecladoModal(page, modal);
  await modal.getByRole("button", { name: "Marcar como apresentado", exact: true }).click();
  modal = page.getByRole("dialog", { name: "Proposta apresentada ao cliente", exact: true }); await tecladoModal(page, modal);
  await page.keyboard.press("Escape");
  const decisao = page.getByRole("button", { name: "Registrar decisão", exact: true });
  await conferirModal(page, decisao, "Registrar decisão do cliente");
  await decisao.click(); modal = page.getByRole("dialog", { name: "Registrar decisão do cliente", exact: true });
  await modal.getByRole("radio", { name: "Aprovado", exact: true }).click(); await modal.getByRole("radio", { name: "WhatsApp" }).click();
  await modal.getByRole("button", { name: "Continuar", exact: true }).click();
  modal = page.getByRole("dialog", { name: "Aprovar orçamento e gerar pedido", exact: true }); await tecladoModal(page, modal);
  // A falha simulada fica antes da API: nenhuma aprovação real deve ocorrer.
  await page.route(`**/api/orcamentos/${o.id}/decisao`, route => route.fulfill({ status: 503, json: falha }));
  await modal.getByRole("button", { name: "Aprovar e gerar pedido", exact: true }).click();
  modal = page.getByRole("dialog", { name: "Não foi possível aprovar o orçamento", exact: true }); await tecladoModal(page, modal);
  await modal.getByRole("button", { name: "Tentar novamente", exact: true }).click();
  await page.unroute(`**/api/orcamentos/${o.id}/decisao`);
  modal = page.getByRole("dialog", { name: "Aprovar orçamento e gerar pedido", exact: true });
  const aprovado = page.waitForResponse(r => r.url().endsWith(`/api/orcamentos/${o.id}/decisao`) && r.request().method() === "POST");
  await modal.getByRole("button", { name: "Aprovar e gerar pedido" }).click(); const r = await aprovado; expect(r.status()).toBe(200); const pedido = (await r.json()).pedidoGerado;
  modal = page.getByRole("dialog", { name: "Pedido gerado com sucesso", exact: true }); await tecladoModal(page, modal); await page.keyboard.press("Escape");
  await page.goto(`/pedidos/${pedido.id}`);
  await conferirModal(page, page.getByRole("button", { name: "Atualizar status", exact: true }), /Atualizar status do pedido/);
  await conferirModal(page, page.getByRole("button", { name: "Cancelar pedido", exact: true }), /Cancelar pedido/);
});

test("R8: menu móvel contém o foco, fecha por Escape e abre rotas por teclado", async ({ page }) => {
  await entrar(page); await page.setViewportSize({ width: 360, height: 900 });
  const abrir = page.getByRole("button", { name: "Abrir menu", exact: true });
  await conferirModal(page, abrir, "Menu de navegação");
  await abrir.click(); const modal = page.getByRole("dialog", { name: "Menu de navegação" });
  await modal.getByRole("link", { name: "Clientes", exact: true }).focus(); await page.keyboard.press("Enter");
  await expect(page).toHaveURL(/\/clientes$/); await expect(modal).toHaveCount(0);
  await abrir.click(); await page.setViewportSize({ width: 1280, height: 900 });
  await expect(modal).toHaveCount(0); await expect(page.getByRole("heading", { name: "Clientes", exact: true })).toBeFocused();
  await capturar(page, "clientes-lista");
  await page.goto("/clientes/novo"); const identificacao = page.getByRole("tab", { name: "Identificação", exact: true });
  await identificacao.focus(); await page.keyboard.press("ArrowRight");
  await expect(page.getByRole("tab", { name: "Contato", exact: true })).toHaveAttribute("aria-selected", "true");
  await capturar(page, "cliente-contato");
});

test("R8: consultas principais mostram carregamento, erro, recuperação e vazio", async ({ page }) => {
  test.setTimeout(120_000); await entrar(page, true);
  for (const [tela, api, vazio] of [["/clientes", "/api/clientes", "Nenhum cliente cadastrado"], ["/orcamentos?periodo=todos", "/api/orcamentos", "Nenhum orçamento encontrado"], ["/pedidos?periodo=todos", "/api/pedidos", "Nenhum pedido encontrado"], ["/produtos", "/api/produtos/catalogo", "Nenhum produto encontrado"], ["/estoque", "/api/estoque/materias-primas", "Nenhuma matéria-prima"], ["/configuracoes", "/api/configuracoes/opcoes", "Nenhuma opção cadastrada"]]) {
    const respostaReal = await page.request.get(api); expect(respostaReal.ok()).toBe(true); const dados = await respostaReal.json();
    let modo = "lento"; let liberar!: () => void; const espera = new Promise<void>(r => liberar = r);
    await page.route(u => u.pathname === api, async route => {
      if (modo === "lento") await espera;
      if (modo !== "vazio") return route.fulfill({ status: 503, json: falha });
      return route.fulfill({ json: Array.isArray(dados) ? [] : { ...dados, itens: [], total: 0, totalSemFiltro: 0, totalPaginas: 0, contagemPorSituacao: { Todos: 0 } } });
    });
    await page.goto(tela); await expect(page.locator('[role="status"], .animate-pulse').first()).toBeVisible(); liberar(); modo = "erro";
    const erro = page.getByRole("alert").filter({ hasText: "Não foi possível carregar" }).first(); await expect(erro).toBeVisible();
    if (api === "/api/clientes") await capturar(page, "clientes-erro");
    modo = "vazio"; await erro.getByRole("button", { name: "Tentar novamente" }).click();
    await expect(page.getByText(vazio, { exact: false }).first()).toBeVisible();
    if (api === "/api/clientes") await capturar(page, "clientes-vazio");
    await page.unrouteAll({ behavior: "wait" });
  }
  await page.route("**/api/dashboard", route => route.fulfill({ status: 503, json: falha }));
  await page.goto("/dashboard"); await expect(page.getByRole("alert").filter({ hasText: "Não foi possível carregar" })).toBeVisible();
  await expect(page.locator(".animate-pulse")).toHaveCount(0);
  await page.unrouteAll({ behavior: "wait" }); await page.getByRole("button", { name: "Tentar novamente" }).click();
  await expect(page.getByRole("heading", { name: "Vendas e operação" })).toBeVisible();
});

test("R8: falhas auxiliares e de gravação têm retorno e preservam o formulário", async ({ page }) => {
  test.setTimeout(120_000); await entrar(page, true);
  await page.route("**/api/auth/me", route => route.fulfill({ status: 503, json: falha }));
  const sessaoIndisponivel = page.waitForResponse(r => new URL(r.url()).pathname === "/api/auth/me" && r.status() === 503);
  await page.goto("/produtos"); await sessaoIndisponivel; await expect(page.getByRole("alert").filter({ hasText: falha.message })).toBeVisible(); await expect(page).toHaveURL(/\/produtos$/);
  await page.unrouteAll({ behavior: "wait" }); await page.getByRole("button", { name: "Tentar novamente" }).click(); await expect(page.getByRole("heading", { name: "Produtos", exact: true })).toBeVisible();
  await page.route("**/api/usuarios/responsaveis", route => route.fulfill({ status: 503, json: falha }));
  const responsaveisIndisponiveis = page.waitForResponse(r => new URL(r.url()).pathname === "/api/usuarios/responsaveis" && r.status() === 503);
  await page.goto("/pedidos"); await responsaveisIndisponiveis; await expect(page.getByText("Não foi possível carregar os responsáveis.", { exact: true })).toBeVisible();
  await page.unrouteAll({ behavior: "wait" });
  await page.route("**/api/clientes/cidades", route => route.fulfill({ status: 503, json: falha }));
  const cidadesIndisponiveis = page.waitForResponse(r => new URL(r.url()).pathname === "/api/clientes/cidades" && r.status() === 503);
  await page.goto("/clientes"); await cidadesIndisponiveis; await expect(page.getByText("Não foi possível carregar as cidades.", { exact: true })).toBeVisible();
  await page.unrouteAll({ behavior: "wait" });
  await page.route(u => u.pathname === "/api/produtos/opcoes", route => route.fulfill({ status: 503, json: falha }));
  const opcoesIndisponiveis = page.waitForResponse(r => new URL(r.url()).pathname === "/api/produtos/opcoes" && r.status() === 503);
  await page.goto("/orcamentos/novo"); await opcoesIndisponiveis; await expect(page.getByText("Não foi possível carregar os produtos e as opções do orçamento.", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Salvar orçamento", exact: true })).toBeDisabled();
  await page.unrouteAll({ behavior: "wait" });
  await page.goto("/produtos"); await page.getByRole("button", { name: "Novo produto", exact: true }).click();
  const modal = page.getByRole("dialog", { name: "Novo produto", exact: true });
  const nome = `Preservar formulário ${Date.now()}`; await modal.getByLabel("Nome do produto").fill(nome); await modal.getByLabel("Categoria", { exact: false }).selectOption("Camiseta");
  await page.route(u => u.pathname === "/api/produtos", route => route.fulfill({ status: 503, json: falha }));
  await modal.getByRole("button", { name: "Salvar produto", exact: true }).click();
  await expect(modal.getByRole("alert")).toContainText(falha.message); await expect(modal.getByLabel("Nome do produto")).toHaveValue(nome);
  await page.unrouteAll({ behavior: "wait" }); await modal.getByRole("button", { name: "Salvar produto", exact: true }).click(); await expect(modal).toHaveCount(0);
  let liberar!: () => void; const espera = new Promise<void>(r => liberar = r);
  await page.route("**/api/auth/logout", async route => { await espera; await route.fulfill({ status: 503, json: falha }); });
  await page.getByRole("button", { name: "Sair", exact: true }).click(); await expect(page.getByRole("button", { name: "Saindo…", exact: true })).toBeDisabled(); liberar();
  await expect(page.getByText(`Não foi possível sair. ${falha.message}`, { exact: true })).toBeVisible(); await expect(page).toHaveURL(/\/produtos$/);
  await page.unrouteAll({ behavior: "wait" }); await page.getByRole("button", { name: "Sair", exact: true }).click(); await expect(page).toHaveURL(/\/login$/);
});

test("R8: rotas e abas restantes em 360 e 1280 px", async ({ page }) => {
  test.setTimeout(120_000); await entrar(page, true); const o = await criarOrcamento(page);
  const clienteId = o.cliente.id;
  for (const [rota, nome] of [["/orcamentos?periodo=todos", "orcamentos-lista"], ["/pedidos/novo", "pedido-manual-bloqueado"], [`/clientes/${clienteId}/editar`, "cliente-editar"], [`/orcamentos/${o.id}/editar`, "orcamento-editar"], ...["visao-geral", "orcamentos", "pedidos", "observacoes", "historico"].map(aba => [`/clientes/${clienteId}?aba=${aba}`, `cliente-${aba}`])]) {
    const api = rota.startsWith("/clientes/") ? `/api/clientes/${clienteId}` : rota.includes("/editar") ? `/api/orcamentos/${o.id}` : null;
    const dados = api ? page.waitForResponse(r => new URL(r.url()).pathname === api && r.ok()) : null;
    await page.goto(rota); if (dados) await dados; await expect(page.locator("main h1")).toBeVisible(); await expect(page.getByText("Carregando...", { exact: true })).toHaveCount(0);
    await capturar(page, nome);
  }
  await page.context().clearCookies(); await page.goto("/login"); await capturar(page, "login");
});

test("R8: erros de detalhes, abas, materiais e históricos podem ser recuperados", async ({ page }) => {
  test.setTimeout(180_000); await entrar(page, true); const o = await criarOrcamento(page); const { nome, material } = await fixture(page);
  async function conferir(tela: string, api: string, abrir?: () => Promise<void>) {
    await page.route(u => u.pathname === api, route => route.fulfill({ status: 503, json: falha }));
    const bloqueada = page.waitForResponse(r => new URL(r.url()).pathname === api && r.status() === 503);
    await page.goto(tela); if (abrir) await abrir(); await bloqueada;
    const erro = page.getByRole("alert").filter({ hasText: "Não foi possível carregar" }).first(); await expect(erro).toBeVisible();
    await page.unrouteAll({ behavior: "wait" }); await erro.getByRole("button", { name: "Tentar novamente" }).click();
    await expect(erro).toHaveCount(0);
  }
  for (const [aba, sufixo] of [["visao-geral", "visao-geral"], ["orcamentos", "orcamentos"], ["pedidos", "pedidos"], ["observacoes", "observacoes"], ["historico", "historico"]])
    await conferir(`/clientes/${o.cliente.id}?aba=${aba}`, `/api/clientes/${o.cliente.id}/${sufixo}`);
  await conferir(`/clientes/${o.cliente.id}`, `/api/clientes/${o.cliente.id}`);
  await conferir(`/orcamentos/${o.id}`, `/api/orcamentos/${o.id}`);
  await conferir(`/orcamentos/${o.id}`, `/api/orcamentos/${o.id}/historico`);
  await conferir(`/orcamentos/${o.id}`, `/api/orcamentos/${o.id}/analise-interna`);
  await conferir("/estoque", "/api/estoque/pendencias"); await conferir("/estoque", "/api/estoque/compras");
  await conferir("/estoque", `/api/estoque/materias-primas/${material.id}/movimentos`, async () => {
    await page.getByRole("textbox", { name: "Buscar matérias-primas" }).fill(nome);
    await page.getByRole("button", { name: `Ver movimentações de ${nome}`, exact: true }).click();
  });
  await conferir("/configuracoes", "/api/configuracoes/modelos-proposta");
  await conferir(`/orcamentos/${o.id}`, `/api/orcamentos/${o.id}/proposta`, async () => page.getByRole("button", { name: "Apresentar ao cliente", exact: true }).click());
  const apresentado = await (await page.request.post(`/api/orcamentos/${o.id}/aguardando-retorno`, { data: { versao: o.versao, meioApresentacao: "WhatsApp" } })).json();
  const r = await page.request.post(`/api/orcamentos/${o.id}/decisao`, { data: { versao: apresentado.versao, resultado: "Aprovado", meioContato: "WhatsApp", dataHora: new Date().toISOString() } });
  expect(r.status()).toBe(200); const pedido = (await r.json()).pedidoGerado;
  await conferir(`/pedidos/${pedido.id}`, `/api/pedidos/${pedido.id}`);
  await conferir(`/pedidos/${pedido.id}`, `/api/pedidos/${pedido.id}/historico`);
  await conferir(`/pedidos/${pedido.id}`, `/api/pedidos/${pedido.id}/materiais`);
});
