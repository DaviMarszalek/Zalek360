// Verificação opcional contra uma instância de DEMONSTRAÇÃO isolada: altera o #000123.
// PLAYWRIGHT_MODULE: caminho para playwright-core; CHROME_PATH: executável Chromium local.
// ZALEK_URL: padrão http://localhost:13000; ARTEFATOS_PDF: pasta para PDFs e screenshots.
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright-core');
const fs = require('node:fs/promises');
const path = require('node:path');
const assert = require('node:assert/strict');

(async () => {
  const destino = process.env.ARTEFATOS_PDF || path.resolve('data/validacao-pdf');
  await fs.mkdir(destino, { recursive: true });
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH, headless: true });
  const context = await browser.newContext({ baseURL: process.env.ZALEK_URL || 'http://localhost:13000', acceptDownloads: true, viewport: { width: 1500, height: 1000 } });
  const page = await context.newPage();
  const erros = [];
  page.on('pageerror', e => erros.push(e.message));
  try {
    await page.goto('/login');
    await page.getByLabel('E-mail', { exact: true }).fill('maria.silva@zalekpersonalizados.com.br');
    await page.getByLabel('Senha', { exact: true }).fill('Zalek@2026');
    await page.getByRole('button', { name: 'Entrar', exact: true }).click();
    await page.waitForURL('**/dashboard');
    const lista = await (await context.request.get('/api/orcamentos?busca=123')).json();
    const resumo = lista.itens.find(o => o.numero === 123);
    assert.ok(resumo, 'Orçamento 123 deve existir no seed');
    let o = await (await context.request.get(`/api/orcamentos/${resumo.id}`)).json();

    // Anexa uma imagem ao primeiro dos três itens usando o mesmo contrato do formulário.
    let atalhoApresentacao = false;
    if (o.situacao === 'EmElaboracao') {
      const upload = await context.request.post('/api/anexos', { multipart: { arquivo: {
        name: 'arte-demo.png', mimeType: 'image/png',
        buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jXioAAAAASUVORK5CYII=', 'base64'),
      } } });
      assert.equal(upload.status(), 201, await upload.text());
      const arte = await upload.json();
      const atualizacao = await context.request.put(`/api/orcamentos/${o.id}`, { data: {
        clienteId: o.cliente.id, responsavelId: o.responsavelId, validade: o.validade,
        prazoEstimadoDiasUteis: o.prazoEstimadoDiasUteis, condicaoPagamento: o.condicaoPagamento,
        observacoesComerciais: o.observacoesComerciais, desconto: o.resumo.desconto, versao: o.versao,
        itens: o.itens.map((i, index) => ({ id: i.id, produtoId: i.produtoId, descricao: i.descricao,
          quantidade: i.quantidade, valorUnitario: i.valorUnitario, valorPersonalizacaoUnitario: i.valorPersonalizacaoUnitario,
          ...i.personalizacao, anexoIds: [...i.anexos.map(a => a.id), ...(index === 0 ? [arte.id] : [])] })),
      } });
      assert.equal(atualizacao.status(), 200, await atualizacao.text());
      o = await atualizacao.json();
    }
    await page.goto(`/orcamentos/${o.id}`);
    await page.getByRole('button', { name: 'Exportar PDF', exact: true }).waitFor();
    await page.screenshot({ path: path.join(destino, 'detalhe-orcamento.png'), fullPage: true });
    let baixou = page.waitForEvent('download');
    await page.getByRole('button', { name: 'Exportar PDF', exact: true }).click();
    let arquivo = await baixou;
    assert.match(arquivo.suggestedFilename(), /^Orcamento-000123-.*\.pdf$/);
    await arquivo.saveAs(path.join(destino, o.situacao === 'EmElaboracao' ? 'proposta-rascunho.pdf' : 'proposta-comercial.pdf'));
    await page.getByText('Exportado em PDF', { exact: true }).first().waitFor();

    const abriu = context.waitForEvent('page');
    const respondeu = page.waitForResponse(r => r.url().includes('/exportacoes?inline=true'));
    await page.getByRole('button', { name: 'Abrir em nova aba', exact: true }).click();
    const aba = await abriu;
    assert.equal((await respondeu).status(), 200);
    await aba.waitForURL(url => url.protocol === 'blob:', { waitUntil: 'commit' });
    await aba.close();

    // Erro JSON recebido como Blob deve aparecer com a mensagem da API.
    const padrao = '**/api/orcamentos/*/exportacoes*';
    await page.route(padrao, route => route.fulfill({ status: 422, contentType: 'application/json',
      body: JSON.stringify({ status: 422, code: 'TESTE_PDF', message: 'Mensagem de erro PDF preservada.', errors: {} }) }));
    await page.getByRole('button', { name: 'Exportar PDF', exact: true }).click();
    await page.getByText('Mensagem de erro PDF preservada.', { exact: true }).waitFor();
    await page.unroute(padrao);

    if (o.situacao === 'EmElaboracao') {
      await page.getByRole('button', { name: 'Apresentar ao cliente', exact: true }).click();
      await page.getByRole('button', { name: 'Marcar como apresentado', exact: true }).click();
      const atalho = page.getByRole('button', { name: 'Exportar PDF para enviar ao cliente', exact: true });
      await atalho.waitFor();
      baixou = page.waitForEvent('download');
      await atalho.click();
      arquivo = await baixou;
      await arquivo.saveAs(path.join(destino, 'proposta-comercial.pdf'));
      atalhoApresentacao = true;
      await page.getByRole('button', { name: 'Fechar', exact: true }).first().click();
    }
    const historico = await (await context.request.get(`/api/clientes/${o.cliente.id}/historico`)).json();
    assert.ok(historico.some(e => e.tipo === 'OrcamentoExportadoPdf'));
    const detalhes = await (await context.request.get(`/api/orcamentos/${o.id}`)).json();
    await fs.writeFile(path.join(destino, 'valores-tela.json'), JSON.stringify(detalhes.resumo, null, 2));
    // Exporta todos os exemplos do seed sem registrar eventos (GET).
    const todos = await (await context.request.get('/api/orcamentos?tamanhoPagina=100')).json();
    if (o.situacao !== 'EmElaboracao') {
      const novo = await context.request.post('/api/orcamentos', { data: {
        clienteId: detalhes.cliente.id, responsavelId: detalhes.responsavelId, validade: detalhes.validade,
        prazoEstimadoDiasUteis: detalhes.prazoEstimadoDiasUteis, condicaoPagamento: detalhes.condicaoPagamento,
        observacoesComerciais: detalhes.observacoesComerciais, desconto: detalhes.resumo.desconto, versao: null,
        itens: detalhes.itens.map(i => ({ id: null, produtoId: i.produtoId, descricao: i.descricao,
          quantidade: i.quantidade, valorUnitario: i.valorUnitario, valorPersonalizacaoUnitario: i.valorPersonalizacaoUnitario,
          ...i.personalizacao, anexoIds: [] })),
      } });
      assert.equal(novo.status(), 201, await novo.text());
      const rascunho = await novo.json();
      const resposta = await context.request.get(`/api/orcamentos/${rascunho.id}/exportar-pdf`);
      assert.equal(resposta.status(), 200);
      await fs.writeFile(path.join(destino, 'proposta-rascunho.pdf'), await resposta.body());
    }
    for (const situacao of ['Aprovado', 'Recusado', 'Expirado', 'Cancelado']) {
      const exemplo = todos.itens.find(x => x.situacao === situacao);
      assert.ok(exemplo, `Seed deve conter ${situacao}`);
      const resposta = await context.request.get(`/api/orcamentos/${exemplo.id}/exportar-pdf?inline=true`);
      assert.equal(resposta.status(), 200);
      await fs.writeFile(path.join(destino, `proposta-${situacao.toLowerCase()}.pdf`), await resposta.body());
    }
    // Força a tabela a atravessar páginas para conferir a repetição dos títulos fora do renderer.
    const base = detalhes.itens[0];
    const criado = await context.request.post('/api/orcamentos', { data: {
      clienteId: detalhes.cliente.id, responsavelId: detalhes.responsavelId, validade: detalhes.validade,
      prazoEstimadoDiasUteis: detalhes.prazoEstimadoDiasUteis, condicaoPagamento: detalhes.condicaoPagamento,
      observacoesComerciais: null, desconto: 0, versao: null,
      itens: Array.from({ length: 12 }, (_, index) => ({ id: null, produtoId: base.produtoId,
        descricao: `Item de paginação ${index + 1}. Camisa para conferência de paginação da proposta.`,
        quantidade: 1, valorUnitario: base.valorUnitario, valorPersonalizacaoUnitario: base.valorPersonalizacaoUnitario,
        ...base.personalizacao, anexoIds: [],
        observacoesTecnicas: `Início do bloco ${index + 1}. ` + 'Verificar cor e posição da arte. '.repeat(12) + `Fim do bloco ${index + 1}.`,
      })),
    } });
    assert.equal(criado.status(), 201, await criado.text());
    const extenso = await criado.json();
    const extensoPdf = await context.request.get(`/api/orcamentos/${extenso.id}/exportar-pdf`);
    assert.equal(extensoPdf.status(), 200);
    await fs.writeFile(path.join(destino, 'proposta-paginacao.pdf'), await extensoPdf.body());
    await page.screenshot({ path: path.join(destino, 'historico-exportado.png'), fullPage: true });
    assert.deepEqual(erros, []);
    console.log(JSON.stringify({ resultado: 'OK', download: true, novaAba: true, erroBlob: true,
      atalhoApresentacao, historicoCliente: true, tresItensComArte: true, pdfMultipaginaGerado: true, artefatos: destino }));
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
