import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { FormularioOrcamento } from "./FormularioOrcamento";
import type { ItemDetalhe, OrcamentoDetalhe } from "@/types/api";

const mocks = vi.hoisted(() => ({
  produtos: [
    { id: "p1", nome: "Produto A", categoria: "Canecas", descricaoBase: "Descrição do produto A" },
    { id: "p2", nome: "Produto B", categoria: "Canecas", descricaoBase: null },
    { id: "p3", nome: "Produto C", categoria: "Canecas", descricaoBase: "Descrição do produto C" },
  ],
  anexo: { id: "a1", nome: "arte.png", extensao: ".png", contentType: "image/png", tamanhoBytes: 1, ehImagem: true },
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), back: vi.fn() }),
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock("@/hooks", () => ({
  useDebounce: (value: unknown) => value,
  useProdutos: () => ({ data: mocks.produtos }),
  useOpcoesOrcamento: () => ({ data: { condicoesPagamento: ["À vista"], tiposPersonalizacao: ["Serigrafia"], tamanhoMaximoBytes: 20 * 1024 * 1024, extensoesPermitidas: [".png"] } }),
  useResponsaveis: () => ({ data: [{ id: "u1", nome: "Responsável" }] }),
  useUsuarioAtual: () => ({ data: { id: "u1" } }),
}));

vi.mock("@/services", () => ({
  anexosService: { enviar: vi.fn(async (_arquivo: File, progresso: (valor: number) => void) => { progresso(100); return mocks.anexo; }), remover: vi.fn(), url: vi.fn(() => "/anexos/a1") },
  clientesService: { obter: vi.fn() },
  orcamentosService: {
    calcular: vi.fn(async () => ({ itens: [], quantidadeItens: 0, totalUnidades: 0, subtotalProdutos: 0, valorPersonalizacao: 0, desconto: 0, valorTotal: 0, descontoExcedeValor: false })),
    criar: vi.fn(),
    atualizar: vi.fn(),
  },
}));

vi.mock("@/components/ui/toast", () => ({ useToast: () => vi.fn() }));

afterEach(() => {
  cleanup();
});

function renderFormulario(orcamento?: OrcamentoDetalhe) {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={cliente}>
      <FormularioOrcamento orcamento={orcamento} />
    </QueryClientProvider>,
  );
}

function linhaItem(numero: number) {
  const titulo = screen.getByText(`Item ${numero}`, { exact: false });
  const linha = titulo.closest(".p-5");
  if (!linha) throw new Error(`Linha do item ${numero} não encontrada.`);
  return Object.assign(within(linha as HTMLElement), { container: linha as HTMLElement });
}

function valor(campo: HTMLElement) {
  return (campo as HTMLInputElement).value;
}

function controle(numero: number, rotulo: string) {
  const campo = linhaItem(numero).getByText(rotulo, { exact: true }).parentElement?.querySelector("input, textarea, select");
  if (!campo) throw new Error(`Campo "${rotulo}" não encontrado no item ${numero}.`);
  return campo as HTMLElement;
}

async function adicionarItem(user: ReturnType<typeof userEvent.setup>) {
  await user.click(screen.getByRole("button", { name: "Adicionar item" }));
}

async function selecionarProduto(user: ReturnType<typeof userEvent.setup>, numero: number, id: string) {
  await user.selectOptions(controle(numero, "Produto"), id);
}

function itemExistente(id: string, produtoId: string, produtoNome: string, descricao: string, corPeca: string): ItemDetalhe {
  return {
    id, ordem: 1, produtoId, produtoNome, descricao, quantidade: 1, valorUnitario: 10,
    valorPersonalizacaoUnitario: 0, subtotalProduto: 10, totalPersonalizacao: 0, valorTotal: 10,
    personalizacao: { tipoPersonalizacao: "Serigrafia", corPeca, coresArte: null, medidas: "10 cm", localAplicacao: null, referenciaArte: null, observacoesTecnicas: null },
    anexos: [],
  };
}

