"use client";
import { useState } from "react";
import { FileImage, FileText } from "lucide-react";
import { anexosService } from "@/services";
import type { Anexo } from "@/types/api";

export function ReferenciaAnexo({ anexo }: { anexo: Anexo }) {
  const [falhou, setFalhou] = useState(false);
  return anexo.ehImagem && !falhou
    ? <img src={anexosService.url(anexo.id, true)} alt={`Prévia de ${anexo.nome}`} width={64} height={64} loading="lazy" decoding="async" onError={() => setFalhou(true)} className="h-16 w-16 shrink-0 rounded border border-borda bg-white object-contain" />
    : anexo.ehImagem ? <FileImage aria-label="Prévia indisponível" className="h-5 w-5 shrink-0 text-texto-fraco" /> : <FileText className="h-5 w-5 shrink-0 text-texto-fraco" />;
}
