// Formatação pt-BR. O backend guarda documentos/telefones só com dígitos e datas em UTC.
const FUSO = "America/Sao_Paulo";

const moedaFmt = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });
const numeroFmt = new Intl.NumberFormat("pt-BR");
const dataHoraFmt = new Intl.DateTimeFormat("pt-BR", { timeZone: FUSO, day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
const horaFmt = new Intl.DateTimeFormat("pt-BR", { timeZone: FUSO, hour: "2-digit", minute: "2-digit" });
const dataDeInstanteFmt = new Intl.DateTimeFormat("pt-BR", { timeZone: FUSO, day: "2-digit", month: "2-digit", year: "numeric" });

export const moeda = (valor: number | null | undefined) => moedaFmt.format(valor ?? 0);
export const numero = (valor: number) => numeroFmt.format(valor);
export const numeroDocumento = (n: number) => `#${String(n).padStart(6, "0")}`;
export const somenteDigitos = (v: string | null | undefined) => (v ?? "").replace(/\D/g, "");

/** "2026-09-24" → "24/09/2026" (DateOnly, sem conversão de fuso). */
export function data(valor: string | null | undefined): string {
  if (!valor) return "—";
  const [a, m, d] = valor.slice(0, 10).split("-");
  return `${d}/${m}/${a}`;
}

/** Instante UTC → "24/09/2026 14:32" no horário de Brasília. */
export const dataHora = (iso: string | null | undefined) => (iso ? dataHoraFmt.format(new Date(iso)).replace(",", "") : "—");
export const hora = (iso: string) => horaFmt.format(new Date(iso));
export const dataDeInstante = (iso: string) => dataDeInstanteFmt.format(new Date(iso));

/** Data de hoje (AAAA-MM-DD) no fuso de negócio. */
export function hojeIso(): string {
  const partes = new Intl.DateTimeFormat("en-CA", { timeZone: FUSO, year: "numeric", month: "2-digit", day: "2-digit" }).format(new Date());
  return partes;
}

export function somarDias(iso: string, dias: number): string {
  const [a, m, d] = iso.split("-").map(Number);
  const dt = new Date(Date.UTC(a, m - 1, d + dias));
  return dt.toISOString().slice(0, 10);
}

/** Valor para <input type="datetime-local"> com a hora atual de Brasília. */
export function agoraLocalInput(): string {
  const f = new Intl.DateTimeFormat("sv-SE", { timeZone: FUSO, year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit" });
  return f.format(new Date()).replace(" ", "T");
}

/** "2026-09-24T14:32" (horário de Brasília) → ISO com offset, como o backend espera. */
export const localInputParaIso = (valor: string) => (valor ? `${valor}:00-03:00` : null);

export function diasEntre(inicioIso: string, fimIso: string): number {
  const a = Date.parse(inicioIso.slice(0, 10) + "T00:00:00Z");
  const b = Date.parse(fimIso.slice(0, 10) + "T00:00:00Z");
  return Math.round((b - a) / 86_400_000);
}

export function relativo(iso: string | null | undefined): string {
  if (!iso) return "";
  const dias = diasEntre(dataIsoDeInstante(iso), hojeIso());
  if (dias <= 0) return `hoje às ${hora(iso)}`;
  if (dias === 1) return "ontem";
  return `há ${dias} dias`;
}

export function dataIsoDeInstante(iso: string): string {
  return new Intl.DateTimeFormat("en-CA", { timeZone: FUSO, year: "numeric", month: "2-digit", day: "2-digit" }).format(new Date(iso));
}

export function documento(valor: string | null | undefined): string {
  const d = somenteDigitos(valor);
  if (d.length === 11) return d.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, "$1.$2.$3-$4");
  if (d.length === 14) return d.replace(/(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})/, "$1.$2.$3/$4-$5");
  return valor ?? "";
}

export function mascaraDocumento(valor: string, tipo: "PessoaFisica" | "PessoaJuridica"): string {
  const d = somenteDigitos(valor).slice(0, tipo === "PessoaFisica" ? 11 : 14);
  if (tipo === "PessoaFisica")
    return d.replace(/^(\d{3})(\d)/, "$1.$2").replace(/^(\d{3})\.(\d{3})(\d)/, "$1.$2.$3").replace(/\.(\d{3})(\d{1,2})$/, ".$1-$2");
  return d.replace(/^(\d{2})(\d)/, "$1.$2").replace(/^(\d{2})\.(\d{3})(\d)/, "$1.$2.$3").replace(/\.(\d{3})(\d)/, ".$1/$2").replace(/(\d{4})(\d{1,2})$/, "$1-$2");
}

export function telefone(valor: string | null | undefined): string {
  const d = somenteDigitos(valor);
  if (d.length === 11) return d.replace(/(\d{2})(\d{5})(\d{4})/, "($1) $2-$3");
  if (d.length === 10) return d.replace(/(\d{2})(\d{4})(\d{4})/, "($1) $2-$3");
  return valor ?? "";
}

export function mascaraTelefone(valor: string): string {
  const d = somenteDigitos(valor).slice(0, 11);
  if (d.length <= 2) return d.length ? `(${d}` : "";
  if (d.length <= 6) return `(${d.slice(0, 2)}) ${d.slice(2)}`;
  if (d.length <= 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
}

export const mascaraCep = (valor: string) => somenteDigitos(valor).slice(0, 8).replace(/^(\d{5})(\d)/, "$1-$2");

export function tamanhoArquivo(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1).replace(".", ",")} MB`;
}

/**
 * Converte texto digitado para número, aceitando o padrão pt-BR e o ponto decimal:
 * - com vírgula: vírgula é o decimal e pontos são milhares ("1.234,50" → 1234.5; "40,50" → 40.5);
 * - sem vírgula, só com grupos de três dígitos após cada ponto: milhares ("1.234" → 1234; "1.234.567" → 1234567);
 * - sem vírgula, com um único ponto: ponto decimal ("40.50" → 40.5; "1.5" → 1.5).
 * Formatos ambíguos (duas vírgulas, vários pontos fora do padrão de milhares) retornam null.
 */
export function paraNumero(texto: string): number | null {
  const limpo = texto.replace(/[^\d,.-]/g, "");
  if (limpo === "" || limpo === "-") return null;
  let normal: string;
  if (limpo.includes(",")) {
    if ((limpo.match(/,/g) ?? []).length > 1) return null;
    normal = limpo.replace(/\./g, "").replace(",", ".");
  } else if (/^-?\d{1,3}(\.\d{3})+$/.test(limpo)) {
    normal = limpo.replace(/\./g, "");
  } else {
    if ((limpo.match(/\./g) ?? []).length > 1) return null;
    normal = limpo;
  }
  const n = Number(normal);
  return Number.isFinite(n) ? n : null;
}

export const paraTextoDecimal = (n: number | null | undefined) =>
  n === null || n === undefined ? "" : n.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