function orcamentoExistente(itens: ItemDetalhe[]): OrcamentoDetalhe {
  return {
    id: "o1", numero: 1, situacao: "EmElaboracao",
    cliente: { id: "c1", nome: "Cliente", tipoPessoa: "PessoaFisica", documento: "123", contatoNome: null, contatoCargo: null, telefone: null, whatsApp: null, email: null, cidade: null },
    dataOrcamento: "2026-01-01", criadoEm: "2026-01-01T00:00:00Z", validade: "2026-01-15",
    prazoEstimadoDiasUteis: 15, previsaoEntrega: "2026-01-22", condicaoPagamento: "À vista", observacoesComerciais: null,
    responsavelId: "u1", responsavel: "Responsável", itens,
    resumo: { quantidadeItens: itens.length, totalUnidades: itens.length, subtotalProdutos: itens.length * 10, valorPersonalizacao: 0, desconto: 0, valorTotal: itens.length * 10 },
    decisao: null, pedido: null, aguardandoRetornoDesde: null, dataUltimaEdicao: null, podeSerEditado: true, versao: 1,
  };
}

describe("itens do orçamento", () => {
  it("substitui a descrição ao trocar o produto, inclusive após edição manual", async () => {
    const user = userEvent.setup();
    renderFormulario();
    await adicionarItem(user);
    await selecionarProduto(user, 1, "p1");
    const descricao = controle(1, "Descrição");
    expect(valor(descricao)).toBe("Descrição do produto A");

    await user.clear(descricao);
    await user.type(descricao, "Descrição escrita manualmente");
    await selecionarProduto(user, 1, "p2");
    expect(valor(descricao)).toBe("");
  });

  it("cria itens vazios e mantém campos e anexos ligados ao item após trocar e remover itens", async () => {
    const user = userEvent.setup();
    renderFormulario();
    await adicionarItem(user);
    await selecionarProduto(user, 1, "p1");
    await user.type(controle(1, "Cor da peça"), "Azul");
    await user.type(controle(1, "Medidas"), "5 cm");
    const arquivo = new File(["arte"], "arte.png", { type: "image/png" });
    await user.upload(linhaItem(1).container.querySelector('input[type="file"]')!, arquivo);
    expect(await screen.findByText("arte.png")).not.toBeNull();

    await adicionarItem(user);
    await selecionarProduto(user, 2, "p2");
    await user.type(controle(2, "Cor da peça"), "Verde");
    await user.type(controle(2, "Medidas"), "8 cm");
    await adicionarItem(user);
    await selecionarProduto(user, 3, "p3");

    await selecionarProduto(user, 2, "p3");
    expect(valor(controle(2, "Descrição"))).toBe("Descrição do produto C");
    await user.click(linhaItem(1).getByRole("button", { name: "Remover" }));

    expect(valor(controle(1, "Produto"))).toBe("p3");
    expect(valor(controle(1, "Cor da peça"))).toBe("Verde");
    expect(valor(controle(1, "Medidas"))).toBe("8 cm");
    expect(linhaItem(1).queryByText("arte.png")).toBeNull();
    expect(valor(controle(2, "Produto"))).toBe("p3");

    await adicionarItem(user);
    expect(valor(controle(3, "Produto"))).toBe("");
    expect(valor(controle(3, "Descrição"))).toBe("");
    expect(valor(controle(3, "Cor da peça"))).toBe("");
    expect(valor(controle(3, "Medidas"))).toBe("");
    expect(linhaItem(3).container.querySelectorAll('input[type="file"]')).toHaveLength(1);
    expect(linhaItem(3).queryByText("arte.png")).toBeNull();
  });

  it("preserva os dados dos itens existentes ao remover um item durante a edição", async () => {
    const user = userEvent.setup();
    renderFormulario(orcamentoExistente([
      itemExistente("i1", "p1", "Produto A", "Descrição 1", "Vermelha"),
      itemExistente("i2", "p3", "Produto C", "Descrição 2", "Preta"),
    ]));

    await user.click(linhaItem(1).getByRole("button", { name: "Remover" }));
    expect(valor(controle(1, "Produto"))).toBe("p3");
    expect(valor(controle(1, "Descrição"))).toBe("Descrição 2");
    expect(valor(controle(1, "Cor da peça"))).toBe("Preta");
  });

  it("ao editar um orçamento, a troca do produto substitui a descrição e preserva a personalização e os anexos", async () => {
    const user = userEvent.setup();
    const item = itemExistente("i1", "p1", "Produto A", "Descrição alterada anteriormente", "Azul");
    item.anexos = [mocks.anexo];
    renderFormulario(orcamentoExistente([item]));

    await selecionarProduto(user, 1, "p2");
    expect(valor(controle(1, "Descrição"))).toBe("");
    expect(valor(controle(1, "Cor da peça"))).toBe("Azul");
    expect(valor(controle(1, "Medidas"))).toBe("10 cm");
    expect(linhaItem(1).getByText("arte.png")).not.toBeNull();
  });
});
