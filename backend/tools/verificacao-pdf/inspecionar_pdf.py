"""Inspeção independente dos PDFs produzidos pelo teste de navegador (pip install pymupdf)."""
import json
import pathlib
import re
import sys
import pymupdf

raiz = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else "data/validacao-pdf")
resumo = json.loads((raiz / "valores-tela.json").read_text())

def moeda(valor):
    return "R$ " + f"{valor:,.2f}".replace(",", "_").replace(".", ",").replace("_", ".")

relatorio = []
for arquivo in sorted(raiz.glob("*.pdf")):
    with pymupdf.open(arquivo) as pdf:
        textos = [p.get_text() for p in pdf]
        texto = "\n".join(textos)
        if "paginacao" in arquivo.name:
            assert len(pdf) > 2
            for p in textos:
                if "Item de paginação" in p:
                    assert "Unitário" in p and "Pers./un." in p and "Qtd." in p
            for i in range(1, 13):
                paginas = [n for n, p in enumerate(textos) if f"Início do bloco {i}." in p]
                assert len(paginas) == 1
                assert f"Fim do bloco {i}." in textos[paginas[0]], ("Item dividido", i)
        else:
            assert 1 <= len(pdf) <= 2, (arquivo.name, len(pdf))
        assert "Proposta Comercial" in texto
        assert all(f"Página {i + 1} de {len(pdf)}" in p for i, p in enumerate(textos))
        assert all(abs(p.rect.width - 595.28) < 1 for p in pdf)
        assert "Luciana pediu" not in texto and "Enviada a revisão" not in texto
        assert not re.search(r"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-", texto)
        if "rascunho" in arquivo.name:
            assert "RASCUNHO" in texto
        if arquivo.name == "proposta-comercial.pdf":
            assert "RASCUNHO" not in texto
            assert "15 dias úteis" in texto and "3 semanas" in texto
            assert "Previsão se aprovada hoje" in texto and "último dia de validade" in texto
            for valor in ("subtotalProdutos", "valorPersonalizacao", "valorTotal"):
                assert moeda(resumo[valor]) in texto, (valor, moeda(resumo[valor]))
            assert "arte-demo.png" in texto
        if any(s in arquivo.name for s in ("recusado", "expirado", "cancelado")):
            assert "Previsão se aprovada" not in texto and "último dia de validade" not in texto
            assert "após a aprovação" not in texto and "Para aprovar" not in texto
        if "aprovado" in arquivo.name:
            assert "APROVADO" in texto and "Pedido nº" in texto
            assert "Previsão se aprovada" not in texto
        for i, pagina in enumerate(pdf):
            pagina.get_pixmap(matrix=pymupdf.Matrix(1.3, 1.3)).save(raiz / f"{arquivo.stem}-{i + 1}.png")
            # Todos os blocos devem estar dentro da página.
            assert all(pagina.rect.contains(pymupdf.Rect(b[:4])) for b in pagina.get_text("blocks"))
        (raiz / f"{arquivo.stem}.txt").write_text(texto, encoding="utf-8")
        relatorio.append({"arquivo": arquivo.name, "paginas": len(pdf), "bytes": arquivo.stat().st_size,
                          "fontes": sorted({f[3] for p in pdf for f in p.get_fonts()})})
print(json.dumps(relatorio, ensure_ascii=False, indent=2))
(raiz / "inspecao.json").write_text(json.dumps(relatorio, ensure_ascii=False, indent=2), encoding="utf-8")
