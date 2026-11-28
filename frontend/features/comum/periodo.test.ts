import { describe, expect, it } from "vitest";
import { intervaloDoPeriodo, periodoDaUrl } from "./index";

describe("periodoDaUrl", () => {
  it.each(["7", "30", "90", "mes", "todos"])("aceita %s", (valor) => {
    expect(periodoDaUrl(valor)).toBe(valor);
  });

  it.each(["abc", "", "15", "personalizado", null])("ignora %s fora da lista", (valor) => {
    expect(periodoDaUrl(valor)).toBeNull();
  });

  it("aceita personalizado só onde a tela oferece datas", () => {
    expect(periodoDaUrl("personalizado", true)).toBe("personalizado");
    expect(periodoDaUrl("abc", true)).toBeNull();
  });

  it("todo período válido gera um intervalo sem exceção (revisão B1)", () => {
    for (const p of ["7", "30", "90", "mes", "todos"] as const) expect(() => intervaloDoPeriodo(p)).not.toThrow();
  });
});
