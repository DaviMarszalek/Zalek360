import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { CartaoCliente } from "./index";
import type { ClienteCard } from "@/types/api";

const base: ClienteCard = {
  id: "c1", nome: "Academia Movimento", tipoPessoa: "PessoaJuridica", documento: "38221907000153",
  contatoNome: null, contatoCargo: null, telefone: null, whatsApp: null, email: null, cidade: null,
};

afterEach(cleanup);

describe("CartaoCliente", () => {
  it("mostra a IE junto do CNPJ quando o cliente PJ tem inscrição estadual", () => {
    render(<CartaoCliente cliente={{ ...base, inscricaoEstadual: "123.456.789" }} />);
    expect(screen.getByText("PJ · 38.221.907/0001-53 · IE 123.456.789")).toBeTruthy();
  });

  it.each([
    ["PJ sem IE", { ...base, inscricaoEstadual: null }],
    ["PJ com IE vazia", { ...base, inscricaoEstadual: "  " }],
    ["resposta antiga sem o campo", base],
  ])("não mostra IE para %s", (_caso, cliente) => {
    render(<CartaoCliente cliente={cliente} />);
    expect(screen.queryByText(/IE/)).toBeNull();
  });

  it("não mostra IE para pessoa física", () => {
    render(<CartaoCliente cliente={{ ...base, tipoPessoa: "PessoaFisica", documento: "31844296016", inscricaoEstadual: "123" }} />);
    expect(screen.queryByText(/IE/)).toBeNull();
  });
});
