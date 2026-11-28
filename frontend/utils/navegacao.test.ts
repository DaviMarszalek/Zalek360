import { describe, expect, it } from "vitest";
import { destinoInterno } from "./navegacao";

const origem = "http://localhost:3000";

describe("destinoInterno (redirecionamento após o login)", () => {
  it.each([
    ["/orcamentos/123", "/orcamentos/123"],
    ["/pedidos?status=Aberto#topo", "/pedidos?status=Aberto#topo"],
    ["/", "/"],
  ])("aceita caminho interno %s", (destino, esperado) => {
    expect(destinoInterno(destino, origem)).toBe(esperado);
  });

  it.each([
    "/\\example.com/", // revisão M5: o navegador trata "/\" como "//"
    "//example.com",
    "\\\\example.com",
    "https://example.com",
    "javascript:alert(1)",
    "/\t/example.com",
    "dashboard",
    "",
    null,
  ])("recusa destino externo ou inválido %s", (destino) => {
    expect(destinoInterno(destino, origem)).toBe("/dashboard");
  });
});
