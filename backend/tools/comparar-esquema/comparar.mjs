import fs from 'node:fs';

const read = file => JSON.parse(fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, ''));
const canonical = x => Array.isArray(x) ? x.map(canonical) : x && typeof x === 'object'
  ? Object.fromEntries(Object.keys(x).sort().map(k => [k, canonical(x[k])])) : x;
const equal = (a, b) => JSON.stringify(canonical(a)) === JSON.stringify(canonical(b));
const omit = (x, keys) => Object.fromEntries(Object.entries(x).filter(([k]) => !keys.includes(k)));

export function comparar(a, b) {
  const divergencias = [];
  for (const tipo of ['tables', 'columns', 'constraints', 'indexes', 'sequences']) {
    const key = x => tipo === 'tables' ? x.table : tipo === 'sequences' ? x.name : x.table + '.' + x.name;
    const ma = new Map((a[tipo] ?? []).map(x => [key(x), x]));
    const mb = new Map((b[tipo] ?? []).map(x => [key(x), x]));
    if (ma.size !== (a[tipo] ?? []).length || mb.size !== (b[tipo] ?? []).length)
      throw new Error('Objeto duplicado no catálogo: ' + tipo);
    for (const objeto of [...new Set([...ma.keys(), ...mb.keys()])].sort()) {
      const av = ma.get(objeto), bv = mb.get(objeto);
      if (!av || !bv) { divergencias.push({ tipo, objeto, a: av ?? null, b: bv ?? null }); continue; }
      // A ordem física é aceita; as definições das colunas continuam sendo comparadas integralmente.
      const aa = tipo === 'columns' ? omit(av, ['ordinal_position']) : av;
      const bb = tipo === 'columns' ? omit(bv, ['ordinal_position']) : bv;
      if (!equal(aa, bb)) divergencias.push({ tipo, objeto, a: aa, b: bb });
    }
  }
  const ordemFisicaIgnorada = (a.tables ?? []).flatMap(t => {
    const nomes = c => [...(c.columns ?? [])].filter(x => x.table === t.table)
      .sort((x, y) => x.ordinal_position - y.ordinal_position).map(x => x.name);
    const aa = nomes(a), bb = nomes(b);
    return equal(aa, bb) ? [] : [{ tabela: t.table, a: aa, b: bb }];
  });
  return { divergencias, ordemFisicaIgnorada, totais: Object.fromEntries(
    ['tables', 'columns', 'constraints', 'indexes', 'sequences'].map(k => [k, { a: a[k]?.length ?? 0, b: b[k]?.length ?? 0 }])) };
}

function identificadores(sql) {
  const nomes = [];
  let tabela = '';
  for (const linha of sql.split(/\r?\n/)) {
    const table = linha.match(/^CREATE TABLE "?([a-zA-Z_]\w*)"?/);
    if (table) { tabela = table[1]; nomes.push('tabela:' + tabela); continue; }
    const objeto = linha.match(/(?:CREATE\s+(?:UNIQUE\s+)?(INDEX|SEQUENCE)|CONSTRAINT)\s+"?([a-zA-Z_]\w*)"?/);
    if (objeto) { nomes.push((objeto[1]?.toLowerCase() ?? 'constraint') + ':' + objeto[2]); continue; }
    const coluna = linha.match(/^\s{4}([a-z_]\w*)\s+(?:uuid|character|numeric|text|integer|boolean|date|timestamp|bigint)\b/);
    if (coluna) nomes.push('coluna:' + tabela + '.' + coluna[1]);
  }
  return nomes.sort();
}

if (process.argv[1]?.endsWith('comparar.mjs')) {
  const [fileA, fileB, modo] = process.argv.slice(2);
  if (!fileA || !fileB) throw new Error('Uso: node comparar.mjs A.json B.json [--nomes-sql]');
  if (modo === '--nomes-sql') {
    const antes = identificadores(fs.readFileSync(fileA, 'utf8'));
    const depois = identificadores(fs.readFileSync(fileB, 'utf8'));
    const removidos = antes.filter(n => !depois.includes(n));
    const adicionados = depois.filter(n => !antes.includes(n));
    console.log(JSON.stringify({ antes, depois, removidos, adicionados }, null, 2));
  } else {
    const resultado = comparar(read(fileA), read(fileB));
    console.log(JSON.stringify(resultado, null, 2));
    process.exitCode = resultado.divergencias.length ? 1 : 0;
  }
}

