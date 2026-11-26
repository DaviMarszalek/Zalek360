import { describe, expect, it } from "vitest";
import { paraNumero } from "./formatos";

describe("paraNumero", () => {
  it.each([
    ["40,50", 40.5],
    ["40.50", 40.5],
    ["1.5", 1.5],
    ["1.234,56", 1234.56],
    ["1.234", 1234],
    ["1.234.567", 1234567],
    ["1234.5", 1234.5],
    ["R$ 2.500,00", 2500],
    ["80", 80],
    ["0,004", 0.004],
    ["-3,5", -3.5],
  ])("interpreta %s como %s", (texto, esperado) => {
    expect(paraNumero(texto)).toBe(esperado);
  });

  it.each(["", "-", "abc", "1,2,3", "1.2.3", "12.34.56"])("rejeita %s", (texto) => {
    expect(paraNumero(texto)).toBeNull();
  });

  it("não multiplica por 100 um valor digitado com ponto decimal (revisão M1)", () => {
    expect(paraNumero("40.50")).not.toBe(4050);
  });
});
