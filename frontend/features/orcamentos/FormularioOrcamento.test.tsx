import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { FormularioOrcamento } from "./FormularioOrcamento";
import type { ItemDetalhe, OrcamentoDetalhe } from "@/types/api";

const mocks = vi.hoisted(() => ({
  produtos: [
    { id: "p1", nome: "Produto A", categoria: "Canecas", descricaoBase: "Descrição do produto A", precoVenda: 25, custo: 10 },
    { id: "p2", nome: "Produto B", categoria: "Canecas", descricaoBase: null, precoVenda: null, custo: null },
    { id: "p3", nome: "Produto C", categoria: "Canecas", descricaoBase: "Descrição do produto C", precoVenda: 60, custo: 25 },
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
  useUsuarioAtual: () => ({ data: { id: "u1", perfil: "Administrador", nome: "Responsável" } }),
}));

vi.mock("@/services", () => ({
  anexosService: { enviar: vi.fn(async (_arquivo: File, progresso: (valor: number) => void) => { progresso(100); return mocks.anexo; }), remover: vi.fn(), url: vi.fn(() => "/anexos/a1") },
  clientesService: { obter: vi.fn() },
  orcamentosService: {
    calcular: vi.fn(async () => ({ itens: [], quantidadeItens: 0, totalUnidades: 0, subtotalProdutos: 0, valorPersonalizacao: 0, desconto: 0, valorTotal: 0, descontoExcedeValor: false })),
    analisar: vi.fn(async () => ({ receita: 0, custoTotal: null, margemValor: null, margemPercentual: null, completa: false, itens: [] })),
    criar: vi.fn(),
    atualizar: vi.fn(),
  },
}));

vi.mock("@/features/produtos/FormularioProduto", () => ({
  FormularioProduto: ({ aoSalvar }: { aoSalvar: (p: unknown) => void }) => <button onClick={() => aoSalvar({ id: "p4", nome: "Produto novo", descricaoBase: "Nova descrição", precoVenda: 90, custo: 30 })}>Salvar produto simulado</button>,
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
  it("substitui a descrição ao trocar o produto, inclusive após edição manual", () => {
    renderFormulario();
    fireEvent.click(screen.getByRole("button", { name: "Adicionar item" }));
    fireEvent.change(controle(1, "Produto"), { target: { value: "p1" } });
    const descricao = controle(1, "Descrição");
    expect(valor(descricao)).toBe("Descrição do produto A");

    fireEvent.change(descricao, { target: { value: "Descrição escrita manualmente" } });
    fireEvent.change(controle(1, "Produto"), { target: { value: "p2" } });
    expect(valor(descricao)).toBe("");
  });

  it("cria itens vazios e mantém campos e anexos ligados ao item após trocar e remover itens", async () => {
    const user = userEvent.setup();
    renderFormulario();
    fireEvent.click(screen.getByRole("button", { name: "Adicionar item" }));
    fireEvent.change(controle(1, "Produto"), { target: { value: "p1" } });
    fireEvent.change(controle(1, "Cor da peça"), { target: { value: "Azul" } });
    fireEvent.change(controle(1, "Medidas"), { target: { value: "5 cm" } });
    const arquivo = new File(["arte"], "arte.png", { type: "image/png" });
    await user.upload(linhaItem(1).container.querySelector('input[type="file"]')!, arquivo);
    expect(await screen.findByText("arte.png")).not.toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Adicionar item" }));
    fireEvent.change(controle(2, "Produto"), { target: { value: "p2" } });
    fireEvent.change(controle(2, "Cor da peça"), { target: { value: "Verde" } });
    fireEvent.change(controle(2, "Medidas"), { target: { value: "8 cm" } });
    fireEvent.click(screen.getByRole("button", { name: "Adicionar item" }));
    fireEvent.change(controle(3, "Produto"), { target: { value: "p3" } });

    fireEvent.change(controle(2, "Produto"), { target: { value: "p3" } });
    expect(valor(controle(2, "Descrição"))).toBe("Descrição do produto C");
    fireEvent.click(linhaItem(1).getByRole("button", { name: "Remover" }));

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

  it("mostra o responsável original como texto e deixa a condição de pagamento vazia", () => {
    const orcamento = orcamentoExistente([itemExistente("i1", "p1", "Produto A", "Descrição", "Azul")]);
    orcamento.condicaoPagamento = null;
    renderFormulario(orcamento);

    const responsavel = screen.getAllByText("Responsável", { exact: true })[0];
    expect(responsavel.parentElement?.querySelector("select")).toBeNull();
    const campoCondicao = screen.getByText("Condição de pagamento", { exact: true }).parentElement?.querySelector("select");
    expect((campoCondicao as HTMLSelectElement).value).toBe("");
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
  it("aplica preço cadastrado, permite alteração e restaura; produto sem preço exige valor manual", async () => {
    const user = userEvent.setup();
    renderFormulario();
    await adicionarItem(user);
    await selecionarProduto(user, 1, "p1");
    expect(valor(controle(1, "Valor unitário (R$)"))).toBe("25,00");
    fireEvent.change(controle(1, "Valor unitário (R$)"), { target: { value: "35,00" } });
    expect(screen.getByText(/Valor diferente do preço cadastrado/)).not.toBeNull();
    await user.click(screen.getByRole("button", { name: "Restaurar preço cadastrado" }));
    expect(valor(controle(1, "Valor unitário (R$)"))).toBe("25,00");
    await selecionarProduto(user, 1, "p2");
    expect(valor(controle(1, "Valor unitário (R$)"))).toBe("");
    expect(screen.getByText("Produto sem preço cadastrado. Informe o valor unitário.")).not.toBeNull();
  });

  it("mantém preço histórico ao abrir edição mesmo com preço diferente no catálogo", () => {
    renderFormulario(orcamentoExistente([itemExistente("i1", "p1", "Produto A", "Descrição", "Azul")]));
    expect(valor(controle(1, "Valor unitário (R$)"))).toBe("10,00");
    expect(screen.getByRole("button", { name: "Restaurar preço cadastrado" })).not.toBeNull();
  });

  it("cria produto no modal preservando cliente, outros itens, valores, personalização e anexos", async () => {
    const user = userEvent.setup();
    const item = itemExistente("i1", "p1", "Produto A", "Descrição manual", "Azul");
    item.anexos = [mocks.anexo];
    renderFormulario(orcamentoExistente([item, itemExistente("i2", "p3", "Produto C", "Segundo item", "Verde")]));
    fireEvent.change(screen.getByRole("textbox", { name: "Desconto" }), { target: { value: "5,00" } });
    await user.click(linhaItem(1).getByRole("button", { name: "Novo produto neste item" }));
    await user.click(screen.getByRole("button", { name: "Salvar produto simulado" }));
    expect(valor(controle(1, "Produto"))).toBe("p4");
    expect(valor(controle(1, "Descrição"))).toBe("Descrição manual");
    expect(valor(controle(1, "Valor unitário (R$)"))).toBe("10,00");
    expect(valor(controle(1, "Cor da peça"))).toBe("Azul");
    expect(linhaItem(1).getByText("arte.png")).not.toBeNull();
    expect(valor(controle(2, "Produto"))).toBe("p3");
    expect(valor(controle(2, "Descrição"))).toBe("Segundo item");
    expect(valor(screen.getByRole("textbox", { name: "Desconto" }))).toBe("5,00");
    expect(screen.getByRole("link", { name: "Cliente" })).not.toBeNull();
  });

});
