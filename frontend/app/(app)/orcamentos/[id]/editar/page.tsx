import { Suspense } from "react";
import type { Metadata } from "next";
import { Carregando } from "@/components/ui";
import { EditarOrcamento } from "@/features/orcamentos/EditarOrcamento";

export const metadata: Metadata = { title: "Editar orçamento" };

export default function Pagina() {
  return <Suspense fallback={<Carregando />}><EditarOrcamento /></Suspense>;
}
