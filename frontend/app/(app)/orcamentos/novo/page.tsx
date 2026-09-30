import { Suspense } from "react";
import type { Metadata } from "next";
import { Carregando } from "@/components/ui";
import { FormularioOrcamento } from "@/features/orcamentos/FormularioOrcamento";

export const metadata: Metadata = { title: "Novo orçamento" };

export default function Pagina() {
  return <Suspense fallback={<Carregando />}><FormularioOrcamento /></Suspense>;
}
