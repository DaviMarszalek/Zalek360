import { Suspense } from "react";
import type { Metadata } from "next";
import { Carregando } from "@/components/ui";
import { ListaOrcamentos } from "@/features/orcamentos/ListaOrcamentos";

export const metadata: Metadata = { title: "Orçamentos" };

export default function Pagina() {
  return <Suspense fallback={<Carregando />}><ListaOrcamentos /></Suspense>;
}
