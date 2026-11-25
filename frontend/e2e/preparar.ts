import { request, type FullConfig } from "@playwright/test";

// Compose --wait não verifica a API neste projeto. Antes das ações de escrita,
// aguarda uma leitura autenticada responder pelo proxy, sem cookies ou retries de POST.
export default async function preparar(config: FullConfig) {
  const api = await request.newContext({ baseURL: config.projects[0].use.baseURL, timeout: 3000 });
  const limite = Date.now() + 30_000;
  let status = 0;
  try {
    while (Date.now() < limite) {
      try { status = (await api.get("/api/auth/me")).status(); } catch { status = 0; }
      if (status === 401) return;
      await new Promise(resolve => setTimeout(resolve, 500));
    }
    throw new Error(`A API não ficou pronta pelo proxy do frontend (HTTP ${status || "sem resposta"}).`);
  } finally { await api.dispose(); }
}
