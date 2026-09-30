import { Suspense } from "react";
import type { Metadata } from "next";
import { Carregando } from "@/components/ui";
import { FormularioCliente } from "@/features/clientes/FormularioCliente";

export const metadata: Metadata = { title: "Novo cliente" };

export default function Pagina() {
  return <Suspense fallback={<Carregando />}><FormularioCliente /></Suspense>;
}
