/**
 * Retorna o destino pós-login somente se for um caminho interno do próprio sistema; caso contrário, o padrão.
 * Bloqueia redirecionamento aberto: "//site", "/\site", "\\site", "https://site", "javascript:" etc.
 */
export function destinoInterno(destino: string | null | undefined, origem: string, padrao = "/dashboard"): string {
  if (!destino || !destino.startsWith("/") || destino.startsWith("//") || destino.includes("\\")) return padrao;
  if (/[\u0000-\u001F\u007F]/.test(destino)) return padrao;
  try {
    const url = new URL(destino, origem);
    if (url.origin !== new URL(origem).origin) return padrao;
    return url.pathname + url.search + url.hash;
  } catch {
    return padrao;
  }
}
