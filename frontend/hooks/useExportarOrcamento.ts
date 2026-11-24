"use client";
import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useToast } from "@/components/ui/toast";
import { erroDaApi } from "@/lib/api";
import { orcamentosService } from "@/services";

export function useExportarOrcamento(id: string, clienteId: string, registrar = true) {
  const [carregando, setCarregando] = useState(false);
  const toast = useToast();
  const qc = useQueryClient();
  async function exportar(inline = false) {
    const aba = inline ? window.open("about:blank", "_blank") : null;
    if (inline && !aba) { toast("erro", "Permita abrir novas abas no navegador e tente novamente."); return; }
    if (aba) aba.opener = null;
    setCarregando(true);
    try {
      const { arquivo, nome } = await orcamentosService.exportarPdf(id, inline, registrar);
      const url = URL.createObjectURL(arquivo);
      if (aba) aba.location.replace(url);
      else {
        const link = document.createElement("a");
        link.href = url; link.download = nome;
        document.body.appendChild(link); link.click(); link.remove();
      }
      window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
      if (registrar) {
        void qc.invalidateQueries({ queryKey: ["orcamento", id] });
        void qc.invalidateQueries({ queryKey: ["cliente", clienteId] });
      }
      toast("sucesso", registrar ? "PDF exportado com sucesso." : "PDF preparado com sucesso.");
    } catch (erro) {
      aba?.close();
      toast("erro", erroDaApi(erro).message);
    } finally { setCarregando(false); }
  }
  return { carregando, exportar };
}
