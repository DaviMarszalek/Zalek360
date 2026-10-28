# Verificação opcional de aceite PDF

Use somente uma instância **isolada de demonstração**: o teste apresenta o #000123, anexa uma arte, registra exportações e cria rascunhos para conferir paginação. Não use contra dados de trabalho.

Pré-requisitos: Node, `playwright-core` e um Chrome/Chromium local. Não há dependências adicionais no frontend do produto. Configure `PLAYWRIGHT_MODULE` com o caminho do pacote e `CHROME_PATH` com o executável. `ZALEK_URL` usa `http://localhost:13000` por padrão; `ARTEFATOS_PDF` usa `data/validacao-pdf`.

```sh
node backend/tools/verificacao-pdf/verificar-interface.cjs
```

O script verifica login real, nome do download, abertura em nova aba, conversão de erro JSON/blob, histórico do cliente e o atalho após apresentação (este último somente se o #000123 ainda estiver em elaboração). Gera PDFs de todas as situações e um orçamento com 12 itens.

Com Python e `pymupdf`, confira o documento independentemente do gerador:

```sh
python backend/tools/verificacao-pdf/inspecionar_pdf.py data/validacao-pdf
```

A inspeção verifica A4, fontes, número de páginas, totais iguais à API, marcas de rascunho, ausência de previsão em propostas encerradas, prazo do pedido aprovado, repetição do cabeçalho e itens inteiros na tabela extensa. Também exporta imagens para revisão visual. Os artefatos não são versionados.
