import { Suspense } from "react";
import type { Metadata } from "next";
import { Carregando } from "@/components/ui";
import { DetalheCliente } from "@/features/clientes/DetalheCliente";

export const metadata: Metadata = { title: "Cliente" };

export default function Pagina() {
  return <Suspense fallback={<Carregando />}><DetalheCliente /></Suspense>;
}
