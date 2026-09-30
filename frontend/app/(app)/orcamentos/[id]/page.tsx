import { Suspense } from "react";
import type { Metadata } from "next";
import { Carregando } from "@/components/ui";
import { DetalheOrcamento } from "@/features/orcamentos/DetalheOrcamento";

export const metadata: Metadata = { title: "Orçamento" };

export default function Pagina() {
  return <Suspense fallback={<Carregando />}><DetalheOrcamento /></Suspense>;
}
